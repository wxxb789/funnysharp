namespace FunnySharp.Tests;

public sealed class StableCarrierAsyncMatrixTests
{
    [Theory]
    [InlineData(181, 0, false)]
    [InlineData(181, 0, true)]
    [InlineData(181, 1, false)]
    [InlineData(181, 1, true)]
    [InlineData(182, 0, false)]
    [InlineData(182, 0, true)]
    [InlineData(182, 1, false)]
    [InlineData(182, 1, true)]
    [InlineData(183, 0, false)]
    [InlineData(183, 0, true)]
    [InlineData(183, 1, false)]
    [InlineData(183, 1, true)]
    [InlineData(183, 2, false)]
    [InlineData(184, 0, false)]
    [InlineData(184, 0, true)]
    [InlineData(184, 1, false)]
    [InlineData(184, 1, true)]
    [InlineData(184, 2, false)]
    [InlineData(185, 0, false)]
    [InlineData(185, 0, true)]
    [InlineData(185, 1, false)]
    [InlineData(185, 1, true)]
    [InlineData(185, 2, false)]
    [InlineData(186, 0, false)]
    [InlineData(186, 0, true)]
    [InlineData(186, 1, false)]
    [InlineData(186, 1, true)]
    [InlineData(186, 2, false)]
    [InlineData(187, 0, false)]
    [InlineData(187, 0, true)]
    [InlineData(187, 1, false)]
    [InlineData(187, 1, true)]
    [InlineData(188, 0, false)]
    [InlineData(188, 0, true)]
    [InlineData(188, 1, false)]
    [InlineData(188, 1, true)]
    [InlineData(189, 0, false)]
    [InlineData(189, 0, true)]
    [InlineData(189, 1, false)]
    [InlineData(189, 1, true)]
    [InlineData(189, 2, false)]
    [InlineData(190, 0, false)]
    [InlineData(190, 0, true)]
    [InlineData(190, 1, false)]
    [InlineData(190, 1, true)]
    [InlineData(190, 2, false)]
    [InlineData(191, 0, false)]
    [InlineData(191, 0, true)]
    [InlineData(191, 1, false)]
    [InlineData(191, 1, true)]
    [InlineData(191, 2, false)]
    [InlineData(192, 0, false)]
    [InlineData(192, 0, true)]
    [InlineData(192, 1, false)]
    [InlineData(192, 1, true)]
    [InlineData(192, 2, false)]
    [InlineData(449, 0, false)]
    [InlineData(449, 0, true)]
    [InlineData(449, 1, false)]
    [InlineData(449, 1, true)]
    [InlineData(449, 2, false)]
    [InlineData(450, 0, false)]
    [InlineData(450, 0, true)]
    [InlineData(450, 1, false)]
    [InlineData(450, 1, true)]
    [InlineData(450, 2, false)]
    [InlineData(451, 0, false)]
    [InlineData(451, 0, true)]
    [InlineData(451, 1, false)]
    [InlineData(451, 1, true)]
    [InlineData(451, 2, false)]
    [InlineData(452, 0, false)]
    [InlineData(452, 0, true)]
    [InlineData(452, 1, false)]
    [InlineData(452, 1, true)]
    [InlineData(452, 2, false)]
    [InlineData(457, 0, false)]
    [InlineData(457, 0, true)]
    [InlineData(457, 1, false)]
    [InlineData(457, 1, true)]
    [InlineData(457, 2, false)]
    [InlineData(458, 0, false)]
    [InlineData(458, 0, true)]
    [InlineData(458, 1, false)]
    [InlineData(458, 1, true)]
    [InlineData(458, 2, false)]
    [InlineData(459, 0, false)]
    [InlineData(459, 0, true)]
    [InlineData(459, 1, false)]
    [InlineData(459, 1, true)]
    [InlineData(459, 2, false)]
    [InlineData(460, 0, false)]
    [InlineData(460, 0, true)]
    [InlineData(460, 1, false)]
    [InlineData(460, 1, true)]
    [InlineData(460, 2, false)]
    [InlineData(461, 0, false)]
    [InlineData(461, 0, true)]
    [InlineData(461, 1, false)]
    [InlineData(461, 1, true)]
    [InlineData(462, 0, false)]
    [InlineData(462, 0, true)]
    [InlineData(462, 1, false)]
    [InlineData(462, 1, true)]
    [InlineData(462, 2, false)]
    [InlineData(463, 0, false)]
    [InlineData(463, 0, true)]
    [InlineData(463, 1, false)]
    [InlineData(463, 1, true)]
    [InlineData(463, 2, false)]
    [InlineData(464, 0, false)]
    [InlineData(464, 0, true)]
    [InlineData(464, 1, false)]
    [InlineData(464, 1, true)]
    [InlineData(464, 2, false)]
    [InlineData(465, 0, false)]
    [InlineData(465, 0, true)]
    [InlineData(465, 1, false)]
    [InlineData(465, 1, true)]
    [InlineData(465, 2, false)]
    [InlineData(466, 0, false)]
    [InlineData(466, 0, true)]
    [InlineData(466, 1, false)]
    [InlineData(466, 1, true)]
    [InlineData(503, 0, false)]
    [InlineData(503, 0, true)]
    [InlineData(503, 1, false)]
    [InlineData(503, 1, true)]
    [InlineData(503, 2, false)]
    [InlineData(504, 0, false)]
    [InlineData(504, 0, true)]
    [InlineData(504, 1, false)]
    [InlineData(504, 1, true)]
    [InlineData(504, 2, false)]
    [InlineData(505, 0, false)]
    [InlineData(505, 0, true)]
    [InlineData(505, 1, false)]
    [InlineData(505, 1, true)]
    [InlineData(505, 2, false)]
    [InlineData(506, 0, false)]
    [InlineData(506, 0, true)]
    [InlineData(506, 1, false)]
    [InlineData(506, 1, true)]
    [InlineData(506, 2, false)]
    public async Task CancellationRoutesPreserveExceptionIdentityTokenAndExactStatus(int identity, int outcome, bool pending)
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var token = cancellation.Token;
        var mapperCalls = 0;
        switch (identity)
        {
            case 181:
                await ProbeCancellation<int?, Option<int>>((operation, token) => operation().ToOptionAsync(), outcome, pending, true, true, token);
                break;
            case 182:
                await ProbeCancellation<string?, Option<string>>((operation, token) => operation().ToOptionAsync(), outcome, pending, true, true, token);
                break;
            case 183:
                await ProbeCancellation<Option<int>, Option<int>>((operation, token) => Option.Some(1).BindAsync((_, ct) => CheckToken(ct, token, operation()), token), outcome, pending, true, false, token);
                break;
            case 184:
                await ProbeCancellation<Option<int>, Option<int>>((operation, token) => Option.Some(1).BindAsync(_ => operation()), outcome, pending, true, false, token);
                break;
            case 185:
                await ProbeCancellation<int, Option<int>>((operation, token) => Option.Some(1).MapAsync((_, ct) => CheckToken(ct, token, operation()), token), outcome, pending, true, false, token);
                break;
            case 186:
                await ProbeCancellation<int, Option<int>>((operation, token) => Option.Some(1).MapAsync(_ => operation()), outcome, pending, true, false, token);
                break;
            case 187:
                await ProbeCancellation<int?, Option<int>>((operation, token) => new ValueTask<int?>(operation()).ToOptionAsync().AsTask(), outcome, pending, true, true, token);
                break;
            case 188:
                await ProbeCancellation<string?, Option<string>>((operation, token) => new ValueTask<string?>(operation()).ToOptionAsync().AsTask(), outcome, pending, true, true, token);
                break;
            case 189:
                await ProbeCancellation<Option<int>, Option<int>>((operation, token) => Option.Some(1).BindValueAsync((_, ct) => CheckToken(ct, token, new ValueTask<Option<int>>(operation())), token).AsTask(), outcome, pending, true, false, token);
                break;
            case 190:
                await ProbeCancellation<Option<int>, Option<int>>((operation, token) => Option.Some(1).BindValueAsync(_ => new ValueTask<Option<int>>(operation())).AsTask(), outcome, pending, true, false, token);
                break;
            case 191:
                await ProbeCancellation<int, Option<int>>((operation, token) => Option.Some(1).MapValueAsync((_, ct) => CheckToken(ct, token, new ValueTask<int>(operation())), token).AsTask(), outcome, pending, true, false, token);
                break;
            case 192:
                await ProbeCancellation<int, Option<int>>((operation, token) => Option.Some(1).MapValueAsync(_ => new ValueTask<int>(operation())).AsTask(), outcome, pending, true, false, token);
                break;
            case 449:
                await ProbeCancellation<int, UnitResult<Exception>>((operation, token) => UnitResult.TryAsync(() => operation()), outcome, pending, false, false, token);
                break;
            case 450:
                await ProbeCancellation<int, UnitResult<string>>((operation, token) => UnitResult.TryAsync(() => operation(), _ => { mapperCalls++; return "mapped"; }), outcome, pending, false, false, token);
                break;
            case 451:
                await ProbeCancellation<int, UnitResult<Exception>>((operation, token) => UnitResult.TryValueAsync(() => new ValueTask(operation())).AsTask(), outcome, pending, false, false, token);
                break;
            case 452:
                await ProbeCancellation<int, UnitResult<string>>((operation, token) => UnitResult.TryValueAsync(() => new ValueTask(operation()), _ => { mapperCalls++; return "mapped"; }).AsTask(), outcome, pending, false, false, token);
                break;
            case 457:
                await ProbeCancellation<int, Result<int, string>>((operation, token) => UnitResult<string>.Success().ToResultAsync(ct => CheckToken(ct, token, operation()), token), outcome, pending, false, false, token);
                break;
            case 458:
                await ProbeCancellation<int, Result<int, string>>((operation, token) => UnitResult<string>.Success().ToResultAsync(() => operation()), outcome, pending, false, false, token);
                break;
            case 459:
                await ProbeCancellation<UnitResult<string>, UnitResult<string>>((operation, token) => UnitResult<string>.Success().BindAsync(ct => CheckToken(ct, token, operation()), token), outcome, pending, false, false, token);
                break;
            case 460:
                await ProbeCancellation<UnitResult<string>, UnitResult<string>>((operation, token) => UnitResult<string>.Success().BindAsync(() => operation()), outcome, pending, false, false, token);
                break;
            case 461:
                await ProbeCancellation<Result<int, string>, UnitResult<string>>((operation, token) => operation().ToUnitResultAsync(), outcome, pending, true, true, token);
                break;
            case 462:
                await ProbeCancellation<int, Result<int, string>>((operation, token) => UnitResult<string>.Success().ToResultValueAsync(ct => CheckToken(ct, token, new ValueTask<int>(operation())), token).AsTask(), outcome, pending, false, false, token);
                break;
            case 463:
                await ProbeCancellation<int, Result<int, string>>((operation, token) => UnitResult<string>.Success().ToResultValueAsync(() => new ValueTask<int>(operation())).AsTask(), outcome, pending, false, false, token);
                break;
            case 464:
                await ProbeCancellation<UnitResult<string>, UnitResult<string>>((operation, token) => UnitResult<string>.Success().BindValueAsync(ct => CheckToken(ct, token, new ValueTask<UnitResult<string>>(operation())), token).AsTask(), outcome, pending, false, false, token);
                break;
            case 465:
                await ProbeCancellation<UnitResult<string>, UnitResult<string>>((operation, token) => UnitResult<string>.Success().BindValueAsync(() => new ValueTask<UnitResult<string>>(operation())).AsTask(), outcome, pending, false, false, token);
                break;
            case 466:
                await ProbeCancellation<Result<int, string>, UnitResult<string>>((operation, token) => new ValueTask<Result<int, string>>(operation()).ToUnitResultAsync().AsTask(), outcome, pending, true, true, token);
                break;
            case 503:
                await ProbeCancellation<int, Validation<int, string>>((operation, token) => Validation<int, string>.Valid(1).MapAsync((_, ct) => CheckToken(ct, token, operation()), token), outcome, pending, false, false, token);
                break;
            case 504:
                await ProbeCancellation<int, Validation<int, string>>((operation, token) => Validation<int, string>.Valid(1).MapAsync(_ => operation()), outcome, pending, false, false, token);
                break;
            case 505:
                await ProbeCancellation<int, Validation<int, string>>((operation, token) => Validation<int, string>.Valid(1).MapValueAsync((_, ct) => CheckToken(ct, token, new ValueTask<int>(operation())), token).AsTask(), outcome, pending, false, false, token);
                break;
            case 506:
                await ProbeCancellation<int, Validation<int, string>>((operation, token) => Validation<int, string>.Valid(1).MapValueAsync(_ => new ValueTask<int>(operation())).AsTask(), outcome, pending, false, false, token);
                break;
            default: throw new ArgumentOutOfRangeException(nameof(identity));
        }
        Assert.Equal(0, mapperCalls);
    }

    internal static T CheckToken<T>(CancellationToken actual, CancellationToken expected, T operation)
    {
        Assert.Equal(expected, actual);
        return operation;
    }

    private static async Task ProbeCancellation<TInput, TOutput>(
        Func<Func<Task<TInput>>, CancellationToken, Task<TOutput>> invoke,
        int outcome, bool pending, bool natural, bool conversion, CancellationToken token)
    {
        var expected = new OperationCanceledException("matrix cancellation", token);
        var source = new TaskCompletionSource<TInput>(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancellationSignal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<TInput> input = source.Task;
        async Task<TInput> CanceledInput()
        {
            await cancellationSignal.Task.ConfigureAwait(false);
            throw expected;
        }
        var calls = 0;
        Task<TInput> Operation()
        {
            calls++;
            if (outcome == 2) throw expected;
            return input;
        }
        void Complete()
        {
            if (outcome == 0) source.SetException(expected);
            else cancellationSignal.SetResult();
        }
        if (!pending && outcome != 2) Complete();
        if (outcome == 1) input = CanceledInput();
        var returned = invoke(Operation, token);
        Assert.Equal(1, calls);
        var signal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _ = returned.ContinueWith(_ => signal.TrySetResult(), CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        if (pending)
        {
            Assert.False(returned.IsCompleted);
            Assert.False(signal.Task.IsCompleted);
            Complete();
        }
        await signal.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        var actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => returned);
        if (outcome == 1)
        {
            Assert.True(input.IsCanceled);
            var original = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => input);
            Assert.Same(expected, original);
            Assert.Same(original, actual);
        }
        else Assert.Same(expected, actual);
        Assert.Equal(token, actual.CancellationToken);
        Assert.Equal(outcome != 0 || natural, returned.IsCanceled);
        Assert.Equal(outcome == 0 && !natural, returned.IsFaulted);
        Assert.Equal(1, calls);
    }

    [Theory]
    [InlineData(183)]
    [InlineData(184)]
    [InlineData(185)]
    [InlineData(294)]
    [InlineData(295)]
    [InlineData(296)]
    [InlineData(297)]
    [InlineData(457)]
    [InlineData(458)]
    [InlineData(459)]
    [InlineData(460)]
    [InlineData(503)]
    [InlineData(504)]
    public async Task NullCallbackTasksFaultWithoutAnInventedBoundaryGuard(int identity)
    {
        Task returned = identity switch
        {
            183 => Option.Some(1).BindAsync((_, ct) => CheckToken(ct, CancellationToken.None, (Task<Option<int>>)null!), CancellationToken.None),
            184 => Option.Some(1).BindAsync(_ => (Task<Option<int>>)null!),
            185 => Option.Some(1).MapAsync((_, ct) => CheckToken(ct, CancellationToken.None, (Task<int>)null!), CancellationToken.None),
            294 => Result<int, string>.Success(1).BindAsync((_, ct) => CheckToken(ct, CancellationToken.None, (Task<Result<int, string>>)null!), CancellationToken.None),
            295 => Result<int, string>.Success(1).BindAsync(_ => (Task<Result<int, string>>)null!),
            296 => Result<int, string>.Success(1).MapAsync((_, ct) => CheckToken(ct, CancellationToken.None, (Task<int>)null!), CancellationToken.None),
            297 => Result<int, string>.Success(1).MapAsync(_ => (Task<int>)null!),
            457 => UnitResult<string>.Success().ToResultAsync(ct => CheckToken(ct, CancellationToken.None, (Task<int>)null!), CancellationToken.None),
            458 => UnitResult<string>.Success().ToResultAsync(() => (Task<int>)null!),
            459 => UnitResult<string>.Success().BindAsync(ct => CheckToken(ct, CancellationToken.None, (Task<UnitResult<string>>)null!), CancellationToken.None),
            460 => UnitResult<string>.Success().BindAsync(() => (Task<UnitResult<string>>)null!),
            503 => Validation<int, string>.Valid(1).MapAsync((_, ct) => CheckToken(ct, CancellationToken.None, (Task<int>)null!), CancellationToken.None),
            504 => Validation<int, string>.Valid(1).MapAsync(_ => (Task<int>)null!),
            _ => throw new ArgumentOutOfRangeException(nameof(identity)),
        };
        await Assert.ThrowsAsync<NullReferenceException>(() => returned);
        Assert.True(returned.IsFaulted);
        Assert.False(returned.IsCanceled);
    }

    [Fact]
    public async Task Identity296NULLPAYLOADPreservesTheExactAsyncRuntimeBehavior()
    {
        var result = await Result<int, string>.Success(1).MapAsync((_, ct) => CheckToken(ct, CancellationToken.None, Task.FromResult<string?>(null)), CancellationToken.None);
        Assert.True(result.TryGetValue(out var value));
        Assert.Null(value);
    }

    [Fact]
    public async Task Identity297NULLPAYLOADPreservesTheExactAsyncRuntimeBehavior()
    {
        var result = await Result<int, string>.Success(1).MapAsync(_ => Task.FromResult<string?>(null));
        Assert.True(result.TryGetValue(out var value));
        Assert.Null(value);
    }

    [Fact]
    public async Task Identity300NULLPAYLOADPreservesTheExactAsyncRuntimeBehavior()
    {
        var result = await Result<int, string>.Success(1).MapValueAsync((_, ct) => CheckToken(ct, CancellationToken.None, new ValueTask<string?>(Task.FromResult<string?>(null))), CancellationToken.None).AsTask();
        Assert.True(result.TryGetValue(out var value));
        Assert.Null(value);
    }

    [Fact]
    public async Task Identity301NULLPAYLOADPreservesTheExactAsyncRuntimeBehavior()
    {
        var result = await Result<int, string>.Success(1).MapValueAsync(_ => new ValueTask<string?>(Task.FromResult<string?>(null))).AsTask();
        Assert.True(result.TryGetValue(out var value));
        Assert.Null(value);
    }

    [Fact]
    public async Task Identity457NULLPAYLOADPreservesTheExactAsyncRuntimeBehavior()
    {
        var result = await UnitResult<string>.Success().ToResultAsync(ct => CheckToken(ct, CancellationToken.None, Task.FromResult<string?>(null)), CancellationToken.None);
        Assert.True(result.TryGetValue(out var value));
        Assert.Null(value);
    }

    [Fact]
    public async Task Identity458NULLPAYLOADPreservesTheExactAsyncRuntimeBehavior()
    {
        var result = await UnitResult<string>.Success().ToResultAsync(() => Task.FromResult<string?>(null));
        Assert.True(result.TryGetValue(out var value));
        Assert.Null(value);
    }

    [Fact]
    public async Task Identity462NULLPAYLOADPreservesTheExactAsyncRuntimeBehavior()
    {
        var result = await UnitResult<string>.Success().ToResultValueAsync(ct => CheckToken(ct, CancellationToken.None, new ValueTask<string?>(Task.FromResult<string?>(null))), CancellationToken.None).AsTask();
        Assert.True(result.TryGetValue(out var value));
        Assert.Null(value);
    }

    [Fact]
    public async Task Identity463NULLPAYLOADPreservesTheExactAsyncRuntimeBehavior()
    {
        var result = await UnitResult<string>.Success().ToResultValueAsync(() => new ValueTask<string?>(Task.FromResult<string?>(null))).AsTask();
        Assert.True(result.TryGetValue(out var value));
        Assert.Null(value);
    }

    [Fact]
    public async Task Identity503NULLPAYLOADPreservesTheExactAsyncRuntimeBehavior()
    {
        var result = await Validation<int, string>.Valid(1).MapAsync((_, ct) => CheckToken(ct, CancellationToken.None, Task.FromResult<string?>(null)), CancellationToken.None);
        Assert.True(result.TryGetValue(out var value));
        Assert.Null(value);
    }

    [Fact]
    public async Task Identity504NULLPAYLOADPreservesTheExactAsyncRuntimeBehavior()
    {
        var result = await Validation<int, string>.Valid(1).MapAsync(_ => Task.FromResult<string?>(null));
        Assert.True(result.TryGetValue(out var value));
        Assert.Null(value);
    }

    [Fact]
    public async Task Identity505NULLPAYLOADPreservesTheExactAsyncRuntimeBehavior()
    {
        var result = await Validation<int, string>.Valid(1).MapValueAsync((_, ct) => CheckToken(ct, CancellationToken.None, new ValueTask<string?>(Task.FromResult<string?>(null))), CancellationToken.None).AsTask();
        Assert.True(result.TryGetValue(out var value));
        Assert.Null(value);
    }

    [Fact]
    public async Task Identity506NULLPAYLOADPreservesTheExactAsyncRuntimeBehavior()
    {
        var result = await Validation<int, string>.Valid(1).MapValueAsync(_ => new ValueTask<string?>(Task.FromResult<string?>(null))).AsTask();
        Assert.True(result.TryGetValue(out var value));
        Assert.Null(value);
    }

    [Fact]
    public async Task Identity450NULLPAYLOADPreservesTheExactAsyncRuntimeBehavior()
    {
        var expected = new InvalidOperationException("operation");
        var calls = 0;
        var result = await UnitResult.TryAsync<string?>(() => Task.FromException(expected), exception => { Assert.Same(expected, exception); calls++; return null; });
        Assert.True(result.TryGetError(out var error));
        Assert.Null(error);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task Identity452NULLPAYLOADPreservesTheExactAsyncRuntimeBehavior()
    {
        var expected = new InvalidOperationException("operation");
        var calls = 0;
        var result = await UnitResult.TryValueAsync<string?>(() => ValueTask.FromException(expected), exception => { Assert.Same(expected, exception); calls++; return null; });
        Assert.True(result.TryGetError(out var error));
        Assert.Null(error);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task Identity461NULLPAYLOADPreservesTheExactAsyncRuntimeBehavior()
    {
        var result = await Task.FromResult(Result<string?, string?>.Failure(null)).ToUnitResultAsync();
        Assert.True(result.TryGetError(out var error));
        Assert.Null(error);
    }

    [Fact]
    public async Task Identity466NULLPAYLOADPreservesTheExactAsyncRuntimeBehavior()
    {
        var result = await ValueTask.FromResult(Result<string?, string?>.Failure(null)).ToUnitResultAsync();
        Assert.True(result.TryGetError(out var error));
        Assert.Null(error);
    }

    [Fact]
    public async Task Identity294BINDERDEFAULTPreservesTheExactAsyncRuntimeBehavior()
    {
#pragma warning disable FS1001 // The returned default binder carrier is accepted, not inspected by composition.
        var result = await Result<int, string>.Success(1).BindAsync((_, ct) => CheckToken(ct, CancellationToken.None, Task.FromResult(default(Result<int, string>))), CancellationToken.None);
#pragma warning restore FS1001
        Assert.Equal("Uninitialized", result.ToString());
        Assert.Throws<InvalidOperationException>(() => result.IsSuccess);
    }

    [Fact]
    public async Task Identity295BINDERDEFAULTPreservesTheExactAsyncRuntimeBehavior()
    {
#pragma warning disable FS1001 // The returned default binder carrier is accepted, not inspected by composition.
        var result = await Result<int, string>.Success(1).BindAsync(_ => Task.FromResult(default(Result<int, string>)));
#pragma warning restore FS1001
        Assert.Equal("Uninitialized", result.ToString());
        Assert.Throws<InvalidOperationException>(() => result.IsSuccess);
    }

    [Fact]
    public async Task Identity298BINDERDEFAULTPreservesTheExactAsyncRuntimeBehavior()
    {
#pragma warning disable FS1001 // The returned default binder carrier is accepted, not inspected by composition.
        var result = await Result<int, string>.Success(1).BindValueAsync((_, ct) => CheckToken(ct, CancellationToken.None, new ValueTask<Result<int, string>>(Task.FromResult(default(Result<int, string>)))), CancellationToken.None).AsTask();
#pragma warning restore FS1001
        Assert.Equal("Uninitialized", result.ToString());
        Assert.Throws<InvalidOperationException>(() => result.IsSuccess);
    }

    [Fact]
    public async Task Identity299BINDERDEFAULTPreservesTheExactAsyncRuntimeBehavior()
    {
#pragma warning disable FS1001 // The returned default binder carrier is accepted, not inspected by composition.
        var result = await Result<int, string>.Success(1).BindValueAsync(_ => new ValueTask<Result<int, string>>(Task.FromResult(default(Result<int, string>)))).AsTask();
#pragma warning restore FS1001
        Assert.Equal("Uninitialized", result.ToString());
        Assert.Throws<InvalidOperationException>(() => result.IsSuccess);
    }

    [Fact]
    public async Task Identity459BINDERDEFAULTPreservesTheExactAsyncRuntimeBehavior()
    {
#pragma warning disable FS1001 // The returned default binder carrier is accepted, not inspected by composition.
        var result = await UnitResult<string>.Success().BindAsync(ct => CheckToken(ct, CancellationToken.None, Task.FromResult(default(UnitResult<string>))), CancellationToken.None);
#pragma warning restore FS1001
        Assert.Equal("Uninitialized", result.ToString());
        Assert.Throws<InvalidOperationException>(() => result.IsSuccess);
    }

    [Fact]
    public async Task Identity460BINDERDEFAULTPreservesTheExactAsyncRuntimeBehavior()
    {
#pragma warning disable FS1001 // The returned default binder carrier is accepted, not inspected by composition.
        var result = await UnitResult<string>.Success().BindAsync(() => Task.FromResult(default(UnitResult<string>)));
#pragma warning restore FS1001
        Assert.Equal("Uninitialized", result.ToString());
        Assert.Throws<InvalidOperationException>(() => result.IsSuccess);
    }

    [Fact]
    public async Task Identity464BINDERDEFAULTPreservesTheExactAsyncRuntimeBehavior()
    {
#pragma warning disable FS1001 // The returned default binder carrier is accepted, not inspected by composition.
        var result = await UnitResult<string>.Success().BindValueAsync(ct => CheckToken(ct, CancellationToken.None, new ValueTask<UnitResult<string>>(Task.FromResult(default(UnitResult<string>)))), CancellationToken.None).AsTask();
#pragma warning restore FS1001
        Assert.Equal("Uninitialized", result.ToString());
        Assert.Throws<InvalidOperationException>(() => result.IsSuccess);
    }

    [Fact]
    public async Task Identity465BINDERDEFAULTPreservesTheExactAsyncRuntimeBehavior()
    {
#pragma warning disable FS1001 // The returned default binder carrier is accepted, not inspected by composition.
        var result = await UnitResult<string>.Success().BindValueAsync(() => new ValueTask<UnitResult<string>>(Task.FromResult(default(UnitResult<string>)))).AsTask();
#pragma warning restore FS1001
        Assert.Equal("Uninitialized", result.ToString());
        Assert.Throws<InvalidOperationException>(() => result.IsSuccess);
    }
}

