using FunnySharp;

internal static class SummaryExamples
{
    private static readonly Summary<ExampleLine, int, int> Count =
        Summary<ExampleLine>.Count();

    private static readonly Summary<ExampleLine, decimal, decimal> Total =
        Summary<ExampleLine>.Sum(static line => line.Amount);

    private static readonly Summary<ExampleLine, Option<decimal>, Option<decimal>> Cheapest =
        Summary<ExampleLine>.Min(static line => line.UnitPrice);

    private static readonly Summary<ExampleLine, Option<decimal>, Option<decimal>> MostExpensive =
        Summary<ExampleLine>.Max(static line => line.UnitPrice);

    private static readonly Summary<ExampleLine, Dictionary<string, int>, IReadOnlyList<CategoryCount>> Categories =
        Summary.Create<ExampleLine, Dictionary<string, int>, IReadOnlyList<CategoryCount>>(
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

    private static readonly Summary<ExampleLine, Option<string>, Option<string>> FirstSku =
        Summary.Create<ExampleLine, Option<string>>(
            Option<string>.None,
            static (first, line) => first.IsSome ? first : Option.Some(line.Sku));

    private static readonly ReportPlan Report = BuildReport();

    internal static void Verify()
    {
        var plan = Report;
        var lines = ExampleLines();

        var report = plan.Evaluate(lines);
        AssertReport(report);

        var empty = plan.Evaluate(Array.Empty<ExampleLine>());
        ExampleAssertions.Equal(0, empty.Count);
        ExampleAssertions.Equal(0m, empty.Total);
        ExampleAssertions.True(empty.Cheapest.IsNone, "An empty source has no cheapest unit price.");
        ExampleAssertions.True(empty.MostExpensive.IsNone, "An empty source has no most expensive unit price.");
        ExampleAssertions.Equal(0, empty.Categories.Count);
        ExampleAssertions.True(empty.FirstSku.IsNone, "An empty source has no first SKU.");

        var visits = Summary.Create<ExampleLine, List<string>>(
            static () => [],
            static (seen, line) =>
            {
                seen.Add(line.Sku);
                return seen;
            });
        var (firstVisit, secondVisit) = Summary.Zip(visits, visits).Evaluate(lines);
        ExampleAssertions.True(
            !ReferenceEquals(firstVisit, secondVisit),
            "A factory-seeded summary must isolate each composed component's state.");
        ExampleAssertions.SequenceEqual(["SKU-BOOK", "SKU-PEN", "SKU-NOTE"], firstVisit);
        ExampleAssertions.SequenceEqual(["SKU-BOOK", "SKU-PEN", "SKU-NOTE"], secondVisit);

        ExampleAssertions.Throws<OverflowException>(
            () => Summary<decimal>.Sum(static value => value).Evaluate([decimal.MaxValue, 1m]));
    }

    internal static async Task VerifyAsync()
    {
        var plan = Report;
        var lines = ExampleLines();

        using var cancellationSource = new CancellationTokenSource();
        var synchronous = plan.Evaluate(lines);
        var asynchronous = await plan.EvaluateAsync(ToAsync(lines), cancellationSource.Token);

        ExampleAssertions.Equal(synchronous.Count, asynchronous.Count);
        ExampleAssertions.Equal(synchronous.Total, asynchronous.Total);
        ExampleAssertions.Equal(synchronous.Cheapest, asynchronous.Cheapest);
        ExampleAssertions.Equal(synchronous.MostExpensive, asynchronous.MostExpensive);
        ExampleAssertions.Equal(synchronous.FirstSku, asynchronous.FirstSku);
        ExampleAssertions.SequenceEqual(synchronous.Categories, asynchronous.Categories);
        AssertReport(asynchronous);
    }

    private static void AssertReport(OrderReport report)
    {
        ExampleAssertions.Equal(3, report.Count);
        ExampleAssertions.Equal(41.00m, report.Total);
        ExampleAssertions.Equal(Option.Some(4.00m), report.Cheapest);
        ExampleAssertions.Equal(Option.Some(12.50m), report.MostExpensive);
        ExampleAssertions.Equal(Option.Some("SKU-BOOK"), report.FirstSku);
        ExampleAssertions.SequenceEqual(
            [new CategoryCount("books", 2), new CategoryCount("tools", 1)],
            report.Categories);
    }

    private static ReportPlan BuildReport()
    {
        var totals = Summary.Zip(Count, Total, Cheapest, MostExpensive)
            .MapResult(static results => new OrderTotals(
                results.First,
                results.Second,
                results.Third,
                results.Fourth));

        var summary = Summary.Zip(totals, Categories, FirstSku)
            .MapResult(static results => new OrderReport(
                results.First.Count,
                results.First.Total,
                results.First.Cheapest,
                results.First.MostExpensive,
                results.Second,
                results.Third));

        return new ReportPlan(summary.Evaluate, summary.EvaluateAsync);
    }

    private static ExampleLine[] ExampleLines() =>
    [
        new ExampleLine("SKU-BOOK", "books", 2, 12.50m),
        new ExampleLine("SKU-PEN", "tools", 1, 4.00m),
        new ExampleLine("SKU-NOTE", "books", 3, 4.00m),
    ];

    private static async IAsyncEnumerable<ExampleLine> ToAsync(IEnumerable<ExampleLine> lines)
    {
        foreach (var line in lines)
        {
            await Task.Yield();
            yield return line;
        }
    }

    private sealed record ExampleLine(string Sku, string Category, int Quantity, decimal UnitPrice)
    {
        internal decimal Amount => Quantity * UnitPrice;
    }

    private sealed record CategoryCount(string Category, int Count);

    private sealed record OrderTotals(
        int Count,
        decimal Total,
        Option<decimal> Cheapest,
        Option<decimal> MostExpensive);

    private readonly record struct ReportPlan(
        Func<IEnumerable<ExampleLine>, OrderReport> Evaluate,
        Func<IAsyncEnumerable<ExampleLine>, CancellationToken, ValueTask<OrderReport>> EvaluateAsync);

    private sealed record OrderReport(
        int Count,
        decimal Total,
        Option<decimal> Cheapest,
        Option<decimal> MostExpensive,
        IReadOnlyList<CategoryCount> Categories,
        Option<string> FirstSku);
}
