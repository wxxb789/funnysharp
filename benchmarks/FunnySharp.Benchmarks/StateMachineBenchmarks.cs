using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using FunnySharp;

namespace FunnySharp.Benchmarks;

[ShortRunJob]
[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class StateMachineBenchmarks
{
    private StateTransition<int, int> transition = null!;

    [Params(8, 64, 256)]
    public int Count { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        static StateChange<int, int> Increment(int state) =>
            StateChange<int, int>.To(state + 1, state + 1);

        transition = Increment;
        for (var index = 1; index < Count; index++)
        {
            transition = transition.Then(Increment);
        }
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Left-associated Then chain")]
    public int DirectLoop()
    {
        var outputs = new int[Count];
        var state = 0;
        for (var index = 0; index < outputs.Length; index++)
        {
            state++;
            outputs[index] = state;
        }

        return state + outputs[0] + outputs[^1];
    }

    [Benchmark]
    [BenchmarkCategory("Left-associated Then chain")]
    public int FunnySharpThenChain()
    {
        var change = transition(0);
        return change.State + change.Outputs[0] + change.Outputs[^1];
    }
}
