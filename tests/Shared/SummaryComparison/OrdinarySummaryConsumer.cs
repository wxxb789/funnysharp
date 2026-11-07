using FunnySharp;

namespace FunnySharp.TestSupport.SummaryComparison;

internal static class OrdinarySummaryConsumer
{
    internal static OrderReport Evaluate(IEnumerable<OrderLine> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var count = 0;
        var total = 0m;
        var cheapest = Option<decimal>.None;
        var mostExpensive = Option<decimal>.None;
        var firstSku = Option<string>.None;
        var categories = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var line in source)
        {
            count = checked(count + 1);
            total = checked(total + line.Amount);

            if (!cheapest.TryGetValue(out var currentCheapest) || line.UnitPrice < currentCheapest)
            {
                cheapest = Option.Some(line.UnitPrice);
            }

            if (!mostExpensive.TryGetValue(out var currentMostExpensive)
                || line.UnitPrice > currentMostExpensive)
            {
                mostExpensive = Option.Some(line.UnitPrice);
            }

            categories.TryGetValue(line.Category, out var categoryCount);
            categories[line.Category] = checked(categoryCount + 1);

            if (firstSku.IsNone)
            {
                firstSku = Option.Some(line.Sku);
            }
        }

        return new OrderReport(
            new OrderTotals(count, total, cheapest, mostExpensive),
            SummaryRules.ProjectCategories(categories),
            firstSku);
    }

    internal static async ValueTask<OrderReport> EvaluateAsync(
        IAsyncEnumerable<OrderLine> source,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);

        var count = 0;
        var total = 0m;
        var cheapest = Option<decimal>.None;
        var mostExpensive = Option<decimal>.None;
        var firstSku = Option<string>.None;
        var categories = new Dictionary<string, int>(StringComparer.Ordinal);

        await foreach (var line in source.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            count = checked(count + 1);
            total = checked(total + line.Amount);

            if (!cheapest.TryGetValue(out var currentCheapest) || line.UnitPrice < currentCheapest)
            {
                cheapest = Option.Some(line.UnitPrice);
            }

            if (!mostExpensive.TryGetValue(out var currentMostExpensive)
                || line.UnitPrice > currentMostExpensive)
            {
                mostExpensive = Option.Some(line.UnitPrice);
            }

            categories.TryGetValue(line.Category, out var categoryCount);
            categories[line.Category] = checked(categoryCount + 1);

            if (firstSku.IsNone)
            {
                firstSku = Option.Some(line.Sku);
            }
        }

        return new OrderReport(
            new OrderTotals(count, total, cheapest, mostExpensive),
            SummaryRules.ProjectCategories(categories),
            firstSku);
    }
}
