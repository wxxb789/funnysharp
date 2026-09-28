using System.Net;
using FunnySharp.VerticalSlice.Application;
using FunnySharp.VerticalSlice.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace FunnySharp.VerticalSlice.Tests;

/// <summary>
/// A rejected placement must leave nothing behind: no draft in the store, and no stock the lines that
/// did succeed had already reserved. Both are observable: the store through its own stream, stock
/// through the reconciliation report.
/// </summary>
public sealed class PlacementAtomicityTests
{
    [Fact]
    public async Task ARejectedPlacementLeavesNoDraftAndReleasesTheLinesItReserved()
    {
        GateOrderStore? store = null;
        await using var host = await SliceHost.StartAsync(
            services => services.AddSingleton<IOrderStore>(store!),
            options => store = new GateOrderStore(options));

        var kept = await host.PlaceOrderAsync("customer-42", ("SKU-KEEP", 1));
        var availabilityBefore = await AvailabilityAsync(host, kept.OrderId);

        using var rejected = await host.Client.PostJsonAsync(
            "/orders",
            new PlaceOrderRequest(
                "customer-42",
                [new PlaceOrderLineRequest("SKU-KEEP", 1), new PlaceOrderLineRequest("SKU-SCARCE", 20)]));

        Assert.Equal(HttpStatusCode.Conflict, rejected.StatusCode);
        var problem = await rejected.ReadJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("https://funnysharp.example/problems/inventory-shortfall", problem.Type);
        Assert.Equal("SKU-SCARCE", problem.Extensions["sku"]?.ToString());

        var stored = new List<string>();
        await foreach (var record in store!.PeekStreamAsync(TestContext.Current.CancellationToken))
        {
            stored.Add(record.Order.Id.Value);
        }

        Assert.Equal([kept.OrderId], stored);
        Assert.Equal(availabilityBefore, await AvailabilityAsync(host, kept.OrderId));
    }

    private static async Task<int> AvailabilityAsync(SliceHost host, string orderId)
    {
        var report = await host.Client.GetJsonAsync<ReconciliationResponse>($"/orders/{orderId}/reconcile");
        Assert.NotNull(report);
        var line = Assert.Single(report.Lines);
        return line.Available;
    }
}
