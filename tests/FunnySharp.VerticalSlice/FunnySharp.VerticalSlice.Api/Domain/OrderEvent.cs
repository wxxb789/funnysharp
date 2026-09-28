namespace FunnySharp.VerticalSlice.Domain;

/// <summary>
/// A lifecycle event. The hierarchy is closed: the pure machine owns every case, and each event
/// carries the instant it happened so transitions never read a clock.
/// </summary>
public abstract record OrderEvent
{
    private OrderEvent(OrderEventKind kind, DateTimeOffset occurredAt)
    {
        Kind = kind;
        OccurredAt = occurredAt;
    }

    public OrderEventKind Kind { get; }

    public DateTimeOffset OccurredAt { get; }

    public sealed record Place(DateTimeOffset OccurredAt) : OrderEvent(OrderEventKind.Place, OccurredAt);

    public sealed record Pay(PaymentAuthorization Authorization, Money Amount, DateTimeOffset OccurredAt)
        : OrderEvent(OrderEventKind.Pay, OccurredAt);

    public sealed record Ship(TrackingCode TrackingCode, DateTimeOffset OccurredAt)
        : OrderEvent(OrderEventKind.Ship, OccurredAt);

    public sealed record Cancel(string Reason, DateTimeOffset OccurredAt)
        : OrderEvent(OrderEventKind.Cancel, OccurredAt);
}
