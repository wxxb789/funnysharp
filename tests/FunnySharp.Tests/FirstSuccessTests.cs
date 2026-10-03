using System.Threading.Tasks.Sources;

namespace FunnySharp.Tests;

public sealed class FirstSuccessTests
{
    private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(30);

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
        Task<Result<int, string>>? winnerCompletion = null;
        Task<Result<int, string>>? slowLoserCompletion = null;
        var faultedLoserCanceled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var slowLoserCanceled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken slowLoserToken = default;
        CancellationToken faultedLoserToken = default;
        var expectedFault = new InvalidOperationException("loser fault");
        var effects = new[]
        {
            Effect.FromTask<Result<int, string>>(_ => winnerCompletion = winner.CreateValueTask().AsTask()),
            Effect.FromTask<Result<int, string>>(token =>
            {
                slowLoserToken = token;
                token.Register(() => slowLoserCanceled.TrySetResult());
                return slowLoserCompletion = slowLoser.CreateValueTask().AsTask();
            }),
            Effect.FromValueTask<Result<int, string>>(token =>
            {
                faultedLoserToken = token;
                token.Register(() => faultedLoserCanceled.TrySetResult());
                return faultedLoser.CreateValueTask();
            }),
        };

        var operation = AwaitFirstSuccessAsync(effects, callerCancellation.Token);
        winner.SetResult(Result<int, string>.Success(42));

        await slowLoserCanceled.Task.WaitAsync(TestTimeout, TestContext.Current.CancellationToken);
        await faultedLoserCanceled.Task.WaitAsync(TestTimeout, TestContext.Current.CancellationToken);
        // This is the test-owned source Task, not a claim about coordinator publication.
        Assert.NotNull(slowLoserCompletion);
        Assert.False(slowLoserCompletion.IsCompleted);
        Assert.NotEqual(callerCancellation.Token, slowLoserToken);
        Assert.NotEqual(callerCancellation.Token, faultedLoserToken);
        Assert.True(slowLoserToken.IsCancellationRequested);
        Assert.True(faultedLoserToken.IsCancellationRequested);

        slowLoser.SetResult(Result<int, string>.Failure("late"));
        _ = await slowLoserCompletion.WaitAsync(TestTimeout, TestContext.Current.CancellationToken);
        Assert.NotNull(winnerCompletion);
        _ = await winnerCompletion.WaitAsync(TestTimeout, TestContext.Current.CancellationToken);
        // The sole remaining candidate must supply this exact fault, not an early success.
        var failure = Assert.ThrowsAsync<InvalidOperationException>(() => operation);
        faultedLoser.SetException(expectedFault);
        var actual = await failure;

        Assert.Same(expectedFault, actual);
        Assert.Equal(1, winner.GetResultCount);
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
        foreach (var lateReply in new[]
        {
            Result<int, string>.Failure("late"),
            Result<int, string>.Success(99),
        })
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
            await loserCanceled.Task.WaitAsync(TestTimeout, TestContext.Current.CancellationToken);
            Assert.Equal(0, timeProvider.ScheduledTimerCount);

            timeProvider.Advance(TimeSpan.FromSeconds(5));
            // Late outcomes must neither replace the selected input nor revive its timeout.
            loser.SetResult(lateReply);

            Assert.Equal(42, GetValue(await operation));
            Assert.Equal(1, winner.GetResultCount);
            Assert.Equal(1, loser.GetResultCount);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CallerCancellationOverridesTimeoutSelectedBeforeCleanupCompletes(bool hasWinner)
    {
        var timeProvider = new ManualTimeProvider();
        using var callerCancellation = new CancellationTokenSource();
        var pending = new ControllableValueTaskSource<Result<int, string>>();
        var pendingCanceled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var effects = new[]
        {
            Effect.FromValueTask<Result<int, string>>(token =>
            {
                token.Register(() => pendingCanceled.TrySetResult());
                return pending.CreateValueTask();
            }),
            Effect.FromValueTask<Result<int, string>>(_ =>
            {
                timeProvider.Advance(TimeSpan.FromSeconds(5));
                return ValueTask.FromResult(hasWinner
                    ? Result<int, string>.Success(42)
                    : Result<int, string>.Failure("completed"));
            }),
        };
        var operation = AwaitFirstSuccessAsync(
            effects,
            TimeSpan.FromSeconds(5),
            timeProvider,
            callerCancellation.Token);

        await pendingCanceled.Task.WaitAsync(TestTimeout, TestContext.Current.CancellationToken);
        var cancellation = Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation);

        callerCancellation.Cancel();
        pending.SetResult(Result<int, string>.Failure("late"));

        var actual = await cancellation;
        Assert.Equal(callerCancellation.Token, actual.CancellationToken);
        Assert.True(operation.IsCanceled);
        Assert.Equal(1, pending.GetResultCount);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CallerCancellationRemainsPrimaryWhenTimeoutCleanupAlsoFails(bool hasWinner)
    {
        foreach (var faultDuringDrain in new[] { false, true })
        {
            var timeProvider = new ManualTimeProvider();
            using var callerCancellation = new CancellationTokenSource();
            var pending = new ControllableValueTaskSource<Result<int, string>>();
            var cleanupStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var cleanupFailure = new InvalidOperationException("cleanup callback");
            var drainFailure = new ArgumentException("last candidate");
            var effects = new[]
            {
                Effect.FromValueTask<Result<int, string>>(token =>
                {
                    token.Register(() =>
                    {
                        cleanupStarted.TrySetResult();
                        throw cleanupFailure;
                    });
                    return pending.CreateValueTask();
                }),
                Effect.FromValueTask<Result<int, string>>(_ =>
                {
                    timeProvider.Advance(TimeSpan.FromSeconds(5));
                    return ValueTask.FromResult(hasWinner
                        ? Result<int, string>.Success(42)
                        : Result<int, string>.Failure("completed"));
                }),
            };
            var operation = AwaitFirstSuccessAsync(
                effects,
                TimeSpan.FromSeconds(5),
                timeProvider,
                callerCancellation.Token);

            await cleanupStarted.Task.WaitAsync(TestTimeout, TestContext.Current.CancellationToken);
            var failure = Assert.ThrowsAsync<AggregateException>(() => operation);

            callerCancellation.Cancel();
            if (faultDuringDrain)
            {
                pending.SetException(drainFailure);
            }
            else
            {
                pending.SetResult(Result<int, string>.Failure("late"));
            }

            var actual = await failure;
            var primary = Assert.IsAssignableFrom<OperationCanceledException>(actual.InnerExceptions[0]);
            Assert.Equal(callerCancellation.Token, primary.CancellationToken);
            Exception[] expected = faultDuringDrain
                ? [primary, drainFailure, cleanupFailure]
                : [primary, cleanupFailure];
            Assert.Equal(expected, actual.Flatten().InnerExceptions);
            Assert.Equal(1, pending.GetResultCount);
        }
    }

    [Fact]
    public async Task FirstSuccessAsyncRejectsAnEmptyInputExplicitly()
    {
        IEnumerable<Effect<Result<int, string>>> effects = [];

        await Assert.ThrowsAsync<ArgumentException>(() =>
            AwaitFirstSuccessAsync(effects, TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task LegacyFirstSuccessOverloadsAdmitOnlyThirtyTwoOf1024SynchronousSuccesses(int overload)
    {
        var starts = new List<int>();
        var effects = Enumerable.Range(0, 1024)
            .Select(index => SuccessfulEffect(index, index, starts))
            .ToArray();

        var result = await (overload switch
        {
            0 => effects.FirstSuccessAsync(TestContext.Current.CancellationToken),
            1 => effects.FirstSuccessAsync(
                Timeout.InfiniteTimeSpan, TestContext.Current.CancellationToken),
            _ => effects.FirstSuccessAsync(
                Timeout.InfiniteTimeSpan, TimeProvider.System, TestContext.Current.CancellationToken),
        });

        Assert.Equal(0, GetValue(result));
        Assert.Equal(Enumerable.Range(0, 32), starts);
    }

    [Fact]
    public async Task LegacyFirstSuccessBoundsHeldCandidatesRefillsAfterFailureAndStopsAfterSelection()
    {
        var probe = new AdmissionProbe(1024);
        var operation = AwaitFirstSuccessAsync(probe.Effects, TestContext.Current.CancellationToken);
        try
        {
            await probe.WaitForStartAsync(31);
            probe.Results[0].SetResult(Result<int, string>.Failure("refill"));
            await probe.WaitForStartAsync(32);
            probe.Results[32].SetResult(Result<int, string>.Success(42));

            Assert.Equal(42, GetValue(await operation));
            Assert.Equal(33, probe.StartCount);
            Assert.Equal(32, probe.MaximumActive);
            Assert.Equal(0, probe.ActiveCount);
        }
        finally
        {
            probe.ReleaseAll();
            await DrainAfterAssertionAsync(operation);
        }
    }

    [Fact]
    public async Task FirstSuccessRetainsAFaultAlreadyAccountedForBeforeARefilledCandidateWins()
    {
        var expected = new InvalidOperationException("earlier fault");
        var probe = new AdmissionProbe(33);
        probe.Results[0].SetException(expected);
        var operation = AwaitFirstSuccessAsync(probe.Effects, TestContext.Current.CancellationToken);
        try
        {
            // Admission of index 32 witnesses accounting of the fault in the initial window.
            await probe.WaitForStartAsync(32);
            probe.Results[32].SetResult(Result<int, string>.Success(42));

            var actual = await Assert.ThrowsAsync<InvalidOperationException>(() => operation);

            Assert.Same(expected, actual);
            Assert.Equal(33, probe.StartCount);
            Assert.Equal(0, probe.ActiveCount);
        }
        finally
        {
            probe.ReleaseAll();
            await DrainAfterAssertionAsync(operation);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FirstSuccessRetainsEveryExceptionRepresentedByASourceTask(bool hasWinner)
    {
        var first = new InvalidOperationException("first");
        var second = new ArgumentException("second");
        var source = new TaskCompletionSource<Result<int, string>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        source.SetException([first, second]);
        var effects = new[]
        {
            Effect.FromTask<Result<int, string>>(() => source.Task),
            Effect.FromResult(hasWinner
                ? Result<int, string>.Success(42)
                : Result<int, string>.Failure("unavailable")),
        };

        var actual = await Assert.ThrowsAsync<AggregateException>(() =>
            AwaitFirstSuccessAsync(effects, TestContext.Current.CancellationToken));

        Assert.Equal([first, second], actual.InnerExceptions);
    }

    [Fact]
    public async Task FirstSuccessAccountsForEachCandidateEvenWhen1024CandidatesShareOneTask()
    {
        var shared = Task.FromResult(Result<int, string>.Failure("shared"));
        var effects = Enumerable.Range(0, 1024)
            .Select(_ => Effect.FromTask<Result<int, string>>(() => shared))
            .ToArray();

        var result = await AwaitFirstSuccessAsync(effects, TestContext.Current.CancellationToken);

        Assert.True(result.TryGetErrors(out var errors));
        Assert.Equal(Enumerable.Repeat("shared", 1024), errors);
    }

    [Fact]
    public async Task FirstSuccessDoesNotHideUnrelatedSourceCancellationBehindAWinner()
    {
        using var sourceCancellation = new CancellationTokenSource();
        sourceCancellation.Cancel();
        var effects = new[]
        {
            Effect.FromTask<Result<int, string>>(
                () => Task.FromCanceled<Result<int, string>>(sourceCancellation.Token)),
            Effect.FromResult(Result<int, string>.Success(42)),
        };
        var operation = AwaitFirstSuccessAsync(effects, TestContext.Current.CancellationToken);

        var actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation);

        Assert.Equal(sourceCancellation.Token, actual.CancellationToken);
        Assert.True(operation.IsCanceled);
    }

    [Fact]
    public async Task FirstSuccessRetainsAFaultedCancellationExceptionEvenWhenItUsesTheOperationToken()
    {
        var winner = new ControllableValueTaskSource<Result<int, string>>();
        var loser = new TaskCompletionSource<Result<int, string>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        OperationCanceledException? expected = null;
        var effects = new[]
        {
            Effect.FromValueTask<Result<int, string>>(_ => winner.CreateValueTask()),
            Effect.FromTask<Result<int, string>>(token =>
            {
                var fault = new OperationCanceledException(token);
                expected = fault;
                token.Register(() => loser.TrySetException(fault));
                return loser.Task;
            }),
        };
        var operation = AwaitFirstSuccessAsync(effects, TestContext.Current.CancellationToken);

        winner.SetResult(Result<int, string>.Success(42));
        var actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation);

        Assert.True(loser.Task.IsFaulted);
        Assert.Same(expected, actual);
        Assert.Equal(1, winner.GetResultCount);
    }

    [Fact]
    public async Task FirstSuccessAwaitsRealUsingAsyncDisposalAndPropagatesItsFailureAfterAWinner()
    {
        var winner = new ControllableValueTaskSource<Result<int, string>>();
        var resource = new GatedAsyncDisposable();
        var body = new TaskCompletionSource<Result<int, string>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var expected = new InvalidOperationException("dispose");
        var effects = new[]
        {
            Effect.FromValueTask<Result<int, string>>(_ => winner.CreateValueTask()),
            Effect.FromValue(resource).UsingAsync(_ =>
                Effect.FromTask<Result<int, string>>(token =>
                {
                    token.Register(() => body.TrySetCanceled(token));
                    return body.Task;
                })),
        };
        var operation = AwaitFirstSuccessAsync(effects, TestContext.Current.CancellationToken);
        try
        {
            winner.SetResult(Result<int, string>.Success(42));
            await resource.Started.Task.WaitAsync(TestTimeout, TestContext.Current.CancellationToken);
            // Only the test can release this gate; disposal entry is not coordinator quiescence.
            Assert.False(resource.Completion.Task.IsCompleted);
            Assert.True(body.Task.IsCanceled);
            var failure = Assert.ThrowsAsync<InvalidOperationException>(() => operation);
            resource.Completion.SetException(expected);

            var actual = await failure;

            Assert.True(resource.Completion.Task.IsFaulted);
            Assert.Same(expected, actual);
            Assert.Equal(1, resource.DisposeCount);
            Assert.Equal(1, winner.GetResultCount);
        }
        finally
        {
            resource.Completion.TrySetResult();
            await DrainAfterAssertionAsync(operation);
        }
    }

    [Fact]
    public async Task FirstSuccessRetainsEarlierDrainAndCancellationCallbackFaultsTogether()
    {
        var earlier = new InvalidOperationException("earlier");
        var firstDrain = new ArgumentException("first drain");
        var secondDrain = new NotSupportedException("second drain");
        var callback = new ApplicationException("callback");
        var winner = new ControllableValueTaskSource<Result<int, string>>();
        var loser = new TaskCompletionSource<Result<int, string>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var effects = new[]
        {
            Effect.FromTask<Result<int, string>>(
                () => Task.FromException<Result<int, string>>(earlier)),
            Effect.FromValueTask<Result<int, string>>(_ => winner.CreateValueTask()),
            Effect.FromTask<Result<int, string>>(token =>
            {
                token.Register(() =>
                {
                    loser.TrySetException([firstDrain, secondDrain]);
                    throw callback;
                });
                return loser.Task;
            }),
        };
        var operation = AwaitFirstSuccessAsync(effects, TestContext.Current.CancellationToken);

        winner.SetResult(Result<int, string>.Success(42));
        var actual = await Assert.ThrowsAsync<AggregateException>(() => operation);

        Assert.Equal(
            [earlier, firstDrain, secondDrain, callback],
            actual.Flatten().InnerExceptions);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FirstSuccessKeepsCancellationOrTimeoutPrimaryWithoutDroppingCandidateFaults(
        bool cancelCaller)
    {
        using var callerCancellation = new CancellationTokenSource();
        var timeProvider = new ManualTimeProvider();
        var earlier = new InvalidOperationException("earlier");
        var duringDrain = new ArgumentException("during drain");
        var pending = new TaskCompletionSource<Result<int, string>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var canceled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var effects = new[]
        {
            Effect.FromTask<Result<int, string>>(
                () => Task.FromException<Result<int, string>>(earlier)),
            Effect.FromTask<Result<int, string>>(token =>
            {
                token.Register(() => canceled.TrySetResult());
                return pending.Task;
            }),
        };
        var operation = AwaitFirstSuccessAsync(
            effects, TimeSpan.FromSeconds(5), timeProvider, callerCancellation.Token);
        try
        {
            if (cancelCaller)
            {
                callerCancellation.Cancel();
            }
            else
            {
                timeProvider.Advance(TimeSpan.FromSeconds(5));
            }

            await canceled.Task.WaitAsync(TestTimeout, TestContext.Current.CancellationToken);
            // This unreleased input is fixed test state, not a library Task-state oracle.
            Assert.False(pending.Task.IsCompleted);
            var failure = Assert.ThrowsAsync<AggregateException>(() => operation);
            pending.SetException(duringDrain);
            var actual = await failure;

            Assert.True(pending.Task.IsFaulted);
            Assert.Equal(3, actual.InnerExceptions.Count);
            if (cancelCaller)
            {
                var primary = Assert.IsAssignableFrom<OperationCanceledException>(actual.InnerExceptions[0]);
                Assert.Equal(callerCancellation.Token, primary.CancellationToken);
            }
            else
            {
                Assert.IsType<TimeoutException>(actual.InnerExceptions[0]);
            }

            Assert.Same(earlier, actual.InnerExceptions[1]);
            Assert.Same(duringDrain, actual.InnerExceptions[2]);
        }
        finally
        {
            pending.TrySetException(duringDrain);
            await DrainAfterAssertionAsync(operation);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FirstSuccessStopsAdmissionWhenTheCallerCancelsBeforeOrDuringStartup(bool beforeStartup)
    {
        using var callerCancellation = new CancellationTokenSource();
        var starts = 0;
        var effects = Enumerable.Range(0, 1024)
            .Select(_ => Effect.FromSync(() =>
            {
                starts++;
                callerCancellation.Cancel();
                return Result<int, string>.Failure("unavailable");
            }))
            .ToArray();
        if (beforeStartup)
        {
            callerCancellation.Cancel();
        }

        var actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            AwaitFirstSuccessAsync(effects, callerCancellation.Token));

        Assert.Equal(callerCancellation.Token, actual.CancellationToken);
        Assert.Equal(beforeStartup ? 0 : 1, starts);
    }

    [Fact]
    public async Task FirstSuccessStopsAdmissionWhenTheTimeoutExpiresDuringStartup()
    {
        var timeProvider = new ManualTimeProvider();
        var starts = 0;
        var effects = Enumerable.Range(0, 1024)
            .Select(_ => Effect.FromSync(() =>
            {
                starts++;
                timeProvider.Advance(TimeSpan.FromSeconds(5));
                return Result<int, string>.Failure("unavailable");
            }))
            .ToArray();

        await Assert.ThrowsAsync<TimeoutException>(() => AwaitFirstSuccessAsync(
            effects, TimeSpan.FromSeconds(5), timeProvider, TestContext.Current.CancellationToken));

        Assert.Equal(1, starts);
    }

    [Fact]
    public async Task FirstSuccessSnapshotsItsFiniteInputOnceBeforeStartingAnyCandidate()
    {
        var enumerations = 0;
        var yielded = 0;
        var starts = 0;
        var startedBeforeSnapshot = false;
        IEnumerable<Effect<Result<int, string>>> Input()
        {
            enumerations++;
            for (var index = 0; index < 1024; index++)
            {
                var error = index.ToString();
                yielded++;
                yield return Effect.FromSync(() =>
                {
                    startedBeforeSnapshot |= yielded != 1024;
                    starts++;
                    return Result<int, string>.Failure(error);
                });
            }
        }

        var result = await AwaitFirstSuccessAsync(Input(), TestContext.Current.CancellationToken);

        Assert.Equal(1, enumerations);
        Assert.Equal(1024, yielded);
        Assert.Equal(1024, starts);
        Assert.False(startedBeforeSnapshot);
        Assert.True(result.TryGetErrors(out var errors));
        Assert.Equal(Enumerable.Range(0, 1024).Select(index => index.ToString()), errors);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ExplicitFirstSuccessBoundIsValidatedBeforeEnumeratingTheInput(int maxConcurrency)
    {
        var enumerations = 0;
        IEnumerable<Effect<Result<int, string>>> Input()
        {
            enumerations++;
            yield return Effect.FromResult(Result<int, string>.Success(1));
        }

        Assert.Throws<ArgumentOutOfRangeException>(() => Input().FirstSuccessAsync(
            Timeout.InfiniteTimeSpan,
            TimeProvider.System,
            TestContext.Current.CancellationToken,
            maxConcurrency));
        Assert.Equal(0, enumerations);
    }

    [Fact]
    public async Task ExplicitBoundDoesNotRebindExistingDefaultLiteralOrMethodGroupCalls()
    {
        var effects = new[] { Effect.FromResult(Result<int, string>.Success(7)) };
        Func<IEnumerable<Effect<Result<int, string>>>, CancellationToken,
            ValueTask<Validation<int, string>>> existing = ConcurrentEffectExtensions.FirstSuccessAsync<int, string>;

        Assert.Throws<ArgumentNullException>(() => effects.FirstSuccessAsync(
            default, default!, TestContext.Current.CancellationToken));
        Assert.Equal(7, GetValue(await existing(effects, TestContext.Current.CancellationToken)));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(32)]
    public async Task ExplicitFirstSuccessBoundLimits1024HeldCandidatesAndRefillsExactlyOneSlot(
        int maxConcurrency)
    {
        var probe = new AdmissionProbe(1024);
        var operation = AwaitBoundedFirstSuccessAsync(probe.Effects, maxConcurrency);
        try
        {
            await probe.WaitForStartAsync(maxConcurrency - 1);
            probe.Results[0].SetResult(Result<int, string>.Failure("refill"));
            await probe.WaitForStartAsync(maxConcurrency);
            probe.Results[maxConcurrency].SetResult(Result<int, string>.Success(42));

            Assert.Equal(42, GetValue(await operation));
            Assert.Equal(maxConcurrency + 1, probe.StartCount);
            Assert.Equal(maxConcurrency, probe.MaximumActive);
            Assert.Equal(0, probe.ActiveCount);
        }
        finally
        {
            probe.ReleaseAll();
            await DrainAfterAssertionAsync(operation);
        }
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(2, true)]
    [InlineData(32, false)]
    [InlineData(32, true)]
    public async Task ExplicitFirstSuccessBoundStopsAdmissionAndDrainsOnTimeoutOrCallerCancellation(
        int maxConcurrency,
        bool cancelCaller)
    {
        using var callerCancellation = new CancellationTokenSource();
        var timeProvider = new ManualTimeProvider();
        var probe = new AdmissionProbe(1024);
        var operation = probe.Effects.FirstSuccessAsync(
            TimeSpan.FromSeconds(5), timeProvider, callerCancellation.Token, maxConcurrency)
            .AsTask().WaitAsync(TestTimeout, TestContext.Current.CancellationToken);
        try
        {
            await probe.WaitForStartAsync(maxConcurrency - 1);
            if (cancelCaller)
            {
                callerCancellation.Cancel();
                var actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation);
                Assert.Equal(callerCancellation.Token, actual.CancellationToken);
            }
            else
            {
                timeProvider.Advance(TimeSpan.FromSeconds(5));
                await Assert.ThrowsAsync<TimeoutException>(() => operation);
            }

            Assert.Equal(maxConcurrency, probe.StartCount);
            Assert.Equal(maxConcurrency, probe.MaximumActive);
            Assert.Equal(0, probe.ActiveCount);
            Assert.Equal(0, timeProvider.ScheduledTimerCount);
        }
        finally
        {
            probe.ReleaseAll();
            await DrainAfterAssertionAsync(operation);
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(32)]
    [InlineData(4096)]
    public async Task ExplicitFirstSuccessHandlesLargeSynchronousFailuresInOrderWithOneValueTaskConsumption(
        int maxConcurrency)
    {
        var sources = Enumerable.Range(0, 4096)
            .Select(index => CompletedValueTask(Result<int, string>.Failure(index.ToString())))
            .ToArray();
        var effects = sources
            .Select(source => Effect.FromValueTask<Result<int, string>>(_ => source.CreateValueTask()))
            .ToArray();

        var result = await AwaitBoundedFirstSuccessAsync(effects, maxConcurrency);

        Assert.True(result.TryGetErrors(out var errors));
        Assert.Equal(Enumerable.Range(0, 4096).Select(index => index.ToString()), errors);
        Assert.All(sources, source => Assert.Equal(1, source.GetResultCount));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(32)]
    [InlineData(1024)]
    public async Task ExplicitFirstSuccessAccountsForLargeStaggeredBatchesWithoutReorderingTypedFailures(
        int maxConcurrency)
    {
        var probe = new AdmissionProbe(1024);
        var operation = AwaitBoundedFirstSuccessAsync(probe.Effects, maxConcurrency);
        try
        {
            for (var start = 0; start < probe.Results.Length; start += maxConcurrency)
            {
                var end = Math.Min(start + maxConcurrency, probe.Results.Length);
                await probe.WaitForStartAsync(end - 1);
                for (var index = end - 1; index >= start; index--)
                {
                    probe.Results[index].SetResult(Result<int, string>.Failure(index.ToString()));
                }
            }

            var result = await operation;

            Assert.True(result.TryGetErrors(out var errors));
            Assert.Equal(Enumerable.Range(0, 1024).Select(index => index.ToString()), errors);
            Assert.Equal(1024, probe.StartCount);
            Assert.Equal(maxConcurrency, probe.MaximumActive);
            Assert.Equal(0, probe.ActiveCount);
        }
        finally
        {
            probe.ReleaseAll();
            await DrainAfterAssertionAsync(operation);
        }
    }

    private static async Task<Validation<int, string>> AwaitBoundedFirstSuccessAsync(
        IEnumerable<Effect<Result<int, string>>> effects,
        int maxConcurrency) =>
        await effects.FirstSuccessAsync(
            Timeout.InfiniteTimeSpan,
            TimeProvider.System,
            TestContext.Current.CancellationToken,
            maxConcurrency).AsTask().WaitAsync(TestTimeout);

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
        await effects.FirstSuccessAsync(cancellationToken).AsTask().WaitAsync(TestTimeout);

    private static async Task<Validation<int, string>> AwaitFirstSuccessAsync(
        IEnumerable<Effect<Result<int, string>>> effects,
        TimeSpan timeout,
        TimeProvider timeProvider,
        CancellationToken cancellationToken) =>
        await effects.FirstSuccessAsync(timeout, timeProvider, cancellationToken)
            .AsTask().WaitAsync(TestTimeout);

    // The assertion path checks the outcome. Teardown releases gates and observes that same task
    // without replacing an assertion failure with the operation's already-expected exception.
    private static async Task DrainAfterAssertionAsync(Task operation) =>
        await operation.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

    private static int GetValue(Validation<int, string> validation)
    {
        Assert.True(validation.TryGetValue(out var value));
        return value;
    }

    private sealed class AdmissionProbe
    {
        private readonly TaskCompletionSource[] started;
        private int activeCount;
        private int startCount;

        public AdmissionProbe(int count)
        {
            Results = Enumerable.Range(0, count)
                .Select(_ => new TaskCompletionSource<Result<int, string>>(
                    TaskCreationOptions.RunContinuationsAsynchronously))
                .ToArray();
            started = Enumerable.Range(0, count)
                .Select(_ => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously))
                .ToArray();
            Effects = Enumerable.Range(0, count)
                .Select(index => Effect.FromTask<Result<int, string>>(token => RunAsync(index, token)))
                .ToArray();
        }

        public TaskCompletionSource<Result<int, string>>[] Results { get; }

        public Effect<Result<int, string>>[] Effects { get; }

        public int ActiveCount => Volatile.Read(ref activeCount);

        public int StartCount => Volatile.Read(ref startCount);

        public int MaximumActive { get; private set; }

        public Task WaitForStartAsync(int index) => started[index].Task.WaitAsync(TestTimeout);

        public void ReleaseAll()
        {
            foreach (var result in Results)
            {
                result.TrySetResult(Result<int, string>.Failure("released"));
            }
        }

        private async Task<Result<int, string>> RunAsync(int index, CancellationToken token)
        {
            // Starts are serialized by the coordinator; completions may run on other threads.
            MaximumActive = Math.Max(MaximumActive, Interlocked.Increment(ref activeCount));
            Interlocked.Increment(ref startCount);
            using var registration = token.Register(() => Results[index].TrySetCanceled(token));
            started[index].TrySetResult();
            try
            {
                return await Results[index].Task.ConfigureAwait(false);
            }
            finally
            {
                Interlocked.Decrement(ref activeCount);
            }
        }
    }

    private sealed class GatedAsyncDisposable : IAsyncDisposable
    {
        public TaskCompletionSource Started { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Completion { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int DisposeCount { get; private set; }

        public async ValueTask DisposeAsync()
        {
            DisposeCount++;
            Started.TrySetResult();
            await Completion.Task.ConfigureAwait(false);
        }
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

        public int ScheduledTimerCount => timers.Count(timer => timer.IsScheduled);

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
            private DateTimeOffset dueAt = dueTime == Timeout.InfiniteTimeSpan
                ? DateTimeOffset.MaxValue
                : timeProvider.utcNow + dueTime;
            private TimeSpan currentPeriod = period;
            private bool isDisposed;

            public bool IsScheduled => !isDisposed && dueAt != DateTimeOffset.MaxValue;

            public bool Change(TimeSpan dueTime, TimeSpan period)
            {
                if (isDisposed)
                {
                    return false;
                }

                dueAt = dueTime == Timeout.InfiniteTimeSpan
                    ? DateTimeOffset.MaxValue
                    : timeProvider.utcNow + dueTime;
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
