namespace FunnySharp;

/// <summary>
/// A materialized keyed association that preserves every occurrence of a key under an explicit
/// equality policy.
/// </summary>
/// <typeparam name="TKey">The non-null key type.</typeparam>
/// <typeparam name="TValue">The retained value type.</typeparam>
/// <remarks>
/// <para>
/// An index is built once from a lookup sequence and is read-only afterwards. It stores one entry
/// per retained value and keeps the entries for the same key in lookup-source order, so
/// <see cref="GetMany"/> observes the original order. Construction is eager and enumerates the
/// source exactly once; keyed reads do not enumerate it again.
/// </para>
/// <para>
/// The index retains the supplied values by reference. The key selector runs once per source item
/// during construction, so a later change to a value's key is not observed by the index; a later
/// mutation of a value itself is visible through every read. The index does not clone, snapshot,
/// freeze, or roll back caller-owned values, and it adds no locks. Concurrent reads are safe only
/// when no caller mutates the retained values or their keys concurrently.
/// </para>
/// <para>
/// The equality policy is always the caller-supplied comparer or
/// <see cref="EqualityComparer{TKey}.Default"/>; it is exposed through <see cref="Comparer"/>.
/// A null key never reaches the comparer. There is deliberately no factory that accepts an
/// <see cref="IReadOnlyDictionary{TKey, TValue}"/> and inherits an equality policy the interface
/// cannot reveal.
/// </para>
/// </remarks>
public sealed class LookupIndex<TKey, TValue>
    where TKey : notnull
{
    private readonly Dictionary<TKey, GroupHead> _heads;
    private readonly List<Entry> _entries;
    private readonly IEqualityComparer<TKey> _comparer;

    internal LookupIndex(
        Dictionary<TKey, GroupHead> heads,
        List<Entry> entries,
        IEqualityComparer<TKey> comparer)
    {
        _heads = heads;
        _entries = entries;
        _comparer = comparer;
    }

    /// <summary>Gets the equality policy used for key grouping and lookup.</summary>
    public IEqualityComparer<TKey> Comparer => _comparer;

    /// <summary>Gets the number of distinct keys retained by this index.</summary>
    public int DistinctCount => _heads.Count;

    /// <summary>Gets the number of values retained by this index.</summary>
    /// <remarks>
    /// This equals the number of non-null-key lookup-source items. A value whose selected key was
    /// null is not retained; the invalid factory result reports its position instead.
    /// </remarks>
    public int EntryCount => _entries.Count;

    /// <summary>
    /// Returns every value associated with a key, in lookup-source order, or an empty list when
    /// the key is absent.
    /// </summary>
    /// <param name="key">The non-null key to read.</param>
    /// <returns>
    /// A read-only list of every retained value for <paramref name="key"/>. The empty result is
    /// shared and the non-empty result is a fresh array.
    /// </returns>
    /// <remarks>
    /// This is the explicit one-to-many read. It does not enforce uniqueness and never invokes a
    /// callback. A missing key and a present key with no values cannot be confused because an
    /// indexed group always has at least one value; a direct keyed read distinguishes absence
    /// through <see cref="GetZeroOrOne"/> or <see cref="GetExactlyOne"/>. Present null values are
    /// returned in the list unchanged.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
    public IReadOnlyList<TValue> GetMany(TKey key)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (!_heads.TryGetValue(key, out var head))
        {
            return Array.Empty<TValue>();
        }

        var values = new TValue[head.Count];
        var cursor = head.Head;
        for (var index = 0; index < values.Length; index++)
        {
            var entry = _entries[cursor];
            values[index] = entry.Value;
            cursor = entry.Next;
        }

        return values;
    }

    /// <summary>
    /// Reads a key whose association permits zero or one value.
    /// </summary>
    /// <param name="key">The non-null key to read.</param>
    /// <returns>
    /// A successful option containing the value, a successful empty option when the key is absent,
    /// or a <see cref="LookupErrorKind.DuplicateKey"/> failure when the key has multiple values.
    /// </returns>
    /// <remarks>
    /// This read enforces uniqueness for the matched key only; duplicate keys elsewhere in the
    /// index are not inspected. That is the matched-entry scope used by
    /// <see cref="LookupExtensions.JoinExactlyOneResults{TSource, TKey, TValue, TResult}"/>, not
    /// the complete-input scope of <see cref="RequireUniqueKeys"/>. The option carrier cannot
    /// represent a present null, so a present null value is rejected with
    /// <see cref="ArgumentNullException"/> exactly like the existing container bridges; use
    /// <see cref="GetMany"/> or <see cref="GetExactlyOne"/> when a null value is legitimate.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
    public Result<Option<TValue>, LookupError<TKey>> GetZeroOrOne(TKey key)
    {
        ArgumentNullException.ThrowIfNull(key);

        return GetGroupState(key, out var value, out var firstIndex, out var secondIndex) switch
        {
            LookupGroupState.Missing => Result<Option<TValue>, LookupError<TKey>>.Success(
                Option<TValue>.None),
            LookupGroupState.Single => Result<Option<TValue>, LookupError<TKey>>.Success(
                Option<TValue>.Some(value!)),
            _ => Result<Option<TValue>, LookupError<TKey>>.Failure(
                LookupError<TKey>.DuplicateKey(key, firstIndex, secondIndex)),
        };
    }

    /// <summary>
    /// Reads a key that must have exactly one value.
    /// </summary>
    /// <param name="key">The non-null key to read.</param>
    /// <returns>
    /// The single associated value, a <see cref="LookupErrorKind.MissingKey"/> failure when the key
    /// is absent, or a <see cref="LookupErrorKind.DuplicateKey"/> failure when it has multiple
    /// values.
    /// </returns>
    /// <remarks>
    /// A direct read has no probe row, so a missing failure has no
    /// <see cref="LookupError{TKey}.SourceIndex"/>; a join supplies the row position. A present
    /// null value is a successful result because <see cref="Result{TValue, TError}"/> permits a
    /// null payload. Uniqueness is checked for the matched key only. The value selector and
    /// comparer are never invoked by this read after construction.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <see langword="null"/>.</exception>
    public Result<TValue, LookupError<TKey>> GetExactlyOne(TKey key)
    {
        ArgumentNullException.ThrowIfNull(key);

        return GetGroupState(key, out var value, out var firstIndex, out var secondIndex) switch
        {
            LookupGroupState.Missing => Result<TValue, LookupError<TKey>>.Failure(
                LookupError<TKey>.MissingKey(key)),
            LookupGroupState.Single => Result<TValue, LookupError<TKey>>.Success(value),
            _ => Result<TValue, LookupError<TKey>>.Failure(
                LookupError<TKey>.DuplicateKey(key, firstIndex, secondIndex)),
        };
    }

    /// <summary>
    /// Validates at most one value per key over the complete retained index.
    /// </summary>
    /// <returns>
    /// A valid unique index when every key occurs exactly once, or every occurrence after the first
    /// for each duplicated key as ordered duplicate failures.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This is the complete-input uniqueness check. It inspects every retained group, including
    /// groups no later lookup would touch. For a key that occurs <c>n</c> times, it reports
    /// <c>n - 1</c> <see cref="LookupErrorKind.DuplicateKey"/> errors: the second through
    /// <c>n</c>th occurrences, each carrying its own position and the first occurrence's position.
    /// Failures are ordered by the repeated occurrence's lookup position.
    /// </para>
    /// <para>
    /// The returned <see cref="UniqueLookupIndex{TKey, TValue}"/> wraps this same storage and
    /// copies no values. Repeating the call over an unchanged index returns an equivalent
    /// validation; the index itself is never modified.
    /// </para>
    /// </remarks>
    public Validation<UniqueLookupIndex<TKey, TValue>, LookupError<TKey>> RequireUniqueKeys()
    {
        List<LookupError<TKey>>? errors = null;
        var unique = TryCreateUnique(ref errors);
        return unique is null
            ? Validation<UniqueLookupIndex<TKey, TValue>, LookupError<TKey>>.InvalidFromOwnedErrors(
                errors!)
            : Validation<UniqueLookupIndex<TKey, TValue>, LookupError<TKey>>.Valid(unique);
    }

    internal UniqueLookupIndex<TKey, TValue>? TryCreateUnique(
        ref List<LookupError<TKey>>? errors)
    {
        var duplicateErrors = CollectDuplicateErrors();
        if (duplicateErrors is not null)
        {
            errors ??= [];
            errors.AddRange(duplicateErrors);
        }

        if (errors is null || errors.Count == 0)
        {
            return new UniqueLookupIndex<TKey, TValue>(this);
        }

        LookupExtensions.SortByLookupPosition(errors);
        return null;
    }

    internal List<LookupError<TKey>>? CollectDuplicateErrors()
    {
        List<LookupError<TKey>>? errors = null;

        foreach (var pair in _heads)
        {
            var head = pair.Value;
            if (head.Count == 1)
            {
                continue;
            }

            errors ??= [];
            var firstIndex = _entries[head.Head].SourceIndex;
            var cursor = _entries[head.Head].Next;
            while (cursor >= 0)
            {
                var entry = _entries[cursor];
                errors.Add(LookupError<TKey>.DuplicateKey(pair.Key, firstIndex, entry.SourceIndex));
                cursor = entry.Next;
            }
        }

        return errors;
    }

    /// <summary>
    /// Builds an index in one source enumeration, appending null-key failures to
    /// <paramref name="errors"/>.
    /// </summary>
    internal static LookupIndex<TKey, TValue> Build(
        IEnumerable<TValue> source,
        Func<TValue, TKey> keySelector,
        IEqualityComparer<TKey> comparer,
        List<LookupError<TKey>> errors)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(keySelector);
        ArgumentNullException.ThrowIfNull(comparer);
        ArgumentNullException.ThrowIfNull(errors);

        var heads = new Dictionary<TKey, GroupHead>(comparer);
        var entries = new List<Entry>(SequenceExtensions.GetInitialCapacity(source));
        var sourceIndex = 0;

        foreach (var item in source)
        {
            var key = keySelector(item);
            if (key is null)
            {
                errors.Add(LookupError<TKey>.NullLookupKey(sourceIndex));
            }
            else if (heads.TryGetValue(key, out var head))
            {
                var next = entries.Count;
                entries.Add(new Entry(item, sourceIndex, -1));

                var tail = entries[head.Tail];
                tail.Next = next;
                entries[head.Tail] = tail;

                head.Tail = next;
                head.Count++;
                heads[key] = head;
            }
            else
            {
                var index = entries.Count;
                entries.Add(new Entry(item, sourceIndex, -1));
                heads.Add(key, new GroupHead { Head = index, Tail = index, Count = 1 });
            }

            sourceIndex++;
        }

        return new LookupIndex<TKey, TValue>(heads, entries, comparer);
    }

    /// <summary>
    /// Reads the single retained value for a key when the index is known to satisfy the unique-key
    /// guarantee.
    /// </summary>
    internal bool TryGetSingle(TKey key, out TValue value)
    {
        return GetGroupState(key, out value, out _, out _) switch
        {
            LookupGroupState.Single => true,
            LookupGroupState.Missing => false,
            _ => throw new InvalidOperationException(
                "The lookup index does not satisfy the unique-key guarantee."),
        };
    }

    internal LookupGroupState GetGroupState(
        TKey key,
        out TValue value,
        out int firstSourceIndex,
        out int secondSourceIndex)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (!_heads.TryGetValue(key, out var head))
        {
            value = default!;
            firstSourceIndex = -1;
            secondSourceIndex = -1;
            return LookupGroupState.Missing;
        }

        var first = _entries[head.Head];
        value = first.Value;
        firstSourceIndex = first.SourceIndex;

        if (head.Count == 1)
        {
            secondSourceIndex = -1;
            return LookupGroupState.Single;
        }

        secondSourceIndex = _entries[first.Next].SourceIndex;
        return LookupGroupState.Multiple;
    }

    internal LookupRow<TKey, TValue> ResolveRow(TKey key, int sourceIndex)
    {
        var state = GetGroupState(key, out var value, out var firstSourceIndex, out var secondSourceIndex);
        return state switch
        {
            LookupGroupState.Missing => new(
                default!,
                LookupError<TKey>.MissingKey(key, sourceIndex)),
            LookupGroupState.Single => new(value, null),
            LookupGroupState.Multiple => new(
                value,
                LookupError<TKey>.DuplicateKey(
                    key,
                    firstSourceIndex,
                    secondSourceIndex,
                    sourceIndex)),
            _ => throw new InvalidOperationException("Unknown lookup group state."),
        };
    }

    internal struct GroupHead
    {
        internal int Head;

        internal int Tail;

        internal int Count;
    }

    internal struct Entry
    {
        internal Entry(TValue value, int sourceIndex, int next)
        {
            Value = value;
            SourceIndex = sourceIndex;
            Next = next;
        }

        internal TValue Value { get; }

        internal int SourceIndex { get; }

        internal int Next { get; set; }
    }
}

internal readonly record struct LookupRow<TKey, TValue>(
    TValue Value,
    LookupError<TKey>? Error)
    where TKey : notnull;

/// <summary>Describes the number of retained values for one key.</summary>
internal enum LookupGroupState
{
    Missing = 0,
    Single = 1,
    Multiple = 2,
}
