#pragma warning disable FS0017

using System.Globalization;

namespace FunnySharp.TreeTransformations;

/// <summary>
/// An immutable, value-equal path from a tree root to one node. A path is a sequence of named and
/// indexed <see cref="TreePathSegment"/> values; the empty path addresses the root.
/// </summary>
/// <remarks>
/// The prototype parses the application's path syntax once so every compared consumer starts from
/// the same segments. A segment is either <c>.name</c> or <c>[index]</c>; the first segment is a
/// name. Names use letters, digits, underscores, and hyphens. Parsing is not part of the tree
/// mechanism and is not charged to any compared consumer.
/// </remarks>
public sealed class TreePath : IEquatable<TreePath>
{
    private readonly TreePathSegment[] _segments;

    private TreePath(TreePathSegment[] segments) => _segments = segments;

    /// <summary>Gets the empty path that addresses the root.</summary>
    public static TreePath Root { get; } = new([]);

    /// <summary>Gets the number of segments.</summary>
    public int Count => _segments.Length;

    /// <summary>Gets the segment at <paramref name="index"/>.</summary>
    /// <param name="index">The zero-based segment position.</param>
    /// <returns>The segment.</returns>
    public TreePathSegment this[int index] => _segments[index];

    /// <summary>Gets the segments in root-to-leaf order.</summary>
    public IReadOnlyList<TreePathSegment> Segments => _segments;

    /// <summary>Creates a path from segments, copying the sequence.</summary>
    /// <param name="segments">The non-null segment sequence.</param>
    /// <returns>A new path.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="segments"/> is null.</exception>
    public static TreePath FromSegments(IEnumerable<TreePathSegment> segments)
    {
        ArgumentNullException.ThrowIfNull(segments);

        return new([.. segments]);
    }

    internal static TreePath FromOwnedSegments(TreePathSegment[] segments) => new(segments);

    /// <summary>Appends one segment.</summary>
    /// <param name="segment">The segment to append.</param>
    /// <returns>A new path.</returns>
    public TreePath Append(TreePathSegment segment)
    {
        var appended = new TreePathSegment[_segments.Length + 1];
        _segments.CopyTo(appended, 0);
        appended[_segments.Length] = segment;

        return new(appended);
    }

    /// <summary>Renders the path as a <see cref="Location"/> such as <c>services[3].endpoints[0].port</c>.</summary>
    /// <returns>A new location with the path's segments.</returns>
    public Location ToLocation()
    {
        var location = Location.Root;

        foreach (var segment in _segments)
        {
            location = segment.AppendTo(location);
        }

        return location;
    }

    /// <summary>Parses the application's path syntax.</summary>
    /// <param name="text">The non-null path text; an empty string is the root path.</param>
    /// <returns>The parsed path.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="text"/> is null.</exception>
    /// <exception cref="FormatException">The text is not a valid path.</exception>
    public static TreePath Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (text.Length == 0)
        {
            return Root;
        }

        var segments = new List<TreePathSegment>();
        var position = 0;
        segments.Add(TreePathSegment.Named(ReadName(text, ref position)));

        while (position < text.Length)
        {
            if (text[position] == '.')
            {
                position++;
                segments.Add(TreePathSegment.Named(ReadName(text, ref position)));
                continue;
            }

            if (text[position] == '[')
            {
                position++;
                var start = position;

                while (position < text.Length && char.IsAsciiDigit(text[position]))
                {
                    position++;
                }

                if (position == start || position >= text.Length || text[position] != ']')
                {
                    throw new FormatException($"Invalid index segment at position {start - 1} in '{text}'.");
                }

                if (!int.TryParse(
                        text.AsSpan(start, position - start),
                        NumberStyles.None,
                        CultureInfo.InvariantCulture,
                        out var index))
                {
                    throw new FormatException($"Index segment at position {start - 1} is out of range in '{text}'.");
                }

                segments.Add(TreePathSegment.At(index));
                position++;
                continue;
            }

            throw new FormatException($"Unexpected character '{text[position]}' at position {position} in '{text}'.");
        }

        return new([.. segments]);
    }

    /// <inheritdoc />
    public bool Equals(TreePath? other) =>
        other is not null && _segments.AsSpan().SequenceEqual(other._segments);

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as TreePath);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();

        foreach (var segment in _segments)
        {
            hash.Add(segment);
        }

        return hash.ToHashCode();
    }

    /// <inheritdoc />
    public override string ToString() => ToLocation().ToString();

    /// <summary>Returns whether two paths are equal.</summary>
    /// <param name="left">The first path.</param>
    /// <param name="right">The second path.</param>
    /// <returns><see langword="true"/> when the paths are equal.</returns>
    public static bool operator ==(TreePath? left, TreePath? right)
    {
        if (right is null)
        {
            return left is null;
        }

        return right.Equals(left);
    }

    /// <summary>Returns whether two paths differ.</summary>
    /// <param name="left">The first path.</param>
    /// <param name="right">The second path.</param>
    /// <returns><see langword="true"/> when the paths differ.</returns>
    public static bool operator !=(TreePath? left, TreePath? right) => !(left == right);

    private static string ReadName(string text, ref int position)
    {
        var start = position;

        if (position >= text.Length || !IsNameStart(text[position]))
        {
            throw new FormatException($"Expected a name at position {position} in '{text}'.");
        }

        position++;

        while (position < text.Length && IsNamePart(text[position]))
        {
            position++;
        }

        return text[start..position];
    }

    private static bool IsNameStart(char value) => char.IsAsciiLetter(value) || value == '_';

    private static bool IsNamePart(char value) => char.IsAsciiLetterOrDigit(value) || value is '_' or '-';
}
