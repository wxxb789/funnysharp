using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using FunnySharp;

namespace FunnySharp.Benchmarks;

[ShortRunJob]
[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class SequenceBenchmarks
{
    private const string ResultError = "invalid";
    private IEnumerable<Option<int>> options = null!;
    private IEnumerable<Result<int, string>> results = null!;
    private IEnumerable<Validation<int, string>> validations = null!;

    [Params(16, 1024)]
    public int Count { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var validationErrors = new[] { "invalid-name", "invalid-format" };
        var optionItems = new Option<int>[Count];
        var resultItems = new Result<int, string>[Count];
        var validationItems = new Validation<int, string>[Count];

        for (var index = 0; index < Count; index++)
        {
            optionItems[index] = Option.Some(index);
            resultItems[index] = index == 0
                ? Result<int, string>.Failure(ResultError)
                : Result<int, string>.Success(index);
            validationItems[index] = index % 4 == 3
                ? Validation<int, string>.InvalidMany(validationErrors)
                : Validation<int, string>.Valid(index);
        }

        options = optionItems;
        results = resultItems;
        validations = validationItems;
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Option sequence - successful buffering")]
    public IReadOnlyList<int> DirectOptionSequence()
    {
        var values = new List<int>(Count);
        foreach (var option in options)
        {
            option.TryGetValue(out var value);
            values.Add(value);
        }

        return values.AsReadOnly();
    }

    [Benchmark]
    [BenchmarkCategory("Option sequence - successful buffering")]
    public IReadOnlyList<int> FunnySharpOptionSequence()
    {
        var sequence = options.Sequence();
        sequence.TryGetValue(out var values);
        return values!;
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Result sequence - first failure")]
    public string DirectResultSequence()
    {
        foreach (var result in results)
        {
            if (result.TryGetError(out var error))
            {
                return error!;
            }
        }

        return string.Empty;
    }

    [Benchmark]
    [BenchmarkCategory("Result sequence - first failure")]
    public string FunnySharpResultSequence()
    {
        var sequence = results.Sequence();
        sequence.TryGetError(out var error);
        return error!;
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Validation sequence - full accumulation")]
    public IReadOnlyList<string> DirectValidationSequence()
    {
        List<int>? values = null;
        List<string>? errors = null;

        foreach (var validation in validations)
        {
            if (validation.TryGetValue(out var value))
            {
                if (errors is null)
                {
                    (values ??= new List<int>(Count)).Add(value);
                }

                continue;
            }

            values = null;
            validation.TryGetErrors(out var validationErrors);
            var currentErrors = validationErrors!;
            errors ??= new List<string>(currentErrors.Count);
            for (var index = 0; index < currentErrors.Count; index++)
            {
                errors.Add(currentErrors[index]);
            }
        }

        return errors is null ? Array.Empty<string>() : errors.AsReadOnly();
    }

    [Benchmark]
    [BenchmarkCategory("Validation sequence - full accumulation")]
    public IReadOnlyList<string> FunnySharpValidationSequence()
    {
        var sequence = validations.Sequence();
        sequence.TryGetErrors(out var errors);
        return errors!;
    }
}
