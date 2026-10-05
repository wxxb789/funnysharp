using System.Runtime.ExceptionServices;

public static class AvailabilityCoordinator
{
    public static async Task<IReadOnlyList<ItemAvailability>> CheckAvailabilityAsync(
        IReadOnlyList<StockItem> items, WarehouseGateway gateway, int maxConcurrency, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxConcurrency);
        using var owned = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var results = new ItemAvailability[items.Count];
        var next = -1;
        async Task WorkerAsync()
        {
            try
            {
                while (true)
                {
                    owned.Token.ThrowIfCancellationRequested();
                    var index = Interlocked.Increment(ref next);
                    if (index >= items.Count) return;
                    var item = items[index];
                    var reply = await gateway.CheckAsync(item.Sku, owned.Token);
                    results[index] = new(item.Sku, reply.OnHand, reply.OnHand >= item.Quantity);
                }
            }
            catch
            {
                owned.Cancel();
                throw;
            }
        }
#if UNBOUNDED_ADMISSION
        // Deliberate control defect: ignore the requested concurrency bound.
        var workerCount = items.Count;
#else
        var workerCount = Math.Min(items.Count, maxConcurrency);
#endif
        var workers = Enumerable.Range(0, workerCount).Select(_ => WorkerAsync()).ToArray();
        await Task.WhenAll(workers);
        cancellationToken.ThrowIfCancellationRequested();
        return results;
    }

    public static async Task<ReservationOutcome> ReserveFirstAsync(IReadOnlyList<string> suppliers,
        SupplierGateway gateway, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (suppliers.Count == 0) return new(false, null, null, []);
        using var owned = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var tasks = suppliers.Select(name => gateway.ProbeAsync(name, owned.Token)).ToArray();
        var remaining = tasks.ToList();
        var winner = -1;
        SupplierReply? accepted = null;
        Exception? fault = null;
        while (remaining.Count != 0)
        {
            var completed = await Task.WhenAny(remaining);
            remaining.Remove(completed);
            try
            {
                var reply = await completed;
                if (!reply.Accepts) continue;
                winner = Array.IndexOf(tasks, completed);
                accepted = reply;
                break;
            }
            catch (Exception error)
            {
                fault = error;
                break;
            }
        }
        if (winner >= 0 || fault is not null) owned.Cancel();
#if UNOBSERVED_CLEANUP_FAULT
        // Deliberate control defect: publish success before losers finish cleanup.
        if (winner >= 0) return new(true, suppliers[winner], accepted!.ReservationId, []);
#endif
        foreach (var task in tasks)
        {
            try { await task; }
            catch (OperationCanceledException error) when (owned.IsCancellationRequested && error.CancellationToken == owned.Token) { }
            catch (Exception error) { fault ??= error; }
        }
        if (fault is not null) ExceptionDispatchInfo.Capture(fault).Throw();
        cancellationToken.ThrowIfCancellationRequested();
#if LAST_WINNER
        // Deliberate control defect: late replies overwrite the selected winner.
        for (var i = 0; i < tasks.Length; i++)
            if (tasks[i].IsCompletedSuccessfully && tasks[i].Result.Accepts) { winner = i; accepted = tasks[i].Result; }
#endif
        return winner < 0 ? new(false, null, null, suppliers.ToArray())
            : new(true, suppliers[winner], accepted!.ReservationId, []);
    }
}
