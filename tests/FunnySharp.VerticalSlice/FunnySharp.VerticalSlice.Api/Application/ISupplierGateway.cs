using FunnySharp.VerticalSlice.Domain;

namespace FunnySharp.VerticalSlice.Application;

/// <summary>
/// The supplier port. Discovery is a lazy stream so the caller can bound how many quotes run at once,
/// and each quote is one cancellable round trip.
/// </summary>
public interface ISupplierGateway
{
    IAsyncEnumerable<SupplierRef> FindSuppliersAsync(Sku sku, CancellationToken cancellationToken);

    ValueTask<Result<SupplierQuote, OrderError>> QuoteAsync(
        SupplierRef supplier,
        Sku sku,
        Quantity quantity,
        CancellationToken cancellationToken);
}
