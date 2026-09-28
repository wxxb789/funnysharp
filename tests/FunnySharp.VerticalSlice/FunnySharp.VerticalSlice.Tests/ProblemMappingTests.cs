using System.Net;
using FunnySharp.VerticalSlice.Application;
using FunnySharp.VerticalSlice.Domain;
using FunnySharp.VerticalSlice.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace FunnySharp.VerticalSlice.Tests;

/// <summary>Every expected domain failure maps to one explicit status, type, and extension set.</summary>
public sealed class ProblemMappingTests
{
    [Fact]
    public async Task ShippingAnUnpaidOrderIsAConflictWithTheTransitionCode()
    {
        await using var host = await SliceHost.StartAsync();
        var placed = await host.PlaceOrderAsync();

        using var response = await host.Client.PostJsonAsync(
            $"/orders/{placed.OrderId}/shipments",
            new ShipOrderRequest("TRACK-12345"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.ReadJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("https://funnysharp.example/problems/order-transition-rejected", problem.Type);
        Assert.Equal("order-not-paid", problem.Extensions["code"]?.ToString());
        Assert.Equal("Placed", problem.Extensions["orderStatus"]?.ToString());
        Assert.Equal("Ship", problem.Extensions["event"]?.ToString());
    }

    [Fact]
    public async Task ADeclinedPaymentIsPaymentRequired()
    {
        await using var host = await SliceHost.StartAsync();
        var placed = await host.PlaceOrderAsync();

        using var response = await host.Client.PostJsonAsync(
            $"/orders/{placed.OrderId}/payments",
            new PayOrderRequest("card-declined"));

        Assert.Equal(HttpStatusCode.PaymentRequired, response.StatusCode);
        var problem = await response.ReadJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("https://funnysharp.example/problems/payment-declined", problem.Type);
        Assert.Equal("the issuer declined the card", problem.Detail);
    }

    [Fact]
    public async Task AnUnavailablePaymentDependencyIsAServiceUnavailable()
    {
        await using var host = await SliceHost.StartAsync();
        var placed = await host.PlaceOrderAsync();

        using var response = await host.Client.PostJsonAsync(
            $"/orders/{placed.OrderId}/payments",
            new PayOrderRequest("card-unavailable"));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var problem = await response.ReadJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("https://funnysharp.example/problems/dependency-unavailable", problem.Type);
        Assert.Equal("payments", problem.Extensions["dependency"]?.ToString());
    }

    [Fact]
    public async Task AnOrderLargerThanStockIsAConflictWithTheShortfall()
    {
        await using var host = await SliceHost.StartAsync();

        using var response = await host.Client.PostJsonAsync(
            "/orders",
            new PlaceOrderRequest("customer-42", [new PlaceOrderLineRequest("SKU-BOOK", 20)]));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.ReadJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("https://funnysharp.example/problems/inventory-shortfall", problem.Type);
        Assert.Equal("SKU-BOOK", problem.Extensions["sku"]?.ToString());
        Assert.Equal("20", problem.Extensions["requested"]?.ToString());
        Assert.True(int.Parse(problem.Extensions["available"]!.ToString()!, System.Globalization.CultureInfo.InvariantCulture) < 20);
    }

    [Fact]
    public async Task AQuoteNoSupplierCanAnswerIsAServiceUnavailable()
    {
        await using var host = await SliceHost.StartAsync();

        using var response = await host.Client.GetAsync("/suppliers/SKU-UNLISTED/quotes?quantity=1");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var problem = await response.ReadJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("https://funnysharp.example/problems/no-supplier-quote", problem.Type);
        Assert.NotEmpty(problem.Extensions["suppliers"]!.ToString()!);
    }

    [Fact]
    public async Task ALostUpdateIsAConflictCarryingBothRevisions()
    {
        GateOrderStore? store = null;
        await using var host = await SliceHost.StartAsync(
            services => services.AddSingleton<IOrderStore>(store!),
            options => store = new GateOrderStore(options));
        var placed = await host.PlaceOrderAsync();
        store!.OnPersist = static (order, _, _) => ValueTask.FromResult(UnitResult<OrderError>.Failure(
            new OrderError.ConcurrencyConflict(order.Id, order.Revision, order.Revision + 3)));

        using var response = await host.Client.PostJsonAsync(
            $"/orders/{placed.OrderId}/payments",
            new PayOrderRequest("card"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.ReadJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("https://funnysharp.example/problems/concurrency-conflict", problem.Type);
        Assert.Equal("2", problem.Extensions["expectedRevision"]?.ToString());
        Assert.Equal("5", problem.Extensions["actualRevision"]?.ToString());
    }

    [Fact]
    public async Task AHistoryThatNoLongerReplaysIsATypedServerFault()
    {
        GateOrderStore? store = null;
        await using var host = await SliceHost.StartAsync(
            services => services.AddSingleton<IOrderStore>(store!),
            options => store = new GateOrderStore(options));
        var draft = Order.Draft(
            OrderId.New(),
            Refined.CustomerOf("customer-42"),
            Refined.LinesOf(9.95m, ("SKU-BOOK", 2)),
            Refined.Now);
        store!.OnFind = (_, _) => ValueTask.FromResult(Option.Some(
            new OrderRecord(draft, [new OrderEvent.Ship(Refined.TrackingOf("TRACK-1"), Refined.Now)])));

        using var response = await host.Client.GetAsync($"/orders/{draft.Id.Value}/timeline");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var problem = await response.ReadJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("https://funnysharp.example/problems/history-diverged", problem.Type);
        Assert.Equal("HistoryDiverged", problem.Extensions["failure"]?.ToString());
    }
}
