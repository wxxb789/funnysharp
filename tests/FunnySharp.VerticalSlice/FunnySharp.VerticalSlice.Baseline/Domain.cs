using System.Collections.Concurrent;

namespace FunnySharp.VerticalSlice.Baseline;

/// <summary>Lifecycle status of an order.</summary>
public enum OrderStatus
{
    Draft,
    Placed,
    Paid,
    Shipped,
    Cancelled,
}

/// <summary>One priced order line. Refinement happens once, at the endpoint boundary.</summary>
public sealed record OrderLine(string Sku, int Quantity, decimal UnitPrice);

/// <summary>The order projection, carrying the currency alongside the amount.</summary>
public sealed record Order
{
    public required string Id { get; init; }

    public required string CustomerId { get; init; }

    public required IReadOnlyList<OrderLine> Lines { get; init; }

    public required string Currency { get; init; }

    public required OrderStatus Status { get; init; }

    public required int Revision { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public required DateTimeOffset UpdatedAt { get; init; }

    public string? PaymentReference { get; init; }

    public string? TrackingCode { get; init; }

    public string? CancellationReason { get; init; }

    public decimal Total => Lines.Sum(static line => line.UnitPrice * line.Quantity);
}

/// <summary>Lifecycle events, modelled as one record with a kind.</summary>
public sealed record OrderEvent(
    string Kind,
    string? Reference = null,
    decimal Amount = 0m,
    bool Approved = false,
    string? Reason = null);

/// <summary>In-memory persistence with a revision check; a lost update throws.</summary>
public sealed class InMemoryOrderStore
{
    private readonly Lock gate = new();
    private readonly Dictionary<string, (Order Order, List<string> History)> orders = [];

    public void Create(Order draft)
    {
        lock (gate)
        {
            orders[draft.Id] = (draft, []);
        }
    }

    public Order? Find(string id)
    {
        lock (gate)
        {
            return orders.TryGetValue(id, out var entry) ? entry.Order : null;
        }
    }

    public IReadOnlyList<string> History(string id)
    {
        lock (gate)
        {
            return orders.TryGetValue(id, out var entry) ? [.. entry.History] : [];
        }
    }

    public void Persist(Order order, string change)
    {
        lock (gate)
        {
            if (!orders.TryGetValue(order.Id, out var entry))
            {
                throw new OrderNotFoundException(order.Id);
            }

            if (order.Revision != entry.Order.Revision + 1)
            {
                throw new ConcurrencyConflictException(order.Id, order.Revision, entry.Order.Revision + 1);
            }

            entry.History.Add(change);
            orders[order.Id] = (order, entry.History);
        }
    }

    public IReadOnlyList<Order> Snapshot()
    {
        lock (gate)
        {
            return
            [
                .. orders.Values
                    .Select(static entry => entry.Order)
                    .OrderBy(static order => order.CreatedAt)
                    .ThenBy(static order => order.Id, StringComparer.Ordinal),
            ];
        }
    }
}

/// <summary>Deterministic stock and pricing simulators, the way a small service usually starts out.</summary>
public static class SimulatedDependencies
{
    private static readonly Dictionary<string, int> Reservations = new(StringComparer.Ordinal);

    private static readonly Lock ReservationGate = new();

    public static int StockOf(string sku) => 3 + (StableHash(sku) % 10);

    public static int Available(string sku)
    {
        lock (ReservationGate)
        {
            return AvailableCore(sku);
        }
    }

    /// <summary>
    /// Reserves every line or none of them, so a rejected placement leaves no stock behind. The
    /// comparison app has no interpreter to compensate a partial run, so it decides first and commits once.
    /// </summary>
    public static void ReserveAll(IReadOnlyList<OrderLine> lines)
    {
        lock (ReservationGate)
        {
            foreach (var line in lines)
            {
                if (AvailableCore(line.Sku) < line.Quantity)
                {
                    throw new InventoryShortfallException(line.Sku, line.Quantity, AvailableCore(line.Sku));
                }
            }

            foreach (var line in lines)
            {
                Reservations[line.Sku] = Reserved(line.Sku) + line.Quantity;
            }
        }
    }

    public static void Release(string sku, int quantity)
    {
        lock (ReservationGate)
        {
            var remaining = Math.Max(0, Reserved(sku) - quantity);
            if (remaining == 0)
            {
                // A fully released SKU leaves no permanent entry behind.
                Reservations.Remove(sku);
            }
            else
            {
                Reservations[sku] = remaining;
            }
        }
    }

    public static decimal PriceOf(string sku, string supplier) => 9.95m + (StableHash(sku + supplier) % 500) / 100m;

    public static int StableHash(string value)
    {
        var hash = 17;
        foreach (var character in value)
        {
            hash = ((hash * 31) + character) & 0x7fffffff;
        }

        return hash;
    }

    private static int Reserved(string sku) => Reservations.TryGetValue(sku, out var reserved) ? reserved : 0;

    /// <summary>Availability for a caller that already holds <see cref="ReservationGate"/>.</summary>
    private static int AvailableCore(string sku) => StockOf(sku) - Reserved(sku);
}
