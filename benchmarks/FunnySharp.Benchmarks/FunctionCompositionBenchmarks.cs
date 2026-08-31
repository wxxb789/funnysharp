using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using FunnySharp;

namespace FunnySharp.Benchmarks;

[ShortRunJob]
[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class FunctionCompositionBenchmarks
{
    private const int Input = 42;

    private static readonly Func<int, int> First = Increment;
    private static readonly Func<int, int> Second = Double;
    private static readonly Func<int, Task<int>> TaskFirst = IncrementTask;
    private static readonly Func<int, Task<int>> TaskSecond = DoubleTask;
    private static readonly Func<int, ValueTask<int>> ValueTaskFirst = IncrementValueTask;
    private static readonly Func<int, ValueTask<int>> ValueTaskSecond = DoubleValueTask;

    private readonly Func<int, int> directSynchronous = DirectTransform;
    private readonly Func<int, int> composedSynchronous = First.Compose(Second);
    private readonly Func<int, Task<int>> directTask = DirectTaskTransform;
    private readonly Func<int, Task<int>> composedTask = TaskFirst.ComposeAsync(TaskSecond);
    private readonly Func<int, ValueTask<int>> directValueTask = DirectValueTaskTransform;
    private readonly Func<int, ValueTask<int>> composedValueTask = ValueTaskFirst.ComposeAsync(ValueTaskSecond);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Synchronous invocation")]
    public int DirectSynchronousInvocation() => directSynchronous(Input);

    [Benchmark]
    [BenchmarkCategory("Synchronous invocation")]
    public int ComposeSynchronousInvocation() => composedSynchronous(Input);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Delegate construction")]
    public Func<int, int> DirectNoncapturingDelegateConstruction() => new(DirectTransform);

    [Benchmark]
    [BenchmarkCategory("Delegate construction")]
    public Func<int, int> ComposeWrapperConstruction() => First.Compose(Second);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Completed Task invocation")]
    public Task<int> DirectCompletedTaskInvocation() => directTask(Input);

    [Benchmark]
    [BenchmarkCategory("Completed Task invocation")]
    public Task<int> ComposeAsyncCompletedTaskInvocation() => composedTask(Input);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Completed ValueTask invocation")]
    public ValueTask<int> DirectCompletedValueTaskInvocation() => directValueTask(Input);

    [Benchmark]
    [BenchmarkCategory("Completed ValueTask invocation")]
    public ValueTask<int> ComposeAsyncCompletedValueTaskInvocation() => composedValueTask(Input);

    private static int Increment(int value) => value + 1;

    private static int Double(int value) => value * 2;

    private static int DirectTransform(int value) => Double(Increment(value));

    private static async Task<int> DirectTaskTransform(int value)
    {
        var intermediate = await IncrementTask(value).ConfigureAwait(false);
        return await DoubleTask(intermediate).ConfigureAwait(false);
    }

    private static async ValueTask<int> DirectValueTaskTransform(int value)
    {
        var intermediate = await IncrementValueTask(value).ConfigureAwait(false);
        return await DoubleValueTask(intermediate).ConfigureAwait(false);
    }

    private static Task<int> IncrementTask(int value) => Task.FromResult(Increment(value));

    private static Task<int> DoubleTask(int value) => Task.FromResult(Double(value));

    private static ValueTask<int> IncrementValueTask(int value) => ValueTask.FromResult(Increment(value));

    private static ValueTask<int> DoubleValueTask(int value) => ValueTask.FromResult(Double(value));
}
