using System.Threading.Tasks.Sources;

namespace FunnySharp.Tests;

public sealed class StableFunctionRuntimeContractTests
{
    [Theory]
    [InlineData(false, false, 0)]
    [InlineData(false, false, 1)]
    [InlineData(false, false, 2)]
    [InlineData(false, false, 3)]
    [InlineData(false, true, 0)]
    [InlineData(false, true, 1)]
    [InlineData(false, true, 2)]
    [InlineData(false, true, 3)]
    [InlineData(true, false, 0)]
    [InlineData(true, false, 1)]
    [InlineData(true, false, 2)]
    [InlineData(true, false, 3)]
    [InlineData(true, true, 0)]
    [InlineData(true, true, 1)]
    [InlineData(true, true, 2)]
    [InlineData(true, true, 3)]
    public async Task TaskCompositionCapturesEachStageFaultAndPreservesNaturalCancellation(
        bool tokenAware, bool secondStage, int outcome)
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var token = cancellation.Token;
        Exception expected = outcome == 1
            ? new InvalidOperationException("stage")
            : new OperationCanceledException("stage", token);
        var firstCalls = 0;
        var secondCalls = 0;
        Task<string?> Fault() => outcome switch
        {
            0 => null!,
            1 or 2 => throw expected,
            3 => Task.FromException<string?>(expected),
            _ => throw new ArgumentOutOfRangeException(nameof(outcome)),
        };
        Task<string?> First(string? value)
        {
            Assert.Null(value);
            firstCalls++;
            return secondStage ? Task.FromResult<string?>(null) : Fault();
        }
        Task<string?> Second(string? value)
        {
            Assert.Null(value);
            secondCalls++;
            return Fault();
        }
        Func<string?, Task<string?>> plain = First;
        Func<string?, CancellationToken, Task<string?>> aware = (value, actual) =>
        {
            Assert.Equal(token, actual);
            return First(value);
        };
        var returned = tokenAware
            ? aware.ComposeAsync((value, actual) => { Assert.Equal(token, actual); return Second(value); })(null, token)
            : plain.ComposeAsync(Second)(null);
        var actual = await Assert.ThrowsAnyAsync<Exception>(() => returned);
        if (outcome == 0) Assert.IsType<NullReferenceException>(actual);
        else Assert.Same(expected, actual);
        Assert.Equal(outcome >= 2, returned.IsCanceled);
        Assert.Equal(outcome < 2, returned.IsFaulted);
        if (actual is OperationCanceledException canceled) Assert.Equal(token, canceled.CancellationToken);
        Assert.Equal(1, firstCalls);
        Assert.Equal(secondStage ? 1 : 0, secondCalls);
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(false, 1)]
    [InlineData(false, 2)]
    [InlineData(true, 0)]
    [InlineData(true, 1)]
    [InlineData(true, 2)]
    public async Task ValueCompositionConsumesEachStageOnceAfterItsSubscribedCompletion(bool tokenAware, int pendingStage)
    {
        var first = new Source();
        var second = new Source();
        if (pendingStage != 1) first.Complete();
        if (pendingStage != 2) second.Complete();
        var firstCalls = 0;
        var secondCalls = 0;
        var token = TestContext.Current.CancellationToken;
        Func<string?, ValueTask<string?>> firstFunction = value =>
        {
            Assert.Null(value);
            firstCalls++;
            return first.Operation();
        };
        Func<string?, ValueTask<string?>> secondFunction = value =>
        {
            Assert.Null(value);
            secondCalls++;
            return second.Operation();
        };
        Func<string?, CancellationToken, ValueTask<string?>> aware = (value, actual) =>
        {
            Assert.Equal(token, actual);
            return firstFunction(value);
        };
        var returned = (tokenAware
            ? aware.ComposeValueAsync((value, actual) => { Assert.Equal(token, actual); return secondFunction(value); })(null, token)
            : firstFunction.ComposeValueAsync(secondFunction)(null)).AsTask();
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _ = returned.ContinueWith(_ => completed.TrySetResult(), CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        if (pendingStage != 0)
        {
            var pending = pendingStage == 1 ? first : second;
            await pending.Subscribed.Task.WaitAsync(TimeSpan.FromSeconds(10), token);
            Assert.False(returned.IsCompleted);
            Assert.Equal(0, pending.Reads);
            if (pendingStage == 1) Assert.Equal(0, secondCalls);
            pending.Complete();
        }
        await completed.Task.WaitAsync(TimeSpan.FromSeconds(10), token);
        Assert.Null(await returned);
        Assert.Equal(1, firstCalls);
        Assert.Equal(1, secondCalls);
        Assert.Equal(1, first.Reads);
        Assert.Equal(1, second.Reads);
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(false, 1)]
    [InlineData(false, 2)]
    [InlineData(false, 3)]
    [InlineData(true, 0)]
    [InlineData(true, 1)]
    [InlineData(true, 2)]
    [InlineData(true, 3)]
    public async Task TaskObservationPreservesNullableInputAndExactFaultOrCancellation(bool tokenAware, int outcome)
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var token = cancellation.Token;
        Exception expected = outcome == 1
            ? new InvalidOperationException("observer")
            : new OperationCanceledException("observer", token);
        var calls = 0;
        Task Observe(string? value)
        {
            Assert.Null(value);
            calls++;
            return outcome switch { 0 => null!, 1 or 2 => throw expected, 3 => Task.FromException(expected), _ => throw new ArgumentOutOfRangeException(nameof(outcome)) };
        }
        string? input = null;
        var returned = tokenAware
            ? input.TapAsync((value, actual) => { Assert.Equal(token, actual); return Observe(value); }, token)
            : input.TapAsync(Observe);
        var actual = await Assert.ThrowsAnyAsync<Exception>(() => returned);
        if (outcome == 0) Assert.IsType<NullReferenceException>(actual);
        else Assert.Same(expected, actual);
        Assert.Equal(outcome >= 2, returned.IsCanceled);
        Assert.Equal(outcome < 2, returned.IsFaulted);
        if (actual is OperationCanceledException canceled) Assert.Equal(token, canceled.CancellationToken);
        Assert.Equal(1, calls);
    }

    private sealed class Source : IValueTaskSource<string?>
    {
        private ManualResetValueTaskSourceCore<string?> core = new() { RunContinuationsAsynchronously = true };
        public TaskCompletionSource Subscribed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int Reads { get; private set; }
        public ValueTask<string?> Operation() => new(this, core.Version);
        public void Complete() => core.SetResult(null);
        public string? GetResult(short token) { Reads++; Assert.Equal(1, Reads); return core.GetResult(token); }
        public ValueTaskSourceStatus GetStatus(short token) => core.GetStatus(token);
        public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
        {
            core.OnCompleted(continuation, state, token, flags);
            Subscribed.TrySetResult();
        }
    }
}
