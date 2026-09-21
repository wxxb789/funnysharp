namespace FunnySharp;

/// <summary>
/// Provides eager asynchronous cardinality-sensitive operations for asynchronous sequences,
/// mirroring the synchronous <see cref="CardinalityExtensions"/> capability set.
/// </summary>
/// <remarks>
/// Every member is an awaitable operation (the <c>...Async</c> grammar): it rejects
/// <see langword="null"/> arguments eagerly before it returns the operation, enumerates the
/// source exactly once, forwards the caller's <see cref="CancellationToken"/> to the
/// asynchronous enumerator, and never converts cancellation into absence or an error. The
/// predicate overloads of the synchronous family are deliberately not mirrored, and only the
/// <c>Min</c>/<c>Max</c> comparer overloads carry over: the async carriers adopt the capability
/// set, not every overload.
/// </remarks>
public static class AsyncCardinalityExtensions
{
    /// <summary>
    /// Asynchronously returns the first item of a sequence, or <c>None</c> when it is empty.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="source">The asynchronous sequence to read.</param>
    /// <param name="cancellationToken">The token passed to the asynchronous enumerator.</param>
    /// <returns>
    /// An asynchronous operation that produces an option containing the first item, or
    /// <c>None</c> when the sequence is empty.
    /// </returns>
    /// <remarks>
    /// The source is enumerated at most once and the first item stops the enumeration, so
    /// <c>None</c> can only mean that the sequence was empty. Cancellation and exceptions flow
    /// through normal <c>await foreach</c> behavior without wrapping.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    public static ValueTask<Option<T>> FirstOrNoneAsync<T>(
        this IAsyncEnumerable<T> source,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);

        return FirstOrNoneAsyncCore(source, cancellationToken);
    }

    /// <summary>
    /// Asynchronously returns the last item of a sequence, or <c>None</c> when it is empty.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="source">The asynchronous sequence to read.</param>
    /// <param name="cancellationToken">The token passed to the asynchronous enumerator.</param>
    /// <returns>
    /// An asynchronous operation that produces an option containing the last item, or
    /// <c>None</c> when the sequence is empty.
    /// </returns>
    /// <remarks>
    /// The source is enumerated exactly once to its end because the total sequence decides the
    /// last item. Cancellation and exceptions flow through normal <c>await foreach</c> behavior
    /// without wrapping.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    public static ValueTask<Option<T>> LastOrNoneAsync<T>(
        this IAsyncEnumerable<T> source,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);

        return LastOrNoneAsyncCore(source, cancellationToken);
    }

    /// <summary>
    /// Asynchronously returns the only item of a sequence, or <c>None</c> when it is empty or has
    /// multiple items.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="source">The asynchronous sequence to read.</param>
    /// <param name="cancellationToken">The token passed to the asynchronous enumerator.</param>
    /// <returns>
    /// An asynchronous operation that produces an option containing the only item, or <c>None</c>
    /// when the sequence is empty or contains more than one item.
    /// </returns>
    /// <remarks>
    /// The empty-versus-multiple collapse matches the synchronous <c>SingleOrNone</c>: absence is
    /// the at-most-one answer, and callers that must distinguish empty from multiple read the
    /// count through <see cref="AsyncCardinalityExtensions.ToNonEmptyOrNoneAsync{T}"/>. The source
    /// is enumerated at most until the second item. Cancellation and exceptions flow through
    /// normal <c>await foreach</c> behavior without wrapping.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    public static ValueTask<Option<T>> SingleOrNoneAsync<T>(
        this IAsyncEnumerable<T> source,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);

        return SingleOrNoneAsyncCore(source, cancellationToken);
    }

    /// <summary>
    /// Asynchronously returns the item at an index, or <c>None</c> for any out-of-range index.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="source">The asynchronous sequence to read.</param>
    /// <param name="index">The zero-based item index.</param>
    /// <param name="cancellationToken">The token passed to the asynchronous enumerator.</param>
    /// <returns>
    /// An asynchronous operation that produces an option containing the item at
    /// <paramref name="index"/>, or <c>None</c> when <paramref name="index"/> is negative or
    /// beyond the end of the sequence.
    /// </returns>
    /// <remarks>
    /// Any out-of-range index, negative or beyond the end, is <c>None</c>, matching the
    /// synchronous <c>ElementAtOrNone</c>. A negative index returns without enumerating the
    /// source. Cancellation and exceptions flow through normal <c>await foreach</c> behavior
    /// without wrapping.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    public static ValueTask<Option<T>> ElementAtOrNoneAsync<T>(
        this IAsyncEnumerable<T> source,
        int index,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (index < 0)
        {
            return ValueTask.FromResult(Option<T>.None);
        }

        return ElementAtOrNoneAsyncCore(source, index, cancellationToken);
    }

    /// <summary>
    /// Asynchronously returns the smallest item, or <c>None</c> when the sequence is empty or
    /// every item is null.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="source">The asynchronous sequence to read.</param>
    /// <param name="cancellationToken">The token passed to the asynchronous enumerator.</param>
    /// <returns>
    /// An asynchronous operation that produces an option containing the smallest non-null item
    /// compared with <see cref="Comparer{T}.Default"/>, or <c>None</c> when the sequence is empty
    /// or contains only null items.
    /// </returns>
    /// <remarks>
    /// Null items are skipped exactly like the BCL <c>Min</c> semantics, so the comparer never
    /// observes a null item. The source is enumerated exactly once. Cancellation and exceptions
    /// flow through normal <c>await foreach</c> behavior without wrapping.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    public static ValueTask<Option<T>> MinOrNoneAsync<T>(
        this IAsyncEnumerable<T> source,
        CancellationToken cancellationToken = default) =>
        MinOrNoneAsync(source, Comparer<T>.Default, cancellationToken);

    /// <summary>
    /// Asynchronously returns the smallest item under a comparer, or <c>None</c> when the
    /// sequence is empty or every item is null.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="source">The asynchronous sequence to read.</param>
    /// <param name="comparer">The comparer, which never observes a null item.</param>
    /// <param name="cancellationToken">The token passed to the asynchronous enumerator.</param>
    /// <returns>
    /// An asynchronous operation that produces an option containing the smallest non-null item
    /// under <paramref name="comparer"/>, or <c>None</c> when the sequence is empty or contains
    /// only null items.
    /// </returns>
    /// <remarks>
    /// Null items are skipped exactly like the BCL <c>Min</c> semantics, so
    /// <paramref name="comparer"/> never observes a null item. The source is enumerated exactly
    /// once. Cancellation and exceptions flow through normal <c>await foreach</c> behavior
    /// without wrapping.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> or <paramref name="comparer"/> is <see langword="null"/>.
    /// </exception>
    public static ValueTask<Option<T>> MinOrNoneAsync<T>(
        this IAsyncEnumerable<T> source,
        IComparer<T> comparer,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(comparer);

        return MinOrNoneAsyncCore(source, comparer, cancellationToken);
    }

    /// <summary>
    /// Asynchronously returns the largest item, or <c>None</c> when the sequence is empty or
    /// every item is null.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="source">The asynchronous sequence to read.</param>
    /// <param name="cancellationToken">The token passed to the asynchronous enumerator.</param>
    /// <returns>
    /// An asynchronous operation that produces an option containing the largest non-null item
    /// compared with <see cref="Comparer{T}.Default"/>, or <c>None</c> when the sequence is empty
    /// or contains only null items.
    /// </returns>
    /// <remarks>
    /// Null items are skipped exactly like the BCL <c>Max</c> semantics, so the comparer never
    /// observes a null item. The source is enumerated exactly once. Cancellation and exceptions
    /// flow through normal <c>await foreach</c> behavior without wrapping.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    public static ValueTask<Option<T>> MaxOrNoneAsync<T>(
        this IAsyncEnumerable<T> source,
        CancellationToken cancellationToken = default) =>
        MaxOrNoneAsync(source, Comparer<T>.Default, cancellationToken);

    /// <summary>
    /// Asynchronously returns the largest item under a comparer, or <c>None</c> when the
    /// sequence is empty or every item is null.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="source">The asynchronous sequence to read.</param>
    /// <param name="comparer">The comparer, which never observes a null item.</param>
    /// <param name="cancellationToken">The token passed to the asynchronous enumerator.</param>
    /// <returns>
    /// An asynchronous operation that produces an option containing the largest non-null item
    /// under <paramref name="comparer"/>, or <c>None</c> when the sequence is empty or contains
    /// only null items.
    /// </returns>
    /// <remarks>
    /// Null items are skipped exactly like the BCL <c>Max</c> semantics, so
    /// <paramref name="comparer"/> never observes a null item. The source is enumerated exactly
    /// once. Cancellation and exceptions flow through normal <c>await foreach</c> behavior
    /// without wrapping.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> or <paramref name="comparer"/> is <see langword="null"/>.
    /// </exception>
    public static ValueTask<Option<T>> MaxOrNoneAsync<T>(
        this IAsyncEnumerable<T> source,
        IComparer<T> comparer,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(comparer);

        return MaxOrNoneAsyncCore(source, comparer, cancellationToken);
    }

    /// <summary>
    /// Asynchronously converts a sequence to a non-empty value, or <c>None</c> when it is empty.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="source">The asynchronous sequence to read.</param>
    /// <param name="cancellationToken">The token passed to the asynchronous enumerator.</param>
    /// <returns>
    /// An asynchronous operation that produces an option containing the non-empty value over the
    /// source items, or <c>None</c> when the sequence is empty.
    /// </returns>
    /// <remarks>
    /// The source is enumerated exactly once and the items are materialized into the non-empty
    /// value's rest list, so <c>None</c> can only mean that the sequence was empty and
    /// <c>Rest.Count == 0</c> on the produced value distinguishes a singleton. Cancellation and
    /// exceptions flow through normal <c>await foreach</c> behavior without wrapping.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    public static ValueTask<Option<NonEmpty<T>>> ToNonEmptyOrNoneAsync<T>(
        this IAsyncEnumerable<T> source,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);

        return ToNonEmptyOrNoneAsyncCore(source, cancellationToken);
    }

    private static async ValueTask<Option<T>> FirstOrNoneAsyncCore<T>(
        IAsyncEnumerable<T> source,
        CancellationToken cancellationToken)
    {
        await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            return Option<T>.Some(item!);
        }

        return Option<T>.None;
    }

    private static async ValueTask<Option<T>> LastOrNoneAsyncCore<T>(
        IAsyncEnumerable<T> source,
        CancellationToken cancellationToken)
    {
        Option<T> last = Option<T>.None;
        await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            last = Option<T>.Some(item!);
        }

        return last;
    }

    private static async ValueTask<Option<T>> SingleOrNoneAsyncCore<T>(
        IAsyncEnumerable<T> source,
        CancellationToken cancellationToken)
    {
        var enumerator = source.GetAsyncEnumerator(cancellationToken);
        try
        {
            if (!await enumerator.MoveNextAsync().ConfigureAwait(false))
            {
                return Option<T>.None;
            }

            var single = enumerator.Current;
            return await enumerator.MoveNextAsync().ConfigureAwait(false)
                ? Option<T>.None
                : Option<T>.Some(single!);
        }
        finally
        {
            await enumerator.DisposeAsync().ConfigureAwait(false);
        }
    }

    private static async ValueTask<Option<T>> ElementAtOrNoneAsyncCore<T>(
        IAsyncEnumerable<T> source,
        int index,
        CancellationToken cancellationToken)
    {
        var current = 0;
        await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            if (current == index)
            {
                return Option<T>.Some(item!);
            }

            current++;
        }

        return Option<T>.None;
    }

    private static async ValueTask<Option<T>> MinOrNoneAsyncCore<T>(
        IAsyncEnumerable<T> source,
        IComparer<T> comparer,
        CancellationToken cancellationToken)
    {
        T? minimum = default;
        var hasMinimum = false;
        await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            if (item is null)
            {
                continue;
            }

            if (!hasMinimum || comparer.Compare(item, minimum) < 0)
            {
                minimum = item;
                hasMinimum = true;
            }
        }

        return hasMinimum ? Option<T>.Some(minimum!) : Option<T>.None;
    }

    private static async ValueTask<Option<T>> MaxOrNoneAsyncCore<T>(
        IAsyncEnumerable<T> source,
        IComparer<T> comparer,
        CancellationToken cancellationToken)
    {
        T? maximum = default;
        var hasMaximum = false;
        await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            if (item is null)
            {
                continue;
            }

            if (!hasMaximum || comparer.Compare(item, maximum) > 0)
            {
                maximum = item;
                hasMaximum = true;
            }
        }

        return hasMaximum ? Option<T>.Some(maximum!) : Option<T>.None;
    }

    private static async ValueTask<Option<NonEmpty<T>>> ToNonEmptyOrNoneAsyncCore<T>(
        IAsyncEnumerable<T> source,
        CancellationToken cancellationToken)
    {
        List<T>? rest = null;
        T? first = default;
        var hasFirst = false;
        await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            if (!hasFirst)
            {
                first = item;
                hasFirst = true;
                continue;
            }

            (rest ??= new List<T>()).Add(item!);
        }

        return hasFirst ? Option<NonEmpty<T>>.Some(new NonEmpty<T>(first!, rest ?? [])) : Option<NonEmpty<T>>.None;
    }
}
