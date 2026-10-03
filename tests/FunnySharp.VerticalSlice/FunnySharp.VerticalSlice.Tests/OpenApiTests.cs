using System.Net;
using System.Text.Json;
using FunnySharp.VerticalSlice.Application;
using FunnySharp.VerticalSlice.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace FunnySharp.VerticalSlice.Tests;

/// <summary>
/// The OpenAPI document is generated from the typed-result metadata, so it must describe the same
/// outcomes the endpoints actually produce.
/// </summary>
public sealed class OpenApiTests
{
    [Fact]
    public async Task EveryEndpointAppearsWithItsRealOutcomes()
    {
        await using var host = await SliceHost.StartAsync();

        using var response = await host.Client.GetAsync("/openapi/v1.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.ReadStringAsync();
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        var paths = root.GetProperty("paths");

        foreach (var expected in new[]
        {
            "/orders",
            "/orders/{id}",
            "/orders/{id}/payments",
            "/orders/{id}/shipments",
            "/orders/{id}/cancellation",
            "/orders/{id}/timeline",
            "/orders/{id}/reconcile",
            "/orders/export",
            "/suppliers/{sku}/quotes",
            "/suppliers/{sku}/quotes/stream",
        })
        {
            Assert.True(paths.TryGetProperty(expected, out _), $"The document has no path '{expected}'.");
        }

        var placeOrder = paths.GetProperty("/orders").GetProperty("post");
        Assert.Contains("OrderReceiptResponse", SchemaOf(Respond(placeOrder, "201")), StringComparison.Ordinal);
        Assert.Contains("ValidationProblemDetails", SchemaOf(Respond(placeOrder, "400")), StringComparison.Ordinal);
        Assert.Contains("ProblemDetails", SchemaOf(Respond(placeOrder, "409")), StringComparison.Ordinal);
        Assert.Contains("ProblemDetails", SchemaOf(Respond(placeOrder, "503")), StringComparison.Ordinal);

        var payments = paths.GetProperty("/orders/{id}/payments").GetProperty("post");
        Assert.Contains("PaymentReceiptResponse", SchemaOf(Respond(payments, "200")), StringComparison.Ordinal);
        Assert.Contains("ProblemDetails", SchemaOf(Respond(payments, "402")), StringComparison.Ordinal);

        var shipment = paths.GetProperty("/orders/{id}/shipments").GetProperty("post");
        Assert.Contains("OrderReceiptResponse", SchemaOf(Respond(shipment, "200")), StringComparison.Ordinal);

        var cancellation = paths.GetProperty("/orders/{id}/cancellation").GetProperty("post");
        Assert.True(cancellation.GetProperty("responses").TryGetProperty("204", out _));

        var reconciliation = paths.GetProperty("/orders/{id}/reconcile").GetProperty("get");
        Assert.Contains("ReconciliationResponse", SchemaOf(Respond(reconciliation, "200")), StringComparison.Ordinal);
        foreach (var status in new[] { "404", "500", "503" })
        {
            Assert.Contains("ProblemDetails", SchemaOf(Respond(reconciliation, status)), StringComparison.Ordinal);
        }

        var export = paths.GetProperty("/orders/export").GetProperty("get");
        Assert.Contains("ExportRow", SchemaOf(Respond(export, "200")), StringComparison.Ordinal);

        Assert.True(
            Respond(export, "200").GetProperty("content").TryGetProperty("application/x-ndjson", out _),
            "The export response is not documented as NDJSON.");

        var quotes = paths.GetProperty("/suppliers/{sku}/quotes").GetProperty("get");
        Assert.Contains("QuoteResponse", SchemaOf(Respond(quotes, "200")), StringComparison.Ordinal);

        var streaming = paths.GetProperty("/suppliers/{sku}/quotes/stream").GetProperty("get");
        Assert.Contains("QuoteStreamRow", SchemaOf(Respond(streaming, "200")), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReconciliationHistoryFaultMatchesTheSameKestrelHostsOpenApi()
    {
        GateOrderStore? store = null;
        await using var host = await SliceHost.StartKestrelAsync(
            services => services.AddSingleton<IOrderStore>(store!),
            options => store = new GateOrderStore(options));
        var draft = Order.Draft(
            OrderId.New(),
            Refined.CustomerOf("customer-42"),
            Refined.LinesOf(9.95m, ("SKU-BOOK", 2)),
            Refined.Now);
        store!.OnFind = (_, _) => ValueTask.FromResult(Option.Some(
            new OrderRecord(draft, [new OrderEvent.Ship(Refined.TrackingOf("TRACK-1"), Refined.Now)])));

        using var response = await host.Client.GetAsync($"/orders/{draft.Id.Value}/reconcile");
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.ReadJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("https://funnysharp.example/problems/history-diverged", problem.Type);
        Assert.Equal("HistoryDiverged", problem.Extensions["failure"]?.ToString());

        using var openApiResponse = await host.Client.GetAsync("/openapi/v1.json");
        Assert.Equal(HttpStatusCode.OK, openApiResponse.StatusCode);
        using var document = JsonDocument.Parse(await openApiResponse.ReadStringAsync());
        var operation = document.RootElement.GetProperty("paths")
            .GetProperty("/orders/{id}/reconcile").GetProperty("get");
        var declaredFailure = Respond(operation, "500");
        Assert.True(declaredFailure.GetProperty("content").TryGetProperty("application/problem+json", out _));
        Assert.Contains("ProblemDetails", SchemaOf(declaredFailure), StringComparison.Ordinal);
    }

    [Fact]
    public async Task TheContractCarriesNoHandlerOrDomainTypes()
    {
        await using var host = await SliceHost.StartAsync();

        var json = await host.Client.GetStringAsync("/openapi/v1.json");

        Assert.DoesNotContain("IResult", json, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpContext", json, StringComparison.Ordinal);
        Assert.DoesNotContain("VerticalSlice.Domain", json, StringComparison.Ordinal);
        Assert.DoesNotContain("FunnySharp.Option", json, StringComparison.Ordinal);
        Assert.DoesNotContain("FunnySharp.Result", json, StringComparison.Ordinal);
    }

    private static JsonElement Respond(JsonElement operation, string statusCode) =>
        operation.GetProperty("responses").GetProperty(statusCode);

    private static string SchemaOf(JsonElement response)
    {
        if (!response.TryGetProperty("content", out var content))
        {
            return string.Empty;
        }

        foreach (var media in content.EnumerateObject())
        {
            if (!media.Value.TryGetProperty("schema", out var schema))
            {
                continue;
            }

            return schema.TryGetProperty("$ref", out var reference)
                ? reference.GetString() ?? string.Empty
                : schema.ToString();
        }

        return string.Empty;
    }
}
