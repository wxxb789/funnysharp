using System.Runtime.CompilerServices;
using FunnySharp;

using TotalsState = (int First, decimal Second, FunnySharp.Option<decimal> Third, FunnySharp.Option<decimal> Fourth);
using ReportState = (
    (int First, decimal Second, FunnySharp.Option<decimal> Third, FunnySharp.Option<decimal> Fourth) First,
    System.Collections.Generic.Dictionary<string, int> Second,
    FunnySharp.Option<string> Third);

namespace FunnySharp.TestSupport.SummaryComparison;

internal static class SummaryRules
{
    internal static readonly Summary<OrderLine, int, int> Count =
        Summary<OrderLine>.Count();

    internal static readonly Summary<OrderLine, decimal, decimal> Total =
        Summary<OrderLine>.Sum(static line => line.Amount);

    internal static readonly Summary<OrderLine, Option<decimal>, Option<decimal>> Cheapest =
        Summary<OrderLine>.Min(static line => line.UnitPrice);

    internal static readonly Summary<OrderLine, Option<decimal>, Option<decimal>> MostExpensive =
        Summary<OrderLine>.Max(static line => line.UnitPrice);

    internal static readonly Summary<OrderLine, Dictionary<string, int>, Dictionary<string, int>> CategoryCounts =
        Summary.Create<OrderLine, Dictionary<string, int>>(
            static () => new Dictionary<string, int>(StringComparer.Ordinal),
            static (counts, line) =>
            {
                counts.TryGetValue(line.Category, out var count);
                counts[line.Category] = checked(count + 1);
                return counts;
            });

    internal static readonly Summary<OrderLine, Dictionary<string, int>, IReadOnlyList<CategoryCount>> Categories =
        CategoryCounts.MapResult(ProjectCategories);

    internal static readonly Summary<OrderLine, Option<string>, Option<string>> FirstSku =
        Summary.Create<OrderLine, Option<string>>(
            Option<string>.None,
            static (first, line) => first.IsSome ? first : Option<string>.Some(line.Sku));

    internal static readonly Summary<OrderLine, TotalsState, OrderTotals> Totals =
        Summary.Zip(Count, Total, Cheapest, MostExpensive)
            .MapResult(static results => new OrderTotals(
                results.First,
                results.Second,
                results.Third,
                results.Fourth));

    internal static int TotalsStateSize => Unsafe.SizeOf<TotalsState>();

    internal static int ReportStateSize => Unsafe.SizeOf<ReportState>();

    internal static readonly Summary<OrderLine, ReportState, OrderReport> Composed =
        Summary.Zip(Totals, Categories, FirstSku)
            .MapResult(static results => new OrderReport(
                results.First,
                results.Second,
                results.Third));

    internal static IReadOnlyList<CategoryCount> ProjectCategories(
        Dictionary<string, int> counts) =>
        counts
            .OrderBy(static pair => pair.Key, StringComparer.Ordinal)
            .Select(static pair => new CategoryCount(pair.Key, pair.Value))
            .ToArray();
}
