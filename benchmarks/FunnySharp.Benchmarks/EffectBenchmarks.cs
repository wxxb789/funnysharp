using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using FunnySharp;

namespace FunnySharp.Benchmarks;

[ShortRunJob]
[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class EffectBenchmarks
{
    private const string Error = "invalid";
    private const int Input = 42;

    private static readonly BenchmarkEnvironment Environment = new(3);
    private static readonly Result<int, string> CompletedValue = Result<int, string>.Success(Input);
    private static readonly Func<BenchmarkEnvironment, Result<int, string>> SynchronousOperation = CreateResult;
    private static readonly Func<BenchmarkEnvironment, Task<Result<int, string>>> TaskOperation = CreateTaskResult;
    private static readonly Func<BenchmarkEnvironment, ValueTask<Result<int, string>>> ValueTaskOperation = CreateValueTaskResult;
    private static readonly Effect<BenchmarkEnvironment, Result<int, string>> SynchronousEffect =
        Effect.FromSync(SynchronousOperation);
    private static readonly Effect<BenchmarkEnvironment, Result<int, string>> TaskEffect =
        Effect.FromTask(TaskOperation);
    private static readonly Effect<BenchmarkEnvironment, Result<int, string>> ValueTaskEffect =
        Effect.FromValueTask(ValueTaskOperation);
    private static readonly Effect<Result<int, string>> CompletedValueEffect = Effect.FromValue(CompletedValue);
    private static readonly Effect<BenchmarkEnvironment, Result<int, string>> MappedSynchronousEffect =
        SynchronousEffect.Map(MapResult);
    private static readonly Effect<BenchmarkEnvironment, Result<int, string>> MappedTaskEffect =
        TaskEffect.Map(MapResult);
    private static readonly Effect<BenchmarkEnvironment, Result<int, string>> MappedValueTaskEffect =
        ValueTaskEffect.Map(MapResult);
    private static readonly Effect<BenchmarkEnvironment, Result<int, string>> BoundValueTaskEffect =
        ValueTaskEffect.Bind(BindValueTaskEffect);
    private static readonly Effect<Result<int, string>> ProvidedSynchronousEffect =
        SynchronousEffect.Provide(Environment);
    private static readonly Effect<Result<int, string>> SynchronousScopedEffect =
        Effect.FromValue(default(SynchronousResource)).Using(static _ => CompletedValueEffect);
    private static readonly Effect<Result<int, string>> AsynchronousScopedEffect =
        Effect.FromValue(default(AsynchronousResource)).UsingAsync(static _ => CompletedValueEffect);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Wrapper construction")]
    public Func<BenchmarkEnvironment, Result<int, string>> DirectSynchronousWrapperConstruction() =>
        SynchronousOperation;

    [Benchmark]
    [BenchmarkCategory("Wrapper construction")]
    public Effect<BenchmarkEnvironment, Result<int, string>> EffectSynchronousWrapperConstruction() =>
        Effect.FromSync(SynchronousOperation);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Completed value RunAsync")]
    public ValueTask<Result<int, string>> DirectCompletedValueRunAsync() =>
        ValueTask.FromResult(CompletedValue);

    [Benchmark]
    [BenchmarkCategory("Completed value RunAsync")]
    public ValueTask<Result<int, string>> EffectCompletedValueRunAsync() =>
        CompletedValueEffect.RunAsync();

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Completed synchronous RunAsync")]
    public ValueTask<Result<int, string>> DirectCompletedSynchronousRunAsync() =>
        ValueTask.FromResult(SynchronousOperation(Environment));

    [Benchmark]
    [BenchmarkCategory("Completed synchronous RunAsync")]
    public ValueTask<Result<int, string>> EffectCompletedSynchronousRunAsync() =>
        SynchronousEffect.RunAsync(Environment);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Map composition")]
    public ValueTask<Result<int, string>> DirectMapComposition() =>
        ValueTask.FromResult(MapResult(SynchronousOperation(Environment)));

    [Benchmark]
    [BenchmarkCategory("Map composition")]
    public ValueTask<Result<int, string>> EffectMapComposition() =>
        MappedSynchronousEffect.RunAsync(Environment);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Bind ValueTask composition")]
    public async ValueTask<Result<int, string>> DirectBindValueTaskComposition()
    {
        var result = await ValueTaskOperation(Environment).ConfigureAwait(false);
        return await BindValueTaskResult(result, Environment).ConfigureAwait(false);
    }

    [Benchmark]
    [BenchmarkCategory("Bind ValueTask composition")]
    public ValueTask<Result<int, string>> EffectBindValueTaskComposition() =>
        BoundValueTaskEffect.RunAsync(Environment);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Completed Task composition")]
    public async ValueTask<Result<int, string>> DirectCompletedTaskComposition()
    {
        var result = await TaskOperation(Environment).ConfigureAwait(false);
        return MapResult(result);
    }

    [Benchmark]
    [BenchmarkCategory("Completed Task composition")]
    public ValueTask<Result<int, string>> EffectCompletedTaskComposition() =>
        MappedTaskEffect.RunAsync(Environment);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Completed ValueTask map composition")]
    public async ValueTask<Result<int, string>> DirectCompletedValueTaskComposition()
    {
        var result = await ValueTaskOperation(Environment).ConfigureAwait(false);
        return MapResult(result);
    }

    [Benchmark]
    [BenchmarkCategory("Completed ValueTask map composition")]
    public ValueTask<Result<int, string>> EffectCompletedValueTaskComposition() =>
        MappedValueTaskEffect.RunAsync(Environment);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Environment Provide")]
    public ValueTask<Result<int, string>> DirectEnvironmentProvide() =>
        ValueTask.FromResult(SynchronousOperation(Environment));

    [Benchmark]
    [BenchmarkCategory("Environment Provide")]
    public ValueTask<Result<int, string>> EffectEnvironmentProvide() =>
        ProvidedSynchronousEffect.RunAsync();

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Using")]
    public ValueTask<Result<int, string>> DirectUsing()
    {
        using var resource = default(SynchronousResource);
        return ValueTask.FromResult(CompletedValue);
    }

    [Benchmark]
    [BenchmarkCategory("Using")]
    public ValueTask<Result<int, string>> EffectUsing() =>
        SynchronousScopedEffect.RunAsync();

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("UsingAsync")]
    public ValueTask<Result<int, string>> DirectUsingAsync() => DirectUsingAsyncCore();

    [Benchmark]
    [BenchmarkCategory("UsingAsync")]
    public ValueTask<Result<int, string>> EffectUsingAsync() =>
        AsynchronousScopedEffect.RunAsync();

    private static Result<int, string> CreateResult(BenchmarkEnvironment environment) =>
        environment.Offset >= 0
            ? Result<int, string>.Success(Input + environment.Offset)
            : Result<int, string>.Failure(Error);

    private static Task<Result<int, string>> CreateTaskResult(BenchmarkEnvironment environment) =>
        Task.FromResult(CreateResult(environment));

    private static ValueTask<Result<int, string>> CreateValueTaskResult(BenchmarkEnvironment environment) =>
        ValueTask.FromResult(CreateResult(environment));

    private static Result<int, string> MapResult(Result<int, string> result) =>
        result.Map(static value => value + 1);

    private static Result<int, string> BindResult(Result<int, string> result, BenchmarkEnvironment environment) =>
        result.Bind(value =>
            environment.Offset >= 0
                ? Result<int, string>.Success(value * 2)
                : Result<int, string>.Failure(Error));

    private static ValueTask<Result<int, string>> BindValueTaskResult(
        Result<int, string> result,
        BenchmarkEnvironment environment) =>
        ValueTask.FromResult(BindResult(result, environment));

    private static Effect<BenchmarkEnvironment, Result<int, string>> BindValueTaskEffect(
        Result<int, string> result) =>
        Effect.FromValueTask((BenchmarkEnvironment environment) => BindValueTaskResult(result, environment));

    private static async ValueTask<Result<int, string>> DirectUsingAsyncCore()
    {
        await using var resource = default(AsynchronousResource);
        return CompletedValue;
    }

    public sealed record BenchmarkEnvironment(int Offset);

    private readonly struct SynchronousResource : IDisposable
    {
        public void Dispose()
        {
        }
    }

    private readonly struct AsynchronousResource : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
