using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using FunnySharp;
using FunckyMonads = Funcky.Monads;
using Lext = LanguageExt;
using Microsoft.FSharp.Core;

namespace FunnySharp.Benchmarks;

// Equivalence contract for every scenario in this class: each compared path receives the same
// pre-built input carrier and produces the same final int - the mapped value for a present
// carrier or the fallback for an absent one. Libraries differ in how absence is represented
// (FunnySharp, Funcky, and language-ext use struct carriers; FSharp.Core uses a nullable
// reference carrier), which is exactly the representation difference these comparisons expose:
// construction, dispatch, and extraction cost for the same observable semantics. Delegates and
// converters are cached so no path pays per-operation delegate construction.
[ShortRunJob]
[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class OptionCarrierBenchmarks
{
    private const int PresentValue = 42;
    private const int FallbackValue = -1;

    private static readonly Func<int, int> IncrementSelector = Increment;

    private static readonly Func<int, int> PassThroughSelector = static value => value;

    private static readonly Converter<int, int> IncrementConverter = Increment;

    private static readonly FSharpFunc<int, int> IncrementFSharpFunc = IncrementConverter;

    private bool hasPresentValue;
    private Option<int> funnySharpSome;
    private Option<int> funnySharpNone;
    private FSharpOption<int> fSharpSome = null!;
    private FSharpOption<int> fSharpNone = null!;
    private FunckyMonads.Option<int> funckySome;
    private FunckyMonads.Option<int> funckyNone;
    private Lext.Option<int> languageExtSome;
    private Lext.Option<int> languageExtNone;

    [GlobalSetup]
    public void Setup()
    {
        hasPresentValue = true;
        funnySharpSome = Option.Some(PresentValue);
        funnySharpNone = Option.None<int>();
        fSharpSome = FSharpOption<int>.Some(PresentValue);
        fSharpNone = FSharpOption<int>.None;
        funckySome = FunckyMonads.Option.Some(PresentValue);
        funckyNone = FunckyMonads.Option<int>.None;
        languageExtSome = Lext.Option<int>.Some(PresentValue);
        languageExtNone = Lext.Option<int>.None;
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Map - present")]
    public int DirectMapPresent() =>
        hasPresentValue ? Increment(PresentValue) : FallbackValue;

    [Benchmark]
    [BenchmarkCategory("Map - present")]
    public int FunnySharpMapPresent() =>
        funnySharpSome.Map(IncrementSelector).GetValueOr(FallbackValue);

    [Benchmark]
    [BenchmarkCategory("Map - present")]
    public int FSharpCoreMapPresent() =>
        OptionModule.DefaultValue(FallbackValue, OptionModule.Map(IncrementFSharpFunc, fSharpSome));

    [Benchmark]
    [BenchmarkCategory("Map - present")]
    public int FunckyMapPresent()
    {
        return funckySome.Select(IncrementSelector).Match(FallbackValue, PassThroughSelector);
    }

    [Benchmark]
    [BenchmarkCategory("Map - present")]
    public int LanguageExtMapPresent() =>
        languageExtSome.Map(IncrementSelector).IfNone(FallbackValue);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Map - absent")]
    public int DirectMapAbsent() =>
        hasPresentValue ? FallbackValue : Increment(PresentValue);

    [Benchmark]
    [BenchmarkCategory("Map - absent")]
    public int FunnySharpMapAbsent() =>
        funnySharpNone.Map(IncrementSelector).GetValueOr(FallbackValue);

    [Benchmark]
    [BenchmarkCategory("Map - absent")]
    public int FSharpCoreMapAbsent() =>
        OptionModule.DefaultValue(FallbackValue, OptionModule.Map(IncrementFSharpFunc, fSharpNone));

    [Benchmark]
    [BenchmarkCategory("Map - absent")]
    public int FunckyMapAbsent()
    {
        return funckyNone.Select(IncrementSelector).Match(FallbackValue, PassThroughSelector);
    }

    [Benchmark]
    [BenchmarkCategory("Map - absent")]
    public int LanguageExtMapAbsent() =>
        languageExtNone.Map(IncrementSelector).IfNone(FallbackValue);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Value-or-fallback - present")]
    public int DirectValueOrFallbackPresent() =>
        hasPresentValue ? PresentValue : FallbackValue;

    [Benchmark]
    [BenchmarkCategory("Value-or-fallback - present")]
    public int FunnySharpValueOrFallbackPresent() =>
        funnySharpSome.GetValueOr(FallbackValue);

    [Benchmark]
    [BenchmarkCategory("Value-or-fallback - present")]
    public int FSharpCoreValueOrFallbackPresent() =>
        OptionModule.DefaultValue(FallbackValue, fSharpSome);

    [Benchmark]
    [BenchmarkCategory("Value-or-fallback - present")]
    public int FunckyValueOrFallbackPresent() =>
        funckySome.Match(FallbackValue, PassThroughSelector);

    [Benchmark]
    [BenchmarkCategory("Value-or-fallback - present")]
    public int LanguageExtValueOrFallbackPresent() =>
        languageExtSome.IfNone(FallbackValue);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Value-or-fallback - absent")]
    public int DirectValueOrFallbackAbsent() =>
        hasPresentValue ? FallbackValue : PresentValue;

    [Benchmark]
    [BenchmarkCategory("Value-or-fallback - absent")]
    public int FunnySharpValueOrFallbackAbsent() =>
        funnySharpNone.GetValueOr(FallbackValue);

    [Benchmark]
    [BenchmarkCategory("Value-or-fallback - absent")]
    public int FSharpCoreValueOrFallbackAbsent() =>
        OptionModule.DefaultValue(FallbackValue, fSharpNone);

    [Benchmark]
    [BenchmarkCategory("Value-or-fallback - absent")]
    public int FunckyValueOrFallbackAbsent() =>
        funckyNone.Match(FallbackValue, PassThroughSelector);

    [Benchmark]
    [BenchmarkCategory("Value-or-fallback - absent")]
    public int LanguageExtValueOrFallbackAbsent() =>
        languageExtNone.IfNone(FallbackValue);

    internal int[] ValidateEquivalence()
    {
        Setup();
        return
        [
            DirectMapPresent(),
            FunnySharpMapPresent(),
            FSharpCoreMapPresent(),
            FunckyMapPresent(),
            LanguageExtMapPresent(),
            DirectMapAbsent(),
            FunnySharpMapAbsent(),
            FSharpCoreMapAbsent(),
            FunckyMapAbsent(),
            LanguageExtMapAbsent(),
            DirectValueOrFallbackPresent(),
            FunnySharpValueOrFallbackPresent(),
            FSharpCoreValueOrFallbackPresent(),
            FunckyValueOrFallbackPresent(),
            LanguageExtValueOrFallbackPresent(),
            DirectValueOrFallbackAbsent(),
            FunnySharpValueOrFallbackAbsent(),
            FSharpCoreValueOrFallbackAbsent(),
            FunckyValueOrFallbackAbsent(),
            LanguageExtValueOrFallbackAbsent(),
        ];
    }

    private static int Increment(int value) => value + 1;
}
