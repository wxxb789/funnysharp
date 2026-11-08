namespace FunnySharp.TestSupport.TreeComparison;

/// <summary>The deterministic work ledger for one complete apply or validation operation.</summary>
/// <param name="VisitedNodes">Nodes examined by path resolution, rewrite, or validation.</param>
/// <param name="RebuiltNodes">New nodes created by the operation.</param>
/// <param name="SharedNodes">Sibling references copied without rebuilding.</param>
/// <param name="OverridesApplied">Valid overrides that changed a leaf.</param>
/// <param name="Errors">Failures recorded by the operation.</param>
internal readonly record struct ConfigEditStats(
    int VisitedNodes,
    int RebuiltNodes,
    int SharedNodes,
    int OverridesApplied,
    int Errors);
