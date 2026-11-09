#pragma warning disable FS0017

using FunnySharp;
using FunnySharp.TreeTransformations;

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

/// <summary>Computes the resolution nodes a consumer examined before the path resolved or failed.</summary>
internal static class ConfigResolution
{
    /// <summary>Returns the work of a resolution that reached the target node.</summary>
    internal static int Visited(TreePath path) => path.Count + 1;

    /// <summary>Returns the work of a resolution that failed at a path prefix.</summary>
    internal static int Visited(TreePath path, Location failure)
    {
        var location = Location.Root;

        for (var index = 0; index < path.Count; index++)
        {
            location = path[index].AppendTo(location);

            if (location.Equals(failure))
            {
                return index + 1;
            }
        }

        throw new InvalidOperationException("The failure location is not a prefix of the path.");
    }
}
