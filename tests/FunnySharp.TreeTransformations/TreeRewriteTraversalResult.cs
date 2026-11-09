namespace FunnySharp.TreeTransformations;

/// <summary>Reports a bottom-up rewrite's root and traversal work.</summary>
/// <typeparam name="TNode">The caller's reference-type node.</typeparam>
/// <param name="Root">The rewritten root.</param>
/// <param name="VisitedNodes">The number of rewrite callback calls.</param>
/// <param name="DuplicateVisits">The number of DAG occurrences skipped because the node was seen.</param>
/// <param name="MaxDepth">The largest root-relative depth visited.</param>
public readonly record struct TreeRewriteTraversalResult<TNode>(
    TNode Root,
    int VisitedNodes,
    int DuplicateVisits,
    int MaxDepth)
    where TNode : class;
