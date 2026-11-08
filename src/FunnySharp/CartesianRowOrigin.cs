namespace FunnySharp;

/// <summary>
/// Identifies the parent record and both child positions that produced one Cartesian expanded row.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="FunnySharp.NestedDataExtensions.ExpandCartesian{TParent, TFirst, TSecond, TResult}"/>
/// call passes an origin to the combine callback so a caller can retain the record and both element
/// positions without building a path string. The value is a readonly struct and passing it does not
/// allocate. A structural failure carries only the record position, so a caller that records a
/// rejection or needs element positions reads the origin.
/// </para>
/// <para>
/// Positions are zero-based input ordinals. They are not item identities and carry no information
/// about a later mutation of either collection. This value is provenance, not an outcome carrier.
/// </para>
/// </remarks>
public readonly struct CartesianRowOrigin : IEquatable<CartesianRowOrigin>
{
    internal CartesianRowOrigin(int recordIndex, int firstElementIndex, int secondElementIndex)
    {
        RecordIndex = recordIndex;
        FirstElementIndex = firstElementIndex;
        SecondElementIndex = secondElementIndex;
    }

    /// <summary>Gets the zero-based position of the parent record in the source sequence.</summary>
    public int RecordIndex { get; }

    /// <summary>Gets the zero-based position in the first child sequence.</summary>
    public int FirstElementIndex { get; }

    /// <summary>Gets the zero-based position in the second child sequence.</summary>
    public int SecondElementIndex { get; }

    /// <summary>Compares two origins by record and both element positions.</summary>
    /// <param name="other">The other origin.</param>
    /// <returns><see langword="true"/> when every position is equal.</returns>
    public bool Equals(CartesianRowOrigin other) =>
        RecordIndex == other.RecordIndex
        && FirstElementIndex == other.FirstElementIndex
        && SecondElementIndex == other.SecondElementIndex;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is CartesianRowOrigin other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() =>
        HashCode.Combine(RecordIndex, FirstElementIndex, SecondElementIndex);

    /// <summary>Renders the record and both element positions deterministically.</summary>
    /// <returns>A compact path such as <c>record[3].elements[1,2]</c>.</returns>
    /// <remarks>The string is created only when this member is called.</remarks>
    public override string ToString() =>
        $"record[{RecordIndex}].elements[{FirstElementIndex},{SecondElementIndex}]";
}
