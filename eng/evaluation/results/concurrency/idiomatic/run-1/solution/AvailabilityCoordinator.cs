// Availability coordination for the fulfillment service: stock checks with bounded
// parallelism and a first-observed-success supplier reservation, built on plain BCL primitives.

public static class AvailabilityCoordinator
{
    public static async Task<IReadOnlyList<ItemAvailability>> CheckAvailabilityAsync(
        IReadOnlyList<StockItem> items,
        WarehouseGateway gateway,
        int maxConcurrency,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxConcurrency, 1);

        if (items.Count == 0)
        {
            return [];
        }

        // The semaphore is the concurrency bound: at most maxConcurrency checks are in flight at
        // once, and with spare capacity the queued checks start as soon as a slot frees up. Each
        // answer is written at its item's index, so results keep source order regardless of the
        // order in which the checks finish.
        var results = new ItemAvailability[items.Count];
        using var gate = new SemaphoreSlim(maxConcurrency, maxConcurrency);

        await Task.WhenAll(items.Select(CheckItemAsync)).ConfigureAwait(false);
        return results;

        async Task CheckItemAsync(StockItem item, int index)
        {
            await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var reply = await gateway.CheckAsync(item.Sku, cancellationToken).ConfigureAwait(false);
                results[index] = new ItemAvailability(item.Sku, reply.OnHand, reply.OnHand >= item.Quantity);
            }
            finally
            {
                gate.Release();
            }
        }
    }

    public static async Task<ReservationOutcome> ReserveFirstAsync(
        IReadOnlyList<string> suppliers,
        SupplierGateway gateway,
        CancellationToken cancellationToken)
    {
        if (suppliers.Count == 0)
        {
            return new ReservationOutcome(Reserved: false, SupplierId: null, ReservationId: null, FailedSuppliers: []);
        }

        // Every probe starts immediately and runs concurrently, so a decline never ends the search.
        // The first accepting reply observed wins: TrySetResult records only the earliest winner
        // and ignores every later accepting reply.
        var winner = new TaskCompletionSource<(string SupplierId, SupplierReply Reply)>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        await Task.WhenAll(suppliers.Select(ProbeSupplierAsync)).ConfigureAwait(false);

        if (!winner.Task.IsCompletedSuccessfully)
        {
            // Every supplier declined; report them in input order, not completion order.
            return new ReservationOutcome(
                Reserved: false,
                SupplierId: null,
                ReservationId: null,
                FailedSuppliers: suppliers);
        }

        var (supplierId, reply) = await winner.Task.ConfigureAwait(false);
        return new ReservationOutcome(
            Reserved: true,
            SupplierId: supplierId,
            ReservationId: reply.ReservationId,
            FailedSuppliers: []);

        async Task ProbeSupplierAsync(string supplierId)
        {
            var reply = await gateway.ProbeAsync(supplierId, cancellationToken).ConfigureAwait(false);
            if (reply.Accepts)
            {
                winner.TrySetResult((supplierId, reply));
            }
        }
    }
}
