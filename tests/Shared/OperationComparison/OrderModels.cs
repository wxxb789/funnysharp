namespace FunnySharp.TestSupport.OperationComparison;

internal sealed record PlaceOrderLine(string Sku, int Quantity);

internal sealed record PlaceOrderRequest(string CustomerId, IReadOnlyList<PlaceOrderLine> Lines);

internal sealed record Customer(string Id, string Name, int CreditLimitCents, bool Active);

internal sealed record CatalogItem(string Sku, int UnitPriceCents, bool Active);

internal sealed record PricedLine(string Sku, int Quantity, int UnitPriceCents)
{
    internal int LineTotalCents => checked(Quantity * UnitPriceCents);
}

internal sealed record OrderState(
    PlaceOrderRequest Request,
    Customer? Customer = null,
    IReadOnlyDictionary<string, CatalogItem>? Catalog = null,
    long OrderId = 0,
    int TotalCents = 0,
    IReadOnlyList<PricedLine>? Lines = null,
    string? PaymentReference = null);

internal sealed record PaymentAttempt(
    long OrderId,
    int AmountCents,
    string? Reference,
    bool Authorized);

internal abstract record OrderError
{
    private OrderError()
    {
    }

    internal sealed record CustomerNotFound(string CustomerId) : OrderError;

    internal sealed record CustomerInactive(string CustomerId) : OrderError;

    internal sealed record CatalogItemNotFound(string Sku) : OrderError;

    internal sealed record CatalogItemInactive(string Sku) : OrderError;

    internal sealed record EmptyOrder : OrderError;

    internal sealed record CreditLimitExceeded(int TotalCents, int CreditLimitCents) : OrderError;

    internal sealed record InsufficientStock(string Sku, int Requested, int Available) : OrderError;

    internal sealed record PaymentDeclined(string Reason) : OrderError;

    internal sealed record PaymentRecordConflict(string Reference) : OrderError;

    internal sealed record PaymentReferenceMissing : OrderError;
}
