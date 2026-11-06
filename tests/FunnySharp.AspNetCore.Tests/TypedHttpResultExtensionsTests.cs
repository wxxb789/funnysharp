using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FunnySharp;
using FunnySharp.AspNetCore;
using FunnySharp.Tests;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;

namespace FunnySharp.AspNetCore.Tests;

public sealed class TypedHttpResultExtensionsTests
{
    [Fact]
    public async Task OptionTypedMappingServesPresenceAndAbsence()
    {
        await using var host = await TestApplication.StartAsync(app =>
        {
            app.MapGet("/typed/option/some", () =>
                Option.Some(new Payload("some", 1)).ToHttpResults(NotFoundAbsence));
            app.MapGet("/typed/option/none", () =>
                Option.None<Payload>().ToHttpResults(NotFoundAbsence));
        });

        using var some = await host.Client.GetAsync("/typed/option/some", TestContext.Current.CancellationToken);
        using var none = await host.Client.GetAsync("/typed/option/none", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, some.StatusCode);
        Assert.Equal(
            new Payload("some", 1),
            await some.Content.ReadFromJsonAsync<Payload>(TestContext.Current.CancellationToken));
        Assert.Equal(HttpStatusCode.NotFound, none.StatusCode);
        Assert.Equal("application/json", none.Content.Headers.ContentType?.MediaType);
        using var noneProblem = await ReadProblemAsync(none);
        Assert.Equal(
            "option-absent",
            noneProblem.RootElement.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task OptionTypedMappingUsesCallerSelectedPresenceResult()
    {
        await using var host = await TestApplication.StartAsync(app =>
            app.MapGet("/typed/option/accepted", () =>
                Option.Some(new Payload("accepted", 2)).ToHttpResults(
                    NotFoundAbsence,
                    value => TypedResults.Accepted("/typed/option/accepted", value))));

        using var response = await host.Client.GetAsync(
            "/typed/option/accepted",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal("/typed/option/accepted", response.Headers.Location?.OriginalString);
        Assert.Equal(
            new Payload("accepted", 2),
            await response.Content.ReadFromJsonAsync<Payload>(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ResultTypedMappingServesDefaultSuccessAndSelectedFailure()
    {
        await using var host = await TestApplication.StartAsync(app =>
        {
            app.MapGet("/typed/result/success", () =>
                Result<Payload, DomainError>.Success(new Payload("result", 3))
                    .ToHttpResults(NotFoundFailure));
            app.MapGet("/typed/result/failure", () =>
                Result<Payload, DomainError>.Failure(new DomainError("missing"))
                    .ToHttpResults(NotFoundFailure));
        });

        using var success = await host.Client.GetAsync(
            "/typed/result/success",
            TestContext.Current.CancellationToken);
        using var failure = await host.Client.GetAsync(
            "/typed/result/failure",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, success.StatusCode);
        Assert.Equal(
            new Payload("result", 3),
            await success.Content.ReadFromJsonAsync<Payload>(TestContext.Current.CancellationToken));
        Assert.Equal(HttpStatusCode.NotFound, failure.StatusCode);
        Assert.Equal("application/json", failure.Content.Headers.ContentType?.MediaType);
        using var failureProblem = await ReadProblemAsync(failure);
        Assert.Equal("missing", failureProblem.RootElement.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task ResultTypedMappingUsesCallerSelectedSuccessResult()
    {
        await using var host = await TestApplication.StartAsync(app =>
            app.MapGet("/typed/result/created", () =>
                Result<Payload, DomainError>.Success(new Payload("created", 4))
                    .ToHttpResults(
                        NotFoundFailure,
                        value => TypedResults.Created($"/typed/result/{value.Name}", value))));

        using var response = await host.Client.GetAsync(
            "/typed/result/created",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("/typed/result/created", response.Headers.Location?.OriginalString);
        Assert.Equal(
            new Payload("created", 4),
            await response.Content.ReadFromJsonAsync<Payload>(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task UnitResultTypedMappingServesNoContentAndSelectedFailure()
    {
        await using var host = await TestApplication.StartAsync(app =>
        {
            app.MapGet("/typed/unit/success", () =>
                UnitResult<DomainError>.Success().ToHttpResults(ConflictFailure));
            app.MapGet("/typed/unit/failure", () =>
                UnitResult<DomainError>.Failure(new DomainError("busy")).ToHttpResults(ConflictFailure));
        });

        using var success = await host.Client.GetAsync(
            "/typed/unit/success",
            TestContext.Current.CancellationToken);
        using var failure = await host.Client.GetAsync(
            "/typed/unit/failure",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, success.StatusCode);
        Assert.Empty(await success.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken));
        Assert.Null(success.Content.Headers.ContentType);
        Assert.Equal(HttpStatusCode.Conflict, failure.StatusCode);
        using var failureProblem = await ReadProblemAsync(failure);
        Assert.Equal("busy", failureProblem.RootElement.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task ValidationTypedMappingServesValidAndAccumulatedErrors()
    {
        await using var host = await TestApplication.StartAsync(app =>
        {
            app.MapGet("/typed/validation/valid", () =>
                Validation<Payload, InputError>.Valid(new Payload("valid", 5))
                    .ToHttpResults(InvalidValidation));
            app.MapGet("/typed/validation/invalid", () =>
                Validation<Payload, InputError>.InvalidMany(
                    [
                        new InputError("sku", "A SKU is required."),
                        new InputError("quantity", "Quantity must be positive."),
                        new InputError("quantity", "Quantity cannot exceed 20."),
                    ])
                    .ToHttpResults(InvalidValidation));
        });

        using var valid = await host.Client.GetAsync(
            "/typed/validation/valid",
            TestContext.Current.CancellationToken);
        using var invalid = await host.Client.GetAsync(
            "/typed/validation/invalid",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, valid.StatusCode);
        Assert.Equal(
            new Payload("valid", 5),
            await valid.Content.ReadFromJsonAsync<Payload>(TestContext.Current.CancellationToken));
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        Assert.Equal("application/problem+json", invalid.Content.Headers.ContentType?.MediaType);

        using var document = JsonDocument.Parse(
            await invalid.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        var errors = document.RootElement.GetProperty("errors");
        Assert.Equal("A SKU is required.", errors.GetProperty("sku")[0].GetString());
        Assert.Equal(
            ["Quantity must be positive.", "Quantity cannot exceed 20."],
            errors.GetProperty("quantity").EnumerateArray().Select(static item => item.GetString()).ToArray());
    }

    [Fact]
    public async Task ValidationTypedMappingAllowsCallerSelectedUnprocessableStatus()
    {
        await using var host = await TestApplication.StartAsync(app =>
            app.MapGet("/typed/validation/unprocessable", () =>
                Validation<Payload, InputError>.InvalidMany(
                    [
                        new InputError("sku", "A SKU is required."),
                    ])
                    .ToHttpResults(UnprocessableValidation)));

        using var response = await host.Client.GetAsync(
            "/typed/validation/unprocessable",
            TestContext.Current.CancellationToken);
        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal(422, document.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("A SKU is required.", document.RootElement.GetProperty("errors").GetProperty("sku")[0].GetString());
    }

    [Fact]
    public async Task TaskTypedMappingsServeEveryCarrier()
    {
        await using var host = await TestApplication.StartAsync(app =>
        {
            app.MapGet("/typed/task/option", () =>
                Task.FromResult(Option.Some(new Payload("task-option", 6))).ToHttpResultsAsync(NotFoundAbsence));
            app.MapGet("/typed/task/option/none", () =>
                Task.FromResult(Option.None<Payload>()).ToHttpResultsAsync(NotFoundAbsence));
            app.MapGet("/typed/task/result", () =>
                Task.FromResult(Result<Payload, DomainError>.Success(new Payload("task-result", 7)))
                    .ToHttpResultsAsync(NotFoundFailure));
            app.MapGet("/typed/task/result/failure", () =>
                Task.FromResult(Result<Payload, DomainError>.Failure(new DomainError("task-missing")))
                    .ToHttpResultsAsync(NotFoundFailure));
            app.MapGet("/typed/task/validation", () =>
                Task.FromResult(Validation<Payload, InputError>.Valid(new Payload("task-validation", 8)))
                    .ToHttpResultsAsync(InvalidValidation));
            app.MapGet("/typed/task/validation/invalid", () =>
                Task.FromResult(Validation<Payload, InputError>.InvalidMany(
                        [new InputError("sku", "A SKU is required.")]))
                    .ToHttpResultsAsync(InvalidValidation));
            app.MapGet("/typed/task/unit", () =>
                Task.FromResult(UnitResult<DomainError>.Success()).ToHttpResultsAsync(ConflictFailure));
            app.MapGet("/typed/task/unit/failure", () =>
                Task.FromResult(UnitResult<DomainError>.Failure(new DomainError("task-conflict")))
                    .ToHttpResultsAsync(ConflictFailure));
        });

        Assert.Equal(HttpStatusCode.OK, await StatusAsync(host, "/typed/task/option"));
        Assert.Equal(HttpStatusCode.NotFound, await StatusAsync(host, "/typed/task/option/none"));
        Assert.Equal(HttpStatusCode.OK, await StatusAsync(host, "/typed/task/result"));
        Assert.Equal(HttpStatusCode.NotFound, await StatusAsync(host, "/typed/task/result/failure"));
        Assert.Equal(HttpStatusCode.OK, await StatusAsync(host, "/typed/task/validation"));
        Assert.Equal(HttpStatusCode.BadRequest, await StatusAsync(host, "/typed/task/validation/invalid"));
        Assert.Equal(HttpStatusCode.NoContent, await StatusAsync(host, "/typed/task/unit"));
        Assert.Equal(HttpStatusCode.Conflict, await StatusAsync(host, "/typed/task/unit/failure"));
    }

    [Fact]
    public async Task ValueTaskTypedMappingsServeEveryCarrier()
    {
        await using var host = await TestApplication.StartAsync(app =>
        {
            app.MapGet("/typed/value-task/option", () =>
                ValueTask.FromResult(Option.Some(new Payload("value-task-option", 9)))
                    .ToHttpResultsAsync(NotFoundAbsence));
            app.MapGet("/typed/value-task/result", () =>
                ValueTask.FromResult(Result<Payload, DomainError>.Success(new Payload("value-task-result", 10)))
                    .ToHttpResultsAsync(NotFoundFailure));
            app.MapGet("/typed/value-task/result/failure", () =>
                ValueTask.FromResult(Result<Payload, DomainError>.Failure(new DomainError("value-task-missing")))
                    .ToHttpResultsAsync(NotFoundFailure));
            app.MapGet("/typed/value-task/validation", () =>
                ValueTask.FromResult(Validation<Payload, InputError>.Valid(new Payload("value-task-validation", 11)))
                    .ToHttpResultsAsync(InvalidValidation));
            app.MapGet("/typed/value-task/validation/invalid", () =>
                ValueTask.FromResult(Validation<Payload, InputError>.InvalidMany(
                        [new InputError("sku", "A SKU is required.")]))
                    .ToHttpResultsAsync(InvalidValidation));
            app.MapGet("/typed/value-task/unit", () =>
                ValueTask.FromResult(UnitResult<DomainError>.Success()).ToHttpResultsAsync(ConflictFailure));
            app.MapGet("/typed/value-task/unit/failure", () =>
                ValueTask.FromResult(UnitResult<DomainError>.Failure(new DomainError("value-task-conflict")))
                    .ToHttpResultsAsync(ConflictFailure));
        });

        Assert.Equal(HttpStatusCode.OK, await StatusAsync(host, "/typed/value-task/option"));
        Assert.Equal(HttpStatusCode.OK, await StatusAsync(host, "/typed/value-task/result"));
        Assert.Equal(HttpStatusCode.NotFound, await StatusAsync(host, "/typed/value-task/result/failure"));
        Assert.Equal(HttpStatusCode.OK, await StatusAsync(host, "/typed/value-task/validation"));
        Assert.Equal(HttpStatusCode.BadRequest, await StatusAsync(host, "/typed/value-task/validation/invalid"));
        Assert.Equal(HttpStatusCode.NoContent, await StatusAsync(host, "/typed/value-task/unit"));
        Assert.Equal(HttpStatusCode.Conflict, await StatusAsync(host, "/typed/value-task/unit/failure"));
    }

    [Fact]
    public async Task TypedAsyncMappingPropagatesCancellationAndFaultsWithoutConvertingThem()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var expected = new OperationCanceledException(cancellation.Token);
        var option = Task.FromException<Option<int>>(expected);
        var result = Task.FromException<Result<int, string>>(expected);
        var validation = Task.FromException<Validation<int, string>>(expected);
        var unit = Task.FromException<UnitResult<string>>(expected);
        Assert.All(new Task[] { option, result, validation, unit }, source => Assert.True(source.IsFaulted));

        var operations = new Task[]
        {
            option.ToHttpResultsAsync(NotFoundAbsence),
            result.ToHttpResultsAsync(NotFoundStringFailure),
            validation.ToHttpResultsAsync(InvalidStringValidation),
            unit.ToHttpResultsAsync(ConflictStringFailure),
            new ValueTask<Option<int>>(option).ToHttpResultsAsync(NotFoundAbsence).AsTask(),
            new ValueTask<Result<int, string>>(result).ToHttpResultsAsync(NotFoundStringFailure).AsTask(),
            new ValueTask<Validation<int, string>>(validation).ToHttpResultsAsync(InvalidStringValidation).AsTask(),
            new ValueTask<UnitResult<string>>(unit).ToHttpResultsAsync(ConflictStringFailure).AsTask(),
        };

        foreach (var operation in operations)
        {
            var actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation);
            Assert.Same(expected, actual);
            Assert.Equal(cancellation.Token, actual.CancellationToken);
            Assert.True(operation.IsCanceled);
            Assert.False(operation.IsFaulted);
        }

        var fault = new InvalidOperationException("independent fault");
        var faulted = Task.FromException<Result<int, string>>(fault)
            .ToHttpResultsAsync(NotFoundStringFailure);
        var observed = await Assert.ThrowsAsync<InvalidOperationException>(() => faulted);
        Assert.Same(fault, observed);
        Assert.True(faulted.IsFaulted);
        Assert.False(faulted.IsCanceled);
    }

    [Fact]
    public void TypedMappingPropagatesMapperExceptionsUnchanged()
    {
        var expected = new InvalidOperationException("mapper fault");

        var exception = Assert.Throws<InvalidOperationException>(() =>
            Result<int, string>.Failure("failed").ToHttpResults<int, string, NotFound<ProblemDetails>>(
                _ => throw expected));

        Assert.Same(expected, exception);
    }

    [Fact]
    public void TypedMappingRejectsNullMappersEagerly()
    {
        static NotFound<ProblemDetails> NotFoundAny<T>(T _) =>
            TypedResults.NotFound(new ProblemDetails { Status = StatusCodes.Status404NotFound });

        static NotFound<ProblemDetails> NotFoundFailure(string _) =>
            TypedResults.NotFound(new ProblemDetails { Status = StatusCodes.Status404NotFound });

        static ValidationProblem InvalidValidation(IReadOnlyList<string> _) =>
            TypedResults.ValidationProblem(new Dictionary<string, string[]>());

        var option = Option.Some(1);
        var result = Result<int, string>.Success(1);
        var validation = Validation<int, string>.Valid(1);
        var unit = UnitResult<string>.Success();
        Task<Result<int, string>>? nullTask = null;
        Func<int, NotFound<ProblemDetails>>? nullSome = null;
        Func<int, NotFound<ProblemDetails>>? nullSuccess = null;
        Func<int, ValidationProblem>? nullValid = null;
        Func<NotFound<ProblemDetails>>? nullUnitSuccess = null;
        Func<string, NotFound<ProblemDetails>>? nullFailure = null;

        Assert.Equal(
            "none",
            Assert.Throws<ArgumentNullException>(() => option.ToHttpResults<int, NotFound<ProblemDetails>>(null!)).ParamName);
        Assert.Equal(
            "some",
            Assert.Throws<ArgumentNullException>(() => option.ToHttpResults(NotFoundAbsence, nullSome!)).ParamName);
        Assert.Equal(
            "failure",
            Assert.Throws<ArgumentNullException>(() => result.ToHttpResults<int, string, NotFound<ProblemDetails>>(null!)).ParamName);
        Assert.Equal(
            "failure",
            Assert.Throws<ArgumentNullException>(() => result.ToHttpResults(nullFailure!, NotFoundAny)).ParamName);
        Assert.Equal(
            "success",
            Assert.Throws<ArgumentNullException>(() => result.ToHttpResults(NotFoundFailure, nullSuccess!)).ParamName);
        Assert.Equal(
            "invalid",
            Assert.Throws<ArgumentNullException>(() => validation.ToHttpResults<int, string, ValidationProblem>(null!)).ParamName);
        Assert.Equal(
            "valid",
            Assert.Throws<ArgumentNullException>(() => validation.ToHttpResults(InvalidValidation, nullValid!)).ParamName);
        Assert.Equal(
            "failure",
            Assert.Throws<ArgumentNullException>(() => unit.ToHttpResults<string, NotFound<ProblemDetails>>(null!)).ParamName);
        Assert.Equal(
            "success",
            Assert.Throws<ArgumentNullException>(() => unit.ToHttpResults(NotFoundFailure, nullUnitSuccess!)).ParamName);
        Assert.Equal(
            "result",
            Assert.Throws<ArgumentNullException>(() =>
            {
                _ = nullTask!.ToHttpResultsAsync<int, string, NotFound<ProblemDetails>>(NotFoundFailure);
            }).ParamName);
        Assert.Equal(
            "none",
            Assert.Throws<ArgumentNullException>(() => ValueTask.FromResult(option).ToHttpResultsAsync<int, NotFound<ProblemDetails>>(null!)).ParamName);
    }

    [Fact]
    public void TypedMappingRejectsNullMapperResults()
    {
        static NotFound<ProblemDetails> NullFailure(string _) => null!;

        var exception = Assert.Throws<InvalidOperationException>(() =>
            Result<int, string>.Failure("failed").ToHttpResults(NullFailure));

        Assert.Equal("The HTTP result mapper returned null.", exception.Message);
    }

    [Fact]
    public async Task TypedMappingsRejectUninitializedCarriers()
    {
        static NotFound<ProblemDetails> Failure(string _) =>
            TypedResults.NotFound(new ProblemDetails { Status = StatusCodes.Status404NotFound });

        static ValidationProblem Invalid(IReadOnlyList<string> _) =>
            TypedResults.ValidationProblem(new Dictionary<string, string[]>());

        var result = default(Result<int, string>);
        var validation = default(Validation<int, string>);
        var unit = default(UnitResult<string>);

        Assert.Throws<InvalidOperationException>(() => result.ToHttpResults(Failure));
        Assert.Throws<InvalidOperationException>(() => validation.ToHttpResults(Invalid));
        Assert.Throws<InvalidOperationException>(() => unit.ToHttpResults(Failure));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Task.FromResult(result).ToHttpResultsAsync(Failure));
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Task.FromResult(validation).ToHttpResultsAsync(Invalid));
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Task.FromResult(unit).ToHttpResultsAsync(Failure));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => ValueTask.FromResult(result).ToHttpResultsAsync(Failure).AsTask());
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => ValueTask.FromResult(validation).ToHttpResultsAsync(Invalid).AsTask());
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => ValueTask.FromResult(unit).ToHttpResultsAsync(Failure).AsTask());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TypedValueTaskMappingsConsumeEachInputOnce(bool pending)
    {
        var option = pending
            ? new CountingValueTaskSource<Option<int>>()
            : new CountingValueTaskSource<Option<int>>(Option.Some(1));
        var result = pending
            ? new CountingValueTaskSource<Result<int, string>>()
            : new CountingValueTaskSource<Result<int, string>>(Result<int, string>.Success(1));
        var validation = pending
            ? new CountingValueTaskSource<Validation<int, string>>()
            : new CountingValueTaskSource<Validation<int, string>>(Validation<int, string>.Valid(1));
        var unit = pending
            ? new CountingValueTaskSource<UnitResult<string>>()
            : new CountingValueTaskSource<UnitResult<string>>(UnitResult<string>.Success());

        var operations = new Task[]
        {
            option.CreateValueTask().ToHttpResultsAsync(NotFoundAbsence).AsTask(),
            result.CreateValueTask().ToHttpResultsAsync(NotFoundStringFailure).AsTask(),
            validation.CreateValueTask().ToHttpResultsAsync(InvalidStringValidation).AsTask(),
            unit.CreateValueTask().ToHttpResultsAsync(ConflictStringFailure).AsTask(),
        };

        if (pending)
        {
            option.SetResult(Option.Some(1));
            result.SetResult(Result<int, string>.Success(1));
            validation.SetResult(Validation<int, string>.Valid(1));
            unit.SetResult(UnitResult<string>.Success());
        }

        await Task.WhenAll(operations).WaitAsync(
            TimeSpan.FromSeconds(30),
            TestContext.Current.CancellationToken);
        Assert.Equal(1, option.GetResultCount);
        Assert.Equal(1, result.GetResultCount);
        Assert.Equal(1, validation.GetResultCount);
        Assert.Equal(1, unit.GetResultCount);
    }

    private static async Task<HttpStatusCode> StatusAsync(TestApplication host, string route)
    {
        using var response = await host.Client.GetAsync(route, TestContext.Current.CancellationToken);
        return response.StatusCode;
    }

    private static async Task<JsonDocument> ReadProblemAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

    private static NotFound<ProblemDetails> NotFoundAbsence() =>
        TypedResults.NotFound(new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Title = "Not found",
            Detail = "option-absent",
        });

    private static NotFound<ProblemDetails> NotFoundFailure(DomainError error) =>
        TypedResults.NotFound(new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Title = "Not found",
            Detail = error.Code,
        });

    private static NotFound<ProblemDetails> NotFoundStringFailure(string error) =>
        NotFoundFailure(new DomainError(error));

    private static Conflict<ProblemDetails> ConflictFailure(DomainError error) =>
        TypedResults.Conflict(new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "Conflict",
            Detail = error.Code,
        });

    private static Conflict<ProblemDetails> ConflictStringFailure(string error) =>
        ConflictFailure(new DomainError(error));

    private static ValidationProblem InvalidValidation(IReadOnlyList<InputError> errors) =>
        TypedResults.ValidationProblem(
            Group(errors),
            title: "Order validation failed");

    private static ValidationProblem InvalidStringValidation(IReadOnlyList<string> errors) =>
        TypedResults.ValidationProblem(
            new Dictionary<string, string[]> { ["input"] = errors.ToArray() },
            title: "Input validation failed");

    private static UnprocessableEntity<HttpValidationProblemDetails> UnprocessableValidation(
        IReadOnlyList<InputError> errors) =>
        TypedResults.UnprocessableEntity(new HttpValidationProblemDetails(Group(errors))
        {
            Status = StatusCodes.Status422UnprocessableEntity,
            Title = "Order validation failed",
        });

    private static Dictionary<string, string[]> Group(IReadOnlyList<InputError> errors)
    {
        var grouped = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var error in errors)
        {
            if (!grouped.TryGetValue(error.Field, out var messages))
            {
                messages = [];
                grouped.Add(error.Field, messages);
            }

            messages.Add(error.Message);
        }

        return grouped.ToDictionary(
            static pair => pair.Key,
            static pair => pair.Value.ToArray(),
            StringComparer.Ordinal);
    }

    private sealed record Payload(string Name, int Count);

    private sealed record DomainError(string Code);

    private sealed record InputError(string Field, string Message);

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
}
