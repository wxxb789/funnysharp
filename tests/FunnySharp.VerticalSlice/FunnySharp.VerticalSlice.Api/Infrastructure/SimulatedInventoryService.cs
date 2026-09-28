using FunnySharp.VerticalSlice.Application;
using FunnySharp.VerticalSlice.Domain;

namespace FunnySharp.VerticalSlice.Infrastructure;

/// <summary>
/// A deterministic stock simulator: each SKU has 3 to 12 units derived from its name, and reservations
/// are subtracted as they are placed. Every read-modify-write runs under one gate, so two requests for
/// the same SKU can never both pass the availability check and oversell it.
/// </summary>
public sealed class SimulatedInventoryService(VerticalSliceOptions options) : IInventoryService
{
    private readonly Lock gate = new();
    private readonly Dictionary<string, int> reservations = [];

    public async ValueTask<Option<int>> AvailableAsync(Sku sku, CancellationToken cancellationToken)
    {
        await SimulatedDelay.WaitAsync(options.DependencyLatency, cancellationToken);
        lock (gate)
        {
            return Option.Some(Available(sku));
        }
    }

    public async ValueTask<UnitResult<OrderError>> ReserveAsync(
        Sku sku,
        Quantity quantity,
        CancellationToken cancellationToken)
    {
        await SimulatedDelay.WaitAsync(options.DependencyLatency, cancellationToken);
        lock (gate)
        {
            var available = Available(sku);
            if (available < quantity.Value)
            {
                return UnitResult<OrderError>.Failure(new OrderError.InventoryShortfall(sku, quantity, available));
            }

            reservations[sku.Value] = Reserved(sku) + quantity.Value;
        }

        return UnitResult<OrderError>.Success();
    }

    public async ValueTask<UnitResult<OrderError>> ReleaseAsync(
        Sku sku,
        Quantity quantity,
        CancellationToken cancellationToken)
    {
        await SimulatedDelay.WaitAsync(options.DependencyLatency, cancellationToken);
        lock (gate)
        {
            var remaining = Math.Max(0, Reserved(sku) - quantity.Value);
            if (remaining == 0)
            {
                // A fully released SKU leaves no entry behind; a missing key and a zero key read the same.
                reservations.Remove(sku.Value);
            }
            else
            {
                reservations[sku.Value] = remaining;
            }
        }

        return UnitResult<OrderError>.Success();
    }

    /// <summary>Availability for a caller that already holds <see cref="gate"/>.</summary>
    private int Available(Sku sku) => StockOf(sku) - Reserved(sku);

    private int Reserved(Sku sku) => reservations.TryGetValue(sku.Value, out var reserved) ? reserved : 0;

    private static int StockOf(Sku sku) => 3 + (StableSimulationHash.Of(sku.Value) % 10);
}
