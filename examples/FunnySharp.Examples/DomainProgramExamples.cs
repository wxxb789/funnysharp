using FunnySharp;

internal enum ExampleInventoryStatus
{
    Idle,
    Reserving,
    Reserved,
    Unavailable,
}

internal sealed record ExampleInventory(
    string Sku,
    int Quantity,
    ExampleInventoryStatus Status,
    long Version);

internal enum ExamplePaymentStatus
{
    Idle,
    Charging,
    Captured,
    Declined,
    TimedOut,
}

internal sealed record ExamplePayment(
    decimal Amount,
    ExamplePaymentStatus Status,
    long Version,
    DateTimeOffset Deadline);

internal enum ExampleCancellationStatus
{
    None,
    Cancelling,
    RefundFailed,
}

internal sealed record ExampleCancellation(
    ExampleCancellationStatus Status,
    long Version);

internal sealed record ExampleOrder(
    string OrderId,
    ExampleInventory Inventory,
    ExamplePayment Payment,
    ExampleCancellation Cancellation);

internal sealed record ExampleShipment(ExampleInventory Inventory, string Carrier);

internal abstract record ExampleMessage;

internal sealed record ExampleReserve(
    string OrderId,
    string Sku,
    int Quantity,
    long Version) : ExampleMessage;

internal sealed record ExampleReserved(
    string OrderId,
    long Version,
    bool Accepted) : ExampleMessage;

internal sealed record ExampleCharge(
    string OrderId,
    decimal Amount,
    long Version,
    DateTimeOffset At) : ExampleMessage;

internal sealed record ExampleCharged(
    string OrderId,
    long Version,
    bool Captured,
    string? Failure) : ExampleMessage;

internal sealed record ExampleTimeout(string OrderId, DateTimeOffset At) : ExampleMessage;

internal sealed record ExampleCancel(string OrderId, long Version) : ExampleMessage;

internal sealed record ExampleRefundFailed(
    string OrderId,
    long Version,
    string Reason) : ExampleMessage;

internal sealed record ExampleUnrouted(string OrderId) : ExampleMessage;

internal abstract record ExampleCommand;

internal sealed record ExampleReserveCommand(
    string OrderId,
    string Sku,
    int Quantity,
    long Version) : ExampleCommand;

internal sealed record ExampleChargeCommand(
    string OrderId,
    decimal Amount,
    long Version) : ExampleCommand;

internal sealed record ExampleReleaseCommand(
    string OrderId,
    string Sku,
    int Quantity) : ExampleCommand;

internal sealed record ExampleRefundCommand(
    string OrderId,
    decimal Amount) : ExampleCommand;

internal sealed record ExampleNotifyCommand(
    string OrderId,
    string Message) : ExampleCommand;

internal abstract record ExampleError;

internal sealed record ExampleInvalid(string Reason) : ExampleError;

internal sealed record ExampleStale(
    string Message,
    long Received,
    long Expected) : ExampleError;

internal sealed record ExampleDuplicate(string Message, long Version) : ExampleError;

internal sealed record ExampleCancellationRejected(string Reason) : ExampleError;

internal sealed record ExampleExecutionUnavailable(string Reason) : ExampleError;

internal abstract record ExampleInventoryMessage;

internal sealed record ExampleInventoryRequested(
    int Quantity,
    long Version) : ExampleInventoryMessage;

internal sealed record ExampleInventoryAnswered(
    long Version,
    bool Accepted) : ExampleInventoryMessage;

internal abstract record ExampleInventoryCommand;

internal sealed record ExampleInventoryReserveCommand(
    string Sku,
    int Quantity,
    long Version) : ExampleInventoryCommand;

internal sealed record ExampleInventoryNotifyCommand(string Message) : ExampleInventoryCommand;

internal abstract record ExamplePaymentMessage;

internal sealed record ExamplePaymentRequested(
    decimal Amount,
    long Version,
    DateTimeOffset At) : ExamplePaymentMessage;

internal sealed record ExamplePaymentAnswered(
    long Version,
    bool Captured,
    string? Failure) : ExamplePaymentMessage;

internal sealed record ExamplePaymentTimedOut(DateTimeOffset At) : ExamplePaymentMessage;

internal abstract record ExamplePaymentCommand;

internal sealed record ExamplePaymentChargeCommand(
    decimal Amount,
    long Version) : ExamplePaymentCommand;

internal sealed record ExamplePaymentNotifyCommand(string Message) : ExamplePaymentCommand;

internal static class DomainProgramExamples
{
    private static readonly DateTimeOffset StartedAt =
        new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static readonly DomainProgram<ExampleInventory, ExampleInventoryMessage,
        ExampleInventoryCommand, ExampleError> InventoryProgram =
        DomainProgram.Create<ExampleInventory, ExampleInventoryMessage,
            ExampleInventoryCommand, ExampleError>(UpdateInventory);

    private static readonly DomainProgram<ExamplePayment, ExamplePaymentMessage,
        ExamplePaymentCommand, ExampleError> PaymentProgram =
        DomainProgram.Create<ExamplePayment, ExamplePaymentMessage,
            ExamplePaymentCommand, ExampleError>(UpdatePayment);

    internal static void Verify()
    {
        VerifySuccessfulCancellation();
        VerifyLateAndDuplicateResponses();
        VerifyTimeAndRandomness();
        VerifyChildReuse();
    }

    internal static async Task VerifyAsync()
    {
        var program = CreateProgram();
        var simulation = new ProgramSimulation<ExampleOrder, ExampleMessage, ExampleCommand,
            ExampleError>(program, InitialOrder(), StartedAt);
        using var cancellation = new CancellationTokenSource();
        var gateway = new ExampleGateway();

        RequireApplied(
            simulation.Deliver(new ExampleReserve("ORD-1", "SKU-1", 2, 100)),
            "reserve");
        RequireApplied(
            simulation.Deliver(new ExampleCharge("ORD-1", 25m, 200, simulation.Now)),
            "charge");

        while (simulation.PendingCommands.Count > 0)
        {
            var emitted = simulation.PendingCommands[0];
            var reply = await gateway.ExecuteAsync(emitted.Command, cancellation.Token);
            RequireApplied(simulation.Reply(emitted.Sequence, reply), "reply");
        }

        ExampleAssertions.Equal(2, gateway.Calls);
        ExampleAssertions.Equal(cancellation.Token, gateway.LastToken);
        ExampleAssertions.Equal(ExamplePaymentStatus.Captured, simulation.Model.Payment.Status);

        var failingSimulation = new ProgramSimulation<ExampleOrder, ExampleMessage,
            ExampleCommand, ExampleError>(program, InitialOrder(), StartedAt);
        RequireApplied(
            failingSimulation.Deliver(new ExampleCharge("ORD-1", 25m, 200, failingSimulation.Now)),
            "charge");
        var pending = failingSimulation.PendingCommands[0];

        try
        {
            _ = await new ThrowingGateway().ExecuteAsync(pending.Command, cancellation.Token);
            throw new InvalidOperationException("The gateway was expected to fail.");
        }
        catch (ExampleGatewayException exception)
        {
            var failure = failingSimulation.FailCommand(
                pending.Sequence,
                new ExampleExecutionUnavailable(exception.Message));
            ExampleAssertions.Equal(pending.Sequence, failure.Command.Sequence);
            ExampleAssertions.Equal(new ExampleExecutionUnavailable("gateway unavailable"), failure.Error);
        }

        ExampleAssertions.Equal(ExamplePaymentStatus.Charging, failingSimulation.Model.Payment.Status);
        ExampleAssertions.True(
            failingSimulation.CommandFailures.Count == 1,
            "The execution failure must be recorded separately from the domain state.");
    }

    private static void VerifySuccessfulCancellation()
    {
        var simulation = new ProgramSimulation<ExampleOrder, ExampleMessage, ExampleCommand,
            ExampleError>(CreateProgram(), InitialOrder(), StartedAt);
        var initial = simulation.Model;

        RequireApplied(
            simulation.Deliver(new ExampleReserve("ORD-1", "SKU-1", 2, 100)),
            "reserve");
        ExampleAssertions.True(
            ReferenceEquals(initial.Payment, simulation.Model.Payment),
            "The inventory update must not replace the payment substate.");
        ExampleAssertions.True(
            ReferenceEquals(initial.Cancellation, simulation.Model.Cancellation),
            "The inventory update must not replace the cancellation substate.");
        var reserve = simulation.PendingCommands[0];
        ExampleAssertions.True(
            reserve.Command is ExampleReserveCommand,
            "The inventory child must lift its reserve command.");

        simulation.AdvanceBy(TimeSpan.FromMinutes(1));
        RequireApplied(
            simulation.Reply(reserve.Sequence, new ExampleReserved("ORD-1", 100, true)),
            "inventory reply");

        RequireApplied(
            simulation.Deliver(new ExampleCharge("ORD-1", 25m, 200, simulation.Now)),
            "charge");
        var charge = simulation.PendingCommands[0];
        simulation.AdvanceBy(TimeSpan.FromMinutes(2));
        RequireApplied(
            simulation.Reply(charge.Sequence, new ExampleCharged("ORD-1", 200, true, null)),
            "payment reply");

        RequireApplied(simulation.Deliver(new ExampleCancel("ORD-1", 300)), "cancel");
        ExampleAssertions.SequenceEqual(
            new[] { "ExampleReleaseCommand", "ExampleRefundCommand", "ExampleNotifyCommand" },
            simulation.PendingCommands.Select(static command => command.Command.GetType().Name).ToArray());

        var refund = simulation.PendingCommands[1];
        var failure = simulation.FailCommand(
            refund.Sequence,
            new ExampleExecutionUnavailable("refund gateway"));
        ExampleAssertions.Equal(refund.Sequence, failure.Command.Sequence);
        ExampleAssertions.Equal(
            new ExampleExecutionUnavailable("refund gateway"),
            failure.Error);

        RequireApplied(
            simulation.Deliver(new ExampleRefundFailed("ORD-1", 300, "refund gateway")),
            "refund failure message");

        ExampleAssertions.Equal(ExampleInventoryStatus.Reserved, simulation.Model.Inventory.Status);
        ExampleAssertions.Equal(ExamplePaymentStatus.Captured, simulation.Model.Payment.Status);
        ExampleAssertions.Equal(
            ExampleCancellationStatus.RefundFailed,
            simulation.Model.Cancellation.Status);
        ExampleAssertions.Equal(1, simulation.CommandFailures.Count);

        var emittedCount = simulation.EmittedCommands.Count;
        var replayed = RequireApplied(simulation.Replay(), "replay");
        ExampleAssertions.Equal(simulation.Model, replayed.State);
        ExampleAssertions.Equal(6, replayed.Outputs.Count);
        ExampleAssertions.Equal(emittedCount, simulation.EmittedCommands.Count);
        ExampleAssertions.Equal(1, simulation.CommandFailures.Count);
    }

    private static void VerifyLateAndDuplicateResponses()
    {
        var simulation = new ProgramSimulation<ExampleOrder, ExampleMessage, ExampleCommand,
            ExampleError>(CreateProgram(), InitialOrder(), StartedAt);

        RequireApplied(
            simulation.Deliver(new ExampleCharge("ORD-1", 25m, 200, simulation.Now)),
            "first charge");
        var first = simulation.PendingCommands[0];

        simulation.AdvanceBy(TimeSpan.FromMinutes(1));
        RequireApplied(
            simulation.Deliver(new ExampleCharge("ORD-1", 30m, 201, simulation.Now)),
            "retry charge");

        var stale = RequireRejected(
            simulation.Reply(first.Sequence, new ExampleCharged("ORD-1", 200, true, null)),
            "old reply");
        ExampleAssertions.Equal(new ExampleStale("PaymentCharged", 200, 201), stale);

        var second = simulation.PendingCommands[0];
        RequireApplied(
            simulation.Reply(second.Sequence, new ExampleCharged("ORD-1", 201, true, null)),
            "current reply");

        var duplicate = RequireRejected(
            simulation.Reply(second.Sequence, new ExampleCharged("ORD-1", 201, true, null)),
            "duplicate reply");
        ExampleAssertions.Equal(new ExampleDuplicate("PaymentCharged", 201), duplicate);
        ExampleAssertions.Equal(ExamplePaymentStatus.Captured, simulation.Model.Payment.Status);
    }

    private static void VerifyTimeAndRandomness()
    {
        var timed = new ProgramSimulation<ExampleOrder, ExampleMessage, ExampleCommand,
            ExampleError>(CreateProgram(), InitialOrder(), StartedAt);
        RequireApplied(
            timed.Deliver(new ExampleCharge("ORD-1", 25m, 200, timed.Now)),
            "charge");
        timed.AdvanceBy(TimeSpan.FromMinutes(1));
        var early = RequireRejected(
            timed.Deliver(new ExampleTimeout("ORD-1", timed.Now)),
            "early timeout");
        ExampleAssertions.Equal(
            new ExampleInvalid("Payment deadline has not been reached."),
            early);

        timed.AdvanceBy(TimeSpan.FromMinutes(5));
        RequireApplied(timed.Deliver(new ExampleTimeout("ORD-1", timed.Now)), "late timeout");
        ExampleAssertions.Equal(ExamplePaymentStatus.TimedOut, timed.Model.Payment.Status);

        var random = new Random(2);
        var randomized = new ProgramSimulation<ExampleOrder, ExampleMessage, ExampleCommand,
            ExampleError>(CreateProgram(), InitialOrder(), StartedAt, random);
        ExampleAssertions.True(
            ReferenceEquals(random, randomized.Random),
            "The simulation must expose the caller-supplied random source.");
        RequireApplied(
            randomized.Deliver(new ExampleCharge("ORD-1", 25m, 200, randomized.Now)),
            "charge");
        var charge = randomized.PendingCommands[0];
        var captured = randomized.Random!.Next(2) == 0;
        RequireApplied(
            randomized.Reply(
                charge.Sequence,
                new ExampleCharged("ORD-1", 200, captured, captured ? null : "declined")),
            "random reply");
        ExampleAssertions.Equal(
            captured ? ExamplePaymentStatus.Captured : ExamplePaymentStatus.Declined,
            randomized.Model.Payment.Status);
    }

    private static void VerifyChildReuse()
    {
        var shipment = InventoryProgram.Lift<ExampleShipment, ExampleInventoryMessage,
            ExampleInventoryCommand>(
            static model => model.Inventory,
            static (model, inventory) => model with { Inventory = inventory },
            static message => Option.Some(message),
            static (_, command) => command);

        var result = RequireApplied(
            shipment.Update(
                new ExampleShipment(InitialOrder().Inventory, "carrier"),
                new ExampleInventoryRequested(2, 100)),
            "shipment reserve");

        ExampleAssertions.Equal(ExampleInventoryStatus.Reserving, result.State.Inventory.Status);
        ExampleAssertions.Equal("carrier", result.State.Carrier);
        ExampleAssertions.Equal(1, result.Outputs.Count);
        ExampleAssertions.True(
            result.Outputs[0] is ExampleInventoryReserveCommand,
            "The same child command must be lifted into the shipment parent.");
    }

    private static DomainProgram<ExampleOrder, ExampleMessage, ExampleCommand, ExampleError>
        CreateProgram()
    {
        var cancellation = DomainProgram.Create<ExampleOrder, ExampleMessage, ExampleCommand,
            ExampleError>(UpdateCancellation);

        return InventoryProgram
            .Lift<ExampleOrder, ExampleMessage, ExampleCommand>(
                static model => model.Inventory,
                static (model, inventory) => model with { Inventory = inventory },
                static message => message switch
                {
                    ExampleReserve reserve => Option.Some<ExampleInventoryMessage>(
                        new ExampleInventoryRequested(reserve.Quantity, reserve.Version)),
                    ExampleReserved reserved => Option.Some<ExampleInventoryMessage>(
                        new ExampleInventoryAnswered(reserved.Version, reserved.Accepted)),
                    _ => Option.None<ExampleInventoryMessage>(),
                },
                static (parent, command) => command switch
                {
                    ExampleInventoryReserveCommand reserve => new ExampleReserveCommand(
                        parent.OrderId,
                        reserve.Sku,
                        reserve.Quantity,
                        reserve.Version),
                    ExampleInventoryNotifyCommand notify => new ExampleNotifyCommand(
                        parent.OrderId,
                        notify.Message),
                    _ => throw new InvalidOperationException("Unexpected inventory command."),
                })
            .OrElse(PaymentProgram.Lift<ExampleOrder, ExampleMessage, ExampleCommand>(
                static model => model.Payment,
                static (model, payment) => model with { Payment = payment },
                static message => message switch
                {
                    ExampleCharge charge => Option.Some<ExamplePaymentMessage>(
                        new ExamplePaymentRequested(
                            charge.Amount,
                            charge.Version,
                            charge.At)),
                    ExampleCharged charged => Option.Some<ExamplePaymentMessage>(
                        new ExamplePaymentAnswered(
                            charged.Version,
                            charged.Captured,
                            charged.Failure)),
                    ExampleTimeout timeout => Option.Some<ExamplePaymentMessage>(
                        new ExamplePaymentTimedOut(timeout.At)),
                    _ => Option.None<ExamplePaymentMessage>(),
                },
                static (parent, command) => command switch
                {
                    ExamplePaymentChargeCommand charge => new ExampleChargeCommand(
                        parent.OrderId,
                        charge.Amount,
                        charge.Version),
                    ExamplePaymentNotifyCommand notify => new ExampleNotifyCommand(
                        parent.OrderId,
                        notify.Message),
                    _ => throw new InvalidOperationException("Unexpected payment command."),
                }))
            .OrElse(cancellation);
    }

    private static TransitionResult<ExampleInventory, ExampleInventoryCommand, ExampleError>
        UpdateInventory(ExampleInventory inventory, ExampleInventoryMessage message) =>
        message switch
        {
            ExampleInventoryRequested requested
                when inventory.Status == ExampleInventoryStatus.Idle =>
                InventoryApplied(StateChange<ExampleInventory, ExampleInventoryCommand>.To(
                    inventory with
                    {
                        Quantity = requested.Quantity,
                        Status = ExampleInventoryStatus.Reserving,
                        Version = requested.Version,
                    },
                    new ExampleInventoryReserveCommand(
                        inventory.Sku,
                        requested.Quantity,
                        requested.Version))),
            ExampleInventoryRequested => InventoryRejected(
                new ExampleInvalid("Inventory has already been requested.")),
            ExampleInventoryAnswered answered
                when answered.Version == inventory.Version &&
                    inventory.Status == ExampleInventoryStatus.Reserving =>
                answered.Accepted
                    ? InventoryApplied(StateChange<ExampleInventory, ExampleInventoryCommand>.To(
                        inventory with { Status = ExampleInventoryStatus.Reserved }))
                    : InventoryApplied(StateChange<ExampleInventory, ExampleInventoryCommand>.To(
                        inventory with { Status = ExampleInventoryStatus.Unavailable },
                        new ExampleInventoryNotifyCommand("Inventory is unavailable."))),
            ExampleInventoryAnswered answered when answered.Version == inventory.Version =>
                InventoryRejected(new ExampleDuplicate("InventoryReserved", answered.Version)),
            ExampleInventoryAnswered answered => InventoryRejected(
                new ExampleStale(
                    "InventoryReserved",
                    answered.Version,
                    inventory.Version)),
            _ => TransitionResult<ExampleInventory, ExampleInventoryCommand, ExampleError>
                .Undefined(),
        };

    private static TransitionResult<ExamplePayment, ExamplePaymentCommand, ExampleError>
        UpdatePayment(ExamplePayment payment, ExamplePaymentMessage message) =>
        message switch
        {
            ExamplePaymentRequested requested
                when payment.Status is ExamplePaymentStatus.Idle
                    or ExamplePaymentStatus.Declined
                    or ExamplePaymentStatus.TimedOut =>
                PaymentApplied(StateChange<ExamplePayment, ExamplePaymentCommand>.To(
                    payment with
                    {
                        Amount = requested.Amount,
                        Status = ExamplePaymentStatus.Charging,
                        Version = requested.Version,
                        Deadline = requested.At.AddMinutes(5),
                    },
                    new ExamplePaymentChargeCommand(requested.Amount, requested.Version))),
            ExamplePaymentRequested requested
                when payment.Status == ExamplePaymentStatus.Charging &&
                    requested.Version > payment.Version =>
                PaymentApplied(StateChange<ExamplePayment, ExamplePaymentCommand>.To(
                    payment with
                    {
                        Amount = requested.Amount,
                        Version = requested.Version,
                        Deadline = requested.At.AddMinutes(5),
                    },
                    new ExamplePaymentChargeCommand(requested.Amount, requested.Version))),
            ExamplePaymentRequested => PaymentRejected(
                new ExampleInvalid("Payment is already charging or captured.")),
            ExamplePaymentAnswered answered
                when answered.Version == payment.Version &&
                    payment.Status == ExamplePaymentStatus.Charging =>
                answered.Captured
                    ? PaymentApplied(StateChange<ExamplePayment, ExamplePaymentCommand>.To(
                        payment with { Status = ExamplePaymentStatus.Captured }))
                    : PaymentApplied(StateChange<ExamplePayment, ExamplePaymentCommand>.To(
                        payment with { Status = ExamplePaymentStatus.Declined },
                        new ExamplePaymentNotifyCommand(
                            "Payment declined: " + (answered.Failure ?? "declined") + "."))),
            ExamplePaymentAnswered answered when answered.Version == payment.Version =>
                PaymentRejected(new ExampleDuplicate("PaymentCharged", answered.Version)),
            ExamplePaymentAnswered answered => PaymentRejected(
                new ExampleStale(
                    "PaymentCharged",
                    answered.Version,
                    payment.Version)),
            ExamplePaymentTimedOut timedOut
                when payment.Status == ExamplePaymentStatus.Charging &&
                    timedOut.At >= payment.Deadline =>
                PaymentApplied(StateChange<ExamplePayment, ExamplePaymentCommand>.To(
                    payment with { Status = ExamplePaymentStatus.TimedOut },
                    new ExamplePaymentNotifyCommand("Payment timed out."))),
            ExamplePaymentTimedOut when payment.Status == ExamplePaymentStatus.Charging =>
                PaymentRejected(new ExampleInvalid("Payment deadline has not been reached.")),
            ExamplePaymentTimedOut => PaymentRejected(
                new ExampleInvalid("Payment is not awaiting a timeout.")),
            _ => TransitionResult<ExamplePayment, ExamplePaymentCommand, ExampleError>
                .Undefined(),
        };

    private static TransitionResult<ExampleOrder, ExampleCommand, ExampleError>
        UpdateCancellation(ExampleOrder order, ExampleMessage message)
    {
        var cancellation = order.Cancellation;
        return message switch
        {
            ExampleCancel cancel when cancellation.Status == ExampleCancellationStatus.None =>
                order.Inventory.Status == ExampleInventoryStatus.Reserved &&
                order.Payment.Status == ExamplePaymentStatus.Captured
                    ? OrderApplied(StateChange<ExampleOrder, ExampleCommand>.To(
                        order with
                        {
                            Cancellation = cancellation with
                            {
                                Status = ExampleCancellationStatus.Cancelling,
                                Version = cancel.Version,
                            },
                        },
                        new ExampleReleaseCommand(
                            order.OrderId,
                            order.Inventory.Sku,
                            order.Inventory.Quantity),
                        new ExampleRefundCommand(order.OrderId, order.Payment.Amount),
                        new ExampleNotifyCommand(order.OrderId, "Cancellation accepted.")))
                    : OrderRejected(new ExampleCancellationRejected(
                        "Cancellation requires reserved inventory and captured payment.")),
            ExampleCancel cancel when cancel.Version == cancellation.Version =>
                OrderRejected(new ExampleDuplicate("CancelOrder", cancel.Version)),
            ExampleCancel cancel => OrderRejected(
                new ExampleStale("CancelOrder", cancel.Version, cancellation.Version)),
            ExampleRefundFailed failed
                when failed.Version == cancellation.Version &&
                    cancellation.Status == ExampleCancellationStatus.Cancelling =>
                OrderApplied(StateChange<ExampleOrder, ExampleCommand>.To(
                    order with
                    {
                        Cancellation = cancellation with
                        {
                            Status = ExampleCancellationStatus.RefundFailed,
                        },
                    },
                    new ExampleNotifyCommand(
                        order.OrderId,
                        "Refund failed: " + failed.Reason + "."))),
            ExampleRefundFailed failed when failed.Version == cancellation.Version =>
                OrderRejected(new ExampleDuplicate("PaymentRefundFailed", failed.Version)),
            ExampleRefundFailed failed => OrderRejected(
                new ExampleStale(
                    "PaymentRefundFailed",
                    failed.Version,
                    cancellation.Version)),
            _ => TransitionResult<ExampleOrder, ExampleCommand, ExampleError>.Undefined(),
        };
    }

    private static ExampleOrder InitialOrder() =>
        new(
            "ORD-1",
            new ExampleInventory("SKU-1", 0, ExampleInventoryStatus.Idle, 0),
            new ExamplePayment(0m, ExamplePaymentStatus.Idle, 0, default),
            new ExampleCancellation(ExampleCancellationStatus.None, 0));

    private static StateChange<TModel, TCommand> RequireApplied<TModel, TCommand>(
        TransitionResult<TModel, TCommand, ExampleError> result,
        string description)
    {
        if (!result.TryGetChange(out var change))
        {
            throw new InvalidOperationException(description + " must apply: " + result);
        }

        return change;
    }

    private static ExampleError RequireRejected<TModel, TCommand>(
        TransitionResult<TModel, TCommand, ExampleError> result,
        string description)
    {
        if (!result.IsRejected || !result.TryGetError(out var error))
        {
            throw new InvalidOperationException(description + " must be rejected: " + result);
        }

        return error!;
    }

    private static TransitionResult<ExampleInventory, ExampleInventoryCommand, ExampleError>
        InventoryApplied(StateChange<ExampleInventory, ExampleInventoryCommand> change) =>
        TransitionResult<ExampleInventory, ExampleInventoryCommand, ExampleError>.Applied(change);

    private static TransitionResult<ExampleInventory, ExampleInventoryCommand, ExampleError>
        InventoryRejected(ExampleError error) =>
        TransitionResult<ExampleInventory, ExampleInventoryCommand, ExampleError>.Rejected(error);

    private static TransitionResult<ExamplePayment, ExamplePaymentCommand, ExampleError>
        PaymentApplied(StateChange<ExamplePayment, ExamplePaymentCommand> change) =>
        TransitionResult<ExamplePayment, ExamplePaymentCommand, ExampleError>.Applied(change);

    private static TransitionResult<ExamplePayment, ExamplePaymentCommand, ExampleError>
        PaymentRejected(ExampleError error) =>
        TransitionResult<ExamplePayment, ExamplePaymentCommand, ExampleError>.Rejected(error);

    private static TransitionResult<ExampleOrder, ExampleCommand, ExampleError>
        OrderApplied(StateChange<ExampleOrder, ExampleCommand> change) =>
        TransitionResult<ExampleOrder, ExampleCommand, ExampleError>.Applied(change);

    private static TransitionResult<ExampleOrder, ExampleCommand, ExampleError>
        OrderRejected(ExampleError error) =>
        TransitionResult<ExampleOrder, ExampleCommand, ExampleError>.Rejected(error);

    private sealed class ExampleGateway
    {
        public int Calls { get; private set; }

        public CancellationToken LastToken { get; private set; }

        public ValueTask<ExampleMessage> ExecuteAsync(
            ExampleCommand command,
            CancellationToken cancellationToken)
        {
            Calls++;
            LastToken = cancellationToken;
            return command switch
            {
                ExampleReserveCommand reserve => ValueTask.FromResult<ExampleMessage>(
                    new ExampleReserved(reserve.OrderId, reserve.Version, true)),
                ExampleChargeCommand charge => ValueTask.FromResult<ExampleMessage>(
                    new ExampleCharged(charge.OrderId, charge.Version, true, null)),
                _ => throw new InvalidOperationException("Unexpected command."),
            };
        }
    }

    private sealed class ThrowingGateway
    {
        public ValueTask<ExampleMessage> ExecuteAsync(
            ExampleCommand command,
            CancellationToken cancellationToken) =>
            throw new ExampleGatewayException("gateway unavailable");
    }

    private sealed class ExampleGatewayException(string message) : Exception(message);
}
