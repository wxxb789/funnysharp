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

        var input = items.ToArray();
        if (input.Length == 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Array.Empty<ItemAvailability>();
        }

        using var operation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var results = new ItemAvailability[input.Length];
        var started = new List<Task<WarehouseReply>>();
        var active = new List<(int Index, Task<WarehouseReply> Task)>();
        var cancellationFaults = new List<Exception>();
        var next = 0;
        var stoppedEarly = false;

        while (true)
        {
            // A slot is reusable only after the gateway task is terminal,
            // including its cleanup and active-count accounting.
            while (next < input.Length && active.Count < maxConcurrency &&
                   !operation.IsCancellationRequested)
            {
                var index = next++;
                var task = Start(() => gateway.CheckAsync(input[index].Sku, operation.Token));
                started.Add(task);
                active.Add((index, task));
            }

            if (operation.IsCancellationRequested)
            {
                stoppedEarly = true;
                break;
            }

            if (active.Count == 0)
                break;

            var finished = await Task.WhenAny(active.Select(entry => entry.Task))
                .ConfigureAwait(false);
            var position = active.FindIndex(entry => ReferenceEquals(entry.Task, finished));
            var entry = active[position];
            active.RemoveAt(position);

            try
            {
                var reply = await finished.ConfigureAwait(false);
                var item = input[entry.Index];
                results[entry.Index] = new ItemAvailability(
                    item.Sku, reply.OnHand, reply.OnHand >= item.Quantity);
            }
            catch (Exception)
            {
                // The definitive failure collection below retains every task fault.
                stoppedEarly = true;
                break;
            }
        }

        if (stoppedEarly)
            Cancel(operation, cancellationFaults);

        var failures = await DrainAsync(started, operation.Token).ConfigureAwait(false);
        PublishFailures(failures, cancellationFaults, cancellationToken);
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
        if (input.Length == 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return new ReservationOutcome(false, null, null, Array.Empty<string>());
        }

        using var operation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var started = new List<Task<SupplierReply>>(input.Length);
        var pending = new List<(int Index, Task<SupplierReply> Task)>(input.Length);
        var cancellationFaults = new List<Exception>();

        // Admission precedes selection, including when some replies are already complete.
        for (var index = 0; index < input.Length; index++)
        {
            if (operation.IsCancellationRequested)
                break;

            var supplier = input[index];
            var task = Start(() => gateway.ProbeAsync(supplier, operation.Token));
            started.Add(task);
            pending.Add((index, task));
        }

        ReservationOutcome? winner = null;
        var stoppedEarly = false;
        while (pending.Count > 0)
        {
            if (operation.IsCancellationRequested)
            {
                stoppedEarly = true;
                break;
            }

            // WhenAny observes completion without waiting for an earlier supplier.
            // Pending remains input-ordered, resolving already-complete replies
            // in input order.
            var finished = await Task.WhenAny(pending.Select(entry => entry.Task))
                .ConfigureAwait(false);
            var position = pending.FindIndex(entry => ReferenceEquals(entry.Task, finished));
            var entry = pending[position];
            pending.RemoveAt(position);

            SupplierReply reply;
            try
            {
                reply = await finished.ConfigureAwait(false);
            }
            catch (Exception)
            {
                stoppedEarly = true;
                break;
            }

            if (reply.Accepts)
            {
                winner = new ReservationOutcome(
                    true, input[entry.Index], reply.ReservationId, Array.Empty<string>());
                stoppedEarly = true;
                break;
            }
        }

        if (stoppedEarly || operation.IsCancellationRequested)
            Cancel(operation, cancellationFaults);

        // Freeze the selected winner. Late successes are observed only for cleanup;
        // they cannot replace it. Independent failures can still prevent publication.
        var failures = await DrainAsync(started, operation.Token).ConfigureAwait(false);
        PublishFailures(failures, cancellationFaults, cancellationToken);
        return winner ?? new ReservationOutcome(false, null, null, input);
    }

    private static Task<T> Start<T>(Func<Task<T>> invocation)
    {
        try
        {
            return invocation() ?? Task.FromException<T>(
                new InvalidOperationException("The gateway returned a null task."));
        }
        catch (Exception error)
        {
            // Represent a synchronous invocation failure without abandoning calls
            // that have already started.
            return Task.FromException<T>(error);
        }
    }

    private static void Cancel(CancellationTokenSource operation, List<Exception> failures)
    {
        try
        {
            operation.Cancel();
        }
        catch (AggregateException error)
        {
            failures.AddRange(error.InnerExceptions);
        }
        catch (Exception error)
        {
            failures.Add(error);
        }
    }

    private static async Task<List<Exception>> DrainAsync<T>(
        IEnumerable<Task<T>> tasks,
        CancellationToken operationToken)
    {
        var failures = new List<Exception>();
        foreach (var task in tasks)
        {
            try
            {
                await task.ConfigureAwait(false);
            }
            catch (Exception error)
            {
                if (task.IsFaulted)
                {
                    // Await exposes one exception; the task retains every fault.
                    // Faulted OperationCanceledExceptions are independent faults too.
                    failures.AddRange(task.Exception!.InnerExceptions);
                }
                else if (error is not OperationCanceledException canceled ||
                         !operationToken.IsCancellationRequested ||
                         canceled.CancellationToken != operationToken)
                {
                    failures.Add(error);
                }
                // Suppress only cancellation caused by the canceled owned token.
            }
        }

        return failures;
    }

    private static void PublishFailures(
        List<Exception> failures,
        List<Exception> cancellationFaults,
        CancellationToken callerToken)
    {
        failures.AddRange(cancellationFaults);

        // This shared publication point also catches caller cancellation that
        // occurred while draining non-cooperating calls or held cleanup.
        if (callerToken.IsCancellationRequested)
            failures.Insert(0, new OperationCanceledException(callerToken));

        if (failures.Count == 1)
            ExceptionDispatchInfo.Capture(failures[0]).Throw();

        if (failures.Count > 1)
            throw new AggregateException(failures);
    }
}
