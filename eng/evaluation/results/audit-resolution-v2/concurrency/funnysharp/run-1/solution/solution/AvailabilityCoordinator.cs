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

        var mapped = EnumerateItems(items).SelectParallelValueAsync(
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

        var results = new List<ItemAvailability>(items.Count);
        // FunnySharp owns bounded admission and drains started selectors when
        // enumeration fails, is canceled, or is disposed.
        await foreach (var result in mapped.WithCancellation(cancellationToken)
            .ConfigureAwait(false))
        {
            results.Add(result);
        }

        cancellationToken.ThrowIfCancellationRequested();
        return results.ToArray();
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

        using var operation = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken);
        var tasks = new List<Task<SupplierReply>>(supplierIds.Length);

        // This workload requires every probe to start before winner selection,
        // including when replies complete synchronously. Use an explicit BCL
        // race rather than cold FirstSuccessAsync's stop-admission policy.
        for (var index = 0; index < supplierIds.Length; index++)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                tasks.Add(gateway.ProbeAsync(supplierIds[index], operation.Token));
            }
            catch (Exception error)
            {
                // A synchronous invocation failure must not orphan probes that
                // have already started. Retain it for the shared drain path.
                tasks.Add(Task.FromException<SupplierReply>(error));
            }
        }

        var pending = Enumerable.Range(0, tasks.Count).ToList();
        int? winnerIndex = null;
        SupplierReply? winnerReply = null;
        var stopped = false;

        while (pending.Count != 0 && !stopped)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                stopped = true;
                break;
            }

            await Task.WhenAny(pending.Select(index => tasks[index]))
                .ConfigureAwait(false);

            if (cancellationToken.IsCancellationRequested)
            {
                stopped = true;
                break;
            }

            // Account for the observed ready batch in input order. This also
            // resolves an initially completed batch deterministically.
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
                    stopped = true;
                    break;
                }

                var reply = await task.ConfigureAwait(false);
                if (reply.Accepts)
                {
                    winnerIndex = index;
                    winnerReply = reply;
                    stopped = true;
                    break;
                }
            }
        }

        var cancellationFailures = new List<Exception>();
        if (stopped || cancellationToken.IsCancellationRequested)
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

        try
        {
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // WhenAll has drained every exact gateway task. Inspect each task
            // below to retain all independent failures, not just await's first.
        }

        var failures = new List<Exception>();
        for (var index = 0; index < tasks.Count; index++)
        {
            var task = tasks[index];
            if (task.IsFaulted)
            {
                // A faulted OCE is a fault, not an internal cancellation artifact.
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

        failures.AddRange(cancellationFailures);

        // Publication occurs only after every started gateway task has exited.
        // Cancellation during loser cleanup therefore supersedes a winner.
        if (cancellationToken.IsCancellationRequested)
        {
            failures.Insert(0, new OperationCanceledException(cancellationToken));
        }

        if (failures.Count == 1)
        {
            ExceptionDispatchInfo.Capture(failures[0]).Throw();
        }
        else if (failures.Count > 1)
        {
            throw new AggregateException(failures);
        }

        if (winnerIndex is int selected)
        {
            // Never reconsider the winner using late replies observed in drain.
            return new ReservationOutcome(
                true,
                supplierIds[selected],
                winnerReply!.ReservationId,
                Array.Empty<string>());
        }

        return new ReservationOutcome(false, null, null, supplierIds);
    }

    private static async IAsyncEnumerable<StockItem> EnumerateItems(
        IReadOnlyList<StockItem> items,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        for (var index = 0; index < items.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return items[index];
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }
}
