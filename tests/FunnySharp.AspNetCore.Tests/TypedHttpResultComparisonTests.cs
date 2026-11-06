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

            var ordinaryBody = await ordinary.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var matchBody = await match.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var typedBody = await typed.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

            Assert.Equal(ordinary.StatusCode, match.StatusCode);
            Assert.Equal(ordinary.StatusCode, typed.StatusCode);
            Assert.Equal(ordinaryBody, matchBody);
            Assert.Equal(ordinaryBody, typedBody);
            Assert.Equal(
                ordinary.Content.Headers.ContentType?.MediaType,
                typed.Content.Headers.ContentType?.MediaType);
        }

        var ordinaryProduces = Produces(host.Application, "/compare/ordinary/{id}");
        Assert.Equal(ordinaryProduces, Produces(host.Application, "/compare/match/{id}"));
        Assert.Equal(ordinaryProduces, Produces(host.Application, "/compare/typed/{id}"));
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
