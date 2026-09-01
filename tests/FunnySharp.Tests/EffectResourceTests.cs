namespace FunnySharp.Tests;

public sealed class EffectResourceTests
{
    [Fact]
    public async Task UsingAndUsingAsyncAreDeferredAndDisposeFreshResourcesExactlyOnceOnEveryRun()
    {
        var synchronousTrace = new List<string>();
        var synchronousResources = new List<TrackingDisposable>();
        var synchronous = Effect.FromSync(() =>
            {
                synchronousTrace.Add("acquire");
                var resource = new TrackingDisposable(synchronousTrace);
                synchronousResources.Add(resource);
                return resource;
            })
            .Using(_ => Effect.FromSync(() =>
            {
                synchronousTrace.Add("use");
                return 7;
            }));
        var asynchronousTrace = new List<string>();
        var asynchronousResources = new List<TrackingAsyncDisposable>();
        var asynchronous = Effect.FromSync(() =>
            {
                asynchronousTrace.Add("acquire");
                var resource = new TrackingAsyncDisposable(trace: asynchronousTrace);
                asynchronousResources.Add(resource);
                return resource;
            })
            .UsingAsync(_ => Effect.FromSync(() =>
            {
                asynchronousTrace.Add("use");
                return 9;
            }));

        Assert.Empty(synchronousTrace);
        Assert.Empty(asynchronousTrace);

        Assert.Equal(7, await synchronous.RunAsync(TestContext.Current.CancellationToken));
        Assert.Equal(7, await synchronous.RunAsync(TestContext.Current.CancellationToken));
        Assert.Equal(9, await asynchronous.RunAsync(TestContext.Current.CancellationToken));
        Assert.Equal(9, await asynchronous.RunAsync(TestContext.Current.CancellationToken));
        Assert.Equal(["acquire", "use", "dispose", "acquire", "use", "dispose"], synchronousTrace);
        Assert.Equal(["acquire", "use", "dispose", "acquire", "use", "dispose"], asynchronousTrace);
        Assert.Equal(2, synchronousResources.Count);
        Assert.Equal(2, asynchronousResources.Count);
        Assert.All(synchronousResources, resource => Assert.Equal(1, resource.DisposeCount));
        Assert.All(asynchronousResources, resource => Assert.Equal(1, resource.DisposeAsyncCount));
    }

    [Fact]
    public async Task UsingAndUsingAsyncReleaseResourcesWhenUseReturnsADomainFailure()
    {
        var synchronousResource = new TrackingDisposable();
        var asynchronousResource = new TrackingAsyncDisposable();

        var synchronous = await Effect.FromValue(synchronousResource)
            .Using(_ => Effect.FromValue(Result<int, string>.Failure("rejected")))
            .RunAsync(TestContext.Current.CancellationToken);
        var asynchronous = await Effect.FromValue(asynchronousResource)
            .UsingAsync(_ => Effect.FromValue(Result<int, string>.Failure("rejected")))
            .RunAsync(TestContext.Current.CancellationToken);

        Assert.Equal(Result<int, string>.Failure("rejected"), synchronous);
        Assert.Equal(Result<int, string>.Failure("rejected"), asynchronous);
        Assert.Equal(1, synchronousResource.DisposeCount);
        Assert.Equal(1, asynchronousResource.DisposeAsyncCount);
    }

    [Fact]
    public async Task UsingVariantsDoNotUseOrDisposeAfterAcquisitionFaultOrCancellation()
    {
        var fault = new InvalidOperationException("acquire fault");
        var useCalls = 0;
        var faulted = Effect.FromSync<TrackingDisposable>(() => throw fault)
            .Using(_ =>
            {
                useCalls++;
                return Effect.FromValue(1);
            });

        Assert.Same(
            fault,
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await faulted.RunAsync(TestContext.Current.CancellationToken)));
        Assert.Equal(0, useCalls);

        var asynchronousUseCalls = 0;
        var asynchronouslyFaulted = Effect.FromSync<TrackingAsyncDisposable>(() => throw fault)
            .UsingAsync(_ =>
            {
                asynchronousUseCalls++;
                return Effect.FromValue(1);
            });

        Assert.Same(
            fault,
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await asynchronouslyFaulted.RunAsync(TestContext.Current.CancellationToken)));
        Assert.Equal(0, asynchronousUseCalls);

        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var canceledUseCalls = 0;
        var canceled = Effect.FromTask<TrackingDisposable>(
                _ => Task.FromCanceled<TrackingDisposable>(cancellationSource.Token))
            .Using(_ =>
            {
                canceledUseCalls++;
                return Effect.FromValue(1);
            })
            .RunAsync(TestContext.Current.CancellationToken)
            .AsTask();

        var cancellation = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => canceled);
        Assert.True(canceled.IsCanceled);
        Assert.Equal(cancellationSource.Token, cancellation.CancellationToken);
        Assert.Equal(0, canceledUseCalls);

        var asynchronouslyCanceledUseCalls = 0;
        var asynchronouslyCanceled = Effect.FromTask<TrackingAsyncDisposable>(
                _ => Task.FromCanceled<TrackingAsyncDisposable>(cancellationSource.Token))
            .UsingAsync(_ =>
            {
                asynchronouslyCanceledUseCalls++;
                return Effect.FromValue(1);
            })
            .RunAsync(TestContext.Current.CancellationToken)
            .AsTask();

        var asynchronousCancellation = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => asynchronouslyCanceled);
        Assert.True(asynchronouslyCanceled.IsCanceled);
        Assert.Equal(cancellationSource.Token, asynchronousCancellation.CancellationToken);
        Assert.Equal(0, asynchronouslyCanceledUseCalls);
    }

    [Fact]
    public async Task UsingVariantsDisposeExactlyOnceAfterUseFaultAndCancellation()
    {
        var faultingResource = new TrackingDisposable();
        var fault = new InvalidOperationException("use fault");
        var faulted = Effect.FromValue(faultingResource)
            .Using(_ => Effect.FromSync<int>(() => throw fault));

        Assert.Same(
            fault,
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await faulted.RunAsync(TestContext.Current.CancellationToken)));
        Assert.Equal(1, faultingResource.DisposeCount);

        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var canceledResource = new TrackingDisposable();
        var canceled = Effect.FromValue(canceledResource)
            .Using(_ => Effect.FromTask<int>(_ => Task.FromCanceled<int>(cancellationSource.Token)))
            .RunAsync(TestContext.Current.CancellationToken)
            .AsTask();

        var cancellation = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => canceled);
        Assert.True(canceled.IsCanceled);
        Assert.Equal(cancellationSource.Token, cancellation.CancellationToken);
        Assert.Equal(1, canceledResource.DisposeCount);

        var asyncFaultingResource = new TrackingAsyncDisposable();
        var asynchronouslyFaulted = Effect.FromValue(asyncFaultingResource)
            .UsingAsync(_ => Effect.FromSync<int>(() => throw fault));

        Assert.Same(
            fault,
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await asynchronouslyFaulted.RunAsync(TestContext.Current.CancellationToken)));
        Assert.Equal(1, asyncFaultingResource.DisposeAsyncCount);

        var asyncCanceledResource = new TrackingAsyncDisposable();
        var asynchronouslyCanceled = Effect.FromValue(asyncCanceledResource)
            .UsingAsync(_ => Effect.FromTask<int>(_ => Task.FromCanceled<int>(cancellationSource.Token)))
            .RunAsync(TestContext.Current.CancellationToken)
            .AsTask();

        var asynchronousCancellation = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => asynchronouslyCanceled);
        Assert.True(asynchronouslyCanceled.IsCanceled);
        Assert.Equal(cancellationSource.Token, asynchronousCancellation.CancellationToken);
        Assert.Equal(1, asyncCanceledResource.DisposeAsyncCount);
    }

    [Fact]
    public async Task UsingAndUsingAsyncRejectNullResourcesWithoutInvokingUse()
    {
        TrackingDisposable? synchronousResource = null;
        TrackingAsyncDisposable? asynchronousResource = null;
        var synchronousUseCalls = 0;
        var asynchronousUseCalls = 0;

        var synchronous = Effect.FromValue(synchronousResource).Using(_ =>
        {
            synchronousUseCalls++;
            return Effect.FromValue(1);
        });
        var asynchronous = Effect.FromValue(asynchronousResource).UsingAsync(_ =>
        {
            asynchronousUseCalls++;
            return Effect.FromValue(1);
        });

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await synchronous.RunAsync(TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await asynchronous.RunAsync(TestContext.Current.CancellationToken));
        Assert.Equal(0, synchronousUseCalls);
        Assert.Equal(0, asynchronousUseCalls);
    }

    [Fact]
    public async Task DisposeFailuresTakePrecedenceOverUseFailures()
    {
        var useFault = new InvalidOperationException("use fault");
        var disposeFault = new InvalidOperationException("dispose fault");
        var asyncDisposeFault = new InvalidOperationException("async dispose fault");
        var synchronousResource = new TrackingDisposable(disposeException: disposeFault);
        var asynchronousResource = new TrackingAsyncDisposable(disposeException: asyncDisposeFault);

        var synchronous = Effect.FromValue(synchronousResource)
            .Using(_ => Effect.FromSync<int>(() => throw useFault));
        var asynchronous = Effect.FromValue(asynchronousResource)
            .UsingAsync(_ => Effect.FromSync<int>(() => throw useFault));

        Assert.Same(
            disposeFault,
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await synchronous.RunAsync(TestContext.Current.CancellationToken)));
        Assert.Same(
            asyncDisposeFault,
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await asynchronous.RunAsync(TestContext.Current.CancellationToken)));
        Assert.Equal(1, synchronousResource.DisposeCount);
        Assert.Equal(1, asynchronousResource.DisposeAsyncCount);
    }

    [Fact]
    public async Task DisposeFailuresAfterSuccessfulUseArePropagated()
    {
        var disposeFault = new InvalidOperationException("dispose fault");
        var asyncDisposeFault = new InvalidOperationException("async dispose fault");
        var synchronousResource = new TrackingDisposable(disposeException: disposeFault);
        var asynchronousResource = new TrackingAsyncDisposable(disposeException: asyncDisposeFault);

        Assert.Same(
            disposeFault,
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await Effect.FromValue(synchronousResource)
                    .Using(_ => Effect.FromValue(1))
                    .RunAsync(TestContext.Current.CancellationToken)));
        Assert.Same(
            asyncDisposeFault,
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await Effect.FromValue(asynchronousResource)
                    .UsingAsync(_ => Effect.FromValue(1))
                    .RunAsync(TestContext.Current.CancellationToken)));
        Assert.Equal(1, synchronousResource.DisposeCount);
        Assert.Equal(1, asynchronousResource.DisposeAsyncCount);
    }

    [Fact]
    public async Task DisposalCancellationTakesPrecedenceOverUseFailureForUsingAndUsingAsync()
    {
        var useFailure = new InvalidOperationException("use failure");
        using var disposalCancellationSource = new CancellationTokenSource();
        disposalCancellationSource.Cancel();
        var disposalCancellation = new OperationCanceledException(disposalCancellationSource.Token);
        var synchronousResource = new TrackingDisposable(disposeException: disposalCancellation);
        var asynchronousResource = new TrackingAsyncDisposable(disposeException: disposalCancellation);
        var synchronous = Effect.FromValue(synchronousResource)
            .Using(_ => Effect.FromSync<int>(() => throw useFailure))
            .RunAsync(TestContext.Current.CancellationToken)
            .AsTask();
        var asynchronous = Effect.FromValue(asynchronousResource)
            .UsingAsync(_ => Effect.FromSync<int>(() => throw useFailure))
            .RunAsync(TestContext.Current.CancellationToken)
            .AsTask();

        var synchronousCancellation = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => synchronous);
        var asynchronousCancellation = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => asynchronous);

        Assert.True(synchronous.IsCanceled);
        Assert.Equal(disposalCancellationSource.Token, synchronousCancellation.CancellationToken);
        Assert.True(asynchronous.IsCanceled);
        Assert.Equal(disposalCancellationSource.Token, asynchronousCancellation.CancellationToken);
        Assert.Equal(1, synchronousResource.DisposeCount);
        Assert.Equal(1, asynchronousResource.DisposeAsyncCount);
    }

    [Fact]
    public async Task UsingVariantsDisposeWhenUseSelectorThrowsSynchronously()
    {
        var expected = new InvalidOperationException("selector failure");
        var synchronousResource = new TrackingDisposable();
        var asynchronousResource = new TrackingAsyncDisposable();
        var synchronous = Effect.FromValue(synchronousResource)
            .Using(_ => ThrowSelector(expected));
        var asynchronous = Effect.FromValue(asynchronousResource)
            .UsingAsync(_ => ThrowSelector(expected));

        Assert.Same(
            expected,
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await synchronous.RunAsync(TestContext.Current.CancellationToken)));
        Assert.Same(
            expected,
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await asynchronous.RunAsync(TestContext.Current.CancellationToken)));
        Assert.Equal(1, synchronousResource.DisposeCount);
        Assert.Equal(1, asynchronousResource.DisposeAsyncCount);

        static Effect<int> ThrowSelector(Exception exception) => throw exception;
    }

    [Fact]
    public async Task EnvironmentUsingVariantsForwardTheSameEnvironmentAndTokenToAcquireAndUse()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var environment = new TestEnvironment(4);
        var observations = new List<(TestEnvironment Environment, CancellationToken Token)>();
        var synchronousResource = new TrackingDisposable();
        var asynchronousResource = new TrackingAsyncDisposable();
        var synchronous = Effect.FromValueTask<TestEnvironment, TrackingDisposable>((current, token) =>
            {
                observations.Add((current, token));
                return ValueTask.FromResult(synchronousResource);
            })
            .Using(_ => Effect.FromValueTask<TestEnvironment, int>((current, token) =>
            {
                observations.Add((current, token));
                return ValueTask.FromResult(current.Value);
            }));
        var asynchronous = Effect.FromValueTask<TestEnvironment, TrackingAsyncDisposable>((current, token) =>
            {
                observations.Add((current, token));
                return ValueTask.FromResult(asynchronousResource);
            })
            .UsingAsync(_ => Effect.FromValueTask<TestEnvironment, int>((current, token) =>
            {
                observations.Add((current, token));
                return ValueTask.FromResult(current.Value * 2);
            }));

        Assert.Equal(4, await synchronous.RunAsync(environment, cancellationSource.Token));
        Assert.Equal(8, await asynchronous.RunAsync(environment, cancellationSource.Token));
        Assert.Equal(4, observations.Count);
        Assert.All(observations, observation =>
        {
            Assert.Same(environment, observation.Environment);
            Assert.Equal(cancellationSource.Token, observation.Token);
        });
        Assert.Equal(1, synchronousResource.DisposeCount);
        Assert.Equal(1, asynchronousResource.DisposeAsyncCount);
    }

    [Fact]
    public async Task EnvironmentUsingVariantsDisposeExactlyOnceAfterUseFailureAndCancellation()
    {
        var environment = new TestEnvironment(4);
        var fault = new InvalidOperationException("use fault");
        var synchronousResource = new TrackingDisposable();
        var faulted = Effect.FromValueTask<TestEnvironment, TrackingDisposable>(
                (_, _) => ValueTask.FromResult(synchronousResource))
            .Using(_ => Effect.FromSync<TestEnvironment, int>(_ => throw fault));

        Assert.Same(
            fault,
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await faulted.RunAsync(environment, TestContext.Current.CancellationToken)));
        Assert.Equal(1, synchronousResource.DisposeCount);

        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var asynchronousResource = new TrackingAsyncDisposable();
        var canceled = Effect.FromValueTask<TestEnvironment, TrackingAsyncDisposable>(
                (_, _) => ValueTask.FromResult(asynchronousResource))
            .UsingAsync(_ => Effect.FromTask<TestEnvironment, int>(
                (_, _) => Task.FromCanceled<int>(cancellationSource.Token)))
            .RunAsync(environment, TestContext.Current.CancellationToken)
            .AsTask();

        var cancellation = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => canceled);
        Assert.True(canceled.IsCanceled);
        Assert.Equal(cancellationSource.Token, cancellation.CancellationToken);
        Assert.Equal(1, asynchronousResource.DisposeAsyncCount);
    }

    [Fact]
    public async Task UsingAsyncWaitsForPendingDisposalAndConsumesTheAcquisitionValueTaskOnce()
    {
        var disposalCompletion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var resource = new TrackingAsyncDisposable(disposalCompletion);
        var source = new CountingValueTaskSource<TrackingAsyncDisposable>(resource);
        var factoryCalls = 0;
        var effect = Effect.FromValueTask<TrackingAsyncDisposable>(_ =>
            {
                factoryCalls++;
                return source.CreateValueTask();
            })
            .UsingAsync(_ => Effect.FromValue(11));

        var operation = effect.RunAsync(TestContext.Current.CancellationToken).AsTask();

        Assert.False(operation.IsCompleted);
        Assert.Equal(1, resource.DisposeAsyncCount);
        Assert.Equal(1, factoryCalls);
        Assert.Equal(1, source.GetResultCount);

        disposalCompletion.SetResult(true);

        Assert.Equal(11, await operation);
        Assert.Equal(1, resource.DisposeAsyncCount);
    }

    [Fact]
    public async Task UsingSelectsSynchronousDisposalAndUsingAsyncSelectsAsynchronousDisposal()
    {
        var synchronousResource = new DualDisposable();
        var asynchronousResource = new DualDisposable();

        Assert.Equal(
            1,
            await Effect.FromValue(synchronousResource)
                .Using(_ => Effect.FromValue(1))
                .RunAsync(TestContext.Current.CancellationToken));
        Assert.Equal(
            2,
            await Effect.FromValue(asynchronousResource)
                .UsingAsync(_ => Effect.FromValue(2))
                .RunAsync(TestContext.Current.CancellationToken));

        Assert.Equal(1, synchronousResource.DisposeCount);
        Assert.Equal(0, synchronousResource.DisposeAsyncCount);
        Assert.Equal(0, asynchronousResource.DisposeCount);
        Assert.Equal(1, asynchronousResource.DisposeAsyncCount);
    }

    private sealed class TrackingDisposable(
        List<string>? trace = null,
        Exception? disposeException = null) : IDisposable
    {
        public int DisposeCount { get; private set; }

        public void Dispose()
        {
            DisposeCount++;
            trace?.Add("dispose");
            if (disposeException is not null)
            {
                throw disposeException;
            }
        }
    }

    private sealed class TrackingAsyncDisposable(
        TaskCompletionSource<bool>? disposalCompletion = null,
        Exception? disposeException = null,
        List<string>? trace = null) : IAsyncDisposable
    {
        public int DisposeAsyncCount { get; private set; }

        public ValueTask DisposeAsync()
        {
            DisposeAsyncCount++;
            trace?.Add("dispose");
            if (disposeException is not null)
            {
                return ValueTask.FromException(disposeException);
            }

            return disposalCompletion is null
                ? ValueTask.CompletedTask
                : new ValueTask(disposalCompletion.Task);
        }
    }

    private sealed record TestEnvironment(int Value);

    private sealed class DualDisposable : IDisposable, IAsyncDisposable
    {
        public int DisposeCount { get; private set; }

        public int DisposeAsyncCount { get; private set; }

        public void Dispose() => DisposeCount++;

        public ValueTask DisposeAsync()
        {
            DisposeAsyncCount++;
            return ValueTask.CompletedTask;
        }
    }
}
