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

    [Fact]
    public void FromBooleanEagerUsesTheConditionAndNormalizesRuntimeNull()
    {
        string? nullValue = null;

        Assert.True(Option.FromBoolean(false, 1).IsNone);
        Assert.True(Option.FromBoolean<string?>(false, "value").IsNone);
        Assert.Equal(Option.Some(1), Option.FromBoolean(true, 1));
        Assert.True(Option.FromBoolean<string?>(true, nullValue).IsNone);
        Assert.Equal(Option.Some(0), Option.FromBoolean(true, 0));
        Assert.Equal(Option.Some(false), Option.FromBoolean(true, false));
        Assert.Equal(Option.Some(default(DateTime)), Option.FromBoolean(true, default(DateTime)));
    }

    [Fact]
    public void FromBooleanFactoryIsLazyNormalizesNullAndValidatesEagerly()
    {
        var calls = 0;
        var absent = Option.FromBoolean(false, () =>
        {
            calls++;
            return 42;
        });
        var present = Option.FromBoolean(true, () =>
        {
            calls++;
            return 42;
        });

        Assert.True(absent.IsNone);
        Assert.Equal(Option.Some(42), present);
        Assert.Equal(1, calls);
        Assert.True(Option.FromBoolean<string?>(true, () => null).IsNone);

        Func<int> missingFactory = null!;
        Assert.Throws<ArgumentNullException>(() => Option.FromBoolean(false, missingFactory));
        Assert.Throws<ArgumentNullException>(() => Option.FromBoolean(true, missingFactory));
    }

    [Fact]
    public void ToNullableBridgesValueOptionsWithoutChangingTheirEquality()
    {
        Option<int> absent = default;
        var present = Option.Some(7);
        var hashBefore = present.GetHashCode();

        Assert.Null(absent.ToNullable());
        Assert.Null(Option.None<int>().ToNullable());
        Assert.Null(Option.None<DateTime>().ToNullable());

        var zero = Option.Some(0).ToNullable();
        var flag = Option.Some(false).ToNullable();
        var epoch = Option.Some(default(DateTime)).ToNullable();

        Assert.True(zero.HasValue);
        Assert.Equal(0, zero.Value);
        Assert.True(flag.HasValue);
        Assert.False(flag.Value);
        Assert.True(epoch.HasValue);
        Assert.Equal(default(DateTime), epoch.Value);

        Assert.Equal((int?)7, present.ToNullable());
        Assert.Equal(hashBefore, present.GetHashCode());
        Assert.True(present == Option.Some(7));
        Assert.True(present.Equals(Option.Some(7)));
        Assert.NotEqual(Option.None<int>(), present);
        Assert.NotEqual(Option.None<int>().GetHashCode(), present.GetHashCode());
    }

    [Fact]
    public void ZipCombinerOverloadsRunOnlyWhenEveryOptionIsPresent()
    {
        var twoCalls = 0;
        Func<int, string, string> combineTwo = (first, second) =>
        {
            twoCalls++;
            return $"{first}:{second}";
        };
        var threeCalls = 0;
        Func<int, string, bool, string> combineThree = (first, second, third) =>
        {
            threeCalls++;
            return $"{first}:{second}:{third}";
        };

        Assert.Equal(Option.Some("1:two"), Option.Some(1).Zip(Option.Some("two"), combineTwo));
        Assert.True(Option.Some(1).Zip(Option.None<string>(), combineTwo).IsNone);
        Assert.True(Option.None<int>().Zip(Option.Some("two"), combineTwo).IsNone);
        Assert.True(default(Option<int>).Zip(Option.Some("two"), combineTwo).IsNone);
        Assert.Equal(1, twoCalls);

        var third = Option.Some(1).Zip(Option.Some("two"), Option.Some(true), combineThree);
        Assert.True(third.TryGetValue(out var combined));
        Assert.Equal("1:two:True", combined);
        Assert.True(Option.Some(1).Zip(Option.None<string>(), Option.Some(true), combineThree).IsNone);
        Assert.True(Option.Some(1).Zip(Option.Some("two"), Option.None<bool>(), combineThree).IsNone);
        Assert.True(Option.None<int>().Zip(Option.Some("two"), Option.Some(true), combineThree).IsNone);
        Assert.True(default(Option<int>).Zip(Option.Some("two"), Option.Some(true), combineThree).IsNone);
        Assert.True(Option.Some(1).Zip(Option.Some("two"), default(Option<bool>), combineThree).IsNone);
        Assert.Equal(1, threeCalls);
    }

    [Fact]
    public void ZipCombinersNormalizeNullResultsAndPreserveExceptionsAndValidation()
    {
        Assert.True(Option.Some(1).Zip(Option.Some(2), (_, _) => (string?)null).IsNone);
        Assert.Equal(
            Option.Some(1).Zip(Option.Some(2)).Map(_ => (string?)null),
            Option.Some(1).Zip(Option.Some(2), (_, _) => (string?)null));
        Assert.True(Option.Some(1)
            .Zip(Option.Some(2), Option.Some(3), (_, _, _) => (string?)null)
            .IsNone);
        Assert.Equal(
            Option.Some(1).Zip(Option.Some(2)).Zip(Option.Some(3)).Map(_ => (string?)null),
            Option.Some(1).Zip(Option.Some(2), Option.Some(3), (_, _, _) => (string?)null));

        var expected = new InvalidOperationException("combine failed");
        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() =>
            Option.Some(1).Zip<int, int>(Option.Some(2), (_, _) => throw expected)));
        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() =>
            Option.Some(1).Zip<int, int, int>(
                Option.Some(2),
                Option.Some(3),
                (_, _, _) => throw expected)));

        Assert.Throws<ArgumentNullException>(() =>
            Option.Some(1).Zip(Option.Some(2), (Func<int, int, int>)null!));
        Assert.Throws<ArgumentNullException>(() =>
            Option.None<int>().Zip(Option.None<int>(), (Func<int, int, int>)null!));
        Assert.Throws<ArgumentNullException>(() =>
            Option.Some(1).Zip(Option.Some(2), Option.Some(3), (Func<int, int, int, int>)null!));
        Assert.Throws<ArgumentNullException>(() =>
            Option.None<int>().Zip(
                Option.None<int>(),
                Option.None<int>(),
                (Func<int, int, int, int>)null!));
    }

    [Fact]
    public void ZipCombinerOverloadsMatchTupleThenMapForEveryPresenceCombination()
    {
        static string CombineTwo(int first, string second) => $"{first}:{second}";
        static string CombineThree(int first, string second, bool third) => $"{first}:{second}:{third}";

        var firsts = new[] { Option.Some(1), Option.None<int>(), default(Option<int>) };
        var seconds = new[] { Option.Some("two"), Option.None<string>(), default(Option<string>) };
        var thirds = new[] { Option.Some(true), Option.None<bool>(), default(Option<bool>) };

        foreach (var first in firsts)
        {
            foreach (var second in seconds)
            {
                Assert.Equal(
                    first.Zip(second).Map(pair => CombineTwo(pair.First, pair.Second)),
                    first.Zip(second, CombineTwo));
            }
        }

        foreach (var first in firsts)
        {
            foreach (var second in seconds)
            {
                foreach (var third in thirds)
                {
                    Assert.Equal(
                        first.Zip(second).Zip(third).Map(pair => CombineThree(
                            pair.First.First,
                            pair.First.Second,
                            pair.Second)),
                        first.Zip(second, third, CombineThree));
                }
            }
        }
    }

    [Fact]
    public void FourWayZipRunsTheCombinerOnlyWhenEveryOptionIsPresent()
    {
        var calls = 0;
        Func<int, string, bool, long, string> combineFour = (first, second, third, fourth) =>
        {
            calls++;
            return $"{first}:{second}:{third}:{fourth}";
        };
        var first = Option.Some(1);
        var second = Option.Some("two");
        var third = Option.Some(true);
        var fourth = Option.Some(7L);

        var quad = first.Zip(second, third, fourth, combineFour);

        Assert.True(quad.TryGetValue(out var combined));
        Assert.Equal("1:two:True:7", combined);
        Assert.True(first.Zip(Option.None<string>(), third, fourth, combineFour).IsNone);
        Assert.True(first.Zip(second, Option.None<bool>(), fourth, combineFour).IsNone);
        Assert.True(first.Zip(second, third, Option.None<long>(), combineFour).IsNone);
        Assert.True(Option.None<int>().Zip(second, third, fourth, combineFour).IsNone);
        Assert.True(default(Option<int>).Zip(second, third, fourth, combineFour).IsNone);
        Assert.Equal(1, calls);
    }

    [Fact]
    public void FourWayZipNormalizesNullCombineResultsAndPreservesExceptionsAndValidation()
    {
        Assert.True(Option.Some(1)
            .Zip(Option.Some(2), Option.Some(3), Option.Some(4), (_, _, _, _) => (string?)null)
            .IsNone);
        Assert.Equal(
            Option.Some(1).Zip(Option.Some(2)).Zip(Option.Some(3)).Zip(Option.Some(4))
                .Map(_ => (string?)null),
            Option.Some(1).Zip(Option.Some(2), Option.Some(3), Option.Some(4), (_, _, _, _) => (string?)null));

        var expected = new InvalidOperationException("combine failed");
        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() =>
            Option.Some(1).Zip<int, int, int, int>(
                Option.Some(2),
                Option.Some(3),
                Option.Some(4),
                (_, _, _, _) => throw expected)));

        Assert.Throws<ArgumentNullException>(() =>
            Option.Some(1).Zip(
                Option.Some(2),
                Option.Some(3),
                Option.Some(4),
                (Func<int, int, int, int, int>)null!));
        Assert.Throws<ArgumentNullException>(() =>
            Option.None<int>().Zip(
                Option.Some(2),
                Option.Some(3),
                Option.Some(4),
                (Func<int, int, int, int, int>)null!));
    }

    [Fact]
    public void FourWayZipMatchesTupleThenMapForEveryPresenceCombination()
    {
        static string CombineFour(int first, string second, bool third, long fourth) =>
            $"{first}:{second}:{third}:{fourth}";

        var firsts = new[] { Option.Some(1), Option.None<int>(), default(Option<int>) };
        var seconds = new[] { Option.Some("two"), Option.None<string>(), default(Option<string>) };
        var thirds = new[] { Option.Some(true), Option.None<bool>(), default(Option<bool>) };
        var fourths = new[] { Option.Some(7L), Option.None<long>(), default(Option<long>) };

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
}
