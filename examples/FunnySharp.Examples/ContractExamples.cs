using System.Text.Json;
using FunnySharp;

internal static class ContractExamples
{
    internal static void Verify()
    {
        var fields = Contract.For<ExampleLine>();
        var lineContract = Contract.Object(
            fields.Field("sku", Contract.NonEmptyString, static line => line.Sku),
            fields.Field("quantity", Contract.Int32Range(1, 100), static line => line.Quantity),
            static (sku, quantity) => new ExampleLine(sku, quantity));

        ExampleAssertions.Equal(ContractDescriptionKind.Object, lineContract.Description.Kind);
        ExampleAssertions.Equal(2, lineContract.Description.Fields.Count);
        ExampleAssertions.True(
            lineContract.Capabilities.HasCompleteSchema,
            "A structural line contract must expose a complete schema.");
        ExampleAssertions.True(
            lineContract.Explain().Contains("capabilities:", StringComparison.Ordinal),
            "The explanation must report capabilities.");

        var valid = lineContract.Validate(new ExampleLine("BOOK-1", 2));
        ExampleAssertions.True(valid.IsValid, "The valid line must pass validation.");

        var invalid = lineContract.Validate(new ExampleLine(string.Empty, 0));
        ExampleAssertions.True(
            invalid.TryGetErrors(out var validationErrors),
            "The invalid line must accumulate errors.");
        ExampleAssertions.Equal(2, validationErrors!.Count);
        ExampleAssertions.Equal("sku", validationErrors[0].Path);
        ExampleAssertions.Equal(
            "quantity: value must be between 1 and 100",
            validationErrors[1].ToString());

        var created = lineContract.CreateDecoder();
        ExampleAssertions.True(created.TryGetValue(out var decoder), "The contract must decode.");

        using var document = JsonDocument.Parse("""{ "sku": "BOOK-1", "quantity": 2 }""");
        ExampleAssertions.True(
            decoder!.Decode(document.RootElement).TryGetValue(out var decoded),
            "The valid JSON line must decode.");
        ExampleAssertions.Equal(new ExampleLine("BOOK-1", 2), decoded!);

        var generated = lineContract.Generate(new ContractGenerationOptions(seed: 17));
        ExampleAssertions.True(generated.TryGetValue(out var generatedLine), "The line must generate.");
        ExampleAssertions.True(
            lineContract.Validate(generatedLine!).IsValid,
            "A generated line must satisfy the contract.");

        var shrunk = lineContract.Shrink(generatedLine!);
        ExampleAssertions.True(shrunk.TryGetValue(out var shrinkCandidates), "The line must shrink.");
        ExampleAssertions.True(shrinkCandidates!.Count > 0, "The line must produce shrink candidates.");
        foreach (var candidate in shrinkCandidates)
        {
            ExampleAssertions.True(
                lineContract.Validate(candidate).IsValid,
                "Every shrink candidate must satisfy the contract.");
        }

        var even = Contract.From<int>(
            "even integer",
            static value => value % 2 == 0
                ? Validation<int, string>.Valid(value)
                : Validation<int, string>.Invalid("value must be even"));
        ExampleAssertions.True(
            !even.Capabilities.CanGenerate,
            "An arbitrary predicate must not claim a derived generator.");
        ExampleAssertions.True(
            even.Generate(new ContractGenerationOptions(seed: 1)).TryGetError(out var limitation),
            "Generation must fail explicitly.");
        ExampleAssertions.Equal(ContractDerivation.Generation, limitation!.Derivation);

        var extendable = even
            .WithDecoder(Decoder.Int32)
            .WithGenerator(static random => random.Next(0, 10) * 2)
            .WithShrinker(static value => value >= 2 ? [value - 2] : [])
            .WithSchema(ContractDescription.Scalar("even integer"));
        ExampleAssertions.True(
            extendable.Capabilities.CanGenerate
                && extendable.Capabilities.CanDecode
                && extendable.Capabilities.CanShrink
                && extendable.Capabilities.HasCompleteSchema,
            "Caller-supplied derivations must restore the capabilities.");
    }

    private sealed record ExampleLine(string Sku, int Quantity);
}
