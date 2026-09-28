using FunnySharp.VerticalSlice.Application;
using FunnySharp.VerticalSlice.Domain;
using FunnySharp.VerticalSlice.Infrastructure;

namespace FunnySharp.VerticalSlice.Tests;

/// <summary>The real stock simulator plus a recording/gating seam and the token it was handed.</summary>
internal sealed class GateInventoryService : IInventoryService
{
    private readonly SimulatedInventoryService inner;

    public GateInventoryService(VerticalSliceOptions options) => inner = new SimulatedInventoryService(options);

    public ValueTask<Option<int>> AvailableAsync(Sku sku, CancellationToken cancellationToken) =>
        inner.AvailableAsync(sku, cancellationToken);

    public ValueTask<UnitResult<OrderError>> ReserveAsync(
        Sku sku,
        Quantity quantity,
        CancellationToken cancellationToken) =>
        inner.ReserveAsync(sku, quantity, cancellationToken);

    public Func<Sku, Quantity, CancellationToken, ValueTask<UnitResult<OrderError>>>? OnRelease { get; set; }

    public CancellationToken LastReleaseToken { get; private set; }

    public TaskCompletionSource<CancellationToken> ReleaseStarted { get; } =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public ValueTask<UnitResult<OrderError>> ReleaseAsync(
        Sku sku,
        Quantity quantity,
        CancellationToken cancellationToken)
    {
        LastReleaseToken = cancellationToken;
        ReleaseStarted.TrySetResult(cancellationToken);
        return OnRelease is { } release
            ? release(sku, quantity, cancellationToken)
            : inner.ReleaseAsync(sku, quantity, cancellationToken);
    }
}
