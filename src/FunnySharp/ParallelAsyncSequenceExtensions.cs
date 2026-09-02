using System.Runtime.ExceptionServices;

namespace FunnySharp;

/// <summary>
/// Provides bounded-parallel traversal operations for asynchronous sequences.
/// </summary>
public static class ParallelAsyncSequenceExtensions
{
    /// <summary>
    /// Applies an option-producing selector to source items with bounded parallelism.
    /// </summary>
    /// <typeparam name="TSource">The source item type.</typeparam>
    /// <typeparam name="TResult">The selected option value type.</typeparam>
    /// <param name="source">The asynchronous sequence to traverse.</param>
    /// <param name="maxConcurrency">The maximum number of started selector operations.</param>
    /// <param name="selector">The ValueTask-based option-producing selector.</param>
    /// <param name="cancellationToken">The caller cancellation token.</param>
    /// <returns>
    /// An asynchronous operation that produces values in source order, or <c>None</c> when a started selector
    /// returns <c>None</c>. Remaining started work is canceled and drained before that result is returned.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> or <paramref name="selector"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxConcurrency"/> is less than one.</exception>
    public static ValueTask<Option<IReadOnlyList<TResult>>> TraverseParallelValueAsync<TSource, TResult>(
        this IAsyncEnumerable<TSource> source,
        int maxConcurrency,
        Func<TSource, ValueTask<Option<TResult>>> selector,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxConcurrency, 1);

        return TraverseOptionValueAsyncCore(
            source,
            maxConcurrency,
            (value, _) => selector(value),
            cancellationToken);
    }

    /// <summary>
    /// Applies a cancellation-aware option-producing selector to source items with bounded parallelism.
    /// </summary>
    /// <typeparam name="TSource">The source item type.</typeparam>
    /// <typeparam name="TResult">The selected option value type.</typeparam>
    /// <param name="source">The asynchronous sequence to traverse.</param>
    /// <param name="maxConcurrency">The maximum number of started selector operations.</param>
    /// <param name="selector">The cancellation-aware ValueTask-based option-producing selector.</param>
    /// <param name="cancellationToken">The caller cancellation token.</param>
    /// <returns>
    /// An asynchronous operation that produces values in source order, or <c>None</c> when a started selector
    /// returns <c>None</c>. The selector and enumerator receive a linked operation token; cleanup completes first.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> or <paramref name="selector"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxConcurrency"/> is less than one.</exception>
    public static ValueTask<Option<IReadOnlyList<TResult>>> TraverseParallelValueAsync<TSource, TResult>(
        this IAsyncEnumerable<TSource> source,
        int maxConcurrency,
        Func<TSource, CancellationToken, ValueTask<Option<TResult>>> selector,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxConcurrency, 1);

        return TraverseOptionValueAsyncCore(source, maxConcurrency, selector, cancellationToken);
    }

    /// <summary>
    /// Applies a result-producing selector to source items with bounded parallelism.
    /// </summary>
    /// <typeparam name="TSource">The source item type.</typeparam>
    /// <typeparam name="TResult">The selected result value type.</typeparam>
    /// <typeparam name="TError">The result error type.</typeparam>
    /// <param name="source">The asynchronous sequence to traverse.</param>
    /// <param name="maxConcurrency">The maximum number of started selector operations.</param>
    /// <param name="selector">The ValueTask-based result-producing selector.</param>
    /// <param name="cancellationToken">The caller cancellation token.</param>
    /// <returns>
    /// An asynchronous operation that produces values in source order, or the source-earliest normally completed
    /// failure among started selectors. Once a failure is observed, remaining started work is canceled and drained.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> or <paramref name="selector"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxConcurrency"/> is less than one.</exception>
    public static ValueTask<Result<IReadOnlyList<TResult>, TError>> TraverseParallelValueAsync<TSource, TResult, TError>(
        this IAsyncEnumerable<TSource> source,
        int maxConcurrency,
        Func<TSource, ValueTask<Result<TResult, TError>>> selector,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxConcurrency, 1);

        return TraverseResultValueAsyncCore(
            source,
            maxConcurrency,
            (value, _) => selector(value),
            cancellationToken);
    }

    /// <summary>
    /// Applies a cancellation-aware result-producing selector to source items with bounded parallelism.
    /// </summary>
    /// <typeparam name="TSource">The source item type.</typeparam>
    /// <typeparam name="TResult">The selected result value type.</typeparam>
    /// <typeparam name="TError">The result error type.</typeparam>
    /// <param name="source">The asynchronous sequence to traverse.</param>
    /// <param name="maxConcurrency">The maximum number of started selector operations.</param>
    /// <param name="selector">The cancellation-aware ValueTask-based result-producing selector.</param>
    /// <param name="cancellationToken">The caller cancellation token.</param>
    /// <returns>
    /// An asynchronous operation that produces values in source order, or the source-earliest normally completed
    /// failure among started selectors. The selector and enumerator receive a linked operation token; cleanup completes first.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> or <paramref name="selector"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxConcurrency"/> is less than one.</exception>
    public static ValueTask<Result<IReadOnlyList<TResult>, TError>> TraverseParallelValueAsync<TSource, TResult, TError>(
        this IAsyncEnumerable<TSource> source,
        int maxConcurrency,
        Func<TSource, CancellationToken, ValueTask<Result<TResult, TError>>> selector,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxConcurrency, 1);

        return TraverseResultValueAsyncCore(source, maxConcurrency, selector, cancellationToken);
    }

    /// <summary>
    /// Applies a validation-producing selector to every source item with bounded parallelism.
    /// </summary>
    /// <typeparam name="TSource">The source item type.</typeparam>
    /// <typeparam name="TResult">The selected validation value type.</typeparam>
    /// <typeparam name="TError">The validation error type.</typeparam>
    /// <param name="source">The asynchronous sequence to traverse.</param>
    /// <param name="maxConcurrency">The maximum number of started selector operations.</param>
    /// <param name="selector">The ValueTask-based validation-producing selector.</param>
    /// <param name="cancellationToken">The caller cancellation token.</param>
    /// <returns>
    /// An asynchronous operation that, in the absence of cancellation or ordinary exceptions, processes every source
    /// item and returns values or errors in source order. Errors within each invalid validation retain their order.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> or <paramref name="selector"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxConcurrency"/> is less than one.</exception>
    public static ValueTask<Validation<IReadOnlyList<TResult>, TError>> TraverseParallelValueAsync<TSource, TResult, TError>(
        this IAsyncEnumerable<TSource> source,
        int maxConcurrency,
        Func<TSource, ValueTask<Validation<TResult, TError>>> selector,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxConcurrency, 1);

        return TraverseValidationValueAsyncCore(
            source,
            maxConcurrency,
            (value, _) => selector(value),
            cancellationToken);
    }

    /// <summary>
    /// Applies a cancellation-aware validation-producing selector to every source item with bounded parallelism.
    /// </summary>
    /// <typeparam name="TSource">The source item type.</typeparam>
    /// <typeparam name="TResult">The selected validation value type.</typeparam>
    /// <typeparam name="TError">The validation error type.</typeparam>
    /// <param name="source">The asynchronous sequence to traverse.</param>
    /// <param name="maxConcurrency">The maximum number of started selector operations.</param>
    /// <param name="selector">The cancellation-aware ValueTask-based validation-producing selector.</param>
    /// <param name="cancellationToken">The caller cancellation token.</param>
    /// <returns>
    /// An asynchronous operation that, in the absence of cancellation or ordinary exceptions, processes every source
    /// item and returns values or errors in source order. The selector and enumerator receive a linked operation token,
    /// and cancellation waits for started work to finish.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> or <paramref name="selector"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxConcurrency"/> is less than one.</exception>
    public static ValueTask<Validation<IReadOnlyList<TResult>, TError>> TraverseParallelValueAsync<TSource, TResult, TError>(
        this IAsyncEnumerable<TSource> source,
        int maxConcurrency,
        Func<TSource, CancellationToken, ValueTask<Validation<TResult, TError>>> selector,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxConcurrency, 1);

        return TraverseValidationValueAsyncCore(source, maxConcurrency, selector, cancellationToken);
    }

    private static async ValueTask<Option<IReadOnlyList<TResult>>> TraverseOptionValueAsyncCore<TSource, TResult>(
        IAsyncEnumerable<TSource> source,
        int maxConcurrency,
        Func<TSource, CancellationToken, ValueTask<Option<TResult>>> selector,
        CancellationToken cancellationToken)
    {
        var outcomes = await TraverseParallelCore(
            source,
            maxConcurrency,
            selector,
            static option => option.IsNone,
            cancellationToken).ConfigureAwait(false);

        outcomes.Sort(static (left, right) => left.Index.CompareTo(right.Index));
        return outcomes.Traverse(static outcome => outcome.Outcome);
    }

    private static async ValueTask<Result<IReadOnlyList<TResult>, TError>> TraverseResultValueAsyncCore<TSource, TResult, TError>(
        IAsyncEnumerable<TSource> source,
        int maxConcurrency,
        Func<TSource, CancellationToken, ValueTask<Result<TResult, TError>>> selector,
        CancellationToken cancellationToken)
    {
        var outcomes = await TraverseParallelCore(
            source,
            maxConcurrency,
            selector,
            static result => result.IsFailure,
            cancellationToken).ConfigureAwait(false);

        outcomes.Sort(static (left, right) => left.Index.CompareTo(right.Index));
        return outcomes.Traverse(static outcome => outcome.Outcome);
    }

    private static async ValueTask<Validation<IReadOnlyList<TResult>, TError>> TraverseValidationValueAsyncCore<TSource, TResult, TError>(
        IAsyncEnumerable<TSource> source,
        int maxConcurrency,
        Func<TSource, CancellationToken, ValueTask<Validation<TResult, TError>>> selector,
        CancellationToken cancellationToken)
    {
        var outcomes = await TraverseParallelCore(
            source,
            maxConcurrency,
            selector,
            static _ => false,
            cancellationToken).ConfigureAwait(false);

        outcomes.Sort(static (left, right) => left.Index.CompareTo(right.Index));
        return outcomes.Traverse(static outcome => outcome.Outcome);
    }

    private static async ValueTask<List<IndexedOutcome<TOutcome>>> TraverseParallelCore<TSource, TOutcome>(
        IAsyncEnumerable<TSource> source,
        int maxConcurrency,
        Func<TSource, CancellationToken, ValueTask<TOutcome>> selector,
        Func<TOutcome, bool> isTerminal,
        CancellationToken cancellationToken)
    {
        using var operationCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var operationToken = operationCancellationSource.Token;
        var outcomes = new List<IndexedOutcome<TOutcome>>();
        var active = new List<StartedOutcome<TOutcome>>(maxConcurrency);
        List<Exception>? failures = null;
        IAsyncEnumerator<TSource>? enumerator = null;
        var sourceCompleted = false;
        var stopAdmission = false;
        var nextIndex = 0;

        void StopOperations()
        {
            stopAdmission = true;
            if (operationCancellationSource.IsCancellationRequested)
            {
                return;
            }

            try
            {
                operationCancellationSource.Cancel();
            }
            catch (Exception exception)
            {
                (failures ??= new List<Exception>()).Add(exception);
            }
        }

        void RecordException(Exception exception)
        {
            if (exception is OperationCanceledException cancellation &&
                operationCancellationSource.IsCancellationRequested &&
                cancellation.CancellationToken == operationToken)
            {
                return;
            }

            (failures ??= new List<Exception>()).Add(exception);
            StopOperations();
        }

        async Task ObserveCompletedAsync()
        {
            for (var index = 0; index < active.Count;)
            {
                var started = active[index];
                if (!started.Task.IsCompleted)
                {
                    index++;
                    continue;
                }

                active.RemoveAt(index);
                try
                {
                    var outcome = await started.Task.ConfigureAwait(false);
                    outcomes.Add(new IndexedOutcome<TOutcome>(started.Index, outcome));
                    if (isTerminal(outcome))
                    {
                        StopOperations();
                    }
                }
                catch (Exception exception)
                {
                    RecordException(exception);
                }
            }
        }

        async Task DrainMoveNextAsync(Task<bool> moveNextTask)
        {
            try
            {
                _ = await moveNextTask.ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                RecordException(exception);
            }
        }

        try
        {
            enumerator = source.GetAsyncEnumerator(operationToken);
            while (!sourceCompleted && !stopAdmission)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    StopOperations();
                    break;
                }

                while (active.Count < maxConcurrency && !sourceCompleted && !stopAdmission)
                {
                    var moveNextTask = AwaitMoveNextAsync(enumerator);
                    if (active.Count > 0 && !moveNextTask.IsCompleted)
                    {
                        var workerSignal = Task.WhenAny(active.Select(static started => started.Task));
                        var signal = await Task.WhenAny(moveNextTask, workerSignal).ConfigureAwait(false);
                        if (signal == workerSignal)
                        {
                            await ObserveCompletedAsync().ConfigureAwait(false);
                            if (stopAdmission)
                            {
                                await DrainMoveNextAsync(moveNextTask).ConfigureAwait(false);
                                break;
                            }
                        }
                    }

                    bool moved;
                    try
                    {
                        moved = await moveNextTask.ConfigureAwait(false);
                    }
                    catch (Exception exception)
                    {
                        RecordException(exception);
                        break;
                    }

                    if (!moved)
                    {
                        sourceCompleted = true;
                        break;
                    }

                    await ObserveCompletedAsync().ConfigureAwait(false);
                    if (stopAdmission || cancellationToken.IsCancellationRequested)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            StopOperations();
                        }

                        break;
                    }

                    var item = enumerator.Current;
                    active.Add(new StartedOutcome<TOutcome>(
                        nextIndex++,
                        Effect.Invoke(selector, item, operationToken).AsTask()));
                    await ObserveCompletedAsync().ConfigureAwait(false);
                }

                if (stopAdmission || sourceCompleted)
                {
                    break;
                }

                await Task.WhenAny(active.Select(static started => started.Task)).ConfigureAwait(false);
                await ObserveCompletedAsync().ConfigureAwait(false);
            }
        }
        catch (Exception exception)
        {
            RecordException(exception);
        }
        finally
        {
            if (stopAdmission || cancellationToken.IsCancellationRequested || failures is not null)
            {
                StopOperations();
            }

            while (active.Count > 0)
            {
                await Task.WhenAny(active.Select(static started => started.Task)).ConfigureAwait(false);
                await ObserveCompletedAsync().ConfigureAwait(false);
            }

            if (enumerator is not null)
            {
                try
                {
                    await enumerator.DisposeAsync().ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    RecordException(exception);
                }
            }
        }

        if (cancellationToken.IsCancellationRequested)
        {
            var cancellation = new OperationCanceledException(cancellationToken);
            if (failures is null)
            {
                throw cancellation;
            }

            failures.Insert(0, cancellation);
            throw new AggregateException(failures);
        }

        ThrowFailures(failures);
        return outcomes;
    }

    private static async Task<bool> AwaitMoveNextAsync<TSource>(IAsyncEnumerator<TSource> enumerator) =>
        await enumerator.MoveNextAsync().ConfigureAwait(false);

    private static void ThrowFailures(List<Exception>? failures)
    {
        if (failures is null)
        {
            return;
        }

        if (failures.Count == 1)
        {
            ExceptionDispatchInfo.Capture(failures[0]).Throw();
            return;
        }

        throw new AggregateException(failures);
    }

    private readonly record struct IndexedOutcome<TOutcome>(int Index, TOutcome Outcome);

    private readonly record struct StartedOutcome<TOutcome>(int Index, Task<TOutcome> Task);
}
