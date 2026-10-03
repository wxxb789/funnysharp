using System;
using System.Collections.Concurrent;
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
        if (maxConcurrency <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxConcurrency));

        cancellationToken.ThrowIfCancellationRequested();
        int count = items.Count;
        if (count == 0)
            return Array.Empty<ItemAvailability>();

        using var operation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var results = new ItemAvailability[count];
        var calls = new Task<WarehouseReply>?[count];
        var additionalFailures = new ConcurrentQueue<Exception>();
        int nextIndex = -1;

        // A worker does not take another item until its exact gateway task has
        // terminated, including resource cleanup and active-count accounting.
        async Task WorkerAsync()
        {
            while (!operation.IsCancellationRequested)
            {
                int index = Interlocked.Increment(ref nextIndex);
                if (index >= count || operation.IsCancellationRequested)
                    return;

                Task<WarehouseReply>? call = null;
                try
                {
                    var item = items[index];
                    call = StartCall(() => gateway.CheckAsync(item.Sku, operation.Token));
                    calls[index] = call;
                    var reply = await call.ConfigureAwait(false);
                    results[index] = new ItemAvailability(
                        item.Sku, reply.OnHand, reply.OnHand >= item.Quantity);
                }
                catch (Exception error)
                {
                    // Gateway failures are collected from the original task
                    // after all workers finish. Retain other worker failures here.
                    if (call is null || call.IsCompletedSuccessfully)
                        additionalFailures.Enqueue(error);

                    CancelOperation(operation, additionalFailures);
                    return;
                }
            }
        }

        var workers = new Task[Math.Min(count, maxConcurrency)];
        for (int index = 0; index < workers.Length; index++)
            workers[index] = WorkerAsync();

        await Task.WhenAll(workers).ConfigureAwait(false);
        var failures = await DrainAsync(calls.OfType<Task>(), operation.Token)
            .ConfigureAwait(false);
        failures.AddRange(additionalFailures);
        ThrowFailures(failures, cancellationToken);
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
            return new ReservationOutcome(false, null, null, Array.Empty<string>());

        using var operation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var additionalFailures = new ConcurrentQueue<Exception>();
        var calls = new List<Task<SupplierReply>>(supplierIds.Length);

        // Start every probe before selecting, even if an earlier call completed
        // synchronously. The supplied workload contains at most 32 suppliers.
        foreach (string supplierId in supplierIds)
            calls.Add(StartCall(() => gateway.ProbeAsync(supplierId, operation.Token)));

        var pending = new List<Task<SupplierReply>>(calls);
        int winnerIndex = -1;
        SupplierReply? winner = null;

        while (pending.Count != 0 && !cancellationToken.IsCancellationRequested)
        {
            // The list retains input order, so already-completed replies are
            // considered in that order. Otherwise WhenAny observes completion.
            var completed = await Task.WhenAny(pending).ConfigureAwait(false);
            if (cancellationToken.IsCancellationRequested || !completed.IsCompletedSuccessfully)
                break;

            var reply = await completed.ConfigureAwait(false);
            pending.Remove(completed);
            if (reply.Accepts)
            {
                winnerIndex = calls.IndexOf(completed);
                winner = reply;
                break;
            }
        }

        // Freeze the winner before cancellation. Late successful loser replies
        // are only drained; they cannot replace the selected reservation.
        if (winner is not null || pending.Count != 0 || cancellationToken.IsCancellationRequested)
            CancelOperation(operation, additionalFailures);

        var failures = await DrainAsync(calls, operation.Token).ConfigureAwait(false);
        failures.AddRange(additionalFailures);

        // Publication happens only after every started task has terminated.
        // Independent faults remain observable even when there was a winner.
        ThrowFailures(failures, cancellationToken);
        return winner is null
            ? new ReservationOutcome(false, null, null, supplierIds)
            : new ReservationOutcome(
                true, supplierIds[winnerIndex], winner.ReservationId, Array.Empty<string>());
    }

    private static Task<T> StartCall<T>(Func<Task<T>> start)
    {
        try
        {
            return start() ?? Task.FromException<T>(
                new InvalidOperationException("The gateway returned a null task."));
        }
        catch (OperationCanceledException error)
        {
            var canceled = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            canceled.SetCanceled(error.CancellationToken);
            return canceled.Task;
        }
        catch (Exception error)
        {
            return Task.FromException<T>(error);
        }
    }

    private static void CancelOperation(
        CancellationTokenSource operation,
        ConcurrentQueue<Exception> failures)
    {
        try
        {
            operation.Cancel();
        }
        catch (AggregateException error)
        {
            foreach (var failure in error.InnerExceptions)
                failures.Enqueue(failure);
        }
        catch (Exception error)
        {
            failures.Enqueue(error);
        }
    }

    private static async Task<List<Exception>> DrainAsync(
        IEnumerable<Task> calls,
        CancellationToken operationToken)
    {
        var failures = new List<Exception>();
        foreach (var call in calls)
        {
            try
            {
                await call.ConfigureAwait(false);
            }
            catch (Exception error)
            {
                if (call.IsFaulted)
                {
                    // Preserve every exception represented by the original task,
                    // including faulted OperationCanceledExceptions.
                    failures.AddRange(call.Exception!.InnerExceptions);
                }
                else if (!(call.IsCanceled &&
                    operationToken.IsCancellationRequested &&
                    error is OperationCanceledException cancellation &&
                    cancellation.CancellationToken == operationToken))
                {
                    failures.Add(error);
                }
            }
        }
        return failures;
    }

    private static void ThrowFailures(List<Exception> failures, CancellationToken callerToken)
    {
        if (callerToken.IsCancellationRequested)
            failures.Insert(0, new OperationCanceledException(callerToken));

        if (failures.Count == 1)
            ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count > 1)
            throw new AggregateException(failures);
    }
}
