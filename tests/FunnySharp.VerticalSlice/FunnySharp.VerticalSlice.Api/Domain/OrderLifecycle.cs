namespace FunnySharp.VerticalSlice.Domain;

/// <summary>
/// The order lifecycle as one pure state machine. Handlers own one event kind each and are combined
/// with <c>OrElse</c>, so an event no handler owns stays <c>Undefined</c> instead of silently
/// succeeding. Transitions read no clock, no randomness, and no I/O.
/// </summary>
public static class OrderLifecycle
{
    private static readonly StateMachine<Order, OrderEvent, OrderCommand, OrderError> Placement = HandlePlacement;

    private static readonly StateMachine<Order, OrderEvent, OrderCommand, OrderError> Payment = HandlePayment;

    private static readonly StateMachine<Order, OrderEvent, OrderCommand, OrderError> Fulfillment = HandleFulfillment;

    private static readonly StateMachine<Order, OrderEvent, OrderCommand, OrderError> Cancellation = HandleCancellation;

    public static StateMachine<Order, OrderEvent, OrderCommand, OrderError> Machine { get; } =
        Placement.OrElse(Payment).OrElse(Fulfillment).OrElse(Cancellation);

    /// <summary>Decides one event and returns either a plan or a typed rejection/failure.</summary>
    public static Result<OrderPlan, OrderError> Plan(Order state, OrderEvent @event) =>
        Machine(state, @event).Match(
            change => Result<OrderPlan, OrderError>.Success(new OrderPlan(change.State, change.Outputs)),
            rejected => Result<OrderPlan, OrderError>.Failure(rejected),
            failed => Result<OrderPlan, OrderError>.Failure(failed),
            () => Result<OrderPlan, OrderError>.Failure(
                new OrderError.UndefinedTransition(state.Id, state.Status, @event.Kind)));

    private static TransitionResult<Order, OrderCommand, OrderError> HandlePlacement(Order state, OrderEvent @event)
    {
        if (@event is not OrderEvent.Place placement)
        {
            return TransitionResult<Order, OrderCommand, OrderError>.Undefined();
        }

        if (state.Status != OrderStatus.Draft)
        {
            return Rejected(state, @event, "order-already-placed");
        }

        var placed = state with
        {
            Status = OrderStatus.Placed,
            Revision = state.Revision + 1,
            UpdatedAt = placement.OccurredAt,
        };

        var outputs = new List<OrderCommand>(placed.Lines.Count + 2);
        foreach (var line in placed.Lines.ToReadOnlyList())
        {
            outputs.Add(new OrderCommand.ReserveInventory(line.Sku, line.Quantity));
        }

        outputs.Add(new OrderCommand.Persist(placed, placement));
        outputs.Add(new OrderCommand.NotifyCustomer(placed.CustomerId, placed.Id, "order-placed"));
        return Applied(placed, outputs);
    }

    private static TransitionResult<Order, OrderCommand, OrderError> HandlePayment(Order state, OrderEvent @event)
    {
        if (@event is not OrderEvent.Pay payment)
        {
            return TransitionResult<Order, OrderCommand, OrderError>.Undefined();
        }

        switch (state.Status)
        {
            case OrderStatus.Draft:
                return Rejected(state, @event, "order-not-placed");
            case OrderStatus.Paid:
                return Rejected(state, @event, "payment-already-recorded");
            case OrderStatus.Shipped:
                return Rejected(state, @event, "order-already-shipped");
            case OrderStatus.Cancelled:
                return Rejected(state, @event, "order-cancelled");
        }

        if (!payment.Authorization.Approved)
        {
            return TransitionResult<Order, OrderCommand, OrderError>.Failed(
                new OrderError.PaymentDeclined(
                    state.Id,
                    payment.Amount,
                    payment.Authorization.DeclineReason ?? "declined"));
        }

        var paid = state with
        {
            Status = OrderStatus.Paid,
            Revision = state.Revision + 1,
            UpdatedAt = payment.OccurredAt,
            PaymentReference = payment.Authorization.Reference,
        };

        return Applied(
            paid,
            [
                new OrderCommand.Persist(paid, payment),
                new OrderCommand.NotifyCustomer(paid.CustomerId, paid.Id, "payment-received"),
            ]);
    }

    private static TransitionResult<Order, OrderCommand, OrderError> HandleFulfillment(Order state, OrderEvent @event)
    {
        if (@event is not OrderEvent.Ship shipment)
        {
            return TransitionResult<Order, OrderCommand, OrderError>.Undefined();
        }

        if (state.Status == OrderStatus.Shipped)
        {
            return Rejected(state, @event, "order-already-shipped");
        }

        if (state.Status != OrderStatus.Paid)
        {
            return Rejected(state, @event, state.Status == OrderStatus.Cancelled ? "order-cancelled" : "order-not-paid");
        }

        var shipped = state with
        {
            Status = OrderStatus.Shipped,
            Revision = state.Revision + 1,
            UpdatedAt = shipment.OccurredAt,
            TrackingCode = shipment.TrackingCode.Value,
        };

        return Applied(
            shipped,
            [
                new OrderCommand.Persist(shipped, shipment),
                new OrderCommand.PublishShipment(shipped.Id, shipment.TrackingCode),
                new OrderCommand.NotifyCustomer(shipped.CustomerId, shipped.Id, "order-shipped"),
            ]);
    }

    private static TransitionResult<Order, OrderCommand, OrderError> HandleCancellation(Order state, OrderEvent @event)
    {
        if (@event is not OrderEvent.Cancel cancellation)
        {
            return TransitionResult<Order, OrderCommand, OrderError>.Undefined();
        }

        if (state.Status == OrderStatus.Shipped)
        {
            return Rejected(state, @event, "order-already-shipped");
        }

        if (state.Status == OrderStatus.Cancelled)
        {
            return Rejected(state, @event, "order-already-cancelled");
        }

        var cancelled = state with
        {
            Status = OrderStatus.Cancelled,
            Revision = state.Revision + 1,
            UpdatedAt = cancellation.OccurredAt,
            CancellationReason = cancellation.Reason,
        };

        var outputs = new List<OrderCommand>(cancelled.Lines.Count + 2);
        foreach (var line in cancelled.Lines.ToReadOnlyList())
        {
            outputs.Add(new OrderCommand.ReleaseInventory(line.Sku, line.Quantity));
        }

        outputs.Add(new OrderCommand.Persist(cancelled, cancellation));
        outputs.Add(new OrderCommand.NotifyCustomer(cancelled.CustomerId, cancelled.Id, "order-cancelled"));
        return Applied(cancelled, outputs);
    }

    private static TransitionResult<Order, OrderCommand, OrderError> Rejected(
        Order state,
        OrderEvent @event,
        string code) =>
        TransitionResult<Order, OrderCommand, OrderError>.Rejected(
            new OrderError.TransitionRejected(state.Id, state.Status, @event.Kind, code));

    private static TransitionResult<Order, OrderCommand, OrderError> Applied(
        Order next,
        IReadOnlyList<OrderCommand> outputs) =>
        TransitionResult<Order, OrderCommand, OrderError>.Applied(StateChange<Order, OrderCommand>.To(next, [.. outputs]));
}
