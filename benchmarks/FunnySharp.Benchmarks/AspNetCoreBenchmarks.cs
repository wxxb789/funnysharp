using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using FunnySharp;
using FunnySharp.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FunnySharp.Benchmarks;

// Equivalence contract: every comparison group maps the same carrier outcome to the same IResult
// shape. The direct baseline is the hand-written Minimal API code an application would write for
// that outcome; the FunnySharp candidate feeds the identical payload/error through the mapping
// helper and lets the caller-owned problem mapper build the same ProblemDetails. Only the
// carrier -> IResult mapping call is measured: no result is executed and no host is started.
[ShortRunJob]
[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class AspNetCoreBenchmarks
{
    private const int Payload = 42;
    private const string Error = "invalid";

    // Cached delegates isolate mapping dispatch from delegate construction.
    private static readonly Func<ProblemDetails> NotFoundFactory = CreateNotFound;
    private static readonly Func<string, ProblemDetails> ConflictFactory = CreateConflict;

    private Result<int, string> success;
    private Result<int, string> failure;
    private Option<int> absent;
    private UnitResult<string> unitSuccess;

    [GlobalSetup]
    public void Setup()
    {
        BenchmarkPreflight.CaptureChild(this);
        success = Result<int, string>.Success(Payload);
        failure = Result<int, string>.Failure(Error);
        absent = Option<int>.None;
        unitSuccess = UnitResult<string>.Success();
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("HTTP mapping overhead - success")]
    public IResult DirectSuccessMapping() => Results.Ok(Payload);

    [Benchmark]
    [BenchmarkCategory("HTTP mapping overhead - success")]
    public IResult FunnySharpSuccessMapping() => success.ToHttpResult(ConflictFactory);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("HTTP mapping overhead - typed failure")]
    public IResult DirectProblemMapping() => Results.Problem(CreateConflict(Error));

    [Benchmark]
    [BenchmarkCategory("HTTP mapping overhead - typed failure")]
    public IResult FunnySharpProblemMapping() => failure.ToHttpResult(ConflictFactory);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("HTTP mapping overhead - absence")]
    public IResult DirectNotFoundMapping() => Results.Problem(statusCode: StatusCodes.Status404NotFound);

    [Benchmark]
    [BenchmarkCategory("HTTP mapping overhead - absence")]
    public IResult FunnySharpNotFoundMapping() => absent.ToHttpResult(NotFoundFactory);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("HTTP mapping overhead - no value")]
    public IResult DirectNoContentMapping() => Results.NoContent();

    [Benchmark]
    [BenchmarkCategory("HTTP mapping overhead - no value")]
    public IResult FunnySharpNoContentMapping() => unitSuccess.ToHttpResult(ConflictFactory);

    private static ProblemDetails CreateNotFound() => new() { Status = StatusCodes.Status404NotFound };

    internal Task<object?> NormalizePreflightAsync(object? value)
    {
        var result = (IResult)value!;
        object? payload = result is Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult problem
            ? problem.ProblemDetails
            : (result as IValueHttpResult)?.Value;
        return Task.FromResult<object?>(new
        {
            status = (result as IStatusCodeHttpResult)?.StatusCode,
            payload,
        });
    }

    private static ProblemDetails CreateConflict(string error) =>
        new()
        {
            Status = StatusCodes.Status409Conflict,
            Title = "Conflict",
            Detail = error,
        };
}
