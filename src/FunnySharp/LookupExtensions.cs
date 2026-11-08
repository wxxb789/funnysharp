namespace FunnySharp;

/// <summary>
/// Builds cardinality-aware keyed indexes and joins BCL sequences through them.
/// </summary>
public static class LookupExtensions
{
    /// <summary>
    /// Builds a many-occurrence index from a lookup sequence in one enumeration.
    /// </summary>
    /// <typeparam name="TKey">The non-null key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <param name="source">The lookup sequence to index.</param>
    /// <param name="keySelector">The function that selects each item's key.</param>
    /// <param name="comparer">
    /// The key equality policy, or <see langword="null"/> for
    /// <see cref="EqualityComparer{TKey}.Default"/>.
    /// </param>
    /// <returns>
    /// A valid index, or every null-key failure in lookup-source order. Duplicate keys are valid
    /// here and are retained for <see cref="LookupIndex{TKey, TValue}.GetMany"/> and the
    /// cardinality-checked reads.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The source is enumerated exactly once, eagerly. The key selector runs once per item. Values
    /// are retained by reference in lookup-source order; the index does not clone them. A null key
    /// is not passed to the comparer and is not retained; its failure carries the item's zero-based
    /// position.
    /// </para>
    /// <para>
    /// The returned <see cref="LookupIndex{TKey, TValue}.Comparer"/> is the exact policy used, so a
    /// caller can inspect it instead of assuming a dictionary's opaque policy. There is no
    /// dictionary overload because an <see cref="IReadOnlyDictionary{TKey, TValue}"/> does not
    /// reveal whether its keys use the default, ordinal-ignore-case, culture-aware, or a custom
    /// comparer. A caller starting from a dictionary passes its pairs as a sequence and states the
    /// intended comparer.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> or <paramref name="keySelector"/> is <see langword="null"/>.
    /// </exception>
    public static Validation<LookupIndex<TKey, TValue>, LookupError<TKey>> ToLookupIndex<TKey, TValue>(
        this IEnumerable<TValue> source,
        Func<TValue, TKey> keySelector,
        IEqualityComparer<TKey>? comparer = null)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(keySelector);

        var keyComparer = comparer ?? EqualityComparer<TKey>.Default;
        var errors = new List<LookupError<TKey>>();
        var index = LookupIndex<TKey, TValue>.Build(source, keySelector, keyComparer, errors);

        return errors.Count == 0
            ? Validation<LookupIndex<TKey, TValue>, LookupError<TKey>>.Valid(index)
            : Validation<LookupIndex<TKey, TValue>, LookupError<TKey>>.InvalidFromOwnedErrors(errors);
    }

    /// <summary>
    /// Builds and validates an at-most-one-value index over the complete lookup sequence in one
    /// enumeration.
    /// </summary>
    /// <typeparam name="TKey">The non-null key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <param name="source">The lookup sequence to index.</param>
    /// <param name="keySelector">The function that selects each item's key.</param>
    /// <param name="comparer">
    /// The key equality policy, or <see langword="null"/> for
    /// <see cref="EqualityComparer{TKey}.Default"/>.
    /// </param>
    /// <returns>
    /// A valid unique index, or every null-key and duplicate-key failure in lookup-source order.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The source is enumerated exactly once, eagerly; uniqueness is then judged over the complete
    /// retained input, including entries no later lookup would touch. For a key that occurs
    /// <c>n</c> times, every occurrence after the first is reported with its own position and the
    /// first occurrence's position. The errors are ordered by the repeated occurrence's lookup
    /// position.
    /// </para>
    /// <para>
    /// The returned type carries the at-most-one guarantee, so a buffered join can require exactly
    /// one source match without re-checking the lookup input. The wrapper shares the original
    /// index storage and copies no values.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> or <paramref name="keySelector"/> is <see langword="null"/>.
    /// </exception>
    public static Validation<UniqueLookupIndex<TKey, TValue>, LookupError<TKey>> ToUniqueLookupIndex<TKey, TValue>(
        this IEnumerable<TValue> source,
        Func<TValue, TKey> keySelector,
        IEqualityComparer<TKey>? comparer = null)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(keySelector);

        var keyComparer = comparer ?? EqualityComparer<TKey>.Default;
        var errorList = new List<LookupError<TKey>>();
        var index = LookupIndex<TKey, TValue>.Build(source, keySelector, keyComparer, errorList);
        List<LookupError<TKey>>? errors = errorList;
        var unique = index.TryCreateUnique(ref errors);
        return unique is null
            ? Validation<UniqueLookupIndex<TKey, TValue>, LookupError<TKey>>.InvalidFromOwnedErrors(
                errors!)
            : Validation<UniqueLookupIndex<TKey, TValue>, LookupError<TKey>>.Valid(unique);
    }

    /// <summary>
    /// Enriches every source row with exactly one lookup match, buffering the lookup index and the
    /// output list.
    /// </summary>
    /// <typeparam name="TSource">The source row type.</typeparam>
    /// <typeparam name="TLookup">The lookup value type.</typeparam>
    /// <typeparam name="TKey">The non-null key type.</typeparam>
    /// <typeparam name="TResult">The combined output type.</typeparam>
    /// <param name="source">The rows to enrich.</param>
    /// <param name="lookupSource">The complete lookup input.</param>
    /// <param name="sourceKeySelector">The function that selects each source row's key.</param>
    /// <param name="lookupKeySelector">The function that selects each lookup value's key.</param>
    /// <param name="combine">The function that combines one source row with its single match.</param>
    /// <param name="comparer">
    /// The key equality policy, or <see langword="null"/> for
    /// <see cref="EqualityComparer{TKey}.Default"/>.
    /// </param>
    /// <returns>
    /// A valid list with exactly one output for every source row, in source order, or no outputs
    /// plus every structural failure.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Type parameters are ordered for inference from the call: <typeparamref name="TSource"/> and
    /// <typeparamref name="TLookup"/> come from the two sequence arguments, <typeparamref name="TKey"/>
    /// follows the selectors, and <typeparamref name="TResult"/> follows
    /// <paramref name="combine"/>.
    /// </para>
    /// <para>
    /// The lookup source is enumerated exactly once and is validated for null and duplicate keys
    /// over its complete input before success is possible. The source is then enumerated exactly
    /// once in source order. A duplicate lookup key is therefore a whole-operation failure even
    /// when no source row uses it. The result is all-or-nothing: a failure returns no partial
    /// output.
    /// </para>
    /// <para>
    /// Failure order is deterministic: lookup-source errors first, ordered by lookup position, then
    /// source-row errors in source order. A source row whose key is null reports
    /// <see cref="LookupErrorKind.NullKey"/>; a row whose key has no match reports
    /// <see cref="LookupErrorKind.MissingKey"/>. A row matching a duplicated lookup key gets no
    /// additional output; the duplicate lookup error already identifies the key and both lookup
    /// positions.
    /// </para>
    /// <para>
    /// <paramref name="combine"/> is invoked only for a source row with exactly one match and only
    /// while no error has been recorded. Once an error is recorded, later rows are still inspected
    /// for null and missing keys, but <paramref name="combine"/> is not invoked. Invocations that
    /// already happened are not rolled back; the output list is discarded on failure. Source,
    /// selector, comparer, and combine exceptions propagate unchanged; the acquired enumerators are
    /// disposed by <c>foreach</c>.
    /// </para>
    /// <para>
    /// Duplicate lookup keys never multiply output rows. The result count on success is exactly the
    /// source count, and the order is the source order. Explicit one-to-many expansion is
    /// <see cref="LookupIndex{TKey, TValue}.GetMany"/> plus ordinary sequence code, never an
    /// inferred Cartesian product.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/>, <paramref name="lookupSource"/>,
    /// <paramref name="sourceKeySelector"/>, <paramref name="lookupKeySelector"/>, or
    /// <paramref name="combine"/> is <see langword="null"/>.
    /// </exception>
    public static Validation<IReadOnlyList<TResult>, LookupError<TKey>> JoinExactlyOne<TSource, TLookup, TKey, TResult>(
        this IEnumerable<TSource> source,
        IEnumerable<TLookup> lookupSource,
        Func<TSource, TKey> sourceKeySelector,
        Func<TLookup, TKey> lookupKeySelector,
        Func<TSource, TLookup, TResult> combine,
        IEqualityComparer<TKey>? comparer = null)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(lookupSource);
        ArgumentNullException.ThrowIfNull(sourceKeySelector);
        ArgumentNullException.ThrowIfNull(lookupKeySelector);
        ArgumentNullException.ThrowIfNull(combine);

        var keyComparer = comparer ?? EqualityComparer<TKey>.Default;
        var errorList = new List<LookupError<TKey>>();
        var lookup = LookupIndex<TKey, TLookup>.Build(
            lookupSource,
            lookupKeySelector,
            keyComparer,
            errorList);
        List<LookupError<TKey>>? errors = errorList;
        var unique = lookup.TryCreateUnique(ref errors);

        if (unique is not null)
        {
            return source.JoinExactlyOne(unique, sourceKeySelector, combine);
        }

        var indexErrors = errors!;
        var sourceErrors = new List<LookupError<TKey>>();
        var sourceIndex = 0;

        foreach (var item in source)
        {
            var key = sourceKeySelector(item);
            if (key is null)
            {
                sourceErrors.Add(LookupError<TKey>.NullSourceKey(sourceIndex));
            }
            else if (lookup.ResolveRow(key, sourceIndex).Error is
            { Kind: LookupErrorKind.MissingKey } error)
            {
                sourceErrors.Add(error);
            }

            sourceIndex++;
        }

        indexErrors.AddRange(sourceErrors);
        return Validation<IReadOnlyList<TResult>, LookupError<TKey>>.InvalidFromOwnedErrors(
            indexErrors);
    }

    /// <summary>
    /// Enriches every source row with exactly one match from a prebuilt unique index, buffering the
    /// output list.
    /// </summary>
    /// <typeparam name="TSource">The source row type.</typeparam>
    /// <typeparam name="TKey">The non-null key type.</typeparam>
    /// <typeparam name="TValue">The lookup value type.</typeparam>
    /// <typeparam name="TResult">The combined output type.</typeparam>
    /// <param name="source">The rows to enrich.</param>
    /// <param name="lookup">A unique index that already passed complete-input validation.</param>
    /// <param name="sourceKeySelector">The function that selects each source row's key.</param>
    /// <param name="combine">The function that combines one source row with its single match.</param>
    /// <returns>
    /// A valid list with exactly one output for every source row, in source order, or no outputs
    /// plus every null/missing source-row failure.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Type parameters are ordered for inference from the source and index:
    /// <typeparamref name="TSource"/> and <typeparamref name="TKey"/> come from the source and its
    /// selector, <typeparamref name="TValue"/> follows the unique-index argument, and
    /// <typeparamref name="TResult"/> follows <paramref name="combine"/>.
    /// </para>
    /// <para>
    /// The source is enumerated exactly once in source order. Duplicate lookup keys are impossible
    /// because <paramref name="lookup"/> carries the at-most-one guarantee over its complete input;
    /// this overload does not rescan the index. Null source keys and missing matches are reported
    /// as <see cref="LookupErrorKind.NullKey"/> and <see cref="LookupErrorKind.MissingKey"/> with
    /// the row position, and the result is all-or-nothing.
    /// </para>
    /// <para>
    /// <paramref name="combine"/> follows the same ledger as the raw lookup-source overload: it runs
    /// only for a row with a match and only until the first recorded error; later rows are still
    /// checked for null/missing keys. Earlier invocations are not rolled back. Exceptions propagate
    /// unchanged, and the acquired enumerator is disposed by <c>foreach</c>.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/>, <paramref name="lookup"/>,
    /// <paramref name="sourceKeySelector"/>, or <paramref name="combine"/> is
    /// <see langword="null"/>.
    /// </exception>
    public static Validation<IReadOnlyList<TResult>, LookupError<TKey>> JoinExactlyOne<TSource, TKey, TValue, TResult>(
        this IEnumerable<TSource> source,
        UniqueLookupIndex<TKey, TValue> lookup,
        Func<TSource, TKey> sourceKeySelector,
        Func<TSource, TValue, TResult> combine)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(lookup);
        ArgumentNullException.ThrowIfNull(sourceKeySelector);
        ArgumentNullException.ThrowIfNull(combine);

        var outputs = new List<TResult>(SequenceExtensions.GetInitialCapacity(source));
        var errors = new List<LookupError<TKey>>();
        var recordedError = false;
        var sourceIndex = 0;

        foreach (var item in source)
        {
            var key = sourceKeySelector(item);
            if (key is null)
            {
                errors.Add(LookupError<TKey>.NullSourceKey(sourceIndex));
                recordedError = true;
            }
            else
            {
                var row = lookup.ResolveRow(key, sourceIndex);
                if (row.Error is { } error)
                {
                    errors.Add(error);
                    recordedError = true;
                }
                else if (!recordedError)
                {
                    outputs.Add(combine(item, row.Value));
                }
            }

            sourceIndex++;
        }

        return errors.Count == 0
            ? Validation<IReadOnlyList<TResult>, LookupError<TKey>>.Valid(outputs.ToArray())
            : Validation<IReadOnlyList<TResult>, LookupError<TKey>>.InvalidFromOwnedErrors(errors);
    }

    /// <summary>
    /// Enriches each source row independently through a prebuilt many-occurrence index and yields
    /// one outcome per row.
    /// </summary>
    /// <typeparam name="TSource">The source row type.</typeparam>
    /// <typeparam name="TKey">The non-null key type.</typeparam>
    /// <typeparam name="TValue">The lookup value type.</typeparam>
    /// <typeparam name="TResult">The combined output type.</typeparam>
    /// <param name="source">The rows to enrich.</param>
    /// <param name="lookup">A prebuilt index; duplicates are checked only when matched.</param>
    /// <param name="sourceKeySelector">The function that selects each source row's key.</param>
    /// <param name="combine">The function that combines one source row with its single match.</param>
    /// <returns>
    /// A deferred sequence with exactly one outcome per source row, in source order. Each outcome is
    /// a success with the combined value, or a typed
    /// <see cref="LookupErrorKind.NullKey"/>, <see cref="LookupErrorKind.MissingKey"/>, or
    /// <see cref="LookupErrorKind.DuplicateKey"/> failure.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This is the streaming partial-output shape. It does not build or validate an index, does not
    /// buffer output, and does not enumerate the lookup source. The source is enumerated once per
    /// consumer enumeration of the returned sequence. A success can be yielded before a later
    /// failure; a failure is never thrown or silently dropped for data reasons. If the consumer
    /// stops early, later rows and their failures are not observed. The shape makes no
    /// complete-input uniqueness or all-or-nothing promise.
    /// </para>
    /// <para>
    /// Uniqueness is checked for the matched key only. A duplicated key elsewhere in
    /// <paramref name="lookup"/> does not fail these rows. A matched duplicate yields exactly one
    /// <see cref="LookupErrorKind.DuplicateKey"/> outcome carrying the source row position and both
    /// lookup positions; it never yields multiple output rows. A null source key yields
    /// <see cref="LookupErrorKind.NullKey"/> with the row position.
    /// </para>
    /// <para>
    /// <paramref name="combine"/> is invoked independently for each successful row, including rows
    /// after an earlier failure, because each row is an independent outcome. It is not invoked for
    /// a failure row. Source, selector, and combine exceptions propagate unchanged and terminate the
    /// enumeration; the acquired enumerator is disposed by <c>foreach</c>. The prebuilt index is
    /// reused without revalidation and is not enumerated again.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/>, <paramref name="lookup"/>,
    /// <paramref name="sourceKeySelector"/>, or <paramref name="combine"/> is
    /// <see langword="null"/>.
    /// </exception>
    public static IEnumerable<Result<TResult, LookupError<TKey>>> JoinExactlyOneResults<TSource, TKey, TValue, TResult>(
        this IEnumerable<TSource> source,
        LookupIndex<TKey, TValue> lookup,
        Func<TSource, TKey> sourceKeySelector,
        Func<TSource, TValue, TResult> combine)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(lookup);
        ArgumentNullException.ThrowIfNull(sourceKeySelector);
        ArgumentNullException.ThrowIfNull(combine);

        return JoinExactlyOneResultsCore(source, lookup, sourceKeySelector, combine);
    }

    private static IEnumerable<Result<TResult, LookupError<TKey>>> JoinExactlyOneResultsCore<TSource, TKey, TValue, TResult>(
        IEnumerable<TSource> source,
        LookupIndex<TKey, TValue> lookup,
        Func<TSource, TKey> sourceKeySelector,
        Func<TSource, TValue, TResult> combine)
        where TKey : notnull
    {
        var sourceIndex = 0;

        foreach (var item in source)
        {
            var key = sourceKeySelector(item);

            if (key is null)
            {
                yield return Result<TResult, LookupError<TKey>>.Failure(
                    LookupError<TKey>.NullSourceKey(sourceIndex));
            }
            else
            {
                var row = lookup.ResolveRow(key, sourceIndex);
                yield return row.Error is { } error
                    ? Result<TResult, LookupError<TKey>>.Failure(error)
                    : Result<TResult, LookupError<TKey>>.Success(combine(item, row.Value));
            }

            sourceIndex++;
        }
    }

    internal static void SortByLookupPosition<TKey>(List<LookupError<TKey>> errors)
        where TKey : notnull
    {
        errors.Sort(
            static (left, right) =>
                left.LookupSourceIndex!.Value.CompareTo(right.LookupSourceIndex!.Value));
    }
}
