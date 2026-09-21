using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace FunnySharp;

/// <summary>
/// The compositional location context a traversal failure can retain, rendered as a path such as
/// <c>customers[17].addresses[2].postalCode</c>.
/// </summary>
/// <remarks>
/// A location is an immutable, value-equal path built from four segment kinds: an item index
/// (<see cref="At"/>), a keyed lookup (<see cref="Key(string)"/> and <see cref="Key(object)"/>),
/// and a property or item name (<see cref="Property"/>). <see cref="Nest"/> composes an outer
/// location with an inner one produced by a nested traversal, so each nesting level contributes
/// its own segments and the library threads the per-item context. This type is marked
/// <c>[Experimental("FS0017")]</c>: it carries no compatibility promise and may change or be
/// removed until the location-context design is promoted. See the
/// FunnySharp collections guide for the composition rules.
/// </remarks>
[Experimental("FS0017")]
public sealed class Location
{
    /// <summary>Gets the empty location that renders as an empty string.</summary>
    public static Location Root { get; } = new([]);

    private readonly Segment[] segments;
    private string? rendered;

    private Location(Segment[] segments) => this.segments = segments;

    /// <summary>
    /// Returns the location of the item at <paramref name="index"/>, rendered as <c>[index]</c>.
    /// </summary>
    /// <param name="index">The zero-based item index, which must not be negative.</param>
    /// <returns>A new location with the index segment appended.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is negative.</exception>
    public Location At(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        return new(Append(new Segment(SegmentKind.Index, null, index, null)));
    }

    /// <summary>
    /// Returns the location of the value at <paramref name="key"/>, rendered as <c>["key"]</c>
    /// with the key quoted.
    /// </summary>
    /// <param name="key">The non-null, non-empty key.</param>
    /// <returns>A new location with the key segment appended.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="key"/> is empty.</exception>
    public Location Key(string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        return new(Append(new Segment(SegmentKind.Key, null, 0, key)));
    }

    /// <summary>
    /// Returns the location of the value at <paramref name="key"/>, rendered as
    /// <c>[key]</c> using the key's <see langword="ToString"/> representation.
    /// </summary>
    /// <param name="key">The non-null key.</param>
    /// <returns>A new location with the key segment appended.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// A string key must use <see cref="Key(string)"/>, which quotes the key so that the key
    /// <c>"17"</c> stays distinguishable from the index <c>17</c>. A non-string key renders
    /// through its own string representation and is not further distinguished from an index.
    /// </remarks>
    public Location Key(object key)
    {
        ArgumentNullException.ThrowIfNull(key);

        return new(Append(new Segment(SegmentKind.Key, null, 0, key)));
    }

    /// <summary>
    /// Returns the location of the named property or item, rendered as <c>.name</c>, or as the
    /// bare <c>name</c> when it is the first segment.
    /// </summary>
    /// <param name="name">The non-null, non-empty property or item name.</param>
    /// <returns>A new location with the name segment appended.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
    public Location Property(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        return new(Append(new Segment(SegmentKind.Property, name, 0, null)));
    }

    /// <summary>
    /// Composes this location with <paramref name="inner"/>, so the combined location renders
    /// this location's path followed by the inner location's path.
    /// </summary>
    /// <param name="inner">The non-null inner location produced by a nested traversal.</param>
    /// <returns>A new location containing this location's segments followed by the inner segments.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="inner"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// Nesting is the composition rule for traversal context: an outer traversal decorates the
    /// failures of an inner traversal by nesting the inner location under the outer item's
    /// location, and each level names only its own segments.
    /// </remarks>
    public Location Nest(Location inner)
    {
        ArgumentNullException.ThrowIfNull(inner);

        if (segments.Length == 0)
        {
            return inner;
        }

        if (inner.segments.Length == 0)
        {
            return this;
        }

        var combined = new Segment[segments.Length + inner.segments.Length];
        segments.CopyTo(combined, 0);
        inner.segments.CopyTo(combined, segments.Length);

        return new(combined);
    }

    /// <summary>
    /// Renders the location path, such as <c>customers[17].addresses[2].postalCode</c>; the
    /// root location renders as an empty string.
    /// </summary>
    /// <returns>The rendered path, cached after the first call.</returns>
    public override string ToString() => rendered ??= Render();

    /// <summary>
    /// Returns a value indicating whether this location and <paramref name="other"/> contain the
    /// same segments in the same order.
    /// </summary>
    /// <param name="other">The location to compare, or <see langword="null"/>.</param>
    /// <returns>
    /// <see langword="true"/> when both locations are the same instance, or both are non-null
    /// with equal segments; otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// Segment equality distinguishes the segment kinds: <c>Location.Root.At(1)</c> does not equal
    /// <c>Location.Root.Key(1)</c>. Keys compare with their own equality semantics.
    /// </remarks>
    public bool Equals(Location? other)
    {
        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (other is null)
        {
            return false;
        }

        return segments.AsSpan().SequenceEqual(other.segments);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as Location);

    /// <inheritdoc />
    public override int GetHashCode() => ToString().GetHashCode(StringComparison.Ordinal);

    /// <summary>
    /// Returns a value indicating whether two locations contain the same segments.
    /// </summary>
    /// <param name="left">The first location, or <see langword="null"/>.</param>
    /// <param name="right">The second location, or <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when the locations are equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(Location? left, Location? right)
    {
        if (right is null)
        {
            return left is null;
        }

        return right.Equals(left);
    }

    /// <summary>
    /// Returns a value indicating whether two locations contain different segments.
    /// </summary>
    /// <param name="left">The first location, or <see langword="null"/>.</param>
    /// <param name="right">The second location, or <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when the locations are unequal; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(Location? left, Location? right) => !(left == right);

    private Segment[] Append(Segment segment)
    {
        var appended = new Segment[segments.Length + 1];
        segments.CopyTo(appended, 0);
        appended[segments.Length] = segment;

        return appended;
    }

    private string Render()
    {
        if (segments.Length == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        for (var index = 0; index < segments.Length; index++)
        {
            AppendSegment(builder, segments[index]);
        }

        return builder.ToString();
    }

    private static void AppendSegment(StringBuilder builder, Segment segment)
    {
        switch (segment.Kind)
        {
            case SegmentKind.Property:
                if (builder.Length > 0)
                {
                    builder.Append('.');
                }

                builder.Append(segment.Name);
                break;
            case SegmentKind.Index:
                builder.Append('[').Append(segment.Index).Append(']');
                break;
            case SegmentKind.Key:
                builder.Append('[');
                if (segment.Key is string text)
                {
                    builder.Append('"').Append(text).Append('"');
                }
                else
                {
                    builder.Append(segment.Key);
                }

                builder.Append(']');
                break;
        }
    }

    private enum SegmentKind : byte
    {
        Property = 0,
        Index = 1,
        Key = 2,
    }

    private readonly struct Segment : IEquatable<Segment>
    {
        public Segment(SegmentKind kind, string? name, int index, object? key)
        {
            Kind = kind;
            Name = name;
            Index = index;
            Key = key;
        }

        public SegmentKind Kind { get; }

        public string? Name { get; }

        public int Index { get; }

        public object? Key { get; }

        public bool Equals(Segment other) =>
            Kind == other.Kind
            && Index == other.Index
            && string.Equals(Name, other.Name, StringComparison.Ordinal)
            && EqualityComparer<object?>.Default.Equals(Key, other.Key);

        public override bool Equals(object? obj) => obj is Segment other && Equals(other);

        public override int GetHashCode() =>
            HashCode.Combine(Kind, Name, Index, Key);
    }
}
