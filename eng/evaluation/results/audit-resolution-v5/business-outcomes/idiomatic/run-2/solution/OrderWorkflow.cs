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
        // Independent validation checks accumulate errors in business-rule order.
        var errors = new List<string>();
        int atIndex = request.CustomerEmail.IndexOf('@');
        if (atIndex < 0 || request.CustomerEmail.IndexOf('.', atIndex + 1) < 0)
        {
            errors.Add("invalid-email");
        }

        if (request.Lines.Count == 0)
        {
            errors.Add("empty-order");
        }

        foreach (var line in request.Lines)
        {
            if (line.Quantity is < 1 or > 99)
            {
                errors.Add($"invalid-quantity:{line.Sku}");
            }
        }

        if (request.PromoCode is not (null or "" or "SAVE10" or "FREESHIP"))
        {
            errors.Add("unknown-promo");
        }

        if (errors.Count > 0)
        {
            return Failure(errors.AsReadOnly());
        }

        // Look up every line once, retaining all missing SKUs in line order.
        decimal goodsSubtotal = 0m;
        foreach (var line in request.Lines)
        {
            if (catalog.TryGetPrice(line.Sku, out decimal price))
            {
                goodsSubtotal += price * line.Quantity;
            }
            else
            {
                errors.Add($"unknown-sku:{line.Sku}");
            }
        }

        if (errors.Count > 0)
        {
            return Failure(errors.AsReadOnly());
        }

        decimal shipping = request.PromoCode == "FREESHIP" ? 0m : 5.00m;
        decimal discount = request.PromoCode == "SAVE10" ? goodsSubtotal * 0.10m : 0m;
        decimal total = decimal.Round(goodsSubtotal - discount + shipping, 2);

        // Dependent steps fail fast: persistence never runs after a decline.
        if (!gateway.Charge(cardToken, total))
        {
            return Failure(["payment-declined"]);
        }

        if (!repository.Save(request.CustomerEmail, total, out int orderId))
        {
            return Failure(["save-failed"]);
        }

        return new OrderOutcome(true, orderId, total, Array.Empty<string>());
    }

    private static OrderOutcome Failure(IReadOnlyList<string> errors) =>
        new(false, 0, 0m, errors);
}
