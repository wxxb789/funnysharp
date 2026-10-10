using System.Reflection;
using System.Text;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;

namespace FunnySharp.Benchmarks;

/// <summary>
/// The shared receipt-writing core for both performance suites. The main suite and the isolated
/// competitor suite each compile this file and pass their exporter name, manifest path, and log prefix.
/// </summary>
internal abstract class ReceiptExporterCore : IExporter
{
    private readonly string name;
    private readonly string manifestRelativePath;
    private readonly string logPrefix;

    protected ReceiptExporterCore(string name, string manifestRelativePath, string logPrefix)
    {
        this.name = name;
        this.manifestRelativePath = manifestRelativePath;
        this.logPrefix = logPrefix;
    }

    public string Name => name;

    public void ExportToLog(Summary summary, ILogger logger)
    {
        var path = GetReceiptPath(summary);
        logger.WriteLineInfo($"{logPrefix}: {path}");
    }

    public IEnumerable<string> ExportToFiles(Summary summary, ILogger consoleLogger)
    {
        var path = GetReceiptPath(summary);
        var repositoryRoot = FindRepositoryRoot();
        var manifestPath = Path.Combine(repositoryRoot, manifestRelativePath);
        using var manifest = File.Exists(manifestPath) ? JsonDocument.Parse(File.ReadAllText(manifestPath)) : null;
        var policyRevision = manifest?.RootElement.GetProperty("policy").GetProperty("revision").GetString()
            ?? "unregistered";
        if (manifest is null)
        {
            throw new InvalidOperationException("Measurement requires its registered manifest.");
        }
        var preflight = BenchmarkPreflight.Current;
        BenchmarkPreflight.Require(preflight.ValueKind == JsonValueKind.Object,
            "Measurement did not execute semantic preflight.");
        var commit = preflight.GetProperty("candidateCommit").GetString();
        var preflightName = preflight.GetProperty("runId").GetString() + "-preflight.json";
        var preflightPath = Path.Combine(summary.ResultsDirectoryPath, preflightName);
        WriteImmutable(preflightPath, preflight.GetRawText() + Environment.NewLine);
        var launches = new List<object>();
        foreach (var report in summary.Reports)
        {
            var benchmark = report.BenchmarkCase;
            var rowId = CreateRowId(benchmark.Descriptor.Type.Name,
                benchmark.Descriptor.Categories.Single(),
                benchmark.Descriptor.WorkloadMethod.Name, benchmark.Parameters.DisplayInfo);
            var launchIndex = 0;
            foreach (var execution in report.ExecuteResults)
            {
                var lines = execution.StandardOutput.ToArray();
                BenchmarkPreflight.Require(
                    lines.Count(line => line.StartsWith(BenchmarkPreflight.Marker, StringComparison.Ordinal)) == 1,
                    "Each measured child launch must emit exactly one workload binding.");
                var file = preflight.GetProperty("runId").GetString() + "-" + benchmark.Descriptor.Type.Name
                    + "-" + launches.Count + "-launch-" + launchIndex++ + ".log";
                var launchPath = Path.Combine(summary.ResultsDirectoryPath, file);
                WriteImmutable(launchPath, string.Join(Environment.NewLine, lines) + Environment.NewLine);
                launches.Add(new { rowId, file });
            }
            BenchmarkPreflight.Require(launchIndex > 0, "A measured row has no child launch evidence.");
        }
        var host = summary.HostEnvironmentInfo;
        var environment = new
        {
            os = host.Os.Value.ToString(),
            architecture = host.Architecture,
            sdkVersion = host.DotNetSdkVersion.Value,
            runtime = host.RuntimeVersion,
            jit = host.JitInfo,
            gcServer = host.IsServerGC,
            gcConcurrent = host.IsConcurrentGC,
            gcAllocationQuantum = host.GCAllocationQuantum,
        };
        var reports = GetReportEvidence(summary);
        var rows = summary.Reports
            .Select(report => CreateRow(report))
            .OrderBy(row => row.BenchmarkClass, StringComparer.Ordinal)
            .ThenBy(row => row.Category, StringComparer.Ordinal)
            .ThenBy(row => row.Method, StringComparer.Ordinal)
            .ThenBy(row => row.Parameters, StringComparer.Ordinal)
            .ToArray();
        var receipt = new
        {
            schemaVersion = 1,
            generatedAtUtc = DateTimeOffset.UtcNow,
            succeeded = summary.Reports.All(report => report.Success),
            candidateCommit = commit,
            policyRevision,
            environment,
            reports,
            binding = new
            {
                preflight = new { file = preflightName },
                launches,
            },
            rows,
        };

        File.WriteAllText(
            path,
            JsonSerializer.Serialize(
                receipt,
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true,
                }) + Environment.NewLine);
        return [path];
    }

    private static string GetReceiptPath(Summary summary)
    {
        var benchmarkClass = summary.BenchmarksCases.Select(benchmark => benchmark.Descriptor.Type.Name).Distinct().Single();
        return Path.Combine(summary.ResultsDirectoryPath, $"{benchmarkClass}-performance-receipt.json");
    }

    private static ReportEvidence[] GetReportEvidence(Summary summary)
    {
        var benchmarkType = summary.BenchmarksCases.Select(benchmark => benchmark.Descriptor.Type).Distinct().Single();
        var prefix = benchmarkType.FullName ?? benchmarkType.Name;
        var fileNames = new[]
        {
            $"{prefix}-report.csv",
            $"{prefix}-report-github.md",
            $"{prefix}-report.html",
        };

        return fileNames.Select(fileName =>
        {
            var path = Path.Combine(summary.ResultsDirectoryPath, fileName);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Benchmark report was not found: {fileName}.", path);
            }

            return new ReportEvidence(fileName);
        }).ToArray();
    }

    internal static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "FunnySharp.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the FunnySharp repository root.");
    }

    private static ReceiptRow CreateRow(BenchmarkReport report)
    {
        var benchmarkCase = report.BenchmarkCase;
        var method = benchmarkCase.Descriptor.WorkloadMethod;
        var category = benchmarkCase.Descriptor.Categories.Single();
        var mean = report.ResultStatistics?.Mean;
        var timingState = mean is null
            ? "unavailable"
            : mean < 0.1
                ? "below-resolution"
                : "observed";

        var parameters = benchmarkCase.Parameters.DisplayInfo;
        var benchmarkClass = benchmarkCase.Descriptor.Type.Name;
        var methodName = method.Name;
        return new ReceiptRow(
            CreateRowId(benchmarkClass, category, methodName, parameters),
            benchmarkClass,
            category,
            methodName,
            parameters,
            method.GetCustomAttribute<BenchmarkAttribute>()?.Baseline is true,
            timingState,
            timingState == "observed" ? mean : null,
            report.GcStats.GetBytesAllocatedPerOperation(benchmarkCase));
    }

    private static string CreateRowId(string benchmarkClass, string category, string method, string parameters) =>
        string.Join('|', benchmarkClass, category, method, parameters);

    private static void WriteImmutable(string path, string text)
    {
        if (File.Exists(path))
        {
            BenchmarkPreflight.Require(File.ReadAllText(path) == text,
                "An immutable run artifact already exists with different content: " + path);
            return;
        }
        using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
        using var writer = new StreamWriter(stream, new UTF8Encoding(false));
        writer.Write(text);
    }

    private sealed record ReportEvidence(string File);

    private sealed record ReceiptRow(
        string Id,
        string BenchmarkClass,
        string Category,
        string Method,
        string Parameters,
        bool Baseline,
        string TimingState,
        double? MeanNanoseconds,
        long? AllocatedBytesPerOperation);
}
