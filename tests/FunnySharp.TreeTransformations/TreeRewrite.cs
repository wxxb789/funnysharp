#pragma warning disable FS0017

using FunnySharp;

namespace FunnySharp.TreeTransformations;

/// <summary>
/// Iterative bottom-up rewriting and path-directed editing over a caller's node type. Neither
/// operation recurses, so deep trees do not exhaust the call stack.
/// </summary>
/// <remarks>
/// <see cref="RewriteBottomUp"/> calls the callback for every visited node after its children. The
/// callback returns the original node to signal an unchanged node; the caller decides whether to
/// rebuild a parent from its rewritten children. DAG mode memoizes a rewritten node by identity so
/// a shared node stays shared. A cycle cannot produce a finite rewrite and always throws
/// <see cref="TreeCycleException"/>; <see cref="TreeTraversalOptions{TNode}.CyclePolicy"/> and
/// <see cref="TreeTraversalOptions{TNode}.Order"/> do not change rewrite behavior.
/// <see cref="RewriteAt"/> resolves one path and rebuilds only its ancestors, sharing every sibling.
/// </remarks>
public static class TreeRewrite
{
    /// <summary>Rewrites a tree bottom-up without recursion.</summary>
    /// <typeparam name="TNode">The caller's reference-type node.</typeparam>
    /// <param name="root">The non-null root.</param>
    /// <param name="adapter">The non-null node adapter.</param>
    /// <param name="rewrite">The non-null rewrite callback.</param>
    /// <param name="options">The optional shape, identity, and limit options.</param>
    /// <returns>The rewritten root and traversal counts.</returns>
    /// <exception cref="ArgumentNullException">A required argument is null.</exception>
    /// <exception cref="TreeCycleException">The input contains a cycle.</exception>
    /// <exception cref="TreeTraversalLimitException">The node limit was exceeded.</exception>
    public static TreeRewriteTraversalResult<TNode> RewriteBottomUp<TNode>(
        TNode root,
        TreeNodeAdapter<TNode> adapter,
        TreeRewriteCallback<TNode> rewrite,
        TreeTraversalOptions<TNode>? options = null)
        where TNode : class
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(adapter);
        ArgumentNullException.ThrowIfNull(rewrite);

        options ??= new TreeTraversalOptions<TNode>();
        options.Validate();

        var identity = options.EffectiveIdentity(adapter);
        var active = new HashSet<TNode>(identity);
        var memo = options.Shape == TreeShape.Dag ? new Dictionary<TNode, TNode>(identity) : null;
        var stack = new RewriteFrame<TNode>[16];
        var stackCount = 0;
        var path = new TreePathSegment[16];
        var pathLength = 0;
        var entered = 0;
        var visitedNodes = 0;
        var duplicateVisits = 0;
        var maxDepth = 0;
        TNode? rootResult = null;

        Push(ref stack, ref stackCount, new RewriteFrame<TNode>(root, depth: 0));

        while (stackCount > 0)
        {
            ref var frame = ref stack[stackCount - 1];
            var node = frame.Node;
            var depth = frame.Depth;

            if (!frame.Entered)
            {
                if (active.Contains(node))
                {
                    throw new TreeCycleException(new TreePathView(path, depth).ToLocation());
                }

                if (memo is not null && memo.TryGetValue(node, out var existing))
                {
                    duplicateVisits++;
                    Pop(ref stackCount, stack, ref pathLength);

                    if (stackCount == 0)
                    {
                        rootResult = existing;
                        break;
                    }

                    Deliver(ref stack, stackCount, existing);
                    continue;
                }

                entered++;

                if (entered > options.MaxNodes)
                {
                    throw new TreeTraversalLimitException(
                        options.MaxNodes,
                        new TreePathView(path, depth).ToLocation());
                }

                active.Add(node);
                frame.Entered = true;
                frame.Children = adapter.Children(node);
                frame.Rewritten = new TNode[frame.Children.Count];
                frame.NextChild = 0;

                if (frame.Children.Count == 0)
                {
                    var finished = Finish(
                        node,
                        frame.Children,
                        frame.Rewritten,
                        new TreePathView(path, depth),
                        rewrite);

                    if (memo is not null)
                    {
                        memo[node] = finished;
                    }

                    active.Remove(node);
                    visitedNodes++;
                    maxDepth = Math.Max(maxDepth, depth);
                    Pop(ref stackCount, stack, ref pathLength);

                    if (stackCount == 0)
                    {
                        rootResult = finished;
                        break;
                    }

                    Deliver(ref stack, stackCount, finished);
                    continue;
                }

                var firstChild = frame.Children[0];
                var firstSegment = adapter.SegmentAt(node, 0);
                frame.NextChild = 1;
                AppendSegment(ref path, ref pathLength, firstSegment);
                Push(ref stack, ref stackCount, new RewriteFrame<TNode>(firstChild, depth + 1));
                continue;
            }

            if (frame.NextChild < frame.Children!.Count)
            {
                var childIndex = frame.NextChild;
                var child = frame.Children[childIndex];
                var segment = adapter.SegmentAt(node, childIndex);
                frame.NextChild = childIndex + 1;
                AppendSegment(ref path, ref pathLength, segment);
                Push(ref stack, ref stackCount, new RewriteFrame<TNode>(child, depth + 1));
                continue;
            }

            var completed = Finish(
                node,
                frame.Children!,
                frame.Rewritten!,
                new TreePathView(path, depth),
                rewrite);

            if (memo is not null)
            {
                memo[node] = completed;
            }

            active.Remove(node);
            visitedNodes++;
            maxDepth = Math.Max(maxDepth, depth);
            Pop(ref stackCount, stack, ref pathLength);

            if (stackCount == 0)
            {
                rootResult = completed;
                break;
            }

            Deliver(ref stack, stackCount, completed);
        }

        return new TreeRewriteTraversalResult<TNode>(rootResult!, visitedNodes, duplicateVisits, maxDepth);
    }

    /// <summary>Resolves one path without rebuilding.</summary>
    /// <typeparam name="TNode">The caller's reference-type node.</typeparam>
    /// <param name="root">The non-null root.</param>
    /// <param name="adapter">The non-null node adapter.</param>
    /// <param name="path">The non-null path; the empty path resolves the root.</param>
    /// <returns>The resolved node and location, or a typed path failure.</returns>
    /// <exception cref="ArgumentNullException">A required argument is null.</exception>
    public static Result<TreeResolvedNode<TNode>, TreeEditError> Resolve<TNode>(
        TNode root,
        TreeNodeAdapter<TNode> adapter,
        TreePath path)
        where TNode : class
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(adapter);
        ArgumentNullException.ThrowIfNull(path);

        var current = root;

        for (var index = 0; index < path.Count; index++)
        {
            var segment = path[index];
            var children = adapter.Children(current);
            var childIndex = adapter.ChildIndex(current, segment);

            if (!childIndex.TryGetValue(out var position) || (uint)position >= (uint)children.Count)
            {
                return Result<TreeResolvedNode<TNode>, TreeEditError>.Failure(
                    PathError(path, index, segment));
            }

            current = children[position];
        }

        return Result<TreeResolvedNode<TNode>, TreeEditError>.Success(new(current, path));
    }

    /// <summary>Edits one path and rebuilds only its ancestors.</summary>
    /// <typeparam name="TNode">The caller's reference-type node.</typeparam>
    /// <typeparam name="TError">The caller's edit error type.</typeparam>
    /// <param name="root">The non-null root.</param>
    /// <param name="adapter">The non-null node adapter.</param>
    /// <param name="path">The non-null path; the empty path edits the root.</param>
    /// <param name="edit">The non-null edit callback; it may reject the resolved node.</param>
    /// <param name="errorFactory">Maps a path failure to the caller's error type.</param>
    /// <returns>The resulting root and rebuild counts, or an edit failure.</returns>
    /// <exception cref="ArgumentNullException">A required argument is null.</exception>
    /// <exception cref="InvalidOperationException">The edit callback returned a null node.</exception>
    public static Result<TreeRewriteResult<TNode>, TError> RewriteAt<TNode, TError>(
        TNode root,
        TreeNodeAdapter<TNode> adapter,
        TreePath path,
        Func<TreeResolvedNode<TNode>, Result<TNode, TError>> edit,
        Func<TreeEditErrorKind, Location, TError> errorFactory)
        where TNode : class
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(adapter);
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(edit);
        ArgumentNullException.ThrowIfNull(errorFactory);

        var frames = new EditFrame<TNode>[path.Count];
        var current = root;

        for (var index = 0; index < path.Count; index++)
        {
            var segment = path[index];
            var children = adapter.Children(current);
            var childIndex = adapter.ChildIndex(current, segment);

            if (!childIndex.TryGetValue(out var position) || (uint)position >= (uint)children.Count)
            {
                var failure = PathError(path, index, segment);
                return Result<TreeRewriteResult<TNode>, TError>.Failure(
                    errorFactory(failure.Kind, failure.Location));
            }

            frames[index] = new EditFrame<TNode>(current, children, position);
            current = children[position];
        }

        var edited = edit(new TreeResolvedNode<TNode>(current, path));

        if (edited.TryGetError(out var editError))
        {
            return Result<TreeRewriteResult<TNode>, TError>.Failure(editError);
        }

        if (!edited.TryGetValue(out var editedNode) || editedNode is null)
        {
            throw new InvalidOperationException("The edit callback returned a null node.");
        }

        if (ReferenceEquals(editedNode, current))
        {
            return Result<TreeRewriteResult<TNode>, TError>.Success(
                new TreeRewriteResult<TNode>(root, path.Count + 1, 0, 0, Changed: false));
        }

        var rebuilt = 1;
        var shared = 0;
        var newChild = editedNode;

        for (var index = frames.Length - 1; index >= 0; index--)
        {
            var frame = frames[index];
            var newChildren = new TNode[frame.Children.Count];

            for (var childIndex = 0; childIndex < newChildren.Length; childIndex++)
            {
                newChildren[childIndex] = frame.Children[childIndex];
            }

            newChildren[frame.ChildIndex] = newChild;
            shared += frame.Children.Count - 1;
            newChild = adapter.Rebuild(frame.Node, newChildren);
            rebuilt++;
        }

        return Result<TreeRewriteResult<TNode>, TError>.Success(
            new TreeRewriteResult<TNode>(newChild, path.Count + 1, rebuilt, shared, Changed: true));
    }

    /// <summary>Edits one path with the prototype's edit error type.</summary>
    /// <typeparam name="TNode">The caller's reference-type node.</typeparam>
    /// <param name="root">The non-null root.</param>
    /// <param name="adapter">The non-null node adapter.</param>
    /// <param name="path">The non-null path; the empty path edits the root.</param>
    /// <param name="edit">The non-null edit callback.</param>
    /// <returns>The resulting root and rebuild counts, or a path failure.</returns>
    public static Result<TreeRewriteResult<TNode>, TreeEditError> RewriteAt<TNode>(
        TNode root,
        TreeNodeAdapter<TNode> adapter,
        TreePath path,
        Func<TNode, TNode> edit)
        where TNode : class
    {
        ArgumentNullException.ThrowIfNull(edit);

        return RewriteAt<TNode, TreeEditError>(
            root,
            adapter,
            path,
            resolved => Result<TNode, TreeEditError>.Success(edit(resolved.Node)),
            static (kind, location) => PathError(kind, location));
    }

    private static TreeEditError PathError(TreePath path, int failingIndex, TreePathSegment segment) =>
        PathError(
            segment.Kind == TreePathSegmentKind.Name
                ? TreeEditErrorKind.PathNotFound
                : TreeEditErrorKind.IndexOutOfRange,
            LocationAt(path, failingIndex));

    private static TreeEditError PathError(TreeEditErrorKind kind, Location location) =>
        new(kind, location, $"The path segment at '{location}' did not resolve.");

    private static Location LocationAt(TreePath path, int failingIndex) =>
        TreePath.FromSegments(path.Segments.Take(failingIndex + 1)).ToLocation();

    private static TNode Finish<TNode>(
        TNode node,
        IReadOnlyList<TNode> originalChildren,
        IReadOnlyList<TNode> rewrittenChildren,
        TreePathView path,
        TreeRewriteCallback<TNode> rewrite)
        where TNode : class
    {
        var result = rewrite(node, originalChildren, rewrittenChildren, path);

        return result ?? throw new InvalidOperationException("The rewrite callback returned a null node.");
    }

    private static void Deliver<TNode>(ref RewriteFrame<TNode>[] stack, int stackCount, TNode completed)
        where TNode : class
    {
        ref var parent = ref stack[stackCount - 1];
        parent.Rewritten![parent.NextChild - 1] = completed;
    }

    private static void AppendSegment(
        ref TreePathSegment[] path,
        ref int pathLength,
        TreePathSegment segment)
    {
        if (pathLength >= path.Length)
        {
            Array.Resize(ref path, path.Length * 2);
        }

        path[pathLength++] = segment;
    }

    private static void Push<TNode>(ref RewriteFrame<TNode>[] stack, ref int count, in RewriteFrame<TNode> frame)
        where TNode : class
    {
        if (count == stack.Length)
        {
            Array.Resize(ref stack, stack.Length * 2);
        }

        stack[count++] = frame;
    }

    private static void Pop<TNode>(ref int count, RewriteFrame<TNode>[] stack, ref int pathLength)
        where TNode : class
    {
        count--;
        pathLength = count == 0 ? 0 : stack[count - 1].Depth;
    }

    private struct EditFrame<TNode>
        where TNode : class
    {
        public EditFrame(TNode node, IReadOnlyList<TNode> children, int childIndex)
        {
            Node = node;
            Children = children;
            ChildIndex = childIndex;
        }

        public TNode Node;
        public IReadOnlyList<TNode> Children;
        public int ChildIndex;
    }

    private struct RewriteFrame<TNode>
        where TNode : class
    {
        public RewriteFrame(TNode node, int depth)
        {
            Node = node;
            Depth = depth;
            Children = null;
            Rewritten = null;
            NextChild = 0;
            Entered = false;
        }

        public TNode Node;
        public int Depth;
        public IReadOnlyList<TNode>? Children;
        public TNode[]? Rewritten;
        public int NextChild;
        public bool Entered;
    }
}
