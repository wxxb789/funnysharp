using System.Text.Json;

namespace FunnySharp.TestSupport.Decoding;

internal static class OrderComparison
{
    internal static readonly (string Name, Func<JsonElement, OrderDecodeResult> Decode)[] Consumers =
    [
        ("decoder", DecoderOrderConsumer.Decode),
        ("ordinary", OrdinaryOrderConsumer.Decode),
        ("validation", ValidationOrderConsumer.Decode),
    ];

    internal static void Verify()
    {
        foreach (var fixture in OrderFixtures.All)
        {
            using var document = JsonDocument.Parse(fixture.Json);
            var results = Consumers
                .Select(consumer => (consumer.Name, Result: consumer.Decode(document.RootElement)))
                .ToArray();

            for (var left = 0; left < results.Length; left++)
            {
                for (var right = left + 1; right < results.Length; right++)
                {
                    Compare(
                        fixture.Name,
                        results[left].Name,
                        results[left].Result,
                        results[right].Name,
                        results[right].Result);
                }
            }
        }

        VerifyExpected();
    }

    private static void VerifyExpected()
    {
        var expectedOrder = new Order(
            new OrderId(Guid.Parse("0f8fad5b-d9cb-469f-a165-70867728950e")),
            new Customer(
                "Ada",
                new EmailAddress("ada@example.com"),
                [new Address("1 Main St", "Springfield", "12345")]),
            [
                new OrderLine("BOOK-1", 2, 12.50m),
                new OrderLine("PEN-1", 1, 1.25m),
            ],
            Option<string>.None);

        var expectedErrors = new[]
        {
            new OrderError("orderId", OrderErrorCode.Malformed, "malformed"),
            new OrderError("customer.name", OrderErrorCode.Empty, "empty"),
            new OrderError(
                "customer.email",
                OrderErrorCode.Invalid,
                "email must contain a local part and domain"),
            new OrderError("customer.addresses[1].postalCode", OrderErrorCode.Null, "null"),
            new OrderError("lines[1].sku", OrderErrorCode.Empty, "empty"),
            new OrderError(
                "lines[1].quantity",
                OrderErrorCode.Invalid,
                "quantity must be positive"),
            new OrderError("note", OrderErrorCode.Null, "null"),
        };

        foreach (var (name, decode) in Consumers)
        {
            var valid = Decode(OrderFixtures.Valid, decode);
            if (!valid.IsSuccess || !OrderEquals(valid.Value!, expectedOrder))
            {
                throw new InvalidOperationException(
                    $"{name}: the valid fixture did not produce the expected order.");
            }

            var invalid = Decode(OrderFixtures.Invalid, decode);
            if (invalid.IsSuccess || !invalid.Errors.SequenceEqual(expectedErrors))
            {
                throw new InvalidOperationException(
                    $"{name}: the invalid fixture did not produce the expected errors.\n" +
                    "expected: " + FormatErrors(expectedErrors) + "\n" +
                    "actual:   " + FormatErrors(invalid.Errors));
            }
        }

        var emptyAddresses = Decode(OrderFixtures.ValidEmptyAddresses, DecoderOrderConsumer.Decode);
        if (!emptyAddresses.IsSuccess || emptyAddresses.Value!.Customer.Addresses.Count != 0)
        {
            throw new InvalidOperationException(
                "The decoder did not accept an empty addresses list.");
        }

        var emptyNote = Decode(OrderFixtures.EmptyNote, DecoderOrderConsumer.Decode);
        if (!emptyNote.IsSuccess
            || !emptyNote.Value!.Note.TryGetValue(out var note)
            || note.Length != 0)
        {
            throw new InvalidOperationException(
                "The decoder did not preserve an empty note as Some(\"\").");
        }
    }

    internal static bool OrderEquals(Order left, Order right) =>
        left.Id == right.Id
        && string.Equals(left.Customer.Name, right.Customer.Name, StringComparison.Ordinal)
        && left.Customer.Email == right.Customer.Email
        && left.Customer.Addresses.SequenceEqual(right.Customer.Addresses)
        && left.Lines.SequenceEqual(right.Lines)
        && left.Note.Equals(right.Note);

    private static void Compare(
        string fixture,
        string leftName,
        OrderDecodeResult left,
        string rightName,
        OrderDecodeResult right)
    {
        if (left.IsSuccess != right.IsSuccess)
        {
            throw new InvalidOperationException(
                $"{fixture}: {leftName} success={left.IsSuccess}, " +
                $"{rightName} success={right.IsSuccess}.");
        }

        if (left.IsSuccess)
        {
            if (!OrderEquals(left.Value!, right.Value!))
            {
                throw new InvalidOperationException(
                    $"{fixture}: {leftName} and {rightName} produced different orders.");
            }

            return;
        }

        if (!left.Errors.SequenceEqual(right.Errors))
        {
            throw new InvalidOperationException(
                $"{fixture}: {leftName} and {rightName} produced different errors.\n" +
                $"{leftName}: {FormatErrors(left.Errors)}\n" +
                $"{rightName}: {FormatErrors(right.Errors)}");
        }
    }

    private static OrderDecodeResult Decode(
        DecodeFixture fixture,
        Func<JsonElement, OrderDecodeResult> decode)
    {
        using var document = JsonDocument.Parse(fixture.Json);
        return decode(document.RootElement);
    }

    private static string FormatErrors(IReadOnlyList<OrderError> errors) =>
        string.Join(
            "; ",
            errors.Select(static error => $"{error.Path}|{error.Code}|{error.Detail}"));
}
