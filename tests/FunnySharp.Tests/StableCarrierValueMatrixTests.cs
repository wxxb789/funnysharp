using System.Threading.Tasks.Sources;

namespace FunnySharp.Tests;

public sealed class StableCarrierValueMatrixTests
{
    [Theory]
    [InlineData(187, false, false)]
    [InlineData(187, false, true)]
    [InlineData(187, true, false)]
    [InlineData(187, true, true)]
    [InlineData(188, false, false)]
    [InlineData(188, false, true)]
    [InlineData(188, true, false)]
    [InlineData(188, true, true)]
    [InlineData(189, false, false)]
    [InlineData(189, false, true)]
    [InlineData(189, true, false)]
    [InlineData(189, true, true)]
    [InlineData(190, false, false)]
    [InlineData(190, false, true)]
    [InlineData(190, true, false)]
    [InlineData(190, true, true)]
    [InlineData(191, false, false)]
    [InlineData(191, false, true)]
    [InlineData(191, true, false)]
    [InlineData(191, true, true)]
    [InlineData(192, false, false)]
    [InlineData(192, false, true)]
    [InlineData(192, true, false)]
    [InlineData(192, true, true)]
    [InlineData(298, false, false)]
    [InlineData(298, false, true)]
    [InlineData(298, true, false)]
    [InlineData(298, true, true)]
    [InlineData(299, false, false)]
    [InlineData(299, false, true)]
    [InlineData(299, true, false)]
    [InlineData(299, true, true)]
    [InlineData(300, false, false)]
    [InlineData(300, false, true)]
    [InlineData(300, true, false)]
    [InlineData(300, true, true)]
    [InlineData(301, false, false)]
    [InlineData(301, false, true)]
    [InlineData(301, true, false)]
    [InlineData(301, true, true)]
    [InlineData(451, false, false)]
    [InlineData(451, false, true)]
    [InlineData(451, true, false)]
    [InlineData(451, true, true)]
    [InlineData(452, false, false)]
    [InlineData(452, false, true)]
    [InlineData(452, true, false)]
    [InlineData(452, true, true)]
    [InlineData(462, false, false)]
    [InlineData(462, false, true)]
    [InlineData(462, true, false)]
    [InlineData(462, true, true)]
    [InlineData(463, false, false)]
    [InlineData(463, false, true)]
    [InlineData(463, true, false)]
    [InlineData(463, true, true)]
    [InlineData(464, false, false)]
    [InlineData(464, false, true)]
    [InlineData(464, true, false)]
    [InlineData(464, true, true)]
    [InlineData(465, false, false)]
    [InlineData(465, false, true)]
    [InlineData(465, true, false)]
    [InlineData(465, true, true)]
    [InlineData(466, false, false)]
    [InlineData(466, false, true)]
    [InlineData(466, true, false)]
    [InlineData(466, true, true)]
    [InlineData(505, false, false)]
    [InlineData(505, false, true)]
    [InlineData(505, true, false)]
    [InlineData(505, true, true)]
    [InlineData(506, false, false)]
    [InlineData(506, false, true)]
    [InlineData(506, true, false)]
    [InlineData(506, true, true)]
    public async Task SourceBackedValueTasksAreConsumedOnceAfterControlledCompletion(int identity, bool pending, bool faulted)
    {
        using var cancellation = new CancellationTokenSource();
        var token = cancellation.Token;
        switch (identity)
        {
            case 187:
                await ProbeSource<int?, Option<int>>((operation, token) => operation().ToOptionAsync().AsTask(), 42, Option.Some(42), pending, faulted, token);
                break;
            case 188:
                await ProbeSource<string?, Option<string>>((operation, token) => operation().ToOptionAsync().AsTask(), "value", Option.Some("value"), pending, faulted, token);
                break;
            case 189:
                await ProbeSource<Option<int>, Option<int>>((operation, token) => Option.Some(1).BindValueAsync((_, ct) => CheckToken(ct, token, operation()), token).AsTask(), Option.Some(42), Option.Some(42), pending, faulted, token);
                break;
            case 190:
                await ProbeSource<Option<int>, Option<int>>((operation, token) => Option.Some(1).BindValueAsync(_ => operation()).AsTask(), Option.Some(42), Option.Some(42), pending, faulted, token);
                break;
            case 191:
                await ProbeSource<int, Option<int>>((operation, token) => Option.Some(1).MapValueAsync((_, ct) => CheckToken(ct, token, operation()), token).AsTask(), 42, Option.Some(42), pending, faulted, token);
                break;
            case 192:
                await ProbeSource<int, Option<int>>((operation, token) => Option.Some(1).MapValueAsync(_ => operation()).AsTask(), 42, Option.Some(42), pending, faulted, token);
                break;
            case 298:
                await ProbeSource<Result<int, string>, Result<int, string>>((operation, token) => Result<int, string>.Success(1).BindValueAsync((_, ct) => CheckToken(ct, token, operation()), token).AsTask(), Result<int, string>.Success(42), Result<int, string>.Success(42), pending, faulted, token);
                break;
            case 299:
                await ProbeSource<Result<int, string>, Result<int, string>>((operation, token) => Result<int, string>.Success(1).BindValueAsync(_ => operation()).AsTask(), Result<int, string>.Success(42), Result<int, string>.Success(42), pending, faulted, token);
                break;
            case 300:
                await ProbeSource<int, Result<int, string>>((operation, token) => Result<int, string>.Success(1).MapValueAsync((_, ct) => CheckToken(ct, token, operation()), token).AsTask(), 42, Result<int, string>.Success(42), pending, faulted, token);
                break;
            case 301:
                await ProbeSource<int, Result<int, string>>((operation, token) => Result<int, string>.Success(1).MapValueAsync(_ => operation()).AsTask(), 42, Result<int, string>.Success(42), pending, faulted, token);
                break;
            case 451:
                await ProbeVoidSource(false, pending, faulted, token);
                break;
            case 452:
                await ProbeVoidSource(true, pending, faulted, token);
                break;
            case 462:
                await ProbeSource<int, Result<int, string>>((operation, token) => UnitResult<string>.Success().ToResultValueAsync(ct => CheckToken(ct, token, operation()), token).AsTask(), 42, Result<int, string>.Success(42), pending, faulted, token);
                break;
            case 463:
                await ProbeSource<int, Result<int, string>>((operation, token) => UnitResult<string>.Success().ToResultValueAsync(() => operation()).AsTask(), 42, Result<int, string>.Success(42), pending, faulted, token);
                break;
            case 464:
                await ProbeSource<UnitResult<string>, UnitResult<string>>((operation, token) => UnitResult<string>.Success().BindValueAsync(ct => CheckToken(ct, token, operation()), token).AsTask(), UnitResult<string>.Success(), UnitResult<string>.Success(), pending, faulted, token);
                break;
            case 465:
                await ProbeSource<UnitResult<string>, UnitResult<string>>((operation, token) => UnitResult<string>.Success().BindValueAsync(() => operation()).AsTask(), UnitResult<string>.Success(), UnitResult<string>.Success(), pending, faulted, token);
                break;
            case 466:
                await ProbeSource<Result<int, string>, UnitResult<string>>((operation, token) => operation().ToUnitResultAsync().AsTask(), Result<int, string>.Success(42), UnitResult<string>.Success(), pending, faulted, token);
                break;
            case 505:
                await ProbeSource<int, Validation<int, string>>((operation, token) => Validation<int, string>.Valid(1).MapValueAsync((_, ct) => CheckToken(ct, token, operation()), token).AsTask(), 42, Validation<int, string>.Valid(42), pending, faulted, token);
                break;
            case 506:
                await ProbeSource<int, Validation<int, string>>((operation, token) => Validation<int, string>.Valid(1).MapValueAsync(_ => operation()).AsTask(), 42, Validation<int, string>.Valid(42), pending, faulted, token);
                break;
            default: throw new ArgumentOutOfRangeException(nameof(identity));
        }
    }

    private static T CheckToken<T>(CancellationToken actual, CancellationToken expected, T operation) =>
        StableCarrierAsyncMatrixTests.CheckToken(actual, expected, operation);

    private static async Task ProbeSource<TInput, TOutput>(
        Func<Func<ValueTask<TInput>>, CancellationToken, Task<TOutput>> invoke,
        TInput payload, TOutput expectedResult, bool pending, bool faulted, CancellationToken token)
    {
        var source = new SingleSource<TInput>();
        var expected = new InvalidOperationException("source fault");
        var calls = 0;
        void Complete()
        {
            if (faulted) source.SetException(expected);
            else source.SetResult(payload);
        }
        if (!pending) Complete();
        var returned = invoke(() => { calls++; return source.CreateValueTask(); }, token);
        Assert.Equal(1, calls);
        var completion = Subscribe(returned);
        if (pending)
        {
            Assert.False(returned.IsCompleted);
            Assert.Equal(0, source.GetResultCount);
            await source.Subscribed.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            Assert.Equal(1, source.OnCompletedCount);
            Complete();
        }
        await completion.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        if (faulted)
        {
            Assert.Same(expected, await Assert.ThrowsAsync<InvalidOperationException>(() => returned));
            Assert.True(returned.IsFaulted);
        }
        else Assert.Equal(expectedResult, await returned);
        Assert.Equal(1, source.GetResultCount);
        Assert.Equal(pending ? 1 : 0, source.OnCompletedCount);
        Assert.Equal(1, calls);
    }

    private static async Task ProbeVoidSource(bool mapped, bool pending, bool faulted, CancellationToken token)
    {
        var source = new VoidSource();
        var expected = new InvalidOperationException("void source fault");
        var mapperCalls = 0;
        void Complete()
        {
            if (faulted) source.Core.SetException(expected);
            else source.Core.SetResult(true);
        }
        if (!pending) Complete();
        Task returned;
        Task<UnitResult<Exception>>? plain = null;
        Task<UnitResult<string>>? typed = null;
        if (mapped)
        {
            typed = UnitResult.TryValueAsync(() => source.CreateValueTask(), exception =>
            {
                Assert.Same(expected, exception);
                mapperCalls++;
                return "mapped";
            }).AsTask();
            returned = typed;
        }
        else
        {
            plain = UnitResult.TryValueAsync(() => source.CreateValueTask()).AsTask();
            returned = plain;
        }
        var completion = Subscribe(returned);
        if (pending)
        {
            Assert.False(returned.IsCompleted);
            Assert.Equal(0, source.Core.GetResultCount);
            await source.Core.Subscribed.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            Complete();
        }
        await completion.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        if (typed is not null)
        {
            var result = await typed;
            Assert.Equal(faulted, result.IsFailure);
            if (faulted)
            {
                Assert.True(result.TryGetError(out var error));
                Assert.Equal("mapped", error);
            }
        }
        else
        {
            var result = await plain!;
            Assert.Equal(faulted, result.IsFailure);
            if (faulted)
            {
                Assert.True(result.TryGetError(out var error));
                Assert.Same(expected, error);
            }
        }
        Assert.Equal(mapped && faulted ? 1 : 0, mapperCalls);
        Assert.Equal(1, source.Core.GetResultCount);
        Assert.Equal(pending ? 1 : 0, source.Core.OnCompletedCount);
    }

    private static Task Subscribe(Task returned)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _ = returned.ContinueWith(_ => completion.TrySetResult(), CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        return completion.Task;
    }

    private sealed class SingleSource<T> : IValueTaskSource<T>
    {
        private ManualResetValueTaskSourceCore<T> source = new() { RunContinuationsAsynchronously = true };
        public TaskCompletionSource Subscribed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int GetResultCount { get; private set; }
        public int OnCompletedCount { get; private set; }
        public ValueTask<T> CreateValueTask() => new(this, source.Version);
        public void SetResult(T value) => source.SetResult(value);
        public void SetException(Exception exception) => source.SetException(exception);
        public T GetResult(short token)
        {
            GetResultCount++;
            Assert.Equal(1, GetResultCount);
            return source.GetResult(token);
        }
        public ValueTaskSourceStatus GetStatus(short token) => source.GetStatus(token);
        public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
        {
            OnCompletedCount++;
            source.OnCompleted(continuation, state, token, flags);
            Subscribed.TrySetResult();
        }
        public short Version => source.Version;
    }

    private sealed class VoidSource : IValueTaskSource
    {
        public SingleSource<bool> Core { get; } = new();
        public ValueTask CreateValueTask() => new(this, Core.Version);
        public void GetResult(short token) => Core.GetResult(token);
        public ValueTaskSourceStatus GetStatus(short token) => Core.GetStatus(token);
        public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) =>
            Core.OnCompleted(continuation, state, token, flags);
    }
}

