namespace FunnySharp.TestSupport.Decoding;

internal static class OrderFixtures
{
    internal static readonly DecodeFixture Valid = new(
        "valid",
        """
        {
          "orderId": "0f8fad5b-d9cb-469f-a165-70867728950e",
          "customer": {
            "name": "Ada",
            "email": "ada@example.com",
            "addresses": [
              { "line": "1 Main St", "city": "Springfield", "postalCode": "12345" }
            ]
          },
          "lines": [
            { "sku": "BOOK-1", "quantity": 2, "unitPrice": 12.50 },
            { "sku": "PEN-1", "quantity": 1, "unitPrice": 1.25 }
          ]
        }
        """);

    internal static readonly DecodeFixture ValidWithNote = new(
        "valid-with-note",
        """
        {
          "orderId": "0f8fad5b-d9cb-469f-a165-70867728950e",
          "customer": {
            "name": "Ada",
            "email": "ada@example.com",
            "addresses": [
              { "line": "1 Main St", "city": "Springfield", "postalCode": "12345" }
            ]
          },
          "lines": [
            { "sku": "BOOK-1", "quantity": 2, "unitPrice": 12.50 }
          ],
          "note": "gift"
        }
        """);

    internal static readonly DecodeFixture ValidEmptyAddresses = new(
        "valid-empty-addresses",
        """
        {
          "orderId": "0f8fad5b-d9cb-469f-a165-70867728950e",
          "customer": {
            "name": "Ada",
            "email": "ada@example.com",
            "addresses": []
          },
          "lines": [
            { "sku": "BOOK-1", "quantity": 2, "unitPrice": 12.50 }
          ]
        }
        """);

    internal static readonly DecodeFixture EmptyNote = new(
        "empty-note",
        """
        {
          "orderId": "0f8fad5b-d9cb-469f-a165-70867728950e",
          "customer": {
            "name": "Ada",
            "email": "ada@example.com",
            "addresses": []
          },
          "lines": [
            { "sku": "BOOK-1", "quantity": 2, "unitPrice": 12.50 }
          ],
          "note": ""
        }
        """);

    internal static readonly DecodeFixture MissingId = new(
        "missing-id",
        """
        {
          "customer": {
            "name": "Ada",
            "email": "ada@example.com",
            "addresses": []
          },
          "lines": [
            { "sku": "BOOK-1", "quantity": 2, "unitPrice": 12.50 }
          ]
        }
        """);

    internal static readonly DecodeFixture NullId = new(
        "null-id",
        """
        {
          "orderId": null,
          "customer": {
            "name": "Ada",
            "email": "ada@example.com",
            "addresses": []
          },
          "lines": [
            { "sku": "BOOK-1", "quantity": 2, "unitPrice": 12.50 }
          ]
        }
        """);

    internal static readonly DecodeFixture WrongKinds = new(
        "wrong-kinds",
        """
        {
          "orderId": 42,
          "customer": { "name": 42, "email": "a@b", "addresses": [] },
          "lines": {},
          "note": 42
        }
        """);

    internal static readonly DecodeFixture Invalid = new(
        "invalid",
        """
        {
          "orderId": "not-a-guid",
          "customer": {
            "name": "",
            "email": "not-an-email",
            "addresses": [
              { "line": "1 Main St", "city": "Springfield", "postalCode": "12345" },
              { "line": "2 Main St", "city": "Springfield", "postalCode": null }
            ]
          },
          "lines": [
            { "sku": "BOOK-1", "quantity": 2, "unitPrice": 12.50 },
            { "sku": "", "quantity": 0, "unitPrice": 1.25 },
            { "sku": "PEN-1", "quantity": 1, "unitPrice": 1.25 }
          ],
          "note": null
        }
        """);

    internal static readonly DecodeFixture EmptyNameAndLines = new(
        "empty-name-and-lines",
        """
        {
          "orderId": "0f8fad5b-d9cb-469f-a165-70867728950e",
          "customer": {
            "name": "",
            "email": "ada@example.com",
            "addresses": []
          },
          "lines": []
        }
        """);

    internal static readonly DecodeFixture MissingAddresses = new(
        "missing-addresses",
        """
        {
          "orderId": "0f8fad5b-d9cb-469f-a165-70867728950e",
          "customer": { "name": "Ada", "email": "ada@example.com" },
          "lines": [
            { "sku": "BOOK-1", "quantity": 2, "unitPrice": 12.50 }
          ]
        }
        """);

    internal static readonly DecodeFixture NullAddresses = new(
        "null-addresses",
        """
        {
          "orderId": "0f8fad5b-d9cb-469f-a165-70867728950e",
          "customer": { "name": "Ada", "email": "ada@example.com", "addresses": null },
          "lines": [
            { "sku": "BOOK-1", "quantity": 2, "unitPrice": 12.50 }
          ]
        }
        """);

    internal static readonly DecodeFixture NonObjectCustomer = new(
        "non-object-customer",
        """
        {
          "orderId": "0f8fad5b-d9cb-469f-a165-70867728950e",
          "customer": "x",
          "lines": [
            { "sku": "BOOK-1", "quantity": 2, "unitPrice": 12.50 }
          ]
        }
        """);

    internal static readonly DecodeFixture MalformedLineNumbers = new(
        "malformed-line-numbers",
        """
        {
          "orderId": "0f8fad5b-d9cb-469f-a165-70867728950e",
          "customer": { "name": "Ada", "email": "ada@example.com", "addresses": [] },
          "lines": [
            { "sku": "A", "quantity": 1.5, "unitPrice": 1.0 },
            { "sku": "B", "quantity": 1, "unitPrice": -1.0 },
            { "sku": "", "quantity": "two", "unitPrice": "three" }
          ]
        }
        """);

    internal static readonly DecodeFixture EmptyEmail = new(
        "empty-email",
        """
        {
          "orderId": "0f8fad5b-d9cb-469f-a165-70867728950e",
          "customer": {
            "name": "Ada",
            "email": "",
            "addresses": []
          },
          "lines": [
            { "sku": "BOOK-1", "quantity": 2, "unitPrice": 12.50 }
          ]
        }
        """);

    internal static readonly DecodeFixture RootNull = new("root-null", "null");

    internal static readonly DecodeFixture RootArray = new("root-array", "[]");

    internal static IReadOnlyList<DecodeFixture> All { get; } =
    [
        Valid,
        ValidWithNote,
        ValidEmptyAddresses,
        EmptyNote,
        MissingId,
        NullId,
        WrongKinds,
        Invalid,
        EmptyNameAndLines,
        MissingAddresses,
        NullAddresses,
        NonObjectCustomer,
        MalformedLineNumbers,
        EmptyEmail,
        RootNull,
        RootArray,
    ];
}
