using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using FunnySharp;

public static class AvailabilityCoordinator
{
    public static async Task<IReadOnlyList<ItemAvailability>> CheckAvailabilityAsync(
        IReadOnlyList<StockItem> items,
        WarehouseGateway gateway,
        int maxConcurrency,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(gateway);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxConcurrency);
        cancellationToken.ThrowIfCancellationRequested();

        var checks = EnumerateItems(items).SelectParallelValueAsync(
            maxConcurrency,
            async (item, operationToken) =>
            {
                var reply = await gateway.CheckAsync(item.Sku, operationToken)
                    .ConfigureAwait(false);
                return new ItemAvailability(
                    item.Sku, reply.OnHand, reply.OnHand >= item.Quantity);
            });

        var results = new List<ItemAvailability>(items.Count);
        // The ordered streaming coordinator bounds admission and drains started
        // work when enumeration fails, is canceled, or is disposed early.
        await foreach (var result in checks.WithCancellation(cancellationToken)
            .ConfigureAwait(false))
        {
            results.Add(result);
        }

        cancellationToken.ThrowIfCancellationRequested();
        return results.AsReadOnly();
    }

    public static async Task<ReservationOutcome> ReserveFirstAsync(
        IReadOnlyList<string> suppliers,
        SupplierGateway gateway,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(suppliers);
        ArgumentNullException.ThrowIfNull(gateway);
        cancellationToken.ThrowIfCancellationRequested();

        var names = suppliers.ToArray();
        if (names.Length == 0)
            return new ReservationOutcome(false, null, null, Array.Empty<string>());

        using var operation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var canceled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var registration = cancellationToken.Register(() => canceled.TrySetResult());

        var tasks = new List<Task<SupplierReply>>(names.Length);
        // This protocol requires launching all suppliers before inspecting replies,
        // including when replies complete synchronously. Use explicit BCL admission
        // rather than a cold-effect race that can stop admitting after an early win.
        foreach (var name in names)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                tasks.Add(gateway.ProbeAsync(name, operation.Token)
                    ?? Task.FromException<SupplierReply>(
                        new InvalidOperationException("The supplier gateway returned a null task.")));
            }
            catch (Exception error)
            {
                // Retain a synchronous launch failure while still accounting for
                // every other call that was started.
                tasks.Add(Task.FromException<SupplierReply>(error));
            }
        }

        var winner = Option.None<(string SupplierId, string? ReservationId)>();
        var observed = new bool[tasks.Count];
        var remaining = tasks.Count;
        var terminalFailure = false;

        while (remaining > 0 && !cancellationToken.IsCancellationRequested)
        {
            // Inspect each ready batch in input order. Once selected, the winner
            // is immutable; cleanup replies are never candidates for replacement.
            for (var index = 0; index < tasks.Count; index++)
            {
                var task = tasks[index];
                if (observed[index] || !task.IsCompleted)
                    continue;

                observed[index] = true;
                remaining--;
                if (!task.IsCompletedSuccessfully)
                {
                    terminalFailure = true;
                    continue;
                }

                var reply = task.GetAwaiter().GetResult();
                if (winner.IsNone && reply.Accepts)
                    winner = Option.Some((SupplierId: names[index], ReservationId: reply.ReservationId));
            }

            if (winner.IsSome || terminalFailure || remaining == 0)
                break;

            var waitSet = new List<Task>(remaining + 1) { canceled.Task };
            for (var index = 0; index < tasks.Count; index++)
                if (!observed[index])
                    waitSet.Add(tasks[index]);

            await Task.WhenAny(waitSet).ConfigureAwait(false);
        }

        var cancellationFaults = new List<Exception>();
        if (winner.IsSome || terminalFailure || cancellationToken.IsCancellationRequested)
        {
            try
            {
                operation.Cancel();
            }
            catch (AggregateException error)
            {
                cancellationFaults.AddRange(error.InnerExceptions);
            }
            catch (Exception error)
            {
                cancellationFaults.Add(error);
            }
        }

        // WhenAll waits for the exact gateway tasks, including their cleanup and
        // final accounting. Its await exception is inspected per task below so
        // neither additional faults nor faulted OCEs disappear.
        try
        {
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Every terminal failure is collected below after all calls exit.
        }

        var failures = new List<Exception>();
        foreach (var task in tasks)
        {
            if (task.IsFaulted)
            {
                failures.AddRange(task.Exception!.InnerExceptions);
            }
            else if (task.IsCanceled)
            {
                try
                {
                    await task.ConfigureAwait(false);
                }
                catch (OperationCanceledException error)
                {
                    // Only cancellation of our own operation is a loser artifact.
                    if (!operation.IsCancellationRequested || error.CancellationToken != operation.Token)
                        failures.Add(error);
                }
            }
        }
        failures.AddRange(cancellationFaults);

        // Publication happens only after draining. Caller cancellation therefore
        // also takes precedence when it arrives during loser cleanup.
        if (cancellationToken.IsCancellationRequested)
            failures.Insert(0, new OperationCanceledException(cancellationToken));

        ThrowFailures(failures);
        return winner.Match(
            selected => new ReservationOutcome(
                true, selected.SupplierId, selected.ReservationId, Array.Empty<string>()),
            () => new ReservationOutcome(false, null, null, Array.AsReadOnly(names)));
    }

    private static async IAsyncEnumerable<StockItem> EnumerateItems(
        IReadOnlyList<StockItem> items,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Completed await: adapting a synchronous source adds no scheduling.
        await Task.CompletedTask.ConfigureAwait(false);
        for (var index = 0; index < items.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return items[index];
        }
    }

    private static void ThrowFailures(List<Exception> failures)
    {
        if (failures.Count == 1)
            ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count > 1)
            throw new AggregateException(failures);
    }
}
