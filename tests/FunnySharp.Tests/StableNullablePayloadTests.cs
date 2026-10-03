namespace FunnySharp.Tests;

public sealed class StableNullablePayloadTests
{
    [Fact]
    public async Task NullableValidationAndEffectValuesArePreserved()
    {
        var validation = Validation<string?, string?>.Valid(null);
        Assert.True(validation.TryGetValue(out var value));
        Assert.Null(value);

        var invalid = Validation<int, string?>.InvalidMany(new string?[] { null, "second", null });
        Assert.True(invalid.TryGetErrors(out var errors));
        Assert.Equal(new string?[] { null, "second", null }, errors);

        Assert.Null(await Effect.FromValue<string?>(null).RunAsync(TestContext.Current.CancellationToken));
        Assert.Null(await Effect.FromValue(1).Map<string?>(_ => null)
            .RunAsync(TestContext.Current.CancellationToken));
    }
}
