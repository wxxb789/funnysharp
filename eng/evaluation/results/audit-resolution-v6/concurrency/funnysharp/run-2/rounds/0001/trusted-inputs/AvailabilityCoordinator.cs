using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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

        // The ordered parallel map bounds admission and drains started gateway
        // calls on cancellation or failure, including their asynchronous cleanup.
        var results = await StreamItems(items)
            .SelectParallelValueAsync(
                maxConcurrency,
                async (item, operationToken) =>
                {
                    var reply = await gateway.CheckAsync(item.Sku, operationToken)
                        .ConfigureAwait(false);
                    return new ItemAvailability(
                        item.Sku,
                        reply.OnHand,
                        reply.OnHand >= item.Quantity);
                })
            .ToListAsync(cancellationToken)
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

        if (suppliers.Count == 0)
            return new ReservationOutcome(false, null, null, Array.Empty<string>());

        // These effects remain cold until the coordinator admits them. The
        // study's maximum of 32 suppliers fits the first-success admission bound.
        var probes = suppliers.Select(supplier =>
            Effect.FromTask<SupplierReply>(
                    operationToken => gateway.ProbeAsync(supplier, operationToken))
                .Map(reply => reply.Accepts
                    ? Result<ReservationOutcome, string>.Success(
                        new ReservationOutcome(
                            true, supplier, reply.ReservationId, Array.Empty<string>()))
                    : Result<ReservationOutcome, string>.Failure(supplier)))
            .ToArray();

        // Selection freezes the winner before canceling and draining losers.
        // Independent probe/cleanup faults propagate, and typed declines retain
        // input order. Already-ready successes are resolved in input order.
        var reservation = await probes.FirstSuccessAsync(
                Timeout.InfiniteTimeSpan,
                TimeProvider.System,
                cancellationToken)
            .ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();
        return reservation.Match(
            winner => winner,
            failures => new ReservationOutcome(false, null, null, failures));
    }

    private static async IAsyncEnumerable<StockItem> StreamItems(
        IReadOnlyList<StockItem> items,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        for (var index = 0; index < items.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return items[index];
        }
    }
}
