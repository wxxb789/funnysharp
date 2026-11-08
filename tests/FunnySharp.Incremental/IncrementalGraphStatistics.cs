namespace FunnySharp.Incremental;

/// <summary>
/// Cumulative counters for committed graph work. Initial construction is not counted; reset the
/// counters after setup to measure one update.
/// </summary>
public readonly record struct IncrementalGraphStatistics(
    long Commits,
    long InputChanges,
    long NodeEvaluations,
    long CutoffStops,
    long ChangedNodes,
    long ObserverNotifications,
    int NodeCount,
    int EdgeCount,
    int ObserverCount)
{
    /// <summary>
    /// Nodes not evaluated in the measured commit set. Valid only after a statistics reset and a
    /// single observed commit; it is a convenience for the research harness, not a promise across
    /// several commits.
    /// </summary>
    public int SkippedNodeEvaluations => Math.Max(0, NodeCount - (int)NodeEvaluations);
}