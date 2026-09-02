using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using FunnySharp;

namespace FunnySharp.Benchmarks;

[ShortRunJob]
[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class ConcurrencyBenchmarks
{
    private const int MaxConcurrency = 4;

    private IAsyncEnumerable<int> source = null!;
    private Func<int, CancellationToken, ValueTask<int>> selector = null!;
    private ParallelOptions parallelOptions = null!;

    [Params(16, 1024)]
    public int Count { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        source = Enumerable.Range(0, Count).ToAsyncEnumerable();
        selector = MapAsync;
        parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = MaxConcurrency };
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Ordered bounded asynchronous map")]
    public async Task<int[]> BclParallelForEachAsync()
    {
        var results = new int[Count];

        await Parallel.ForEachAsync(source, parallelOptions, async (value, cancellationToken) =>
        {
            results[value] = await selector(value, cancellationToken).ConfigureAwait(false);
        }).ConfigureAwait(false);

        return results;
    }

    [Benchmark]
    [BenchmarkCategory("Ordered bounded asynchronous map")]
    public Task<int[]> FunnySharpSelectParallelValueAsync() =>
        source.SelectParallelValueAsync(MaxConcurrency, selector).ToArrayAsync().AsTask();

    private static int Map(int value) => unchecked((value * 31) + 7);

    private static async ValueTask<int> MapAsync(int value, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();
        return Map(value);
    }
}

[ShortRunJob]
[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class ParallelTraverseConcurrencyBenchmarks
{
    private const int MaxConcurrency = 4;
    private const string ValidationError = "invalid";

    private IAsyncEnumerable<int> source = null!;
    private Option<int>[] optionOutcomes = null!;
    private Validation<int, string>[] validationOutcomes = null!;
    private Func<int, CancellationToken, ValueTask<Option<int>>> optionSelector = null!;
    private Func<int, CancellationToken, ValueTask<Validation<int, string>>> validationSelector = null!;
    private ParallelOptions parallelOptions = null!;

    [Params(16, 1024)]
    public int Count { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        source = Enumerable.Range(0, Count).ToAsyncEnumerable();
        optionOutcomes = Enumerable.Range(0, Count).Select(Option.Some).ToArray();
        validationOutcomes = Enumerable.Range(0, Count)
            .Select(static value => value % 4 == 0
                ? Validation<int, string>.Invalid(ValidationError)
                : Validation<int, string>.Valid(value))
            .ToArray();
        optionSelector = SelectOptionAsync;
        validationSelector = SelectValidationAsync;
        parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = MaxConcurrency };
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Parallel Option traversal")]
    public async Task<Option<IReadOnlyList<int>>> BclParallelOptionTraversal()
    {
        var outcomes = new Option<int>[Count];
        await Parallel.ForEachAsync(source, parallelOptions, async (value, cancellationToken) =>
        {
            outcomes[value] = await optionSelector(value, cancellationToken).ConfigureAwait(false);
        }).ConfigureAwait(false);
        return SequenceOptions(outcomes);
    }

    [Benchmark]
    [BenchmarkCategory("Parallel Option traversal")]
    public Task<Option<IReadOnlyList<int>>> FunnySharpParallelOptionTraversal() =>
        source.TraverseParallelValueAsync(MaxConcurrency, optionSelector).AsTask();

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Parallel Validation accumulation")]
    public async Task<Validation<IReadOnlyList<int>, string>> BclParallelValidationTraversal()
    {
        var outcomes = new Validation<int, string>[Count];
        await Parallel.ForEachAsync(source, parallelOptions, async (value, cancellationToken) =>
        {
            outcomes[value] = await validationSelector(value, cancellationToken).ConfigureAwait(false);
        }).ConfigureAwait(false);
        return SequenceValidations(outcomes);
    }

    [Benchmark]
    [BenchmarkCategory("Parallel Validation accumulation")]
    public Task<Validation<IReadOnlyList<int>, string>> FunnySharpParallelValidationTraversal() =>
        source.TraverseParallelValueAsync(MaxConcurrency, validationSelector).AsTask();

    private async ValueTask<Option<int>> SelectOptionAsync(int value, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.Yield();
        return optionOutcomes[value];
    }

    private async ValueTask<Validation<int, string>> SelectValidationAsync(
        int value,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.Yield();
        return validationOutcomes[value];
    }

    private static Option<IReadOnlyList<int>> SequenceOptions(IEnumerable<Option<int>> outcomes)
    {
        var values = new List<int>();
        foreach (var outcome in outcomes)
        {
            if (!outcome.TryGetValue(out var value))
            {
                return Option.None<IReadOnlyList<int>>();
            }

            values.Add(value);
        }

        return Option.Some<IReadOnlyList<int>>(values.AsReadOnly());
    }

    private static Validation<IReadOnlyList<int>, string> SequenceValidations(
        IEnumerable<Validation<int, string>> outcomes)
    {
        List<int>? values = null;
        List<string>? errors = null;
        foreach (var outcome in outcomes)
        {
            if (outcome.TryGetValue(out var value))
            {
                if (errors is null)
                {
                    (values ??= []).Add(value);
                }

                continue;
            }

            values = null;
            if (!outcome.TryGetErrors(out var currentErrors))
            {
                throw new InvalidOperationException("Expected an invalid validation.");
            }

            errors ??= new List<string>(currentErrors.Count);
            errors.AddRange(currentErrors);
        }

        if (errors is not null)
        {
            return Validation<IReadOnlyList<int>, string>.InvalidMany(errors);
        }

        IReadOnlyList<int> readOnlyValues = values is null ? Array.Empty<int>() : values.AsReadOnly();
        return Validation<IReadOnlyList<int>, string>.Valid(readOnlyValues);
    }
}

[ShortRunJob]
[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class FirstSuccessConcurrencyBenchmarks
{
    private const string Error = "failed";

    private Func<CancellationToken, ValueTask<Result<int, string>>>[] operations = null!;
    private Effect<Result<int, string>>[] effects = null!;

    [Params(4, 16)]
    public int CandidateCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        operations = new Func<CancellationToken, ValueTask<Result<int, string>>>[CandidateCount];
        var successIndex = CandidateCount / 2;

        for (var index = 0; index < operations.Length; index++)
        {
            var candidate = index;
            operations[index] = candidate == successIndex
                ? static cancellationToken => CompleteAsync(Result<int, string>.Success(42), cancellationToken)
                : static cancellationToken => CompleteAsync(Result<int, string>.Failure(Error), cancellationToken);
        }

        effects = operations.Select(static operation => Effect.FromValueTask(operation)).ToArray();
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("First successful cold Result operation")]
    public Task<int> BclTaskWhenAnyCancelAndDrain() =>
        FirstSuccessWithBclAsync(operations, CancellationToken.None).AsTask();

    [Benchmark]
    [BenchmarkCategory("First successful cold Result operation")]
    public async Task<int> FunnySharpFirstSuccessAsync()
    {
        var result = await effects.FirstSuccessAsync().ConfigureAwait(false);
        result.TryGetValue(out var value);
        return value;
    }

    private static async ValueTask<int> FirstSuccessWithBclAsync(
        IReadOnlyList<Func<CancellationToken, ValueTask<Result<int, string>>>> source,
        CancellationToken cancellationToken)
    {
        using var operationCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var tasks = new Task<Result<int, string>>[source.Count];

        for (var index = 0; index < source.Count; index++)
        {
            tasks[index] = source[index](operationCancellation.Token).AsTask();
        }

        var pending = new List<Task<Result<int, string>>>(tasks);
        while (pending.Count > 0)
        {
            var completed = await Task.WhenAny(pending).ConfigureAwait(false);
            pending.Remove(completed);
            var result = await completed.ConfigureAwait(false);
            if (!result.TryGetValue(out var value))
            {
                continue;
            }

            operationCancellation.Cancel();
            try
            {
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            catch (Exception)
            {
            }

            return value;
        }

        throw new InvalidOperationException("No successful result was produced.");
    }

    private static async ValueTask<Result<int, string>> CompleteAsync(
        Result<int, string> result,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.Yield();
        return result;
    }
}
