namespace FunnySharp.Tests;

public sealed class EffectTests
{
    [Fact]
    public async Task EffectsAreDeferredAndEachRunExecutesTheWholeCompositionInOrder()
    {
        var trace = new List<string>();
        var effect = Effect.FromSync(() =>
            {
                trace.Add("source");
                return 2;
            })
            .Map(value =>
            {
                trace.Add("map");
                return value + 1;
            })
            .Bind(value => Effect.FromSync(() =>
            {
                trace.Add("bind");
                return value * 3;
            }));

        Assert.Empty(trace);

        Assert.Equal(9, await effect.RunAsync(TestContext.Current.CancellationToken));
        Assert.Equal(["source", "map", "bind"], trace);

        trace.Clear();
        Assert.Equal(9, await effect.RunAsync(TestContext.Current.CancellationToken));
        Assert.Equal(["source", "map", "bind"], trace);
    }

    [Fact]
    public async Task SelectAndSelectManySupportStandardQueryComposition()
    {
        var trace = new List<string>();
        var effect =
            from left in Effect.FromSync(() =>
            {
                trace.Add("left");
                return 4;
            })
            from right in Effect.FromSync(() =>
            {
                trace.Add("right");
                return 5;
            })
            select left + right;

        Assert.Equal(9, await effect.RunAsync(TestContext.Current.CancellationToken));
        Assert.Equal(["left", "right"], trace);
    }

    [Fact]
    public async Task EnvironmentEffectsComposeWithTheSameEnvironmentAndExactToken()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var environment = new TestEnvironment(7);
        var observations = new List<(TestEnvironment Environment, CancellationToken Token)>();
        var source = Effect.FromValueTask<TestEnvironment, int>((current, token) =>
        {
            observations.Add((current, token));
            return ValueTask.FromResult(current.Value);
        });

        var mappedAndBound = source
            .Map(value => value * 2)
            .Bind(value => Effect.FromValueTask<TestEnvironment, int>((current, token) =>
            {
                observations.Add((current, token));
                return ValueTask.FromResult(value + current.Value);
            }));
        var query =
            from left in source
            from right in Effect.FromValueTask<TestEnvironment, int>((current, token) =>
            {
                observations.Add((current, token));
                return ValueTask.FromResult(current.Value + left);
            })
            select left + right;

        Assert.Equal(21, await mappedAndBound.RunAsync(environment, cancellationSource.Token));
        Assert.Equal(21, await query.RunAsync(environment, cancellationSource.Token));
        Assert.Equal(4, observations.Count);
        Assert.All(observations, observation =>
        {
            Assert.Same(environment, observation.Environment);
            Assert.Equal(cancellationSource.Token, observation.Token);
        });
    }

    [Fact]
    public async Task DefaultEnvironmentEffectFailsAndLiftedEffectIgnoresEnvironmentWhileForwardingToken()
    {
        Effect<TestEnvironment, int> uninitialized = default;

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await uninitialized.RunAsync(new TestEnvironment(0), TestContext.Current.CancellationToken));

        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        CancellationToken observedToken = default;
        var lifted = Effect.FromSync(token =>
        {
            observedToken = token;
            return 8;
        }).WithEnvironment<TestEnvironment>();

        Assert.Equal(8, await lifted.RunAsync(new TestEnvironment(99), cancellationSource.Token));
        Assert.Equal(cancellationSource.Token, observedToken);
    }

    [Fact]
    public async Task FactoriesBridgeValuesSynchronousTasksValueTasksAndResults()
    {
        var fromValue = Effect.FromValue(1);
        var fromSync = Effect.FromSync(() => 2);
        var fromTask = Effect.FromTask(token =>
        {
            Assert.Equal(TestContext.Current.CancellationToken, token);
            return Task.FromResult(3);
        });
        var fromValueTask = Effect.FromValueTask(token =>
        {
            Assert.Equal(TestContext.Current.CancellationToken, token);
            return ValueTask.FromResult(4);
        });
        var failure = Result<int, string>.Failure("denied");
        var fromResult = Effect.FromResult(failure);

        Assert.Equal(1, await fromValue.RunAsync(TestContext.Current.CancellationToken));
        Assert.Equal(2, await fromSync.RunAsync(TestContext.Current.CancellationToken));
        Assert.Equal(3, await fromTask.RunAsync(TestContext.Current.CancellationToken));
        Assert.Equal(4, await fromValueTask.RunAsync(TestContext.Current.CancellationToken));
        Assert.Equal(failure, await fromResult.RunAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DomainFailureRemainsASuccessfulEffectValue()
    {
        var operation = Effect.FromResult(Result<int, string>.Failure("denied"))
            .RunAsync(TestContext.Current.CancellationToken)
            .AsTask();

        var result = await operation;

        Assert.True(operation.IsCompletedSuccessfully);
        Assert.True(result.IsFailure);
        Assert.True(result.TryGetError(out var error));
        Assert.Equal("denied", error);
    }

    [Fact]
    public async Task CallerTokensAreForwardedWithoutAnEagerCancellationCheck()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var observedTokens = new List<CancellationToken>();
        var effect = Effect.FromValueTask<int>(token =>
        {
            observedTokens.Add(token);
            return ValueTask.FromResult(42);
        });

        Assert.Equal(42, await effect.RunAsync(cancellationSource.Token));
        Assert.Equal([cancellationSource.Token], observedTokens);
    }

    [Fact]
    public async Task ProvideBindsOneEnvironmentAndForwardsEachRunToken()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var environment = new TestEnvironment(13);
        var observations = new List<(TestEnvironment Environment, CancellationToken Token)>();
        var effect = Effect.FromValueTask<TestEnvironment, int>((current, token) =>
            {
                observations.Add((current, token));
                return ValueTask.FromResult(current.Value);
            })
            .Provide(environment);

        Assert.Equal(13, await effect.RunAsync(cancellationSource.Token));
        Assert.Equal(13, await effect.RunAsync(TestContext.Current.CancellationToken));
        Assert.Equal(
            [
                (environment, cancellationSource.Token),
                (environment, TestContext.Current.CancellationToken),
            ],
            observations);
    }

    [Fact]
    public async Task RunAsyncReturnsTaskBackedValueTasksWithoutRewrappingThem()
    {
        var completion = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var environmentCompletion = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var effect = Effect.FromValueTask<int>(_ => new ValueTask<int>(completion.Task));
        var environmentEffect = Effect.FromValueTask<TestEnvironment, int>(
            (_, _) => new ValueTask<int>(environmentCompletion.Task));

        var operation = effect.RunAsync(TestContext.Current.CancellationToken).AsTask();
        var environmentOperation = environmentEffect
            .RunAsync(new TestEnvironment(0), TestContext.Current.CancellationToken)
            .AsTask();

        Assert.Same(completion.Task, operation);
        Assert.Same(environmentCompletion.Task, environmentOperation);

        completion.SetResult(17);
        environmentCompletion.SetResult(19);
        Assert.Equal(17, await operation);
        Assert.Equal(19, await environmentOperation);
    }

    [Fact]
    public async Task RunAsyncCapturesSynchronousDelegateFailuresInTheReturnedAwaitable()
    {
        var expected = new InvalidOperationException("sync failure");
        ValueTask<int> operation = default;

        var callException = Record.Exception(() =>
        {
            operation = Effect.FromSync<int>(() => throw expected).RunAsync(TestContext.Current.CancellationToken);
        });

        Assert.Null(callException);
        Assert.Same(
            expected,
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await operation));
    }

    [Fact]
    public async Task RunAsyncCapturesSynchronousCancellationInTheReturnedAwaitable()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var expected = new OperationCanceledException(cancellationSource.Token);
        ValueTask<int> operation = default;

        var callException = Record.Exception(() =>
            operation = Effect.FromSync<int>(() => throw expected)
                .RunAsync(TestContext.Current.CancellationToken));
        var task = operation.AsTask();

        Assert.Null(callException);
        var cancellation = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        Assert.True(task.IsCanceled);
        Assert.Equal(cancellationSource.Token, cancellation.CancellationToken);
    }

    [Fact]
    public async Task CompletedCompositionCapturesSynchronousStageFailuresInTheReturnedAwaitable()
    {
        var expected = new InvalidOperationException("stage failure");
        var source = Effect.FromValue(1);
        var effects = new[]
        {
            source.Map<int>(_ => throw expected),
            source.Bind<int>(_ => throw expected),
            source.SelectMany<int, int>(_ => Effect.FromValue(2), (_, _) => throw expected),
        };

        foreach (var effect in effects)
        {
            ValueTask<int> operation = default;
            var callException = Record.Exception(() =>
                operation = effect.RunAsync(TestContext.Current.CancellationToken));

            Assert.Null(callException);
            Assert.Same(
                expected,
                await Assert.ThrowsAsync<InvalidOperationException>(async () => await operation));
        }
    }

    [Fact]
    public async Task TaskAndValueTaskFactoriesCaptureSynchronousFactoryFailuresInTheReturnedAwaitable()
    {
        var taskFailure = new InvalidOperationException("task factory failure");
        var valueTaskFailure = new InvalidOperationException("value task factory failure");
        var tokenAwareTaskFailure = new InvalidOperationException("token-aware task factory failure");
        var tokenAwareValueTaskFailure = new InvalidOperationException("token-aware value task factory failure");
        var factories = new (Func<Effect<int>> Create, InvalidOperationException Expected)[]
        {
            (() => Effect.FromTask<int>(() => throw taskFailure), taskFailure),
            (() => Effect.FromValueTask<int>(() => throw valueTaskFailure), valueTaskFailure),
            (() => Effect.FromTask<int>(_ => throw tokenAwareTaskFailure), tokenAwareTaskFailure),
            (() => Effect.FromValueTask<int>(_ => throw tokenAwareValueTaskFailure), tokenAwareValueTaskFailure),
        };

        foreach (var (create, expected) in factories)
        {
            ValueTask<int> operation = default;

            var callException = Record.Exception(() =>
                operation = create().RunAsync(TestContext.Current.CancellationToken));

            Assert.Null(callException);
            Assert.Same(
                expected,
                await Assert.ThrowsAsync<InvalidOperationException>(async () => await operation));
        }
    }

    [Fact]
    public async Task TaskAndValueTaskFactoriesNaturallyPropagateFaultedAndCanceledCarriers()
    {
        var taskFault = new InvalidOperationException("task fault");
        var valueTaskFault = new InvalidOperationException("value task fault");

        Assert.Same(
            taskFault,
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await Effect.FromTask<int>(() => Task.FromException<int>(taskFault))
                    .RunAsync(TestContext.Current.CancellationToken)));
        Assert.Same(
            valueTaskFault,
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await Effect.FromValueTask<int>(() => ValueTask.FromException<int>(valueTaskFault))
                    .RunAsync(TestContext.Current.CancellationToken)));

        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var canceledFactories = new Func<Effect<int>>[]
        {
            () => Effect.FromTask<int>(() => Task.FromCanceled<int>(cancellationSource.Token)),
            () => Effect.FromValueTask<int>(() => ValueTask.FromCanceled<int>(cancellationSource.Token)),
        };

        foreach (var create in canceledFactories)
        {
            var operation = create().RunAsync(TestContext.Current.CancellationToken).AsTask();

            var cancellation = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation);
            Assert.True(operation.IsCanceled);
            Assert.Equal(cancellationSource.Token, cancellation.CancellationToken);
        }
    }

    [Fact]
    public async Task UpstreamFaultSkipsLaterMapBindAndQueryStages()
    {
        var expected = new InvalidOperationException("upstream fault");
        var executedStages = new List<string>();
        var source = Effect.FromSync<int>(() => throw expected);
        var compositions = new (string Name, Effect<int> Effect)[]
        {
            ("map", source.Map(value =>
            {
                executedStages.Add("map");
                return value + 1;
            })),
            ("bind", source.Bind(value =>
            {
                executedStages.Add("bind");
                return Effect.FromValue(value + 1);
            })),
            ("query",
                from value in source
                from next in Effect.FromSync(() =>
                {
                    executedStages.Add("query");
                    return value + 1;
                })
                select next),
        };

        foreach (var (_, effect) in compositions)
        {
            Assert.Same(
                expected,
                await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                    await effect.RunAsync(TestContext.Current.CancellationToken)));
        }

        Assert.Empty(executedStages);
    }

    [Fact]
    public async Task UpstreamCancellationSkipsLaterMapBindAndQueryStagesAndPreservesStatusAndToken()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var compositions = new Func<Effect<int>, Action, Effect<int>>[]
        {
            (source, later) => source.Map(value =>
            {
                later();
                return value + 1;
            }),
            (source, later) => source.Bind(value =>
            {
                later();
                return Effect.FromValue(value + 1);
            }),
            (source, later) =>
                from value in source
                from next in Effect.FromSync(() =>
                {
                    later();
                    return value + 1;
                })
                select next,
        };

        foreach (var compose in compositions)
        {
            var laterCalls = 0;
            var source = Effect.FromTask<int>(_ => Task.FromCanceled<int>(cancellationSource.Token));
            var operation = compose(source, () => laterCalls++)
                .RunAsync(TestContext.Current.CancellationToken)
                .AsTask();

            var cancellation = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation);
            Assert.True(operation.IsCanceled);
            Assert.Equal(cancellationSource.Token, cancellation.CancellationToken);
            Assert.Equal(0, laterCalls);
        }
    }

    [Fact]
    public async Task ValueTaskFactoryInvocationAndConsumptionAreExactlyOnceThroughComposition()
    {
        var source = new CountingValueTaskSource<int>(12);
        var factoryCalls = 0;
        var effect = Effect.FromValueTask<int>(_ =>
            {
                factoryCalls++;
                return source.CreateValueTask();
            })
            .Map(value => value * 2)
            .Bind(value => Effect.FromValue(value + 1));

        Assert.Equal(25, await effect.RunAsync(TestContext.Current.CancellationToken));
        Assert.Equal(1, factoryCalls);
        Assert.Equal(1, source.GetResultCount);
    }

    [Fact]
    public async Task IncompleteAsyncCompositionRunsEachStageOnceInOrder()
    {
        var completion = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var trace = new List<string>();
        var effect = Effect.FromValueTask<int>(_ => new ValueTask<int>(completion.Task))
            .Map(value =>
            {
                trace.Add("map");
                return value + 1;
            })
            .Bind(value =>
            {
                trace.Add("bind");
                return Effect.FromValue(value * 2);
            });

        var operation = effect.RunAsync(TestContext.Current.CancellationToken).AsTask();
        Assert.False(operation.IsCompleted);
        Assert.Empty(trace);

        completion.SetResult(4);

        Assert.Equal(10, await operation);
        Assert.Equal(["map", "bind"], trace);
    }

    [Fact]
    public async Task DefaultEffectFailsExplicitlyWhenRun()
    {
        Effect<int> effect = default;

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await effect.RunAsync(TestContext.Current.CancellationToken));
    }

    private sealed record TestEnvironment(int Value);
}
