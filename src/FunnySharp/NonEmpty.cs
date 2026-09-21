namespace FunnySharp;

/// <summary>
/// Represents the guarantee that a sequence contains at least one item.
/// </summary>
/// <typeparam name="T">The item type.</typeparam>
/// <remarks>
/// A non-empty value carries <see cref="First"/> (present by construction, but its value can be
/// <c>default(T)</c>, including <see langword="null"/> for nullable item types) and
/// <see cref="Rest"/> (possibly empty, in source order), so <see cref="Count"/> is always at
/// least one. It is not a collection type: it implements no <see cref="IEnumerable{T}"/>,
/// exposes no indexer, and never enumerates. Materialize with <see cref="ToReadOnlyList"/> or
/// use LINQ over <see cref="Rest"/>. The only public constructor path is
/// <see cref="CardinalityExtensions.ToNonEmptyOrNone{T}(IEnumerable{T})"/>, which returns
/// <c>None</c> for an empty source instead of a default value. The default value of
/// <see cref="NonEmpty{T}"/> is uninitialized: every member that reads it throws
/// <see cref="InvalidOperationException"/> like the other FunnySharp carriers; only
/// <see cref="ToString"/> returns diagnostic text for it.
/// </remarks>
public readonly struct NonEmpty<T>
{
    private const byte UninitializedState = 0;
    private const byte InitializedState = 1;

    private readonly T? first;
    private readonly List<T>? rest;
    private readonly byte state;

    internal NonEmpty(T first, List<T> rest)
    {
        ArgumentNullException.ThrowIfNull(rest);

        this.first = first;
        this.rest = rest;
        state = InitializedState;
    }

    /// <summary>
    /// Gets the first item, which is present by construction: a first item always exists, but its
    /// value can be <c>default(T)</c>, including <see langword="null"/> for nullable item types.
    /// </summary>
    /// <exception cref="InvalidOperationException">This non-empty sequence is uninitialized.</exception>
    public T First
    {
        get
        {
            EnsureInitialized();
            return first!;
        }
    }

    /// <summary>
    /// Gets the remaining items in source order; empty for a singleton.
    /// </summary>
    /// <exception cref="InvalidOperationException">This non-empty sequence is uninitialized.</exception>
    public IReadOnlyList<T> Rest
    {
        get
        {
            EnsureInitialized();
            return rest!;
        }
    }

    /// <summary>
    /// Gets the item count: <see cref="First"/> plus <see cref="Rest"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">This non-empty sequence is uninitialized.</exception>
    public int Count
    {
        get
        {
            EnsureInitialized();
            return 1 + rest!.Count;
        }
    }

    /// <summary>
    /// Folds the items with an accumulator function, starting from <see cref="First"/>.
    /// </summary>
    /// <param name="func">The accumulator applied to the running value and each <see cref="Rest"/> item in order.</param>
    /// <returns>The folded value.</returns>
    /// <remarks>
    /// This is the seedless left fold: it starts from <see cref="First"/> and folds every
    /// <see cref="Rest"/> item in order. Unlike BCL <c>Enumerable.Aggregate</c>, which throws
    /// <see cref="InvalidOperationException"/> on an empty sequence, this fold cannot encounter
    /// emptiness because the type carries the guarantee. Exceptions thrown by
    /// <paramref name="func"/> propagate unchanged.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="func"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">This non-empty sequence is uninitialized.</exception>
    public T Aggregate(Func<T, T, T> func)
    {
        ArgumentNullException.ThrowIfNull(func);
        EnsureInitialized();

        var accumulator = first!;
        foreach (var item in rest!)
        {
            accumulator = func(accumulator, item);
        }

        return accumulator;
    }

    /// <summary>
    /// Materializes the items into a fresh, ordered list.
    /// </summary>
    /// <returns>
    /// A read-only list containing <see cref="First"/> followed by every <see cref="Rest"/>
    /// item in order.
    /// </returns>
    /// <remarks>
    /// The result is a fresh allocation (a list sized to <see cref="Count"/> plus its read-only
    /// wrapper) and never aliases the state of this value.
    /// </remarks>
    /// <exception cref="InvalidOperationException">This non-empty sequence is uninitialized.</exception>
    public IReadOnlyList<T> ToReadOnlyList()
    {
        EnsureInitialized();

        var items = new List<T>(Count);
        items.Add(first!);
        items.AddRange(rest!);
        return items.AsReadOnly();
    }

    /// <summary>
    /// Returns diagnostic text for this non-empty sequence.
    /// </summary>
    /// <returns><c>NonEmpty[3]</c> with <see cref="Count"/>, or <c>Uninitialized</c> for the default value.</returns>
    public override string ToString() =>
        state == UninitializedState ? "Uninitialized" : $"NonEmpty[{Count}]";

    private void EnsureInitialized()
    {
        if (state == UninitializedState)
        {
            throw new InvalidOperationException("The non-empty sequence is uninitialized.");
        }
    }
}
