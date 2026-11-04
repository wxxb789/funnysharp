using System.Text.Json;
using FunnySharp;

internal static class DomainExamples
{
    private readonly struct OrderIdTag;

    private readonly struct CustomerIdTag;

    private readonly struct SkuTag;

    private readonly struct QuantityTag;

    private readonly struct TrackingTag;

    private static readonly Domain<string, OrderIdTag> OrderIds =
        Domain.For<string, OrderIdTag>(
            Contract.NonEmptyString.Ensure(
                static value => value.StartsWith("ORD-", StringComparison.Ordinal),
                "order identifier must start with ORD-"),
            "order identifier");

    private static readonly Domain<string, CustomerIdTag> CustomerIds =
        Domain.For<string, CustomerIdTag>(
            Contract.NonEmptyString.Ensure(
                static value => value.StartsWith("CUS-", StringComparison.Ordinal),
                "customer identifier must start with CUS-"),
            "customer identifier");

    private static readonly Domain<string, SkuTag> Skus =
        Domain.For<string, SkuTag>(
            Contract.NonEmptyString.Ensure(
                static value => value.StartsWith("SKU-", StringComparison.Ordinal),
                "sku must start with SKU-"),
            "SKU");

    private static readonly Domain<int, QuantityTag> Quantities =
        Domain.For<int, QuantityTag>(Contract.Int32Range(1, 100), "quantity");

    private static readonly Domain<string, TrackingTag> TrackingCodes =
        Domain.For<string, TrackingTag>(
            Contract.NonEmptyString.Ensure(
                static value => value.StartsWith("TRK-", StringComparison.Ordinal),
                "tracking code must start with TRK-"),
            "tracking code");

    internal static void Verify()
    {
        var orderId = Require(OrderIds.Validate("ORD-100"));
        var customerId = Require(CustomerIds.Validate("CUS-100"));

        ExampleAssertions.Equal("ORD-100", orderId.Value);
        ExampleAssertions.Equal("CUS-100", customerId.Value);

        var invalidQuantity = Quantities.Validate(0);
        ExampleAssertions.True(
            invalidQuantity.TryGetErrors(out var quantityErrors),
            "A zero quantity must be rejected at the domain boundary.");
        var quantityError = quantityErrors![0];
        ExampleAssertions.Equal(DecodeErrorKind.InvalidValue, quantityError.Kind);
        ExampleAssertions.Equal("value must be between 1 and 100", quantityError.Detail!);

        var invalidSku = Skus.Validate("BOOK-1");
        ExampleAssertions.True(
            invalidSku.TryGetErrors(out var skuErrors),
            "A SKU without its prefix must be rejected.");
        ExampleAssertions.Equal("sku must start with SKU-", skuErrors![0].Detail!);

        var decoded = Decode(
            """
            {
              "orderId": "ORD-100",
              "customerId": "CUS-100",
              "line": { "sku": "SKU-BOOK", "quantity": 2 },
              "token": "tok-valid"
            }
            """);
        var placed = decoded.Submit();
        ExampleAssertions.True(placed.TryGetValue(out var placedOrder), "A valid order must be placed.");
        var shipped = placedOrder!.Ship(Require(TrackingCodes.Validate("TRK-ORD-100")));
        ExampleAssertions.Equal("TRK-ORD-100", shipped.Tracking.Value);
        ExampleAssertions.Equal(12.50m, shipped.UnitPrice);

        var unknownSku = Decode(
            """
            {
              "orderId": "ORD-200",
              "customerId": "CUS-200",
              "line": { "sku": "SKU-UNKNOWN", "quantity": 1 },
              "token": "tok-valid"
            }
            """).Submit();
        ExampleAssertions.True(unknownSku.TryGetError(out var pricingError), "Unknown SKUs must fail pricing.");
        ExampleAssertions.Equal("pricing", pricingError!.Source);
        ExampleAssertions.Equal("unknown-sku", pricingError.Code);

        var declined = Decode(
            """
            {
              "orderId": "ORD-300",
              "customerId": "CUS-300",
              "line": { "sku": "SKU-BOOK", "quantity": 1 },
              "token": "tok-declined"
            }
            """).Submit();
        ExampleAssertions.True(declined.TryGetError(out var paymentError), "Declined payments must fail.");
        ExampleAssertions.Equal("payment", paymentError!.Source);
        ExampleAssertions.Equal("payment-declined", paymentError.Code);

        // A default wrapper is uninitialized by design; FS1001 reports every default creation of
        // a carrier with no valid default, so this deliberate probe is suppressed.
#pragma warning disable FS1001
        var defaultValue = default(DomainValue<int, QuantityTag>);
#pragma warning restore FS1001
        ExampleAssertions.True(!defaultValue.IsInitialized, "A default domain value must be uninitialized.");
        ExampleAssertions.Throws<InvalidOperationException>(() => _ = defaultValue.Value);

        var cancelled = placedOrder.Cancel("customer-request");
        ExampleAssertions.Equal("customer-request", cancelled.Reason);
    }

    private static DraftOrder Decode(string json)
    {
        using var document = JsonDocument.Parse(json);
        return Require(DraftDecoder.Decode(document.RootElement));
    }

    private static readonly Decoder<DraftOrder> DraftDecoder = BuildDecoder();

    private static Decoder<DraftOrder> BuildDecoder()
    {
        var lineDecoder = Decoder.Object(
            Decoder.Field("sku", RequireDecoder(Skus)),
            Decoder.Field("quantity", RequireDecoder(Quantities)),
            static (sku, quantity) => new DraftLine(sku, quantity));

        return Decoder.Object(
            Decoder.Field("orderId", RequireDecoder(OrderIds)),
            Decoder.Field("customerId", RequireDecoder(CustomerIds)),
            Decoder.Field("line", lineDecoder),
            Decoder.Field("token", Decoder.NonEmptyString),
            static (orderId, customerId, line, token) =>
                new DraftOrder(orderId, customerId, line.Sku, line.Quantity, token));
    }

    private static Decoder<DomainValue<TValue, TTag>> RequireDecoder<TValue, TTag>(
        Domain<TValue, TTag> domain)
        where TValue : notnull
        where TTag : notnull =>
        domain.CreateDecoder().Match(
            static decoder => decoder,
            static limitation => throw new InvalidOperationException(limitation.ToString()));

    private static T Require<T>(Validation<T, DecodeError> validation) =>
        validation.Match(
            static value => value,
            static errors => throw new InvalidOperationException(
                string.Join("; ", errors.Select(static error => error.ToString()))));

    private sealed record DraftLine(
        DomainValue<string, SkuTag> Sku,
        DomainValue<int, QuantityTag> Quantity);

    private sealed record DraftOrder(
        DomainValue<string, OrderIdTag> Id,
        DomainValue<string, CustomerIdTag> CustomerId,
        DomainValue<string, SkuTag> Sku,
        DomainValue<int, QuantityTag> Quantity,
        string PaymentToken)
    {
        internal Result<PlacedOrder, WorkflowError> Submit()
        {
            var priced = Pricing.Price(Sku.Value, Quantity.Value)
                .MapError(static failure => (WorkflowError)new PricingFailed(failure));
            var reserved = Inventory.Reserve(Sku.Value, Quantity.Value)
                .MapError(static failure => (WorkflowError)new InventoryFailed(failure));

            return priced
                .Zip(reserved)
                .Bind(values => Payments.Authorize(PaymentToken, values.First * Quantity.Value)
                    .MapError(static failure => (WorkflowError)new PaymentFailed(failure))
                    .Map(authorization => new PlacedOrder(
                        Id,
                        Sku,
                        Quantity,
                        values.First,
                        values.Second,
                        authorization)));
        }

        internal CancelledOrder Cancel(string reason) => new(Id, reason);
    }

    private sealed record PlacedOrder(
        DomainValue<string, OrderIdTag> Id,
        DomainValue<string, SkuTag> Sku,
        DomainValue<int, QuantityTag> Quantity,
        decimal UnitPrice,
        string ReservationCode,
        string AuthorizationCode)
    {
        internal ShippedOrder Ship(DomainValue<string, TrackingTag> tracking) =>
            new(Id, Quantity, UnitPrice, ReservationCode, AuthorizationCode, tracking);

        internal CancelledOrder Cancel(string reason) => new(Id, reason);
    }

    private sealed record ShippedOrder(
        DomainValue<string, OrderIdTag> Id,
        DomainValue<int, QuantityTag> Quantity,
        decimal UnitPrice,
        string ReservationCode,
        string AuthorizationCode,
        DomainValue<string, TrackingTag> Tracking);

    private sealed record CancelledOrder(DomainValue<string, OrderIdTag> Id, string Reason);

    private sealed record PricingFailure(string Sku, int Quantity, string Code);

    private sealed record InventoryFailure(string Sku, int Quantity, string Code);

    private sealed record PaymentFailure(string Code);

    private abstract record WorkflowError
    {
        internal abstract string Source { get; }

        internal abstract string Code { get; }
    }

    private sealed record PricingFailed(PricingFailure Failure) : WorkflowError
    {
        internal override string Source => "pricing";

        internal override string Code => Failure.Code;
    }

    private sealed record InventoryFailed(InventoryFailure Failure) : WorkflowError
    {
        internal override string Source => "inventory";

        internal override string Code => Failure.Code;
    }

    private sealed record PaymentFailed(PaymentFailure Failure) : WorkflowError
    {
        internal override string Source => "payment";

        internal override string Code => Failure.Code;
    }

    private static class Pricing
    {
        internal static Result<decimal, PricingFailure> Price(string sku, int quantity) =>
            sku == "SKU-BOOK"
                ? Result<decimal, PricingFailure>.Success(12.50m)
                : Result<decimal, PricingFailure>.Failure(
                    new PricingFailure(sku, quantity, "unknown-sku"));
    }

    private static class Inventory
    {
        internal static Result<string, InventoryFailure> Reserve(string sku, int quantity) =>
            quantity <= 8
                ? Result<string, InventoryFailure>.Success("RSV-100")
                : Result<string, InventoryFailure>.Failure(
                    new InventoryFailure(sku, quantity, "insufficient-stock"));
    }

    private static class Payments
    {
        internal static Result<string, PaymentFailure> Authorize(string token, decimal amount) =>
            token == "tok-valid" && amount > 0
                ? Result<string, PaymentFailure>.Success("AUTH-100")
                : Result<string, PaymentFailure>.Failure(new PaymentFailure("payment-declined"));
    }
}
