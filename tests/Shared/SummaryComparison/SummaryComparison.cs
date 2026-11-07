using FunnySharp;
using FunnySharp.TestSupport.FallibleReduction;

namespace FunnySharp.TestSupport.SummaryComparison;

internal static class SummaryComparisonVerifier
{
    internal static void Verify()
    {
        MapAccumAssessment.Verify();
        VerifyComponentOrder();
        VerifyComponentException();
        VerifyEarlyDeterminedComponent();

        foreach (var fixture in SummaryFixtures.All)
        {
            VerifySyncFixture(fixture);
        }
    }

    internal static async Task VerifyAsync()
    {
        foreach (var fixture in SummaryFixtures.All)
        {
            await VerifyAsyncFixture(fixture).ConfigureAwait(false);
        }
    }

    private static void VerifySyncFixture(SummaryFixture fixture)
    {
        var ordinaryProbe = new ReductionProbe<OrderLine>(fixture.Lines);
        var ordinary = OrdinarySummaryConsumer.Evaluate(ordinaryProbe);

        var separateProbe = new ReductionProbe<OrderLine>(fixture.Lines);
        var separate = SeparateSummaryConsumer.Evaluate(separateProbe);

        var funnyProbe = new ReductionProbe<OrderLine>(fixture.Lines);
        var funny = FunnySharpSummaryConsumer.Evaluate(funnyProbe);

        Equal(ordinary, separate, fixture.Name + " separate report");
        Equal(ordinary, funny, fixture.Name + " composed report");

        AssertSyncProbe(ordinaryProbe, fixture.Lines.Count, fixture.Name + " ordinary");
        AssertSyncProbe(funnyProbe, fixture.Lines.Count, fixture.Name + " composed");
        AssertSeparateSyncProbe(separateProbe, fixture.Lines.Count, fixture.Name + " separate");
    }

    private static async Task VerifyAsyncFixture(SummaryFixture fixture)
    {
        using var cancellation = new CancellationTokenSource();
        var token = cancellation.Token;

        var ordinaryProbe = new AsyncReductionProbe<OrderLine>(fixture.Lines);
        var ordinary = await OrdinarySummaryConsumer
            .EvaluateAsync(ordinaryProbe, token)
            .ConfigureAwait(false);

        var separateProbe = new AsyncReductionProbe<OrderLine>(fixture.Lines);
        var separate = await SeparateSummaryConsumer
            .EvaluateAsync(separateProbe, token)
            .ConfigureAwait(false);

        var funnyProbe = new AsyncReductionProbe<OrderLine>(fixture.Lines);
        var funny = await FunnySharpSummaryConsumer
            .EvaluateAsync(funnyProbe, token)
            .ConfigureAwait(false);

        Equal(ordinary, separate, fixture.Name + " async separate report");
        Equal(ordinary, funny, fixture.Name + " async composed report");

        AssertAsyncProbe(ordinaryProbe, fixture.Lines.Count, token, fixture.Name + " async ordinary");
        AssertAsyncProbe(funnyProbe, fixture.Lines.Count, token, fixture.Name + " async composed");
        AssertSeparateAsyncProbe(separateProbe, fixture.Lines.Count, token, fixture.Name + " async separate");
    }

    private static void VerifyComponentOrder()
    {
        var lines = SummaryFixtures.MixedCategories.Lines;
        var ledger = new List<string>();

        var first = Summary.Create<OrderLine, int, int>(
            0,
            (count, line) =>
            {
                ledger.Add("first:" + line.Sku);
                return count + 1;
            },
            count =>
            {
                ledger.Add("first-result");
                return count;
            });

        var second = Summary.Create<OrderLine, int, int>(
            0,
            (count, line) =>
            {
                ledger.Add("second:" + line.Sku);
                return count + 1;
            },
            count =>
            {
                ledger.Add("second-result");
                return count;
            });

        var result = Summary.Zip(first, second).Evaluate(lines);
        Equal(lines.Count, result.First, "component order first result");
        Equal(lines.Count, result.Second, "component order second result");

        var expected = new List<string>();
        foreach (var line in lines)
        {
            expected.Add("first:" + line.Sku);
            expected.Add("second:" + line.Sku);
        }

        expected.Add("first-result");
        expected.Add("second-result");

        Equal(expected.Count, ledger.Count, "component order ledger length");
        for (var index = 0; index < expected.Count; index++)
        {
            Equal(expected[index], ledger[index], "component order ledger entry " + index);
        }
    }

    private static void VerifyComponentException()
    {
        var lines = SummaryFixtures.MixedCategories.Lines;
        var ledger = new List<string>();
        var fault = new InvalidOperationException("component fault");

        var first = Summary.Create<OrderLine, int>(
            0,
            (count, line) =>
            {
                ledger.Add("first:" + line.Sku);
                if (count == 1)
                {
                    throw fault;
                }

                return count + 1;
            });

        var second = Summary.Create<OrderLine, int>(
            0,
            (count, line) =>
            {
                ledger.Add("second:" + line.Sku);
                return count + 1;
            });

        var probe = new ReductionProbe<OrderLine>(lines);
        Exception? thrown = null;
        try
        {
            _ = Summary.Zip(first, second).Evaluate(probe);
        }
        catch (Exception exception)
        {
            thrown = exception;
        }

        True(ReferenceEquals(fault, thrown), "component exception must propagate unchanged");
        Equal(3, ledger.Count, "component exception ledger length");
        Equal("first:" + lines[0].Sku, ledger[0], "component exception first item");
        Equal("second:" + lines[0].Sku, ledger[1], "component exception second component");
        Equal("first:" + lines[1].Sku, ledger[2], "component exception faulting item");
        Equal(2, probe.MoveNextCalls, "component exception MoveNext");
        Equal(1, probe.DisposeCalls, "component exception disposal");
    }

    private static void VerifyEarlyDeterminedComponent()
    {
        var lines = SummaryFixtures.MixedCategories.Lines;

        var firstOnly = Summary.Create<OrderLine, Option<string>>(
            Option<string>.None,
            static (first, line) => first.IsSome ? first : Option<string>.Some(line.Sku));

        var probe = new ReductionProbe<OrderLine>(lines);
        var result = Summary.Zip(firstOnly, Summary<OrderLine>.Count()).Evaluate(probe);

        Equal(Option.Some(lines[0].Sku), result.First, "early-determined first sku");
        Equal(lines.Count, result.Second, "early-determined full count");
        Equal(lines.Count + 1, probe.MoveNextCalls, "early-determined component must not stop the source");
        Equal(lines.Count, probe.CurrentReads, "early-determined Current reads");
        Equal(1, probe.DisposeCalls, "early-determined disposal");
    }

    private static void AssertSyncProbe(
        ReductionProbe<OrderLine> probe,
        int itemCount,
        string context)
    {
        Equal(1, probe.GetEnumeratorCalls, context + " enumerators");
        Equal(itemCount + 1, probe.MoveNextCalls, context + " MoveNext");
        Equal(itemCount, probe.CurrentReads, context + " Current");
        Equal(1, probe.DisposeCalls, context + " disposals");
    }

    private static void AssertSeparateSyncProbe(
        ReductionProbe<OrderLine> probe,
        int itemCount,
        string context)
    {
        Equal(6, probe.GetEnumeratorCalls, context + " enumerators");
        Equal(6 * (itemCount + 1), probe.MoveNextCalls, context + " MoveNext");
        Equal(6 * itemCount, probe.CurrentReads, context + " Current");
        Equal(6, probe.DisposeCalls, context + " disposals");
    }

    private static void AssertAsyncProbe(
        AsyncReductionProbe<OrderLine> probe,
        int itemCount,
        CancellationToken token,
        string context)
    {
        Equal(1, probe.GetAsyncEnumeratorCalls, context + " enumerators");
        Equal(itemCount + 1, probe.MoveNextAsyncCalls, context + " MoveNextAsync");
        Equal(itemCount, probe.CurrentReads, context + " Current");
        Equal(1, probe.DisposeAsyncCalls, context + " disposals");
        Equal(token, probe.Token, context + " token");
    }

    private static void AssertSeparateAsyncProbe(
        AsyncReductionProbe<OrderLine> probe,
        int itemCount,
        CancellationToken token,
        string context)
    {
        Equal(6, probe.GetAsyncEnumeratorCalls, context + " enumerators");
        Equal(6 * (itemCount + 1), probe.MoveNextAsyncCalls, context + " MoveNextAsync");
        Equal(6 * itemCount, probe.CurrentReads, context + " Current");
        Equal(6, probe.DisposeAsyncCalls, context + " disposals");
        Equal(token, probe.Token, context + " token");
    }

    private static void Equal(OrderReport expected, OrderReport actual, string description)
    {
        Equal(expected.Totals, actual.Totals, description + " totals");
        Equal(expected.FirstSku, actual.FirstSku, description + " first sku");
        Equal(expected.Categories.Count, actual.Categories.Count, description + " category count");

        for (var index = 0; index < expected.Categories.Count; index++)
        {
            Equal(expected.Categories[index], actual.Categories[index], description + " category " + index);
        }
    }

    internal static void Equal<T>(T expected, T actual, string description)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException(
                $"{description}: expected {expected}, actual {actual}.");
        }
    }

    private static void True(bool condition, string description)
    {
        if (!condition)
        {
            throw new InvalidOperationException(description);
        }
    }
}
