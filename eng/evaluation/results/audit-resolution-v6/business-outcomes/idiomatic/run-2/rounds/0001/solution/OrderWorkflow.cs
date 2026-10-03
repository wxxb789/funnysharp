using System;
using System.Collections.Generic;

public static class OrderWorkflow
{
    public static OrderOutcome PlaceOrder(
        OrderRequest request,
        ProductCatalog catalog,
        PaymentGateway gateway,
        OrderRepository repository,
        string cardToken)
    {
        // Independent validation checks accumulate in the prescribed order.
        var validationErrors = new List<string>();
        int atIndex = request.CustomerEmail.IndexOf('@');
        if (atIndex < 0 || request.CustomerEmail.IndexOf('.', atIndex + 1) < 0)
        {
            validationErrors.Add("invalid-email");
        }

        if (request.Lines.Count == 0)
        {
            validationErrors.Add("empty-order");
        }

        foreach (var line in request.Lines)
        {
            if (line.Quantity is < 1 or > 99)
            {
                validationErrors.Add($"invalid-quantity:{line.Sku}");
            }
        }

        if (request.PromoCode is not (null or "" or "SAVE10" or "FREESHIP"))
        {
            validationErrors.Add("unknown-promo");
        }

        if (validationErrors.Count > 0)
        {
            return Failed(validationErrors.AsReadOnly());
        }

        // Look up every line once, accumulating all missing SKUs in line order.
        var pricingErrors = new List<string>();
        decimal goodsSubtotal = 0m;
        foreach (var line in request.Lines)
        {
            if (catalog.TryGetPrice(line.Sku, out decimal price))
            {
                goodsSubtotal += price * line.Quantity;
            }
            else
            {
                pricingErrors.Add($"unknown-sku:{line.Sku}");
            }
        }

        if (pricingErrors.Count > 0)
        {
            return Failed(pricingErrors.AsReadOnly());
        }

        decimal shipping = request.PromoCode == "FREESHIP" ? 0m : 5.00m;
        decimal discount = request.PromoCode == "SAVE10" ? goodsSubtotal * 0.10m : 0m;
        decimal total = decimal.Round(goodsSubtotal - discount + shipping, 2);

        // Dependent operations stop at the first failure; persistence requires payment.
        if (!gateway.Charge(cardToken, total))
        {
            return Failed(["payment-declined"]);
        }

        if (!repository.Save(request.CustomerEmail, total, out int orderId))
        {
            return Failed(["save-failed"]);
        }

        return new OrderOutcome(true, orderId, total, Array.Empty<string>());
    }

    private static OrderOutcome Failed(IReadOnlyList<string> errors) =>
        new(false, 0, 0m, errors);
}
