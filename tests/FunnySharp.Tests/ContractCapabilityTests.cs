using System.Text.Json;
using FunnySharp.TestSupport.Contracts;

namespace FunnySharp.Tests;

public sealed class ContractCapabilityTests
{
    [Fact]
    public void OpaqueContractReportsEveryMissingDerivationAndEveryFailureIsExplicit()
    {
        var contract = OpaqueEvenInt();
        var capabilities = contract.Capabilities;

        Assert.False(capabilities.CanDecode);
        Assert.False(capabilities.CanGenerate);
        Assert.False(capabilities.CanShrink);
        Assert.False(capabilities.HasCompleteSchema);
        Assert.Equal(4, capabilities.Limitations.Count);
        Assert.Equal(
            new[]
            {
                ContractDerivation.Decoding,
                ContractDerivation.Generation,
                ContractDerivation.Shrinking,
                ContractDerivation.Schema,
            },
            capabilities.Limitations.Select(static item => item.Derivation).ToArray());
        Assert.All(capabilities.Limitations, static item => Assert.Equal(string.Empty, item.Path));
        Assert.Contains(capabilities.Limitations, static item => item.Reason.Contains("WithDecoder", StringComparison.Ordinal));
        Assert.Contains(capabilities.Limitations, static item => item.Reason.Contains("WithGenerator", StringComparison.Ordinal));
        Assert.Contains(capabilities.Limitations, static item => item.Reason.Contains("WithShrinker", StringComparison.Ordinal));
        Assert.Contains(capabilities.Limitations, static item => item.Reason.Contains("WithSchema", StringComparison.Ordinal));

        var decoder = contract.CreateDecoder();
        AssertDerivation(decoder, ContractDerivation.Decoding);

        var generated = contract.Generate(new ContractGenerationOptions(seed: 1));
        AssertDerivation(generated, ContractDerivation.Generation);

        var shrunk = contract.Shrink(2);
        AssertDerivation(shrunk, ContractDerivation.Shrinking);

        Assert.Equal(ContractDescriptionKind.Opaque, contract.Description.Kind);
        Assert.Equal("even integer", contract.Description.Detail);
        var explanation = contract.Explain();
        Assert.Contains("decoding: unavailable", explanation, StringComparison.Ordinal);
        Assert.Contains("schema: incomplete", explanation, StringComparison.Ordinal);
        Assert.Contains("with WithGenerator", explanation, StringComparison.Ordinal);
    }

    [Fact]
    public void CallerSuppliedDerivationsRestoreEveryCapability()
    {
        var contract = OpaqueEvenInt()
            .WithDecoder(Decoder.Int32)
            .WithGenerator(static random => random.Next(0, 10) * 2)
            .WithShrinker(static value => value >= 2 ? [value - 2] : [])
            .WithSchema(ContractDescription.Scalar("even integer"));

        var capabilities = contract.Capabilities;
        Assert.True(capabilities.CanDecode);
        Assert.True(capabilities.CanGenerate);
        Assert.True(capabilities.CanShrink);
        Assert.True(capabilities.HasCompleteSchema);
        Assert.Empty(capabilities.Limitations);
        Assert.Equal(ContractDescriptionKind.Scalar, contract.Description.Kind);

        using var valid = JsonDocument.Parse("4");
        Assert.True(contract.CreateDecoder().TryGetValue(out var decoder));
        Assert.True(decoder!.Decode(valid.RootElement).TryGetValue(out var decoded));
        Assert.Equal(4, decoded);

        using var invalid = JsonDocument.Parse("3");
        Assert.True(decoder.Decode(invalid.RootElement).TryGetErrors(out var errors));
        Assert.Single(errors!);
        Assert.Equal("value must be an even integer", errors[0].Detail);

        Assert.True(contract.Generate(new ContractGenerationOptions(seed: 1)).TryGetValue(out var generated));
        Assert.Equal(0, generated % 2);

        Assert.True(contract.Shrink(4).TryGetValue(out var shrunk));
        Assert.Equal([2], shrunk!.ToArray());
    }

    [Fact]
    public void RefinementKeepsValidationDecodingAndShrinkingButNotGenerationOrSchema()
    {
        var contract = Contract.Int32Range(0, 10).Ensure(
            static value => value % 2 == 0,
            "value must be even");

        var capabilities = contract.Capabilities;
        Assert.True(capabilities.CanDecode);
        Assert.True(capabilities.CanShrink);
        Assert.False(capabilities.CanGenerate);
        Assert.False(capabilities.HasCompleteSchema);
        Assert.Equal(
            new[]
            {
                ContractDerivation.Generation,
                ContractDerivation.Schema,
            },
            capabilities.Limitations.Select(static item => item.Derivation).ToArray());

        using var document = JsonDocument.Parse("3");
        Assert.True(contract.CreateDecoder().TryGetValue(out var decoder));
        Assert.True(decoder!.Decode(document.RootElement).TryGetErrors(out var errors));
        Assert.Equal("value must be even", Assert.Single(errors!).Detail);

        var shrunk = contract.Shrink(8);
        Assert.True(shrunk.TryGetValue(out var candidates));
        Assert.All(candidates!, static candidate => Assert.Equal(0, candidate % 2));
    }

    [Fact]
    public void NestedLimitationsNameTheirContractPath()
    {
        var contract = Contract.Object(
            Contract.Field("email", OpaqueEvenInt(), static (Envelope value) => value.Email),
            static email => new Envelope(email));

        var capabilities = contract.Capabilities;
        Assert.False(capabilities.CanGenerate);
        Assert.False(capabilities.HasCompleteSchema);
        Assert.Contains(
            capabilities.Limitations,
            static item => item.Derivation == ContractDerivation.Generation
                && item.Path == "email");
        Assert.Contains(
            capabilities.Limitations,
            static item => item.Derivation == ContractDerivation.Schema
                && item.Path == "email");

        var generated = contract.Generate(new ContractGenerationOptions(seed: 1));
        var limitation = Assert.IsType<ContractLimitation>(
            generated.TryGetError(out var error) ? error : null);
        Assert.Equal("email", limitation.Path);
    }

    [Fact]
    public void OneOfWithoutAnInputSelectorReportsDecodingAsUnavailable()
    {
        var contract = Contract.OneOf<int>(
            static value => value,
            selectInput: null,
            discriminator: null,
            Contract.Alternative("zero", Contract.Int32.Ensure(static value => value == 0, "must be zero")),
            Contract.Alternative("other", Contract.Int32.Ensure(static value => value != 0, "must not be zero")));

        var capabilities = contract.Capabilities;
        Assert.False(capabilities.CanDecode);
        Assert.Contains(
            capabilities.Limitations,
            static item => item.Derivation == ContractDerivation.Decoding
                && item.Reason.Contains("input selector", StringComparison.Ordinal));

        var decoder = contract.CreateDecoder();
        var limitation = Assert.IsType<ContractLimitation>(
            decoder.TryGetError(out var error) ? error : null);
        Assert.Equal(ContractDerivation.Decoding, limitation.Derivation);
        Assert.Equal(string.Empty, limitation.Path);
    }

    [Fact]
    public void RecursiveSchemaIsIncompleteWhileGenerationRemainsAvailable()
    {
        Contract<Category>? category = null;
        category = Contract.Defer(
            "category",
            () => Contract.Object(
                Contract.Field("name", Contract.NonEmptyString, static (Category value) => value.Name),
                Contract.Field(
                    "children",
                    Contract.List(category!, 0, 2),
                    static (Category value) => value.Children),
                static (name, children) => new Category(name, children)));

        var capabilities = category.Capabilities;
        Assert.True(capabilities.CanDecode);
        Assert.True(capabilities.CanGenerate);
        Assert.True(capabilities.CanShrink);
        Assert.False(capabilities.HasCompleteSchema);
        Assert.Contains(
            capabilities.Limitations,
            static item => item.Derivation == ContractDerivation.Schema
                && item.Reason.Contains("recursive contract", StringComparison.Ordinal));

        var generated = category.Generate(new ContractGenerationOptions(seed: 4, maxDepth: 2));
        Assert.True(generated.TryGetValue(out var value));
        Assert.True(category.Validate(value!).IsValid);
    }

    private static Contract<int> OpaqueEvenInt() =>
        Contract.From<int>(
            "even integer",
            static value => value % 2 == 0
                ? Validation<int, string>.Valid(value)
                : Validation<int, string>.Invalid("value must be an even integer"));

    private static void AssertDerivation<T>(
        Result<T, ContractLimitation> result,
        ContractDerivation derivation)
    {
        var limitation = Assert.IsType<ContractLimitation>(
            result.TryGetError(out var error) ? error : null);
        Assert.Equal(derivation, limitation.Derivation);
    }

    private sealed record Envelope(int Email);
}
