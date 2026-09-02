using System.Threading.Tasks.Sources;

namespace FunnySharp.Tests;

public sealed class FirstSuccessTests
{
    [Fact]
    public void FirstSuccessAsyncRejectsInvalidArgumentsEagerly()
    {
        IEnumerable<Effect<Result<int, string>>>? effects = null;
        var oneEffect = new[] { Effect.FromResult(Result<int, string>.Success(1)) };

        Assert.Throws<ArgumentNullException>(() =>
            effects!.FirstSuccessAsync(TestContext.Current.CancellationToken));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            oneEffect.FirstSuccessAsync(TimeSpan.FromMilliseconds(-2), TestContext.Current.CancellationToken));
        Assert.Throws<ArgumentNullException>(() =>
            oneEffect.FirstSuccessAsync(
                TimeSpan.FromSeconds(1),
                null!,
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task FirstSuccessAsyncStartsColdEffectsAndUsesInputOrderForAlreadyCompletedSuccesses()
    {
        var starts = new List<int>();
        var effects = new[]
        {
            SuccessfulEffect(0, 10, starts),
            SuccessfulEffect(1, 20, starts),
            SuccessfulEffect(2, 30, starts),
        };

        var result = await effects.FirstSuccessAsync(TestContext.Current.CancellationToken);

        Assert.Equal([0, 1, 2], starts);
        Assert.Equal(10, GetValue(result));
    }

    [Fact]
    public async Task FirstSuccessAsyncAccumulatesAllTypedFailuresInInputOrderAndConsumesEachValueTaskOnce()
    {
        var first = CompletedValueTask(Result<int, string>.Failure("first"));
        var second = CompletedValueTask(Result<int, string>.Failure("second"));
        var third = CompletedValueTask(Result<int, string>.Failure("third"));
        var effects = new[]
        {
            Effect.FromValueTask<Result<int, string>>(_ => first.CreateValueTask()),
            Effect.FromValueTask<Result<int, string>>(_ => second.CreateValueTask()),
            Effect.FromValueTask<Result<int, string>>(_ => third.CreateValueTask()),
        };

        var result = await effects.FirstSuccessAsync(TestContext.Current.CancellationToken);

        Assert.True(result.TryGetErrors(out var errors));
        Assert.Equal(["first", "second", "third"], errors);
        Assert.Equal(1, first.GetResultCount);
        Assert.Equal(1, second.GetResultCount);
        Assert.Equal(1, third.GetResultCount);
    }

    [Fact]
    public async Task FirstSuccessAsyncReturnsALaterSuccessAfterAnEarlierTypedFailure()
    {
        var failure = new ControllableValueTaskSource<Result<int, string>>();
        var success = new ControllableValueTaskSource<Result<int, string>>();
        var effects = new[]
        {
            Effect.FromValueTask<Result<int, string>>(_ => failure.CreateValueTask()),
            Effect.FromValueTask<Result<int, string>>(_ => success.CreateValueTask()),
        };
        var operation = AwaitFirstSuccessAsync(effects, TestContext.Current.CancellationToken);

        failure.SetResult(Result<int, string>.Failure("unavailable"));
        success.SetResult(Result<int, string>.Success(42));

        Assert.Equal(42, GetValue(await operation));
        Assert.Equal(1, failure.GetResultCount);
        Assert.Equal(1, success.GetResultCount);
    }

    [Fact]
    public async Task FirstSuccessAsyncAggregatesSimultaneousFaultsInInputOrder()
    {
        var first = new InvalidOperationException("first");
        var second = new ArgumentException("second");
        var effects = new[]
        {
            Effect.FromValueTask<Result<int, string>>(_ => ValueTask.FromException<Result<int, string>>(first)),
            Effect.FromValueTask<Result<int, string>>(_ => ValueTask.FromException<Result<int, string>>(second)),
        };

        var actual = await Assert.ThrowsAsync<AggregateException>(() =>
            AwaitFirstSuccessAsync(effects, TestContext.Current.CancellationToken));

        Assert.Equal([first, second], actual.InnerExceptions);
    }

    [Fact]
    public async Task FirstSuccessAsyncPreservesASingleFaultByIdentity()
    {
        var expected = new InvalidOperationException("single");
        var effects = new[]
        {
            Effect.FromValueTask<Result<int, string>>(
                _ => ValueTask.FromException<Result<int, string>>(expected)),
        };

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            AwaitFirstSuccessAsync(effects, TestContext.Current.CancellationToken));

        Assert.Same(expected, actual);
    }

    [Fact]
    public async Task FirstSuccessAsyncPreservesSourceCancellationWhenNoCandidateSucceeds()
    {
        using var firstCancellation = new CancellationTokenSource();
        using var secondCancellation = new CancellationTokenSource();
        firstCancellation.Cancel();
        secondCancellation.Cancel();
        var effects = new[]
        {
            Effect.FromTask<Result<int, string>>(
                () => Task.FromCanceled<Result<int, string>>(firstCancellation.Token)),
            Effect.FromTask<Result<int, string>>(
                () => Task.FromCanceled<Result<int, string>>(secondCancellation.Token)),
        };
        var operation = AwaitFirstSuccessAsync(effects, TestContext.Current.CancellationToken);

        var actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation);

        Assert.True(operation.IsCanceled);
        Assert.Equal(firstCancellation.Token, actual.CancellationToken);
    }

    [Fact]
    public async Task FirstSuccessAsyncCancelsAndDrainsLosersAndObservesTheirFaultsBeforeReturning()
    {
        using var callerCancellation = new CancellationTokenSource();
        var winner = new ControllableValueTaskSource<Result<int, string>>();
        var slowLoser = new ControllableValueTaskSource<Result<int, string>>();
        var faultedLoser = new ControllableValueTaskSource<Result<int, string>>();
        var slowLoserCanceled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken slowLoserToken = default;
        CancellationToken faultedLoserToken = default;
        var expectedFault = new InvalidOperationException("loser fault");
        var effects = new[]
        {
            Effect.FromValueTask<Result<int, string>>(_ => winner.CreateValueTask()),
            Effect.FromValueTask<Result<int, string>>(token =>
            {
                slowLoserToken = token;
                token.Register(() => slowLoserCanceled.TrySetResult());
                return slowLoser.CreateValueTask();
            }),
            Effect.FromValueTask<Result<int, string>>(token =>
            {
                faultedLoserToken = token;
                token.Register(() => faultedLoser.SetException(expectedFault));
                return faultedLoser.CreateValueTask();
            }),
        };

        var operation = AwaitFirstSuccessAsync(effects, callerCancellation.Token);
        winner.SetResult(Result<int, string>.Success(42));

        await slowLoserCanceled.Task;
        Assert.False(operation.IsCompleted);
        Assert.NotEqual(callerCancellation.Token, slowLoserToken);
        Assert.NotEqual(callerCancellation.Token, faultedLoserToken);
        Assert.True(slowLoserToken.IsCancellationRequested);
        Assert.True(faultedLoserToken.IsCancellationRequested);

        slowLoser.SetResult(Result<int, string>.Failure("late"));
        var result = await operation;

        Assert.Equal(42, GetValue(result));
        Assert.Equal(1, faultedLoser.GetResultCount);
        Assert.Equal(1, slowLoser.GetResultCount);
    }

    [Fact]
    public async Task FirstSuccessAsyncRetainsCancellationCallbackFailuresAfterAWinner()
    {
        var winner = new ControllableValueTaskSource<Result<int, string>>();
        var loser = new ControllableValueTaskSource<Result<int, string>>();
        var expected = new InvalidOperationException("cancellation callback");
        var effects = new[]
        {
            Effect.FromValueTask<Result<int, string>>(_ => winner.CreateValueTask()),
            Effect.FromValueTask<Result<int, string>>(token =>
            {
                token.Register(() =>
                {
                    loser.SetResult(Result<int, string>.Failure("late"));
                    throw expected;
                });
                return loser.CreateValueTask();
            }),
        };
        var operation = AwaitFirstSuccessAsync(effects, TestContext.Current.CancellationToken);

        winner.SetResult(Result<int, string>.Success(42));
        var actual = await Assert.ThrowsAsync<AggregateException>(() => operation);

        Assert.Contains(expected, actual.Flatten().InnerExceptions);
        Assert.Equal(1, winner.GetResultCount);
        Assert.Equal(1, loser.GetResultCount);
    }

    [Fact]
    public async Task FirstSuccessAsyncPreservesTheCallersCancellationToken()
    {
        using var callerCancellation = new CancellationTokenSource();
        var first = new ControllableValueTaskSource<Result<int, string>>();
        var second = new ControllableValueTaskSource<Result<int, string>>();
        var effectTokens = new List<CancellationToken>();
        var effects = new[]
        {
            CancellableEffect(first, effectTokens),
            CancellableEffect(second, effectTokens),
        };

        var operation = AwaitFirstSuccessAsync(effects, callerCancellation.Token);
        callerCancellation.Cancel();

        var cancellation = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation);

        Assert.True(operation.IsCanceled);
        Assert.Equal(callerCancellation.Token, cancellation.CancellationToken);
        Assert.Equal(2, effectTokens.Count);
        Assert.All(effectTokens, token =>
        {
            Assert.NotEqual(callerCancellation.Token, token);
            Assert.True(token.IsCancellationRequested);
        });
        Assert.Equal(1, first.GetResultCount);
        Assert.Equal(1, second.GetResultCount);
    }

    [Fact]
    public async Task TimeoutOverloadCancelsCooperatingEffectsDrainsThemAndThrowsTimeoutException()
    {
        var timeProvider = new ManualTimeProvider();
        var first = new ControllableValueTaskSource<Result<int, string>>();
        var second = new ControllableValueTaskSource<Result<int, string>>();
        var effectTokens = new List<CancellationToken>();
        var effects = new[]
        {
            CancellableEffect(first, effectTokens),
            CancellableEffect(second, effectTokens),
        };

        var operation = AwaitFirstSuccessAsync(
            effects,
            TimeSpan.FromSeconds(5),
            timeProvider,
            TestContext.Current.CancellationToken);

        Assert.Equal(1, timeProvider.TimerCount);
        timeProvider.Advance(TimeSpan.FromSeconds(5));

        await Assert.ThrowsAsync<TimeoutException>(() => operation);
        Assert.Equal(2, effectTokens.Count);
        Assert.All(effectTokens, token => Assert.True(token.IsCancellationRequested));
        Assert.Equal(1, first.GetResultCount);
        Assert.Equal(1, second.GetResultCount);
    }

    [Fact]
    public async Task TimeoutAndCancellationTokenOverloadRunsAnImmediateSuccessWithoutWaitingForWallClockTime()
    {
        var effects = new[]
        {
            Effect.FromResult(Result<int, string>.Success(7)),
        };

        var result = await effects.FirstSuccessAsync(
            TimeSpan.FromDays(1),
            TestContext.Current.CancellationToken);

        Assert.Equal(7, GetValue(result));
    }

    [Fact]
    public async Task TimeoutDoesNotReplaceAWinnerWhileCanceledWorkDrains()
    {
        var timeProvider = new ManualTimeProvider();
        var winner = new ControllableValueTaskSource<Result<int, string>>();
        var loser = new ControllableValueTaskSource<Result<int, string>>();
        var loserCanceled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var effects = new[]
        {
            Effect.FromValueTask<Result<int, string>>(_ => winner.CreateValueTask()),
            Effect.FromValueTask<Result<int, string>>(token =>
            {
                token.Register(() => loserCanceled.TrySetResult());
                return loser.CreateValueTask();
            }),
        };
        var operation = AwaitFirstSuccessAsync(
            effects,
            TimeSpan.FromSeconds(5),
            timeProvider,
            TestContext.Current.CancellationToken);

        winner.SetResult(Result<int, string>.Success(42));
        await loserCanceled.Task;
        Assert.False(operation.IsCompleted);

        timeProvider.Advance(TimeSpan.FromSeconds(5));
        loser.SetResult(Result<int, string>.Failure("late"));

        Assert.Equal(42, GetValue(await operation));
    }

    [Fact]
    public async Task FirstSuccessAsyncRejectsAnEmptyInputExplicitly()
    {
        IEnumerable<Effect<Result<int, string>>> effects = [];

        await Assert.ThrowsAsync<ArgumentException>(() =>
            AwaitFirstSuccessAsync(effects, TestContext.Current.CancellationToken));
    }

    private static Effect<Result<int, string>> SuccessfulEffect(
        int index,
        int value,
        ICollection<int> starts) =>
        Effect.FromValueTask<Result<int, string>>(_ =>
        {
            starts.Add(index);
            return ValueTask.FromResult(Result<int, string>.Success(value));
        });

    private static Effect<Result<int, string>> CancellableEffect(
        ControllableValueTaskSource<Result<int, string>> source,
        ICollection<CancellationToken> tokens) =>
        Effect.FromValueTask<Result<int, string>>(token =>
        {
            tokens.Add(token);
            token.Register(() => source.SetException(new OperationCanceledException(token)));
            return source.CreateValueTask();
        });

    private static ControllableValueTaskSource<T> CompletedValueTask<T>(T result)
    {
        var source = new ControllableValueTaskSource<T>();
        source.SetResult(result);
        return source;
    }

    private static async Task<Validation<int, string>> AwaitFirstSuccessAsync(
        IEnumerable<Effect<Result<int, string>>> effects,
        CancellationToken cancellationToken) =>
        await effects.FirstSuccessAsync(cancellationToken);

    private static async Task<Validation<int, string>> AwaitFirstSuccessAsync(
        IEnumerable<Effect<Result<int, string>>> effects,
        TimeSpan timeout,
        TimeProvider timeProvider,
        CancellationToken cancellationToken) =>
        await effects.FirstSuccessAsync(timeout, timeProvider, cancellationToken);

    private static int GetValue(Validation<int, string> validation)
    {
        Assert.True(validation.TryGetValue(out var value));
        return value;
    }

    private sealed class ControllableValueTaskSource<T> : IValueTaskSource<T>
    {
        private ManualResetValueTaskSourceCore<T> source;

        public ControllableValueTaskSource() => source.RunContinuationsAsynchronously = true;

        public int GetResultCount { get; private set; }

        public ValueTask<T> CreateValueTask() => new(this, source.Version);

        public void SetResult(T result) => source.SetResult(result);

        public void SetException(Exception exception) => source.SetException(exception);

        T IValueTaskSource<T>.GetResult(short token)
        {
            GetResultCount++;
            return source.GetResult(token);
        }

        ValueTaskSourceStatus IValueTaskSource<T>.GetStatus(short token) => source.GetStatus(token);

        void IValueTaskSource<T>.OnCompleted(
            Action<object?> continuation,
            object? state,
            short token,
            ValueTaskSourceOnCompletedFlags flags) =>
            source.OnCompleted(continuation, state, token, flags);
    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        private readonly List<ManualTimer> timers = [];
        private DateTimeOffset utcNow = DateTimeOffset.UnixEpoch;

        public int TimerCount => timers.Count;

        public override DateTimeOffset GetUtcNow() => utcNow;

        public override long GetTimestamp() => utcNow.Ticks;

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

        public override ITimer CreateTimer(
            TimerCallback callback,
            object? state,
            TimeSpan dueTime,
            TimeSpan period)
        {
            var timer = new ManualTimer(this, callback, state, dueTime, period);
            timers.Add(timer);
            return timer;
        }

        public void Advance(TimeSpan elapsed)
        {
            if (elapsed < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(elapsed));
            }

            utcNow += elapsed;

            foreach (var timer in timers.ToArray())
            {
                timer.FireDueCallbacks();
            }
        }

        private sealed class ManualTimer(
            ManualTimeProvider timeProvider,
            TimerCallback callback,
            object? state,
            TimeSpan dueTime,
            TimeSpan period) : ITimer
        {
            private DateTimeOffset dueAt = timeProvider.utcNow + dueTime;
            private TimeSpan currentPeriod = period;
            private bool isDisposed;

            public bool Change(TimeSpan dueTime, TimeSpan period)
            {
                if (isDisposed)
                {
                    return false;
                }

                dueAt = timeProvider.utcNow + dueTime;
                currentPeriod = period;
                return true;
            }

            public void Dispose() => isDisposed = true;

            public ValueTask DisposeAsync()
            {
                Dispose();
                return ValueTask.CompletedTask;
            }

            public void FireDueCallbacks()
            {
                if (isDisposed || dueAt == DateTimeOffset.MaxValue || timeProvider.utcNow < dueAt)
                {
                    return;
                }

                callback(state);
                if (currentPeriod == Timeout.InfiniteTimeSpan)
                {
                    isDisposed = true;
                    return;
                }

                dueAt += currentPeriod;
            }
        }
    }
}
