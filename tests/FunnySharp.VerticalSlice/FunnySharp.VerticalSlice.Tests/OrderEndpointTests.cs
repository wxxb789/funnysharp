using System.Net;
using FunnySharp.VerticalSlice.Http;

namespace FunnySharp.VerticalSlice.Tests;

/// <summary>The realistic request flow: place, read, pay, ship, cancel, replay, reconcile.</summary>
public sealed class OrderEndpointTests
{
    [Fact]
    public async Task AnOrderCanBePlacedReadPaidAndShipped()
    {
        await using var host = await SliceHost.StartAsync();

        var placed = await host.PlaceOrderAsync("customer-42", ("SKU-BOOK", 2));

        Assert.Equal("Placed", placed.Status);
        Assert.Equal(1, placed.Revision);
        Assert.True(placed.Total > 0m);
        Assert.Equal("EUR", placed.Currency);

        var fetched = await host.Client.GetJsonAsync<OrderResponse>($"/orders/{placed.OrderId}");
        Assert.NotNull(fetched);
        Assert.Equal("customer-42", fetched.CustomerId);
        var line = Assert.Single(fetched.Lines);
        Assert.Equal("SKU-BOOK", line.Sku);
        Assert.Equal(2, line.Quantity);

        using var payment = await host.Client.PostJsonAsync(
            $"/orders/{placed.OrderId}/payments",
            new PayOrderRequest("card"));
        Assert.Equal(HttpStatusCode.OK, payment.StatusCode);
        var paid = await payment.ReadJsonAsync<PaymentReceiptResponse>();
        Assert.NotNull(paid);
        Assert.StartsWith("PAY-", paid.PaymentReference, StringComparison.Ordinal);

        using var shipment = await host.Client.PostJsonAsync(
            $"/orders/{placed.OrderId}/shipments",
            new ShipOrderRequest("TRACK-12345"));
        Assert.Equal(HttpStatusCode.OK, shipment.StatusCode);
        var shipped = await shipment.ReadJsonAsync<OrderReceiptResponse>();
        Assert.NotNull(shipped);
        Assert.Equal("Shipped", shipped.Status);
        Assert.Equal("TRACK-12345", shipped.TrackingCode);
        Assert.Equal(3, shipped.Revision);
    }

    [Fact]
    public async Task CancellationReturnsNoContentAndTheOrderReadsAsCancelled()
    {
        await using var host = await SliceHost.StartAsync();
        var placed = await host.PlaceOrderAsync();

        using var response = await host.Client.PostJsonAsync(
            $"/orders/{placed.OrderId}/cancellation",
            new CancelOrderRequest("customer changed their mind"));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(await response.ReadBytesAsync());
        var fetched = await host.Client.GetJsonAsync<OrderResponse>($"/orders/{placed.OrderId}");
        Assert.NotNull(fetched);
        Assert.Equal("Cancelled", fetched.Status);
        Assert.Equal("customer changed their mind", fetched.CancellationReason);
    }

    [Fact]
    public async Task TheTimelineReplaysHistoryAndStillMatchesTheStoredProjection()
    {
        await using var host = await SliceHost.StartAsync();
        var placed = await host.PlaceOrderAsync();
        using var payment = await host.Client.PostJsonAsync(
            $"/orders/{placed.OrderId}/payments",
            new PayOrderRequest("card"));
        Assert.Equal(HttpStatusCode.OK, payment.StatusCode);

        var timeline = await host.Client.GetJsonAsync<TimelineResponse>($"/orders/{placed.OrderId}/timeline");

        Assert.NotNull(timeline);
        Assert.True(timeline.MatchesStoredProjection);
        Assert.Equal("Paid", timeline.Status);
        Assert.Equal(2, timeline.Revision);
        Assert.Collection(
            timeline.Entries,
            entry =>
            {
                Assert.Equal("Place", entry.Event);
                Assert.Contains("notify-customer:order-placed", entry.Commands);
            },
            entry =>
            {
                Assert.Equal("Pay", entry.Event);
                Assert.Contains("notify-customer:payment-received", entry.Commands);
            });
    }

    [Fact]
    public async Task ReconciliationReportsAvailabilityAndReplayAgreement()
    {
        await using var host = await SliceHost.StartAsync();
        var placed = await host.PlaceOrderAsync("customer-42", ("SKU-BOOK", 2));

        var report = await host.Client.GetJsonAsync<ReconciliationResponse>($"/orders/{placed.OrderId}/reconcile");

        Assert.NotNull(report);
        Assert.Equal("Placed", report.Status);
        Assert.True(report.HistoryReplaysToStoredProjection);
        var line = Assert.Single(report.Lines);
        Assert.Equal("SKU-BOOK", line.Sku);
        Assert.Equal(2, line.Ordered);
        Assert.True(line.Available >= 0);
    }

    [Fact]
    public async Task AnUnknownOrderIsANotFoundProblem()
    {
        await using var host = await SliceHost.StartAsync();

        using var response = await host.Client.GetAsync("/orders/ORD-999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.ReadJsonAsync<Microsoft.AspNetCore.Mvc.ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("https://funnysharp.example/problems/order-not-found", problem.Type);
    }
}
