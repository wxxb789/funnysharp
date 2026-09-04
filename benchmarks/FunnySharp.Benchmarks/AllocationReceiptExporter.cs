using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;

namespace FunnySharp.Benchmarks;

internal sealed class AllocationReceiptExporter : IExporter
{
    public string Name => nameof(AllocationReceiptExporter);

    public void ExportToLog(Summary summary, ILogger logger)
    {
        var path = GetReceiptPath(summary);
        logger.WriteLineInfo($"Performance receipt: {path}");
    }

    public IEnumerable<string> ExportToFiles(Summary summary, ILogger consoleLogger)
    {
        var path = GetReceiptPath(summary);
        var repositoryRoot = FindRepositoryRoot();
        var manifestPath = Path.Combine(repositoryRoot, "eng", "performance", "baseline.json");
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
            candidateCommit = Environment.GetEnvironmentVariable("FUNNYSHARP_CANDIDATE_COMMIT"),
            policyRevision,
            policyFingerprint,
            benchmarkInputFingerprint,
            protocolFingerprint,
            environmentKey,
            environment,
            reports,
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

    private static string FindRepositoryRoot()
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

    private static string GetFingerprint(string repositoryRoot, JsonElement files)
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

            var line = $"{relativePath}\0{Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant()}\n";
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

    private static string GetSha256(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private static string GetFileSha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
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
