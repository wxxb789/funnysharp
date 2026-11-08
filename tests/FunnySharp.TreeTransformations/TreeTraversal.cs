#pragma warning disable FS0017

using FunnySharp;

namespace FunnySharp.TreeTransformations;

/// <summary>
/// Iterative pre-order and post-order traversal over a caller's node type. The traversal never
/// recurses, so depth is limited by heap memory rather than the call stack.
/// </summary>
/// <remarks>
/// The visitor receives a callback-scoped <see cref="TreePathView"/>; it should call
/// <see cref="TreePathView.ToLocation"/> only when it records a failure. In tree mode every
/// reference is a distinct occurrence; in DAG mode each distinct node is visited once and later
/// occurrences are counted as duplicates. An ancestor repeat is a cycle: it throws or stops
/// according to the options. The node limit is a final guard against unbounded input.
/// </remarks>
public static class TreeTraversal
{
    /// <summary>Traverses a tree or DAG from <paramref name="root"/>.</summary>
    /// <typeparam name="TNode">The caller's reference-type node.</typeparam>
    /// <param name="root">The non-null root.</param>
    /// <param name="adapter">The non-null node adapter.</param>
    /// <param name="visitor">The non-null visitor.</param>
    /// <param name="options">The optional traversal options.</param>
    /// <returns>The traversal result.</returns>
    /// <exception cref="ArgumentNullException">A required argument is null.</exception>
    /// <exception cref="TreeCycleException">A cycle was found under <see cref="TreeCyclePolicy.Throw"/>.</exception>
    /// <exception cref="TreeTraversalLimitException">The node limit was exceeded.</exception>
    public static TreeTraversalResult Traverse<TNode>(
        TNode root,
        TreeNodeAdapter<TNode> adapter,
        TreeVisitor<TNode> visitor,
        TreeTraversalOptions<TNode>? options = null)
        where TNode : class
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(adapter);
        ArgumentNullException.ThrowIfNull(visitor);

        options ??= new TreeTraversalOptions<TNode>();
        options.Validate();

        var identity = options.EffectiveIdentity(adapter);
        var active = new HashSet<TNode>(identity);
        var visited = options.Shape == TreeShape.Dag ? new HashSet<TNode>(identity) : null;
        var stack = new Frame<TNode>[16];
        var stackCount = 0;
        var path = new TreePathSegment[16];
        var pathLength = 0;
        var entered = 0;
        var visitedNodes = 0;
        var duplicateVisits = 0;
        var maxDepth = 0;
        var stopped = false;
        var cycleDetected = false;

        Push(ref stack, ref stackCount, new Frame<TNode>(root, depth: 0));

        while (stackCount > 0)
        {
            ref var frame = ref stack[stackCount - 1];
            var node = frame.Node;
            var depth = frame.Depth;

            if (!frame.Entered)
            {
                if (active.Contains(node))
                {
                    var location = new TreePathView(path, depth).ToLocation();

                    if (options.CyclePolicy == TreeCyclePolicy.Throw)
                    {
                        throw new TreeCycleException(location);
                    }

                    cycleDetected = true;
                    break;
                }

                if (visited is not null && visited.Contains(node))
                {
                    duplicateVisits++;
                    Pop(ref stackCount, stack, ref pathLength);
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

                if (visited is not null)
                {
                    visited.Add(node);
                }

                frame.Entered = true;
                frame.Children = adapter.Children(node);
                frame.NextChild = 0;

                if (options.Order == TreeVisitOrder.PreOrder)
                {
                    visitedNodes++;
                    maxDepth = Math.Max(maxDepth, depth);

                    if (visitor(node, depth, new TreePathView(path, depth)) == TreeVisitDecision.Stop)
                    {
                        stopped = true;
                        break;
                    }
                }

                if (frame.Children.Count == 0)
                {
                    if (options.Order == TreeVisitOrder.PostOrder)
                    {
                        visitedNodes++;
                        maxDepth = Math.Max(maxDepth, depth);

                        if (visitor(node, depth, new TreePathView(path, depth)) == TreeVisitDecision.Stop)
                        {
                            stopped = true;
                            break;
                        }
                    }

                    active.Remove(node);
                    Pop(ref stackCount, stack, ref pathLength);
                }

                continue;
            }

            if (frame.NextChild < frame.Children!.Count)
            {
                var childIndex = frame.NextChild;
                var segment = adapter.SegmentAt(node, childIndex);
                frame.NextChild = childIndex + 1;

                if (pathLength >= path.Length)
                {
                    Array.Resize(ref path, path.Length * 2);
                }

                path[pathLength++] = segment;
                Push(ref stack, ref stackCount, new Frame<TNode>(frame.Children[childIndex], depth + 1));
                continue;
            }

            if (options.Order == TreeVisitOrder.PostOrder)
            {
                visitedNodes++;
                maxDepth = Math.Max(maxDepth, depth);

                if (visitor(node, depth, new TreePathView(path, depth)) == TreeVisitDecision.Stop)
                {
                    stopped = true;
                    break;
                }
            }

            active.Remove(node);
            Pop(ref stackCount, stack, ref pathLength);
        }

        return new TreeTraversalResult(visitedNodes, duplicateVisits, maxDepth, stopped, cycleDetected);
    }

    private static void Push<TNode>(ref Frame<TNode>[] stack, ref int count, in Frame<TNode> frame)
        where TNode : class
    {
        if (count == stack.Length)
        {
            Array.Resize(ref stack, stack.Length * 2);
        }

        stack[count++] = frame;
    }

    private static void Pop<TNode>(ref int count, Frame<TNode>[] stack, ref int pathLength)
        where TNode : class
    {
        count--;
        pathLength = count == 0 ? 0 : stack[count - 1].Depth;
    }

    private struct Frame<TNode>
        where TNode : class
    {
        public Frame(TNode node, int depth)
        {
            Node = node;
            Depth = depth;
            Children = null;
            NextChild = 0;
            Entered = false;
        }

        public TNode Node;
        public int Depth;
        public IReadOnlyList<TNode>? Children;
        public int NextChild;
        public bool Entered;
    }
}
