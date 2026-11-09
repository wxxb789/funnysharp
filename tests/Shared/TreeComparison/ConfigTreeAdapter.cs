using FunnySharp;
using FunnySharp.TreeTransformations;

namespace FunnySharp.TestSupport.TreeComparison;

/// <summary>The adapter that describes the application's ordinary configuration node.</summary>
internal static class ConfigTreeAdapter
{
    internal static TreeNodeAdapter<ConfigNode> Instance { get; } = new(
        children: static node => node.Children,
        rebuild: static (node, children) => node with { Children = children },
        childIndex: static (node, segment) => FindChild(node, segment),
        segmentAt: static (node, index) => SegmentFor(node, index),
        label: static node => node.Name,
        identity: ReferenceEqualityComparer.Instance);

    internal static TreePathSegment SegmentFor(ConfigNode parent, int index) =>
        parent.Indexed
            ? TreePathSegment.At(index)
            : TreePathSegment.Named(parent.Children[index].Name);

    private static Option<int> FindChild(ConfigNode node, TreePathSegment segment)
    {
        if (node.Indexed)
        {
            return segment.Kind == TreePathSegmentKind.Index && segment.Index < node.Children.Count
                ? Option.Some(segment.Index)
                : Option<int>.None;
        }

        if (segment.Kind != TreePathSegmentKind.Name)
        {
            return Option<int>.None;
        }

        for (var index = 0; index < node.Children.Count; index++)
        {
            if (string.Equals(node.Children[index].Name, segment.Name, StringComparison.Ordinal))
            {
                return Option.Some(index);
            }
        }

        return Option<int>.None;
    }
}
