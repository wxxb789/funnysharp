using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using FunnySharp.FusedExecution.Generator;
using FunnySharp.TestSupport;
using FunnySharp.TestSupport.FusedExecution;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using static FunnySharp.TestSupport.MeasurementHarness;

namespace FunnySharp.FusedExecution.Measurements;

internal static class Program
{
    private const int SummaryIterations = 10_000;
    private const int SummaryWarmup = 200;
    private const int DecoderIterations = 100_000;
    private const int DecoderWarmup = 1_000;
    private const int InvalidDecoderIterations = 50_000;
    private const int InvalidDecoderWarmup = 1_000;

    private static void Main()
    {
        Console.WriteLine($"runtime: {RuntimeInformation.FrameworkDescription}");
        Console.WriteLine($"os: {RuntimeInformation.OSDescription}");
        Console.WriteLine($"cores: {Environment.ProcessorCount}");
        Console.WriteLine();

        var checks = FusedComparison.VerifyAll();
        Console.WriteLine($"parity checks: {checks}");
        WriteGenerationCost();
        WriteStaticDescriptionCost();
        WriteSummaryRows();
        WriteDecoderRows();
    }

    private static void WriteStaticDescriptionCost()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var before = GC.GetTotalAllocatedBytes(precise: true);
        var stopwatch = Stopwatch.StartNew();
        System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(
            typeof(OneShotSummaryDefinition).TypeHandle);
        stopwatch.Stop();
        var allocated = GC.GetTotalAllocatedBytes(precise: true) - before;
        Console.WriteLine("## One-time static Summary description");
        Console.WriteLine();
        Console.WriteLine("| Metric | Value |");
        Console.WriteLine("| --- | ---: |");
        Console.WriteLine($"| Allocated once by the 4-component Summary field | {allocated:N0} B |");
        Console.WriteLine($"| Static constructor time | {stopwatch.Elapsed.TotalMilliseconds:F3} ms |");
        Console.WriteLine();
    }

    private static void WriteSummaryRows()
    {
        var lines = FusedFixtures.CreateLines(1024);
        var empty = Array.Empty<OrderLine>();

        _ = FusedConsumers.SummaryFused(lines);
        Console.WriteLine("## Summary: count + checked sum + extrema, N=1024");
        Console.WriteLine();
        Console.WriteLine("| Consumer | Allocated B/op | Time ns/op |");
        Console.WriteLine("| --- | ---: | ---: |");
        WriteRow(
            "Generated fused summary",
            MeasureTotal(() => FusedConsumers.SummaryFused(lines), SummaryIterations, SummaryWarmup));
        WriteRow(
            "Delegated Summary.Zip",
            MeasureTotal(() => FusedConsumers.SummaryDelegated(lines), SummaryIterations, SummaryWarmup));
        WriteRow(
            "Hand-written loop",
            MeasureTotal(() => FusedConsumers.SummaryHandWritten(lines), SummaryIterations, SummaryWarmup));
        WriteRow(
            "LINQ one-pass Aggregate",
            MeasureTotal(() => FusedConsumers.SummaryLinqAggregate(lines), SummaryIterations, SummaryWarmup));
        WriteRow(
            "LINQ separate Count/Sum/Min/Max",
            MeasureTotal(() => FusedConsumers.SummaryLinqSeparate(lines), SummaryIterations, SummaryWarmup));
        Console.WriteLine();
        Console.WriteLine("## Summary: empty source");
        Console.WriteLine();
        Console.WriteLine("| Consumer | Allocated B/op | Time ns/op |");
        Console.WriteLine("| --- | ---: | ---: |");
        WriteRow(
            "Generated fused summary",
            MeasureTotal(() => FusedConsumers.SummaryFused(empty), SummaryIterations, SummaryWarmup));
        WriteRow(
            "Delegated Summary.Zip",
            MeasureTotal(() => FusedConsumers.SummaryDelegated(empty), SummaryIterations, SummaryWarmup));
        WriteRow(
            "Hand-written loop",
            MeasureTotal(() => FusedConsumers.SummaryHandWritten(empty), SummaryIterations, SummaryWarmup));
        Console.WriteLine();
    }

    private static void WriteDecoderRows()
    {
        using var validDocument = JsonDocument.Parse(FusedFixtures.ValidJson);
        using var invalidDocument = JsonDocument.Parse(FusedFixtures.EmptyTagElementJson);
        var valid = validDocument.RootElement;
        var invalid = invalidDocument.RootElement;

        Console.WriteLine("## Decoder: valid flat order input");
        Console.WriteLine();
        Console.WriteLine("| Consumer | Allocated B/op | Time ns/op |");
        Console.WriteLine("| --- | ---: | ---: |");
        WriteRow(
            "Generated fast path",
            MeasureTotal(() => FusedConsumers.DecoderFused(valid), DecoderIterations, DecoderWarmup));
        WriteRow(
            "Delegated Decoder.Object",
            MeasureTotal(() => FusedConsumers.DecoderDelegated(valid), DecoderIterations, DecoderWarmup));
        WriteRow(
            "Hand-written valid-only parse",
            MeasureTotal(() => FusedConsumers.DecoderHandWritten(valid), DecoderIterations, DecoderWarmup));
        Console.WriteLine();
        Console.WriteLine("## Decoder: invalid input (empty tag element)");
        Console.WriteLine();
        Console.WriteLine("| Consumer | Allocated B/op | Time ns/op |");
        Console.WriteLine("| --- | ---: | ---: |");
        WriteRow(
            "Generated fast path + runtime fallback",
            MeasureTotal(() => FusedConsumers.DecoderFused(invalid), InvalidDecoderIterations, InvalidDecoderWarmup));
        WriteRow(
            "Delegated Decoder.Object",
            MeasureTotal(() => FusedConsumers.DecoderDelegated(invalid), InvalidDecoderIterations, InvalidDecoderWarmup));
        Console.WriteLine();
    }

    private static void WriteGenerationCost()
    {
        const string minimalSource = """
            using System.Collections.Generic;
            using System.Text.Json;
            using FunnySharp;

            public sealed record Line(int Quantity, decimal Price);
            public sealed record Pair(int Id, string Name);

            public static partial class Sample
            {
                public static readonly Summary<Line, (int First, decimal Second), (int First, decimal Second)> Stats =
                    Summary.Zip(
                        Summary<Line>.Count(),
                        Summary<Line>.Sum(static line => line.Quantity * line.Price));

                public static readonly Decoder<Pair> PairDecoder =
                    Decoder.Object(
                        Decoder.Field("id", Decoder.Int32),
                        Decoder.Field("name", Decoder.NonEmptyString),
                        static (id, name) => new Pair(id, name));

                [FusedSummary(nameof(Stats))]
                public static partial (int First, decimal Second) Evaluate(IEnumerable<Line> source);

                [FusedDecoding(nameof(PairDecoder))]
                public static partial Validation<Pair, DecodeError> Decode(JsonElement input);
            }
            """;

        const string representativeSource = """
            using System.Collections.Generic;
            using System.Text.Json;
            using FunnySharp;

            public sealed record OrderLine(string Sku, string? Note, int Quantity, decimal UnitPrice);
            public sealed record OrderInput(int Id, string Customer, decimal Total, IReadOnlyList<string> Tags);

            public static partial class Sample
            {
                public static readonly Summary<
                    OrderLine,
                    (int First, decimal Second, Option<decimal> Third, Option<decimal> Fourth),
                    (int First, decimal Second, Option<decimal> Third, Option<decimal> Fourth)> Stats =
                    Summary.Zip(
                        Summary<OrderLine>.Count(),
                        Summary<OrderLine>.Sum(static line => line.Quantity * line.UnitPrice),
                        Summary<OrderLine>.Min(static line => line.UnitPrice),
                        Summary<OrderLine>.Max(static line => line.UnitPrice));

                public static readonly Decoder<OrderInput> OrderDecoder =
                    Decoder.Object(
                        Decoder.Field("id", Decoder.Int32),
                        Decoder.Field("customer", Decoder.NonEmptyString),
                        Decoder.Field("total", Decoder.Decimal),
                        Decoder.Field("tags", Decoder.NonEmptyList(Decoder.NonEmptyString)),
                        static (id, customer, total, tags) => new OrderInput(id, customer, total, tags));

                [FusedSummary(nameof(Stats))]
                public static partial (int First, decimal Second, Option<decimal> Third, Option<decimal> Fourth) Evaluate(
                    IEnumerable<OrderLine> source);

                [FusedDecoding(nameof(OrderDecoder))]
                public static partial Validation<OrderInput, DecodeError> Decode(JsonElement input);
            }
            """;

        var representative = MeasureGeneration(representativeSource);
        var minimal = MeasureGeneration(minimalSource);

        Console.WriteLine("## Generator execution cost");
        Console.WriteLine();
        Console.WriteLine("| Sample | Declarations | Generated files | Generated source size | Generator run min / mean (10 measured) | Diagnostics |");
        Console.WriteLine("| --- | ---: | ---: | ---: | ---: | ---: |");
        WriteGenerationRow("Representative 4/4 summary and decoder", 2, representative);
        WriteGenerationRow("Minimal 2/2 summary and decoder", 2, minimal);
        Console.WriteLine();
        Console.WriteLine("## Generated source size by declaration");
        Console.WriteLine();
        Console.WriteLine("| Sample | Declaration | Bytes | Lines |");
        Console.WriteLine("| --- | --- | ---: | ---: |");
        WriteGenerationFileRows("Representative 4/4", representative);
        WriteGenerationFileRows("Minimal 2/2", minimal);
        Console.WriteLine();
    }

    private static GenerationMetrics MeasureGeneration(string source)
    {
        var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
        var tree = CSharpSyntaxTree.ParseText(source, parseOptions);
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(static path => (MetadataReference)MetadataReference.CreateFromFile(path))
            .Append(MetadataReference.CreateFromFile(typeof(Summary).Assembly.Location))
            .ToArray();
        var compilation = CSharpCompilation.Create(
            "FusedGenerationCost",
            [tree],
            references,
            new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable));

        var samples = new List<double>(12);
        GeneratorDriverRunResult? last = null;
        for (var iteration = 0; iteration < 12; iteration++)
        {
            var stopwatch = Stopwatch.StartNew();
            GeneratorDriver driver = CSharpGeneratorDriver.Create(
                [new FusedExecutionGenerator().AsSourceGenerator()],
                parseOptions: parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);
            stopwatch.Stop();
            last = driver.GetRunResult();
            if (iteration >= 2)
            {
                samples.Add(stopwatch.Elapsed.TotalMilliseconds);
            }
        }

        var generated = last!.Results
            .SelectMany(static result => result.GeneratedSources)
            .Select(
                static item => new GeneratedFile(
                    item.HintName,
                    Encoding.UTF8.GetByteCount(item.SourceText.ToString()),
                    item.SourceText.Lines.Count))
            .ToArray();
        return new GenerationMetrics(generated, samples.Min(), samples.Average(), last.Diagnostics.Length);
    }

    private static void WriteGenerationRow(string name, int declarations, GenerationMetrics metrics) =>
        Console.WriteLine(
            $"| {name} | {declarations} | {metrics.Files.Length} | {metrics.Files.Sum(static file => file.Bytes):N0} B / {metrics.Files.Sum(static file => file.Lines)} lines | {metrics.Minimum:F2} / {metrics.Mean:F2} ms | {metrics.Diagnostics} |");

    private static void WriteGenerationFileRows(string sample, GenerationMetrics metrics)
    {
        foreach (var file in metrics.Files)
        {
            var kind = file.HintName.Contains("Attributes", StringComparison.Ordinal)
                ? "Injected attributes"
                : file.HintName.Contains(".Evaluate.", StringComparison.Ordinal)
                    ? "Summary declaration"
                    : file.HintName.Contains(".Decode.", StringComparison.Ordinal)
                        ? "Decoder declaration"
                        : file.HintName;
            Console.WriteLine($"| {sample} | {kind} | {file.Bytes:N0} | {file.Lines} |");
        }
    }

    private static void WriteRow(string name, Measurement measurement) =>
        Console.WriteLine(
            $"| {name} | {measurement.BytesPerOperation:N0} | {measurement.NanosecondsPerOperation:N0} |");

    private sealed record GeneratedFile(string HintName, int Bytes, int Lines);

    private sealed record GenerationMetrics(
        GeneratedFile[] Files,
        double Minimum,
        double Mean,
        int Diagnostics);
}
