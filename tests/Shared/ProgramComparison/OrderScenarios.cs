using FunnySharp;

namespace FunnySharp.TestSupport.ProgramComparison;

internal sealed record ExpectedDelivery(
    long? CommandSequence,
    DateTimeOffset At,
    TransitionStatus Status,
    OrderError? Error);

internal sealed record OrderScenarioExpectation(
    string Name,
    OrderModel Model,
    DateTimeOffset Now,
    IReadOnlyList<EmittedOrderCommand> EmittedCommands,
    IReadOnlyList<ExpectedDelivery> Deliveries,
    IReadOnlyList<OrderFailure> CommandFailures);

internal sealed record OrderScenario(
    string Name,
    int? Seed,
    Action<IOrderSimulation> Run,
    OrderScenarioExpectation Expectation);

internal sealed record OrderRun(IOrderSimulation Simulation, OrderRunReport Report);

internal static class OrderScenarios
{
    internal static readonly DateTimeOffset StartedAt =
        new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    internal static readonly IReadOnlyList<OrderScenario> All =
    [
        SuccessfulCancellation(),
        LateDuplicateAndStale(),
        RejectedTransitions(),
        FailedAndCommandFailure(),
        InventoryUnavailableAndTimeout(),
        RandomGateway("Random gateway captured", seed: 1, captured: true),
        RandomGateway("Random gateway declined", seed: 2, captured: false),
    ];

    internal static OrderModel InitialModel() =>
        new(
            "ORD-1",
            new InventoryState("SKU-1", 0, 0, InventoryStatus.Idle, 0),
            new PaymentState(0m, PaymentStatus.Idle, 0, default),
            new CancellationState(CancellationStatus.None, null, 0));

    internal static OrderRun RunHandWritten(OrderScenario scenario)
    {
        var simulation = new HandWrittenOrderSimulation(
            InitialModel(),
            StartedAt,
            scenario.Seed is { } seed ? new Random(seed) : null);
        scenario.Run(simulation);
        return new OrderRun(simulation, OrderRunReports.Capture(simulation));
    }

    internal static OrderRun RunFunnySharp(OrderScenario scenario)
    {
        var simulation = new FunnySharpOrderSimulation(
            FunnySharpOrderPrograms.Create(),
            InitialModel(),
            StartedAt,
            scenario.Seed is { } seed ? new Random(seed) : null);
        scenario.Run(simulation);
        return new OrderRun(simulation, OrderRunReports.Capture(simulation));
    }

    private static OrderScenario SuccessfulCancellation()
    {
        const string name = "successful cancellation";
        return new OrderScenario(
            name,
            Seed: null,
            Run: static simulation =>
            {
                var initial = simulation.Model;
                var reserve = simulation.Deliver(
                    new ReserveInventory("ORD-1", "SKU-1", 2, 100));
                OrderAssert.True(reserve.IsApplied, name + ": reserve applies");
                OrderAssert.True(
                    ReferenceEquals(initial.Payment, simulation.Model.Payment),
                    name + ": inventory update preserves the payment reference");
                OrderAssert.True(
                    ReferenceEquals(initial.Cancellation, simulation.Model.Cancellation),
                    name + ": inventory update preserves the cancellation reference");
                OrderAssert.Equal(1, simulation.PendingCount, name + ": one reserve command");
                var reserveSequence = simulation.PeekPendingSequence();
                OrderAssert.Equal(
                    new ReserveInventoryCommand("ORD-1", "SKU-1", 2, 100),
                    simulation.PeekPendingCommand(),
                    name + ": reserve command");

                simulation.AdvanceBy(TimeSpan.FromMinutes(1));
                var reserved = simulation.Reply(
                    reserveSequence,
                    new InventoryReserved("ORD-1", 100, true));
                OrderAssert.True(reserved.IsApplied, name + ": inventory reply applies");

                var afterReserve = simulation.Model;
                var charge = simulation.Deliver(
                    new ChargePayment("ORD-1", 25m, 200, simulation.Now));
                OrderAssert.True(charge.IsApplied, name + ": charge applies");
                OrderAssert.True(
                    ReferenceEquals(afterReserve.Inventory, simulation.Model.Inventory),
                    name + ": payment update preserves the inventory reference");
                OrderAssert.True(
                    ReferenceEquals(afterReserve.Cancellation, simulation.Model.Cancellation),
                    name + ": payment update preserves the cancellation reference");
                var chargeSequence = simulation.PeekPendingSequence();
                OrderAssert.Equal(
                    new ChargePaymentCommand("ORD-1", 25m, 200),
                    simulation.PeekPendingCommand(),
                    name + ": charge command");

                simulation.AdvanceBy(TimeSpan.FromMinutes(2));
                var captured = simulation.Reply(
                    chargeSequence,
                    new PaymentCharged("ORD-1", 200, true, null));
                OrderAssert.True(captured.IsApplied, name + ": payment reply applies");

                var cancel = simulation.Deliver(
                    new CancelOrder("ORD-1", "customer", 300));
                OrderAssert.True(cancel.IsApplied, name + ": cancellation applies");
                OrderAssert.Equal(3, simulation.PendingCount, name + ": three cancel commands");
                var releaseSequence = simulation.PeekPendingSequence();
                OrderAssert.Equal(
                    new ReleaseInventoryCommand("ORD-1", "SKU-1", 2),
                    simulation.PeekPendingCommand(),
                    name + ": release command is first");

                simulation.AdvanceBy(TimeSpan.FromMinutes(3));
                var refundSequence = releaseSequence + 1;
                var failure = simulation.FailCommand(
                    refundSequence,
                    new RefundGatewayUnavailable("gateway"));
                OrderAssert.Equal(refundSequence, failure.CommandSequence, name + ": refund failure sequence");
                OrderAssert.Equal(
                    new RefundGatewayUnavailable("gateway"),
                    failure.Error,
                    name + ": refund failure error");

                var refundFailed = simulation.Deliver(
                    new PaymentRefundFailed("ORD-1", 300, "gateway"));
                OrderAssert.True(refundFailed.IsApplied, name + ": refund failure message applies");
            },
            Expectation: new OrderScenarioExpectation(
                name,
                new OrderModel(
                    "ORD-1",
                    new InventoryState("SKU-1", 2, 2, InventoryStatus.Reserved, 100),
                    new PaymentState(
                        25m,
                        PaymentStatus.Captured,
                        200,
                        StartedAt.AddMinutes(6)),
                    new CancellationState(CancellationStatus.RefundFailed, "customer", 300)),
                StartedAt.AddMinutes(6),
                [
                    new EmittedOrderCommand(
                        1,
                        new ReserveInventoryCommand("ORD-1", "SKU-1", 2, 100),
                        StartedAt),
                    new EmittedOrderCommand(
                        2,
                        new ChargePaymentCommand("ORD-1", 25m, 200),
                        StartedAt.AddMinutes(1)),
                    new EmittedOrderCommand(
                        3,
                        new ReleaseInventoryCommand("ORD-1", "SKU-1", 2),
                        StartedAt.AddMinutes(3)),
                    new EmittedOrderCommand(
                        4,
                        new RefundPaymentCommand("ORD-1", 25m),
                        StartedAt.AddMinutes(3)),
                    new EmittedOrderCommand(
                        5,
                        new NotifyCustomerCommand("ORD-1", "Cancellation accepted."),
                        StartedAt.AddMinutes(3)),
                    new EmittedOrderCommand(
                        6,
                        new NotifyCustomerCommand("ORD-1", "Refund failed: gateway."),
                        StartedAt.AddMinutes(6)),
                ],
                [
                    new ExpectedDelivery(null, StartedAt, TransitionStatus.Applied, null),
                    new ExpectedDelivery(
                        1,
                        StartedAt.AddMinutes(1),
                        TransitionStatus.Applied,
                        null),
                    new ExpectedDelivery(
                        null,
                        StartedAt.AddMinutes(1),
                        TransitionStatus.Applied,
                        null),
                    new ExpectedDelivery(
                        2,
                        StartedAt.AddMinutes(3),
                        TransitionStatus.Applied,
                        null),
                    new ExpectedDelivery(
                        null,
                        StartedAt.AddMinutes(3),
                        TransitionStatus.Applied,
                        null),
                    new ExpectedDelivery(
                        null,
                        StartedAt.AddMinutes(6),
                        TransitionStatus.Applied,
                        null),
                ],
                [
                    new OrderFailure(
                        4,
                        new RefundGatewayUnavailable("gateway"),
                        StartedAt.AddMinutes(6)),
                ]));
    }

    private static OrderScenario LateDuplicateAndStale()
    {
        const string name = "late duplicate and stale responses";
        return new OrderScenario(
            name,
            Seed: null,
            Run: static simulation =>
            {
                var firstCharge = simulation.Deliver(
                    new ChargePayment("ORD-1", 25m, 200, simulation.Now));
                OrderAssert.True(firstCharge.IsApplied, name + ": first charge applies");
                var firstSequence = simulation.PeekPendingSequence();

                simulation.AdvanceBy(TimeSpan.FromMinutes(1));
                var secondCharge = simulation.Deliver(
                    new ChargePayment("ORD-1", 30m, 201, simulation.Now));
                OrderAssert.True(secondCharge.IsApplied, name + ": retry charge applies");
                OrderAssert.Equal(2, simulation.PendingCount, name + ": two pending charges");

                var stale = simulation.Reply(
                    firstSequence,
                    new PaymentCharged("ORD-1", 200, true, null));
                OrderAssert.True(stale.IsRejected, name + ": old reply is rejected");
                OrderAssert.True(stale.TryGetError(out var staleError), name + ": old reply error");
                OrderAssert.Equal(
                    new StaleResponse("PaymentCharged", 200, 201),
                    staleError!,
                    name + ": old reply is stale");

                var secondSequence = simulation.PeekPendingSequence();
                var captured = simulation.Reply(
                    secondSequence,
                    new PaymentCharged("ORD-1", 201, true, null));
                OrderAssert.True(captured.IsApplied, name + ": current reply applies");

                var duplicate = simulation.Reply(
                    secondSequence,
                    new PaymentCharged("ORD-1", 201, true, null));
                OrderAssert.True(duplicate.IsRejected, name + ": duplicate reply is rejected");
                OrderAssert.True(duplicate.TryGetError(out var duplicateError), name + ": duplicate error");
                OrderAssert.Equal(
                    new DuplicateResponse("PaymentCharged", 201),
                    duplicateError!,
                    name + ": duplicate reply is identified");

                var late = simulation.Reply(
                    firstSequence,
                    new PaymentCharged("ORD-1", 200, true, null));
                OrderAssert.True(late.IsRejected, name + ": late reply is rejected");
                OrderAssert.True(late.TryGetError(out var lateError), name + ": late reply error");
                OrderAssert.Equal(
                    new StaleResponse("PaymentCharged", 200, 201),
                    lateError!,
                    name + ": late reply is stale");
            },
            Expectation: new OrderScenarioExpectation(
                name,
                new OrderModel(
                    "ORD-1",
                    new InventoryState("SKU-1", 0, 0, InventoryStatus.Idle, 0),
                    new PaymentState(
                        30m,
                        PaymentStatus.Captured,
                        201,
                        StartedAt.AddMinutes(6)),
                    new CancellationState(CancellationStatus.None, null, 0)),
                StartedAt.AddMinutes(1),
                [
                    new EmittedOrderCommand(
                        1,
                        new ChargePaymentCommand("ORD-1", 25m, 200),
                        StartedAt),
                    new EmittedOrderCommand(
                        2,
                        new ChargePaymentCommand("ORD-1", 30m, 201),
                        StartedAt.AddMinutes(1)),
                ],
                [
                    new ExpectedDelivery(null, StartedAt, TransitionStatus.Applied, null),
                    new ExpectedDelivery(
                        null,
                        StartedAt.AddMinutes(1),
                        TransitionStatus.Applied,
                        null),
                    new ExpectedDelivery(
                        1,
                        StartedAt.AddMinutes(1),
                        TransitionStatus.Rejected,
                        new StaleResponse("PaymentCharged", 200, 201)),
                    new ExpectedDelivery(
                        2,
                        StartedAt.AddMinutes(1),
                        TransitionStatus.Applied,
                        null),
                    new ExpectedDelivery(
                        2,
                        StartedAt.AddMinutes(1),
                        TransitionStatus.Rejected,
                        new DuplicateResponse("PaymentCharged", 201)),
                    new ExpectedDelivery(
                        1,
                        StartedAt.AddMinutes(1),
                        TransitionStatus.Rejected,
                        new StaleResponse("PaymentCharged", 200, 201)),
                ],
                []));
    }

    private static OrderScenario RejectedTransitions()
    {
        const string name = "rejected transitions";
        return new OrderScenario(
            name,
            Seed: null,
            Run: static simulation =>
            {
                var earlyCancel = simulation.Deliver(
                    new CancelOrder("ORD-1", "too early", 300));
                OrderAssert.True(earlyCancel.IsRejected, name + ": early cancellation is rejected");
                OrderAssert.True(earlyCancel.TryGetError(out var earlyError), name + ": early cancellation error");
                OrderAssert.Equal(
                    new CancellationRejected(
                        "Cancellation requires reserved inventory and captured payment."),
                    earlyError!,
                    name + ": early cancellation reason");

                ReserveAndCapture(simulation, name);

                var cancel = simulation.Deliver(
                    new CancelOrder("ORD-1", "customer", 300));
                OrderAssert.True(cancel.IsApplied, name + ": cancellation applies");

                var duplicate = simulation.Deliver(
                    new CancelOrder("ORD-1", "again", 300));
                OrderAssert.True(duplicate.IsRejected, name + ": duplicate cancellation is rejected");
                OrderAssert.True(duplicate.TryGetError(out var duplicateError), name + ": duplicate cancellation error");
                OrderAssert.Equal(
                    new DuplicateResponse("CancelOrder", 300),
                    duplicateError!,
                    name + ": duplicate cancellation is identified");

                var stale = simulation.Deliver(
                    new CancelOrder("ORD-1", "new", 301));
                OrderAssert.True(stale.IsRejected, name + ": stale cancellation is rejected");
                OrderAssert.True(stale.TryGetError(out var staleError), name + ": stale cancellation error");
                OrderAssert.Equal(
                    new StaleResponse("CancelOrder", 301, 300),
                    staleError!,
                    name + ": stale cancellation is identified");

                var unrouted = simulation.Deliver(new UnroutedOrderMessage("ORD-1"));
                OrderAssert.True(unrouted.IsUndefined, name + ": unrouted message is undefined");
            },
            Expectation: new OrderScenarioExpectation(
                name,
                new OrderModel(
                    "ORD-1",
                    new InventoryState("SKU-1", 2, 2, InventoryStatus.Reserved, 100),
                    new PaymentState(
                        25m,
                        PaymentStatus.Captured,
                        200,
                        StartedAt.AddMinutes(5)),
                    new CancellationState(CancellationStatus.Cancelling, "customer", 300)),
                StartedAt,
                [
                    new EmittedOrderCommand(
                        1,
                        new ReserveInventoryCommand("ORD-1", "SKU-1", 2, 100),
                        StartedAt),
                    new EmittedOrderCommand(
                        2,
                        new ChargePaymentCommand("ORD-1", 25m, 200),
                        StartedAt),
                    new EmittedOrderCommand(
                        3,
                        new ReleaseInventoryCommand("ORD-1", "SKU-1", 2),
                        StartedAt),
                    new EmittedOrderCommand(
                        4,
                        new RefundPaymentCommand("ORD-1", 25m),
                        StartedAt),
                    new EmittedOrderCommand(
                        5,
                        new NotifyCustomerCommand("ORD-1", "Cancellation accepted."),
                        StartedAt),
                ],
                [
                    new ExpectedDelivery(
                        null,
                        StartedAt,
                        TransitionStatus.Rejected,
                        new CancellationRejected(
                            "Cancellation requires reserved inventory and captured payment.")),
                    new ExpectedDelivery(null, StartedAt, TransitionStatus.Applied, null),
                    new ExpectedDelivery(1, StartedAt, TransitionStatus.Applied, null),
                    new ExpectedDelivery(null, StartedAt, TransitionStatus.Applied, null),
                    new ExpectedDelivery(2, StartedAt, TransitionStatus.Applied, null),
                    new ExpectedDelivery(null, StartedAt, TransitionStatus.Applied, null),
                    new ExpectedDelivery(
                        null,
                        StartedAt,
                        TransitionStatus.Rejected,
                        new DuplicateResponse("CancelOrder", 300)),
                    new ExpectedDelivery(
                        null,
                        StartedAt,
                        TransitionStatus.Rejected,
                        new StaleResponse("CancelOrder", 301, 300)),
                    new ExpectedDelivery(null, StartedAt, TransitionStatus.Undefined, null),
                ],
                []));
    }

    private static OrderScenario FailedAndCommandFailure()
    {
        const string name = "failed transition and command failure";
        return new OrderScenario(
            name,
            Seed: null,
            Run: static simulation =>
            {
                var invalid = simulation.Deliver(
                    new ChargePayment("ORD-1", 0m, 199, simulation.Now));
                OrderAssert.True(invalid.IsFailed, name + ": invalid charge fails");
                OrderAssert.True(invalid.TryGetError(out var invalidError), name + ": invalid charge error");
                OrderAssert.Equal(
                    new InvalidTransition("Payment amount must be positive."),
                    invalidError!,
                    name + ": invalid charge reason");

                ReserveAndCapture(simulation, name);

                var cancel = simulation.Deliver(
                    new CancelOrder("ORD-1", "customer", 300));
                OrderAssert.True(cancel.IsApplied, name + ": cancellation applies");
                var releaseSequence = simulation.PeekPendingSequence();
                var refundSequence = releaseSequence + 1;

                var failure = simulation.FailCommand(
                    refundSequence,
                    new RefundGatewayUnavailable("gateway"));
                OrderAssert.Equal(refundSequence, failure.CommandSequence, name + ": refund failure sequence");

                var refundFailed = simulation.Deliver(
                    new PaymentRefundFailed("ORD-1", 300, "gateway"));
                OrderAssert.True(refundFailed.IsApplied, name + ": refund failure message applies");

                var stale = simulation.Deliver(
                    new PaymentRefundFailed("ORD-1", 299, "stale"));
                OrderAssert.True(stale.IsRejected, name + ": stale refund failure is rejected");
                OrderAssert.True(stale.TryGetError(out var staleError), name + ": stale refund failure error");
                OrderAssert.Equal(
                    new StaleResponse("PaymentRefundFailed", 299, 300),
                    staleError!,
                    name + ": stale refund failure is identified");

                var duplicate = simulation.Deliver(
                    new PaymentRefundFailed("ORD-1", 300, "duplicate"));
                OrderAssert.True(duplicate.IsRejected, name + ": duplicate refund failure is rejected");
                OrderAssert.True(duplicate.TryGetError(out var duplicateError), name + ": duplicate refund failure error");
                OrderAssert.Equal(
                    new DuplicateResponse("PaymentRefundFailed", 300),
                    duplicateError!,
                    name + ": duplicate refund failure is identified");
            },
            Expectation: new OrderScenarioExpectation(
                name,
                new OrderModel(
                    "ORD-1",
                    new InventoryState("SKU-1", 2, 2, InventoryStatus.Reserved, 100),
                    new PaymentState(
                        25m,
                        PaymentStatus.Captured,
                        200,
                        StartedAt.AddMinutes(5)),
                    new CancellationState(CancellationStatus.RefundFailed, "customer", 300)),
                StartedAt,
                [
                    new EmittedOrderCommand(
                        1,
                        new ReserveInventoryCommand("ORD-1", "SKU-1", 2, 100),
                        StartedAt),
                    new EmittedOrderCommand(
                        2,
                        new ChargePaymentCommand("ORD-1", 25m, 200),
                        StartedAt),
                    new EmittedOrderCommand(
                        3,
                        new ReleaseInventoryCommand("ORD-1", "SKU-1", 2),
                        StartedAt),
                    new EmittedOrderCommand(
                        4,
                        new RefundPaymentCommand("ORD-1", 25m),
                        StartedAt),
                    new EmittedOrderCommand(
                        5,
                        new NotifyCustomerCommand("ORD-1", "Cancellation accepted."),
                        StartedAt),
                    new EmittedOrderCommand(
                        6,
                        new NotifyCustomerCommand("ORD-1", "Refund failed: gateway."),
                        StartedAt),
                ],
                [
                    new ExpectedDelivery(
                        null,
                        StartedAt,
                        TransitionStatus.Failed,
                        new InvalidTransition("Payment amount must be positive.")),
                    new ExpectedDelivery(null, StartedAt, TransitionStatus.Applied, null),
                    new ExpectedDelivery(1, StartedAt, TransitionStatus.Applied, null),
                    new ExpectedDelivery(null, StartedAt, TransitionStatus.Applied, null),
                    new ExpectedDelivery(2, StartedAt, TransitionStatus.Applied, null),
                    new ExpectedDelivery(null, StartedAt, TransitionStatus.Applied, null),
                    new ExpectedDelivery(null, StartedAt, TransitionStatus.Applied, null),
                    new ExpectedDelivery(
                        null,
                        StartedAt,
                        TransitionStatus.Rejected,
                        new StaleResponse("PaymentRefundFailed", 299, 300)),
                    new ExpectedDelivery(
                        null,
                        StartedAt,
                        TransitionStatus.Rejected,
                        new DuplicateResponse("PaymentRefundFailed", 300)),
                ],
                [
                    new OrderFailure(
                        4,
                        new RefundGatewayUnavailable("gateway"),
                        StartedAt),
                ]));
    }

    private static OrderScenario InventoryUnavailableAndTimeout()
    {
        const string name = "inventory unavailable and payment timeout";
        return new OrderScenario(
            name,
            Seed: null,
            Run: static simulation =>
            {
                var reserve = simulation.Deliver(
                    new ReserveInventory("ORD-1", "SKU-1", 2, 100));
                OrderAssert.True(reserve.IsApplied, name + ": reserve applies");
                var reserveSequence = simulation.PeekPendingSequence();

                var unavailable = simulation.Reply(
                    reserveSequence,
                    new InventoryReserved("ORD-1", 100, false));
                OrderAssert.True(unavailable.IsApplied, name + ": unavailable reply applies");
                OrderAssert.Equal(
                    InventoryStatus.Unavailable,
                    simulation.Model.Inventory.Status,
                    name + ": inventory becomes unavailable");

                var duplicate = simulation.Deliver(
                    new InventoryReserved("ORD-1", 100, false));
                OrderAssert.True(duplicate.IsRejected, name + ": duplicate unavailable reply is rejected");
                OrderAssert.True(duplicate.TryGetError(out var duplicateError), name + ": duplicate unavailable error");
                OrderAssert.Equal(
                    new DuplicateResponse("InventoryReserved", 100),
                    duplicateError!,
                    name + ": duplicate unavailable is identified");

                var again = simulation.Deliver(
                    new ReserveInventory("ORD-1", "SKU-1", 2, 101));
                OrderAssert.True(again.IsRejected, name + ": second reserve is rejected");
                OrderAssert.True(again.TryGetError(out var againError), name + ": second reserve error");
                OrderAssert.Equal(
                    new InvalidTransition("Inventory has already been requested."),
                    againError!,
                    name + ": second reserve reason");

                var charge = simulation.Deliver(
                    new ChargePayment("ORD-1", 25m, 200, simulation.Now));
                OrderAssert.True(charge.IsApplied, name + ": charge applies");
                OrderAssert.Equal(
                    new ChargePaymentCommand("ORD-1", 25m, 200),
                    simulation.PeekPendingCommand(1),
                    name + ": charge command follows the pending unavailable notice");

                simulation.AdvanceBy(TimeSpan.FromMinutes(1));
                var early = simulation.Deliver(
                    new PaymentTimeout("ORD-1", simulation.Now));
                OrderAssert.True(early.IsRejected, name + ": early timeout is rejected");
                OrderAssert.True(early.TryGetError(out var earlyError), name + ": early timeout error");
                OrderAssert.Equal(
                    new InvalidTransition("Payment deadline has not been reached."),
                    earlyError!,
                    name + ": early timeout reason");

                simulation.AdvanceBy(TimeSpan.FromMinutes(5));
                var timedOut = simulation.Deliver(
                    new PaymentTimeout("ORD-1", simulation.Now));
                OrderAssert.True(timedOut.IsApplied, name + ": late timeout applies");

                var duplicateTimeout = simulation.Deliver(
                    new PaymentTimeout("ORD-1", simulation.Now));
                OrderAssert.True(duplicateTimeout.IsRejected, name + ": duplicate timeout is rejected");
                OrderAssert.True(duplicateTimeout.TryGetError(out var timeoutError), name + ": duplicate timeout error");
                OrderAssert.Equal(
                    new InvalidTransition("Payment is not awaiting a timeout."),
                    timeoutError!,
                    name + ": duplicate timeout reason");
            },
            Expectation: new OrderScenarioExpectation(
                name,
                new OrderModel(
                    "ORD-1",
                    new InventoryState("SKU-1", 2, 0, InventoryStatus.Unavailable, 100),
                    new PaymentState(
                        25m,
                        PaymentStatus.TimedOut,
                        200,
                        StartedAt.AddMinutes(5)),
                    new CancellationState(CancellationStatus.None, null, 0)),
                StartedAt.AddMinutes(6),
                [
                    new EmittedOrderCommand(
                        1,
                        new ReserveInventoryCommand("ORD-1", "SKU-1", 2, 100),
                        StartedAt),
                    new EmittedOrderCommand(
                        2,
                        new NotifyCustomerCommand(
                            "ORD-1",
                            "Inventory is unavailable for SKU-1."),
                        StartedAt),
                    new EmittedOrderCommand(
                        3,
                        new ChargePaymentCommand("ORD-1", 25m, 200),
                        StartedAt),
                    new EmittedOrderCommand(
                        4,
                        new NotifyCustomerCommand("ORD-1", "Payment timed out."),
                        StartedAt.AddMinutes(6)),
                ],
                [
                    new ExpectedDelivery(null, StartedAt, TransitionStatus.Applied, null),
                    new ExpectedDelivery(1, StartedAt, TransitionStatus.Applied, null),
                    new ExpectedDelivery(
                        null,
                        StartedAt,
                        TransitionStatus.Rejected,
                        new DuplicateResponse("InventoryReserved", 100)),
                    new ExpectedDelivery(
                        null,
                        StartedAt,
                        TransitionStatus.Rejected,
                        new InvalidTransition("Inventory has already been requested.")),
                    new ExpectedDelivery(null, StartedAt, TransitionStatus.Applied, null),
                    new ExpectedDelivery(
                        null,
                        StartedAt.AddMinutes(1),
                        TransitionStatus.Rejected,
                        new InvalidTransition("Payment deadline has not been reached.")),
                    new ExpectedDelivery(
                        null,
                        StartedAt.AddMinutes(6),
                        TransitionStatus.Applied,
                        null),
                    new ExpectedDelivery(
                        null,
                        StartedAt.AddMinutes(6),
                        TransitionStatus.Rejected,
                        new InvalidTransition("Payment is not awaiting a timeout.")),
                ],
                []));
    }

    private static OrderScenario RandomGateway(string name, int seed, bool captured)
    {
        return new OrderScenario(
            name,
            seed,
            Run: simulation =>
            {
                var reserve = simulation.Deliver(
                    new ReserveInventory("ORD-1", "SKU-1", 2, 100));
                OrderAssert.True(reserve.IsApplied, name + ": reserve applies");
                var reserveSequence = simulation.PeekPendingSequence();
                var reserved = simulation.Reply(
                    reserveSequence,
                    new InventoryReserved("ORD-1", 100, true));
                OrderAssert.True(reserved.IsApplied, name + ": inventory reply applies");

                var charge = simulation.Deliver(
                    new ChargePayment("ORD-1", 25m, 200, simulation.Now));
                OrderAssert.True(charge.IsApplied, name + ": charge applies");
                var chargeSequence = simulation.PeekPendingSequence();

                var actualCaptured = simulation.Random!.Next(2) == 0;
                OrderAssert.Equal(captured, actualCaptured, name + ": seeded outcome");

                var reply = simulation.Reply(
                    chargeSequence,
                    new PaymentCharged(
                        "ORD-1",
                        200,
                        actualCaptured,
                        actualCaptured ? null : "declined"));
                OrderAssert.True(reply.IsApplied, name + ": payment reply applies");
            },
            Expectation: new OrderScenarioExpectation(
                name,
                new OrderModel(
                    "ORD-1",
                    new InventoryState("SKU-1", 2, 2, InventoryStatus.Reserved, 100),
                    new PaymentState(
                        25m,
                        captured ? PaymentStatus.Captured : PaymentStatus.Declined,
                        200,
                        StartedAt.AddMinutes(5)),
                    new CancellationState(CancellationStatus.None, null, 0)),
                StartedAt,
                captured
                    ?
                    [
                        new EmittedOrderCommand(
                            1,
                            new ReserveInventoryCommand("ORD-1", "SKU-1", 2, 100),
                            StartedAt),
                        new EmittedOrderCommand(
                            2,
                            new ChargePaymentCommand("ORD-1", 25m, 200),
                            StartedAt),
                    ]
                    :
                    [
                        new EmittedOrderCommand(
                            1,
                            new ReserveInventoryCommand("ORD-1", "SKU-1", 2, 100),
                            StartedAt),
                        new EmittedOrderCommand(
                            2,
                            new ChargePaymentCommand("ORD-1", 25m, 200),
                            StartedAt),
                        new EmittedOrderCommand(
                            3,
                            new NotifyCustomerCommand("ORD-1", "Payment declined: declined."),
                            StartedAt),
                    ],
                [
                    new ExpectedDelivery(null, StartedAt, TransitionStatus.Applied, null),
                    new ExpectedDelivery(1, StartedAt, TransitionStatus.Applied, null),
                    new ExpectedDelivery(null, StartedAt, TransitionStatus.Applied, null),
                    new ExpectedDelivery(2, StartedAt, TransitionStatus.Applied, null),
                ],
                []));
    }

    private static void ReserveAndCapture(IOrderSimulation simulation, string name)
    {
        var reserve = simulation.Deliver(
            new ReserveInventory("ORD-1", "SKU-1", 2, 100));
        OrderAssert.True(reserve.IsApplied, name + ": reserve applies");
        var reserveSequence = simulation.PeekPendingSequence();
        var reserved = simulation.Reply(
            reserveSequence,
            new InventoryReserved("ORD-1", 100, true));
        OrderAssert.True(reserved.IsApplied, name + ": inventory reply applies");

        var charge = simulation.Deliver(
            new ChargePayment("ORD-1", 25m, 200, simulation.Now));
        OrderAssert.True(charge.IsApplied, name + ": charge applies");
        var chargeSequence = simulation.PeekPendingSequence();
        var captured = simulation.Reply(
            chargeSequence,
            new PaymentCharged("ORD-1", 200, true, null));
        OrderAssert.True(captured.IsApplied, name + ": payment reply applies");
    }
}
