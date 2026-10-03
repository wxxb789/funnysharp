using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

public sealed class AspnetcoreTests
{
    [Fact]
    public async Task PostOrderAccumulatesValidationErrorsInOrder()
    {
        await using var server = await ShopServer.StartAsync();
        var invalid = await server.PostOrder(new
        {
            customerEmail = "not-an-email",
            lines = new[]
            {
                new { sku = "keyboard", quantity = 0 },
                new { sku = "mouse", quantity = 100 },
            },
            promoCode = "SECRET",
        });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal("application/problem+json", invalid.Content.Headers.ContentType?.MediaType);
        Assert.Equal(
            ["invalid-email", "invalid-quantity:keyboard", "invalid-quantity:mouse", "unknown-promo"],
            await ErrorCodesAsync(invalid));

        var empty = await server.PostOrder(new
        {
            customerEmail = "ada@example.com",
            lines = Array.Empty<object>(),
            promoCode = (string?)null,
        });
        Assert.Equal(HttpStatusCode.BadRequest, empty.StatusCode);
        Assert.Equal(["empty-order"], await ErrorCodesAsync(empty));
        Assert.Empty(ShopData.Orders);
        Assert.Empty(ShopData.Charges);
    }

    [Fact]
    public async Task PostOrderReportsUnknownSku()
    {
        await using var server = await ShopServer.StartAsync();
        var response = await server.PostOrder(new
        {
            customerEmail = "ada@example.com",
            lines = new[] { new { sku = "trackball", quantity = 1 } },
            promoCode = (string?)null,
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(["unknown-sku:trackball"], await ErrorCodesAsync(response));
        Assert.Empty(ShopData.Orders);
    }

    [Fact]
    public async Task PostOrderStoresPricedOrderAndReturnsItsId()
    {
        await using var server = await ShopServer.StartAsync();
        var response = await server.PostOrder(new
        {
            customerEmail = "ada@example.com",
            lines = new[]
            {
                new { sku = "keyboard", quantity = 1 },
                new { sku = "mouse", quantity = 2 },
            },
            promoCode = (string?)null,
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var orderId = await OrderIdAsync(response);
        Assert.Equal(1001, orderId);
        var order = Assert.Single(ShopData.Orders);
        Assert.Equal(1001, order.Id);
        Assert.Equal("ada@example.com", order.CustomerEmail);
        // 40 + 2 x 15 + 5.00 shipping.
        Assert.Equal(75.00m, order.Total);
        Assert.False(order.Paid);
    }

    [Fact]
    public async Task PostOrderAppliesPromoCodes()
    {
        await using var server = await ShopServer.StartAsync();
        var save10 = await server.PostOrder(new
        {
            customerEmail = "ada@example.com",
            lines = new[] { new { sku = "monitor", quantity = 1 } },
            promoCode = "SAVE10",
        });
        Assert.Equal(HttpStatusCode.Created, save10.StatusCode);
        var freeship = await server.PostOrder(new
        {
            customerEmail = "grace@example.com",
            lines = new[] { new { sku = "mouse", quantity = 1 } },
            promoCode = "FREESHIP",
        });
        Assert.Equal(HttpStatusCode.Created, freeship.StatusCode);
        Assert.Equal(1001, ShopData.Orders[0].Id);
        // 220 + 5 shipping, then 10% off the goods, shipping stays.
        Assert.Equal(203.00m, ShopData.Orders[0].Total);
        Assert.Equal(1002, ShopData.Orders[1].Id);
        // FREESHIP removes the shipping.
        Assert.Equal(15.00m, ShopData.Orders[1].Total);
    }

    [Fact]
    public async Task GetProductReturnsKnownProduct()
    {
        await using var server = await ShopServer.StartAsync();
        var response = await server.GetProduct("keyboard");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = await ParseAsync(response);
        Assert.Equal("keyboard", document.RootElement.GetProperty("id").GetString());
        Assert.Equal("Mechanical keyboard", document.RootElement.GetProperty("name").GetString());
        Assert.Equal(40.00m, document.RootElement.GetProperty("price").GetDecimal());
    }

    [Fact]
    public async Task GetProductReportsUnknownProduct()
    {
        await using var server = await ShopServer.StartAsync();
        var response = await server.GetProduct("speakers");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("product-not-found:speakers", await DetailAsync(response));
    }

    [Fact]
    public async Task DeleteOrderReportsUnknownOrder()
    {
        await using var server = await ShopServer.StartAsync();
        var response = await server.DeleteOrder(4242);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("order-not-found:4242", await DetailAsync(response));
    }

    [Fact]
    public async Task DeleteOrderKeepsPaidOrderAndRemovesUnpaidOrder()
    {
        await using var server = await ShopServer.StartAsync();
        var paidOrderId = await server.CreateOrderAsync();
        var pay = await server.PayOrder(paidOrderId, "card-1");
        Assert.Equal(HttpStatusCode.OK, pay.StatusCode);
        var paidDelete = await server.DeleteOrder(paidOrderId);
        Assert.Equal(HttpStatusCode.Conflict, paidDelete.StatusCode);
        Assert.Equal("order-paid", await DetailAsync(paidDelete));
        Assert.Single(ShopData.Orders);

        var unpaidOrderId = await server.CreateOrderAsync();
        var delete = await server.DeleteOrder(unpaidOrderId);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.DoesNotContain(ShopData.Orders, order => order.Id == unpaidOrderId);
        Assert.Single(ShopData.Orders);
    }

    [Fact]
    public async Task PayOrderReportsUnknownOrderDeclinesThenSucceeds()
    {
        await using var server = await ShopServer.StartAsync();
        var unknown = await server.PayOrder(4242, "card-1");
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        Assert.Equal("order-not-found:4242", await DetailAsync(unknown));

        var orderId = await server.CreateOrderAsync();
        var declined = await server.PayOrder(orderId, "declined");
        Assert.Equal(HttpStatusCode.PaymentRequired, declined.StatusCode);
        Assert.Equal("application/problem+json", declined.Content.Headers.ContentType?.MediaType);
        Assert.Equal("payment-declined", await DetailAsync(declined));
        Assert.False(ShopData.Orders.Single().Paid);
        Assert.Empty(ShopData.Charges);

        var paid = await server.PayOrder(orderId, "card-1");
        Assert.Equal(HttpStatusCode.OK, paid.StatusCode);
        using var document = await ParseAsync(paid);
        Assert.True(document.RootElement.GetProperty("paid").GetBoolean());
        Assert.True(ShopData.Orders.Single().Paid);
        var charge = Assert.Single(ShopData.Charges);
        Assert.Equal(("card-1", 20.00m), charge);
    }

    private static async Task<JsonDocument> ParseAsync(HttpResponseMessage response) =>
        await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());

    private static async Task<int> OrderIdAsync(HttpResponseMessage response)
    {
        using var document = await ParseAsync(response);
        return document.RootElement.GetProperty("orderId").GetInt32();
    }

    private static async Task<IReadOnlyList<string>> ErrorCodesAsync(HttpResponseMessage response)
    {
        using var document = await ParseAsync(response);
        return document.RootElement.GetProperty("errors").GetProperty("order").EnumerateArray()
            .Select(code => code.GetString() ?? "")
            .ToList();
    }

    private static async Task<string> DetailAsync(HttpResponseMessage response)
    {
        using var document = await ParseAsync(response);
        return document.RootElement.GetProperty("detail").GetString() ?? "";
    }

    private sealed class ShopServer : IAsyncDisposable
    {
        private ShopServer(WebApplication app, HttpClient client)
        {
            App = app;
            Client = client;
        }

        private WebApplication App { get; }

        private HttpClient Client { get; }

        public static async Task<ShopServer> StartAsync()
        {
            ShopData.Reset();
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseTestServer();
            var app = builder.Build();
            Api.MapApi(app);
            await app.StartAsync();
            var client = ((TestServer)app.Services.GetRequiredService<IServer>()).CreateClient();
            return new ShopServer(app, client);
        }

        public Task<HttpResponseMessage> PostOrder(object order) =>
            Client.PostAsJsonAsync("/orders", order);

        public Task<HttpResponseMessage> GetProduct(string id) =>
            Client.GetAsync($"/products/{id}");

        public Task<HttpResponseMessage> DeleteOrder(int id) =>
            Client.DeleteAsync($"/orders/{id}");

        public Task<HttpResponseMessage> PayOrder(int orderId, string cardToken) =>
            Client.PostAsJsonAsync($"/orders/{orderId}/pay", new { cardToken });

        // A mouse order: 15.00 + 5.00 shipping.
        public async Task<int> CreateOrderAsync()
        {
            var response = await PostOrder(new
            {
                customerEmail = "ada@example.com",
                lines = new[] { new { sku = "mouse", quantity = 1 } },
                promoCode = (string?)null,
            });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            return await OrderIdAsync(response);
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await App.StopAsync();
            await App.DisposeAsync();
        }
    }
}
