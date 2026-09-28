using System.Net;
using FunnySharp.VerticalSlice.Application;
using FunnySharp.VerticalSlice.Domain;
using FunnySharp.VerticalSlice.Http;
using Microsoft.Extensions.DependencyInjection;

namespace FunnySharp.VerticalSlice.Tests;

/// <summary>
/// The concurrency cap is a real admission bound, and streamed quotes arrive in completion order
/// rather than source order.
/// </summary>
public sealed class BoundedParallelismTests
{
    [Fact]
    public async Task TheQuoteEndpointNeverRunsMoreQuotesThanTheCapAndAdmitsMoreAsResultsArrive()
    {
        GateSupplierGateway? gateway = null;
        await using var host = await SliceHost.StartAsync(
            services => services.AddSingleton<ISupplierGateway>(gateway!),
            options =>
            {
                options.SupplierCount = 6;
                options.QuoteConcurrency = 2;
                gateway = new GateSupplierGateway(
                    "supplier-01",
                    "supplier-02",
                    "supplier-03",
                    "supplier-04",
                    "supplier-05",
                    "supplier-06");
            });
        foreach (var supplier in gateway!.Suppliers)
        {
            gateway.Hold(supplier);
        }

        gateway.Prices["supplier-03"] = 8.00m;

        var request = host.Client.GetAsync("/suppliers/SKU-BOOK/quotes?quantity=2");

        await gateway.StartedAtLeast(2).WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        Assert.Equal(new[] { "supplier-01", "supplier-02" }, gateway.Started);

        gateway.Release("supplier-01");
        await gateway.StartedAtLeast(3).WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        Assert.Equal(new[] { "supplier-01", "supplier-02", "supplier-03" }, gateway.Started);

        foreach (var supplier in gateway.Suppliers)
        {
            gateway.Release(supplier);
        }

        using var response = await request;
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var quote = await response.ReadJsonAsync<QuoteResponse>();
        Assert.NotNull(quote);
        Assert.Equal("supplier-03", quote.Supplier);
        Assert.Equal(8.00m, quote.UnitPrice);
        Assert.Empty(quote.UnavailableSuppliers);
        Assert.True(gateway.PeakInFlight <= 2, $"Peak in-flight quotes were {gateway.PeakInFlight}, above the cap of 2.");
    }

    [Fact]
    public async Task TheStreamingQuoteEndpointDeliversEachAnswerAsItArrives()
    {
        GateSupplierGateway? gateway = null;
        await using var host = await SliceHost.StartAsync(
            services => services.AddSingleton<ISupplierGateway>(gateway!),
            options =>
            {
                options.SupplierCount = 3;
                options.QuoteConcurrency = 3;
                gateway = new GateSupplierGateway("supplier-01", "supplier-02", "supplier-03");
            });
        gateway!.Prices["supplier-01"] = 5.00m;
        gateway.Prices["supplier-02"] = 9.00m;
        gateway.Prices["supplier-03"] = 12.00m;
        gateway.Hold("supplier-01");
        gateway.Hold("supplier-02");

        using var response = await host.Client.GetStreamAsync("/suppliers/SKU-BOOK/quotes/stream?quantity=2");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var stream = await response.ReadStreamAsync();
        using var reader = new StreamReader(stream);

        Assert.Contains("supplier-03", await reader.ReadLineBoundedAsync(), StringComparison.Ordinal);

        gateway.Release("supplier-02");
        Assert.Contains("supplier-02", await reader.ReadLineBoundedAsync(), StringComparison.Ordinal);

        gateway.Release("supplier-01");
        Assert.Contains("supplier-01", await reader.ReadLineBoundedAsync(), StringComparison.Ordinal);

        Assert.Null(await reader.ReadLineBoundedAsync());
    }

    [Fact]
    public async Task TheBestQuoteIsDeterministicWhenSuppliersTie()
    {
        GateSupplierGateway? gateway = null;
        await using var host = await SliceHost.StartAsync(
            services => services.AddSingleton<ISupplierGateway>(gateway!),
            options =>
            {
                options.SupplierCount = 3;
                options.QuoteConcurrency = 3;
                gateway = new GateSupplierGateway("supplier-01", "supplier-02", "supplier-03");
            });
        gateway!.Prices["supplier-01"] = 7.00m;
        gateway.Prices["supplier-02"] = 7.00m;
        gateway.Prices["supplier-03"] = 7.50m;

        var first = await host.Client.GetJsonAsync<QuoteResponse>("/suppliers/SKU-BOOK/quotes?quantity=1");
        var second = await host.Client.GetJsonAsync<QuoteResponse>("/suppliers/SKU-BOOK/quotes?quantity=1");

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal("supplier-01", first.Supplier);
        Assert.Equal(first.Supplier, second.Supplier);
    }

    [Fact]
    public async Task ASupplierThatFailsIsReportedWithoutFailingTheWholeQuote()
    {
        GateSupplierGateway? gateway = null;
        await using var host = await SliceHost.StartAsync(
            services => services.AddSingleton<ISupplierGateway>(gateway!),
            options =>
            {
                options.SupplierCount = 3;
                options.QuoteConcurrency = 3;
                gateway = new GateSupplierGateway("supplier-01", "supplier-02", "supplier-03");
            });
        gateway!.Prices["supplier-01"] = 4.00m;
        gateway.Failures["supplier-02"] = new OrderError.SupplierUnavailable(
            "supplier-02",
            Refined.SkuOf("SKU-BOOK"),
            "no capacity");

        var quote = await host.Client.GetJsonAsync<QuoteResponse>("/suppliers/SKU-BOOK/quotes?quantity=1");

        Assert.NotNull(quote);
        Assert.Equal("supplier-01", quote.Supplier);
        Assert.Equal(4.00m, quote.UnitPrice);
        Assert.Equal(new[] { "supplier-02" }, quote.UnavailableSuppliers);
    }
}
