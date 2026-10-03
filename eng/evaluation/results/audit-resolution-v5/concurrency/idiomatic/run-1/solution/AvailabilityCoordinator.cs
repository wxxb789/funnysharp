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
            return Array.Empty<ItemAvailability>();

        var results = new ItemAvailability[items.Count];
        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = maxConcurrency,
            CancellationToken = cancellationToken,
        };

        // ForEachAsync bounds admission and waits for every started body, including
        // its gateway cleanup, before completing on success, failure or cancellation.
        await Parallel.ForEachAsync(
            Enumerable.Range(0, items.Count),
            options,
            async (index, operationToken) =>
            {
                var item = items[index];
                var reply = await gateway.CheckAsync(item.Sku, operationToken)
                    .ConfigureAwait(false);
                results[index] = new ItemAvailability(
                    item.Sku, reply.OnHand, reply.OnHand >= item.Quantity);
            }).ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();
        return Array.AsReadOnly(results);
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
        var probes = new Task<SupplierReply>[names.Length];

        // Start the entire finite study input before observing any winner. A
        // synchronous invocation failure is tracked alongside asynchronous failures.
        for (var index = 0; index < names.Length; index++)
        {
            try
            {
                probes[index] = gateway.ProbeAsync(names[index], operation.Token);
            }
            catch (Exception error)
            {
                probes[index] = Task.FromException<SupplierReply>(error);
            }
        }

        var pending = Enumerable.Range(0, probes.Length).ToList();
        var winnerIndex = -1;
        SupplierReply? winnerReply = null;
        var stoppedByFailure = false;

        while (pending.Count != 0 && winnerIndex < 0 && !stoppedByFailure
            && !cancellationToken.IsCancellationRequested)
        {
            await Task.WhenAny(pending.Select(index => probes[index]))
                .ConfigureAwait(false);

            // Observe a ready batch in input order, including the case in which
            // several accepting replies were already complete at invocation.
            var ready = pending.Where(index => probes[index].IsCompleted).ToArray();
            foreach (var index in ready)
            {
                pending.Remove(index);
                var probe = probes[index];
                if (!probe.IsCompletedSuccessfully)
                {
                    stoppedByFailure = true;
                    break;
                }

                var reply = await probe.ConfigureAwait(false);
                if (reply.Accepts)
                {
                    winnerIndex = index;
                    winnerReply = reply;
                    break;
                }
            }
        }

        Exception? cancellationFault = null;
        if (winnerIndex >= 0 || stoppedByFailure || cancellationToken.IsCancellationRequested)
        {
            try
            {
                operation.Cancel();
            }
            catch (Exception error)
            {
                // Cancellation callbacks may fail; that must not bypass draining.
                cancellationFault = error;
            }
        }

        var failures = new List<Exception>();
        foreach (var probe in probes)
        {
            try
            {
                // Await the exact gateway task, not a cancellation race or a
                // cleanup-entry signal. Late successes cannot replace the winner.
                await probe.ConfigureAwait(false);
            }
            catch (Exception error)
            {
                if (probe.IsFaulted)
                {
                    // Retain every fault represented by the Task, including a
                    // faulted OperationCanceledException rather than treating it
                    // as an artifact of our cancellation request.
                    failures.AddRange(probe.Exception!.InnerExceptions);
                }
                else if (!(probe.IsCanceled
                    && operation.IsCancellationRequested
                    && error is OperationCanceledException canceled
                    && canceled.CancellationToken == operation.Token))
                {
                    failures.Add(error);
                }
            }
        }

        if (cancellationFault is AggregateException callbackFailures)
            failures.AddRange(callbackFailures.InnerExceptions);
        else if (cancellationFault is not null)
            failures.Add(cancellationFault);

        // Publication happens only after all started calls have exited. Caller
        // cancellation wins over a reservation, without discarding cleanup faults.
        if (cancellationToken.IsCancellationRequested)
        {
            var canceled = new OperationCanceledException(cancellationToken);
            if (failures.Count != 0)
            {
                failures.Insert(0, canceled);
                throw new AggregateException(failures);
            }

            throw canceled;
        }

        if (failures.Count == 1)
            ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count > 1)
            throw new AggregateException(failures);

        if (winnerIndex >= 0)
        {
            return new ReservationOutcome(
                true, names[winnerIndex], winnerReply!.ReservationId,
                Array.Empty<string>());
        }

        return new ReservationOutcome(false, null, null, Array.AsReadOnly(names));
    }
}
