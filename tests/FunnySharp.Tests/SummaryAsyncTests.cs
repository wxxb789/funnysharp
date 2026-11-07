using FunnySharp.TestSupport.FallibleReduction;
using FunnySharp.TestSupport.SummaryComparison;

namespace FunnySharp.Tests;

public sealed class SummaryAsyncTests
{
    [Fact]
    public async Task EvaluateAsyncMatchesTheSynchronousEvaluationAndConsumesOnce()
    {
        var summary = Summary.Create<int, int>(0, static (state, item) => state + item);
        var syncProbe = new ReductionProbe<int>([1, 2, 3]);
        var asyncProbe = new AsyncReductionProbe<int>([1, 2, 3]);
        using var cancellation = new CancellationTokenSource();

        var sync = summary.Evaluate(syncProbe);
        var asynchronous = await summary.EvaluateAsync(asyncProbe, cancellation.Token);

        Assert.Equal(sync, asynchronous);
        Assert.Equal(1, asyncProbe.GetAsyncEnumeratorCalls);
        Assert.Equal(4, asyncProbe.MoveNextAsyncCalls);
        Assert.Equal(3, asyncProbe.CurrentReads);
        Assert.Equal(1, asyncProbe.DisposeAsyncCalls);
        Assert.Equal(cancellation.Token, asyncProbe.Token);
    }

    [Fact]
    public async Task EvaluateAsyncEmptyInputInvokesTheSeedAndResultWithoutAccumulateCalls()
    {
        var seedCalls = 0;
        var accumulateCalls = 0;
        var summary = Summary.Create<int, int>(
            () =>
            {
                seedCalls++;
                return 5;
            },
            (state, item) =>
            {
                accumulateCalls++;
                return state + item;
            });
        var probe = new AsyncReductionProbe<int>([]);

        Assert.Equal(5, await summary.EvaluateAsync(probe, TestContext.Current.CancellationToken));
        Assert.Equal(1, seedCalls);
        Assert.Equal(0, accumulateCalls);
        Assert.Equal(1, probe.GetAsyncEnumeratorCalls);
        Assert.Equal(1, probe.MoveNextAsyncCalls);
        Assert.Equal(0, probe.CurrentReads);
        Assert.Equal(1, probe.DisposeAsyncCalls);
    }

    [Fact]
    public async Task TheSameSummaryDefinitionIsReusableAcrossSynchronousAndAsynchronousSources()
    {
        var summary = Summary.Create<int, int>(0, static (state, item) => state + item);

        var synchronous = summary.Evaluate([1, 2, 3]);
        var asynchronous = await summary.EvaluateAsync(new AsyncReductionProbe<int>([1, 2, 3]), TestContext.Current.CancellationToken);
        var repeated = await summary.EvaluateAsync(new AsyncReductionProbe<int>([1, 2, 3]), TestContext.Current.CancellationToken);

        Assert.Equal(6, synchronous);
        Assert.Equal(synchronous, asynchronous);
        Assert.Equal(asynchronous, repeated);
    }

    [Fact]
    public async Task EvaluateAsyncForwardsTheExactTokenToTheEnumerator()
    {
        using var cancellation = new CancellationTokenSource();
        var probe = new AsyncReductionProbe<int>([1]);
        var summary = Summary<int>.Count();

        _ = await summary.EvaluateAsync(probe, cancellation.Token);

        Assert.Equal(cancellation.Token, probe.Token);
    }

    [Fact]
    public async Task AlreadyCanceledTokenIsNotInspectedEagerly()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var probe = new AsyncReductionProbe<int>([1, 2]);
        var summary = Summary<int>.Count();

        Assert.Equal(2, await summary.EvaluateAsync(probe, cancellation.Token));
        Assert.Equal(cancellation.Token, probe.Token);
    }

    [Fact]
    public async Task CanceledSourceRemainsCanceledAndDisposesTheEnumerator()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var source = new CanceledAsyncEnumerable();
        var summary = Summary<int>.Count();

        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => summary.EvaluateAsync(source, cancellation.Token).AsTask());

        Assert.Equal(cancellation.Token, exception.CancellationToken);
        Assert.Equal(1, source.DisposeCalls);
    }

    [Fact]
    public async Task SumOverflowPropagatesThroughEvaluateAsyncAndDisposes()
    {
        var probe = new AsyncReductionProbe<int>([int.MaxValue, 1]);
        var summary = Summary<int>.Sum(static value => value);

        var thrown = await Assert.ThrowsAsync<OverflowException>(
            () => summary.EvaluateAsync(probe, TestContext.Current.CancellationToken).AsTask());

        Assert.IsType<OverflowException>(thrown);
        Assert.Equal(1, probe.DisposeAsyncCalls);
    }

    [Fact]
    public async Task SourceExceptionPropagatesAndDisposes()
    {
        var fault = new InvalidOperationException("source fault");
        var probe = new AsyncReductionProbe<int>([1, 2, 3], 1, fault);
        var summary = Summary<int>.Count();

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(
            () => summary.EvaluateAsync(probe, TestContext.Current.CancellationToken).AsTask());

        Assert.Same(fault, thrown);
        Assert.Equal(2, probe.MoveNextAsyncCalls);
        Assert.Equal(1, probe.CurrentReads);
        Assert.Equal(1, probe.DisposeAsyncCalls);
    }

    [Fact]
    public async Task AccumulateExceptionFaultsTheOperationAndDisposes()
    {
        var fault = new InvalidOperationException("accumulate fault");
        var probe = new AsyncReductionProbe<int>([1, 2, 3]);
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

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(
            () => summary.EvaluateAsync(probe, TestContext.Current.CancellationToken).AsTask());

        Assert.Same(fault, thrown);
        Assert.Equal(2, probe.MoveNextAsyncCalls);
        Assert.Equal(2, probe.CurrentReads);
        Assert.Equal(1, probe.DisposeAsyncCalls);
    }

    [Fact]
    public async Task DisposalExceptionReplacesAPendingAsyncResult()
    {
        var disposalFault = new InvalidOperationException("dispose fault");
        var probe = new AsyncReductionProbe<int>([1], disposeException: disposalFault);
        var summary = Summary<int>.Count();

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(
            () => summary.EvaluateAsync(probe, TestContext.Current.CancellationToken).AsTask());

        Assert.Same(disposalFault, thrown);
        Assert.Equal(1, probe.DisposeAsyncCalls);
    }

    [Fact]
    public async Task PendingSourceDoesNotBlockAndCompletesWhenReleased()
    {
        var source = new GatedAsyncEnumerable<int>(7);
        var summary = Summary<int>.Count();

        var operation = summary.EvaluateAsync(source, TestContext.Current.CancellationToken);

        Assert.False(operation.IsCompleted);
        source.Release();
        Assert.Equal(1, await operation);
        Assert.Equal(1, source.DisposeCalls);
    }

    [Fact]
    public async Task SeedFactoryExceptionFaultsTheAsyncOperationBeforeEnumeration()
    {
        var fault = new InvalidOperationException("seed fault");
        var probe = new AsyncReductionProbe<int>([1]);
        var summary = Summary.Create<int, int, int>(
            () => throw fault,
            static (state, item) => state + item,
            static state => state);

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(
            () => summary.EvaluateAsync(probe, TestContext.Current.CancellationToken).AsTask());

        Assert.Same(fault, thrown);
        Assert.Equal(0, probe.GetAsyncEnumeratorCalls);
        Assert.Equal(0, probe.DisposeAsyncCalls);
    }

    [Fact]
    public void NullAsyncSourceIsRejectedBeforeEnumeration()
    {
        var summary = Summary<int>.Count();

        Assert.Throws<ArgumentNullException>(() => summary.EvaluateAsync(null!, TestContext.Current.CancellationToken));
    }

    private sealed class CanceledAsyncEnumerable : IAsyncEnumerable<int>
    {
        internal int DisposeCalls { get; private set; }

        public IAsyncEnumerator<int> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
            new Enumerator(this, cancellationToken);

        private sealed class Enumerator(
            CanceledAsyncEnumerable owner,
            CancellationToken token) : IAsyncEnumerator<int>
        {
            public int Current => 0;

            public ValueTask<bool> MoveNextAsync() => ValueTask.FromCanceled<bool>(token);

            public ValueTask DisposeAsync()
            {
                owner.DisposeCalls++;
                return ValueTask.CompletedTask;
            }
        }
    }

    private sealed class GatedAsyncEnumerable<T>(T value) : IAsyncEnumerable<T>
    {
        private readonly TaskCompletionSource<bool> gate =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        internal int DisposeCalls { get; private set; }

        internal void Release() => gate.SetResult(true);

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
            new Enumerator(this, value);

        private sealed class Enumerator : IAsyncEnumerator<T>
        {
            private readonly GatedAsyncEnumerable<T> owner;
            private readonly T value;
            private int index = -1;

            internal Enumerator(GatedAsyncEnumerable<T> owner, T value)
            {
                this.owner = owner;
                this.value = value;
            }

            public T Current => value;

            public async ValueTask<bool> MoveNextAsync()
            {
                if (index >= 0)
                {
                    return false;
                }

                await owner.gate.Task.ConfigureAwait(false);
                index = 0;
                return true;
            }

            public ValueTask DisposeAsync()
            {
                owner.DisposeCalls++;
                return ValueTask.CompletedTask;
            }
        }
    }
}
