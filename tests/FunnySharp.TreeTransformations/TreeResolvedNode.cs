#pragma warning disable FS0017

namespace FunnySharp.TreeTransformations;

/// <summary>A resolved node and its path.</summary>
/// <typeparam name="TNode">The caller's reference-type node.</typeparam>
/// <param name="Node">The resolved node.</param>
/// <param name="Path">The path from the root to the node.</param>
/// <remarks>
/// <see cref="Location"/> is computed on demand. Building a location for a very deep path is
/// quadratic in the path length because <see cref="Location"/> appends one segment at a time; a
/// caller that records a failure on a very deep path pays that cost, while a successful deep path
/// pays nothing.
/// </remarks>
public readonly record struct TreeResolvedNode<TNode>(TNode Node, TreePath Path)
    where TNode : class
{
    /// <summary>Gets the node's location, built on demand.</summary>
    public Location Location => Path.ToLocation();
}
