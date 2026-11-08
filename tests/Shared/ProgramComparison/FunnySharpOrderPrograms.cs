using FunnySharp;

namespace FunnySharp.TestSupport.ProgramComparison;

internal abstract record InventoryMessage;

internal sealed record InventoryRequested(int Quantity, long Version) : InventoryMessage;

internal sealed record InventoryAnswered(long Version, bool Accepted) : InventoryMessage;

internal abstract record InventoryCommand;

internal sealed record InventoryReserveCommand(
    string Sku,
    int Quantity,
    long Version) : InventoryCommand;

internal sealed record InventoryNotifyCommand(string Message) : InventoryCommand;

internal abstract record PaymentMessage;

internal sealed record PaymentRequested(
    decimal Amount,
    long Version,
    DateTimeOffset RequestedAt) : PaymentMessage;

internal sealed record PaymentAnswered(
    long Version,
    bool Captured,
    string? Failure) : PaymentMessage;

internal sealed record PaymentGatewayReported(long Version, string Reason) : PaymentMessage;

internal sealed record PaymentTimedOut(DateTimeOffset At) : PaymentMessage;

internal abstract record PaymentCommand;

internal sealed record PaymentChargeCommand(decimal Amount, long Version) : PaymentCommand;

internal sealed record PaymentNotifyCommand(string Message) : PaymentCommand;

internal static class FunnySharpOrderPrograms
{
    private static readonly DomainProgram<InventoryState, InventoryMessage, InventoryCommand, OrderError>
        InventoryProgram =
        DomainProgram.Create<InventoryState, InventoryMessage, InventoryCommand, OrderError>(
            UpdateInventory);

    private static readonly DomainProgram<PaymentState, PaymentMessage, PaymentCommand, OrderError>
        PaymentProgram =
        DomainProgram.Create<PaymentState, PaymentMessage, PaymentCommand, OrderError>(
            UpdatePayment);

    private static readonly DomainProgram<OrderModel, OrderMessage, OrderCommand, OrderError>
        CancellationProgram =
        DomainProgram.Create<OrderModel, OrderMessage, OrderCommand, OrderError>(
            UpdateCancellation);

    internal static DomainProgram<OrderModel, OrderMessage, OrderCommand, OrderError> Create() =>
        LiftInventory(InventoryProgram)
            .OrElse(LiftPayment(PaymentProgram))
            .OrElse(CancellationProgram);

    private static DomainProgram<OrderModel, OrderMessage, OrderCommand, OrderError> LiftInventory(
        DomainProgram<InventoryState, InventoryMessage, InventoryCommand, OrderError> child) =>
        child.Lift<OrderModel, OrderMessage, OrderCommand>(
            static model => model.Inventory,
            static (model, inventory) => model with { Inventory = inventory },
            static message => message switch
            {
                ReserveInventory reserve => Option.Some<InventoryMessage>(
                    new InventoryRequested(reserve.Quantity, reserve.Version)),
                InventoryReserved reserved => Option.Some<InventoryMessage>(
                    new InventoryAnswered(reserved.Version, reserved.Accepted)),
                _ => Option.None<InventoryMessage>(),
            },
            static (parent, command) => command switch
            {
                InventoryReserveCommand reserve => new ReserveInventoryCommand(
                    parent.OrderId,
                    reserve.Sku,
                    reserve.Quantity,
                    reserve.Version),
                InventoryNotifyCommand notify => new NotifyCustomerCommand(
                    parent.OrderId,
                    notify.Message),
                _ => throw new InvalidOperationException("Unexpected inventory command."),
            });

    private static DomainProgram<OrderModel, OrderMessage, OrderCommand, OrderError> LiftPayment(
        DomainProgram<PaymentState, PaymentMessage, PaymentCommand, OrderError> child) =>
        child.Lift<OrderModel, OrderMessage, OrderCommand>(
            static model => model.Payment,
            static (model, payment) => model with { Payment = payment },
            static message => message switch
            {
                ChargePayment charge => Option.Some<PaymentMessage>(
                    new PaymentRequested(charge.Amount, charge.Version, charge.RequestedAt)),
                PaymentCharged charged => Option.Some<PaymentMessage>(
                    new PaymentAnswered(charged.Version, charged.Captured, charged.Failure)),
                PaymentGatewayFailed failed => Option.Some<PaymentMessage>(
                    new PaymentGatewayReported(failed.Version, failed.Reason)),
                PaymentTimeout timeout => Option.Some<PaymentMessage>(
                    new PaymentTimedOut(timeout.At)),
                _ => Option.None<PaymentMessage>(),
            },
            static (parent, command) => command switch
            {
                PaymentChargeCommand charge => new ChargePaymentCommand(
                    parent.OrderId,
                    charge.Amount,
                    charge.Version),
                PaymentNotifyCommand notify => new NotifyCustomerCommand(
                    parent.OrderId,
                    notify.Message),
                _ => throw new InvalidOperationException("Unexpected payment command."),
            });

    private static TransitionResult<InventoryState, InventoryCommand, OrderError> UpdateInventory(
        InventoryState inventory,
        InventoryMessage message) =>
        message switch
        {
            InventoryRequested requested when inventory.Status == InventoryStatus.Idle =>
                AppliedInventory(StateChange<InventoryState, InventoryCommand>.To(
                    inventory with
                    {
                        RequestedQuantity = requested.Quantity,
                        Status = InventoryStatus.Reserving,
                        PendingVersion = requested.Version,
                    },
                    new InventoryReserveCommand(
                        inventory.Sku,
                        requested.Quantity,
                        requested.Version))),
            InventoryRequested => RejectedInventory(
                new InvalidTransition("Inventory has already been requested.")),
            InventoryAnswered answered
                when answered.Version == inventory.PendingVersion &&
                    inventory.Status == InventoryStatus.Reserving =>
                answered.Accepted
                    ? AppliedInventory(StateChange<InventoryState, InventoryCommand>.To(
                        inventory with
                        {
                            Status = InventoryStatus.Reserved,
                            ReservedQuantity = inventory.RequestedQuantity,
                        }))
                    : AppliedInventory(StateChange<InventoryState, InventoryCommand>.To(
                        inventory with { Status = InventoryStatus.Unavailable },
                        new InventoryNotifyCommand(
                            "Inventory is unavailable for " + inventory.Sku + "."))),
            InventoryAnswered answered
                when answered.Version == inventory.PendingVersion &&
                    inventory.Status is InventoryStatus.Reserved or InventoryStatus.Unavailable =>
                RejectedInventory(new DuplicateResponse("InventoryReserved", answered.Version)),
            InventoryAnswered answered => RejectedInventory(
                new StaleResponse(
                    "InventoryReserved",
                    answered.Version,
                    inventory.PendingVersion)),
            _ => TransitionResult<InventoryState, InventoryCommand, OrderError>.Undefined(),
        };

    private static TransitionResult<PaymentState, PaymentCommand, OrderError> UpdatePayment(
        PaymentState payment,
        PaymentMessage message) =>
        message switch
        {
            PaymentRequested requested when requested.Amount <= 0m => FailedPayment(
                new InvalidTransition("Payment amount must be positive.")),
            PaymentRequested requested
                when payment.Status is PaymentStatus.Idle
                    or PaymentStatus.Declined
                    or PaymentStatus.TimedOut
                    or PaymentStatus.Failed =>
                AppliedPayment(StateChange<PaymentState, PaymentCommand>.To(
                    payment with
                    {
                        Amount = requested.Amount,
                        Status = PaymentStatus.Charging,
                        PendingVersion = requested.Version,
                        Deadline = requested.RequestedAt.AddMinutes(5),
                    },
                    new PaymentChargeCommand(requested.Amount, requested.Version))),
            PaymentRequested requested
                when payment.Status == PaymentStatus.Charging &&
                    requested.Version > payment.PendingVersion =>
                AppliedPayment(StateChange<PaymentState, PaymentCommand>.To(
                    payment with
                    {
                        Amount = requested.Amount,
                        PendingVersion = requested.Version,
                        Deadline = requested.RequestedAt.AddMinutes(5),
                    },
                    new PaymentChargeCommand(requested.Amount, requested.Version))),
            PaymentRequested => RejectedPayment(
                new InvalidTransition("Payment is already charging or captured.")),
            PaymentAnswered answered
                when answered.Version == payment.PendingVersion &&
                    payment.Status == PaymentStatus.Charging =>
                answered.Captured
                    ? AppliedPayment(StateChange<PaymentState, PaymentCommand>.To(
                        payment with { Status = PaymentStatus.Captured }))
                    : AppliedPayment(StateChange<PaymentState, PaymentCommand>.To(
                        payment with { Status = PaymentStatus.Declined },
                        new PaymentNotifyCommand(
                            "Payment declined: " + (answered.Failure ?? "declined") + "."))),
            PaymentAnswered answered
                when answered.Version == payment.PendingVersion &&
                    payment.Status is PaymentStatus.Captured or PaymentStatus.Declined =>
                RejectedPayment(new DuplicateResponse("PaymentCharged", answered.Version)),
            PaymentAnswered answered when answered.Version == payment.PendingVersion =>
                RejectedPayment(new UnexpectedResponse("Payment is no longer charging.")),
            PaymentAnswered answered => RejectedPayment(
                new StaleResponse(
                    "PaymentCharged",
                    answered.Version,
                    payment.PendingVersion)),
            PaymentGatewayReported reported
                when reported.Version == payment.PendingVersion &&
                    payment.Status == PaymentStatus.Charging =>
                AppliedPayment(StateChange<PaymentState, PaymentCommand>.To(
                    payment with { Status = PaymentStatus.Failed },
                    new PaymentNotifyCommand(
                        "Payment gateway failed: " + reported.Reason + "."))),
            PaymentGatewayReported reported
                when reported.Version == payment.PendingVersion &&
                    payment.Status == PaymentStatus.Failed =>
                RejectedPayment(new DuplicateResponse("PaymentGatewayFailed", reported.Version)),
            PaymentGatewayReported reported when reported.Version == payment.PendingVersion =>
                RejectedPayment(
                    new UnexpectedResponse("Payment is not awaiting a gateway failure.")),
            PaymentGatewayReported reported => RejectedPayment(
                new StaleResponse(
                    "PaymentGatewayFailed",
                    reported.Version,
                    payment.PendingVersion)),
            PaymentTimedOut timedOut
                when payment.Status == PaymentStatus.Charging &&
                    timedOut.At >= payment.Deadline =>
                AppliedPayment(StateChange<PaymentState, PaymentCommand>.To(
                    payment with { Status = PaymentStatus.TimedOut },
                    new PaymentNotifyCommand("Payment timed out."))),
            PaymentTimedOut timedOut when payment.Status == PaymentStatus.Charging =>
                RejectedPayment(
                    new InvalidTransition("Payment deadline has not been reached.")),
            PaymentTimedOut => RejectedPayment(
                new InvalidTransition("Payment is not awaiting a timeout.")),
            _ => TransitionResult<PaymentState, PaymentCommand, OrderError>.Undefined(),
        };

    private static TransitionResult<OrderModel, OrderCommand, OrderError> UpdateCancellation(
        OrderModel model,
        OrderMessage message)
    {
        var cancellation = model.Cancellation;
        return message switch
        {
            CancelOrder cancel when cancellation.Status == CancellationStatus.None =>
                model.Inventory.Status == InventoryStatus.Reserved &&
                model.Payment.Status == PaymentStatus.Captured
                    ? AppliedOrder(StateChange<OrderModel, OrderCommand>.To(
                        model with
                        {
                            Cancellation = cancellation with
                            {
                                Status = CancellationStatus.Cancelling,
                                Reason = cancel.Reason,
                                Version = cancel.Version,
                            },
                        },
                        new ReleaseInventoryCommand(
                            model.OrderId,
                            model.Inventory.Sku,
                            model.Inventory.ReservedQuantity),
                        new RefundPaymentCommand(model.OrderId, model.Payment.Amount),
                        new NotifyCustomerCommand(model.OrderId, "Cancellation accepted.")))
                    : RejectedOrder(new CancellationRejected(
                        "Cancellation requires reserved inventory and captured payment.")),
            CancelOrder cancel when cancel.Version == cancellation.Version =>
                RejectedOrder(new DuplicateResponse("CancelOrder", cancel.Version)),
            CancelOrder cancel => RejectedOrder(
                new StaleResponse(
                    "CancelOrder",
                    cancel.Version,
                    cancellation.Version)),
            PaymentRefundFailed refund
                when refund.Version == cancellation.Version &&
                    cancellation.Status == CancellationStatus.Cancelling =>
                AppliedOrder(StateChange<OrderModel, OrderCommand>.To(
                    model with
                    {
                        Cancellation = cancellation with
                        {
                            Status = CancellationStatus.RefundFailed,
                        },
                    },
                    new NotifyCustomerCommand(
                        model.OrderId,
                        "Refund failed: " + refund.Reason + "."))),
            PaymentRefundFailed refund
                when refund.Version == cancellation.Version &&
                    cancellation.Status == CancellationStatus.RefundFailed =>
                RejectedOrder(new DuplicateResponse("PaymentRefundFailed", refund.Version)),
            PaymentRefundFailed refund when refund.Version == cancellation.Version =>
                RejectedOrder(
                    new UnexpectedResponse("Cancellation is not awaiting a refund.")),
            PaymentRefundFailed refund => RejectedOrder(
                new StaleResponse(
                    "PaymentRefundFailed",
                    refund.Version,
                    cancellation.Version)),
            _ => TransitionResult<OrderModel, OrderCommand, OrderError>.Undefined(),
        };
    }

    private static TransitionResult<InventoryState, InventoryCommand, OrderError> AppliedInventory(
        StateChange<InventoryState, InventoryCommand> change) =>
        TransitionResult<InventoryState, InventoryCommand, OrderError>.Applied(change);

    private static TransitionResult<InventoryState, InventoryCommand, OrderError> RejectedInventory(
        OrderError error) =>
        TransitionResult<InventoryState, InventoryCommand, OrderError>.Rejected(error);

    private static TransitionResult<PaymentState, PaymentCommand, OrderError> AppliedPayment(
        StateChange<PaymentState, PaymentCommand> change) =>
        TransitionResult<PaymentState, PaymentCommand, OrderError>.Applied(change);

    private static TransitionResult<PaymentState, PaymentCommand, OrderError> RejectedPayment(
        OrderError error) =>
        TransitionResult<PaymentState, PaymentCommand, OrderError>.Rejected(error);

    private static TransitionResult<PaymentState, PaymentCommand, OrderError> FailedPayment(
        OrderError error) =>
        TransitionResult<PaymentState, PaymentCommand, OrderError>.Failed(error);

    private static TransitionResult<OrderModel, OrderCommand, OrderError> AppliedOrder(
        StateChange<OrderModel, OrderCommand> change) =>
        TransitionResult<OrderModel, OrderCommand, OrderError>.Applied(change);

    private static TransitionResult<OrderModel, OrderCommand, OrderError> RejectedOrder(
        OrderError error) =>
        TransitionResult<OrderModel, OrderCommand, OrderError>.Rejected(error);
}
