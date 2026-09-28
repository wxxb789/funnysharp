using FunnySharp.VerticalSlice.Application;
using FunnySharp.VerticalSlice.Domain;
using FunnySharp.VerticalSlice.Infrastructure;

namespace FunnySharp.VerticalSlice.Tests;

/// <summary>The pure lifecycle: decisions, emitted commands, replay, and the store's revision check.</summary>
public sealed class OrderLifecycleTests
{
    [Fact]
    public void PlacementAppliesAndEmitsInventoryThenPersistThenNotification()
    {
        var draft = Draft();

        var plan = OrderLifecycle.Plan(draft, new OrderEvent.Place(Refined.Now));

        Assert.True(plan.TryGetValue(out var planned));
        Assert.Equal(OrderStatus.Placed, planned.Order.Status);
        Assert.Equal(1, planned.Order.Revision);
        Assert.Equal(Refined.Now, planned.Order.UpdatedAt);
        Assert.Collection(
            planned.Commands,
            command => Assert.Equal(new OrderCommand.ReserveInventory(Refined.SkuOf("SKU-BOOK"), Refined.QuantityOf(2)), command),
            command => Assert.Equal(new OrderCommand.Persist(planned.Order, new OrderEvent.Place(Refined.Now)), command),
            command => Assert.IsType<OrderCommand.NotifyCustomer>(command));
    }

    [Fact]
    public void PaymentOnADraftOrderIsRejectedWithATypedCode()
    {
        var plan = OrderLifecycle.Plan(Draft(), new OrderEvent.Pay(PaymentAuthorization.Granted("PAY-1"), Refined.MoneyOf(20m), Refined.Now));

        Assert.False(plan.IsSuccess);
        Assert.True(plan.TryGetError(out var error));
        var rejected = Assert.IsType<OrderError.TransitionRejected>(error);
        Assert.Equal("order-not-placed", rejected.Code);
        Assert.Equal(OrderEventKind.Pay, rejected.Event);
        Assert.Equal(OrderStatus.Draft, rejected.Status);
    }

    [Fact]
    public void ADeclinedAuthorizationFailsTheDefinedTransition()
    {
        var placed = Placed();

        var plan = OrderLifecycle.Plan(
            placed,
            new OrderEvent.Pay(PaymentAuthorization.Declined("insufficient funds"), placed.Total, Refined.Now));

        Assert.True(plan.TryGetError(out var error));
        var declined = Assert.IsType<OrderError.PaymentDeclined>(error);
        Assert.Equal("insufficient funds", declined.Reason);
        Assert.Equal(placed.Total, declined.Amount);
    }

    [Fact]
    public void ShippingBeforePaymentIsRejectedAndCancellingAfterShippingIsRejected()
    {
        var placed = Placed();
        var shipTooEarly = OrderLifecycle.Plan(placed, new OrderEvent.Ship(Refined.TrackingOf("TRACK-1"), Refined.Now));
        Assert.True(shipTooEarly.TryGetError(out var notPaid));
        Assert.Equal("order-not-paid", Assert.IsType<OrderError.TransitionRejected>(notPaid).Code);

        var shipped = Shipped();
        var cancelTooLate = OrderLifecycle.Plan(shipped, new OrderEvent.Cancel("customer changed their mind", Refined.Now));
        Assert.True(cancelTooLate.TryGetError(out var rejection));
        Assert.Equal("order-already-shipped", Assert.IsType<OrderError.TransitionRejected>(rejection).Code);
    }

    [Fact]
    public void CancellationReleasesEveryLineBeforeItPersists()
    {
        var placed = Placed();

        var plan = OrderLifecycle.Plan(placed, new OrderEvent.Cancel("customer changed their mind", Refined.Now));

        Assert.True(plan.TryGetValue(out var planned));
        Assert.Equal(OrderStatus.Cancelled, planned.Order.Status);
        Assert.IsType<OrderCommand.ReleaseInventory>(planned.Commands[0]);
        Assert.IsType<OrderCommand.Persist>(planned.Commands[1]);
        Assert.IsType<OrderCommand.NotifyCustomer>(planned.Commands[2]);
    }

    [Fact]
    public void ReplayReproducesTheStoredProjectionAndDetectsDivergence()
    {
        var placed = Placed();
        var paid = OrderLifecycle.Plan(placed, new OrderEvent.Pay(PaymentAuthorization.Granted("PAY-1"), placed.Total, Refined.Now));
        Assert.True(paid.TryGetValue(out var paidPlan));
        var record = new OrderRecord(
            paidPlan.Order,
            [new OrderEvent.Place(Refined.Now), new OrderEvent.Pay(PaymentAuthorization.Granted("PAY-1"), placed.Total, Refined.Now)]);

        var first = OrderTimeline.Project(record);
        var second = OrderTimeline.Project(record);
        Assert.True(first.TryGetValue(out var projection));
        Assert.True(second.TryGetValue(out var repeated));
        Assert.True(projection.MatchesStored);
        Assert.Equal(projection.Recomputed, repeated.Recomputed);

        var diverged = OrderTimeline.Project(
            new OrderRecord(
                paidPlan.Order,
                [new OrderEvent.Place(Refined.Now), new OrderEvent.Ship(Refined.TrackingOf("TRACK-1"), Refined.Now)]));
        Assert.True(diverged.TryGetError(out var error));
        var divergence = Assert.IsType<OrderError.HistoryDiverged>(error);
        Assert.Equal(OrderEventKind.Ship, divergence.Event);
    }

    [Fact]
    public async Task TheStoreRejectsAStaleRevisionInsteadOfOverwritingIt()
    {
        var store = new InMemoryOrderStore(new VerticalSliceOptions { StoreLatency = TimeSpan.Zero });
        var draft = Draft();
        await store.CreateAsync(draft, TestContext.Current.CancellationToken);
        var placed = OrderLifecycle.Plan(draft, new OrderEvent.Place(Refined.Now));
        Assert.True(placed.TryGetValue(out var first));

        var accepted = await store.PersistAsync(
            first.Order,
            new OrderEvent.Place(Refined.Now),
            TestContext.Current.CancellationToken);

        Assert.True(accepted.IsSuccess);
        var stale = first.Order with { Status = OrderStatus.Cancelled, UpdatedAt = Refined.Now };
        var rejected = await store.PersistAsync(
            stale,
            new OrderEvent.Cancel("customer changed their mind", Refined.Now),
            TestContext.Current.CancellationToken);
        Assert.True(rejected.TryGetError(out var error));
        var conflict = Assert.IsType<OrderError.ConcurrencyConflict>(error);
        Assert.Equal(1, conflict.ExpectedRevision);
        Assert.Equal(2, conflict.ActualRevision);
    }

    private static Order Draft() =>
        Order.Draft(OrderId.New(), Refined.CustomerOf("customer-42"), Refined.LinesOf(9.95m, ("SKU-BOOK", 2)), Refined.Now);

    private static Order Placed()
    {
        var plan = OrderLifecycle.Plan(Draft(), new OrderEvent.Place(Refined.Now));
        Assert.True(plan.TryGetValue(out var planned));
        return planned.Order;
    }

    private static Order Paid()
    {
        var placed = Placed();
        var plan = OrderLifecycle.Plan(placed, new OrderEvent.Pay(PaymentAuthorization.Granted("PAY-1"), placed.Total, Refined.Now));
        Assert.True(plan.TryGetValue(out var paid));
        return paid.Order;
    }

    private static Order Shipped()
    {
        var paid = Paid();
        var plan = OrderLifecycle.Plan(paid, new OrderEvent.Ship(Refined.TrackingOf("TRACK-1"), Refined.Now));
        Assert.True(plan.TryGetValue(out var shipped));
        return shipped.Order;
    }

    [Fact]
    public void PaymentIsRejectedForEveryStateThatCannotAcceptIt()
    {
        var paid = Paid();
        var payAgain = OrderLifecycle.Plan(paid, new OrderEvent.Pay(PaymentAuthorization.Granted("PAY-2"), paid.Total, Refined.Now));
        Assert.True(payAgain.TryGetError(out var alreadyPaid));
        Assert.Equal("payment-already-recorded", Assert.IsType<OrderError.TransitionRejected>(alreadyPaid).Code);

        var shipped = Shipped();
        var payShipped = OrderLifecycle.Plan(shipped, new OrderEvent.Pay(PaymentAuthorization.Granted("PAY-2"), shipped.Total, Refined.Now));
        Assert.True(payShipped.TryGetError(out var afterShipment));
        Assert.Equal("order-already-shipped", Assert.IsType<OrderError.TransitionRejected>(afterShipment).Code);

        var cancelled = Cancelled();
        var payCancelled = OrderLifecycle.Plan(cancelled, new OrderEvent.Pay(PaymentAuthorization.Granted("PAY-2"), cancelled.Total, Refined.Now));
        Assert.True(payCancelled.TryGetError(out var afterCancellation));
        Assert.Equal("order-cancelled", Assert.IsType<OrderError.TransitionRejected>(afterCancellation).Code);
    }

    private static Order Cancelled()
    {
        var paid = Paid();
        var plan = OrderLifecycle.Plan(paid, new OrderEvent.Cancel("customer changed their mind", Refined.Now));
        Assert.True(plan.TryGetValue(out var cancelled));
        return cancelled.Order;
    }
}
