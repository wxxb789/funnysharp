using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FunnySharp;
using FunnySharp.AspNetCore;
using FunnySharp.Tests;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;

namespace FunnySharp.AspNetCore.Tests;

public sealed class HttpResultExtensionsTests
{
    [Fact]
    public async Task OptionSomeUsesTheDefaultJsonSuccessResult()
    {
        await using var host = await TestApplication.StartAsync(app =>
            app.MapGet("/option", () =>
                Option.Some(new Payload("option", 1)).ToHttpResult(NotFound)));

        using var response = await host.Client.GetAsync("/option", TestContext.Current.CancellationToken);
        var payload = await response.Content.ReadFromJsonAsync<Payload>(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(new Payload("option", 1), payload);
    }

    [Fact]
    public async Task OptionNoneUsesTheMappedProblemStatusIncludingCustomOverrides()
    {
        await using var host = await TestApplication.StartAsync(app =>
        {
            app.MapGet("/option/not-found", () => Option.None<Payload>().ToHttpResult(NotFound));
            app.MapGet("/option/gone", () => Option.None<Payload>().ToHttpResult(Gone));
        });

        using var notFound = await host.Client.GetAsync("/option/not-found", TestContext.Current.CancellationToken);
        using var gone = await host.Client.GetAsync("/option/gone", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, notFound.StatusCode);
        Assert.Equal("application/problem+json", notFound.Content.Headers.ContentType?.MediaType);
        Assert.Equal(HttpStatusCode.Gone, gone.StatusCode);
        Assert.Equal("application/problem+json", gone.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task ResultFailureUsesTheMappedDomainProblem()
    {
        await using var host = await TestApplication.StartAsync(app =>
            app.MapGet("/result", () =>
                Result<Payload, DomainError>.Failure(new DomainError("account-disabled"))
                    .ToHttpResult(error => new ProblemDetails
                    {
                        Status = StatusCodes.Status403Forbidden,
                        Type = "https://example.invalid/problems/account-unavailable",
                        Title = "Account unavailable",
                        Detail = error.Code,
                        Instance = "/result",
                        Extensions =
                        {
                            ["errorCode"] = error.Code,
                        },
                    })));

        using var response = await host.Client.GetAsync("/result", TestContext.Current.CancellationToken);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        var problem = document.RootElement;

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(StatusCodes.Status403Forbidden, problem.GetProperty("status").GetInt32());
        Assert.Equal("https://example.invalid/problems/account-unavailable", problem.GetProperty("type").GetString());
        Assert.Equal("Account unavailable", problem.GetProperty("title").GetString());
        Assert.Equal("account-disabled", problem.GetProperty("detail").GetString());
        Assert.Equal("/result", problem.GetProperty("instance").GetString());
        Assert.Equal("account-disabled", problem.GetProperty("errorCode").GetString());
    }

    [Fact]
    public async Task SuccessMappingCanOverrideTheDefaultJsonResult()
    {
        var successMapCalls = 0;
        await using var host = await TestApplication.StartAsync(app =>
        {
            app.MapGet("/result/success", () => Result<Payload, DomainError>.Success(new Payload("result", 6))
                .ToHttpResult(
                    Forbidden,
                    value =>
                    {
                        successMapCalls++;
                        return Results.Created("/result/6", value);
                    }));
            app.MapGet("/validation/valid", () => Validation<Payload, string>.Valid(new Payload("validation", 7))
                .ToHttpResult(Invalid));
        });

        using var resultResponse = await host.Client.GetAsync("/result/success", TestContext.Current.CancellationToken);
        using var validationResponse = await host.Client.GetAsync("/validation/valid", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, resultResponse.StatusCode);
        Assert.Equal("application/json", resultResponse.Content.Headers.ContentType?.MediaType);
        Assert.Equal("/result/6", resultResponse.Headers.Location?.OriginalString);
        Assert.Equal(new Payload("result", 6), await resultResponse.Content.ReadFromJsonAsync<Payload>(TestContext.Current.CancellationToken));
        Assert.Equal(1, successMapCalls);
        Assert.Equal(HttpStatusCode.OK, validationResponse.StatusCode);
        Assert.Equal("application/json", validationResponse.Content.Headers.ContentType?.MediaType);
        Assert.Equal(new Payload("validation", 7), await validationResponse.Content.ReadFromJsonAsync<Payload>(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ValidationInvalidUsesTheMappedValidationProblemWithOrderedErrors()
    {
        await using var host = await TestApplication.StartAsync(app =>
            app.MapGet("/validation", () =>
                Validation<Payload, string>.InvalidMany(["email", "age"])
                    .ToHttpResult(errors => new HttpValidationProblemDetails(
                        new Dictionary<string, string[]>
                        {
                            ["input"] = errors.ToArray(),
                        })
                    {
                        Status = StatusCodes.Status422UnprocessableEntity,
                        Type = "https://example.invalid/problems/validation",
                        Title = "Validation failed",
                        Detail = "Two fields were invalid.",
                        Instance = "/validation",
                        Extensions =
                        {
                            ["errorCount"] = errors.Count,
                        },
                    })));

        using var response = await host.Client.GetAsync("/validation", TestContext.Current.CancellationToken);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        var errors = document.RootElement
            .GetProperty("errors")
            .GetProperty("input")
            .EnumerateArray()
            .Select(error => error.GetString())
            .ToArray();

        Assert.Equal((HttpStatusCode)StatusCodes.Status422UnprocessableEntity, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("https://example.invalid/problems/validation", document.RootElement.GetProperty("type").GetString());
        Assert.Equal("Validation failed", document.RootElement.GetProperty("title").GetString());
        Assert.Equal("Two fields were invalid.", document.RootElement.GetProperty("detail").GetString());
        Assert.Equal("/validation", document.RootElement.GetProperty("instance").GetString());
        Assert.Equal(2, document.RootElement.GetProperty("errorCount").GetInt32());
        Assert.Equal(["email", "age"], errors);
    }

    [Fact]
    public async Task TaskAndValueTaskCarriersUseMatchingAsyncMappers()
    {
        await using var host = await TestApplication.StartAsync(app =>
        {
            app.MapGet("/task/option", () => Task.FromResult(Option.Some(new Payload("task-option", 1)))
                .ToHttpResultAsync(NotFound));
            app.MapGet("/value-task/option", () => ValueTask.FromResult(Option.Some(new Payload("value-task-option", 2)))
                .ToHttpResultAsync(NotFound));
            app.MapGet("/task/result", () => Task.FromResult(Result<Payload, DomainError>.Failure(new DomainError("task-result")))
                .ToHttpResultAsync(Forbidden));
            app.MapGet("/value-task/result", () => ValueTask.FromResult(Result<Payload, DomainError>.Failure(new DomainError("value-task-result")))
                .ToHttpResultAsync(Forbidden));
            app.MapGet("/task/validation", () => Task.FromResult(Validation<Payload, string>.Invalid("task-validation"))
                .ToHttpResultAsync(Invalid));
            app.MapGet("/value-task/validation", () => ValueTask.FromResult(Validation<Payload, string>.Invalid("value-task-validation"))
                .ToHttpResultAsync(Invalid));
        });

        using var taskOption = await host.Client.GetAsync("/task/option", TestContext.Current.CancellationToken);
        using var valueTaskOption = await host.Client.GetAsync("/value-task/option", TestContext.Current.CancellationToken);
        using var taskResult = await host.Client.GetAsync("/task/result", TestContext.Current.CancellationToken);
        using var valueTaskResult = await host.Client.GetAsync("/value-task/result", TestContext.Current.CancellationToken);
        using var taskValidation = await host.Client.GetAsync("/task/validation", TestContext.Current.CancellationToken);
        using var valueTaskValidation = await host.Client.GetAsync("/value-task/validation", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, taskOption.StatusCode);
        Assert.Equal(HttpStatusCode.OK, valueTaskOption.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, taskResult.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, valueTaskResult.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, taskValidation.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, valueTaskValidation.StatusCode);
    }

    [Fact]
    public async Task AsyncSuccessMappersOverrideTheDefaultJsonResult()
    {
        var taskMapCalls = 0;
        var valueTaskMapCalls = 0;
        var effectMapCalls = 0;
        var effect = Effect.FromValue(
            Result<Payload, DomainError>.Success(new Payload("effect-result", 13)));

        await using var host = await TestApplication.StartAsync(app =>
        {
            app.MapGet("/task/result/success", () =>
                Task.FromResult(Result<Payload, DomainError>.Success(new Payload("task-result", 11)))
                    .ToHttpResultAsync(
                        Forbidden,
                        value =>
                        {
                            taskMapCalls++;
                            return Results.Created("/task/result/11", value);
                        }));
            app.MapGet("/value-task/result/success", () =>
                ValueTask.FromResult(Result<Payload, DomainError>.Success(new Payload("value-task-result", 12)))
                    .ToHttpResultAsync(
                        Forbidden,
                        value =>
                        {
                            valueTaskMapCalls++;
                            return Results.Created("/value-task/result/12", value);
                        }));
            app.MapGet("/effect/result/success", (HttpContext context) =>
                effect.ToHttpResultAsync(
                    context,
                    Forbidden,
                    value =>
                    {
                        effectMapCalls++;
                        return Results.Created("/effect/result/13", value);
                    }));
        });

        using var taskResponse = await host.Client.GetAsync(
            "/task/result/success",
            TestContext.Current.CancellationToken);
        using var valueTaskResponse = await host.Client.GetAsync(
            "/value-task/result/success",
            TestContext.Current.CancellationToken);
        using var effectResponse = await host.Client.GetAsync(
            "/effect/result/success",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, taskResponse.StatusCode);
        Assert.Equal("/task/result/11", taskResponse.Headers.Location?.OriginalString);
        Assert.Equal(
            new Payload("task-result", 11),
            await taskResponse.Content.ReadFromJsonAsync<Payload>(TestContext.Current.CancellationToken));
        Assert.Equal(1, taskMapCalls);

        Assert.Equal(HttpStatusCode.Created, valueTaskResponse.StatusCode);
        Assert.Equal("/value-task/result/12", valueTaskResponse.Headers.Location?.OriginalString);
        Assert.Equal(
            new Payload("value-task-result", 12),
            await valueTaskResponse.Content.ReadFromJsonAsync<Payload>(TestContext.Current.CancellationToken));
        Assert.Equal(1, valueTaskMapCalls);

        Assert.Equal(HttpStatusCode.Created, effectResponse.StatusCode);
        Assert.Equal("/effect/result/13", effectResponse.Headers.Location?.OriginalString);
        Assert.Equal(
            new Payload("effect-result", 13),
            await effectResponse.Content.ReadFromJsonAsync<Payload>(TestContext.Current.CancellationToken));
        Assert.Equal(1, effectMapCalls);
    }

    [Fact]
    public async Task ValueTaskCarrierIsConsumedExactlyOnce()
    {
        var source = new CountingValueTaskSource<Option<Payload>>(Option.Some(new Payload("single-use", 8)));
        await using var host = await TestApplication.StartAsync(app =>
            app.MapGet("/value-task/single-use", () => source.CreateValueTask().ToHttpResultAsync(NotFound)));

        using var response = await host.Client.GetAsync("/value-task/single-use", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, source.GetResultCount);
    }

    [Fact]
    public async Task AsyncCarriersPreserveFaultIdentityAndCancellationToken()
    {
        var failure = new InvalidOperationException("task failed");
        var faulted = Task.FromException<Option<Payload>>(failure).ToHttpResultAsync(NotFound);

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() => faulted);

        Assert.Same(failure, actual);

        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var canceled = new ValueTask<Result<Payload, DomainError>>(
                Task.FromCanceled<Result<Payload, DomainError>>(cancellationSource.Token))
            .ToHttpResultAsync(Forbidden);

        var cancellation = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await canceled);

        Assert.Equal(cancellationSource.Token, cancellation.CancellationToken);
    }

    [Fact]
    public async Task EffectUsesTheExactRequestAbortedToken()
    {
        var observedRequestToken = new TaskCompletionSource<CancellationToken>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var observedEffectToken = new TaskCompletionSource<CancellationToken>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var effect = Effect.FromSync<Option<Payload>>(token =>
        {
            observedEffectToken.TrySetResult(token);
            return Option.Some(new Payload("effect", 3));
        });

        await using var host = await TestApplication.StartAsync(app =>
            app.MapGet("/effect", (HttpContext context) =>
            {
                observedRequestToken.TrySetResult(context.RequestAborted);
                return effect.ToHttpResultAsync(context, NotFound);
            }));

        using var response = await host.Client.GetAsync("/effect", TestContext.Current.CancellationToken);
        var requestToken = await observedRequestToken.Task.WaitAsync(TestContext.Current.CancellationToken);
        var effectToken = await observedEffectToken.Task.WaitAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(requestToken, effectToken);
    }

    [Fact]
    public async Task EffectObservesClientCancellationThroughRequestAborted()
    {
        var requestStarted = new TaskCompletionSource<CancellationToken>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var effectStarted = new TaskCompletionSource<CancellationToken>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var canceled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var effect = Effect.FromValueTask<Option<Payload>>(async token =>
        {
            effectStarted.TrySetResult(token);
            using var registration = token.Register(canceled.SetResult);
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
            return Option.Some(new Payload("unreachable", 0));
        });

        await using var host = await TestApplication.StartAsync(app =>
            app.MapGet("/effect/cancel", (HttpContext context) =>
            {
                requestStarted.TrySetResult(context.RequestAborted);
                return effect.ToHttpResultAsync(context, NotFound);
            }));

        using var cancellationSource = new CancellationTokenSource();
        var request = host.Client.GetAsync("/effect/cancel", cancellationSource.Token);
        var requestToken = await requestStarted.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        var effectToken = await effectStarted.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        cancellationSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => request);
        await canceled.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        Assert.Equal(requestToken, effectToken);
        Assert.True(effectToken.IsCancellationRequested);
    }

    [Fact]
    public async Task TaskHandlerObservesRequestAbortedWhenTheCallerPassesIt()
    {
        var operationStarted = new TaskCompletionSource<CancellationToken>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var canceled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await using var host = await TestApplication.StartAsync(app =>
            app.MapGet("/task/cancel", (CancellationToken cancellationToken) =>
                WaitForCancellationAsync(cancellationToken).ToHttpResultAsync(NotFound)));

        using var cancellationSource = new CancellationTokenSource();
        var request = host.Client.GetAsync("/task/cancel", cancellationSource.Token);
        var observedToken = await operationStarted.Task.WaitAsync(
            TimeSpan.FromSeconds(10),
            TestContext.Current.CancellationToken);

        cancellationSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => request);
        await canceled.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        Assert.True(observedToken.IsCancellationRequested);

        async Task<Option<Payload>> WaitForCancellationAsync(CancellationToken cancellationToken)
        {
            operationStarted.TrySetResult(cancellationToken);
            using var registration = cancellationToken.Register(canceled.SetResult);
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return Option.Some(new Payload("unreachable", 0));
        }
    }

    [Fact]
    public async Task EnvironmentEffectReceivesTheExplicitEnvironment()
    {
        var environment = new EndpointEnvironment("environment-value");
        EndpointEnvironment? observedEnvironment = null;
        var effect = Effect.FromSync<EndpointEnvironment, Option<Payload>>((current, _) =>
        {
            observedEnvironment = current;
            return Option.Some(new Payload(current.Value, 4));
        });

        await using var host = await TestApplication.StartAsync(app =>
            app.MapGet("/effect/environment", (HttpContext context) =>
                effect.ToHttpResultAsync(environment, context, NotFound)));

        using var response = await host.Client.GetAsync("/effect/environment", TestContext.Current.CancellationToken);
        var payload = await response.Content.ReadFromJsonAsync<Payload>(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Same(environment, observedEnvironment);
        Assert.Equal(new Payload("environment-value", 4), payload);
    }

    [Fact]
    public async Task ResultAndValidationEffectsMapWithAndWithoutEnvironments()
    {
        var environment = new EndpointEnvironment("environment");
        CancellationToken resultRequestToken = default;
        CancellationToken resultEffectToken = default;
        CancellationToken validationRequestToken = default;
        CancellationToken validationEffectToken = default;
        CancellationToken environmentResultRequestToken = default;
        CancellationToken environmentResultEffectToken = default;
        CancellationToken environmentValidationRequestToken = default;
        CancellationToken environmentValidationEffectToken = default;

        var resultEffect = Effect.FromSync<Result<Payload, DomainError>>(token =>
        {
            resultEffectToken = token;
            return Result<Payload, DomainError>.Failure(new DomainError("result-effect"));
        });
        var validationEffect = Effect.FromSync<Validation<Payload, string>>(token =>
        {
            validationEffectToken = token;
            return Validation<Payload, string>.Invalid("validation-effect");
        });
        var environmentResultEffect = Effect.FromSync<EndpointEnvironment, Result<Payload, DomainError>>(
            (current, token) =>
            {
                environmentResultEffectToken = token;
                return Result<Payload, DomainError>.Success(new Payload(current.Value, 9));
            });
        var environmentValidationEffect = Effect.FromSync<EndpointEnvironment, Validation<Payload, string>>(
            (_, token) =>
            {
                environmentValidationEffectToken = token;
                return Validation<Payload, string>.Invalid("environment-validation-effect");
            });

        await using var host = await TestApplication.StartAsync(app =>
        {
            app.MapGet("/effect/result", (HttpContext context) =>
            {
                resultRequestToken = context.RequestAborted;
                return resultEffect.ToHttpResultAsync(context, Forbidden);
            });
            app.MapGet("/effect/validation", (HttpContext context) =>
            {
                validationRequestToken = context.RequestAborted;
                return validationEffect.ToHttpResultAsync(context, Invalid);
            });
            app.MapGet("/effect/environment/result", (HttpContext context) =>
            {
                environmentResultRequestToken = context.RequestAborted;
                return environmentResultEffect.ToHttpResultAsync(environment, context, Forbidden);
            });
            app.MapGet("/effect/environment/validation", (HttpContext context) =>
            {
                environmentValidationRequestToken = context.RequestAborted;
                return environmentValidationEffect.ToHttpResultAsync(environment, context, Invalid);
            });
        });

        using var resultResponse = await host.Client.GetAsync("/effect/result", TestContext.Current.CancellationToken);
        using var validationResponse = await host.Client.GetAsync("/effect/validation", TestContext.Current.CancellationToken);
        using var environmentResultResponse = await host.Client.GetAsync(
            "/effect/environment/result",
            TestContext.Current.CancellationToken);
        using var environmentValidationResponse = await host.Client.GetAsync(
            "/effect/environment/validation",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, resultResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, validationResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, environmentResultResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, environmentValidationResponse.StatusCode);
        Assert.Equal(resultRequestToken, resultEffectToken);
        Assert.Equal(validationRequestToken, validationEffectToken);
        Assert.Equal(environmentResultRequestToken, environmentResultEffectToken);
        Assert.Equal(environmentValidationRequestToken, environmentValidationEffectToken);
    }

    [Fact]
    public async Task UnexpectedMapperHandlerAndEffectExceptionsReachTheHostBoundary()
    {
        var mapperException = new InvalidOperationException("none mapper failed");
        var handlerException = new InvalidOperationException("some handler failed");
        var effectException = new InvalidOperationException("effect failed");

        await using var host = await TestApplication.StartAsync(app =>
        {
            app.Use(ExceptionCaptureMiddleware);
            app.MapGet("/exception/mapper", () => Option.None<Payload>().ToHttpResult(() => throw mapperException));
            app.MapGet("/exception/handler", () => Option.Some(new Payload("handler", 5))
                .ToHttpResult(NotFound, _ => throw handlerException));
            app.MapGet("/exception/effect", async context => await Effect.FromSync<Option<Payload>>(() => throw effectException)
                .ToHttpResultAsync(context, NotFound));
        });

        using var mapperResponse = await host.Client.GetAsync("/exception/mapper", TestContext.Current.CancellationToken);
        using var handlerResponse = await host.Client.GetAsync("/exception/handler", TestContext.Current.CancellationToken);
        using var effectResponse = await host.Client.GetAsync("/exception/effect", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.InternalServerError, mapperResponse.StatusCode);
        Assert.Equal("none mapper failed", await mapperResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.Equal(HttpStatusCode.InternalServerError, handlerResponse.StatusCode);
        Assert.Equal("some handler failed", await handlerResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.Equal(HttpStatusCode.InternalServerError, effectResponse.StatusCode);
        Assert.Equal("effect failed", await effectResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public void RequiredMappersAreValidatedAndProblemsWithoutStatusAreRejected()
    {
        Assert.Throws<ArgumentNullException>(() => Option.None<Payload>().ToHttpResult(null!));
        Assert.Throws<ArgumentNullException>(() => Result<Payload, DomainError>.Failure(new DomainError("failure"))
            .ToHttpResult(null!));
        Assert.Throws<ArgumentNullException>(() => Validation<Payload, string>.Invalid("invalid").ToHttpResult(null!));

        Assert.Throws<InvalidOperationException>(() => Option.None<Payload>().ToHttpResult(() => new ProblemDetails()));
        Assert.Throws<InvalidOperationException>(() => Result<Payload, DomainError>.Failure(new DomainError("failure"))
            .ToHttpResult(_ => new ProblemDetails()));
        Assert.Throws<InvalidOperationException>(() => Validation<Payload, string>.Invalid("invalid")
            .ToHttpResult(_ => new HttpValidationProblemDetails()));
        Assert.Throws<InvalidOperationException>(() => Option.None<Payload>().ToHttpResult(() => null!));
        Assert.Throws<InvalidOperationException>(() => Option.Some(new Payload("some", 10))
            .ToHttpResult(NotFound, _ => null!));
    }

    [Fact]
    public void AsyncOverloadsValidateEveryRequiredArgument()
    {
        var payload = new Payload("guard", 14);
        var context = new DefaultHttpContext();
        var environment = new EndpointEnvironment("guard-environment");

        var optionTask = Task.FromResult(Option.Some(payload));
        var resultTask = Task.FromResult(Result<Payload, DomainError>.Success(payload));
        var validationTask = Task.FromResult(Validation<Payload, string>.Valid(payload));
        Task<Option<Payload>>? nullOptionTask = null;
        Task<Result<Payload, DomainError>>? nullResultTask = null;
        Task<Validation<Payload, string>>? nullValidationTask = null;

        var optionValueTask = ValueTask.FromResult(Option.Some(payload));
        var resultValueTask = ValueTask.FromResult(Result<Payload, DomainError>.Success(payload));
        var validationValueTask = ValueTask.FromResult(Validation<Payload, string>.Valid(payload));

        var optionEffect = Effect.FromValue(Option.Some(payload));
        var resultEffect = Effect.FromValue(Result<Payload, DomainError>.Success(payload));
        var validationEffect = Effect.FromValue(Validation<Payload, string>.Valid(payload));
        var environmentOptionEffect = Effect.FromSync<EndpointEnvironment, Option<Payload>>(_ => Option.Some(payload));
        var environmentResultEffect = Effect.FromSync<EndpointEnvironment, Result<Payload, DomainError>>(
            _ => Result<Payload, DomainError>.Success(payload));
        var environmentValidationEffect = Effect.FromSync<EndpointEnvironment, Validation<Payload, string>>(
            _ => Validation<Payload, string>.Valid(payload));

        var cases = new (string Name, string ParameterName, Action Invoke)[]
        {
            ("Task<Option> source", "option", () => nullOptionTask!.ToHttpResultAsync(NotFound)),
            ("Task<Result> source", "result", () => nullResultTask!.ToHttpResultAsync(Forbidden)),
            ("Task<Validation> source", "validation", () => nullValidationTask!.ToHttpResultAsync(Invalid)),
            ("Task<Option> mapper", "none", () => optionTask.ToHttpResultAsync(null!)),
            ("Task<Result> mapper", "failure", () => resultTask.ToHttpResultAsync(null!)),
            ("Task<Validation> mapper", "invalid", () => validationTask.ToHttpResultAsync(null!)),
            ("ValueTask<Option> mapper", "none", () => optionValueTask.ToHttpResultAsync(null!)),
            ("ValueTask<Result> mapper", "failure", () => resultValueTask.ToHttpResultAsync(null!)),
            ("ValueTask<Validation> mapper", "invalid", () => validationValueTask.ToHttpResultAsync(null!)),
            ("Effect<Option> context", "context", () => optionEffect.ToHttpResultAsync(null!, NotFound)),
            ("Effect<Result> context", "context", () => resultEffect.ToHttpResultAsync(null!, Forbidden)),
            ("Effect<Validation> context", "context", () => validationEffect.ToHttpResultAsync(null!, Invalid)),
            ("Effect<Option> mapper", "none", () => optionEffect.ToHttpResultAsync(context, null!)),
            ("Effect<Result> mapper", "failure", () => resultEffect.ToHttpResultAsync(context, null!)),
            ("Effect<Validation> mapper", "invalid", () => validationEffect.ToHttpResultAsync(context, null!)),
            ("Environment Effect<Option> context", "context", () =>
                environmentOptionEffect.ToHttpResultAsync(environment, null!, NotFound)),
            ("Environment Effect<Result> context", "context", () =>
                environmentResultEffect.ToHttpResultAsync(environment, null!, Forbidden)),
            ("Environment Effect<Validation> context", "context", () =>
                environmentValidationEffect.ToHttpResultAsync(environment, null!, Invalid)),
            ("Environment Effect<Option> mapper", "none", () =>
                environmentOptionEffect.ToHttpResultAsync(environment, context, null!)),
            ("Environment Effect<Result> mapper", "failure", () =>
                environmentResultEffect.ToHttpResultAsync(environment, context, null!)),
            ("Environment Effect<Validation> mapper", "invalid", () =>
                environmentValidationEffect.ToHttpResultAsync(environment, context, null!)),
        };

        foreach (var (name, parameterName, invoke) in cases)
        {
            var exception = Assert.Throws<ArgumentNullException>(invoke);
            Assert.True(
                string.Equals(parameterName, exception.ParamName, StringComparison.Ordinal),
                $"{name}: expected parameter '{parameterName}', but received '{exception.ParamName}'.");
        }
    }

    private static ProblemDetails NotFound() => new()
    {
        Status = StatusCodes.Status404NotFound,
        Title = "Resource not found",
    };

    private static ProblemDetails Gone() => new()
    {
        Status = StatusCodes.Status410Gone,
        Title = "Resource expired",
    };

    private static ProblemDetails Forbidden(DomainError error) => new()
    {
        Status = StatusCodes.Status403Forbidden,
        Title = "Domain operation denied",
        Detail = error.Code,
    };

    private static HttpValidationProblemDetails Invalid(IReadOnlyList<string> errors) => new(
        new Dictionary<string, string[]>
        {
            ["input"] = errors.ToArray(),
        })
    {
        Status = StatusCodes.Status400BadRequest,
        Title = "Invalid request",
    };

    private static async Task ExceptionCaptureMiddleware(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsync(exception.Message, context.RequestAborted);
        }
    }

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
