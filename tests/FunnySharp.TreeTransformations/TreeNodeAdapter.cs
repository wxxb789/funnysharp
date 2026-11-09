namespace FunnySharp.TreeTransformations;

/// <summary>
/// Describes how a caller's ordinary node type exposes children, rebuilds a node, maps a path
/// segment to a child position, and labels a child. The adapter is the only place a reusable
/// traversal needs application knowledge; it never replaces the caller's node type or collection.
/// </summary>
/// <typeparam name="TNode">The caller's reference-type node.</typeparam>
/// <remarks>
/// <see cref="Rebuild"/> receives the same number of children in the same order as
/// <see cref="Children"/> returned. It may retain the supplied list; the prototype always passes a
/// fresh array. <see cref="Identity"/> defaults to reference equality because record structural
/// equality is usually wrong for DAG deduplication and can recurse without bound on a cycle.
/// </remarks>
public sealed class TreeNodeAdapter<TNode>
    where TNode : class
{
    private readonly Func<TNode, IReadOnlyList<TNode>> _children;
    private readonly Func<TNode, IReadOnlyList<TNode>, TNode> _rebuild;
    private readonly Func<TNode, TreePathSegment, Option<int>> _childIndex;
    private readonly Func<TNode, int, TreePathSegment> _segmentAt;
    private readonly Func<TNode, string> _label;

    /// <summary>Creates an adapter over a caller's node type.</summary>
    /// <param name="children">Returns the non-null ordered child list for a node.</param>
    /// <param name="rebuild">Returns a node with the supplied ordered children.</param>
    /// <param name="childIndex">Maps a path segment to a child position, or <c>None</c>.</param>
    /// <param name="segmentAt">Returns the path segment for the child at a position.</param>
    /// <param name="label">Returns the child label used for named paths.</param>
    /// <param name="identity">The optional identity comparer; the default is reference equality.</param>
    /// <exception cref="ArgumentNullException">A required delegate is null.</exception>
    public TreeNodeAdapter(
        Func<TNode, IReadOnlyList<TNode>> children,
        Func<TNode, IReadOnlyList<TNode>, TNode> rebuild,
        Func<TNode, TreePathSegment, Option<int>> childIndex,
        Func<TNode, int, TreePathSegment> segmentAt,
        Func<TNode, string> label,
        IEqualityComparer<TNode>? identity = null)
    {
        _children = children ?? throw new ArgumentNullException(nameof(children));
        _rebuild = rebuild ?? throw new ArgumentNullException(nameof(rebuild));
        _childIndex = childIndex ?? throw new ArgumentNullException(nameof(childIndex));
        _segmentAt = segmentAt ?? throw new ArgumentNullException(nameof(segmentAt));
        _label = label ?? throw new ArgumentNullException(nameof(label));
        Identity = identity ?? (IEqualityComparer<TNode>)ReferenceEqualityComparer.Instance;
    }

    /// <summary>Gets the identity comparer used for DAG deduplication and cycle checks.</summary>
    public IEqualityComparer<TNode> Identity { get; }

    /// <summary>Returns the ordered children of a node.</summary>
    /// <param name="node">The non-null node.</param>
    /// <returns>The non-null child list.</returns>
    /// <exception cref="InvalidOperationException">The adapter returned a null child list.</exception>
    public IReadOnlyList<TNode> Children(TNode node) =>
        _children(node) ?? throw new InvalidOperationException("The adapter returned a null child list.");

    /// <summary>Returns a node with the supplied ordered children.</summary>
    /// <param name="node">The non-null node.</param>
    /// <param name="children">The non-null replacement child list.</param>
    /// <returns>The rebuilt node.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="children"/> is null.</exception>
    /// <exception cref="InvalidOperationException">The adapter returned a null node.</exception>
    public TNode Rebuild(TNode node, IReadOnlyList<TNode> children)
    {
        ArgumentNullException.ThrowIfNull(children);

        return _rebuild(node, children)
            ?? throw new InvalidOperationException("The adapter returned a null rebuilt node.");
    }

    /// <summary>Maps a path segment to a child position.</summary>
    /// <param name="node">The non-null parent node.</param>
    /// <param name="segment">The path segment.</param>
    /// <returns>The child position, or <c>None</c> when the segment does not select a child.</returns>
    public Option<int> ChildIndex(TNode node, TreePathSegment segment) => _childIndex(node, segment);

    /// <summary>Returns the path segment for a child position.</summary>
    /// <param name="node">The non-null parent node.</param>
    /// <param name="index">The zero-based child position.</param>
    /// <returns>The segment that selects that child.</returns>
    public TreePathSegment SegmentAt(TNode node, int index) => _segmentAt(node, index);

    /// <summary>Returns the label used for a named path segment.</summary>
    /// <param name="node">The non-null node.</param>
    /// <returns>The non-null label.</returns>
    /// <exception cref="InvalidOperationException">The adapter returned a null label.</exception>
    public string Label(TNode node) =>
        _label(node) ?? throw new InvalidOperationException("The adapter returned a null label.");
}
