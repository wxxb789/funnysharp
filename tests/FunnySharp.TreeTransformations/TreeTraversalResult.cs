namespace FunnySharp.TreeTransformations;

/// <summary>Reports what a traversal observed.</summary>
/// <param name="VisitedNodes">The number of visitor calls.</param>
/// <param name="DuplicateVisits">The number of DAG occurrences skipped because the node was seen.</param>
/// <param name="MaxDepth">The largest root-relative depth visited.</param>
/// <param name="Stopped">Whether the visitor returned <see cref="TreeVisitDecision.Stop"/>.</param>
/// <param name="CycleDetected">Whether an ancestor repeat was detected under <see cref="TreeCyclePolicy.Stop"/>.</param>
public readonly record struct TreeTraversalResult(
    int VisitedNodes,
    int DuplicateVisits,
    int MaxDepth,
    bool Stopped,
    bool CycleDetected);
