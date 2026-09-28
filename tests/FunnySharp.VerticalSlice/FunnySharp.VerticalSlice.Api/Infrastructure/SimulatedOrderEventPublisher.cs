using FunnySharp.VerticalSlice.Application;
using FunnySharp.VerticalSlice.Domain;

namespace FunnySharp.VerticalSlice.Infrastructure;

/// <summary>A no-op outbound publisher that keeps the slice dependency-free.</summary>
public sealed class SimulatedOrderEventPublisher(VerticalSliceOptions options) : IOrderEventPublisher
{
    public async ValueTask<UnitResult<OrderError>> NotifyCustomerAsync(
        CustomerId customerId,
        OrderId orderId,
        string kind,
        CancellationToken cancellationToken)
    {
        await SimulatedDelay.WaitAsync(options.DependencyLatency, cancellationToken);
        return UnitResult<OrderError>.Success();
    }

    public async ValueTask<UnitResult<OrderError>> PublishShipmentAsync(
        OrderId orderId,
        TrackingCode trackingCode,
        CancellationToken cancellationToken)
    {
        await SimulatedDelay.WaitAsync(options.DependencyLatency, cancellationToken);
        return UnitResult<OrderError>.Success();
    }
}
