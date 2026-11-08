namespace FunnySharp.TestSupport.ProgramComparison;

internal abstract record OrderMessage;

internal sealed record ReserveInventory(
    string OrderId,
    string Sku,
    int Quantity,
    long Version) : OrderMessage;

internal sealed record InventoryReserved(
    string OrderId,
    long Version,
    bool Accepted) : OrderMessage;

internal sealed record ChargePayment(
    string OrderId,
    decimal Amount,
    long Version,
    DateTimeOffset RequestedAt) : OrderMessage;

internal sealed record PaymentCharged(
    string OrderId,
    long Version,
    bool Captured,
    string? Failure) : OrderMessage;

internal sealed record PaymentGatewayFailed(
    string OrderId,
    long Version,
    string Reason) : OrderMessage;

internal sealed record PaymentTimeout(
    string OrderId,
    DateTimeOffset At) : OrderMessage;

internal sealed record CancelOrder(
    string OrderId,
    string Reason,
    long Version) : OrderMessage;

internal sealed record PaymentRefundFailed(
    string OrderId,
    long Version,
    string Reason) : OrderMessage;

internal sealed record UnroutedOrderMessage(string OrderId) : OrderMessage;
