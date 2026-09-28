// Order-placement workflow for the shop: validate the request, price it, charge the card,
// then persist the order. Each step reports its own failures and stops the pipeline, so a
// maintainer can see at a glance which step failed and why. Plain BCL types throughout, plus
// the Try/out absence idiom the domain fakes already speak.

public static class OrderWorkflow
{
    private const decimal ShippingFee = 5.00m;
    private const decimal Save10Rate = 0.10m;
    private const int MaxLineQuantity = 99;

    public static OrderOutcome PlaceOrder(
        OrderRequest request,
        ProductCatalog catalog,
        PaymentGateway gateway,
        OrderRepository repository,
        string cardToken)
    {
        // Step 1 - validation. The checks are independent, so every failure is collected
        // (in the fixed order inside Validate) before the workflow stops; nothing is priced,
        // charged, or saved.
        var validationErrors = Validate(request);
        if (validationErrors.Count > 0)
        {
            return Rejected(validationErrors);
        }

        // Step 2 - pricing. Every line must have a catalog price; all missing skus are
        // reported together (one per line, in line order) before the workflow stops.
        if (!TryPrice(request, catalog, out var pricingErrors, out var total))
        {
            return Rejected(pricingErrors);
        }

        // Step 3 - payment. A declined card stops the workflow before anything is saved.
        if (!gateway.Charge(cardToken, total))
        {
            return Rejected(["payment-declined"]);
        }

        // Step 4 - persistence. The charge has already gone through; only the save can fail.
        if (!repository.Save(request.CustomerEmail, total, out var orderId))
        {
            return Rejected(["save-failed"]);
        }

        return new OrderOutcome(Placed: true, OrderId: orderId, Total: total, Errors: []);
    }

    // Independent validation checks; all failures are reported, in this order.
    private static List<string> Validate(OrderRequest request)
    {
        var errors = new List<string>();

        if (!HasEmailShape(request.CustomerEmail))
        {
            errors.Add("invalid-email");
        }

        if (request.Lines.Count == 0)
        {
            errors.Add("empty-order");
        }

        // One error per offending line, in line order.
        errors.AddRange(
            request.Lines
                .Where(line => line.Quantity is < 1 or > MaxLineQuantity)
                .Select(line => $"invalid-quantity:{line.Sku}"));

        if (!IsKnownPromo(request.PromoCode))
        {
            errors.Add("unknown-promo");
        }

        return errors;
    }

    // Prices the order: goods subtotal plus shipping, with the promo applied and the final
    // total rounded to two decimals. Returns false, with one error per unpriceable line in
    // line order, when any sku is missing from the catalog.
    private static bool TryPrice(
        OrderRequest request,
        ProductCatalog catalog,
        out IReadOnlyList<string> errors,
        out decimal total)
    {
        var unknownSkus = new List<string>();
        var goodsSubtotal = 0m;

        foreach (var line in request.Lines)
        {
            if (catalog.TryGetPrice(line.Sku, out var price))
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
            errors = unknownSkus;
            total = 0m;
            return false;
        }

        errors = [];
        total = Math.Round(ApplyPromo(request.PromoCode, goodsSubtotal), 2);
        return true;
    }

    // Loose by design: the shop only requires an '@' with a '.' somewhere after it.
    private static bool HasEmailShape(string email)
    {
        var atSign = email.IndexOf('@');
        return atSign >= 0 && email.IndexOf('.', atSign + 1) >= 0;
    }

    private static bool IsKnownPromo(string? promoCode) =>
        promoCode is null or "" or "SAVE10" or "FREESHIP";

    private static decimal ApplyPromo(string? promoCode, decimal goodsSubtotal) =>
        promoCode switch
        {
            // 10% off the goods subtotal; shipping is unaffected.
            "SAVE10" => goodsSubtotal * (1m - Save10Rate) + ShippingFee,

            // Shipping is dropped entirely.
            "FREESHIP" => goodsSubtotal,

            _ => goodsSubtotal + ShippingFee,
        };

    // Every failure path shares this shape: nothing was placed, nothing was saved.
    private static OrderOutcome Rejected(IReadOnlyList<string> errors) =>
        new(Placed: false, OrderId: 0, Total: 0m, Errors: errors);
}
