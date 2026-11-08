#pragma warning disable FS0017

using FunnySharp;
using FunnySharp.TreeTransformations;

namespace FunnySharp.TestSupport.TreeComparison;

/// <summary>
/// Shared reference behavior and work ledger for every compared configuration consumer. Both the
/// xUnit suite and the measurement harness call this verifier; a disagreement fails both.
/// </summary>
internal static class TreeComparison
{
    private static int s_checks;

    internal static int Verify()
    {
        s_checks = 0;
        VerifyPathParsing();
        VerifyTraversal();
        VerifyRewrite();
        VerifyZipper();
        VerifySingleOverride();
        VerifyBatchOverrides();
        VerifyMixedOverrides();
        VerifyFailFast();
        VerifyValidation();
        VerifyDag();
        VerifyCycle();
        VerifyMutableContents();
        return s_checks;
    }

    private static void VerifyPathParsing()
    {
        var path = TreePath.Parse("services[3].endpoints[0].port");

        Check(path.Count == 5, "parsed path length");
        Check(path[0] == TreePathSegment.Named("services"), "first segment");
        Check(path[1] == TreePathSegment.At(3), "second segment");
        Check(path.ToString() == "services[3].endpoints[0].port", "path rendering");
        Check(TreePath.Parse(string.Empty).Count == 0, "root path");
        Check(TreePath.Parse("a.b-c").Count == 2, "hyphenated name");
        CheckThrows<FormatException>(() => TreePath.Parse("services["), "unterminated index");
        CheckThrows<FormatException>(() => TreePath.Parse("services..port"), "empty name");
        CheckThrows<FormatException>(() => TreePath.Parse("[0]"), "root index");
    }

    private static void VerifyTraversal()
    {
        var small = ConfigFixtures.CreateSmall();
        var adapter = ConfigTreeAdapter.Instance;
        var preOrder = new List<string>();
        var preResult = TreeTraversal.Traverse(
            small,
            adapter,
            (node, depth, path) =>
            {
                preOrder.Add($"{depth}:{node.Name}:{path.ToPath()}");
                return TreeVisitDecision.Continue;
            });

        Check(
            preOrder.SequenceEqual(
            [
                "0:root:",
                "1:a:a",
                "1:items:items",
                "2:first:items[0]",
                "3:x:items[0].x",
                "2:second:items[1]",
                "3:y:items[1].y",
                "1:z:z",
            ]),
            "pre-order names, depths, and paths");
        Check(preResult.VisitedNodes == 8, "pre-order visited count");
        Check(preResult.MaxDepth == 3, "pre-order max depth");

        var postOrder = new List<string>();
        var postResult = TreeTraversal.Traverse(
            small,
            adapter,
            (node, depth, path) =>
            {
                postOrder.Add($"{depth}:{node.Name}");
                return TreeVisitDecision.Continue;
            },
            new TreeTraversalOptions<ConfigNode> { Order = TreeVisitOrder.PostOrder });

        Check(
            postOrder.SequenceEqual(
            [
                "1:a",
                "3:x",
                "2:first",
                "3:y",
                "2:second",
                "1:items",
                "1:z",
                "0:root",
            ]),
            "post-order sequence");
        Check(postResult.VisitedNodes == 8, "post-order visited count");

        var shortCircuit = new List<string>();
        var shortResult = TreeTraversal.Traverse(
            small,
            adapter,
            (node, depth, path) =>
            {
                shortCircuit.Add(node.Name);
                return node.Name == "first" ? TreeVisitDecision.Stop : TreeVisitDecision.Continue;
            });

        Check(shortCircuit.SequenceEqual(["root", "a", "items", "first"]), "visitor stop sequence");
        Check(shortResult.Stopped && shortResult.VisitedNodes == 4, "visitor stop result");
        CheckThrows<TreeTraversalLimitException>(
            () => TreeTraversal.Traverse(
                small,
                adapter,
                (node, depth, path) => TreeVisitDecision.Continue,
                new TreeTraversalOptions<ConfigNode> { MaxNodes = 3 }),
            "node limit");

        var dag = ConfigFixtures.CreateDag();
        var treeVisit = TreeTraversal.Traverse(dag, adapter, (node, depth, path) => TreeVisitDecision.Continue);
        var dagVisit = TreeTraversal.Traverse(
            dag,
            adapter,
            (node, depth, path) => TreeVisitDecision.Continue,
            new TreeTraversalOptions<ConfigNode> { Shape = TreeShape.Dag });

        Check(treeVisit.VisitedNodes == 8, "DAG tree-mode visits every occurrence");
        Check(dagVisit.VisitedNodes == 5 && dagVisit.DuplicateVisits == 1, "DAG mode visits once");

        var equalDistinct = ConfigFixtures.CreateEqualDistinctDag();
        var referenceVisit = TreeTraversal.Traverse(
            equalDistinct,
            adapter,
            (node, depth, path) => TreeVisitDecision.Continue,
            new TreeTraversalOptions<ConfigNode> { Shape = TreeShape.Dag });
        var recordVisit = TreeTraversal.Traverse(
            equalDistinct,
            adapter,
            (node, depth, path) => TreeVisitDecision.Continue,
            new TreeTraversalOptions<ConfigNode>
            {
                Shape = TreeShape.Dag,
                Identity = EqualityComparer<ConfigNode>.Default,
            });
        var nameVisit = TreeTraversal.Traverse(
            equalDistinct,
            adapter,
            (node, depth, path) => TreeVisitDecision.Continue,
            new TreeTraversalOptions<ConfigNode>
            {
                Shape = TreeShape.Dag,
                Identity = new ConfigNameComparer(),
            });

        Check(referenceVisit.VisitedNodes == 6, "reference identity keeps equal-distinct nodes");
        Check(
            recordVisit.VisitedNodes == 5 && recordVisit.DuplicateVisits == 1,
            "record equality compares child-list references; only the shared leaf deduplicates");
        Check(
            nameVisit.VisitedNodes == 4 && nameVisit.DuplicateVisits == 1,
            "caller-supplied equality deduplicates distinct nodes");
    }

    private static void VerifyRewrite()
    {
        var small = ConfigFixtures.CreateSmall();
        var adapter = ConfigTreeAdapter.Instance;
        var result = TreeRewrite.RewriteAt(
            small,
            adapter,
            TreePath.Parse("items[0].x"),
            node => node with { Value = "9" });

        Check(result.TryGetValue(out var rewrite), "path rewrite success");
        Check(rewrite.Changed, "path rewrite changed");
        Check(rewrite.VisitedNodes == 4, "path rewrite visited");
        Check(rewrite.RebuiltNodes == 4, "path rewrite rebuilt");
        Check(rewrite.SharedNodes == 3, "path rewrite shared");

        var smallItems = small.Children[1];
        var rewrittenItems = rewrite.Root.Children[1];
        var smallFirst = smallItems.Children[0];
        var rewrittenFirst = rewrittenItems.Children[0];

        Check(ReferenceEquals(rewrite.Root.Children[0], small.Children[0]), "a shared");
        Check(ReferenceEquals(rewrite.Root.Children[2], small.Children[2]), "z shared");
        Check(!ReferenceEquals(rewrittenItems, smallItems), "items rebuilt");
        Check(ReferenceEquals(rewrittenItems.Children[1], smallItems.Children[1]), "second shared");
        Check(!ReferenceEquals(rewrittenFirst, smallFirst), "first rebuilt");
        Check(rewrittenFirst.Children[0].Value == "9", "edited value");
        Check(smallFirst.Children[0].Value == "2", "base unchanged");

        var missing = TreeRewrite.RewriteAt(
            small,
            adapter,
            TreePath.Parse("items[0].missing"),
            node => node);
        Check(missing.TryGetError(out var missingError), "missing path failure");
        Check(missingError!.Kind == TreeEditErrorKind.PathNotFound, "missing path kind");
        Check(missingError.Location.ToString() == "items[0].missing", "missing path location");

        var outOfRange = TreeRewrite.RewriteAt(
            small,
            adapter,
            TreePath.Parse("items[9].x"),
            node => node);
        Check(outOfRange.TryGetError(out var rangeError), "range failure");
        Check(rangeError!.Kind == TreeEditErrorKind.IndexOutOfRange, "range kind");

        var rootEdit = TreeRewrite.RewriteAt(
            small,
            adapter,
            TreePath.Root,
            node => node with { Children = [] });
        Check(rootEdit.TryGetValue(out var rootRewrite) && rootRewrite.Changed, "root edit");
        Check(rootRewrite.RebuiltNodes == 1 && rootRewrite.SharedNodes == 0, "root edit counts");

        var noOp = TreeRewrite.RewriteAt(small, adapter, TreePath.Parse("items[0].x"), node => node);
        Check(noOp.TryGetValue(out var noOpRewrite), "no-op success");
        Check(!noOpRewrite.Changed && noOpRewrite.RebuiltNodes == 0, "no-op counts");
        Check(ReferenceEquals(noOpRewrite.Root, small), "no-op shares root");

        var bottomUp = TreeRewrite.RewriteBottomUp(
            small,
            adapter,
            (node, original, rewritten, path) =>
            {
                if (!node.IsSection && node.Name == "x")
                {
                    return node with { Value = "9" };
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

                return changed ? adapter.Rebuild(node, rewritten) : node;
            });

        Check(bottomUp.Root.Children[1].Children[0].Children[0].Value == "9", "bottom-up edit");
        Check(bottomUp.VisitedNodes == 8, "bottom-up visited");
        Check(small.Children[1].Children[0].Children[0].Value == "2", "bottom-up base unchanged");

        var dag = ConfigFixtures.CreateDag();
        var treeMode = TreeRewrite.RewriteBottomUp(
            dag,
            adapter,
            (node, original, rewritten, path) => ChangeValueLeaf(node, original, rewritten, adapter));
        var dagMode = TreeRewrite.RewriteBottomUp(
            dag,
            adapter,
            (node, original, rewritten, path) => ChangeValueLeaf(node, original, rewritten, adapter),
            new TreeTraversalOptions<ConfigNode> { Shape = TreeShape.Dag });

        var treeItems = treeMode.Root.Children[0];
        var dagItems = dagMode.Root.Children[0];
        Check(!ReferenceEquals(treeItems.Children[0], treeItems.Children[1]), "tree mode duplicates shared node");
        Check(ReferenceEquals(dagItems.Children[0], dagItems.Children[1]), "DAG mode preserves sharing");
        Check(dagMode.VisitedNodes == 5 && dagMode.DuplicateVisits == 1, "DAG rewrite counts");
    }

    private static ConfigNode ChangeValueLeaf(
        ConfigNode node,
        IReadOnlyList<ConfigNode> original,
        IReadOnlyList<ConfigNode> rewritten,
        TreeNodeAdapter<ConfigNode> adapter)
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

        return changed ? adapter.Rebuild(node, rewritten) : node;
    }

    private static void VerifyZipper()
    {
        var small = ConfigFixtures.CreateSmall();
        var adapter = ConfigTreeAdapter.Instance;
        var root = Zipper<ConfigNode>.FromRoot(small, adapter);

        Check(root.IsRoot && root.Depth == 0 && ReferenceEquals(root.Current, small), "root cursor");
        Check(root.Up().IsNone, "root has no parent");

        var items = root.Down(TreePathSegment.Named("items")).GetValueOrDefault()!;
        Check(items.Current.Name == "items" && items.Depth == 1, "items cursor");
        Check(items.Path.ToString() == "items", "items path");

        var first = items.Down(0).GetValueOrDefault()!;
        var x = first.Down(TreePathSegment.Named("x")).GetValueOrDefault()!;
        Check(x.Current.Value == "2" && x.Path.ToString() == "items[0].x", "x cursor");
        Check(first.Left().IsNone, "first has no left sibling");
        Check(first.Right().GetValueOrDefault()!.Current.Name == "second", "right sibling");
        Check(first.Right().GetValueOrDefault()!.Left().GetValueOrDefault()!.Current.Name == "first", "left sibling");
        Check(items.Down(1).GetValueOrDefault()!.Right().IsNone, "last has no right sibling");
        Check(x.Up().GetValueOrDefault()!.Current.Name == "first", "up");

        var edited = x.Replace(x.Current with { Value = "9" });
        var reconstructed = edited.ToRoot();
        Check(reconstructed.Changed, "zipper changed");
        Check(reconstructed.RebuiltNodes == 3, "zipper rebuilt parents");
        Check(reconstructed.SharedNodes == 3, "zipper shared siblings");
        Check(reconstructed.Root.Children[1].Children[0].Children[0].Value == "9", "zipper edited value");
        Check(ReferenceEquals(reconstructed.Root.Children[0], small.Children[0]), "zipper a shared");
        Check(ReferenceEquals(reconstructed.Root.Children[2], small.Children[2]), "zipper z shared");
        Check(ReferenceEquals(reconstructed.Root.Children[1].Children[1], small.Children[1].Children[1]), "zipper second shared");
        Check(small.Children[1].Children[0].Children[0].Value == "2", "zipper base unchanged");

        var upAfterEdit = x.Replace(x.Current with { Value = "9" }).Up().GetValueOrDefault()!;
        Check(upAfterEdit.Current.Children[0].Value == "9", "up preserves edit");
        Check(upAfterEdit.ToRoot().Root.Children[1].Children[0].Children[0].Value == "9", "up root edit");

        var rightAfterEdit = first.Replace(first.Current with { Children = [] }).Right().GetValueOrDefault()!;
        Check(rightAfterEdit.Current.Name == "second", "right after edit moves");
        var rightRoot = rightAfterEdit.ToRoot().Root;
        Check(rightRoot.Children[1].Children[0].Children.Count == 0, "right after edit keeps edited sibling");
        Check(rightRoot.Children[1].Children[1].Children[0].Value == "3", "right after edit keeps second");

        var removed = first.Remove().GetValueOrDefault()!;
        Check(removed.Current.Name == "items", "remove returns parent");
        Check(removed.Current.Children.Count == 1, "remove drops child");
        Check(ReferenceEquals(removed.Current.Children[0], small.Children[1].Children[1]), "remove shares sibling");
        Check(root.Remove().IsNone, "cannot remove root");

        var (mutableRoot, _, mutableChildren) = ConfigFixtures.CreateMutableShared();
        mutableChildren.Add(ConfigNode.Leaf("extra", "x"));
        var mutableZipper = Zipper<ConfigNode>.FromRoot(mutableRoot, adapter);
        var mutableValue = mutableZipper
            .Down(TreePathSegment.Named("items")).GetValueOrDefault()!
            .Down(0).GetValueOrDefault()!
            .Down(TreePathSegment.Named("value")).GetValueOrDefault()!;
        var mutableEdit = mutableValue.Replace(mutableValue.Current with { Value = "9" }).ToRoot();
        Check(mutableEdit.Root.Children[0].Children[0].Children.Count == 2, "zipper sees mutable child list");
        Check(mutableEdit.Root.Children[0].Children[1].Children.Count == 2, "zipper shares mutable sibling list");
    }

    private static void VerifySingleOverride()
    {
        var baseTree = ConfigFixtures.CreateBase();
        var before = ConfigKernel.ToCanonicalJson(baseTree);
        var overrides = new[]
        {
            new ConfigOverride(
                "services[3].endpoints[0].port",
                TreePath.Parse("services[3].endpoints[0].port"),
                "9999"),
        };

        var (editors, results) = ApplyAll(baseTree, overrides);

        for (var index = 1; index < results.Length; index++)
        {
            CompareValidation(results[0], results[index], $"single override {editors[index].Name}");
        }

        AssertStats(editors[0], 3_080, 6, 76, 1, 0, "single override");
        AssertStats(editors[1], 3_080, 6, 76, 1, 0, "single override");
        AssertStats(editors[2], 6_154, 6, 76, 1, 0, "single override");
        AssertStats(editors[3], 3_080, 6, 76, 1, 0, "single override");
        AssertStats(editors[4], 3_080, 6, 76, 1, 0, "single override");
        AssertStats(editors[5], 3_080, 3_074, 0, 1, 0, "single override");

        Check(results[0].TryGetValue(out var tree), "single override valid");

        for (var index = 0; index < editors.Length; index++)
        {
            Check(results[index].TryGetValue(out var actual), $"single override valid {editors[index].Name}");

            if (index != 5)
            {
                AssertSingleEditSharing(baseTree, actual!, editors[index].Name);
            }
        }

        Check(ConfigKernel.ToCanonicalJson(baseTree) == before, "single override leaves base unchanged");
        Check(tree!.Children[0].Children[3].Children[2].Children[0].Children[1].Value == "9999", "single override value");
    }

    private static void VerifyBatchOverrides()
    {
        var baseTree = ConfigFixtures.CreateBase();
        var overrides = ConfigFixtures.CreateValidOverrides();
        var (editors, results) = ApplyAll(baseTree, overrides);

        for (var index = 1; index < results.Length; index++)
        {
            CompareValidation(results[0], results[index], $"batch {editors[index].Name}");
        }

        AssertStats(editors[0], 3_258, 184, 2_380, 32, 0, "batch");
        AssertStats(editors[1], 3_258, 184, 2_380, 32, 0, "batch");
        AssertStats(editors[2], 6_332, 112, 358, 32, 0, "batch");
        AssertStats(editors[3], 3_258, 184, 2_380, 32, 0, "batch");
        AssertStats(editors[4], 3_258, 184, 2_380, 32, 0, "batch");
        AssertStats(editors[5], 3_258, 3_074, 0, 32, 0, "batch");

        for (var index = 0; index < editors.Length; index++)
        {
            Check(results[index].TryGetValue(out var actual), $"batch valid {editors[index].Name}");

            if (index != 5)
            {
                Check(
                    ReferenceEquals(actual!.Children[0].Children[63], baseTree.Children[0].Children[63]),
                    $"batch unchanged service shared {editors[index].Name}");
            }
        }

        Check(editors[2].Stats.RebuiltNodes < editors[0].Stats.RebuiltNodes, "full rewrite rebuilds the union");
    }

    private static void VerifyMixedOverrides()
    {
        var baseTree = ConfigFixtures.CreateBase();
        var overrides = ConfigFixtures.CreateMixedOverrides();
        var expectedErrors = new List<ConfigError>
        {
            ConfigErrors.Path(
                overrides[32],
                overrides[32].Path[1],
                TreePath.Parse("services[64]").ToLocation()),
            ConfigErrors.NotAValue(overrides[33], TreePath.Parse("services[0].endpoints").ToLocation()),
            ConfigErrors.InvalidValue(
                overrides[34],
                TreePath.Parse("services[1].endpoints[0].port").ToLocation()),
            ConfigErrors.Duplicate(overrides[36]),
        };

        var (editors, results) = ApplyAll(baseTree, overrides);

        for (var index = 0; index < results.Length; index++)
        {
            Check(!results[index].TryGetValue(out _), $"mixed invalid {editors[index].Name}");
            results[index].TryGetErrors(out var errors);
            CompareErrors(expectedErrors, errors!, $"mixed {editors[index].Name}");
        }

        AssertStats(editors[0], 3_275, 189, 2_448, 33, 4, "mixed");
        AssertStats(editors[1], 3_275, 189, 2_448, 33, 4, "mixed");
        AssertStats(editors[2], 6_353, 114, 359, 33, 4, "mixed");
        AssertStats(editors[3], 3_263, 189, 2_448, 33, 4, "mixed");
        AssertStats(editors[4], 3_275, 189, 2_448, 33, 4, "mixed");
        AssertStats(editors[5], 3_275, 3_074, 0, 33, 4, "mixed");
    }

    private static void VerifyFailFast()
    {
        var baseTree = ConfigFixtures.CreateBase();
        var overrides = ConfigFixtures.CreateMixedOverrides();
        var expected = ConfigErrors.Path(
            overrides[32],
            overrides[32].Path[1],
            TreePath.Parse("services[64]").ToLocation());

        foreach (var editor in CreateEditors(baseTree))
        {
            editor.ResetStats();
            var result = editor.ApplyFirst(overrides);

            Check(result.TryGetError(out var error), $"fail-fast failure {editor.Name}");
            CompareError(expected, error!, $"fail-fast {editor.Name}");
            Check(editor.Stats.Errors == 1, $"fail-fast one error {editor.Name}");
        }
    }

    private static void VerifyValidation()
    {
        var baseTree = ConfigFixtures.CreateBase();

        foreach (var editor in CreateEditors(baseTree))
        {
            editor.ResetStats();
            var validation = editor.Validate();

            Check(validation.TryGetValue(out var tree), $"valid base {editor.Name}");
            CompareTrees(baseTree, tree!, $"valid base {editor.Name}");
            AssertStats(editor, 3_074, 0, 0, 0, 0, "valid base");
        }

        var invalidBase = ConfigFixtures.CreateInvalidBase();
        var expected = ConfigErrors.InvalidBase(
            "port",
            "0",
            TreePath.Parse("services[0].endpoints[0].port").ToLocation());

        foreach (var editor in CreateEditors(invalidBase))
        {
            editor.ResetStats();
            var validation = editor.Validate();

            Check(!validation.TryGetValue(out _), $"invalid base {editor.Name}");
            validation.TryGetErrors(out var errors);
            Check(errors is { Count: 1 }, $"invalid base one error {editor.Name}");
            CompareError(expected, errors![0], $"invalid base {editor.Name}");
        }
    }

    private static void VerifyDag()
    {
        var dag = ConfigFixtures.CreateDag();
        var adapter = ConfigTreeAdapter.Instance;
        var overrides = new[] { ConfigFixtures.CreateDagOverride() };
        var (editors, results) = ApplyAll(dag, overrides);

        for (var index = 1; index < results.Length; index++)
        {
            CompareValidation(results[0], results[index], $"DAG edit {editors[index].Name}");
        }

        Check(results[0].TryGetValue(out var tree), "DAG edit valid");
        var items = tree!.Children[0];
        Check(items.Children[0].Children[0].Value == "2", "DAG first occurrence edited");
        Check(items.Children[1].Children[0].Value == "1", "DAG second occurrence unchanged");
        Check(ReferenceEquals(items.Children[1], dag.Children[0].Children[1]), "DAG second occurrence shared");

        var rewritten = TreeRewrite.RewriteAt(
            dag,
            adapter,
            TreePath.Parse("items[0].value"),
            node => node with { Value = "2" });
        Check(rewritten.TryGetValue(out var rewrite), "DAG path rewrite valid");
        Check(rewrite.RebuiltNodes == 4 && rewrite.SharedNodes == 2, "DAG path rewrite counts");
    }

    private static void VerifyCycle()
    {
        var (cycle, leaf) = ConfigFixtures.CreateCycle();
        var adapter = ConfigTreeAdapter.Instance;
        CheckThrows<TreeCycleException>(
            () => TreeTraversal.Traverse(cycle, adapter, (node, depth, path) => TreeVisitDecision.Continue),
            "cycle throws");
        var stopped = TreeTraversal.Traverse(
            cycle,
            adapter,
            (node, depth, path) => TreeVisitDecision.Continue,
            new TreeTraversalOptions<ConfigNode> { CyclePolicy = TreeCyclePolicy.Stop });
        Check(stopped.CycleDetected && !stopped.Stopped, "cycle stop result");
        Check(stopped.VisitedNodes == 2, "cycle stop visited");
        CheckThrows<TreeCycleException>(
            () => TreeRewrite.RewriteBottomUp(
                cycle,
                adapter,
                (node, original, rewritten, path) => node),
            "cycle rewrite throws");

        var edit = TreeRewrite.RewriteAt(cycle, adapter, TreePath.Parse("value"), node => node with { Value = "2" });
        Check(edit.TryGetValue(out var rewrite), "cycle finite path edit");
        Check(rewrite.Root.Children[0].Value == "2", "cycle leaf edited");
        Check(ReferenceEquals(rewrite.Root.Children[1], cycle), "cycle edge preserved");
        Check(leaf.Value == "1", "cycle base leaf unchanged");

        var zipper = Zipper<ConfigNode>.FromRoot(cycle, adapter);
        var cycleLeaf = zipper.Down(TreePathSegment.Named("value")).GetValueOrDefault()!;
        Check(cycleLeaf.Current.Name == "value", "cycle leaf cursor");
        var back = cycleLeaf.Up().GetValueOrDefault()!;
        Check(ReferenceEquals(back.Current, cycle), "cycle up");
        var cycleEdge = back.Down(TreePathSegment.Named("root")).GetValueOrDefault()!;
        Check(ReferenceEquals(cycleEdge.Current, cycle), "cycle edge cursor is root");
        Check(!cycleEdge.ToRoot().Changed, "cycle edge root reconstruction is unchanged");
    }

    private static void VerifyMutableContents()
    {
        var (root, shared, mutableChildren) = ConfigFixtures.CreateMutableShared();
        var before = ConfigKernel.Digest(root);
        mutableChildren.Add(ConfigNode.Leaf("extra", "x"));
        var after = ConfigKernel.Digest(root);
        Check(before != after, "mutable child list is visible through the shared root");
        Check(shared.Children.Count == 2, "shared section sees mutation");

        var edit = TreeRewrite.RewriteAt(
            root,
            ConfigTreeAdapter.Instance,
            TreePath.Parse("items[0].value"),
            node => node with { Value = "2" });
        Check(edit.TryGetValue(out var rewrite), "mutable shared edit");
        Check(rewrite.Root.Children[0].Children[0].Children.Count == 2, "rebuilt first occurrence copies mutable list");
        Check(rewrite.Root.Children[0].Children[1].Children.Count == 2, "second occurrence keeps shared mutable list");
        Check(ReferenceEquals(rewrite.Root.Children[0].Children[1], shared), "second occurrence is the original shared node");
    }

    private static (IConfigEditor[] Editors, Validation<ConfigNode, ConfigError>[] Results) ApplyAll(
        ConfigNode baseTree,
        IReadOnlyList<ConfigOverride> overrides)
    {
        var editors = CreateEditors(baseTree);
        var results = new Validation<ConfigNode, ConfigError>[editors.Length];

        for (var index = 0; index < editors.Length; index++)
        {
            editors[index].ResetStats();
            results[index] = editors[index].Apply(overrides);
        }

        return (editors, results);
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

    private static void AssertSingleEditSharing(ConfigNode baseTree, ConfigNode actual, string description)
    {
        var baseServices = baseTree.Children[0];
        var actualServices = actual.Children[0];
        Check(!ReferenceEquals(actualServices, baseServices), $"{description} services rebuilt");

        for (var service = 0; service < ConfigFixtures.ServiceCount; service++)
        {
            if (service == 3)
            {
                Check(
                    !ReferenceEquals(actualServices.Children[service], baseServices.Children[service]),
                    $"{description} service 3 rebuilt");
                continue;
            }

            Check(
                ReferenceEquals(actualServices.Children[service], baseServices.Children[service]),
                $"{description} service {service} shared");
        }

        var baseService = baseServices.Children[3];
        var actualService = actualServices.Children[3];
        Check(ReferenceEquals(actualService.Children[0], baseService.Children[0]), $"{description} name shared");
        Check(ReferenceEquals(actualService.Children[1], baseService.Children[1]), $"{description} mode shared");
        Check(!ReferenceEquals(actualService.Children[2], baseService.Children[2]), $"{description} endpoints rebuilt");
        Check(ReferenceEquals(actualService.Children[3], baseService.Children[3]), $"{description} limits shared");

        var baseEndpoints = baseService.Children[2];
        var actualEndpoints = actualService.Children[2];

        for (var endpoint = 0; endpoint < ConfigFixtures.EndpointsPerService; endpoint++)
        {
            if (endpoint == 0)
            {
                continue;
            }

            Check(
                ReferenceEquals(actualEndpoints.Children[endpoint], baseEndpoints.Children[endpoint]),
                $"{description} endpoint {endpoint} shared");
        }

        var baseEndpoint = baseEndpoints.Children[0];
        var actualEndpoint = actualEndpoints.Children[0];
        Check(!ReferenceEquals(actualEndpoint, baseEndpoint), $"{description} endpoint 0 rebuilt");
        Check(ReferenceEquals(actualEndpoint.Children[0], baseEndpoint.Children[0]), $"{description} host shared");
        Check(!ReferenceEquals(actualEndpoint.Children[1], baseEndpoint.Children[1]), $"{description} port rebuilt");
        Check(ReferenceEquals(actualEndpoint.Children[2], baseEndpoint.Children[2]), $"{description} path shared");
        Check(ReferenceEquals(actualEndpoint.Children[3], baseEndpoint.Children[3]), $"{description} tls shared");
        Check(actualEndpoint.Children[1].Value == "9999", $"{description} edited value");
    }

    private static void CompareValidation(
        Validation<ConfigNode, ConfigError> expected,
        Validation<ConfigNode, ConfigError> actual,
        string description)
    {
        var expectedValid = expected.TryGetValue(out var expectedTree);
        var actualValid = actual.TryGetValue(out var actualTree);
        Check(expectedValid == actualValid, $"{description} validity");

        if (expectedValid && actualValid)
        {
            CompareTrees(expectedTree!, actualTree!, description);
        }

        if (!expectedValid)
        {
            expected.TryGetErrors(out var expectedErrors);
            actual.TryGetErrors(out var actualErrors);
            CompareErrors(expectedErrors!, actualErrors!, description);
        }
    }

    private static void CompareTrees(ConfigNode expected, ConfigNode actual, string description)
    {
        Check(
            ConfigKernel.ToCanonicalJson(expected) == ConfigKernel.ToCanonicalJson(actual),
            $"{description} canonical tree");
        Check(ConfigKernel.Digest(expected) == ConfigKernel.Digest(actual), $"{description} digest");
    }

    private static void CompareErrors(
        IReadOnlyList<ConfigError> expected,
        IReadOnlyList<ConfigError> actual,
        string description)
    {
        Check(expected.Count == actual.Count, $"{description} error count");

        for (var index = 0; index < Math.Min(expected.Count, actual.Count); index++)
        {
            CompareError(expected[index], actual[index], $"{description} error {index}");
        }
    }

    private static void CompareError(ConfigError expected, ConfigError actual, string description)
    {
        Check(expected.Kind == actual.Kind, $"{description} kind {expected.Kind}/{actual.Kind}");
        Check(expected.Path == actual.Path, $"{description} path {expected.Path}/{actual.Path}");
        Check(expected.Message == actual.Message, $"{description} message {expected.Message}/{actual.Message}");
    }

    private static void AssertStats(
        IConfigEditor editor,
        int visited,
        int rebuilt,
        int shared,
        int applied,
        int errors,
        string description)
    {
        var expected = new ConfigEditStats(visited, rebuilt, shared, applied, errors);
        var actual = editor.Stats;
        Check(actual == expected, $"{description} {editor.Name} stats {actual} expected {expected}");
    }

    private static void CheckThrows<TException>(Action action, string description)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            Check(true, description);
            return;
        }

        throw new InvalidOperationException($"{description}: expected {typeof(TException).Name}.");
    }

    private sealed class ConfigNameComparer : IEqualityComparer<ConfigNode>
    {
        public bool Equals(ConfigNode? x, ConfigNode? y) =>
            ReferenceEquals(x, y) || (x is not null && y is not null && string.Equals(x.Name, y.Name, StringComparison.Ordinal));

        public int GetHashCode(ConfigNode obj) => StringComparer.Ordinal.GetHashCode(obj.Name);
    }

    private static void Check(bool condition, string description)
    {
        if (!condition)
        {
            throw new InvalidOperationException(description);
        }

        s_checks++;
    }
}
