using System.Collections.Generic;
using System.Linq;
using FunnySharp;

// <snippet DocumentationSamples.OnePassSummaries.ReportStateAlias>
using ReportState = (
    (int First, decimal Second, FunnySharp.Option<decimal> Third, FunnySharp.Option<decimal> Fourth) First,
    System.Collections.Generic.Dictionary<string, int> Second,
    FunnySharp.Option<string> Third);
// </snippet>

namespace FunnySharp.DocumentationSamples;

internal static class OnePassSummarySamples
{
    private static readonly Summary<OrderLine, Dictionary<string, int>, IReadOnlyList<CategoryCount>> Categories =
        Summary.Create<OrderLine, Dictionary<string, int>, IReadOnlyList<CategoryCount>>(
            static () => new Dictionary<string, int>(StringComparer.Ordinal),
            static (counts, line) =>
            {
                counts.TryGetValue(line.Category, out var count);
                counts[line.Category] = count + 1;
                return counts;
            },
            static counts => counts
                .OrderBy(static pair => pair.Key, StringComparer.Ordinal)
                .Select(static pair => new CategoryCount(pair.Key, pair.Value))
                .ToArray());

    private static readonly Summary<OrderLine, Option<string>, Option<string>> FirstSku =
        Summary.Create<OrderLine, Option<string>>(
            Option<string>.None,
            static (first, line) => first.IsSome ? first : Option.Some(line.Sku));

    internal static readonly Summary<OrderLine, ReportState, OrderReport> ReusableReport =
        ComposeReport();

    private static Summary<OrderLine, ReportState, OrderReport> ComposeReport()
    {
        var categories = Categories;
        var firstSku = FirstSku;

        // <snippet DocumentationSamples.OnePassSummaries.ComposeReport>
        var totals = Summary.Zip(
                Summary<OrderLine>.Count(),
                Summary<OrderLine>.Sum(static line => line.Amount),
                Summary<OrderLine>.Min(static line => line.UnitPrice),
                Summary<OrderLine>.Max(static line => line.UnitPrice))
            .MapResult(static results => new OrderTotals(
                results.First,
                results.Second,
                results.Third,
                results.Fourth));

        var summary = Summary.Zip(totals, categories, firstSku)
            .MapResult(static results => new OrderReport(
                results.First.Count,
                results.First.Total,
                results.First.Cheapest,
                results.First.MostExpensive,
                results.Second,
                results.Third));

        return summary;
        // </snippet>
    }

    private static (
        Summary<OrderLine, Option<string>, Option<string>> FirstSku,
        Summary<OrderLine, Dictionary<string, int>, IReadOnlyList<CategoryCount>> Categories)
        CreateLeafSummaries()
    {
        // <snippet DocumentationSamples.OnePassSummaries.DefineLeafSummaries>
        var firstSku = Summary.Create<OrderLine, Option<string>>(
            Option<string>.None,
            static (first, line) => first.IsSome ? first : Option.Some(line.Sku));

        var categories = Summary.Create<OrderLine, Dictionary<string, int>, IReadOnlyList<CategoryCount>>(
            static () => new Dictionary<string, int>(StringComparer.Ordinal),
            static (counts, line) =>
            {
                counts.TryGetValue(line.Category, out var count);
                counts[line.Category] = count + 1;
                return counts;
            },
            static counts => counts
                .OrderBy(static pair => pair.Key, StringComparer.Ordinal)
                .Select(static pair => new CategoryCount(pair.Key, pair.Value))
                .ToArray());
        // </snippet>

        return (firstSku, categories);
    }

    private static async Task ReuseSummaryAsync(
        Summary<OrderLine, ReportState, OrderReport> summary,
        IEnumerable<OrderLine> lines,
        CancellationToken cancellationToken)
    {
        // <snippet DocumentationSamples.OnePassSummaries.ReuseSyncAndAsync>
        var synchronous = summary.Evaluate(lines);
        var asynchronous = await summary.EvaluateAsync(ToAsync(lines), cancellationToken);
        // </snippet>

        _ = synchronous;
        _ = asynchronous;
    }

    private static (List<string> FirstVisit, List<string> SecondVisit) IsolateFactoryState(
        IEnumerable<OrderLine> lines)
    {
        // <snippet DocumentationSamples.OnePassSummaries.FactoryIsolation>
        var visits = Summary.Create<OrderLine, List<string>>(
            static () => [],
            static (seen, line) =>
            {
                seen.Add(line.Sku);
                return seen;
            });

        var (firstVisit, secondVisit) = Summary.Zip(visits, visits).Evaluate(lines);
        // firstVisit and secondVisit are separate lists.
        // </snippet>

        return (firstVisit, secondVisit);
    }

    private static async IAsyncEnumerable<OrderLine> ToAsync(IEnumerable<OrderLine> lines)
    {
        await Task.Yield();
        foreach (var line in lines)
        {
            yield return line;
        }
    }

    internal sealed record OrderLine(string Sku, string Category, int Quantity, decimal UnitPrice)
    {
        internal decimal Amount => Quantity * UnitPrice;
    }

    internal sealed record CategoryCount(string Category, int Count);

    internal sealed record OrderTotals(
        int Count,
        decimal Total,
        Option<decimal> Cheapest,
        Option<decimal> MostExpensive);

    internal sealed record OrderReport(
        int Count,
        decimal Total,
        Option<decimal> Cheapest,
        Option<decimal> MostExpensive,
        IReadOnlyList<CategoryCount> Categories,
        Option<string> FirstSku);
}
