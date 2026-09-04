using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using FunnySharp;

namespace FunnySharp.Benchmarks;

[ShortRunJob]
[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class OptionBenchmarks
{
    private const string ParseableText = "123456";
    private const string UnparseableText = "not-a-number";
    private const string ExistingKey = "existing";
    private const string MissingKey = "missing";

    // Cached adapters isolate bridge dispatch from delegate construction.
    private static readonly Func<int, int> IncrementSelector = Increment;
    private static readonly TryOperation<int> ParseHitAdapter = TryParseHit;
    private static readonly TryOperation<int> ParseMissAdapter = TryParseMiss;
    private static readonly Func<int, Task<int>> TaskIncrementSelector = IncrementTask;
    private static readonly Func<int, ValueTask<int>> ValueTaskIncrementSelector = IncrementValueTask;

    private int presentValue;
    private int fallbackValue;
    private bool hasPresentValue;
    private bool hasAbsentValue;
    private Option<int> someInt;
    private Option<int> noneInt;
    private int? presentNullable;
    private int? absentNullable;
    private IReadOnlyDictionary<string, int> values = null!;

    [GlobalSetup]
    public void Setup()
    {
        presentValue = 42;
        fallbackValue = -1;
        hasPresentValue = true;
        hasAbsentValue = false;
        someInt = Option.Some(presentValue);
        noneInt = Option.None<int>();
        presentNullable = presentValue;
        absentNullable = null;
        values = new Dictionary<string, int>
        {
            [ExistingKey] = presentValue,
        };
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Map - Some")]
    public int DirectMapSome() => hasPresentValue ? Increment(presentValue) : fallbackValue;

    [Benchmark]
    [BenchmarkCategory("Map - Some")]
    public int OptionMapSome() =>
        someInt.Map(IncrementSelector).GetValueOr(fallbackValue);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Map - None")]
    public int DirectMapNone() => hasAbsentValue ? Increment(presentValue) : fallbackValue;

    [Benchmark]
    [BenchmarkCategory("Map - None")]
    public int OptionMapNone() =>
        noneInt.Map(IncrementSelector).GetValueOr(fallbackValue);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("GetValueOr - Some")]
    public int DirectGetValueOrSome() => hasPresentValue ? presentValue : fallbackValue;

    [Benchmark]
    [BenchmarkCategory("GetValueOr - Some")]
    public int OptionGetValueOrSome() => someInt.GetValueOr(fallbackValue);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("GetValueOr - None")]
    public int DirectGetValueOrNone() => hasAbsentValue ? presentValue : fallbackValue;

    [Benchmark]
    [BenchmarkCategory("GetValueOr - None")]
    public int OptionGetValueOrNone() => noneInt.GetValueOr(fallbackValue);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Nullable conversion - Some")]
    public int DirectNullableSome() =>
        presentNullable.HasValue ? presentNullable.GetValueOrDefault() : fallbackValue;

    [Benchmark]
    [BenchmarkCategory("Nullable conversion - Some")]
    public int OptionNullableSome() => presentNullable.ToOption().GetValueOr(fallbackValue);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Nullable conversion - None")]
    public int DirectNullableNone() =>
        absentNullable.HasValue ? absentNullable.GetValueOrDefault() : fallbackValue;

    [Benchmark]
    [BenchmarkCategory("Nullable conversion - None")]
    public int OptionNullableNone() => absentNullable.ToOption().GetValueOr(fallbackValue);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Try pattern - hit")]
    public int DirectTryParseHit() =>
        int.TryParse(ParseableText, out var value) ? value : fallbackValue;

    [Benchmark]
    [BenchmarkCategory("Try pattern - hit")]
    public int OptionTryParseHit() => Option.FromTry(ParseHitAdapter).GetValueOr(fallbackValue);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Try pattern - miss")]
    public int DirectTryParseMiss() =>
        int.TryParse(UnparseableText, out var value) ? value : fallbackValue;

    [Benchmark]
    [BenchmarkCategory("Try pattern - miss")]
    public int OptionTryParseMiss() => Option.FromTry(ParseMissAdapter).GetValueOr(fallbackValue);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Dictionary lookup - hit")]
    public int DirectDictionaryHit() =>
        values.TryGetValue(ExistingKey, out var value) ? value : fallbackValue;

    [Benchmark]
    [BenchmarkCategory("Dictionary lookup - hit")]
    public int OptionDictionaryHit() => values.GetOption(ExistingKey).GetValueOr(fallbackValue);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Dictionary lookup - miss")]
    public int DirectDictionaryMiss() =>
        values.TryGetValue(MissingKey, out var value) ? value : fallbackValue;

    [Benchmark]
    [BenchmarkCategory("Dictionary lookup - miss")]
    public int OptionDictionaryMiss() => values.GetOption(MissingKey).GetValueOr(fallbackValue);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Completed Task mapping")]
    public async Task<int> DirectCompletedTaskMap() =>
        await IncrementTask(presentValue).ConfigureAwait(false);

    [Benchmark]
    [BenchmarkCategory("Completed Task mapping")]
    public async Task<int> OptionCompletedTaskMap() =>
        (await someInt.MapAsync(TaskIncrementSelector).ConfigureAwait(false)).GetValueOr(fallbackValue);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Completed ValueTask mapping")]
    public async ValueTask<int> DirectCompletedValueTaskMap() =>
        await IncrementValueTask(presentValue).ConfigureAwait(false);

    [Benchmark]
    [BenchmarkCategory("Completed ValueTask mapping")]
    public async ValueTask<int> OptionCompletedValueTaskMap() =>
        (await someInt.MapValueAsync(ValueTaskIncrementSelector).ConfigureAwait(false)).GetValueOr(fallbackValue);

    private static int Increment(int value) => value + 1;

    private static bool TryParseHit(out int value) => int.TryParse(ParseableText, out value);

    private static bool TryParseMiss(out int value) => int.TryParse(UnparseableText, out value);

    private static Task<int> IncrementTask(int value) => Task.FromResult(Increment(value));

    // Creates a new completed ValueTask per invocation; none is reused across benchmark iterations.
    private static ValueTask<int> IncrementValueTask(int value) => ValueTask.FromResult(Increment(value));

}
