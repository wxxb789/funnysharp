using System.Text.Json;
using FunnySharp;

namespace FunnySharp.TestSupport.Domains;

internal sealed record OrdinaryDecodeError(string Path, string Kind, string Detail);

internal sealed record OrdinaryDraftOrder(
    string OrderId,
    string CustomerId,
    string Sku,
    int Quantity,
    string PaymentToken);

internal enum OrdinaryOrderState
{
    Draft,
    Placed,
    Shipped,
    Cancelled,
}

internal abstract record OrdinaryWorkflowError : IWorkflowFailure
{
    public abstract string Source { get; }

    public abstract string Code { get; }

    internal abstract string Detail { get; }
}

internal sealed record OrdinaryRequestRejected(IReadOnlyList<OrdinaryDecodeError> Errors)
    : OrdinaryWorkflowError
{
    public override string Source => "request";

    public override string Code => "invalid-request";

    internal override string Detail =>
        string.Join("; ", Errors.Select(static error => $"{error.Path}:{error.Kind}:{error.Detail}"));
}

internal sealed record OrdinaryPricingRejected(string OrderId, PricingFailure Failure)
    : OrdinaryWorkflowError
{
    public override string Source => "pricing";

    public override string Code => Failure.Code;

    internal override string Detail => OrderModules.Format(Failure.Sku, Failure.Quantity);
}

internal sealed record OrdinaryInventoryRejected(string OrderId, InventoryFailure Failure)
    : OrdinaryWorkflowError
{
    public override string Source => "inventory";

    public override string Code => Failure.Code;

    internal override string Detail => OrderModules.Format(Failure.Sku, Failure.Quantity);
}

internal sealed record OrdinaryPaymentRejected(string OrderId, PaymentFailure Failure)
    : OrdinaryWorkflowError
{
    public override string Source => "payment";

    public override string Code => Failure.Code;

    internal override string Detail => Failure.Code;
}

internal sealed record OrdinaryStateRejected : OrdinaryWorkflowError
{
    internal OrdinaryStateRejected(string code) => Code = code;

    public override string Source => "state";

    public override string Code { get; }

    internal override string Detail => Code;
}

internal sealed record OrdinaryOrder(
    string OrderId,
    string CustomerId,
    string Sku,
    int Quantity,
    OrdinaryOrderState State,
    decimal UnitPrice,
    string? ReservationCode,
    string? AuthorizationCode,
    string? TrackingCode,
    string? CancellationReason)
{
    internal Result<OrdinaryOrder, OrdinaryWorkflowError> Ship(string tracking)
    {
        if (State != OrdinaryOrderState.Placed)
        {
            return Result<OrdinaryOrder, OrdinaryWorkflowError>.Failure(
                new OrdinaryStateRejected("invalid-state"));
        }

        if (!tracking.StartsWith("TRK-", StringComparison.Ordinal))
        {
            return Result<OrdinaryOrder, OrdinaryWorkflowError>.Failure(
                new OrdinaryStateRejected("invalid-tracking"));
        }

        return Result<OrdinaryOrder, OrdinaryWorkflowError>.Success(
            this with { State = OrdinaryOrderState.Shipped, TrackingCode = tracking });
    }

    internal Result<OrdinaryOrder, OrdinaryWorkflowError> Cancel(string reason)
    {
        if (State is OrdinaryOrderState.Shipped or OrdinaryOrderState.Cancelled)
        {
            return Result<OrdinaryOrder, OrdinaryWorkflowError>.Failure(
                new OrdinaryStateRejected("invalid-state"));
        }

        return Result<OrdinaryOrder, OrdinaryWorkflowError>.Success(
            this with { State = OrdinaryOrderState.Cancelled, CancellationReason = reason });
    }
}

internal static class OrdinaryOrderWorkflow
{
    internal static WorkflowOutcome Run(JsonElement input)
    {
        var decoded = OrdinaryOrderDecoder.Decode(input);
        if (!decoded.TryGetValue(out var draft))
        {
            decoded.TryGetErrors(out var errors);
            return Rejected(new OrdinaryRequestRejected(errors!));
        }

        var submitted = Submit(draft!);
        if (!submitted.TryGetValue(out var placed))
        {
            submitted.TryGetError(out var error);
            return Rejected(error!);
        }

        var shipped = placed!.Ship("TRK-" + placed.OrderId);
        if (!shipped.TryGetValue(out var final))
        {
            shipped.TryGetError(out var error);
            return Rejected(error!);
        }

        return new WorkflowOutcome(
            "shipped",
            final!.OrderId,
            final.Quantity,
            final.UnitPrice,
            [],
            final.ReservationCode,
            final.AuthorizationCode,
            final.TrackingCode,
            null);
    }

    internal static WorkflowOutcome RunCancel(JsonElement input)
    {
        var decoded = OrdinaryOrderDecoder.Decode(input);
        if (!decoded.TryGetValue(out var draft))
        {
            decoded.TryGetErrors(out var errors);
            return Rejected(new OrdinaryRequestRejected(errors!));
        }

        var order = new OrdinaryOrder(
            draft!.OrderId,
            draft.CustomerId,
            draft.Sku,
            draft.Quantity,
            OrdinaryOrderState.Draft,
            0m,
            null,
            null,
            null,
            null);
        var cancelled = order.Cancel("customer-request");
        if (!cancelled.TryGetValue(out var final))
        {
            cancelled.TryGetError(out var error);
            return Rejected(error!);
        }

        return new WorkflowOutcome(
            "cancelled",
            final!.OrderId,
            0,
            0m,
            [],
            null,
            null,
            null,
            final.CancellationReason);
    }

    internal static WorkflowOutcome Rejected(OrdinaryWorkflowError error) => error switch
    {
        OrdinaryRequestRejected request => new WorkflowOutcome(
            "rejected",
            null,
            0,
            0m,
            request.Errors
                .Select(static item => new WorkflowFailureView(
                    "request",
                    item.Kind,
                    item.Path,
                    item.Detail))
                .ToArray(),
            null,
            null,
            null,
            null),
        OrdinaryPricingRejected pricing => ModuleRejected(pricing.OrderId, pricing),
        OrdinaryInventoryRejected inventory => ModuleRejected(inventory.OrderId, inventory),
        OrdinaryPaymentRejected payment => ModuleRejected(payment.OrderId, payment),
        OrdinaryStateRejected state => ModuleRejected(null, state),
        _ => throw new ArgumentOutOfRangeException(nameof(error)),
    };

    private static WorkflowOutcome ModuleRejected(
        string? orderId,
        OrdinaryWorkflowError error) =>
        WorkflowOutcome.Rejected(orderId, error.ToView(error.Detail));

    private static Result<OrdinaryOrder, OrdinaryWorkflowError> Submit(OrdinaryDraftOrder draft)
    {
        var priced = OrderModules.Price(draft.Sku, draft.Quantity)
            .MapError(failure => (OrdinaryWorkflowError)new OrdinaryPricingRejected(
                draft.OrderId,
                failure));
        var reserved = OrderModules.Reserve(draft.Sku, draft.Quantity)
            .MapError(failure => (OrdinaryWorkflowError)new OrdinaryInventoryRejected(
                draft.OrderId,
                failure));

        return priced
            .Zip(reserved)
            .Bind(values => OrderModules.Authorize(draft.PaymentToken, values.First * draft.Quantity)
                .MapError(failure => (OrdinaryWorkflowError)new OrdinaryPaymentRejected(
                    draft.OrderId,
                    failure))
                .Map(authorization => new OrdinaryOrder(
                    draft.OrderId,
                    draft.CustomerId,
                    draft.Sku,
                    draft.Quantity,
                    OrdinaryOrderState.Placed,
                    values.First,
                    values.Second,
                    authorization,
                    null,
                    null)));
    }
}

internal static class OrdinaryOrderDecoder
{
    internal static Validation<OrdinaryDraftOrder, OrdinaryDecodeError> Decode(JsonElement input)
    {
        if (input.ValueKind != JsonValueKind.Object)
        {
            return Validation<OrdinaryDraftOrder, OrdinaryDecodeError>.Invalid(
                new OrdinaryDecodeError(
                    string.Empty,
                    input.ValueKind == JsonValueKind.Null ? "Null" : "WrongKind",
                    input.ValueKind == JsonValueKind.Null
                        ? string.Empty
                        : "found " + input.ValueKind));
        }

        var errors = new List<OrdinaryDecodeError>();
        var orderId = ReadIdentifier(
            input,
            "orderId",
            "ORD-",
            "order identifier must start with ORD-",
            errors);
        var customerId = ReadIdentifier(
            input,
            "customerId",
            "CUS-",
            "customer identifier must start with CUS-",
            errors);

        var line = ReadObject(input, "line", errors);
        string? sku = null;
        int? quantity = null;
        if (line is { } lineObject)
        {
            sku = ReadIdentifier(lineObject, "line.sku", "SKU-", "sku must start with SKU-", errors);
            quantity = ReadQuantity(lineObject, "line.quantity", errors);
        }

        var payment = ReadObject(input, "payment", errors);
        string? paymentToken = null;
        if (payment is { } paymentObject)
        {
            paymentToken = ReadText(paymentObject, "payment.token", errors);
        }

        if (errors.Count > 0)
        {
            return Validation<OrdinaryDraftOrder, OrdinaryDecodeError>.InvalidMany(errors);
        }

        return Validation<OrdinaryDraftOrder, OrdinaryDecodeError>.Valid(
            new OrdinaryDraftOrder(orderId!, customerId!, sku!, quantity!.Value, paymentToken!));
    }

    private static JsonElement? ReadObject(
        JsonElement input,
        string name,
        List<OrdinaryDecodeError> errors)
    {
        if (!TryGetFirstProperty(input, name, out var value))
        {
            errors.Add(new OrdinaryDecodeError(name, "Missing", string.Empty));
            return null;
        }

        if (value.ValueKind == JsonValueKind.Null)
        {
            errors.Add(new OrdinaryDecodeError(name, "Null", string.Empty));
            return null;
        }

        if (value.ValueKind != JsonValueKind.Object)
        {
            errors.Add(new OrdinaryDecodeError(name, "WrongKind", "found " + value.ValueKind));
            return null;
        }

        return value;
    }

    private static string? ReadIdentifier(
        JsonElement input,
        string path,
        string prefix,
        string detail,
        List<OrdinaryDecodeError> errors)
    {
        var value = ReadText(input, path, errors);
        if (value is not null && !value.StartsWith(prefix, StringComparison.Ordinal))
        {
            errors.Add(new OrdinaryDecodeError(path, "InvalidValue", detail));
            return null;
        }

        return value;
    }

    private static string? ReadText(JsonElement input, string path, List<OrdinaryDecodeError> errors)
    {
        var name = path[(path.LastIndexOf('.') + 1)..];
        if (!TryGetFirstProperty(input, name, out var value))
        {
            errors.Add(new OrdinaryDecodeError(path, "Missing", string.Empty));
            return null;
        }

        if (value.ValueKind == JsonValueKind.Null)
        {
            errors.Add(new OrdinaryDecodeError(path, "Null", string.Empty));
            return null;
        }

        if (value.ValueKind != JsonValueKind.String)
        {
            errors.Add(new OrdinaryDecodeError(path, "WrongKind", "found " + value.ValueKind));
            return null;
        }

        var text = value.GetString()!;
        if (text.Length == 0)
        {
            errors.Add(new OrdinaryDecodeError(path, "Empty", string.Empty));
            return null;
        }

        return text;
    }

    private static int? ReadQuantity(
        JsonElement input,
        string path,
        List<OrdinaryDecodeError> errors)
    {
        var name = path[(path.LastIndexOf('.') + 1)..];
        if (!TryGetFirstProperty(input, name, out var value))
        {
            errors.Add(new OrdinaryDecodeError(path, "Missing", string.Empty));
            return null;
        }

        if (value.ValueKind == JsonValueKind.Null)
        {
            errors.Add(new OrdinaryDecodeError(path, "Null", string.Empty));
            return null;
        }

        if (value.ValueKind != JsonValueKind.Number)
        {
            errors.Add(new OrdinaryDecodeError(path, "WrongKind", "found " + value.ValueKind));
            return null;
        }

        if (!value.TryGetInt32(out var quantity))
        {
            errors.Add(new OrdinaryDecodeError(
                path,
                "Malformed",
                "the value cannot be parsed as a 32-bit integer"));
            return null;
        }

        if (quantity is < 1 or > 100)
        {
            errors.Add(new OrdinaryDecodeError(
                path,
                "InvalidValue",
                "value must be between 1 and 100"));
            return null;
        }

        return quantity;
    }

    private static bool TryGetFirstProperty(JsonElement input, string name, out JsonElement value)
    {
        foreach (var property in input.EnumerateObject())
        {
            if (property.NameEquals(name))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }
}
