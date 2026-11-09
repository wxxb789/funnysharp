#pragma warning disable FS0017

namespace FunnySharp.TreeTransformations;

/// <summary>Identifies how a path segment selects a child.</summary>
public enum TreePathSegmentKind : byte
{
    /// <summary>The segment selects a child by its adapter label.</summary>
    Name = 0,

    /// <summary>The segment selects a child by its zero-based position.</summary>
    Index = 1,
}

/// <summary>
/// One step in a <see cref="TreePath"/>: either a named child or an indexed child. The value is
/// immutable and value-equal; a name segment and an index segment are never equal.
/// </summary>
public readonly struct TreePathSegment : IEquatable<TreePathSegment>
{
    private TreePathSegment(TreePathSegmentKind kind, string? name, int index)
    {
        Kind = kind;
        Name = name;
        Index = index;
    }

    /// <summary>Gets the segment kind.</summary>
    public TreePathSegmentKind Kind { get; }

    /// <summary>Gets the child name for a <see cref="TreePathSegmentKind.Name"/> segment.</summary>
    public string? Name { get; }

    /// <summary>Gets the child position for a <see cref="TreePathSegmentKind.Index"/> segment.</summary>
    public int Index { get; }

    /// <summary>Creates a named segment.</summary>
    /// <param name="name">The non-null, non-empty child name.</param>
    /// <returns>A name segment.</returns>
    /// <exception cref="ArgumentException"><paramref name="name"/> is null or empty.</exception>
    public static TreePathSegment Named(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        return new(TreePathSegmentKind.Name, name, -1);
    }

    /// <summary>Creates an indexed segment.</summary>
    /// <param name="index">The non-negative child position.</param>
    /// <returns>An index segment.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is negative.</exception>
    public static TreePathSegment At(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        return new(TreePathSegmentKind.Index, null, index);
    }

    /// <summary>Appends this segment to a location.</summary>
    /// <param name="location">The non-null parent location.</param>
    /// <returns>The parent location with this segment appended.</returns>
    public Location AppendTo(Location location)
    {
        ArgumentNullException.ThrowIfNull(location);

        return Kind == TreePathSegmentKind.Name
            ? location.Property(Name!)
            : location.At(Index);
    }

    /// <inheritdoc />
    public bool Equals(TreePathSegment other) =>
        Kind == other.Kind
        && Index == other.Index
        && string.Equals(Name, other.Name, StringComparison.Ordinal);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is TreePathSegment other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Kind, Name, Index);

    /// <inheritdoc />
    public override string ToString() =>
        Kind == TreePathSegmentKind.Name ? Name! : $"[{Index}]";

    /// <summary>Returns whether two segments are equal.</summary>
    /// <param name="left">The first segment.</param>
    /// <param name="right">The second segment.</param>
    /// <returns><see langword="true"/> when the segments are equal.</returns>
    public static bool operator ==(TreePathSegment left, TreePathSegment right) => left.Equals(right);

    /// <summary>Returns whether two segments differ.</summary>
    /// <param name="left">The first segment.</param>
    /// <param name="right">The second segment.</param>
    /// <returns><see langword="true"/> when the segments differ.</returns>
    public static bool operator !=(TreePathSegment left, TreePathSegment right) => !left.Equals(right);
}
