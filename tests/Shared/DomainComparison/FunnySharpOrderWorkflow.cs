using System.Text.Json;
using FunnySharp;

namespace FunnySharp.TestSupport.Domains;

internal static class FunnySharpOrderWorkflow
{
    internal static readonly Decoder<DraftOrder> DraftDecoder = BuildDecoder();

    internal static WorkflowOutcome Run(JsonElement input)
    {
        var decoded = DraftDecoder.Decode(input);
        if (!decoded.TryGetValue(out var draft))
        {
            decoded.TryGetErrors(out var errors);
            return Rejected(new RequestRejected(errors!));
        }

        var submitted = draft!.Submit();
        if (!submitted.TryGetValue(out var placed))
        {
            submitted.TryGetError(out var error);
            return Rejected(error!);
        }

        var shipped = placed!.Ship(Domains.TrackingCode(placed.Id.Value));
        return Shipped(shipped);
    }

    internal static WorkflowOutcome RunCancel(JsonElement input)
    {
        var decoded = DraftDecoder.Decode(input);
        if (!decoded.TryGetValue(out var draft))
        {
            decoded.TryGetErrors(out var errors);
            return Rejected(new RequestRejected(errors!));
        }

        return Cancelled(draft!.Cancel("customer-request"));
    }

    internal static WorkflowOutcome Rejected(OrderWorkflowError error) => error switch
    {
        RequestRejected request => new WorkflowOutcome(
            "rejected",
            null,
            0,
            0m,
            request.Errors
                .Select(static item => new WorkflowFailureView(
                    "request",
                    item.Kind.ToString(),
                    item.Path,
                    item.Detail ?? string.Empty))
                .ToArray(),
            null,
            null,
            null,
            null),
        PricingRejected pricing => ModuleRejected(pricing.OrderId, pricing),
        InventoryRejected inventory => ModuleRejected(inventory.OrderId, inventory),
        PaymentRejected payment => ModuleRejected(payment.OrderId, payment),
        _ => throw new ArgumentOutOfRangeException(nameof(error)),
    };

    private static WorkflowOutcome ModuleRejected(
        DomainValue<string, OrderIdTag> orderId,
        OrderWorkflowError error) =>
        WorkflowOutcome.Rejected(orderId.Value, error.ToView(error.Detail));

    private static WorkflowOutcome Shipped(ShippedOrder order) =>
        new(
            "shipped",
            order.Id.Value,
            order.Quantity.Value,
            order.UnitPrice,
            [],
            order.ReservationCode,
            order.AuthorizationCode,
            order.Tracking.Value,
            null);

    private static WorkflowOutcome Cancelled(CancelledOrder order) =>
        new(
            "cancelled",
            order.Id.Value,
            0,
            0m,
            [],
            null,
            null,
            null,
            order.Reason);

    private static Decoder<DraftOrder> BuildDecoder()
    {
        var lineDecoder = Decoder.Object(
            Decoder.Field("sku", Domains.SkuDecoder),
            Decoder.Field("quantity", Domains.QuantityDecoder),
            static (sku, quantity) => new DraftLine(sku, quantity));
        var paymentDecoder = Decoder.Field("token", Decoder.NonEmptyString)
            .Map(static token => new PaymentDetails(token));

        return Decoder.Object(
            Decoder.Field("orderId", Domains.OrderIdDecoder),
            Decoder.Field("customerId", Domains.CustomerIdDecoder),
            Decoder.Field("line", lineDecoder),
            Decoder.Field("payment", paymentDecoder),
            static (orderId, customerId, line, payment) =>
                new DraftOrder(orderId, customerId, line.Sku, line.Quantity, payment.Token));
    }

}
