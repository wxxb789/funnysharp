using FunnySharp.VerticalSlice.Application;
using FunnySharp.VerticalSlice.Domain;

namespace FunnySharp.VerticalSlice.Infrastructure;

/// <summary>
/// A payment simulator. Two method names drive the failure paths deterministically:
/// <c>card-declined</c> returns a successful authorization call carrying a declined decision, and
/// <c>card-unavailable</c> fails the call itself.
/// </summary>
public sealed class SimulatedPaymentGateway(VerticalSliceOptions options) : IPaymentGateway
{
    public async ValueTask<Result<PaymentAuthorization, OrderError>> AuthorizeAsync(
        OrderId orderId,
        Money amount,
        string paymentMethod,
        CancellationToken cancellationToken)
    {
        await SimulatedDelay.WaitAsync(options.PaymentLatency, cancellationToken);
        return paymentMethod switch
        {
            PaymentMethods.CardDeclined => Result<PaymentAuthorization, OrderError>.Success(
                PaymentAuthorization.Declined("the issuer declined the card")),
            PaymentMethods.CardUnavailable => Result<PaymentAuthorization, OrderError>.Failure(
                new OrderError.DependencyUnavailable("payments", "authorize")),
            _ => Result<PaymentAuthorization, OrderError>.Success(
                PaymentAuthorization.Granted($"PAY-{orderId.Value}-{amount.Amount:0.00}")),
        };
    }
}
