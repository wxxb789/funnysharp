using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks.Sources;

namespace FunnySharp.Tests;

#pragma warning disable xUnit1051

public sealed class KeyedConcurrencyTests
{
    private static readonly TimeSpan GateTimeout = TimeSpan.FromSeconds(5);

    [Fact]
    public void RejectsInvalidArgumentsEagerly()
    {
        IAsyncEnumerable<int>? nullSource = null;
        var source = AsyncValues(1);

        Assert.Throws<ArgumentNullException>(() => nullSource!
            .SelectParallelByKeyCompletionOrderValueAsync(
                static value => value,
                1,
                1,
                static value => ValueTask.FromResult(Result<int, string>.Success(value))));
        Assert.Throws<ArgumentNullException>(() => source
            .SelectParallelByKeyCompletionOrderValueAsync(
                (Func<int, int>)null!,
                1,
                1,
                static value => ValueTask.FromResult(Result<int, string>.Success(value))));
        Assert.Throws<ArgumentNullException>(() => source
            .SelectParallelByKeyCompletionOrderValueAsync(
                static value => value,
                1,
                1,
                (Func<int, ValueTask<Result<int, string>>>)null!));
        Assert.Throws<ArgumentNullException>(() => source
            .SelectParallelByKeyCompletionOrderValueAsync(
                static value => value,
                1,
                1,
                (Func<int, CancellationToken, ValueTask<Result<int, string>>>)null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => source
            .SelectParallelByKeyCompletionOrderValueAsync(
                static value => value,
                0,
                1,
                static value => ValueTask.FromResult(Result<int, string>.Success(value))));
        Assert.Throws<ArgumentOutOfRangeException>(() => source
            .SelectParallelByKeyCompletionOrderValueAsync(
                static value => value,
                1,
                0,
                static value => ValueTask.FromResult(Result<int, string>.Success(value))));
        Assert.Throws<ArgumentOutOfRangeException>(() => source
            .SelectParallelByKeyCompletionOrderValueAsync(
                static value => value,
                -1,
                1,
                static value => ValueTask.FromResult(Result<int, string>.Success(value))));
        Assert.Throws<ArgumentOutOfRangeException>(() => source
            .SelectParallelByKeyCompletionOrderValueAsync(
                static value => value,
                1,
                -1,
                static value => ValueTask.FromResult(Result<int, string>.Success(value))));
    }

    [Fact]
    public async Task DefersSourceKeyAndSelectorUntilFirstPull()
    {
        var source = AsyncValues(1);
        var keyCalls = 0;
        var selectorCalls = 0;
        var pipeline = source.SelectParallelByKeyCompletionOrderValueAsync(
            value =>
            {
                keyCalls++;
                return value;
            },
            2,
            2,
            value =>
            {
                selectorCalls++;
                return ValueTask.FromResult(Result<int, string>.Success(value * 10));
            });

        Assert.Equal(0, source.EnumeratorCount);
        Assert.Equal(0, keyCalls);
        Assert.Equal(0, selectorCalls);

        await using var enumerator = pipeline.GetAsyncEnumerator();
        Assert.Equal(0, source.EnumeratorCount);
        Assert.Equal(0, keyCalls);
        Assert.Equal(0, selectorCalls);

        var result = await NextAsync(enumerator);
        Assert.Equal(1, result.Key);
        Assert.Equal(1, result.Source);
        Assert.True(result.Outcome.TryGetValue(out var selected));
        Assert.Equal(10, selected);
        Assert.Equal(1, source.EnumeratorCount);
        Assert.Equal(1, keyCalls);
        Assert.Equal(1, selectorCalls);
    }

    [Fact]
    public async Task SameKeyNeverOverlapsAndStartsAfterThePredecessorCompletes()
    {
        var log = new StartOrder();
        var a1Started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var a2Started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var b1Started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var a1Gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var a2Gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var b1Gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var source = new ControlledAsyncEnumerable<Item>(
        [
            new("A", 1),
            new("A", 2),
            new("B", 1),
        ]);

        var pipeline = source.SelectParallelByKeyCompletionOrderValueAsync(
            static item => item.Key,
            2,
            2,
            async (item, _) =>
            {
                var name = item.Key + item.Sequence;
                log.Begin(name);
                switch (name)
                {
                    case "A1":
                        a1Started.TrySetResult();
                        await a1Gate.Task.ConfigureAwait(false);
                        break;
                    case "A2":
                        a2Started.TrySetResult();
                        await a2Gate.Task.ConfigureAwait(false);
                        break;
                    default:
                        b1Started.TrySetResult();
                        await b1Gate.Task.ConfigureAwait(false);
                        break;
                }

                log.End(name);
                return Result<int, string>.Success(item.Sequence);
            });

        await using var enumerator = pipeline.GetAsyncEnumerator();
        var firstPull = PullAsync(enumerator);

        await a1Started.Task.WaitAsync(GateTimeout);
        await b1Started.Task.WaitAsync(GateTimeout);
        Assert.False(a2Started.Task.IsCompleted);
        Assert.Equal(2, log.MaxActive);
        Assert.Equal(["A1", "B1"], log.StartedNames);

        a1Gate.SetResult();
        var first = await firstPull.WaitAsync(GateTimeout);
        Assert.True(first);
        Assert.Equal("A", enumerator.Current.Key);
        Assert.Equal(1, enumerator.Current.Source.Sequence);

        await a2Started.Task.WaitAsync(GateTimeout);
        Assert.Equal(["A1", "B1", "A2"], log.StartedNames);
        Assert.Equal(["A1"], log.CompletedNames);
        Assert.Equal(2, log.MaxActive);

        a2Gate.SetResult();
        Assert.True(await PullAsync(enumerator).WaitAsync(GateTimeout));
        Assert.Equal("A", enumerator.Current.Key);
        Assert.Equal(2, enumerator.Current.Source.Sequence);

        b1Gate.SetResult();
        Assert.True(await PullAsync(enumerator).WaitAsync(GateTimeout));
        Assert.Equal("B", enumerator.Current.Key);
        Assert.Equal(1, enumerator.Current.Source.Sequence);
        Assert.False(await PullAsync(enumerator).WaitAsync(GateTimeout));
        Assert.Equal(1, source.DisposeCount);
    }

    [Fact]
    public async Task DifferentKeysRunConcurrentlyUpToTheBound()
    {
        var log = new StartOrder();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var gates = Enumerable.Range(0, 4)
            .Select(static _ => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously))
            .ToArray();
        var source = AsyncValues(0, 1, 2, 3);

        var pipeline = source.SelectParallelByKeyCompletionOrderValueAsync(
            static value => value,
            2,
            2,
            async (value, _) =>
            {
                log.Begin(value.ToString(System.Globalization.CultureInfo.InvariantCulture));
                if (log.Active == 2)
                {
                    started.TrySetResult();
                }

                await gates[value].Task.ConfigureAwait(false);
                log.End(value.ToString(System.Globalization.CultureInfo.InvariantCulture));
                return Result<int, string>.Success(value);
            });

        await using var enumerator = pipeline.GetAsyncEnumerator();
        var firstPull = PullAsync(enumerator);

        await started.Task.WaitAsync(GateTimeout);
        Assert.Equal(2, log.MaxActive);
        Assert.Equal(["0", "1"], log.StartedNames);

        gates[0].SetResult();
        Assert.True(await firstPull.WaitAsync(GateTimeout));
        Assert.Equal(3, log.StartedNames.Length);
        Assert.Equal(2, log.MaxActive);

        gates[1].SetResult();
        Assert.True(await PullAsync(enumerator).WaitAsync(GateTimeout));
        Assert.Equal(4, log.StartedNames.Length);
        Assert.Equal(2, log.MaxActive);

        gates[2].SetResult();
        gates[3].SetResult();
        Assert.True(await PullAsync(enumerator).WaitAsync(GateTimeout));
        Assert.True(await PullAsync(enumerator).WaitAsync(GateTimeout));
        Assert.False(await PullAsync(enumerator).WaitAsync(GateTimeout));
        Assert.Equal(2, log.MaxActive);
    }

    [Fact]
    public async Task OutcomesArriveInCompletionOrderAcrossDifferentKeys()
    {
        var aStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var bStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var aGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var bGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var source = AsyncValues(new Item("A", 1), new Item("B", 1));

        var pipeline = source.SelectParallelByKeyCompletionOrderValueAsync(
            static item => item.Key,
            2,
            2,
            async (item, _) =>
            {
                if (item.Key == "A")
                {
                    aStarted.TrySetResult();
                    await aGate.Task.ConfigureAwait(false);
                }
                else
                {
                    bStarted.TrySetResult();
                    await bGate.Task.ConfigureAwait(false);
                }

                return Result<int, string>.Success(item.Sequence);
            });

        await using var enumerator = pipeline.GetAsyncEnumerator();
        var firstPull = PullAsync(enumerator);

        await aStarted.Task.WaitAsync(GateTimeout);
        await bStarted.Task.WaitAsync(GateTimeout);

        bGate.SetResult();
        Assert.True(await firstPull.WaitAsync(GateTimeout));
        Assert.Equal("B", enumerator.Current.Key);

        aGate.SetResult();
        Assert.True(await PullAsync(enumerator).WaitAsync(GateTimeout));
        Assert.Equal("A", enumerator.Current.Key);
        Assert.False(await PullAsync(enumerator).WaitAsync(GateTimeout));
    }

    [Fact]
    public async Task TypedFailureIsDeliveredAndTheNextSameKeyOperationStillRuns()
    {
        var a1Started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var a2Started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var a1Gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var a2Gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var source = new ControlledAsyncEnumerable<Item>([new("A", 1), new("A", 2)]);

        var pipeline = source.SelectParallelByKeyCompletionOrderValueAsync(
            static item => item.Key,
            1,
            2,
            async (item, _) =>
            {
                if (item.Sequence == 1)
                {
                    a1Started.TrySetResult();
                    await a1Gate.Task.ConfigureAwait(false);
                    return Result<int, string>.Failure("account unavailable");
                }

                a2Started.TrySetResult();
                await a2Gate.Task.ConfigureAwait(false);
                return Result<int, string>.Success(item.Sequence);
            });

        await using var enumerator = pipeline.GetAsyncEnumerator();
        var firstPull = PullAsync(enumerator);

        await a1Started.Task.WaitAsync(GateTimeout);
        Assert.False(a2Started.Task.IsCompleted);

        a1Gate.SetResult();
        Assert.True(await firstPull.WaitAsync(GateTimeout));
        Assert.True(enumerator.Current.Outcome.IsFailure);
        Assert.True(enumerator.Current.Outcome.TryGetError(out var error));
        Assert.Equal("account unavailable", error);

        await a2Started.Task.WaitAsync(GateTimeout);
        a2Gate.SetResult();
        Assert.True(await PullAsync(enumerator).WaitAsync(GateTimeout));
        Assert.True(enumerator.Current.Outcome.IsSuccess);
        Assert.True(enumerator.Current.Outcome.TryGetValue(out var value));
        Assert.Equal(2, value);
        Assert.False(await PullAsync(enumerator).WaitAsync(GateTimeout));
    }

    [Fact]
    public async Task UninitializedResultIsATerminalProgrammingFault()
    {
        var source = AsyncValues(1);

        var pipeline = source.SelectParallelByKeyCompletionOrderValueAsync(
            static value => value,
            1,
            1,
            static _ => ValueTask.FromResult(default(Result<int, string>)));

        await using var enumerator = pipeline.GetAsyncEnumerator();
        _ = await Assert.ThrowsAsync<InvalidOperationException>(
            () => PullAsync(enumerator).WaitAsync(GateTimeout));
        Assert.Equal(1, source.DisposeCount);
    }

    [Fact]
    public async Task CallbackFaultStopsAdmissionDrainsStartedWorkAndPropagatesTheExactFault()
    {
        var expected = new InvalidOperationException("selector fault");
        var aStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var bStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var bCanceled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var aGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var source = AsyncValues(new Item("A", 1), new Item("B", 1));

        var pipeline = source.SelectParallelByKeyCompletionOrderValueAsync(
            static item => item.Key,
            2,
            2,
            async (item, cancellationToken) =>
            {
                if (item.Key == "A")
                {
                    aStarted.TrySetResult();
                    await aGate.Task.ConfigureAwait(false);
                    throw expected;
                }

                bStarted.TrySetResult();
                try
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    bCanceled.TrySetResult();
                    throw;
                }

                return Result<int, string>.Success(1);
            });

        await using var enumerator = pipeline.GetAsyncEnumerator();
        var firstPull = PullAsync(enumerator);

        await aStarted.Task.WaitAsync(GateTimeout);
        await bStarted.Task.WaitAsync(GateTimeout);
        aGate.SetResult();

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => firstPull.WaitAsync(GateTimeout));
        Assert.Same(expected, actual);
        await bCanceled.Task.WaitAsync(GateTimeout);
        Assert.Equal(1, source.DisposeCount);
    }

    [Fact]
    public async Task SourceFaultDrainsStartedWorkBeforePropagating()
    {
        var expected = new IOException("source fault");
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var source = new ControlledAsyncEnumerable<int>(
            [1, 2],
            beforeYield: (index, _) => index == 1
                ? ValueTask.FromException(new IOException("source fault"))
                : ValueTask.CompletedTask);

        var pipeline = source.SelectParallelByKeyCompletionOrderValueAsync(
            static value => value,
            2,
            2,
            async (value, _) =>
            {
                started.TrySetResult();
                await gate.Task.ConfigureAwait(false);
                return Result<int, string>.Success(value);
            });

        await using var enumerator = pipeline.GetAsyncEnumerator();
        var firstPull = PullAsync(enumerator);

        await started.Task.WaitAsync(GateTimeout);
        await Task.Yield();
        Assert.False(firstPull.IsCompleted);
        gate.SetResult();

        var actual = await Assert.ThrowsAsync<IOException>(() => firstPull.WaitAsync(GateTimeout));
        Assert.Same(expected.Message, actual.Message);
        Assert.Equal(1, source.DisposeCount);
    }

    [Fact]
    public async Task ConsumerCancellationPreservesTheCallerTokenAndWaitsForCleanup()
    {
        using var cancellationSource = new CancellationTokenSource();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken selectorToken = default;
        var source = AsyncValues(1);

        var pipeline = source.SelectParallelByKeyCompletionOrderValueAsync(
            static value => value,
            1,
            1,
            async (value, cancellationToken) =>
            {
                selectorToken = cancellationToken;
                started.TrySetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
                return Result<int, string>.Success(value);
            });

        await using var enumerator = pipeline.GetAsyncEnumerator(cancellationSource.Token);
        var pull = PullAsync(enumerator);

        await started.Task.WaitAsync(GateTimeout);
        cancellationSource.Cancel();

        var actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => pull.WaitAsync(GateTimeout));
        Assert.Equal(cancellationSource.Token, actual.CancellationToken);
        Assert.True(selectorToken.CanBeCanceled);
        Assert.True(selectorToken.IsCancellationRequested);
        Assert.Equal(source.ReceivedToken, selectorToken);
        Assert.Equal(1, source.DisposeCount);
    }

    [Fact]
    public async Task NonCooperativeCallbackIsAwaitedByCancellation()
    {
        using var cancellationSource = new CancellationTokenSource();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var source = AsyncValues(1);

        var pipeline = source.SelectParallelByKeyCompletionOrderValueAsync(
            static value => value,
            1,
            1,
            async (value, _) =>
            {
                started.TrySetResult();
                await release.Task.ConfigureAwait(false);
                return Result<int, string>.Success(value);
            });

        await using var enumerator = pipeline.GetAsyncEnumerator(cancellationSource.Token);
        var pull = PullAsync(enumerator);

        await started.Task.WaitAsync(GateTimeout);
        cancellationSource.Cancel();
        await Task.Yield();
        Assert.False(pull.IsCompleted);

        release.SetResult();
        var actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => pull.WaitAsync(GateTimeout));
        Assert.Equal(cancellationSource.Token, actual.CancellationToken);
        Assert.Equal(1, source.DisposeCount);
    }

    [Fact]
    public async Task EarlyDisposalDropsBufferedItemsAndWaitsForStartedWork()
    {
        var a1Started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var a1Gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var selectorCalls = 0;
        var source = AsyncValues(new Item("A", 1), new Item("A", 2), new Item("A", 3));

        var pipeline = source.SelectParallelByKeyCompletionOrderValueAsync(
            static item => item.Key,
            1,
            2,
            async (item, _) =>
            {
                Interlocked.Increment(ref selectorCalls);
                a1Started.TrySetResult();
                await a1Gate.Task.ConfigureAwait(false);
                return Result<int, string>.Success(item.Sequence);
            });

        var enumerator = pipeline.GetAsyncEnumerator();
        var firstPull = PullAsync(enumerator);

        await a1Started.Task.WaitAsync(GateTimeout);
        Assert.Equal(1, selectorCalls);

        var disposal = enumerator.DisposeAsync().AsTask();
        await Task.Yield();
        Assert.False(disposal.IsCompleted);

        a1Gate.SetResult();
        await disposal.WaitAsync(GateTimeout);
        Assert.Equal(1, selectorCalls);
        Assert.Equal(1, source.DisposeCount);
        _ = await Assert.ThrowsAsync<ObjectDisposedException>(() => PullAsync(enumerator));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => firstPull);
        await enumerator.DisposeAsync();
    }

    [Fact]
    public async Task DisposeBeforeFirstPullDoesNotEnumerateTheSource()
    {
        var source = AsyncValues(1);
        var pipeline = source.SelectParallelByKeyCompletionOrderValueAsync(
            static value => value,
            1,
            1,
            static value => ValueTask.FromResult(Result<int, string>.Success(value)));
        var enumerator = pipeline.GetAsyncEnumerator();

        await enumerator.DisposeAsync();

        Assert.Equal(0, source.EnumeratorCount);
        Assert.Equal(0, source.DisposeCount);
    }

    [Fact]
    public async Task ConcurrentMoveNextCallsAreRejected()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var source = AsyncValues(1);

        var pipeline = source.SelectParallelByKeyCompletionOrderValueAsync(
            static value => value,
            1,
            1,
            async (value, _) =>
            {
                started.TrySetResult();
                await gate.Task.ConfigureAwait(false);
                return Result<int, string>.Success(value);
            });

        await using var enumerator = pipeline.GetAsyncEnumerator();
        var firstPull = PullAsync(enumerator);

        await started.Task.WaitAsync(GateTimeout);
        _ = await Assert.ThrowsAsync<InvalidOperationException>(() => PullAsync(enumerator));

        gate.SetResult();
        Assert.True(await firstPull.WaitAsync(GateTimeout));
    }

    [Fact]
    public async Task NullKeyIsATerminalKeySelectorFault()
    {
        var selectorCalls = 0;
        var source = AsyncValues(1);

        var pipeline = source.SelectParallelByKeyCompletionOrderValueAsync<int, string, int, string>(
            static _ => null!,
            1,
            1,
            (value) =>
            {
                selectorCalls++;
                return ValueTask.FromResult(Result<int, string>.Success(value));
            });

        await using var enumerator = pipeline.GetAsyncEnumerator();
        var actual = await Assert.ThrowsAsync<ArgumentNullException>(
            () => PullAsync(enumerator).WaitAsync(GateTimeout));
        Assert.Equal("keySelector", actual.ParamName);
        Assert.Equal(0, selectorCalls);
        Assert.Equal(1, source.DisposeCount);
    }

    [Fact]
    public async Task KeySelectorFaultIsTerminalAndDropsTheSourceEnumerator()
    {
        var expected = new FormatException("bad key");
        var source = AsyncValues(1);

        var pipeline = source.SelectParallelByKeyCompletionOrderValueAsync<int, int, int, string>(
            _ => throw expected,
            1,
            1,
            static value => ValueTask.FromResult(Result<int, string>.Success(value)));

        await using var enumerator = pipeline.GetAsyncEnumerator();
        var actual = await Assert.ThrowsAsync<FormatException>(
            () => PullAsync(enumerator).WaitAsync(GateTimeout));
        Assert.Same(expected, actual);
        Assert.Equal(1, source.DisposeCount);
    }

    [Fact]
    public async Task ExplicitComparerSerializesEquivalentKeysAndPreservesTheObservedKey()
    {
        var firstStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var source = AsyncValues(new Item("A", 1), new Item("a", 2));

        var pipeline = source.SelectParallelByKeyCompletionOrderValueAsync(
            static item => item.Key,
            1,
            2,
            async (item, _) =>
            {
                if (item.Sequence == 1)
                {
                    firstStarted.TrySetResult();
                    await firstGate.Task.ConfigureAwait(false);
                }
                else
                {
                    secondStarted.TrySetResult();
                    await secondGate.Task.ConfigureAwait(false);
                }

                return Result<int, string>.Success(item.Sequence);
            },
            StringComparer.OrdinalIgnoreCase);

        await using var enumerator = pipeline.GetAsyncEnumerator();
        var firstPull = PullAsync(enumerator);

        await firstStarted.Task.WaitAsync(GateTimeout);
        Assert.False(secondStarted.Task.IsCompleted);

        firstGate.SetResult();
        Assert.True(await firstPull.WaitAsync(GateTimeout));
        Assert.Equal("A", enumerator.Current.Key);

        await secondStarted.Task.WaitAsync(GateTimeout);
        secondGate.SetResult();
        Assert.True(await PullAsync(enumerator).WaitAsync(GateTimeout));
        Assert.Equal("a", enumerator.Current.Key);
        Assert.False(await PullAsync(enumerator).WaitAsync(GateTimeout));
    }

    [Fact]
    public async Task UnboundedKeyCardinalityRetainsOnlyTheDeclaredWindow()
    {
        const int maxConcurrency = 2;
        const int maxBufferedItems = 2;
        var fourthYielded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var fifthReadAttempted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var startedCount = 0;
        var twoStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var source = new ControlledAsyncEnumerable<int>(
            Enumerable.Range(0, 10_000).ToArray(),
            beforeYield: (index, _) =>
            {
                if (index == 4)
                {
                    fifthReadAttempted.TrySetResult();
                }

                return ValueTask.CompletedTask;
            },
            afterYield: index =>
            {
                if (index == 3)
                {
                    fourthYielded.TrySetResult();
                }
            });

        var pipeline = source.SelectParallelByKeyCompletionOrderValueAsync(
            static value => value,
            maxConcurrency,
            maxBufferedItems,
            async (value, _) =>
            {
                if (Interlocked.Increment(ref startedCount) == 2)
                {
                    twoStarted.TrySetResult();
                }

                await gate.Task.ConfigureAwait(false);
                return Result<int, string>.Success(value);
            });

        await using var enumerator = pipeline.GetAsyncEnumerator();
        var firstPull = PullAsync(enumerator);

        await twoStarted.Task.WaitAsync(GateTimeout);
        await fourthYielded.Task.WaitAsync(GateTimeout);
        Assert.False(fifthReadAttempted.Task.IsCompleted);
        Assert.Equal(maxConcurrency + maxBufferedItems, source.ItemsYielded);
        Assert.Equal(2, startedCount);
        Assert.Equal(maxConcurrency + maxBufferedItems, LiveKeyStateCount(enumerator));

        gate.SetResult();
        Assert.True(await firstPull.WaitAsync(GateTimeout));
        await fifthReadAttempted.Task.WaitAsync(GateTimeout);

        // Drain the rest without holding the test open on the unbounded source.
        while (await PullAsync(enumerator).WaitAsync(GateTimeout))
        {
        }

        Assert.Equal(0, LiveKeyStateCount(enumerator));
        Assert.Equal(1, source.DisposeCount);
    }

    [Fact]
    public async Task ReadyKeysAreStartedFairlyAfterAnActiveHotKeyCompletes()
    {
        var a1Started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var a2Started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var b1Started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var a1Gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var a2Gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var b1Gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var source = AsyncValues(new Item("A", 1), new Item("A", 2), new Item("B", 1));

        var pipeline = source.SelectParallelByKeyCompletionOrderValueAsync(
            static item => item.Key,
            1,
            2,
            async (item, _) =>
            {
                var name = item.Key + item.Sequence;
                switch (name)
                {
                    case "A1":
                        a1Started.TrySetResult();
                        await a1Gate.Task.ConfigureAwait(false);
                        break;
                    case "A2":
                        a2Started.TrySetResult();
                        await a2Gate.Task.ConfigureAwait(false);
                        break;
                    default:
                        b1Started.TrySetResult();
                        await b1Gate.Task.ConfigureAwait(false);
                        break;
                }

                return Result<int, string>.Success(item.Sequence);
            });

        await using var enumerator = pipeline.GetAsyncEnumerator();
        var firstPull = PullAsync(enumerator);

        await a1Started.Task.WaitAsync(GateTimeout);
        Assert.False(a2Started.Task.IsCompleted);
        Assert.False(b1Started.Task.IsCompleted);

        a1Gate.SetResult();
        Assert.True(await firstPull.WaitAsync(GateTimeout));
        Assert.Equal("A", enumerator.Current.Key);

        await b1Started.Task.WaitAsync(GateTimeout);
        Assert.False(a2Started.Task.IsCompleted);

        b1Gate.SetResult();
        Assert.True(await PullAsync(enumerator).WaitAsync(GateTimeout));
        Assert.Equal("B", enumerator.Current.Key);

        await a2Started.Task.WaitAsync(GateTimeout);
        a2Gate.SetResult();
        Assert.True(await PullAsync(enumerator).WaitAsync(GateTimeout));
        Assert.Equal("A", enumerator.Current.Key);
        Assert.False(await PullAsync(enumerator).WaitAsync(GateTimeout));
    }

    [Fact]
    public async Task IndependentCallbackFaultsAreAggregatedWithoutLosingTheSecond()
    {
        var first = new InvalidOperationException("first fault");
        var second = new ArgumentException("second fault");
        var firstStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var terminalRecorded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var source = AsyncValues(new Item("A", 1), new Item("B", 1));

        var pipeline = source.SelectParallelByKeyCompletionOrderValueAsync<Item, string, int, string>(
            static item => item.Key,
            2,
            2,
            async (item, cancellationToken) =>
            {
                if (item.Key == "A")
                {
                    cancellationToken.Register(() => terminalRecorded.TrySetResult());
                    firstStarted.TrySetResult();
                    await firstGate.Task.ConfigureAwait(false);
                    throw first;
                }

                secondStarted.TrySetResult();
                await secondGate.Task.ConfigureAwait(false);
                throw second;
            });

        var enumerator = pipeline.GetAsyncEnumerator();
        var pull = PullAsync(enumerator);

        await firstStarted.Task.WaitAsync(GateTimeout);
        await secondStarted.Task.WaitAsync(GateTimeout);
        firstGate.SetResult();
        await terminalRecorded.Task.WaitAsync(GateTimeout);
        secondGate.SetResult();

        var actual = await Assert.ThrowsAsync<AggregateException>(() => pull.WaitAsync(GateTimeout));
        Assert.Contains(actual.InnerExceptions, exception => ReferenceEquals(exception, first));
        Assert.Contains(actual.InnerExceptions, exception => ReferenceEquals(exception, second));
        Assert.Equal(1, source.DisposeCount);

        var disposalFailure = await Assert.ThrowsAsync<ArgumentException>(
            () => enumerator.DisposeAsync().AsTask());
        Assert.Same(second, disposalFailure);
    }

    [Fact]
    public async Task DisposalDuringACallbackFaultObservesTheFault()
    {
        var expected = new InvalidOperationException("fault during disposal");
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var source = AsyncValues(1);

        var pipeline = source.SelectParallelByKeyCompletionOrderValueAsync<int, int, int, string>(
            static value => value,
            1,
            1,
            async (value, _) =>
            {
                started.TrySetResult();
                await gate.Task.ConfigureAwait(false);
                throw expected;
            });

        var enumerator = pipeline.GetAsyncEnumerator();
        var pull = PullAsync(enumerator);

        await started.Task.WaitAsync(GateTimeout);
        var disposal = enumerator.DisposeAsync().AsTask();
        await Task.Yield();
        Assert.False(disposal.IsCompleted);

        gate.SetResult();
        var actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => disposal.WaitAsync(GateTimeout));
        Assert.Same(expected, actual);
        Assert.Equal(1, source.DisposeCount);

        var pullFailure = await Record.ExceptionAsync(() => pull.WaitAsync(GateTimeout));
        Assert.NotNull(pullFailure);
        Assert.True(
            ReferenceEquals(pullFailure, expected) ||
            (pullFailure is AggregateException aggregate &&
             aggregate.InnerExceptions.Any(exception => ReferenceEquals(exception, expected))),
            "The in-flight pull must observe the callback fault.");
    }

    [Fact]
    public async Task BufferedSuccessIsYieldedBeforeTheTerminalCallbackFault()
    {
        var fault = new InvalidOperationException("terminal callback fault");
        var terminalRecorded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var source = AsyncValues(new Item("A", 1), new Item("B", 1), new Item("C", 1));

        var pipeline = source.SelectParallelByKeyCompletionOrderValueAsync(
            static item => item.Key,
            2,
            3,
            (item, cancellationToken) =>
            {
                if (item.Key == "C")
                {
                    cancellationToken.Register(() => terminalRecorded.TrySetResult());
                    throw fault;
                }

                return ValueTask.FromResult(Result<int, string>.Success(item.Sequence));
            });

        await using var enumerator = pipeline.GetAsyncEnumerator();

        Assert.True(await PullAsync(enumerator).WaitAsync(GateTimeout));
        Assert.Equal("A", enumerator.Current.Key);
        await terminalRecorded.Task.WaitAsync(GateTimeout);

        Assert.True(await PullAsync(enumerator).WaitAsync(GateTimeout));
        Assert.Equal("B", enumerator.Current.Key);
        Assert.True(enumerator.Current.Outcome.IsSuccess);

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => PullAsync(enumerator).WaitAsync(GateTimeout));
        Assert.Same(fault, actual);
        Assert.Equal(1, source.DisposeCount);
    }

    [Fact]
    public async Task SourceDisposeFaultIsRetainedAfterCleanCompletion()
    {
        var expected = new IOException("source dispose fault");
        var source = new ControlledAsyncEnumerable<int>([1], disposeFault: expected);

        var pipeline = source.SelectParallelByKeyCompletionOrderValueAsync(
            static value => value,
            1,
            1,
            static value => ValueTask.FromResult(Result<int, string>.Success(value)));

        var enumerator = pipeline.GetAsyncEnumerator();
        Assert.True(await PullAsync(enumerator).WaitAsync(GateTimeout));

        var actual = await Assert.ThrowsAsync<IOException>(
            () => PullAsync(enumerator).WaitAsync(GateTimeout));
        Assert.Same(expected, actual);
        Assert.Equal(1, source.DisposeCount);

        var disposalFailure = await Assert.ThrowsAsync<IOException>(
            () => enumerator.DisposeAsync().AsTask());
        Assert.Same(expected, disposalFailure);
    }

    [Fact]
    public async Task SourceAndSourceDisposeFaultsAreAggregatedAfterThePrimaryFailure()
    {
        var sourceFault = new IOException("source fault");
        var disposeFault = new InvalidOperationException("source dispose fault");
        var source = new ControlledAsyncEnumerable<int>(
            [1, 2],
            beforeYield: (index, _) => index == 1
                ? ValueTask.FromException(sourceFault)
                : ValueTask.CompletedTask,
            disposeFault: disposeFault);

        var pipeline = source.SelectParallelByKeyCompletionOrderValueAsync(
            static value => value,
            1,
            1,
            static value => ValueTask.FromResult(Result<int, string>.Success(value)));

        var enumerator = pipeline.GetAsyncEnumerator();
        Assert.True(await PullAsync(enumerator).WaitAsync(GateTimeout));

        var actual = await Assert.ThrowsAsync<AggregateException>(
            () => PullAsync(enumerator).WaitAsync(GateTimeout));
        Assert.Contains(actual.InnerExceptions, exception => ReferenceEquals(exception, sourceFault));
        Assert.Contains(actual.InnerExceptions, exception => ReferenceEquals(exception, disposeFault));
        Assert.Equal(1, source.DisposeCount);

        var disposalFailure = await Assert.ThrowsAsync<InvalidOperationException>(
            () => enumerator.DisposeAsync().AsTask());
        Assert.Same(disposeFault, disposalFailure);
    }

    [Fact]
    public async Task CancellationCallbackFailureIsAggregatedWithTheCallbackFault()
    {
        var selectorFault = new ApplicationException("selector fault");
        var cancellationFault = new InvalidOperationException("cancellation callback fault");
        var source = AsyncValues(1);

        var pipeline = source.SelectParallelByKeyCompletionOrderValueAsync(
            static value => value,
            1,
            1,
            (value, cancellationToken) =>
            {
                cancellationToken.Register(() => throw cancellationFault);
                return ValueTask.FromException<Result<int, string>>(selectorFault);
            });

        var enumerator = pipeline.GetAsyncEnumerator();
        var actual = await Assert.ThrowsAsync<AggregateException>(
            () => PullAsync(enumerator).WaitAsync(GateTimeout));
        Assert.Contains(actual.InnerExceptions, exception => ReferenceEquals(exception, selectorFault));
        Assert.Contains(
            actual.InnerExceptions,
            exception => ReferenceEquals(exception, cancellationFault));
        Assert.Equal(1, source.DisposeCount);

        var disposalFailure = await Assert.ThrowsAsync<InvalidOperationException>(
            () => enumerator.DisposeAsync().AsTask());
        Assert.Same(cancellationFault, disposalFailure);
    }

    [Fact]
    public async Task ConcurrentDisposeCallsShareTheSameCleanup()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var source = AsyncValues(1);

        var pipeline = source.SelectParallelByKeyCompletionOrderValueAsync(
            static value => value,
            1,
            1,
            async (value, _) =>
            {
                started.TrySetResult();
                await gate.Task.ConfigureAwait(false);
                return Result<int, string>.Success(value);
            });

        var enumerator = pipeline.GetAsyncEnumerator();
        var pull = PullAsync(enumerator);

        await started.Task.WaitAsync(GateTimeout);
        var first = enumerator.DisposeAsync().AsTask();
        var second = enumerator.DisposeAsync().AsTask();
        await Task.Yield();
        Assert.False(first.IsCompleted);
        Assert.False(second.IsCompleted);

        gate.SetResult();
        await Task.WhenAll(first, second).WaitAsync(GateTimeout);
        Assert.Equal(1, source.DisposeCount);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pull.WaitAsync(GateTimeout));
    }

    [Fact]
    public async Task HotKeyFillsThePendingBufferAndBlocksLaterKeys()
    {
        const int maxConcurrency = 2;
        const int maxBufferedItems = 2;
        var firstStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var fourthReadAttempted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var fifthReadAttempted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var source = new ControlledAsyncEnumerable<Item>(
            [
                new("A", 1),
                new("A", 2),
                new("A", 3),
                new("A", 4),
                new("B", 1),
            ],
            beforeYield: (index, _) =>
            {
                if (index == 3)
                {
                    fourthReadAttempted.TrySetResult();
                }

                if (index == 4)
                {
                    fifthReadAttempted.TrySetResult();
                }

                return ValueTask.CompletedTask;
            });

        var pipeline = source.SelectParallelByKeyCompletionOrderValueAsync(
            static item => item.Key,
            maxConcurrency,
            maxBufferedItems,
            async (item, _) =>
            {
                if (item.Key == "A" && item.Sequence == 1)
                {
                    firstStarted.TrySetResult();
                    await firstGate.Task.ConfigureAwait(false);
                }
                else if (item.Key == "A" && item.Sequence == 2)
                {
                    secondStarted.TrySetResult();
                    await secondGate.Task.ConfigureAwait(false);
                }

                return Result<int, string>.Success(item.Sequence);
            });

        await using var enumerator = pipeline.GetAsyncEnumerator();
        var firstPull = PullAsync(enumerator);

        await firstStarted.Task.WaitAsync(GateTimeout);
        Assert.Equal(maxConcurrency + maxBufferedItems - 1, source.ItemsYielded);
        Assert.False(fourthReadAttempted.Task.IsCompleted);

        firstGate.SetResult();
        Assert.True(await firstPull.WaitAsync(GateTimeout));
        Assert.Equal("A", enumerator.Current.Key);
        await secondStarted.Task.WaitAsync(GateTimeout);
        await fourthReadAttempted.Task.WaitAsync(GateTimeout);
        Assert.Equal(maxConcurrency + maxBufferedItems, source.ItemsYielded);
        Assert.False(fifthReadAttempted.Task.IsCompleted);

        secondGate.SetResult();
        Assert.True(await PullAsync(enumerator).WaitAsync(GateTimeout));
        await fifthReadAttempted.Task.WaitAsync(GateTimeout);

        while (await PullAsync(enumerator).WaitAsync(GateTimeout))
        {
        }

        Assert.Equal(1, source.DisposeCount);
    }

    [Fact]
    public async Task MalformedValueTaskStartFailureSettlesCleanupWithoutHanging()
    {
        var source = AsyncValues(1);

        var pipeline = source.SelectParallelByKeyCompletionOrderValueAsync(
            static value => value,
            1,
            1,
            static _ => new ThrowingValueTaskSource<Result<int, string>>().CreateValueTask());

        await using var enumerator = pipeline.GetAsyncEnumerator();
        var actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => PullAsync(enumerator).WaitAsync(GateTimeout));
        Assert.Equal("malformed ValueTask", actual.Message);
        Assert.Equal(1, source.DisposeCount);
    }

    private static int LiveKeyStateCount<T>(IAsyncEnumerator<T> enumerator)
    {
        var field = enumerator.GetType().GetField(
            "keyStates",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        var keyStates = field.GetValue(enumerator);
        Assert.NotNull(keyStates);
        var count = keyStates.GetType().GetProperty("Count");
        Assert.NotNull(count);
        return (int)count.GetValue(keyStates)!;
    }

    private static Task<bool> PullAsync<T>(IAsyncEnumerator<T> enumerator) =>
        enumerator.MoveNextAsync().AsTask();

    private static async Task<(TKey Key, TSource Source, Result<TResult, TError> Outcome)> NextAsync<TKey, TSource, TResult, TError>(
        IAsyncEnumerator<(TKey Key, TSource Source, Result<TResult, TError> Outcome)> enumerator)
    {
        Assert.True(await enumerator.MoveNextAsync().AsTask().WaitAsync(GateTimeout));
        return enumerator.Current;
    }

    private static ControlledAsyncEnumerable<T> AsyncValues<T>(params T[] values) => new(values);

    private readonly record struct Item(string Key, int Sequence);

    private sealed class ControlledAsyncEnumerable<T>(
        IReadOnlyList<T> values,
        Func<int, CancellationToken, ValueTask>? beforeYield = null,
        Action<int>? afterYield = null,
        Exception? disposeFault = null) : IAsyncEnumerable<T>
    {
        public int EnumeratorCount;
        public int ItemsYielded;
        public int DisposeCount;
        public CancellationToken ReceivedToken { get; private set; }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            ReceivedToken = cancellationToken;
            Interlocked.Increment(ref EnumeratorCount);
            return new Enumerator(this, values, beforeYield, afterYield, disposeFault);
        }

        private sealed class Enumerator(
            ControlledAsyncEnumerable<T> owner,
            IReadOnlyList<T> values,
            Func<int, CancellationToken, ValueTask>? beforeYield,
            Action<int>? afterYield,
            Exception? disposeFault) : IAsyncEnumerator<T>
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

                if (beforeYield is not null)
                {
                    await beforeYield(next, owner.ReceivedToken).ConfigureAwait(false);
                }

                index = next;
                Interlocked.Increment(ref owner.ItemsYielded);
                afterYield?.Invoke(next);
                return true;
            }

            public ValueTask DisposeAsync()
            {
                Interlocked.Increment(ref owner.DisposeCount);
                return disposeFault is null
                    ? ValueTask.CompletedTask
                    : ValueTask.FromException(disposeFault);
            }
        }
    }

    private sealed class ThrowingValueTaskSource<T> : IValueTaskSource<T>
    {
        public ValueTask<T> CreateValueTask() => new(this, 0);

        public T GetResult(short token) => throw new InvalidOperationException("malformed ValueTask");

        public ValueTaskSourceStatus GetStatus(short token) => ValueTaskSourceStatus.Pending;

        public void OnCompleted(
            Action<object?> continuation,
            object? state,
            short token,
            ValueTaskSourceOnCompletedFlags flags) =>
            throw new InvalidOperationException("malformed ValueTask");
    }

    private sealed class StartOrder
    {
        private readonly object gate = new();
        private readonly List<string> started = [];
        private readonly List<string> completed = [];
        private int active;

        public int Active
        {
            get
            {
                lock (gate)
                {
                    return active;
                }
            }
        }

        public int MaxActive { get; private set; }

        public string[] StartedNames
        {
            get
            {
                lock (gate)
                {
                    return [.. started];
                }
            }
        }

        public string[] CompletedNames
        {
            get
            {
                lock (gate)
                {
                    return [.. completed];
                }
            }
        }

        public void Begin(string name)
        {
            lock (gate)
            {
                active++;
                MaxActive = Math.Max(MaxActive, active);
                started.Add(name);
            }
        }

        public void End(string name)
        {
            lock (gate)
            {
                active--;
                completed.Add(name);
            }
        }
    }
}

#pragma warning restore xUnit1051
