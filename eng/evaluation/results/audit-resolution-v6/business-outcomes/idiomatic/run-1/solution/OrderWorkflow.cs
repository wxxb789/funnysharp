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
        // Independent validation checks accumulate in business-rule order.
        var errors = new List<string>();
        var atIndex = request.CustomerEmail.IndexOf('@');
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
            return new OrderOutcome(false, 0, 0m, errors.AsReadOnly());
        }

        // Inspect every line so all missing prices are reported before payment.
        decimal subtotal = 0m;
        foreach (var line in request.Lines)
        {
            if (catalog.TryGetPrice(line.Sku, out var price))
            {
                subtotal += price * line.Quantity;
            }
            else
            {
                errors.Add($"unknown-sku:{line.Sku}");
            }
        }

        if (errors.Count > 0)
        {
            return new OrderOutcome(false, 0, 0m, errors.AsReadOnly());
        }

        decimal shipping = request.PromoCode == "FREESHIP" ? 0m : 5.00m;
        decimal discount = request.PromoCode == "SAVE10" ? subtotal * 0.10m : 0m;
        decimal total = decimal.Round(subtotal - discount + shipping, 2);

        // Dependent steps stop at the first failure; persistence requires payment.
        if (!gateway.Charge(cardToken, total))
        {
            return new OrderOutcome(false, 0, total, ["payment-declined"]);
        }

        if (!repository.Save(request.CustomerEmail, total, out var orderId))
        {
            return new OrderOutcome(false, 0, total, ["save-failed"]);
        }

        return new OrderOutcome(true, orderId, total, Array.Empty<string>());
    }
}
