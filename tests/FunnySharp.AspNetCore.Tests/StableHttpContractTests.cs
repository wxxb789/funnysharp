using FunnySharp;
using FunnySharp.AspNetCore;
using FunnySharp.Tests;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FunnySharp.AspNetCore.Tests;

public sealed class StableHttpContractTests
{
    [Fact]
    public async Task HttpAwaitTurnsFaultedOperationCanceledExceptionIntoCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var expected = new OperationCanceledException(cancellation.Token);
        var option = Task.FromException<Option<int>>(expected);
        var result = Task.FromException<Result<int, string>>(expected);
        var validation = Task.FromException<Validation<int, string>>(expected);
        var unit = Task.FromException<UnitResult<string>>(expected);
        Assert.All(new Task[] { option, result, validation, unit }, source => Assert.True(source.IsFaulted));

        var mapped = new Task<IResult>[]
        {
            option.ToHttpResultAsync(() => new ProblemDetails { Status = 404 }),
            result.ToHttpResultAsync(_ => new ProblemDetails { Status = 400 }),
            validation.ToHttpResultAsync(_ => new HttpValidationProblemDetails { Status = 400 }),
            unit.ToHttpResultAsync(_ => new ProblemDetails { Status = 400 }),
            new ValueTask<Option<int>>(option).ToHttpResultAsync(() => new ProblemDetails { Status = 404 }).AsTask(),
            new ValueTask<Result<int, string>>(result).ToHttpResultAsync(_ => new ProblemDetails { Status = 400 }).AsTask(),
            new ValueTask<Validation<int, string>>(validation).ToHttpResultAsync(_ => new HttpValidationProblemDetails { Status = 400 }).AsTask(),
            new ValueTask<UnitResult<string>>(unit).ToHttpResultAsync(_ => new ProblemDetails { Status = 400 }).AsTask(),
        };

        foreach (var operation in mapped)
        {
            var actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation);
            Assert.Same(expected, actual);
            Assert.Equal(cancellation.Token, actual.CancellationToken);
            Assert.True(operation.IsCanceled);
            Assert.False(operation.IsFaulted);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task HttpValueTaskCarriersAreConsumedOnceForCompletedAndPendingInputs(bool pending)
    {
        var option = pending ? new CountingValueTaskSource<Option<int>>() : new CountingValueTaskSource<Option<int>>(Option.Some(42));
        var result = pending ? new CountingValueTaskSource<Result<int, string>>() : new CountingValueTaskSource<Result<int, string>>(Result<int, string>.Success(42));
        var validation = pending ? new CountingValueTaskSource<Validation<int, string>>() : new CountingValueTaskSource<Validation<int, string>>(Validation<int, string>.Valid(42));
        var unit = pending ? new CountingValueTaskSource<UnitResult<string>>() : new CountingValueTaskSource<UnitResult<string>>(UnitResult<string>.Success());

        // Each mapping's real async Core subscribes to its pending ValueTask
        // before this call returns; completion below triggers those exact awaits.
        var operations = new Task<IResult>[]
        {
            option.CreateValueTask().ToHttpResultAsync(() => new ProblemDetails { Status = 404 }).AsTask(),
            result.CreateValueTask().ToHttpResultAsync(_ => new ProblemDetails { Status = 400 }).AsTask(),
            validation.CreateValueTask().ToHttpResultAsync(_ => new HttpValidationProblemDetails { Status = 400 }).AsTask(),
            unit.CreateValueTask().ToHttpResultAsync(_ => new ProblemDetails { Status = 400 }).AsTask(),
        };

        if (pending)
        {
            option.SetResult(Option.Some(42));
            result.SetResult(Result<int, string>.Success(42));
            validation.SetResult(Validation<int, string>.Valid(42));
            unit.SetResult(UnitResult<string>.Success());
        }

        var responses = await Task.WhenAll(operations).WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);
        Assert.All(responses, response => Assert.NotNull(response));
        Assert.Equal(1, option.GetResultCount);
        Assert.Equal(1, result.GetResultCount);
        Assert.Equal(1, validation.GetResultCount);
        Assert.Equal(1, unit.GetResultCount);
    }
}
