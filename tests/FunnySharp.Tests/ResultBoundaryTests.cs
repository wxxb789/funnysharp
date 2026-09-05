using System.Diagnostics;
using System.Runtime.ExceptionServices;

namespace FunnySharp.Tests;

public sealed class ResultBoundaryTests
{
    [Fact]
    public void TryReturnsSuccessOrTheOriginalException()
    {
        var expected = new InvalidOperationException("boundary failed");

        var success = Result.Try(() => 42);
        var failure = Result.Try<int>(() => Throw(expected));

        Assert.Equal(Result<int, Exception>.Success(42), success);
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

        var success = Result.Try<int, DomainFailure>(() => 42, mapper);
        var failure = Result.Try<int, DomainFailure>(() => Throw(expected), mapper);

        Assert.Equal(Result<int, DomainFailure>.Success(42), success);
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
            () => Result.Try<int>(() => Throw(expected)));

        Assert.Same(expected, actual);
        Assert.Equal(cancellationSource.Token, actual.CancellationToken);
    }

    [Fact]
    public void TryValidatesDelegatesAndPreservesMapperExceptions()
    {
        var expected = new InvalidOperationException("mapper failed");

        Assert.Throws<ArgumentNullException>(() => Result.Try<int>(null!));
        Assert.Throws<ArgumentNullException>(() => Result.Try<int, string>(() => 1, null!));
        var actual = Assert.Throws<InvalidOperationException>(() =>
            Result.Try<int, string>(
                () => throw new FormatException("bad input"),
                _ => throw expected));

        Assert.Same(expected, actual);
    }

    [Fact]
    public async Task TryAsyncReturnsSuccessOrTheOriginalException()
    {
        var expected = new InvalidOperationException("task boundary failed");

        var success = await Result.TryAsync(() => Task.FromResult(42));
        var failure = await Result.TryAsync(() => Task.FromException<int>(expected));

        Assert.Equal(Result<int, Exception>.Success(42), success);
        Assert.True(failure.TryGetError(out var actual));
        Assert.Same(expected, actual);
    }

    [Fact]
    public async Task TryValueAsyncReturnsSuccessOrTheOriginalExceptionAndConsumesOnce()
    {
        var expected = new InvalidOperationException("value task boundary failed");
        var source = new CountingValueTaskSource<int>();

        var pending = Result.TryValueAsync(() => source.CreateValueTask());
        var failure = await Result.TryValueAsync(() => ValueTask.FromException<int>(expected));

        Assert.False(pending.IsCompleted);
        source.SetResult(42);
        Assert.Equal(Result<int, Exception>.Success(42), await pending);
        Assert.Equal(1, source.GetResultCount);
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

        var taskSuccess = await Result.TryAsync<int, DomainFailure>(() => Task.FromResult(1), mapper);
        var taskError = await Result.TryAsync<int, DomainFailure>(
            () => Task.FromException<int>(taskFailure),
            mapper);
        var valueSuccess = await Result.TryValueAsync<int, DomainFailure>(
            () => ValueTask.FromResult(2),
            mapper);
        var valueError = await Result.TryValueAsync<int, DomainFailure>(
            () => ValueTask.FromException<int>(valueFailure),
            mapper);

        Assert.Equal(Result<int, DomainFailure>.Success(1), taskSuccess);
        Assert.Equal(Result<int, DomainFailure>.Success(2), valueSuccess);
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

        var task = Result.TryAsync(() => Task.FromCanceled<int>(cancellationSource.Token));
        var valueTask = Result.TryValueAsync(
            () => ValueTask.FromCanceled<int>(cancellationSource.Token));
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

        var task = Result.TryAsync<int, string>(
            () => Task.FromException<int>(taskFailure),
            _ =>
            {
                mapperCalls++;
                return "mapped";
            });
        var valueTask = Result.TryValueAsync<int, string>(
            () => ValueTask.FromException<int>(valueTaskFailure),
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
        Task<Result<int, Exception>> failedTask = null!;
        ValueTask<Result<int, Exception>> failedValueTask = default;
        Task<Result<int, Exception>> cancelledTask = null!;
        ValueTask<Result<int, Exception>> cancelledValueTask = default;

        var taskCallException = Record.Exception(() =>
        {
            failedTask = Result.TryAsync<int>(() => throw failure);
        });
        var valueCallException = Record.Exception(() =>
        {
            failedValueTask = Result.TryValueAsync<int>(() => throw failure);
        });
        var taskCancellationCallException = Record.Exception(() =>
        {
            cancelledTask = Result.TryAsync<int>(() => throw cancellation);
        });
        var valueCancellationCallException = Record.Exception(() =>
        {
            cancelledValueTask = Result.TryValueAsync<int>(() => throw cancellation);
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
    public async Task SynchronousAsyncCancellationPreservesIdentityStackTokenAndStatus()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var taskCancellation = CaptureCancellation(cancellationSource.Token);
        var valueTaskCancellation = CaptureCancellation(cancellationSource.Token);

        var task = Result.TryAsync<int>(() =>
            Rethrow<Task<int>>(ExceptionDispatchInfo.Capture(taskCancellation)));
        var valueTask = Result.TryValueAsync<int>(() =>
            Rethrow<ValueTask<int>>(ExceptionDispatchInfo.Capture(valueTaskCancellation))).AsTask();

        var actualTaskCancellation = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        var actualValueTaskCancellation = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => valueTask);

        AssertCancellation(
            task,
            actualTaskCancellation,
            taskCancellation,
            cancellationSource.Token,
            nameof(ThrowOriginalCancellation));
        AssertCancellation(
            valueTask,
            actualValueTaskCancellation,
            valueTaskCancellation,
            cancellationSource.Token,
            nameof(ThrowOriginalCancellation));
    }

    [Fact]
    public async Task AsyncErrorMapperCancellationPreservesIdentityStackTokenAndStatus()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var taskCancellation = new OperationCanceledException(cancellationSource.Token);
        var valueTaskCancellation = new OperationCanceledException(cancellationSource.Token);

        var task = Result.TryAsync<int, string>(
            () => Task.FromException<int>(new InvalidOperationException("task source")),
            _ => ThrowCancellation<string>(taskCancellation));
        var valueTask = Result.TryValueAsync<int, string>(
            () => ValueTask.FromException<int>(new InvalidOperationException("value task source")),
            _ => ThrowCancellation<string>(valueTaskCancellation)).AsTask();

        var actualTaskCancellation = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        var actualValueTaskCancellation = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => valueTask);

        AssertCancellation(
            task,
            actualTaskCancellation,
            taskCancellation,
            cancellationSource.Token,
            nameof(ThrowCancellation));
        AssertCancellation(
            valueTask,
            actualValueTaskCancellation,
            valueTaskCancellation,
            cancellationSource.Token,
            nameof(ThrowCancellation));
    }

    [Fact]
    public async Task PendingCanceledSourcesPreserveIdentityStackTokenAndStatus()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var taskCancellation = CaptureCancellation(cancellationSource.Token);
        var valueTaskCancellation = CaptureCancellation(cancellationSource.Token);
        var taskSource = CreateCanceledSourceAsync<int>(taskCancellation);
        var valueTaskSource = CreateCanceledSourceAsync<int>(valueTaskCancellation);

        var task = Result.TryAsync(() => taskSource);
        var valueTask = Result.TryValueAsync(() => new ValueTask<int>(valueTaskSource)).AsTask();

        var actualTaskCancellation = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        var actualValueTaskCancellation = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => valueTask);

        AssertCancellation(
            task,
            actualTaskCancellation,
            taskCancellation,
            cancellationSource.Token,
            nameof(ThrowOriginalCancellation));
        AssertCancellation(
            valueTask,
            actualValueTaskCancellation,
            valueTaskCancellation,
            cancellationSource.Token,
            nameof(ThrowOriginalCancellation));
    }

    [Fact]
    public async Task TryAsyncRejectsNullTasksWithoutMappingThem()
    {
        var mapperCalls = 0;

        var defaultResult = Result.TryAsync<int>(() => null!);
        var mappedResult = Result.TryAsync<int, string>(
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
    public void AsyncTryValidatesDelegatesSynchronously()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = Result.TryAsync<int>(null!);
        });
        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = Result.TryAsync<int, string>(() => Task.FromResult(1), null!);
        });
        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = Result.TryValueAsync<int>(null!);
        });
        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = Result.TryValueAsync<int, string>(() => ValueTask.FromResult(1), null!);
        });
    }

    private static int Throw(Exception exception) => throw exception;

    private static OperationCanceledException CaptureCancellation(CancellationToken cancellationToken)
    {
        try
        {
            ThrowOriginalCancellation(cancellationToken);
        }
        catch (OperationCanceledException exception)
        {
            return exception;
        }

        throw new UnreachableException();
    }

    private static async Task<T> CreateCanceledSourceAsync<T>(OperationCanceledException cancellation)
    {
        await Task.Yield();
        ExceptionDispatchInfo.Capture(cancellation).Throw();
        return default!;
    }

    private static TResult Rethrow<TResult>(ExceptionDispatchInfo dispatchInfo)
    {
        dispatchInfo.Throw();
        throw new UnreachableException();
    }

    private static TResult ThrowCancellation<TResult>(OperationCanceledException cancellation) =>
        throw cancellation;

    private static void ThrowOriginalCancellation(CancellationToken cancellationToken) =>
        throw new OperationCanceledException("prepared cancellation", innerException: null, cancellationToken);

    private static void AssertCancellation<T>(
        Task<T> operation,
        OperationCanceledException actual,
        OperationCanceledException expected,
        CancellationToken cancellationToken,
        string stackMarker)
    {
        Assert.True(operation.IsCanceled);
        Assert.Same(expected, actual);
        Assert.Equal(cancellationToken, actual.CancellationToken);
        Assert.Contains(stackMarker, actual.StackTrace);
    }

    private sealed record DomainFailure(string Code, Exception Exception);

}
