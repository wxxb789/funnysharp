using System.Text.Json;

namespace FunnySharp.Tests;

public sealed class DecoderExceptionTests
{
    [Fact]
    public void NullFactoryAndCombinatorArgumentsAreRejectedEagerly()
    {
        Assert.Throws<ArgumentNullException>(() => Decoder.From<int>(null!));
        Assert.Throws<ArgumentNullException>(() => Decoder.Field(null!, Decoder.String));
        Assert.Throws<ArgumentException>(() => Decoder.Field("", Decoder.String));
        Assert.Throws<ArgumentNullException>(() => Decoder.Field<int>("x", null!));
        Assert.Throws<ArgumentNullException>(() => Decoder.OptionalField(null!, Decoder.String));
        Assert.Throws<ArgumentException>(() => Decoder.OptionalField("", Decoder.String));
        Assert.Throws<ArgumentNullException>(() => Decoder.OptionalField<int>("x", null!));
        Assert.Throws<ArgumentNullException>(() => Decoder.NullableField(null!, Decoder.String));
        Assert.Throws<ArgumentException>(() => Decoder.NullableField("", Decoder.String));
        Assert.Throws<ArgumentNullException>(() => Decoder.NullableField<int>("x", null!));
        Assert.Throws<ArgumentNullException>(() => Decoder.OptionalValue<int>(null!));
        Assert.Throws<ArgumentNullException>(() => Decoder.List<int>(null!));
        Assert.Throws<ArgumentNullException>(() => Decoder.NonEmptyList<int>(null!));

        var ints = Decoder.Int32;
        var strings = Decoder.String;
        Assert.Throws<ArgumentNullException>(
            () => Decoder.Object<int, string, string>(
                null!,
                strings,
                static (value, text) => text));
        Assert.Throws<ArgumentNullException>(
            () => Decoder.Object<int, string, string>(
                ints,
                null!,
                static (value, text) => text));
        Assert.Throws<ArgumentNullException>(
            () => Decoder.Object<int, string, string>(
                ints,
                strings,
                null!));
        Assert.Throws<ArgumentNullException>(
            () => Decoder.Object<int, string, int, string>(
                ints,
                strings,
                null!,
                static (a, b, c) => b));
        Assert.Throws<ArgumentNullException>(
            () => Decoder.Object<int, string, int, string, string>(
                null!,
                strings,
                ints,
                strings,
                static (a, b, c, d) => b + d));

        Assert.Throws<ArgumentNullException>(() => Decoder.Int32.Map<string>(null!));
        Assert.Throws<ArgumentNullException>(() => Decoder.Int32.Bind<string>(null!));
        Assert.Throws<ArgumentNullException>(() => Decoder.Int32.Ensure(null!, "x"));
        Assert.Throws<ArgumentNullException>(() => Decoder.Int32.Ensure(static _ => true, null!));
        Assert.Throws<ArgumentException>(() => Decoder.Int32.Ensure(static _ => true, ""));
        Assert.Throws<ArgumentNullException>(() => Decoder.Int32.Validate(null!));
    }

    [Fact]
    public void ThrowingCallbacksPropagateUnchanged()
    {
        var expected = new InvalidOperationException("boom");

        var map = Decoder.Int32.Map<int>(_ => throw expected);
        Assert.Same(
            expected,
            Assert.Throws<InvalidOperationException>(
                () => DecoderTestData.Decode(map, "1")));

        var bind = Decoder.Int32.Bind<int>(_ => throw expected);
        Assert.Same(
            expected,
            Assert.Throws<InvalidOperationException>(
                () => DecoderTestData.Decode(bind, "1")));

        var ensure = Decoder.Int32.Ensure(_ => throw expected, "must pass");
        Assert.Same(
            expected,
            Assert.Throws<InvalidOperationException>(
                () => DecoderTestData.Decode(ensure, "1")));

        var validate = Decoder.Int32.Validate(_ => throw expected);
        Assert.Same(
            expected,
            Assert.Throws<InvalidOperationException>(
                () => DecoderTestData.Decode(validate, "1")));

        var from = Decoder.From<int>(_ => throw expected);
        Assert.Same(
            expected,
            Assert.Throws<InvalidOperationException>(
                () => DecoderTestData.Decode(from, "1")));

        var combine = Decoder.Object<int, int, int>(
            Decoder.Field("a", Decoder.Int32),
            Decoder.Field("b", Decoder.Int32),
            (int a, int b) => throw expected);
        Assert.Same(
            expected,
            Assert.Throws<InvalidOperationException>(
                () => DecoderTestData.Decode(combine, """{"a":1,"b":2}""")));
    }

    [Fact]
    public void DisposedDocumentFaultsPropagateUnchanged()
    {
        var document = JsonDocument.Parse("{}");
        var root = document.RootElement;
        document.Dispose();

        Assert.Throws<ObjectDisposedException>(() => Decoder.String.Decode(root));
        Assert.Throws<ObjectDisposedException>(() => DecoderTestData.OrderDecoder.Decode(root));
    }

    [Fact]
    public void BindNullResultIsAProgrammingError()
    {
        var decoder = Decoder.Int32.Bind<int>(_ => null!);

        var actual = Assert.Throws<InvalidOperationException>(
            () => DecoderTestData.Decode(decoder, "1"));

        Assert.Equal("The decoder binder returned null.", actual.Message);
    }

    [Fact]
    public void SuccessfulNullDoesNotBecomeOptionNone()
    {
        var nullDecoder = Decoder.From<string?>(
            _ => Validation<string?, DecodeError>.Valid(null));
        var optional = Decoder.OptionalField("note", nullDecoder);

        Assert.Throws<ArgumentNullException>(
            () => DecoderTestData.Decode(optional, """{"note":"x"}"""));
    }
}
