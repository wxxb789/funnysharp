using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Xml.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using FunnySharp;

namespace FunnySharp.Benchmarks;

// Shared by the two existing suites, not a second measurement harness.
internal static class BenchmarkPreflight
{
    internal const string Marker = "// funnysharp-workload:";
    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    internal static JsonElement Current { get; private set; }

    internal static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    internal static string Snapshot(
        JsonElement manifest, string policy, string input, string protocol)
    {
        var binding = manifest.GetProperty("runBinding");
        var commit = binding.GetProperty("baseCommit").GetString()!;
        var tree = binding.GetProperty("baseTree").GetString()!;
        Require(commit.Length == 40 && commit.All(Uri.IsHexDigit)
            && tree.Length == 40 && tree.All(Uri.IsHexDigit),
            "A source snapshot requires the declared base commit and tree.");
        return "snapshot:" + ReceiptExporterCore.GetSha256(
            string.Join("\0", commit, tree, policy, input, protocol));
    }

    internal static void ValidateInputs(string root, JsonElement manifest)
    {
        var listed = manifest.GetProperty("benchmarkInput").GetProperty("files")
            .EnumerateArray().Select(file => file.GetString()!)
            .ToHashSet(StringComparer.Ordinal);
        var project = manifest.GetProperty("runBinding")
            .GetProperty("benchmarkProject").GetString()!;
        var benchmarkDirectory = Path.GetDirectoryName(Path.Combine(root, project))!;
        var required = Directory.EnumerateFiles(
                Path.Combine(root, "src"), "*", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(
                benchmarkDirectory, "*", SearchOption.AllDirectories))
            .Where(path => !Path.GetRelativePath(root, path).Replace('\\', '/')
                .Split('/').Any(part => part is "bin" or "obj"))
            .Where(IsBuildInput)
            .Concat(Directory.EnumerateFiles(root, "*.props"))
            .Concat(Directory.EnumerateFiles(root, "*.targets"))
            .Concat(new[]
            {
                Path.Combine(root, "Directory.Build.props"),
                Path.Combine(root, "README.md"),
                Path.Combine(root, "global.json"),
            })
            .Select(path => Path.GetRelativePath(root, path).Replace('\\', '/'))
            .ToHashSet(StringComparer.Ordinal);

        // Current projects have no explicit imports. If one is introduced, follow
        // literal repository imports; unresolved property/glob imports fail closed.
        var pending = new Queue<string>(required.Where(IsProjectInput));
        var visited = new HashSet<string>(StringComparer.Ordinal);
        while (pending.TryDequeue(out var relative))
        {
            if (!visited.Add(relative))
            {
                continue;
            }

            var path = Path.Combine(root, relative);
            foreach (var import in XDocument.Load(path).Descendants()
                .Where(element => element.Name.LocalName == "Import"))
            {
                var value = import.Attribute("Project")!.Value.Replace(
                    "$(MSBuildThisFileDirectory)",
                    Path.GetDirectoryName(path)! + Path.DirectorySeparatorChar,
                    StringComparison.Ordinal);
                Require(!value.Contains("$(", StringComparison.Ordinal)
                    && !value.Contains('*') && !value.Contains('?'),
                    "An explicit benchmark build import must have a resolvable repository path.");
                var imported = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, value));
                var importedRelative = Path.GetRelativePath(root, imported).Replace('\\', '/');
                Require(!importedRelative.Split('/').Contains("..")
                    && !Path.IsPathFullyQualified(importedRelative) && File.Exists(imported),
                    "An explicit benchmark build import must exist inside the repository.");
                required.Add(importedRelative);
                pending.Enqueue(importedRelative);
            }
        }

        Require(required.All(listed.Contains),
            "The benchmark input list omits shipping, project, import, or lock inputs: "
            + string.Join(", ", required.Where(file => !listed.Contains(file))
                .OrderBy(file => file, StringComparer.Ordinal)));
    }

    private static bool IsProjectInput(string path) =>
        path.EndsWith(".csproj", StringComparison.Ordinal)
        || path.EndsWith(".props", StringComparison.Ordinal)
        || path.EndsWith(".targets", StringComparison.Ordinal);

    private static bool IsBuildInput(string path) =>
        IsProjectInput(path)
        || path.EndsWith(".cs", StringComparison.Ordinal)
        || Path.GetFileName(path) == "packages.lock.json"
        || Path.GetFileName(path).StartsWith("AnalyzerReleases.", StringComparison.Ordinal);

    internal static BinaryBinding ReadBinary(string path)
    {
        path = Path.GetFullPath(path);
        using var stream = File.OpenRead(path);
        using var pe = new PEReader(stream);
        string? mvid = null;
        string? identity = null;
        if (pe.HasMetadata)
        {
            var metadata = pe.GetMetadataReader();
            mvid = metadata.GetGuid(metadata.GetModuleDefinition().Mvid).ToString();
            identity = AssemblyName.GetAssemblyName(path).FullName;
        }

        return new BinaryBinding(path, ReceiptExporterCore.GetFileSha256(path), mvid, identity);
    }

    private static BinaryBinding RetainBinary(string root, string runId, string path)
    {
        var actual = ReadBinary(path);
        var directory = Path.Combine(root, "artifacts", "performance-preflight", runId,
            "binaries", actual.Sha256);
        Directory.CreateDirectory(directory);
        var retained = Path.Combine(directory, Path.GetFileName(actual.File));
        if (!File.Exists(retained))
        {
            File.Copy(actual.File, retained);
        }

        Require(ReceiptExporterCore.GetFileSha256(retained) == actual.Sha256,
            "A retained workload or private assembly differs from its loaded binary.");
        return actual with { File = retained };
    }

    private static BinaryBinding[] ReadPrivateBinaries(string root, string runId) =>
        Directory.EnumerateFiles(AppContext.BaseDirectory, "*.dll")
            .OrderBy(path => Path.GetFileName(path), StringComparer.Ordinal)
            .Select(path => RetainBinary(root, runId, path)).ToArray();

    internal static void CaptureChild(object benchmark)
    {
        var runId = Environment.GetEnvironmentVariable("FUNNYSHARP_PERFORMANCE_RUN");
        if (string.IsNullOrEmpty(runId))
        {
            return; // Host preflight, before BenchmarkSwitcher starts any child.
        }

        var entry = Assembly.GetEntryAssembly()!;
        Require(entry != typeof(BenchmarkPreflight).Assembly,
            "Fresh measurement requires a BenchmarkDotNet child, not the exporter host.");
        var benchmarkType = benchmark.GetType().GetMethods()
            .First(method => method.GetCustomAttribute<BenchmarkAttribute>() is not null)
            .DeclaringType!;
        var root = ReceiptExporterCore.FindRepositoryRoot();
        var manifestRelative = Environment.GetEnvironmentVariable("FUNNYSHARP_PERFORMANCE_MANIFEST")!;
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, manifestRelative)));
        var manifest = document.RootElement;
        ValidateInputs(root, manifest);
        var policy = ReceiptExporterCore.GetSha256(manifest.GetProperty("policy").GetRawText());
        var input = ReceiptExporterCore.GetFingerprint(
            root, manifest.GetProperty("benchmarkInput").GetProperty("files"));
        var protocol = ReceiptExporterCore.GetFingerprint(
            root, manifest.GetProperty("protocol").GetProperty("files"));
        var snapshot = Snapshot(manifest, policy, input, protocol);
        Require(snapshot == Environment.GetEnvironmentVariable("FUNNYSHARP_PERFORMANCE_SNAPSHOT"),
            "Source inputs changed between preflight and the measured child.");
        var parameters = benchmarkType.GetProperties()
            .Where(property => property.GetCustomAttribute<ParamsAttribute>() is not null)
            .OrderBy(property => property.Name, StringComparer.Ordinal).ToArray();
        var display = parameters.Length == 0 ? "" : "["
            + string.Join(", ", parameters.Select(property =>
                property.Name + "=" + Convert.ToString(
                    property.GetValue(benchmark), System.Globalization.CultureInfo.InvariantCulture)))
            + "]";
        var binding = JsonSerializer.Serialize(new
        {
            runId,
            candidateSnapshot = snapshot,
            capturedAtUtc = DateTimeOffset.UtcNow,
            processId = Environment.ProcessId,
            runtime = RuntimeInformation.FrameworkDescription,
            benchmarkClass = benchmarkType.Name,
            parameters = display,
            originalWorkloadFile = entry.Location,
            workload = RetainBinary(root, runId, entry.Location),
            assemblies = ReadPrivateBinaries(root, runId),
        }, JsonOptions);
        Console.WriteLine(Marker + Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes(binding)));
    }

    internal static async Task RunAsync(
        Assembly assembly, string manifestRelative, int expectedRows, int expectedExclusions)
    {
        Environment.SetEnvironmentVariable("FUNNYSHARP_PERFORMANCE_RUN", null);
        var root = ReceiptExporterCore.FindRepositoryRoot();
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, manifestRelative)));
        var manifest = document.RootElement;
        ValidateInputs(root, manifest);
        var policy = ReceiptExporterCore.GetSha256(manifest.GetProperty("policy").GetRawText());
        var input = ReceiptExporterCore.GetFingerprint(
            root, manifest.GetProperty("benchmarkInput").GetProperty("files"));
        var protocol = ReceiptExporterCore.GetFingerprint(
            root, manifest.GetProperty("protocol").GetProperty("files"));
        var snapshot = Snapshot(manifest, policy, input, protocol);
        var allPolicyRows = manifest.GetProperty("policy").GetProperty("rows").EnumerateArray().ToArray();
        var included = allPolicyRows.Where(row => row.GetProperty("included").GetBoolean())
            .ToDictionary(row => row.GetProperty("id").GetString()!, StringComparer.Ordinal);
        var excluded = allPolicyRows.Where(row => !row.GetProperty("included").GetBoolean()).ToArray();
        Require(included.Count == expectedRows && excluded.Length == expectedExclusions,
            "The full benchmark policy/parameter inventory changed.");
        Require(excluded.All(row => !string.IsNullOrWhiteSpace(
            row.GetProperty("exclusionReason").GetString())), "An exclusion lacks its rationale.");
        var validated = new List<object>();
        var validatedIds = new HashSet<string>(StringComparer.Ordinal);
        var semanticCases = new List<string>();

        foreach (var type in assembly.GetTypes().Where(type =>
            type.GetMethods().Any(method => method.GetCustomAttribute<BenchmarkAttribute>() is not null))
            .OrderBy(type => type.Name, StringComparer.Ordinal))
        {
            var cases = BenchmarkConverter.TypeToBenchmarks(type).BenchmarksCases;
            foreach (var parameterGroup in cases.GroupBy(benchmark => benchmark.Parameters.DisplayInfo))
            {
                var instance = Activator.CreateInstance(type)!;
                foreach (var parameter in parameterGroup.First().Parameters.Items)
                {
                    type.GetProperty(parameter.Name)!.SetValue(instance, parameter.Value);
                }

                var setup = type.GetMethods().SingleOrDefault(method =>
                    method.GetCustomAttribute<GlobalSetupAttribute>() is not null);
                setup?.Invoke(instance, null);
                var full = type.GetMethod("ValidateFullSemanticsAsync",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (full is not null)
                {
                    await ((Task)full.Invoke(instance, null)!).ConfigureAwait(false);
                }

                semanticCases.Add(type.Name + "|" + parameterGroup.Key);
                foreach (var group in parameterGroup.GroupBy(benchmark =>
                    benchmark.Descriptor.Categories.Single()))
                {
                    var baseline = group.Single(benchmark =>
                        benchmark.Descriptor.WorkloadMethod
                            .GetCustomAttribute<BenchmarkAttribute>()!.Baseline);
                    var expected = await InvokeAndNormalizeAsync(
                        instance, baseline.Descriptor.WorkloadMethod, group.Key).ConfigureAwait(false);
                    foreach (var benchmark in group)
                    {
                        var method = benchmark.Descriptor.WorkloadMethod;
                        var id = string.Join("|", type.Name, group.Key, method.Name, parameterGroup.Key);
                        Require(included.TryGetValue(id, out var policyRow),
                            "A reflected benchmark/parameter row is missing from policy: " + id);
                        Require(validatedIds.Add(id), "A benchmark row was validated twice: " + id);
                        var baselineFlag = method.GetCustomAttribute<BenchmarkAttribute>()!.Baseline;
                        Require(policyRow.GetProperty("benchmarkClass").GetString() == type.Name
                            && policyRow.GetProperty("category").GetString() == group.Key
                            && policyRow.GetProperty("method").GetString() == method.Name
                            && policyRow.GetProperty("parameters").GetString() == parameterGroup.Key
                            && policyRow.GetProperty("baseline").GetBoolean() == baselineFlag,
                            "Policy does not match the actual benchmark descriptor: " + id);
                        var actual = benchmark == baseline ? expected
                            : await InvokeAndNormalizeAsync(instance, method, group.Key).ConfigureAwait(false);
                        Require(actual == expected, "Benchmark semantic outputs differ: " + id);
                        validated.Add(new
                        {
                            id,
                            benchmarkClass = type.Name,
                            category = group.Key,
                            method = method.Name,
                            parameters = parameterGroup.Key,
                            baseline = baselineFlag,
                            outcomeSha256 = ReceiptExporterCore.GetSha256(actual),
                        });
                    }
                }
            }
        }

        Require(validatedIds.SetEquals(included.Keys),
            "Preflight did not validate every included policy/parameter row.");
        Require(input == ReceiptExporterCore.GetFingerprint(
            root, manifest.GetProperty("benchmarkInput").GetProperty("files"))
            && protocol == ReceiptExporterCore.GetFingerprint(
                root, manifest.GetProperty("protocol").GetProperty("files")),
            "Inputs changed during semantic preflight.");
        var runId = Guid.NewGuid().ToString("N");
        var binding = manifest.GetProperty("runBinding");
        Current = JsonSerializer.SerializeToElement(new
        {
            schemaVersion = 1,
            runId,
            completedAtUtc = DateTimeOffset.UtcNow,
            candidateSnapshot = snapshot,
            baseCommit = binding.GetProperty("baseCommit").GetString(),
            baseTree = binding.GetProperty("baseTree").GetString(),
            policyFingerprint = policy,
            benchmarkInputFingerprint = input,
            protocolFingerprint = protocol,
            runtime = RuntimeInformation.FrameworkDescription,
            rows = validated,
            semanticCases = semanticCases.OrderBy(value => value, StringComparer.Ordinal),
            excludedIds = excluded.Select(row => row.GetProperty("id").GetString())
                .OrderBy(value => value, StringComparer.Ordinal),
            assemblies = ReadPrivateBinaries(root, runId),
        }, JsonOptions);
        var evidenceDirectory = Path.Combine(root, "artifacts", "performance-preflight", runId);
        Directory.CreateDirectory(evidenceDirectory);
        var evidencePath = Path.Combine(evidenceDirectory, "preflight.json");
        using (var stream = new FileStream(evidencePath, FileMode.CreateNew, FileAccess.Write))
        using (var writer = new StreamWriter(stream, new System.Text.UTF8Encoding(false)))
        {
            writer.Write(Current.GetRawText());
        }
        Console.WriteLine("PREFLIGHT_RECEIPT " + evidencePath);
        Environment.SetEnvironmentVariable("FUNNYSHARP_PERFORMANCE_RUN", runId);
        Environment.SetEnvironmentVariable("FUNNYSHARP_PERFORMANCE_MANIFEST", manifestRelative);
        Environment.SetEnvironmentVariable("FUNNYSHARP_PERFORMANCE_SNAPSHOT", snapshot);
    }

    private static async Task<string> InvokeAndNormalizeAsync(
        object instance, MethodInfo method, string category)
    {
        var value = method.Invoke(instance, null);
        if (value is not null && value.GetType().IsGenericType
            && value.GetType().GetGenericTypeDefinition() == typeof(ValueTask<>))
        {
            value = value.GetType().GetMethod("AsTask")!.Invoke(value, null);
        }

        if (value is Task task)
        {
            await task.ConfigureAwait(false);
            value = task.GetType().GetProperty("Result")!.GetValue(task);
        }

        var custom = instance.GetType().GetMethod("NormalizePreflightAsync",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (custom is not null)
        {
            value = await ((Task<object?>)custom.Invoke(instance, new[] { value })!)
                .ConfigureAwait(false);
        }
        else
        {
            value = value switch
            {
                Func<int, int> function => new[] { function(-1), function(0), function(42) },
                Result<int, string> result => result.TryGetValue(out var item)
                    ? new { valid = true, value = item, errors = Array.Empty<string>() }
                    : new { valid = false, value = 0, errors = new[] { result.Match(_ => "", error => error) } },
                Option<IReadOnlyList<int>> option => option.TryGetValue(out var items)
                    ? new { present = true, values = items.ToArray() }
                    : new { present = false, values = Array.Empty<int>() },
                Validation<IReadOnlyList<int>, string> validation =>
                    validation.TryGetValue(out var items)
                        ? new { valid = true, values = items.ToArray(), errors = Array.Empty<string>() }
                        : new
                        {
                            valid = false,
                            values = Array.Empty<int>(),
                            errors = validation.Match(_ => Array.Empty<string>(), errors => errors.ToArray())
                        },
                _ => value,
            };
        }

        if (category == "Completion-order bounded asynchronous map")
        {
            // Independent executions may observe different simultaneous-completion ties.
            value = ((int[])value!).Order().ToArray();
        }

        return JsonSerializer.Serialize(value, value?.GetType() ?? typeof(object), JsonOptions);
    }

    internal sealed record BinaryBinding(string File, string Sha256, string? Mvid, string? Identity);
}
