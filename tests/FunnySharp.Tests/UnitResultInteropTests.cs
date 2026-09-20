namespace FunnySharp.Tests;

public sealed class UnitResultInteropTests
{
    [Fact]
    public void ResultConvertsToUnitResultDroppingTheValueAndPreservingFailureIdentity()
    {
        var error = new InvalidOperationException("failed");

        var success = Result<int, Exception>.Success(42).ToUnitResult();
        var failure = Result<int, Exception>.Failure(error).ToUnitResult();

        Assert.Equal(UnitResult<Exception>.Success(), success);
        Assert.True(failure.TryGetError(out var actual));
        Assert.Same(error, actual);

        var uninitializedException = Assert.Throws<InvalidOperationException>(
            () => default(Result<int, string>).ToUnitResult());
        Assert.Equal("The result has not been initialized.", uninitializedException.Message);
    }

    [Fact]
    public async Task TaskResultToUnitResultAsyncPreservesSuccessFailureFaultAndCancellation()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var fault = new InvalidOperationException("fault");

        var success = await Task.FromResult(Result<int, string>.Success(42)).ToUnitResultAsync();
        var failure = await Task.FromResult(Result<int, string>.Failure("bad")).ToUnitResultAsync();
        var faultedOperation = Task.FromException<Result<int, string>>(fault).ToUnitResultAsync();
        var canceledOperation = Task.FromCanceled<Result<int, string>>(cancellationSource.Token)
            .ToUnitResultAsync();

        Assert.Equal(UnitResult<string>.Success(), success);
        Assert.Equal(UnitResult<string>.Failure("bad"), failure);
        Assert.Same(fault, await Assert.ThrowsAsync<InvalidOperationException>(() => faultedOperation));
        Assert.True(faultedOperation.IsFaulted);
        var cancellation = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => canceledOperation);
        Assert.True(canceledOperation.IsCanceled);
        Assert.Equal(cancellationSource.Token, cancellation.CancellationToken);
        Assert.Throws<ArgumentNullException>(() => { _ = ((Task<Result<int, string>>)null!).ToUnitResultAsync(); });
    }

    [Fact]
    public async Task ValueTaskResultToUnitResultAsyncPreservesSuccessFailureFaultAndCancellation()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var fault = new InvalidOperationException("fault");

        var success = await new ValueTask<Result<int, string>>(Result<int, string>.Success(42))
            .ToUnitResultAsync();
        var failure = await new ValueTask<Result<int, string>>(Result<int, string>.Failure("bad"))
            .ToUnitResultAsync();
        var faultedOperation = ValueTask.FromException<Result<int, string>>(fault)
            .ToUnitResultAsync()
            .AsTask();
        var canceledOperation = ValueTask.FromCanceled<Result<int, string>>(cancellationSource.Token)
            .ToUnitResultAsync()
            .AsTask();

        Assert.Equal(UnitResult<string>.Success(), success);
        Assert.Equal(UnitResult<string>.Failure("bad"), failure);
        Assert.Same(fault, await Assert.ThrowsAsync<InvalidOperationException>(() => faultedOperation));
        Assert.True(faultedOperation.IsFaulted);
        var cancellation = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => canceledOperation);
        Assert.True(canceledOperation.IsCanceled);
        Assert.Equal(cancellationSource.Token, cancellation.CancellationToken);
    }

    [Fact]
    public void OptionConvertsToUnitResultWithEagerOrLazyFailure()
    {
        var errorFactoryCalls = 0;
        Func<string> errorFactory = () =>
        {
            errorFactoryCalls++;
            return "missing";
        };

        var present = Option.Some(42).ToUnitResult(errorFactory);
        var absent = Option.None<int>().ToUnitResult(errorFactory);
        var eager = Option.None<int>().ToUnitResult("missing");

        Assert.Equal(UnitResult<string>.Success(), present);
        Assert.Equal(UnitResult<string>.Failure("missing"), absent);
        Assert.Equal(UnitResult<string>.Failure("missing"), eager);
        Assert.Equal(1, errorFactoryCalls);
        Assert.Throws<ArgumentNullException>(() => { _ = Option.Some(42).ToUnitResult((Func<string>)null!); });
    }

    [Fact]
    public void UnitResultAndResultRoundTripThroughToResult()
    {
        var success = UnitResult<string>.Success();
        var failure = UnitResult<string>.Failure("bad");
        var resultSuccess = Result<int, string>.Success(42);
        var resultFailure = Result<int, string>.Failure("bad");

        Assert.Equal(success, success.ToResult(() => 42).ToUnitResult());
        Assert.Equal(failure, failure.ToResult(() => 42).ToUnitResult());
        Assert.Equal(resultSuccess, resultSuccess.ToUnitResult().ToResult(() => 42));
        Assert.Equal(resultFailure, resultFailure.ToUnitResult().ToResult(() => 42));
    }
}
