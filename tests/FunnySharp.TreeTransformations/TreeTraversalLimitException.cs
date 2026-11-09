#pragma warning disable FS0017

namespace FunnySharp.TreeTransformations;

/// <summary>Thrown when a traversal enters more nodes than the configured limit.</summary>
public sealed class TreeTraversalLimitException : Exception
{
    /// <summary>Creates the exception for the configured limit and current location.</summary>
    /// <param name="limit">The positive node limit.</param>
    /// <param name="location">The non-null location that exceeded the limit.</param>
    public TreeTraversalLimitException(int limit, Location location)
        : base($"The traversal exceeded the node limit of {limit} at '{location}'.")
    {
        Limit = limit;
        Location = location ?? throw new ArgumentNullException(nameof(location));
    }

    /// <summary>Gets the configured node limit.</summary>
    public int Limit { get; }

    /// <summary>Gets the location that exceeded the limit.</summary>
    public Location Location { get; }
}
