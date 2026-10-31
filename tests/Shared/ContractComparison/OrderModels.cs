namespace FunnySharp.TestSupport.Contracts;

internal abstract record Payment;

internal sealed record CardPayment(string Last4, string Currency) : Payment;

internal sealed record CashPayment(string Currency) : Payment;

internal sealed record Customer(string Name, string Email, Option<string> Note);

internal sealed record OrderLine(string Sku, int Quantity, decimal UnitPrice);

internal sealed record Order(
    Guid Id,
    Customer Customer,
    IReadOnlyList<OrderLine> Lines,
    Payment Payment);

internal sealed record Category(string Name, IReadOnlyList<Category> Children);

internal sealed record OrderDecodeResult(Order? Value, IReadOnlyList<OrderError> Errors)
{
    internal bool IsSuccess => Errors.Count == 0;
}

internal static class OrderFixtures
{
    internal const string ValidJson =
        """
        {
          "id": "0f8fad5b-d9cb-469f-a165-70867728950e",
          "customer": {
            "name": "Ada",
            "email": "ada@example.com",
            "note": "leave at door"
          },
          "lines": [
            { "sku": "BOOK-1", "quantity": 2, "unitPrice": 12.50 }
          ],
          "payment": { "method": "card", "last4": "4242", "currency": "USD" }
        }
        """;

    internal const string InvalidBoundariesJson =
        """
        {
          "id": "not-a-guid",
          "customer": { "name": "", "email": "bad", "note": null },
          "lines": [],
          "payment": { "method": "cheque", "currency": "" }
        }
        """;

    internal const string InvalidValuesJson =
        """
        {
          "id": "0f8fad5b-d9cb-469f-a165-70867728950e",
          "customer": { "name": "Ada", "email": "ada@example.com", "note": "x" },
          "lines": [
            { "sku": "", "quantity": 0, "unitPrice": 0.001 }
          ],
          "payment": { "method": "card", "last4": "42", "currency": "" }
        }
        """;

    internal static readonly DecodeFixture Valid = new("valid", ValidJson);

    internal static readonly DecodeFixture ValidCash = new(
        "valid-cash",
        """
        {
          "id": "0f8fad5b-d9cb-469f-a165-70867728950e",
          "customer": { "name": "Grace", "email": "grace@example.com" },
          "lines": [
            { "sku": "PEN-1", "quantity": 1, "unitPrice": 2.25 },
            { "sku": "PAD-2", "quantity": 3, "unitPrice": 4.50 }
          ],
          "payment": { "method": "cash", "currency": "EUR" }
        }
        """);

    internal static readonly DecodeFixture InvalidBoundaries =
        new("invalid-boundaries", InvalidBoundariesJson);

    internal static readonly DecodeFixture InvalidValues = new("invalid-values", InvalidValuesJson);

    internal static readonly DecodeFixture TotalExceeded = new(
        "total-exceeded",
        """
        {
          "id": "0f8fad5b-d9cb-469f-a165-70867728950e",
          "customer": { "name": "Ada", "email": "ada@example.com" },
          "lines": [ { "sku": "BULK", "quantity": 100, "unitPrice": 5000 } ],
          "payment": { "method": "cash", "currency": "USD" }
        }
        """);

    internal static readonly DecodeFixture MissingFields = new("missing-fields", "{}");

    internal static readonly DecodeFixture WrongKinds = new(
        "wrong-kinds",
        """{ "id": 42, "customer": [], "lines": {}, "payment": "card" }""");

    internal static readonly DecodeFixture RootNull = new("root-null", "null");

    internal static readonly DecodeFixture RootArray = new("root-array", "[]");

    internal static readonly DecodeFixture NoteNull = new(
        "note-null",
        """
        {
          "id": "0f8fad5b-d9cb-469f-a165-70867728950e",
          "customer": { "name": "Ada", "email": "ada@example.com", "note": null },
          "lines": [ { "sku": "BOOK-1", "quantity": 1, "unitPrice": 1.00 } ],
          "payment": { "method": "cash", "currency": "USD" }
        }
        """);

    internal static readonly DecodeFixture LineNull = new(
        "line-null",
        """
        {
          "id": "0f8fad5b-d9cb-469f-a165-70867728950e",
          "customer": { "name": "Ada", "email": "ada@example.com" },
          "lines": [ null ],
          "payment": { "method": "cash", "currency": "USD" }
        }
        """);

    internal static readonly DecodeFixture LinesTooMany = new(
        "lines-too-many",
        """
        {
          "id": "0f8fad5b-d9cb-469f-a165-70867728950e",
          "customer": { "name": "Ada", "email": "ada@example.com" },
          "lines": [
            { "sku": "A", "quantity": 1, "unitPrice": 1 },
            { "sku": "B", "quantity": 1, "unitPrice": 1 },
            { "sku": "C", "quantity": 1, "unitPrice": 1 },
            { "sku": "D", "quantity": 1, "unitPrice": 1 },
            { "sku": "E", "quantity": 1, "unitPrice": 1 },
            { "sku": "F", "quantity": 1, "unitPrice": 1 },
            { "sku": "G", "quantity": 1, "unitPrice": 1 },
            { "sku": "H", "quantity": 1, "unitPrice": 1 },
            { "sku": "I", "quantity": 1, "unitPrice": 1 },
            { "sku": "J", "quantity": 1, "unitPrice": 1 },
            { "sku": "K", "quantity": 1, "unitPrice": 1 }
          ],
          "payment": { "method": "cash", "currency": "USD" }
        }
        """);

    internal static readonly DecodeFixture PaymentMethodMissing = new(
        "payment-method-missing",
        """
        {
          "id": "0f8fad5b-d9cb-469f-a165-70867728950e",
          "customer": { "name": "Ada", "email": "ada@example.com" },
          "lines": [ { "sku": "BOOK-1", "quantity": 1, "unitPrice": 1 } ],
          "payment": { "currency": "USD" }
        }
        """);

    internal static Order InvalidOrder() =>
        new(
            Guid.Parse("0f8fad5b-d9cb-469f-a165-70867728950e"),
            new Customer(string.Empty, "bad", Option<string>.Some(string.Empty)),
            [new OrderLine(string.Empty, 0, 0.001m)],
            new CardPayment("42", string.Empty));

    internal static readonly IReadOnlyList<DecodeFixture> All =
    [
        Valid,
        ValidCash,
        InvalidBoundaries,
        InvalidValues,
        TotalExceeded,
        MissingFields,
        WrongKinds,
        RootNull,
        RootArray,
        NoteNull,
        LineNull,
        LinesTooMany,
        PaymentMethodMissing,
    ];
}
