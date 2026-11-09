#pragma warning disable FS0017

namespace FunnySharp.TreeTransformations;

/// <summary>
/// An immutable cursor over a caller's tree: navigation returns a new cursor, an edit returns a new
/// cursor, and <see cref="ToRoot"/> rebuilds only the path from the cursor to the root while sharing
/// every sibling subtree.
/// </summary>
/// <typeparam name="TNode">The caller's reference-type node.</typeparam>
/// <remarks>
/// Navigation is by child position, not equality. A shared DAG node reachable by two paths has two
/// independent cursors; editing one path leaves the other occurrence untouched. The cursor assumes
/// a tree or DAG: following a cycle downward is unbounded and is not detected. The cursor captures
/// each parent's child-list reference when it descends; mutating that list afterwards is visible and
/// is not defended against. Reusing nodes is reference sharing, not deep immutability.
/// </remarks>
public sealed class Zipper<TNode>
    where TNode : class
{
    private readonly TreeNodeAdapter<TNode> _adapter;
    private readonly TNode _root;
    private readonly TNode _current;
    private readonly Frame? _frame;

    private Zipper(TreeNodeAdapter<TNode> adapter, TNode root, TNode current, Frame? frame)
    {
        _adapter = adapter;
        _root = root;
        _current = current;
        _frame = frame;
    }

    /// <summary>Gets the current node.</summary>
    public TNode Current => _current;

    /// <summary>Gets the current depth; the root is zero.</summary>
    public int Depth => _frame?.Depth ?? 0;

    /// <summary>Gets whether the cursor is at the root.</summary>
    public bool IsRoot => _frame is null;

    /// <summary>Gets the path from the root to the current node.</summary>
    public TreePath Path
    {
        get
        {
            var segments = new TreePathSegment[Depth];
            var frame = _frame;

            for (var index = Depth - 1; index >= 0; index--)
            {
                segments[index] = frame!.Segment;
                frame = frame.Previous;
            }

            return TreePath.FromOwnedSegments(segments);
        }
    }

    /// <summary>Gets the current location.</summary>
    public Location Location => Path.ToLocation();

    /// <summary>Creates a cursor at a root.</summary>
    /// <param name="root">The non-null root.</param>
    /// <param name="adapter">The non-null node adapter.</param>
    /// <returns>A root cursor.</returns>
    /// <exception cref="ArgumentNullException">A required argument is null.</exception>
    public static Zipper<TNode> FromRoot(TNode root, TreeNodeAdapter<TNode> adapter)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(adapter);

        return new Zipper<TNode>(adapter, root, root, frame: null);
    }

    /// <summary>Moves to the child at a position.</summary>
    /// <param name="index">The zero-based child position.</param>
    /// <returns>The child cursor, or <c>None</c> when the position is out of range.</returns>
    public Option<Zipper<TNode>> Down(int index)
    {
        var children = _adapter.Children(_current);

        if ((uint)index >= (uint)children.Count)
        {
            return Option<Zipper<TNode>>.None;
        }

        return Option.Some(DownCore(index, _adapter.SegmentAt(_current, index), children));
    }

    /// <summary>Moves to the child selected by a path segment.</summary>
    /// <param name="segment">The path segment.</param>
    /// <returns>The child cursor, or <c>None</c> when the segment selects no child.</returns>
    public Option<Zipper<TNode>> Down(TreePathSegment segment)
    {
        var childIndex = _adapter.ChildIndex(_current, segment);

        if (!childIndex.TryGetValue(out var index))
        {
            return Option<Zipper<TNode>>.None;
        }

        var children = _adapter.Children(_current);

        if ((uint)index >= (uint)children.Count)
        {
            return Option<Zipper<TNode>>.None;
        }

        return Option.Some(DownCore(index, segment, children));
    }

    /// <summary>Moves to the parent, preserving an edit to the current node.</summary>
    /// <returns>The parent cursor, or <c>None</c> at the root.</returns>
    public Option<Zipper<TNode>> Up()
    {
        if (_frame is null)
        {
            return Option<Zipper<TNode>>.None;
        }

        var (parent, _) = MaterializeParent();

        return Option.Some(new Zipper<TNode>(_adapter, _root, parent, _frame.Previous));
    }

    /// <summary>Moves to the previous sibling, preserving an edit to the current node.</summary>
    /// <returns>The sibling cursor, or <c>None</c> at the first child.</returns>
    public Option<Zipper<TNode>> Left() => MoveSibling(-1);

    /// <summary>Moves to the next sibling, preserving an edit to the current node.</summary>
    /// <returns>The sibling cursor, or <c>None</c> at the last child.</returns>
    public Option<Zipper<TNode>> Right() => MoveSibling(1);

    /// <summary>Replaces the current node.</summary>
    /// <param name="node">The non-null replacement node.</param>
    /// <returns>A new cursor at the same path.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="node"/> is null.</exception>
    public Zipper<TNode> Replace(TNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        return new Zipper<TNode>(_adapter, _root, node, _frame);
    }

    /// <summary>Removes the current node and returns a cursor at its rebuilt parent.</summary>
    /// <returns>The parent cursor, or <c>None</c> when the cursor is at the root.</returns>
    public Option<Zipper<TNode>> Remove()
    {
        if (_frame is null)
        {
            return Option<Zipper<TNode>>.None;
        }

        var frame = _frame;
        var newChildren = new TNode[frame.Children.Count - 1];
        var target = 0;

        for (var index = 0; index < frame.Children.Count; index++)
        {
            if (index != frame.Index)
            {
                newChildren[target++] = frame.Children[index];
            }
        }

        var parent = _adapter.Rebuild(frame.Parent, newChildren);

        return Option.Some(new Zipper<TNode>(_adapter, _root, parent, frame.Previous));
    }

    /// <summary>Rebuilds the root, sharing every sibling subtree.</summary>
    /// <returns>The reconstructed root and rebuild counts.</returns>
    public ZipperRootResult<TNode> ToRoot()
    {
        var current = _current;
        var frame = _frame;
        var rebuilt = 0;
        var shared = 0;

        while (frame is not null)
        {
            if (ReferenceEquals(current, frame.Children[frame.Index]))
            {
                current = frame.Parent;
            }
            else
            {
                var newChildren = ReplaceChild(frame.Children, frame.Index, current);
                shared += frame.Children.Count - 1;
                current = _adapter.Rebuild(frame.Parent, newChildren);
                rebuilt++;
            }

            frame = frame.Previous;
        }

        return new ZipperRootResult<TNode>(current, rebuilt, shared, !ReferenceEquals(current, _root));
    }

    private Zipper<TNode> DownCore(int index, TreePathSegment segment, IReadOnlyList<TNode> children) =>
        new(
            _adapter,
            _root,
            children[index],
            new Frame(_current, children, index, segment, _frame, Depth + 1));

    private Option<Zipper<TNode>> MoveSibling(int delta)
    {
        if (_frame is null)
        {
            return Option<Zipper<TNode>>.None;
        }

        var newIndex = _frame.Index + delta;

        if ((uint)newIndex >= (uint)_frame.Children.Count)
        {
            return Option<Zipper<TNode>>.None;
        }

        var (parent, children) = MaterializeParent();
        var segment = _adapter.SegmentAt(parent, newIndex);

        return Option.Some(
            new Zipper<TNode>(
                _adapter,
                _root,
                children[newIndex],
                new Frame(parent, children, newIndex, segment, _frame.Previous, Depth)));
    }

    private (TNode Parent, IReadOnlyList<TNode> Children) MaterializeParent()
    {
        var frame = _frame!;

        if (ReferenceEquals(_current, frame.Children[frame.Index]))
        {
            return (frame.Parent, frame.Children);
        }

        var newChildren = ReplaceChild(frame.Children, frame.Index, _current);
        var parent = _adapter.Rebuild(frame.Parent, newChildren);

        return (parent, _adapter.Children(parent));
    }

    private static TNode[] ReplaceChild(IReadOnlyList<TNode> children, int index, TNode value)
    {
        var newChildren = new TNode[children.Count];

        for (var childIndex = 0; childIndex < newChildren.Length; childIndex++)
        {
            newChildren[childIndex] = children[childIndex];
        }

        newChildren[index] = value;

        return newChildren;
    }

    private sealed class Frame
    {
        public Frame(
            TNode parent,
            IReadOnlyList<TNode> children,
            int index,
            TreePathSegment segment,
            Frame? previous,
            int depth)
        {
            Parent = parent;
            Children = children;
            Index = index;
            Segment = segment;
            Previous = previous;
            Depth = depth;
        }

        public TNode Parent { get; }

        public IReadOnlyList<TNode> Children { get; }

        public int Index { get; }

        public TreePathSegment Segment { get; }

        public Frame? Previous { get; }

        public int Depth { get; }
    }
}
