namespace FunnySharp.Tests;

public sealed class ResultTests
{
    [Fact]
    public void DefaultResultIsFailureWithTheDefaultError()
    {
        Result<string, int> result = default;

        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);
        Assert.True(result.TryGetError(out var error));
        Assert.Equal(0, error);
        Assert.False(result.TryGetValue(out var value));
        Assert.Null(value);
    }

    [Fact]
    public void FactoriesPreserveTheirCaseAndPayloadIncludingNull()
    {
        var success = Result<string?, string>.Success(null);
        var failure = Result<string, string?>.Failure(null);

        Assert.True(success.IsSuccess);
        Assert.True(success.TryGetValue(out var value));
        Assert.Null(value);
        Assert.True(failure.IsFailure);
        Assert.True(failure.TryGetError(out var error));
        Assert.Null(error);
    }

    [Fact]
    public void MatchExecutesExactlyOneBranch()
    {
        var successCalls = 0;
        var failureCalls = 0;

        var success = Result<int, string>.Success(3).Match(
            value =>
            {
                successCalls++;
                return value * 2;
            },
            error =>
            {
                failureCalls++;
                return error.Length;
            });
        var failure = Result<int, string>.Failure("bad").Match(
            value =>
            {
                successCalls++;
                return value * 2;
            },
            error =>
            {
                failureCalls++;
                return error.Length;
            });

        Assert.Equal(6, success);
        Assert.Equal(3, failure);
        Assert.Equal(1, successCalls);
        Assert.Equal(1, failureCalls);
    }

    [Fact]
    public void ActionMatchExecutesExactlyOneBranch()
    {
        var values = new List<int>();
        var errors = new List<string>();

        Result<int, string>.Success(3).Match(values.Add, errors.Add);
        Result<int, string>.Failure("bad").Match(values.Add, errors.Add);

        Assert.Equal([3], values);
        Assert.Equal(["bad"], errors);
    }

    [Fact]
    public void MatchValidatesBothBranchesBeforeInspectingTheCase()
    {
        var success = Result<int, string>.Success(1);
        var failure = Result<int, string>.Failure("bad");

        Assert.Throws<ArgumentNullException>(() => success.Match<int>(null!, _ => 0));
        Assert.Throws<ArgumentNullException>(() => success.Match(_ => 0, null!));
        Assert.Throws<ArgumentNullException>(() => failure.Match<int>(null!, _ => 0));
        Assert.Throws<ArgumentNullException>(() => failure.Match(_ => 0, null!));
        Assert.Throws<ArgumentNullException>(() => success.Match(null!, _ => { }));
        Assert.Throws<ArgumentNullException>(() => failure.Match(_ => { }, null!));
    }

    [Fact]
    public void MapObeysIdentityAndCompositionLaws()
    {
        static int Increment(int value) => value + 1;
        static string Render(int value) => $"value:{value}";

        var results = new[]
        {
            Result<int, string>.Success(2),
            Result<int, string>.Failure("bad"),
        };

        foreach (var result in results)
        {
            Assert.Equal(result, result.Map(value => value));
            Assert.Equal(
                result.Map(Increment).Map(Render),
                result.Map(value => Render(Increment(value))));
        }
    }

    [Fact]
    public void MapShortCircuitsFailureAndPreservesCallbackExceptions()
    {
        var calls = 0;
        var expected = new InvalidOperationException("map failed");

        var failure = Result<int, string>.Failure("bad").Map(value =>
        {
            calls++;
            return value + 1;
        });
        var actual = Assert.Throws<InvalidOperationException>(
            () => Result<int, string>.Success(1).Map<int>(_ => throw expected));

        Assert.Equal(Result<int, string>.Failure("bad"), failure);
        Assert.Equal(0, calls);
        Assert.Same(expected, actual);
        Assert.Throws<ArgumentNullException>(() => Result<int, string>.Failure("bad").Map<int>(null!));
    }

    [Fact]
    public void BindObeysMonadLaws()
    {
        static Result<int, string> HalfWhenEven(int value) =>
            value % 2 == 0
                ? Result<int, string>.Success(value / 2)
                : Result<int, string>.Failure("odd");
        static Result<string, string> RenderPositive(int value) =>
            value > 0
                ? Result<string, string>.Success($"value:{value}")
                : Result<string, string>.Failure("not-positive");

        var value = 4;
        Assert.Equal(HalfWhenEven(value), Result<int, string>.Success(value).Bind(HalfWhenEven));

        var results = new[]
        {
            Result<int, string>.Success(4),
            Result<int, string>.Failure("bad"),
        };

        foreach (var result in results)
        {
            Assert.Equal(result, result.Bind(Result<int, string>.Success));
            Assert.Equal(
                result.Bind(HalfWhenEven).Bind(RenderPositive),
                result.Bind(value => HalfWhenEven(value).Bind(RenderPositive)));
        }
    }

    [Fact]
    public void BindShortCircuitsFailureAndPreservesCallbackExceptions()
    {
        var calls = 0;
        var expected = new InvalidOperationException("bind failed");

        var failure = Result<int, string>.Failure("bad").Bind(value =>
        {
            calls++;
            return Result<int, string>.Success(value + 1);
        });
        var actual = Assert.Throws<InvalidOperationException>(
            () => Result<int, string>.Success(1).Bind<int>(_ => throw expected));

        Assert.Equal(Result<int, string>.Failure("bad"), failure);
        Assert.Equal(0, calls);
        Assert.Same(expected, actual);
        Assert.Throws<ArgumentNullException>(() => Result<int, string>.Failure("bad").Bind<int>(null!));
    }

    [Fact]
    public void MapErrorObeysIdentityAndCompositionLaws()
    {
        static string Describe(int error) => $"error:{error}";
        static int Length(string error) => error.Length;

        var results = new[]
        {
            Result<string, int>.Success("ok"),
            Result<string, int>.Failure(42),
        };

        foreach (var result in results)
        {
            Assert.Equal(result, result.MapError(error => error));
            Assert.Equal(
                result.MapError(Describe).MapError(Length),
                result.MapError(error => Length(Describe(error))));
        }
    }

    [Fact]
    public void MapErrorShortCircuitsSuccessAndPreservesCallbackExceptions()
    {
        var calls = 0;
        var expected = new InvalidOperationException("error map failed");

        var success = Result<int, string>.Success(3).MapError(error =>
        {
            calls++;
            return error.Length;
        });
        var actual = Assert.Throws<InvalidOperationException>(
            () => Result<int, string>.Failure("bad").MapError<int>(_ => throw expected));

        Assert.Equal(Result<int, int>.Success(3), success);
        Assert.Equal(0, calls);
        Assert.Same(expected, actual);
        Assert.Throws<ArgumentNullException>(() => Result<int, string>.Success(3).MapError<int>(null!));
    }

    [Fact]
    public void EnsureValidatesSuccessAndPreservesExistingFailure()
    {
        var predicateCalls = 0;
        var errorCalls = 0;
        Func<int, bool> predicate = value =>
        {
            predicateCalls++;
            return value > 0;
        };
        Func<int, string> errorFactory = value =>
        {
            errorCalls++;
            return $"invalid:{value}";
        };

        var valid = Result<int, string>.Success(2).Ensure(predicate, errorFactory);
        var invalid = Result<int, string>.Success(0).Ensure(predicate, errorFactory);
        var failure = Result<int, string>.Failure("existing").Ensure(predicate, errorFactory);

        Assert.Equal(Result<int, string>.Success(2), valid);
        Assert.Equal(Result<int, string>.Failure("invalid:0"), invalid);
        Assert.Equal(Result<int, string>.Failure("existing"), failure);
        Assert.Equal(2, predicateCalls);
        Assert.Equal(1, errorCalls);
        Assert.Equal(
            Result<int, string>.Failure("invalid"),
            Result<int, string>.Success(-1).Ensure(value => value >= 0, "invalid"));
    }

    [Fact]
    public void EnsureValidatesDelegatesEvenForFailure()
    {
        var failure = Result<int, string>.Failure("bad");

        Assert.Throws<ArgumentNullException>(() => failure.Ensure(null!, "invalid"));
        Assert.Throws<ArgumentNullException>(() => failure.Ensure(null!, _ => "invalid"));
        Assert.Throws<ArgumentNullException>(() => failure.Ensure(_ => true, (Func<int, string>)null!));
    }

    [Fact]
    public void RecoverTransformsOnlyFailures()
    {
        var recoverCalls = 0;

        var success = Result<int, string>.Success(2).Recover(error =>
        {
            recoverCalls++;
            return error.Length;
        });
        var recovered = Result<int, string>.Failure("bad").Recover(error =>
        {
            recoverCalls++;
            return error.Length;
        });
        var unresolved = Result<int, string>.Failure("bad").RecoverWith(
            error => Result<int, string>.Failure($"unresolved:{error}"));

        Assert.Equal(Result<int, string>.Success(2), success);
        Assert.Equal(Result<int, string>.Success(3), recovered);
        Assert.Equal(Result<int, string>.Failure("unresolved:bad"), unresolved);
        Assert.Equal(1, recoverCalls);
        Assert.Throws<ArgumentNullException>(() => Result<int, string>.Success(2).Recover(null!));
        Assert.Throws<ArgumentNullException>(() => Result<int, string>.Success(2).RecoverWith(null!));
    }

    [Fact]
    public void ZipCombinesSuccessesAndReturnsTheFirstFailure()
    {
        var combined = Result<int, string>.Success(2).Zip(Result<string, string>.Success("ok"));
        var firstFailure = Result<int, string>.Failure("first").Zip(Result<string, string>.Failure("second"));
        var secondFailure = Result<int, string>.Success(2).Zip(Result<string, string>.Failure("second"));

        Assert.Equal(Result<(int First, string Second), string>.Success((2, "ok")), combined);
        Assert.Equal(Result<(int First, string Second), string>.Failure("first"), firstFailure);
        Assert.Equal(Result<(int First, string Second), string>.Failure("second"), secondFailure);
    }

    [Fact]
    public void ZipWithSkipsTheSecondOperationAfterFailure()
    {
        var calls = 0;
        Func<Result<string, string>> second = () =>
        {
            calls++;
            return Result<string, string>.Success("ok");
        };

        var failure = Result<int, string>.Failure("bad").ZipWith(second);
        var success = Result<int, string>.Success(2).ZipWith(second);

        Assert.Equal(Result<(int First, string Second), string>.Failure("bad"), failure);
        Assert.Equal(Result<(int First, string Second), string>.Success((2, "ok")), success);
        Assert.Equal(1, calls);
        Assert.Throws<ArgumentNullException>(() =>
            Result<int, string>.Failure("bad").ZipWith<string>(null!));
    }

    [Fact]
    public void LinqQuerySyntaxUsesFailFastBinding()
    {
        var bindCalls = 0;
        var projectorCalls = 0;
        Func<int, Result<int, string>> next = value =>
        {
            bindCalls++;
            return Result<int, string>.Success(value + 1);
        };
        Func<int, int, int> project = (left, right) =>
        {
            projectorCalls++;
            return left * right;
        };

        var success =
            from left in Result<int, string>.Success(2)
            from right in next(left)
            select project(left, right);
        var failure =
            from left in Result<int, string>.Failure("bad")
            from right in next(left)
            select project(left, right);
        var intermediateFailure =
            from left in Result<int, string>.Success(3)
            from right in Result<int, string>.Failure("odd")
            select project(left, right);

        Assert.Equal(Result<int, string>.Success(6), success);
        Assert.Equal(Result<int, string>.Failure("bad"), failure);
        Assert.Equal(1, bindCalls);
        Assert.Equal(Result<int, string>.Failure("odd"), intermediateFailure);
        Assert.Equal(1, projectorCalls);
    }

    [Fact]
    public void EqualityHashingAndTextIncludeTheActiveCase()
    {
        var firstSuccess = Result<int, int>.Success(1);
        var secondSuccess = Result<int, int>.Success(1);
        var failure = Result<int, int>.Failure(1);

        Assert.True(firstSuccess == secondSuccess);
        Assert.False(firstSuccess != secondSuccess);
        Assert.NotEqual(firstSuccess, failure);
        Assert.Equal(firstSuccess.GetHashCode(), secondSuccess.GetHashCode());
        Assert.Equal("Success(1)", firstSuccess.ToString());
        Assert.Equal("Failure(1)", failure.ToString());
    }
}
