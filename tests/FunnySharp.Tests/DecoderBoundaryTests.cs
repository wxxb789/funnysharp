using System.Text.Json;

namespace FunnySharp.Tests;

public sealed class DecoderBoundaryTests
{
    [Fact]
    public void PrimitiveDecodersDistinguishMalformedWrongKindAndNull()
    {
        Assert.Equal(42, DecoderTestData.Value(DecoderTestData.Decode(Decoder.Int32, "42")));

        var fractional = SingleError(Decoder.Int32, "1.5");
        Assert.Equal(DecodeErrorKind.Malformed, fractional.Kind);
        Assert.Equal("a 32-bit integer", fractional.Expected);

        var outOfRange = SingleError(Decoder.Int32, "2147483648");
        Assert.Equal(DecodeErrorKind.Malformed, outOfRange.Kind);

        var numericText = SingleError(Decoder.Int32, "\"42\"");
        Assert.Equal(DecodeErrorKind.WrongKind, numericText.Kind);
        Assert.Equal("found String", numericText.Detail);

        var nullNumber = SingleError(Decoder.Int32, "null");
        Assert.Equal(DecodeErrorKind.Null, nullNumber.Kind);

        Assert.Equal(42L, DecoderTestData.Value(DecoderTestData.Decode(Decoder.Int64, "42")));
        Assert.Equal(
            DecodeErrorKind.Malformed,
            SingleError(Decoder.Int64, "9223372036854775808").Kind);

        Assert.Equal(
            1.5m,
            DecoderTestData.Value(DecoderTestData.Decode(Decoder.Decimal, "1.5")));
        Assert.Equal(
            DecodeErrorKind.WrongKind,
            SingleError(Decoder.Decimal, "\"1.5\"").Kind);

        Assert.Equal(
            1.5d,
            DecoderTestData.Value(DecoderTestData.Decode(Decoder.Double, "1.5")));
        Assert.Equal(
            DecodeErrorKind.WrongKind,
            SingleError(Decoder.Double, "\"1.5\"").Kind);

        Assert.True(DecoderTestData.Value(DecoderTestData.Decode(Decoder.Boolean, "true")));
        Assert.Equal(
            DecodeErrorKind.WrongKind,
            SingleError(Decoder.Boolean, "\"true\"").Kind);
        Assert.Equal(
            DecodeErrorKind.Null,
            SingleError(Decoder.Boolean, "null").Kind);
    }

    [Fact]
    public void GuidAndDateDecodersReportMalformedTextSeparatelyFromWrongKinds()
    {
        var guid = Guid.Parse("0f8fad5b-d9cb-469f-a165-70867728950e");

        Assert.Equal(
            guid,
            DecoderTestData.Value(
                DecoderTestData.Decode(
                    Decoder.Guid,
                    "\"0f8fad5b-d9cb-469f-a165-70867728950e\"")));

        var malformedGuid = SingleError(Decoder.Guid, "\"not-a-guid\"");
        Assert.Equal(DecodeErrorKind.Malformed, malformedGuid.Kind);
        Assert.Equal("a GUID string", malformedGuid.Expected);

        var numericGuid = SingleError(Decoder.Guid, "42");
        Assert.Equal(DecodeErrorKind.WrongKind, numericGuid.Kind);

        Assert.Equal(
            DateTime.Parse(
                "2026-10-30T00:00:00Z",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.RoundtripKind),
            DecoderTestData.Value(
                DecoderTestData.Decode(
                    Decoder.DateTime,
                    "\"2026-10-30T00:00:00Z\"")));

        Assert.Equal(
            DecodeErrorKind.Malformed,
            SingleError(Decoder.DateTime, "\"not-a-date\"").Kind);

        Assert.Equal(
            DateTimeOffset.Parse(
                "2026-10-30T00:00:00Z",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.RoundtripKind),
            DecoderTestData.Value(
                DecoderTestData.Decode(
                    Decoder.DateTimeOffset,
                    "\"2026-10-30T00:00:00Z\"")));

        Assert.Equal(
            DecodeErrorKind.Malformed,
            SingleError(Decoder.DateTimeOffset, "\"not-a-date\"").Kind);
    }

    [Fact]
    public void StringDecodersAcceptEmptyOnlyWhenTheNameSaysSo()
    {
        Assert.Equal(
            string.Empty,
            DecoderTestData.Value(DecoderTestData.Decode(Decoder.String, "\"\"")));

        var empty = SingleError(Decoder.NonEmptyString, "\"\"");
        Assert.Equal(DecodeErrorKind.Empty, empty.Kind);
        Assert.Equal("a non-empty string", empty.Expected);

        Assert.Equal(
            DecodeErrorKind.Null,
            SingleError(Decoder.NonEmptyString, "null").Kind);

        Assert.Equal(
            DecodeErrorKind.WrongKind,
            SingleError(Decoder.String, "42").Kind);
    }

    [Fact]
    public void StructuralDecodersReportNullAndWrongKindWithoutThrowing()
    {
        var field = Decoder.Field("x", Decoder.Int32);

        var fieldNull = SingleError(field, "null");
        Assert.Equal(DecodeErrorKind.Null, fieldNull.Kind);
        Assert.Equal(string.Empty, fieldNull.Path);

        var fieldArray = SingleError(field, "[]");
        Assert.Equal(DecodeErrorKind.WrongKind, fieldArray.Kind);
        Assert.Equal("found Array", fieldArray.Detail);

        var optionalField = Decoder.OptionalField("x", Decoder.Int32);
        Assert.Equal(
            DecodeErrorKind.Null,
            SingleError(optionalField, "null").Kind);

        var nullableField = Decoder.NullableField("x", Decoder.Int32);
        Assert.Equal(
            DecodeErrorKind.Null,
            SingleError(nullableField, "null").Kind);

        var list = Decoder.List(Decoder.Int32);
        Assert.Equal(DecodeErrorKind.Null, SingleError(list, "null").Kind);
        Assert.Equal(DecodeErrorKind.WrongKind, SingleError(list, "{}").Kind);

        var nonEmptyList = Decoder.NonEmptyList(Decoder.Int32);
        Assert.Equal(DecodeErrorKind.Null, SingleError(nonEmptyList, "null").Kind);
        Assert.Equal(DecodeErrorKind.Empty, SingleError(nonEmptyList, "[]").Kind);

        var objectDecoder = Decoder.Object(
            Decoder.Field("a", Decoder.Int32),
            Decoder.Field("b", Decoder.Int32),
            static (a, b) => a + b);
        Assert.Equal(DecodeErrorKind.Null, SingleError(objectDecoder, "null").Kind);
        Assert.Equal(DecodeErrorKind.WrongKind, SingleError(objectDecoder, "[]").Kind);
    }

    [Fact]
    public void DefaultJsonElementIsAWrongKindRatherThanAnException()
    {
        Assert.Equal(
            DecodeErrorKind.WrongKind,
            SingleDefaultError(Decoder.String).Kind);
        Assert.Equal(
            DecodeErrorKind.WrongKind,
            SingleDefaultError(Decoder.Int32).Kind);
        Assert.Equal(
            DecodeErrorKind.WrongKind,
            SingleDefaultError(Decoder.List(Decoder.Int32)).Kind);
        Assert.Equal(
            DecodeErrorKind.WrongKind,
            SingleDefaultError(Decoder.Field("x", Decoder.String)).Kind);
        Assert.Equal(
            DecodeErrorKind.WrongKind,
            SingleDefaultError(
                Decoder.Object(
                    Decoder.Field("a", Decoder.Int32),
                    Decoder.Field("b", Decoder.Int32),
                    static (a, b) => a + b)).Kind);
        Assert.Equal(
            DecodeErrorKind.WrongKind,
            SingleDefaultError(Decoder.Element).Kind);
        Assert.Equal(
            "found Undefined",
            SingleDefaultError(Decoder.Element).Detail);
    }

    [Fact]
    public void ElementDecoderReturnsRawNullAndValues()
    {
        using var nullDocument = JsonDocument.Parse("null");
        var nullElement = DecoderTestData.Value(
            Decoder.Element.Decode(nullDocument.RootElement));
        Assert.Equal(JsonValueKind.Null, nullElement.ValueKind);

        using var objectDocument = JsonDocument.Parse("""{"a":1}""");
        var objectElement = DecoderTestData.Value(
            Decoder.Element.Decode(objectDocument.RootElement));
        Assert.Equal(JsonValueKind.Object, objectElement.ValueKind);
    }

    [Fact]
    public void OptionalValueDoesNotTreatDefaultAsAbsence()
    {
        var decoder = Decoder.OptionalValue(Decoder.Int32);

        Assert.Equal(DecodeErrorKind.WrongKind, SingleDefaultError(decoder).Kind);
        Assert.True(
            DecoderTestData.Value(DecoderTestData.Decode(decoder, "null")).IsNone);
    }

    private static DecodeError SingleError<T>(Decoder<T> decoder, string json) =>
        Assert.Single(DecoderTestData.Errors(DecoderTestData.Decode(decoder, json)));

    private static DecodeError SingleDefaultError<T>(Decoder<T> decoder) =>
        Assert.Single(DecoderTestData.Errors(decoder.Decode(default)));
}
