using FunnySharp;
using FunnySharp.TestSupport.TreeComparison;
using FunnySharp.TreeTransformations;

namespace FunnySharp.TreeTransformations.Tests;

public sealed class TreeNodeAdapterTests
{
    [Fact]
    public void ConstructorRejectsNullDelegates()
    {
        var children = Assert.Throws<ArgumentNullException>(
            () => new TreeNodeAdapter<ConfigNode>(
                null!,
                static (node, updated) => node,
                static (node, segment) => Option<int>.None,
                static (node, index) => TreePathSegment.At(index),
                static node => node.Name));
        var rebuild = Assert.Throws<ArgumentNullException>(
            () => new TreeNodeAdapter<ConfigNode>(
                static node => node.Children,
                null!,
                static (node, segment) => Option<int>.None,
                static (node, index) => TreePathSegment.At(index),
                static node => node.Name));
        var childIndex = Assert.Throws<ArgumentNullException>(
            () => new TreeNodeAdapter<ConfigNode>(
                static node => node.Children,
                static (node, updated) => node,
                null!,
                static (node, index) => TreePathSegment.At(index),
                static node => node.Name));
        var segmentAt = Assert.Throws<ArgumentNullException>(
            () => new TreeNodeAdapter<ConfigNode>(
                static node => node.Children,
                static (node, updated) => node,
                static (node, segment) => Option<int>.None,
                null!,
                static node => node.Name));
        var label = Assert.Throws<ArgumentNullException>(
            () => new TreeNodeAdapter<ConfigNode>(
                static node => node.Children,
                static (node, updated) => node,
                static (node, segment) => Option<int>.None,
                static (node, index) => TreePathSegment.At(index),
                null!));

        Assert.Equal("children", children.ParamName);
        Assert.Equal("rebuild", rebuild.ParamName);
        Assert.Equal("childIndex", childIndex.ParamName);
        Assert.Equal("segmentAt", segmentAt.ParamName);
        Assert.Equal("label", label.ParamName);
    }

    [Fact]
    public void ChildrenRejectsANullList()
    {
        var adapter = CreateAdapter(children: static node => null!);

        Assert.Throws<InvalidOperationException>(
            () => adapter.Children(ConfigNode.Leaf("value", "1")));
    }

    [Fact]
    public void RebuildRejectsNullChildren()
    {
        var adapter = CreateAdapter();

        Assert.Throws<ArgumentNullException>(
            () => adapter.Rebuild(ConfigNode.Leaf("value", "1"), null!));
    }

    [Fact]
    public void RebuildRejectsANullNode()
    {
        var adapter = CreateAdapter(rebuild: static (node, updated) => null!);

        Assert.Throws<InvalidOperationException>(
            () => adapter.Rebuild(ConfigNode.Leaf("value", "1"), []));
    }

    [Fact]
    public void LabelRejectsANullLabel()
    {
        var adapter = CreateAdapter(label: static node => null!);

        Assert.Throws<InvalidOperationException>(
            () => adapter.Label(ConfigNode.Leaf("value", "1")));
    }

    private static TreeNodeAdapter<ConfigNode> CreateAdapter(
        Func<ConfigNode, IReadOnlyList<ConfigNode>>? children = null,
        Func<ConfigNode, IReadOnlyList<ConfigNode>, ConfigNode>? rebuild = null,
        Func<ConfigNode, TreePathSegment, Option<int>>? childIndex = null,
        Func<ConfigNode, int, TreePathSegment>? segmentAt = null,
        Func<ConfigNode, string>? label = null)
    {
        children ??= static node => node.Children;
        rebuild ??= static (node, updated) => node with { Children = updated };
        childIndex ??= static (node, segment) => Option<int>.None;
        segmentAt ??= static (node, index) => TreePathSegment.At(index);
        label ??= static node => node.Name;

        return new TreeNodeAdapter<ConfigNode>(children, rebuild, childIndex, segmentAt, label);
    }
}
