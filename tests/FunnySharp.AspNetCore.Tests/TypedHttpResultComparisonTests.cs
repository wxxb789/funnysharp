using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FunnySharp;
using FunnySharp.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;

namespace FunnySharp.AspNetCore.Tests;

public sealed class TypedHttpResultComparisonTests
{
    [Fact]
    public async Task OrdinaryMatchAndTypedConsumersAgreeOnResponsesAndMetadata()
    {
        await using var host = await TestApplication.StartAsync(app =>
        {
            app.MapGet("/compare/ordinary/{id}", (string id) => Ordinary(id));
            app.MapGet("/compare/match/{id}", (string id) => Match(id));
            app.MapGet("/compare/typed/{id}", (string id) =>
                Find(id).ToHttpResults(MapFailure));
        });

        foreach (var id in new[] { "ok", "conflict", "missing" })
        {
            using var ordinary = await host.Client.GetAsync(
                $"/compare/ordinary/{id}",
                TestContext.Current.CancellationToken);
            using var match = await host.Client.GetAsync(
                $"/compare/match/{id}",
                TestContext.Current.CancellationToken);
            using var typed = await host.Client.GetAsync(
                $"/compare/typed/{id}",
                TestContext.Current.CancellationToken);

            Assert.Equal(ordinary.StatusCode, match.StatusCode);
            Assert.Equal(ordinary.StatusCode, typed.StatusCode);
            Assert.Equal(
                await ordinary.Content.ReadAsStringAsync(TestContext.Current.CancellationToken),
                await match.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
            Assert.Equal(
                await ordinary.Content.ReadAsStringAsync(TestContext.Current.CancellationToken),
                await typed.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
            Assert.Equal(
                ordinary.Content.Headers.ContentType?.MediaType,
                typed.Content.Headers.ContentType?.MediaType);
        }

        Assert.Equal(
            Produces(host.Application, "/compare/ordinary/{id}"),
            Produces(host.Application, "/compare/match/{id}"));
        Assert.Equal(
            Produces(host.Application, "/compare/ordinary/{id}"),
            Produces(host.Application, "/compare/typed/{id}"));
    }

    [Fact]
    public async Task TypedConsumerServesTheRepresentativeSuccessAndFailureBodies()
    {
        await using var host = await TestApplication.StartAsync(app =>
            app.MapGet("/compare/typed/{id}", (string id) =>
                Find(id).ToHttpResults(MapFailure)));

        using var success = await host.Client.GetAsync(
            "/compare/typed/ok",
            TestContext.Current.CancellationToken);
        using var conflict = await host.Client.GetAsync(
            "/compare/typed/conflict",
            TestContext.Current.CancellationToken);
        using var missing = await host.Client.GetAsync(
            "/compare/typed/missing",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, success.StatusCode);
        Assert.Equal(
            new Order("ok", "SKU-BOOK"),
            await success.Content.ReadFromJsonAsync<Order>(TestContext.Current.CancellationToken));
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        Assert.Equal("conflict", (await ReadProblemAsync(conflict)).GetProperty("detail").GetString());
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal("missing", (await ReadProblemAsync(missing)).GetProperty("detail").GetString());
    }

    private static Results<Ok<Order>, Results<NotFound<ProblemDetails>, Conflict<ProblemDetails>>> Ordinary(
        string id)
    {
        var outcome = Find(id);
        if (outcome.TryGetValue(out var order))
        {
            return TypedResults.Ok(order);
        }

        _ = outcome.TryGetError(out var failure);
        return MapFailure(failure!);
    }

    private static Results<Ok<Order>, Results<NotFound<ProblemDetails>, Conflict<ProblemDetails>>> Match(
        string id) =>
        Find(id).Match<Results<Ok<Order>, Results<NotFound<ProblemDetails>, Conflict<ProblemDetails>>>>(
            order => TypedResults.Ok(order),
            failure => MapFailure(failure));

    private static Results<NotFound<ProblemDetails>, Conflict<ProblemDetails>> MapFailure(
        OrderFailure failure) =>
        failure.Kind == OrderFailureKind.Conflict
            ? TypedResults.Conflict(Problem(failure, StatusCodes.Status409Conflict))
            : TypedResults.NotFound(Problem(failure, StatusCodes.Status404NotFound));

    private static Result<Order, OrderFailure> Find(string id) =>
        id switch
        {
            "ok" => Result<Order, OrderFailure>.Success(new Order("ok", "SKU-BOOK")),
            "conflict" => Result<Order, OrderFailure>.Failure(
                new OrderFailure(OrderFailureKind.Conflict, "conflict")),
            _ => Result<Order, OrderFailure>.Failure(
                new OrderFailure(OrderFailureKind.NotFound, "missing")),
        };

    private static ProblemDetails Problem(OrderFailure failure, int status) =>
        new()
        {
            Status = status,
            Title = status == StatusCodes.Status409Conflict ? "Conflict" : "Not found",
            Detail = failure.Code,
        };

    private static async Task<JsonElement> ReadProblemAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).RootElement;

    private static IReadOnlyList<(int StatusCode, Type? Type)> Produces(WebApplication app, string route)
    {
        var endpoint = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(static source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(candidate =>
                string.Equals(candidate.RoutePattern.RawText, route, StringComparison.Ordinal));

        return endpoint.Metadata
            .OfType<IProducesResponseTypeMetadata>()
            .Select(static metadata => (metadata.StatusCode, metadata.Type))
            .OrderBy(static metadata => metadata.StatusCode)
            .ToArray();
    }

    private enum OrderFailureKind
    {
        NotFound,
        Conflict,
    }

    private sealed record Order(string Id, string Sku);

    private sealed record OrderFailure(OrderFailureKind Kind, string Code);

    private sealed class TestApplication(WebApplication application, HttpClient client) : IAsyncDisposable
    {
        public HttpClient Client { get; } = client;

        public WebApplication Application { get; } = application;

        public static async Task<TestApplication> StartAsync(Action<WebApplication> configure)
        {
            var builder = WebApplication.CreateSlimBuilder();
            builder.WebHost.UseTestServer();
            var application = builder.Build();
            configure(application);
            await application.StartAsync(TestContext.Current.CancellationToken);
            return new TestApplication(application, application.GetTestClient());
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await Application.StopAsync(TestContext.Current.CancellationToken);
            await Application.DisposeAsync();
        }
    }
}
