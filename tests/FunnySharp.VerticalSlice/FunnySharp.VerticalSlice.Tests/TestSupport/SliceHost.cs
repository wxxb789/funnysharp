using System.Net;
using FunnySharp.VerticalSlice.Application;
using FunnySharp.VerticalSlice.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace FunnySharp.VerticalSlice.Tests;

/// <summary>
/// Hosts the whole slice in-process on a TestServer. Tests replace ports through the composition
/// root's documented seam, so every substitute is visible at the top of the test that needs it.
/// </summary>
internal sealed class SliceHost : IAsyncDisposable
{
    private readonly WebApplication app;

    private SliceHost(WebApplication app, SliceClient client, VerticalSliceOptions options, Uri? baseAddress)
    {
        this.app = app;
        Client = client;
        Options = options;
        BaseAddress = baseAddress;
    }

    internal SliceClient Client { get; }

    internal VerticalSliceOptions Options { get; }

    internal static async Task<SliceHost> StartAsync(
        Action<IServiceCollection>? configureServices = null,
        Action<VerticalSliceOptions>? configureOptions = null)
    {
        var options = DefaultOptions(supplierCount: 4);
        configureOptions?.Invoke(options);

        var app = Build(options, configureServices, host => host.UseTestServer());
        await app.StartAsync(TestContext.Current.CancellationToken);
        return new SliceHost(app, new SliceClient(app.GetTestClient()), options, null);
    }

    /// <summary>
    /// Hosts the slice on real Kestrel for the tests that need to observe a client disconnect: an
    /// in-process TestServer cannot be relied on to abort a streaming response.
    /// </summary>
    internal static async Task<SliceHost> StartKestrelAsync(
        Action<IServiceCollection>? configureServices = null,
        Action<VerticalSliceOptions>? configureOptions = null)
    {
        var options = DefaultOptions(supplierCount: 2);
        configureOptions?.Invoke(options);

        var app = Build(options, configureServices, host => host.UseUrls("http://127.0.0.1:0"));
        await app.StartAsync(TestContext.Current.CancellationToken);
        var address = app.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!
            .Addresses
            .First();
        var client = new HttpClient { BaseAddress = new Uri(address) };
        return new SliceHost(app, new SliceClient(client), options, new Uri(address));
    }

    /// <summary>The Kestrel endpoint address, or <c>null</c> for an in-process host.</summary>
    internal Uri? BaseAddress { get; }

    private static VerticalSliceOptions DefaultOptions(int supplierCount) => new()
    {
        StoreLatency = TimeSpan.Zero,
        DependencyLatency = TimeSpan.Zero,
        PaymentLatency = TimeSpan.Zero,
        SupplierLatency = TimeSpan.Zero,
        SupplierCount = supplierCount,
        QuoteConcurrency = 2,
        Currency = "EUR",
    };

    private static WebApplication Build(
        VerticalSliceOptions options,
        Action<IServiceCollection>? configureServices,
        Action<IWebHostBuilder> configureHost) =>
        VerticalSliceApp.Build(
            [],
            services =>
            {
                services.AddSingleton(options);
                configureServices?.Invoke(services);
            },
            configureHost);

    internal async Task<OrderReceiptResponse> PlaceOrderAsync(
        string customerId = "customer-42",
        params (string Sku, int Quantity)[] lines)
    {
        var requested = lines.Length == 0 ? [("SKU-BOOK", 2)] : lines;
        var payload = new PlaceOrderRequest(
            customerId,
            [.. requested.Select(static line => new PlaceOrderLineRequest(line.Item1, line.Item2))]);
        using var response = await Client.PostJsonAsync("/orders", payload);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.ReadJsonAsync<OrderReceiptResponse>())!;
    }

    public async ValueTask DisposeAsync()
    {
        await app.StopAsync(TestContext.Current.CancellationToken);
        await app.DisposeAsync();
    }
}
