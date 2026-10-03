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
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxConcurrency);
        cancellationToken.ThrowIfCancellationRequested();

        var input = items.ToArray();
        if (input.Length == 0)
            return Array.Empty<ItemAvailability>();

        using var operation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var operationToken = operation.Token;
        var results = new ItemAvailability[input.Length];
        var calls = new Task<WarehouseReply>?[input.Length];
        var invocationErrors = new Exception?[input.Length];
        var cancellationErrors = new ConcurrentQueue<Exception>();
        var nextIndex = -1;

        async Task WorkerAsync()
        {
            while (!operationToken.IsCancellationRequested)
            {
                var index = Interlocked.Increment(ref nextIndex);
                if (index >= input.Length || operationToken.IsCancellationRequested)
                    return;

                StockItem item;
                Task<WarehouseReply> call;
                try
                {
                    item = input[index];
                    call = gateway.CheckAsync(item.Sku, operationToken)
                        ?? throw new InvalidOperationException("The warehouse gateway returned a null task.");
                    calls[index] = call;
                }
                catch (Exception error)
                {
                    invocationErrors[index] = error;
                    CancelAndRecord(operation, cancellationErrors);
                    return;
                }

                try
                {
                    var reply = await call.ConfigureAwait(false);
                    results[index] = new ItemAvailability(
                        item.Sku, reply.OnHand, reply.OnHand >= item.Quantity);
                }
                catch (Exception error)
                {
                    // Failed gateway tasks are inspected after every worker exits.
                    // A failure after a successful task is a separate processing error.
                    if (call.IsCompletedSuccessfully)
                        invocationErrors[index] = error;
                    CancelAndRecord(operation, cancellationErrors);
                    return;
                }
            }
        }

        var workers = new Task[Math.Min(maxConcurrency, input.Length)];
        for (var i = 0; i < workers.Length; i++)
            workers[i] = WorkerAsync();

        // Each worker waits for its actual gateway task, including gateway cleanup,
        // before releasing its slot or exiting.
        await Task.WhenAll(workers).ConfigureAwait(false);

        var failures = ReadFailures(calls, operationToken);
        foreach (var error in invocationErrors)
        {
            if (error is not null)
                failures.Add(error);
        }
        failures.AddRange(cancellationErrors);
        ThrowBeforePublication(failures, cancellationToken);
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
        var operationToken = operation.Token;
        var cancellationErrors = new ConcurrentQueue<Exception>();
        var calls = new Task<SupplierReply>[supplierIds.Length];

        // The supplied workload has at most 32 suppliers. Start the entire batch
        // before inspecting replies, including replies completed synchronously.
        for (var i = 0; i < supplierIds.Length; i++)
        {
            try
            {
                calls[i] = gateway.ProbeAsync(supplierIds[i], operationToken)
                    ?? throw new InvalidOperationException("The supplier gateway returned a null task.");
            }
            catch (Exception error)
            {
                // Retain synchronous failures without abandoning already-started calls.
                calls[i] = Task.FromException<SupplierReply>(error);
            }
        }

        var observed = new bool[calls.Length];
        var remaining = calls.Length;
        var winner = -1;
        SupplierReply? winningReply = null;
        var failed = false;

        while (remaining > 0 && winner < 0 && !failed)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var observedCompletion = false;
            for (var i = 0; i < calls.Length; i++)
            {
                var call = calls[i];
                if (observed[i] || !call.IsCompleted)
                    continue;

                observed[i] = true;
                remaining--;
                observedCompletion = true;

                if (!call.IsCompletedSuccessfully)
                {
                    failed = true;
                    continue;
                }

                // Tasks are terminal here. Input-order inspection breaks ties
                // among successes already ready at the observation boundary.
                var reply = call.GetAwaiter().GetResult();
                if (reply.Accepts && winner < 0)
                {
                    winner = i;
                    winningReply = reply;
                }
            }

            if (winner >= 0 || failed || remaining == 0 || cancellationToken.IsCancellationRequested)
                break;

            if (!observedCompletion)
            {
                var pending = new List<Task<SupplierReply>>(remaining);
                for (var i = 0; i < calls.Length; i++)
                {
                    if (!observed[i])
                        pending.Add(calls[i]);
                }
                await Task.WhenAny(pending).ConfigureAwait(false);
            }
        }

        if (winner >= 0 || failed || cancellationToken.IsCancellationRequested)
            CancelAndRecord(operation, cancellationErrors);

        // Winner selection is frozen. Late successful loser replies cannot replace it.
        // WhenAll waits for every terminal gateway task, not merely cleanup entry.
        try
        {
            await Task.WhenAll(calls).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // Inspect all original tasks below: awaiting WhenAll alone exposes only
            // one exception and could conceal another independent cleanup fault.
        }

        var failures = ReadFailures(calls, operationToken);
        failures.AddRange(cancellationErrors);

        var outcome = winner >= 0
            ? new ReservationOutcome(
                true, supplierIds[winner], winningReply!.ReservationId, Array.Empty<string>())
            : new ReservationOutcome(false, null, null, supplierIds);

        ThrowBeforePublication(failures, cancellationToken);
        return outcome;
    }

    private static void CancelAndRecord(
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

    private static List<Exception> ReadFailures(
        IEnumerable<Task?> calls,
        CancellationToken operationToken)
    {
        var failures = new List<Exception>();
        foreach (var call in calls)
        {
            if (call is null)
                continue;

            if (call.IsFaulted)
            {
                // Preserve every fault, including a faulted OperationCanceledException.
                failures.AddRange(call.Exception!.InnerExceptions);
            }
            else if (call.IsCanceled)
            {
                try
                {
                    call.GetAwaiter().GetResult();
                }
                catch (OperationCanceledException error)
                {
                    // Only cancellation attributable to our canceled operation token
                    // is an expected loser/worker shutdown, rather than an independent failure.
                    if (!operationToken.IsCancellationRequested ||
                        error.CancellationToken != operationToken)
                    {
                        failures.Add(error);
                    }
                }
            }
        }
        return failures;
    }

    private static void ThrowBeforePublication(
        List<Exception> failures,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            if (failures.Count == 0)
                cancellationToken.ThrowIfCancellationRequested();

            // Caller cancellation takes precedence, but independent cleanup failures
            // must remain observable rather than disappearing behind cancellation.
            failures.Insert(0, new OperationCanceledException(cancellationToken));
        }

        if (failures.Count == 1)
            ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count > 1)
            throw new AggregateException(failures);
    }
}
