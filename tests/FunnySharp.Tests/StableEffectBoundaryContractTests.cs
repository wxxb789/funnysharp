using FunnySharp;

namespace FunnySharp.Tests;

public sealed class StableEffectBoundaryContractTests(ITestOutputHelper output)
{
    [Fact]
    public async Task EnvironmentTokenAwareSyncFactoryDefersExecutionAndForwardsTheExactEnvironmentAndCanceledCallerToken()
    {
        var environment = new object();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var calls = 0;
        var effect = Effect.FromSync<object, int>((current, token) =>
        {
            calls++;
            Assert.Same(environment, current);
            Assert.Equal(cancellation.Token, token);
            return 17;
        });

        Assert.Equal(0, calls);
        var operation = effect.RunAsync(environment, cancellation.Token);
        Assert.True(operation.IsCompletedSuccessfully);
        Assert.Equal(17, await operation);
        Assert.Equal(1, calls);
        output.WriteLine("C:82/C:106: completed-success; output=17; environment=same; caller-token=exact,canceled; calls=1");
    }

    [Fact]
    public async Task EnvironmentTokenlessTaskFactoryAndRunPreserveTheFaultedOceTaskRatherThanNaturallyCancelingIt()
    {
        var environment = new object();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var expected = new OperationCanceledException("environment task fault", cancellation.Token);
        var input = Task.FromException<int>(expected);
        var calls = 0;
        var effect = Effect.FromTask<object, int>(current =>
        {
            calls++;
            Assert.Same(environment, current);
            return input;
        });

        Assert.Equal(0, calls);
        var operation = effect.RunAsync(environment, TestContext.Current.CancellationToken);
        Assert.True(operation.IsFaulted);
        Assert.False(operation.IsCanceled);
        var returned = operation.AsTask();
        Assert.Same(input, returned);
        var observed = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => returned);
        output.WriteLine($"C:85/C:106: task-status={returned.Status}; task-identity={ReferenceEquals(input, returned)}; exception-identity={ReferenceEquals(expected, observed)}; exception-token={observed.CancellationToken == cancellation.Token}; calls={calls}");
        Assert.Same(expected, observed);
        Assert.Same(expected, returned.Exception!.InnerException);
        Assert.Equal(cancellation.Token, observed.CancellationToken);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task EnvironmentTokenlessValueTaskFactoryDefersExecutionAndReturnsTheExactEnvironmentDependentValue()
    {
        var environment = new object();
        var calls = 0;
        var effect = Effect.FromValueTask<object, int>(current =>
        {
            calls++;
            Assert.Same(environment, current);
            return ValueTask.FromResult(23);
        });

        Assert.Equal(0, calls);
        var operation = effect.RunAsync(environment, TestContext.Current.CancellationToken);
        Assert.True(operation.IsCompletedSuccessfully);
        Assert.Equal(23, await operation);
        Assert.Equal(1, calls);
        output.WriteLine("C:87/C:106: completed-success; output=23; environment=same; calls=1");
    }

    [Fact]
    public async Task EnvironmentIndependentRunPreservesTheFaultedOceTaskAndItsExceptionAndToken()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var expected = new OperationCanceledException("direct run fault", cancellation.Token);
        var input = Task.FromException<int>(expected);
        var calls = 0;
        var effect = Effect.FromValueTask<int>(token =>
        {
            calls++;
            Assert.Equal(TestContext.Current.CancellationToken, token);
            return new ValueTask<int>(input);
        });

        Assert.Equal(0, calls);
        var operation = effect.RunAsync(TestContext.Current.CancellationToken);
        Assert.True(operation.IsFaulted);
        Assert.False(operation.IsCanceled);
        var returned = operation.AsTask();
        Assert.Same(input, returned);
        var observed = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => returned);
        output.WriteLine($"C:99: task-status={returned.Status}; task-identity={ReferenceEquals(input, returned)}; exception-identity={ReferenceEquals(expected, observed)}; exception-token={observed.CancellationToken == cancellation.Token}; calls={calls}");
        Assert.Same(expected, observed);
        Assert.Same(expected, returned.Exception!.InnerException);
        Assert.Equal(cancellation.Token, observed.CancellationToken);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task EnvironmentIndependentBindDirectlyReturnsTheFaultedOceInnerTaskForCompletedSourceButNaturallyCancelsAfterPendingSource()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var expected = new OperationCanceledException("bound run fault", cancellation.Token);
        var innerTask = Task.FromException<int>(expected);
        var innerCalls = 0;
        var binderCalls = 0;
        var inner = Effect.FromValueTask<int>(token =>
        {
            innerCalls++;
            Assert.Equal(TestContext.Current.CancellationToken, token);
            return new ValueTask<int>(innerTask);
        });
        Effect<int> Bind(int value)
        {
            binderCalls++;
            Assert.Equal(7, value);
            return inner;
        }

        var completed = Effect.FromValue(7).Bind(Bind).RunAsync(TestContext.Current.CancellationToken);
        Assert.True(completed.IsFaulted);
        Assert.False(completed.IsCanceled);
        var direct = completed.AsTask();
        Assert.Same(innerTask, direct);
        var directException = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => direct);
        Assert.Same(expected, directException);
        Assert.Equal(cancellation.Token, directException.CancellationToken);

        var source = new StableEffectPendingSource<int>();
        var sourceCalls = 0;
        var pendingEffect = Effect.FromValueTask<int>(token =>
        {
            sourceCalls++;
            Assert.Equal(TestContext.Current.CancellationToken, token);
            return source.Operation;
        }).Bind(Bind);
        var pending = pendingEffect.RunAsync(TestContext.Current.CancellationToken).AsTask();
        Assert.False(pending.IsCompleted);
        Assert.Equal(1, binderCalls);
        Assert.Equal(1, innerCalls);
        await source.ReleaseAsync(7);
        var pendingException = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => pending.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
        output.WriteLine($"C:95: completed-source status={direct.Status}, inner-task-identity={ReferenceEquals(innerTask, direct)}, exception-identity={ReferenceEquals(expected, directException)}; pending-source status={pending.Status}, inner-task-identity={ReferenceEquals(innerTask, pending)}, exception-identity={ReferenceEquals(expected, pendingException)}, exception-token={pendingException.CancellationToken == cancellation.Token}; source-GetResult={source.GetResultCount}; binder-calls={binderCalls}; inner-calls={innerCalls}");
        Assert.True(pending.IsCanceled);
        Assert.False(pending.IsFaulted);
        Assert.NotSame(innerTask, pending);
        Assert.Same(expected, pendingException);
        Assert.Equal(cancellation.Token, pendingException.CancellationToken);
        Assert.Equal(1, sourceCalls);
        Assert.Equal(1, source.GetResultCount);
        Assert.Equal(1, source.OnCompletedCount);
        Assert.Equal(2, binderCalls);
        Assert.Equal(2, innerCalls);
        Assert.True(innerTask.IsFaulted);
    }

    [Fact]
    public async Task EnvironmentBindDirectlyReturnsTheFaultedOceInnerTaskForCompletedSourceButNaturallyCancelsAfterPendingSource()
    {
        var environment = new object();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var expected = new OperationCanceledException("environment bound run fault", cancellation.Token);
        var innerTask = Task.FromException<int>(expected);
        var innerCalls = 0;
        var binderCalls = 0;
        var inner = Effect.FromValueTask<object, int>((current, token) =>
        {
            innerCalls++;
            Assert.Same(environment, current);
            Assert.Equal(TestContext.Current.CancellationToken, token);
            return new ValueTask<int>(innerTask);
        });
        Effect<object, int> Bind(int value)
        {
            binderCalls++;
            Assert.Equal(7, value);
            return inner;
        }

        var completed = Effect.FromSync<object, int>(current =>
        {
            Assert.Same(environment, current);
            return 7;
        }).Bind(Bind).RunAsync(environment, TestContext.Current.CancellationToken);
        Assert.True(completed.IsFaulted);
        Assert.False(completed.IsCanceled);
        var direct = completed.AsTask();
        Assert.Same(innerTask, direct);
        var directException = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => direct);
        Assert.Same(expected, directException);
        Assert.Equal(cancellation.Token, directException.CancellationToken);

        var source = new StableEffectPendingSource<int>();
        var sourceCalls = 0;
        var pendingEffect = Effect.FromValueTask<object, int>((current, token) =>
        {
            sourceCalls++;
            Assert.Same(environment, current);
            Assert.Equal(TestContext.Current.CancellationToken, token);
            return source.Operation;
        }).Bind(Bind);
        var pending = pendingEffect.RunAsync(environment, TestContext.Current.CancellationToken).AsTask();
        Assert.False(pending.IsCompleted);
        Assert.Equal(1, binderCalls);
        Assert.Equal(1, innerCalls);
        await source.ReleaseAsync(7);
        var pendingException = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => pending.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
        output.WriteLine($"C:102: completed-source status={direct.Status}, inner-task-identity={ReferenceEquals(innerTask, direct)}, exception-identity={ReferenceEquals(expected, directException)}; pending-source status={pending.Status}, inner-task-identity={ReferenceEquals(innerTask, pending)}, exception-identity={ReferenceEquals(expected, pendingException)}, exception-token={pendingException.CancellationToken == cancellation.Token}; source-GetResult={source.GetResultCount}; binder-calls={binderCalls}; inner-calls={innerCalls}; environment=same; caller-token=exact");
        Assert.True(pending.IsCanceled);
        Assert.False(pending.IsFaulted);
        Assert.NotSame(innerTask, pending);
        Assert.Same(expected, pendingException);
        Assert.Equal(cancellation.Token, pendingException.CancellationToken);
        Assert.Equal(1, sourceCalls);
        Assert.Equal(1, source.GetResultCount);
        Assert.Equal(1, source.OnCompletedCount);
        Assert.Equal(2, binderCalls);
        Assert.Equal(2, innerCalls);
        Assert.True(innerTask.IsFaulted);
    }
}
