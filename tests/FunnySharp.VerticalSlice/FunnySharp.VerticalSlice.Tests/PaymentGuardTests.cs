using System.Net;
using FunnySharp.VerticalSlice.Application;
using FunnySharp.VerticalSlice.Domain;
using FunnySharp.VerticalSlice.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace FunnySharp.VerticalSlice.Tests;

/// <summary>
/// Payment is the one command whose validity the lifecycle owns: an order that is not payable is
/// rejected with a typed code and the payment gateway is never called for it.
/// </summary>
public sealed class PaymentGuardTests
{
    [Fact]
    public async Task PayingAnAlreadyPaidOrderIsRejectedWithoutCallingTheGateway()
    {
        RecordingPaymentGateway? gateway = null;
        await using var host = await SliceHost.StartAsync(
            services => services.AddSingleton<IPaymentGateway>(gateway!),
            options => gateway = new RecordingPaymentGateway(options));
        var placed = await host.PlaceOrderAsync();

        using var first = await host.Client.PostJsonAsync(
            $"/orders/{placed.OrderId}/payments",
            new PayOrderRequest(PaymentMethods.Card));
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(1, gateway!.Calls);

        using var second = await host.Client.PostJsonAsync(
            $"/orders/{placed.OrderId}/payments",
            new PayOrderRequest(PaymentMethods.Card));

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        var problem = await second.ReadJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("payment-already-recorded", problem.Extensions["code"]?.ToString());
        Assert.Equal("Paid", problem.Extensions["orderStatus"]?.ToString());
        Assert.Equal(1, gateway.Calls);
    }

    [Fact]
    public async Task PayingACancelledOrderIsRejectedWithoutCallingTheGateway()
    {
        RecordingPaymentGateway? gateway = null;
        await using var host = await SliceHost.StartAsync(
            services => services.AddSingleton<IPaymentGateway>(gateway!),
            options => gateway = new RecordingPaymentGateway(options));
        var placed = await host.PlaceOrderAsync();
        using var cancellation = await host.Client.PostJsonAsync(
            $"/orders/{placed.OrderId}/cancellation",
            new CancelOrderRequest("customer changed their mind"));
        Assert.Equal(HttpStatusCode.NoContent, cancellation.StatusCode);

        using var payment = await host.Client.PostJsonAsync(
            $"/orders/{placed.OrderId}/payments",
            new PayOrderRequest(PaymentMethods.Card));

        Assert.Equal(HttpStatusCode.Conflict, payment.StatusCode);
        var problem = await payment.ReadJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("order-cancelled", problem.Extensions["code"]?.ToString());
        Assert.Equal(0, gateway!.Calls);
    }
}
