using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using FunnySharp;

namespace FunnySharp.Benchmarks;

[ShortRunJob]
[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class DataPipelineBenchmarks
{
    private IEnumerable<int> values = null!;
    private IAsyncEnumerable<int> asyncValues = null!;
    private int[] spanValues = null!;
    private int[] spanDestination = null!;

    [Params(16, 1024)]
    public int Count { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        values = Enumerable.Range(0, Count);
        asyncValues = values.ToAsyncEnumerable();
        spanValues = Enumerable.Range(0, Count).ToArray();
        spanDestination = new int[Count];
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("IEnumerable filter-map")]
    public int LinqWhereSelect() => values.Where(IsEven).Select(Double).Sum();

    [Benchmark]
    [BenchmarkCategory("IEnumerable filter-map")]
    public int FunnySharpChoose() => values.Choose(ChooseEven).Sum();

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Span filter-map")]
    public int DirectSpanLoop()
    {
        ReadOnlySpan<int> source = spanValues;
        Span<int> destination = spanDestination;
        var written = 0;

        for (var index = 0; index < source.Length; index++)
        {
            var value = source[index];
            if (IsEven(value))
            {
                destination[written++] = Double(value);
            }
        }

        return Sum(destination[..written]);
    }

    [Benchmark]
    [BenchmarkCategory("Span filter-map")]
    public int FunnySharpSpanChoose()
    {
        var chosen = spanValues.AsSpan().ChooseTo(spanDestination.AsSpan(), ChooseEven);
        return Sum(chosen);
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Async stream filter-map")]
    public ValueTask<int> DirectAsyncStream() => SumEvenAsync(asyncValues);

    [Benchmark]
    [BenchmarkCategory("Async stream filter-map")]
    public ValueTask<int> BclAsyncWhereSelect() => SumAsync(asyncValues.Where(IsEven).Select(Double));

    [Benchmark]
    [BenchmarkCategory("Async stream filter-map")]
    public ValueTask<int> FunnySharpAsyncChoose() => SumAsync(asyncValues.Choose(ChooseEven));

    private static bool IsEven(int value) => value % 2 == 0;

    private static int Double(int value) => value * 2;

    private static Option<int> ChooseEven(int value) =>
        IsEven(value) ? Option.Some(Double(value)) : Option.None<int>();

    private static int Sum(ReadOnlySpan<int> values)
    {
        var sum = 0;
        for (var index = 0; index < values.Length; index++)
        {
            sum += values[index];
        }

        return sum;
    }

    private static async ValueTask<int> SumEvenAsync(IAsyncEnumerable<int> source)
    {
        var sum = 0;
        await foreach (var value in source.ConfigureAwait(false))
        {
            if (IsEven(value))
            {
                sum += Double(value);
            }
        }

        return sum;
    }

    private static async ValueTask<int> SumAsync(IAsyncEnumerable<int> source)
    {
        var sum = 0;
        await foreach (var value in source.ConfigureAwait(false))
        {
            sum += value;
        }

        return sum;
    }
}
