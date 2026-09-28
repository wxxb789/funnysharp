namespace FunnySharp.VerticalSlice.Domain;

/// <summary>
/// An output command emitted by a pure transition and executed later at the application boundary.
/// Commands are data: constructing one performs no I/O.
/// </summary>
public abstract record OrderCommand
{
    private OrderCommand()
    {
    }

    public sealed record ReserveInventory(Sku Sku, Quantity Quantity) : OrderCommand;

    public sealed record ReleaseInventory(Sku Sku, Quantity Quantity) : OrderCommand;

    public sealed record Persist(Order Order, OrderEvent Change) : OrderCommand;

    public sealed record NotifyCustomer(CustomerId CustomerId, OrderId OrderId, string Kind) : OrderCommand;

    public sealed record PublishShipment(OrderId OrderId, TrackingCode TrackingCode) : OrderCommand;
}
