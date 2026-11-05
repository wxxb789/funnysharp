using System.Text.Json;
using FunnySharp.TestSupport.Domains;

namespace FunnySharp.Tests;

public sealed class DomainValueTests
{
    private readonly struct NotesTag;

    [Fact]
    public void DefaultAndNewValuesAreUninitializedAndThrowOnAccess()
    {
        var defaultValue = default(DomainValue<int, QuantityTag>);
        var newValue = new DomainValue<int, QuantityTag>();

        Assert.False(defaultValue.IsInitialized);
        Assert.False(newValue.IsInitialized);
        Assert.Equal(defaultValue, newValue);
        Assert.Equal(defaultValue.GetHashCode(), newValue.GetHashCode());
        Assert.Contains("uninitialized", defaultValue.ToString(), StringComparison.Ordinal);
        Assert.Contains(nameof(QuantityTag), defaultValue.ToString(), StringComparison.Ordinal);
        Assert.False(defaultValue.TryGetValue(out _));
        Assert.Throws<InvalidOperationException>(() => defaultValue.Value);
    }

    [Fact]
    public void ValidateWrapsOnlyValuesThatSatisfyTheContract()
    {
        var valid = Domains.Quantity.Validate(42);
        Assert.True(valid.TryGetValue(out var wrapped));
        Assert.True(wrapped.TryGetValue(out var quantity));
        Assert.Equal(42, quantity);
        Assert.Equal("42", wrapped.ToString());
        Assert.Equal(wrapped, Domains.Quantity.Validate(42).Match(
            static value => value,
            static _ => throw new InvalidOperationException("Expected a valid quantity.")));

        var invalid = Domains.Quantity.Validate(0);
        Assert.True(invalid.TryGetErrors(out var errors));
        var error = Assert.Single(errors!);
        Assert.Equal(DecodeErrorKind.InvalidValue, error.Kind);
        Assert.Equal("value must be between 1 and 100", error.Detail);
    }

    [Fact]
    public void ValidateTreatsRuntimeNullAsANullFailure()
    {
        var result = Domains.Sku.Validate(null!);

        Assert.True(result.TryGetErrors(out var errors));
        var error = Assert.Single(errors!);
        Assert.Equal(DecodeErrorKind.Null, error.Kind);
    }

    [Fact]
    public void CreateDecoderPreservesUnderlyingKindsAndLocations()
    {
        var created = Domains.Quantity.CreateDecoder();
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
    public void DerivedContractValidatesDecodesGeneratesShrinksAndDescribes()
    {
        var generated = Domains.Quantity.Contract.Generate(new ContractGenerationOptions(seed: 17));
        Assert.True(generated.TryGetValue(out var generatedValue));
        Assert.True(generatedValue.TryGetValue(out var generatedQuantity));
        Assert.InRange(generatedQuantity, 1, 100);

        var shrunk = Domains.Quantity.Contract.Shrink(generatedValue);
        Assert.True(shrunk.TryGetValue(out var candidates));
        Assert.NotEmpty(candidates!);
        Assert.All(
            candidates!,
            candidate =>
            {
                Assert.NotEqual(generatedValue, candidate);
                Assert.True(Domains.Quantity.Contract.Validate(candidate).IsValid);
            });

        Assert.Equal(ContractDescriptionKind.Derived, Domains.Quantity.Contract.Description.Kind);
        Assert.Contains(
            "not a validated quantity",
            Domains.Quantity.Contract.Explain(),
            StringComparison.Ordinal);

        var defaultValidation = Domains.Quantity.Contract.Validate(default);
        Assert.True(defaultValidation.TryGetErrors(out var defaultErrors));
        var defaultError = Assert.Single(defaultErrors!);
        Assert.Equal(DecodeErrorKind.InvalidValue, defaultError.Kind);
        Assert.Contains("not a validated quantity", defaultError.Detail, StringComparison.Ordinal);

        var valid = Domains.Quantity.Validate(42);
        Assert.True(valid.TryGetValue(out var wrapped));
        Assert.True(Domains.Quantity.Contract.Validate(wrapped).IsValid);

        var created = Domains.Quantity.Contract.CreateDecoder();
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
    public void DeserializationAndActivatorCannotForgeAValidDomainValue()
    {
        var fromWrapperJson = JsonSerializer.Deserialize<DomainValue<string, SkuTag>>(
            """{"Value":"SKU-1"}""");
        var fromRecordJson = JsonSerializer.Deserialize<DomainItem>(
            """{"Sku":{"Value":"SKU-9"}}""");
        var fromActivator = Activator.CreateInstance<DomainValue<string, SkuTag>>();

        Assert.False(fromWrapperJson.IsInitialized);
        Assert.False(fromActivator.IsInitialized);
        Assert.NotNull(fromRecordJson);
        Assert.False(fromRecordJson.Sku.IsInitialized);
        Assert.False(fromWrapperJson.TryGetValue(out _));
        Assert.False(fromRecordJson.Sku.TryGetValue(out _));
        Assert.False(fromActivator.TryGetValue(out _));
        Assert.Throws<InvalidOperationException>(() => fromWrapperJson.Value);
    }

    [Fact]
    public void EqualityDistinguishesInitializationAndUnderlyingValues()
    {
        Assert.True(Domains.Sku.Validate("SKU-1").TryGetValue(out var first));
        Assert.True(Domains.Sku.Validate("SKU-1").TryGetValue(out var second));
        Assert.True(Domains.Sku.Validate("SKU-2").TryGetValue(out var third));

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
            AssertOrderIdParity(domain, handWritten);
        }

        var domainNull = Domains.OrderId.Validate(null!);
        var handWrittenNull = HandWrittenOrderId.Create(null!);
        Assert.Equal(domainNull.IsValid, handWrittenNull.IsValid);
        AssertOrderIdParity(domainNull, handWrittenNull);

        using var validDocument = JsonDocument.Parse("\"ORD-100\"");
        var domainValid = Domains.OrderIdDecoder.Decode(validDocument.RootElement);
        var handWrittenValid = HandWrittenOrderId.Decoder.Decode(validDocument.RootElement);
        Assert.True(domainValid.TryGetValue(out var domainValue));
        Assert.True(handWrittenValid.TryGetValue(out var handWrittenValue));
        Assert.Equal(domainValue.Value, handWrittenValue.Value);

        using var invalidDocument = JsonDocument.Parse("\"ORDER-100\"");
        var domainInvalid = Domains.OrderIdDecoder.Decode(invalidDocument.RootElement);
        var handWrittenInvalid = HandWrittenOrderId.Decoder.Decode(invalidDocument.RootElement);
        Assert.Equal(ErrorShape(domainInvalid), ErrorShape(handWrittenInvalid));
    }

    private static void AssertOrderIdParity(
        Validation<DomainValue<string, OrderIdTag>, DecodeError> domain,
        Validation<HandWrittenOrderId, DecodeError> handWritten)
    {
        if (domain.TryGetValue(out var domainValue))
        {
            Assert.True(handWritten.TryGetValue(out var handWrittenValue));
            Assert.Equal(domainValue.Value, handWrittenValue.Value);
            return;
        }

        Assert.Equal(ErrorShape(domain), ErrorShape(handWritten));
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
            Domains.Sku.Contract,
            Lens.Create<DomainItem, DomainValue<string, SkuTag>>(
                static item => item.Sku,
                static (item, sku) => item with { Sku = sku }));
        var candidate = Contract.Object(
            Contract.For<DomainItem>().Field(
                "sku",
                Domains.Sku.Contract,
                static item => item.Sku),
            static sku => new DomainItem(sku));
        var schemaResult = PatchSchema<DomainItem>.Create(candidate, field);
        Assert.True(schemaResult.TryGetValue(out var schema));

        using var validDocument = JsonDocument.Parse("""{"sku":"SKU-2"}""");
        var valid = schema!.Decode(validDocument.RootElement);
        Assert.True(valid.TryGetValue(out var patch));
        var item = new DomainItem(Domains.Sku.Validate("SKU-1").Match(
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
