using FunnySharp.VerticalSlice.Domain;

namespace FunnySharp.VerticalSlice.Application;

/// <summary>The stock port: absence is an <see cref="Option{T}"/>, a rejected reservation is typed.</summary>
public interface IInventoryService
{
    ValueTask<Option<int>> AvailableAsync(Sku sku, CancellationToken cancellationToken);

    ValueTask<UnitResult<OrderError>> ReserveAsync(Sku sku, Quantity quantity, CancellationToken cancellationToken);

    ValueTask<UnitResult<OrderError>> ReleaseAsync(Sku sku, Quantity quantity, CancellationToken cancellationToken);
}
