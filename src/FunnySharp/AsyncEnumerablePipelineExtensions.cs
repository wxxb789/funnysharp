using System.Runtime.CompilerServices;

namespace FunnySharp;

/// <summary>
/// Provides streaming data-pipeline operations for asynchronous sequences.
/// </summary>
public static class AsyncEnumerablePipelineExtensions
{
    /// <summary>
    /// Applies a chooser to each source item and yields every present result in source order.
    /// </summary>
    /// <typeparam name="TSource">The source item type.</typeparam>
    /// <typeparam name="TResult">The chosen result type.</typeparam>
    /// <param name="source">The asynchronous sequence to process.</param>
    /// <param name="chooser">The function that returns a present result or absence for each item.</param>
    /// <returns>A deferred asynchronous sequence containing only present chooser results.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> or <paramref name="chooser"/> is <see langword="null"/>.
    /// </exception>
    public static IAsyncEnumerable<TResult> Choose<TSource, TResult>(
        this IAsyncEnumerable<TSource> source,
        Func<TSource, Option<TResult>> chooser)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(chooser);

        return ChooseValueAsyncCore(
            source,
            (item, _) => ValueTask.FromResult(chooser(item)));
    }

    /// <summary>
    /// Asynchronously applies a chooser to each source item and yields every present result in source order.
    /// </summary>
    /// <typeparam name="TSource">The source item type.</typeparam>
    /// <typeparam name="TResult">The chosen result type.</typeparam>
    /// <param name="source">The asynchronous sequence to process.</param>
    /// <param name="chooser">The ValueTask-based chooser to apply to each item.</param>
    /// <returns>A deferred asynchronous sequence containing only present chooser results.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> or <paramref name="chooser"/> is <see langword="null"/>.
    /// </exception>
    public static IAsyncEnumerable<TResult> ChooseValueAsync<TSource, TResult>(
        this IAsyncEnumerable<TSource> source,
        Func<TSource, ValueTask<Option<TResult>>> chooser)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(chooser);

        return ChooseValueAsyncCore(source, (item, _) => chooser(item));
    }

    /// <summary>
    /// Asynchronously applies a cancellation-aware chooser to each source item and yields every present result
    /// in source order.
    /// </summary>
    /// <typeparam name="TSource">The source item type.</typeparam>
    /// <typeparam name="TResult">The chosen result type.</typeparam>
    /// <param name="source">The asynchronous sequence to process.</param>
    /// <param name="chooser">The chooser that receives the enumeration cancellation token.</param>
    /// <returns>A deferred asynchronous sequence containing only present chooser results.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> or <paramref name="chooser"/> is <see langword="null"/>.
    /// </exception>
    public static IAsyncEnumerable<TResult> ChooseValueAsync<TSource, TResult>(
        this IAsyncEnumerable<TSource> source,
        Func<TSource, CancellationToken, ValueTask<Option<TResult>>> chooser)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(chooser);

        return ChooseValueAsyncCore(source, chooser);
    }

    /// <summary>
    /// Produces the running aggregate of an asynchronous sequence with a synchronous accumulator
    /// function, yielding the accumulator after each element.
    /// </summary>
    /// <typeparam name="TSource">The source item type.</typeparam>
    /// <typeparam name="TAccumulate">The accumulator type.</typeparam>
    /// <param name="source">The asynchronous sequence to aggregate.</param>
    /// <param name="seed">The initial accumulator, which is never itself yielded.</param>
    /// <param name="accumulate">The function that combines the accumulator with each source item.</param>
    /// <returns>
    /// A deferred asynchronous sequence of one accumulated value per source item, in source order.
    /// The seed is not yielded; an empty source yields no elements; for a non-empty source the
    /// last yielded value equals <c>await source.AggregateAsync(seed, accumulate)</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> or <paramref name="accumulate"/> is <see langword="null"/>.
    /// </exception>
    public static IAsyncEnumerable<TAccumulate> Scan<TSource, TAccumulate>(
        this IAsyncEnumerable<TSource> source,
        TAccumulate seed,
        Func<TAccumulate, TSource, TAccumulate> accumulate)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(accumulate);

        return ScanValueAsyncCore(
            source,
            seed,
            (accumulator, item, _) => ValueTask.FromResult(accumulate(accumulator, item)));
    }

    /// <summary>
    /// Produces the running aggregate of an asynchronous sequence with a ValueTask-based
    /// accumulator function, yielding the accumulator after each element.
    /// </summary>
    /// <typeparam name="TSource">The source item type.</typeparam>
    /// <typeparam name="TAccumulate">The accumulator type.</typeparam>
    /// <param name="source">The asynchronous sequence to aggregate.</param>
    /// <param name="seed">The initial accumulator, which is never itself yielded.</param>
    /// <param name="accumulate">The ValueTask-based function that combines the accumulator with each source item.</param>
    /// <returns>
    /// A deferred asynchronous sequence of one accumulated value per source item, in source order.
    /// The seed is not yielded; an empty source yields no elements.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> or <paramref name="accumulate"/> is <see langword="null"/>.
    /// </exception>
    public static IAsyncEnumerable<TAccumulate> ScanValueAsync<TSource, TAccumulate>(
        this IAsyncEnumerable<TSource> source,
        TAccumulate seed,
        Func<TAccumulate, TSource, ValueTask<TAccumulate>> accumulate)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(accumulate);

        return ScanValueAsyncCore(source, seed, (accumulator, item, _) => accumulate(accumulator, item));
    }

    /// <summary>
    /// Produces the running aggregate of an asynchronous sequence with a cancellation-aware
    /// ValueTask-based accumulator function, yielding the accumulator after each element.
    /// </summary>
    /// <typeparam name="TSource">The source item type.</typeparam>
    /// <typeparam name="TAccumulate">The accumulator type.</typeparam>
    /// <param name="source">The asynchronous sequence to aggregate.</param>
    /// <param name="seed">The initial accumulator, which is never itself yielded.</param>
    /// <param name="accumulate">The accumulator that receives the enumeration cancellation token.</param>
    /// <returns>
    /// A deferred asynchronous sequence of one accumulated value per source item, in source order.
    /// The seed is not yielded; an empty source yields no elements.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> or <paramref name="accumulate"/> is <see langword="null"/>.
    /// </exception>
    public static IAsyncEnumerable<TAccumulate> ScanValueAsync<TSource, TAccumulate>(
        this IAsyncEnumerable<TSource> source,
        TAccumulate seed,
        Func<TAccumulate, TSource, CancellationToken, ValueTask<TAccumulate>> accumulate)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(accumulate);

        return ScanValueAsyncCore(source, seed, accumulate);
    }

    private static async IAsyncEnumerable<TResult> ChooseValueAsyncCore<TSource, TResult>(
        IAsyncEnumerable<TSource> source,
        Func<TSource, CancellationToken, ValueTask<Option<TResult>>> chooser,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            var choice = await chooser(item, cancellationToken).ConfigureAwait(false);
            if (choice.TryGetValue(out var value))
            {
                yield return value!;
            }
        }
    }

    private static async IAsyncEnumerable<TAccumulate> ScanValueAsyncCore<TSource, TAccumulate>(
        IAsyncEnumerable<TSource> source,
        TAccumulate seed,
        Func<TAccumulate, TSource, CancellationToken, ValueTask<TAccumulate>> accumulate,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var accumulator = seed;
        await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            accumulator = await accumulate(accumulator, item, cancellationToken).ConfigureAwait(false);
            yield return accumulator;
        }
    }
}
