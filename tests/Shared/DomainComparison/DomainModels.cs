using System.Globalization;
using FunnySharp;

namespace FunnySharp.TestSupport.Domains;

using OrderIdValue = DomainValue<string, OrderIdTag>;
using CustomerIdValue = DomainValue<string, CustomerIdTag>;
using SkuValue = DomainValue<string, SkuTag>;
using QuantityValue = DomainValue<int, QuantityTag>;
using TrackingValue = DomainValue<string, TrackingTag>;

internal readonly struct OrderIdTag;

internal readonly struct CustomerIdTag;

internal readonly struct SkuTag;

internal readonly struct QuantityTag;

internal readonly struct TrackingTag;

internal static class Domains
{
    internal static readonly Domain<string, OrderIdTag> OrderId =
        Domain.For<string, OrderIdTag>(
            Contract.NonEmptyString.Ensure(
                static value => value.StartsWith("ORD-", StringComparison.Ordinal),
                "order identifier must start with ORD-"),
            "order identifier");

    internal static readonly Domain<string, CustomerIdTag> CustomerId =
        Domain.For<string, CustomerIdTag>(
            Contract.NonEmptyString.Ensure(
                static value => value.StartsWith("CUS-", StringComparison.Ordinal),
                "customer identifier must start with CUS-"),
            "customer identifier");

    internal static readonly Domain<string, SkuTag> Sku =
        Domain.For<string, SkuTag>(
            Contract.NonEmptyString.Ensure(
                static value => value.StartsWith("SKU-", StringComparison.Ordinal),
                "sku must start with SKU-"),
            "SKU");

    internal static readonly Domain<int, QuantityTag> Quantity =
        Domain.For<int, QuantityTag>(Contract.Int32Range(1, 100), "quantity");

    internal static readonly Domain<string, TrackingTag> Tracking =
        Domain.For<string, TrackingTag>(
            Contract.NonEmptyString.Ensure(
                static value => value.StartsWith("TRK-", StringComparison.Ordinal),
                "tracking code must start with TRK-"),
            "tracking code");

    internal static readonly Decoder<OrderIdValue> OrderIdDecoder = RequireDecoder(OrderId.CreateDecoder());

    internal static readonly Decoder<CustomerIdValue> CustomerIdDecoder =
        RequireDecoder(CustomerId.CreateDecoder());

    internal static readonly Decoder<SkuValue> SkuDecoder = RequireDecoder(Sku.CreateDecoder());

    internal static readonly Decoder<QuantityValue> QuantityDecoder = RequireDecoder(Quantity.CreateDecoder());

    private static Decoder<T> RequireDecoder<T>(Result<Decoder<T>, ContractLimitation> created) =>
        created.Match(
            static decoder => decoder,
            static limitation => throw new InvalidOperationException(limitation.ToString()));

    internal static TrackingValue TrackingCode(string orderId) =>
        Tracking.Validate("TRK-" + orderId).Match(
            static value => value,
            static errors => throw new InvalidOperationException(
                string.Join("; ", errors.Select(static error => error.ToString()))));
}

internal sealed record PricingFailure(string Sku, int Quantity, string Code);

internal sealed record InventoryFailure(string Sku, int Quantity, string Code);

internal sealed record PaymentFailure(string Code);

internal static class OrderModules
{
    internal static Result<decimal, PricingFailure> Price(string sku, int quantity) =>
        sku switch
        {
            "SKU-BOOK" => Result<decimal, PricingFailure>.Success(12.50m),
            "SKU-PEN" => Result<decimal, PricingFailure>.Success(1.25m),
            _ => Result<decimal, PricingFailure>.Failure(
                new PricingFailure(sku, quantity, "unknown-sku")),
        };

    internal static Result<string, InventoryFailure> Reserve(string sku, int quantity) =>
        quantity <= 8
            ? Result<string, InventoryFailure>.Success("RSV-100")
            : Result<string, InventoryFailure>.Failure(
                new InventoryFailure(sku, quantity, "insufficient-stock"));

    internal static Result<string, PaymentFailure> Authorize(string token, decimal amount) =>
        token == "tok-valid" && amount > 0
            ? Result<string, PaymentFailure>.Success("AUTH-100")
            : Result<string, PaymentFailure>.Failure(new PaymentFailure("payment-declined"));

    internal static string Format(PricingFailure failure) =>
        failure.Sku + "|" + failure.Quantity.ToString(CultureInfo.InvariantCulture);

    internal static string Format(InventoryFailure failure) =>
        failure.Sku + "|" + failure.Quantity.ToString(CultureInfo.InvariantCulture);
}

internal interface IWorkflowFailure
{
    string Source { get; }

    string Code { get; }
}

internal static class WorkflowFailureExtensions
{
    internal static WorkflowFailureView ToView(
        this IWorkflowFailure failure,
        string detail,
        string path = "") =>
        new(failure.Source, failure.Code, path, detail);
}

internal abstract record OrderWorkflowError : IWorkflowFailure
{
    public abstract string Source { get; }

    public abstract string Code { get; }

    internal abstract string Detail { get; }

    internal virtual string Path => string.Empty;
}

internal sealed record RequestRejected(IReadOnlyList<DecodeError> Errors) : OrderWorkflowError
{
    public override string Source => "request";

    public override string Code => "invalid-request";

    internal override string Detail =>
        string.Join("; ", Errors.Select(static error => error.ToString()));
}

internal sealed record PricingRejected(OrderIdValue OrderId, PricingFailure Failure) : OrderWorkflowError
{
    public override string Source => "pricing";

    public override string Code => Failure.Code;

    internal override string Detail => OrderModules.Format(Failure);
}

internal sealed record InventoryRejected(OrderIdValue OrderId, InventoryFailure Failure) : OrderWorkflowError
{
    public override string Source => "inventory";

    public override string Code => Failure.Code;

    internal override string Detail => OrderModules.Format(Failure);
}

internal sealed record PaymentRejected(OrderIdValue OrderId, PaymentFailure Failure) : OrderWorkflowError
{
    public override string Source => "payment";

    public override string Code => Failure.Code;

    internal override string Detail => Failure.Code;
}

internal sealed record DraftLine(SkuValue Sku, QuantityValue Quantity);

internal sealed record PaymentDetails(string Token);

internal sealed record DraftOrder(
    OrderIdValue Id,
    CustomerIdValue CustomerId,
    SkuValue Sku,
    QuantityValue Quantity,
    string PaymentToken)
{
    internal Result<PlacedOrder, OrderWorkflowError> Submit()
    {
        var priced = OrderModules.Price(Sku.Value, Quantity.Value)
            .MapError(failure => (OrderWorkflowError)new PricingRejected(Id, failure));
        var reserved = OrderModules.Reserve(Sku.Value, Quantity.Value)
            .MapError(failure => (OrderWorkflowError)new InventoryRejected(Id, failure));

        return priced
            .Zip(reserved)
            .Bind(values => OrderModules.Authorize(PaymentToken, values.First * Quantity.Value)
                .MapError(failure => (OrderWorkflowError)new PaymentRejected(Id, failure))
                .Map(authorization => new PlacedOrder(
                    Id,
                    CustomerId,
                    Sku,
                    Quantity,
                    values.First,
                    values.Second,
                    authorization)));
    }

    internal CancelledOrder Cancel(string reason) => new(Id, reason);
}

internal sealed record PlacedOrder(
    OrderIdValue Id,
    CustomerIdValue CustomerId,
    SkuValue Sku,
    QuantityValue Quantity,
    decimal UnitPrice,
    string ReservationCode,
    string AuthorizationCode)
{
    internal ShippedOrder Ship(TrackingValue tracking) =>
        new(Id, CustomerId, Sku, Quantity, UnitPrice, ReservationCode, AuthorizationCode, tracking);

    internal CancelledOrder Cancel(string reason) => new(Id, reason);
}

internal sealed record ShippedOrder(
    OrderIdValue Id,
    CustomerIdValue CustomerId,
    SkuValue Sku,
    QuantityValue Quantity,
    decimal UnitPrice,
    string ReservationCode,
    string AuthorizationCode,
    TrackingValue Tracking);

internal sealed record CancelledOrder(OrderIdValue Id, string Reason);

internal sealed record WorkflowFailureView(string Source, string Code, string Path, string Detail);

internal sealed record WorkflowOutcome(
    string Status,
    string? OrderId,
    int Quantity,
    decimal UnitPrice,
    IReadOnlyList<WorkflowFailureView> Failures,
    string? ReservationCode,
    string? AuthorizationCode,
    string? TrackingCode,
    string? CancellationReason);
