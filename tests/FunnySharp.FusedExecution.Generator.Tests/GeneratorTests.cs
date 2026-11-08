using System.Collections.Immutable;
using FunnySharp.FusedExecution.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace FunnySharp.FusedExecution.Generator.Tests;

public sealed class GeneratorTests
{
    [Fact]
    public void SupportedSummaryEmitsCheckedExecutionAndCompiles()
    {
        const string source = """
            using System.Collections.Generic;
            using FunnySharp;

            public sealed record Line(int Quantity, decimal Price);

            public static partial class Sample
            {
                public static readonly Summary<Line, (int First, decimal Second), (int First, decimal Second)> Stats =
                    Summary.Zip(
                        Summary<Line>.Count(),
                        Summary<Line>.Sum(static line => line.Quantity * line.Price));

                [FusedSummary(nameof(Stats))]
                public static partial (int First, decimal Second) Evaluate(IEnumerable<Line> source);
            }
            """;

        var (result, output) = GeneratorHarness.RunWithCompilation(source);

        AssertCompiles(result, output);
        var generated = GeneratedText(result);
        Assert.Contains("static decimal __FusedSelector1(", generated, StringComparison.Ordinal);
        Assert.Contains("checked(__fused1 + __FusedSelector1(__fusedItem))", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("checked(__fused1 + (", generated, StringComparison.Ordinal);
        Assert.Contains("ArgumentNullException.ThrowIfNull(source, \"source\")", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void SupportedDecoderEmitsFastPathAndReadOnlyListAndCompiles()
    {
        const string source = """
            using System.Collections.Generic;
            using System.Text.Json;
            using FunnySharp;

            public sealed record Pair(int Id, IReadOnlyList<string> Names);

            public static partial class Sample
            {
                public static readonly Decoder<Pair> PairDecoder =
                    Decoder.Object(
                        Decoder.Field("id", Decoder.Int32),
                        Decoder.Field("names", Decoder.List(Decoder.NonEmptyString)),
                        static (id, names) => new Pair(id, names));

                [FusedDecoding(nameof(PairDecoder))]
                public static partial Validation<Pair, DecodeError> Decode(JsonElement input);
            }
            """;

        var (result, output) = GeneratorHarness.RunWithCompilation(source);

        AssertCompiles(result, output);
        var generated = GeneratedText(result);
        Assert.Contains("PairDecoder.Decode(input)", generated, StringComparison.Ordinal);
        Assert.Contains("__FusedTryGetFirstProperty", generated, StringComparison.Ordinal);
        Assert.Contains("IReadOnlyList<string> __fusedField1;", generated, StringComparison.Ordinal);
        Assert.Contains("array.Empty<string>()", generated, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(".AsReadOnly()", generated, StringComparison.Ordinal);
        Assert.Contains("__FusedCombine(int id, global::System.Collections.Generic.IReadOnlyList<string> names)", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void CrossNamespaceCombineUsesFileScopedUsingDirectives()
    {
        const string source = """
            using System.Collections.Generic;
            using System.Text.Json;
            using FunnySharp;
            using Other;

            namespace Other
            {
                public sealed record Pair(int Id, string Name);
            }

            namespace SampleNs
            {
                public static partial class Sample
                {
                    public static readonly Decoder<Pair> PairDecoder =
                        Decoder.Object(
                            Decoder.Field("id", Decoder.Int32),
                            Decoder.Field("name", Decoder.NonEmptyString),
                            static (id, name) => new Pair(id, name));

                    [FusedDecoding(nameof(PairDecoder))]
                    public static partial Validation<Pair, DecodeError> Decode(JsonElement input);
                }
            }
            """;

        var (result, output) = GeneratorHarness.RunWithCompilation(source);

        AssertCompiles(result, output);
    }

    [Fact]
    public void BlockScopedNamespaceUsingsAreCopied()
    {
        const string source = """
            namespace SampleNs
            {
                using System.Collections.Generic;
                using System.Text.Json;
                using FunnySharp;
                using Other;

                public static partial class Sample
                {
                    public static readonly Decoder<Pair> PairDecoder =
                        Decoder.Object(
                            Decoder.Field("id", Decoder.Int32),
                            Decoder.Field("name", Decoder.NonEmptyString),
                            static (id, name) => new Pair(id, name));

                    [FusedDecoding(nameof(PairDecoder))]
                    public static partial Validation<Pair, DecodeError> Decode(JsonElement input);
                }
            }

            namespace Other
            {
                public sealed record Pair(int Id, string Name);
            }
            """;

        var (result, output) = GeneratorHarness.RunWithCompilation(source);

        AssertCompiles(result, output);
    }

    [Fact]
    public void KeywordNamespaceSegmentsAreEscaped()
    {
        const string source = """
            using System.Collections.Generic;
            using FunnySharp;

            namespace @class
            {
                public sealed record Line(int Quantity);

                public static partial class Sample
                {
                    public static readonly Summary<Line, (int First, int Second), (int First, int Second)> Stats =
                        Summary.Zip(
                            Summary<Line>.Count(),
                            Summary<Line>.Sum(static line => line.Quantity));

                    [FusedSummary(nameof(Stats))]
                    public static partial (int First, int Second) Evaluate(IEnumerable<Line> source);
                }
            }
            """;

        var (result, output) = GeneratorHarness.RunWithCompilation(source);

        AssertCompiles(result, output);
        Assert.Contains("namespace @class", GeneratedText(result), StringComparison.Ordinal);
    }

    [Fact]
    public void TargetTypedSelectorBodiesCompile()
    {
        const string source = """
            using System;
            using System.Collections.Generic;
            using FunnySharp;

            public sealed record Line(int Quantity);
            public sealed class Foo : IComparable<Foo>
            {
                public int CompareTo(Foo? other) => 0;
            }

            public static partial class Sample
            {
                public static readonly Summary<
                    Line,
                    (int First, Option<string> Second, decimal Third, Option<int> Fourth),
                    (int First, Option<string> Second, decimal Third, Option<int> Fourth)> Stats =
                    Summary.Zip(
                        Summary<Line>.Count(),
                        Summary<Line>.Min<string>(static line => null),
                        Summary<Line>.Sum<decimal>(static line => default),
                        Summary<Line>.Min<int>(static line => throw new InvalidOperationException("boom")));

                public static readonly Summary<Line, (Option<Foo> First, Option<Foo> Second), (Option<Foo> First, Option<Foo> Second)> Extrema =
                    Summary.Zip(
                        Summary<Line>.Min<Foo>(static line => new()),
                        Summary<Line>.Max<Foo>(static line => new()));

                [FusedSummary(nameof(Stats))]
                public static partial (int First, Option<string> Second, decimal Third, Option<int> Fourth) Evaluate(
                    IEnumerable<Line> source);

                [FusedSummary(nameof(Extrema))]
                public static partial (Option<Foo> First, Option<Foo> Second) EvaluateExtrema(IEnumerable<Line> source);
            }
            """;

        var (result, output) = GeneratorHarness.RunWithCompilation(source);

        AssertCompiles(result, output);
    }

    [Fact]
    public void SelectorParametersCollidingWithSourceAndGeneratedLocalsCompile()
    {
        const string source = """
            using System.Collections.Generic;
            using FunnySharp;

            public sealed record Line(int Quantity);

            public static partial class Sample
            {
                public static readonly Summary<Line, (int First, int Second, int Third), (int First, int Second, int Third)> Stats =
                    Summary.Zip(
                        Summary<Line>.Count(),
                        Summary<Line>.Sum(static source => source.Quantity),
                        Summary<Line>.Sum(static __fused0 => __fused0.Quantity));

                [FusedSummary(nameof(Stats))]
                public static partial (int First, int Second, int Third) Evaluate(IEnumerable<Line> source);
            }
            """;

        var (result, output) = GeneratorHarness.RunWithCompilation(source);

        AssertCompiles(result, output);
    }

    [Fact]
    public void DecoderCombineParametersMayShadowMembersAndGeneratedLocals()
    {
        const string source = """
            using System.Text.Json;
            using FunnySharp;

            public sealed record Pair(int Id, string Name);

            public static partial class Sample
            {
                public static readonly Decoder<Pair> PairDecoder =
                    Decoder.Object(
                        Decoder.Field("id", Decoder.Int32),
                        Decoder.Field("name", Decoder.NonEmptyString),
                        static (input, __fused0) => new Pair(input, __fused0));

                public static readonly Decoder<Pair> OtherDecoder =
                    Decoder.Object(
                        Decoder.Field("id", Decoder.Int32),
                        Decoder.Field("name", Decoder.NonEmptyString),
                        static (id, OtherDecoder) => new Pair(id, OtherDecoder));

                [FusedDecoding(nameof(PairDecoder))]
                public static partial Validation<Pair, DecodeError> Decode(JsonElement input);

                [FusedDecoding(nameof(OtherDecoder))]
                public static partial Validation<Pair, DecodeError> DecodeOther(JsonElement input);
            }
            """;

        var (result, output) = GeneratorHarness.RunWithCompilation(source);

        AssertCompiles(result, output);
    }

    [Fact]
    public void GeneratedNamesDoNotShadowUserMembers()
    {
        const string source = """
            using System.Collections.Generic;
            using System.Text.Json;
            using FunnySharp;

            public sealed record Line(int Quantity);
            public sealed record Pair(int Id, string Name);

            public static partial class Sample
            {
                private static readonly int __fused0 = 5;
                private static readonly int __fusedItem = 7;

                public static readonly Summary<Line, (int First, int Second), (int First, int Second)> Stats =
                    Summary.Zip(
                        Summary<Line>.Count(),
                        Summary<Line>.Sum(static line => line.Quantity + __fused0 + __fusedItem));

                public static readonly Decoder<Pair> PairDecoder =
                    Decoder.Object(
                        Decoder.Field("id", Decoder.Int32),
                        Decoder.Field("name", Decoder.NonEmptyString),
                        static (id, name) => new Pair(id + __fusedField0, name));

                [FusedSummary(nameof(Stats))]
                public static partial (int First, int Second) Evaluate(IEnumerable<Line> source);

                [FusedDecoding(nameof(PairDecoder))]
                public static partial Validation<Pair, DecodeError> Decode(JsonElement input);

                private static readonly int __fusedField0 = 3;
            }
            """;

        var (result, output) = GeneratorHarness.RunWithCompilation(source);

        AssertCompiles(result, output);
    }

    [Fact]
    public void NullableSourceSignatureIsPreserved()
    {
        const string source = """
            using System.Collections.Generic;
            using FunnySharp;

            public sealed record Line(int Quantity);

            public static partial class Sample
            {
                public static readonly Summary<
                    Line?,
                    (int First, Option<string?> Second),
                    (int First, Option<string?> Second)> Stats =
                    Summary.Zip(
                        Summary<Line?>.Count(),
                        Summary<Line?>.Min(static line => line == null ? null : "note"));

                [FusedSummary(nameof(Stats))]
                public static partial (int First, Option<string?> Second) Evaluate(IEnumerable<Line?> source);
            }
            """;

        var (result, output) = GeneratorHarness.RunWithCompilation(source);

        AssertCompiles(result, output);
        var generated = GeneratedText(result);
        Assert.Contains("IEnumerable<global::Line?> source", generated, StringComparison.Ordinal);
        Assert.Contains("Option<string?>", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void LocalFunctionAttributeReportsFs3903()
    {
        const string source = """
            using FunnySharp;

            public static partial class Sample
            {
                public static int Run()
                {
                    [FusedSummary("Stats")] static int Local() => 0;
                    return Local();
                }
            }
            """;

        var result = GeneratorHarness.Run(source);

        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Id == "FS3903");
        Assert.DoesNotContain(
            result.Results.SelectMany(static item => item.GeneratedSources),
            static item => item.HintName.Contains("Local", StringComparison.Ordinal));
    }

    [Fact]
    public void AccessorAttributeReportsFs3903()
    {
        const string source = """
            using FunnySharp;

            public static partial class Sample
            {
                public static int Value
                {
                    [FusedSummary("Stats")] get => 0;
                }
            }
            """;

        var result = GeneratorHarness.Run(source);

        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Id == "FS3903");
    }

    [Fact]
    public void FileLocalContainingTypeReportsFs3903()
    {
        const string source = """
            using System.Collections.Generic;
            using FunnySharp;

            public sealed record Line(int Quantity);

            file static partial class Sample
            {
                public static readonly Summary<Line, int, int> Stats =
                    Summary.Zip(
                        Summary<Line>.Count(),
                        Summary<Line>.Sum(static line => line.Quantity));

                [FusedSummary(nameof(Stats))]
                public static partial int Evaluate(IEnumerable<Line> source);
            }
            """;

        var result = GeneratorHarness.Run(source);

        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Id == "FS3903");
    }

    [Fact]
    public void UnsupportedSummaryFactoryReportsFs3901()
    {
        const string source = """
            using System.Collections.Generic;
            using FunnySharp;

            public sealed record Line(int Quantity, decimal Price);

            public static partial class Sample
            {
                public static readonly Summary<Line, int, int> Stats =
                    Summary.Create<Line, int>(0, static (count, _) => count + 1);

                [FusedSummary(nameof(Stats))]
                public static partial int Evaluate(IEnumerable<Line> source);
            }
            """;

        var result = GeneratorHarness.Run(source);

        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Id == "FS3901");
    }

    [Fact]
    public void CustomDecoderReportsFs3902()
    {
        const string source = """
            using System.Text.Json;
            using FunnySharp;

            public static partial class Sample
            {
                public static readonly Decoder<int> ValueDecoder =
                    Decoder.From<int>(static _ => Validation<int, DecodeError>.Valid(0));

                [FusedDecoding(nameof(ValueDecoder))]
                public static partial Validation<int, DecodeError> Decode(JsonElement input);
            }
            """;

        var result = GeneratorHarness.Run(source);

        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Id == "FS3902");
    }

    [Fact]
    public void NonPartialMethodReportsFs3903()
    {
        const string source = """
            using System.Collections.Generic;
            using FunnySharp;

            public sealed record Line(int Quantity, decimal Price);

            public static partial class Sample
            {
                public static readonly Summary<Line, int, int> Stats =
                    Summary.Zip(
                        Summary<Line>.Count(),
                        Summary<Line>.Sum(static line => line.Quantity));

                [FusedSummary(nameof(Stats))]
                public static int Evaluate(IEnumerable<Line> source) => 0;
            }
            """;

        var result = GeneratorHarness.Run(source);

        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Id == "FS3903");
    }

    [Fact]
    public void NonStaticSelectorReportsFs3901()
    {
        const string source = """
            using System.Collections.Generic;
            using FunnySharp;

            public sealed record Line(int Quantity, decimal Price);

            public static partial class Sample
            {
                public static readonly Summary<Line, (int, decimal), (int, decimal)> Stats =
                    Summary.Zip(
                        Summary<Line>.Count(),
                        Summary<Line>.Sum(line => line.Quantity));

                [FusedSummary(nameof(Stats))]
                public static partial (int, decimal) Evaluate(IEnumerable<Line> source);
            }
            """;

        var result = GeneratorHarness.Run(source);

        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Id == "FS3901");
    }

    [Fact]
    public void NestedObjectFieldReportsFs3902()
    {
        const string source = """
            using System.Text.Json;
            using FunnySharp;

            public sealed record Address(string City);
            public sealed record Customer(int Id, Address Address);

            public static partial class Sample
            {
                public static readonly Decoder<Customer> CustomerDecoder =
                    Decoder.Object(
                        Decoder.Field("id", Decoder.Int32),
                        Decoder.Field("address", Decoder.Object(
                            Decoder.Field("city", Decoder.NonEmptyString),
                            static city => new Address(city))),
                        static (id, address) => new Customer(id, address));

                [FusedDecoding(nameof(CustomerDecoder))]
                public static partial Validation<Customer, DecodeError> Decode(JsonElement input);
            }
            """;

        var result = GeneratorHarness.Run(source);

        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Id == "FS3902");
    }

    [Fact]
    public void AsyncSourceMethodReportsFs3903()
    {
        const string source = """
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using FunnySharp;

            public sealed record Line(int Quantity, decimal Price);

            public static partial class Sample
            {
                public static readonly Summary<Line, (int, decimal), (int, decimal)> Stats =
                    Summary.Zip(
                        Summary<Line>.Count(),
                        Summary<Line>.Sum(static line => line.Quantity));

                [FusedSummary(nameof(Stats))]
                public static partial (int, decimal) Evaluate(IAsyncEnumerable<Line> source);
            }
            """;

        var result = GeneratorHarness.Run(source);

        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Id == "FS3903");
    }

    [Fact]
    public void MutableSummaryFieldReportsFs3904()
    {
        const string source = """
            using System.Collections.Generic;
            using FunnySharp;

            public sealed record Line(int Quantity, decimal Price);

            public static partial class Sample
            {
                public static Summary<Line, int, int> Stats =
                    Summary.Zip(
                        Summary<Line>.Count(),
                        Summary<Line>.Sum(static line => line.Quantity));

                [FusedSummary(nameof(Stats))]
                public static partial int Evaluate(IEnumerable<Line> source);
            }
            """;

        var result = GeneratorHarness.Run(source);

        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Id == "FS3904");
    }

    [Fact]
    public void ParameterlessAttributedMethodReportsFs3903AndOtherDeclarationsStillFuse()
    {
        const string source = """
            using System.Collections.Generic;
            using FunnySharp;

            public sealed record Line(int Quantity, decimal Price);

            public static partial class Sample
            {
                public static readonly Summary<Line, (int First, decimal Second), (int First, decimal Second)> Stats =
                    Summary.Zip(
                        Summary<Line>.Count(),
                        Summary<Line>.Sum(static line => line.Quantity * line.Price));

                [FusedSummary(nameof(Stats))]
                public static partial (int First, decimal Second) Evaluate(IEnumerable<Line> source);

                [FusedSummary(nameof(Stats))]
                public static partial (int First, decimal Second) EvaluateEveryLine();
            }
            """;

        var result = GeneratorHarness.Run(source);

        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Id == "FS3903");
        Assert.DoesNotContain(result.Diagnostics, static diagnostic => diagnostic.Id == "CS8785");
        Assert.Contains(
            result.Results.SelectMany(static item => item.GeneratedSources),
            static item => item.HintName.Contains("Sample.Evaluate.", StringComparison.Ordinal));
        Assert.Contains(
            "static decimal __FusedSelector1(",
            GeneratedText(result),
            StringComparison.Ordinal);
    }

    [Fact]
    public void StaticConstructorReassignmentReportsFs3904()
    {
        const string source = """
            using System.Collections.Generic;
            using FunnySharp;

            public sealed record Line(int Quantity, decimal Price);

            public static partial class Sample
            {
                public static readonly Summary<Line, (int First, decimal Second), (int First, decimal Second)> Stats =
                    Summary.Zip(
                        Summary<Line>.Count(),
                        Summary<Line>.Sum(static line => line.Quantity));

                static Sample()
                {
                    Stats = Summary.Zip(
                        Summary<Line>.Count(),
                        Summary<Line>.Sum(static line => line.Price));
                }

                [FusedSummary(nameof(Stats))]
                public static partial (int First, decimal Second) Evaluate(IEnumerable<Line> source);
            }
            """;

        var result = GeneratorHarness.Run(source);

        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Id == "FS3904");
        Assert.DoesNotContain("__FusedSelector", GeneratedText(result), StringComparison.Ordinal);
    }

    [Fact]
    public void FieldInitializerReassignmentReportsFs3904()
    {
        const string source = """
            using System.Collections.Generic;
            using FunnySharp;

            public sealed record Line(int Quantity, decimal Price);

            public static partial class Sample
            {
                public static readonly Summary<Line, (int First, decimal Second), (int First, decimal Second)> Stats =
                    Summary.Zip(
                        Summary<Line>.Count(),
                        Summary<Line>.Sum(static line => line.Quantity));

                public static readonly Summary<Line, (int First, decimal Second), (int First, decimal Second)> Replacement =
                    Stats = Summary.Zip(
                        Summary<Line>.Count(),
                        Summary<Line>.Sum(static line => line.Price));

                [FusedSummary(nameof(Stats))]
                public static partial (int First, decimal Second) Evaluate(IEnumerable<Line> source);
            }
            """;

        var result = GeneratorHarness.Run(source);

        Assert.Contains(result.Diagnostics, static diagnostic => diagnostic.Id == "FS3904");
    }

    [Fact]
    public void InArgumentReadDoesNotRejectFusion()
    {
        const string source = """
            using System.Collections.Generic;
            using FunnySharp;

            public sealed record Line(int Quantity, decimal Price);

            public static partial class Sample
            {
                public static readonly Summary<Line, (int First, decimal Second), (int First, decimal Second)> Stats =
                    Summary.Zip(
                        Summary<Line>.Count(),
                        Summary<Line>.Sum(static line => line.Quantity * line.Price));

                public static readonly Summary<Line, (int First, decimal Second), (int First, decimal Second)> Snapshot =
                    Inspect(in Stats);

                static Sample()
                {
                    InspectReadOnly(ref Stats);
                }

                private static Summary<Line, (int First, decimal Second), (int First, decimal Second)> Inspect(
                    in Summary<Line, (int First, decimal Second), (int First, decimal Second)> stats) => stats;

                private static void InspectReadOnly(
                    ref readonly Summary<Line, (int First, decimal Second), (int First, decimal Second)> stats)
                {
                }

                [FusedSummary(nameof(Stats))]
                public static partial (int First, decimal Second) Evaluate(IEnumerable<Line> source);
            }
            """;

        var (result, output) = GeneratorHarness.RunWithCompilation(source);

        AssertCompiles(result, output);
        Assert.Contains(
            "static decimal __FusedSelector1(",
            GeneratedText(result),
            StringComparison.Ordinal);
    }

    [Fact]
    public void RefAndOutArgumentsStillReportFs3904()
    {
        const string refSource = """
            using System.Collections.Generic;
            using FunnySharp;

            public sealed record Line(int Quantity, decimal Price);

            public static partial class Sample
            {
                public static readonly Summary<Line, (int First, decimal Second), (int First, decimal Second)> Stats =
                    Summary.Zip(
                        Summary<Line>.Count(),
                        Summary<Line>.Sum(static line => line.Quantity));

                static Sample()
                {
                    Touch(ref Stats);
                }

                private static void Touch(
                    ref Summary<Line, (int First, decimal Second), (int First, decimal Second)> stats)
                {
                }

                [FusedSummary(nameof(Stats))]
                public static partial (int First, decimal Second) Evaluate(IEnumerable<Line> source);
            }
            """;
        const string outSource = """
            using System.Collections.Generic;
            using FunnySharp;

            public sealed record Line(int Quantity, decimal Price);

            public static partial class Sample
            {
                public static readonly Summary<Line, (int First, decimal Second), (int First, decimal Second)> Stats =
                    Summary.Zip(
                        Summary<Line>.Count(),
                        Summary<Line>.Sum(static line => line.Quantity));

                static Sample()
                {
                    Initialize(out Stats);
                }

                private static void Initialize(
                    out Summary<Line, (int First, decimal Second), (int First, decimal Second)> stats) =>
                    stats = default;

                [FusedSummary(nameof(Stats))]
                public static partial (int First, decimal Second) Evaluate(IEnumerable<Line> source);
            }
            """;

        foreach (var source in new[] { refSource, outSource })
        {
            var result = GeneratorHarness.Run(source);

            Assert.True(
                result.Diagnostics.Any(static diagnostic => diagnostic.Id == "FS3904"),
                source);
        }
    }

    [Fact]
    public void StaticConstructorWritingAnotherFieldStillFuses()
    {
        const string source = """
            using System.Collections.Generic;
            using FunnySharp;

            public sealed record Line(int Quantity, decimal Price);

            public static partial class Sample
            {
                private static readonly int Offset;

                static Sample()
                {
                    Offset = 100;
                }

                public static readonly Summary<Line, (int First, int Second), (int First, int Second)> Stats =
                    Summary.Zip(
                        Summary<Line>.Count(),
                        Summary<Line>.Sum(static line => line.Quantity + Offset));

                [FusedSummary(nameof(Stats))]
                public static partial (int First, int Second) Evaluate(IEnumerable<Line> source);
            }
            """;

        var (result, output) = GeneratorHarness.RunWithCompilation(source);

        AssertCompiles(result, output);
        Assert.Contains(
            "__FusedSelector1(",
            GeneratedText(result),
            StringComparison.Ordinal);
    }

    private static string GeneratedText(GeneratorDriverRunResult result) =>
        string.Join(
            "\n",
            result.Results
                .SelectMany(static item => item.GeneratedSources)
                .Select(static item => item.SourceText.ToString()));

    private static void AssertCompiles(GeneratorDriverRunResult result, Compilation output)
    {
        Assert.Empty(result.Diagnostics);
        var errors = output
            .GetDiagnostics(TestContext.Current.CancellationToken)
            .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToArray();
        Assert.True(
            errors.Length == 0,
            "Generated compilation failed:\n" + string.Join("\n", errors.Select(static error => error.ToString())));
    }
}

internal static class GeneratorHarness
{
    internal static GeneratorDriverRunResult Run(string source) =>
        RunWithCompilation(source).Result;

    internal static (GeneratorDriverRunResult Result, Compilation Output) RunWithCompilation(string source)
    {
        var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
        var tree = CSharpSyntaxTree.ParseText(source, parseOptions);
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(static path => (MetadataReference)MetadataReference.CreateFromFile(path))
            .ToImmutableArray()
            .Add(MetadataReference.CreateFromFile(typeof(Summary).Assembly.Location));

        var compilation = CSharpCompilation.Create(
            "FusedGeneratorTests",
            [tree],
            references,
            new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new FusedExecutionGenerator().AsSourceGenerator()],
            parseOptions: parseOptions);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var output, out _);
        return (driver.GetRunResult(), output);
    }
}
