#pragma warning disable FS0017

namespace FunnySharp.TreeTransformations;

/// <summary>Thrown when a traversal or rewrite finds a node that is its own ancestor.</summary>
public sealed class TreeCycleException : Exception
{
    /// <summary>Creates the exception for the repeated node's location.</summary>
    /// <param name="location">The non-null location of the repeated node.</param>
    public TreeCycleException(Location location)
        : base($"The tree contains a cycle at '{location}'.")
    {
        Location = location ?? throw new ArgumentNullException(nameof(location));
    }

    /// <summary>Gets the location of the repeated node.</summary>
    public Location Location { get; }
}
