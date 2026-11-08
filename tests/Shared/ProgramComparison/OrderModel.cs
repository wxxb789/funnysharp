namespace FunnySharp.TestSupport.ProgramComparison;

internal enum InventoryStatus
{
    Idle,
    Reserving,
    Reserved,
    Unavailable,
    Released,
}

internal sealed record InventoryState(
    string Sku,
    int RequestedQuantity,
    int ReservedQuantity,
    InventoryStatus Status,
    long PendingVersion);

internal enum PaymentStatus
{
    Idle,
    Charging,
    Captured,
    Declined,
    TimedOut,
    Failed,
    Refunded,
}

internal sealed record PaymentState(
    decimal Amount,
    PaymentStatus Status,
    long PendingVersion,
    DateTimeOffset Deadline);

internal enum CancellationStatus
{
    None,
    Cancelling,
    RefundFailed,
}

internal sealed record CancellationState(
    CancellationStatus Status,
    string? Reason,
    long Version);

internal sealed record OrderModel(
    string OrderId,
    InventoryState Inventory,
    PaymentState Payment,
    CancellationState Cancellation);
