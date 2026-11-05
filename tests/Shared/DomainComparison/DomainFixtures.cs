using FunnySharp.TestSupport;

namespace FunnySharp.TestSupport.Domains;

internal static class DomainFixtures
{
    internal static readonly DecodeFixture Valid = new(
        "valid",
        """
        {
          "orderId": "ORD-100",
          "customerId": "CUS-100",
          "line": { "sku": "SKU-BOOK", "quantity": 2 },
          "payment": { "token": "tok-valid" }
        }
        """);

    internal static readonly DecodeFixture ValidPen = new(
        "valid-pen",
        """
        {
          "orderId": "ORD-200",
          "customerId": "CUS-200",
          "line": { "sku": "SKU-PEN", "quantity": 3 },
          "payment": { "token": "tok-valid" }
        }
        """);

    internal static readonly DecodeFixture UnknownSku = new(
        "unknown-sku",
        """
        {
          "orderId": "ORD-300",
          "customerId": "CUS-300",
          "line": { "sku": "SKU-UNKNOWN", "quantity": 2 },
          "payment": { "token": "tok-valid" }
        }
        """);

    internal static readonly DecodeFixture OutOfStock = new(
        "out-of-stock",
        """
        {
          "orderId": "ORD-400",
          "customerId": "CUS-400",
          "line": { "sku": "SKU-BOOK", "quantity": 9 },
          "payment": { "token": "tok-valid" }
        }
        """);

    internal static readonly DecodeFixture PaymentDeclined = new(
        "payment-declined",
        """
        {
          "orderId": "ORD-500",
          "customerId": "CUS-500",
          "line": { "sku": "SKU-BOOK", "quantity": 1 },
          "payment": { "token": "tok-declined" }
        }
        """);

    internal static readonly DecodeFixture InvalidOrderId = new(
        "invalid-order-id",
        """
        {
          "orderId": "ORDER-100",
          "customerId": "CUS-100",
          "line": { "sku": "SKU-BOOK", "quantity": 2 },
          "payment": { "token": "tok-valid" }
        }
        """);

    internal static readonly DecodeFixture InvalidCustomerId = new(
        "invalid-customer-id",
        """
        {
          "orderId": "ORD-100",
          "customerId": "CUSTOMER-100",
          "line": { "sku": "SKU-BOOK", "quantity": 2 },
          "payment": { "token": "tok-valid" }
        }
        """);

    internal static readonly DecodeFixture EmptySku = new(
        "empty-sku",
        """
        {
          "orderId": "ORD-100",
          "customerId": "CUS-100",
          "line": { "sku": "", "quantity": 2 },
          "payment": { "token": "tok-valid" }
        }
        """);

    internal static readonly DecodeFixture ZeroQuantity = new(
        "zero-quantity",
        """
        {
          "orderId": "ORD-100",
          "customerId": "CUS-100",
          "line": { "sku": "SKU-BOOK", "quantity": 0 },
          "payment": { "token": "tok-valid" }
        }
        """);

    internal static readonly DecodeFixture MissingQuantity = new(
        "missing-quantity",
        """
        {
          "orderId": "ORD-100",
          "customerId": "CUS-100",
          "line": { "sku": "SKU-BOOK" },
          "payment": { "token": "tok-valid" }
        }
        """);

    internal static readonly DecodeFixture WrongKindQuantity = new(
        "wrong-kind-quantity",
        """
        {
          "orderId": "ORD-100",
          "customerId": "CUS-100",
          "line": { "sku": "SKU-BOOK", "quantity": "two" },
          "payment": { "token": "tok-valid" }
        }
        """);

    internal static readonly DecodeFixture FractionalQuantity = new(
        "fractional-quantity",
        """
        {
          "orderId": "ORD-100",
          "customerId": "CUS-100",
          "line": { "sku": "SKU-BOOK", "quantity": 1.5 },
          "payment": { "token": "tok-valid" }
        }
        """);

    internal static readonly DecodeFixture NullCustomerId = new(
        "null-customer-id",
        """
        {
          "orderId": "ORD-100",
          "customerId": null,
          "line": { "sku": "SKU-BOOK", "quantity": 2 },
          "payment": { "token": "tok-valid" }
        }
        """);

    internal static readonly DecodeFixture RootArray = new("root-array", "[]");

    internal static readonly DecodeFixture EmptyObject = new("empty-object", "{}");

    internal static readonly DecodeFixture MultipleInvalid = new(
        "multiple-invalid",
        """
        {
          "orderId": "ORDER-100",
          "customerId": null,
          "line": { "sku": "", "quantity": 0 },
          "payment": { "token": "" }
        }
        """);

    internal static readonly DecodeFixture ExtraField = new(
        "extra-field",
        """
        {
          "orderId": "ORD-600",
          "customerId": "CUS-600",
          "line": { "sku": "SKU-BOOK", "quantity": 2 },
          "payment": { "token": "tok-valid" },
          "ignored": true
        }
        """);

    internal static readonly DecodeFixture DuplicateOrderId = new(
        "duplicate-order-id",
        """
        {
          "orderId": "ORD-700",
          "orderId": "BAD",
          "customerId": "CUS-700",
          "line": { "sku": "SKU-BOOK", "quantity": 2 },
          "payment": { "token": "tok-valid" }
        }
        """);

    internal static readonly IReadOnlyList<DecodeFixture> All =
    [
        Valid,
        ValidPen,
        UnknownSku,
        OutOfStock,
        PaymentDeclined,
        InvalidOrderId,
        InvalidCustomerId,
        EmptySku,
        ZeroQuantity,
        MissingQuantity,
        WrongKindQuantity,
        FractionalQuantity,
        NullCustomerId,
        RootArray,
        EmptyObject,
        MultipleInvalid,
        ExtraField,
        DuplicateOrderId,
    ];
}
