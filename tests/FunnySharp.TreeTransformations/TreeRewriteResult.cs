namespace FunnySharp.TreeTransformations;

/// <summary>Reports a path-directed rewrite's root and rebuild work.</summary>
/// <typeparam name="TNode">The caller's reference-type node.</typeparam>
/// <param name="Root">The resulting root; the original root when nothing changed.</param>
/// <param name="VisitedNodes">The number of nodes examined while resolving the path.</param>
/// <param name="RebuiltNodes">The number of new nodes created; zero when the edit was a no-op.</param>
/// <param name="SharedNodes">The number of sibling references copied without rebuilding.</param>
/// <param name="Changed">Whether the edit produced a different root.</param>
public readonly record struct TreeRewriteResult<TNode>(
    TNode Root,
    int VisitedNodes,
    int RebuiltNodes,
    int SharedNodes,
    bool Changed)
    where TNode : class;
