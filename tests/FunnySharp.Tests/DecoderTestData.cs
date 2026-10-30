using System.Text.Json;

namespace FunnySharp.Tests;

internal static class DecoderTestData
{
    internal sealed record Line(string Sku, int Quantity);

    internal sealed record Address(string PostalCode);

    internal sealed record Customer(string Name, string Email, IReadOnlyList<Address> Addresses);

    internal sealed record Order(Guid Id, Customer Customer, IReadOnlyList<Line> Lines, Option<string> Note);

    internal static readonly Decoder<string> EmailDecoder =
        Decoder.String.Validate(static email =>
            email.Contains('@')
                ? Validation<string, string>.Valid(email)
                : Validation<string, string>.Invalid("email must contain '@'"));

    internal static readonly Decoder<Line> LineDecoder =
        Decoder.Object(
            Decoder.Field("sku", Decoder.NonEmptyString),
            Decoder.Field(
                "quantity",
                Decoder.Int32.Ensure(
                    static quantity => quantity > 0,
                    "quantity must be positive")),
            static (sku, quantity) => new Line(sku, quantity));

    internal static readonly Decoder<Address> AddressDecoder =
        Decoder.Field("postalCode", Decoder.NonEmptyString)
            .Map(static postalCode => new Address(postalCode));

    internal static readonly Decoder<Customer> CustomerDecoder =
        Decoder.Object(
            Decoder.Field("name", Decoder.NonEmptyString),
            Decoder.Field("email", EmailDecoder),
            Decoder.Field("addresses", Decoder.List(AddressDecoder)),
            static (name, email, addresses) => new Customer(name, email, addresses));

    internal static readonly Decoder<Order> OrderDecoder =
        Decoder.Object(
            Decoder.Field("id", Decoder.Guid),
            Decoder.Field("customer", CustomerDecoder),
            Decoder.Field("lines", Decoder.NonEmptyList(LineDecoder)),
            Decoder.OptionalField("note", Decoder.String),
            static (id, customer, lines, note) => new Order(id, customer, lines, note));

    internal const string ValidOrderJson = """
        {
          "id": "0f8fad5b-d9cb-469f-a165-70867728950e",
          "customer": {
            "name": "Ada",
            "email": "ada@example.com",
            "addresses": [
              { "postalCode": "12345" }
            ]
          },
          "lines": [
            { "sku": "BOOK-1", "quantity": 2 },
            { "sku": "PEN-1", "quantity": 1 }
          ]
        }
        """;

    internal const string InvalidOrderJson = """
        {
          "id": "not-a-guid",
          "customer": {
            "name": "",
            "email": "not-an-email",
            "addresses": [
              { "postalCode": "12345" },
              { "postalCode": null }
            ]
          },
          "lines": [
            { "sku": "BOOK-1", "quantity": 2 },
            { "sku": "", "quantity": 0 },
            { "sku": "PEN-1", "quantity": 1 }
          ],
          "note": null
        }
        """;

    internal static Validation<T, DecodeError> Decode<T>(Decoder<T> decoder, string json)
    {
        using var document = JsonDocument.Parse(json);
        return decoder.Decode(document.RootElement);
    }

    internal static IReadOnlyList<DecodeError> Errors<T>(Validation<T, DecodeError> validation)
    {
        Assert.True(validation.TryGetErrors(out var errors));
        return errors!;
    }

    internal static T Value<T>(Validation<T, DecodeError> validation)
    {
        Assert.True(validation.TryGetValue(out var value));
        return value!;
    }
}
