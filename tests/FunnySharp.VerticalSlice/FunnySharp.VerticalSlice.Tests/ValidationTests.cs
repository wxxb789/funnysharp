using System.Net;
using FunnySharp.VerticalSlice.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FunnySharp.VerticalSlice.Tests;

/// <summary>External input validation: every mistake is reported, in the order it was encountered.</summary>
public sealed class ValidationTests
{
    [Fact]
    public async Task EveryInvalidPlacementFieldIsReportedInInputOrder()
    {
        await using var host = await SliceHost.StartAsync();
        var request = new PlaceOrderRequest(
            "x",
            [new PlaceOrderLineRequest("bad sku!", 0), new PlaceOrderLineRequest("SKU-BOOK", 99)]);

        using var response = await host.Client.PostJsonAsync("/orders", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.ReadJsonAsync<HttpValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("https://funnysharp.example/problems/request-invalid", problem.Type);
        Assert.Equal(
            new[] { "customerId", "lines[0].sku", "lines[0].quantity", "lines[1].quantity" },
            problem.Errors.Keys);
        Assert.Equal("A quantity cannot exceed 20.", problem.Errors["lines[1].quantity"][0]);
    }

    [Fact]
    public async Task AnOrderWithoutLinesIsRejected()
    {
        await using var host = await SliceHost.StartAsync();

        using var response = await host.Client.PostJsonAsync("/orders", new PlaceOrderRequest("customer-42", []));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.ReadJsonAsync<HttpValidationProblemDetails>();
        Assert.NotNull(problem);
        var messages = Assert.Single(problem.Errors);
        Assert.Equal("lines", messages.Key);
    }

    [Fact]
    public async Task ANullOrderLineIsAValidationErrorRatherThanAServerFault()
    {
        await using var host = await SliceHost.StartAsync();

        using var response = await host.Client.PostRawJsonAsync(
            "/orders",
            """{"customerId":"customer-42","lines":[null]}""");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.ReadJsonAsync<HttpValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains("lines[0]", problem.Errors.Keys);
    }

    [Fact]
    public async Task AnUnsupportedPaymentMethodIsRejectedBeforeTheGatewayRuns()
    {
        await using var host = await SliceHost.StartAsync();
        var placed = await host.PlaceOrderAsync();

        using var response = await host.Client.PostJsonAsync(
            $"/orders/{placed.OrderId}/payments",
            new PayOrderRequest("crypto"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.ReadJsonAsync<HttpValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains("paymentMethod", problem.Errors.Keys);
        var fetched = await host.Client.GetJsonAsync<OrderResponse>($"/orders/{placed.OrderId}");
        Assert.NotNull(fetched);
        Assert.Equal("Placed", fetched.Status);
    }

    [Fact]
    public async Task ATrackingCodeAndACancellationReasonAreValidated()
    {
        await using var host = await SliceHost.StartAsync();
        var placed = await host.PlaceOrderAsync();

        using var shipment = await host.Client.PostJsonAsync(
            $"/orders/{placed.OrderId}/shipments",
            new ShipOrderRequest("no"));
        Assert.Equal(HttpStatusCode.BadRequest, shipment.StatusCode);
        var shipmentProblem = await shipment.ReadJsonAsync<HttpValidationProblemDetails>();
        Assert.NotNull(shipmentProblem);
        Assert.Contains("trackingCode", shipmentProblem.Errors.Keys);

        using var cancellation = await host.Client.PostJsonAsync(
            $"/orders/{placed.OrderId}/cancellation",
            new CancelOrderRequest("no"));
        Assert.Equal(HttpStatusCode.BadRequest, cancellation.StatusCode);
        var cancellationProblem = await cancellation.ReadJsonAsync<HttpValidationProblemDetails>();
        Assert.NotNull(cancellationProblem);
        Assert.Contains("reason", cancellationProblem.Errors.Keys);
    }

    [Fact]
    public async Task AQuoteQuantityOutsideTheRefinedRangeIsRejected()
    {
        await using var host = await SliceHost.StartAsync();

        using var response = await host.Client.GetAsync("/suppliers/SKU-BOOK/quotes?quantity=25");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.ReadJsonAsync<HttpValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Contains("quantity", problem.Errors.Keys);
    }

    [Fact]
    public async Task AMalformedOrderIdIsNotFoundRatherThanAValidationError()
    {
        await using var host = await SliceHost.StartAsync();

        using var response = await host.Client.GetAsync("/orders/not-an-order-id");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.ReadJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("https://funnysharp.example/problems/order-not-found", problem.Type);
    }
}
