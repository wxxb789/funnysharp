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

        var results = new ItemAvailability[items.Count];
        if (items.Count == 0)
            return results;

        // Each worker awaits the entire gateway call, including cleanup, before
        // admitting another item. ForEachAsync also joins workers on cancellation.
        await Parallel.ForEachAsync(
            Enumerable.Range(0, items.Count),
            new ParallelOptions
            {
                MaxDegreeOfParallelism = maxConcurrency,
                CancellationToken = cancellationToken,
            },
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
        {
            cancellationToken.ThrowIfCancellationRequested();
            return new ReservationOutcome(false, null, null, Array.Empty<string>());
        }

        using var operation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var started = new List<(int Index, Task<SupplierReply> Task)>(names.Length);
        Exception? startupFailure = null;

        // Start every probe before selecting a reply, including when calls finish
        // synchronously. The study's supplier inputs are bounded to 32 entries.
        for (var index = 0; index < names.Length; index++)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                var task = gateway.ProbeAsync(names[index], operation.Token);
                started.Add((index, task));
            }
            catch (Exception error)
            {
                startupFailure = error;
                break;
            }
        }

        var pending = new List<(int Index, Task<SupplierReply> Task)>(started);
        var winnerIndex = -1;
        SupplierReply? winnerReply = null;

        if (startupFailure is null)
        {
            while (pending.Count != 0 && !cancellationToken.IsCancellationRequested)
            {
                // WhenAny scans already-completed tasks in the supplied order.
                // Otherwise it reports the first completion it observes.
                var completed = await Task.WhenAny(pending.Select(entry => entry.Task))
                    .ConfigureAwait(false);
                var position = pending.FindIndex(entry => ReferenceEquals(entry.Task, completed));
                var entry = pending[position];
                pending.RemoveAt(position);

                if (!completed.IsCompletedSuccessfully)
                    break; // Cancel siblings, then collect the failure during drain.

                var reply = await completed.ConfigureAwait(false);
                if (reply.Accepts)
                {
                    winnerIndex = entry.Index;
                    winnerReply = reply;
                    break;
                }
            }
        }

        var cancellationFailures = new List<Exception>();
        if (winnerReply is not null || startupFailure is not null ||
            pending.Count != 0 || cancellationToken.IsCancellationRequested)
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

        // Freeze the selected winner. Late successful replies during cancellation
        // are only drained; they cannot replace it. Continue draining after faults.
        var failures = new List<Exception>();
        foreach (var entry in started)
        {
            try
            {
                await entry.Task.ConfigureAwait(false);
            }
            catch (Exception error)
            {
                if (entry.Task.IsFaulted)
                {
                    // Retain every exception represented by the gateway task.
                    failures.AddRange(entry.Task.Exception!.InnerExceptions);
                }
                else if (entry.Task.IsCanceled &&
                    error is OperationCanceledException canceled &&
                    operation.IsCancellationRequested &&
                    canceled.CancellationToken == operation.Token)
                {
                    // Only cancellation of our own operation is a loser artifact.
                }
                else
                {
                    failures.Add(error);
                }
            }
        }

        if (startupFailure is not null)
            failures.Add(startupFailure);
        failures.AddRange(cancellationFailures);

        // Publication happens only after every started gateway task is terminal.
        // Caller cancellation during cleanup still takes precedence over a winner.
        if (cancellationToken.IsCancellationRequested)
        {
            if (failures.Count == 0)
                cancellationToken.ThrowIfCancellationRequested();
            failures.Insert(0, new OperationCanceledException(cancellationToken));
        }

        if (failures.Count == 1)
            ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures.Count > 1)
            throw new AggregateException(failures);

        return winnerReply is not null
            ? new ReservationOutcome(true, names[winnerIndex], winnerReply.ReservationId,
                Array.Empty<string>())
            : new ReservationOutcome(false, null, null, Array.AsReadOnly(names));
    }
}
