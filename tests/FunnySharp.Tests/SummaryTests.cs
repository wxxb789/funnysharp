using FunnySharp.TestSupport.FallibleReduction;
using FunnySharp.TestSupport.SummaryComparison;

namespace FunnySharp.Tests;

public sealed class SummaryTests
{
    [Fact]
    public void CreateRejectsNullFactoriesDelegatesAndSources()
    {
        var summary = Summary.Create<int, int>(0, static (state, item) => state + item);

        Assert.Throws<ArgumentNullException>(
            () => Summary.Create<int, int>(0, null!));
        Assert.Throws<ArgumentNullException>(
            () => Summary.Create<int, int>((Func<int>)null!, static (state, item) => state + item));
        Assert.Throws<ArgumentNullException>(
            () => Summary.Create<int, int, int>(0, static (state, item) => state + item, null!));
        Assert.Throws<ArgumentNullException>(
            () => summary.Evaluate(null!));
        Assert.Throws<ArgumentNullException>(
            () => summary.MapResult<int>(null!));

        Summary<int, int, int>? missing = null;
        Assert.Throws<ArgumentNullException>(
            () => Summary.Zip(missing!, summary));
        Assert.Throws<ArgumentNullException>(
            () => Summary.Zip(summary, missing!));
    }

    [Fact]
    public void CountReturnsTheExactItemCountAndZeroForAnEmptySource()
    {
        var summary = Summary<int>.Count();

        Assert.Equal(4, summary.Evaluate([1, 2, 3, 4]));
        Assert.Equal(0, summary.Evaluate(Array.Empty<int>()));
        Assert.Equal(2, summary.Evaluate([9, 9]));
    }

    [Fact]
    public void EmptyInputInvokesTheSeedFactoryAndResultOnceWithoutAccumulateCalls()
    {
        var seedCalls = 0;
        var accumulateCalls = 0;
        var resultCalls = 0;
        var summary = Summary.Create<int, int, int>(
            () =>
            {
                seedCalls++;
                return 5;
            },
            (state, item) =>
            {
                accumulateCalls++;
                return state + item;
            },
            state =>
            {
                resultCalls++;
                return state * 2;
            });

        Assert.Equal(10, summary.Evaluate(Array.Empty<int>()));
        Assert.Equal(1, seedCalls);
        Assert.Equal(0, accumulateCalls);
        Assert.Equal(1, resultCalls);

        Assert.Equal(14, summary.Evaluate([2]));
        Assert.Equal(2, seedCalls);
        Assert.Equal(1, accumulateCalls);
        Assert.Equal(2, resultCalls);
    }

    [Fact]
    public void EvaluateEnumeratesOnceInSourceOrderAndDisposesOnce()
    {
        var probe = new ReductionProbe<int>([3, 4, 5]);
        var seen = new List<int>();
        var summary = Summary.Create<int, List<int>>(
            static () => [],
            (state, item) =>
            {
                seen.Add(item);
                return state;
            });

        _ = summary.Evaluate(probe);

        Assert.Equal([3, 4, 5], seen);
        Assert.Equal(1, probe.GetEnumeratorCalls);
        Assert.Equal(4, probe.MoveNextCalls);
        Assert.Equal(3, probe.CurrentReads);
        Assert.Equal(1, probe.DisposeCalls);
    }

    [Fact]
    public void ValueSeedSharesMutableStateAcrossEvaluationsAndIsNeverCloned()
    {
        var seed = new List<int>();
        var summary = Summary.Create<int, List<int>>(
            seed,
            static (state, item) =>
            {
                state.Add(item);
                return state;
            });

        var first = summary.Evaluate([1]);
        var second = summary.Evaluate([2]);

        Assert.Same(seed, first);
        Assert.Same(first, second);
        Assert.Equal([1, 2], second);
    }

    [Fact]
    public void SeedFactoryIsolatesMutableStateAcrossEvaluationsAndZipComponents()
    {
        var summary = Summary.Create<int, List<int>>(
            static () => [],
            static (state, item) =>
            {
                state.Add(item);
                return state;
            });

        var first = summary.Evaluate([1]);
        var second = summary.Evaluate([2]);
        Assert.NotSame(first, second);
        Assert.Equal([1], first);
        Assert.Equal([2], second);

        var zipped = Summary.Zip(summary, summary);
        var (left, right) = zipped.Evaluate([1, 2, 3]);

        Assert.NotSame(left, right);
        Assert.Equal([1, 2, 3], left);
        Assert.Equal([1, 2, 3], right);
    }

    [Fact]
    public void SeedFactoryExceptionPropagatesBeforeEnumeration()
    {
        var fault = new InvalidOperationException("seed fault");
        var probe = new ReductionProbe<int>([1]);
        var summary = Summary.Create<int, int, int>(
            () => throw fault,
            static (state, item) => state + item,
            static state => state);

        var thrown = Assert.Throws<InvalidOperationException>(() => summary.Evaluate(probe));

        Assert.Same(fault, thrown);
        Assert.Equal(0, probe.GetEnumeratorCalls);
        Assert.Equal(0, probe.DisposeCalls);
    }

    [Fact]
    public void NullValueSeedIsAcceptedAndUsedByTheAccumulateDelegate()
    {
        string? seed = null;
        var summary = Summary.Create<int, string>(
            seed!,
            static (state, item) => state ?? item.ToString());

        Assert.Equal("1", summary.Evaluate([1]));
    }

    [Fact]
    public void AccumulateExceptionPropagatesStopsTheSourceAndDisposes()
    {
        var fault = new InvalidOperationException("accumulate fault");
        var probe = new ReductionProbe<int>([1, 2, 3]);
        var summary = Summary.Create<int, int>(
            0,
            (state, item) =>
            {
                if (item == 2)
                {
                    throw fault;
                }

                return state + item;
            });

        var thrown = Assert.Throws<InvalidOperationException>(() => summary.Evaluate(probe));

        Assert.Same(fault, thrown);
        Assert.Equal(2, probe.MoveNextCalls);
        Assert.Equal(2, probe.CurrentReads);
        Assert.Equal(1, probe.DisposeCalls);
    }

    [Fact]
    public void ResultExceptionPropagatesAfterFullTraversalAndDisposes()
    {
        var fault = new InvalidOperationException("result fault");
        var probe = new ReductionProbe<int>([1, 2]);
        var summary = Summary.Create<int, int, int>(
            0,
            static (state, item) => state + item,
            _ => throw fault);

        var thrown = Assert.Throws<InvalidOperationException>(() => summary.Evaluate(probe));

        Assert.Same(fault, thrown);
        Assert.Equal(3, probe.MoveNextCalls);
        Assert.Equal(2, probe.CurrentReads);
        Assert.Equal(1, probe.DisposeCalls);
    }

    [Fact]
    public void SourceExceptionPropagatesAndDisposes()
    {
        var fault = new InvalidOperationException("source fault");
        var probe = new ReductionProbe<int>([1, 2, 3], 1, fault);
        var summary = Summary<int>.Count();

        var thrown = Assert.Throws<InvalidOperationException>(() => summary.Evaluate(probe));

        Assert.Same(fault, thrown);
        Assert.Equal(2, probe.MoveNextCalls);
        Assert.Equal(1, probe.CurrentReads);
        Assert.Equal(1, probe.DisposeCalls);
    }

    [Fact]
    public void DisposalExceptionReplacesAPendingSuccess()
    {
        var disposalFault = new InvalidOperationException("dispose fault");
        var probe = new ReductionProbe<int>([1], disposeException: disposalFault);
        var summary = Summary<int>.Count();

        var thrown = Assert.Throws<InvalidOperationException>(() => summary.Evaluate(probe));

        Assert.Same(disposalFault, thrown);
        Assert.Equal(1, probe.GetEnumeratorCalls);
        Assert.Equal(2, probe.MoveNextCalls);
        Assert.Equal(1, probe.DisposeCalls);
    }

    [Fact]
    public void SumAddsInSourceOrderAndReturnsZeroForEmptyInput()
    {
        Assert.Equal(6, Summary<int>.Sum(static value => value).Evaluate([1, 2, 3]));
        Assert.Equal(0, Summary<int>.Sum(static value => value).Evaluate(Array.Empty<int>()));
        Assert.Equal(7.5m, Summary<decimal>.Sum(static value => value).Evaluate([1.5m, 2.5m, 3.5m]));

        var sourceOrderSensitive = Summary<double>.Sum(static value => value)
            .Evaluate([1e16, 1d, -1e16]);
        Assert.Equal(0d, sourceOrderSensitive);
    }

    [Fact]
    public void SumThrowsOnCheckedIntegralAndDecimalOverflowAndDisposes()
    {
        var integralProbe = new ReductionProbe<int>([int.MaxValue, 1]);
        Assert.Throws<OverflowException>(
            () => Summary<int>.Sum(static value => value).Evaluate(integralProbe));
        Assert.Equal(2, integralProbe.MoveNextCalls);
        Assert.Equal(1, integralProbe.DisposeCalls);

        Assert.Throws<OverflowException>(
            () => Summary<decimal>.Sum(static value => value).Evaluate([decimal.MaxValue, 1m]));
    }

    [Fact]
    public void SumOverflowPropagatesThroughZipAndDisposes()
    {
        var probe = new ReductionProbe<int>([int.MaxValue, 1]);
        var summary = Summary.Zip(
            Summary<int>.Sum(static value => value),
            Summary<int>.Count());

        Assert.Throws<OverflowException>(() => summary.Evaluate(probe));
        Assert.Equal(1, probe.DisposeCalls);
    }

    [Fact]
    public void SumSelectorRunsOncePerItemAndItsExceptionPropagates()
    {
        var calls = 0;
        var fault = new InvalidOperationException("selector fault");
        var summary = Summary<int>.Sum(
            (value) =>
            {
                calls++;
                if (value == 2)
                {
                    throw fault;
                }

                return value;
            });

        var thrown = Assert.Throws<InvalidOperationException>(() => summary.Evaluate([1, 2, 3]));

        Assert.Same(fault, thrown);
        Assert.Equal(2, calls);
    }

    [Fact]
    public void MinAndMaxReturnNoneForEmptyInput()
    {
        Assert.True(Summary<int>.Min(static value => value).Evaluate(Array.Empty<int>()).IsNone);
        Assert.True(Summary<int>.Max(static value => value).Evaluate(Array.Empty<int>()).IsNone);
    }

    [Fact]
    public void MinAndMaxReturnTheExtremaInSourceOrder()
    {
        var source = new[] { 4, 1, 7, 2 };

        var minimum = Summary<int>.Min(static value => value).Evaluate(source);
        var maximum = Summary<int>.Max(static value => value).Evaluate(source);

        Assert.True(minimum.TryGetValue(out var min));
        Assert.Equal(1, min);
        Assert.True(maximum.TryGetValue(out var max));
        Assert.Equal(7, max);
    }

    [Fact]
    public void MinAndMaxSkipNullKeysWithoutInvokingTheComparer()
    {
        var comparer = new NullRejectingComparer();
        var summary = Summary<KeyedItem>.Min(static item => item.Key, comparer);

        var mixed = summary.Evaluate([new KeyedItem(null!), new KeyedItem("b"), new KeyedItem(null!), new KeyedItem("a")]);
        Assert.True(mixed.TryGetValue(out var min));
        Assert.Equal("a", min);
        Assert.Equal(1, comparer.Calls);

        comparer.Reset();
        var allNull = summary.Evaluate([new KeyedItem(null!), new KeyedItem(null!)]);
        Assert.True(allNull.IsNone);
        Assert.Equal(0, comparer.Calls);
    }

    [Fact]
    public void MinAndMaxRetainTheFirstKeyReferenceOnAComparerTie()
    {
        var first = new string('x', 1);
        var second = new string('x', 1);
        Assert.NotSame(first, second);

        var comparer = new AlwaysEqualComparer();
        var items = new[] { new KeyedItem(first), new KeyedItem(second) };

        var minimum = Summary<KeyedItem>.Min(static item => item.Key, comparer).Evaluate(items);
        var maximum = Summary<KeyedItem>.Max(static item => item.Key, comparer).Evaluate(items);

        Assert.True(minimum.TryGetValue(out var min));
        Assert.Same(first, min);
        Assert.True(maximum.TryGetValue(out var max));
        Assert.Same(first, max);
    }

    [Fact]
    public void MinAndMaxRejectNullSelectorsAndComparers()
    {
        Assert.Throws<ArgumentNullException>(
            () => Summary<int>.Min<int>(null!));
        Assert.Throws<ArgumentNullException>(
            () => Summary<int>.Min(static value => value, (IComparer<int>)null!));
        Assert.Throws<ArgumentNullException>(
            () => Summary<int>.Max<int>(null!));
        Assert.Throws<ArgumentNullException>(
            () => Summary<int>.Max(static value => value, (IComparer<int>)null!));
        Assert.Throws<ArgumentNullException>(
            () => Summary<int>.Sum<int>(null!));
    }

    [Fact]
    public void MapResultSelectorExceptionPropagatesAfterTraversal()
    {
        var fault = new InvalidOperationException("map fault");
        var summary = Summary<int>.Count().MapResult<int>(_ => throw fault);

        var thrown = Assert.Throws<InvalidOperationException>(() => summary.Evaluate([1, 2]));

        Assert.Same(fault, thrown);
    }

    [Fact]
    public void ComparerExceptionPropagatesFromAnExtremumSummary()
    {
        var fault = new InvalidOperationException("comparer fault");
        var summary = Summary<int>.Min(static value => value, new ThrowingComparer(fault));

        var thrown = Assert.Throws<InvalidOperationException>(() => summary.Evaluate([2, 1]));

        Assert.Same(fault, thrown);
    }

    [Fact]
    public void ZipComposesFlatTwoThreeAndFourSummaryResults()
    {
        var pair = Summary.Zip(Summary<int>.Count(), Summary<int>.Sum(static value => value));
        var pairResult = pair.Evaluate([2, 1, 3]);
        Assert.Equal(3, pairResult.First);
        Assert.Equal(6, pairResult.Second);

        var triple = Summary.Zip(
            Summary<int>.Count(),
            Summary<int>.Sum(static value => value),
            Summary<int>.Min(static value => value));
        var tripleResult = triple.Evaluate([2, 1, 3]);
        Assert.Equal(3, tripleResult.First);
        Assert.Equal(6, tripleResult.Second);
        Assert.Equal(Option.Some(1), tripleResult.Third);

        var quadruple = Summary.Zip(
            Summary<int>.Count(),
            Summary<int>.Sum(static value => value),
            Summary<int>.Min(static value => value),
            Summary<int>.Max(static value => value));
        var quadrupleResult = quadruple.Evaluate([2, 1, 3]);
        Assert.Equal(3, quadrupleResult.First);
        Assert.Equal(6, quadrupleResult.Second);
        Assert.Equal(Option.Some(1), quadrupleResult.Third);
        Assert.Equal(Option.Some(3), quadrupleResult.Fourth);
    }

    [Fact]
    public void MapResultRunsOnceAfterTraversal()
    {
        var calls = 0;
        var summary = Summary.Zip(
                Summary<int>.Count(),
                Summary<int>.Sum(static value => value))
            .MapResult(results =>
            {
                calls++;
                return results.First + results.Second;
            });

        Assert.Equal(5, summary.Evaluate([1, 2]));
        Assert.Equal(1, calls);
    }

    [Fact]
    public void MapResultSelectorIsNotInvokedWhenAComponentThrows()
    {
        var fault = new InvalidOperationException("component fault");
        var resultCalls = 0;
        var throwing = Summary.Create<int, int>(0, (state, item) => throw fault);
        var summary = Summary.Zip(Summary<int>.Count(), throwing)
            .MapResult(_ =>
            {
                resultCalls++;
                return 0;
            });

        var thrown = Assert.Throws<InvalidOperationException>(() => summary.Evaluate([1]));

        Assert.Same(fault, thrown);
        Assert.Equal(0, resultCalls);
    }

    [Fact]
    public void LeafEvaluationAllocatesNothingBeyondTheSourceEnumerator()
    {
        IEnumerable<int> source = [1, 2, 3];
        var summary = Summary.Create<int, int>(0, static (state, item) => state + item);

        for (var index = 0; index < 100; index++)
        {
            _ = summary.Evaluate(source);
            _ = Count(source);
        }

        var baselineStart = GC.GetAllocatedBytesForCurrentThread();
        _ = Count(source);
        var baseline = GC.GetAllocatedBytesForCurrentThread() - baselineStart;

        var start = GC.GetAllocatedBytesForCurrentThread();
        _ = summary.Evaluate(source);
        var actual = GC.GetAllocatedBytesForCurrentThread() - start;

        Assert.Equal(baseline, actual);

        static int Count(IEnumerable<int> values)
        {
            var count = 0;
            foreach (var _ in values)
            {
                count++;
            }

            return count;
        }
    }

    private sealed record KeyedItem(string Key);

    private sealed class NullRejectingComparer : IComparer<string>
    {
        internal int Calls { get; private set; }

        public int Compare(string? x, string? y)
        {
            Calls++;
            if (x is null || y is null)
            {
                throw new InvalidOperationException("The comparer must not observe a null key.");
            }

            return string.CompareOrdinal(x, y);
        }

        internal void Reset() => Calls = 0;
    }

    private sealed class AlwaysEqualComparer : IComparer<string>
    {
        public int Compare(string? x, string? y) => 0;
    }

    private sealed class ThrowingComparer(Exception fault) : IComparer<int>
    {
        public int Compare(int x, int y) => throw fault;
    }
}
