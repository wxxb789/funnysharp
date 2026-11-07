namespace FunnySharp;

/// <summary>
/// Provides eager fail-fast folds that reduce an asynchronous sequence through a fallible
/// accumulator.
/// </summary>
/// <remarks>
/// These operations stay on BCL <see cref="IAsyncEnumerable{T}"/> and the existing
/// <see cref="Result{TValue, TError}"/> carrier. They process one item at a time, never block, and
/// do not introduce a stream hierarchy, an async wrapper, or a second failure carrier.
/// </remarks>
public static class AsyncFallibleReductionExtensions
{
    /// <summary>
    /// Asynchronously reduces an asynchronous sequence through a synchronous fallible accumulator
    /// and stops requesting source items after the first typed failure.
    /// </summary>
    /// <typeparam name="TSource">The source item type.</typeparam>
    /// <typeparam name="TAccumulate">The accumulator type.</typeparam>
    /// <typeparam name="TError">The failure value type.</typeparam>
    /// <param name="source">The asynchronous sequence to reduce.</param>
    /// <param name="seed">
    /// The initial accumulator. It is not validated and is returned unchanged, including as a null
    /// reference, when the source is empty.
    /// </param>
    /// <param name="accumulate">
    /// The synchronous function that combines the current accumulator with one source item. It
    /// returns the next accumulator or a typed failure.
    /// </param>
    /// <param name="cancellationToken">
    /// The token forwarded unchanged to the asynchronous enumerator. It is not inspected eagerly.
    /// </param>
    /// <returns>
    /// A value task that produces a successful result containing the final accumulator, or the
    /// first failed step result unchanged.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The source is enumerated exactly once in source order. Nothing is materialized, buffered,
    /// cached, or deferred beyond the returned operation; the step is invoked once per reached
    /// item. When the step returns a failure, that failed result is returned without another
    /// <c>MoveNextAsync</c> or step invocation.
    /// </para>
    /// <para>
    /// The operation begins enumerating synchronously up to the first incomplete await, like an
    /// ordinary <c>async</c> method, so source side effects can occur before the caller awaits the
    /// returned operation. Every source and step awaitable is observed once. The exact caller token
    /// is forwarded to <c>GetAsyncEnumerator</c>; this overload does not pass it to the synchronous
    /// step.
    /// </para>
    /// <para>
    /// Source cancellation or faults and step exceptions keep normal asynchronous behavior. They
    /// are never converted into a typed failure. A synchronous exception from the step is
    /// represented by the returned operation rather than thrown to the caller. An uninitialized
    /// step result faults the returned operation with <see cref="InvalidOperationException"/> when
    /// inspected. The enumerator is disposed with <c>DisposeAsync</c> after success, failure,
    /// exception, or cancellation; a disposal exception can replace a pending outcome.
    /// </para>
    /// <para>
    /// The accumulator is passed by value and is not cloned, snapshotted, isolated, or rolled back.
    /// Reference-type mutations and external work performed before a failure remain observable.
    /// Null accumulators and null errors are valid payloads.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> or <paramref name="accumulate"/> is <see langword="null"/>.
    /// </exception>
    public static ValueTask<Result<TAccumulate, TError>> AggregateResultAsync<TSource, TAccumulate, TError>(
        this IAsyncEnumerable<TSource> source,
        TAccumulate seed,
        Func<TAccumulate, TSource, Result<TAccumulate, TError>> accumulate,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(accumulate);

        return AggregateResultValueAsyncCore(
            source,
            seed,
            (accumulator, item, _) => ValueTask.FromResult(accumulate(accumulator, item)),
            cancellationToken);
    }

    /// <summary>
    /// Asynchronously reduces an asynchronous sequence through a <see cref="ValueTask"/>-based
    /// fallible accumulator and stops requesting source items after the first typed failure.
    /// </summary>
    /// <typeparam name="TSource">The source item type.</typeparam>
    /// <typeparam name="TAccumulate">The accumulator type.</typeparam>
    /// <typeparam name="TError">The failure value type.</typeparam>
    /// <param name="source">The asynchronous sequence to reduce.</param>
    /// <param name="seed">The initial accumulator, returned unchanged for an empty source.</param>
    /// <param name="accumulate">
    /// The value-task-based function that combines the current accumulator with one source item.
    /// </param>
    /// <param name="cancellationToken">
    /// The token forwarded unchanged to the asynchronous enumerator. It is not inspected eagerly.
    /// </param>
    /// <returns>
    /// A value task that produces a successful result containing the final accumulator, or the
    /// first failed step result unchanged.
    /// </returns>
    /// <remarks>
    /// Each supplied step value task is awaited exactly once. A step failure returns immediately
    /// without another source request or step invocation; the enumerator is disposed with
    /// <c>DisposeAsync</c>. Cancellation, faults, uninitialized step results, accumulator mutation,
    /// null payloads, and disposal follow the same contract as
    /// <see cref="AggregateResultAsync{TSource, TAccumulate, TError}"/>. This overload forwards the
    /// token to the enumerator but not to the step.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> or <paramref name="accumulate"/> is <see langword="null"/>.
    /// </exception>
    public static ValueTask<Result<TAccumulate, TError>> AggregateResultValueAsync<TSource, TAccumulate, TError>(
        this IAsyncEnumerable<TSource> source,
        TAccumulate seed,
        Func<TAccumulate, TSource, ValueTask<Result<TAccumulate, TError>>> accumulate,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(accumulate);

        return AggregateResultValueAsyncCore(
            source,
            seed,
            (accumulator, item, _) => accumulate(accumulator, item),
            cancellationToken);
    }

    /// <summary>
    /// Asynchronously reduces an asynchronous sequence through a cancellation-aware
    /// <see cref="ValueTask"/>-based fallible accumulator and stops requesting source items after
    /// the first typed failure.
    /// </summary>
    /// <typeparam name="TSource">The source item type.</typeparam>
    /// <typeparam name="TAccumulate">The accumulator type.</typeparam>
    /// <typeparam name="TError">The failure value type.</typeparam>
    /// <param name="source">The asynchronous sequence to reduce.</param>
    /// <param name="seed">The initial accumulator, returned unchanged for an empty source.</param>
    /// <param name="accumulate">
    /// The cancellation-aware function that combines the current accumulator with one source item.
    /// It receives the exact caller token.
    /// </param>
    /// <param name="cancellationToken">
    /// The token forwarded unchanged to the asynchronous enumerator and every reached step
    /// invocation. It is not inspected eagerly.
    /// </param>
    /// <returns>
    /// A value task that produces a successful result containing the final accumulator, or the
    /// first failed step result unchanged.
    /// </returns>
    /// <remarks>
    /// Each supplied step value task is awaited exactly once. A step failure returns immediately
    /// without another source request or step invocation; the enumerator is disposed with
    /// <c>DisposeAsync</c>. Cancellation, faults, uninitialized step results, accumulator mutation,
    /// null payloads, and disposal follow the same contract as
    /// <see cref="AggregateResultAsync{TSource, TAccumulate, TError}"/>.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> or <paramref name="accumulate"/> is <see langword="null"/>.
    /// </exception>
    public static ValueTask<Result<TAccumulate, TError>> AggregateResultValueAsync<TSource, TAccumulate, TError>(
        this IAsyncEnumerable<TSource> source,
        TAccumulate seed,
        Func<TAccumulate, TSource, CancellationToken, ValueTask<Result<TAccumulate, TError>>> accumulate,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(accumulate);

        return AggregateResultValueAsyncCore(source, seed, accumulate, cancellationToken);
    }

    private static async ValueTask<Result<TAccumulate, TError>> AggregateResultValueAsyncCore<
        TSource,
        TAccumulate,
        TError>(
        IAsyncEnumerable<TSource> source,
        TAccumulate seed,
        Func<TAccumulate, TSource, CancellationToken, ValueTask<Result<TAccumulate, TError>>> accumulate,
        CancellationToken cancellationToken)
    {
        var accumulator = seed;
        await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            var step = await accumulate(accumulator, item, cancellationToken).ConfigureAwait(false);
            if (!step.TryGetValue(out var next))
            {
                return step;
            }

            accumulator = next;
        }

        return Result<TAccumulate, TError>.Success(accumulator);
    }
}
