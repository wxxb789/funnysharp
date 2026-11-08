namespace FunnySharp;

/// <summary>
/// Identifies the parent record and nested child that produced one expanded row.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="FunnySharp.NestedDataExtensions.Expand{TParent, TChild, TResult}"/> or aligned
/// expansion passes an origin to the combine callback so a caller can retain the row's position
/// without building a path string. A structural failure carries only the record position (and both
/// counts for a mismatch), so a caller that records a rejection or needs an element position reads
/// the origin. The value is a readonly struct and passing it does not allocate.
/// </para>
/// <para>
/// Positions are zero-based input ordinals. <see cref="RecordIndex"/> identifies the record in the
/// source sequence and <see cref="ElementIndex"/> identifies the child in that record's child list.
/// They are not item identities and carry no information about a later mutation of either
/// collection.
/// </para>
/// <para>
/// This value is provenance, not an outcome carrier. Rows are produced inside the canonical
/// <see cref="Validation{TValue, TError}"/> result; this struct only describes the position of a
/// successful row.
/// </para>
/// </remarks>
public readonly struct RowOrigin : IEquatable<RowOrigin>
{
    internal RowOrigin(int recordIndex, int elementIndex)
    {
        RecordIndex = recordIndex;
        ElementIndex = elementIndex;
    }

    /// <summary>Gets the zero-based position of the parent record in the source sequence.</summary>
    public int RecordIndex { get; }

    /// <summary>Gets the zero-based position of the child in the record's child list.</summary>
    public int ElementIndex { get; }

    /// <summary>Compares two origins by record and element position.</summary>
    /// <param name="other">The other origin.</param>
    /// <returns><see langword="true"/> when both positions are equal.</returns>
    public bool Equals(RowOrigin other) =>
        RecordIndex == other.RecordIndex && ElementIndex == other.ElementIndex;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is RowOrigin other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(RecordIndex, ElementIndex);

    /// <summary>Renders the record and element positions deterministically.</summary>
    /// <returns>A compact path such as <c>record[3].element[1]</c>.</returns>
    /// <remarks>The string is created only when this member is called.</remarks>
    public override string ToString() => $"record[{RecordIndex}].element[{ElementIndex}]";
}
