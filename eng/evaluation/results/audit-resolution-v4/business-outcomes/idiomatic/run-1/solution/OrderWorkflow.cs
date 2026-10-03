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
        // Independent validation checks accumulate errors in the required order.
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
            return new OrderOutcome(false, 0, 0m, errors.AsReadOnly());
        }

        // Price every line so all missing SKUs are reported before payment.
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
            return new OrderOutcome(false, 0, 0m, errors.AsReadOnly());
        }

        decimal total = request.PromoCode switch
        {
            "SAVE10" => goodsSubtotal * 0.90m + 5.00m,
            "FREESHIP" => goodsSubtotal,
            _ => goodsSubtotal + 5.00m,
        };
        total = decimal.Round(total, 2);

        // Dependent steps fail fast: persistence only follows a successful charge.
        if (!gateway.Charge(cardToken, total))
        {
            return new OrderOutcome(false, 0, total, new[] { "payment-declined" });
        }

        if (!repository.Save(request.CustomerEmail, total, out int orderId))
        {
            return new OrderOutcome(false, 0, total, new[] { "save-failed" });
        }

        return new OrderOutcome(true, orderId, total, Array.Empty<string>());
    }
}
