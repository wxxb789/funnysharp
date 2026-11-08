using FunnySharp.TestSupport.TreeComparison;
using FunnySharp.TreeTransformations;

namespace FunnySharp.TreeTransformations.Tests;

public sealed class TreeRewriteTests
{
    [Fact]
    public void PathRewriteRebuildsOnlyTheChangedPath()
    {
        var small = ConfigFixtures.CreateSmall();
        var result = TreeRewrite.RewriteAt(
            small,
            ConfigTreeAdapter.Instance,
            TreePath.Parse("items[0].x"),
            node => node with { Value = "9" });

        Assert.True(result.TryGetValue(out var rewrite));
        Assert.True(rewrite.Changed);
        Assert.Equal(4, rewrite.VisitedNodes);
        Assert.Equal(4, rewrite.RebuiltNodes);
        Assert.Equal(3, rewrite.SharedNodes);
        Assert.Equal("9", rewrite.Root.Children[1].Children[0].Children[0].Value);
        Assert.Same(small.Children[0], rewrite.Root.Children[0]);
        Assert.Same(small.Children[2], rewrite.Root.Children[2]);
        Assert.Same(small.Children[1].Children[1], rewrite.Root.Children[1].Children[1]);
        Assert.Equal("2", small.Children[1].Children[0].Children[0].Value);
    }

    [Fact]
    public void PathFailuresAndNoOpsAreExplicit()
    {
        var small = ConfigFixtures.CreateSmall();
        var missing = TreeRewrite.RewriteAt(
            small,
            ConfigTreeAdapter.Instance,
            TreePath.Parse("items[0].missing"),
            node => node);
        var outOfRange = TreeRewrite.RewriteAt(
            small,
            ConfigTreeAdapter.Instance,
            TreePath.Parse("items[9].x"),
            node => node);
        var noOp = TreeRewrite.RewriteAt(
            small,
            ConfigTreeAdapter.Instance,
            TreePath.Parse("items[0].x"),
            node => node);

        Assert.True(missing.TryGetError(out var missingError));
        Assert.Equal(TreeEditErrorKind.PathNotFound, missingError!.Kind);
        Assert.Equal("items[0].missing", missingError.Location.ToString());
        Assert.True(outOfRange.TryGetError(out var rangeError));
        Assert.Equal(TreeEditErrorKind.IndexOutOfRange, rangeError!.Kind);
        Assert.True(noOp.TryGetValue(out var noOpRewrite));
        Assert.False(noOpRewrite.Changed);
        Assert.Equal(0, noOpRewrite.RebuiltNodes);
        Assert.Same(small, noOpRewrite.Root);
    }

    [Fact]
    public void RootEditReplacesTheWholeRoot()
    {
        var leaf = ConfigNode.Leaf("value", "1");
        var result = TreeRewrite.RewriteAt(
            leaf,
            ConfigTreeAdapter.Instance,
            TreePath.Root,
            node => node with { Value = "2" });

        Assert.True(result.TryGetValue(out var rewrite));
        Assert.Equal("2", rewrite.Root.Value);
        Assert.Equal(1, rewrite.RebuiltNodes);
        Assert.Equal(0, rewrite.SharedNodes);
    }

    [Fact]
    public void BottomUpRewriteSharesUnchangedSubtrees()
    {
        var small = ConfigFixtures.CreateSmall();
        var result = TreeRewrite.RewriteBottomUp(
            small,
            ConfigTreeAdapter.Instance,
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

                return changed ? ConfigTreeAdapter.Instance.Rebuild(node, rewritten) : node;
            });

        Assert.Equal(8, result.VisitedNodes);
        Assert.Equal("9", result.Root.Children[1].Children[0].Children[0].Value);
        Assert.Same(small.Children[0], result.Root.Children[0]);
        Assert.Same(small.Children[2], result.Root.Children[2]);
        Assert.Same(small.Children[1].Children[1], result.Root.Children[1].Children[1]);
    }

    [Fact]
    public void DagRewriteMemoizesByIdentity()
    {
        var dag = ConfigFixtures.CreateDag();
        ConfigNode Rewrite(ConfigNode node, IReadOnlyList<ConfigNode> original, IReadOnlyList<ConfigNode> rewritten)
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

        var treeMode = TreeRewrite.RewriteBottomUp(
            dag,
            ConfigTreeAdapter.Instance,
            (node, original, rewritten, path) => Rewrite(node, original, rewritten));
        var dagMode = TreeRewrite.RewriteBottomUp(
            dag,
            ConfigTreeAdapter.Instance,
            (node, original, rewritten, path) => Rewrite(node, original, rewritten),
            new TreeTraversalOptions<ConfigNode> { Shape = TreeShape.Dag });

        Assert.NotSame(treeMode.Root.Children[0].Children[0], treeMode.Root.Children[0].Children[1]);
        Assert.Same(dagMode.Root.Children[0].Children[0], dagMode.Root.Children[0].Children[1]);
        Assert.Equal(5, dagMode.VisitedNodes);
        Assert.Equal(1, dagMode.DuplicateVisits);
    }

    [Fact]
    public void CycleAlwaysThrowsForAWholeRewrite()
    {
        var (cycle, _) = ConfigFixtures.CreateCycle();

        Assert.Throws<TreeCycleException>(
            () => TreeRewrite.RewriteBottomUp(
                cycle,
                ConfigTreeAdapter.Instance,
                (node, original, rewritten, path) => node));
    }

    [Fact]
    public void RejectsNullArguments()
    {
        var small = ConfigFixtures.CreateSmall();

        Assert.Throws<ArgumentNullException>(
            () => TreeRewrite.RewriteAt<ConfigNode>(null!, ConfigTreeAdapter.Instance, TreePath.Root, node => node));
        Assert.Throws<ArgumentNullException>(
            () => TreeRewrite.RewriteAt(small, null!, TreePath.Root, node => node));
        Assert.Throws<ArgumentNullException>(
            () => TreeRewrite.RewriteAt(small, ConfigTreeAdapter.Instance, null!, node => node));
        Assert.Throws<ArgumentNullException>(
            () => TreeRewrite.RewriteAt(small, ConfigTreeAdapter.Instance, TreePath.Root, (Func<ConfigNode, ConfigNode>)null!));
    }
}
