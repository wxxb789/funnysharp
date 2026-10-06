using System.Collections.Generic;

namespace FunnySharp.Tests;

#pragma warning disable xUnit1051

public sealed class ParallelAsyncEnumerableTests
{
    private static readonly TimeSpan GateTimeout = TimeSpan.FromSeconds(5);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RejectsInvalidArgumentsEagerly(bool completionOrder)
    {
        IAsyncEnumerable<int>? source = null;

        Assert.Throws<ArgumentNullException>(() => source!.SelectParallel(
            1,
            static value => ValueTask.FromResult(value),
            completionOrder));
        Assert.Throws<ArgumentOutOfRangeException>(() => AsyncValues(1).SelectParallel(
            0,
            static value => ValueTask.FromResult(value),
            completionOrder));
        Assert.Throws<ArgumentOutOfRangeException>(() => AsyncValues(1).SelectParallel(
            -1,
            static value => ValueTask.FromResult(value),
            completionOrder));
        Assert.Throws<ArgumentNullException>(() => AsyncValues(1).SelectParallel(
            1,
            (Func<int, ValueTask<int>>)null!,
            completionOrder));
        Assert.Throws<ArgumentNullException>(() => AsyncValues(1).SelectParallel(
            1,
            (Func<int, CancellationToken, ValueTask<int>>)null!,
            completionOrder));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DefersSourceAndSelectorUntilFirstPull(bool completionOrder)
    {
        var source = new ProbeAsyncEnumerable<int>([1]);
        var selectorCalls = 0;
        var pipeline = source.SelectParallel(2, value =>
        {
            selectorCalls++;
            return ValueTask.FromResult(value * 10);
        }, completionOrder);

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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RejectsConcurrentMoveNextCalls(bool completionOrder)
    {
        var selectorStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var selector = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var pipeline = AsyncValues(1).SelectParallel(1, _ =>
        {
            selectorStarted.TrySetResult();
            return new ValueTask<int>(selector.Task);
        }, completionOrder);
        await using var enumerator = pipeline.GetAsyncEnumerator();
        var firstMove = enumerator.MoveNextAsync().AsTask();

        await selectorStarted.Task.WaitAsync(GateTimeout);
        try
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                enumerator.MoveNextAsync().AsTask());
        }
        finally
        {
            selector.TrySetResult(1);
        }
        Assert.True(await firstMove.WaitAsync(GateTimeout));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RejectsMoveNextAfterDisposal(bool completionOrder)
    {
        var enumerator = AsyncValues(1)
            .SelectParallel(1, static value => ValueTask.FromResult(value), completionOrder)
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
        var acknowledgedDeliveries = 0;
        var retainedAdmissionHighWater = 0;
        var thirdStartedBeforeFirstDelivery = false;

        var pipeline = source.SelectParallelValueAsync(2, value =>
        {
            var deliveries = Volatile.Read(ref acknowledgedDeliveries);
            retainedAdmissionHighWater = Math.Max(retainedAdmissionHighWater, source.ItemsYielded - deliveries);
            switch (value)
            {
                case 1:
                    firstStarted.TrySetResult();
                    return new ValueTask<int>(first.Task);
                case 2:
                    secondStarted.TrySetResult();
                    return new ValueTask<int>(second.Task);
                case 3:
                    thirdStartedBeforeFirstDelivery = deliveries == 0;
                    thirdStarted.TrySetResult();
                    return new ValueTask<int>(third.Task);
                default:
                    throw new InvalidOperationException("Unexpected source item.");
            }
        });

        var enumerator = pipeline.GetAsyncEnumerator();
        Task<bool>? firstPull = null;
        var initialAdmissions = Task.WhenAll(firstStarted.Task, secondStarted.Task)
            .WaitAsync(GateTimeout, TestContext.Current.CancellationToken);
        var thirdAdmission = thirdStarted.Task.WaitAsync(GateTimeout, TestContext.Current.CancellationToken);
        try
        {
            // Already completed on admission: its observer publishes synchronously.
            second.SetResult(20);
            firstPull = AcknowledgeDeliveryAsync(
                enumerator.MoveNextAsync(),
                () => Interlocked.Increment(ref acknowledgedDeliveries));
            await initialAdmissions;

            first.SetResult(10);
            Assert.True(await firstPull.WaitAsync(GateTimeout, TestContext.Current.CancellationToken));
            Assert.Equal(10, enumerator.Current);
            await thirdAdmission;
            Assert.False(thirdStartedBeforeFirstDelivery);
            Assert.Equal(2, retainedAdmissionHighWater);
            Assert.Equal(3, source.ItemsYielded);

            Assert.True(await enumerator.MoveNextAsync().AsTask().WaitAsync(GateTimeout, TestContext.Current.CancellationToken));
            Assert.Equal(20, enumerator.Current);

            third.SetResult(30);
            Assert.True(await enumerator.MoveNextAsync().AsTask().WaitAsync(GateTimeout, TestContext.Current.CancellationToken));
            Assert.Equal(30, enumerator.Current);
            Assert.False(await enumerator.MoveNextAsync().AsTask().WaitAsync(GateTimeout, TestContext.Current.CancellationToken));
            Assert.Equal(1, source.DisposeCount);
        }
        finally
        {
            first.TrySetResult(10);
            second.TrySetResult(20);
            third.TrySetResult(30);
            try
            {
                if (firstPull is not null)
                {
                    _ = await firstPull.WaitAsync(GateTimeout, TestContext.Current.CancellationToken);
                }
            }
            finally
            {
                await enumerator.DisposeAsync().AsTask().WaitAsync(GateTimeout, TestContext.Current.CancellationToken);
            }
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TokenAwareSelectorUsesOneCancelableOperationToken(bool completionOrder)
    {
        var source = new ProbeAsyncEnumerable<int>([1, 2]);
        var selectorTokens = new List<CancellationToken>();

        var values = await source.SelectParallel(2, (value, token) =>
        {
            selectorTokens.Add(token);
            return ValueTask.FromResult(value * 10);
        }, completionOrder).ToListAsync();

        Assert.Equal([10, 20], values);
        Assert.True(source.ReceivedToken.CanBeCanceled);
        Assert.Equal(2, selectorTokens.Count);
        Assert.All(selectorTokens, token => Assert.Equal(source.ReceivedToken, token));
        Assert.Equal(1, source.DisposeCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ConsumerBreakCancelsStartedSelectorsWaitsForThemAndDisposesSource(bool completionOrder)
    {
        // Retain successful break cleanup and add a terminal fault dependency in the same case.
        foreach (var cleanupFault in new[] { false, true })
        {
            var source = new ProbeAsyncEnumerable<int>([1, 2]);
            var first = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var firstStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var secondStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var secondCanceled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var secondFinished = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var expected = new ArgumentException("selector cleanup");
            CancellationToken secondToken = default;

            var pipeline = source.SelectParallel(2, (value, token) =>
            {
                if (value == 1)
                {
                    firstStarted.TrySetResult();
                    return new ValueTask<int>(first.Task);
                }

                secondToken = token;
                secondStarted.TrySetResult();
                return FinishAfterCancellationAsync(token, secondCanceled, secondFinished.Task);
            }, completionOrder);
            var admissions = Task.WhenAll(firstStarted.Task, secondStarted.Task)
                .WaitAsync(GateTimeout, TestContext.Current.CancellationToken);
            var cancellationObserved = secondCanceled.Task.WaitAsync(GateTimeout, TestContext.Current.CancellationToken);
            var consumption = ConsumeOneAsync(pipeline);

            try
            {
                await admissions;
                first.SetResult(1);
                await cancellationObserved;
                Assert.True(secondToken.IsCancellationRequested);

                if (cleanupFault)
                {
                    var failureObserved = Assert.ThrowsAsync<ArgumentException>(
                        () => consumption.WaitAsync(GateTimeout, TestContext.Current.CancellationToken));
                    secondFinished.SetException(expected);
                    Assert.Same(expected, await failureObserved);
                }
                else
                {
                    var completionObserved = consumption.WaitAsync(GateTimeout, TestContext.Current.CancellationToken);
                    secondFinished.SetResult(2);
                    await completionObserved;
                }

                Assert.Equal(1, source.DisposeCount);
            }
            finally
            {
                first.TrySetResult(1);
                secondFinished.TrySetResult(2);
                try
                {
                    await consumption.WaitAsync(GateTimeout, TestContext.Current.CancellationToken);
                }
                catch (ArgumentException exception) when (cleanupFault && ReferenceEquals(exception, expected))
                {
                    // The asserted terminal cleanup fault is also observed during teardown.
                }
            }
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExternalCancellationPreservesTheCallerTokenAndWaitsForCleanup(bool completionOrder)
    {
        using var cancellationSource = new CancellationTokenSource();
        var source = new ProbeAsyncEnumerable<int>([1]);
        var selectorStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var operation = source.SelectParallel(
                1,
                (_, token) => WaitForCancellationAsync(token, selectorStarted),
                completionOrder)
            .ToListAsync(cancellationSource.Token)
            .AsTask();

        await selectorStarted.Task.WaitAsync(GateTimeout);
        cancellationSource.Cancel();

        var actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation.WaitAsync(GateTimeout));

        Assert.True(operation.IsCanceled);
        Assert.Equal(cancellationSource.Token, actual.CancellationToken);
        Assert.Equal(1, source.DisposeCount);
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, false)]
    [InlineData(false, true, true)]
    [InlineData(true, false, false)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    [InlineData(true, true, true)]
    public async Task ExternalCancellationDoesNotPublishANonCooperatingSelectorAndCleansUpOnce(
        bool completionOrder,
        bool tokenAware,
        bool cleanupFault)
    {
        using var cancellationSource = new CancellationTokenSource();
        var disposeException = new InvalidOperationException("dispose");
        var selectorException = new ArgumentException("selector cleanup");
        var source = new ProbeAsyncEnumerable<int>(
            [1],
            disposeException: cleanupFault ? disposeException : null);
        var selector = new CountingValueTaskSource<int>();
        var selectorCalls = 0;
        var selectorReleased = false;

        ValueTask<int> Selector(int value)
        {
            selectorCalls++;
            return selector.CreateValueTask();
        }

        var pipeline = tokenAware
            ? source.SelectParallel(1, (value, _) => Selector(value), completionOrder)
            : source.SelectParallel(1, Selector, completionOrder);

        await using var enumerator = pipeline.GetAsyncEnumerator(cancellationSource.Token);
        Task<bool>? firstPull = null;
        try
        {
            firstPull = enumerator.MoveNextAsync().AsTask();

            // The synchronous source starts the pending selector before the pull returns.
            Assert.Equal(1, selectorCalls);
            Assert.Equal(1, source.ItemsYielded);
            Assert.Equal(0, selector.GetResultCount);

            cancellationSource.Cancel();
            Assert.True(source.ReceivedToken.IsCancellationRequested);

            if (cleanupFault)
            {
                var failureObserved = Assert.ThrowsAsync<AggregateException>(
                    () => firstPull.WaitAsync(GateTimeout, TestContext.Current.CancellationToken));
                selector.SetException(selectorException);
                selectorReleased = true;
                var actual = await failureObserved;

                Assert.Collection(
                    actual.InnerExceptions,
                    failure => Assert.Equal(
                        cancellationSource.Token,
                        Assert.IsType<OperationCanceledException>(failure).CancellationToken),
                    failure => Assert.Same(selectorException, failure),
                    failure => Assert.Same(disposeException, failure));
            }
            else
            {
                var cancellationObserved = Assert.ThrowsAnyAsync<OperationCanceledException>(
                    () => firstPull.WaitAsync(GateTimeout, TestContext.Current.CancellationToken));
                selector.SetResult(10);
                selectorReleased = true;
                var actual = await cancellationObserved;

                Assert.Equal(cancellationSource.Token, actual.CancellationToken);
                Assert.True(firstPull.IsCanceled);
            }

            Assert.Equal(0, enumerator.Current);
            Assert.Equal(1, selectorCalls);
            Assert.Equal(1, selector.GetResultCount);
            Assert.Equal(1, source.DisposeCount);

            await enumerator.DisposeAsync().AsTask().WaitAsync(GateTimeout, TestContext.Current.CancellationToken);
            Assert.Equal(1, source.DisposeCount);
        }
        finally
        {
            if (!selectorReleased)
            {
                selector.SetResult(10);
            }

            if (firstPull is not null)
            {
                try
                {
                    _ = await firstPull.WaitAsync(GateTimeout, TestContext.Current.CancellationToken);
                }
                catch (OperationCanceledException) when (cancellationSource.IsCancellationRequested)
                {
                }
                catch (AggregateException exception) when (
                    cleanupFault && exception.InnerExceptions.Contains(disposeException))
                {
                }
            }
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task LaterSelectorFaultDoesNotPublishAnEarlierNonCooperatingSelector(
        bool completionOrder,
        bool tokenAware)
    {
        var source = new ProbeAsyncEnumerable<int>([1, 2]);
        var first = new CountingValueTaskSource<int>();
        var second = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var operationCanceled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var expected = new InvalidOperationException("later selector");
        var firstReleased = false;

        ValueTask<int> Selector(int value) => value == 1
            ? first.CreateValueTask()
            : new ValueTask<int>(second.Task);

        var pipeline = tokenAware
            ? source.SelectParallel(3, (value, _) => Selector(value), completionOrder)
            : source.SelectParallel(3, Selector, completionOrder);

        await using var enumerator = pipeline.GetAsyncEnumerator();
        Task<bool>? firstPull = null;
        try
        {
            firstPull = enumerator.MoveNextAsync().AsTask();
            Assert.Equal(2, source.ItemsYielded);

            using var registration = source.ReceivedToken.Register(
                () => _ = operationCanceled.TrySetResult());
            var cancellationObserved = operationCanceled.Task.WaitAsync(GateTimeout, TestContext.Current.CancellationToken);

            second.SetException(expected);
            await cancellationObserved;

            first.SetResult(10);
            firstReleased = true;
            var actual = await Assert.ThrowsAsync<InvalidOperationException>(
                () => firstPull.WaitAsync(GateTimeout, TestContext.Current.CancellationToken));

            Assert.Same(expected, actual);
            Assert.Equal(0, enumerator.Current);
            Assert.Equal(1, first.GetResultCount);
            Assert.Equal(1, source.DisposeCount);

            await enumerator.DisposeAsync().AsTask().WaitAsync(GateTimeout, TestContext.Current.CancellationToken);
            Assert.Equal(1, source.DisposeCount);
        }
        finally
        {
            if (!firstReleased)
            {
                first.SetResult(10);
            }

            second.TrySetResult(20);
            if (firstPull is not null)
            {
                try
                {
                    _ = await firstPull.WaitAsync(GateTimeout, TestContext.Current.CancellationToken);
                }
                catch (InvalidOperationException exception) when (ReferenceEquals(exception, expected))
                {
                }
            }
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LaterSelectorFaultCancelsAndDrainsAnEarlierSelectorBeforeFailing(bool completionOrder)
    {
        // Keep the original single-fault identity path as well as the drain-fault witness.
        foreach (var cleanupFault in new[] { false, true })
        {
            using var cancellationSource = new CancellationTokenSource();
            var source = new ProbeAsyncEnumerable<int>([0, 1]);
            var earlierCanceled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var earlierFinished = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            var laterStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var expected = new InvalidOperationException("later selector");
            var cleanupException = new ArgumentException("earlier selector cleanup");
            var laterAdmission = laterStarted.Task.WaitAsync(GateTimeout, TestContext.Current.CancellationToken);
            var cancellationObserved = earlierCanceled.Task.WaitAsync(GateTimeout, TestContext.Current.CancellationToken);
            var operation = source.SelectParallel(2, (value, token) =>
            {
                if (value == 0)
                {
                    return FinishAfterCancellationAsync(token, earlierCanceled, earlierFinished.Task);
                }

                laterStarted.TrySetResult();
                return ValueTask.FromException<int>(expected);
            }, completionOrder).ToListAsync(cancellationSource.Token).AsTask();

            try
            {
                await laterAdmission;
                await cancellationObserved;

                if (cleanupFault)
                {
                    var failureObserved = Assert.ThrowsAsync<AggregateException>(
                        () => operation.WaitAsync(GateTimeout, TestContext.Current.CancellationToken));
                    earlierFinished.SetException(cleanupException);
                    var actual = await failureObserved;
                    Assert.Collection(
                        actual.InnerExceptions,
                        failure => Assert.Same(expected, failure),
                        failure => Assert.Same(cleanupException, failure));
                }
                else
                {
                    var failureObserved = Assert.ThrowsAsync<InvalidOperationException>(
                        () => operation.WaitAsync(GateTimeout, TestContext.Current.CancellationToken));
                    earlierFinished.SetResult(0);
                    Assert.Same(expected, await failureObserved);
                }

                Assert.Equal(1, source.DisposeCount);
            }
            finally
            {
                cancellationSource.Cancel();
                earlierFinished.TrySetResult(0);
                try
                {
                    await operation.WaitAsync(GateTimeout, TestContext.Current.CancellationToken);
                }
                catch (InvalidOperationException exception) when (ReferenceEquals(exception, expected))
                {
                    // Already asserted above, or reached while releasing the held selector.
                }
                catch (AggregateException exception) when (
                    cleanupFault && exception.InnerExceptions.Count == 2 &&
                    ReferenceEquals(exception.InnerExceptions[0], expected) &&
                    ReferenceEquals(exception.InnerExceptions[1], cleanupException))
                {
                    // Both exact terminal failures are also observed during teardown.
                }
                catch (OperationCanceledException) when (cancellationSource.IsCancellationRequested)
                {
                    // Cancellation is requested only to unwind a failed test.
                }
            }
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SelectorFailureRetainsASourceFaultRaisedDuringCleanup(bool completionOrder)
    {
        var selectorException = new InvalidOperationException("selector");
        var sourceException = new ArgumentException("source cleanup");
        var source = new FaultAfterCancellationAsyncEnumerable<int>(1, sourceException);
        var selector = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var operation = source.SelectParallel(
                2,
                _ => new ValueTask<int>(selector.Task),
                completionOrder)
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SelectorFailureDoesNotAdmitAnItemYieldedAfterCancellation(bool completionOrder)
    {
        var expected = new InvalidOperationException("selector");
        var source = new CancellationIgnoringAsyncEnumerable<int>([0, 1]);
        var firstSelector = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var selectorCalls = new List<int>();
        var operation = source.SelectParallel(2, (value, _) =>
        {
            selectorCalls.Add(value);
            return value == 0
                ? new ValueTask<int>(firstSelector.Task)
                : ValueTask.FromResult(value);
        }, completionOrder).ToListAsync().AsTask();

        await source.WaitForPendingMoveNextAsync().WaitAsync(GateTimeout);
        firstSelector.SetException(expected);
        await source.WaitForCancellationAsync().WaitAsync(GateTimeout);
        source.ReleasePendingMoveNext();

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() => operation.WaitAsync(GateTimeout));

        Assert.Same(expected, actual);
        Assert.Equal([0], selectorCalls);
        Assert.Equal(1, source.DisposeCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SelectorFailureRetainsAnUnrelatedCanceledSibling(bool completionOrder)
    {
        using var unrelatedCancellation = new CancellationTokenSource();
        unrelatedCancellation.Cancel();
        var primaryException = new InvalidOperationException("primary");
        var siblingCancellation = new OperationCanceledException(unrelatedCancellation.Token);
        var primary = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var siblingStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var operation = AsyncValues(0, 1).SelectParallel(2, (value, token) =>
        {
            if (value == 0)
            {
                return new ValueTask<int>(primary.Task);
            }

            siblingStarted.TrySetResult();
            return ThrowAfterCancellationAsync(token, siblingCancellation);
        }, completionOrder).ToListAsync().AsTask();

        await siblingStarted.Task.WaitAsync(GateTimeout);
        primary.SetException(primaryException);

        var actual = await Assert.ThrowsAsync<AggregateException>(() => operation.WaitAsync(GateTimeout));

        Assert.Collection(
            actual.InnerExceptions,
            failure => Assert.Same(primaryException, failure),
            failure => Assert.Same(siblingCancellation, failure));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PreservesSelectorAndSourceFaultsByIdentity(bool completionOrder)
    {
        var selectorException = new InvalidOperationException("selector");
        var selectorSource = new ProbeAsyncEnumerable<int>([1]);

        var selectorActual = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await selectorSource.SelectParallel(
                1,
                _ => ValueTask.FromException<int>(selectorException),
                completionOrder).ToListAsync());

        var sourceException = new InvalidOperationException("source");
        var source = new ProbeAsyncEnumerable<int>([1], moveNextExceptionAfterValues: sourceException);
        var selectorStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var selectorCanceled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var sourceOperation = source.SelectParallel(2, (_, token) =>
        {
            selectorStarted.TrySetResult();
            return CompleteAfterCancellationAsync(token, selectorCanceled, 1);
        }, completionOrder).ToListAsync().AsTask();

        await selectorStarted.Task;
        await selectorCanceled.Task;
        var sourceActual = await Assert.ThrowsAsync<InvalidOperationException>(() => sourceOperation);

        Assert.Same(selectorException, selectorActual);
        Assert.Same(sourceException, sourceActual);
        Assert.Equal(1, selectorSource.DisposeCount);
        Assert.Equal(1, source.DisposeCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PropagatesDisposeFaultByIdentity(bool completionOrder)
    {
        var expected = new InvalidOperationException("dispose");
        var source = new ProbeAsyncEnumerable<int>([1], disposeException: expected);

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await source.SelectParallel(1, static value => ValueTask.FromResult(value), completionOrder).ToListAsync());

        Assert.Same(expected, actual);
        Assert.Equal(1, source.DisposeCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ConsumesEachSelectorValueTaskOnce(bool completionOrder)
    {
        var selector = new CountingValueTaskSource<int>(7);

        var values = await AsyncValues(1)
            .SelectParallel(1, _ => selector.CreateValueTask(), completionOrder)
            .ToListAsync();

        Assert.Equal([7], values);
        Assert.Equal(1, selector.GetResultCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AllowsSelectorsToShareTheSameTask(bool completionOrder)
    {
        var shared = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var selectorsStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var selectorCount = 0;
        var operation = AsyncValues(1, 2).SelectParallel(2, _ =>
        {
            if (Interlocked.Increment(ref selectorCount) == 2)
            {
                selectorsStarted.TrySetResult();
            }

            return new ValueTask<int>(shared.Task);
        }, completionOrder).ToArrayAsync(TestContext.Current.CancellationToken).AsTask();

        await selectorsStarted.Task.WaitAsync(GateTimeout);
        shared.SetResult(7);

        var values = await operation.WaitAsync(GateTimeout);
        Assert.Equal([7, 7], values);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AllowsImmediateSequentialMoveNextCalls(bool completionOrder)
    {
        var expected = Enumerable.Range(0, 64).ToArray();

        for (var iteration = 0; iteration < 32; iteration++)
        {
            var actual = await AsyncValues(expected)
                .SelectParallel(4, YieldAsync, completionOrder)
                .ToArrayAsync(TestContext.Current.CancellationToken);

            // Source order must deliver exactly; completion order may deliver any permutation.
            if (completionOrder)
            {
                Assert.Equal(expected, actual.Order());
            }
            else
            {
                Assert.Equal(expected, actual);
            }
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task YieldsNothingForAnEmptySource(bool completionOrder)
    {
        var source = new ProbeAsyncEnumerable<int>([]);

        var values = await source.SelectParallel(
                2,
                static value => ValueTask.FromResult(value),
                completionOrder)
            .ToListAsync();

        Assert.Empty(values);
        Assert.Equal(1, source.EnumeratorCount);
        Assert.Equal(1, source.DisposeCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SelectParallelRoutesCompletionOrderTrueToCompletionOrderDelivery(bool tokenAware)
    {
        var source = new ProbeAsyncEnumerable<int>([1, 2]);
        var first = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

        ValueTask<int> Selector(int value) => value == 1
            ? new ValueTask<int>(first.Task)
            : ValueTask.FromResult(20);

        // The guard pins both factory overloads: the method-group selector binds the
        // non-token overload, the two-parameter lambda the token-aware one.
        var pipeline = tokenAware
            ? source.SelectParallel(
                3,
                (value, _) => Selector(value),
                completionOrder: true)
            : source.SelectParallel(
                3,
                Selector,
                completionOrder: true);

        await using var enumerator = pipeline.GetAsyncEnumerator();
        Task<bool>? firstPull = null;
        try
        {
            var sourceExhausted = source.WaitForExhaustionAsync().WaitAsync(GateTimeout, TestContext.Current.CancellationToken);
            firstPull = enumerator.MoveNextAsync().AsTask();
            await sourceExhausted;

            // The completed second selector is published before the producer asks
            // for source exhaustion, so releasing the first cannot change this order.
            first.SetResult(10);
            Assert.True(await firstPull.WaitAsync(GateTimeout, TestContext.Current.CancellationToken));
            Assert.Equal(20, enumerator.Current);

            Assert.True(await enumerator.MoveNextAsync().AsTask().WaitAsync(GateTimeout));
            Assert.Equal(10, enumerator.Current);
            Assert.False(await enumerator.MoveNextAsync().AsTask().WaitAsync(GateTimeout));
            Assert.Equal(1, source.DisposeCount);
        }
        finally
        {
            first.TrySetResult(10);
            if (firstPull is not null)
            {
                _ = await firstPull.WaitAsync(GateTimeout, TestContext.Current.CancellationToken);
            }
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SelectParallelRoutesCompletionOrderFalseToSourceOrderDelivery(bool tokenAware)
    {
        var source = new ProbeAsyncEnumerable<int>([1, 2]);
        var first = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

        ValueTask<int> Selector(int value) => value == 1
            ? new ValueTask<int>(first.Task)
            : ValueTask.FromResult(20);

        // The guard pins both factory overloads: the method-group selector binds the
        // non-token overload, the two-parameter lambda the token-aware one.
        var pipeline = tokenAware
            ? source.SelectParallel(
                3,
                (value, _) => Selector(value),
                completionOrder: false)
            : source.SelectParallel(
                3,
                Selector,
                completionOrder: false);

        await using var enumerator = pipeline.GetAsyncEnumerator();
        Task<bool>? firstPull = null;
        try
        {
            var sourceExhausted = source.WaitForExhaustionAsync().WaitAsync(GateTimeout, TestContext.Current.CancellationToken);
            firstPull = enumerator.MoveNextAsync().AsTask();
            await sourceExhausted;

            // With completion-order routing, 20 is already published at exhaustion.
            // A wrong route therefore fails on Current, not on elapsed silence.
            first.SetResult(10);
            Assert.True(await firstPull.WaitAsync(GateTimeout));
            Assert.Equal(10, enumerator.Current);

            Assert.True(await enumerator.MoveNextAsync().AsTask().WaitAsync(GateTimeout));
            Assert.Equal(20, enumerator.Current);
            Assert.False(await enumerator.MoveNextAsync().AsTask().WaitAsync(GateTimeout));
            Assert.Equal(1, source.DisposeCount);
        }
        finally
        {
            first.TrySetResult(10);
            if (firstPull is not null)
            {
                _ = await firstPull.WaitAsync(GateTimeout, TestContext.Current.CancellationToken);
            }
        }
    }

    [Fact]
    public async Task SelectParallelCompletionOrderValueAsyncDeliversResultsInCompletionOrder()
    {
        var first = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var second = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var third = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var pipeline = AsyncValues(1, 2, 3).SelectParallelCompletionOrderValueAsync(3, value => value switch
        {
            1 => new ValueTask<int>(first.Task),
            2 => new ValueTask<int>(second.Task),
            _ => new ValueTask<int>(third.Task),
        });

        await using var enumerator = pipeline.GetAsyncEnumerator();

        third.SetResult(30);
        Assert.True(await enumerator.MoveNextAsync());
        Assert.Equal(30, enumerator.Current);
        Assert.False(first.Task.IsCompleted);
        Assert.False(second.Task.IsCompleted);

        first.SetResult(10);
        Assert.True(await enumerator.MoveNextAsync());
        Assert.Equal(10, enumerator.Current);

        second.SetResult(20);
        Assert.True(await enumerator.MoveNextAsync());
        Assert.Equal(20, enumerator.Current);
        Assert.False(await enumerator.MoveNextAsync());
    }

    [Fact]
    public async Task SelectParallelCompletionOrderValueAsyncBoundsUndeliveredWorkAndKeepsStreaming()
    {
        var source = new ProbeAsyncEnumerable<int>([1, 2, 3]);
        var first = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var second = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var third = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thirdStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var acknowledgedDeliveries = 0;
        var retainedAdmissionHighWater = 0;
        var thirdStartedBeforeFirstDelivery = false;

        var pipeline = source.SelectParallelCompletionOrderValueAsync(2, value =>
        {
            var deliveries = Volatile.Read(ref acknowledgedDeliveries);
            retainedAdmissionHighWater = Math.Max(retainedAdmissionHighWater, source.ItemsYielded - deliveries);
            switch (value)
            {
                case 1:
                    firstStarted.TrySetResult();
                    return new ValueTask<int>(first.Task);
                case 2:
                    secondStarted.TrySetResult();
                    return new ValueTask<int>(second.Task);
                case 3:
                    thirdStartedBeforeFirstDelivery = deliveries == 0;
                    thirdStarted.TrySetResult();
                    return new ValueTask<int>(third.Task);
                default:
                    throw new InvalidOperationException("Unexpected source item.");
            }
        });

        var enumerator = pipeline.GetAsyncEnumerator();
        Task<bool>? firstPull = null;
        var initialAdmissions = Task.WhenAll(firstStarted.Task, secondStarted.Task)
            .WaitAsync(GateTimeout, TestContext.Current.CancellationToken);
        var thirdAdmission = thirdStarted.Task.WaitAsync(GateTimeout, TestContext.Current.CancellationToken);
        try
        {
            // Already completed on admission: its observer publishes synchronously.
            second.SetResult(20);
            firstPull = AcknowledgeDeliveryAsync(
                enumerator.MoveNextAsync(),
                () => Interlocked.Increment(ref acknowledgedDeliveries));
            await initialAdmissions;

            Assert.True(await firstPull.WaitAsync(GateTimeout, TestContext.Current.CancellationToken));
            Assert.Equal(20, enumerator.Current);
            await thirdAdmission;
            Assert.False(thirdStartedBeforeFirstDelivery);
            Assert.Equal(2, retainedAdmissionHighWater);
            Assert.Equal(3, source.ItemsYielded);

            first.SetResult(10);
            Assert.True(await enumerator.MoveNextAsync().AsTask().WaitAsync(GateTimeout, TestContext.Current.CancellationToken));
            Assert.Equal(10, enumerator.Current);

            third.SetResult(30);
            Assert.True(await enumerator.MoveNextAsync().AsTask().WaitAsync(GateTimeout, TestContext.Current.CancellationToken));
            Assert.Equal(30, enumerator.Current);
            Assert.False(await enumerator.MoveNextAsync().AsTask().WaitAsync(GateTimeout, TestContext.Current.CancellationToken));
            Assert.Equal(1, source.DisposeCount);
        }
        finally
        {
            first.TrySetResult(10);
            second.TrySetResult(20);
            third.TrySetResult(30);
            try
            {
                if (firstPull is not null)
                {
                    _ = await firstPull.WaitAsync(GateTimeout, TestContext.Current.CancellationToken);
                }
            }
            finally
            {
                await enumerator.DisposeAsync().AsTask().WaitAsync(GateTimeout, TestContext.Current.CancellationToken);
            }
        }
    }

    [Fact]
    public async Task SelectParallelCompletionOrderValueAsyncDeliversResultsAfterSourceExhaustion()
    {
        var source = new ProbeAsyncEnumerable<int>([1, 2]);
        var first = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var second = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        // The spare slot lets the producer reach actual exhaustion while both selectors are held.
        var pipeline = source.SelectParallelCompletionOrderValueAsync(3, value => value == 1
            ? new ValueTask<int>(first.Task)
            : new ValueTask<int>(second.Task));

        var enumerator = pipeline.GetAsyncEnumerator();
        Task<bool>? firstPull = null;
        var sourceExhausted = source.WaitForExhaustionAsync().WaitAsync(GateTimeout, TestContext.Current.CancellationToken);
        try
        {
            firstPull = enumerator.MoveNextAsync().AsTask();
            await sourceExhausted;
            Assert.Equal(2, source.ItemsYielded);

            second.SetResult(20);
            Assert.True(await firstPull.WaitAsync(GateTimeout, TestContext.Current.CancellationToken));
            Assert.Equal(20, enumerator.Current);

            first.SetResult(10);
            Assert.True(await enumerator.MoveNextAsync().AsTask().WaitAsync(GateTimeout, TestContext.Current.CancellationToken));
            Assert.Equal(10, enumerator.Current);
            Assert.False(await enumerator.MoveNextAsync().AsTask().WaitAsync(GateTimeout, TestContext.Current.CancellationToken));
            Assert.Equal(1, source.DisposeCount);
        }
        finally
        {
            first.TrySetResult(10);
            second.TrySetResult(20);
            try
            {
                if (firstPull is not null)
                {
                    _ = await firstPull.WaitAsync(GateTimeout, TestContext.Current.CancellationToken);
                }
            }
            finally
            {
                await enumerator.DisposeAsync().AsTask().WaitAsync(GateTimeout, TestContext.Current.CancellationToken);
            }
        }
    }

    private static async Task<bool> AcknowledgeDeliveryAsync(ValueTask<bool> moveNext, Action acknowledge)
    {
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var awaiter = moveNext.GetAwaiter();
        if (awaiter.IsCompleted)
        {
            ready.SetResult();
        }
        else
        {
            awaiter.UnsafeOnCompleted(() => _ = ready.TrySetResult());
        }

        await ready.Task.WaitAsync(GateTimeout, TestContext.Current.CancellationToken);
        // A public MoveNext completion is ready, not merely a selector callback entered.
        // Record readiness before GetResult can release the library's admission slot.
        // GetResult is still called exactly once; a false/fault result fails the caller.
        acknowledge();
        return awaiter.GetResult();
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
        private readonly TaskCompletionSource exhausted = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int EnumeratorCount { get; private set; }

        public int ItemsYielded { get; private set; }

        public int DisposeCount { get; private set; }

        public CancellationToken ReceivedToken { get; private set; }

        public Task WaitForExhaustionAsync() => exhausted.Task;

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

                owner.exhausted.TrySetResult();
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

internal static class ParallelAsyncEnumerableTestExtensions
{
    internal static IAsyncEnumerable<TResult> SelectParallel<TSource, TResult>(
        this IAsyncEnumerable<TSource> source,
        int maxConcurrency,
        Func<TSource, ValueTask<TResult>> selector,
        bool completionOrder) => completionOrder
        ? source.SelectParallelCompletionOrderValueAsync(maxConcurrency, selector)
        : source.SelectParallelValueAsync(maxConcurrency, selector);

    internal static IAsyncEnumerable<TResult> SelectParallel<TSource, TResult>(
        this IAsyncEnumerable<TSource> source,
        int maxConcurrency,
        Func<TSource, CancellationToken, ValueTask<TResult>> selector,
        bool completionOrder) => completionOrder
        ? source.SelectParallelCompletionOrderValueAsync(maxConcurrency, selector)
        : source.SelectParallelValueAsync(maxConcurrency, selector);
}

#pragma warning restore xUnit1051
