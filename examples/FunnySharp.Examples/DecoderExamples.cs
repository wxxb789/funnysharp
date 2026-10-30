using System.Text.Json;
using FunnySharp;

internal static class DecoderExamples
{
    internal static void Verify()
    {
        var lineDecoder = Decoder.Object(
            Decoder.Field("sku", Decoder.NonEmptyString),
            Decoder.Field(
                "quantity",
                Decoder.Int32.Ensure(
                    static quantity => quantity > 0,
                    "quantity must be positive")),
            static (sku, quantity) => new ExampleLine(sku, quantity));

        var orderDecoder = Decoder.Object(
            Decoder.Field("orderId", Decoder.Guid),
            Decoder.Field("lines", Decoder.NonEmptyList(lineDecoder)),
            static (orderId, lines) => new ExampleOrder(orderId, lines));

        using var validDocument = JsonDocument.Parse(
            """
            {
              "orderId": "0f8fad5b-d9cb-469f-a165-70867728950e",
              "lines": [
                { "sku": "BOOK-1", "quantity": 2 },
                { "sku": "PEN-1", "quantity": 1 }
              ]
            }
            """);

        var valid = orderDecoder.Decode(validDocument.RootElement);
        ExampleAssertions.True(valid.TryGetValue(out var order), "The valid order must decode.");
        ExampleAssertions.Equal(2, order!.Lines.Count);
        ExampleAssertions.Equal("BOOK-1", order.Lines[0].Sku);
        ExampleAssertions.Equal(2, order.Lines[0].Quantity);

        using var invalidDocument = JsonDocument.Parse(
            """
            {
              "orderId": "0f8fad5b-d9cb-469f-a165-70867728950e",
              "lines": [
                { "sku": "BOOK-1", "quantity": 2 },
                { "sku": "", "quantity": 0 }
              ]
            }
            """);

        var invalid = orderDecoder.Decode(invalidDocument.RootElement);
        ExampleAssertions.True(
            invalid.TryGetErrors(out var errors),
            "The invalid order must accumulate errors.");

        ExampleAssertions.Equal(2, errors!.Count);
        ExampleAssertions.Equal("lines[1].sku", errors[0].Path);
        ExampleAssertions.Equal("lines[1].quantity", errors[1].Path);
        ExampleAssertions.Equal(
            "lines[1].quantity: quantity must be positive",
            errors[1].ToString());
    }

    private sealed record ExampleOrder(Guid OrderId, IReadOnlyList<ExampleLine> Lines);

    private sealed record ExampleLine(string Sku, int Quantity);
}
