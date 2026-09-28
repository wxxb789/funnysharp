// Style-neutral contract shared by both evaluation variants: the data types, the deterministic
// domain fakes, and the neutral outcome record the solution must produce. No FunnySharp types
// appear here on purpose: both styles end at the same seam.
public sealed record OrderRequest(string CustomerEmail, IReadOnlyList<OrderLine> Lines, string? PromoCode);

public sealed record OrderLine(string Sku, int Quantity);

public sealed record OrderOutcome(bool Placed, int OrderId, decimal Total, IReadOnlyList<string> Errors);

public sealed class ProductCatalog
{
    private readonly IReadOnlyDictionary<string, decimal> prices;

    public ProductCatalog(IReadOnlyDictionary<string, decimal> prices) => this.prices = prices;

    public bool TryGetPrice(string sku, out decimal price) => prices.TryGetValue(sku, out price);
}

public sealed class PaymentGateway
{
    public required string DeclineCard { get; init; }

    public int Charges { get; private set; }

    public bool Charge(string cardToken, decimal amount)
    {
        if (cardToken == DeclineCard)
        {
            return false;
        }

        Charges++;
        return true;
    }
}

public sealed class OrderRepository
{
    public required string FailOnCustomer { get; init; }

    public List<(int OrderId, string CustomerEmail, decimal Total)> Saved { get; } = [];

    public bool Save(string customerEmail, decimal total, out int orderId)
    {
        if (customerEmail == FailOnCustomer)
        {
            orderId = 0;
            return false;
        }

        orderId = 1001 + Saved.Count;
        Saved.Add((orderId, customerEmail, total));
        return true;
    }
}
