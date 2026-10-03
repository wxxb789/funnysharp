using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

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

        var results = new ItemAvailability[items.Count];
        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = maxConcurrency,
            CancellationToken = cancellationToken,
        };

        // Parallel.ForEachAsync bounds admission and waits for every active body,
        // including its awaited gateway cleanup, before completing.
        await Parallel.ForEachAsync(
            Enumerable.Range(0, items.Count),
            options,
            async (index, operationToken) =>
            {
                var item = items[index];
                var reply = await gateway.CheckAsync(item.Sku, operationToken)
                    .ConfigureAwait(false);
                results[index] = new ItemAvailability(
                    item.Sku,
                    reply.OnHand,
                    reply.OnHand >= item.Quantity);
            }).ConfigureAwait(false);

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

        var supplierIds = suppliers.ToArray();
        if (supplierIds.Length == 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return new ReservationOutcome(false, null, null, Array.Empty<string>());
        }

        using var operation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var calls = new Task<SupplierReply>[supplierIds.Length];

        // Invoke every probe before observing replies. These are asynchronous I/O
        // calls; no extra thread or Task.Run is needed to make them overlap.
        for (var index = 0; index < supplierIds.Length; index++)
        {
            try
            {
                calls[index] = gateway.ProbeAsync(supplierIds[index], operation.Token);
            }
            catch (Exception error)
            {
                // A synchronous invocation failure must not orphan earlier calls.
                calls[index] = Task.FromException<SupplierReply>(error);
            }
        }

        var pending = Enumerable.Range(0, calls.Length).ToList();
        var winnerIndex = -1;
        SupplierReply? winnerReply = null;
        var observedFailure = false;

        while (pending.Count != 0 && winnerIndex < 0 &&
               !observedFailure && !cancellationToken.IsCancellationRequested)
        {
            await Task.WhenAny(pending.Select(index => calls[index]))
                .ConfigureAwait(false);

            // Resolve a ready batch in input order, including replies that were
            // already complete when the probes were started.
            for (var position = 0; position < pending.Count;)
            {
                var index = pending[position];
                var call = calls[index];
                if (!call.IsCompleted)
                {
                    position++;
                    continue;
                }

                pending.RemoveAt(position);
                if (!call.IsCompletedSuccessfully)
                {
                    observedFailure = true;
                    break;
                }

                var reply = await call.ConfigureAwait(false);
                if (reply.Accepts)
                {
                    winnerIndex = index;
                    winnerReply = reply;
                    break;
                }
            }
        }

        // Do not mistake a cancellation already observed before our own stop
        // request for a cancellation artifact created by that request.
        var independentlyCanceled = new bool[calls.Length];
        if (!operation.IsCancellationRequested)
        {
            for (var index = 0; index < calls.Length; index++)
            {
                independentlyCanceled[index] = calls[index].IsCanceled;
            }
        }

        var cancellationFaults = new List<Exception>();
        if (winnerIndex >= 0 || observedFailure || cancellationToken.IsCancellationRequested)
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

        var failures = new List<Exception>();
        for (var index = 0; index < calls.Length; index++)
        {
            var call = calls[index];
            try
            {
                // Await the actual gateway task, not merely a reply or a cleanup
                // notification. Late successful losers cannot replace the winner.
                await call.ConfigureAwait(false);
            }
            catch (Exception error)
            {
                if (call.IsFaulted)
                {
                    // Retain all exceptions represented by the task, including
                    // faulted OperationCanceledExceptions and cleanup failures.
                    failures.AddRange(call.Exception!.InnerExceptions);
                }
                else if (error is OperationCanceledException canceled &&
                         !independentlyCanceled[index] &&
                         operation.IsCancellationRequested &&
                         canceled.CancellationToken == operation.Token)
                {
                    // Expected cancellation of a started sibling is not a fault.
                }
                else
                {
                    failures.Add(error);
                }
            }
        }

        failures.AddRange(cancellationFaults);

        var outcome = winnerIndex >= 0
            ? new ReservationOutcome(
                true,
                supplierIds[winnerIndex],
                winnerReply!.ReservationId,
                Array.Empty<string>())
            : new ReservationOutcome(false, null, null, Array.AsReadOnly(supplierIds));

        // Publication happens only after all started gateway tasks are terminal.
        // Caller cancellation during loser cleanup still takes precedence.
        ThrowFailures(failures, cancellationToken);
        return outcome;
    }

    private static void ThrowFailures(
        List<Exception> failures,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            if (failures.Count == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            failures.Insert(0, new OperationCanceledException(cancellationToken));
            throw new AggregateException(failures);
        }

        if (failures.Count == 1)
        {
            ExceptionDispatchInfo.Capture(failures[0]).Throw();
        }

        if (failures.Count > 1)
        {
            throw new AggregateException(failures);
        }
    }
}
