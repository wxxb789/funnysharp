// Availability coordination for the fulfillment service.
//
// FunnySharp carries the outcomes: each supplier probe is a fail-fast
// Result<Reservation, string> whose error names the declining supplier, and the
// all-suppliers-declined case arrives as the accumulated Validation returned by
// FirstSuccessAsync. Cancellation and gateway faults stay ordinary exceptions.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using FunnySharp;

public static class AvailabilityCoordinator
{
    // Checks every item through the warehouse gateway with bounded parallelism.
    // SelectParallelValueAsync keeps at most maxConcurrency checks in flight and
    // yields the answers in source order even when an early item finishes last.
    // The caller's token cancels the enumeration and every in-flight check.
    public static async Task<IReadOnlyList<ItemAvailability>> CheckAvailabilityAsync(
        IReadOnlyList<StockItem> items,
        WarehouseGateway gateway,
        int maxConcurrency,
        CancellationToken cancellationToken)
    {
        var results = await items.ToAsyncEnumerable()
            .SelectParallelValueAsync(
                maxConcurrency,
                (item, token) => new ValueTask<ItemAvailability>(CheckItemAsync(gateway, item, token)))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return results;
    }

    // Reserves from the first supplier that accepts. Every supplier is probed up
    // front (a declining probe is a typed failure and never stops the search); the
    // first accepting reply observed wins and FirstSuccessAsync drains the rest.
    public static async Task<ReservationOutcome> ReserveFirstAsync(
        IReadOnlyList<string> suppliers,
        SupplierGateway gateway,
        CancellationToken cancellationToken)
    {
        if (suppliers.Count == 0)
        {
            return new ReservationOutcome(false, null, null, Array.Empty<string>());
        }

        var probes = suppliers.Select(supplierId => Effect.FromTask<Result<Reservation, string>>(
            async token =>
            {
                var reply = await gateway.ProbeAsync(supplierId, token).ConfigureAwait(false);
                return reply.Accepts
                    ? Result<Reservation, string>.Success(new Reservation(supplierId, reply.ReservationId))
                    : Result<Reservation, string>.Failure(supplierId);
            }));

        var race = await probes.FirstSuccessAsync(cancellationToken).ConfigureAwait(false);

        // The winner is the Valid case; every decline accumulates into the Invalid
        // case with the supplier ids in input order, not completion order.
        return race.Match(
            reservation => new ReservationOutcome(
                true,
                reservation.SupplierId,
                reservation.ReservationId,
                Array.Empty<string>()),
            failedSuppliers => new ReservationOutcome(
                false,
                null,
                null,
                failedSuppliers));
    }

    private static async Task<ItemAvailability> CheckItemAsync(
        WarehouseGateway gateway,
        StockItem item,
        CancellationToken cancellationToken)
    {
        var reply = await gateway.CheckAsync(item.Sku, cancellationToken).ConfigureAwait(false);
        return new ItemAvailability(reply.Sku, reply.OnHand, reply.OnHand >= item.Quantity);
    }

    private sealed record Reservation(string SupplierId, string? ReservationId);
}
