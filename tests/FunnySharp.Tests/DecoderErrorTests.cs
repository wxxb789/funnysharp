namespace FunnySharp.Tests;

public sealed class DecoderErrorTests
{
    [Fact]
    public void DecodeErrorCreateBuildsARelativeErrorAndRejectsAnUndefinedKind()
    {
        var error = DecodeError.Create(
            DecodeErrorKind.InvalidValue,
            detail: "detail");

        Assert.Equal(string.Empty, error.Path);
        Assert.Equal(DecodeErrorKind.InvalidValue, error.Kind);
        Assert.Equal("detail", error.Detail);
        Assert.Equal("input: detail", error.ToString());

        Assert.Throws<ArgumentOutOfRangeException>(
            () => DecodeError.Create((DecodeErrorKind)99));
    }

    [Fact]
    public void DecodeErrorsCompareByLocationKindExpectedAndDetail()
    {
        var first = DecoderTestData.Errors(
            DecoderTestData.Decode(
                DecoderTestData.OrderDecoder,
                DecoderTestData.InvalidOrderJson));
        var second = DecoderTestData.Errors(
            DecoderTestData.Decode(
                DecoderTestData.OrderDecoder,
                DecoderTestData.InvalidOrderJson));

        Assert.Equal(first, second);
        for (var index = 0; index < first.Count; index++)
        {
            Assert.Equal(first[index], second[index]);
            Assert.Equal(first[index].GetHashCode(), second[index].GetHashCode());
        }

        Assert.NotEqual(first[0], first[1]);

        var set = new HashSet<DecodeError> { first[0] };
        Assert.Contains(second[0], set);
        Assert.False(set.Add(second[0]));
    }

    [Fact]
    public void DecodeErrorToStringIsDeterministicAndUsesInputForTheRoot()
    {
        var field = Decoder.Field("id", Decoder.Guid);

        Assert.Equal(
            "id: required value is missing",
            Assert.Single(
                DecoderTestData.Errors(DecoderTestData.Decode(field, "{}"))).ToString());

        Assert.Equal(
            "id: value must not be null; expected a GUID string",
            Assert.Single(
                DecoderTestData.Errors(
                    DecoderTestData.Decode(field, """{"id":null}"""))).ToString());

        Assert.Equal(
            "id: expected a GUID string (found Number)",
            Assert.Single(
                DecoderTestData.Errors(
                    DecoderTestData.Decode(field, """{"id":42}"""))).ToString());

        Assert.Equal(
            "id: expected a GUID string (the value cannot be parsed as a GUID string)",
            Assert.Single(
                DecoderTestData.Errors(
                    DecoderTestData.Decode(field, """{"id":"bad"}"""))).ToString());

        Assert.Equal(
            "input: expected a non-empty string",
            Assert.Single(
                DecoderTestData.Errors(
                    DecoderTestData.Decode(Decoder.NonEmptyString, "\"\""))).ToString());

        var error = Assert.Single(
            DecoderTestData.Errors(
                DecoderTestData.Decode(
                    Decoder.Object(
                        Decoder.Field("a", Decoder.Int32),
                        Decoder.Field("b", Decoder.Int32),
                        static (a, b) => a + b),
                    "[]")));
        Assert.Equal("input: expected an object (found Array)", error.ToString());
    }

    [Fact]
    public void DecodeErrorPathIsTheStableRenderingOfItsLocation()
    {
        var error = Assert.Single(
            DecoderTestData.Errors(
                DecoderTestData.Decode(
                    DecoderTestData.LineDecoder,
                    """{"sku":"","quantity":1}""")));

        Assert.Equal("sku", error.Path);
        Assert.Equal("sku: expected a non-empty string", error.ToString());
    }
}
