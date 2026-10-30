using System.Text.Json;
using FunnySharp;

namespace FunnySharp.TestSupport.Decoding;

internal static class DecoderOrderConsumer
{
    private const string EmailDetail = "email must contain a local part and domain";

    private static readonly Decoder<EmailAddress> EmailDecoder =
        Decoder.String.Validate(static email =>
                IsEmail(email)
                    ? Validation<string, string>.Valid(email)
                    : Validation<string, string>.Invalid(EmailDetail))
            .Map(static email => new EmailAddress(email));

    internal static readonly Decoder<Address> AddressDecoder =
        Decoder.Object(
            Decoder.Field("line", Decoder.NonEmptyString),
            Decoder.Field("city", Decoder.NonEmptyString),
            Decoder.Field("postalCode", Decoder.NonEmptyString),
            static (line, city, postalCode) => new Address(line, city, postalCode));

    internal static readonly Decoder<OrderLine> LineDecoder =
        Decoder.Object(
            Decoder.Field("sku", Decoder.NonEmptyString),
            Decoder.Field(
                "quantity",
                Decoder.Int32.Ensure(
                    static quantity => quantity > 0,
                    "quantity must be positive")),
            Decoder.Field(
                "unitPrice",
                Decoder.Decimal.Ensure(
                    static price => price >= 0,
                    "unit price must not be negative")),
            static (sku, quantity, unitPrice) => new OrderLine(sku, quantity, unitPrice));

    internal static readonly Decoder<Customer> CustomerDecoder =
        Decoder.Object(
            Decoder.Field("name", Decoder.NonEmptyString),
            Decoder.Field("email", EmailDecoder),
            Decoder.Field("addresses", Decoder.List(AddressDecoder)),
            static (name, email, addresses) => new Customer(name, email, addresses));

    internal static readonly Decoder<Order> OrderDecoder =
        Decoder.Object(
            Decoder.Field(
                "orderId",
                Decoder.Guid.Map(static id => new OrderId(id))),
            Decoder.Field("customer", CustomerDecoder),
            Decoder.Field("lines", Decoder.NonEmptyList(LineDecoder)),
            Decoder.OptionalField("note", Decoder.String),
            static (id, customer, lines, note) => new Order(id, customer, lines, note));

    internal static OrderDecodeResult Decode(JsonElement root)
    {
        var validation = OrderDecoder.Decode(root);
        if (validation.TryGetValue(out var order))
        {
            return new OrderDecodeResult(order!, Array.Empty<OrderError>());
        }

        validation.TryGetErrors(out var errors);
        return new OrderDecodeResult(
            null,
            errors!.Select(static error => MapError(error)).ToArray());
    }

    private static OrderError MapError(DecodeError error) =>
        new(
            error.Path,
            error.Kind switch
            {
                DecodeErrorKind.Missing => OrderErrorCode.Required,
                DecodeErrorKind.Null => OrderErrorCode.Null,
                DecodeErrorKind.WrongKind => OrderErrorCode.WrongKind,
                DecodeErrorKind.Malformed => OrderErrorCode.Malformed,
                DecodeErrorKind.Empty => OrderErrorCode.Empty,
                _ => OrderErrorCode.Invalid,
            },
            error.Kind == DecodeErrorKind.InvalidValue
                ? error.Detail ?? "invalid"
                : error.Kind switch
                {
                    DecodeErrorKind.Missing => "required",
                    DecodeErrorKind.Null => "null",
                    DecodeErrorKind.WrongKind => "wrong-kind",
                    DecodeErrorKind.Malformed => "malformed",
                    DecodeErrorKind.Empty => "empty",
                    _ => "invalid",
                });

    private static bool IsEmail(string value)
    {
        var at = value.IndexOf('@');

        return at > 0 && at < value.Length - 1;
    }
}
