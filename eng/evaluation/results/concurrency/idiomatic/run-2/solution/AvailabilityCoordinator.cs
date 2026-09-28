// Availability coordination for the fulfillment service: bounded-parallel availability checks and
// first-success reservation, expressed with the .NET BCL only (SemaphoreSlim, Task.WhenAll,
// TaskCompletionSource).

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
        ArgumentOutOfRangeException.ThrowIfLessThan(maxConcurrency, 1);

        if (items.Count == 0)
        {
            return [];
        }

        // The gate is the bound on concurrency: a check holds one of its maxConcurrency slots for
        // the whole gateway call, so the gateway never observes more than maxConcurrency checks in
        // flight, and with maxConcurrency 1 the checks run one at a time.
        using var gate = new SemaphoreSlim(initialCount: maxConcurrency, maxCount: maxConcurrency);

        // Slot i holds item i's reply, so Task.WhenAll returns the answers in source order no
        // matter which check finishes first.
        var checks = new Task<WarehouseReply>[items.Count];
        for (var index = 0; index < items.Count; index++)
        {
            checks[index] = CheckItemAsync(items[index], gateway, gate, cancellationToken);
        }

        var replies = await Task.WhenAll(checks).ConfigureAwait(false);

        return
        [
            .. items.Zip(
                replies,
                (item, reply) => new ItemAvailability(item.Sku, reply.OnHand, reply.OnHand >= item.Quantity))
        ];
    }

    public static async Task<ReservationOutcome> ReserveFirstAsync(
        IReadOnlyList<string> suppliers,
        SupplierGateway gateway,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(suppliers);
        ArgumentNullException.ThrowIfNull(gateway);

        if (suppliers.Count == 0)
        {
            return new ReservationOutcome(Reserved: false, SupplierId: null, ReservationId: null, FailedSuppliers: []);
        }

        // Every supplier is probed at once, and the first accepting reply observed claims the win,
        // so a quick decline never ends the search.
        var winner = new TaskCompletionSource<(string SupplierId, SupplierReply Reply)>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        var probes = suppliers
            .Select(supplier => ProbeSupplierAsync(supplier, gateway, winner, cancellationToken))
            .ToList();

        // Awaiting every probe keeps the remaining suppliers running through early declines; a
        // cancelled token surfaces here as OperationCanceledException.
        await Task.WhenAll(probes).ConfigureAwait(false);

        if (winner.Task.IsCompletedSuccessfully)
        {
            var (supplierId, reply) = await winner.Task.ConfigureAwait(false);
            return new ReservationOutcome(
                Reserved: true,
                SupplierId: supplierId,
                ReservationId: reply.ReservationId,
                FailedSuppliers: []);
        }

        // Nobody accepted: every supplier failed, reported in input (not completion) order.
        return new ReservationOutcome(
            Reserved: false,
            SupplierId: null,
            ReservationId: null,
            FailedSuppliers: [.. suppliers]);
    }

    private static async Task<WarehouseReply> CheckItemAsync(
        StockItem item,
        WarehouseGateway gateway,
        SemaphoreSlim gate,
        CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await gateway.CheckAsync(item.Sku, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            gate.Release();
        }
    }

    private static async Task ProbeSupplierAsync(
        string supplierId,
        SupplierGateway gateway,
        TaskCompletionSource<(string SupplierId, SupplierReply Reply)> winner,
        CancellationToken cancellationToken)
    {
        var reply = await gateway.ProbeAsync(supplierId, cancellationToken).ConfigureAwait(false);
        if (reply.Accepts)
        {
            winner.TrySetResult((supplierId, reply));
        }
    }
}
