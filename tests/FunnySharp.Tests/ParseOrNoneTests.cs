using System.Globalization;

namespace FunnySharp.Tests;

public sealed class ParseOrNoneTests
{
    [Fact]
    public void ParseBridgesRejectNullArgumentsEagerly()
    {
        string? source = null;

        Assert.Throws<ArgumentNullException>(() => source!.ParseOrNone<int>());
        Assert.Throws<ArgumentNullException>(() => source!.ParseOrNone<int>(CultureInfo.InvariantCulture));
        Assert.Throws<ArgumentNullException>(() => source!.ParseIntOrNone());
        Assert.Throws<ArgumentNullException>(() => source!.ParseBoolOrNone());
        Assert.Throws<ArgumentNullException>(() => source!.ParseGuidOrNone());
        Assert.Throws<ArgumentNullException>(() => source!.ParseDecimalOrNone());
    }

    [Fact]
    public void NamedAndGenericBridgesParseValidInputsToTheirValues()
    {
        Assert.True("42".ParseIntOrNone().TryGetValue(out var parsedInt));
        Assert.Equal(42, parsedInt);
        Assert.True("42".ParseOrNone<int>().TryGetValue(out var genericInt));
        Assert.Equal(42, genericInt);
        Assert.True("true".ParseBoolOrNone().TryGetValue(out var parsedBool));
        Assert.True(parsedBool);
        Assert.True("255".ParseByteOrNone().TryGetValue(out var parsedByte));
        Assert.Equal((byte)255, parsedByte);
        Assert.True("127".ParseSByteOrNone().TryGetValue(out var parsedSByte));
        Assert.Equal((sbyte)127, parsedSByte);
        Assert.True("12345".ParseShortOrNone().TryGetValue(out var parsedShort));
        Assert.Equal((short)12345, parsedShort);
        Assert.True("12345".ParseUShortOrNone().TryGetValue(out var parsedUShort));
        Assert.Equal((ushort)12345, parsedUShort);
        Assert.True("42".ParseUIntOrNone().TryGetValue(out var parsedUInt));
        Assert.Equal(42u, parsedUInt);
        Assert.True("42".ParseLongOrNone().TryGetValue(out var parsedLong));
        Assert.Equal(42L, parsedLong);
        Assert.True("42".ParseULongOrNone().TryGetValue(out var parsedULong));
        Assert.Equal(42ul, parsedULong);
        Assert.True("42".ParseFloatOrNone().TryGetValue(out var parsedFloat));
        Assert.Equal(42f, parsedFloat);
        Assert.True("42".ParseDoubleOrNone().TryGetValue(out var parsedDouble));
        Assert.Equal(42d, parsedDouble);
        Assert.True("42".ParseDecimalOrNone().TryGetValue(out var parsedDecimal));
        Assert.Equal(42m, parsedDecimal);
        Assert.True("1.02:03:04".ParseTimeSpanOrNone().TryGetValue(out var parsedTimeSpan));
        Assert.Equal(new TimeSpan(1, 2, 3, 4), parsedTimeSpan);
        Assert.True("01234567-89ab-cdef-0123-456789abcdef".ParseGuidOrNone().TryGetValue(out var parsedGuid));
        Assert.Equal(new Guid("01234567-89ab-cdef-0123-456789abcdef"), parsedGuid);
    }

    [Fact]
    public void ParseOrNoneHonorsExplicitFormatProviders()
    {
        var german = new CultureInfo("de-DE");

        Assert.True("1,5".ParseOrNone<decimal>(german).TryGetValue(out var germanDecimal));
        Assert.Equal(1.5m, germanDecimal);
        Assert.True("1.5".ParseOrNone<decimal>(CultureInfo.InvariantCulture).TryGetValue(out var invariantDecimal));
        Assert.Equal(1.5m, invariantDecimal);
        Assert.True("1.5".ParseOrNone<double>(CultureInfo.InvariantCulture).TryGetValue(out var invariantDouble));
        Assert.Equal(1.5d, invariantDouble);
        Assert.True("2026-09-20".ParseOrNone<DateTime>(CultureInfo.InvariantCulture).TryGetValue(out var date));
        Assert.Equal(new DateTime(2026, 9, 20), date);
        Assert.True("2026-09-20".ParseOrNone<DateTimeOffset>(CultureInfo.InvariantCulture).TryGetValue(out var offsetDate));
        Assert.Equal(new DateTimeOffset(new DateTime(2026, 9, 20)), offsetDate);
    }

    [Fact]
    public void InvalidInputsReturnNoneForEveryNamedBridgeAndTheGenericForm()
    {
        Assert.True("abc".ParseBoolOrNone().IsNone);
        Assert.True("abc".ParseByteOrNone().IsNone);
        Assert.True("abc".ParseSByteOrNone().IsNone);
        Assert.True("abc".ParseShortOrNone().IsNone);
        Assert.True("abc".ParseUShortOrNone().IsNone);
        Assert.True("1.5".ParseIntOrNone().IsNone);
        Assert.True("-1".ParseUIntOrNone().IsNone);
        Assert.True("99999999999999999999999".ParseLongOrNone().IsNone);
        Assert.True("-1".ParseULongOrNone().IsNone);
        Assert.True("abc".ParseFloatOrNone().IsNone);
        Assert.True("abc".ParseDoubleOrNone().IsNone);
        Assert.True("abc".ParseDecimalOrNone().IsNone);
        Assert.True("not-a-date".ParseDateTimeOrNone().IsNone);
        Assert.True("not-a-date".ParseDateTimeOffsetOrNone().IsNone);
        Assert.True("abc".ParseTimeSpanOrNone().IsNone);
        Assert.True("not-a-guid".ParseGuidOrNone().IsNone);
        Assert.True("abc".ParseOrNone<int>().IsNone);
        Assert.True("abc".ParseOrNone<decimal>().IsNone);
        Assert.True("not-a-date".ParseOrNone<DateTimeOffset>(CultureInfo.InvariantCulture).IsNone);
    }

    [Fact]
    public void EmptyStringsReturnNoneInsteadOfThrowing()
    {
        Assert.True(string.Empty.ParseIntOrNone().IsNone);
        Assert.True(string.Empty.ParseBoolOrNone().IsNone);
        Assert.True(string.Empty.ParseGuidOrNone().IsNone);
        Assert.True(string.Empty.ParseDateTimeOrNone().IsNone);
        Assert.True(string.Empty.ParseOrNone<int>().IsNone);
    }

    [Fact]
    public void ParseIntOrNoneMatchesTheBclParseWhitespaceBehavior()
    {
        Assert.True(" 42".ParseIntOrNone().TryGetValue(out var leading));
        Assert.Equal(42, leading);
        Assert.True("42 ".ParseIntOrNone().TryGetValue(out var trailing));
        Assert.Equal(42, trailing);
        Assert.True(" 42 ".ParseIntOrNone().TryGetValue(out var surrounding));
        Assert.Equal(42, surrounding);
    }

    [Fact]
    public void GenericParseOrNoneDelegatesToTheTypesOwnTryParse()
    {
        Assert.True("widget".ParseOrNone<Widget>().TryGetValue(out var widget));
        Assert.Equal(new Widget(7), widget);
        Assert.True("widget".ParseOrNone<Widget>(CultureInfo.InvariantCulture).TryGetValue(out var invariantWidget));
        Assert.Equal(new Widget(7), invariantWidget);
        Assert.True("anything-else".ParseOrNone<Widget>().IsNone);
    }

    [Fact]
    public void GenericParseOrNonePropagatesExceptionsThrownByTheTypesOwnTryParse()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => "anything".ParseOrNone<ThrowingWidget>());

        Assert.Equal("Custom TryParse implementations may throw; the bridge must not swallow them.", exception.Message);
    }

    private readonly record struct Widget(int Value) : IParsable<Widget>
    {
        public static Widget Parse(string s, IFormatProvider? provider) =>
            throw new InvalidOperationException("The generic bridge must delegate to TryParse instead of Parse.");

        public static bool TryParse(string? s, IFormatProvider? provider, out Widget result)
        {
            if (s == "widget")
            {
                result = new Widget(7);
                return true;
            }

            result = default;
            return false;
        }
    }

    private readonly record struct ThrowingWidget : IParsable<ThrowingWidget>
    {
        public static ThrowingWidget Parse(string s, IFormatProvider? provider) =>
            throw new InvalidOperationException("The generic bridge must delegate to TryParse instead of Parse.");

        public static bool TryParse(string? s, IFormatProvider? provider, out ThrowingWidget result) =>
            throw new InvalidOperationException("Custom TryParse implementations may throw; the bridge must not swallow them.");
    }
}
