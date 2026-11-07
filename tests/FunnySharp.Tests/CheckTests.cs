using System.Reflection;

namespace FunnySharp.Tests;

public sealed class CheckTests
{
    [Fact]
    public void CheckRetainsTheExactSuccessfulValueAndReplacesOnlyTheFailure()
    {
        var order = new object();
        var result = Result<object, string>.Success(order);
        var calls = 0;

        var kept = result.Check(value =>
        {
            calls++;
            Assert.Same(order, value);
            return UnitResult<string>.Success();
        });
        var replaced = result.Check(_ => UnitResult<string>.Failure("rejected"));

        Assert.True(kept.TryGetValue(out var keptValue));
        Assert.Same(order, keptValue);
        Assert.True(replaced.TryGetError(out var error));
        Assert.Equal("rejected", error);
        Assert.Equal(1, calls);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void CheckSkipsTheCallbackAfterFailureAndStillValidatesTheDelegate()
    {
        var original = new InvalidOperationException("existing");
        var failure = Result<object, Exception>.Failure(original);
        var calls = 0;

        var checkedFailure = failure.Check(_ =>
        {
            calls++;
            return UnitResult<Exception>.Success();
        });

        Assert.Equal(failure, checkedFailure);
        Assert.True(checkedFailure.TryGetError(out var error));
        Assert.Same(original, error);
        Assert.Equal(0, calls);

        Assert.Throws<ArgumentNullException>(() => { _ = failure.Check(null!); });
    }

    [Fact]
    public void CheckRejectsUninitializedReceiversAndUninitializedCheckResults()
    {
        var success = Result<int, string>.Success(1);
#pragma warning disable FS1001 // Intentional uninitialized receiver witness.
        Result<int, string> uninitialized = default;
#pragma warning restore FS1001

        Assert.Throws<InvalidOperationException>(
            () => { _ = uninitialized.Check(_ => UnitResult<string>.Success()); });
        Assert.Throws<ArgumentNullException>(
            () => { _ = uninitialized.Check(null!); });
#pragma warning disable FS1001 // Intentional runtime misuse witness.
        Assert.Throws<InvalidOperationException>(
            () => { _ = success.Check(_ => default); });
#pragma warning restore FS1001
    }

    [Fact]
    public void CheckPreservesRuntimeNullValuesAndNullErrors()
    {
        object? observed = new();
        var result = Result<object?, string?>.Success(null);

        var kept = result.Check(value =>
        {
            observed = value;
            return UnitResult<string?>.Success();
        });
        var failed = result.Check(_ => UnitResult<string?>.Failure(null));

        Assert.True(kept.TryGetValue(out var keptValue));
        Assert.Null(keptValue);
        Assert.Null(observed);
        Assert.True(failed.TryGetError(out var error));
        Assert.Null(error);
    }

    [Fact]
    public void CheckCallbackExceptionsPropagateUnchanged()
    {
        var expected = new InvalidOperationException("check failed");
        var result = Result<int, string>.Success(1);

        var actual = Assert.Throws<InvalidOperationException>(
            () => { _ = result.Check(_ => throw expected); });

        Assert.Same(expected, actual);
    }

    [Fact]
    public void CheckAllRetainsTheExactValueOrAccumulatesEveryErrorInRuleOrder()
    {
        var order = new object();
        var result = Result<object, string>.Success(order);
        var calls = new List<string>();

        Func<object, UnitResult<string>> first = value =>
        {
            Assert.Same(order, value);
            calls.Add("first");
            return UnitResult<string>.Failure("first-error");
        };
        Func<object, UnitResult<string>> second = value =>
        {
            Assert.Same(order, value);
            calls.Add("second");
            return UnitResult<string>.Success();
        };
        Func<object, UnitResult<string>> third = value =>
        {
            Assert.Same(order, value);
            calls.Add("third");
            return UnitResult<string>.Failure("third-error");
        };

        var invalid = result.CheckAll([first, second, third]);

        Assert.True(invalid.TryGetErrors(out var errors));
        Assert.Equal(["first-error", "third-error"], errors);
        Assert.Equal(["first", "second", "third"], calls);

        var valid = result.CheckAll([second]);
        Assert.True(valid.TryGetValue(out var value));
        Assert.Same(order, value);
        Assert.Equal(["first", "second", "third", "second"], calls);
    }

    [Fact]
    public void CheckAllEmptyRulesAreValidIdentityAndRunNothing()
    {
        var order = new object();
        var result = Result<object, string>.Success(order);
        var validation = Validation<object, string>.Valid(order);

        var fromResult = result.CheckAll(
            Array.Empty<Func<object, UnitResult<string>>>());
        var fromValidation = validation.CheckAll(
            Array.Empty<Func<object, UnitResult<string>>>());

        Assert.True(fromResult.IsValid);
        Assert.True(fromResult.TryGetValue(out var resultValue));
        Assert.Same(order, resultValue);
        Assert.True(fromValidation.IsValid);
        Assert.True(fromValidation.TryGetValue(out var validationValue));
        Assert.Same(order, validationValue);
    }

    [Fact]
    public void CheckAllFailedInputDoesNotEnumerateOrInvokeRules()
    {
        var existing = new InvalidOperationException("existing");
        var result = Result<object, Exception>.Failure(existing);
        var validation = Validation<object, Exception>.Invalid(existing);
        var checks = new ProbeEnumerable<Func<object, UnitResult<Exception>>>(
            [_ => throw new InvalidOperationException("must not run")]);

        var fromResult = result.CheckAll(checks);
        var fromValidation = validation.CheckAll(checks);

        Assert.True(fromResult.TryGetErrors(out var resultErrors));
        Assert.Same(existing, Assert.Single(resultErrors));
        Assert.True(fromValidation.TryGetErrors(out var validationErrors));
        Assert.Same(existing, Assert.Single(validationErrors));
        Assert.Equal(0, checks.EnumeratorCount);
        Assert.Equal(0, checks.ItemsYielded);
    }

    [Fact]
    public void CheckAllInvalidValidationReturnsItsExistingErrorsWithoutInvokingRules()
    {
        var existing = new InvalidOperationException("existing");
        var validation = Validation<object, Exception>.InvalidMany([existing, existing]);
        var calls = 0;

        var actual = validation.CheckAll(
            [_ => { calls++; return UnitResult<Exception>.Success(); }]);

        Assert.True(actual.TryGetErrors(out var errors));
        Assert.Equal(2, errors.Count);
        Assert.Same(existing, errors[0]);
        Assert.Same(existing, errors[1]);
        Assert.Equal(0, calls);
    }

    [Fact]
    public void CheckAllSnapshotsRulesAndRejectsNullElementsBeforeInvokingAny()
    {
        var order = new object();
        var result = Result<object, string>.Success(order);
        var calls = new List<string>();
        var rules = new List<Func<object, UnitResult<string>>>();
        rules.Add(_ =>
        {
            calls.Add("first");
            rules.Add(_ => UnitResult<string>.Failure("late"));
            return UnitResult<string>.Success();
        });
        rules.Add(_ =>
        {
            calls.Add("second");
            return UnitResult<string>.Success();
        });

        var snapshot = result.CheckAll(rules);

        Assert.True(snapshot.IsValid);
        Assert.Equal(["first", "second"], calls);

        calls.Clear();
        var nullRule = new Func<object, UnitResult<string>>[]
        {
            _ => { calls.Add("should-not-run"); return UnitResult<string>.Success(); },
            null!,
        };

        Assert.Throws<ArgumentException>(() => { _ = result.CheckAll(nullRule); });
        Assert.Empty(calls);
    }

    [Fact]
    public void CheckAllFailedInputDoesNotValidateNullRuleElements()
    {
        var checks = new ProbeEnumerable<Func<object, UnitResult<string>>>([null!]);
        var result = Result<object, string>.Failure("existing");

        var validation = result.CheckAll(checks);

        Assert.True(validation.TryGetErrors(out var errors));
        Assert.Equal(["existing"], errors);
        Assert.Equal(0, checks.EnumeratorCount);
    }

    [Fact]
    public void CheckAllCallbackFaultStopsLaterRulesAndPreservesTheException()
    {
        var expected = new InvalidOperationException("check failed");
        var result = Result<int, string>.Success(1);
        var calls = new List<string>();

        var actual = Assert.Throws<InvalidOperationException>(() =>
        {
            _ = result.CheckAll(
            [
                value =>
                {
                    calls.Add("first");
                    Assert.Equal(1, value);
                    return UnitResult<string>.Failure("first-error");
                },
                _ =>
                {
                    calls.Add("second");
                    throw expected;
                },
                _ =>
                {
                    calls.Add("third");
                    return UnitResult<string>.Success();
                },
            ]);
        });

        Assert.Same(expected, actual);
        Assert.Equal(["first", "second"], calls);
    }

    [Fact]
    public void CheckAllUninitializedCheckResultsAndNullChecksAreRejected()
    {
        var result = Result<int, string>.Success(1);
        var calls = 0;

#pragma warning disable FS1001 // Intentional runtime misuse witness.
        Assert.Throws<InvalidOperationException>(() =>
        {
            _ = result.CheckAll(
            [
                _ => { calls++; return default; },
                _ => { calls++; return UnitResult<string>.Success(); },
            ]);
        });
#pragma warning restore FS1001
        Assert.Equal(1, calls);

        Assert.Throws<ArgumentNullException>(() => { _ = result.CheckAll(null!); });
#pragma warning disable FS1001 // Intentional uninitialized receiver witness.
        Result<int, string> uninitialized = default;
#pragma warning restore FS1001
        Assert.Throws<ArgumentNullException>(
            () => { _ = uninitialized.CheckAll(null!); });
        Assert.Throws<InvalidOperationException>(
            () => { _ = uninitialized.CheckAll(Array.Empty<Func<int, UnitResult<string>>>()); });
    }

    [Fact]
    public void CheckAllPreservesRuntimeNullValuesAndNullErrors()
    {
        var result = Result<object?, string?>.Success(null);
        object? observed = new object();

        var invalid = result.CheckAll(
        [
            value =>
            {
                observed = value;
                return UnitResult<string?>.Failure(null);
            },
            value =>
            {
                Assert.Null(value);
                return UnitResult<string?>.Success();
            },
        ]);
        var valid = result.CheckAll([_ => UnitResult<string?>.Success()]);

        Assert.Null(observed);
        Assert.True(invalid.TryGetErrors(out var errors));
        Assert.Null(Assert.Single(errors));
        Assert.True(valid.TryGetValue(out var value));
        Assert.Null(value);
    }

    [Fact]
    public void CheckAllOnValidationAccumulatesAfterAValidInputAndPreservesItsValue()
    {
        var order = new object();
        var validation = Validation<object, string>.Valid(order);

        var actual = validation.CheckAll(
        [
            _ => UnitResult<string>.Failure("first"),
            _ => UnitResult<string>.Success(),
            _ => UnitResult<string>.Failure("third"),
        ]);

        Assert.True(actual.TryGetErrors(out var errors));
        Assert.Equal(["first", "third"], errors);
    }

    [Fact]
    public void CheckAllDuplicateRulesAreInvokedAndAccumulatedPerPosition()
    {
        var calls = 0;
        var result = Result<int, string>.Success(1);
        Func<int, UnitResult<string>> check = _ =>
        {
            calls++;
            return UnitResult<string>.Failure("duplicate");
        };

        var validation = result.CheckAll([check, check]);

        Assert.True(validation.TryGetErrors(out var errors));
        Assert.Equal(["duplicate", "duplicate"], errors);
        Assert.Equal(2, calls);
    }
    [Fact]
    public void CheckAllEnumeratesTheRuleSourceOnceAndInvokesEachSnapshotRuleOnce()
    {
        var calls = 0;
        var checks = new ProbeEnumerable<Func<int, UnitResult<string>>>(
        [
            _ =>
            {
                calls++;
                return UnitResult<string>.Success();
            },
            _ =>
            {
                calls++;
                return UnitResult<string>.Failure("second");
            },
        ]);

        var validation = Result<int, string>.Success(1).CheckAll(checks);

        Assert.True(validation.TryGetErrors(out var errors));
        Assert.Equal(["second"], errors);
        Assert.Equal(1, checks.EnumeratorCount);
        Assert.Equal(2, checks.ItemsYielded);
        Assert.Equal(2, calls);
    }

    [Fact]
    public void ValidationDoesNotDeclareBindOrSelectMany()
    {
        var forbidden = typeof(Validation<,>)
            .GetMethods(
                BindingFlags.Public
                | BindingFlags.Instance
                | BindingFlags.Static
                | BindingFlags.DeclaredOnly)
            .Where(static method => method.Name is "Bind" or "SelectMany")
            .ToArray();

        Assert.Empty(forbidden);
    }
}
