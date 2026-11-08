namespace FunnySharp.TestSupport.ProgramComparison;

internal abstract record OrderCommand;

internal sealed record ReserveInventoryCommand(
    string OrderId,
    string Sku,
    int Quantity,
    long Version) : OrderCommand;

internal sealed record ChargePaymentCommand(
    string OrderId,
    decimal Amount,
    long Version) : OrderCommand;

internal sealed record ReleaseInventoryCommand(
    string OrderId,
    string Sku,
    int Quantity) : OrderCommand;

internal sealed record RefundPaymentCommand(
    string OrderId,
    decimal Amount) : OrderCommand;

internal sealed record NotifyCustomerCommand(
    string OrderId,
    string Message) : OrderCommand;
