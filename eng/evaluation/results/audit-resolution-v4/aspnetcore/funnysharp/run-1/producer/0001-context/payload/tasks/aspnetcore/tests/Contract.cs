// Style-neutral contract shared by both evaluation variants: the wire DTOs, the domain records,
// and the deterministic in-memory shop data the API endpoints read and write. No FunnySharp types
// appear here on purpose: both styles end at the same seam.

public sealed record OrderRequest(string CustomerEmail, IReadOnlyList<OrderLine> Lines, string? PromoCode);

public sealed record OrderLine(string Sku, int Quantity);

public sealed record OrderCreated(int OrderId);

public sealed record PaymentRequest(string CardToken);

public sealed record PaymentAccepted(bool Paid);

public sealed record Product(string Id, string Name, decimal Price);

public sealed record StoredOrder(int Id, string CustomerEmail, decimal Total, bool Paid);

public static class ShopData
{
    private static readonly List<StoredOrder> orders = [];
    private static readonly List<(string CardToken, decimal Amount)> charges = [];
    private static int nextOrderId = 1001;

    public static IReadOnlyList<Product> Products { get; } =
    [
        new Product("keyboard", "Mechanical keyboard", 40m),
        new Product("mouse", "Wireless mouse", 15m),
        new Product("monitor", "27-inch monitor", 220m),
    ];

    public static IReadOnlyList<StoredOrder> Orders => orders;

    public static IReadOnlyList<(string CardToken, decimal Amount)> Charges => charges;

    public static string DeclinedCardToken { get; set; } = "declined";

    public static Product? FindProduct(string id) => Products.FirstOrDefault(product => product.Id == id);

    public static StoredOrder? FindOrder(int id) => orders.FirstOrDefault(order => order.Id == id);

    public static int AddOrder(string customerEmail, decimal total)
    {
        var order = new StoredOrder(nextOrderId++, customerEmail, total, Paid: false);
        orders.Add(order);
        return order.Id;
    }

    public static void MarkOrderPaid(int id)
    {
        var index = orders.FindIndex(order => order.Id == id);
        if (index >= 0)
        {
            orders[index] = orders[index] with { Paid = true };
        }
    }

    public static bool RemoveOrder(int id)
    {
        var order = orders.FirstOrDefault(order => order.Id == id);
        return order is not null && orders.Remove(order);
    }

    public static bool Charge(string cardToken, decimal amount)
    {
        if (cardToken == DeclinedCardToken)
        {
            return false;
        }

        charges.Add((cardToken, amount));
        return true;
    }

    public static void Reset()
    {
        orders.Clear();
        charges.Clear();
        nextOrderId = 1001;
        DeclinedCardToken = "declined";
    }
}
