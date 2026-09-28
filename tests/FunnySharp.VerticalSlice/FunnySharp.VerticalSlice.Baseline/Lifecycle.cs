namespace FunnySharp.VerticalSlice.Baseline;

/// <summary>
/// The lifecycle as one switch: the state machine is implicit in the pattern match, and an event the
/// current state does not accept throws. Effects are executed by the endpoint after Apply returns.
/// </summary>
public static class Lifecycle
{
    public static Order Apply(Order state, OrderEvent @event) => @event.Kind switch
    {
        "Place" => state.Status == OrderStatus.Draft
            ? state with
            {
                Status = OrderStatus.Placed,
                Revision = state.Revision + 1,
                UpdatedAt = DateTimeOffset.UtcNow,
            }
            : throw new TransitionRejectedException("order-already-placed", state.Status, @event.Kind),
        "Cancel" when state.Status is OrderStatus.Draft or OrderStatus.Placed or OrderStatus.Paid =>
            state with
            {
                Status = OrderStatus.Cancelled,
                Revision = state.Revision + 1,
                UpdatedAt = DateTimeOffset.UtcNow,
                CancellationReason = @event.Reason,
            },
        "Cancel" => throw new TransitionRejectedException(
            state.Status == OrderStatus.Shipped ? "order-already-shipped" : "order-already-cancelled",
            state.Status,
            @event.Kind),
        "Ship" when state.Status == OrderStatus.Paid => state with
        {
            Status = OrderStatus.Shipped,
            Revision = state.Revision + 1,
            UpdatedAt = DateTimeOffset.UtcNow,
            TrackingCode = @event.Reference,
        },
        "Ship" => throw new TransitionRejectedException(
            state.Status switch
            {
                OrderStatus.Shipped => "order-already-shipped",
                OrderStatus.Cancelled => "order-cancelled",
                _ => "order-not-paid",
            },
            state.Status,
            @event.Kind),
        "Pay" when state.Status != OrderStatus.Placed => throw new TransitionRejectedException(
            state.Status switch
            {
                OrderStatus.Draft => "order-not-placed",
                OrderStatus.Paid => "payment-already-recorded",
                OrderStatus.Shipped => "order-already-shipped",
                _ => "order-cancelled",
            },
            state.Status,
            @event.Kind),
        "Pay" when !@event.Approved => throw new PaymentDeclinedException(
            @event.Reason ?? "declined",
            @event.Amount,
            state.Currency),
        "Pay" => state with
        {
            Status = OrderStatus.Paid,
            Revision = state.Revision + 1,
            UpdatedAt = DateTimeOffset.UtcNow,
            PaymentReference = @event.Reference,
        },
        _ => throw new UndefinedTransitionException(state.Status, @event.Kind),
    };

    /// <summary>Replays history to prove the stored projection still follows from its events.</summary>
    public static (Order Order, bool MatchesStored) Replay(Order stored, IReadOnlyList<string> history)
    {
        var state = stored with
        {
            Status = OrderStatus.Draft,
            Revision = 0,
            UpdatedAt = stored.CreatedAt,
            PaymentReference = null,
            TrackingCode = null,
            CancellationReason = null,
        };

        foreach (var kind in history)
        {
            state = Apply(state, new OrderEvent(kind));
        }

        return (state, state.Status == stored.Status && state.Revision == stored.Revision);
    }
}
