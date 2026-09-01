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
}
