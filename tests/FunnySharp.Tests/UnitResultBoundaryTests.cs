using System.Diagnostics;
using System.Runtime.ExceptionServices;

namespace FunnySharp.Tests;

public sealed class UnitResultBoundaryTests
{
    [Fact]
    public void TryReturnsSuccessOrTheOriginalException()
    {
        var expected = new InvalidOperationException("boundary failed");

        var success = UnitResult.Try(() => { });
        var failure = UnitResult.Try(() => Throw(expected));

        Assert.True(success.IsSuccess);
        Assert.True(failure.TryGetError(out var actual));
        Assert.Same(expected, actual);
        Assert.NotNull(actual);
        Assert.Contains(nameof(Throw), actual.StackTrace);
    }

    [Fact]
    public void TryMapsErrorsOnlyWhenTheOperationFails()
    {
        var mapperCalls = 0;
        var expected = new InvalidOperationException("boundary failed");
        Func<Exception, DomainFailure> mapper = exception =>
        {
            mapperCalls++;
            return new("external-api", exception);
        };

        var success = UnitResult.Try<DomainFailure>(() => { }, mapper);
        var failure = UnitResult.Try<DomainFailure>(() => throw expected, mapper);

        Assert.True(success.IsSuccess);
        Assert.True(failure.TryGetError(out var error));
        Assert.NotNull(error);
        Assert.Equal("external-api", error.Code);
        Assert.Same(expected, error.Exception);
        Assert.Equal(1, mapperCalls);
    }

    [Fact]
    public void TryNeverConvertsCancellationIntoFailure()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var expected = new OperationCanceledException(cancellationSource.Token);

        var actual = Assert.Throws<OperationCanceledException>(
            () => UnitResult.Try(() => Throw(expected)));

        Assert.Same(expected, actual);
        Assert.Equal(cancellationSource.Token, actual.CancellationToken);
    }

    [Fact]
    public void TryValidatesDelegatesAndPreservesMapperExceptions()
    {
        var expected = new InvalidOperationException("mapper failed");

        Assert.Throws<ArgumentNullException>(() => UnitResult.Try(null!));
        Assert.Throws<ArgumentNullException>(() => UnitResult.Try<string>(() => { }, null!));
        var actual = Assert.Throws<InvalidOperationException>(() =>
            UnitResult.Try<string>(
                () => throw new FormatException("bad input"),
                _ => throw expected));

        Assert.Same(expected, actual);
    }

    [Fact]
    public async Task TryAsyncReturnsSuccessOrTheOriginalException()
    {
        var expected = new InvalidOperationException("task boundary failed");

        var success = await UnitResult.TryAsync(() => Task.CompletedTask);
        var failure = await UnitResult.TryAsync(() => Task.FromException(expected));

        Assert.True(success.IsSuccess);
        Assert.True(failure.TryGetError(out var actual));
        Assert.Same(expected, actual);
    }

    [Fact]
    public async Task TryValueAsyncReturnsSuccessOrTheOriginalException()
    {
        var expected = new InvalidOperationException("value task boundary failed");

        var success = await UnitResult.TryValueAsync(() => ValueTask.CompletedTask);
        var failure = await UnitResult.TryValueAsync(() => ValueTask.FromException(expected));

        Assert.True(success.IsSuccess);
        Assert.True(failure.TryGetError(out var actual));
        Assert.Same(expected, actual);
    }

    [Fact]
    public async Task AsyncTryMappersRunOnlyForFailure()
    {
        var mapperCalls = 0;
        var taskFailure = new InvalidOperationException("task failed");
        var valueFailure = new InvalidOperationException("value task failed");
        Func<Exception, DomainFailure> mapper = exception =>
        {
            mapperCalls++;
            return new("external-api", exception);
        };

        var taskSuccess = await UnitResult.TryAsync<DomainFailure>(() => Task.CompletedTask, mapper);
        var taskError = await UnitResult.TryAsync<DomainFailure>(
            () => Task.FromException(taskFailure),
            mapper);
        var valueSuccess = await UnitResult.TryValueAsync<DomainFailure>(
            () => ValueTask.CompletedTask,
            mapper);
        var valueError = await UnitResult.TryValueAsync<DomainFailure>(
            () => ValueTask.FromException(valueFailure),
            mapper);

        Assert.True(taskSuccess.IsSuccess);
        Assert.True(valueSuccess.IsSuccess);
        Assert.True(taskError.TryGetError(out var taskDomainError));
        Assert.True(valueError.TryGetError(out var valueDomainError));
        Assert.NotNull(taskDomainError);
        Assert.NotNull(valueDomainError);
        Assert.Same(taskFailure, taskDomainError.Exception);
        Assert.Same(valueFailure, valueDomainError.Exception);
        Assert.Equal(2, mapperCalls);
    }

    [Fact]
    public async Task AsyncTryNeverConvertsCancellationIntoFailure()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        var task = UnitResult.TryAsync(() => Task.FromCanceled(cancellationSource.Token));
        var valueTask = UnitResult.TryValueAsync(
            () => ValueTask.FromCanceled(cancellationSource.Token));
        var taskCancellation = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        var valueTaskCancellation = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await valueTask);

        Assert.True(task.IsCanceled);
        Assert.True(valueTask.IsCanceled);
        Assert.Equal(cancellationSource.Token, taskCancellation.CancellationToken);
        Assert.Equal(cancellationSource.Token, valueTaskCancellation.CancellationToken);
    }

    [Fact]
    public async Task AsyncTryKeepsFaultedOperationCanceledExceptionsFaulted()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var taskFailure = new OperationCanceledException(cancellationSource.Token);
        var valueTaskFailure = new OperationCanceledException(cancellationSource.Token);
        var mapperCalls = 0;

        var task = UnitResult.TryAsync<string>(
            () => Task.FromException(taskFailure),
            _ =>
            {
                mapperCalls++;
                return "mapped";
            });
        var valueTask = UnitResult.TryValueAsync<string>(
            () => ValueTask.FromException(valueTaskFailure),
            _ =>
            {
                mapperCalls++;
                return "mapped";
            }).AsTask();

        Assert.Same(taskFailure, await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task));
        Assert.Same(
            valueTaskFailure,
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => valueTask));
        Assert.True(task.IsFaulted);
        Assert.True(valueTask.IsFaulted);
        Assert.False(task.IsCanceled);
        Assert.False(valueTask.IsCanceled);
        Assert.Equal(0, mapperCalls);
    }

    [Fact]
    public async Task SynchronousAsyncBoundaryThrowsAreReturnedAndClassified()
    {
        var failure = new InvalidOperationException("synchronous operation failure");
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var cancellation = new OperationCanceledException(cancellationSource.Token);
        Task<UnitResult<Exception>> failedTask = null!;
        ValueTask<UnitResult<Exception>> failedValueTask = default;
        Task<UnitResult<Exception>> cancelledTask = null!;
        ValueTask<UnitResult<Exception>> cancelledValueTask = default;

        var taskCallException = Record.Exception(() =>
        {
            failedTask = UnitResult.TryAsync(() => throw failure);
        });
        var valueCallException = Record.Exception(() =>
        {
            failedValueTask = UnitResult.TryValueAsync(() => throw failure);
        });
        var taskCancellationCallException = Record.Exception(() =>
        {
            cancelledTask = UnitResult.TryAsync(() => throw cancellation);
        });
        var valueCancellationCallException = Record.Exception(() =>
        {
            cancelledValueTask = UnitResult.TryValueAsync(() => throw cancellation);
        });

        Assert.Null(taskCallException);
        Assert.Null(valueCallException);
        Assert.Null(taskCancellationCallException);
        Assert.Null(valueCancellationCallException);
        Assert.True((await failedTask).TryGetError(out var taskError));
        Assert.True((await failedValueTask).TryGetError(out var valueError));
        Assert.Same(failure, taskError);
        Assert.Same(failure, valueError);
        Assert.Same(
            cancellation,
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cancelledTask));
        Assert.Same(
            cancellation,
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await cancelledValueTask));
        Assert.True(cancelledTask.IsCanceled);
        Assert.True(cancelledValueTask.IsCanceled);
    }

    [Fact]
    public async Task PendingTaskBoundaryMapsSuccessAndFailure()
    {
        var completion = new TaskCompletionSource();
        var expected = new InvalidOperationException("pending failure");

        var successOperation = UnitResult.TryAsync(() => completion.Task);

        Assert.False(successOperation.IsCompleted);
        completion.SetResult();
        Assert.True((await successOperation).IsSuccess);

        var faultedCompletion = new TaskCompletionSource();
        var failureOperation = UnitResult.TryAsync<DomainFailure>(
            () => faultedCompletion.Task,
            _ => new DomainFailure("mapped", expected));

        Assert.False(failureOperation.IsCompleted);
        faultedCompletion.SetException(expected);
        Assert.True((await failureOperation).TryGetError(out var error));
        Assert.NotNull(error);
        Assert.Equal("mapped", error.Code);
        Assert.Same(expected, error.Exception);
    }

    [Fact]
    public async Task AsyncTryRejectsNullTasksWithoutMappingThem()
    {
        var mapperCalls = 0;

        var defaultResult = UnitResult.TryAsync(() => null!);
        var mappedResult = UnitResult.TryAsync<string>(
            () => null!,
            _ =>
            {
                mapperCalls++;
                return "mapped";
            });

        var defaultFailure = await Assert.ThrowsAsync<InvalidOperationException>(() => defaultResult);
        var mappedFailure = await Assert.ThrowsAsync<InvalidOperationException>(() => mappedResult);

        Assert.Equal("The operation returned a null task.", defaultFailure.Message);
        Assert.Equal("The operation returned a null task.", mappedFailure.Message);
        Assert.Equal(0, mapperCalls);
    }

    [Fact]
    public async Task AsyncErrorMapperExceptionsFaultTheReturnedAwaitables()
    {
        var expected = new InvalidOperationException("mapper failed");
        var task = UnitResult.TryAsync<string>(
            () => Task.FromException(new FormatException("bad input")),
            _ => throw expected);
        var valueTask = UnitResult.TryValueAsync<string>(
            () => ValueTask.FromException(new FormatException("bad input")),
            _ => throw expected).AsTask();

        Assert.Same(expected, await Assert.ThrowsAsync<InvalidOperationException>(() => task));
        Assert.Same(expected, await Assert.ThrowsAsync<InvalidOperationException>(() => valueTask));
    }

    [Fact]
    public void AsyncTryValidatesDelegatesSynchronously()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = UnitResult.TryAsync(null!);
        });
        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = UnitResult.TryAsync<string>(() => Task.CompletedTask, null!);
        });
        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = UnitResult.TryValueAsync(null!);
        });
        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = UnitResult.TryValueAsync<string>(() => ValueTask.CompletedTask, null!);
        });
    }

    private static int Throw(Exception exception) => throw exception;

    private sealed record DomainFailure(string Code, Exception Exception);
}
