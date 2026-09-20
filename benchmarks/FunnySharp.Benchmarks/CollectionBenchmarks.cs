#pragma warning disable FS0017

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using FunnySharp;

namespace FunnySharp.Benchmarks;

[ShortRunJob]
[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class CollectionBenchmarks
{
    private const string Target = "target";
    private const string Rows = "rows";
    private const string FieldName = "value";
    private const string ParseableText = "123456";
    private const int NegativeValue = -1;

    private int[] values = null!;
    private int[] otherValues = null!;
    private string?[] texts = null!;
    private string?[] nullableTexts = null!;
    private int[] queueSource = null!;
    private int[] rows = null!;

    [Params(16, 1024)]
    public int Count { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        values = Enumerable.Range(0, Count).ToArray();

        // The unique negative at the end keeps the single-or-none and min-or-none scans full-length.
        values[Count - 1] = NegativeValue;
        otherValues = Enumerable.Range(1, Count).ToArray();
        queueSource = Enumerable.Range(0, Count).ToArray();
        rows = Enumerable.Range(0, Count).ToArray();

        var items = new string?[Count];
        for (var index = 0; index < Count; index++)
        {
            items[index] = $"item-{index}";
        }

        items[Count / 2] = null;
        items[Count - 1] = Target;
        texts = items;

        var sparse = new string?[Count];
        for (var index = 0; index < Count; index++)
        {
            sparse[index] = index % 8 == 0 ? null : $"text-{index}";
        }

        nullableTexts = sparse;
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("IEnumerable first-or-none")]
    public int LinqFirstOrDefault()
    {
        var match = texts.FirstOrDefault(IsTarget);
        return match is not null ? match.Length : 0;
    }

    [Benchmark]
    [BenchmarkCategory("IEnumerable first-or-none")]
    public int FunnySharpFirstOrNone() =>
        texts.FirstOrNone(IsTarget).Match(static text => text!.Length, static () => 0);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("IEnumerable single-or-none")]
    public int LinqSingleMatchCheck()
    {
        var matches = values.Where(IsNegative).Take(2).ToList();
        return matches.Count == 1 ? matches[0] : 0;
    }

    [Benchmark]
    [BenchmarkCategory("IEnumerable single-or-none")]
    public int FunnySharpSingleOrNone() => values.SingleOrNone(IsNegative).GetValueOr(0);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("IEnumerable min-or-none")]
    public int LinqNullableMin() => values.Cast<int?>().Min() ?? 0;

    [Benchmark]
    [BenchmarkCategory("IEnumerable min-or-none")]
    public int FunnySharpMinOrNone() => values.MinOrNone().GetValueOr(0);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("IEnumerable partition")]
    public int LinqTwoWherePasses()
    {
        var evens = values.Where(IsEven).ToList();
        var odds = values.Where(IsOdd).ToList();
        return evens.Count + odds.Count;
    }

    [Benchmark]
    [BenchmarkCategory("IEnumerable partition")]
    public int FunnySharpPartition()
    {
        var (evens, odds) = values.Partition(IsEven);
        return evens.Count + odds.Count;
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("IEnumerable non-empty total")]
    public int LinqAnyThenAggregate()
    {
        if (!values.Any())
        {
            return 0;
        }

        return values.Aggregate(Add);
    }

    [Benchmark]
    [BenchmarkCategory("IEnumerable non-empty total")]
    public int FunnySharpNonEmptyAggregate() =>
        values.ToNonEmptyOrNone().Match(static nonEmpty => nonEmpty.Aggregate(Add), static () => 0);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("IEnumerable exact zip")]
    public int LinqZipWithCountCheck()
    {
        if (values.Count() != otherValues.Count())
        {
            return 0;
        }

        return values.Zip(otherValues, Add).Sum();
    }

    [Benchmark]
    [BenchmarkCategory("IEnumerable exact zip")]
    public int FunnySharpZipExact() =>
        values.ZipExactOrNone(otherValues)
            .Match(static pairs => pairs.Sum(static pair => pair.First + pair.Second), static () => 0);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Queue dequeue drain")]
    public int BclQueueDrain()
    {
        var queue = new Queue<int>(queueSource);
        var total = 0;

        while (queue.TryDequeue(out var item))
        {
            total += item;
        }

        return total;
    }

    [Benchmark]
    [BenchmarkCategory("Queue dequeue drain")]
    public int FunnySharpQueueDrain()
    {
        var queue = new Queue<int>(queueSource);
        var total = 0;

        while (queue.DequeueOrNone().TryGetValue(out var item))
        {
            total += item;
        }

        return total;
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Parse int")]
    public int BclTryParse() => int.TryParse(ParseableText, out var value) ? value : 0;

    [Benchmark]
    [BenchmarkCategory("Parse int")]
    public int FunnySharpParseIntOrNone() => ParseableText.ParseIntOrNone().GetValueOr(0);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("IEnumerable where-not-null")]
    public int LinqWhereNotNull() => nullableTexts.Where(IsNotNull).ToList().Count;

    [Benchmark]
    [BenchmarkCategory("IEnumerable where-not-null")]
    public int FunnySharpWhereNotNull() => nullableTexts.WhereNotNull().ToList().Count;

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Located traverse validation")]
    public IReadOnlyList<string> HandWrittenLocationPaths()
    {
        var errors = new List<string>();

        for (var index = 0; index < rows.Length; index++)
        {
            if (IsInvalid(rows[index]))
            {
                errors.Add($"{Rows}[{index}].{FieldName}: invalid");
            }
        }

        return errors;
    }

    [Benchmark]
    [BenchmarkCategory("Located traverse validation")]
    public IReadOnlyList<string> FunnySharpLocatedTraverse()
    {
        var validation = rows.Traverse(
            Location.Root.Property(Rows),
            static (location, row) => IsInvalid(row)
                ? Validation<int, string>.Invalid(location.Property(FieldName).ToString() + ": invalid")
                : Validation<int, string>.Valid(row));
        validation.TryGetErrors(out var errors);
        return errors!;
    }

    private static bool IsTarget(string? text) => text == Target;

    private static bool IsNegative(int value) => value < 0;

    private static bool IsEven(int value) => value % 2 == 0;

    private static bool IsOdd(int value) => value % 2 != 0;

    private static bool IsNotNull(string? text) => text is not null;

    private static bool IsInvalid(int row) => row % 4 == 3;

    private static int Add(int left, int right) => left + right;
}
