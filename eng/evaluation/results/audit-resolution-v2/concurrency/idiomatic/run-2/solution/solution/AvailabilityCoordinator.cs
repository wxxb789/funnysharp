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

        var inputs = items.ToArray();
        if (inputs.Length == 0)
            return Array.Empty<ItemAvailability>();

        using var operation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var results = new ItemAvailability[inputs.Length];
        var calls = new List<Task<WarehouseReply>>();
        var pending = new List<(int Index, Task<WarehouseReply> Call)>();
        int next = 0;
        bool stop = false;
        Exception? coordinationFailure = null;

        try
        {
            while (next < inputs.Length || pending.Count != 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                while (next < inputs.Length && pending.Count < maxConcurrency)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var index = next++;
                    var call = StartCall(() => gateway.CheckAsync(inputs[index].Sku, operation.Token));
                    calls.Add(call);
                    pending.Add((index, call));
                }

                await Task.WhenAny(pending.Select(entry => entry.Call))
                    .WaitAsync(cancellationToken).ConfigureAwait(false);

                // Stop admission as soon as a failed call is observed. Its exception
                // is collected below, together with any failures during sibling cleanup.
                if (pending.Any(entry => entry.Call.IsCompleted && !entry.Call.IsCompletedSuccessfully))
                {
                    stop = true;
                    break;
                }

                for (int i = pending.Count - 1; i >= 0; i--)
                {
                    var entry = pending[i];
                    if (!entry.Call.IsCompletedSuccessfully)
                        continue;

                    var reply = await entry.Call.ConfigureAwait(false);
                    var item = inputs[entry.Index];
                    results[entry.Index] = new ItemAvailability(
                        item.Sku, reply.OnHand, reply.OnHand >= item.Quantity);
                    pending.RemoveAt(i);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            stop = true;
        }
        catch (Exception error)
        {
            stop = true;
            coordinationFailure = error;
        }

        var failures = await DrainAsync(
            calls, operation, stop || cancellationToken.IsCancellationRequested).ConfigureAwait(false);
        if (coordinationFailure is not null)
            failures.Insert(0, coordinationFailure);

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

        var inputs = suppliers.ToArray();
        if (inputs.Length == 0)
            return new ReservationOutcome(false, null, null, Array.Empty<string>());

        using var operation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var calls = new List<Task<SupplierReply>>(inputs.Length);
        var pending = new List<(int Index, Task<SupplierReply> Call)>(inputs.Length);
        (int Index, SupplierReply Reply)? winner = null;
        bool stop = false;
        Exception? coordinationFailure = null;

        try
        {
            // Start every probe before selecting, including when gateway tasks
            // complete synchronously. No Task.Run or test-only signals are needed.
            for (int index = 0; index < inputs.Length; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var supplier = inputs[index];
                var call = StartCall(() => gateway.ProbeAsync(supplier, operation.Token));
                calls.Add(call);
                pending.Add((index, call));
            }

            while (pending.Count != 0)
            {
                await Task.WhenAny(pending.Select(entry => entry.Call))
                    .WaitAsync(cancellationToken).ConfigureAwait(false);

                bool failed = false;
                // Pending entries retain input order, resolving an already-ready
                // batch deterministically. Once selected, the winner is immutable.
                foreach (var entry in pending)
                {
                    if (entry.Call.IsCompletedSuccessfully)
                    {
                        var reply = await entry.Call.ConfigureAwait(false);
                        if (reply.Accepts)
                        {
                            winner = (entry.Index, reply);
                            break;
                        }
                    }
                    else if (entry.Call.IsCompleted)
                    {
                        failed = true;
                    }
                }

                if (winner.HasValue || failed)
                {
                    stop = true;
                    break;
                }

                pending.RemoveAll(entry => entry.Call.IsCompleted);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            stop = true;
        }
        catch (Exception error)
        {
            stop = true;
            coordinationFailure = error;
        }

        // Await the exact gateway tasks, not merely their cleanup-entry signals.
        // Late successful losers are drained but never reconsidered as winners.
        var failures = await DrainAsync(
            calls, operation, stop || cancellationToken.IsCancellationRequested).ConfigureAwait(false);
        if (coordinationFailure is not null)
            failures.Insert(0, coordinationFailure);

        var outcome = winner is { } selected
            ? new ReservationOutcome(true, inputs[selected.Index], selected.Reply.ReservationId, Array.Empty<string>())
            : new ReservationOutcome(false, null, null, inputs);

        ThrowBeforePublication(failures, cancellationToken);
        return outcome;
    }

    private static Task<T> StartCall<T>(Func<Task<T>> start)
    {
        try
        {
            return start() ?? Task.FromException<T>(
                new InvalidOperationException("The gateway returned a null task."));
        }
        catch (Exception error)
        {
            // A synchronous invocation failure must not bypass draining calls
            // that were already started.
            return Task.FromException<T>(error);
        }
    }

    private static async Task<List<Exception>> DrainAsync<T>(
        IReadOnlyList<Task<T>> calls,
        CancellationTokenSource operation,
        bool cancel)
    {
        Exception? cancellationFailure = null;
        if (cancel)
        {
            try
            {
                operation.Cancel();
            }
            catch (Exception error)
            {
                cancellationFailure = error;
            }
        }

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
                    // Await exposes only one exception; retain all task faults,
                    // including faulted OperationCanceledExceptions.
                    failures.AddRange(call.Exception!.InnerExceptions);
                }
                else if (error is not OperationCanceledException cancellation
                    || !operation.IsCancellationRequested
                    || cancellation.CancellationToken != operation.Token)
                {
                    failures.Add(error);
                }
                // Only canceled tasks bearing our canceled operation token are
                // expected cancellation artifacts rather than independent failures.
            }
        }

        if (cancellationFailure is AggregateException aggregate)
            failures.AddRange(aggregate.InnerExceptions);
        else if (cancellationFailure is not null)
            failures.Add(cancellationFailure);

        return failures;
    }

    private static void ThrowBeforePublication(
        List<Exception> failures,
        CancellationToken cancellationToken)
    {
        // Caller cancellation is checked after cleanup, so cancellation during
        // loser draining cannot be hidden by a previously selected winner.
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
            ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count > 1)
            throw new AggregateException(failures);
    }
}
