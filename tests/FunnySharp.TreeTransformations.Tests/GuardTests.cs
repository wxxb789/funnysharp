using FunnySharp;
using FunnySharp.TestSupport.TreeComparison;
using FunnySharp.TreeTransformations;

namespace FunnySharp.TreeTransformations.Tests;

public sealed class GuardTests
{
    [Fact]
    public void ResolveAcceptsTheEmptyPathAndRejectsNull()
    {
        var small = ConfigFixtures.CreateSmall();
        var resolved = TreeRewrite.Resolve(small, ConfigTreeAdapter.Instance, TreePath.Root);

        Assert.True(resolved.TryGetValue(out var node));
        Assert.Same(small, node.Node);
        Assert.Equal(TreePath.Root, node.Path);
        Assert.Throws<ArgumentNullException>(
            () => TreeRewrite.Resolve(small, ConfigTreeAdapter.Instance, null!));
    }

    [Fact]
    public void DownReturnsNoneOutsideTheChildren()
    {
        var root = Zipper<ConfigNode>.FromRoot(ConfigFixtures.CreateSmall(), ConfigTreeAdapter.Instance);

        Assert.True(root.Down(99).IsNone);
        Assert.True(root.Down(-1).IsNone);
        Assert.True(root.Down(TreePathSegment.Named("missing")).IsNone);
    }

    [Fact]
    public void TraversalOptionsRejectUndefinedEnums()
    {
        var small = ConfigFixtures.CreateSmall();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => TreeTraversal.Traverse(
                small,
                ConfigTreeAdapter.Instance,
                (node, depth, path) => TreeVisitDecision.Continue,
                new TreeTraversalOptions<ConfigNode> { Order = (TreeVisitOrder)99 }));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => TreeTraversal.Traverse(
                small,
                ConfigTreeAdapter.Instance,
                (node, depth, path) => TreeVisitDecision.Continue,
                new TreeTraversalOptions<ConfigNode> { Shape = (TreeShape)99 }));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => TreeTraversal.Traverse(
                small,
                ConfigTreeAdapter.Instance,
                (node, depth, path) => TreeVisitDecision.Continue,
                new TreeTraversalOptions<ConfigNode> { CyclePolicy = (TreeCyclePolicy)99 }));
    }

    [Fact]
    public void FromSegmentsRejectsNull() =>
        Assert.Throws<ArgumentNullException>(() => TreePath.FromSegments(null!));

    [Fact]
    public void SegmentsValidateTheirInputs()
    {
        Assert.Throws<ArgumentNullException>(() => TreePathSegment.Named(null!));
        Assert.Throws<ArgumentException>(() => TreePathSegment.Named(string.Empty));
        Assert.Throws<ArgumentOutOfRangeException>(() => TreePathSegment.At(-1));
    }

    [Fact]
    public void PathViewIndexerRejectsOutOfRangePositions()
    {
        TreePathView? captured = null;
        TreeTraversal.Traverse(
            ConfigFixtures.CreateSmall(),
            ConfigTreeAdapter.Instance,
            (node, depth, path) =>
            {
                if (node.Name == "x")
                {
                    captured = path;
                }

                return TreeVisitDecision.Continue;
            });

        var view = captured!.Value;

        Assert.Equal(3, view.Count);
        Assert.Throws<ArgumentOutOfRangeException>(() => view[-1]);
        Assert.Throws<ArgumentOutOfRangeException>(() => view[view.Count]);
    }
}
