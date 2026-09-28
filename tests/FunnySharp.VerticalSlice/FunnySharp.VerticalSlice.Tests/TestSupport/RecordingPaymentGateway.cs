using FunnySharp.VerticalSlice.Application;
using FunnySharp.VerticalSlice.Domain;
using FunnySharp.VerticalSlice.Infrastructure;

namespace FunnySharp.VerticalSlice.Tests;

/// <summary>
/// The real payment simulator plus a call counter, so a test can prove the gateway is never consulted
/// for an order whose lifecycle does not accept a payment.
/// </summary>
internal sealed class RecordingPaymentGateway : IPaymentGateway
{
    private readonly SimulatedPaymentGateway inner;

    internal RecordingPaymentGateway(VerticalSliceOptions options) => inner = new SimulatedPaymentGateway(options);

    internal int Calls { get; private set; }

    public ValueTask<Result<PaymentAuthorization, OrderError>> AuthorizeAsync(
        OrderId orderId,
        Money amount,
        string paymentMethod,
        CancellationToken cancellationToken)
    {
        Calls++;
        return inner.AuthorizeAsync(orderId, amount, paymentMethod, cancellationToken);
    }
}
