namespace FunnySharp;

/// <summary>
/// Provides deferred, BCL-based data-pipeline operations for synchronous sequences.
/// </summary>
public static class EnumerablePipelineExtensions
{
    /// <summary>
    /// Applies a chooser to each source item and yields every present result in source order.
    /// </summary>
    /// <typeparam name="TSource">The source item type.</typeparam>
    /// <typeparam name="TResult">The chosen result type.</typeparam>
    /// <param name="source">The sequence to process.</param>
    /// <param name="chooser">The function that returns a present result or absence for each item.</param>
    /// <returns>A deferred sequence containing only present chooser results.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> or <paramref name="chooser"/> is <see langword="null"/>.
    /// </exception>
    public static IEnumerable<TResult> Choose<TSource, TResult>(
        this IEnumerable<TSource> source,
        Func<TSource, Option<TResult>> chooser)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(chooser);

        return ChooseIterator(source, chooser);
    }

    /// <summary>
    /// Produces the running aggregate of a sequence, yielding the accumulator after each element.
    /// </summary>
    /// <typeparam name="TSource">The source item type.</typeparam>
    /// <typeparam name="TAccumulate">The accumulator type.</typeparam>
    /// <param name="source">The sequence to aggregate.</param>
    /// <param name="seed">The initial accumulator, which is never itself yielded.</param>
    /// <param name="accumulate">The function that combines the accumulator with each source item.</param>
    /// <returns>
    /// A deferred sequence of one accumulated value per source item, in source order. The seed is
    /// not yielded; an empty source yields no elements; for a non-empty source the last yielded
    /// value equals <c>source.Aggregate(seed, accumulate)</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> or <paramref name="accumulate"/> is <see langword="null"/>.
    /// </exception>
    public static IEnumerable<TAccumulate> Scan<TSource, TAccumulate>(
        this IEnumerable<TSource> source,
        TAccumulate seed,
        Func<TAccumulate, TSource, TAccumulate> accumulate)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(accumulate);

        return ScanIterator(source, seed, accumulate);
    }

    private static IEnumerable<TResult> ChooseIterator<TSource, TResult>(
        IEnumerable<TSource> source,
        Func<TSource, Option<TResult>> chooser)
    {
        foreach (var item in source)
        {
            if (chooser(item).TryGetValue(out var value))
            {
                yield return value!;
            }
        }
    }

    private static IEnumerable<TAccumulate> ScanIterator<TSource, TAccumulate>(
        IEnumerable<TSource> source,
        TAccumulate seed,
        Func<TAccumulate, TSource, TAccumulate> accumulate)
    {
        var accumulator = seed;
        foreach (var item in source)
        {
            accumulator = accumulate(accumulator, item);
            yield return accumulator;
        }
    }
}
