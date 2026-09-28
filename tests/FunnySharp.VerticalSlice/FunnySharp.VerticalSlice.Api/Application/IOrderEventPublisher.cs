using FunnySharp.VerticalSlice.Domain;

namespace FunnySharp.VerticalSlice.Application;

/// <summary>The outbound messaging port used by the command interpreter.</summary>
public interface IOrderEventPublisher
{
    ValueTask<UnitResult<OrderError>> NotifyCustomerAsync(
        CustomerId customerId,
        OrderId orderId,
        string kind,
        CancellationToken cancellationToken);

    ValueTask<UnitResult<OrderError>> PublishShipmentAsync(
        OrderId orderId,
        TrackingCode trackingCode,
        CancellationToken cancellationToken);
}
