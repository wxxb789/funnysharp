namespace FunnySharp;

/// <summary>
/// Describes one structural keyed-lookup or join failure without converting source, selector,
/// comparer, or programming exceptions into data.
/// </summary>
/// <typeparam name="TKey">The non-null key type.</typeparam>
/// <remarks>
/// <para>
/// A <see cref="LookupErrorKind.MissingKey"/> error is a property of a source row and carries its
/// zero-based position in <see cref="SourceIndex"/>. A
/// <see cref="LookupErrorKind.DuplicateKey"/> error is a property of the lookup input and carries
/// the repeated occurrence in <see cref="LookupSourceIndex"/> and the first occurrence in
/// <see cref="FirstLookupSourceIndex"/>; when the duplicate was observed while probing a source
/// row, <see cref="SourceIndex"/> also identifies that row. A <see cref="LookupErrorKind.NullKey"/>
/// error identifies the offending row in exactly one of the two positions.
/// </para>
/// <para>
/// Positions are zero-based input ordinals, not item identities. A missing error created by a
/// direct keyed read has no source position because the library has no source row; the buffered
/// and streaming joins always supply the row position they are processing.
/// </para>
/// <para>
/// This value is a diagnostic, not an outcome carrier. Lookups and joins return the canonical
/// <see cref="Option{T}"/>, <see cref="Result{TValue, TError}"/>, and
/// <see cref="Validation{TValue, TError}"/> values; this type only describes their failures.
/// </para>
/// </remarks>
public readonly struct LookupError<TKey> : IEquatable<LookupError<TKey>>
    where TKey : notnull
{
    private readonly TKey? _key;

    private LookupError(
        LookupErrorKind kind,
        TKey? key,
        int? sourceIndex,
        int? lookupSourceIndex,
        int? firstLookupSourceIndex)
    {
        Kind = kind;
        _key = key;
        SourceIndex = sourceIndex;
        LookupSourceIndex = lookupSourceIndex;
        FirstLookupSourceIndex = firstLookupSourceIndex;
    }

    /// <summary>Gets the structural reason for the failure.</summary>
    public LookupErrorKind Kind { get; }

    /// <summary>
    /// Gets the key involved in the failure, or <see langword="null"/> for a null-key failure.
    /// </summary>
    /// <remarks>
    /// For a duplicate or missing key this is the key value observed at the reported position. A
    /// duplicate found while building an index reports the first stored spelling under the index's
    /// comparer. A duplicate observed while probing a source row reports that row's selected key.
    /// A direct keyed read reports the queried key.
    /// </remarks>
    public TKey? Key => _key;

    /// <summary>
    /// Gets the zero-based position in the probe/source sequence, when the failure belongs to a
    /// source row.
    /// </summary>
    /// <value>
    /// The source-row position for a missing key, a null source key, or a duplicate observed while
    /// probing; otherwise <see langword="null"/>.
    /// </value>
    public int? SourceIndex { get; }

    /// <summary>
    /// Gets the zero-based position in the lookup sequence, when the failure belongs to the lookup
    /// input.
    /// </summary>
    /// <value>
    /// The repeated occurrence for a duplicate key, or the offending row for a null lookup key;
    /// otherwise <see langword="null"/>.
    /// </value>
    public int? LookupSourceIndex { get; }

    /// <summary>
    /// Gets the zero-based position of the first occurrence of a duplicated lookup key.
    /// </summary>
    /// <value>The first position for a duplicate key; otherwise <see langword="null"/>.</value>
    public int? FirstLookupSourceIndex { get; }

    /// <summary>Creates a failure for a null key in the lookup input.</summary>
    /// <param name="lookupSourceIndex">The zero-based position of the offending lookup row.</param>
    /// <returns>A <see cref="LookupErrorKind.NullKey"/> failure with no probe position.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="lookupSourceIndex"/> is negative.</exception>
    public static LookupError<TKey> NullLookupKey(int lookupSourceIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(lookupSourceIndex);
        return new LookupError<TKey>(LookupErrorKind.NullKey, default, null, lookupSourceIndex, null);
    }

    /// <summary>Creates a failure for a null key in the probe/source input.</summary>
    /// <param name="sourceIndex">The zero-based position of the offending source row.</param>
    /// <returns>A <see cref="LookupErrorKind.NullKey"/> failure with no lookup position.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="sourceIndex"/> is negative.</exception>
    public static LookupError<TKey> NullSourceKey(int sourceIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(sourceIndex);
        return new LookupError<TKey>(LookupErrorKind.NullKey, default, sourceIndex, null, null);
    }

    /// <summary>Creates a failure for a key that occurs more than once in the lookup input.</summary>
    /// <param name="key">The duplicated key.</param>
    /// <param name="firstLookupSourceIndex">The zero-based position of the first occurrence.</param>
    /// <param name="lookupSourceIndex">The zero-based position of the repeated occurrence.</param>
    /// <param name="sourceIndex">
    /// The zero-based source-row position when the duplicate was observed while probing that row;
    /// otherwise <see langword="null"/>.
    /// </param>
    /// <returns>A <see cref="LookupErrorKind.DuplicateKey"/> failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="firstLookupSourceIndex"/>, <paramref name="lookupSourceIndex"/>, or
    /// <paramref name="sourceIndex"/> is negative.
    /// </exception>
    public static LookupError<TKey> DuplicateKey(
        TKey key,
        int firstLookupSourceIndex,
        int lookupSourceIndex,
        int? sourceIndex = null)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentOutOfRangeException.ThrowIfNegative(firstLookupSourceIndex);
        ArgumentOutOfRangeException.ThrowIfNegative(lookupSourceIndex);

        if (sourceIndex is int value)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
        }

        return new LookupError<TKey>(
            LookupErrorKind.DuplicateKey,
            key,
            sourceIndex,
            lookupSourceIndex,
            firstLookupSourceIndex);
    }

    /// <summary>Creates a failure for a key with no lookup match.</summary>
    /// <param name="key">The key that had no match.</param>
    /// <param name="sourceIndex">
    /// The zero-based position of the source row, or <see langword="null"/> for a direct keyed read
    /// that has no source row.
    /// </param>
    /// <returns>A <see cref="LookupErrorKind.MissingKey"/> failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="sourceIndex"/> is negative.</exception>
    public static LookupError<TKey> MissingKey(TKey key, int? sourceIndex = null)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (sourceIndex is int value)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
        }

        return new LookupError<TKey>(LookupErrorKind.MissingKey, key, sourceIndex, null, null);
    }

    /// <summary>Compares two lookup errors by kind, key, and all positions.</summary>
    /// <param name="other">The other error.</param>
    /// <returns><see langword="true"/> when every observed field is equal.</returns>
    public bool Equals(LookupError<TKey> other) =>
        Kind == other.Kind
        && EqualityComparer<TKey>.Default.Equals(_key!, other._key!)
        && SourceIndex == other.SourceIndex
        && LookupSourceIndex == other.LookupSourceIndex
        && FirstLookupSourceIndex == other.FirstLookupSourceIndex;

    /// <inheritdoc/>
    public override bool Equals(object? obj) =>
        obj is LookupError<TKey> other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() =>
        HashCode.Combine(Kind, _key, SourceIndex, LookupSourceIndex, FirstLookupSourceIndex);

    /// <summary>Renders the kind, key, and known positions deterministically.</summary>
    /// <returns>A compact diagnostic such as <c>DuplicateKey 'C-2' at lookup[3] (first at lookup[1])</c>.</returns>
    public override string ToString()
    {
        var key = _key is null ? "<null>" : _key.ToString() ?? string.Empty;

        if (Kind == LookupErrorKind.NullKey)
        {
            if (LookupSourceIndex is int lookupIndex)
            {
                return $"NullKey at lookup[{lookupIndex}]";
            }

            if (SourceIndex is int sourceIndex)
            {
                return $"NullKey at source[{sourceIndex}]";
            }

            return "NullKey";
        }

        if (Kind == LookupErrorKind.DuplicateKey)
        {
            var suffix = $"(first at lookup[{FirstLookupSourceIndex}])";

            if (SourceIndex is int sourceIndex && LookupSourceIndex is int lookupIndex)
            {
                return $"DuplicateKey '{key}' at source[{sourceIndex}] / lookup[{lookupIndex}] {suffix}";
            }

            if (LookupSourceIndex is int onlyLookupIndex)
            {
                return $"DuplicateKey '{key}' at lookup[{onlyLookupIndex}] {suffix}";
            }

            return $"DuplicateKey '{key}' {suffix}";
        }

        if (Kind == LookupErrorKind.MissingKey && SourceIndex is int missingIndex)
        {
            return $"MissingKey '{key}' at source[{missingIndex}]";
        }

        return $"MissingKey '{key}'";
    }
}
