using System.Text.Json;
using FunnySharp.TestSupport.Domains;

namespace FunnySharp.Tests;

public sealed class DomainValueTests
{
    private readonly struct QuantityTag;

    private readonly struct SkuTag;

    private readonly struct NotesTag;

    private static readonly Domain<int, QuantityTag> Quantity =
        Domain.For<int, QuantityTag>(Contract.Int32Range(1, 100), "quantity");

    private static readonly Domain<string, SkuTag> Sku =
        Domain.For<string, SkuTag>(
            Contract.NonEmptyString.Ensure(
                static value => value.StartsWith("SKU-", StringComparison.Ordinal),
                "value must start with SKU-"),
            "SKU");

    [Fact]
    public void DefaultAndNewValuesAreUninitializedAndThrowOnAccess()
    {
        var defaultValue = default(DomainValue<int, QuantityTag>);
        var newValue = new DomainValue<int, QuantityTag>();

        Assert.False(defaultValue.IsInitialized);
        Assert.False(newValue.IsInitialized);
        Assert.Equal(defaultValue, newValue);
        Assert.Equal(0, defaultValue.GetHashCode());
        Assert.Contains("uninitialized", defaultValue.ToString(), StringComparison.Ordinal);
        Assert.Contains(nameof(QuantityTag), defaultValue.ToString(), StringComparison.Ordinal);
        Assert.False(defaultValue.TryGetValue(out var missing));
        Assert.Equal(0, missing);
        Assert.Throws<InvalidOperationException>(() => defaultValue.Value);
    }

    [Fact]
    public void ValidateWrapsOnlyValuesThatSatisfyTheContract()
    {
        var valid = Quantity.Validate(42);
        Assert.True(valid.TryGetValue(out var wrapped));
        Assert.True(wrapped.TryGetValue(out var quantity));
        Assert.Equal(42, quantity);
        Assert.Equal("42", wrapped.ToString());
        Assert.Equal(wrapped, Quantity.Validate(42).Match(
            static value => value,
            static _ => throw new InvalidOperationException("Expected a valid quantity.")));

        var invalid = Quantity.Validate(0);
        Assert.True(invalid.TryGetErrors(out var errors));
        var error = Assert.Single(errors!);
        Assert.Equal(DecodeErrorKind.InvalidValue, error.Kind);
        Assert.Equal("value must be between 1 and 100", error.Detail);
    }

    [Fact]
    public void ValidateTreatsRuntimeNullAsANullFailure()
    {
        var result = Sku.Validate(null!);

        Assert.True(result.TryGetErrors(out var errors));
        var error = Assert.Single(errors!);
        Assert.Equal(DecodeErrorKind.Null, error.Kind);
    }

    [Fact]
    public void CreateDecoderPreservesUnderlyingKindsAndLocations()
    {
        var created = Quantity.CreateDecoder();
        Assert.True(created.TryGetValue(out var decoder));

        using var validDocument = JsonDocument.Parse("42");
        Assert.True(decoder!.Decode(validDocument.RootElement).TryGetValue(out var decoded));
        Assert.Equal(42, decoded.Value);

        using var malformedDocument = JsonDocument.Parse("1.5");
        var malformed = decoder.Decode(malformedDocument.RootElement);
        Assert.True(malformed.TryGetErrors(out var malformedErrors));
        Assert.Equal(DecodeErrorKind.Malformed, Assert.Single(malformedErrors!).Kind);

        using var invalidDocument = JsonDocument.Parse("0");
        var invalid = decoder.Decode(invalidDocument.RootElement);
        Assert.True(invalid.TryGetErrors(out var invalidErrors));
        var error = Assert.Single(invalidErrors!);
        Assert.Equal(DecodeErrorKind.InvalidValue, error.Kind);
        Assert.Equal("value must be between 1 and 100", error.Detail);

        var fieldDecoder = Decoder.Field("quantity", decoder);
        using var nestedDocument = JsonDocument.Parse("""{"quantity":0}""");
        var nested = fieldDecoder.Decode(nestedDocument.RootElement);
        Assert.True(nested.TryGetErrors(out var nestedErrors));
        Assert.Equal("quantity", Assert.Single(nestedErrors!).Path);
    }

    [Fact]
    public void DerivedContractValidatesDecodesGeneratesAndShrinks()
    {
        var generated = Quantity.Contract.Generate(new ContractGenerationOptions(seed: 17));
        Assert.True(generated.TryGetValue(out var generatedValue));
        Assert.True(generatedValue.TryGetValue(out var generatedQuantity));
        Assert.InRange(generatedQuantity, 1, 100);

        var shrunk = Quantity.Contract.Shrink(generatedValue);
        Assert.True(shrunk.TryGetValue(out var candidates));
        Assert.NotEmpty(candidates!);
        Assert.All(candidates!, static candidate => Assert.True(candidate.IsInitialized));

        var defaultValidation = Quantity.Contract.Validate(default);
        Assert.True(defaultValidation.TryGetErrors(out var defaultErrors));
        var defaultError = Assert.Single(defaultErrors!);
        Assert.Equal(DecodeErrorKind.InvalidValue, defaultError.Kind);
        Assert.Contains("not a validated quantity", defaultError.Detail, StringComparison.Ordinal);

        var valid = Quantity.Validate(42);
        Assert.True(valid.TryGetValue(out var wrapped));
        Assert.True(Quantity.Contract.Validate(wrapped).IsValid);

        var created = Quantity.Contract.CreateDecoder();
        Assert.True(created.TryGetValue(out var decoder));
        using var document = JsonDocument.Parse("37");
        var decoded = decoder!.Decode(document.RootElement);
        Assert.True(decoded.TryGetValue(out var contractValue));
        Assert.Equal(37, contractValue.Value);
    }

    [Fact]
    public void MutablePayloadsRemainTheCallersResponsibility()
    {
        var domain = Domain.For<IReadOnlyList<int>, NotesTag>(
            Contract.NonEmptyList(Contract.Int32),
            "notes");

        var values = new List<int> { 1, 2 };
        var validation = domain.Validate(values);
        Assert.True(validation.TryGetValue(out var wrapped));
        Assert.True(wrapped.TryGetValue(out var payload));
        Assert.Same(values, payload);

        values.Clear();

        Assert.Same(values, wrapped.Value);
        Assert.True(domain.Contract.Validate(wrapped).TryGetErrors(out var errors));
        Assert.NotEmpty(errors!);
    }

    [Fact]
    public void DeserializingAConsumerRecordDoesNotForgeAValidDomainValue()
    {
        var item = JsonSerializer.Deserialize<DomainItem>(
            """{"Sku":{"Value":"SKU-9"}}""");

        Assert.NotNull(item);
        Assert.False(item.Sku.IsInitialized);
        Assert.False(item.Sku.TryGetValue(out _));
    }

    [Fact]
    public void FrameworkAlternativeConstructionCannotProduceAValidValue()
    {
        var fromJson = JsonSerializer.Deserialize<DomainValue<string, SkuTag>>(
            """{"Value":"SKU-1"}""");
        var fromActivator = Activator.CreateInstance<DomainValue<string, SkuTag>>();

        Assert.False(fromJson.IsInitialized);
        Assert.False(fromActivator.IsInitialized);
        Assert.False(fromJson.TryGetValue(out _));
        Assert.False(fromActivator.TryGetValue(out _));
        Assert.Throws<InvalidOperationException>(() => fromJson.Value);
    }

    [Fact]
    public void EqualityDistinguishesInitializationAndUnderlyingValues()
    {
        Assert.True(Sku.Validate("SKU-1").TryGetValue(out var first));
        Assert.True(Sku.Validate("SKU-1").TryGetValue(out var second));
        Assert.True(Sku.Validate("SKU-2").TryGetValue(out var third));

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.NotEqual(first, third);
        Assert.True(first == second);
        Assert.True(first != third);
        Assert.NotEqual(default, first);
        Assert.Equal(default, default(DomainValue<string, SkuTag>));
    }

    [Fact]
    public void BoundaryAndContractArgumentsAreValidatedEagerly()
    {
        Assert.Throws<ArgumentNullException>(() => Domain.For<int, QuantityTag>(null!, "quantity"));
        Assert.Throws<ArgumentException>(() => Domain.For<int, QuantityTag>(Contract.Int32, string.Empty));
        Assert.Throws<ArgumentException>(() => Domain.For<int, QuantityTag>(Contract.Int32, " "));
    }

    [Fact]
    public void UnavailableUnderlyingDecoderIsAnExplicitLimitation()
    {
        var opaque = Domain.For<int, QuantityTag>(
            Contract.From<int>(
                "opaque integer",
                static value => Validation<int, string>.Valid(value)),
            "opaque integer");

        Assert.True(opaque.Validate(1).IsValid);

        var created = opaque.CreateDecoder();
        Assert.True(created.TryGetError(out var limitation));
        Assert.Equal(ContractDerivation.Decoding, limitation!.Derivation);
        Assert.Equal(string.Empty, limitation.Path);
    }

    [Fact]
    public void HandWrittenWrapperAndDomainValueAgreeOnValidationAndDecoding()
    {
        foreach (var value in new[] { "ORD-100", "ORDER-100", string.Empty })
        {
            var domain = Domains.OrderId.Validate(value);
            var handWritten = HandWrittenOrderId.Create(value);

            Assert.Equal(domain.IsValid, handWritten.IsValid);
            Assert.Equal(ErrorShape(domain), ErrorShape(handWritten));
        }

        var domainNull = Domains.OrderId.Validate(null!);
        var handWrittenNull = HandWrittenOrderId.Create(null!);
        Assert.Equal(domainNull.IsValid, handWrittenNull.IsValid);
        Assert.Equal(ErrorShape(domainNull), ErrorShape(handWrittenNull));

        using var validDocument = JsonDocument.Parse("\"ORD-100\"");
        Assert.True(
            Domains.OrderIdDecoder.Decode(validDocument.RootElement).IsValid);
        Assert.True(
            HandWrittenOrderId.Decoder.Decode(validDocument.RootElement).IsValid);

        using var invalidDocument = JsonDocument.Parse("\"ORDER-100\"");
        var domainInvalid = Domains.OrderIdDecoder.Decode(invalidDocument.RootElement);
        var handWrittenInvalid = HandWrittenOrderId.Decoder.Decode(invalidDocument.RootElement);
        Assert.Equal(ErrorShape(domainInvalid), ErrorShape(handWrittenInvalid));
    }

    private static (string Kind, string Path, string Detail)[] ErrorShape<T>(
        Validation<T, DecodeError> validation)
    {
        validation.TryGetErrors(out var errors);
        return errors is null
            ? []
            : errors.Select(static error => (
                error.Kind.ToString(),
                error.Path,
                error.Detail ?? string.Empty)).ToArray();
    }

    [Fact]
    public void DerivedContractInteroperatesWithPatchSchema()
    {
        var field = Patch.Field<DomainItem, DomainValue<string, SkuTag>>(
            "sku",
            Sku.Contract,
            Lens.Create<DomainItem, DomainValue<string, SkuTag>>(
                static item => item.Sku,
                static (item, sku) => item with { Sku = sku }));
        var candidate = Contract.Object(
            Contract.For<DomainItem>().Field(
                "sku",
                Sku.Contract,
                static item => item.Sku),
            static sku => new DomainItem(sku));
        var schemaResult = PatchSchema<DomainItem>.Create(candidate, field);
        Assert.True(schemaResult.TryGetValue(out var schema));

        using var validDocument = JsonDocument.Parse("""{"sku":"SKU-2"}""");
        var valid = schema!.Decode(validDocument.RootElement);
        Assert.True(valid.TryGetValue(out var patch));
        var item = new DomainItem(Sku.Validate("SKU-1").Match(
            static value => value,
            static _ => throw new InvalidOperationException("Expected a valid SKU.")));
        var applied = patch!.ApplyTo(item);
        Assert.Equal("SKU-2", applied.Candidate.Sku.Value);
        Assert.False(applied.IsIdentity);

        using var invalidDocument = JsonDocument.Parse("""{"sku":""}""");
        var invalid = schema.Decode(invalidDocument.RootElement);
        Assert.True(invalid.TryGetErrors(out var errors));
        var error = Assert.Single(errors!);
        Assert.Equal("sku", error.Path);
        Assert.Equal(DecodeErrorKind.Empty, error.Kind);
    }

    private sealed record DomainItem(DomainValue<string, SkuTag> Sku);
}
