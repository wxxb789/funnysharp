namespace FunnySharp.Tests;

public sealed class ResultInteropTests
{
    [Fact]
    public void OptionConvertsToResultWithEagerOrLazyFailure()
    {
        var errorFactoryCalls = 0;
        Func<string> errorFactory = () =>
        {
            errorFactoryCalls++;
            return "missing";
        };

        var present = Option.Some(42).ToResult(errorFactory);
        var absent = Option.None<int>().ToResult(errorFactory);
        var eager = Option.None<int>().ToResult("missing");

        Assert.Equal(Result<int, string>.Success(42), present);
        Assert.Equal(Result<int, string>.Failure("missing"), absent);
        Assert.Equal(Result<int, string>.Failure("missing"), eager);
        Assert.Equal(1, errorFactoryCalls);
        Assert.Throws<ArgumentNullException>(() => Option.Some(42).ToResult((Func<string>)null!));
    }

    [Fact]
    public void ResultConvertsToOptionAndNormalizesSuccessfulNull()
    {
        var success = Result<int, string>.Success(42).ToOption();
        var failure = Result<int, string>.Failure("bad").ToOption();
        var nullSuccess = Result<string?, string>.Success(null).ToOption();

        Assert.Equal(Option.Some(42), success);
        Assert.Equal(Option.None<int>(), failure);
        Assert.Equal(Option.None<string?>(), nullSuccess);
    }

    [Fact]
    public void StandardDelegatesComposeThroughPipeAndBind()
    {
        Func<string, Result<int, string>> parse = text =>
            int.TryParse(text, out var value)
                ? Result<int, string>.Success(value)
                : Result<int, string>.Failure("not-an-integer");
        Func<int, Result<int, string>> validate = value =>
            value > 0
                ? Result<int, string>.Success(value)
                : Result<int, string>.Failure("not-positive");

        var success = "42".Pipe(parse).Bind(validate).Map(value => value * 2);
        var parseFailure = "invalid".Pipe(parse).Bind(validate);
        var validationFailure = "0".Pipe(parse).Bind(validate);

        Assert.Equal(Result<int, string>.Success(84), success);
        Assert.Equal(Result<int, string>.Failure("not-an-integer"), parseFailure);
        Assert.Equal(Result<int, string>.Failure("not-positive"), validationFailure);
    }
}
