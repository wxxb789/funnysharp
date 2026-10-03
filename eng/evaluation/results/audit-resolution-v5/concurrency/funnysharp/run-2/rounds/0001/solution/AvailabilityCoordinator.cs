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
        if (maxConcurrency <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxConcurrency));

        cancellationToken.ThrowIfCancellationRequested();
        var snapshot = items.ToArray();
        var results = new List<ItemAvailability>(snapshot.Length);

        // FunnySharp supplies source ordering, bounded admission, and cancel-and-drain
        // disposal. Gateway tasks include their complete cleanup and accounting.
        var checks = Enumerate(snapshot).SelectParallelValueAsync(
            maxConcurrency,
            async (item, operationToken) =>
            {
                var reply = await gateway.CheckAsync(item.Sku, operationToken)
                    .ConfigureAwait(false);
                return new ItemAvailability(
                    item.Sku, reply.OnHand, reply.OnHand >= item.Quantity);
            });

        await foreach (var result in checks.WithCancellation(cancellationToken)
            .ConfigureAwait(false))
        {
            results.Add(result);
        }

        cancellationToken.ThrowIfCancellationRequested();
        return results;
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
        using var operation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var callerCanceled = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        using var registration = cancellationToken.Register(
            static state => ((TaskCompletionSource)state!).TrySetResult(), callerCanceled);

        // Launch the whole bounded input before observing replies. In particular,
        // a synchronously completed acceptance must not skip subsequent probes.
        var tasks = new Task<SupplierReply>[names.Length];
        for (var index = 0; index < names.Length; index++)
        {
            try
            {
                tasks[index] = gateway.ProbeAsync(names[index], operation.Token);
            }
            catch (Exception error)
            {
                // Preserve a synchronous launch failure while still accounting for
                // every other launched call through the shared drain below.
                tasks[index] = Task.FromException<SupplierReply>(error);
            }
        }

        var waiting = new List<Task>(tasks.Length + 1);
        waiting.AddRange(tasks);
        waiting.Add(callerCanceled.Task);
        var observed = new bool[tasks.Length];
        var remaining = tasks.Length;
        var stopForFailure = false;
        SupplierReply? winner = null;
        string? winnerSupplier = null;

        while (remaining > 0 && !cancellationToken.IsCancellationRequested)
        {
            await Task.WhenAny(waiting).ConfigureAwait(false);
            if (cancellationToken.IsCancellationRequested)
                break;

            // Resolve the observed ready batch in input order. Once selected,
            // the winner is never reconsidered during cancellation or cleanup.
            for (var index = 0; index < tasks.Length; index++)
            {
                var task = tasks[index];
                if (observed[index] || !task.IsCompleted)
                    continue;

                observed[index] = true;
                remaining--;
                waiting.Remove(task);
                if (!task.IsCompletedSuccessfully)
                {
                    stopForFailure = true;
                    continue;
                }

                var reply = task.GetAwaiter().GetResult();
                if (winner is null && reply.Accepts)
                {
                    winner = reply;
                    winnerSupplier = names[index];
                }
            }

            if (winner is not null || stopForFailure)
                break;
        }

        var cancellationFaults = new List<Exception>();
        if (winner is not null || stopForFailure || cancellationToken.IsCancellationRequested)
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

        try
        {
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // WhenAll has drained every task. Inspect each terminal state below
            // instead of losing additional exceptions through await's first fault.
        }

        var failures = new List<Exception>();
        foreach (var task in tasks)
        {
            if (task.IsFaulted)
            {
                // A faulted OCE is an independent fault, not owned cancellation.
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
                    if (!operation.IsCancellationRequested ||
                        error.CancellationToken != operation.Token)
                    {
                        failures.Add(error);
                    }
                }
            }
        }
        failures.AddRange(cancellationFaults);

        // Publication happens only after all gateway tasks are terminal. Caller
        // cancellation takes precedence, including cancellation during cleanup.
        if (cancellationToken.IsCancellationRequested)
        {
            var canceled = new OperationCanceledException(cancellationToken);
            if (failures.Count == 0)
                throw canceled;
            failures.Insert(0, canceled);
            throw new AggregateException(failures);
        }

        if (failures.Count == 1)
            ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count > 1)
            throw new AggregateException(failures);

        return winner is not null
            ? new ReservationOutcome(true, winnerSupplier, winner.ReservationId, Array.Empty<string>())
            : new ReservationOutcome(false, null, null, names);
    }

    private static async IAsyncEnumerable<StockItem> Enumerate(
        IReadOnlyList<StockItem> items,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // A synchronous source adapter: no scheduler handoff or prefetching.
        await Task.CompletedTask.ConfigureAwait(false);
        for (var index = 0; index < items.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return items[index];
        }
    }
}
