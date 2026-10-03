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
        if (snapshot.Length == 0)
            return Array.Empty<ItemAvailability>();

        Func<StockItem, CancellationToken, ValueTask<ItemAvailability>> selector =
            async (item, operationToken) =>
            {
                var reply = await gateway.CheckAsync(item.Sku, operationToken)
                    .ConfigureAwait(false);
                return new ItemAvailability(
                    item.Sku, reply.OnHand, reply.OnHand >= item.Quantity);
            };

        // The ordered FunnySharp map bounds admission and drains started calls
        // on cancellation, failure, and enumerator disposal.
        var mapped = ReadItems(snapshot).SelectParallelValueAsync(maxConcurrency, selector);
        var results = new List<ItemAvailability>(snapshot.Length);
        await foreach (var result in mapped.WithCancellation(cancellationToken)
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
        if (names.Length == 0)
            return new ReservationOutcome(false, null, null, Array.Empty<string>());

        using var operation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var tasks = new List<Task<SupplierReply>>(names.Length);

        // Start the entire finite study batch before observing replies. In
        // particular, synchronous success must not prevent later probes starting.
        foreach (var name in names)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                tasks.Add(gateway.ProbeAsync(name, operation.Token)
                    ?? Task.FromException<SupplierReply>(
                        new InvalidOperationException("The gateway returned a null task.")));
            }
            catch (Exception error)
            {
                // Retain synchronous invocation failures while still arranging
                // cancellation and terminal observation of previously started work.
                tasks.Add(Task.FromException<SupplierReply>(error));
            }
        }

        var pending = Enumerable.Range(0, tasks.Count).ToList();
        var winnerIndex = -1;
        SupplierReply? winner = null;
        Exception? invalidReply = null;
        var stoppedOnFailure = false;

        while (pending.Count != 0 && !cancellationToken.IsCancellationRequested)
        {
            // Observe each ready batch in input order. Once selected, the winner
            // is frozen; successful replies obtained during drain cannot replace it.
            for (var position = 0; position < pending.Count;)
            {
                var index = pending[position];
                var task = tasks[index];
                if (!task.IsCompleted)
                {
                    position++;
                    continue;
                }

                pending.RemoveAt(position);
                if (!task.IsCompletedSuccessfully)
                {
                    stoppedOnFailure = true;
                    break;
                }

                var reply = task.GetAwaiter().GetResult();
                if (reply is null)
                {
                    invalidReply = new InvalidOperationException("The gateway returned a null reply.");
                    stoppedOnFailure = true;
                    break;
                }

                if (reply.Accepts)
                {
                    winnerIndex = index;
                    winner = reply;
                    break;
                }
            }

            if (winnerIndex >= 0 || stoppedOnFailure || pending.Count == 0)
                break;

            await Task.WhenAny(pending.Select(index => tasks[index])).ConfigureAwait(false);
        }

        var cancellationFaults = new List<Exception>();
        if (winnerIndex >= 0 || stoppedOnFailure || cancellationToken.IsCancellationRequested)
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

        // Await the exact gateway tasks, not cleanup-entry notifications. WhenAll
        // completes only after every started call reaches its terminal state.
        try
        {
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        catch
        {
            // Inspect every original task below, retaining all independent faults
            // rather than only the one exception exposed by awaiting WhenAll.
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
                    task.GetAwaiter().GetResult();
                }
                catch (OperationCanceledException error)
                {
                    // Only cancellation attributed to our canceled operation is
                    // an expected loser. Independent cancellation still propagates.
                    if (!operation.IsCancellationRequested ||
                        error.CancellationToken != operation.Token)
                    {
                        failures.Add(error);
                    }
                }
            }
        }

        if (invalidReply is not null)
            failures.Add(invalidReply);
        failures.AddRange(cancellationFaults);

        // Publication happens after draining. Caller cancellation during cleanup
        // therefore takes precedence over an otherwise successful reservation.
        if (cancellationToken.IsCancellationRequested)
            failures.Insert(0, new OperationCanceledException(cancellationToken));

        if (failures.Count == 1)
            ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count > 1)
            throw new AggregateException(failures);

        if (winnerIndex >= 0)
        {
            return new ReservationOutcome(
                true, names[winnerIndex], winner!.ReservationId, Array.Empty<string>());
        }

        return new ReservationOutcome(false, null, null, Array.AsReadOnly(names));
    }

    private static async IAsyncEnumerable<StockItem> ReadItems(
        IReadOnlyList<StockItem> items,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // A synchronous snapshot needs no scheduling or prefetching to become an
        // async source; parallelism belongs exclusively to the mapping operator.
        await Task.CompletedTask.ConfigureAwait(false);
        foreach (var item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return item;
        }
    }
}
