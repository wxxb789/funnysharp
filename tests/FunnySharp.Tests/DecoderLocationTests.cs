#pragma warning disable FS0017

namespace FunnySharp.Tests;

public sealed class DecoderLocationTests
{
    [Fact]
    public void DecodeErrorLocationComposesTheSameSegmentsAsThePath()
    {
        var errors = DecoderTestData.Errors(
            DecoderTestData.Decode(
                DecoderTestData.OrderDecoder,
                DecoderTestData.InvalidOrderJson));

        var sku = errors.Single(static error => error.Path == "lines[1].sku");
        Assert.Equal("lines[1].sku", sku.Location.ToString());
        Assert.Equal(
            Location.Root.Property("lines").At(1).Property("sku"),
            sku.Location);

        var note = errors.Single(static error => error.Path == "note");
        Assert.Equal(Location.Root.Property("note"), note.Location);
    }

    [Fact]
    public void NestedFieldsWithTheSameNameKeepEverySegment()
    {
        var decoder = Decoder.Field("a", Decoder.Field("a", Decoder.Int32));

        var error = Assert.Single(
            DecoderTestData.Errors(
                DecoderTestData.Decode(decoder, """{"a":{"a":null}}""")));

        Assert.Equal("a.a", error.Path);
        Assert.Equal(DecodeErrorKind.Null, error.Kind);
    }

    [Fact]
    public void NestedListsWithTheSameIndexKeepEverySegment()
    {
        var decoder = Decoder.List(Decoder.List(Decoder.Int32));

        var error = Assert.Single(
            DecoderTestData.Errors(DecoderTestData.Decode(decoder, "[[null]]")));

        Assert.Equal("[0][0]", error.Path);
        Assert.Equal(DecodeErrorKind.Null, error.Kind);
    }

    [Fact]
    public void PropertyNamesContainingDotsRenderAsQuotedKeysSoDottedAndNestedFieldsStayDistinct()
    {
        var dotted = Assert.Single(
            DecoderTestData.Errors(
                DecoderTestData.Decode(
                    Decoder.Field("a.b", Decoder.Int32),
                    """{"a.b":null}""")));
        var nested = Assert.Single(
            DecoderTestData.Errors(
                DecoderTestData.Decode(
                    Decoder.Field("a", Decoder.Field("b", Decoder.Int32)),
                    """{"a":{"b":null}}""")));

        Assert.Equal("[\"a.b\"]", dotted.Path);
        Assert.Equal("a.b", nested.Path);
        Assert.NotEqual(dotted.Path, nested.Path);
        Assert.NotEqual(dotted.Location, nested.Location);
    }

    [Fact]
    public void PropertyNamesContainingBracketsRenderAsQuotedKeysSoNamedAndIndexedSegmentsStayDistinct()
    {
        var named = Assert.Single(
            DecoderTestData.Errors(
                DecoderTestData.Decode(
                    Decoder.Field("a[0]", Decoder.Int32),
                    """{"a[0]":null}""")));
        var indexed = Assert.Single(
            DecoderTestData.Errors(
                DecoderTestData.Decode(
                    Decoder.Field("a", Decoder.List(Decoder.Int32)),
                    """{"a":[null]}""")));

        Assert.Equal("[\"a[0]\"]", named.Path);
        Assert.Equal("a[0]", indexed.Path);
        Assert.NotEqual(named.Path, indexed.Path);
    }

    [Fact]
    public void PropertyNamesWithoutPathSeparatorsKeepThePlainRendering()
    {
        var error = Assert.Single(
            DecoderTestData.Errors(
                DecoderTestData.Decode(
                    Decoder.Field("postalCode", Decoder.Int32),
                    """{"postalCode":null}""")));

        Assert.Equal("postalCode", error.Path);
        Assert.Equal("postalCode", error.Location.ToString());
    }

    [Fact]
    public void DecodeErrorCreateReturnsTheRootLocationAsARelativeError()
    {
        var error = DecodeError.Create(DecodeErrorKind.Missing);

        Assert.Equal(Location.Root, error.Location);
        Assert.Equal(string.Empty, error.Path);
    }
}
