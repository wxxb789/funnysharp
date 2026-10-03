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
        // Accumulate every independent validation error before performing any work.
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
            if (line.Quantity < 1 || line.Quantity > 99)
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
            return Failed(errors);
        }

        // Look up every line so missing SKUs accumulate in source order.
        decimal subtotal = 0m;
        foreach (var line in request.Lines)
        {
            if (catalog.TryGetPrice(line.Sku, out decimal price))
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
            return Failed(errors);
        }

        decimal shipping = request.PromoCode == "FREESHIP" ? 0m : 5.00m;
        decimal discount = request.PromoCode == "SAVE10" ? subtotal * 0.10m : 0m;
        decimal total = decimal.Round(subtotal - discount + shipping, 2);

        // Dependent steps fail fast: persistence must never run after a decline.
        if (!gateway.Charge(cardToken, total))
        {
            return new OrderOutcome(false, 0, 0m, new[] { "payment-declined" });
        }

        if (!repository.Save(request.CustomerEmail, total, out int orderId))
        {
            return new OrderOutcome(false, 0, 0m, new[] { "save-failed" });
        }

        return new OrderOutcome(true, orderId, total, Array.Empty<string>());
    }

    private static OrderOutcome Failed(List<string> errors) =>
        new OrderOutcome(false, 0, 0m, errors.AsReadOnly());
}
