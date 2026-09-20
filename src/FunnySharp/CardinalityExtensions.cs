namespace FunnySharp;

/// <summary>
/// Provides eager cardinality-sensitive operations for sequences.
/// </summary>
/// <remarks>
/// Every member validates its arguments eagerly, enumerates the source at most once, preserves
/// source order, never invokes a delegate again after short-circuiting, and runs iteratively, so
/// inputs of any size are safe (no recursion). The option carrier cannot hold
/// <see langword="null"/>: a selected null item, which is possible only for a nullable reference
/// item type, is rejected by the option's non-null guarantee with
/// <see cref="ArgumentNullException"/> instead of being folded into <c>None</c>. The
/// null-skipping <c>MinOrNone</c> and <c>MaxOrNone</c> members are the exception. Each member
/// documents the single meaning of its <c>None</c>.
/// </remarks>
public static class CardinalityExtensions
{
    /// <summary>
    /// Returns the first item of a sequence, or <c>None</c> when the sequence is empty.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="source">The sequence to read.</param>
    /// <returns>
    /// An option containing the first item, or <c>None</c> when <paramref name="source"/> is
    /// empty.
    /// </returns>
    /// <remarks>
    /// This member is eager, enumerates <paramref name="source"/> at most once, stops at the
    /// first item, and preserves source order. <c>None</c> can only mean that the source is
    /// empty. The loop is iterative, so inputs of any size are safe (no recursion).
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    public static Option<T> FirstOrNone<T>(this IEnumerable<T> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        foreach (var item in source)
        {
            return Option<T>.Some(item!);
        }

        return Option<T>.None;
    }

    /// <summary>
    /// Returns the first item that matches a predicate, or <c>None</c> when no item matches.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="source">The sequence to search.</param>
    /// <param name="predicate">The predicate to evaluate for each item in source order.</param>
    /// <returns>
    /// An option containing the first matching item, or <c>None</c> when no item matches.
    /// </returns>
    /// <remarks>
    /// This member is eager, enumerates <paramref name="source"/> at most once, and preserves
    /// source order. <paramref name="predicate"/> is invoked at most once per item in source
    /// order and never again after the first match, so <c>None</c> can only mean that no item
    /// matches. The loop is iterative, so inputs of any size are safe (no recursion).
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> or <paramref name="predicate"/> is <see langword="null"/>.
    /// </exception>
    public static Option<T> FirstOrNone<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(predicate);

        foreach (var item in source)
        {
            if (predicate(item))
            {
                return Option<T>.Some(item!);
            }
        }

        return Option<T>.None;
    }

    /// <summary>
    /// Returns the last item of a sequence, or <c>None</c> when the sequence is empty.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="source">The sequence to read.</param>
    /// <returns>
    /// An option containing the last item, or <c>None</c> when <paramref name="source"/> is
    /// empty.
    /// </returns>
    /// <remarks>
    /// This member is eager, enumerates <paramref name="source"/> at most once (the whole
    /// sequence, because only the last item decides the answer), and preserves source order.
    /// <c>None</c> can only mean that the source is empty. The loop is iterative, so inputs of
    /// any size are safe (no recursion).
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    public static Option<T> LastOrNone<T>(this IEnumerable<T> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        T? last = default;
        var found = false;
        foreach (var item in source)
        {
            last = item;
            found = true;
        }

        return found ? Option<T>.Some(last!) : Option<T>.None;
    }

    /// <summary>
    /// Returns the last item that matches a predicate, or <c>None</c> when no item matches.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="source">The sequence to search.</param>
    /// <param name="predicate">The predicate to evaluate for each item in source order.</param>
    /// <returns>
    /// An option containing the last matching item, or <c>None</c> when no item matches.
    /// </returns>
    /// <remarks>
    /// This member is eager, enumerates <paramref name="source"/> at most once (the whole
    /// sequence, because only the last match decides the answer), and preserves source order.
    /// <paramref name="predicate"/> is invoked at most once per item in source order, so
    /// <c>None</c> can only mean that no item matches. The loop is iterative, so inputs of any
    /// size are safe (no recursion).
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> or <paramref name="predicate"/> is <see langword="null"/>.
    /// </exception>
    public static Option<T> LastOrNone<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(predicate);

        T? last = default;
        var found = false;
        foreach (var item in source)
        {
            if (predicate(item))
            {
                last = item;
                found = true;
            }
        }

        return found ? Option<T>.Some(last!) : Option<T>.None;
    }

    /// <summary>
    /// Returns the only item of a sequence, or <c>None</c> when it is empty or has multiple
    /// items.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="source">The sequence to read.</param>
    /// <returns>
    /// An option containing the single item, or <c>None</c> when <paramref name="source"/> is
    /// empty or contains multiple items.
    /// </returns>
    /// <remarks>
    /// This member is eager, enumerates <paramref name="source"/> at most once, and preserves
    /// source order. It collapses the empty and multiple-item cases into <c>None</c> (the
    /// at-most-one collapse): <c>None</c> means only that the source does not contain exactly
    /// one item, never which of the two cases occurred. A second element is read to detect
    /// multiple items, and enumeration stops there. Callers that must distinguish the cases use
    /// <see cref="ToNonEmptyOrNone{T}(IEnumerable{T})"/> instead: <c>None</c> there can only
    /// mean an empty source, and <see cref="NonEmpty{T}.Rest"/>.Count == 0 on the non-empty
    /// value distinguishes a singleton. The loop is iterative, so inputs of any size are safe
    /// (no recursion).
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    public static Option<T> SingleOrNone<T>(this IEnumerable<T> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        T? single = default;
        var count = 0;
        foreach (var item in source)
        {
            count++;
            if (count > 1)
            {
                return Option<T>.None;
            }

            single = item;
        }

        return count == 1 ? Option<T>.Some(single!) : Option<T>.None;
    }

    /// <summary>
    /// Returns the only item that matches a predicate, or <c>None</c> when no item or multiple
    /// items match.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="source">The sequence to search.</param>
    /// <param name="predicate">The predicate to evaluate for each item in source order.</param>
    /// <returns>
    /// An option containing the single matching item, or <c>None</c> when no item or multiple
    /// items match.
    /// </returns>
    /// <remarks>
    /// This member is eager, enumerates <paramref name="source"/> at most once, and preserves
    /// source order. It collapses the no-match and multiple-match cases into <c>None</c> (the
    /// at-most-one collapse): <c>None</c> means only that the source does not contain exactly
    /// one matching item, never which of the two cases occurred. <paramref name="predicate"/> is
    /// invoked at most once per item in source order, and enumeration stops when a second match
    /// is found. Callers that must distinguish the cases use
    /// <see cref="ToNonEmptyOrNone{T}(IEnumerable{T})"/> instead: <c>None</c> there can only
    /// mean an empty source, and <see cref="NonEmpty{T}.Rest"/>.Count == 0 on the non-empty
    /// value distinguishes a singleton. The loop is iterative, so inputs of any size are safe
    /// (no recursion).
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> or <paramref name="predicate"/> is <see langword="null"/>.
    /// </exception>
    public static Option<T> SingleOrNone<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(predicate);

        T? single = default;
        var count = 0;
        foreach (var item in source)
        {
            if (!predicate(item))
            {
                continue;
            }

            count++;
            if (count > 1)
            {
                return Option<T>.None;
            }

            single = item;
        }

        return count == 1 ? Option<T>.Some(single!) : Option<T>.None;
    }

    /// <summary>
    /// Returns the item at a zero-based index, or <c>None</c> when the index is out of range.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="source">The sequence to read.</param>
    /// <param name="index">The zero-based index of the item to return.</param>
    /// <returns>
    /// An option containing the item at <paramref name="index"/>, or <c>None</c> when
    /// <paramref name="index"/> is negative or beyond the end of <paramref name="source"/>.
    /// </returns>
    /// <remarks>
    /// This member is eager, enumerates <paramref name="source"/> at most once, and preserves
    /// source order. Unlike BCL <c>Enumerable.ElementAt</c>, which throws for a negative index,
    /// every out-of-range index becomes <c>None</c>: this matches the absence behavior of
    /// <c>Enumerable.ElementAtOrDefault</c> without its default-value ambiguity, so <c>None</c>
    /// can only mean that the index is out of range. The loop is iterative, so inputs of any
    /// size are safe (no recursion).
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    public static Option<T> ElementAtOrNone<T>(this IEnumerable<T> source, int index)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (index < 0)
        {
            return Option<T>.None;
        }

        var current = 0;
        foreach (var item in source)
        {
            if (current == index)
            {
                return Option<T>.Some(item!);
            }

            current++;
        }

        return Option<T>.None;
    }

    /// <summary>
    /// Returns the smallest item, or <c>None</c> when the source is empty or every item is null.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="source">The sequence to read.</param>
    /// <returns>
    /// An option containing the smallest item compared with <see cref="Comparer{T}.Default"/>,
    /// or <c>None</c> when <paramref name="source"/> is empty or contains only null items.
    /// </returns>
    /// <remarks>
    /// This member is eager, enumerates <paramref name="source"/> at most once, and preserves
    /// source order. It follows the BCL null semantics for a nullable reference item type: null
    /// items are skipped, the comparison never sees a null item, and a source that is empty or
    /// all null becomes <c>None</c>. The loop is iterative, so inputs of any size are safe
    /// (no recursion).
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    public static Option<T> MinOrNone<T>(this IEnumerable<T> source) =>
        source.MinOrNone(Comparer<T>.Default);

    /// <summary>
    /// Returns the smallest item under a caller-supplied comparer, or <c>None</c> when the
    /// source is empty or every item is null.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="source">The sequence to read.</param>
    /// <param name="comparer">The comparer that orders the items.</param>
    /// <returns>
    /// An option containing the smallest item under <paramref name="comparer"/>, or <c>None</c>
    /// when <paramref name="source"/> is empty or contains only null items.
    /// </returns>
    /// <remarks>
    /// This member is eager, enumerates <paramref name="source"/> at most once, and preserves
    /// source order. It follows the BCL null semantics for a nullable reference item type: null
    /// items are skipped, so <paramref name="comparer"/> never sees a null item, and a source
    /// that is empty or all null becomes <c>None</c>. The loop is iterative, so inputs of any
    /// size are safe (no recursion).
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> or <paramref name="comparer"/> is <see langword="null"/>.
    /// </exception>
    public static Option<T> MinOrNone<T>(this IEnumerable<T> source, IComparer<T> comparer)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(comparer);

        T? minimum = default;
        var found = false;
        foreach (var item in source)
        {
            if (item is null)
            {
                continue;
            }

            if (!found || comparer.Compare(item, minimum!) < 0)
            {
                minimum = item;
                found = true;
            }
        }

        return found ? Option<T>.Some(minimum!) : Option<T>.None;
    }

    /// <summary>
    /// Returns the largest item, or <c>None</c> when the source is empty or every item is null.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="source">The sequence to read.</param>
    /// <returns>
    /// An option containing the largest item compared with <see cref="Comparer{T}.Default"/>,
    /// or <c>None</c> when <paramref name="source"/> is empty or contains only null items.
    /// </returns>
    /// <remarks>
    /// This member is eager, enumerates <paramref name="source"/> at most once, and preserves
    /// source order. It follows the BCL null semantics for a nullable reference item type: null
    /// items are skipped, the comparison never sees a null item, and a source that is empty or
    /// all null becomes <c>None</c>. The loop is iterative, so inputs of any size are safe
    /// (no recursion).
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    public static Option<T> MaxOrNone<T>(this IEnumerable<T> source) =>
        source.MaxOrNone(Comparer<T>.Default);

    /// <summary>
    /// Returns the largest item under a caller-supplied comparer, or <c>None</c> when the
    /// source is empty or every item is null.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="source">The sequence to read.</param>
    /// <param name="comparer">The comparer that orders the items.</param>
    /// <returns>
    /// An option containing the largest item under <paramref name="comparer"/>, or <c>None</c>
    /// when <paramref name="source"/> is empty or contains only null items.
    /// </returns>
    /// <remarks>
    /// This member is eager, enumerates <paramref name="source"/> at most once, and preserves
    /// source order. It follows the BCL null semantics for a nullable reference item type: null
    /// items are skipped, so <paramref name="comparer"/> never sees a null item, and a source
    /// that is empty or all null becomes <c>None</c>. The loop is iterative, so inputs of any
    /// size are safe (no recursion).
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> or <paramref name="comparer"/> is <see langword="null"/>.
    /// </exception>
    public static Option<T> MaxOrNone<T>(this IEnumerable<T> source, IComparer<T> comparer)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(comparer);

        T? maximum = default;
        var found = false;
        foreach (var item in source)
        {
            if (item is null)
            {
                continue;
            }

            if (!found || comparer.Compare(item, maximum!) > 0)
            {
                maximum = item;
                found = true;
            }
        }

        return found ? Option<T>.Some(maximum!) : Option<T>.None;
    }

    /// <summary>
    /// Converts a sequence into a non-empty value that carries the guarantee of at least one
    /// item.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="source">The sequence to materialize.</param>
    /// <returns>
    /// An option containing a <see cref="NonEmpty{T}"/> over the source items, or <c>None</c>
    /// when <paramref name="source"/> is empty.
    /// </returns>
    /// <remarks>
    /// This member is eager, enumerates <paramref name="source"/> at most once, and preserves
    /// source order. <c>None</c> can only mean that the source is empty; every non-empty source
    /// produces <c>Some</c>, so singleton and multiple-item sources stay distinguishable
    /// through <see cref="NonEmpty{T}.Rest"/>. The remaining items are materialized once into a
    /// list with the source's count as the capacity hint; the loop is iterative, so inputs of
    /// any size are safe (no recursion).
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    public static Option<NonEmpty<T>> ToNonEmptyOrNone<T>(this IEnumerable<T> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        using var enumerator = source.GetEnumerator();
        if (!enumerator.MoveNext())
        {
            return Option<NonEmpty<T>>.None;
        }

        var first = enumerator.Current;
        var rest = new List<T>(GetInitialCapacity(source));
        while (enumerator.MoveNext())
        {
            rest.Add(enumerator.Current);
        }

        return Option<NonEmpty<T>>.Some(new NonEmpty<T>(first, rest));
    }

    private static int GetInitialCapacity<T>(IEnumerable<T> source) =>
        Enumerable.TryGetNonEnumeratedCount(source, out var count) ? count : 0;
}
