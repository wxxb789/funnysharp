namespace FunnySharp.Tests;

public sealed class ValidationTests
{
    [Fact]
    public void DefaultValidationIsInvalidWithExactlyTheDefaultError()
    {
        Validation<string, int> validation = default;

        Assert.True(validation.IsInvalid);
        Assert.False(validation.IsValid);
        Assert.False(validation.TryGetValue(out var value));
        Assert.Null(value);
        Assert.True(validation.TryGetErrors(out var errors));
        Assert.Equal([0], errors);
    }

    [Fact]
    public void InvalidManySnapshotsTheInputAndEnumeratesItOnce()
    {
        var enumerations = 0;
        IEnumerable<string> Source()
        {
            enumerations++;
            yield return "first";
            yield return "second";
        }

        var validation = Validation<int, string>.InvalidMany(Source());

        Assert.Equal(1, enumerations);
        Assert.True(validation.TryGetErrors(out var errors));
        Assert.Equal(["first", "second"], errors);
        Assert.Equal(1, enumerations);

        var mutableErrors = new List<string> { "original" };
        var snapshot = Validation<int, string>.InvalidMany(mutableErrors);
        mutableErrors[0] = "changed";
        mutableErrors.Add("later");

        Assert.True(snapshot.TryGetErrors(out var snapshotErrors));
        Assert.Equal(["original"], snapshotErrors);
    }

    [Fact]
    public void InvalidManyRejectsEmptySequencesAndInvalidPreservesSingleErrorsIncludingNull()
    {
        Assert.Throws<ArgumentException>(() => Validation<int, string>.InvalidMany([]));

        var validation = Validation<int, string?>.Invalid(null);

        Assert.True(validation.TryGetErrors(out var errors));
        Assert.Single(errors);
        Assert.Null(errors[0]);
    }

    [Fact]
    public void MatchValidatesCallbacksAndExecutesOnlyTheActiveBranch()
    {
        var validCalls = 0;
        var invalidCalls = 0;
        var valid = Validation<int, string>.Valid(3);
        var invalid = Validation<int, string>.Invalid("bad");

        Assert.Equal(
            6,
            valid.Match(
                value =>
                {
                    validCalls++;
                    return value * 2;
                },
                errors =>
                {
                    invalidCalls++;
                    return errors.Count;
                }));
        Assert.Equal(
            1,
            invalid.Match(
                value =>
                {
                    validCalls++;
                    return value * 2;
                },
                errors =>
                {
                    invalidCalls++;
                    return errors.Count;
                }));
        Assert.Equal(1, validCalls);
        Assert.Equal(1, invalidCalls);

        Assert.Throws<ArgumentNullException>(() => valid.Match<int>(null!, _ => 0));
        Assert.Throws<ArgumentNullException>(() => valid.Match(_ => 0, null!));
        Assert.Throws<ArgumentNullException>(() => invalid.Match<int>(null!, _ => 0));
        Assert.Throws<ArgumentNullException>(() => invalid.Match(_ => 0, null!));
    }

    [Fact]
    public void MapObeysIdentityAndCompositionLawsAndShortCircuitsInvalidValues()
    {
        static int Increment(int value) => value + 1;
        static string Render(int value) => $"value:{value}";

        var validations = new[]
        {
            Validation<int, string>.Valid(2),
            Validation<int, string>.InvalidMany(["bad", "worse"]),
        };

        foreach (var validation in validations)
        {
            Assert.Equal(validation, validation.Map(value => value));
            Assert.Equal(
                validation.Map(Increment).Map(Render),
                validation.Map(value => Render(Increment(value))));
        }

        var calls = 0;
        var invalid = Validation<int, string>.Invalid("bad").Map(value =>
        {
            calls++;
            return value + 1;
        });
        var expected = new InvalidOperationException("map failed");

        Assert.Equal(Validation<int, string>.Invalid("bad"), invalid);
        Assert.Equal(0, calls);
        Assert.Same(
            expected,
            Assert.Throws<InvalidOperationException>(
                () => Validation<int, string>.Valid(1).Map<int>(_ => throw expected)));
        Assert.Throws<ArgumentNullException>(() => Validation<int, string>.Invalid("bad").Map<int>(null!));
    }

    [Fact]
    public void MapErrorsObeysIdentityAndCompositionLawsAndShortCircuitsValidValues()
    {
        static string Describe(int error) => $"error:{error}";
        static int Length(string error) => error.Length;

        var validations = new[]
        {
            Validation<string, int>.Valid("ok"),
            Validation<string, int>.InvalidMany([42, 7]),
        };

        foreach (var validation in validations)
        {
            Assert.Equal(validation, validation.MapErrors(error => error));
            Assert.Equal(
                validation.MapErrors(Describe).MapErrors(Length),
                validation.MapErrors(error => Length(Describe(error))));
        }

        var calls = 0;
        var valid = Validation<int, string>.Valid(3).MapErrors(error =>
        {
            calls++;
            return error.Length;
        });
        var expected = new InvalidOperationException("error map failed");

        Assert.Equal(Validation<int, int>.Valid(3), valid);
        Assert.Equal(0, calls);
        Assert.Same(
            expected,
            Assert.Throws<InvalidOperationException>(
                () => Validation<int, string>.Invalid("bad").MapErrors<int>(_ => throw expected)));
        Assert.Throws<ArgumentNullException>(() => Validation<int, string>.Valid(3).MapErrors<int>(null!));
    }

    [Fact]
    public void ZipAccumulatesInvalidErrorsInDeterministicLeftThenRightOrder()
    {
        var combined = Validation<int, string>.Valid(2).Zip(Validation<string, string>.Valid("ok"));
        var leftInvalid = Validation<int, string>.InvalidMany(["left-1", "left-2"]);
        var rightInvalid = Validation<string, string>.InvalidMany(["right-1", "right-2"]);

        Assert.Equal(Validation<(int First, string Second), string>.Valid((2, "ok")), combined);
        Assert.Equal(
            Validation<(int First, string Second), string>.InvalidMany(
                ["left-1", "left-2", "right-1", "right-2"]),
            leftInvalid.Zip(rightInvalid));
        Assert.Equal(
            Validation<(int First, string Second), string>.InvalidMany(["right-1", "right-2"]),
            Validation<int, string>.Valid(2).Zip(rightInvalid));
        Assert.Equal(
            Validation<(int First, string Second), string>.InvalidMany(["left-1", "left-2"]),
            leftInvalid.Zip(Validation<string, string>.Valid("ok")));
    }

    [Fact]
    public void EqualityHashingAndTextAreStructuralAndIncludeTheActiveCase()
    {
        var firstValid = Validation<int, int>.Valid(1);
        var secondValid = Validation<int, int>.Valid(1);
        var invalid = Validation<int, int>.InvalidMany([1, 2]);

        Assert.True(firstValid == secondValid);
        Assert.False(firstValid != secondValid);
        Assert.NotEqual(firstValid, invalid);
        Assert.Equal(firstValid.GetHashCode(), secondValid.GetHashCode());
        Assert.Equal(Validation<int, int>.InvalidMany([1, 2]), invalid);
        Assert.NotEqual(Validation<int, int>.InvalidMany([2, 1]), invalid);
        Assert.Equal("Valid(1)", firstValid.ToString());
        Assert.Equal("Invalid([1, 2])", invalid.ToString());
    }

    [Fact]
    public void ApplyObeysApplicativeLawsAndAccumulatesErrorsInApplicationOrder()
    {
        static int Increment(int value) => value + 1;
        static int Double(int value) => value * 2;
        static int Compose(Func<int, int> outer, Func<int, int> inner, int value) => outer(inner(value));

        var value = Validation<int, string>.Valid(3);
        var functions = Validation<Func<int, int>, string>.Valid(Increment);
        var identity = Validation<Func<int, int>, string>.Valid(static item => item);

        Assert.Equal(value, identity.Apply(value));
        Assert.Equal(
            Validation<int, string>.Valid(Increment(3)),
            Validation<Func<int, int>, string>.Valid(Increment).Apply(Validation<int, string>.Valid(3)));
        Assert.Equal(
            functions.Apply(Validation<int, string>.Valid(3)),
            Validation<Func<Func<int, int>, int>, string>.Valid(function => function(3)).Apply(functions));

        var outer = Validation<Func<int, int>, string>.Valid(Double);
        var inner = Validation<Func<int, int>, string>.Valid(Increment);
        var composition = outer.Map(
            first => (Func<Func<int, int>, Func<int, int>>)(second => value => Compose(first, second, value)));

        Assert.Equal(
            composition.Apply(inner).Apply(value),
            outer.Apply(inner.Apply(value)));

        var functionInvalid = Validation<Func<int, int>, string>.InvalidMany(["function-1", "function-2"]);
        var argumentInvalid = Validation<int, string>.InvalidMany(["argument-1", "argument-2"]);

        Assert.Equal(
            Validation<int, string>.InvalidMany(["function-1", "function-2", "argument-1", "argument-2"]),
            functionInvalid.Apply(argumentInvalid));

        var left = Validation<Func<int, int>, string>.Invalid("u");
        var middle = Validation<Func<int, int>, string>.Invalid("v");
        var right = Validation<int, string>.Invalid("w");
        var invalidComposition = left.Map(
            first => (Func<Func<int, int>, Func<int, int>>)(second => value => Compose(first, second, value)));

        Assert.Equal(
            Validation<int, string>.InvalidMany(["u", "v", "w"]),
            invalidComposition.Apply(middle).Apply(right));
    }
}
