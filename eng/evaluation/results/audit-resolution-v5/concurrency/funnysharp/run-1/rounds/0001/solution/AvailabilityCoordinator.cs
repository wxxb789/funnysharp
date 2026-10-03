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

        if (items.Count == 0)
        {
            return Array.Empty<ItemAvailability>();
        }

        var results = new List<ItemAvailability>(items.Count);
        var checks = Enumerate(items).SelectParallelValueAsync(
            maxConcurrency,
            async (item, operationToken) =>
            {
                var reply = await gateway.CheckAsync(item.Sku, operationToken)
                    .ConfigureAwait(false);
                return new ItemAvailability(
                    item.Sku,
                    reply.OnHand,
                    reply.OnHand >= item.Quantity);
            });

        // The ordered operator bounds admission and drains started work when
        // enumeration faults, is canceled, or is disposed.
        await foreach (var result in checks
            .WithCancellation(cancellationToken)
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

        var supplierIds = suppliers.ToArray();
        if (supplierIds.Length == 0)
        {
            return new ReservationOutcome(false, null, null, Array.Empty<string>());
        }

        using var operation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var probes = new Task<SupplierReply>[supplierIds.Length];

        // Start every probe before observing replies. In particular, a
        // synchronously completed acceptance must not prevent later calls.
        for (var index = 0; index < supplierIds.Length; index++)
        {
            try
            {
                probes[index] = gateway.ProbeAsync(supplierIds[index], operation.Token);
            }
            catch (Exception error)
            {
                // Retain a synchronous invocation failure while still keeping
                // every successfully started task available for draining.
                probes[index] = Task.FromException<SupplierReply>(error);
            }
        }

        var pending = new List<Task<SupplierReply>>(probes);
        var observed = new bool[probes.Length];
        var winner = Option<ReservationOutcome>.None;
        var failedOperation = false;
        var cancellationFailures = new List<Exception>();

        while (pending.Count != 0 && winner.IsNone && !failedOperation &&
            !cancellationToken.IsCancellationRequested)
        {
            _ = await Task.WhenAny(pending).ConfigureAwait(false);

            // Inspect the ready batch in input order. The first accepting
            // reply observed here is frozen before cancellation or cleanup.
            for (var index = 0; index < probes.Length; index++)
            {
                var probe = probes[index];
                if (observed[index] || !probe.IsCompleted)
                {
                    continue;
                }

                observed[index] = true;
                pending.Remove(probe);

                if (!probe.IsCompletedSuccessfully)
                {
                    failedOperation = true;
                    break;
                }

                var reply = probe.GetAwaiter().GetResult();
                if (reply.Accepts)
                {
                    winner = Option.Some(new ReservationOutcome(
                        true,
                        supplierIds[index],
                        reply.ReservationId,
                        Array.Empty<string>()));
                    break;
                }
            }
        }

        if (winner.IsSome || failedOperation || cancellationToken.IsCancellationRequested)
        {
            try
            {
                operation.Cancel();
            }
            catch (AggregateException error)
            {
                cancellationFailures.AddRange(error.InnerExceptions);
            }
            catch (Exception error)
            {
                cancellationFailures.Add(error);
            }
        }

        var failures = new List<Exception>();

        // Await the exact gateway tasks, not cleanup-entry signals. Never
        // reconsider the winner based on a late reply from a canceled loser.
        // Even after a fault, continue awaiting every other started call.
        foreach (var probe in probes)
        {
            try
            {
                _ = await probe.ConfigureAwait(false);
            }
            catch (Exception error)
            {
                if (probe.IsFaulted)
                {
                    // Preserve all faults represented by the source task,
                    // including faulted OperationCanceledExceptions.
                    failures.AddRange(probe.Exception!.InnerExceptions);
                }
                else if (!(probe.IsCanceled &&
                    error is OperationCanceledException canceled &&
                    operation.IsCancellationRequested &&
                    canceled.CancellationToken == operation.Token))
                {
                    // Only cancellation attributable to our canceled operation
                    // token is an expected loser-cleanup artifact.
                    failures.Add(error);
                }
            }
        }

        failures.AddRange(cancellationFailures);

        // Publication happens only after all gateway tasks have terminated.
        // Caller cancellation also wins if it occurred during loser cleanup.
        if (cancellationToken.IsCancellationRequested)
        {
            if (failures.Count != 0)
            {
                failures.Insert(0, new OperationCanceledException(cancellationToken));
                throw new AggregateException(failures);
            }

            cancellationToken.ThrowIfCancellationRequested();
        }

        if (failures.Count == 1)
        {
            ExceptionDispatchInfo.Capture(failures[0]).Throw();
        }

        if (failures.Count > 1)
        {
            throw new AggregateException(failures);
        }

        return winner.Match(
            static selected => selected,
            () => new ReservationOutcome(
                false,
                null,
                null,
                Array.AsReadOnly(supplierIds)));
    }

    private static async IAsyncEnumerable<StockItem> Enumerate(
        IReadOnlyList<StockItem> items,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Adapt the finite input without introducing scheduling or prefetch.
        await Task.CompletedTask.ConfigureAwait(false);
        for (var index = 0; index < items.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return items[index];
        }
    }
}
