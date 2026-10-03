using System.Threading.Tasks.Sources;
using FunnySharp;

namespace FunnySharp.Tests;

public sealed class StableEffectConsumptionContractTests(ITestOutputHelper output)
{
    [Fact]
    public async Task EnvironmentMapAwaitsItsPendingSourceOnceBeforeInvokingTheSelectorWithTheExactValue()
    {
        var environment = new object();
        var source = new StableEffectPendingSource<int>();
        var sourceCalls = 0;
        var selectorCalls = 0;
        var effect = Effect.FromValueTask<object, int>((current, token) =>
        {
            sourceCalls++;
            Assert.Same(environment, current);
            Assert.Equal(TestContext.Current.CancellationToken, token);
            return source.Operation;
        }).Map(value =>
        {
            selectorCalls++;
            Assert.Equal(7, value);
            return value + 3;
        });

        Assert.Equal(0, sourceCalls);
        var operation = effect.RunAsync(environment, TestContext.Current.CancellationToken).AsTask();
        Assert.False(operation.IsCompleted);
        Assert.Equal(0, selectorCalls);
        await source.ReleaseAsync(7);
        Assert.Equal(10, await operation.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
        Assert.Equal(1, sourceCalls);
        Assert.Equal(1, selectorCalls);
        AssertConsumedOnce(source);
        output.WriteLine("C:104: source GetResult=1, OnCompleted=1; source/selector calls=1/1; output=10; environment=same; caller-token=exact");
    }

    [Fact]
    public async Task EnvironmentBindAwaitsThePendingSourceAndSelectedInnerRunOnceWithTheSameEnvironmentAndToken()
    {
        var environment = new object();
        var source = new StableEffectPendingSource<int>();
        var inner = new StableEffectPendingSource<int>();
        var sourceCalls = 0;
        var binderCalls = 0;
        var innerCalls = 0;
        var effect = Effect.FromValueTask<object, int>((current, token) =>
        {
            sourceCalls++;
            Assert.Same(environment, current);
            Assert.Equal(TestContext.Current.CancellationToken, token);
            return source.Operation;
        }).Bind(value =>
        {
            binderCalls++;
            Assert.Equal(7, value);
            return Effect.FromValueTask<object, int>((current, token) =>
            {
                innerCalls++;
                Assert.Same(environment, current);
                Assert.Equal(TestContext.Current.CancellationToken, token);
                return inner.Operation;
            });
        });

        Assert.Equal(0, sourceCalls);
        var operation = effect.RunAsync(environment, TestContext.Current.CancellationToken).AsTask();
        Assert.False(operation.IsCompleted);
        Assert.Equal(0, binderCalls);
        Assert.Equal(0, innerCalls);
        await source.ReleaseAsync(7);
        await inner.WaitForSubscriptionAsync();
        Assert.False(operation.IsCompleted);
        Assert.Equal(1, binderCalls);
        Assert.Equal(1, innerCalls);
        AssertConsumedOnce(source);
        await inner.ReleaseAsync(19);
        Assert.Equal(19, await operation.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
        Assert.Equal(1, sourceCalls);
        Assert.Equal(1, binderCalls);
        Assert.Equal(1, innerCalls);
        AssertConsumedOnce(source);
        AssertConsumedOnce(inner);
        output.WriteLine("C:102: source/inner GetResult=1/1, OnCompleted=1/1; source/binder/inner calls=1/1/1; output=19; environment=same at both runners; caller-token=exact at both runners");
    }

    [Fact]
    public async Task EnvironmentQueryAwaitsItsPendingSourceAndInnerRunOnceBeforeProjectingBothExactValues()
    {
        var environment = new object();
        var source = new StableEffectPendingSource<int>();
        var inner = new StableEffectPendingSource<int>();
        var sourceCalls = 0;
        var binderCalls = 0;
        var innerCalls = 0;
        var projectorCalls = 0;
        var effect = Effect.FromValueTask<object, int>((current, token) =>
        {
            sourceCalls++;
            Assert.Same(environment, current);
            Assert.Equal(TestContext.Current.CancellationToken, token);
            return source.Operation;
        }).SelectMany(value =>
        {
            binderCalls++;
            Assert.Equal(7, value);
            return Effect.FromValueTask<object, int>((current, token) =>
            {
                innerCalls++;
                Assert.Same(environment, current);
                Assert.Equal(TestContext.Current.CancellationToken, token);
                return inner.Operation;
            });
        }, (value, selected) =>
        {
            projectorCalls++;
            Assert.Equal(7, value);
            Assert.Equal(19, selected);
            return value + selected;
        });

        Assert.Equal(0, sourceCalls);
        var operation = effect.RunAsync(environment, TestContext.Current.CancellationToken).AsTask();
        Assert.False(operation.IsCompleted);
        Assert.Equal(0, binderCalls);
        Assert.Equal(0, innerCalls);
        Assert.Equal(0, projectorCalls);
        await source.ReleaseAsync(7);
        await inner.WaitForSubscriptionAsync();
        Assert.False(operation.IsCompleted);
        Assert.Equal(1, binderCalls);
        Assert.Equal(1, innerCalls);
        Assert.Equal(0, projectorCalls);
        AssertConsumedOnce(source);
        await inner.ReleaseAsync(19);
        Assert.Equal(26, await operation.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
        Assert.Equal(1, sourceCalls);
        Assert.Equal(1, binderCalls);
        Assert.Equal(1, innerCalls);
        Assert.Equal(1, projectorCalls);
        AssertConsumedOnce(source);
        AssertConsumedOnce(inner);
        output.WriteLine("C:103: source/inner GetResult=1/1, OnCompleted=1/1; source/binder/inner/projector calls=1/1/1/1; output=26; environment=same at both runners; caller-token=exact at both runners");
    }

    [Fact]
    public async Task EnvironmentUsingConsumesPendingAcquisitionAndUseOnceThenSynchronouslyDisposesTheExactResourceOnce()
    {
        var environment = new object();
        var resource = new TrackingDisposable();
        var acquisition = new StableEffectPendingSource<TrackingDisposable>();
        var use = new StableEffectPendingSource<int>();
        var acquireCalls = 0;
        var useCalls = 0;
        var useRunCalls = 0;
        var effect = Effect.FromValueTask<object, TrackingDisposable>((current, token) =>
        {
            acquireCalls++;
            Assert.Same(environment, current);
            Assert.Equal(TestContext.Current.CancellationToken, token);
            return acquisition.Operation;
        }).Using(selected =>
        {
            useCalls++;
            Assert.Same(resource, selected);
            Assert.Equal(0, resource.DisposeCount);
            return Effect.FromValueTask<object, int>((current, token) =>
            {
                useRunCalls++;
                Assert.Same(environment, current);
                Assert.Equal(TestContext.Current.CancellationToken, token);
                return use.Operation;
            });
        });

        Assert.Equal(0, acquireCalls);
        var operation = effect.RunAsync(environment, TestContext.Current.CancellationToken).AsTask();
        Assert.False(operation.IsCompleted);
        Assert.Equal(0, useCalls);
        Assert.Equal(0, useRunCalls);
        Assert.Equal(0, resource.DisposeCount);
        await acquisition.ReleaseAsync(resource);
        await use.WaitForSubscriptionAsync();
        Assert.False(operation.IsCompleted);
        Assert.Equal(1, useCalls);
        Assert.Equal(1, useRunCalls);
        Assert.Equal(0, resource.DisposeCount);
        AssertConsumedOnce(acquisition);
        await use.ReleaseAsync(31);
        Assert.Equal(31, await operation.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
        Assert.Equal(1, acquireCalls);
        Assert.Equal(1, useCalls);
        Assert.Equal(1, useRunCalls);
        Assert.Equal(1, resource.DisposeCount);
        AssertConsumedOnce(acquisition);
        AssertConsumedOnce(use);
        output.WriteLine("C:89: acquisition/use GetResult=1/1, OnCompleted=1/1; acquire/use/use-run calls=1/1/1; Dispose=1 after use; output=31; resource=same; environment=same; caller-token=exact");
    }

    [Fact]
    public async Task EnvironmentUsingAsyncConsumesPendingAcquisitionUseAndDisposalOnceAndCannotFinishBeforeDisposal()
    {
        var environment = new object();
        var resource = new TrackingAsyncDisposable();
        var acquisition = new StableEffectPendingSource<TrackingAsyncDisposable>();
        var use = new StableEffectPendingSource<int>();
        var acquireCalls = 0;
        var useCalls = 0;
        var useRunCalls = 0;
        var effect = Effect.FromValueTask<object, TrackingAsyncDisposable>((current, token) =>
        {
            acquireCalls++;
            Assert.Same(environment, current);
            Assert.Equal(TestContext.Current.CancellationToken, token);
            return acquisition.Operation;
        }).UsingAsync(selected =>
        {
            useCalls++;
            Assert.Same(resource, selected);
            Assert.Equal(0, resource.DisposeAsyncCount);
            return Effect.FromValueTask<object, int>((current, token) =>
            {
                useRunCalls++;
                Assert.Same(environment, current);
                Assert.Equal(TestContext.Current.CancellationToken, token);
                return use.Operation;
            });
        });

        Assert.Equal(0, acquireCalls);
        var operation = effect.RunAsync(environment, TestContext.Current.CancellationToken).AsTask();
        Assert.False(operation.IsCompleted);
        Assert.Equal(0, useCalls);
        Assert.Equal(0, useRunCalls);
        Assert.Equal(0, resource.DisposeAsyncCount);
        await acquisition.ReleaseAsync(resource);
        await use.WaitForSubscriptionAsync();
        Assert.False(operation.IsCompleted);
        Assert.Equal(1, useCalls);
        Assert.Equal(1, useRunCalls);
        Assert.Equal(0, resource.DisposeAsyncCount);
        AssertConsumedOnce(acquisition);
        await use.ReleaseAsync(37);
        await resource.Disposal.WaitForSubscriptionAsync();
        Assert.False(operation.IsCompleted);
        Assert.Equal(1, resource.DisposeAsyncCount);
        AssertConsumedOnce(use);
        Assert.Equal(0, resource.Disposal.GetResultCount);
        await resource.Disposal.ReleaseAsync(true);
        Assert.Equal(37, await operation.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
        Assert.Equal(1, acquireCalls);
        Assert.Equal(1, useCalls);
        Assert.Equal(1, useRunCalls);
        Assert.Equal(1, resource.DisposeAsyncCount);
        AssertConsumedOnce(acquisition);
        AssertConsumedOnce(use);
        AssertConsumedOnce(resource.Disposal);
        output.WriteLine("C:90: acquisition/use/disposal GetResult=1/1/1, OnCompleted=1/1/1; acquire/use/use-run/DisposeAsync calls=1/1/1/1; output=37 only after disposal; resource=same; environment=same; caller-token=exact");
    }

    [Fact]
    public async Task TokenAwareTapValueAsyncConsumesItsPendingObserverOnceAndReturnsTheSamePayloadWithoutPreemptingACanceledToken()
    {
        var payload = new object();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var observer = new StableEffectPendingSource<bool>();
        var calls = 0;
        var operation = payload.TapValueAsync((value, token) =>
        {
            calls++;
            Assert.Same(payload, value);
            Assert.Equal(cancellation.Token, token);
            return observer.VoidOperation;
        }, cancellation.Token).AsTask();

        Assert.False(operation.IsCompleted);
        Assert.Equal(1, calls);
        await observer.ReleaseAsync(true);
        Assert.Same(payload, await operation.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
        Assert.Equal(1, calls);
        AssertConsumedOnce(observer);
        output.WriteLine("C:127: observer GetResult=1, OnCompleted=1; observer-calls=1; output-payload=same; caller-token=exact,canceled; successful observation is not preempted");
    }

    private static void AssertConsumedOnce<T>(StableEffectPendingSource<T> source)
    {
        Assert.Equal(1, source.GetResultCount);
        Assert.Equal(1, source.OnCompletedCount);
    }

    private sealed class TrackingDisposable : IDisposable
    {
        public int DisposeCount { get; private set; }

        public void Dispose() => DisposeCount++;
    }

    private sealed class TrackingAsyncDisposable : IAsyncDisposable
    {
        public StableEffectPendingSource<bool> Disposal { get; } = new();

        public int DisposeAsyncCount { get; private set; }

        public ValueTask DisposeAsync()
        {
            DisposeAsyncCount++;
            return Disposal.VoidOperation;
        }
    }
}

internal sealed class StableEffectPendingSource<T> : IValueTaskSource<T>, IValueTaskSource
{
    private ManualResetValueTaskSourceCore<T> source = new() { RunContinuationsAsynchronously = true };
    private readonly TaskCompletionSource<bool> subscribed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int getResultCount;
    private int onCompletedCount;

    public int GetResultCount => Volatile.Read(ref getResultCount);

    public int OnCompletedCount => Volatile.Read(ref onCompletedCount);

    public ValueTask<T> Operation => new(this, source.Version);

    public ValueTask VoidOperation => new(this, source.Version);

    public Task WaitForSubscriptionAsync() =>
        subscribed.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

    public async Task ReleaseAsync(T value)
    {
        await WaitForSubscriptionAsync();
        Assert.Equal(1, OnCompletedCount);
        Assert.Equal(0, GetResultCount);
        Assert.Equal(ValueTaskSourceStatus.Pending, source.GetStatus(source.Version));
        source.SetResult(value);
    }

    public T GetResult(short token)
    {
        if (Interlocked.Increment(ref getResultCount) != 1)
        {
            throw new InvalidOperationException("A single-use pending operation was consumed more than once.");
        }

        return source.GetResult(token);
    }

    void IValueTaskSource.GetResult(short token) => GetResult(token);

    public ValueTaskSourceStatus GetStatus(short token) => source.GetStatus(token);

    public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
    {
        Interlocked.Increment(ref onCompletedCount);
        source.OnCompleted(continuation, state, token, flags);
        subscribed.TrySetResult(true);
    }
}
