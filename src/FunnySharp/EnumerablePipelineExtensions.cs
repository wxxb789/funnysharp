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
}
