namespace FunnySharp.TreeTransformations;

/// <summary>Rewrites one node after its children have been rewritten.</summary>
/// <typeparam name="TNode">The caller's reference-type node.</typeparam>
/// <param name="node">The original node.</param>
/// <param name="originalChildren">The node's original ordered children.</param>
/// <param name="rewrittenChildren">The ordered children after their rewrites.</param>
/// <param name="path">The callback-scoped path view for the node.</param>
/// <returns>The rewritten node; return <paramref name="node"/> when nothing changed.</returns>
public delegate TNode TreeRewriteCallback<TNode>(
    TNode node,
    IReadOnlyList<TNode> originalChildren,
    IReadOnlyList<TNode> rewrittenChildren,
    TreePathView path)
    where TNode : class;
