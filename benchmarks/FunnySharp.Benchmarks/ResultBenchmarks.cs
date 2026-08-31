using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using FunnySharp;

namespace FunnySharp.Benchmarks;

[ShortRunJob]
[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class ResultBenchmarks
{
    private const string Error = "invalid";
    private const string ParseableText = "123456";
    private const string UnparseableText = "not-a-number";

    // Cached delegates isolate Result dispatch from delegate construction.
    private static readonly Func<int, int> IncrementSelector = Increment;
    private static readonly Func<int, bool> PositivePredicate = IsPositive;
    private static readonly Func<int, Result<int, string>> DoubleBinder = Double;
    private static readonly Func<Task<int>> CompletedTaskOperation = CreateCompletedTask;
    private static readonly Func<ValueTask<int>> CompletedValueTaskOperation = CreateCompletedValueTask;
    private static readonly Func<int, Task<int>> TaskIncrementSelector = IncrementTask;
    private static readonly Func<int, ValueTask<int>> ValueTaskIncrementSelector = IncrementValueTask;

    private int successValue;
    private int fallbackValue;
    private bool hasSuccess;
    private bool hasFailure;
    private Result<int, string> success;
    private Result<int, string> failure;

    [GlobalSetup]
    public void Setup()
    {
        successValue = 42;
        fallbackValue = -1;
        hasSuccess = true;
        hasFailure = false;
        success = Result<int, string>.Success(successValue);
        failure = Result<int, string>.Failure(Error);
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Construction and inspection - success")]
    public int DirectSuccessConstructionAndInspection()
    {
        var value = successValue;
        return hasSuccess ? value : fallbackValue;
    }

    [Benchmark]
    [BenchmarkCategory("Construction and inspection - success")]
    public int ResultSuccessConstructionAndInspection() =>
        GetValueOr(Result<int, string>.Success(successValue), fallbackValue);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Construction and inspection - failure")]
    public int DirectFailureConstructionAndInspection()
    {
        var value = default(int);
        return hasFailure ? value : fallbackValue;
    }

    [Benchmark]
    [BenchmarkCategory("Construction and inspection - failure")]
    public int ResultFailureConstructionAndInspection() =>
        GetValueOr(Result<int, string>.Failure(Error), fallbackValue);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Fail-fast pipeline - success")]
    public int DirectSuccessfulPipeline()
    {
        if (!hasSuccess)
        {
            return fallbackValue;
        }

        var incremented = Increment(successValue);
        return IsPositive(incremented) ? incremented * 2 : fallbackValue;
    }

    [Benchmark]
    [BenchmarkCategory("Fail-fast pipeline - success")]
    public int ResultSuccessfulPipeline() =>
        GetValueOr(
            success
                .Map(IncrementSelector)
                .Ensure(PositivePredicate, Error)
                .Bind(DoubleBinder),
            fallbackValue);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Fail-fast pipeline - failure")]
    public int DirectFailedPipeline()
    {
        if (!hasFailure)
        {
            return fallbackValue;
        }

        var incremented = Increment(successValue);
        return IsPositive(incremented) ? incremented * 2 : fallbackValue;
    }

    [Benchmark]
    [BenchmarkCategory("Fail-fast pipeline - failure")]
    public int ResultFailedPipeline() =>
        GetValueOr(
            failure
                .Map(IncrementSelector)
                .Ensure(PositivePredicate, Error)
                .Bind(DoubleBinder),
            fallbackValue);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Exception boundary - success")]
    public int DirectSuccessfulExceptionBoundary()
    {
        try
        {
            return int.Parse(ParseableText);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return fallbackValue;
        }
    }

    [Benchmark]
    [BenchmarkCategory("Exception boundary - success")]
    public int ResultSuccessfulExceptionBoundary() =>
        GetValueOr(Result.Try(ParseSuccess), fallbackValue);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Exception boundary - failure")]
    public int DirectFailedExceptionBoundary()
    {
        try
        {
            return int.Parse(UnparseableText);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return fallbackValue;
        }
    }

    [Benchmark]
    [BenchmarkCategory("Exception boundary - failure")]
    public int ResultFailedExceptionBoundary() =>
        GetValueOr(Result.Try(ParseFailure), fallbackValue);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Completed Task mapping")]
    public async Task<int> DirectCompletedTaskMap() =>
        await CompletedTaskOperation().ConfigureAwait(false) + 1;

    [Benchmark]
    [BenchmarkCategory("Completed Task mapping")]
    public async Task<int> ResultCompletedTaskMap() =>
        GetValueOr(
            await success.MapAsync(TaskIncrementSelector).ConfigureAwait(false),
            fallbackValue);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Completed ValueTask mapping")]
    public async ValueTask<int> DirectCompletedValueTaskMap() =>
        await CompletedValueTaskOperation().ConfigureAwait(false) + 1;

    [Benchmark]
    [BenchmarkCategory("Completed ValueTask mapping")]
    public async ValueTask<int> ResultCompletedValueTaskMap() =>
        GetValueOr(
            await success.MapValueAsync(ValueTaskIncrementSelector).ConfigureAwait(false),
            fallbackValue);

    private static int GetValueOr<TError>(Result<int, TError> result, int fallback) =>
        result.TryGetValue(out var value) ? value : fallback;

    private static int Increment(int value) => value + 1;

    private static bool IsPositive(int value) => value > 0;

    private static Result<int, string> Double(int value) =>
        Result<int, string>.Success(value * 2);

    private static int ParseSuccess() => int.Parse(ParseableText);

    private static int ParseFailure() => int.Parse(UnparseableText);

    private static Task<int> CreateCompletedTask() => Task.FromResult(42);

    private static ValueTask<int> CreateCompletedValueTask() => ValueTask.FromResult(42);

    private static Task<int> IncrementTask(int value) => Task.FromResult(Increment(value));

    private static ValueTask<int> IncrementValueTask(int value) => ValueTask.FromResult(Increment(value));
}
