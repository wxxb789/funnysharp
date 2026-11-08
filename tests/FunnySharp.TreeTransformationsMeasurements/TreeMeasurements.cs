using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using FunnySharp.TestSupport;
using FunnySharp.TestSupport.TreeComparison;
using FunnySharp.TreeTransformations;
using static FunnySharp.TestSupport.MeasurementHarness;

namespace FunnySharp.TreeTransformationsMeasurements;

internal static class TreeMeasurements
{
    private static readonly ConfigNode BaseTree = ConfigFixtures.CreateBase();
    private static readonly IReadOnlyList<ConfigOverride> ValidOverrides = ConfigFixtures.CreateValidOverrides();
    private static readonly IReadOnlyList<ConfigOverride> MixedOverrides = ConfigFixtures.CreateMixedOverrides();
    private static readonly ConfigOverride SingleOverride = new(
        "services[3].endpoints[0].port",
        TreePath.Parse("services[3].endpoints[0].port"),
        "9999");

    internal static void Run()
    {
        WriteEnvironment();
        var checks = TreeComparison.Verify();
        Console.WriteLine($"parity: verified {checks} checks across recursion, iterative, generic full, generic path, zipper, and JsonNode consumers.");
        Console.WriteLine();
        WriteWorkloadShape();
        WriteLineCounts();
        WriteStats();
        WriteAllocation();
        WriteDeepStack();
        WriteDeepInProcess();
        WriteDagCycleMutable();
    }

    private static void WriteEnvironment()
    {
        Console.WriteLine("## Environment");
        Console.WriteLine();
        Console.WriteLine($"runtime: {RuntimeInformation.FrameworkDescription}");
        Console.WriteLine($"os: {RuntimeInformation.OSDescription}");
        Console.WriteLine($"architecture: {RuntimeInformation.ProcessArchitecture}");
        Console.WriteLine($"cores: {Environment.ProcessorCount}");
        Console.WriteLine($"utc: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"stopwatch frequency: {Stopwatch.Frequency.ToString(CultureInfo.InvariantCulture)}");
        Console.WriteLine();
    }

    private static void WriteWorkloadShape()
    {
        Console.WriteLine("## Workload shape");
        Console.WriteLine();
        Console.WriteLine($"base nodes: {CountNodes(BaseTree)} (constant {ConfigFixtures.BaseNodeCount})");
        Console.WriteLine($"base leaves: {CountLeaves(BaseTree)} (constant {ConfigFixtures.BaseLeafCount})");
        Console.WriteLine($"base depth: {Depth(BaseTree)}");
        Console.WriteLine($"valid overrides: {ValidOverrides.Count}");
        Console.WriteLine($"mixed overrides: {MixedOverrides.Count}");
        Console.WriteLine();
    }

    private static void WriteLineCounts()
    {
        Console.WriteLine("## Consumer source volume (raw lines, braces and comments included)");
        Console.WriteLine();
        Console.WriteLine("| Artifact | Lines |");
        Console.WriteLine("| --- | ---: |");

        var root = TestRepositoryRoot.Find();

        if (root is null)
        {
            Console.WriteLine("| unavailable | - |");
            Console.WriteLine();
            return;
        }

        var entries = new (string Name, string[] Paths)[]
        {
            ("Prototype", [Path.Combine(root, "tests/FunnySharp.TreeTransformations")]),
            ("Adapter", [Path.Combine(root, "tests/Shared/TreeComparison/ConfigTreeAdapter.cs")]),
            ("Direct recursion", [Path.Combine(root, "tests/Shared/TreeComparison/RecursiveConfigEditor.cs")]),
            ("Iterative explicit stack", [Path.Combine(root, "tests/Shared/TreeComparison/IterativeConfigEditor.cs")]),
            ("Generic full rewrite", [Path.Combine(root, "tests/Shared/TreeComparison/GenericFullConfigEditor.cs")]),
            ("Generic path rewrite", [Path.Combine(root, "tests/Shared/TreeComparison/GenericPathConfigEditor.cs")]),
            ("Zipper", [Path.Combine(root, "tests/Shared/TreeComparison/ZipperConfigEditor.cs")]),
            ("JsonNode", [Path.Combine(root, "tests/Shared/TreeComparison/JsonNodeConfigEditor.cs")]),
            (
                "Shared workload and verifier",
                [
                    Path.Combine(root, "tests/Shared/TreeComparison/ConfigNode.cs"),
                    Path.Combine(root, "tests/Shared/TreeComparison/ConfigOverride.cs"),
                    Path.Combine(root, "tests/Shared/TreeComparison/ConfigError.cs"),
                    Path.Combine(root, "tests/Shared/TreeComparison/ConfigEditStats.cs"),
                    Path.Combine(root, "tests/Shared/TreeComparison/ConfigKernel.cs"),
                    Path.Combine(root, "tests/Shared/TreeComparison/ConfigFixtures.cs"),
                    Path.Combine(root, "tests/Shared/TreeComparison/ConfigErrors.cs"),
                    Path.Combine(root, "tests/Shared/TreeComparison/IConfigEditor.cs"),
                    Path.Combine(root, "tests/Shared/TreeComparison/TreeComparison.cs"),
                ]),
            ("Tests", [Path.Combine(root, "tests/FunnySharp.TreeTransformations.Tests")]),
        };

        foreach (var (name, paths) in entries)
        {
            var lines = paths.Sum(CountLines);
            Console.WriteLine($"| {name} | {lines} |");
        }

        Console.WriteLine();
    }

    private static void WriteStats()
    {
        WriteApplyStats("Single valid override", [SingleOverride]);
        WriteApplyStats("32 valid overrides", ValidOverrides);
        WriteApplyStats("Mixed overlay (four errors)", MixedOverrides);

        Console.WriteLine("## Work: full validation");
        Console.WriteLine();
        Console.WriteLine("| Consumer | Visited | Rebuilt | Shared | Applied | Errors | Outcome |");
        Console.WriteLine("| --- | ---: | ---: | ---: | ---: | ---: | --- |");

        foreach (var editor in CreateEditors(BaseTree))
        {
            editor.ResetStats();
            var result = editor.Validate();
            var stats = editor.Stats;
            Console.WriteLine(
                $"| {editor.Name} | {stats.VisitedNodes} | {stats.RebuiltNodes} | {stats.SharedNodes} | "
                + $"{stats.OverridesApplied} | {stats.Errors} | {(result.TryGetValue(out _) ? "valid" : "invalid")} |");
        }

        Console.WriteLine();
    }

    private static void WriteApplyStats(string title, IReadOnlyList<ConfigOverride> overrides)
    {
        Console.WriteLine($"## Work: {title}");
        Console.WriteLine();
        Console.WriteLine("| Consumer | Visited | Rebuilt | Shared | Applied | Errors | Outcome |");
        Console.WriteLine("| --- | ---: | ---: | ---: | ---: | ---: | --- |");

        foreach (var editor in CreateEditors(BaseTree))
        {
            editor.ResetStats();
            var result = editor.Apply(overrides);
            var stats = editor.Stats;
            Console.WriteLine(
                $"| {editor.Name} | {stats.VisitedNodes} | {stats.RebuiltNodes} | {stats.SharedNodes} | "
                + $"{stats.OverridesApplied} | {stats.Errors} | {(result.TryGetValue(out _) ? "valid" : "invalid")} |");
        }

        Console.WriteLine();
    }

    private static void WriteAllocation()
    {
        Console.WriteLine("## Allocation and directional time: complete apply plus validation");
        Console.WriteLine();
        Console.WriteLine("| Scenario | Consumer | Allocated B/op | Time ns/op |");
        Console.WriteLine("| --- | --- | ---: | ---: |");

        WriteAllocationRow("Single valid override", [SingleOverride]);
        WriteAllocationRow("32 valid overrides", ValidOverrides);
        WriteAllocationRow("Mixed overlay", MixedOverrides);
        WriteValidationRow();
        Console.WriteLine();
    }

    private static void WriteAllocationRow(string scenario, IReadOnlyList<ConfigOverride> overrides)
    {
        foreach (var editor in CreateEditors(BaseTree))
        {
            var iterations = Iterations(editor.Name, scenario);
            var measurement = Measure(
                () => Checksum(editor, overrides),
                iterations,
                Math.Max(1, iterations / 10));

            Console.WriteLine(
                $"| {scenario} | {editor.Name} | {Format(measurement.BytesPerOperation)} | "
                + $"{Format(measurement.NanosecondsPerOperation)} |");
        }
    }

    private static void WriteValidationRow()
    {
        foreach (var editor in CreateEditors(BaseTree))
        {
            var iterations = Iterations(editor.Name, "Full validation");
            var measurement = Measure(
                () => ValidationChecksum(editor),
                iterations,
                Math.Max(1, iterations / 10));

            Console.WriteLine(
                $"| Full validation | {editor.Name} | {Format(measurement.BytesPerOperation)} | "
                + $"{Format(measurement.NanosecondsPerOperation)} |");
        }
    }

    private static void WriteDeepStack()
    {
        Console.WriteLine("## Recursive depth probe (child process)");
        Console.WriteLine();
        Console.WriteLine("| Depth | Outcome | Detail |");
        Console.WriteLine("| ---: | --- | --- |");

        foreach (var depth in new[] { 1_000, 5_000, 10_000, 20_000, 50_000, 100_000 })
        {
            var (outcome, detail) = ProbeDepth(depth);
            Console.WriteLine($"| {depth} | {outcome} | {detail} |");
        }

        Console.WriteLine();
    }

    private static void WriteDeepInProcess()
    {
        const int depth = 100_000;
        Console.WriteLine("## Deep path at 100,000 sections (one operation per consumer)");
        Console.WriteLine();
        Console.WriteLine("| Consumer | Result | Allocated B | Time ms |");
        Console.WriteLine("| --- | --- | ---: | ---: |");

        var tree = ConfigFixtures.CreateDeep(depth);
        var item = ConfigFixtures.CreateDeepOverride(depth);

        foreach (var editor in new IConfigEditor[]
        {
            new IterativeConfigEditor(tree),
            new GenericPathConfigEditor(tree),
            new ZipperConfigEditor(tree),
        })
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            var before = GC.GetAllocatedBytesForCurrentThread();
            var stopwatch = Stopwatch.StartNew();
            var result = editor.Apply([item]);
            stopwatch.Stop();
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            var ok = result.TryGetValue(out var actual) && DeepLeafValue(actual!, item) == "2";
            Console.WriteLine(
                $"| {editor.Name} | {(ok ? "ok" : "failed")} | {allocated} | "
                + $"{stopwatch.Elapsed.TotalMilliseconds.ToString("F1", CultureInfo.InvariantCulture)} |");
        }

        Console.WriteLine();
    }

    private static void WriteDagCycleMutable()
    {
        Console.WriteLine("## Tree, DAG, cycle, and mutable-content boundaries");
        Console.WriteLine();
        Console.WriteLine("| Case | Observation |");
        Console.WriteLine("| --- | --- |");

        var dag = ConfigFixtures.CreateDag();
        var treeVisit = TreeTraversal.Traverse(
            dag,
            ConfigTreeAdapter.Instance,
            (node, depth, path) => TreeVisitDecision.Continue);
        var dagVisit = TreeTraversal.Traverse(
            dag,
            ConfigTreeAdapter.Instance,
            (node, depth, path) => TreeVisitDecision.Continue,
            new TreeTraversalOptions<ConfigNode> { Shape = TreeShape.Dag });
        var treeRewrite = TreeRewrite.RewriteBottomUp(
            dag,
            ConfigTreeAdapter.Instance,
            (node, original, rewritten, path) => ChangeValueLeaf(node, original, rewritten),
            new TreeTraversalOptions<ConfigNode> { Shape = TreeShape.Tree });
        var dagRewrite = TreeRewrite.RewriteBottomUp(
            dag,
            ConfigTreeAdapter.Instance,
            (node, original, rewritten, path) => ChangeValueLeaf(node, original, rewritten),
            new TreeTraversalOptions<ConfigNode> { Shape = TreeShape.Dag });
        var dagEdit = TreeRewrite.RewriteAt(
            dag,
            ConfigTreeAdapter.Instance,
            TreePath.Parse("items[0].value"),
            node => node with { Value = "2" });

        Console.WriteLine($"| DAG traversal, tree shape | {treeVisit.VisitedNodes} visits |");
        Console.WriteLine(
            $"| DAG traversal, DAG shape | {dagVisit.VisitedNodes} visits, "
            + $"{dagVisit.DuplicateVisits} duplicate |");
        Console.WriteLine(
            $"| DAG rewrite, tree shape | "
            + $"{(ReferenceEquals(treeRewrite.Root.Children[0].Children[0], treeRewrite.Root.Children[0].Children[1]) ? "shared" : "two instances")} |");
        Console.WriteLine(
            $"| DAG rewrite, DAG shape | "
            + $"{(ReferenceEquals(dagRewrite.Root.Children[0].Children[0], dagRewrite.Root.Children[0].Children[1]) ? "shared" : "two instances")} |");
        dagEdit.TryGetValue(out var dagRewriteResult);
        Console.WriteLine(
            $"| DAG path edit | {dagRewriteResult.RebuiltNodes} rebuilt, {dagRewriteResult.SharedNodes} shared |");

        var (cycle, _) = ConfigFixtures.CreateCycle();
        var cycleThrows = false;

        try
        {
            TreeTraversal.Traverse(
                cycle,
                ConfigTreeAdapter.Instance,
                (node, depth, path) => TreeVisitDecision.Continue);
        }
        catch (TreeCycleException)
        {
            cycleThrows = true;
        }

        var cycleStops = TreeTraversal.Traverse(
            cycle,
            ConfigTreeAdapter.Instance,
            (node, depth, path) => TreeVisitDecision.Continue,
            new TreeTraversalOptions<ConfigNode> { CyclePolicy = TreeCyclePolicy.Stop });

        Console.WriteLine($"| Cycle traversal | {(cycleThrows ? "throws" : "did not throw")} |");
        Console.WriteLine(
            $"| Cycle stop policy | detected={cycleStops.CycleDetected}, visits={cycleStops.VisitedNodes} |");

        var (mutableRoot, shared, mutableChildren) = ConfigFixtures.CreateMutableShared();
        var before = ConfigKernel.Digest(mutableRoot);
        mutableChildren.Add(ConfigNode.Leaf("extra", "x"));
        var after = ConfigKernel.Digest(mutableRoot);
        Console.WriteLine(
            $"| Mutable child list | digest changed={before != after}, shared children={shared.Children.Count} |");

        Console.WriteLine();
    }

    private static ConfigNode ChangeValueLeaf(
        ConfigNode node,
        IReadOnlyList<ConfigNode> original,
        IReadOnlyList<ConfigNode> rewritten)
    {
        if (!node.IsSection && node.Name == "value" && node.Value == "1")
        {
            return node with { Value = "2" };
        }

        var changed = false;

        for (var index = 0; index < original.Count; index++)
        {
            if (!ReferenceEquals(original[index], rewritten[index]))
            {
                changed = true;
                break;
            }
        }

        return changed ? ConfigTreeAdapter.Instance.Rebuild(node, rewritten) : node;
    }

    private static int Checksum(IConfigEditor editor, IReadOnlyList<ConfigOverride> overrides)
    {
        var result = editor.Apply(overrides);

        if (result.TryGetValue(out var tree))
        {
            return unchecked((int)ConfigKernel.Digest(tree));
        }

        result.TryGetErrors(out var errors);
        var hash = 17;

        foreach (var error in errors!)
        {
            hash = unchecked((hash * 31) + StringComparer.Ordinal.GetHashCode(error.Path));
        }

        return hash;
    }

    private static int ValidationChecksum(IConfigEditor editor)
    {
        var result = editor.Validate();

        if (result.TryGetValue(out var tree))
        {
            return unchecked((int)ConfigKernel.Digest(tree));
        }

        result.TryGetErrors(out var errors);
        var hash = 17;

        foreach (var error in errors!)
        {
            hash = unchecked((hash * 31) + StringComparer.Ordinal.GetHashCode(error.Path));
        }

        return hash;
    }

    private static int Iterations(string consumer, string scenario) => consumer switch
    {
        "JsonNode" => scenario == "Full validation" ? 50 : 20,
        "Generic full rewrite" => scenario == "Full validation" ? 100 : 20,
        _ => scenario == "Full validation" ? 500 : 100,
    };

    private static string DeepLeafValue(ConfigNode tree, ConfigOverride item)
    {
        var resolved = TreeRewrite.Resolve(tree, ConfigTreeAdapter.Instance, item.Path);

        return resolved.TryGetValue(out var node) ? node.Node.Value! : "<missing>";
    }

    private static (string Outcome, string Detail) ProbeDepth(int depth)
    {
        var executable = Environment.ProcessPath ?? throw new InvalidOperationException("No process path.");
        var isDotnetHost = string.Equals(
            Path.GetFileNameWithoutExtension(executable),
            "dotnet",
            StringComparison.OrdinalIgnoreCase);
        var startInfo = new ProcessStartInfo(executable)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        if (isDotnetHost)
        {
            startInfo.ArgumentList.Add(typeof(Program).Assembly.Location);
        }

        startInfo.ArgumentList.Add("--recursive-depth");
        startInfo.ArgumentList.Add(depth.ToString(CultureInfo.InvariantCulture));

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Could not start the depth probe.");
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();

        if (!process.WaitForExit(30_000))
        {
            process.Kill(entireProcessTree: true);
            return ("timeout", string.Empty);
        }

        var stdout = output.GetAwaiter().GetResult().Trim();
        var stderr = error.GetAwaiter().GetResult().Trim();

        return process.ExitCode == 0
            ? ("ok", stdout)
            : ($"exit {process.ExitCode}", LastLine(stderr));
    }

    private static string LastLine(string text)
    {
        var lines = text.Split(
            '\n',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return lines.Length == 0 ? string.Empty : lines[^1];
    }

    private static IConfigEditor[] CreateEditors(ConfigNode baseTree) =>
    [
        new RecursiveConfigEditor(baseTree),
        new IterativeConfigEditor(baseTree),
        new GenericFullConfigEditor(baseTree),
        new GenericPathConfigEditor(baseTree),
        new ZipperConfigEditor(baseTree),
        new JsonNodeConfigEditor(baseTree),
    ];

    private static int CountNodes(ConfigNode root)
    {
        var count = 0;
        var stack = new Stack<ConfigNode>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            var node = stack.Pop();
            count++;

            foreach (var child in node.Children)
            {
                stack.Push(child);
            }
        }

        return count;
    }

    private static int CountLeaves(ConfigNode root)
    {
        var count = 0;
        var stack = new Stack<ConfigNode>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            var node = stack.Pop();

            if (node.IsSection)
            {
                foreach (var child in node.Children)
                {
                    stack.Push(child);
                }
            }
            else
            {
                count++;
            }
        }

        return count;
    }

    private static int Depth(ConfigNode root)
    {
        var maxDepth = 0;
        var stack = new Stack<(ConfigNode Node, int Depth)>();
        stack.Push((root, 0));

        while (stack.Count > 0)
        {
            var (node, depth) = stack.Pop();
            maxDepth = Math.Max(maxDepth, depth);

            foreach (var child in node.Children)
            {
                stack.Push((child, depth + 1));
            }
        }

        return maxDepth;
    }

    private static int CountLines(string path)
    {
        if (Directory.Exists(path))
        {
            return Directory
                .GetFiles(path, "*.cs", SearchOption.AllDirectories)
                .Where(static file => !file.Contains(
                    $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
                    StringComparison.Ordinal))
                .Where(static file => !file.Contains(
                    $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}",
                    StringComparison.Ordinal))
                .Sum(static file => File.ReadAllLines(file).Length);
        }

        return File.ReadAllLines(path).Length;
    }
}
