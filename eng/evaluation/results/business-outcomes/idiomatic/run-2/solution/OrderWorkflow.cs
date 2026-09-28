// Order placement for the shop: validate the request, price the lines, charge the card, then
// persist the order. Each step runs only when the previous one succeeded, and every failure
// names the step that failed, so the outcome reads as a run through the pipeline.

/// <summary>Places a shop order: validation, pricing, payment, then persistence.</summary>
public static class OrderWorkflow
{
    private const decimal ShippingFee = 5.00m;

    /// <summary>Runs the order-placement workflow and reports which step, if any, failed.</summary>
    public static OrderOutcome PlaceOrder(
        OrderRequest request,
        ProductCatalog catalog,
        PaymentGateway gateway,
        OrderRepository repository,
        string cardToken)
    {
        // Step 1 - validation. Every check runs and every failure is reported, in order.
        List<string> validationErrors = ValidateRequest(request);
        if (validationErrors.Count > 0)
        {
            return Failed(validationErrors);
        }

        // Step 2 - pricing. Every line needs a catalog price before a total can exist.
        decimal goodsSubtotal = 0m;
        List<string> unknownSkus = [];
        foreach (var line in request.Lines)
        {
            if (catalog.TryGetPrice(line.Sku, out decimal price))
            {
                goodsSubtotal += price * line.Quantity;
            }
            else
            {
                unknownSkus.Add($"unknown-sku:{line.Sku}");
            }
        }

        if (unknownSkus.Count > 0)
        {
            return Failed(unknownSkus);
        }

        decimal total = Math.Round(ApplyPromo(request.PromoCode, goodsSubtotal), 2);

        // Step 3 - payment. A declined card stops the workflow before anything is saved.
        if (!gateway.Charge(cardToken, total))
        {
            // The total is still reported: it is the amount the gateway declined.
            return Failed(["payment-declined"], total);
        }

        // Step 4 - persistence.
        if (!repository.Save(request.CustomerEmail, total, out int orderId))
        {
            return Failed(["save-failed"], total);
        }

        return new OrderOutcome(Placed: true, OrderId: orderId, Total: total, Errors: []);
    }

    /// <summary>Collects every validation failure for the request, in reporting order.</summary>
    private static List<string> ValidateRequest(OrderRequest request)
    {
        List<string> errors = [];

        if (!HasEmailAddressShape(request.CustomerEmail))
        {
            errors.Add("invalid-email");
        }

        if (request.Lines.Count == 0)
        {
            errors.Add("empty-order");
        }

        errors.AddRange(
            request.Lines
                .Where(line => line.Quantity is < 1 or > 99)
                .Select(line => $"invalid-quantity:{line.Sku}"));

        if (request.PromoCode is not (null or "" or "SAVE10" or "FREESHIP"))
        {
            errors.Add("unknown-promo");
        }

        return errors;
    }

    /// <summary>An email-shaped address carries an '@' with a '.' somewhere after it.</summary>
    private static bool HasEmailAddressShape(string email)
    {
        int at = email.LastIndexOf('@');
        return at >= 0 && email.IndexOf('.', at) > at;
    }

    /// <summary>Applies the promo to the goods subtotal: SAVE10 takes 10% off the goods only,
    /// FREESHIP drops the shipping fee, anything else pays goods plus shipping.</summary>
    private static decimal ApplyPromo(string? promoCode, decimal goodsSubtotal) => promoCode switch
    {
        "SAVE10" => goodsSubtotal * 0.90m + ShippingFee,
        "FREESHIP" => goodsSubtotal,
        _ => goodsSubtotal + ShippingFee,
    };

    /// <summary>An order that stopped at the step reporting these errors.</summary>
    private static OrderOutcome Failed(IReadOnlyList<string> errors, decimal total = 0m) =>
        new(Placed: false, OrderId: 0, Total: total, Errors: errors);
}
