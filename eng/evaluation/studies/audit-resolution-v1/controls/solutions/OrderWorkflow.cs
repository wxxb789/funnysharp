public static class OrderWorkflow
{
    public static OrderOutcome PlaceOrder(OrderRequest request, ProductCatalog catalog,
        PaymentGateway gateway, OrderRepository repository, string cardToken)
    {
#if EARLY_SIDE_EFFECTS
        // Deliberate control defect: touch pricing before independent validation.
        foreach (var line in request.Lines) catalog.TryGetPrice(line.Sku, out _);
#endif
        var errors = new List<string>();
        var at = request.CustomerEmail.IndexOf('@');
        if (at < 0 || request.CustomerEmail.IndexOf('.', at + 1) < 0) errors.Add("invalid-email");
        if (request.Lines.Count == 0) errors.Add("empty-order");
        foreach (var line in request.Lines)
            if (line.Quantity is < 1 or > 99) errors.Add("invalid-quantity:" + line.Sku);
        if (request.PromoCode is not (null or "" or "SAVE10" or "FREESHIP")) errors.Add("unknown-promo");
        if (errors.Count != 0) return new(false, 0, 0, errors);

        decimal subtotal = 0;
        foreach (var line in request.Lines)
        {
            if (catalog.TryGetPrice(line.Sku, out var price)) subtotal += price * line.Quantity;
            else errors.Add("unknown-sku:" + line.Sku);
        }
        if (errors.Count != 0) return new(false, 0, 0, errors);
        var total = Math.Round(subtotal * (request.PromoCode == "SAVE10" ? 0.9m : 1m)
            + (request.PromoCode == "FREESHIP" ? 0m : 5m), 2);
        if (!gateway.Charge(cardToken, total)) return new(false, 0, total, ["payment-declined"]);
        if (!repository.Save(request.CustomerEmail, total, out var id)) return new(false, 0, total, ["save-failed"]);
        return new(true, id, total, []);
    }
}
