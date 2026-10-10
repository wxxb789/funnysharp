using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
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

    internal static string SourceCommit(string root)
    {
        string Git(params string[] arguments)
        {
            var info = new System.Diagnostics.ProcessStartInfo("git")
            {
                WorkingDirectory = root,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            foreach (var argument in arguments)
            {
                info.ArgumentList.Add(argument);
            }

            using var process = System.Diagnostics.Process.Start(info)!;
            var output = process.StandardOutput.ReadToEndAsync();
            var error = process.StandardError.ReadToEndAsync();
            process.WaitForExit();
            Require(process.ExitCode == 0,
                "Cannot identify benchmark source state: " + error.GetAwaiter().GetResult());
            return output.GetAwaiter().GetResult().Trim();
        }

        Require(Git("status", "--porcelain", "--untracked-files=no").Length == 0,
            "Benchmark source requires a clean tracked Git working tree and index.");
        return Git("rev-parse", "HEAD");
    }

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
        var commit = Environment.GetEnvironmentVariable("FUNNYSHARP_PERFORMANCE_COMMIT");
        Require(!string.IsNullOrWhiteSpace(commit), "Measured child has no source commit.");
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
            candidateCommit = commit,
            capturedAtUtc = DateTimeOffset.UtcNow,
            processId = Environment.ProcessId,
            runtime = RuntimeInformation.FrameworkDescription,
            benchmarkClass = benchmarkType.Name,
            parameters = display,
            workloadAssembly = entry.FullName,
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
        var commit = SourceCommit(root);
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
                            semanticsChecked = true,
                        });
                    }
                }
            }
        }

        Require(validatedIds.SetEquals(included.Keys),
            "Preflight did not validate every included policy/parameter row.");
        var runId = Guid.NewGuid().ToString("N");
        Current = JsonSerializer.SerializeToElement(new
        {
            schemaVersion = 1,
            runId,
            completedAtUtc = DateTimeOffset.UtcNow,
            candidateCommit = commit,
            policyRevision = manifest.GetProperty("policy").GetProperty("revision").GetString(),
            runtime = RuntimeInformation.FrameworkDescription,
            rows = validated,
            semanticCases = semanticCases.OrderBy(value => value, StringComparer.Ordinal),
            excludedIds = excluded.Select(row => row.GetProperty("id").GetString())
                .OrderBy(value => value, StringComparer.Ordinal),
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
        Environment.SetEnvironmentVariable("FUNNYSHARP_PERFORMANCE_COMMIT", commit);
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

}
