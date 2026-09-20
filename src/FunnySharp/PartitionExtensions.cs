namespace FunnySharp;

/// <summary>
/// The eager partition of a sequence by a predicate, with both sides materialized in source order.
/// </summary>
/// <typeparam name="T">The source item type.</typeparam>
/// <param name="True">The items whose predicate result was <see langword="true"/>, in source order.</param>
/// <param name="False">The items whose predicate result was <see langword="false"/>, in source order.</param>
/// <remarks>
/// The partitioning operation is eager: it enumerates the source once and allocates a list for each
/// non-empty side before it returns.
/// </remarks>
public sealed record Partition<T>(IReadOnlyList<T> True, IReadOnlyList<T> False);

/// <summary>
/// The eager partition of a sequence of options into present values and absent entries.
/// </summary>
/// <typeparam name="T">The option value type.</typeparam>
/// <param name="Somes">The present values, in source order.</param>
/// <param name="Nones">The number of absent options.</param>
/// <remarks>
/// Absence has no value to list, so the absent side is the count of absent options rather than a
/// list. The present values are materialized in source order by a single enumeration.
/// </remarks>
public sealed record OptionPartition<T>(IReadOnlyList<T> Somes, int Nones);

/// <summary>
/// The eager partition of a sequence of results into successful values and failures.
/// </summary>
/// <typeparam name="TValue">The successful value type.</typeparam>
/// <typeparam name="TError">The failure value type.</typeparam>
/// <param name="Passed">The successful values, in source order.</param>
/// <param name="Failed">The errors of the failed results, in source order.</param>
/// <remarks>
/// The failed results' values are not retained: a failed result carries no value, so its error must
/// carry everything the failure needs. Both sides are materialized in source order by a single
/// enumeration.
/// </remarks>
public sealed record ResultPartition<TValue, TError>(
    IReadOnlyList<TValue> Passed,
    IReadOnlyList<TError> Failed);

/// <summary>
/// The eager partition of a sequence of unit results into successes and failures.
/// </summary>
/// <typeparam name="TError">The failure value type.</typeparam>
/// <param name="Succeeded">The number of successful unit results.</param>
/// <param name="Failed">The errors of the failed unit results, in source order.</param>
/// <remarks>
/// A unit result has no value channel, so the successful side is a count rather than a list. The
/// failure side is materialized in source order by a single enumeration.
/// </remarks>
public sealed record UnitResultPartition<TError>(int Succeeded, IReadOnlyList<TError> Failed);

/// <summary>
/// Provides eager, single-enumeration partitioning operations for sequences and functional values.
/// </summary>
public static class PartitionExtensions
{
    /// <summary>
    /// Partitions a sequence by a predicate, materializing both sides in source order.
    /// </summary>
    /// <typeparam name="T">The source item type.</typeparam>
    /// <param name="source">The sequence to partition.</param>
    /// <param name="predicate">The partitioning predicate, invoked exactly once per item.</param>
    /// <returns>
    /// A partition whose <see cref="Partition{T}.True"/> side contains the items whose predicate
    /// result was <see langword="true"/> and whose <see cref="Partition{T}.False"/> side contains
    /// the remaining items, each side in source order.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> or <paramref name="predicate"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// The source is enumerated exactly once and both sides are materialized before this operation
    /// returns, so it allocates a list for each non-empty side. A <paramref name="predicate"/>
    /// exception propagates unchanged; the sides already collected are not rolled back.
    /// </remarks>
    public static Partition<T> Partition<T>(
        this IEnumerable<T> source,
        Func<T, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(predicate);

        List<T>? trueValues = null;
        List<T>? falseValues = null;
        var initialCapacity = GetInitialCapacity(source);
        foreach (var item in source)
        {
            if (predicate(item))
            {
                (trueValues ??= new List<T>(initialCapacity)).Add(item);
            }
            else
            {
                (falseValues ??= new List<T>(initialCapacity)).Add(item);
            }
        }

        return new Partition<T>(
            SequenceExtensions.ToReadOnlyList(trueValues),
            SequenceExtensions.ToReadOnlyList(falseValues));
    }

    /// <summary>
    /// Partitions a sequence of options into the present values and a count of the absent ones.
    /// </summary>
    /// <typeparam name="T">The option value type.</typeparam>
    /// <param name="source">The sequence of options to partition.</param>
    /// <returns>
    /// A partition whose <see cref="OptionPartition{T}.Somes"/> side contains the present values
    /// in source order and whose <see cref="OptionPartition{T}.Nones"/> side counts the absent
    /// options.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// The source is enumerated exactly once and the present values are materialized before this
    /// operation returns. Absence has no value to list, so the absent side is the count of absent
    /// options rather than a list.
    /// </remarks>
    public static OptionPartition<T> Partition<T>(
        this IEnumerable<Option<T>> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        List<T>? somes = null;
        var nones = 0;
        var initialCapacity = GetInitialCapacity(source);
        foreach (var option in source)
        {
            if (option.TryGetValue(out var value))
            {
                (somes ??= new List<T>(initialCapacity)).Add(value!);
            }
            else
            {
                nones++;
            }
        }

        return new OptionPartition<T>(SequenceExtensions.ToReadOnlyList(somes), nones);
    }

    /// <summary>
    /// Partitions a sequence of results into the successful values and the failures' errors.
    /// </summary>
    /// <typeparam name="TValue">The successful value type.</typeparam>
    /// <typeparam name="TError">The failure value type.</typeparam>
    /// <param name="source">The sequence of results to partition.</param>
    /// <returns>
    /// A partition whose <see cref="ResultPartition{TValue, TError}.Passed"/> side contains the
    /// successful values in source order and whose
    /// <see cref="ResultPartition{TValue, TError}.Failed"/> side contains the failed results'
    /// errors in source order.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// The source is enumerated exactly once and both sides are materialized before this operation
    /// returns. The failed results' values are not retained: a failed result carries no value, so
    /// its error must carry everything the failure needs.
    /// </remarks>
    public static ResultPartition<TValue, TError> Partition<TValue, TError>(
        this IEnumerable<Result<TValue, TError>> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        List<TValue>? passed = null;
        List<TError>? failed = null;
        var initialCapacity = GetInitialCapacity(source);
        foreach (var result in source)
        {
            if (result.TryGetValue(out var value))
            {
                (passed ??= new List<TValue>(initialCapacity)).Add(value!);
                continue;
            }

            result.TryGetError(out var error);
            (failed ??= new List<TError>(initialCapacity)).Add(error!);
        }

        return new ResultPartition<TValue, TError>(
            SequenceExtensions.ToReadOnlyList(passed),
            SequenceExtensions.ToReadOnlyList(failed));
    }

    /// <summary>
    /// Partitions a sequence of unit results into a count of successes and the failures' errors.
    /// </summary>
    /// <typeparam name="TError">The failure value type.</typeparam>
    /// <param name="source">The sequence of unit results to partition.</param>
    /// <returns>
    /// A partition whose <see cref="UnitResultPartition{TError}.Succeeded"/> side counts the
    /// successful unit results and whose <see cref="UnitResultPartition{TError}.Failed"/> side
    /// contains the failed unit results' errors in source order.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// The source is enumerated exactly once and the failure side is materialized before this
    /// operation returns. A unit result has no value channel, so the successful side is a count
    /// rather than a list.
    /// </remarks>
    public static UnitResultPartition<TError> Partition<TError>(
        this IEnumerable<UnitResult<TError>> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        List<TError>? failed = null;
        var succeeded = 0;
        var initialCapacity = GetInitialCapacity(source);
        foreach (var result in source)
        {
            if (result.IsSuccess)
            {
                succeeded++;
                continue;
            }

            result.TryGetError(out var error);
            (failed ??= new List<TError>(initialCapacity)).Add(error!);
        }

        return new UnitResultPartition<TError>(succeeded, SequenceExtensions.ToReadOnlyList(failed));
    }

    /// <summary>
    /// Asynchronously partitions an asynchronous sequence by a predicate into materialized true
    /// and false sides.
    /// </summary>
    /// <typeparam name="T">The source item type.</typeparam>
    /// <param name="source">The asynchronous sequence to partition.</param>
    /// <param name="predicate">The partitioning predicate, invoked exactly once per item.</param>
    /// <param name="cancellationToken">The token passed to the asynchronous enumerator.</param>
    /// <returns>
    /// An asynchronous operation that produces a partition whose <see cref="Partition{T}.True"/>
    /// side contains the items whose predicate result was <see langword="true"/> and whose
    /// <see cref="Partition{T}.False"/> side contains the remaining items, each side in source
    /// order.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> or <paramref name="predicate"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// The source is enumerated exactly once and both sides are materialized before the produced
    /// operation completes. The asynchronous surface mirrors only the predicate and
    /// <see cref="Result{TValue, TError}"/> capability set (decision E88). Cancellation and
    /// exceptions flow through normal <c>await foreach</c> behavior and are not wrapped; a
    /// <paramref name="predicate"/> exception propagates unchanged, and the sides already
    /// collected are not rolled back.
    /// </remarks>
    public static ValueTask<Partition<T>> PartitionAsync<T>(
        this IAsyncEnumerable<T> source,
        Func<T, bool> predicate,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(predicate);

        return PartitionPredicateAsyncCore(source, predicate, cancellationToken);
    }

    /// <summary>
    /// Asynchronously partitions an asynchronous sequence of results into the successful values
    /// and the failures' errors.
    /// </summary>
    /// <typeparam name="TValue">The successful value type.</typeparam>
    /// <typeparam name="TError">The failure value type.</typeparam>
    /// <param name="source">The asynchronous sequence of results to partition.</param>
    /// <param name="cancellationToken">The token passed to the asynchronous enumerator.</param>
    /// <returns>
    /// An asynchronous operation that produces a partition whose
    /// <see cref="ResultPartition{TValue, TError}.Passed"/> side contains the successful values in
    /// source order and whose <see cref="ResultPartition{TValue, TError}.Failed"/> side contains
    /// the failed results' errors in source order.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// The source is enumerated exactly once and both sides are materialized before the produced
    /// operation completes. The asynchronous surface mirrors only the predicate and
    /// <see cref="Result{TValue, TError}"/> capability set (decision E88). The failed results'
    /// values are not retained: a failed result carries no value, so its error must carry
    /// everything the failure needs. Cancellation and exceptions flow through normal
    /// <c>await foreach</c> behavior and are not wrapped.
    /// </remarks>
    public static ValueTask<ResultPartition<TValue, TError>> PartitionAsync<TValue, TError>(
        this IAsyncEnumerable<Result<TValue, TError>> source,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);

        return PartitionResultAsyncCore(source, cancellationToken);
    }

    private static async ValueTask<Partition<T>> PartitionPredicateAsyncCore<T>(
        IAsyncEnumerable<T> source,
        Func<T, bool> predicate,
        CancellationToken cancellationToken)
    {
        List<T>? trueValues = null;
        List<T>? falseValues = null;
        await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            if (predicate(item))
            {
                (trueValues ??= new List<T>()).Add(item);
            }
            else
            {
                (falseValues ??= new List<T>()).Add(item);
            }
        }

        return new Partition<T>(
            SequenceExtensions.ToReadOnlyList(trueValues),
            SequenceExtensions.ToReadOnlyList(falseValues));
    }

    private static async ValueTask<ResultPartition<TValue, TError>> PartitionResultAsyncCore<TValue, TError>(
        IAsyncEnumerable<Result<TValue, TError>> source,
        CancellationToken cancellationToken)
    {
        List<TValue>? passed = null;
        List<TError>? failed = null;
        await foreach (var result in source.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            if (result.TryGetValue(out var value))
            {
                (passed ??= new List<TValue>()).Add(value!);
                continue;
            }

            result.TryGetError(out var error);
            (failed ??= new List<TError>()).Add(error!);
        }

        return new ResultPartition<TValue, TError>(
            SequenceExtensions.ToReadOnlyList(passed),
            SequenceExtensions.ToReadOnlyList(failed));
    }

    private static int GetInitialCapacity<T>(IEnumerable<T> source) =>
        Enumerable.TryGetNonEnumeratedCount(source, out var count) ? count : 0;
}
