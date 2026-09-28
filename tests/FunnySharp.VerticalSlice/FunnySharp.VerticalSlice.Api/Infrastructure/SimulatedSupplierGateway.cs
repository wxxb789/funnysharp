using System.Runtime.CompilerServices;
using FunnySharp.VerticalSlice.Application;
using FunnySharp.VerticalSlice.Domain;

namespace FunnySharp.VerticalSlice.Infrastructure;

/// <summary>
/// A supplier simulator: discovery is a lazy stream, each quote costs one simulated round trip whose
/// length depends on the supplier, and <c>SKU-UNLISTED</c> is refused by every supplier.
/// </summary>
public sealed class SimulatedSupplierGateway(VerticalSliceOptions options) : ISupplierGateway
{
    public async IAsyncEnumerable<SupplierRef> FindSuppliersAsync(
        Sku sku,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await SimulatedDelay.WaitAsync(options.SupplierLatency, cancellationToken);
        for (var index = 0; index < options.SupplierCount; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return new SupplierRef($"supplier-{index + 1:00}", index);
            await Task.CompletedTask.ConfigureAwait(false);
        }
    }

    public async ValueTask<Result<SupplierQuote, OrderError>> QuoteAsync(
        SupplierRef supplier,
        Sku sku,
        Quantity quantity,
        CancellationToken cancellationToken)
    {
        // The per-supplier stagger exists so a streaming client sees out-of-order completion; it is
        // disabled with the latency setting, so a zero-latency measurement observes structure only.
        await SimulatedDelay.WaitAsync(
            options.SupplierLatency == TimeSpan.Zero
                ? TimeSpan.Zero
                : options.SupplierLatency + TimeSpan.FromMilliseconds(supplier.Rank),
            cancellationToken);

        if (string.Equals(sku.Value, "SKU-UNLISTED", StringComparison.Ordinal))
        {
            return Result<SupplierQuote, OrderError>.Failure(
                new OrderError.SupplierUnavailable(supplier.Name, sku, "the SKU is not stocked"));
        }

        var unitPrice = Money.Create(
            9.95m + (StableSimulationHash.Of(sku.Value + supplier.Name) % 500) / 100m,
            options.Currency);
        if (!unitPrice.TryGetValue(out var price))
        {
            return Result<SupplierQuote, OrderError>.Failure(
                new OrderError.DependencyUnavailable("suppliers", "quote"));
        }

        return Result<SupplierQuote, OrderError>.Success(
            new SupplierQuote(
                supplier.Name,
                sku,
                quantity,
                price,
                TimeSpan.FromDays(1 + (supplier.Rank % 5))));
    }
}
