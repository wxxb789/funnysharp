namespace FunnySharp.TestSupport.IncrementalComparison;

internal static class PricingFixtures
{
    internal const int ProductCount = 512;
    internal const int OrderCount = 1024;
    internal const int LinesPerOrder = 8;

    private static readonly string[] Regions = ["EU", "US", "APAC"];
    private static readonly string[] Categories = ["A", "B", "C"];
    private static readonly decimal[] TaxRates = [0.19m, 0.07m, 0.00m];

    internal static Product[] CreateProducts()
    {
        var products = new Product[ProductCount];

        for (var index = 0; index < products.Length; index++)
        {
            products[index] = new Product(
                index,
                50m + (index % 97),
                TaxRates[index % TaxRates.Length],
                Categories[index % Categories.Length]);
        }

        return products;
    }

    internal static Order[] CreateOrders()
    {
        var orders = new Order[OrderCount];

        for (var orderIndex = 0; orderIndex < orders.Length; orderIndex++)
        {
            var lines = new OrderLine[LinesPerOrder];

            for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                lines[lineIndex] = new OrderLine(
                    (orderIndex * LinesPerOrder + lineIndex) % ProductCount,
                    1 + ((orderIndex + lineIndex) % 3));
            }

            orders[orderIndex] = new Order(
                orderIndex,
                Regions[orderIndex % Regions.Length],
                lines);
        }

        return orders;
    }

    internal static Order[] CloneOrders(IReadOnlyList<Order> source)
    {
        var orders = new Order[source.Count];

        for (var index = 0; index < orders.Length; index++)
        {
            orders[index] = source[index] with
            {
                Lines = source[index].Lines.ToArray(),
            };
        }

        return orders;
    }

    internal static Product[] CloneProducts(IReadOnlyList<Product> source) =>
        source.ToArray();

    internal static Order ReplaceQuantity(Order order, int lineIndex, int quantity)
    {
        if ((uint)lineIndex >= (uint)order.Lines.Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(lineIndex),
                lineIndex,
                "The order has no line at that index.");
        }

        var lines = new OrderLine[order.Lines.Count];

        for (var index = 0; index < lines.Length; index++)
        {
            lines[index] = order.Lines[index];
        }

        lines[lineIndex] = lines[lineIndex] with { Quantity = quantity };
        return order with { Lines = lines };
    }

    internal static Order CreateAdditionalOrder(int id)
    {
        var lines = new OrderLine[LinesPerOrder];

        for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            lines[lineIndex] = new OrderLine(
                (id * LinesPerOrder + lineIndex) % ProductCount,
                1 + ((id + lineIndex) % 3));
        }

        return new Order(id, Regions[id % Regions.Length], lines);
    }
}