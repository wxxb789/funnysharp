using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;

namespace FunnySharp.Benchmarks;

/// <summary>
/// The shared receipt-writing core for both performance suites. The main suite and the isolated
/// competitor suite each compile this file and pass their own exporter name, manifest path, and log
/// prefix. The single fingerprint algorithm matches <c>eng/Verify-Performance.ps1</c>: each manifest
/// files list is hashed ordinal-sorted with backslashes normalized to forward slashes, so the order
/// of a manifest's files array never changes a fingerprint.
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
        var policyFingerprint = manifest is null
            ? null
            : GetSha256(manifest.RootElement.GetProperty("policy").GetRawText());
        var benchmarkInputFingerprint = manifest is null
            ? null
            : GetFingerprint(repositoryRoot, manifest.RootElement.GetProperty("benchmarkInput").GetProperty("files"));
        var protocolFingerprint = manifest is null
            ? null
            : GetFingerprint(repositoryRoot, manifest.RootElement.GetProperty("protocol").GetProperty("files"));
        if (manifest is null)
        {
            throw new InvalidOperationException("Fresh measurement requires its registered manifest.");
        }
        BenchmarkPreflight.ValidateInputs(repositoryRoot, manifest.RootElement);
        var preflight = BenchmarkPreflight.Current;
        BenchmarkPreflight.Require(preflight.ValueKind == JsonValueKind.Object,
            "Measurement did not execute semantic preflight.");
        var snapshot = BenchmarkPreflight.Snapshot(
            manifest.RootElement, policyFingerprint!, benchmarkInputFingerprint!, protocolFingerprint!);
        BenchmarkPreflight.Require(
            preflight.GetProperty("candidateSnapshot").GetString() == snapshot,
            "Inputs changed after preflight.");
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
                var file = GetSha256(rowId) + "-launch-" + launchIndex++ + ".log";
                var launchPath = Path.Combine(summary.ResultsDirectoryPath, file);
                WriteImmutable(launchPath, string.Join(Environment.NewLine, lines) + Environment.NewLine);
                launches.Add(new { rowId, file, sha256 = GetFileSha256(launchPath) });
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
        var environmentKey = GetSha256(string.Join(
            "\0",
            environment.os,
            environment.architecture,
            environment.sdkVersion,
            environment.runtime,
            environment.jit,
            environment.gcServer.ToString().ToLowerInvariant(),
            environment.gcConcurrent.ToString().ToLowerInvariant(),
            environment.gcAllocationQuantum.ToString(System.Globalization.CultureInfo.InvariantCulture)));
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
            candidateCommit = snapshot,
            policyRevision,
            policyFingerprint,
            benchmarkInputFingerprint,
            protocolFingerprint,
            environmentKey,
            environment,
            reports,
            binding = new
            {
                preflight = new { file = preflightName, sha256 = GetFileSha256(preflightPath) },
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

            return new ReportEvidence(fileName, GetFileSha256(path));
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

    internal static string GetFingerprint(string repositoryRoot, JsonElement files)
    {
        using var input = new MemoryStream();
        foreach (var relativePath in files.EnumerateArray()
                     .Select(file => file.GetString() ?? string.Empty)
                     .OrderBy(path => path, StringComparer.Ordinal))
        {
            var path = Path.Combine(repositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Fingerprint input was not found: {relativePath}.", path);
            }

            var line = $"{relativePath.Replace('\\', '/')}\0{Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant()}\n";
            input.Write(Encoding.UTF8.GetBytes(line));
        }

        return Convert.ToHexString(SHA256.HashData(input.ToArray())).ToLowerInvariant();
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

    internal static string GetSha256(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    internal static string GetFileSha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

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

    private sealed record ReportEvidence(string File, string Sha256);

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
