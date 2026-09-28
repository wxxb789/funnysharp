using FunnySharp.VerticalSlice.Application;
using FunnySharp.VerticalSlice.Domain;

namespace FunnySharp.VerticalSlice.Infrastructure;

/// <summary>
/// An in-memory store with a monotonic revision check, so a lost update is a typed conflict instead
/// of a silent overwrite. It is the persistence stand-in for the slice; the port is what a database
/// adapter would replace.
/// </summary>
public sealed class InMemoryOrderStore(VerticalSliceOptions options) : IOrderStore
{
    private readonly Lock gate = new();
    private readonly Dictionary<OrderId, OrderRecord> orders = [];

    public async ValueTask CreateAsync(Order draft, CancellationToken cancellationToken)
    {
        await SimulatedDelay.WaitAsync(options.StoreLatency, cancellationToken);
        lock (gate)
        {
            orders[draft.Id] = new OrderRecord(draft, []);
        }
    }

    public async ValueTask RemoveAsync(OrderId id, CancellationToken cancellationToken)
    {
        await SimulatedDelay.WaitAsync(options.StoreLatency, cancellationToken);
        lock (gate)
        {
            orders.Remove(id);
        }
    }

    public async ValueTask<Option<OrderRecord>> FindAsync(OrderId id, CancellationToken cancellationToken)
    {
        await SimulatedDelay.WaitAsync(options.StoreLatency, cancellationToken);
        lock (gate)
        {
            return orders.TryGetValue(id, out var record)
                ? Option.Some(record)
                : Option.None<OrderRecord>();
        }
    }

    public async ValueTask<UnitResult<OrderError>> PersistAsync(
        Order order,
        OrderEvent change,
        CancellationToken cancellationToken)
    {
        await SimulatedDelay.WaitAsync(options.StoreLatency, cancellationToken);
        lock (gate)
        {
            if (!orders.TryGetValue(order.Id, out var current))
            {
                return UnitResult<OrderError>.Failure(new OrderError.OrderNotFound(order.Id));
            }

            if (order.Revision != current.Order.Revision + 1)
            {
                return UnitResult<OrderError>.Failure(
                    new OrderError.ConcurrencyConflict(order.Id, order.Revision, current.Order.Revision + 1));
            }

            orders[order.Id] = new OrderRecord(order, [.. current.History, change]);
        }

        return UnitResult<OrderError>.Success();
    }

    public async IAsyncEnumerable<OrderRecord> StreamAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await SimulatedDelay.WaitAsync(options.StoreLatency, cancellationToken);
        foreach (var record in Snapshot())
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return record;
            await Task.CompletedTask.ConfigureAwait(false);
        }
    }

    private OrderRecord[] Snapshot()
    {
        lock (gate)
        {
            return
            [
                .. orders.Values
                    .OrderBy(static record => record.Order.CreatedAt)
                    .ThenBy(static record => record.Order.Id.Value, StringComparer.Ordinal),
            ];
        }
    }
}
