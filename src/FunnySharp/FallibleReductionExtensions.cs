namespace FunnySharp;

/// <summary>
/// Provides eager fail-fast folds that reduce a synchronous sequence through a fallible
/// accumulator.
/// </summary>
/// <remarks>
/// These operations stay on BCL <see cref="IEnumerable{T}"/> and the existing
/// <see cref="Result{TValue, TError}"/> carrier. They do not introduce a stream or sequence
/// hierarchy, an async wrapper, or a second failure carrier.
/// </remarks>
public static class FallibleReductionExtensions
{
    /// <summary>
    /// Reduces a sequence through a fallible accumulator and stops requesting source items after
    /// the first typed failure.
    /// </summary>
    /// <typeparam name="TSource">The source item type.</typeparam>
    /// <typeparam name="TAccumulate">The accumulator type.</typeparam>
    /// <typeparam name="TError">The failure value type.</typeparam>
    /// <param name="source">The sequence to reduce.</param>
    /// <param name="seed">
    /// The initial accumulator. It is not validated and is returned unchanged, including as a null
    /// reference, when the source is empty.
    /// </param>
    /// <param name="accumulate">
    /// The function that combines the current accumulator with one source item. It returns the next
    /// accumulator or a typed failure.
    /// </param>
    /// <returns>
    /// A successful result containing the final accumulator, or the first failed step result
    /// unchanged.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The operation is eager and enumerates <paramref name="source"/> exactly once. It does not
    /// materialize, buffer, cache, or defer an intermediate sequence; the accumulator is a local
    /// value and the step is invoked once per reached item in source order.
    /// </para>
    /// <para>
    /// When the step returns a failure, that failed result is returned immediately. The enumerator
    /// is disposed, and the operation does not call <c>MoveNext</c> or the step again. A failure
    /// therefore leaves the source unconsumed after the reached item; no rollback of earlier
    /// accumulator changes or external work is performed.
    /// </para>
    /// <para>
    /// The accumulator is passed by value. Reference-type and mutable accumulators are not cloned,
    /// snapshotted, isolated, or rolled back, so mutations performed by a step remain observable to
    /// later steps and to a caller that still holds the seed. A failed step's own mutations also
    /// remain. Null accumulators and null errors are valid payloads, matching
    /// <see cref="Result{TValue, TError}.Success"/> and
    /// <see cref="Result{TValue, TError}.Failure"/>.
    /// </para>
    /// <para>
    /// Source exceptions, step exceptions, and an uninitialized step result are not converted to
    /// typed failures. An exception propagates unchanged; an uninitialized step result is a
    /// programming error that throws <see cref="InvalidOperationException"/> when the result is
    /// inspected. An acquired enumerator is still disposed. Normal <c>foreach</c> precedence
    /// applies: an exception from <see cref="IDisposable.Dispose"/> can replace a pending source or
    /// step exception.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> or <paramref name="accumulate"/> is <see langword="null"/>.
    /// </exception>
    public static Result<TAccumulate, TError> AggregateResult<TSource, TAccumulate, TError>(
        this IEnumerable<TSource> source,
        TAccumulate seed,
        Func<TAccumulate, TSource, Result<TAccumulate, TError>> accumulate)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(accumulate);

        var accumulator = seed;
        foreach (var item in source)
        {
            var step = accumulate(accumulator, item);
            if (!step.TryGetValue(out var next))
            {
                return step;
            }

            accumulator = next;
        }

        return Result<TAccumulate, TError>.Success(accumulator);
    }
}
