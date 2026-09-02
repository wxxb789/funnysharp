using FunnySharp;
using FunnySharp.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateSlimBuilder();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, CompatibilityJsonContext.Default));
await using var app = builder.Build();

await AssertStatusAsync(
    Option.Some("option").ToHttpResult(NotFound, static _ => Results.StatusCode(StatusCodes.Status204NoContent)),
    StatusCodes.Status204NoContent,
    app.Services);

await AssertStatusAsync(
    Result<int, string>.Failure("denied").ToHttpResult(Forbidden),
    StatusCodes.Status403Forbidden,
    app.Services);

await AssertStatusAsync(
    Validation<int, string>.InvalidMany(["name", "email"]).ToHttpResult(Invalid),
    StatusCodes.Status422UnprocessableEntity,
    app.Services);

var effect = Effect.FromSync(() => Option.Some("effect"));
await AssertStatusAsync(
    await effect.ToHttpResultAsync(
        new DefaultHttpContext(),
        NotFound,
        static _ => Results.StatusCode(StatusCodes.Status202Accepted)),
    StatusCodes.Status202Accepted,
    app.Services);

Console.WriteLine("FunnySharp ASP.NET Core compatibility smoke passed.");

static ProblemDetails NotFound() => new()
{
    Status = StatusCodes.Status404NotFound,
    Title = "Not found",
};

static ProblemDetails Forbidden(string error) => new()
{
    Status = StatusCodes.Status403Forbidden,
    Title = "Forbidden",
    Detail = error,
};

static HttpValidationProblemDetails Invalid(IReadOnlyList<string> errors) => new(
    new Dictionary<string, string[]>
    {
        ["input"] = errors.ToArray(),
    })
{
    Status = StatusCodes.Status422UnprocessableEntity,
    Title = "Invalid input",
};

static async Task AssertStatusAsync(IResult result, int expectedStatus, IServiceProvider services)
{
    var context = new DefaultHttpContext();
    using var responseBody = new MemoryStream();
    context.Response.Body = responseBody;
    context.RequestServices = services;

    await result.ExecuteAsync(context);

    if (context.Response.StatusCode != expectedStatus)
    {
        throw new InvalidOperationException(
            $"Expected HTTP status {expectedStatus}, but received {context.Response.StatusCode}.");
    }
}

[JsonSerializable(typeof(ProblemDetails))]
[JsonSerializable(typeof(HttpValidationProblemDetails))]
internal partial class CompatibilityJsonContext : JsonSerializerContext;
