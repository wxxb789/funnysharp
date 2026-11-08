using FunnySharp.TestSupport.TreeComparison;
using FunnySharp.TreeTransformations;

namespace FunnySharp.TreeTransformations.Tests;

public sealed class ZipperTests
{
    [Fact]
    public void NavigatesAndReconstructsTheRoot()
    {
        var small = ConfigFixtures.CreateSmall();
        var root = Zipper<ConfigNode>.FromRoot(small, ConfigTreeAdapter.Instance);
        var items = root.Down(TreePathSegment.Named("items")).GetValueOrDefault()!;
        var first = items.Down(0).GetValueOrDefault()!;
        var x = first.Down(TreePathSegment.Named("x")).GetValueOrDefault()!;

        Assert.True(root.IsRoot);
        Assert.True(root.Up().IsNone);
        Assert.Equal("items", items.Path.ToString());
        Assert.Equal("items[0].x", x.Path.ToString());
        Assert.True(first.Left().IsNone);
        Assert.Equal("second", first.Right().GetValueOrDefault()!.Current.Name);
        Assert.Equal("first", first.Right().GetValueOrDefault()!.Left().GetValueOrDefault()!.Current.Name);
        Assert.True(items.Down(1).GetValueOrDefault()!.Right().IsNone);
        Assert.Equal("first", x.Up().GetValueOrDefault()!.Current.Name);

        var reconstructed = x.Replace(x.Current with { Value = "9" }).ToRoot();

        Assert.True(reconstructed.Changed);
        Assert.Equal(3, reconstructed.RebuiltNodes);
        Assert.Equal(3, reconstructed.SharedNodes);
        Assert.Equal("9", reconstructed.Root.Children[1].Children[0].Children[0].Value);
        Assert.Same(small.Children[0], reconstructed.Root.Children[0]);
        Assert.Same(small.Children[2], reconstructed.Root.Children[2]);
        Assert.Same(small.Children[1].Children[1], reconstructed.Root.Children[1].Children[1]);
        Assert.Equal("2", small.Children[1].Children[0].Children[0].Value);
    }

    [Fact]
    public void UpAndSiblingNavigationPreserveAnEdit()
    {
        var small = ConfigFixtures.CreateSmall();
        var root = Zipper<ConfigNode>.FromRoot(small, ConfigTreeAdapter.Instance);
        var first = root.Down(TreePathSegment.Named("items")).GetValueOrDefault()!.Down(0).GetValueOrDefault()!;
        var edited = first.Replace(first.Current with { Children = [] });
        var parent = edited.Up().GetValueOrDefault()!;
        var right = edited.Right().GetValueOrDefault()!;

        Assert.Empty(parent.Current.Children[0].Children);
        Assert.Equal("second", right.Current.Name);
        Assert.Empty(right.ToRoot().Root.Children[1].Children[0].Children);
        Assert.Equal("3", right.ToRoot().Root.Children[1].Children[1].Children[0].Value);
    }

    [Fact]
    public void RemoveReturnsTheRebuiltParentAndRootRemovalIsRejected()
    {
        var small = ConfigFixtures.CreateSmall();
        var root = Zipper<ConfigNode>.FromRoot(small, ConfigTreeAdapter.Instance);
        var first = root.Down(TreePathSegment.Named("items")).GetValueOrDefault()!.Down(0).GetValueOrDefault()!;
        var removed = first.Remove().GetValueOrDefault()!;

        Assert.Equal("items", removed.Current.Name);
        Assert.Single(removed.Current.Children);
        Assert.Same(small.Children[1].Children[1], removed.Current.Children[0]);
        Assert.True(root.Remove().IsNone);
    }

    [Fact]
    public void MutableChildListsRemainVisibleAndShared()
    {
        var (root, shared, mutableChildren) = ConfigFixtures.CreateMutableShared();
        mutableChildren.Add(ConfigNode.Leaf("extra", "x"));
        var zipper = Zipper<ConfigNode>.FromRoot(root, ConfigTreeAdapter.Instance);
        var value = zipper
            .Down(TreePathSegment.Named("items")).GetValueOrDefault()!
            .Down(0).GetValueOrDefault()!
            .Down(TreePathSegment.Named("value")).GetValueOrDefault()!;
        var reconstructed = value.Replace(value.Current with { Value = "9" }).ToRoot();

        Assert.Equal(2, reconstructed.Root.Children[0].Children[0].Children.Count);
        Assert.Equal(2, reconstructed.Root.Children[0].Children[1].Children.Count);
        Assert.Same(shared, reconstructed.Root.Children[0].Children[1]);
    }

    [Fact]
    public void CycleEdgeCanBeObservedButIsNotNavigatedIndefinitely()
    {
        var (cycle, _) = ConfigFixtures.CreateCycle();
        var zipper = Zipper<ConfigNode>.FromRoot(cycle, ConfigTreeAdapter.Instance);
        var leaf = zipper.Down(TreePathSegment.Named("value")).GetValueOrDefault()!;
        var root = leaf.Up().GetValueOrDefault()!;
        var edge = root.Down(TreePathSegment.Named("root")).GetValueOrDefault()!;

        Assert.Same(cycle, root.Current);
        Assert.Same(cycle, edge.Current);
        Assert.False(edge.ToRoot().Changed);
    }

    [Fact]
    public void RejectsNullArguments()
    {
        var small = ConfigFixtures.CreateSmall();

        Assert.Throws<ArgumentNullException>(
            () => Zipper<ConfigNode>.FromRoot(null!, ConfigTreeAdapter.Instance));
        Assert.Throws<ArgumentNullException>(
            () => Zipper<ConfigNode>.FromRoot(small, null!));
        Assert.Throws<ArgumentNullException>(
            () => Zipper<ConfigNode>.FromRoot(small, ConfigTreeAdapter.Instance).Replace(null!));
    }
}
