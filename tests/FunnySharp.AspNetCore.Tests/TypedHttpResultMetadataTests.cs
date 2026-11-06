using System.Net;
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

public sealed class TypedHttpResultMetadataTests
{
    [Fact]
    public async Task TypedMappingPublishesSelectedSuccessAndFailureMetadata()
    {
        await using var host = await TestApplication.StartAsync(app =>
        {
            app.MapGet("/metadata/result", () =>
                Result<Payload, DomainError>.Success(new Payload("result", 2))
                    .ToHttpResults(NotFoundFailure));
            app.MapGet("/metadata/unit", () =>
                UnitResult<DomainError>.Success().ToHttpResults(ConflictFailure));
            app.MapGet("/metadata/validation", () =>
                Validation<Payload, InputError>.Valid(new Payload("validation", 3))
                    .ToHttpResults(InvalidValidation));
        });

        AssertProduces(host.Application, "/metadata/result", [(200, typeof(Payload)), (404, typeof(ProblemDetails))]);
        AssertProduces(host.Application, "/metadata/unit", [(204, typeof(void)), (409, typeof(ProblemDetails))]);
        AssertProduces(
            host.Application,
            "/metadata/validation",
            [(200, typeof(Payload)), (400, typeof(HttpValidationProblemDetails))]);

        Assert.Equal(
            "application/problem+json",
            Produces(host.Application, "/metadata/validation")
                .Single(static metadata => metadata.StatusCode == 400)
                .ContentTypes
                .Single());
    }

    [Fact]
    public async Task NestedFailureUnionPublishesEverySelectedFailureStatus()
    {
        await using var host = await TestApplication.StartAsync(app =>
            app.MapGet("/metadata/union/{id}", (string id) =>
                Find(id).ToHttpResults(OrderFailureResults)));

        var metadata = Produces(host.Application, "/metadata/union/{id}");
        Assert.Equal(
            [(200, typeof(Order)), (404, typeof(ProblemDetails)), (409, typeof(ProblemDetails))],
            metadata.Select(static item => (item.StatusCode, item.Type)).OrderBy(static item => item.StatusCode));

        using var conflict = await host.Client.GetAsync(
            "/metadata/union/conflict",
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        using var document = JsonDocument.Parse(
            await conflict.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.Equal("conflict", document.RootElement.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task DynamicProblemResultDoesNotInventResponseDeclarations()
    {
        await using var host = await TestApplication.StartAsync(app =>
        {
            app.MapGet("/metadata/dynamic", () =>
                Result<Payload, DomainError>.Failure(new DomainError("dynamic", 409))
                    .ToHttpResults(DynamicProblem));
            app.MapGet("/metadata/declared", () =>
                    Result<Payload, DomainError>.Failure(new DomainError("declared", 409))
                        .ToHttpResults(DynamicProblem))
                .ProducesProblem(StatusCodes.Status409Conflict);
        });

        var dynamicMetadata = Produces(host.Application, "/metadata/dynamic");
        Assert.Equal([(200, typeof(Payload))], dynamicMetadata.Select(static item => (item.StatusCode, item.Type)));
        Assert.DoesNotContain(dynamicMetadata, static item => item.StatusCode == 409);

        var declaredMetadata = Produces(host.Application, "/metadata/declared");
        Assert.Contains(declaredMetadata, static item => item.StatusCode == 409);
        Assert.Equal(
            (200, typeof(Payload)),
            declaredMetadata.OrderBy(static item => item.StatusCode).Select(static item => (item.StatusCode, item.Type)).First());

        using var response = await host.Client.GetAsync(
            "/metadata/dynamic",
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.Equal("dynamic", document.RootElement.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task StatusSpecificTypedResultsPublishTheirActualContentType()
    {
        await using var host = await TestApplication.StartAsync(app =>
        {
            app.MapGet("/metadata/not-found", () =>
                Result<Payload, DomainError>.Failure(new DomainError("missing", StatusCodes.Status404NotFound))
                    .ToHttpResults(NotFoundFailure));
            app.MapGet("/metadata/unprocessable", () =>
                Validation<Payload, InputError>.InvalidMany([new InputError("sku", "required")])
                    .ToHttpResults(UnprocessableValidation));
        });

        var notFound = Produces(host.Application, "/metadata/not-found")
            .Single(static item => item.StatusCode == 404);
        Assert.Equal(["application/json"], notFound.ContentTypes);

        var unprocessable = Produces(host.Application, "/metadata/unprocessable")
            .Single(static item => item.StatusCode == 422);
        Assert.Equal(typeof(HttpValidationProblemDetails), unprocessable.Type);
        Assert.Equal(["application/json"], unprocessable.ContentTypes);
    }

    private static IReadOnlyList<IProducesResponseTypeMetadata> Produces(WebApplication app, string route)
    {
        var endpoint = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(static source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(candidate =>
                string.Equals(candidate.RoutePattern.RawText, route, StringComparison.Ordinal));

        return endpoint.Metadata
            .OfType<IProducesResponseTypeMetadata>()
            .OrderBy(static metadata => metadata.StatusCode)
            .ToArray();
    }

    private static void AssertProduces(
        WebApplication app,
        string route,
        IReadOnlyList<(int Status, Type? Type)> expected)
    {
        var actual = Produces(app, route)
            .Select(static metadata => (metadata.StatusCode, metadata.Type))
            .ToArray();

        Assert.Equal(expected, actual);
    }

    private static Result<Order, OrderFailure> Find(string id) =>
        id switch
        {
            "ok" => Result<Order, OrderFailure>.Success(new Order("ok")),
            "conflict" => Result<Order, OrderFailure>.Failure(
                new OrderFailure(OrderFailureKind.Conflict, "conflict")),
            _ => Result<Order, OrderFailure>.Failure(
                new OrderFailure(OrderFailureKind.NotFound, "missing")),
        };

    private static Results<NotFound<ProblemDetails>, Conflict<ProblemDetails>> OrderFailureResults(
        OrderFailure failure) =>
        failure.Kind == OrderFailureKind.Conflict
            ? TypedResults.Conflict(Problem(failure.Code, StatusCodes.Status409Conflict))
            : TypedResults.NotFound(Problem(failure.Code, StatusCodes.Status404NotFound));

    private static ProblemHttpResult DynamicProblem(DomainError error) =>
        TypedResults.Problem(new ProblemDetails
        {
            Status = error.Status,
            Title = "Dynamic problem",
            Detail = error.Code,
        });

    private static NotFound<ProblemDetails> NotFoundAbsence() =>
        TypedResults.NotFound(Problem("absent", StatusCodes.Status404NotFound));

    private static NotFound<ProblemDetails> NotFoundFailure(DomainError error) =>
        TypedResults.NotFound(Problem(error.Code, StatusCodes.Status404NotFound));

    private static Conflict<ProblemDetails> ConflictFailure(DomainError error) =>
        TypedResults.Conflict(Problem(error.Code, StatusCodes.Status409Conflict));

    private static ValidationProblem InvalidValidation(IReadOnlyList<InputError> errors) =>
        TypedResults.ValidationProblem(
            errors
                .GroupBy(static error => error.Field, StringComparer.Ordinal)
                .ToDictionary(
                    static group => group.Key,
                    static group => group.Select(static error => error.Message).ToArray(),
                    StringComparer.Ordinal));

    private static UnprocessableEntity<HttpValidationProblemDetails> UnprocessableValidation(
        IReadOnlyList<InputError> errors) =>
        TypedResults.UnprocessableEntity(new HttpValidationProblemDetails(
            errors
                .GroupBy(static error => error.Field, StringComparer.Ordinal)
                .ToDictionary(
                    static group => group.Key,
                    static group => group.Select(static error => error.Message).ToArray(),
                    StringComparer.Ordinal))
        {
            Status = StatusCodes.Status422UnprocessableEntity,
            Title = "Validation failed",
        });

    private static ProblemDetails Problem(string detail, int status) =>
        new()
        {
            Status = status,
            Title = status == StatusCodes.Status404NotFound ? "Not found" : "Conflict",
            Detail = detail,
        };

    private sealed record Payload(string Name, int Count);

    private sealed record DomainError(string Code, int Status);

    private sealed record InputError(string Field, string Message);

    private sealed record Order(string Id);

    private enum OrderFailureKind
    {
        NotFound,
        Conflict,
    }

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
