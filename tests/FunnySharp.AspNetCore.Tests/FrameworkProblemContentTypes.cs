namespace FunnySharp.AspNetCore.Tests;

/// <summary>
/// ASP.NET Core 11 changed the content type of ProblemDetails-typed status results:
/// <c>TypedResults.NotFound(problem)</c> and
/// <c>TypedResults.UnprocessableEntity(httpValidationProblem)</c> write and declare
/// <c>application/problem+json</c>; .NET 10 writes and declares <c>application/json</c>.
/// FunnySharp delegates the content type to the selected framework typed result, so the
/// expectation follows the target lane.
/// </summary>
internal static class FrameworkProblemContentTypes
{
#if NET11_0
    internal const string StatusResultMediaType = "application/problem+json";
#else
    internal const string StatusResultMediaType = "application/json";
#endif

    internal static readonly string[] StatusResultMediaTypes = [StatusResultMediaType];
}
