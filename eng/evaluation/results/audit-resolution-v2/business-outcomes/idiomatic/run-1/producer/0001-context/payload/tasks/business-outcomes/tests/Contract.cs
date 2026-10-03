public sealed record OrderRequest(string CustomerEmail, IReadOnlyList<OrderLine> Lines, string? PromoCode);
public sealed record OrderLine(string Sku, int Quantity);
public sealed record OrderOutcome(bool Placed, int OrderId, decimal Total, IReadOnlyList<string> Errors);

public sealed class ProductCatalog(IReadOnlyDictionary<string, decimal> prices)
{
    public List<string> Lookups { get; } = [];
    public bool TryGetPrice(string sku, out decimal price)
    {
        this.Lookups.Add(sku);
        return prices.TryGetValue(sku, out price);
    }
}

public sealed class PaymentGateway
{
    public required string DeclineCard { get; init; }
    public int Charges { get; private set; }
    public List<(string Card, decimal Amount)> Attempts { get; } = [];
    public bool Charge(string cardToken, decimal amount)
    {
        this.Attempts.Add((cardToken, amount));
        if (cardToken == this.DeclineCard) return false;
        this.Charges++;
        return true;
    }
}

public sealed class OrderRepository
{
    public required string FailOnCustomer { get; init; }
    public List<(string CustomerEmail, decimal Total)> Attempts { get; } = [];
    public List<(int OrderId, string CustomerEmail, decimal Total)> Saved { get; } = [];
    public bool Save(string customerEmail, decimal total, out int orderId)
    {
        this.Attempts.Add((customerEmail, total));
        if (customerEmail == this.FailOnCustomer) { orderId = 0; return false; }
        orderId = 1001 + this.Saved.Count;
        this.Saved.Add((orderId, customerEmail, total));
        return true;
    }
}
