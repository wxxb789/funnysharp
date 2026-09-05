namespace FunnySharp.Tests;

public sealed class OptionTests
{
    [Fact]
    public void MatchPreservesSelectedCallbackExceptionIdentity()
    {
        var someValueFailure = new InvalidOperationException("some value");
        var noneValueFailure = new InvalidOperationException("none value");
        var someActionFailure = new InvalidOperationException("some action");
        var noneActionFailure = new InvalidOperationException("none action");

        Assert.Same(
            someValueFailure,
            Assert.Throws<InvalidOperationException>(() =>
                Option.Some(1).Match<int>(_ => throw someValueFailure, () => 0)));
        Assert.Same(
            noneValueFailure,
            Assert.Throws<InvalidOperationException>(() =>
                Option.None<int>().Match(_ => 0, () => throw noneValueFailure)));
        Assert.Same(
            someActionFailure,
            Assert.Throws<InvalidOperationException>(() =>
                Option.Some(1).Match(_ => throw someActionFailure, () => { })));
        Assert.Same(
            noneActionFailure,
            Assert.Throws<InvalidOperationException>(() =>
                Option.None<int>().Match(_ => { }, () => throw noneActionFailure)));
    }

    [Fact]
    public void ZipAndLazyFallbackPreserveDefaultAndNestedValues()
    {
        var zippedDefaults = Option.Some(0).Zip(Option.Some(false));
        Assert.True(zippedDefaults.TryGetValue(out var defaults));
        Assert.Equal((0, false), defaults);

        var nestedNone = Option.Some(Option.None<int>());
        var zippedNested = nestedNone.Zip(Option.Some("kept"));
        Assert.True(zippedNested.TryGetValue(out var nested));
        Assert.True(nested.First.IsNone);
        Assert.Equal("kept", nested.Second);

        var fallbackCalls = 0;
        Assert.Equal(
            nestedNone,
            nestedNone.OrElseWith(() =>
            {
                fallbackCalls++;
                return Option.Some(Option.Some(42));
            }));
        Assert.Equal(0, fallbackCalls);

        var expected = new InvalidOperationException("fallback");
        Assert.Same(
            expected,
            Assert.Throws<InvalidOperationException>(() =>
                Option.None<Option<int>>().OrElseWith(() => throw expected)));
    }

    [Fact]
    public void DefaultAndNoneRepresentAbsenceWhileSomePreservesDefaultValues()
    {
        Option<int> defaultOption = default;

        Assert.True(defaultOption.IsNone);
        Assert.False(defaultOption.IsSome);
        Assert.Equal(Option<int>.None, defaultOption);
        Assert.Equal(Option.None<int>(), defaultOption);
        Assert.Equal(Option.Some(0), Option<int>.Some(default));
        Assert.Equal(Option.Some(false), Option<bool>.Some(default));
        Assert.Equal(Option.Some(default(DateTime)), Option<DateTime>.Some(default));
        Assert.NotEqual(defaultOption, Option.Some(0));
    }

    [Fact]
    public void SomeRejectsNullAndNullableConversionTreatsNullAsAbsence()
    {
        string? missingText = null;
        int? missingNumber = null;

        Assert.Throws<ArgumentNullException>(() => Option.Some<string>(null!));
        Assert.Throws<ArgumentNullException>(() => Option<string>.Some(null!));
        Assert.Equal(Option.None<string>(), Option.FromNullable(missingText));
        Assert.Equal(Option.None<int>(), Option.FromNullable(missingNumber));
        Assert.Equal(Option.Some("value"), Option.FromNullable("value"));
        Assert.Equal(Option.Some(42), Option.FromNullable((int?)42));
    }

    [Fact]
    public void NestedOptionsKeepOuterAndInnerAbsenceDistinct()
    {
        var outerNone = Option.None<Option<int>>();
        var innerNone = Option.Some(Option.None<int>());

        Assert.NotEqual(outerNone, innerNone);
        Assert.True(innerNone.TryGetValue(out var inner));
        Assert.True(inner.IsNone);
    }

    [Fact]
    public void TryGetValueExposesOnlyPresentValues()
    {
        var present = Option.Some("value");
        var absent = Option.None<string>();

        Assert.True(present.TryGetValue(out var value));
        Assert.Equal("value", value);
        Assert.False(absent.TryGetValue(out var missing));
        Assert.Null(missing);
    }

    [Fact]
    public void MatchEvaluatesExactlyOneBranch()
    {
        var someCalls = 0;
        var noneCalls = 0;

        var presentResult = Option.Some(3).Match(
            value =>
            {
                someCalls++;
                return value * 2;
            },
            () =>
            {
                noneCalls++;
                return -1;
            });
        var absentResult = Option.None<int>().Match(
            value =>
            {
                someCalls++;
                return value * 2;
            },
            () =>
            {
                noneCalls++;
                return -1;
            });

        Assert.Equal(6, presentResult);
        Assert.Equal(-1, absentResult);
        Assert.Equal(1, someCalls);
        Assert.Equal(1, noneCalls);
    }

    [Fact]
    public void MatchActionEvaluatesExactlyOneBranch()
    {
        var observed = new List<string>();

        Option.Some(3).Match(
            value => observed.Add($"some:{value}"),
            () => observed.Add("none"));
        Option.None<int>().Match(
            value => observed.Add($"some:{value}"),
            () => observed.Add("none"));

        Assert.Equal(["some:3", "none"], observed);
    }

    [Fact]
    public void MatchMayReturnNullWithoutConstructingAnOption()
    {
        Assert.Null(Option.Some(1).Match<string?>(_ => null, () => "none"));
        Assert.Null(Option.None<int>().Match(_ => "some", () => (string?)null));
    }

    [Fact]
    public void MatchValidatesBothBranchesEagerly()
    {
        Func<int, int> some = value => value;
        Func<int> none = () => 0;
        Action<int> someAction = _ => { };
        Action noneAction = () => { };

        Assert.Throws<ArgumentNullException>(() => Option.Some(1).Match<int>(null!, none));
        Assert.Throws<ArgumentNullException>(() => Option.Some(1).Match(some, null!));
        Assert.Throws<ArgumentNullException>(() => Option.None<int>().Match<int>(null!, none));
        Assert.Throws<ArgumentNullException>(() => Option.None<int>().Match(some, null!));
        Assert.Throws<ArgumentNullException>(() => Option.Some(1).Match(null!, noneAction));
        Assert.Throws<ArgumentNullException>(() => Option.Some(1).Match(someAction, null!));
    }

    [Fact]
    public void MapObeysIdentityAndCompositionLaws()
    {
        static int Increment(int value) => value + 1;
        static string Format(int value) => $"value:{value}";

        foreach (var option in new[] { Option.None<int>(), Option.Some(4) })
        {
            Assert.Equal(option, option.Map(value => value));
            Assert.Equal(
                option.Map(Increment).Map(Format),
                option.Map(value => Format(Increment(value))));
        }
    }

    [Fact]
    public void MapShortCircuitsAbsenceAndConvertsNullResultsToNone()
    {
        var absentCalls = 0;
        var chainedCalls = 0;

        var absent = Option.None<int>().Map(value =>
        {
            absentCalls++;
            return value.ToString();
        });
        var nullableNumber = Option.Some(1).Map<int?>(_ => 0);
        var nullableText = Option.Some(1).Map<string?>(_ => "value");
        var nullResult = Option.Some(1)
            .Map<string?>(_ => null)
            .Map(value =>
            {
                chainedCalls++;
                return value!.Length;
            });

        Assert.True(absent.IsNone);
        Assert.Equal(Option.Some<int?>((int?)0), nullableNumber);
        Assert.Equal(Option.Some<string?>("value"), nullableText);
        Assert.True(nullResult.IsNone);
        Assert.Equal(0, absentCalls);
        Assert.Equal(0, chainedCalls);
    }

    [Fact]
    public void BindObeysMonadLaws()
    {
        static Option<int> Some(int value) => Option.Some(value);
        static Option<int> IncrementWhenPositive(int value) =>
            value > 0 ? Option.Some(value + 1) : Option.None<int>();
        static Option<string> FormatWhenEven(int value) =>
            value % 2 == 0 ? Option.Some($"value:{value}") : Option.None<string>();

        var value = 3;
        Assert.Equal(IncrementWhenPositive(value), Option.Some(value).Bind(IncrementWhenPositive));

        foreach (var option in new[] { Option.None<int>(), Option.Some(-1), Option.Some(3) })
        {
            Assert.Equal(option, option.Bind(Some));
            Assert.Equal(
                option.Bind(IncrementWhenPositive).Bind(FormatWhenEven),
                option.Bind(number => IncrementWhenPositive(number).Bind(FormatWhenEven)));
        }
    }

    [Fact]
    public void TransformationsPreserveCallbackExceptions()
    {
        var expected = new InvalidOperationException("callback failed");

        var mapException = Assert.Throws<InvalidOperationException>(
            () => Option.Some(1).Map<int>(_ => throw expected));
        var bindException = Assert.Throws<InvalidOperationException>(
            () => Option.Some(1).Bind<int>(_ => throw expected));
        var filterException = Assert.Throws<InvalidOperationException>(
            () => Option.Some(1).Filter(_ => throw expected));

        Assert.Same(expected, mapException);
        Assert.Same(expected, bindException);
        Assert.Same(expected, filterException);
    }

    [Fact]
    public void TransformationsValidateCallbacksEagerly()
    {
        Assert.Throws<ArgumentNullException>(() => Option.None<int>().Map<int>(null!));
        Assert.Throws<ArgumentNullException>(() => Option.None<int>().Bind<int>(null!));
        Assert.Throws<ArgumentNullException>(() => Option.None<int>().Filter(null!));
    }

    [Fact]
    public void FilterKeepsMatchingValuesAndShortCircuitsAbsence()
    {
        var calls = 0;

        Assert.Equal(Option.Some(4), Option.Some(4).Filter(value => value % 2 == 0));
        Assert.True(Option.Some(3).Filter(value => value % 2 == 0).IsNone);
        Assert.True(Option.None<int>().Filter(value =>
        {
            calls++;
            return true;
        }).IsNone);
        Assert.Equal(0, calls);
    }

    [Fact]
    public void ZipCombinesOnlyTwoPresentOptions()
    {
        Assert.Equal(Option.Some((1, "two")), Option.Some(1).Zip(Option.Some("two")));
        Assert.True(Option.Some(1).Zip(Option.None<string>()).IsNone);
        Assert.True(Option.None<int>().Zip(Option.Some("two")).IsNone);
    }

    [Fact]
    public void ValueFallbacksAreLazyAndExplicit()
    {
        var fallbackCalls = 0;

        Assert.Equal(3, Option.Some(3).GetValueOr(9));
        Assert.Equal(9, Option.None<int>().GetValueOr(9));
        Assert.Equal(
            3,
            Option.Some(3).GetValueOrElse(() =>
            {
                fallbackCalls++;
                return 9;
            }));
        Assert.Equal(0, fallbackCalls);
        Assert.Equal(
            9,
            Option.None<int>().GetValueOrElse(() =>
            {
                fallbackCalls++;
                return 9;
            }));
        Assert.Equal(1, fallbackCalls);
        Assert.Equal(0, Option.None<int>().GetValueOrDefault());
        Assert.Null(Option.None<string>().GetValueOrDefault());
    }

    [Fact]
    public void FallbacksRejectNullAndPreserveFactoryExceptions()
    {
        var expected = new InvalidOperationException("fallback failed");

        Assert.Throws<ArgumentNullException>(() => Option.None<string>().GetValueOr(null!));
        Assert.Throws<ArgumentNullException>(() => Option.Some("value").GetValueOr(null!));
        Assert.Throws<ArgumentNullException>(() => Option.None<string>().GetValueOrElse(null!));
        Assert.Throws<ArgumentNullException>(() => Option.Some("value").GetValueOrElse(null!));
        Assert.Equal("value", Option.Some("value").GetValueOrElse(() => null!));
        Assert.Throws<ArgumentNullException>(() => Option.None<string>().GetValueOrElse(() => null!));
        var actual = Assert.Throws<InvalidOperationException>(
            () => Option.None<string>().GetValueOrElse(() => throw expected));

        Assert.Same(expected, actual);
    }

    [Fact]
    public void OptionFallbacksAreLazy()
    {
        var calls = 0;

        Assert.Equal(Option.Some(1), Option.Some(1).OrElse(Option.Some(2)));
        Assert.Equal(Option.Some(2), Option.None<int>().OrElse(Option.Some(2)));
        Assert.Equal(
            Option.Some(1),
            Option.Some(1).OrElseWith(() =>
            {
                calls++;
                return Option.Some(2);
            }));
        Assert.Equal(0, calls);
        Assert.Equal(
            Option.Some(2),
            Option.None<int>().OrElseWith(() =>
            {
                calls++;
                return Option.Some(2);
            }));
        Assert.Equal(1, calls);
        Assert.Throws<ArgumentNullException>(() => Option.None<int>().OrElseWith(null!));
        Assert.Throws<ArgumentNullException>(() => Option.Some(1).OrElseWith(null!));
    }

    [Fact]
    public void EqualityHashingAndOperatorsFollowPayloadEquality()
    {
        var first = Option.Some("value");
        var equal = Option.Some(new string("value".ToCharArray()));
        var different = Option.Some("other");
        var none = Option.None<string>();

        Assert.True(first.Equals(equal));
        Assert.True(first.Equals((object)equal));
        Assert.Equal(first.GetHashCode(), equal.GetHashCode());
        Assert.True(first == equal);
        Assert.False(first != equal);
        Assert.NotEqual(first, different);
        Assert.NotEqual(first, none);
        Assert.Equal(default(Option<string>).GetHashCode(), none.GetHashCode());
        Assert.Equal(EqualityComparer<Option<string>>.Default.Equals(first, equal), first.Equals(equal));
    }

    [Fact]
    public void ToStringDistinguishesPresenceFromAbsence()
    {
        Assert.Equal("None", Option.None<int>().ToString());
        Assert.Equal("Some(0)", Option.Some(0).ToString());
        Assert.Equal("Some(value)", Option.Some("value").ToString());
    }
}
