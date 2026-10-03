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
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(gateway);
        ArgumentNullException.ThrowIfNull(repository);

        // Validation accumulates every independent failure before any pricing occurs.
        var errors = new List<string>();
        var email = request.CustomerEmail;
        var atIndex = email?.IndexOf('@') ?? -1;
        if (atIndex < 0 || email!.IndexOf('.', atIndex + 1) < 0)
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

        // Missing prices accumulate in line order; no payment can run on partial pricing.
        decimal goodsSubtotal = 0m;
        foreach (var line in request.Lines)
        {
            if (catalog.TryGetPrice(line.Sku, out var price))
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
            return new OrderOutcome(false, 0, 0m, errors.AsReadOnly());
        }

        decimal shipping = request.PromoCode == "FREESHIP" ? 0m : 5m;
        decimal discount = request.PromoCode == "SAVE10" ? goodsSubtotal * 0.10m : 0m;
        decimal total = decimal.Round(goodsSubtotal - discount + shipping, 2);

        // Payment and persistence are dependent steps and stop at the first failure.
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
