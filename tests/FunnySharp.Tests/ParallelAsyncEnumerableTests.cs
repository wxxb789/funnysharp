using System.Collections.Generic;

namespace FunnySharp.Tests;

#pragma warning disable xUnit1051

public sealed class ParallelAsyncEnumerableTests
{
    private static readonly TimeSpan GateTimeout = TimeSpan.FromSeconds(5);

    [Fact]
    public void SelectParallelValueAsyncRejectsInvalidArgumentsEagerly()
    {
        IAsyncEnumerable<int>? source = null;

        Assert.Throws<ArgumentNullException>(() => source!.SelectParallelValueAsync(
            1,
            static value => ValueTask.FromResult(value)));
        Assert.Throws<ArgumentOutOfRangeException>(() => AsyncValues(1).SelectParallelValueAsync(
            0,
            static value => ValueTask.FromResult(value)));
        Assert.Throws<ArgumentOutOfRangeException>(() => AsyncValues(1).SelectParallelValueAsync(
            -1,
            static value => ValueTask.FromResult(value)));
        Assert.Throws<ArgumentNullException>(() => AsyncValues(1).SelectParallelValueAsync(
            1,
            (Func<int, ValueTask<int>>)null!));
        Assert.Throws<ArgumentNullException>(() => AsyncValues(1).SelectParallelValueAsync(
            1,
            (Func<int, CancellationToken, ValueTask<int>>)null!));
    }

    [Fact]
    public async Task SelectParallelValueAsyncDefersSourceAndSelectorUntilFirstPull()
    {
        var source = new ProbeAsyncEnumerable<int>([1]);
        var selectorCalls = 0;
        var pipeline = source.SelectParallelValueAsync(2, value =>
        {
            selectorCalls++;
            return ValueTask.FromResult(value * 10);
        });

        Assert.Equal(0, source.EnumeratorCount);
        Assert.Equal(0, selectorCalls);

        await using var enumerator = pipeline.GetAsyncEnumerator();

        Assert.Equal(0, source.EnumeratorCount);
        Assert.Equal(0, selectorCalls);
        Assert.True(await enumerator.MoveNextAsync());
        Assert.Equal(10, enumerator.Current);
        Assert.Equal(1, source.EnumeratorCount);
        Assert.Equal(1, source.ItemsYielded);
        Assert.Equal(1, selectorCalls);
    }

    [Fact]
    public async Task SelectParallelValueAsyncRejectsConcurrentMoveNextCalls()
    {
        var selectorStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var selector = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var pipeline = AsyncValues(1).SelectParallelValueAsync(1, _ =>
        {
            selectorStarted.TrySetResult();
            return new ValueTask<int>(selector.Task);
        });
        await using var enumerator = pipeline.GetAsyncEnumerator();
        var firstMove = enumerator.MoveNextAsync().AsTask();

        await selectorStarted.Task.WaitAsync(GateTimeout);
        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            enumerator.MoveNextAsync().AsTask());

        Assert.Equal("Concurrent MoveNextAsync calls are not supported.", actual.Message);
        selector.SetResult(1);
        Assert.True(await firstMove.WaitAsync(GateTimeout));
    }

    [Fact]
    public async Task SelectParallelValueAsyncRejectsMoveNextAfterDisposal()
    {
        var enumerator = AsyncValues(1)
            .SelectParallelValueAsync(1, static value => ValueTask.FromResult(value))
            .GetAsyncEnumerator();

        await enumerator.DisposeAsync();

        _ = await Assert.ThrowsAsync<ObjectDisposedException>(() => enumerator.MoveNextAsync().AsTask());
    }

    [Fact]
    public async Task SelectParallelValueAsyncPreservesOrderAndBoundsUndeliveredWork()
    {
        var source = new ProbeAsyncEnumerable<int>([1, 2, 3]);
        var first = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var second = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var third = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thirdStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<bool>? firstPull = null;
        var thirdStartedBeforeFirstDelivery = false;

        var pipeline = source.SelectParallelValueAsync(2, value =>
        {
            switch (value)
            {
                case 1:
                    firstStarted.TrySetResult();
                    return new ValueTask<int>(first.Task);
                case 2:
                    secondStarted.TrySetResult();
                    return new ValueTask<int>(second.Task);
                case 3:
                    thirdStartedBeforeFirstDelivery = firstPull is { IsCompleted: false };
                    thirdStarted.TrySetResult();
                    return new ValueTask<int>(third.Task);
                default:
                    throw new InvalidOperationException("Unexpected source item.");
            }
        });

        await using var enumerator = pipeline.GetAsyncEnumerator();
        firstPull = enumerator.MoveNextAsync().AsTask();

        await firstStarted.Task;
        await secondStarted.Task;
        Assert.Equal(2, source.ItemsYielded);
        Assert.False(thirdStarted.Task.IsCompleted);

        second.SetResult(20);
        Assert.False(firstPull.IsCompleted);

        first.SetResult(10);
        Assert.True(await firstPull);
        Assert.Equal(10, enumerator.Current);
        await thirdStarted.Task;
        Assert.False(thirdStartedBeforeFirstDelivery);

        Assert.True(await enumerator.MoveNextAsync());
        Assert.Equal(20, enumerator.Current);

        third.SetResult(30);
        Assert.True(await enumerator.MoveNextAsync());
        Assert.Equal(30, enumerator.Current);
        Assert.False(await enumerator.MoveNextAsync());
    }

    [Fact]
    public async Task TokenAwareSelectParallelValueAsyncUsesOneCancelableOperationToken()
    {
        var source = new ProbeAsyncEnumerable<int>([1, 2]);
        var selectorTokens = new List<CancellationToken>();

        var values = await source.SelectParallelValueAsync(2, (value, token) =>
        {
            selectorTokens.Add(token);
            return ValueTask.FromResult(value * 10);
        }).ToListAsync();

        Assert.Equal([10, 20], values);
        Assert.True(source.ReceivedToken.CanBeCanceled);
        Assert.Equal(2, selectorTokens.Count);
        Assert.All(selectorTokens, token => Assert.Equal(source.ReceivedToken, token));
        Assert.Equal(1, source.DisposeCount);
    }

    [Fact]
    public async Task ConsumerBreakCancelsStartedSelectorsWaitsForThemAndDisposesSource()
    {
        var source = new ProbeAsyncEnumerable<int>([1, 2]);
        var first = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondCanceled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondFinished = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken secondToken = default;

        var pipeline = source.SelectParallelValueAsync(2, (value, token) =>
        {
            if (value == 1)
            {
                firstStarted.TrySetResult();
                return new ValueTask<int>(first.Task);
            }

            secondStarted.TrySetResult();
            secondToken = token;
            return FinishAfterCancellationAsync(token, secondCanceled, secondFinished.Task);
        });
        var consumption = ConsumeOneAsync(pipeline);

        await firstStarted.Task;
        await secondStarted.Task;
        first.SetResult(1);

        await secondCanceled.Task;
        Assert.True(secondToken.IsCancellationRequested);
        Assert.False(consumption.IsCompleted);

        secondFinished.SetResult(2);
        await consumption;

        Assert.Equal(1, source.DisposeCount);
    }

    [Fact]
    public async Task ExternalCancellationPreservesTheCallerTokenAndWaitsForCleanup()
    {
        using var cancellationSource = new CancellationTokenSource();
        var source = new ProbeAsyncEnumerable<int>([1]);
        var selectorStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var operation = source.SelectParallelValueAsync(
            1,
            (_, token) => WaitForCancellationAsync(token, selectorStarted))
            .ToListAsync(cancellationSource.Token)
            .AsTask();

        await selectorStarted.Task.WaitAsync(GateTimeout);
        cancellationSource.Cancel();

        var actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation.WaitAsync(GateTimeout));

        Assert.True(operation.IsCanceled);
        Assert.Equal(cancellationSource.Token, actual.CancellationToken);
        Assert.Equal(1, source.DisposeCount);
    }

    [Fact]
    public async Task LaterSelectorFaultCancelsAndDrainsAnEarlierSelectorBeforeFailing()
    {
        using var cancellationSource = new CancellationTokenSource();
        var source = new ProbeAsyncEnumerable<int>([0, 1]);
        var earlierCanceled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var earlierFinished = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var laterStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var expected = new InvalidOperationException("later selector");
        var operation = source.SelectParallelValueAsync(2, (value, token) =>
        {
            if (value == 0)
            {
                return FinishAfterCancellationAsync(token, earlierCanceled, earlierFinished.Task);
            }

            laterStarted.TrySetResult();
            return ValueTask.FromException<int>(expected);
        }).ToListAsync(cancellationSource.Token).AsTask();

        try
        {
            await laterStarted.Task.WaitAsync(GateTimeout);
            await earlierCanceled.Task.WaitAsync(GateTimeout);
            Assert.False(operation.IsCompleted);

            earlierFinished.SetResult(0);
            var actual = await Assert.ThrowsAsync<InvalidOperationException>(() => operation.WaitAsync(GateTimeout));

            Assert.Same(expected, actual);
            Assert.Equal(1, source.DisposeCount);
        }
        finally
        {
            cancellationSource.Cancel();
            earlierFinished.TrySetResult(0);
            try
            {
                await operation.WaitAsync(GateTimeout);
            }
            catch (Exception)
            {
            }
        }
    }

    [Fact]
    public async Task SelectorFailureRetainsASourceFaultRaisedDuringCleanup()
    {
        var selectorException = new InvalidOperationException("selector");
        var sourceException = new ArgumentException("source cleanup");
        var source = new FaultAfterCancellationAsyncEnumerable<int>(1, sourceException);
        var selector = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var operation = source.SelectParallelValueAsync(
            2,
            _ => new ValueTask<int>(selector.Task))
            .ToListAsync()
            .AsTask();

        await source.WaitForPendingMoveNextAsync().WaitAsync(GateTimeout);
        selector.SetException(selectorException);

        var actual = await Assert.ThrowsAsync<AggregateException>(() => operation.WaitAsync(GateTimeout));

        Assert.Collection(
            actual.InnerExceptions,
            failure => Assert.Same(selectorException, failure),
            failure => Assert.Same(sourceException, failure));
        Assert.Equal(1, source.DisposeCount);
    }

    [Fact]
    public async Task SelectorFailureDoesNotAdmitAnItemYieldedAfterCancellation()
    {
        var expected = new InvalidOperationException("selector");
        var source = new CancellationIgnoringAsyncEnumerable<int>([0, 1]);
        var firstSelector = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var selectorCalls = new List<int>();
        var operation = source.SelectParallelValueAsync(2, (value, _) =>
        {
            selectorCalls.Add(value);
            return value == 0
                ? new ValueTask<int>(firstSelector.Task)
                : ValueTask.FromResult(value);
        }).ToListAsync().AsTask();

        await source.WaitForPendingMoveNextAsync().WaitAsync(GateTimeout);
        firstSelector.SetException(expected);
        await source.WaitForCancellationAsync().WaitAsync(GateTimeout);
        source.ReleasePendingMoveNext();

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() => operation.WaitAsync(GateTimeout));

        Assert.Same(expected, actual);
        Assert.Equal([0], selectorCalls);
        Assert.Equal(1, source.DisposeCount);
    }

    [Fact]
    public async Task SelectorFailureRetainsAnUnrelatedCanceledSibling()
    {
        using var unrelatedCancellation = new CancellationTokenSource();
        unrelatedCancellation.Cancel();
        var primaryException = new InvalidOperationException("primary");
        var siblingCancellation = new OperationCanceledException(unrelatedCancellation.Token);
        var primary = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var siblingStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var operation = AsyncValues(0, 1).SelectParallelValueAsync(2, (value, token) =>
        {
            if (value == 0)
            {
                return new ValueTask<int>(primary.Task);
            }

            siblingStarted.TrySetResult();
            return ThrowAfterCancellationAsync(token, siblingCancellation);
        }).ToListAsync().AsTask();

        await siblingStarted.Task.WaitAsync(GateTimeout);
        primary.SetException(primaryException);

        var actual = await Assert.ThrowsAsync<AggregateException>(() => operation.WaitAsync(GateTimeout));

        Assert.Collection(
            actual.InnerExceptions,
            failure => Assert.Same(primaryException, failure),
            failure => Assert.Same(siblingCancellation, failure));
    }

    [Fact]
    public async Task SelectParallelValueAsyncPreservesSelectorAndSourceFaultsByIdentity()
    {
        var selectorException = new InvalidOperationException("selector");
        var selectorSource = new ProbeAsyncEnumerable<int>([1]);

        var selectorActual = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await selectorSource.SelectParallelValueAsync(
                1,
                _ => ValueTask.FromException<int>(selectorException)).ToListAsync());

        var sourceException = new InvalidOperationException("source");
        var source = new ProbeAsyncEnumerable<int>([1], moveNextExceptionAfterValues: sourceException);
        var selectorStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var selectorCanceled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var sourceOperation = source.SelectParallelValueAsync(2, (_, token) =>
        {
            selectorStarted.TrySetResult();
            return CompleteAfterCancellationAsync(token, selectorCanceled, 1);
        }).ToListAsync().AsTask();

        await selectorStarted.Task;
        await selectorCanceled.Task;
        var sourceActual = await Assert.ThrowsAsync<InvalidOperationException>(() => sourceOperation);

        Assert.Same(selectorException, selectorActual);
        Assert.Same(sourceException, sourceActual);
        Assert.Equal(1, selectorSource.DisposeCount);
        Assert.Equal(1, source.DisposeCount);
    }

    [Fact]
    public async Task SelectParallelValueAsyncPropagatesDisposeFaultByIdentity()
    {
        var expected = new InvalidOperationException("dispose");
        var source = new ProbeAsyncEnumerable<int>([1], disposeException: expected);

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await source.SelectParallelValueAsync(1, static value => ValueTask.FromResult(value)).ToListAsync());

        Assert.Same(expected, actual);
        Assert.Equal(1, source.DisposeCount);
    }

    [Fact]
    public async Task SelectParallelValueAsyncConsumesEachSelectorValueTaskOnce()
    {
        var selector = new CountingValueTaskSource<int>(7);

        var values = await AsyncValues(1)
            .SelectParallelValueAsync(1, _ => selector.CreateValueTask())
            .ToListAsync();

        Assert.Equal([7], values);
        Assert.Equal(1, selector.GetResultCount);
    }

    [Fact]
    public async Task SelectParallelValueAsyncAllowsSelectorsToShareTheSameTask()
    {
        var shared = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var selectorsStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var selectorCount = 0;
        var operation = AsyncValues(1, 2).SelectParallelValueAsync(2, _ =>
        {
            if (Interlocked.Increment(ref selectorCount) == 2)
            {
                selectorsStarted.TrySetResult();
            }

            return new ValueTask<int>(shared.Task);
        }).ToArrayAsync(TestContext.Current.CancellationToken).AsTask();

        await selectorsStarted.Task.WaitAsync(GateTimeout);
        shared.SetResult(7);

        var values = await operation.WaitAsync(GateTimeout);
        Assert.Equal([7, 7], values);
    }

    [Fact]
    public async Task SelectParallelValueAsyncAllowsImmediateSequentialMoveNextCalls()
    {
        var expected = Enumerable.Range(0, 64).ToArray();

        for (var iteration = 0; iteration < 32; iteration++)
        {
            var actual = await AsyncValues(expected)
                .SelectParallelValueAsync(4, YieldAsync)
                .ToArrayAsync(TestContext.Current.CancellationToken);

            Assert.Equal(expected, actual);
        }
    }

    private static async Task ConsumeOneAsync<T>(IAsyncEnumerable<T> source)
    {
        await foreach (var _ in source)
        {
            break;
        }
    }

    private static async ValueTask<int> FinishAfterCancellationAsync(
        CancellationToken cancellationToken,
        TaskCompletionSource cancellationObserved,
        Task<int> finish)
    {
        using var registration = cancellationToken.Register(() => _ = cancellationObserved.TrySetResult());
        await cancellationObserved.Task;
        return await finish;
    }

    private static async ValueTask<int> CompleteAfterCancellationAsync(
        CancellationToken cancellationToken,
        TaskCompletionSource cancellationObserved,
        int result)
    {
        using var registration = cancellationToken.Register(() => _ = cancellationObserved.TrySetResult());
        await cancellationObserved.Task;
        return result;
    }

    private static async ValueTask<int> WaitForCancellationAsync(
        CancellationToken cancellationToken,
        TaskCompletionSource started)
    {
        started.TrySetResult();
        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
        return 0;
    }

    private static async ValueTask<int> YieldAsync(int value)
    {
        await Task.Yield();
        return value;
    }

    private static async ValueTask<int> ThrowAfterCancellationAsync(
        CancellationToken cancellationToken,
        OperationCanceledException exception)
    {
        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw exception;
        }

        return 0;
    }

    private static async IAsyncEnumerable<T> AsyncValues<T>(params T[] values)
    {
        foreach (var value in values)
        {
            yield return value;
            await Task.Yield();
        }
    }

    private sealed class ProbeAsyncEnumerable<T>(
        IReadOnlyList<T> values,
        Exception? moveNextExceptionAfterValues = null,
        Exception? disposeException = null) : IAsyncEnumerable<T>
    {
        public int EnumeratorCount { get; private set; }

        public int ItemsYielded { get; private set; }

        public int DisposeCount { get; private set; }

        public CancellationToken ReceivedToken { get; private set; }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            EnumeratorCount++;
            ReceivedToken = cancellationToken;
            return new Enumerator(this, values, moveNextExceptionAfterValues, disposeException);
        }

        private sealed class Enumerator(
            ProbeAsyncEnumerable<T> owner,
            IReadOnlyList<T> values,
            Exception? moveNextExceptionAfterValues,
            Exception? disposeException) : IAsyncEnumerator<T>
        {
            private int index = -1;

            public T Current => values[index];

            public ValueTask<bool> MoveNextAsync()
            {
                var nextIndex = index + 1;
                if (nextIndex < values.Count)
                {
                    index = nextIndex;
                    owner.ItemsYielded++;
                    return ValueTask.FromResult(true);
                }

                return moveNextExceptionAfterValues is null
                    ? ValueTask.FromResult(false)
                    : ValueTask.FromException<bool>(moveNextExceptionAfterValues);
            }

            public ValueTask DisposeAsync()
            {
                owner.DisposeCount++;
                return disposeException is null
                    ? ValueTask.CompletedTask
                    : ValueTask.FromException(disposeException);
            }
        }
    }

    private sealed class FaultAfterCancellationAsyncEnumerable<T>(T value, Exception failure) : IAsyncEnumerable<T>
    {
        private readonly TaskCompletionSource pendingMoveNext = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int DisposeCount { get; private set; }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
            new Enumerator(this, value, failure, cancellationToken);

        public Task WaitForPendingMoveNextAsync() => pendingMoveNext.Task;

        private sealed class Enumerator(
            FaultAfterCancellationAsyncEnumerable<T> owner,
            T value,
            Exception failure,
            CancellationToken cancellationToken) : IAsyncEnumerator<T>
        {
            private bool yielded;

            public T Current => value;

            public ValueTask<bool> MoveNextAsync()
            {
                if (!yielded)
                {
                    yielded = true;
                    return ValueTask.FromResult(true);
                }

                owner.pendingMoveNext.TrySetResult();
                return WaitForCancellationAsync();
            }

            public ValueTask DisposeAsync()
            {
                owner.DisposeCount++;
                return ValueTask.CompletedTask;
            }

            private async ValueTask<bool> WaitForCancellationAsync()
            {
                try
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw failure;
                }

                return false;
            }
        }
    }

    private sealed class CancellationIgnoringAsyncEnumerable<T>(IReadOnlyList<T> values) : IAsyncEnumerable<T>
    {
        private readonly TaskCompletionSource cancellation = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource pendingMoveNext = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource releaseMoveNext = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int DisposeCount { get; private set; }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            cancellationToken.Register(() => cancellation.TrySetResult());
            return new Enumerator(this, values);
        }

        public Task WaitForCancellationAsync() => cancellation.Task;

        public Task WaitForPendingMoveNextAsync() => pendingMoveNext.Task;

        public void ReleasePendingMoveNext() => releaseMoveNext.TrySetResult();

        private sealed class Enumerator(
            CancellationIgnoringAsyncEnumerable<T> owner,
            IReadOnlyList<T> values) : IAsyncEnumerator<T>
        {
            private int index = -1;

            public T Current => values[index];

            public async ValueTask<bool> MoveNextAsync()
            {
                var next = index + 1;
                if (next >= values.Count)
                {
                    return false;
                }

                if (next > 0)
                {
                    owner.pendingMoveNext.TrySetResult();
                    await owner.releaseMoveNext.Task.ConfigureAwait(false);
                }

                index = next;
                return true;
            }

            public ValueTask DisposeAsync()
            {
                owner.DisposeCount++;
                return ValueTask.CompletedTask;
            }
        }
    }
}

#pragma warning restore xUnit1051
