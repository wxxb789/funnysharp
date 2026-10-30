#pragma warning disable FS0017

using System.Diagnostics.CodeAnalysis;
using System.Reflection;

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
    public void DecodeErrorCreateReturnsTheRootLocationAsARelativeError()
    {
        var error = DecodeError.Create(DecodeErrorKind.Missing);

        Assert.Same(Location.Root, error.Location);
        Assert.Equal(string.Empty, error.Path);
    }

    [Fact]
    public void DecodeErrorLocationCarriesTheTrackedExperimentalDiagnostic()
    {
        var property = typeof(DecodeError).GetProperty("Location");
        Assert.NotNull(property);

        var attribute = property!.GetCustomAttribute<ExperimentalAttribute>();

        Assert.NotNull(attribute);
        Assert.Equal("FS0017", attribute!.DiagnosticId);
    }
}
