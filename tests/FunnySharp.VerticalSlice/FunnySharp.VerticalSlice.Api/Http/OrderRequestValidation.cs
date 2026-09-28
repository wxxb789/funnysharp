using FunnySharp.VerticalSlice.Application;
using FunnySharp.VerticalSlice.Domain;

namespace FunnySharp.VerticalSlice.Http;

/// <summary>
/// Turns wire shapes into validated commands. Every field is checked and every error is collected, so
/// a caller sees all of its mistakes in one response instead of the first exception.
/// </summary>
public static class OrderRequestValidation
{
    private const int MinimumReasonLength = 5;

    private const int MaximumReasonLength = 200;

    public static Validation<PlaceOrderCommand, InputError> ValidatePlacement(
        PlaceOrderRequest request,
        VerticalSliceOptions options)
    {
        var errors = new List<InputError>();
        var customerId = Collect(CustomerId.Create(request.CustomerId), errors);
        var lines = CollectLines(request.Lines, options.MaximumOrderLines, errors);
        if (errors.Count > 0 || !customerId.TryGetValue(out var customer) || !lines.TryGetValue(out var orderLines))
        {
            return Validation<PlaceOrderCommand, InputError>.InvalidMany(errors);
        }

        return Validation<PlaceOrderCommand, InputError>.Valid(new PlaceOrderCommand(customer, orderLines));
    }

    public static Validation<PayOrderCommand, InputError> ValidatePayment(OrderId orderId, PayOrderRequest request)
    {
        var errors = new List<InputError>();
        if (request.PaymentMethod is not { } method || !PaymentMethods.All.Contains(method, StringComparer.Ordinal))
        {
            errors.Add(new InputError(
                "paymentMethod",
                "unsupported",
                $"A payment method must be one of: {string.Join(", ", PaymentMethods.All)}."));
        }

        return errors.Count == 0
            ? Validation<PayOrderCommand, InputError>.Valid(new PayOrderCommand(orderId, request.PaymentMethod!))
            : Validation<PayOrderCommand, InputError>.InvalidMany(errors);
    }

    public static Validation<ShipOrderCommand, InputError> ValidateShipment(OrderId orderId, ShipOrderRequest request)
    {
        var trackingCode = TrackingCode.Create(request.TrackingCode);
        return trackingCode.Match(
            code => Validation<ShipOrderCommand, InputError>.Valid(new ShipOrderCommand(orderId, code)),
            error => Validation<ShipOrderCommand, InputError>.Invalid(error));
    }

    public static Validation<CancelOrderCommand, InputError> ValidateCancellation(
        OrderId orderId,
        CancelOrderRequest request)
    {
        if (request.Reason is not { } reason
            || reason.Length < MinimumReasonLength
            || reason.Length > MaximumReasonLength)
        {
            return Validation<CancelOrderCommand, InputError>.Invalid(
                new InputError(
                    "reason",
                    "length",
                    $"A cancellation reason must be {MinimumReasonLength} to {MaximumReasonLength} characters."));
        }

        return Validation<CancelOrderCommand, InputError>.Valid(new CancelOrderCommand(orderId, reason));
    }

    public static Validation<QuoteQuery, InputError> ValidateQuote(string? sku, int? quantity)
    {
        var errors = new List<InputError>();
        var refinedSku = Collect(Sku.Create(sku), errors);
        var refinedQuantity = Collect(Quantity.Create(quantity ?? 1), errors);
        if (errors.Count > 0 || !refinedSku.TryGetValue(out var validSku) || !refinedQuantity.TryGetValue(out var validQuantity))
        {
            return Validation<QuoteQuery, InputError>.InvalidMany(errors);
        }

        return Validation<QuoteQuery, InputError>.Valid(new QuoteQuery(validSku, validQuantity));
    }

    private static Option<NonEmpty<PlaceOrderLine>> CollectLines(
        PlaceOrderLineRequest[]? requested,
        int maximumLines,
        List<InputError> errors)
    {
        if (requested is not { Length: > 0 })
        {
            errors.Add(new InputError("lines", "required", "At least one order line is required."));
            return Option.None<NonEmpty<PlaceOrderLine>>();
        }

        if (requested.Length > maximumLines)
        {
            errors.Add(new InputError("lines", "maximum", $"An order can contain at most {maximumLines} lines."));
            return Option.None<NonEmpty<PlaceOrderLine>>();
        }

        var lines = new List<PlaceOrderLine>(requested.Length);
        for (var index = 0; index < requested.Length; index++)
        {
            var field = $"lines[{index}]";
            if (requested[index] is not { } line)
            {
                errors.Add(new InputError(field, "required", "An order line is required."));
                continue;
            }

            var sku = Collect(Sku.Create(line.Sku, $"{field}.sku"), errors);
            var quantity = Collect(Quantity.Create(line.Quantity, $"{field}.quantity"), errors);
            if (sku.TryGetValue(out var validSku) && quantity.TryGetValue(out var validQuantity))
            {
                lines.Add(new PlaceOrderLine(validSku, validQuantity));
            }
        }

        return lines.ToNonEmptyOrNone();
    }

    private static Option<T> Collect<T>(Result<T, InputError> result, List<InputError> errors) =>
        result.Match(static value => Option.Some(value!), error =>
        {
            errors.Add(error);
            return Option.None<T>();
        });
}
