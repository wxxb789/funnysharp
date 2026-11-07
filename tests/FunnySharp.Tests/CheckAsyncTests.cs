using FunnySharp.TestSupport.Checks;

namespace FunnySharp.Tests;

public sealed class CheckAsyncTests
{
    [Fact]
    public async Task CheckAsyncAndCheckValueAsyncRetainTheExactValueAndShortCircuitFailure()
    {
        var order = CheckFixtures.ValidOrder;
        var success = Result<CheckOrder, CheckError>.Success(order);
        var existing = new CheckError("existing", "already failed");
        var failure = Result<CheckOrder, CheckError>.Failure(existing);
        var taskCalls = 0;
        var valueCalls = 0;

        var taskKept = await success.CheckAsync(value =>
        {
            taskCalls++;
            Assert.Same(order, value);
            return Task.FromResult(UnitResult<CheckError>.Success());
        });
        var taskReplaced = await success.CheckAsync(value =>
        {
            taskCalls++;
            Assert.Same(order, value);
            return Task.FromResult(
                UnitResult<CheckError>.Failure(new CheckError("rejected", "task")));
        });
        var taskSkipped = await failure.CheckAsync(_ =>
        {
            taskCalls++;
            return Task.FromResult(UnitResult<CheckError>.Success());
        });

        var valueKept = await success.CheckValueAsync(value =>
        {
            valueCalls++;
            Assert.Same(order, value);
            return ValueTask.FromResult(UnitResult<CheckError>.Success());
        });
        var valueReplaced = await success.CheckValueAsync(value =>
        {
            valueCalls++;
            Assert.Same(order, value);
            return ValueTask.FromResult(
                UnitResult<CheckError>.Failure(new CheckError("rejected", "value")));
        });
        var valueSkipped = await failure.CheckValueAsync(_ =>
        {
            valueCalls++;
            return ValueTask.FromResult(UnitResult<CheckError>.Success());
        });

        Assert.True(taskKept.TryGetValue(out var taskValue));
        Assert.Same(order, taskValue);
        Assert.True(taskReplaced.TryGetError(out var taskError));
        Assert.Equal(new CheckError("rejected", "task"), taskError);
        Assert.Equal(failure, taskSkipped);

        Assert.True(valueKept.TryGetValue(out var valueValue));
        Assert.Same(order, valueValue);
        Assert.True(valueReplaced.TryGetError(out var valueError));
        Assert.Equal(new CheckError("rejected", "value"), valueError);
        Assert.Equal(failure, valueSkipped);

        Assert.Equal(2, taskCalls);
        Assert.Equal(2, valueCalls);
    }

    [Fact]
    public async Task CheckAsyncCallbacksReceiveTheExactTokenWithoutEagerCancellation()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var tokens = new List<CancellationToken>();

        var taskResult = await Result<int, string>.Success(1).CheckAsync(
            (value, token) =>
            {
                tokens.Add(token);
                return Task.FromResult(UnitResult<string>.Success());
            },
            cancellationSource.Token);
        var valueResult = await Result<int, string>.Success(1).CheckValueAsync(
            (value, token) =>
            {
                tokens.Add(token);
                return ValueTask.FromResult(UnitResult<string>.Success());
            },
            cancellationSource.Token);

        Assert.Equal(Result<int, string>.Success(1), taskResult);
        Assert.Equal(Result<int, string>.Success(1), valueResult);
        Assert.Equal(2, tokens.Count);
        Assert.All(tokens, token => Assert.Equal(cancellationSource.Token, token));
    }

    [Fact]
    public async Task CheckAsyncFailureShortCircuitsWithoutInspectingTheCancellation()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var calls = 0;
        var failure = Result<int, string>.Failure("bad");

        var taskResult = await failure.CheckAsync(
            (value, token) =>
            {
                calls++;
                return Task.FromResult(UnitResult<string>.Success());
            },
            cancellationSource.Token);
        var valueResult = await failure.CheckValueAsync(
            (value, token) =>
            {
                calls++;
                return ValueTask.FromResult(UnitResult<string>.Success());
            },
            cancellationSource.Token);

        Assert.Equal(failure, taskResult);
        Assert.Equal(failure, valueResult);
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task CheckAsyncPreservesFaultIdentityAndFaultedCancellationStates()
    {
        var failure = new InvalidOperationException("fault");
        using var cancellationSource = new CancellationTokenSource();
        var faultedCancellation = new OperationCanceledException(cancellationSource.Token);

        var task = Result<int, string>.Success(1).CheckAsync(
            _ => Task.FromException<UnitResult<string>>(failure));
        var taskWithToken = Result<int, string>.Success(1).CheckAsync(
            (_, _) => Task.FromException<UnitResult<string>>(faultedCancellation),
            cancellationSource.Token);
        var valueTask = Result<int, string>.Success(1).CheckValueAsync(
            _ => ValueTask.FromException<UnitResult<string>>(failure)).AsTask();
        var valueTaskWithToken = Result<int, string>.Success(1).CheckValueAsync(
            (_, _) => ValueTask.FromException<UnitResult<string>>(faultedCancellation),
            cancellationSource.Token).AsTask();

        Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => task));
        Assert.Same(
            faultedCancellation,
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => taskWithToken));
        Assert.Same(
            failure,
            await Assert.ThrowsAsync<InvalidOperationException>(() => valueTask));
        Assert.Same(
            faultedCancellation,
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => valueTaskWithToken));

        Assert.True(task.IsFaulted);
        Assert.True(taskWithToken.IsFaulted);
        Assert.True(valueTask.IsFaulted);
        Assert.True(valueTaskWithToken.IsFaulted);
        Assert.False(task.IsCanceled);
        Assert.False(taskWithToken.IsCanceled);
        Assert.False(valueTask.IsCanceled);
        Assert.False(valueTaskWithToken.IsCanceled);
    }

    [Fact]
    public async Task CheckAsyncPreservesCancellationStatusAndToken()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        var task = Result<int, string>.Success(1).CheckAsync(
            _ => Task.FromCanceled<UnitResult<string>>(cancellationSource.Token));
        var valueTask = Result<int, string>.Success(1).CheckValueAsync(
            _ => ValueTask.FromCanceled<UnitResult<string>>(cancellationSource.Token)).AsTask();

        var taskCancellation = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        var valueCancellation = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => valueTask);

        Assert.True(task.IsCanceled);
        Assert.True(valueTask.IsCanceled);
        Assert.False(task.IsFaulted);
        Assert.False(valueTask.IsFaulted);
        Assert.Equal(cancellationSource.Token, taskCancellation.CancellationToken);
        Assert.Equal(cancellationSource.Token, valueCancellation.CancellationToken);
    }

    [Fact]
    public async Task PendingCheckAsyncFaultsAndCancelsWithTheOriginalState()
    {
        var failure = new InvalidOperationException("pending fault");
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var faultedCancellation = new OperationCanceledException(cancellationSource.Token);
        var taskSource = new TaskCompletionSource<UnitResult<string>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var faultedSource = new TaskCompletionSource<UnitResult<string>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var valueTaskSource = new TaskCompletionSource<UnitResult<string>>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        var pending = Result<int, string>.Success(1).CheckAsync(_ => taskSource.Task);
        var faulted = Result<int, string>.Success(1).CheckAsync(_ => faultedSource.Task);
        var valueTask = Result<int, string>.Success(1).CheckValueAsync(
            _ => new ValueTask<UnitResult<string>>(valueTaskSource.Task)).AsTask();

        Assert.False(pending.IsCompleted);
        Assert.False(faulted.IsCompleted);
        Assert.False(valueTask.IsCompleted);

        taskSource.SetException(failure);
        faultedSource.SetException(faultedCancellation);
        valueTaskSource.SetResult(UnitResult<string>.Failure("value-error"));

        Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => pending));
        Assert.Same(
            faultedCancellation,
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => faulted));
        Assert.True(pending.IsFaulted);
        Assert.True(faulted.IsFaulted);
        Assert.False(pending.IsCanceled);
        Assert.False(faulted.IsCanceled);

        var valueResult = await valueTask;
        Assert.True(valueResult.TryGetError(out var valueError));
        Assert.Equal("value-error", valueError);
    }

    [Fact]
    public async Task CheckAsyncSynchronousCallbackExceptionsAreReturnedThroughTheAwaitable()
    {
        var failure = new InvalidOperationException("synchronous callback");
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var cancellation = new OperationCanceledException(cancellationSource.Token);
        Task<Result<int, string>> task = null!;
        ValueTask<Result<int, string>> valueTask = default;

        var taskCallException = Record.Exception(() =>
        {
            task = Result<int, string>.Success(1).CheckAsync(
                _ => ThrowTask<string>(failure));
        });
        var valueCallException = Record.Exception(() =>
        {
            valueTask = Result<int, string>.Success(1).CheckValueAsync(
                _ => ThrowValueTask<string>(failure));
        });
        var taskCancellation = Result<int, string>.Success(1).CheckAsync(
            _ => ThrowTask<string>(cancellation));
        var valueCancellation = Result<int, string>.Success(1).CheckValueAsync(
            _ => ThrowValueTask<string>(cancellation)).AsTask();

        Assert.Null(taskCallException);
        Assert.Null(valueCallException);
        Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => task));
        Assert.Same(
            failure,
            await Assert.ThrowsAsync<InvalidOperationException>(async () => { _ = await valueTask; }));
        Assert.True(taskCancellation.IsCanceled);
        Assert.True(valueCancellation.IsCanceled);
        Assert.Same(
            cancellation,
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => taskCancellation));
        Assert.Same(
            cancellation,
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => valueCancellation));
    }

    [Fact]
    public async Task CheckAsyncNullTasksAndUninitializedCheckResultsAreFaults()
    {
        var nullTask = Result<int, string>.Success(1).CheckAsync(
            _ => (Task<UnitResult<string>>)null!);
#pragma warning disable FS1001 // Intentional runtime misuse witness.
        var uninitialized = Result<int, string>.Success(1).CheckAsync(
            _ => Task.FromResult(default(UnitResult<string>)));
#pragma warning restore FS1001
        var defaultValueTask = Result<int, string>.Success(1).CheckValueAsync(
            _ => default(ValueTask<UnitResult<string>>)).AsTask();

        await Assert.ThrowsAsync<NullReferenceException>(() => nullTask);
        await Assert.ThrowsAsync<InvalidOperationException>(() => uninitialized);
        await Assert.ThrowsAsync<InvalidOperationException>(() => defaultValueTask);
        Assert.True(nullTask.IsFaulted);
        Assert.True(uninitialized.IsFaulted);
        Assert.True(defaultValueTask.IsFaulted);
    }

    [Fact]
    public async Task CheckValueAsyncConsumesTheValueTaskExactlyOnce()
    {
        var pendingSource = new CountingValueTaskSource<UnitResult<CheckError>>();
        var completedSource = new CountingValueTaskSource<UnitResult<CheckError>>(
            UnitResult<CheckError>.Success());

        var pending = Result<CheckOrder, CheckError>
            .Success(CheckFixtures.ValidOrder)
            .CheckValueAsync(_ => pendingSource.CreateValueTask());
        var completed = Result<CheckOrder, CheckError>
            .Success(CheckFixtures.ValidOrder)
            .CheckValueAsync(_ => completedSource.CreateValueTask());

        Assert.False(pending.IsCompleted);
        pendingSource.SetResult(
            UnitResult<CheckError>.Failure(new CheckError("pending", "failure")));

        var pendingResult = await pending.AsTask();
        var completedResult = await completed;

        Assert.True(pendingResult.TryGetError(out var pendingError));
        Assert.Equal(new CheckError("pending", "failure"), pendingError);
        Assert.True(completedResult.TryGetValue(out var completedValue));
        Assert.Same(CheckFixtures.ValidOrder, completedValue);
        Assert.Equal(1, pendingSource.GetResultCount);
        Assert.Equal(1, completedSource.GetResultCount);
    }

    [Fact]
    public async Task CheckAsyncRejectsUninitializedReceiversSynchronously()
    {
#pragma warning disable FS1001 // Intentional uninitialized receiver witness.
        Result<int, string> uninitialized = default;
#pragma warning restore FS1001

        Assert.Throws<ArgumentNullException>(
            () => { _ = uninitialized.CheckAsync(null!); });
        Assert.Throws<InvalidOperationException>(
            () => { _ = uninitialized.CheckAsync(_ => Task.FromResult(UnitResult<string>.Success())); });
        Assert.Throws<ArgumentNullException>(
            () => { _ = uninitialized.CheckValueAsync(null!); });
        Assert.Throws<InvalidOperationException>(
            () =>
            {
                _ = uninitialized.CheckValueAsync(
                _ => ValueTask.FromResult(UnitResult<string>.Success()));
            });
    }

    private static Task<UnitResult<TError>> ThrowTask<TError>(Exception exception) =>
        throw exception;

    private static ValueTask<UnitResult<TError>> ThrowValueTask<TError>(Exception exception) =>
        throw exception;
}
