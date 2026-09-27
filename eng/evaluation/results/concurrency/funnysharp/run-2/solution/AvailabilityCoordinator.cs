using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using FunnySharp;

/// <summary>
/// The reservation held by the supplier that won the first-success probe race.
/// </summary>
public readonly record struct SupplierReservation(string SupplierId, string? ReservationId);

/// <summary>
/// Coordinates warehouse availability checks and supplier reservations for the
/// fulfillment service.
/// </summary>
public static class AvailabilityCoordinator
{
    /// <summary>
    /// Checks every item through the warehouse gateway with bounded parallelism and
    /// returns one <see cref="ItemAvailability"/> per item, in source order.
    /// </summary>
    public static async Task<IReadOnlyList<ItemAvailability>> CheckAvailabilityAsync(
        IReadOnlyList<StockItem> items,
        WarehouseGateway gateway,
        int maxConcurrency,
        CancellationToken cancellationToken)
    {
        // SelectParallelValueAsync bounds the fan-out: at most maxConcurrency gateway
        // checks are in flight at any time (a bound of 1 runs them one at a time),
        // while ordered delivery keeps every answer at its item's source position -
        // the first item's answer stays first even when its check finishes last. The
        // token-aware selector receives a linked operation token that carries
        // cancellation into every gateway call, and ToListAsync forwards the caller
        // token to the enumeration.
        return await items
            .ToAsyncEnumerable()
            .SelectParallelValueAsync(
                maxConcurrency,
                async (item, token) =>
                {
                    var reply = await gateway.CheckAsync(item.Sku, token);
                    return new ItemAvailability(reply.Sku, reply.OnHand, reply.OnHand >= item.Quantity);
                })
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Reserves from the first supplier whose probe accepts.
    /// </summary>
    public static async Task<ReservationOutcome> ReserveFirstAsync(
        IReadOnlyList<string> suppliers,
        SupplierGateway gateway,
        CancellationToken cancellationToken)
    {
        if (suppliers.Count == 0)
        {
            return new ReservationOutcome(false, null, null, Array.Empty<string>());
        }

        // One cold probe effect per supplier: no gateway call starts until
        // FirstSuccessAsync runs the effects. Each probe is fail-fast - accepting is
        // Success carrying the reservation, declining is the typed failure "supplier
        // id" - and the effect's token goes into the gateway call, so a winning or
        // cancelled race cancels the probe.
        var probes = suppliers.Select(
            supplierId => Effect.FromTask<Result<SupplierReservation, string>>(
                async token =>
                {
                    var reply = await gateway.ProbeAsync(supplierId, token);
                    return reply.Accepts
                        ? Result<SupplierReservation, string>.Success(
                            new SupplierReservation(supplierId, reply.ReservationId))
                        : Result<SupplierReservation, string>.Failure(supplierId);
                }));

        // FirstSuccessAsync starts every probe (a quick decline never stops the
        // search), races them, then cancels and drains the losers. Valid carries the
        // first accepting reply observed - the winner; Invalid carries every declined
        // supplier id in input order. Cancellation stays an exception.
        var race = await probes.FirstSuccessAsync(cancellationToken);

        if (race.TryGetValue(out var reserved))
        {
            return new ReservationOutcome(
                true, reserved.SupplierId, reserved.ReservationId, Array.Empty<string>());
        }

        if (race.TryGetErrors(out var failedSuppliers) && failedSuppliers is not null)
        {
            return new ReservationOutcome(false, null, null, failedSuppliers);
        }

        // Unreachable for an initialized race result: an invalid race always carries
        // its errors.
        return new ReservationOutcome(false, null, null, Array.Empty<string>());
    }
}
