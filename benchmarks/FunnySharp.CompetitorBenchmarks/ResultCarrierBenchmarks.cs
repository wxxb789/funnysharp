using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using CSharpFunctionalExtensions;
using CfeResult = CSharpFunctionalExtensions.Result;
using CfeResultOfIntString = CSharpFunctionalExtensions.Result<int, string>;
using FunnySharp;
using Lext = LanguageExt;
using Microsoft.FSharp.Core;

namespace FunnySharp.Benchmarks;

// Equivalence contract for every scenario in this class: each compared path receives the same
// pre-built fail-fast carrier (success or failure) and produces the same final int - the
// transformed value through the map/ensure/bind pipeline for a success carrier, or the fallback
// for a failure carrier. FunnySharp and CSharpFunctionalExtensions express the pipeline as
// Map -> Ensure -> Bind. FSharp.Core and language-ext have no separate Ensure member; their
// idiomatic equivalent fuses the ensure check into the bind step, which preserves the exact
// input-to-output semantics of the paired FunnySharp path (same predicate, same error, same
// final value, same fallback). All delegates are cached so no path pays per-operation delegate
// construction.
[ShortRunJob]
[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class ResultCarrierBenchmarks
{
    private const string Error = "invalid";
    private const int FallbackValue = -1;

    private static readonly Func<int, int> IncrementSelector = Increment;
    private static readonly Func<int, bool> PositivePredicate = IsPositive;
    private static readonly Func<int, Result<int, string>> FunnySharpDoubleBinder = FunnySharpDouble;

    private static readonly Converter<int, int> IncrementConverter = Increment;

    private static readonly FSharpFunc<int, int> IncrementFSharpFunc = IncrementConverter;

    private static readonly FSharpFunc<int, FSharpResult<int, string>> FSharpCheckAndDoubleBinder =
        new Converter<int, FSharpResult<int, string>>(CheckAndDoubleFSharp);

    private static readonly Func<int, CfeResultOfIntString> CSharpFunctionalExtensionsDoubleBinder =
        CSharpFunctionalExtensionsDouble;

    private static readonly Func<int, Lext.Either<string, int>> LanguageExtCheckAndDoubleBinder =
        LanguageExtCheckAndDouble;

    private int successValue;
    private bool hasSuccess;
    private bool hasFailure;
    private FunnySharp.Result<int, string> funnySharpSuccess;
    private FunnySharp.Result<int, string> funnySharpFailure;
    private FSharpResult<int, string> fSharpSuccess;
    private FSharpResult<int, string> fSharpFailure;
    private CfeResultOfIntString cSharpFunctionalExtensionsSuccess;
    private CfeResultOfIntString cSharpFunctionalExtensionsFailure;
    private Lext.Either<string, int> languageExtSuccess;
    private Lext.Either<string, int> languageExtFailure;

    [GlobalSetup]
    public void Setup()
    {
        successValue = 42;
        hasSuccess = true;
        hasFailure = false;
        funnySharpSuccess = FunnySharp.Result<int, string>.Success(successValue);
        funnySharpFailure = FunnySharp.Result<int, string>.Failure(Error);
        fSharpSuccess = FSharpResult<int, string>.NewOk(successValue);
        fSharpFailure = FSharpResult<int, string>.NewError(Error);
        cSharpFunctionalExtensionsSuccess = CfeResult.Success<int, string>(successValue);
        cSharpFunctionalExtensionsFailure = CfeResult.Failure<int, string>(Error);
        languageExtSuccess = Lext.Either<string, int>.Right(successValue);
        languageExtFailure = Lext.Either<string, int>.Left(Error);
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Construction and inspection - success")]
    public int DirectConstructionInspectionSuccess() =>
        hasSuccess ? successValue : FallbackValue;

    [Benchmark]
    [BenchmarkCategory("Construction and inspection - success")]
    public int FunnySharpConstructionInspectionSuccess() =>
        GetValueOr(FunnySharp.Result<int, string>.Success(successValue), FallbackValue);

    [Benchmark]
    [BenchmarkCategory("Construction and inspection - success")]
    public int FSharpCoreConstructionInspectionSuccess()
    {
        var result = FSharpResult<int, string>.NewOk(successValue);
        return result.IsOk ? result.ResultValue : FallbackValue;
    }

    [Benchmark]
    [BenchmarkCategory("Construction and inspection - success")]
    public int CSharpFunctionalExtensionsConstructionInspectionSuccess() =>
        CfeResult.Success<int, string>(successValue).GetValueOrDefault(FallbackValue);

    [Benchmark]
    [BenchmarkCategory("Construction and inspection - success")]
    public int LanguageExtConstructionInspectionSuccess() =>
        Lext.Either<string, int>.Right(successValue).IfLeft(FallbackValue);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Construction and inspection - failure")]
    public int DirectConstructionInspectionFailure() =>
        hasFailure ? successValue : FallbackValue;

    [Benchmark]
    [BenchmarkCategory("Construction and inspection - failure")]
    public int FunnySharpConstructionInspectionFailure() =>
        GetValueOr(FunnySharp.Result<int, string>.Failure(Error), FallbackValue);

    [Benchmark]
    [BenchmarkCategory("Construction and inspection - failure")]
    public int FSharpCoreConstructionInspectionFailure()
    {
        var result = FSharpResult<int, string>.NewError(Error);
        return result.IsOk ? result.ResultValue : FallbackValue;
    }

    [Benchmark]
    [BenchmarkCategory("Construction and inspection - failure")]
    public int CSharpFunctionalExtensionsConstructionInspectionFailure() =>
        CfeResult.Failure<int, string>(Error).GetValueOrDefault(FallbackValue);

    [Benchmark]
    [BenchmarkCategory("Construction and inspection - failure")]
    public int LanguageExtConstructionInspectionFailure() =>
        Lext.Either<string, int>.Left(Error).IfLeft(FallbackValue);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Fail-fast pipeline - success")]
    public int DirectFailFastPipelineSuccess()
    {
        if (!hasSuccess)
        {
            return FallbackValue;
        }

        var incremented = Increment(successValue);
        return IsPositive(incremented) ? incremented * 2 : FallbackValue;
    }

    [Benchmark]
    [BenchmarkCategory("Fail-fast pipeline - success")]
    public int FunnySharpFailFastPipelineSuccess() =>
        GetValueOr(
            funnySharpSuccess
                .Map(IncrementSelector)
                .Ensure(PositivePredicate, Error)
                .Bind(FunnySharpDoubleBinder),
            FallbackValue);

    [Benchmark]
    [BenchmarkCategory("Fail-fast pipeline - success")]
    public int FSharpCoreFailFastPipelineSuccess() =>
        ResultModule.DefaultValue(
            FallbackValue,
            ResultModule.Bind(
                FSharpCheckAndDoubleBinder,
                ResultModule.Map(IncrementFSharpFunc, fSharpSuccess)));

    [Benchmark]
    [BenchmarkCategory("Fail-fast pipeline - success")]
    public int CSharpFunctionalExtensionsFailFastPipelineSuccess() =>
        cSharpFunctionalExtensionsSuccess
            .Map(IncrementSelector)
            .Ensure(PositivePredicate, Error)
            .Bind(CSharpFunctionalExtensionsDoubleBinder)
            .GetValueOrDefault(FallbackValue);

    [Benchmark]
    [BenchmarkCategory("Fail-fast pipeline - success")]
    public int LanguageExtFailFastPipelineSuccess() =>
        languageExtSuccess
            .Map(IncrementSelector)
            .Bind(LanguageExtCheckAndDoubleBinder)
            .IfLeft(FallbackValue);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Fail-fast pipeline - failure")]
    public int DirectFailFastPipelineFailure()
    {
        if (!hasFailure)
        {
            return FallbackValue;
        }

        var incremented = Increment(successValue);
        return IsPositive(incremented) ? incremented * 2 : FallbackValue;
    }

    [Benchmark]
    [BenchmarkCategory("Fail-fast pipeline - failure")]
    public int FunnySharpFailFastPipelineFailure() =>
        GetValueOr(
            funnySharpFailure
                .Map(IncrementSelector)
                .Ensure(PositivePredicate, Error)
                .Bind(FunnySharpDoubleBinder),
            FallbackValue);

    [Benchmark]
    [BenchmarkCategory("Fail-fast pipeline - failure")]
    public int FSharpCoreFailFastPipelineFailure() =>
        ResultModule.DefaultValue(
            FallbackValue,
            ResultModule.Bind(
                FSharpCheckAndDoubleBinder,
                ResultModule.Map(IncrementFSharpFunc, fSharpFailure)));

    [Benchmark]
    [BenchmarkCategory("Fail-fast pipeline - failure")]
    public int CSharpFunctionalExtensionsFailFastPipelineFailure() =>
        cSharpFunctionalExtensionsFailure
            .Map(IncrementSelector)
            .Ensure(PositivePredicate, Error)
            .Bind(CSharpFunctionalExtensionsDoubleBinder)
            .GetValueOrDefault(FallbackValue);

    [Benchmark]
    [BenchmarkCategory("Fail-fast pipeline - failure")]
    public int LanguageExtFailFastPipelineFailure() =>
        languageExtFailure
            .Map(IncrementSelector)
            .Bind(LanguageExtCheckAndDoubleBinder)
            .IfLeft(FallbackValue);

    internal int[] ValidateEquivalence()
    {
        Setup();
        return
        [
            DirectConstructionInspectionSuccess(),
            FunnySharpConstructionInspectionSuccess(),
            FSharpCoreConstructionInspectionSuccess(),
            CSharpFunctionalExtensionsConstructionInspectionSuccess(),
            LanguageExtConstructionInspectionSuccess(),
            DirectConstructionInspectionFailure(),
            FunnySharpConstructionInspectionFailure(),
            FSharpCoreConstructionInspectionFailure(),
            CSharpFunctionalExtensionsConstructionInspectionFailure(),
            LanguageExtConstructionInspectionFailure(),
            DirectFailFastPipelineSuccess(),
            FunnySharpFailFastPipelineSuccess(),
            FSharpCoreFailFastPipelineSuccess(),
            CSharpFunctionalExtensionsFailFastPipelineSuccess(),
            LanguageExtFailFastPipelineSuccess(),
            DirectFailFastPipelineFailure(),
            FunnySharpFailFastPipelineFailure(),
            FSharpCoreFailFastPipelineFailure(),
            CSharpFunctionalExtensionsFailFastPipelineFailure(),
            LanguageExtFailFastPipelineFailure(),
        ];
    }

    private static int GetValueOr<TError>(FunnySharp.Result<int, TError> result, int fallback) =>
        result.TryGetValue(out var value) ? value : fallback;

    private static int Increment(int value) => value + 1;

    private static bool IsPositive(int value) => value > 0;

    private static FunnySharp.Result<int, string> FunnySharpDouble(int value) =>
        FunnySharp.Result<int, string>.Success(value * 2);

    private static CfeResultOfIntString CSharpFunctionalExtensionsDouble(int value) =>
        CfeResult.Success<int, string>(value * 2);

    private static FSharpResult<int, string> CheckAndDoubleFSharp(int value) =>
        IsPositive(value)
            ? FSharpResult<int, string>.NewOk(value * 2)
            : FSharpResult<int, string>.NewError(Error);

    private static Lext.Either<string, int> LanguageExtCheckAndDouble(int value) =>
        IsPositive(value)
            ? Lext.Either<string, int>.Right(value * 2)
            : Lext.Either<string, int>.Left(Error);
}
