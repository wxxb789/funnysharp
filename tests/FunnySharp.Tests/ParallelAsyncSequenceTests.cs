using System.Collections.Generic;

namespace FunnySharp.Tests;

#pragma warning disable xUnit1051

public sealed class ParallelAsyncSequenceTests
{
    private static readonly TimeSpan GateTimeout = TimeSpan.FromSeconds(5);

    [Fact]
    public void TraverseParallelValueAsyncRejectsInvalidArgumentsEagerly()
    {
        IAsyncEnumerable<int>? source = null;

        Assert.Throws<ArgumentNullException>(() => source!.TraverseParallelValueAsync(
            1,
            static value => ValueTask.FromResult(Option.Some(value))));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TrackingAsyncEnumerable<int>([1])
            .TraverseParallelValueAsync(0, static value => ValueTask.FromResult(Option.Some(value))));
        Assert.Throws<ArgumentNullException>(() => new TrackingAsyncEnumerable<int>([1])
            .TraverseParallelValueAsync(1, (Func<int, ValueTask<Option<int>>>)null!));
        Assert.Throws<ArgumentNullException>(() => new TrackingAsyncEnumerable<int>([1])
            .TraverseParallelValueAsync(1, (Func<int, ValueTask<Result<int, string>>>)null!));
        Assert.Throws<ArgumentNullException>(() => new TrackingAsyncEnumerable<int>([1])
            .TraverseParallelValueAsync(1, (Func<int, ValueTask<Validation<int, string>>>)null!));
    }

    [Fact]
    public async Task TraverseParallelValueAsyncBoundsConcurrencyAndRetainsOptionOrder()
    {
        const int maxConcurrency = 3;
        var source = new TrackingAsyncEnumerable<int>([0, 1, 2, 3, 4, 5, 6, 7]);
        var selector = new BoundedOptionSelector(maxConcurrency);
        var operation = source.TraverseParallelValueAsync(maxConcurrency, selector.SelectAsync).AsTask();

        try
        {
            await selector.WaitForLimitAsync().WaitAsync(GateTimeout);
            Assert.Equal(maxConcurrency, selector.MaximumConcurrency);
            Assert.Equal(maxConcurrency, source.ItemsYielded);
        }
        finally
        {
            selector.Release();
        }

        var result = await operation.WaitAsync(GateTimeout);

        Assert.Equal([0, 10, 20, 30, 40, 50, 60, 70], GetOptionValues(result));
        Assert.Equal(maxConcurrency, selector.MaximumConcurrency);
        Assert.Equal(8, source.ItemsYielded);
        Assert.Equal(1, source.DisposeCount);
    }

    [Fact]
    public async Task TraverseParallelValueAsyncSupportsAllFunctionalSelectorOverloads()
    {
        using var cancellationSource = new CancellationTokenSource();
        var optionTokenSource = new TrackingAsyncEnumerable<int>([1, 2]);
        var resultTokenSource = new TrackingAsyncEnumerable<int>([1, 2]);
        var validationTokenSource = new TrackingAsyncEnumerable<int>([1, 2]);
        var optionTokens = new List<CancellationToken>();
        var resultTokens = new List<CancellationToken>();
        var validationTokens = new List<CancellationToken>();

        var option = await new TrackingAsyncEnumerable<int>([1, 2]).TraverseParallelValueAsync(
            2,
            static value => ValueTask.FromResult(Option.Some(value)));
        var optionWithToken = await optionTokenSource.TraverseParallelValueAsync(
            2,
            (value, token) =>
            {
                optionTokens.Add(token);
                return ValueTask.FromResult(Option.Some(value));
            },
            cancellationSource.Token);
        var result = await new TrackingAsyncEnumerable<int>([1, 2]).TraverseParallelValueAsync(
            2,
            static value => ValueTask.FromResult(Result<int, string>.Success(value)));
        var resultWithToken = await resultTokenSource.TraverseParallelValueAsync(
            2,
            (value, token) =>
            {
                resultTokens.Add(token);
                return ValueTask.FromResult(Result<int, string>.Success(value));
            },
            cancellationSource.Token);
        var validation = await new TrackingAsyncEnumerable<int>([1, 2]).TraverseParallelValueAsync(
            2,
            static value => ValueTask.FromResult(Validation<int, string>.Valid(value)));
        var validationWithToken = await validationTokenSource.TraverseParallelValueAsync(
            2,
            (value, token) =>
            {
                validationTokens.Add(token);
                return ValueTask.FromResult(Validation<int, string>.Valid(value));
            },
            cancellationSource.Token);

        Assert.Equal([1, 2], GetOptionValues(option));
        Assert.Equal([1, 2], GetOptionValues(optionWithToken));
        Assert.Equal([1, 2], GetResultValues(result));
        Assert.Equal([1, 2], GetResultValues(resultWithToken));
        Assert.Equal([1, 2], GetValidationValues(validation));
        Assert.Equal([1, 2], GetValidationValues(validationWithToken));
        AssertLinkedOperationToken(optionTokenSource, optionTokens, cancellationSource.Token);
        AssertLinkedOperationToken(resultTokenSource, resultTokens, cancellationSource.Token);
        AssertLinkedOperationToken(validationTokenSource, validationTokens, cancellationSource.Token);
    }

    [Fact]
    public async Task TraverseParallelValueAsyncCancelsAndDrainsStartedOptionSiblingsAfterFailure()
    {
        var source = new TrackingAsyncEnumerable<int>([0, 1, 2, 3]);
        var selector = new OneTerminalFailFastGate<Option<int>>(
            Option.None<int>(),
            static value => Option.Some(value));
        var operation = source.TraverseParallelValueAsync(2, selector.SelectAsync).AsTask();

        try
        {
            await selector.WaitForBothStartedAsync().WaitAsync(GateTimeout);
            Assert.Equal(2, source.ItemsYielded);

            selector.ReleaseTerminal();
            await selector.WaitForSiblingCancellationAsync().WaitAsync(GateTimeout);
            selector.ReleaseSibling();
            var result = await operation.WaitAsync(GateTimeout);

            Assert.True(result.IsNone);
            Assert.True(selector.SiblingObservedCancellation);
            Assert.True(selector.SiblingFinished);
        }
        finally
        {
            selector.ReleaseTerminal();
            selector.ReleaseSibling();
        }

        Assert.Equal(2, source.ItemsYielded);
        Assert.Equal(1, source.DisposeCount);
    }

    [Fact]
    public async Task TraverseParallelValueAsyncChoosesTheEarliestResultFailureAndDrainsLaterWork()
    {
        var source = new TrackingAsyncEnumerable<int>([0, 1, 2, 3, 4]);
        var selector = new SourceOrderedResultFailureGate();
        var operation = source.TraverseParallelValueAsync(3, selector.SelectAsync).AsTask();

        try
        {
            await selector.WaitForAllStartedAsync().WaitAsync(GateTimeout);
            Assert.Equal(3, source.ItemsYielded);

            selector.ReleaseSecondFailure();
            await selector.WaitForLaterSiblingCancellationAsync().WaitAsync(GateTimeout);
            selector.ReleaseLaterSibling();
            selector.ReleaseFirstFailure();

            var result = await operation.WaitAsync(GateTimeout);

            Assert.True(result.TryGetError(out var error));
            Assert.Equal("first", error);
            Assert.True(selector.LaterSiblingFinished);
        }
        finally
        {
            selector.ReleaseFirstFailure();
            selector.ReleaseSecondFailure();
            selector.ReleaseLaterSibling();
        }

        Assert.Equal(3, source.ItemsYielded);
        Assert.Equal(1, source.DisposeCount);
    }

    [Fact]
    public async Task TraverseParallelValueAsyncAccumulatesSimultaneousValidationFailuresInSourceOrder()
    {
        var source = new TrackingAsyncEnumerable<int>([0, 1, 2, 3]);
        var selector = new SimultaneousValidationFailureGate();
        var operation = source.TraverseParallelValueAsync(3, selector.SelectAsync).AsTask();

        try
        {
            await selector.WaitForFailuresStartedAsync().WaitAsync(GateTimeout);
            Assert.Equal(3, source.ItemsYielded);
        }
        finally
        {
            selector.ReleaseFailures();
        }

        var result = await operation.WaitAsync(GateTimeout);

        Assert.True(result.TryGetErrors(out var errors));
        Assert.Equal(["zero", "one", "two"], errors);
        Assert.Equal(4, source.ItemsYielded);
        Assert.Equal(1, source.DisposeCount);
    }

    [Fact]
    public async Task TraverseParallelValueAsyncAccumulatesMixedValidationErrorsInSourceOrder()
    {
        var source = new TrackingAsyncEnumerable<int>([0, 1, 2]);
        var selectorsStarted = new[]
        {
            new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously),
            new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously),
            new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously),
        };
        var outcomes = new[]
        {
            new TaskCompletionSource<Validation<int, string>>(TaskCreationOptions.RunContinuationsAsynchronously),
            new TaskCompletionSource<Validation<int, string>>(TaskCreationOptions.RunContinuationsAsynchronously),
            new TaskCompletionSource<Validation<int, string>>(TaskCreationOptions.RunContinuationsAsynchronously),
        };
        var operation = source.TraverseParallelValueAsync(3, value =>
        {
            selectorsStarted[value].TrySetResult();
            return new ValueTask<Validation<int, string>>(outcomes[value].Task);
        }).AsTask();

        await Task.WhenAll(selectorsStarted.Select(static started => started.Task)).WaitAsync(GateTimeout);
        outcomes[2].SetResult(Validation<int, string>.Valid(2));
        outcomes[1].SetResult(Validation<int, string>.InvalidMany(["one-a", "one-b"]));
        outcomes[0].SetResult(Validation<int, string>.Invalid("zero"));

        var result = await operation.WaitAsync(GateTimeout);

        Assert.True(result.TryGetErrors(out var errors));
        Assert.Equal(["zero", "one-a", "one-b"], errors);
        Assert.Equal(1, source.DisposeCount);
    }

    [Fact]
    public async Task TraverseParallelValueAsyncPropagatesSelectorAndDisposeFaultsByIdentity()
    {
        var selectorException = new InvalidOperationException("selector");
        var selectorSource = new TrackingAsyncEnumerable<int>([1]);
        var selectorOperation = selectorSource.TraverseParallelValueAsync(
            1,
            (Func<int, ValueTask<Option<int>>>)(_ => ValueTask.FromException<Option<int>>(selectorException))).AsTask();

        var selectorActual = await Assert.ThrowsAsync<InvalidOperationException>(() => selectorOperation);

        Assert.Same(selectorException, selectorActual);
        Assert.Equal(1, selectorSource.DisposeCount);

        var disposeException = new InvalidOperationException("dispose");
        var disposeSource = new TrackingAsyncEnumerable<int>([1], disposeException);
        var disposeOperation = disposeSource.TraverseParallelValueAsync(
            1,
            static value => ValueTask.FromResult(Option.Some(value))).AsTask();

        var disposeActual = await Assert.ThrowsAsync<InvalidOperationException>(() => disposeOperation);

        Assert.Same(disposeException, disposeActual);
        Assert.Equal(1, disposeSource.DisposeCount);
    }

    [Fact]
    public async Task TraverseParallelValueAsyncPropagatesExternalCancellationAndDisposes()
    {
        using var cancellationSource = new CancellationTokenSource();
        var source = new TrackingAsyncEnumerable<int>([1]);
        var selectorStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var selectorToken = default(CancellationToken);
        var operation = source.TraverseParallelValueAsync(
            1,
            (value, operationToken) =>
            {
                selectorToken = operationToken;
                selectorStarted.TrySetResult(true);
                return WaitForCancellationAsync(operationToken);
            },
            cancellationSource.Token).AsTask();

        await selectorStarted.Task.WaitAsync(GateTimeout);
        cancellationSource.Cancel();

        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation.WaitAsync(GateTimeout));

        Assert.True(operation.IsCanceled);
        Assert.Equal(source.ReceivedToken, selectorToken);
        Assert.NotEqual(cancellationSource.Token, source.ReceivedToken);
        Assert.Equal(1, source.DisposeCount);
    }

    [Fact]
    public async Task TraverseParallelValueAsyncRetainsDisposeFaultDuringExternalCancellation()
    {
        using var cancellationSource = new CancellationTokenSource();
        var disposeException = new InvalidOperationException("dispose");
        var source = new TrackingAsyncEnumerable<int>([1], disposeException);
        var selectorStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var operation = source.TraverseParallelValueAsync(
            1,
            (value, operationToken) =>
            {
                selectorStarted.TrySetResult();
                return WaitForCancellationAsync(operationToken);
            },
            cancellationSource.Token).AsTask();

        await selectorStarted.Task.WaitAsync(GateTimeout);
        cancellationSource.Cancel();

        var actual = await Assert.ThrowsAsync<AggregateException>(() => operation.WaitAsync(GateTimeout));

        Assert.Collection(
            actual.InnerExceptions,
            failure => Assert.Equal(cancellationSource.Token, Assert.IsAssignableFrom<OperationCanceledException>(failure).CancellationToken),
            failure => Assert.Same(disposeException, failure));
        Assert.Equal(1, source.DisposeCount);
    }

    [Fact]
    public async Task TraverseParallelValueAsyncConsumesEverySelectorValueTaskOnce()
    {
        var source = new TrackingAsyncEnumerable<int>([0, 1, 2]);
        var valueTasks = new[]
        {
            new CountingValueTaskSource<Option<int>>(Option.Some(10)),
            new CountingValueTaskSource<Option<int>>(Option.Some(20)),
            new CountingValueTaskSource<Option<int>>(Option.Some(30)),
        };

        var result = await source.TraverseParallelValueAsync(
            2,
            value => valueTasks[value].CreateValueTask());

        Assert.Equal([10, 20, 30], GetOptionValues(result));
        Assert.All(valueTasks, valueTask => Assert.Equal(1, valueTask.GetResultCount));
        Assert.Equal(1, source.DisposeCount);
    }

    private static IReadOnlyList<T> GetOptionValues<T>(Option<IReadOnlyList<T>> option)
    {
        Assert.True(option.TryGetValue(out var values));
        return values!;
    }

    private static IReadOnlyList<T> GetResultValues<T>(Result<IReadOnlyList<T>, string> result)
    {
        Assert.True(result.TryGetValue(out var values));
        return values!;
    }

    private static IReadOnlyList<T> GetValidationValues<T>(Validation<IReadOnlyList<T>, string> validation)
    {
        Assert.True(validation.TryGetValue(out var values));
        return values!;
    }

    private static void AssertLinkedOperationToken<T>(
        TrackingAsyncEnumerable<T> source,
        IReadOnlyList<CancellationToken> selectorTokens,
        CancellationToken externalToken)
    {
        Assert.NotEqual(externalToken, source.ReceivedToken);
        Assert.All(selectorTokens, token => Assert.Equal(source.ReceivedToken, token));
    }

    private static async ValueTask<Option<int>> WaitForCancellationAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
        return Option.None<int>();
    }

    private sealed class BoundedOptionSelector(int maxConcurrency)
    {
        private readonly TaskCompletionSource<bool> reachedLimit = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<bool> release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int currentConcurrency;
        private int maximumConcurrency;

        public int MaximumConcurrency => Volatile.Read(ref maximumConcurrency);

        public async ValueTask<Option<int>> SelectAsync(int value)
        {
            var current = Interlocked.Increment(ref currentConcurrency);
            UpdateMaximum(current);
            if (current == maxConcurrency)
            {
                reachedLimit.TrySetResult(true);
            }

            try
            {
                await release.Task.ConfigureAwait(false);
                return Option.Some(value * 10);
            }
            finally
            {
                Interlocked.Decrement(ref currentConcurrency);
            }
        }

        public Task WaitForLimitAsync() => reachedLimit.Task;

        public void Release() => release.TrySetResult(true);

        private void UpdateMaximum(int candidate)
        {
            while (true)
            {
                var observed = Volatile.Read(ref maximumConcurrency);
                if (candidate <= observed || Interlocked.CompareExchange(ref maximumConcurrency, candidate, observed) == observed)
                {
                    return;
                }
            }
        }
    }

    private sealed class OneTerminalFailFastGate<TOutcome>(TOutcome terminal, Func<int, TOutcome> success)
    {
        private readonly TaskCompletionSource<bool> firstStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<bool> secondStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<bool> terminalRelease = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<bool> siblingCancellation = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<bool> siblingRelease = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool SiblingObservedCancellation { get; private set; }

        public bool SiblingFinished { get; private set; }

        public async ValueTask<TOutcome> SelectAsync(int value, CancellationToken cancellationToken)
        {
            if (value == 0)
            {
                firstStarted.TrySetResult(true);
                using var registration = cancellationToken.Register(() =>
                {
                    SiblingObservedCancellation = true;
                    siblingCancellation.TrySetResult(true);
                });
                await siblingCancellation.Task.ConfigureAwait(false);
                await siblingRelease.Task.ConfigureAwait(false);
                SiblingFinished = true;
                return success(value);
            }

            if (value == 1)
            {
                secondStarted.TrySetResult(true);
                await terminalRelease.Task.ConfigureAwait(false);
                return terminal;
            }

            throw new InvalidOperationException("The traversal admitted an item after the concurrency window was full.");
        }

        public Task WaitForBothStartedAsync() => Task.WhenAll(firstStarted.Task, secondStarted.Task);

        public Task WaitForSiblingCancellationAsync() => siblingCancellation.Task;

        public void ReleaseTerminal() => terminalRelease.TrySetResult(true);

        public void ReleaseSibling() => siblingRelease.TrySetResult(true);
    }

    private sealed class SourceOrderedResultFailureGate
    {
        private readonly TaskCompletionSource<bool> firstStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<bool> secondStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<bool> laterStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<bool> firstFailureRelease = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<bool> secondFailureRelease = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<bool> laterCancellation = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<bool> laterRelease = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool LaterSiblingFinished { get; private set; }

        public async ValueTask<Result<int, string>> SelectAsync(int value, CancellationToken cancellationToken)
        {
            switch (value)
            {
                case 0:
                    firstStarted.TrySetResult(true);
                    await firstFailureRelease.Task.ConfigureAwait(false);
                    return Result<int, string>.Failure("first");
                case 1:
                    secondStarted.TrySetResult(true);
                    await secondFailureRelease.Task.ConfigureAwait(false);
                    return Result<int, string>.Failure("second");
                case 2:
                    laterStarted.TrySetResult(true);
                    using (cancellationToken.Register(() => laterCancellation.TrySetResult(true)))
                    {
                        await laterCancellation.Task.ConfigureAwait(false);
                    }

                    await laterRelease.Task.ConfigureAwait(false);
                    LaterSiblingFinished = true;
                    return Result<int, string>.Success(value);
                default:
                    throw new InvalidOperationException("The traversal admitted work after a terminal result.");
            }
        }

        public Task WaitForAllStartedAsync() => Task.WhenAll(firstStarted.Task, secondStarted.Task, laterStarted.Task);

        public Task WaitForLaterSiblingCancellationAsync() => laterCancellation.Task;

        public void ReleaseFirstFailure() => firstFailureRelease.TrySetResult(true);

        public void ReleaseSecondFailure() => secondFailureRelease.TrySetResult(true);

        public void ReleaseLaterSibling() => laterRelease.TrySetResult(true);
    }

    private sealed class SimultaneousValidationFailureGate
    {
        private readonly TaskCompletionSource<bool> failuresStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<bool> releaseFailures = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int failureCount;

        public async ValueTask<Validation<int, string>> SelectAsync(int value)
        {
            if (value < 3)
            {
                if (Interlocked.Increment(ref failureCount) == 3)
                {
                    failuresStarted.TrySetResult(true);
                }

                await releaseFailures.Task.ConfigureAwait(false);
                return value switch
                {
                    0 => Validation<int, string>.Invalid("zero"),
                    1 => Validation<int, string>.Invalid("one"),
                    _ => Validation<int, string>.Invalid("two"),
                };
            }

            return Validation<int, string>.Valid(value);
        }

        public Task WaitForFailuresStartedAsync() => failuresStarted.Task;

        public void ReleaseFailures() => releaseFailures.TrySetResult(true);
    }

    private sealed class TrackingAsyncEnumerable<T>(IReadOnlyList<T> values, Exception? disposeException = null) : IAsyncEnumerable<T>
    {
        public int ItemsYielded { get; private set; }

        public int DisposeCount { get; private set; }

        public CancellationToken ReceivedToken { get; private set; }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            ReceivedToken = cancellationToken;
            return new Enumerator(this, values, disposeException);
        }

        private sealed class Enumerator(
            TrackingAsyncEnumerable<T> owner,
            IReadOnlyList<T> values,
            Exception? disposeException) : IAsyncEnumerator<T>
        {
            private int index = -1;

            public T Current => values[index];

            public ValueTask<bool> MoveNextAsync()
            {
                var next = index + 1;
                if (next >= values.Count)
                {
                    return ValueTask.FromResult(false);
                }

                index = next;
                owner.ItemsYielded++;
                return ValueTask.FromResult(true);
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

}

#pragma warning restore xUnit1051
