using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace FunnySharp.VerticalSlice.Http;

/// <summary>
/// The only catch-all in the application, and it is a boundary: the exception is logged with its
/// identity, a generic 500 problem is written, and no domain outcome is fabricated for it. Domain
/// mapping never sees an exception, and an exception never becomes an <c>OrderError</c>.
/// </summary>
public sealed class UnexpectedExceptionHandler(
    IProblemDetailsService problemDetails,
    ILogger<UnexpectedExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(
            exception,
            "Unhandled exception while serving {Method} {Path}.",
            httpContext.Request.Method,
            httpContext.Request.Path);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred",
                Type = "https://funnysharp.example/problems/unexpected-error",
                Detail = "The request failed before it could produce a domain outcome; the failure was logged.",
            },
        });
    }
}
