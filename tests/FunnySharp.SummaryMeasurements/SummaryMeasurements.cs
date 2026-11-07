using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using FunnySharp;
using FunnySharp.TestSupport.FallibleReduction;
using static FunnySharp.TestSupport.MeasurementHarness;

namespace FunnySharp.TestSupport.SummaryComparison;

internal static class SummaryMeasurements
{
    private const int LargeIterations = 10_000;
    private const int LargeWarmup = 200;
    private const int AsyncLargeIterations = 5_000;
    private const int AsyncWarmup = 200;

    private static readonly Summary<OrderLine, LargeState, LargeState> LargeSummary =
        BuildLargeSummary();

    private static readonly Summary<OrderLine, int, int> BuiltInCount =
        Summary<OrderLine>.Count();

    private static readonly Summary<OrderLine, int, int> DelegateCount =
        Summary.Create<OrderLine, int>(0, static (count, _) => checked(count + 1));

    private static readonly Summary<WideLine, int, int> WideSummary =
        Summary<WideLine>.Count();

    private static readonly Summary<OrderLine, Option<decimal>, Option<decimal>> MinSummary =
        Summary<OrderLine>.Min(static line => line.UnitPrice);

    private static readonly Summary<OrderLine, Option<decimal>, Option<decimal>> MaxSummary =
        Summary<OrderLine>.Max(static line => line.UnitPrice);

    internal static async Task RunAsync()
    {
        Console.WriteLine($"runtime: {RuntimeInformation.FrameworkDescription}");
        Console.WriteLine($"os: {RuntimeInformation.OSDescription}");
        Console.WriteLine($"cores: {Environment.ProcessorCount}");
        Console.WriteLine();

        SummaryComparisonVerifier.Verify();
        await SummaryComparisonVerifier.VerifyAsync().ConfigureAwait(false);

        var success = CreateLines(1024);
        var medium = CreateLines(16);
        var empty = Array.Empty<OrderLine>();
        var wide = CreateWideLines(1024);
        var unitPrices = CreateUnitPrices(1024);
        using var cancellationSource = new CancellationTokenSource();
        var token = cancellationSource.Token;
        var asyncSuccess = new AsyncArraySequence<OrderLine>(success);

        await WriteConsumption(success, token).ConfigureAwait(false);
        WriteStateSizes();

        _ = Measure(
            () => Digest(BuiltInCount.Evaluate(success)),
            LargeIterations,
            LargeWarmup);

        Console.WriteLine("## Synchronous allocation and directional timing");
        Console.WriteLine();
        Console.WriteLine("| Operation | Allocated B/op | Time ns/op |");
        Console.WriteLine("| --- | ---: | ---: |");
        WriteRow(
            "Built-in Count summary, N=1024",
            Measure(() => Digest(BuiltInCount.Evaluate(success)), LargeIterations, LargeWarmup));
        WriteRow(
            "Custom delegate Count summary, N=1024",
            Measure(() => Digest(DelegateCount.Evaluate(success)), LargeIterations, LargeWarmup));
        WriteRow(
            "Hand-written Count loop, N=1024",
            Measure(() => Digest(CountLoop(success)), LargeIterations, LargeWarmup));
        WriteRow(
            "Bare minimum control, N=1024",
            Measure(() => Digest(MinLoop(unitPrices)), LargeIterations, LargeWarmup));
        WriteRow(
            "MinOrNone, N=1024",
            Measure(() => Digest(unitPrices.MinOrNone()), LargeIterations, LargeWarmup));
        WriteRow(
            "MaxOrNone, N=1024",
            Measure(() => Digest(unitPrices.MaxOrNone()), LargeIterations, LargeWarmup));
        WriteRow(
            "Summary.Min, N=1024",
            Measure(() => Digest(MinSummary.Evaluate(success)), LargeIterations, LargeWarmup));
        WriteRow(
            "Summary.Max, N=1024",
            Measure(() => Digest(MaxSummary.Evaluate(success)), LargeIterations, LargeWarmup));
        WriteRow(
            "Composed numeric summaries, N=1024",
            Measure(() => Digest(SummaryRules.Totals.Evaluate(success)), LargeIterations, LargeWarmup));
        WriteRow(
            "Hand-written numeric loop, N=1024",
            Measure(() => Digest(NumericLoop(success)), LargeIterations, LargeWarmup));
        WriteRow(
            "Four separate numeric summaries, N=1024",
            Measure(() => Digest(NumericSeparate(success)), LargeIterations, LargeWarmup));
        WriteRow(
            "Composed numeric summaries, N=16",
            Measure(() => Digest(SummaryRules.Totals.Evaluate(medium)), LargeIterations, LargeWarmup));
        WriteRow(
            "Hand-written numeric loop, N=16",
            Measure(() => Digest(NumericLoop(medium)), LargeIterations, LargeWarmup));
        WriteRow(
            "Four separate numeric summaries, N=16",
            Measure(() => Digest(NumericSeparate(medium)), LargeIterations, LargeWarmup));
        WriteRow(
            "Composed numeric summaries, empty",
            Measure(() => Digest(SummaryRules.Totals.Evaluate(empty)), LargeIterations, LargeWarmup));
        WriteRow(
            "Hand-written numeric loop, empty",
            Measure(() => Digest(NumericLoop(empty)), LargeIterations, LargeWarmup));
        WriteRow(
            "Four separate numeric summaries, empty",
            Measure(() => Digest(NumericSeparate(empty)), LargeIterations, LargeWarmup));
        WriteRow(
            "Full report composed summary, N=1024",
            Measure(() => Digest(FunnySharpSummaryConsumer.Evaluate(success)), LargeIterations, LargeWarmup));
        WriteRow(
            "Full report hand-written loop, N=1024",
            Measure(() => Digest(OrdinarySummaryConsumer.Evaluate(success)), LargeIterations, LargeWarmup));
        WriteRow(
            "Full report separate summaries, N=1024",
            Measure(() => Digest(SeparateSummaryConsumer.Evaluate(success)), LargeIterations, LargeWarmup));
        WriteRow(
            "64-byte value state leaf summary, N=1024",
            Measure(() => Digest(LargeSummary.Evaluate(success)), LargeIterations, LargeWarmup));
        WriteRow(
            "64-byte value state hand-written loop, N=1024",
            Measure(() => Digest(LargeLoop(success)), LargeIterations, LargeWarmup));
        WriteRow(
            "64-byte value item count, built-in Count summary, N=1024",
            Measure(() => Digest(WideSummary.Evaluate(wide)), LargeIterations, LargeWarmup));
        WriteRow(
            "64-byte value item count, hand-written loop, N=1024",
            Measure(() => Digest(CountLoop(wide)), LargeIterations, LargeWarmup));
        Console.WriteLine();

        Console.WriteLine("## Asynchronous allocation and directional timing");
        Console.WriteLine();
        Console.WriteLine("| Operation | Allocated B/op | Time ns/op |");
        Console.WriteLine("| --- | ---: | ---: |");
        WriteRow(
            "Composed numeric summaries, async source, N=1024",
            await MeasureAsync(
                async () => Digest(await SummaryRules.Totals.EvaluateAsync(asyncSuccess, token)),
                AsyncLargeIterations,
                AsyncWarmup));
        WriteRow(
            "Hand-written numeric loop, async source, N=1024",
            await MeasureAsync(
                async () => Digest(await NumericLoopAsync(asyncSuccess, token)),
                AsyncLargeIterations,
                AsyncWarmup));
        WriteRow(
            "Four separate numeric summaries, async source, N=1024",
            await MeasureAsync(
                async () => Digest(await NumericSeparateAsync(asyncSuccess, token)),
                AsyncLargeIterations,
                AsyncWarmup));
        WriteRow(
            "Full report composed summary, async source, N=1024",
            await MeasureAsync(
                async () => Digest(await FunnySharpSummaryConsumer.EvaluateAsync(asyncSuccess, token)),
                AsyncLargeIterations,
                AsyncWarmup));
        WriteRow(
            "Full report hand-written loop, async source, N=1024",
            await MeasureAsync(
                async () => Digest(await OrdinarySummaryConsumer.EvaluateAsync(asyncSuccess, token)),
                AsyncLargeIterations,
                AsyncWarmup));
        WriteRow(
            "Full report separate summaries, async source, N=1024",
            await MeasureAsync(
                async () => Digest(await SeparateSummaryConsumer.EvaluateAsync(asyncSuccess, token)),
                AsyncLargeIterations,
                AsyncWarmup));
        Console.WriteLine();
    }

    private static async Task WriteConsumption(
        OrderLine[] success,
        CancellationToken token)
    {
        var composedProbe = new ReductionProbe<OrderLine>(success);
        _ = FunnySharpSummaryConsumer.Evaluate(composedProbe);

        var ordinaryProbe = new ReductionProbe<OrderLine>(success);
        _ = OrdinarySummaryConsumer.Evaluate(ordinaryProbe);

        var separateProbe = new ReductionProbe<OrderLine>(success);
        _ = SeparateSummaryConsumer.Evaluate(separateProbe);

        Console.WriteLine("## Source consumption, N=1024");
        Console.WriteLine();
        Console.WriteLine("| Consumer | GetEnumerator | MoveNext | Current | Dispose |");
        Console.WriteLine("| --- | ---: | ---: | ---: | ---: |");
        WriteConsumptionRow("Composed summary", composedProbe);
        WriteConsumptionRow("Hand-written loop", ordinaryProbe);
        WriteConsumptionRow("Separate summaries", separateProbe);
        Console.WriteLine();

        var composedAsyncProbe = new AsyncReductionProbe<OrderLine>(success);
        _ = await FunnySharpSummaryConsumer.EvaluateAsync(composedAsyncProbe, token).ConfigureAwait(false);

        var ordinaryAsyncProbe = new AsyncReductionProbe<OrderLine>(success);
        _ = await OrdinarySummaryConsumer.EvaluateAsync(ordinaryAsyncProbe, token).ConfigureAwait(false);

        var separateAsyncProbe = new AsyncReductionProbe<OrderLine>(success);
        _ = await SeparateSummaryConsumer.EvaluateAsync(separateAsyncProbe, token).ConfigureAwait(false);

        Console.WriteLine("| Async consumer | GetAsyncEnumerator | MoveNextAsync | Current | DisposeAsync |");
        Console.WriteLine("| --- | ---: | ---: | ---: | ---: |");
        WriteAsyncConsumptionRow("Composed summary", composedAsyncProbe);
        WriteAsyncConsumptionRow("Hand-written loop", ordinaryAsyncProbe);
        WriteAsyncConsumptionRow("Separate summaries", separateAsyncProbe);
        Console.WriteLine();
    }

    private static void WriteConsumptionRow(string consumer, ReductionProbe<OrderLine> probe) =>
        Console.WriteLine(
            $"| {consumer} | {probe.GetEnumeratorCalls} | {probe.MoveNextCalls} | "
            + $"{probe.CurrentReads} | {probe.DisposeCalls} |");

    private static void WriteAsyncConsumptionRow(string consumer, AsyncReductionProbe<OrderLine> probe) =>
        Console.WriteLine(
            $"| {consumer} | {probe.GetAsyncEnumeratorCalls} | {probe.MoveNextAsyncCalls} | "
            + $"{probe.CurrentReads} | {probe.DisposeAsyncCalls} |");

    private static void WriteStateSizes()
    {
        Console.WriteLine("## Summary state sizes");
        Console.WriteLine();
        Console.WriteLine("| State | Bytes |");
        Console.WriteLine("| --- | ---: |");
        WriteStateSize("int", Unsafe.SizeOf<int>());
        WriteStateSize("decimal", Unsafe.SizeOf<decimal>());
        WriteStateSize("Option<decimal>", Unsafe.SizeOf<Option<decimal>>());
        WriteStateSize("numeric totals tuple", SummaryRules.TotalsStateSize);
        WriteStateSize("full report tuple", SummaryRules.ReportStateSize);
        WriteStateSize("64-byte value state", Unsafe.SizeOf<LargeState>());
        WriteStateSize("Dictionary<string,int> reference", Unsafe.SizeOf<Dictionary<string, int>>());
        WriteStateSize("64-byte value item", Unsafe.SizeOf<WideLine>());
        Console.WriteLine();
    }

    private static void WriteStateSize(string state, int bytes) =>
        Console.WriteLine($"| {state} | {bytes} |");

    private static void WriteRow(string operation, Measurement measurement) =>
        Console.WriteLine(
            $"| {operation} | {Format(measurement.BytesPerOperation)} | "
            + $"{Format(measurement.NanosecondsPerOperation)} |");

    private static Summary<OrderLine, LargeState, LargeState> BuildLargeSummary() =>
        Summary.Create<OrderLine, LargeState>(
            default(LargeState),
            static (state, line) => state.Add(line.Quantity));

    private static OrderTotals NumericLoop(IEnumerable<OrderLine> source)
    {
        var count = 0;
        var total = 0m;
        var cheapest = Option<decimal>.None;
        var mostExpensive = Option<decimal>.None;

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
        }

        return new OrderTotals(count, total, cheapest, mostExpensive);
    }

    private static OrderTotals NumericSeparate(IEnumerable<OrderLine> source) =>
        new(
            SummaryRules.Count.Evaluate(source),
            SummaryRules.Total.Evaluate(source),
            SummaryRules.Cheapest.Evaluate(source),
            SummaryRules.MostExpensive.Evaluate(source));

    private static async ValueTask<OrderTotals> NumericLoopAsync(
        IAsyncEnumerable<OrderLine> source,
        CancellationToken cancellationToken)
    {
        var count = 0;
        var total = 0m;
        var cheapest = Option<decimal>.None;
        var mostExpensive = Option<decimal>.None;

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
        }

        return new OrderTotals(count, total, cheapest, mostExpensive);
    }

    private static async ValueTask<OrderTotals> NumericSeparateAsync(
        IAsyncEnumerable<OrderLine> source,
        CancellationToken cancellationToken) =>
        new(
            await SummaryRules.Count.EvaluateAsync(source, cancellationToken).ConfigureAwait(false),
            await SummaryRules.Total.EvaluateAsync(source, cancellationToken).ConfigureAwait(false),
            await SummaryRules.Cheapest.EvaluateAsync(source, cancellationToken).ConfigureAwait(false),
            await SummaryRules.MostExpensive.EvaluateAsync(source, cancellationToken).ConfigureAwait(false));

    private static int CountLoop<T>(IEnumerable<T> source)
    {
        var count = 0;
        foreach (var _ in source)
        {
            count = checked(count + 1);
        }

        return count;
    }

    private static LargeState LargeLoop(IEnumerable<OrderLine> source)
    {
        var state = default(LargeState);
        foreach (var line in source)
        {
            state = state.Add(line.Quantity);
        }

        return state;
    }

    private static decimal MinLoop(IEnumerable<decimal> source)
    {
        var minimum = 0m;
        var found = false;
        foreach (var value in source)
        {
            if (!found || value < minimum)
            {
                minimum = value;
                found = true;
            }
        }

        return minimum;
    }

    private static decimal[] CreateUnitPrices(int count)
    {
        var prices = new decimal[count];
        for (var index = 0; index < count; index++)
        {
            prices[index] = 5m + (index % 17);
        }

        return prices;
    }

    private static int Digest(Option<decimal> value) => value.GetHashCode();

    private static int Digest(decimal value) => value.GetHashCode();

    private static int Digest(OrderTotals totals) =>
        HashCode.Combine(totals.LineCount, totals.Total, totals.CheapestUnitPrice, totals.MostExpensiveUnitPrice);

    private static int Digest(OrderReport report) =>
        HashCode.Combine(report.Totals, report.Categories.Count, report.FirstSku);

    private static int Digest(LargeState state) => state.GetHashCode();

    private static int Digest(int value) => value;

    private static OrderLine[] CreateLines(int count)
    {
        var lines = new OrderLine[count];
        for (var index = 0; index < count; index++)
        {
            lines[index] = new OrderLine(
                $"SKU-{index:D4}",
                index % 3 == 0 ? "books" : index % 3 == 1 ? "tools" : "garden",
                (index % 5) + 1,
                5m + (index % 17));
        }

        return lines;
    }

    private static WideLine[] CreateWideLines(int count)
    {
        var lines = new WideLine[count];
        for (var index = 0; index < count; index++)
        {
            lines[index] = new WideLine(index, index + 1, index + 2, index + 3, index + 4, index + 5, index + 6, index + 7);
        }

        return lines;
    }

    private readonly record struct LargeState(
        long A,
        long B,
        long C,
        long D,
        long E,
        long F,
        long G,
        long H)
    {
        internal LargeState Add(long value) =>
            new(
                checked(A + value),
                B,
                C,
                D,
                E,
                F,
                G,
                H);
    }

    private readonly record struct WideLine(
        long A,
        long B,
        long C,
        long D,
        long E,
        long F,
        long G,
        long H);

}
