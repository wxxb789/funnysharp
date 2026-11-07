namespace FunnySharp.TestSupport.SummaryComparison;

internal sealed record SummaryFixture(string Name, IReadOnlyList<OrderLine> Lines);

internal static class SummaryFixtures
{
    internal static readonly SummaryFixture Empty =
        new("empty", Array.Empty<OrderLine>());

    internal static readonly SummaryFixture Single =
        new(
            "single",
            [
                new OrderLine("SKU-1", "books", 1, 12.50m),
            ]);

    internal static readonly SummaryFixture TiedPrices =
        new(
            "tied prices",
            [
                new OrderLine("SKU-A", "books", 1, 10.00m),
                new OrderLine("SKU-B", "books", 2, 10.00m),
            ]);

    internal static readonly SummaryFixture MixedCategories =
        new(
            "mixed categories",
            [
                new OrderLine("SKU-1", "books", 2, 12.50m),
                new OrderLine("SKU-2", "tools", 1, 40.00m),
                new OrderLine("SKU-3", "books", 3, 8.00m),
                new OrderLine("SKU-4", "garden", 4, 5.25m),
                new OrderLine("SKU-5", "tools", 0, 99.00m),
            ]);

    internal static readonly SummaryFixture[] All =
        [Empty, Single, TiedPrices, MixedCategories];
}
