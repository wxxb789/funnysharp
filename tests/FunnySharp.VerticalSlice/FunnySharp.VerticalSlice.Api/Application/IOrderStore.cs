using FunnySharp.VerticalSlice.Domain;

namespace FunnySharp.VerticalSlice.Application;

/// <summary>The persistence port. Every member is cancellation-aware and carrier-typed.</summary>
public interface IOrderStore
{
    ValueTask CreateAsync(Order draft, CancellationToken cancellationToken);

    /// <summary>Drops a draft that never became an order, so a rejected placement leaves no state.</summary>
    ValueTask RemoveAsync(OrderId id, CancellationToken cancellationToken);

    ValueTask<Option<OrderRecord>> FindAsync(OrderId id, CancellationToken cancellationToken);

    ValueTask<UnitResult<OrderError>> PersistAsync(Order order, OrderEvent change, CancellationToken cancellationToken);

    IAsyncEnumerable<OrderRecord> StreamAsync(CancellationToken cancellationToken);
}
