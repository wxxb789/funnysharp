using FunnySharp.VerticalSlice.Application;
using FunnySharp.VerticalSlice.Domain;

namespace FunnySharp.VerticalSlice.Http;

/// <summary>One requested line exactly as it arrives on the wire, before refinement.</summary>
public sealed record PlaceOrderLineRequest(string? Sku, int Quantity);

/// <summary>The placement request body.</summary>
public sealed record PlaceOrderRequest(string? CustomerId, PlaceOrderLineRequest[]? Lines);

/// <summary>The payment request body.</summary>
public sealed record PayOrderRequest(string? PaymentMethod);

/// <summary>The shipment request body.</summary>
public sealed record ShipOrderRequest(string? TrackingCode);

/// <summary>The cancellation request body.</summary>
public sealed record CancelOrderRequest(string? Reason);

/// <summary>A validated quote query.</summary>
public sealed record QuoteQuery(Sku Sku, Quantity Quantity);

/// <summary>One line of an order response.</summary>
public sealed record OrderLineResponse(string Sku, int Quantity, decimal UnitPrice)
{
    public static OrderLineResponse From(OrderLine line) => new(line.Sku.Value, line.Quantity.Value, line.UnitPrice.Amount);
}

/// <summary>The full order projection.</summary>
public sealed record OrderResponse(
    string OrderId,
    string CustomerId,
    string Status,
    int Revision,
    decimal Total,
    string Currency,
    string CreatedAt,
    string UpdatedAt,
    string? PaymentReference,
    string? TrackingCode,
    string? CancellationReason,
    OrderLineResponse[] Lines)
{
    public static OrderResponse From(Order order) => new(
        order.Id.Value,
        order.CustomerId.Value,
        order.Status.ToString(),
        order.Revision,
        order.Total.Amount,
        order.Total.Currency,
        order.CreatedAt.ToString("O"),
        order.UpdatedAt.ToString("O"),
        order.PaymentReference,
        order.TrackingCode,
        order.CancellationReason,
        [OrderLineResponse.From(order.Lines.First), .. order.Lines.Rest.Select(OrderLineResponse.From)]);
}

/// <summary>The compact receipt returned when a lifecycle event is applied.</summary>
public sealed record OrderReceiptResponse(
    string OrderId,
    string Status,
    int Revision,
    decimal Total,
    string Currency,
    string? PaymentReference,
    string? TrackingCode)
{
    public static OrderReceiptResponse From(Order order) => new(
        order.Id.Value,
        order.Status.ToString(),
        order.Revision,
        order.Total.Amount,
        order.Total.Currency,
        order.PaymentReference,
        order.TrackingCode);
}

/// <summary>The payment receipt.</summary>
public sealed record PaymentReceiptResponse(string OrderId, string Status, string? PaymentReference, decimal Amount)
{
    public static PaymentReceiptResponse From(Order order) => new(
        order.Id.Value,
        order.Status.ToString(),
        order.PaymentReference,
        order.Total.Amount);
}

/// <summary>One replayed timeline entry projected to the fields a client sees.</summary>
public sealed record TimelineEntryResponse(string Event, string Status, string OccurredAt, string[] Commands);

/// <summary>The timeline response, including whether replay still agrees with the stored projection.</summary>
public sealed record TimelineResponse(string Status, int Revision, bool MatchesStoredProjection, TimelineEntryResponse[] Entries);

/// <summary>The cheapest supplier answer for a SKU.</summary>
public sealed record QuoteResponse(
    string Sku,
    int Quantity,
    string Supplier,
    decimal UnitPrice,
    string Currency,
    int LeadTimeDays,
    string[] UnavailableSuppliers)
{
    public static QuoteResponse From(QuoteBoard board) => new(
        board.Sku.Value,
        board.Quantity.Value,
        board.Best.Supplier,
        board.Best.UnitPrice.Amount,
        board.Best.UnitPrice.Currency,
        board.Best.LeadTime.Days,
        [.. board.UnavailableSuppliers]);
}

/// <summary>One line of a reconciliation report.</summary>
public sealed record ReconciliationLineResponse(string Sku, int Ordered, int Available);

/// <summary>The reconciliation report returned by the effect endpoint.</summary>
public sealed record ReconciliationResponse(
    string OrderId,
    string Status,
    int Revision,
    bool HistoryReplaysToStoredProjection,
    ReconciliationLineResponse[] Lines)
{
    public static ReconciliationResponse From(ReconciliationReport report) => new(
        report.OrderId.Value,
        report.Status.ToString(),
        report.Revision,
        report.HistoryReplaysToStoredProjection,
        [.. report.Lines.Select(static line => new ReconciliationLineResponse(line.Sku, line.Ordered, line.Available))]);
}
