using FunnySharp.TestSupport.TreeComparison;
using FunnySharp.TreeTransformations;

namespace FunnySharp.TreeTransformations.Tests;

public sealed class TreeTraversalTests
{
    [Fact]
    public void VisitsPreOrderWithCallbackScopedLocations()
    {
        var small = ConfigFixtures.CreateSmall();
        var names = new List<string>();
        var result = TreeTraversal.Traverse(
            small,
            ConfigTreeAdapter.Instance,
            (node, depth, path) =>
            {
                names.Add(node.Name);

                if (node.Name == "x")
                {
                    Assert.Equal("items[0].x", path.ToLocation().ToString());
                }

                return TreeVisitDecision.Continue;
            });

        Assert.Equal(new[] { "root", "a", "items", "first", "x", "second", "y", "z" }, names);
        Assert.Equal(8, result.VisitedNodes);
        Assert.Equal(3, result.MaxDepth);
    }

    [Fact]
    public void StopsAfterTheCurrentVisitorCall()
    {
        var small = ConfigFixtures.CreateSmall();
        var names = new List<string>();
        var result = TreeTraversal.Traverse(
            small,
            ConfigTreeAdapter.Instance,
            (node, depth, path) =>
            {
                names.Add(node.Name);
                return node.Name == "first" ? TreeVisitDecision.Stop : TreeVisitDecision.Continue;
            });

        Assert.Equal(new[] { "root", "a", "items", "first" }, names);
        Assert.True(result.Stopped);
        Assert.Equal(4, result.VisitedNodes);
    }

    [Fact]
    public void RejectsInvalidArgumentsAndLimits()
    {
        var small = ConfigFixtures.CreateSmall();

        Assert.Throws<ArgumentNullException>(
            () => TreeTraversal.Traverse<ConfigNode>(null!, ConfigTreeAdapter.Instance, (node, depth, path) => TreeVisitDecision.Continue));
        Assert.Throws<ArgumentNullException>(
            () => TreeTraversal.Traverse(small, null!, (node, depth, path) => TreeVisitDecision.Continue));
        Assert.Throws<ArgumentNullException>(
            () => TreeTraversal.Traverse(small, ConfigTreeAdapter.Instance, null!));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => TreeTraversal.Traverse(
                small,
                ConfigTreeAdapter.Instance,
                (node, depth, path) => TreeVisitDecision.Continue,
                new TreeTraversalOptions<ConfigNode> { MaxNodes = 0 }));
        var limit = Assert.Throws<TreeTraversalLimitException>(
            () => TreeTraversal.Traverse(
                small,
                ConfigTreeAdapter.Instance,
                (node, depth, path) => TreeVisitDecision.Continue,
                new TreeTraversalOptions<ConfigNode> { MaxNodes = 3 }));

        Assert.Equal(3, limit.Limit);
        Assert.Equal("items[0]", limit.Location.ToString());
    }

    [Fact]
    public void DagModeVisitsEachDistinctNodeOnce()
    {
        var dag = ConfigFixtures.CreateDag();
        var tree = TreeTraversal.Traverse(
            dag,
            ConfigTreeAdapter.Instance,
            (node, depth, path) => TreeVisitDecision.Continue);
        var distinct = TreeTraversal.Traverse(
            dag,
            ConfigTreeAdapter.Instance,
            (node, depth, path) => TreeVisitDecision.Continue,
            new TreeTraversalOptions<ConfigNode> { Shape = TreeShape.Dag });

        Assert.Equal(8, tree.VisitedNodes);
        Assert.Equal(5, distinct.VisitedNodes);
        Assert.Equal(1, distinct.DuplicateVisits);
    }

    [Fact]
    public void PostOrderStopsAfterChildrenAndReportsDepth()
    {
        var small = ConfigFixtures.CreateSmall();
        var names = new List<string>();
        var result = TreeTraversal.Traverse(
            small,
            ConfigTreeAdapter.Instance,
            (node, depth, path) =>
            {
                names.Add(node.Name);
                return node.Name == "first" ? TreeVisitDecision.Stop : TreeVisitDecision.Continue;
            },
            new TreeTraversalOptions<ConfigNode> { Order = TreeVisitOrder.PostOrder });

        Assert.Equal(new[] { "a", "x", "first" }, names);
        Assert.True(result.Stopped);
        Assert.False(result.CycleDetected);
        Assert.Equal(3, result.VisitedNodes);
        Assert.Equal(3, result.MaxDepth);
    }

    [Fact]
    public void CycleThrowsOrStops()
    {
        var (cycle, _) = ConfigFixtures.CreateCycle();

        var cycleError = Assert.Throws<TreeCycleException>(
            () => TreeTraversal.Traverse(
                cycle,
                ConfigTreeAdapter.Instance,
                (node, depth, path) => TreeVisitDecision.Continue));

        Assert.Equal("root", cycleError.Location.ToString());

        var stopped = TreeTraversal.Traverse(
            cycle,
            ConfigTreeAdapter.Instance,
            (node, depth, path) => TreeVisitDecision.Continue,
            new TreeTraversalOptions<ConfigNode> { CyclePolicy = TreeCyclePolicy.Stop });

        Assert.True(stopped.CycleDetected);
        Assert.False(stopped.Stopped);
        Assert.Equal(2, stopped.VisitedNodes);
    }
}
