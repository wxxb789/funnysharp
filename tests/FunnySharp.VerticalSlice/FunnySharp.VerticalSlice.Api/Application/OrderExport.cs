using FunnySharp.VerticalSlice.Domain;

namespace FunnySharp.VerticalSlice.Application;

/// <summary>One exported row: the order plus the running totals at that point of the stream.</summary>
public sealed record ExportRow(
    string OrderId,
    string Status,
    decimal Total,
    string Currency,
    int Sequence,
    decimal RevenueToDate);

/// <summary>The running cursor carried by the export stream; it never buffers the stream.</summary>
public readonly record struct ExportCursor(int Sequence, decimal Revenue, ExportRow? Row)
{
    public static ExportCursor Start { get; } = new(0, 0m, null);

    public ExportCursor Advance(OrderRecord record)
    {
        if (record.Order.Status == OrderStatus.Draft)
        {
            return this with { Row = null };
        }

        var sequence = Sequence + 1;
        var revenue = Revenue + record.Order.Total.Amount;
        return new ExportCursor(
            sequence,
            revenue,
            new ExportRow(
                record.Order.Id.Value,
                record.Order.Status.ToString(),
                record.Order.Total.Amount,
                record.Order.Total.Currency,
                sequence,
                revenue));
    }
}

/// <summary>The streaming export pipeline: a running aggregate over an asynchronous source.</summary>
public static class OrderExport
{
    public static IAsyncEnumerable<ExportRow> RowsAsync(IOrderStore store, CancellationToken cancellationToken) =>
        store.StreamAsync(cancellationToken)
            .Scan(ExportCursor.Start, static (cursor, record) => cursor.Advance(record))
            .Choose(static cursor => Option.FromNullable(cursor.Row));
}
