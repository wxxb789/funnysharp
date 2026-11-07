namespace FunnySharp;

/// <summary>
/// A keyed association that proves at most one value per key over the complete indexed input.
/// </summary>
/// <typeparam name="TKey">The non-null key type.</typeparam>
/// <typeparam name="TValue">The retained value type.</typeparam>
/// <remarks>
/// <para>
/// This type is produced by <see cref="LookupIndex{TKey, TValue}.RequireUniqueKeys"/>,
/// <see cref="LookupExtensions.ToUniqueLookupIndex{TKey, TValue}"/>, or the raw buffered
/// <c>JoinExactlyOne</c> after its complete-input validation. The construction path scans the
/// complete retained index and fails with <see cref="LookupErrorKind.DuplicateKey"/> or
/// <see cref="LookupErrorKind.NullKey"/> when the guarantee does not hold. The wrapper shares the
/// original index storage; it does not copy or clone values.
/// </para>
/// <para>
/// The index retains caller values by reference. Keys were read once during construction, so later
/// value mutation is visible through <see cref="GetOption"/> while a later key change is not
/// observed. The wrapper adds no locks, snapshotting, or rollback and is safe for concurrent reads
/// only when no caller mutates the retained values concurrently.
/// </para>
/// </remarks>
public sealed class UniqueLookupIndex<TKey, TValue>
    where TKey : notnull
{
    private readonly LookupIndex<TKey, TValue> _index;

    internal UniqueLookupIndex(LookupIndex<TKey, TValue> index)
    {
        _index = index;
    }

    /// <summary>Gets the equality policy used for key grouping and lookup.</summary>
    public IEqualityComparer<TKey> Comparer => _index.Comparer;

    /// <summary>Gets the number of distinct keys in the unique index.</summary>
    /// <remarks>Every group has exactly one value, so this is also the number of retained values.</remarks>
    public int Count => _index.DistinctCount;

    /// <summary>
    /// Reads the value associated with a key, or absence when no key matches.
    /// </summary>
    /// <param name="key">The non-null key to read.</param>
    /// <returns>
    /// An option containing the single associated value, or <c>None</c> when the key is absent.
    /// </returns>
    /// <remarks>
    /// This is the zero-or-one keyed read on a proven unique index; multiple matches are impossible
    /// by construction. The option carrier cannot represent a present null, so a present null value
    /// is rejected with <see cref="ArgumentNullException"/> exactly like the existing container
    /// bridges. The index never enumerates the original source again.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
    public Option<TValue> GetOption(TKey key)
    {
        ArgumentNullException.ThrowIfNull(key);

        return _index.TryGetSingle(key, out var value)
            ? Option<TValue>.Some(value!)
            : Option<TValue>.None;
    }

    internal LookupRow<TKey, TValue> ResolveRow(TKey key, int sourceIndex) =>
        _index.ResolveRow(key, sourceIndex);
}
