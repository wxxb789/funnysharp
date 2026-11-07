using FunnySharp;
using FunnySharp.TestSupport.FallibleReduction;

namespace FunnySharp.TestSupport.SummaryComparison;

internal sealed record PricedLine(string Sku, decimal Amount, decimal RunningTotal);

internal sealed record PricingResult(decimal FinalTotal, IReadOnlyList<PricedLine> Lines);

internal readonly record struct PricingState(decimal RunningTotal, PricedLine Output);

internal static class MapAccumAssessment
{
    private static readonly PricedLine SeedOutput = new(string.Empty, 0m, 0m);

    internal static void Verify()
    {
        foreach (var fixture in SummaryFixtures.All)
        {
            var scanProbe = new ReductionProbe<OrderLine>(fixture.Lines);
            var scan = ScanSelect(scanProbe);

            var aggregateProbe = new ReductionProbe<OrderLine>(fixture.Lines);
            var aggregate = AggregateTuple(aggregateProbe);

            var mapAccumProbe = new ReductionProbe<OrderLine>(fixture.Lines);
            var mapAccum = MapAccum(mapAccumProbe);

            EqualPricing(scan, aggregate, fixture.Name + " Scan plus Select vs Aggregate tuple");
            EqualPricing(scan, mapAccum, fixture.Name + " Scan plus Select vs MapAccum helper");
            SummaryComparisonVerifier.Equal(1, scanProbe.GetEnumeratorCalls, fixture.Name + " Scan enumerators");
            SummaryComparisonVerifier.Equal(1, aggregateProbe.GetEnumeratorCalls, fixture.Name + " Aggregate enumerators");
            SummaryComparisonVerifier.Equal(1, mapAccumProbe.GetEnumeratorCalls, fixture.Name + " MapAccum enumerators");
        }
    }

    internal static PricingResult ScanSelect(IEnumerable<OrderLine> source)
    {
        var states = source.Scan(
            new PricingState(0m, SeedOutput),
            static (state, line) =>
            {
                var runningTotal = state.RunningTotal + line.Amount;
                return new PricingState(
                    runningTotal,
                    new PricedLine(line.Sku, line.Amount, runningTotal));
            });

        var lines = states.Select(static state => state.Output).ToArray();
        var finalTotal = lines.Length == 0 ? 0m : lines[^1].RunningTotal;
        return new PricingResult(finalTotal, lines);
    }

    internal static PricingResult AggregateTuple(IEnumerable<OrderLine> source)
    {
        var (finalTotal, lines) = source.Aggregate(
            (RunningTotal: 0m, Lines: new List<PricedLine>()),
            static (state, line) =>
            {
                var runningTotal = state.RunningTotal + line.Amount;
                state.Lines.Add(new PricedLine(line.Sku, line.Amount, runningTotal));
                return (runningTotal, state.Lines);
            });

        return new PricingResult(finalTotal, lines);
    }

    internal static PricingResult MapAccum(IEnumerable<OrderLine> source)
    {
        var (finalTotal, lines) = MapAccumCore(
            source,
            0m,
            static (runningTotal, line) =>
            {
                var nextTotal = runningTotal + line.Amount;
                return (nextTotal, new PricedLine(line.Sku, line.Amount, nextTotal));
            });

        return new PricingResult(finalTotal, lines);
    }

    private static (TState State, IReadOnlyList<TResult> Outputs) MapAccumCore<TSource, TState, TResult>(
        IEnumerable<TSource> source,
        TState seed,
        Func<TState, TSource, (TState State, TResult Output)> step)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(step);

        var state = seed;
        var outputs = new List<TResult>();
        foreach (var item in source)
        {
            var (nextState, output) = step(state, item);
            state = nextState;
            outputs.Add(output);
        }

        return (state, outputs);
    }

    private static void EqualPricing(PricingResult expected, PricingResult actual, string description)
    {
        if (expected.FinalTotal != actual.FinalTotal)
        {
            throw new InvalidOperationException(
                $"{description}: expected final total {expected.FinalTotal}, actual {actual.FinalTotal}.");
        }

        if (expected.Lines.Count != actual.Lines.Count)
        {
            throw new InvalidOperationException(
                $"{description}: expected {expected.Lines.Count} lines, actual {actual.Lines.Count}.");
        }

        for (var index = 0; index < expected.Lines.Count; index++)
        {
            if (expected.Lines[index] != actual.Lines[index])
            {
                throw new InvalidOperationException(
                    $"{description}: line {index} expected {expected.Lines[index]}, actual {actual.Lines[index]}.");
            }
        }
    }
}
