namespace FunnySharp.Tests;

public sealed class UnitResultTests
{
    [Fact]
    public void FactoriesPreserveTheCaseAndPayloadIncludingNullErrors()
    {
        var staticSuccess = UnitResult.Success<string>();
        var staticFailure = UnitResult.Failure("bad");
        var structSuccess = UnitResult<string>.Success();
        var structFailure = UnitResult<string>.Failure("bad");
        var nullFailure = UnitResult<string?>.Failure(null);

        Assert.True(staticSuccess.IsSuccess);
        Assert.False(staticSuccess.IsFailure);
        Assert.False(staticSuccess.TryGetError(out _));
        Assert.Equal(structSuccess, staticSuccess);

        Assert.True(staticFailure.IsFailure);
        Assert.False(staticFailure.IsSuccess);
        Assert.True(staticFailure.TryGetError(out var error));
        Assert.Equal("bad", error);
        Assert.Equal(staticFailure, structFailure);

        Assert.True(nullFailure.IsFailure);
        Assert.True(nullFailure.TryGetError(out var nullError));
        Assert.Null(nullError);
    }

    [Fact]
    public void DefaultErrorPayloadsAreLegitimateFailures()
    {
        var defaulted = UnitResult<string?>.Failure(default);

        Assert.True(defaulted.IsFailure);
        Assert.True(defaulted.TryGetError(out var error));
        Assert.Null(error);
        Assert.Equal(UnitResult<string?>.Failure(null), defaulted);
        Assert.Equal("Failure()", defaulted.ToString());
    }

    [Fact]
    public void MatchExecutesExactlyOneBranch()
    {
        var successCalls = 0;
        var failureCalls = 0;

        var success = UnitResult<string>.Success().Match(
            () =>
            {
                successCalls++;
                return 6;
            },
            error =>
            {
                failureCalls++;
                return error.Length;
            });
        var failure = UnitResult<string>.Failure("bad").Match(
            () =>
            {
                successCalls++;
                return 6;
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

        var values = new List<string>();
        var errors = new List<string>();

        UnitResult<string>.Success().Match(() => values.Add("success"), errors.Add);
        UnitResult<string>.Failure("bad").Match(() => values.Add("success"), errors.Add);

        Assert.Equal(["success"], values);
        Assert.Equal(["bad"], errors);
    }

    [Fact]
    public void MatchValidatesBothBranchesBeforeInspectingTheCase()
    {
        var success = UnitResult<string>.Success();
        var failure = UnitResult<string>.Failure("bad");
        UnitResult<string> uninitialized = default;

        Assert.Throws<ArgumentNullException>(() => { _ = success.Match<int>(null!, _ => 0); });
        Assert.Throws<ArgumentNullException>(() => { _ = success.Match(() => 0, (Func<string, int>)null!); });
        Assert.Throws<ArgumentNullException>(() => { _ = failure.Match<int>(null!, _ => 0); });
        Assert.Throws<ArgumentNullException>(() => { _ = failure.Match(() => 0, (Func<string, int>)null!); });
        Assert.Throws<ArgumentNullException>(() => success.Match(null!, _ => { }));
        Assert.Throws<ArgumentNullException>(() => failure.Match(() => { }, null!));
        Assert.Throws<ArgumentNullException>(() => { _ = uninitialized.Match<int>(null!, _ => 0); });
        Assert.Throws<ArgumentNullException>(() => uninitialized.Match(null!, _ => { }));
    }

    [Fact]
    public void ToResultProjectsOnlySuccessAndPreservesTheFailureObject()
    {
        var error = new InvalidOperationException("failed");
        var calls = 0;

        var mapped = UnitResult<string>.Success().ToResult(() =>
        {
            calls++;
            return 42;
        });
        var failed = UnitResult<Exception>.Failure(error).ToResult(() =>
        {
            calls++;
            return 42;
        });

        Assert.Equal(Result<int, string>.Success(42), mapped);
        Assert.True(failed.TryGetError(out var actual));
        Assert.Same(error, actual);
        Assert.Equal(1, calls);
        Assert.Throws<ArgumentNullException>(() => { _ = UnitResult<string>.Failure("bad").ToResult<int>(null!); });
    }

    [Fact]
    public void BindInvokesTheBinderOnlyForSuccess()
    {
        var calls = 0;

        var bound = UnitResult<string>.Success().Bind(() =>
        {
            calls++;
            return UnitResult<string>.Failure("bound");
        });
        var error = new InvalidOperationException("existing");
        var failure = UnitResult<Exception>.Failure(error);
        var skipped = failure.Bind(() =>
        {
            calls++;
            return UnitResult<Exception>.Success();
        });

        Assert.Equal(UnitResult<string>.Failure("bound"), bound);
        Assert.Equal(failure, skipped);
        Assert.True(skipped.TryGetError(out var actual));
        Assert.Same(error, actual);
        Assert.Equal(1, calls);
        Assert.Throws<ArgumentNullException>(() => { _ = failure.Bind(null!); });
    }

    [Fact]
    public void EnsureKeepsSuccessOrReplacesItOnlyWhenThePredicateFails()
    {
        var predicateCalls = 0;
        var errorFactoryCalls = 0;
        Func<bool> predicate = () =>
        {
            predicateCalls++;
            return false;
        };
        Func<string> errorFactory = () =>
        {
            errorFactoryCalls++;
            return "invalid";
        };

        var kept = UnitResult<string>.Success().Ensure(() => true, "invalid");
        var replaced = UnitResult<string>.Success().Ensure(predicate, "invalid");
        var lazilyReplaced = UnitResult<string>.Success().Ensure(predicate, errorFactory);
        var failure = UnitResult<string>.Failure("existing");
        var skipped = failure.Ensure(predicate, errorFactory);
        var explicitConditional = UnitResult<string>.Success().Match(
            () => UnitResult<string>.Failure("invalid"),
            _ => UnitResult<string>.Success());

        Assert.Equal(UnitResult<string>.Success(), kept);
        Assert.Equal(UnitResult<string>.Failure("invalid"), replaced);
        Assert.Equal(UnitResult<string>.Failure("invalid"), lazilyReplaced);
        Assert.Equal(explicitConditional, replaced);
        Assert.Equal(failure, skipped);
        Assert.Equal(2, predicateCalls);
        Assert.Equal(1, errorFactoryCalls);
    }

    [Fact]
    public void EnsureValidatesDelegatesEvenForFailure()
    {
        var failure = UnitResult<string>.Failure("bad");
        var predicateCalls = 0;
        Func<bool> predicate = () =>
        {
            predicateCalls++;
            return true;
        };

        Assert.Throws<ArgumentNullException>(() => { _ = failure.Ensure(null!, "invalid"); });
        Assert.Throws<ArgumentNullException>(() => { _ = failure.Ensure(null!, () => "invalid"); });
        Assert.Throws<ArgumentNullException>(() => { _ = failure.Ensure(predicate, (Func<string>)null!); });
        Assert.Equal(0, predicateCalls);
    }

    [Fact]
    public void RecoverWithInvokesRecoveryOnlyForFailure()
    {
        var calls = 0;

        var success = UnitResult<string>.Success().RecoverWith(error =>
        {
            calls++;
            return UnitResult<string>.Failure(error);
        });
        var error = new InvalidOperationException("failed");
        var failure = UnitResult<Exception>.Failure(error);
        var recovered = failure.RecoverWith(_ =>
        {
            calls++;
            return UnitResult<Exception>.Success();
        });

        Assert.Equal(UnitResult<string>.Success(), success);
        Assert.Equal(UnitResult<Exception>.Success(), recovered);
        Assert.Equal(1, calls);
        Assert.Throws<ArgumentNullException>(() => { _ = failure.RecoverWith(null!); });
    }

    [Fact]
    public void MapErrorTransformsOnlyFailures()
    {
        var calls = 0;

        var success = UnitResult<int>.Success().MapError(error =>
        {
            calls++;
            return $"error:{error}";
        });
        var failure = UnitResult<int>.Failure(42).MapError(error =>
        {
            calls++;
            return $"error:{error}";
        });

        Assert.Equal(UnitResult<string>.Success(), success);
        Assert.Equal(UnitResult<string>.Failure("error:42"), failure);
        Assert.Equal(1, calls);
        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = UnitResult<int>.Success().MapError<string>(null!);
        });
    }

    [Fact]
    public void ToResultObeysIdentityAndCompositionLaws()
    {
        var cases = new[]
        {
            UnitResult<string>.Success(),
            UnitResult<string>.Failure("bad"),
        };

        foreach (var result in cases)
        {
            var identity = result.ToResult(() => 7);
            var explicitIdentity = result.Match(
                () => Result<int, string>.Success(7),
                error => Result<int, string>.Failure(error));
            var composed = result.ToResult(() => 2).Map(value => $"value:{value}");
            var explicitComposed = result.ToResult(() => "value:2");

            Assert.Equal(explicitIdentity, identity);
            Assert.Equal(explicitComposed, composed);
        }
    }

    [Fact]
    public void BindObeysMonadLaws()
    {
        static UnitResult<string> Next() => UnitResult<string>.Success();
        static UnitResult<string> Failed() => UnitResult<string>.Failure("next");

        Assert.Equal(Next(), UnitResult<string>.Success().Bind(Next));

        var cases = new[]
        {
            UnitResult<string>.Success(),
            UnitResult<string>.Failure("bad"),
        };

        foreach (var result in cases)
        {
            Assert.Equal(result, result.Bind(UnitResult<string>.Success));
            Assert.Equal(
                result.Bind(Next).Bind(Failed),
                result.Bind(() => Next().Bind(Failed)));
        }
    }

    [Fact]
    public void MapErrorObeysIdentityAndCompositionLaws()
    {
        static string Describe(int error) => $"error:{error}";
        static int Length(string error) => error.Length;

        var cases = new[]
        {
            UnitResult<int>.Success(),
            UnitResult<int>.Failure(42),
        };

        foreach (var result in cases)
        {
            Assert.Equal(result, result.MapError(error => error));
            Assert.Equal(
                result.MapError(Describe).MapError(Length),
                result.MapError(error => Length(Describe(error))));
        }
    }

    [Fact]
    public void ZipReturnsTheFirstFailureAndZipWithSkipsTheFactory()
    {
        var first = UnitResult<string>.Failure("first");
        var second = UnitResult<string>.Failure("second");
        var success = UnitResult<string>.Success();

        Assert.Equal(success, success.Zip(success));
        Assert.Equal(second, success.Zip(second));
        Assert.Equal(second, second.Zip(success));
        Assert.Equal(first, first.Zip(second));
        Assert.Equal(first, first.Zip(success));

        var uninitializedException = Assert.Throws<InvalidOperationException>(() => { _ = success.Zip(default); });
        Assert.Equal("The unit result has not been initialized.", uninitializedException.Message);
        Assert.Throws<InvalidOperationException>(() => { _ = first.Zip(default); });

        var factoryCalls = 0;
        Func<UnitResult<string>> factory = () =>
        {
            factoryCalls++;
            return success;
        };

        Assert.Equal(first, first.ZipWith(factory));
        Assert.Equal(0, factoryCalls);
        Assert.Equal(success, success.ZipWith(factory));
        Assert.Equal(1, factoryCalls);
        Assert.Throws<ArgumentNullException>(() => { _ = first.ZipWith(null!); });
    }

    [Fact]
    public void EqualityAndHashingFollowCaseThenError()
    {
        var success = UnitResult<int>.Success();
        var otherSuccess = UnitResult<int>.Success();
        var failure = UnitResult<int>.Failure(1);
        var equalFailure = UnitResult<int>.Failure(1);
        var unequalFailure = UnitResult<int>.Failure(2);

        Assert.True(success == otherSuccess);
        Assert.False(success != otherSuccess);
        Assert.True(failure == equalFailure);
        Assert.True(success != failure);
        Assert.False(success == failure);
        Assert.False(failure == unequalFailure);
        Assert.True(failure != unequalFailure);
        Assert.Equal(success.GetHashCode(), otherSuccess.GetHashCode());
        Assert.Equal(failure.GetHashCode(), equalFailure.GetHashCode());
        Assert.False(success.Equals((object?)null));
        Assert.False(failure.Equals((object?)null));
        Assert.False(success.Equals("Success"));
        Assert.True(success.Equals((object)otherSuccess));
        Assert.True(success.Equals(otherSuccess));
    }

    [Fact]
    public void NestedUnitResultsCompareStructurallyWithoutFlattening()
    {
        var outerSuccess = UnitResult<UnitResult<string>>.Success();
        var nestedSuccess = UnitResult<UnitResult<string>>.Failure(UnitResult<string>.Success());
        var nestedFailure = UnitResult<UnitResult<string>>.Failure(UnitResult<string>.Failure("inner"));
        var equalNestedFailure = UnitResult<UnitResult<string>>.Failure(UnitResult<string>.Failure("inner"));

        Assert.NotEqual(outerSuccess, nestedSuccess);
        Assert.NotEqual(nestedSuccess, nestedFailure);
        Assert.Equal(nestedFailure, equalNestedFailure);
        Assert.Equal(nestedFailure.GetHashCode(), equalNestedFailure.GetHashCode());
    }

    [Fact]
    public void ToStringDescribesSuccessFailureAndUninitialized()
    {
        Assert.Equal("Success", UnitResult<string>.Success().ToString());
        Assert.Equal("Failure(bad)", UnitResult<string>.Failure("bad").ToString());
        Assert.Equal("Failure(42)", UnitResult<int>.Failure(42).ToString());
        Assert.Equal("Uninitialized", default(UnitResult<string>).ToString());
    }

    [Fact]
    public void DefaultUnitResultThrowsForEveryStateReadingMember()
    {
        UnitResult<string> result = default;

        Assert.Equal("Uninitialized", result.ToString());
        AssertUninitialized(() => { _ = result.IsSuccess; });
        AssertUninitialized(() => { _ = result.IsFailure; });
        AssertUninitialized(() => result.TryGetError(out _));
        AssertUninitialized(() => { _ = result.Match(() => 0, _ => 0); });
        AssertUninitialized(() => result.Match(() => { }, _ => { }));
        AssertUninitialized(() => { _ = result.ToResult(() => 0); });
        AssertUninitialized(() => { _ = result.MapError(error => error.Length); });
        AssertUninitialized(() => { _ = result.Bind(() => UnitResult<string>.Success()); });
        AssertUninitialized(() => { _ = result.Ensure(() => true, "invalid"); });
        AssertUninitialized(() => { _ = result.Ensure(() => true, () => "invalid"); });
        AssertUninitialized(() => { _ = result.RecoverWith(_ => UnitResult<string>.Success()); });
        AssertUninitialized(() => { _ = result.Zip(UnitResult<string>.Success()); });
        AssertUninitialized(() => { _ = result.ZipWith(() => UnitResult<string>.Success()); });
        AssertUninitialized(() => { _ = result.Equals(UnitResult<string>.Success()); });
        AssertUninitialized(() => { _ = result.Equals((object?)null); });
        AssertUninitialized(() => { _ = result.GetHashCode(); });
        AssertUninitialized(() => { _ = result == UnitResult<string>.Success(); });
        AssertUninitialized(() => { _ = result != UnitResult<string>.Success(); });
    }

    [Fact]
    public void ComparisonsBetweenInitializedAndDefaultValuesThrow()
    {
        var success = UnitResult<string>.Success();
        UnitResult<string> uninitialized = default;

        AssertUninitialized(() => { _ = success.Equals((object?)uninitialized); });
        AssertUninitialized(() => { _ = success.Equals(uninitialized); });
        AssertUninitialized(() => { _ = uninitialized.Equals(success); });
        AssertUninitialized(() => { _ = success == uninitialized; });
        AssertUninitialized(() => { _ = uninitialized == success; });
        AssertUninitialized(() => { _ = success != uninitialized; });
        AssertUninitialized(() => { _ = uninitialized != success; });
    }

    [Fact]
    public void DelegateValidationPrecedesCaseInspectionAndShortCircuiting()
    {
        var failure = UnitResult<string>.Failure("bad");
        UnitResult<string> uninitialized = default;

        Assert.Throws<ArgumentNullException>(() => { _ = failure.ToResult<int>(null!); });
        Assert.Throws<ArgumentNullException>(() => { _ = failure.Bind(null!); });
        Assert.Throws<ArgumentNullException>(() => { _ = failure.Ensure(null!, "invalid"); });
        Assert.Throws<ArgumentNullException>(() => { _ = failure.Ensure(() => true, (Func<string>)null!); });
        Assert.Throws<ArgumentNullException>(() => { _ = failure.RecoverWith(null!); });
        Assert.Throws<ArgumentNullException>(() => { _ = failure.MapError<int>(null!); });
        Assert.Throws<ArgumentNullException>(() => { _ = failure.ZipWith(null!); });

        Assert.Throws<ArgumentNullException>(() => { _ = uninitialized.ToResult<int>(null!); });
        Assert.Throws<ArgumentNullException>(() => { _ = uninitialized.Bind(null!); });
        Assert.Throws<ArgumentNullException>(() => { _ = uninitialized.Ensure(null!, "invalid"); });
        Assert.Throws<ArgumentNullException>(() => { _ = uninitialized.RecoverWith(null!); });
        Assert.Throws<ArgumentNullException>(() => { _ = uninitialized.MapError<int>(null!); });
        Assert.Throws<ArgumentNullException>(() => { _ = uninitialized.ZipWith(null!); });
    }

    [Fact]
    public void CallbackExceptionsPropagateUnchanged()
    {
        var expected = new InvalidOperationException("callback");
        var success = UnitResult<string>.Success();
        var failure = UnitResult<string>.Failure("bad");

        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() => { _ = success.ToResult<int>(() => throw expected); }));
        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() => { _ = success.Bind(() => throw expected); }));
        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() => { _ = success.Ensure(() => throw expected, "invalid"); }));
        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() => { _ = success.Ensure(() => false, () => throw expected); }));
        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() => { _ = failure.RecoverWith(_ => throw expected); }));
        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() => { _ = failure.MapError<int>(_ => throw expected); }));
        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() => { _ = success.ZipWith(() => throw expected); }));
        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() => { _ = failure.Match<int>(() => 0, _ => throw expected); }));
        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() => failure.Match(() => { }, _ => throw expected)));
    }

    private static void AssertUninitialized(Action action)
    {
        var exception = Assert.Throws<InvalidOperationException>(action);

        Assert.Equal("The unit result has not been initialized.", exception.Message);
    }
}
