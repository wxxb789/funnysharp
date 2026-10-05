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
            return Array.Empty<ItemAvailability>();

        // The ordered FunnySharp map bounds admission and drains all started
        // selectors before completing, including cancellation and failure paths.
        var results = await Enumerate(items)
            .SelectParallelValueAsync(
                maxConcurrency,
                (item, token) => CheckItemAsync(item, gateway, token))
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

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
        cancellationToken.ThrowIfCancellationRequested();
        if (names.Length == 0)
            return new ReservationOutcome(false, null, null, Array.Empty<string>());

        using var operation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var tasks = new Task<SupplierReply>[names.Length];

        // This workload requires every probe to start, even when an earlier
        // probe completes synchronously. Start all before selecting a winner.
        // Keep the gateway tasks themselves so draining observes their cleanup.
        for (int index = 0; index < names.Length; index++)
        {
            try
            {
                tasks[index] = gateway.ProbeAsync(names[index], operation.Token)
                    ?? Task.FromException<SupplierReply>(
                        new InvalidOperationException("The gateway returned a null task."));
            }
            catch (Exception error)
            {
                // A synchronous invocation failure must not orphan probes that
                // have already started or prevent observing the other calls.
                tasks[index] = Task.FromException<SupplierReply>(error);
            }
        }

        var remaining = Enumerable.Range(0, tasks.Length).ToList();
        int winnerIndex = -1;
        SupplierReply? winner = null;
        bool observedFailure = false;

        try
        {
            while (remaining.Count != 0 && winnerIndex < 0 && !observedFailure)
            {
                await Task.WhenAny(remaining.Select(index => tasks[index]))
                    .WaitAsync(cancellationToken)
                    .ConfigureAwait(false);

                // Account for the observed ready batch in input order. Once a
                // winner is selected it is immutable throughout loser cleanup.
                for (int position = 0; position < remaining.Count;)
                {
                    int index = remaining[position];
                    var task = tasks[index];
                    if (!task.IsCompleted)
                    {
                        position++;
                        continue;
                    }

                    remaining.RemoveAt(position);
                    if (!task.IsCompletedSuccessfully)
                    {
                        observedFailure = true;
                        break;
                    }

                    var reply = task.Result;
                    if (reply.Accepts)
                    {
                        winnerIndex = index;
                        winner = reply;
                        break;
                    }
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Publish caller cancellation only after every started call exits.
        }

        var callbackErrors = new List<Exception>();
        if (remaining.Count != 0 || cancellationToken.IsCancellationRequested)
        {
            try
            {
                operation.Cancel();
            }
            catch (AggregateException error)
            {
                callbackErrors.AddRange(error.InnerExceptions);
            }
            catch (Exception error)
            {
                callbackErrors.Add(error);
            }
        }

        try
        {
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // WhenAll has drained every task. Inspect each terminal state below
            // instead of losing additional failures through its await exception.
        }

        var errors = new List<Exception>();
        foreach (var task in tasks)
        {
            if (task.IsFaulted)
            {
                // Faulted OCEs are independent faults, not internal cancellation.
                errors.AddRange(task.Exception!.InnerExceptions);
            }
            else if (task.IsCanceled)
            {
                try
                {
                    await task.ConfigureAwait(false);
                }
                catch (OperationCanceledException error)
                {
                    bool ownedCancellation = operation.IsCancellationRequested
                        && error.CancellationToken == operation.Token;
                    if (!ownedCancellation)
                        errors.Add(error);
                }
            }
        }
        errors.AddRange(callbackErrors);

        // This is the shared publication point, after cleanup. Caller
        // cancellation takes precedence over a concurrently selected winner.
        if (cancellationToken.IsCancellationRequested)
            errors.Insert(0, new OperationCanceledException(cancellationToken));

        ThrowFailures(errors);

        if (winnerIndex >= 0)
        {
            return new ReservationOutcome(
                true, names[winnerIndex], winner!.ReservationId, Array.Empty<string>());
        }

        return new ReservationOutcome(false, null, null, names);
    }

    private static async ValueTask<ItemAvailability> CheckItemAsync(
        StockItem item,
        WarehouseGateway gateway,
        CancellationToken cancellationToken)
    {
        var reply = await gateway.CheckAsync(item.Sku, cancellationToken).ConfigureAwait(false);
        return new ItemAvailability(item.Sku, reply.OnHand, reply.OnHand >= item.Quantity);
    }

    private static async IAsyncEnumerable<StockItem> Enumerate(
        IReadOnlyList<StockItem> items,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // A synchronously available source; no scheduling or prefetching.
        await Task.CompletedTask.ConfigureAwait(false);
        for (int index = 0; index < items.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return items[index];
        }
    }

    private static void ThrowFailures(IReadOnlyList<Exception> errors)
    {
        if (errors.Count == 1)
            ExceptionDispatchInfo.Capture(errors[0]).Throw();
        if (errors.Count > 1)
            throw new AggregateException(errors);
    }
}
