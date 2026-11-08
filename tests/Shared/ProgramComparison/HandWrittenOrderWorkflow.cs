using FunnySharp;

namespace FunnySharp.TestSupport.ProgramComparison;

internal static class HandWrittenOrderWorkflow
{
    internal static readonly StateMachine<OrderModel, OrderMessage, OrderCommand, OrderError> Update =
        UpdateCore;

    private static TransitionResult<OrderModel, OrderCommand, OrderError> UpdateCore(
        OrderModel model,
        OrderMessage message) =>
        message switch
        {
            ReserveInventory or InventoryReserved => UpdateInventory(model, message),
            ChargePayment or PaymentCharged or PaymentGatewayFailed or PaymentTimeout =>
                UpdatePayment(model, message),
            CancelOrder or PaymentRefundFailed => UpdateCancellation(model, message),
            _ => TransitionResult<OrderModel, OrderCommand, OrderError>.Undefined(),
        };

    private static TransitionResult<OrderModel, OrderCommand, OrderError> UpdateInventory(
        OrderModel model,
        OrderMessage message)
    {
        var inventory = model.Inventory;
        return message switch
        {
            ReserveInventory reserve when inventory.Status == InventoryStatus.Idle =>
                Applied(StateChange<OrderModel, OrderCommand>.To(
                    model with
                    {
                        Inventory = inventory with
                        {
                            RequestedQuantity = reserve.Quantity,
                            Status = InventoryStatus.Reserving,
                            PendingVersion = reserve.Version,
                        },
                    },
                    new ReserveInventoryCommand(
                        model.OrderId,
                        inventory.Sku,
                        reserve.Quantity,
                        reserve.Version))),
            ReserveInventory => Rejected(
                new InvalidTransition("Inventory has already been requested.")),
            InventoryReserved reserved
                when reserved.Version == inventory.PendingVersion &&
                    inventory.Status == InventoryStatus.Reserving =>
                reserved.Accepted
                    ? Applied(StateChange<OrderModel, OrderCommand>.To(
                        model with
                        {
                            Inventory = inventory with
                            {
                                Status = InventoryStatus.Reserved,
                                ReservedQuantity = inventory.RequestedQuantity,
                            },
                        }))
                    : Applied(StateChange<OrderModel, OrderCommand>.To(
                        model with
                        {
                            Inventory = inventory with { Status = InventoryStatus.Unavailable },
                        },
                        new NotifyCustomerCommand(
                            model.OrderId,
                            "Inventory is unavailable for " + inventory.Sku + "."))),
            InventoryReserved reserved
                when reserved.Version == inventory.PendingVersion &&
                    inventory.Status is InventoryStatus.Reserved or InventoryStatus.Unavailable =>
                Rejected(new DuplicateResponse("InventoryReserved", reserved.Version)),
            InventoryReserved reserved => Rejected(
                new StaleResponse(
                    "InventoryReserved",
                    reserved.Version,
                    inventory.PendingVersion)),
            _ => TransitionResult<OrderModel, OrderCommand, OrderError>.Undefined(),
        };
    }

    private static TransitionResult<OrderModel, OrderCommand, OrderError> UpdatePayment(
        OrderModel model,
        OrderMessage message)
    {
        var payment = model.Payment;
        return message switch
        {
            ChargePayment charge when charge.Amount <= 0m => Failed(
                new InvalidTransition("Payment amount must be positive.")),
            ChargePayment charge
                when payment.Status is PaymentStatus.Idle
                    or PaymentStatus.Declined
                    or PaymentStatus.TimedOut
                    or PaymentStatus.Failed =>
                Applied(StateChange<OrderModel, OrderCommand>.To(
                    model with
                    {
                        Payment = payment with
                        {
                            Amount = charge.Amount,
                            Status = PaymentStatus.Charging,
                            PendingVersion = charge.Version,
                            Deadline = charge.RequestedAt.AddMinutes(5),
                        },
                    },
                    new ChargePaymentCommand(model.OrderId, charge.Amount, charge.Version))),
            ChargePayment charge
                when payment.Status == PaymentStatus.Charging &&
                    charge.Version > payment.PendingVersion =>
                Applied(StateChange<OrderModel, OrderCommand>.To(
                    model with
                    {
                        Payment = payment with
                        {
                            Amount = charge.Amount,
                            PendingVersion = charge.Version,
                            Deadline = charge.RequestedAt.AddMinutes(5),
                        },
                    },
                    new ChargePaymentCommand(model.OrderId, charge.Amount, charge.Version))),
            ChargePayment => Rejected(
                new InvalidTransition("Payment is already charging or captured.")),
            PaymentCharged charged
                when charged.Version == payment.PendingVersion &&
                    payment.Status == PaymentStatus.Charging =>
                charged.Captured
                    ? Applied(StateChange<OrderModel, OrderCommand>.To(
                        model with
                        {
                            Payment = payment with { Status = PaymentStatus.Captured },
                        }))
                    : Applied(StateChange<OrderModel, OrderCommand>.To(
                        model with
                        {
                            Payment = payment with { Status = PaymentStatus.Declined },
                        },
                        new NotifyCustomerCommand(
                            model.OrderId,
                            "Payment declined: " + (charged.Failure ?? "declined") + "."))),
            PaymentCharged charged
                when charged.Version == payment.PendingVersion &&
                    payment.Status is PaymentStatus.Captured or PaymentStatus.Declined =>
                Rejected(new DuplicateResponse("PaymentCharged", charged.Version)),
            PaymentCharged charged when charged.Version == payment.PendingVersion =>
                Rejected(new UnexpectedResponse("Payment is no longer charging.")),
            PaymentCharged charged => Rejected(
                new StaleResponse(
                    "PaymentCharged",
                    charged.Version,
                    payment.PendingVersion)),
            PaymentGatewayFailed failed
                when failed.Version == payment.PendingVersion &&
                    payment.Status == PaymentStatus.Charging =>
                Applied(StateChange<OrderModel, OrderCommand>.To(
                    model with
                    {
                        Payment = payment with { Status = PaymentStatus.Failed },
                    },
                    new NotifyCustomerCommand(
                        model.OrderId,
                        "Payment gateway failed: " + failed.Reason + "."))),
            PaymentGatewayFailed failed
                when failed.Version == payment.PendingVersion &&
                    payment.Status == PaymentStatus.Failed =>
                Rejected(new DuplicateResponse("PaymentGatewayFailed", failed.Version)),
            PaymentGatewayFailed failed when failed.Version == payment.PendingVersion =>
                Rejected(new UnexpectedResponse("Payment is not awaiting a gateway failure.")),
            PaymentGatewayFailed failed => Rejected(
                new StaleResponse(
                    "PaymentGatewayFailed",
                    failed.Version,
                    payment.PendingVersion)),
            PaymentTimeout timeout
                when payment.Status == PaymentStatus.Charging &&
                    timeout.At >= payment.Deadline =>
                Applied(StateChange<OrderModel, OrderCommand>.To(
                    model with
                    {
                        Payment = payment with { Status = PaymentStatus.TimedOut },
                    },
                    new NotifyCustomerCommand(model.OrderId, "Payment timed out."))),
            PaymentTimeout timeout when payment.Status == PaymentStatus.Charging =>
                Rejected(new InvalidTransition("Payment deadline has not been reached.")),
            PaymentTimeout => Rejected(
                new InvalidTransition("Payment is not awaiting a timeout.")),
            _ => TransitionResult<OrderModel, OrderCommand, OrderError>.Undefined(),
        };
    }

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
                    ? Applied(StateChange<OrderModel, OrderCommand>.To(
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
                    : Rejected(new CancellationRejected(
                        "Cancellation requires reserved inventory and captured payment.")),
            CancelOrder cancel when cancel.Version == cancellation.Version =>
                Rejected(new DuplicateResponse("CancelOrder", cancel.Version)),
            CancelOrder cancel => Rejected(
                new StaleResponse(
                    "CancelOrder",
                    cancel.Version,
                    cancellation.Version)),
            PaymentRefundFailed refund
                when refund.Version == cancellation.Version &&
                    cancellation.Status == CancellationStatus.Cancelling =>
                Applied(StateChange<OrderModel, OrderCommand>.To(
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
                Rejected(new DuplicateResponse("PaymentRefundFailed", refund.Version)),
            PaymentRefundFailed refund when refund.Version == cancellation.Version =>
                Rejected(new UnexpectedResponse("Cancellation is not awaiting a refund.")),
            PaymentRefundFailed refund => Rejected(
                new StaleResponse(
                    "PaymentRefundFailed",
                    refund.Version,
                    cancellation.Version)),
            _ => TransitionResult<OrderModel, OrderCommand, OrderError>.Undefined(),
        };
    }

    private static TransitionResult<OrderModel, OrderCommand, OrderError> Applied(
        StateChange<OrderModel, OrderCommand> change) =>
        TransitionResult<OrderModel, OrderCommand, OrderError>.Applied(change);

    private static TransitionResult<OrderModel, OrderCommand, OrderError> Rejected(
        OrderError error) =>
        TransitionResult<OrderModel, OrderCommand, OrderError>.Rejected(error);

    private static TransitionResult<OrderModel, OrderCommand, OrderError> Failed(
        OrderError error) =>
        TransitionResult<OrderModel, OrderCommand, OrderError>.Failed(error);
}
