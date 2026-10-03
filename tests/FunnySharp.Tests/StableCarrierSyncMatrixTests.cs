namespace FunnySharp.Tests;

public sealed class StableCarrierSyncMatrixTests
{
    [Fact]
    public void Identity292NULLPAYLOADPreservesTheExactRuntimeBehavior()
    {
        var result = Option.None<string>().ToResult<string, string?>(() => null);
        Assert.True(result.TryGetError(out var error));
        Assert.Null(error);
    }

    [Fact]
    public void Identity293NULLPAYLOADPreservesTheExactRuntimeBehavior()
    {
        var result = Option.None<string>().ToResult<string, string?>((string?)null);
        Assert.True(result.TryGetError(out var error));
        Assert.Null(error);
    }

    [Fact]
    public void Identity318NULLPAYLOADPreservesTheExactRuntimeBehavior()
    {
        var result = Result<string?, string?>.Success(null).SelectMany(_ => Result<string?, string?>.Success(null), (first, second) => { Assert.Null(first); Assert.Null(second); return (string?)null; });
        Assert.True(result.TryGetValue(out var value));
        Assert.Null(value);
    }

    [Fact]
    public void Identity322NULLPAYLOADPreservesTheExactRuntimeBehavior()
    {
        var result = Result<string?, string?>.Success(null).Map(value => { Assert.Null(value); return (string?)null; });
        Assert.True(result.TryGetValue(out var value));
        Assert.Null(value);
    }

    [Fact]
    public void Identity323NULLPAYLOADPreservesTheExactRuntimeBehavior()
    {
        var result = Result<string?, string?>.Success(null).Select(value => { Assert.Null(value); return (string?)null; });
        Assert.True(result.TryGetValue(out var value));
        Assert.Null(value);
    }

    [Fact]
    public void Identity324NULLPAYLOADPreservesTheExactRuntimeBehavior()
    {
        var result = Result<string?, string?>.Success(null).Ensure(value => { Assert.Null(value); return false; }, value => { Assert.Null(value); return (string?)null; });
        Assert.True(result.TryGetError(out var error));
        Assert.Null(error);
    }

    [Fact]
    public void Identity325NULLPAYLOADPreservesTheExactRuntimeBehavior()
    {
        var result = Result<string?, string?>.Success(null).Ensure(value => { Assert.Null(value); return false; }, (string?)null);
        Assert.True(result.TryGetError(out var error));
        Assert.Null(error);
    }

    [Fact]
    public void Identity326NULLPAYLOADPreservesTheExactRuntimeBehavior()
    {
        var result = Result<string?, string?>.Failure(null).Recover(error => { Assert.Null(error); return (string?)null; });
        Assert.True(result.TryGetValue(out var value));
        Assert.Null(value);
    }

    [Fact]
    public void Identity328NULLPAYLOADPreservesTheExactRuntimeBehavior()
    {
        var result = Result<string?, string?>.Failure(null).MapError(error => { Assert.Null(error); return (string?)null; });
        Assert.True(result.TryGetError(out var error));
        Assert.Null(error);
    }

    [Fact]
    public void Identity447NULLPAYLOADPreservesTheExactRuntimeBehavior()
    {
        var result = UnitResult.Failure<string?>(null);
        Assert.True(result.TryGetError(out var error));
        Assert.Null(error);
    }

    [Fact]
    public void Identity448NULLPAYLOADPreservesTheExactRuntimeBehavior()
    {
        var result = UnitResult.Try<string?>(() => throw new InvalidOperationException("operation"), _ => null);
        Assert.True(result.TryGetError(out var error));
        Assert.Null(error);
    }

    [Fact]
    public void Identity454NULLPAYLOADPreservesTheExactRuntimeBehavior()
    {
        var result = Option.None<string>().ToUnitResult<string, string?>(() => null);
        Assert.True(result.TryGetError(out var error));
        Assert.Null(error);
    }

    [Fact]
    public void Identity455NULLPAYLOADPreservesTheExactRuntimeBehavior()
    {
        var result = Option.None<string>().ToUnitResult<string, string?>((string?)null);
        Assert.True(result.TryGetError(out var error));
        Assert.Null(error);
    }

    [Fact]
    public void Identity456NULLPAYLOADPreservesTheExactRuntimeBehavior()
    {
        var result = Result<string?, string?>.Failure(null).ToUnitResult();
        Assert.True(result.TryGetError(out var error));
        Assert.Null(error);
    }

    [Fact]
    public void Identity480NULLPAYLOADPreservesTheExactRuntimeBehavior()
    {
        var result = UnitResult<string?>.Success().ToResult<string?>(() => null);
        Assert.True(result.TryGetValue(out var value));
        Assert.Null(value);
    }

    [Fact]
    public void Identity482NULLPAYLOADPreservesTheExactRuntimeBehavior()
    {
        var result = UnitResult<string?>.Success().Ensure(() => false, () => (string?)null);
        Assert.True(result.TryGetError(out var error));
        Assert.Null(error);
    }

    [Fact]
    public void Identity483NULLPAYLOADPreservesTheExactRuntimeBehavior()
    {
        var result = UnitResult<string?>.Success().Ensure(() => false, (string?)null);
        Assert.True(result.TryGetError(out var error));
        Assert.Null(error);
    }

    [Fact]
    public void Identity487NULLPAYLOADPreservesTheExactRuntimeBehavior()
    {
        var result = UnitResult<string?>.Failure(null).MapError(_ => (string?)null);
        Assert.True(result.TryGetError(out var error));
        Assert.Null(error);
    }

    [Fact]
    public void Identity497NULLPAYLOADPreservesTheExactRuntimeBehavior()
    {
        var calls = 0;
        UnitResult<string?>.Failure(null).Match(() => throw new InvalidOperationException("inactive"), error => { Assert.Null(error); calls++; });
        Assert.Equal(1, calls);
    }

    [Fact]
    public void Identity498NULLPAYLOADPreservesTheExactRuntimeBehavior()
    {
        Assert.Null(UnitResult<string?>.Failure(null).Match<string?>(() => throw new InvalidOperationException("inactive"), error => { Assert.Null(error); return null; }));
        Assert.Null(UnitResult<string?>.Success().Match<string?>(() => null, _ => throw new InvalidOperationException("inactive")));
    }

    [Fact]
    public void Identity502NULLPAYLOADPreservesTheExactRuntimeBehavior()
    {
        var result = Validation<Func<string?, string?>, string?>.Valid(value => { Assert.Null(value); return null; }).Apply(Validation<string?, string?>.Valid(null));
        Assert.True(result.TryGetValue(out var value));
        Assert.Null(value);
    }

    [Fact]
    public void Identity512NULLPAYLOADPreservesTheExactRuntimeBehavior()
    {
        var result = Validation<string?, string?>.Valid(null).Map(value => { Assert.Null(value); return (string?)null; });
        Assert.True(result.TryGetValue(out var value));
        Assert.Null(value);
    }

    [Fact]
    public void Identity513NULLPAYLOADPreservesTheExactRuntimeBehavior()
    {
        var result = Validation<string?, string?>.InvalidMany(new string?[] { null, "second", null }).MapErrors(error => (string?)null);
        Assert.True(result.TryGetErrors(out var errors));
        Assert.Equal(new string?[] { null, null, null }, errors);
    }

    [Fact]
    public void Identity319NULLPAYLOADPreservesTheExactRuntimeBehavior()
    {
        var input = Result<string?, string?>.Success(null);
        var result = input.Zip(input, (v0, v1) => { Assert.Null(v0); Assert.Null(v1); return (string?)null; });
        Assert.True(result.TryGetValue(out var value));
        Assert.Null(value);
    }

    [Fact]
    public void Identity321NULLPAYLOADPreservesTheExactRuntimeBehavior()
    {
        var input = Result<string?, string?>.Success(null);
        var result = input.Zip(input, input, (v0, v1, v2) => { Assert.Null(v0); Assert.Null(v1); Assert.Null(v2); return (string?)null; });
        Assert.True(result.TryGetValue(out var value));
        Assert.Null(value);
    }

    [Fact]
    public void Identity320NULLPAYLOADPreservesTheExactRuntimeBehavior()
    {
        var input = Result<string?, string?>.Success(null);
        var result = input.Zip(input, input, input, (v0, v1, v2, v3) => { Assert.Null(v0); Assert.Null(v1); Assert.Null(v2); Assert.Null(v3); return (string?)null; });
        Assert.True(result.TryGetValue(out var value));
        Assert.Null(value);
    }

    [Fact]
    public void Identity509NULLPAYLOADPreservesTheExactRuntimeBehavior()
    {
        var input = Validation<string?, string?>.Valid(null);
        var result = input.Zip(input, (v0, v1) => { Assert.Null(v0); Assert.Null(v1); return (string?)null; });
        Assert.True(result.TryGetValue(out var value));
        Assert.Null(value);
    }

    [Fact]
    public void Identity511NULLPAYLOADPreservesTheExactRuntimeBehavior()
    {
        var input = Validation<string?, string?>.Valid(null);
        var result = input.Zip(input, input, (v0, v1, v2) => { Assert.Null(v0); Assert.Null(v1); Assert.Null(v2); return (string?)null; });
        Assert.True(result.TryGetValue(out var value));
        Assert.Null(value);
    }

    [Fact]
    public void Identity510NULLPAYLOADPreservesTheExactRuntimeBehavior()
    {
        var input = Validation<string?, string?>.Valid(null);
        var result = input.Zip(input, input, input, (v0, v1, v2, v3) => { Assert.Null(v0); Assert.Null(v1); Assert.Null(v2); Assert.Null(v3); return (string?)null; });
        Assert.True(result.TryGetValue(out var value));
        Assert.Null(value);
    }

    [Fact]
    public void Identity508NULLPAYLOADPreservesTheExactRuntimeBehavior()
    {
        var result = Validation<string?, string?>.Valid(null).Zip(Validation<string?, string?>.Valid(null));
        Assert.True(result.TryGetValue(out var pair));
        Assert.Null(pair.First);
        Assert.Null(pair.Second);
        var invalid = Validation<string?, string?>.InvalidMany(new string?[] { null, "second" }).Zip(Validation<string?, string?>.Invalid((string?)null));
        Assert.True(invalid.TryGetErrors(out var errors));
        Assert.Equal(new string?[] { null, "second", null }, errors);
    }

    [Fact]
    public void Identity317BINDERDEFAULTPreservesTheExactRuntimeBehavior()
    {
#pragma warning disable FS1001 // Deliberately return an uninitialized carrier to distinguish acceptance from observation.
        var returned = Result<int, string>.Success(1).Bind<int>(_ => default);
#pragma warning restore FS1001
        Assert.Equal("Uninitialized", returned.ToString());
        Assert.Throws<InvalidOperationException>(() => returned.IsSuccess);
    }

    [Fact]
    public void Identity327BINDERDEFAULTPreservesTheExactRuntimeBehavior()
    {
#pragma warning disable FS1001 // Deliberately return an uninitialized carrier to distinguish acceptance from observation.
        var returned = Result<int, string>.Failure("bad").RecoverWith(_ => default);
#pragma warning restore FS1001
        Assert.Equal("Uninitialized", returned.ToString());
        Assert.Throws<InvalidOperationException>(() => returned.IsSuccess);
    }

    [Fact]
    public void Identity481BINDERDEFAULTPreservesTheExactRuntimeBehavior()
    {
#pragma warning disable FS1001 // Deliberately return an uninitialized carrier to distinguish acceptance from observation.
        var returned = UnitResult<string>.Success().Bind(() => default);
#pragma warning restore FS1001
        Assert.Equal("Uninitialized", returned.ToString());
        Assert.Throws<InvalidOperationException>(() => returned.IsSuccess);
    }

    [Fact]
    public void Identity484BINDERDEFAULTPreservesTheExactRuntimeBehavior()
    {
#pragma warning disable FS1001 // Deliberately return an uninitialized carrier to distinguish acceptance from observation.
        var returned = UnitResult<string>.Failure("bad").RecoverWith(_ => default);
#pragma warning restore FS1001
        Assert.Equal("Uninitialized", returned.ToString());
        Assert.Throws<InvalidOperationException>(() => returned.IsSuccess);
    }

    [Fact]
    public void Identity316BINDERDEFAULTPreservesTheExactRuntimeBehavior()
    {
#pragma warning disable FS1001 // Deliberately inspect the carrier returned by a binder/factory.
        Assert.Throws<InvalidOperationException>(() => Result<int, string>.Success(1).ZipWith<int>(() => default));
#pragma warning restore FS1001
    }

    [Fact]
    public void Identity318BINDERDEFAULTPreservesTheExactRuntimeBehavior()
    {
#pragma warning disable FS1001 // Deliberately inspect the carrier returned by a binder/factory.
        Assert.Throws<InvalidOperationException>(() => Result<int, string>.Success(1).SelectMany<int, int>(_ => default, (x, y) => x + y));
#pragma warning restore FS1001
    }

    [Fact]
    public void Identity486BINDERDEFAULTPreservesTheExactRuntimeBehavior()
    {
#pragma warning disable FS1001 // Deliberately inspect the carrier returned by a binder/factory.
        Assert.Throws<InvalidOperationException>(() => UnitResult<string>.Success().ZipWith(() => default));
#pragma warning restore FS1001
    }

    [Fact]
    public void Identity164CALLBACKEXCEPTIONPreservesTheExactRuntimeBehavior()
    {
        var expected = new InvalidOperationException("callback");
        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() => Option.FromBoolean<int>(true, () => throw expected)));
        Assert.True(Option.FromBoolean<int>(false, () => throw expected).IsNone);
        var cancellation = new OperationCanceledException(new CancellationToken(true));
        Assert.Same(cancellation, Assert.Throws<OperationCanceledException>(() => Option.FromBoolean<int>(true, () => throw cancellation)));
    }

    [Fact]
    public void Identity292CALLBACKEXCEPTIONPreservesTheExactRuntimeBehavior()
    {
        var expected = new InvalidOperationException("callback");
        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() => Option.None<int>().ToResult<int, string>(() => throw expected)));
        Assert.True(Option.Some(1).ToResult<int, string>(() => throw expected).IsSuccess);
        var cancellation = new OperationCanceledException(new CancellationToken(true));
        Assert.Same(cancellation, Assert.Throws<OperationCanceledException>(() => Option.None<int>().ToResult<int, string>(() => throw cancellation)));
    }

    [Fact]
    public void Identity316CALLBACKEXCEPTIONPreservesTheExactRuntimeBehavior()
    {
        var expected = new InvalidOperationException("callback");
        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() => Result<int, string>.Success(1).ZipWith<int>(() => throw expected)));
        Assert.True(Result<int, string>.Failure("bad").ZipWith<int>(() => throw expected).IsFailure);
        var cancellation = new OperationCanceledException(new CancellationToken(true));
        Assert.Same(cancellation, Assert.Throws<OperationCanceledException>(() => Result<int, string>.Success(1).ZipWith<int>(() => throw cancellation)));
    }

    [Fact]
    public void Identity318CALLBACKEXCEPTIONPreservesTheExactRuntimeBehavior()
    {
        var expected = new InvalidOperationException("callback");
        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() => Result<int, string>.Success(1).SelectMany<int, int>(_ => throw expected, (x, y) => x + y)));
        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() => Result<int, string>.Success(1).SelectMany<int, int>(_ => Result<int, string>.Success(2), (x, y) => throw expected)));
        var cancellation = new OperationCanceledException(new CancellationToken(true));
        Assert.Same(cancellation, Assert.Throws<OperationCanceledException>(() => Result<int, string>.Success(1).SelectMany<int, int>(_ => throw cancellation, (x, y) => x + y)));
    }

    [Fact]
    public void Identity323CALLBACKEXCEPTIONPreservesTheExactRuntimeBehavior()
    {
        var expected = new InvalidOperationException("callback");
        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() => Result<int, string>.Success(1).Select<int>(_ => throw expected)));
        Assert.True(Result<int, string>.Failure("bad").Select<int>(_ => throw expected).IsFailure);
        var cancellation = new OperationCanceledException(new CancellationToken(true));
        Assert.Same(cancellation, Assert.Throws<OperationCanceledException>(() => Result<int, string>.Success(1).Select<int>(_ => throw cancellation)));
    }

    [Fact]
    public void Identity324CALLBACKEXCEPTIONPreservesTheExactRuntimeBehavior()
    {
        var expected = new InvalidOperationException("callback");
        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() => Result<int, string>.Success(1).Ensure(_ => false, _ => throw expected)));
        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() => Result<int, string>.Success(1).Ensure(_ => throw expected, _ => "bad")));
        var cancellation = new OperationCanceledException(new CancellationToken(true));
        Assert.Same(cancellation, Assert.Throws<OperationCanceledException>(() => Result<int, string>.Success(1).Ensure(_ => false, _ => throw cancellation)));
    }

    [Fact]
    public void Identity325CALLBACKEXCEPTIONPreservesTheExactRuntimeBehavior()
    {
        var expected = new InvalidOperationException("callback");
        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() => Result<int, string>.Success(1).Ensure(_ => throw expected, "bad")));
        Assert.True(Result<int, string>.Failure("bad").Ensure(_ => throw expected, "new").IsFailure);
        var cancellation = new OperationCanceledException(new CancellationToken(true));
        Assert.Same(cancellation, Assert.Throws<OperationCanceledException>(() => Result<int, string>.Success(1).Ensure(_ => throw cancellation, "bad")));
    }

    [Fact]
    public void Identity326CALLBACKEXCEPTIONPreservesTheExactRuntimeBehavior()
    {
        var expected = new InvalidOperationException("callback");
        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() => Result<int, string>.Failure("bad").Recover(_ => throw expected)));
        Assert.True(Result<int, string>.Success(1).Recover(_ => throw expected).IsSuccess);
        var cancellation = new OperationCanceledException(new CancellationToken(true));
        Assert.Same(cancellation, Assert.Throws<OperationCanceledException>(() => Result<int, string>.Failure("bad").Recover(_ => throw cancellation)));
    }

    [Fact]
    public void Identity327CALLBACKEXCEPTIONPreservesTheExactRuntimeBehavior()
    {
        var expected = new InvalidOperationException("callback");
        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() => Result<int, string>.Failure("bad").RecoverWith(_ => throw expected)));
        Assert.True(Result<int, string>.Success(1).RecoverWith(_ => throw expected).IsSuccess);
        var cancellation = new OperationCanceledException(new CancellationToken(true));
        Assert.Same(cancellation, Assert.Throws<OperationCanceledException>(() => Result<int, string>.Failure("bad").RecoverWith(_ => throw cancellation)));
    }

    [Fact]
    public void Identity454CALLBACKEXCEPTIONPreservesTheExactRuntimeBehavior()
    {
        var expected = new InvalidOperationException("callback");
        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() => Option.None<int>().ToUnitResult<int, string>(() => throw expected)));
        Assert.True(Option.Some(1).ToUnitResult<int, string>(() => throw expected).IsSuccess);
        var cancellation = new OperationCanceledException(new CancellationToken(true));
        Assert.Same(cancellation, Assert.Throws<OperationCanceledException>(() => Option.None<int>().ToUnitResult<int, string>(() => throw cancellation)));
    }

    [Fact]
    public void Identity339NULLPAYLOADPreservesTheExactRuntimeBehavior()
    {
        var calls = 0;
        Result<string?, string?>.Success(null).Match(value => { Assert.Null(value); calls++; }, _ => throw new InvalidOperationException("inactive"));
        Result<string?, string?>.Failure(null).Match(_ => throw new InvalidOperationException("inactive"), error => { Assert.Null(error); calls++; });
        Assert.Equal(2, calls);
    }

    [Fact]
    public void Identity339CALLBACKEXCEPTIONPreservesTheExactRuntimeBehavior()
    {
        var expected = new InvalidOperationException("branch");
        var cancellation = new OperationCanceledException(new CancellationToken(true));
        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() => Result<string?, string?>.Success(null).Match(_ => throw expected, _ => throw new Exception("inactive"))));
        Assert.Same(cancellation, Assert.Throws<OperationCanceledException>(() => Result<string?, string?>.Success(null).Match(_ => throw cancellation, _ => throw new Exception("inactive"))));
        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() => Result<string?, string?>.Failure(null).Match(_ => throw new Exception("inactive"), _ => throw expected)));
        Assert.Same(cancellation, Assert.Throws<OperationCanceledException>(() => Result<string?, string?>.Failure(null).Match(_ => throw new Exception("inactive"), _ => throw cancellation)));
    }

    [Fact]
    public void Identity340NULLPAYLOADPreservesTheExactRuntimeBehavior()
    {
        Assert.Null(Result<string?, string?>.Success(null).Match<string?>(value => { Assert.Null(value); return null; }, _ => throw new InvalidOperationException("inactive")));
        Assert.Null(Result<string?, string?>.Failure(null).Match<string?>(_ => throw new InvalidOperationException("inactive"), error => { Assert.Null(error); return null; }));
    }

    [Fact]
    public void Identity340CALLBACKEXCEPTIONPreservesTheExactRuntimeBehavior()
    {
        var expected = new InvalidOperationException("branch");
        var cancellation = new OperationCanceledException(new CancellationToken(true));
        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() => Result<string?, string?>.Success(null).Match<int>(_ => throw expected, _ => throw new Exception("inactive"))));
        Assert.Same(cancellation, Assert.Throws<OperationCanceledException>(() => Result<string?, string?>.Success(null).Match<int>(_ => throw cancellation, _ => throw new Exception("inactive"))));
        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() => Result<string?, string?>.Failure(null).Match<int>(_ => throw new Exception("inactive"), _ => throw expected)));
        Assert.Same(cancellation, Assert.Throws<OperationCanceledException>(() => Result<string?, string?>.Failure(null).Match<int>(_ => throw new Exception("inactive"), _ => throw cancellation)));
    }

    [Fact]
    public void Identity525NULLPAYLOADPreservesTheExactRuntimeBehavior()
    {
        var calls = 0;
        Validation<string?, string?>.Valid(null).Match(value => { Assert.Null(value); calls++; }, _ => throw new InvalidOperationException("inactive"));
        Validation<string?, string?>.Invalid((string?)null).Match(_ => throw new InvalidOperationException("inactive"), errors => { Assert.Equal(new string?[] { null }, errors); calls++; });
        Assert.Equal(2, calls);
    }

    [Fact]
    public void Identity525CALLBACKEXCEPTIONPreservesTheExactRuntimeBehavior()
    {
        var expected = new InvalidOperationException("branch");
        var cancellation = new OperationCanceledException(new CancellationToken(true));
        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() => Validation<string?, string?>.Valid(null).Match(_ => throw expected, _ => throw new Exception("inactive"))));
        Assert.Same(cancellation, Assert.Throws<OperationCanceledException>(() => Validation<string?, string?>.Valid(null).Match(_ => throw cancellation, _ => throw new Exception("inactive"))));
        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() => Validation<string?, string?>.Invalid((string?)null).Match(_ => throw new Exception("inactive"), _ => throw expected)));
        Assert.Same(cancellation, Assert.Throws<OperationCanceledException>(() => Validation<string?, string?>.Invalid((string?)null).Match(_ => throw new Exception("inactive"), _ => throw cancellation)));
    }

    [Fact]
    public void Identity526NULLPAYLOADPreservesTheExactRuntimeBehavior()
    {
        Assert.Null(Validation<string?, string?>.Valid(null).Match<string?>(value => { Assert.Null(value); return null; }, _ => throw new InvalidOperationException("inactive")));
        Assert.Null(Validation<string?, string?>.Invalid((string?)null).Match<string?>(_ => throw new InvalidOperationException("inactive"), errors => { Assert.Equal(new string?[] { null }, errors); return null; }));
    }

    [Fact]
    public void Identity526CALLBACKEXCEPTIONPreservesTheExactRuntimeBehavior()
    {
        var expected = new InvalidOperationException("branch");
        var cancellation = new OperationCanceledException(new CancellationToken(true));
        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() => Validation<string?, string?>.Valid(null).Match<int>(_ => throw expected, _ => throw new Exception("inactive"))));
        Assert.Same(cancellation, Assert.Throws<OperationCanceledException>(() => Validation<string?, string?>.Valid(null).Match<int>(_ => throw cancellation, _ => throw new Exception("inactive"))));
        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() => Validation<string?, string?>.Invalid((string?)null).Match<int>(_ => throw new Exception("inactive"), _ => throw expected)));
        Assert.Same(cancellation, Assert.Throws<OperationCanceledException>(() => Validation<string?, string?>.Invalid((string?)null).Match<int>(_ => throw new Exception("inactive"), _ => throw cancellation)));
    }

    [Fact]
    public void Identity525ACTIONMATCHPreservesTheExactRuntimeBehavior()
    {
        var valid = Validation<int, string>.Valid(1);
        var invalid = Validation<int, string>.Invalid("bad");
        var calls = 0;
        valid.Match(value => { Assert.Equal(1, value); calls++; }, _ => throw new Exception("inactive"));
        invalid.Match(_ => throw new Exception("inactive"), errors => { Assert.Equal(new[] { "bad" }, errors); calls++; });
        Assert.Equal(2, calls);
        Assert.Equal("valid", Assert.Throws<ArgumentNullException>(() => invalid.Match((Action<int>)null!, _ => { })).ParamName);
        Assert.Equal("invalid", Assert.Throws<ArgumentNullException>(() => valid.Match(_ => { }, (Action<IReadOnlyList<string>>)null!)).ParamName);
    }

    [Fact]
    public void Identity502APPLYNULLFUNCTIONPreservesTheExactRuntimeBehavior()
    {
        var function = Validation<Func<int, int>, string>.Valid(null!);
        Assert.Throws<NullReferenceException>(() => function.Apply(Validation<int, string>.Valid(1)));
        var skipped = function.Apply(Validation<int, string>.Invalid("argument"));
        Assert.True(skipped.TryGetErrors(out var errors));
        Assert.Equal(new[] { "argument" }, errors);
#pragma warning disable FS1001 // Deliberate default operands distinguish initialization checks from function invocation.
        Assert.Throws<InvalidOperationException>(() => function.Apply(default(Validation<int, string>)));
        Assert.Throws<InvalidOperationException>(() => default(Validation<Func<int, int>, string>).Apply(Validation<int, string>.Valid(1)));
#pragma warning restore FS1001
    }
}

