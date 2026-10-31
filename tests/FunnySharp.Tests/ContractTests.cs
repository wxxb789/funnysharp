using System.Text.Json;
using FunnySharp.TestSupport.Contracts;

namespace FunnySharp.Tests;

public sealed class ContractTests
{
    [Fact]
    public void PrimitiveAndRangeContractsValidateAndDecodeBoundaryValuesConsistently()
    {
        var quantity = Contract.Int32Range(1, 100);

        Assert.True(quantity.Validate(1).IsValid);
        Assert.True(quantity.Validate(100).IsValid);

        var below = SingleError(quantity.Validate(0));
        Assert.Equal(DecodeErrorKind.InvalidValue, below.Kind);
        Assert.Equal(string.Empty, below.Path);
        Assert.Equal("value must be between 1 and 100", below.Detail);

        var above = SingleError(quantity.Validate(101));
        Assert.Equal(below, above);

        using var lowDocument = JsonDocument.Parse("0");
        var decodedBelow = Assert.Single(DecodeErrors(quantity, lowDocument.RootElement));
        Assert.Equal(below, decodedBelow);

        using var highDocument = JsonDocument.Parse("101");
        var decodedAbove = Assert.Single(DecodeErrors(quantity, highDocument.RootElement));
        Assert.Equal(above, decodedAbove);

        var nonEmpty = Contract.NonEmptyString;
        var emptyValidation = SingleError(nonEmpty.Validate(string.Empty));
        Assert.Equal(DecodeErrorKind.Empty, emptyValidation.Kind);
        using var emptyDocument = JsonDocument.Parse("\"\"");
        var emptyDecode = Assert.Single(DecodeErrors(nonEmpty, emptyDocument.RootElement));
        Assert.Equal(emptyValidation, emptyDecode);
    }

    [Fact]
    public void DecodingDiagnosticsRenderNestedPathsAndReasons()
    {
        using var document = JsonDocument.Parse(OrderFixtures.InvalidValuesJson);
        var errors = DecodeErrors(ContractOrderConsumer.OrderContract, document.RootElement);

        Assert.Equal(
            new[]
            {
                "lines[0].sku: expected a non-empty string",
                "lines[0].quantity: value must be between 1 and 100",
                "lines[0].unitPrice: value must be between 0.01 and 5000",
                "payment.card.last4: last4 must contain exactly four digits",
                "payment.card.currency: expected a non-empty string",
            },
            errors.Select(static error => error.ToString()).ToArray());
    }

    [Fact]
    public void TypedValidationReportsTheSamePathsAsDecodingForTheSameRules()
    {
        var order = OrderFixtures.InvalidOrder();

        var errors = DecodedErrors(ContractOrderConsumer.OrderContract.Validate(order));

        Assert.Equal(
            new[]
            {
                "customer.name",
                "customer.email",
                "lines[0].sku",
                "lines[0].quantity",
                "lines[0].unitPrice",
                "payment.card.last4",
                "payment.card.currency",
            },
            errors.Select(static error => error.Path).ToArray());
        Assert.Equal(
            new[]
            {
                DecodeErrorKind.Empty,
                DecodeErrorKind.InvalidValue,
                DecodeErrorKind.Empty,
                DecodeErrorKind.InvalidValue,
                DecodeErrorKind.InvalidValue,
                DecodeErrorKind.InvalidValue,
                DecodeErrorKind.Empty,
            },
            errors.Select(static error => error.Kind).ToArray());
    }

    [Fact]
    public void NullReferenceValuesAreRejectedByValidation()
    {
        var error = SingleError(Contract.String.Validate(null!));
        Assert.Equal(DecodeErrorKind.Null, error.Kind);
        Assert.Equal(string.Empty, error.Path);

        var line = ContractOrderConsumer.LineContract;
        var typed = new FunnySharp.TestSupport.Contracts.OrderLine(string.Empty, 0, 0m);
        Assert.False(line.Validate(typed).IsValid);
        Assert.True(line.Validate(new FunnySharp.TestSupport.Contracts.OrderLine("SKU", 1, 1m)).IsValid);
    }

    [Fact]
    public void PrimitiveValidationReturnsTheCheckedValue()
    {
        var validation = Contract.String.Validate("abc");

        Assert.True(validation.TryGetValue(out var value));
        Assert.Equal("abc", value);
    }

    [Fact]
    public void OptionalAndNullableFieldPoliciesAreDistinct()
    {
        var contract = Contract.Object(
            Contract.OptionalField("note", Contract.String, static (NoteHolder value) => value.Note),
            Contract.NullableField("alias", Contract.String, static (NoteHolder value) => value.Alias),
            static (note, alias) => new NoteHolder(note, alias));

        using var missing = JsonDocument.Parse("{}");
        var missingValue = Assert.IsType<NoteHolder>(Decode(contract, missing.RootElement));
        Assert.True(missingValue.Note.IsNone);
        Assert.True(missingValue.Alias.IsNone);

        using var nulls = JsonDocument.Parse("""{ "note": null, "alias": null }""");
        var errors = DecodeErrors(contract, nulls.RootElement);
        var error = Assert.Single(errors);
        Assert.Equal("note", error.Path);
        Assert.Equal(DecodeErrorKind.Null, error.Kind);

        using var present = JsonDocument.Parse("""{ "note": "", "alias": "a" }""");
        var presentValue = Assert.IsType<NoteHolder>(Decode(contract, present.RootElement));
        Assert.True(presentValue.Note.TryGetValue(out var note));
        Assert.Equal(string.Empty, note);
        Assert.True(presentValue.Alias.TryGetValue(out var alias));
        Assert.Equal("a", alias);
    }

    [Fact]
    public void OneOfContractUsesItsLabelForValidationAndDecodingDiagnostics()
    {
        var card = Contract.Object(
            Contract.Field("last4", Contract.NonEmptyString, static (Card value) => value.Last4),
            static last4 => new Card(last4));
        var cash = Contract.Object(
            Contract.Field("currency", Contract.NonEmptyString, static (Cash value) => value.Currency),
            static currency => new Cash(currency));
        var cardAsPayment = card.Widen<Payment>(
            static value => value,
            static value => value is Card item ? Option<Card>.Some(item) : Option<Card>.None,
            "not a card");
        var cashAsPayment = cash.Widen<Payment>(
            static value => value,
            static value => value is Cash item ? Option<Cash>.Some(item) : Option<Cash>.None,
            "not cash");
        var contract = Contract.DiscriminatedOneOf<Payment>(
            "method",
            static value => value switch { Card => 0, Cash => 1, _ => -1 },
            Contract.Alternative("card", cardAsPayment),
            Contract.Alternative("cash", cashAsPayment));

        using var document = JsonDocument.Parse(
            """{ "method": "card", "last4": "" }""");
        var error = Assert.Single(DecodeErrors(contract, document.RootElement));
        Assert.Equal("card.last4", error.Path);
        Assert.Equal(DecodeErrorKind.Empty, error.Kind);

        var typed = new Card(string.Empty);
        var typedError = Assert.Single(DecodedErrors(contract.Validate(typed)));
        Assert.Equal("card.last4", typedError.Path);

        using var unknown = JsonDocument.Parse("""{ "method": "cheque" }""");
        var unknownError = Assert.Single(DecodeErrors(contract, unknown.RootElement));
        Assert.Equal(string.Empty, unknownError.Path);
        Assert.Equal("expected one of: card, cash", unknownError.Detail);
    }

    [Fact]
    public void OneOfDecoderRejectsAValueTheContractSelectsElsewhere()
    {
        var contract = Contract.OneOf(
            static value => value == 0 ? 1 : 0,
            static element => Option<int>.Some(element.GetInt32() == 0 ? 0 : 1),
            discriminator: null,
            Contract.Alternative("zero", Contract.Int32Range(0, 0)),
            Contract.Alternative("one", Contract.Int32Range(1, 1)));

        using var document = JsonDocument.Parse("0");
        var error = Assert.Single(DecodeErrors(contract, document.RootElement));
        Assert.Equal("one", error.Path);
        Assert.Equal(DecodeErrorKind.InvalidValue, error.Kind);
    }

    [Fact]
    public void DeferredContractGeneratesFiniteRecursiveValuesAndStopsAtTheDepthBudget()
    {
        Contract<Node>? node = null;
        node = Contract.Defer(
            "node",
            () => Contract.Object(
                Contract.Field("value", Contract.Int32Range(0, 9), static (Node value) => value.Value),
                Contract.Field(
                    "children",
                    Contract.List(node!, 0, 3),
                    static (Node value) => value.Children),
                static (value, children) => new Node(value, children)));

        var options = new ContractGenerationOptions(
            seed: 7,
            maxDepth: 3,
            maxCollectionLength: 3,
            maxNodes: 200);
        var generated = node.Generate(options);
        Assert.True(generated.TryGetValue(out var root), "The recursive contract must generate a value.");
        Assert.True(node.Validate(root!).IsValid);
        Assert.True(MaxDepth(root!) <= options.MaxDepth + 1);

        Contract<int> loop = null!;
        loop = Contract.Defer("loop", () => loop);
        var failed = loop.Generate(new ContractGenerationOptions(seed: 1, maxDepth: 2));
        var limitation = Assert.IsType<ContractLimitation>(failed.TryGetError(out var error) ? error : null);
        Assert.Equal(ContractDerivation.Generation, limitation.Derivation);
        Assert.Contains("depth budget", limitation.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void ExplanationIsDeterministicAndReportsCapabilitiesAndLimitations()
    {
        var first = ContractOrderConsumer.OrderContract.Explain();
        var second = ContractOrderConsumer.OrderContract.Explain();

        Assert.Equal(first, second);
        Assert.Contains("capabilities:", first, StringComparison.Ordinal);
        Assert.Contains("validation: available", first, StringComparison.Ordinal);
        Assert.Contains("decoding: available", first, StringComparison.Ordinal);
        Assert.Contains("generation: available", first, StringComparison.Ordinal);
        Assert.Contains("shrinking: available", first, StringComparison.Ordinal);
        Assert.Contains("schema: incomplete", first, StringComparison.Ordinal);
        Assert.Contains("order total must not exceed 1000", first, StringComparison.Ordinal);
        Assert.Contains("email address", first, StringComparison.Ordinal);
    }

    [Fact]
    public void InvalidFactoryArgumentsAreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Contract.Int32Range(5, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Contract.Int64Range(5, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Contract.DecimalRange(5m, 1m));
        Assert.Throws<ArgumentOutOfRangeException>(() => Contract.DoubleRange(5d, 1d));
        Assert.Throws<ArgumentOutOfRangeException>(() => Contract.DoubleRange(double.NaN, 1d));
        Assert.Throws<ArgumentOutOfRangeException>(() => Contract.List(Contract.Int32, 2, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Contract.List(Contract.Int32, -1));
        Assert.Throws<ArgumentException>(() => Contract.Alternative<Payment>("", Contract.From<Payment>("payment", static value => Validation<Payment, string>.Valid(value))));
        Assert.Throws<ArgumentException>(() => Contract.OneOf<Payment>(
            static _ => 0,
            null,
            null,
            Array.Empty<ContractAlternative<Payment>>()));
        Assert.Throws<ArgumentException>(() => Contract.DiscriminatedOneOf<Payment>(
            "",
            static _ => 0,
            Contract.Alternative("card", Contract.From<Payment>("card", static value => Validation<Payment, string>.Valid(value)))));
        Assert.Throws<ArgumentException>(() => Contract.Defer<int>("", () => Contract.Int32));
    }

    [Fact]
    public void ObjectContractsRejectDuplicateFieldNames()
    {
        Assert.Throws<ArgumentException>(() => Contract.Object(
            Contract.Field("name", Contract.String, static (Duplicate value) => value.Name),
            Contract.Field("name", Contract.String, static (Duplicate value) => value.Other),
            static (name, other) => new Duplicate(name, other)));
    }

    private static T Decode<T>(Contract<T> contract, JsonElement input)
    {
        var created = contract.CreateDecoder();
        Assert.True(created.TryGetValue(out var decoder), "The contract must decode.");
        var validation = decoder!.Decode(input);
        Assert.True(validation.TryGetValue(out var value), "The input must decode.");
        return value;
    }

    private static IReadOnlyList<DecodeError> DecodeErrors<T>(Contract<T> contract, JsonElement input)
    {
        var created = contract.CreateDecoder();
        Assert.True(created.TryGetValue(out var decoder), "The contract must decode.");
        return DecodedErrors(decoder!.Decode(input));
    }

    private static IReadOnlyList<DecodeError> DecodedErrors<T>(Validation<T, DecodeError> validation)
    {
        Assert.False(validation.IsValid, "The validation must be invalid.");
        Assert.True(validation.TryGetErrors(out var errors));
        return errors;
    }

    private static DecodeError SingleError<T>(Validation<T, DecodeError> validation) =>
        Assert.Single(DecodedErrors(validation));

    private static int MaxDepth(Node node) =>
        node.Children.Count == 0
            ? 0
            : 1 + node.Children.Max(MaxDepth);

    private sealed record NoteHolder(Option<string> Note, Option<string> Alias);

    private abstract record Payment;

    private sealed record Card(string Last4) : Payment;

    private sealed record Cash(string Currency) : Payment;

    private sealed record Node(int Value, IReadOnlyList<Node> Children);

    private sealed record Duplicate(string Name, string Other);
}
