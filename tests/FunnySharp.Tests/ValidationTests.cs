namespace FunnySharp.Tests;

public sealed class ValidationTests
{
    [Fact]
    public void DefaultValidationIsUninitializedAndThrowsInsteadOfMasquerading()
    {
        Validation<string, int> validation = default;

        Assert.Equal("Uninitialized", validation.ToString());
        Assert.Throws<InvalidOperationException>(() => validation.IsInvalid);
        Assert.Throws<InvalidOperationException>(() => validation.IsValid);
        Assert.Throws<InvalidOperationException>(() => validation.TryGetValue(out _));
        Assert.Throws<InvalidOperationException>(() => validation.TryGetErrors(out _));
        Assert.Throws<InvalidOperationException>(() => validation.GetHashCode());
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

    [Fact]
    public void ZipAndApplyObeyApplicativeLawsForValidAndInvalidInputs()
    {
        static int Increment(int value) => value + 1;
        static int Double(int value) => value * 2;
        static int Compose(Func<int, int> outer, Func<int, int> inner, int value) => outer(inner(value));

        var value = Validation<int, string>.Valid(3);
        var identity = Validation<Func<int, int>, string>.Valid(static item => item);
        var increment = Validation<Func<int, int>, string>.Valid(Increment);
        var doubleFunction = Validation<Func<int, int>, string>.Valid(Double);

        Assert.Equal(value, identity.Apply(value));
        Assert.Equal(Validation<int, string>.Valid(4), increment.Apply(value));
        Assert.Equal(
            increment.Apply(value),
            Validation<Func<Func<int, int>, int>, string>.Valid(function => function(3)).Apply(increment));

        var composition = doubleFunction.Map(
            first => (Func<Func<int, int>, Func<int, int>>)(second => item => Compose(first, second, item)));

        Assert.Equal(
            composition.Apply(increment).Apply(value),
            doubleFunction.Apply(increment.Apply(value)));
        Assert.Equal(
            Validation<int, string>.Valid(Double(Increment(3))),
            composition.Apply(increment).Apply(value));

        var functionErrors = Validation<Func<int, int>, string>.InvalidMany(["function-1", "function-2"]);
        var argumentErrors = Validation<int, string>.InvalidMany(["argument-1", "argument-2"]);

        Assert.Equal(
            Validation<int, string>.InvalidMany(["function-1", "function-2", "argument-1", "argument-2"]),
            functionErrors.Apply(argumentErrors));
        Assert.Equal(
            Validation<int, string>.InvalidMany(["argument-1", "argument-2"]),
            identity.Apply(argumentErrors));
        Assert.Equal(
            Validation<int, string>.InvalidMany(["function-1", "function-2"]),
            functionErrors.Apply(value));
        Assert.Equal(
            functionErrors.Apply(value),
            Validation<Func<Func<int, int>, int>, string>.Valid(function => function(3)).Apply(functionErrors));

        var left = Validation<Func<int, int>, string>.InvalidMany(["left-1", "left-2"]);
        var middle = Validation<Func<int, int>, string>.InvalidMany(["middle-1"]);
        var right = Validation<int, string>.InvalidMany(["right-1", "right-2", "right-3"]);
        var leftComposition = left.Map(
            first => (Func<Func<int, int>, Func<int, int>>)(second => item => Compose(first, second, item)));

        Assert.Equal(
            Validation<int, string>.InvalidMany(["left-1", "left-2", "middle-1", "right-1", "right-2", "right-3"]),
            leftComposition.Apply(middle).Apply(right));
        Assert.Equal(
            leftComposition.Apply(middle).Apply(right),
            left.Apply(middle.Apply(right)));
    }

    [Fact]
    public void ZipCombinerOverloadsInvokeTheCombinerOnlyWhenEveryOperandIsValid()
    {
        var calls = 0;
        var validFirst = Validation<int, string>.Valid(2);
        var second = Validation<string, string>.Valid("middle");
        var third = Validation<long, string>.Valid(7);

        var pair = validFirst.Zip(second, (first, middle) =>
        {
            calls++;
            return $"{first}:{middle}";
        });
        var triple = validFirst.Zip(second, third, (first, middle, last) =>
        {
            calls++;
            return $"{first}:{middle}:{last}";
        });

        Assert.Equal(Validation<string, string>.Valid("2:middle"), pair);
        Assert.Equal(Validation<string, string>.Valid("2:middle:7"), triple);
        Assert.Equal(2, calls);

        var invalidFirst = Validation<int, string>.Invalid("first-error");
        var invalidSecond = Validation<string, string>.Invalid("second-error");
        var invalidThird = Validation<long, string>.Invalid("third-error");

        Assert.Equal(
            Validation<string, string>.Invalid("first-error"),
            invalidFirst.Zip(second, (first, middle) =>
            {
                calls++;
                return $"{first}:{middle}";
            }));
        Assert.Equal(
            Validation<string, string>.Invalid("second-error"),
            validFirst.Zip(invalidSecond, (first, middle) =>
            {
                calls++;
                return $"{first}:{middle}";
            }));
        Assert.Equal(
            Validation<string, string>.InvalidMany(["first-error", "second-error"]),
            invalidFirst.Zip(invalidSecond, (first, middle) =>
            {
                calls++;
                return $"{first}:{middle}";
            }));
        Assert.Equal(
            Validation<string, string>.InvalidMany(["first-error", "second-error", "third-error"]),
            invalidFirst.Zip(invalidSecond, invalidThird, (first, middle, last) =>
            {
                calls++;
                return $"{first}:{middle}:{last}";
            }));
        Assert.Equal(
            Validation<string, string>.Invalid("third-error"),
            validFirst.Zip(second, invalidThird, (first, middle, last) =>
            {
                calls++;
                return $"{first}:{middle}:{last}";
            }));
        Assert.Equal(2, calls);
    }

    [Fact]
    public void ZipCombinerAccumulatesErrorsLeftToRightAndPreservesEachOperandsOwnOrder()
    {
        var calls = 0;
        var first = Validation<int, string>.InvalidMany(["first-1", "first-2", "first-3"]);
        var second = Validation<string, string>.InvalidMany(["second-1", "second-2"]);
        var third = Validation<long, string>.InvalidMany(["third-1"]);
        var validFirst = Validation<int, string>.Valid(1);
        var validSecond = Validation<string, string>.Valid("ok");
        var validThird = Validation<long, string>.Valid(7);

        string Combine(int left, string right)
        {
            calls++;
            return $"{left}:{right}";
        }

        string CombineThree(int left, string middle, long right)
        {
            calls++;
            return $"{left}:{middle}:{right}";
        }

        Assert.Equal(
            Validation<string, string>.InvalidMany(["first-1", "first-2", "first-3", "second-1", "second-2"]),
            first.Zip(second, Combine));
        Assert.Equal(
            Validation<string, string>.InvalidMany(["first-1", "first-2", "first-3"]),
            first.Zip(validSecond, Combine));
        Assert.Equal(
            Validation<string, string>.InvalidMany(["first-1", "first-2", "first-3", "second-1", "second-2", "third-1"]),
            first.Zip(second, third, CombineThree));
        Assert.Equal(
            Validation<string, string>.InvalidMany(["second-1", "second-2", "third-1"]),
            validFirst.Zip(second, third, CombineThree));
        Assert.Equal(
            Validation<string, string>.InvalidMany(["first-1", "first-2", "first-3", "third-1"]),
            first.Zip(validSecond, third, CombineThree));
        Assert.Equal(
            Validation<string, string>.InvalidMany(["third-1"]),
            validFirst.Zip(validSecond, third, CombineThree));
        Assert.Equal(0, calls);
    }

    [Fact]
    public void ZipCombinerPropagatesExceptionsAndRejectsNullAndUninitializedOperands()
    {
        var second = Validation<string, string>.Valid("ok");
        var third = Validation<long, string>.Valid(7);
        var invalidFirst = Validation<int, string>.InvalidMany(["first-1"]);
        var invalidSecond = Validation<string, string>.InvalidMany(["second-1"]);
        var expected = new InvalidOperationException("combine failed");
        Validation<int, string> uninitializedFirst = default;
        Validation<string, string> uninitializedSecond = default;
        Validation<long, string> uninitializedThird = default;

        Assert.Same(
            expected,
            Assert.Throws<InvalidOperationException>(() =>
                Validation<int, string>.Valid(1).Zip<string, string>(second, (_, _) => throw expected)));
        Assert.Same(
            expected,
            Assert.Throws<InvalidOperationException>(() =>
                Validation<int, string>.Valid(1).Zip<string, long, string>(second, third, (_, _, _) => throw expected)));

        Assert.Throws<ArgumentNullException>(() =>
            Validation<int, string>.Valid(1).Zip<string, string>(second, null!));
        Assert.Throws<ArgumentNullException>(() =>
            invalidFirst.Zip<string, string>(invalidSecond, null!));
        Assert.Throws<ArgumentNullException>(() =>
            Validation<int, string>.Valid(1).Zip<string, long, string>(second, third, null!));
        Assert.Throws<ArgumentNullException>(() =>
            invalidFirst.Zip<string, long, string>(invalidSecond, third, null!));
        Assert.Throws<ArgumentNullException>(() =>
            uninitializedFirst.Zip<string, string>(second, null!));
        Assert.Throws<ArgumentNullException>(() =>
            uninitializedFirst.Zip<string, long, string>(second, third, null!));

        Assert.Throws<InvalidOperationException>(() =>
            uninitializedFirst.Zip<string, string>(second, (_, _) => "value"));
        Assert.Throws<InvalidOperationException>(() =>
            Validation<int, string>.Valid(1).Zip<string, string>(uninitializedSecond, (_, _) => "value"));
        Assert.Throws<InvalidOperationException>(() =>
            uninitializedFirst.Zip<string, long, string>(second, third, (_, _, _) => "value"));
        Assert.Throws<InvalidOperationException>(() =>
            Validation<int, string>.Valid(1).Zip<string, long, string>(uninitializedSecond, third, (_, _, _) => "value"));
        Assert.Throws<InvalidOperationException>(() =>
            Validation<int, string>.Valid(1).Zip<string, long, string>(second, uninitializedThird, (_, _, _) => "value"));
    }

    [Fact]
    public void ZipCombinerMatchesTheTupledZipThenMapFormForEveryCombination()
    {
        static string Combine(int first, string second) => $"{first}:{second}";
        static string CombineThree(int first, string second, long third) => $"{first}:{second}:{third}";

        var firsts = new[]
        {
            Validation<int, string>.Valid(2),
            Validation<int, string>.InvalidMany(["first-1", "first-2"]),
        };
        var seconds = new[]
        {
            Validation<string, string>.Valid("ok"),
            Validation<string, string>.InvalidMany(["second-1"]),
        };
        var thirds = new[]
        {
            Validation<long, string>.Valid(7),
            Validation<long, string>.InvalidMany(["third-1", "third-2"]),
        };

        foreach (var first in firsts)
        {
            foreach (var second in seconds)
            {
                Assert.Equal(
                    first.Zip(second).Map(pair => Combine(pair.First, pair.Second)),
                    first.Zip(second, Combine));
            }
        }

        foreach (var first in firsts)
        {
            foreach (var second in seconds)
            {
                foreach (var third in thirds)
                {
                    Assert.Equal(
                        first.Zip(second, (left, right) => (left, right))
                            .Zip(third, (pair, value) => CombineThree(pair.left, pair.right, value)),
                        first.Zip(second, third, CombineThree));
                }
            }
        }
    }

    [Fact]
    public void FourWayZipInvokesTheCombinerOnlyWhenEveryOperandIsValid()
    {
        var calls = 0;

        string CombineFour(int first, string middle, long last, bool flag)
        {
            calls++;
            return $"{first}:{middle}:{last}:{flag}";
        }

        var validFirst = Validation<int, string>.Valid(2);
        var second = Validation<string, string>.Valid("middle");
        var third = Validation<long, string>.Valid(7);
        var fourth = Validation<bool, string>.Valid(true);

        var quad = validFirst.Zip(second, third, fourth, CombineFour);

        Assert.Equal(Validation<string, string>.Valid("2:middle:7:True"), quad);
        Assert.Equal(1, calls);

        var invalidFirst = Validation<int, string>.Invalid("first-error");
        var invalidSecond = Validation<string, string>.Invalid("second-error");
        var invalidThird = Validation<long, string>.Invalid("third-error");
        var invalidFourth = Validation<bool, string>.Invalid("fourth-error");

        Assert.Equal(
            Validation<string, string>.Invalid("first-error"),
            invalidFirst.Zip(second, third, fourth, CombineFour));
        Assert.Equal(
            Validation<string, string>.Invalid("second-error"),
            validFirst.Zip(invalidSecond, third, fourth, CombineFour));
        Assert.Equal(
            Validation<string, string>.Invalid("third-error"),
            validFirst.Zip(second, invalidThird, fourth, CombineFour));
        Assert.Equal(
            Validation<string, string>.Invalid("fourth-error"),
            validFirst.Zip(second, third, invalidFourth, CombineFour));
        Assert.Equal(1, calls);
    }

    [Fact]
    public void FourWayZipAccumulatesErrorsLeftToRightAcrossAllFourOperands()
    {
        var calls = 0;
        var first = Validation<int, string>.InvalidMany(["first-1", "first-2"]);
        var second = Validation<string, string>.InvalidMany(["second-1"]);
        var third = Validation<long, string>.InvalidMany(["third-1", "third-2", "third-3"]);
        var fourth = Validation<bool, string>.InvalidMany(["fourth-1"]);
        var validFirst = Validation<int, string>.Valid(1);
        var validSecond = Validation<string, string>.Valid("ok");
        var validThird = Validation<long, string>.Valid(7);
        var validFourth = Validation<bool, string>.Valid(false);

        string CombineFour(int left, string middle, long right, bool flag)
        {
            calls++;
            return $"{left}:{middle}:{right}:{flag}";
        }

        Assert.Equal(
            Validation<string, string>.InvalidMany(
                ["first-1", "first-2", "second-1", "third-1", "third-2", "third-3", "fourth-1"]),
            first.Zip(second, third, fourth, CombineFour));
        Assert.Equal(
            Validation<string, string>.InvalidMany(
                ["second-1", "third-1", "third-2", "third-3", "fourth-1"]),
            validFirst.Zip(second, third, fourth, CombineFour));
        Assert.Equal(
            Validation<string, string>.InvalidMany(
                ["first-1", "first-2", "third-1", "third-2", "third-3", "fourth-1"]),
            first.Zip(validSecond, third, fourth, CombineFour));
        Assert.Equal(
            Validation<string, string>.InvalidMany(
                ["first-1", "first-2", "second-1", "third-1", "third-2", "third-3"]),
            first.Zip(second, third, validFourth, CombineFour));
        Assert.Equal(
            Validation<string, string>.InvalidMany(["fourth-1"]),
            validFirst.Zip(validSecond, validThird, fourth, CombineFour));
        Assert.Equal(0, calls);
    }

    [Fact]
    public void FourWayZipPropagatesExceptionsAndRejectsNullAndUninitializedOperands()
    {
        var second = Validation<string, string>.Valid("ok");
        var third = Validation<long, string>.Valid(7);
        var fourth = Validation<bool, string>.Valid(true);
        var invalidFirst = Validation<int, string>.InvalidMany(["first-1"]);
        var invalidSecond = Validation<string, string>.InvalidMany(["second-1"]);
        var expected = new InvalidOperationException("combine failed");
        Validation<int, string> uninitializedFirst = default;
        Validation<string, string> uninitializedSecond = default;
        Validation<long, string> uninitializedThird = default;
        Validation<bool, string> uninitializedFourth = default;

        Assert.Same(
            expected,
            Assert.Throws<InvalidOperationException>(() =>
                Validation<int, string>.Valid(1).Zip<string, long, bool, string>(
                    second, third, fourth, (_, _, _, _) => throw expected)));

        Assert.Throws<ArgumentNullException>(() =>
            Validation<int, string>.Valid(1).Zip<string, long, bool, string>(second, third, fourth, null!));
        Assert.Throws<ArgumentNullException>(() =>
            invalidFirst.Zip<string, long, bool, string>(invalidSecond, third, fourth, null!));
        Assert.Throws<ArgumentNullException>(() =>
            uninitializedFirst.Zip<string, long, bool, string>(second, third, fourth, null!));

        Assert.Throws<InvalidOperationException>(() =>
            uninitializedFirst.Zip<string, long, bool, string>(second, third, fourth, (_, _, _, _) => "value"));
        Assert.Throws<InvalidOperationException>(() =>
            Validation<int, string>.Valid(1).Zip<string, long, bool, string>(
                uninitializedSecond, third, fourth, (_, _, _, _) => "value"));
        Assert.Throws<InvalidOperationException>(() =>
            Validation<int, string>.Valid(1).Zip<string, long, bool, string>(
                second, uninitializedThird, fourth, (_, _, _, _) => "value"));
        Assert.Throws<InvalidOperationException>(() =>
            Validation<int, string>.Valid(1).Zip<string, long, bool, string>(
                second, third, uninitializedFourth, (_, _, _, _) => "value"));
    }

    [Fact]
    public void FourWayZipMatchesTheTupledZipThenMapFormForEveryCombination()
    {
        static string CombineFour(int first, string second, long third, bool fourth) =>
            $"{first}:{second}:{third}:{fourth}";

        var firsts = new[]
        {
            Validation<int, string>.Valid(2),
            Validation<int, string>.InvalidMany(["first-1", "first-2"]),
        };
        var seconds = new[]
        {
            Validation<string, string>.Valid("ok"),
            Validation<string, string>.InvalidMany(["second-1"]),
        };
        var thirds = new[]
        {
            Validation<long, string>.Valid(7),
            Validation<long, string>.InvalidMany(["third-1", "third-2"]),
        };
        var fourths = new[]
        {
            Validation<bool, string>.Valid(true),
            Validation<bool, string>.InvalidMany(["fourth-1"]),
        };

        foreach (var first in firsts)
        {
            foreach (var second in seconds)
            {
                foreach (var third in thirds)
                {
                    foreach (var fourth in fourths)
                    {
                        Assert.Equal(
                            first.Zip(second).Zip(third).Zip(fourth).Map(quad => CombineFour(
                                quad.First.First.First,
                                quad.First.First.Second,
                                quad.First.Second,
                                quad.Second)),
                            first.Zip(second, third, fourth, CombineFour));
                    }
                }
            }
        }
    }

    [Fact]
    public void EqualityAndHashingAreStructuralForNestedAndMultiErrorValidations()
    {
        var nestedInvalid = Validation<Validation<int, int>, string>.Valid(
            Validation<int, int>.InvalidMany([1, 2, 3]));
        var nestedInvalidCopy = Validation<Validation<int, int>, string>.Valid(
            Validation<int, int>.InvalidMany([1, 2, 3]));
        var nestedReordered = Validation<Validation<int, int>, string>.Valid(
            Validation<int, int>.InvalidMany([3, 2, 1]));
        var nestedValid = Validation<Validation<int, int>, string>.Valid(Validation<int, int>.Valid(1));

        Assert.Equal(nestedInvalid, nestedInvalidCopy);
        Assert.Equal(nestedInvalid.GetHashCode(), nestedInvalidCopy.GetHashCode());
        Assert.NotEqual(nestedInvalid, nestedReordered);
        Assert.NotEqual(nestedInvalid, nestedValid);

        var valid = Validation<int, int>.Valid(1);
        var invalid = Validation<int, int>.Invalid(1);
        var multiError = Validation<int, int>.InvalidMany([1, 1, 2]);

        Assert.NotEqual(valid, invalid);
        Assert.False(valid.Equals((object)invalid));
        Assert.False(invalid.Equals((object)valid));
        Assert.True(valid.Equals((object)Validation<int, int>.Valid(1)));
        Assert.False(valid.Equals(null));
        Assert.False(valid.Equals("Valid(1)"));

        Assert.Equal(multiError, Validation<int, int>.InvalidMany([1, 1, 2]));
        Assert.Equal(multiError.GetHashCode(), Validation<int, int>.InvalidMany([1, 1, 2]).GetHashCode());
        Assert.NotEqual(multiError, Validation<int, int>.InvalidMany([1, 2, 1]));
        Assert.NotEqual(multiError, Validation<int, int>.InvalidMany([1, 1]));
        Assert.NotEqual(multiError, Validation<int, int>.InvalidMany([1, 1, 2, 2]));

        var stringErrors = Validation<int, string>.InvalidMany(["first", "second"]);
        Assert.NotEqual(stringErrors, Validation<int, string>.InvalidMany(["second", "first"]));
    }

    [Fact]
    public void MapErrorsPreservesOrderCountAndDuplicatesForManyErrors()
    {
        var errors = new[] { 5, 1, 4, 2, 3, 5, 1, 0, 9, 8, 7, 6 };
        var validation = Validation<string, int>.InvalidMany(errors);

        var mapped = validation.MapErrors(error => $"e{error}");

        Assert.True(mapped.TryGetErrors(out var mappedErrors));
        Assert.Equal(errors.Select(error => $"e{error}"), mappedErrors);

        var folded = validation.MapErrors(error => error % 4);

        Assert.True(folded.TryGetErrors(out var foldedErrors));
        Assert.Equal([1, 1, 0, 2, 3, 1, 1, 0, 1, 0, 3, 2], foldedErrors);
    }
}
