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
        var results = new ItemAvailability[input.Length];
        cancellationToken.ThrowIfCancellationRequested();
        if (input.Length == 0)
            return results;

        using var operation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var calls = new Task<WarehouseReply>?[input.Length];
        var independentCancellations = new bool[input.Length];
        var cancellationErrors = new ConcurrentQueue<Exception>();
        int nextIndex = -1;

        // Workers claim distinct indexes. Awaiting a gateway task includes its cleanup,
        // so a worker cannot reuse its slot while that gateway call is still active.
        async Task WorkerAsync()
        {
            while (!operation.IsCancellationRequested)
            {
                int index = Interlocked.Increment(ref nextIndex);
                if (index >= input.Length || operation.IsCancellationRequested)
                    return;

                var item = input[index];
                Task<WarehouseReply> call;
                try
                {
                    call = gateway.CheckAsync(item.Sku, operation.Token)
                        ?? throw new InvalidOperationException("The gateway returned a null task.");
                }
                catch (Exception error)
                {
                    call = Task.FromException<WarehouseReply>(error);
                }
                calls[index] = call;

                WarehouseReply reply;
                try
                {
                    reply = await call.ConfigureAwait(false);
                }
                catch
                {
                    independentCancellations[index] =
                        call.IsCanceled && !operation.IsCancellationRequested;
                    CancelAndRecord(operation, cancellationErrors);
                    return;
                }

                results[index] = new ItemAvailability(
                    item.Sku, reply.OnHand, reply.OnHand >= item.Quantity);
            }
        }

        var workers = new Task[Math.Min(maxConcurrency, input.Length)];
        for (int i = 0; i < workers.Length; i++)
            workers[i] = WorkerAsync();

        await Task.WhenAll(workers).ConfigureAwait(false);

        var errors = await CollectErrorsAsync(
            calls, independentCancellations, operation.Token).ConfigureAwait(false);
        errors.AddRange(cancellationErrors);
        ThrowBeforePublication(cancellationToken, errors);
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

        var input = suppliers.ToArray();
        cancellationToken.ThrowIfCancellationRequested();
        if (input.Length == 0)
            return new ReservationOutcome(false, null, null, Array.Empty<string>());

        using var operation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var calls = new Task<SupplierReply>[input.Length];
        var observed = new bool[input.Length];
        var independentCancellations = new bool[input.Length];
        var cancellationErrors = new ConcurrentQueue<Exception>();

        // Start every probe before selecting even an immediately available winner.
        // These are the actual gateway tasks, not cleanup-entry signals or proxies.
        for (int i = 0; i < input.Length; i++)
        {
            try
            {
                calls[i] = gateway.ProbeAsync(input[i], operation.Token)
                    ?? throw new InvalidOperationException("The gateway returned a null task.");
            }
            catch (Exception error)
            {
                calls[i] = Task.FromException<SupplierReply>(error);
            }
        }

        int remaining = calls.Length;
        int winnerIndex = -1;
        SupplierReply? winner = null;

        while (remaining > 0 && !cancellationToken.IsCancellationRequested)
        {
            bool failureObserved = false;

            // Resolve each ready batch in input order. Once chosen, the winner is
            // frozen; successes arriving during cancellation/draining cannot replace it.
            for (int i = 0; i < calls.Length; i++)
            {
                var call = calls[i];
                if (observed[i] || !call.IsCompleted)
                    continue;

                observed[i] = true;
                remaining--;
                if (call.IsCompletedSuccessfully)
                {
                    var reply = await call.ConfigureAwait(false);
                    if (winnerIndex < 0 && reply.Accepts)
                    {
                        winnerIndex = i;
                        winner = reply;
                    }
                }
                else
                {
                    failureObserved = true;
                    independentCancellations[i] =
                        call.IsCanceled && !operation.IsCancellationRequested;
                }
            }

            if (winnerIndex >= 0 || failureObserved || remaining == 0 ||
                cancellationToken.IsCancellationRequested)
                break;

            await Task.WhenAny(calls.Where((_, index) => !observed[index]))
                .ConfigureAwait(false);
        }

        if (remaining > 0)
            CancelAndRecord(operation, cancellationErrors);

        // WhenAll waits for every terminal gateway state, including late cleanup
        // faults. Its thrown exception is collected below along with any others.
        try
        {
            await Task.WhenAll(calls).ConfigureAwait(false);
        }
        catch
        {
            // Inspect all tasks rather than keeping only WhenAll's first exception.
        }

        var errors = await CollectErrorsAsync(
            calls, independentCancellations, operation.Token).ConfigureAwait(false);
        errors.AddRange(cancellationErrors);

        var outcome = winnerIndex >= 0
            ? new ReservationOutcome(true, input[winnerIndex], winner!.ReservationId,
                Array.Empty<string>())
            : new ReservationOutcome(false, null, null, input);

        ThrowBeforePublication(cancellationToken, errors);
        return outcome;
    }

    private static void CancelAndRecord(
        CancellationTokenSource operation,
        ConcurrentQueue<Exception> errors)
    {
        try
        {
            operation.Cancel();
        }
        catch (AggregateException error)
        {
            foreach (var callbackError in error.InnerExceptions)
                errors.Enqueue(callbackError);
        }
        catch (Exception error)
        {
            errors.Enqueue(error);
        }
    }

    private static async Task<List<Exception>> CollectErrorsAsync<T>(
        IReadOnlyList<Task<T>?> calls,
        IReadOnlyList<bool> independentCancellations,
        CancellationToken operationToken)
    {
        var errors = new List<Exception>();
        for (int i = 0; i < calls.Count; i++)
        {
            var call = calls[i];
            if (call is null)
                continue;

            if (call.IsFaulted)
            {
                // Faulted OCEs are faults too; only canceled tasks can be artifacts
                // of the coordinator's owned cancellation.
                errors.AddRange(call.Exception!.InnerExceptions);
            }
            else if (call.IsCanceled)
            {
                try
                {
                    await call.ConfigureAwait(false);
                }
                catch (OperationCanceledException error)
                {
                    bool ownedCancellation = operationToken.IsCancellationRequested &&
                        error.CancellationToken == operationToken &&
                        !independentCancellations[i];
                    if (!ownedCancellation)
                        errors.Add(error);
                }
            }
        }
        return errors;
    }

    private static void ThrowBeforePublication(
        CancellationToken callerToken,
        List<Exception> errors)
    {
        // Cancellation during draining still prevents publication. When cleanup also
        // failed, retain both the caller cancellation and all independent failures.
        if (callerToken.IsCancellationRequested)
            errors.Insert(0, new OperationCanceledException(callerToken));

        if (errors.Count == 1)
            ExceptionDispatchInfo.Capture(errors[0]).Throw();
        if (errors.Count > 1)
            throw new AggregateException(errors);
    }
}
