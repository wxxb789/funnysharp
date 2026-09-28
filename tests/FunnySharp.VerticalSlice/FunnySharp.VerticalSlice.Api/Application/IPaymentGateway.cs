using FunnySharp.VerticalSlice.Domain;

namespace FunnySharp.VerticalSlice.Application;

/// <summary>
/// The payment port. A declined authorization is a successful call carrying a declined decision;
/// only a transport failure is a typed failure.
/// </summary>
public interface IPaymentGateway
{
    ValueTask<Result<PaymentAuthorization, OrderError>> AuthorizeAsync(
        OrderId orderId,
        Money amount,
        string paymentMethod,
        CancellationToken cancellationToken);
}
