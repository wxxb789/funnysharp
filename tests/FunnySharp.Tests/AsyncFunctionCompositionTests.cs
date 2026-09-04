using System.Threading.Tasks.Sources;

namespace FunnySharp.Tests;

public sealed class AsyncFunctionCompositionTests
{
    [Fact]
    public async Task TaskCompositionIsAssociativeForValueOrderFaultAndCancellation()
    {
        var leftTrace = new List<string>();
        var rightTrace = new List<string>();

        static Func<int, Task<int>> Step(string name, int increment, ICollection<string> trace) => value =>
        {
            trace.Add(name);
            return Task.FromResult(value + increment);
        };

        var left = Step("first", 1, leftTrace)
            .ComposeAsync(Step("second", 2, leftTrace))
            .ComposeAsync(Step("third", 3, leftTrace));
        var right = Step("first", 1, rightTrace)
            .ComposeAsync(Step("second", 2, rightTrace).ComposeAsync(Step("third", 3, rightTrace)));

        Assert.Equal(await left(10), await right(10));
        Assert.Equal(["first", "second", "third"], leftTrace);
        Assert.Equal(leftTrace, rightTrace);

        var expectedFault = new InvalidOperationException("middle");
        Func<int, Task<int>> first = value => Task.FromResult(value + 1);
        Func<int, Task<int>> fault = _ => Task.FromException<int>(expectedFault);
        Func<int, Task<int>> skipped = value => Task.FromResult(value + 1);

        Assert.Same(
            expectedFault,
            await Assert.ThrowsAsync<InvalidOperationException>(() => first.ComposeAsync(fault).ComposeAsync(skipped)(0)));
        Assert.Same(
            expectedFault,
            await Assert.ThrowsAsync<InvalidOperationException>(() => first.ComposeAsync(fault.ComposeAsync(skipped))(0)));

        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        Func<int, Task<int>> canceled = _ => Task.FromCanceled<int>(cancellationSource.Token);

        var leftCancellation = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            canceled.ComposeAsync(skipped).ComposeAsync(skipped)(0));
        var rightCancellation = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            canceled.ComposeAsync(skipped.ComposeAsync(skipped))(0));
        Assert.Equal(cancellationSource.Token, leftCancellation.CancellationToken);
        Assert.Equal(cancellationSource.Token, rightCancellation.CancellationToken);
    }

    [Fact]
    public async Task TaskComposeEvaluatesInOrderWithoutBlocking()
    {
        var calls = new List<string>();
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Func<int, Task<int>> first = async value =>
        {
            calls.Add("first");
            await gate.Task;
            return value + 1;
        };
        Func<int, Task<int>> second = value =>
        {
            calls.Add("second");
            return Task.FromResult(value * 2);
        };

        var resultTask = first.ComposeAsync(second)(3);

        Assert.False(resultTask.IsCompleted);
        Assert.Equal(["first"], calls);
        gate.SetResult();

        Assert.Equal(8, await resultTask);
        Assert.Equal(["first", "second"], calls);
    }

    [Fact]
    public async Task TaskComposePropagatesTheOriginalFaultAndShortCircuits()
    {
        var expected = new InvalidOperationException("first failed");
        var secondCalled = false;
        Func<int, Task<int>> first = _ => Task.FromException<int>(expected);
        Func<int, Task<int>> second = value =>
        {
            secondCalled = true;
            return Task.FromResult(value);
        };

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() => first.ComposeAsync(second)(1));

        Assert.Same(expected, actual);
        Assert.False(secondCalled);
    }

    [Fact]
    public async Task TaskComposePreservesSecondStageFaultIdentity()
    {
        var expected = new InvalidOperationException("second failed");
        Func<int, Task<int>> first = value => Task.FromResult(value + 1);
        Func<int, Task<int>> second = _ => Task.FromException<int>(expected);

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() => first.ComposeAsync(second)(1));

        Assert.Same(expected, actual);
    }

    [Fact]
    public async Task TaskComposePropagatesCancellationAndShortCircuits()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var secondCalled = false;
        Func<int, Task<int>> first = _ => Task.FromCanceled<int>(cancellationSource.Token);
        Func<int, Task<int>> second = value =>
        {
            secondCalled = true;
            return Task.FromResult(value);
        };

        var actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first.ComposeAsync(second)(1));

        Assert.Equal(cancellationSource.Token, actual.CancellationToken);
        Assert.False(secondCalled);
    }

    [Fact]
    public async Task ValueTaskComposeSupportsTheSameFlowAndConsumesItsSourceOnce()
    {
        var source = new CountingValueTaskSource(5);
        var secondCalls = 0;
        Func<int, ValueTask<int>> first = _ => source.CreateValueTask();
        Func<int, ValueTask<int>> second = value =>
        {
            secondCalls++;
            return ValueTask.FromResult(value * 2);
        };

        var result = await first.ComposeAsync(second)(0);

        Assert.Equal(10, result);
        Assert.Equal(1, source.GetResultCount);
        Assert.Equal(1, secondCalls);
    }

    [Fact]
    public async Task ValueTaskComposeWaitsForPendingSourceBeforeRunningSecondStage()
    {
        var source = new CountingValueTaskSource();
        var calls = new List<string>();
        Func<int, ValueTask<int>> first = _ =>
        {
            calls.Add("first");
            return source.CreateValueTask();
        };
        Func<int, ValueTask<int>> second = value =>
        {
            calls.Add("second");
            return ValueTask.FromResult(value * 2);
        };

        var resultTask = first.ComposeAsync(second)(0);

        Assert.False(resultTask.IsCompleted);
        Assert.Equal(["first"], calls);
        source.SetResult(5);

        Assert.Equal(10, await resultTask);
        Assert.Equal(["first", "second"], calls);
        Assert.Equal(1, source.GetResultCount);
    }

    [Fact]
    public async Task ValueTaskComposeConsumesSecondStageOnce()
    {
        var source = new CountingValueTaskSource(10);
        Func<int, ValueTask<int>> first = value => ValueTask.FromResult(value + 1);
        Func<int, ValueTask<int>> second = _ => source.CreateValueTask();

        var result = await first.ComposeAsync(second)(1);

        Assert.Equal(10, result);
        Assert.Equal(1, source.GetResultCount);
    }

    [Fact]
    public async Task ValueTaskComposePreservesFaultIdentityAndShortCircuits()
    {
        var expected = new InvalidOperationException("first failed");
        var secondCalled = false;
        Func<int, ValueTask<int>> first = _ => ValueTask.FromException<int>(expected);
        Func<int, ValueTask<int>> second = value =>
        {
            secondCalled = true;
            return ValueTask.FromResult(value);
        };

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(async () => await first.ComposeAsync(second)(1));

        Assert.Same(expected, actual);
        Assert.False(secondCalled);
    }

    [Fact]
    public async Task ValueTaskComposePreservesSecondStageFaultIdentity()
    {
        var expected = new InvalidOperationException("second failed");
        Func<int, ValueTask<int>> first = value => ValueTask.FromResult(value + 1);
        Func<int, ValueTask<int>> second = _ => ValueTask.FromException<int>(expected);

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await first.ComposeAsync(second)(1));

        Assert.Same(expected, actual);
    }

    [Fact]
    public async Task AsyncComposeCapturesSynchronousDelegateThrowsInReturnedAwaitables()
    {
        var taskException = new InvalidOperationException("task failed");
        var valueTaskException = new InvalidOperationException("value task failed");
        var taskSecondCalled = false;
        var valueTaskSecondCalled = false;
        Func<int, Task<int>> taskFirst = _ => throw taskException;
        Func<int, Task<int>> taskSecond = value =>
        {
            taskSecondCalled = true;
            return Task.FromResult(value);
        };
        Func<int, ValueTask<int>> valueTaskFirst = _ => throw valueTaskException;
        Func<int, ValueTask<int>> valueTaskSecond = value =>
        {
            valueTaskSecondCalled = true;
            return ValueTask.FromResult(value);
        };
        Task<int>? taskResult = null;
        ValueTask<int> valueTaskResult = default;

        var taskInvocationException = Record.Exception(
            () => { taskResult = taskFirst.ComposeAsync(taskSecond)(1); });
        var valueTaskInvocationException = Record.Exception(
            () => { valueTaskResult = valueTaskFirst.ComposeAsync(valueTaskSecond)(1); });

        Assert.Null(taskInvocationException);
        Assert.Null(valueTaskInvocationException);
        Assert.Same(taskException, await Assert.ThrowsAsync<InvalidOperationException>(() => taskResult!));
        Assert.Same(
            valueTaskException,
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await valueTaskResult));
        Assert.False(taskSecondCalled);
        Assert.False(valueTaskSecondCalled);
    }

    [Fact]
    public async Task CancellationAwareTaskComposePassesTheExactTokenToBothStages()
    {
        using var cancellationSource = new CancellationTokenSource();
        var observedTokens = new List<CancellationToken>();
        Func<int, CancellationToken, Task<int>> first = (value, token) =>
        {
            observedTokens.Add(token);
            return Task.FromResult(value + 1);
        };
        Func<int, CancellationToken, Task<int>> second = (value, token) =>
        {
            observedTokens.Add(token);
            return Task.FromResult(value * 2);
        };

        var result = await first.ComposeAsync(second)(3, cancellationSource.Token);

        Assert.Equal(8, result);
        Assert.Equal([cancellationSource.Token, cancellationSource.Token], observedTokens);
    }

    [Fact]
    public async Task CancellationAwareValueTaskComposePassesTheExactTokenAndPropagatesCancellation()
    {
        using var cancellationSource = new CancellationTokenSource();
        var observedTokens = new List<CancellationToken>();
        Func<int, CancellationToken, ValueTask<int>> first = (value, token) =>
        {
            observedTokens.Add(token);
            return ValueTask.FromResult(value + 1);
        };
        Func<int, CancellationToken, ValueTask<int>> second = (value, token) =>
        {
            observedTokens.Add(token);
            return ValueTask.FromCanceled<int>(token);
        };
        cancellationSource.Cancel();

        var actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await first.ComposeAsync(second)(3, cancellationSource.Token));

        Assert.Equal(cancellationSource.Token, actual.CancellationToken);
        Assert.Equal([cancellationSource.Token, cancellationSource.Token], observedTokens);
    }

    [Fact]
    public void AsyncComposeRejectsNullDelegatesEagerly()
    {
        Func<int, Task<int>> task = value => Task.FromResult(value);
        Func<int, ValueTask<int>> valueTask = value => ValueTask.FromResult(value);
        Func<int, CancellationToken, Task<int>> cancellableTask = (value, _) => Task.FromResult(value);
        Func<int, CancellationToken, ValueTask<int>> cancellableValueTask = (value, _) => ValueTask.FromResult(value);

        Assert.Throws<ArgumentNullException>(() => ((Func<int, Task<int>>)null!).ComposeAsync(task));
        Assert.Throws<ArgumentNullException>(() => task.ComposeAsync<int, int, int>(null!));
        Assert.Throws<ArgumentNullException>(() => ((Func<int, ValueTask<int>>)null!).ComposeAsync(valueTask));
        Assert.Throws<ArgumentNullException>(() => valueTask.ComposeAsync<int, int, int>(null!));
        Assert.Throws<ArgumentNullException>(() => ((Func<int, CancellationToken, Task<int>>)null!).ComposeAsync(cancellableTask));
        Assert.Throws<ArgumentNullException>(() => cancellableTask.ComposeAsync<int, int, int>(null!));
        Assert.Throws<ArgumentNullException>(() => ((Func<int, CancellationToken, ValueTask<int>>)null!).ComposeAsync(cancellableValueTask));
        Assert.Throws<ArgumentNullException>(() => cancellableValueTask.ComposeAsync<int, int, int>(null!));
    }

    [Fact]
    public async Task TapAsyncAwaitsObserverAndReturnsTheOriginalValue()
    {
        var value = new object();
        object? observed = null;
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var resultTask = value.TapAsync(async item =>
        {
            observed = item;
            await gate.Task;
        });

        Assert.False(resultTask.IsCompleted);
        Assert.Same(value, observed);
        gate.SetResult();

        Assert.Same(value, await resultTask);
    }

    [Fact]
    public async Task TapValueAsyncAwaitsObserverAndReturnsTheOriginalValue()
    {
        var value = new object();
        object? observed = null;

        var result = await value.TapValueAsync(item =>
        {
            observed = item;
            return ValueTask.CompletedTask;
        });

        Assert.Same(value, result);
        Assert.Same(value, observed);
    }

    [Fact]
    public async Task TapValueAsyncWaitsForPendingObserverAndConsumesItOnce()
    {
        var source = new CountingVoidValueTaskSource();
        var observerCalled = false;

        var resultTask = 7.TapValueAsync(value =>
        {
            observerCalled = value == 7;
            return source.CreateValueTask();
        });

        Assert.True(observerCalled);
        Assert.False(resultTask.IsCompleted);
        source.SetResult();

        Assert.Equal(7, await resultTask);
        Assert.Equal(1, source.GetResultCount);
    }

    [Fact]
    public async Task CancellationAwareTapsPropagateCancellationAndDoNotCancelEagerly()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var taskObserved = false;
        var valueTaskObserved = false;

        Assert.Equal(
            1,
            await 1.TapAsync((value, token) =>
            {
                taskObserved = value == 1 && token == cancellationSource.Token;
                return Task.CompletedTask;
            }, cancellationSource.Token));
        Assert.Equal(
            2,
            await 2.TapValueAsync((value, token) =>
            {
                valueTaskObserved = value == 2 && token == cancellationSource.Token;
                return ValueTask.CompletedTask;
            }, cancellationSource.Token));

        Assert.True(taskObserved);
        Assert.True(valueTaskObserved);

        var taskCancellation = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => 1.TapAsync((_, token) => Task.FromCanceled(token), cancellationSource.Token));
        var valueTaskCancellation = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await 1.TapValueAsync(
                (_, token) => ValueTask.FromCanceled(token),
                cancellationSource.Token));

        Assert.Equal(cancellationSource.Token, taskCancellation.CancellationToken);
        Assert.Equal(cancellationSource.Token, valueTaskCancellation.CancellationToken);
    }

    [Fact]
    public async Task AsyncTapsCaptureSynchronousObserverThrowsInReturnedAwaitables()
    {
        var taskException = new InvalidOperationException("task observer failed");
        var valueTaskException = new InvalidOperationException("value task observer failed");
        Task<int>? taskResult = null;
        ValueTask<int> valueTaskResult = default;

        var taskInvocationException = Record.Exception(
            () => { taskResult = 1.TapAsync(_ => throw taskException); });
        var valueTaskInvocationException = Record.Exception(
            () => { valueTaskResult = 1.TapValueAsync(_ => throw valueTaskException); });

        Assert.Null(taskInvocationException);
        Assert.Null(valueTaskInvocationException);
        Assert.Same(taskException, await Assert.ThrowsAsync<InvalidOperationException>(() => taskResult!));
        Assert.Same(
            valueTaskException,
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await valueTaskResult));
    }

    [Fact]
    public async Task CancellationAwareTapsPassTheirTokenAndPropagateObserverFaults()
    {
        using var cancellationSource = new CancellationTokenSource();
        var expected = new InvalidOperationException("observer failed");
        CancellationToken taskToken = default;
        CancellationToken valueTaskToken = default;

        var taskException = await Assert.ThrowsAsync<InvalidOperationException>(() => 1.TapAsync((_, token) =>
        {
            taskToken = token;
            return Task.FromException(expected);
        }, cancellationSource.Token));
        var valueTaskException = await Assert.ThrowsAsync<InvalidOperationException>(async () => await 1.TapValueAsync((_, token) =>
        {
            valueTaskToken = token;
            return ValueTask.FromException(expected);
        }, cancellationSource.Token));

        Assert.Same(expected, taskException);
        Assert.Same(expected, valueTaskException);
        Assert.Equal(cancellationSource.Token, taskToken);
        Assert.Equal(cancellationSource.Token, valueTaskToken);
    }

    [Fact]
    public void AsyncTapsRejectNullObserversEagerly()
    {
        Action tapAsync = () => { _ = 1.TapAsync<int>(null!); };
        Action cancellableTapAsync = () => { _ = 1.TapAsync<int>(null!, CancellationToken.None); };
        Action tapValueAsync = () => { _ = 1.TapValueAsync<int>(null!); };
        Action cancellableTapValueAsync = () => { _ = 1.TapValueAsync<int>(null!, CancellationToken.None); };

        Assert.Throws<ArgumentNullException>(tapAsync);
        Assert.Throws<ArgumentNullException>(cancellableTapAsync);
        Assert.Throws<ArgumentNullException>(tapValueAsync);
        Assert.Throws<ArgumentNullException>(cancellableTapValueAsync);
    }

    private sealed class CountingValueTaskSource : IValueTaskSource<int>
    {
        private ManualResetValueTaskSourceCore<int> source;

        public CountingValueTaskSource()
        {
            source.RunContinuationsAsynchronously = true;
        }

        public CountingValueTaskSource(int result) : this()
        {
            SetResult(result);
        }

        public int GetResultCount { get; private set; }

        public ValueTask<int> CreateValueTask() => new(this, source.Version);

        public void SetResult(int result) => source.SetResult(result);

        public int GetResult(short token)
        {
            GetResultCount++;
            return source.GetResult(token);
        }

        public ValueTaskSourceStatus GetStatus(short token) => source.GetStatus(token);

        public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) =>
            source.OnCompleted(continuation, state, token, flags);
    }

    private sealed class CountingVoidValueTaskSource : IValueTaskSource
    {
        private ManualResetValueTaskSourceCore<bool> source;

        public CountingVoidValueTaskSource()
        {
            source.RunContinuationsAsynchronously = true;
        }

        public int GetResultCount { get; private set; }

        public ValueTask CreateValueTask() => new(this, source.Version);

        public void SetResult() => source.SetResult(true);

        public void GetResult(short token)
        {
            GetResultCount++;
            source.GetResult(token);
        }

        public ValueTaskSourceStatus GetStatus(short token) => source.GetStatus(token);

        public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) =>
            source.OnCompleted(continuation, state, token, flags);
    }
}
