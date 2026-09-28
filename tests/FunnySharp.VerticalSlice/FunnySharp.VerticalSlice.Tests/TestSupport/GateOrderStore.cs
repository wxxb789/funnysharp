using FunnySharp.VerticalSlice.Application;
using FunnySharp.VerticalSlice.Domain;
using FunnySharp.VerticalSlice.Infrastructure;

namespace FunnySharp.VerticalSlice.Tests;

/// <summary>
/// The real in-memory store with per-member seams: a test replaces exactly the call it wants to
/// control and everything else keeps real behavior. It also records the token a dependency saw, so a
/// cancellation test can compare tokens instead of guessing.
/// </summary>
internal sealed class GateOrderStore : IOrderStore
{
    private readonly InMemoryOrderStore inner;

    public GateOrderStore(VerticalSliceOptions options) => inner = new InMemoryOrderStore(options);

    public Func<OrderId, CancellationToken, ValueTask<Option<OrderRecord>>>? OnFind { get; set; }

    public Func<Order, OrderEvent, CancellationToken, ValueTask<UnitResult<OrderError>>>? OnPersist { get; set; }

    public Func<CancellationToken, IAsyncEnumerable<OrderRecord>>? OnStream { get; set; }

    public CancellationToken LastServedToken { get; private set; }

    /// <summary>Streams the real store directly, ignoring any override.</summary>
    public IAsyncEnumerable<OrderRecord> PeekStreamAsync(CancellationToken cancellationToken) =>
        inner.StreamAsync(cancellationToken);

    public ValueTask CreateAsync(Order draft, CancellationToken cancellationToken) =>
        inner.CreateAsync(draft, cancellationToken);

    public ValueTask RemoveAsync(OrderId id, CancellationToken cancellationToken) =>
        inner.RemoveAsync(id, cancellationToken);

    public ValueTask<Option<OrderRecord>> FindAsync(OrderId id, CancellationToken cancellationToken)
    {
        LastServedToken = cancellationToken;
        return OnFind is { } find ? find(id, cancellationToken) : inner.FindAsync(id, cancellationToken);
    }

    public ValueTask<UnitResult<OrderError>> PersistAsync(
        Order order,
        OrderEvent change,
        CancellationToken cancellationToken) =>
        OnPersist is { } persist
            ? persist(order, change, cancellationToken)
            : inner.PersistAsync(order, change, cancellationToken);

    public IAsyncEnumerable<OrderRecord> StreamAsync(CancellationToken cancellationToken)
    {
        LastServedToken = cancellationToken;
        return OnStream is { } stream ? stream(cancellationToken) : inner.StreamAsync(cancellationToken);
    }
}
