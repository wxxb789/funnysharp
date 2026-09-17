using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FunnySharp;
using FunnySharp.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;

namespace FunnySharp.AspNetCore.Tests;

public sealed class UnitResultHttpResultExtensionsTests
{
    [Fact]
    public async Task UnitResultSuccessUsesTheDefaultNoContentResult()
    {
        await using var host = await TestApplication.StartAsync(app =>
            app.MapGet("/unit/success", () => UnitResult<DomainError>.Success().ToHttpResult(Forbidden)));

        using var response = await host.Client.GetAsync("/unit/success", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(body);
        Assert.Null(response.Content.Headers.ContentType);
    }

    [Fact]
    public async Task UnitResultSuccessMapperOverridesTheDefaultNoContentResult()
    {
        var successMapCalls = 0;
        await using var host = await TestApplication.StartAsync(app =>
        {
            app.MapGet("/unit/success/ok", () => UnitResult<DomainError>.Success()
                .ToHttpResult(
                    Forbidden,
                    () =>
                    {
                        successMapCalls++;
                        return Results.Ok(new Payload("unit-success", 15));
                    }));
            app.MapGet("/unit/success/accepted", () => UnitResult<DomainError>.Success()
                .ToHttpResult(Forbidden, () => Results.Accepted()));
        });

        using var okResponse = await host.Client.GetAsync("/unit/success/ok", TestContext.Current.CancellationToken);
        using var acceptedResponse = await host.Client.GetAsync(
            "/unit/success/accepted",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, okResponse.StatusCode);
        Assert.Equal("application/json", okResponse.Content.Headers.ContentType?.MediaType);
        Assert.Equal(
            new Payload("unit-success", 15),
            await okResponse.Content.ReadFromJsonAsync<Payload>(TestContext.Current.CancellationToken));
        Assert.Equal(HttpStatusCode.Accepted, acceptedResponse.StatusCode);
        Assert.Equal(1, successMapCalls);
    }

    [Fact]
    public async Task UnitResultFailureUsesTheMappedDomainProblem()
    {
        var failureMapCalls = 0;
        await using var host = await TestApplication.StartAsync(app =>
            app.MapDelete("/unit", () =>
                UnitResult<DomainError>.Failure(new DomainError("order-not-cancelable"))
                    .ToHttpResult(error =>
                    {
                        failureMapCalls++;
                        return new ProblemDetails
                        {
                            Status = StatusCodes.Status409Conflict,
                            Type = "https://example.invalid/problems/order-not-cancelable",
                            Title = "Order cannot be canceled",
                            Detail = error.Code,
                            Instance = "/unit",
                            Extensions =
                            {
                                ["errorCode"] = error.Code,
                            },
                        };
                    })));

        using var response = await host.Client.DeleteAsync("/unit", TestContext.Current.CancellationToken);
        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        var problem = document.RootElement;

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(StatusCodes.Status409Conflict, problem.GetProperty("status").GetInt32());
        Assert.Equal(
            "https://example.invalid/problems/order-not-cancelable",
            problem.GetProperty("type").GetString());
        Assert.Equal("Order cannot be canceled", problem.GetProperty("title").GetString());
        Assert.Equal("order-not-cancelable", problem.GetProperty("detail").GetString());
        Assert.Equal("/unit", problem.GetProperty("instance").GetString());
        Assert.Equal("order-not-cancelable", problem.GetProperty("errorCode").GetString());
        Assert.Equal(1, failureMapCalls);
    }

    [Fact]
    public async Task TaskAndValueTaskUnitResultsUseMatchingAsyncMappers()
    {
        var taskMapCalls = 0;
        var valueTaskMapCalls = 0;
        await using var host = await TestApplication.StartAsync(app =>
        {
            app.MapGet("/unit/task/success", () =>
                Task.FromResult(UnitResult<DomainError>.Success()).ToHttpResultAsync(Forbidden));
            app.MapGet("/unit/task/failure", () =>
                Task.FromResult(UnitResult<DomainError>.Failure(new DomainError("task-unit")))
                    .ToHttpResultAsync(Forbidden));
            app.MapGet("/unit/value-task/success", () =>
                ValueTask.FromResult(UnitResult<DomainError>.Success()).ToHttpResultAsync(Forbidden));
            app.MapGet("/unit/value-task/failure", () =>
                ValueTask.FromResult(UnitResult<DomainError>.Failure(new DomainError("value-task-unit")))
                    .ToHttpResultAsync(Forbidden));
            app.MapGet("/unit/task/success/mapped", () =>
                Task.FromResult(UnitResult<DomainError>.Success()).ToHttpResultAsync(
                    Forbidden,
                    () =>
                    {
                        taskMapCalls++;
                        return Results.Ok(new Payload("task-unit", 16));
                    }));
            app.MapGet("/unit/value-task/success/mapped", () =>
                ValueTask.FromResult(UnitResult<DomainError>.Success()).ToHttpResultAsync(
                    Forbidden,
                    () =>
                    {
                        valueTaskMapCalls++;
                        return Results.Ok(new Payload("value-task-unit", 17));
                    }));
        });

        using var taskSuccess = await host.Client.GetAsync("/unit/task/success", TestContext.Current.CancellationToken);
        using var taskFailure = await host.Client.GetAsync("/unit/task/failure", TestContext.Current.CancellationToken);
        using var valueTaskSuccess = await host.Client.GetAsync(
            "/unit/value-task/success",
            TestContext.Current.CancellationToken);
        using var valueTaskFailure = await host.Client.GetAsync(
            "/unit/value-task/failure",
            TestContext.Current.CancellationToken);
        using var taskMapped = await host.Client.GetAsync(
            "/unit/task/success/mapped",
            TestContext.Current.CancellationToken);
        using var valueTaskMapped = await host.Client.GetAsync(
            "/unit/value-task/success/mapped",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, taskSuccess.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, valueTaskSuccess.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, taskFailure.StatusCode);
        Assert.Equal("task-unit", await ReadProblemDetailAsync(taskFailure));
        Assert.Equal(HttpStatusCode.Forbidden, valueTaskFailure.StatusCode);
        Assert.Equal("value-task-unit", await ReadProblemDetailAsync(valueTaskFailure));
        Assert.Equal(HttpStatusCode.OK, taskMapped.StatusCode);
        Assert.Equal(
            new Payload("task-unit", 16),
            await taskMapped.Content.ReadFromJsonAsync<Payload>(TestContext.Current.CancellationToken));
        Assert.Equal(HttpStatusCode.OK, valueTaskMapped.StatusCode);
        Assert.Equal(
            new Payload("value-task-unit", 17),
            await valueTaskMapped.Content.ReadFromJsonAsync<Payload>(TestContext.Current.CancellationToken));
        Assert.Equal(1, taskMapCalls);
        Assert.Equal(1, valueTaskMapCalls);
    }

    [Fact]
    public async Task UnitResultEffectsMapWithAndWithoutEnvironments()
    {
        var environment = new EndpointEnvironment("unit-environment");
        EndpointEnvironment? observedEnvironment = null;
        CancellationToken effectRequestToken = default;
        CancellationToken effectToken = default;
        CancellationToken environmentRequestToken = default;
        CancellationToken environmentFailureToken = default;
        CancellationToken environmentSuccessRequestToken = default;
        CancellationToken environmentSuccessToken = default;

        var successEffect = Effect.FromValue(UnitResult<DomainError>.Success());
        var failureEffect = Effect.FromSync<UnitResult<DomainError>>(token =>
        {
            effectToken = token;
            return UnitResult<DomainError>.Failure(new DomainError("effect-unit"));
        });
        var environmentFailureEffect = Effect.FromSync<EndpointEnvironment, UnitResult<DomainError>>(
            (current, token) =>
            {
                observedEnvironment = current;
                environmentFailureToken = token;
                return UnitResult<DomainError>.Failure(new DomainError("environment-unit"));
            });
        var environmentSuccessEffect = Effect.FromSync<EndpointEnvironment, UnitResult<DomainError>>(
            (_, token) =>
            {
                environmentSuccessToken = token;
                return UnitResult<DomainError>.Success();
            });

        await using var host = await TestApplication.StartAsync(app =>
        {
            app.MapGet("/unit/effect/success", (HttpContext context) =>
                successEffect.ToHttpResultAsync(context, Forbidden));
            app.MapGet("/unit/effect/failure", (HttpContext context) =>
            {
                effectRequestToken = context.RequestAborted;
                return failureEffect.ToHttpResultAsync(context, Forbidden);
            });
            app.MapGet("/unit/effect/environment/failure", (HttpContext context) =>
            {
                environmentRequestToken = context.RequestAborted;
                return environmentFailureEffect.ToHttpResultAsync(environment, context, Forbidden);
            });
            app.MapGet("/unit/effect/environment/success", (HttpContext context) =>
            {
                environmentSuccessRequestToken = context.RequestAborted;
                return environmentSuccessEffect.ToHttpResultAsync(environment, context, Forbidden);
            });
        });

        using var successResponse = await host.Client.GetAsync("/unit/effect/success", TestContext.Current.CancellationToken);
        using var failureResponse = await host.Client.GetAsync("/unit/effect/failure", TestContext.Current.CancellationToken);
        using var environmentFailureResponse = await host.Client.GetAsync(
            "/unit/effect/environment/failure",
            TestContext.Current.CancellationToken);
        using var environmentSuccessResponse = await host.Client.GetAsync(
            "/unit/effect/environment/success",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, successResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, failureResponse.StatusCode);
        Assert.Equal("effect-unit", await ReadProblemDetailAsync(failureResponse));
        Assert.Equal(HttpStatusCode.Forbidden, environmentFailureResponse.StatusCode);
        Assert.Equal("environment-unit", await ReadProblemDetailAsync(environmentFailureResponse));
        Assert.Equal(HttpStatusCode.NoContent, environmentSuccessResponse.StatusCode);
        Assert.Same(environment, observedEnvironment);
        Assert.Equal(effectRequestToken, effectToken);
        Assert.Equal(environmentRequestToken, environmentFailureToken);
        Assert.Equal(environmentSuccessRequestToken, environmentSuccessToken);
    }

    [Fact]
    public async Task UninitializedUnitResultThrowsFromSyncAndAwaitedMappings()
    {
        UnitResult<string> uninitialized = default;

        var syncException = Assert.Throws<InvalidOperationException>(
            () => uninitialized.ToHttpResult(_ => NotFound()));
        var taskException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => Task.FromResult(uninitialized).ToHttpResultAsync(_ => NotFound()));
        var valueTaskException = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await ValueTask.FromResult(uninitialized).ToHttpResultAsync(_ => NotFound()));

        Assert.Equal("The unit result has not been initialized.", syncException.Message);
        Assert.Equal("The unit result has not been initialized.", taskException.Message);
        Assert.Equal("The unit result has not been initialized.", valueTaskException.Message);

        var failure = new InvalidOperationException("unit task failed");
        var faulted = Task.FromException<UnitResult<string>>(failure).ToHttpResultAsync(_ => NotFound());

        Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => faulted));
    }

    [Fact]
    public void UnitResultGuardsRejectNullMappersAndInvalidProblems()
    {
        var success = UnitResult<DomainError>.Success();
        var failure = UnitResult<DomainError>.Failure(new DomainError("guard"));
        var successTask = Task.FromResult(success);
        var successValueTask = ValueTask.FromResult(success);
        var successEffect = Effect.FromValue(success);
        var environmentSuccessEffect = Effect.FromSync<EndpointEnvironment, UnitResult<DomainError>>(_ => success);
        var environment = new EndpointEnvironment("guard-environment");
        var context = new DefaultHttpContext();
        Task<UnitResult<DomainError>>? nullTask = null;

        var cases = new (string Name, string ParameterName, Action Invoke)[]
        {
            ("UnitResult mapper", "failure", () => success.ToHttpResult(null!)),
            ("Task<UnitResult> source", "result", () => nullTask!.ToHttpResultAsync(Forbidden)),
            ("Task<UnitResult> mapper", "failure", () => successTask.ToHttpResultAsync(null!)),
            ("ValueTask<UnitResult> mapper", "failure", () => successValueTask.ToHttpResultAsync(null!)),
            ("Effect<UnitResult> context", "context", () => successEffect.ToHttpResultAsync(null!, Forbidden)),
            ("Effect<UnitResult> mapper", "failure", () => successEffect.ToHttpResultAsync(context, null!)),
            ("Environment Effect<UnitResult> context", "context", () =>
                environmentSuccessEffect.ToHttpResultAsync(environment, null!, Forbidden)),
            ("Environment Effect<UnitResult> mapper", "failure", () =>
                environmentSuccessEffect.ToHttpResultAsync(environment, context, null!)),
        };

        foreach (var (name, parameterName, invoke) in cases)
        {
            var exception = Assert.Throws<ArgumentNullException>(invoke);
            Assert.True(
                string.Equals(parameterName, exception.ParamName, StringComparison.Ordinal),
                $"{name}: expected parameter '{parameterName}', but received '{exception.ParamName}'.");
        }

        var nullProblem = Assert.Throws<InvalidOperationException>(() => failure.ToHttpResult(_ => null!));
        var statuslessProblem = Assert.Throws<InvalidOperationException>(
            () => failure.ToHttpResult(_ => new ProblemDetails()));

        Assert.Equal("Problem mappers must return a problem with a status.", nullProblem.Message);
        Assert.Equal("Problem mappers must return a problem with a status.", statuslessProblem.Message);
        Assert.Throws<InvalidOperationException>(() => success.ToHttpResult(Forbidden, () => null!));
    }

    private static async Task<string?> ReadProblemDetailAsync(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        return document.RootElement.GetProperty("detail").GetString();
    }

    private static ProblemDetails NotFound() => new()
    {
        Status = StatusCodes.Status404NotFound,
        Title = "Resource not found",
    };

    private static ProblemDetails Forbidden(DomainError error) => new()
    {
        Status = StatusCodes.Status403Forbidden,
        Title = "Domain operation denied",
        Detail = error.Code,
    };

    private sealed class TestApplication(WebApplication application, HttpClient client) : IAsyncDisposable
    {
        public HttpClient Client { get; } = client;

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
            await application.StopAsync(TestContext.Current.CancellationToken);
            await application.DisposeAsync();
        }
    }

    private sealed record Payload(string Name, int Count);

    private sealed record DomainError(string Code);

    private sealed record EndpointEnvironment(string Value);
}
