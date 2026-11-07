using FunnySharp;

namespace FunnySharp.TestSupport.FallibleReduction;

internal static class ReductionComparison
{
    internal static void Verify()
    {
        foreach (var fixture in ReductionFixtures.All)
        {
            VerifySyncFixture(fixture);
        }

        VerifySyncSourceFaultAfterFailure();
    }

    internal static async Task VerifyAsync()
    {
        foreach (var fixture in ReductionFixtures.All)
        {
            await VerifyAsyncFixture(fixture).ConfigureAwait(false);
        }

        await VerifyAsyncSourceFaultAfterFailure().ConfigureAwait(false);
    }

    private static void VerifySyncFixture(ReductionFixture fixture)
    {
        var (reached, stoppingMoveNext, completed) = ReductionRules.Expectations(fixture.Lines);

        var ordinaryProbe = new ReductionProbe<ReductionLine>(fixture.Lines);
        var ordinarySteps = 0;
        var ordinary = OrdinaryReductionConsumer.Run(
            ordinaryProbe,
            (total, line) =>
            {
                ordinarySteps++;
                return ReductionRules.Add(total, line);
            });

        var funnyProbe = new ReductionProbe<ReductionLine>(fixture.Lines);
        var funnySteps = 0;
        var funny = FunnySharpReductionConsumer.Run(
            funnyProbe,
            (total, line) =>
            {
                funnySteps++;
                return ReductionRules.Add(total, line);
            });

        var aggregateProbe = new ReductionProbe<ReductionLine>(fixture.Lines);
        var aggregateSteps = 0;
        var aggregate = AggregateOverResultConsumer.Run(
            aggregateProbe,
            (total, line) =>
            {
                aggregateSteps++;
                return ReductionRules.Add(total, line);
            });

        Equal(ordinary, funny, fixture.Name + " outcome");
        Equal(ordinary, aggregate, fixture.Name + " aggregate outcome");
        Equal(reached, ordinarySteps, fixture.Name + " ordinary step calls");
        Equal(reached, funnySteps, fixture.Name + " funny step calls");
        Equal(reached, aggregateSteps, fixture.Name + " aggregate step calls");

        AssertSyncStoppingProbe(ordinaryProbe, stoppingMoveNext, reached, fixture.Name + " ordinary");
        AssertSyncStoppingProbe(funnyProbe, stoppingMoveNext, reached, fixture.Name + " funny");
        Equal(fixture.Lines.Count + 1, aggregateProbe.MoveNextCalls, fixture.Name + " aggregate MoveNext");
        Equal(fixture.Lines.Count, aggregateProbe.CurrentReads, fixture.Name + " aggregate Current");
        Equal(1, aggregateProbe.GetEnumeratorCalls, fixture.Name + " aggregate enumerators");
        Equal(1, aggregateProbe.DisposeCalls, fixture.Name + " aggregate disposals");

        if (!completed)
        {
            True(
                aggregateProbe.MoveNextCalls > funnyProbe.MoveNextCalls,
                fixture.Name + " aggregate must keep consuming after the typed failure");
        }
    }

    private static void VerifySyncSourceFaultAfterFailure()
    {
        var fault = new InvalidOperationException("source fault after domain failure");
        var lines = ReductionFixtures.SourceFaultAfterFailure;
        var reached = ReductionRules.ReachedCount(lines);

        var ordinaryProbe = new ReductionProbe<ReductionLine>(
            lines,
            ReductionFixtures.SourceFaultIndex,
            fault);
        var ordinary = OrdinaryReductionConsumer.Run(ordinaryProbe, ReductionRules.Add);

        var funnyProbe = new ReductionProbe<ReductionLine>(
            lines,
            ReductionFixtures.SourceFaultIndex,
            fault);
        var funny = FunnySharpReductionConsumer.Run(funnyProbe, ReductionRules.Add);

        var aggregateProbe = new ReductionProbe<ReductionLine>(
            lines,
            ReductionFixtures.SourceFaultIndex,
            fault);
        Exception? thrown = null;
        try
        {
            _ = AggregateOverResultConsumer.Run(aggregateProbe, ReductionRules.Add);
        }
        catch (Exception exception)
        {
            thrown = exception;
        }

        var expectedError = new ReductionError("quantity", "SKU-BAD|0");
        AssertFailure(ordinary, expectedError, "source fault ordinary outcome");
        AssertFailure(funny, expectedError, "source fault funny outcome");
        Equal(ordinary, funny, "source fault outcome parity");
        True(ReferenceEquals(fault, thrown), "the aggregate baseline must surface the later source fault");

        Equal(reached, ordinaryProbe.MoveNextCalls, "source fault ordinary MoveNext");
        Equal(reached, funnyProbe.MoveNextCalls, "source fault funny MoveNext");
        Equal(ReductionFixtures.SourceFaultIndex + 1, aggregateProbe.MoveNextCalls, "source fault aggregate MoveNext");
        Equal(reached, ordinaryProbe.CurrentReads, "source fault ordinary Current");
        Equal(reached, funnyProbe.CurrentReads, "source fault funny Current");
        Equal(ReductionFixtures.SourceFaultIndex, aggregateProbe.CurrentReads, "source fault aggregate Current");
        Equal(1, ordinaryProbe.DisposeCalls, "source fault ordinary disposals");
        Equal(1, funnyProbe.DisposeCalls, "source fault funny disposals");
        Equal(1, aggregateProbe.DisposeCalls, "source fault aggregate disposals");
    }

    private static async Task VerifyAsyncFixture(ReductionFixture fixture)
    {
        var (reached, stoppingMoveNext, completed) = ReductionRules.Expectations(fixture.Lines);
        using var cancellationSource = new CancellationTokenSource();
        var token = cancellationSource.Token;

        var ordinaryProbe = new AsyncReductionProbe<ReductionLine>(fixture.Lines);
        var ordinarySteps = 0;
        var ordinary = await OrdinaryReductionConsumer.RunAsync(
            ordinaryProbe,
            (total, line) =>
            {
                ordinarySteps++;
                return ReductionRules.Add(total, line);
            },
            token).ConfigureAwait(false);

        var funnyProbe = new AsyncReductionProbe<ReductionLine>(fixture.Lines);
        var funnySteps = 0;
        var funny = await FunnySharpReductionConsumer.RunAsync(
            funnyProbe,
            (total, line) =>
            {
                funnySteps++;
                return ReductionRules.Add(total, line);
            },
            token).ConfigureAwait(false);

        var aggregateProbe = new AsyncReductionProbe<ReductionLine>(fixture.Lines);
        var aggregateSteps = 0;
        var aggregate = await AggregateOverResultConsumer.RunAsync(
            aggregateProbe,
            (total, line) =>
            {
                aggregateSteps++;
                return ReductionRules.Add(total, line);
            },
            token).ConfigureAwait(false);

        var valueProbe = new AsyncReductionProbe<ReductionLine>(fixture.Lines);
        var valueSteps = 0;
        var value = await OrdinaryReductionConsumer.RunValueAsync(
            valueProbe,
            (total, line) =>
            {
                valueSteps++;
                return ReductionRules.AddValueAsync(total, line);
            },
            token).ConfigureAwait(false);

        var funnyValueProbe = new AsyncReductionProbe<ReductionLine>(fixture.Lines);
        var funnyValueSteps = 0;
        var funnyValue = await FunnySharpReductionConsumer.RunValueAsync(
            funnyValueProbe,
            (total, line) =>
            {
                funnyValueSteps++;
                return ReductionRules.AddValueAsync(total, line);
            },
            token).ConfigureAwait(false);

        var tokenAwareProbe = new AsyncReductionProbe<ReductionLine>(fixture.Lines);
        var tokenAwareSteps = 0;
        var observedStepToken = default(CancellationToken);
        var tokenAware = await FunnySharpReductionConsumer.RunTokenAwareValueAsync(
            tokenAwareProbe,
            (total, line, stepToken) =>
            {
                tokenAwareSteps++;
                observedStepToken = stepToken;
                return ReductionRules.AddValueAsync(total, line);
            },
            token).ConfigureAwait(false);

        Equal(ordinary, funny, fixture.Name + " async outcome");
        Equal(ordinary, aggregate, fixture.Name + " async aggregate outcome");
        Equal(ordinary, value, fixture.Name + " value outcome");
        Equal(ordinary, funnyValue, fixture.Name + " funny value outcome");
        Equal(ordinary, tokenAware, fixture.Name + " token-aware outcome");
        Equal(reached, ordinarySteps, fixture.Name + " async ordinary step calls");
        Equal(reached, funnySteps, fixture.Name + " async funny step calls");
        Equal(reached, aggregateSteps, fixture.Name + " async aggregate step calls");
        Equal(reached, valueSteps, fixture.Name + " value step calls");
        Equal(reached, funnyValueSteps, fixture.Name + " funny value step calls");
        Equal(reached, tokenAwareSteps, fixture.Name + " token-aware step calls");
        if (reached > 0)
        {
            Equal(token, observedStepToken, fixture.Name + " token-aware step token");
        }

        AssertAsyncStoppingProbe(ordinaryProbe, stoppingMoveNext, reached, token, fixture.Name + " async ordinary");
        AssertAsyncStoppingProbe(funnyProbe, stoppingMoveNext, reached, token, fixture.Name + " async funny");
        AssertAsyncStoppingProbe(valueProbe, stoppingMoveNext, reached, token, fixture.Name + " async value");
        AssertAsyncStoppingProbe(funnyValueProbe, stoppingMoveNext, reached, token, fixture.Name + " funny value");
        AssertAsyncStoppingProbe(tokenAwareProbe, stoppingMoveNext, reached, token, fixture.Name + " token-aware");
        Equal(fixture.Lines.Count + 1, aggregateProbe.MoveNextAsyncCalls, fixture.Name + " async aggregate MoveNext");
        Equal(fixture.Lines.Count, aggregateProbe.CurrentReads, fixture.Name + " async aggregate Current");
        Equal(1, aggregateProbe.GetAsyncEnumeratorCalls, fixture.Name + " async aggregate enumerators");
        Equal(1, aggregateProbe.DisposeAsyncCalls, fixture.Name + " async aggregate disposals");
        Equal(token, aggregateProbe.Token, fixture.Name + " async aggregate token");

        if (!completed)
        {
            True(
                aggregateProbe.MoveNextAsyncCalls > funnyProbe.MoveNextAsyncCalls,
                fixture.Name + " async aggregate must keep consuming after the typed failure");
        }
    }

    private static async Task VerifyAsyncSourceFaultAfterFailure()
    {
        var fault = new InvalidOperationException("async source fault after domain failure");
        var lines = ReductionFixtures.SourceFaultAfterFailure;
        var reached = ReductionRules.ReachedCount(lines);
        using var cancellationSource = new CancellationTokenSource();
        var token = cancellationSource.Token;

        var ordinaryProbe = new AsyncReductionProbe<ReductionLine>(
            lines,
            ReductionFixtures.SourceFaultIndex,
            fault);
        var ordinary = await OrdinaryReductionConsumer.RunAsync(
            ordinaryProbe,
            ReductionRules.Add,
            token).ConfigureAwait(false);

        var funnyProbe = new AsyncReductionProbe<ReductionLine>(
            lines,
            ReductionFixtures.SourceFaultIndex,
            fault);
        var funny = await FunnySharpReductionConsumer.RunAsync(
            funnyProbe,
            ReductionRules.Add,
            token).ConfigureAwait(false);

        var aggregateProbe = new AsyncReductionProbe<ReductionLine>(
            lines,
            ReductionFixtures.SourceFaultIndex,
            fault);
        Exception? thrown = null;
        try
        {
            _ = await AggregateOverResultConsumer.RunAsync(
                aggregateProbe,
                ReductionRules.Add,
                token).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            thrown = exception;
        }

        var expectedError = new ReductionError("quantity", "SKU-BAD|0");
        AssertFailure(ordinary, expectedError, "async source fault ordinary outcome");
        AssertFailure(funny, expectedError, "async source fault funny outcome");
        Equal(ordinary, funny, "async source fault outcome parity");
        True(ReferenceEquals(fault, thrown), "the async aggregate baseline must surface the later source fault");

        Equal(reached, ordinaryProbe.MoveNextAsyncCalls, "async source fault ordinary MoveNext");
        Equal(reached, funnyProbe.MoveNextAsyncCalls, "async source fault funny MoveNext");
        Equal(
            ReductionFixtures.SourceFaultIndex + 1,
            aggregateProbe.MoveNextAsyncCalls,
            "async source fault aggregate MoveNext");
        Equal(reached, ordinaryProbe.CurrentReads, "async source fault ordinary Current");
        Equal(reached, funnyProbe.CurrentReads, "async source fault funny Current");
        Equal(
            ReductionFixtures.SourceFaultIndex,
            aggregateProbe.CurrentReads,
            "async source fault aggregate Current");
        Equal(1, ordinaryProbe.DisposeAsyncCalls, "async source fault ordinary disposals");
        Equal(1, funnyProbe.DisposeAsyncCalls, "async source fault funny disposals");
        Equal(1, aggregateProbe.DisposeAsyncCalls, "async source fault aggregate disposals");
        Equal(token, ordinaryProbe.Token, "async source fault ordinary token");
        Equal(token, funnyProbe.Token, "async source fault funny token");
    }

    private static void AssertSyncStoppingProbe(
        ReductionProbe<ReductionLine> probe,
        int moveNextCalls,
        int currentReads,
        string scenario)
    {
        Equal(1, probe.GetEnumeratorCalls, scenario + " enumerators");
        Equal(moveNextCalls, probe.MoveNextCalls, scenario + " MoveNext");
        Equal(currentReads, probe.CurrentReads, scenario + " Current");
        Equal(1, probe.DisposeCalls, scenario + " disposals");
    }

    private static void AssertAsyncStoppingProbe(
        AsyncReductionProbe<ReductionLine> probe,
        int moveNextCalls,
        int currentReads,
        CancellationToken token,
        string scenario)
    {
        Equal(1, probe.GetAsyncEnumeratorCalls, scenario + " enumerators");
        Equal(moveNextCalls, probe.MoveNextAsyncCalls, scenario + " MoveNext");
        Equal(currentReads, probe.CurrentReads, scenario + " Current");
        Equal(1, probe.DisposeAsyncCalls, scenario + " disposals");
        Equal(token, probe.Token, scenario + " token");
    }

    private static void AssertFailure(
        Result<ReductionTotal, ReductionError> result,
        ReductionError expected,
        string scenario)
    {
        if (!result.TryGetError(out var actual))
        {
            throw new InvalidOperationException(scenario + ": expected a typed failure.");
        }

        Equal(expected, actual!, scenario);
    }

    private static void Equal<T>(T expected, T actual, string scenario)
        where T : notnull
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException(
                $"{scenario}: expected '{expected}', received '{actual}'.");
        }
    }

    private static void True(bool condition, string scenario)
    {
        if (!condition)
        {
            throw new InvalidOperationException(scenario);
        }
    }
}
