namespace FunnySharp;

/// <summary>
/// Describes one structural nested-data expansion failure without converting selector, combine,
/// enumeration, or programming exceptions into data.
/// </summary>
/// <remarks>
/// <para>
/// A failure always identifies the zero-based source record in <see cref="RecordIndex"/>.
/// <see cref="NestedRowErrorKind.NullChildSequence"/>,
/// <see cref="NestedRowErrorKind.NullFirstChildSequence"/>, and
/// <see cref="NestedRowErrorKind.NullSecondChildSequence"/> identify which child selector returned
/// a runtime null. <see cref="NestedRowErrorKind.ChildCountMismatch"/> additionally carries both
/// aligned lengths in <see cref="FirstElementCount"/> and <see cref="SecondElementCount"/>.
/// </para>
/// <para>
/// This value is a diagnostic, not an outcome carrier. Expansion returns the canonical
/// <see cref="Validation{TValue, TError}"/> with no rows on failure; this type only describes the
/// failures. A diagnostic string is created only when <see cref="ToString"/> is called.
/// </para>
/// <para>
/// The default value (<c>default(NestedRowError)</c>) reports
/// <see cref="NestedRowErrorKind.NullChildSequence"/> at record 0 because that kind is zero, but the
/// expansion operations never produce it. Every produced failure comes from one of the factories.
/// </para>
/// </remarks>
public readonly struct NestedRowError : IEquatable<NestedRowError>
{
    private NestedRowError(
        NestedRowErrorKind kind,
        int recordIndex,
        int? firstElementCount,
        int? secondElementCount)
    {
        Kind = kind;
        RecordIndex = recordIndex;
        FirstElementCount = firstElementCount;
        SecondElementCount = secondElementCount;
    }

    /// <summary>Gets the structural reason for the failure.</summary>
    public NestedRowErrorKind Kind { get; }

    /// <summary>Gets the zero-based position of the source record that failed.</summary>
    public int RecordIndex { get; }

    /// <summary>
    /// Gets the first aligned child count for a
    /// <see cref="NestedRowErrorKind.ChildCountMismatch"/>; otherwise <see langword="null"/>.
    /// </summary>
    public int? FirstElementCount { get; }

    /// <summary>
    /// Gets the second aligned child count for a
    /// <see cref="NestedRowErrorKind.ChildCountMismatch"/>; otherwise <see langword="null"/>.
    /// </summary>
    public int? SecondElementCount { get; }

    /// <summary>Creates a failure for a single child selector that returned null.</summary>
    /// <param name="recordIndex">The zero-based position of the source record.</param>
    /// <returns>A <see cref="NestedRowErrorKind.NullChildSequence"/> failure.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="recordIndex"/> is negative.</exception>
    public static NestedRowError NullChildSequence(int recordIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(recordIndex);
        return new NestedRowError(NestedRowErrorKind.NullChildSequence, recordIndex, null, null);
    }

    /// <summary>Creates a failure for a null first child sequence in a parallel expansion.</summary>
    /// <param name="recordIndex">The zero-based position of the source record.</param>
    /// <returns>A <see cref="NestedRowErrorKind.NullFirstChildSequence"/> failure.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="recordIndex"/> is negative.</exception>
    public static NestedRowError NullFirstChildSequence(int recordIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(recordIndex);
        return new NestedRowError(NestedRowErrorKind.NullFirstChildSequence, recordIndex, null, null);
    }

    /// <summary>Creates a failure for a null second child sequence in a parallel expansion.</summary>
    /// <param name="recordIndex">The zero-based position of the source record.</param>
    /// <returns>A <see cref="NestedRowErrorKind.NullSecondChildSequence"/> failure.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="recordIndex"/> is negative.</exception>
    public static NestedRowError NullSecondChildSequence(int recordIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(recordIndex);
        return new NestedRowError(NestedRowErrorKind.NullSecondChildSequence, recordIndex, null, null);
    }

    /// <summary>Creates a failure for two aligned child sequences of different lengths.</summary>
    /// <param name="recordIndex">The zero-based position of the source record.</param>
    /// <param name="firstElementCount">The first sequence's length for that record.</param>
    /// <param name="secondElementCount">The second sequence's length for that record.</param>
    /// <returns>A <see cref="NestedRowErrorKind.ChildCountMismatch"/> failure.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="recordIndex"/>, <paramref name="firstElementCount"/>, or
    /// <paramref name="secondElementCount"/> is negative.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="firstElementCount"/> equals <paramref name="secondElementCount"/>.
    /// </exception>
    public static NestedRowError ChildCountMismatch(
        int recordIndex,
        int firstElementCount,
        int secondElementCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(recordIndex);
        ArgumentOutOfRangeException.ThrowIfNegative(firstElementCount);
        ArgumentOutOfRangeException.ThrowIfNegative(secondElementCount);

        if (firstElementCount == secondElementCount)
        {
            throw new ArgumentException(
                "A child count mismatch requires different element counts.",
                nameof(secondElementCount));
        }

        return new NestedRowError(
            NestedRowErrorKind.ChildCountMismatch,
            recordIndex,
            firstElementCount,
            secondElementCount);
    }

    /// <summary>Compares two failures by kind, record position, and both counts.</summary>
    /// <param name="other">The other failure.</param>
    /// <returns><see langword="true"/> when every observed field is equal.</returns>
    public bool Equals(NestedRowError other) =>
        Kind == other.Kind
        && RecordIndex == other.RecordIndex
        && FirstElementCount == other.FirstElementCount
        && SecondElementCount == other.SecondElementCount;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is NestedRowError other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() =>
        HashCode.Combine(Kind, RecordIndex, FirstElementCount, SecondElementCount);

    /// <summary>Renders the kind, record position, and known counts deterministically.</summary>
    /// <returns>
    /// A compact diagnostic such as <c>ChildCountMismatch at record[2]: first has 1, second has 2</c>
    /// or <c>NullChildSequence at record[3]</c>.
    /// </returns>
    public override string ToString() => Kind switch
    {
        NestedRowErrorKind.NullChildSequence => $"NullChildSequence at record[{RecordIndex}]",
        NestedRowErrorKind.NullFirstChildSequence =>
            $"NullFirstChildSequence at record[{RecordIndex}]",
        NestedRowErrorKind.NullSecondChildSequence =>
            $"NullSecondChildSequence at record[{RecordIndex}]",
        _ => $"ChildCountMismatch at record[{RecordIndex}]: first has {FirstElementCount}, "
            + $"second has {SecondElementCount}",
    };
}
