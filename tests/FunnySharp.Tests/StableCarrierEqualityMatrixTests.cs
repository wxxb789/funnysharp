namespace FunnySharp.Tests;

public sealed class StableCarrierEqualityMatrixTests
{
    [Fact]
    public void Identity331COMPARERNULLPreservesNullOrUserExceptionIdentity()
    {
        var null0 = Result<Payload?, Payload?>.Success(null);
        Assert.True(null0 == Result<Payload?, Payload?>.Success(null));
        var null1 = Result<Payload?, Payload?>.Failure(null);
        Assert.True(null1 == Result<Payload?, Payload?>.Failure(null));
        var expected = new InvalidOperationException("payload callback");
        var payload = new Payload(expected);
        var active0 = Result<Payload?, Payload?>.Success(payload);
        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() => active0 == Result<Payload?, Payload?>.Success(payload)));
        var active1 = Result<Payload?, Payload?>.Failure(payload);
        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() => active1 == Result<Payload?, Payload?>.Failure(payload)));
    }

    [Fact]
    public void Identity332COMPARERNULLPreservesNullOrUserExceptionIdentity()
    {
        var null0 = Result<Payload?, Payload?>.Success(null);
        Assert.False(null0 != Result<Payload?, Payload?>.Success(null));
        var null1 = Result<Payload?, Payload?>.Failure(null);
        Assert.False(null1 != Result<Payload?, Payload?>.Failure(null));
        var expected = new InvalidOperationException("payload callback");
        var payload = new Payload(expected);
        var active0 = Result<Payload?, Payload?>.Success(payload);
        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() => active0 != Result<Payload?, Payload?>.Success(payload)));
        var active1 = Result<Payload?, Payload?>.Failure(payload);
        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() => active1 != Result<Payload?, Payload?>.Failure(payload)));
    }

    [Fact]
    public void Identity333COMPARERNULLPreservesNullOrUserExceptionIdentity()
    {
        var null0 = Result<Payload?, Payload?>.Success(null);
        Assert.True(null0.Equals(Result<Payload?, Payload?>.Success(null)));
        var null1 = Result<Payload?, Payload?>.Failure(null);
        Assert.True(null1.Equals(Result<Payload?, Payload?>.Failure(null)));
        var expected = new InvalidOperationException("payload callback");
        var payload = new Payload(expected);
        var active0 = Result<Payload?, Payload?>.Success(payload);
        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() => active0.Equals(Result<Payload?, Payload?>.Success(payload))));
        var active1 = Result<Payload?, Payload?>.Failure(payload);
        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() => active1.Equals(Result<Payload?, Payload?>.Failure(payload))));
    }

    [Fact]
    public void Identity334COMPARERNULLPreservesNullOrUserExceptionIdentity()
    {
        var null0 = Result<Payload?, Payload?>.Success(null);
        Assert.True(null0.Equals((object)Result<Payload?, Payload?>.Success(null)));
        var null1 = Result<Payload?, Payload?>.Failure(null);
        Assert.True(null1.Equals((object)Result<Payload?, Payload?>.Failure(null)));
        var expected = new InvalidOperationException("payload callback");
        var payload = new Payload(expected);
        var active0 = Result<Payload?, Payload?>.Success(payload);
        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() => active0.Equals((object)Result<Payload?, Payload?>.Success(payload))));
        var active1 = Result<Payload?, Payload?>.Failure(payload);
        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() => active1.Equals((object)Result<Payload?, Payload?>.Failure(payload))));
    }

    [Fact]
    public void Identity337COMPARERNULLPreservesNullOrUserExceptionIdentity()
    {
        var null0 = Result<Payload?, Payload?>.Success(null);
        Assert.Equal(Result<Payload?, Payload?>.Success(null).GetHashCode(), null0.GetHashCode());
        var null1 = Result<Payload?, Payload?>.Failure(null);
        Assert.Equal(Result<Payload?, Payload?>.Failure(null).GetHashCode(), null1.GetHashCode());
        var expected = new InvalidOperationException("payload callback");
        var payload = new Payload(expected);
        var active0 = Result<Payload?, Payload?>.Success(payload);
        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() => active0.GetHashCode()));
        var active1 = Result<Payload?, Payload?>.Failure(payload);
        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() => active1.GetHashCode()));
    }

    [Fact]
    public void Identity338COMPARERNULLPreservesNullOrUserExceptionIdentity()
    {
        var null0 = Result<Payload?, Payload?>.Success(null);
        Assert.Equal("Success()", null0.ToString());
        var null1 = Result<Payload?, Payload?>.Failure(null);
        Assert.Equal("Failure()", null1.ToString());
        var expected = new InvalidOperationException("payload callback");
        var payload = new Payload(expected);
        var active0 = Result<Payload?, Payload?>.Success(payload);
        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() => active0.ToString()));
        var active1 = Result<Payload?, Payload?>.Failure(payload);
        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() => active1.ToString()));
    }

    [Fact]
    public void Identity490COMPARERNULLPreservesNullOrUserExceptionIdentity()
    {
        var null0 = UnitResult<Payload?>.Failure(null);
        Assert.True(null0 == UnitResult<Payload?>.Failure(null));
        var expected = new InvalidOperationException("payload callback");
        var payload = new Payload(expected);
        var active0 = UnitResult<Payload?>.Failure(payload);
        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() => active0 == UnitResult<Payload?>.Failure(payload)));
    }

    [Fact]
    public void Identity491COMPARERNULLPreservesNullOrUserExceptionIdentity()
    {
        var null0 = UnitResult<Payload?>.Failure(null);
        Assert.False(null0 != UnitResult<Payload?>.Failure(null));
        var expected = new InvalidOperationException("payload callback");
        var payload = new Payload(expected);
        var active0 = UnitResult<Payload?>.Failure(payload);
        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() => active0 != UnitResult<Payload?>.Failure(payload)));
    }

    [Fact]
    public void Identity492COMPARERNULLPreservesNullOrUserExceptionIdentity()
    {
        var null0 = UnitResult<Payload?>.Failure(null);
        Assert.True(null0.Equals(UnitResult<Payload?>.Failure(null)));
        var expected = new InvalidOperationException("payload callback");
        var payload = new Payload(expected);
        var active0 = UnitResult<Payload?>.Failure(payload);
        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() => active0.Equals(UnitResult<Payload?>.Failure(payload))));
    }

    [Fact]
    public void Identity493COMPARERNULLPreservesNullOrUserExceptionIdentity()
    {
        var null0 = UnitResult<Payload?>.Failure(null);
        Assert.True(null0.Equals((object)UnitResult<Payload?>.Failure(null)));
        var expected = new InvalidOperationException("payload callback");
        var payload = new Payload(expected);
        var active0 = UnitResult<Payload?>.Failure(payload);
        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() => active0.Equals((object)UnitResult<Payload?>.Failure(payload))));
    }

    [Fact]
    public void Identity495COMPARERNULLPreservesNullOrUserExceptionIdentity()
    {
        var null0 = UnitResult<Payload?>.Failure(null);
        Assert.Equal(UnitResult<Payload?>.Failure(null).GetHashCode(), null0.GetHashCode());
        var expected = new InvalidOperationException("payload callback");
        var payload = new Payload(expected);
        var active0 = UnitResult<Payload?>.Failure(payload);
        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() => active0.GetHashCode()));
    }

    [Fact]
    public void Identity496COMPARERNULLPreservesNullOrUserExceptionIdentity()
    {
        var null0 = UnitResult<Payload?>.Failure(null);
        Assert.Equal("Failure()", null0.ToString());
        var expected = new InvalidOperationException("payload callback");
        var payload = new Payload(expected);
        var active0 = UnitResult<Payload?>.Failure(payload);
        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() => active0.ToString()));
    }

    [Fact]
    public void Identity517COMPARERNULLPreservesNullOrUserExceptionIdentity()
    {
        var null0 = Validation<Payload?, Payload?>.Valid(null);
        Assert.True(null0 == Validation<Payload?, Payload?>.Valid(null));
        var null1 = Validation<Payload?, Payload?>.Invalid((Payload?)null);
        Assert.True(null1 == Validation<Payload?, Payload?>.Invalid((Payload?)null));
        var expected = new InvalidOperationException("payload callback");
        var payload = new Payload(expected);
        var active0 = Validation<Payload?, Payload?>.Valid(payload);
        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() => active0 == Validation<Payload?, Payload?>.Valid(payload)));
        var active1 = Validation<Payload?, Payload?>.Invalid(payload);
        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() => active1 == Validation<Payload?, Payload?>.Invalid(payload)));
    }

    [Fact]
    public void Identity518COMPARERNULLPreservesNullOrUserExceptionIdentity()
    {
        var null0 = Validation<Payload?, Payload?>.Valid(null);
        Assert.False(null0 != Validation<Payload?, Payload?>.Valid(null));
        var null1 = Validation<Payload?, Payload?>.Invalid((Payload?)null);
        Assert.False(null1 != Validation<Payload?, Payload?>.Invalid((Payload?)null));
        var expected = new InvalidOperationException("payload callback");
        var payload = new Payload(expected);
        var active0 = Validation<Payload?, Payload?>.Valid(payload);
        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() => active0 != Validation<Payload?, Payload?>.Valid(payload)));
        var active1 = Validation<Payload?, Payload?>.Invalid(payload);
        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() => active1 != Validation<Payload?, Payload?>.Invalid(payload)));
    }

    [Fact]
    public void Identity519COMPARERNULLPreservesNullOrUserExceptionIdentity()
    {
        var null0 = Validation<Payload?, Payload?>.Valid(null);
        Assert.True(null0.Equals(Validation<Payload?, Payload?>.Valid(null)));
        var null1 = Validation<Payload?, Payload?>.Invalid((Payload?)null);
        Assert.True(null1.Equals(Validation<Payload?, Payload?>.Invalid((Payload?)null)));
        var expected = new InvalidOperationException("payload callback");
        var payload = new Payload(expected);
        var active0 = Validation<Payload?, Payload?>.Valid(payload);
        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() => active0.Equals(Validation<Payload?, Payload?>.Valid(payload))));
        var active1 = Validation<Payload?, Payload?>.Invalid(payload);
        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() => active1.Equals(Validation<Payload?, Payload?>.Invalid(payload))));
    }

    [Fact]
    public void Identity520COMPARERNULLPreservesNullOrUserExceptionIdentity()
    {
        var null0 = Validation<Payload?, Payload?>.Valid(null);
        Assert.True(null0.Equals((object)Validation<Payload?, Payload?>.Valid(null)));
        var null1 = Validation<Payload?, Payload?>.Invalid((Payload?)null);
        Assert.True(null1.Equals((object)Validation<Payload?, Payload?>.Invalid((Payload?)null)));
        var expected = new InvalidOperationException("payload callback");
        var payload = new Payload(expected);
        var active0 = Validation<Payload?, Payload?>.Valid(payload);
        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() => active0.Equals((object)Validation<Payload?, Payload?>.Valid(payload))));
        var active1 = Validation<Payload?, Payload?>.Invalid(payload);
        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() => active1.Equals((object)Validation<Payload?, Payload?>.Invalid(payload))));
    }

    [Fact]
    public void Identity523COMPARERNULLPreservesNullOrUserExceptionIdentity()
    {
        var null0 = Validation<Payload?, Payload?>.Valid(null);
        Assert.Equal(Validation<Payload?, Payload?>.Valid(null).GetHashCode(), null0.GetHashCode());
        var null1 = Validation<Payload?, Payload?>.Invalid((Payload?)null);
        Assert.Equal(Validation<Payload?, Payload?>.Invalid((Payload?)null).GetHashCode(), null1.GetHashCode());
        var expected = new InvalidOperationException("payload callback");
        var payload = new Payload(expected);
        var active0 = Validation<Payload?, Payload?>.Valid(payload);
        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() => active0.GetHashCode()));
        var active1 = Validation<Payload?, Payload?>.Invalid(payload);
        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() => active1.GetHashCode()));
    }

    [Fact]
    public void Identity524COMPARERNULLPreservesNullOrUserExceptionIdentity()
    {
        var null0 = Validation<Payload?, Payload?>.Valid(null);
        Assert.Equal("Valid()", null0.ToString());
        var null1 = Validation<Payload?, Payload?>.Invalid((Payload?)null);
        Assert.Equal("Invalid([])", null1.ToString());
        var expected = new InvalidOperationException("payload callback");
        var payload = new Payload(expected);
        var active0 = Validation<Payload?, Payload?>.Valid(payload);
        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() => active0.ToString()));
        var active1 = Validation<Payload?, Payload?>.Invalid(payload);
        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() => active1.ToString()));
    }

    [Fact]
    public void Identity218CALLBACKEXCEPTIONPreservesNullOrUserExceptionIdentity()
    {
        var expected = new InvalidOperationException("payload callback");
        var payload = new Payload(expected);
        var active0 = Option<Payload>.Some(payload);
        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() => active0 == Option<Payload>.Some(payload)));
    }

    [Fact]
    public void Identity219CALLBACKEXCEPTIONPreservesNullOrUserExceptionIdentity()
    {
        var expected = new InvalidOperationException("payload callback");
        var payload = new Payload(expected);
        var active0 = Option<Payload>.Some(payload);
        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() => active0 != Option<Payload>.Some(payload)));
    }

    [Fact]
    public void Identity220CALLBACKEXCEPTIONPreservesNullOrUserExceptionIdentity()
    {
        var expected = new InvalidOperationException("payload callback");
        var payload = new Payload(expected);
        var active0 = Option<Payload>.Some(payload);
        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() => active0.Equals(Option<Payload>.Some(payload))));
    }

    [Fact]
    public void Identity223CALLBACKEXCEPTIONPreservesNullOrUserExceptionIdentity()
    {
        var expected = new InvalidOperationException("payload callback");
        var payload = new Payload(expected);
        var active0 = Option<Payload>.Some(payload);
        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() => active0.GetHashCode()));
    }

    [Fact]
    public void Identity224CALLBACKEXCEPTIONPreservesNullOrUserExceptionIdentity()
    {
        var expected = new InvalidOperationException("payload callback");
        var payload = new Payload(expected);
        var active0 = Option<Payload>.Some(payload);
        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() => active0.ToString()));
    }

    private sealed class Payload(InvalidOperationException exception) : IEquatable<Payload>
    {
        public bool Equals(Payload? other) => throw exception;
        public override bool Equals(object? obj) => throw exception;
        public override int GetHashCode() => throw exception;
        public override string ToString() => throw exception;
    }
}

