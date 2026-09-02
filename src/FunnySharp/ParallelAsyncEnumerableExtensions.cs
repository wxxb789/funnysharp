using System.Runtime.ExceptionServices;
using System.Threading.Channels;
using System.Threading.Tasks.Sources;

namespace FunnySharp;

/// <summary>
/// Provides bounded parallel mapping operations for asynchronous sequences.
/// </summary>
public static class ParallelAsyncEnumerableExtensions
{
    /// <summary>
    /// Asynchronously maps source items with at most <paramref name="maxConcurrency"/> selectors in flight
    /// and yields the results in source order.
    /// </summary>
    /// <typeparam name="TSource">The source item type.</typeparam>
    /// <typeparam name="TResult">The selected result type.</typeparam>
    /// <param name="source">The asynchronous sequence to process.</param>
    /// <param name="maxConcurrency">The maximum number of started but not yet delivered selectors.</param>
    /// <param name="selector">The ValueTask-based selector to apply to each source item.</param>
    /// <returns>
    /// A deferred asynchronous sequence of selected values in source order. The first observed source or selector
    /// failure stops admission, cancels the linked operation, drains started work, and then propagates.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> or <paramref name="selector"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="maxConcurrency"/> is less than one.
    /// </exception>
    public static IAsyncEnumerable<TResult> SelectParallelValueAsync<TSource, TResult>(
        this IAsyncEnumerable<TSource> source,
        int maxConcurrency,
        Func<TSource, ValueTask<TResult>> selector)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxConcurrency, 1);
        ArgumentNullException.ThrowIfNull(selector);

        return new ParallelSelectAsyncEnumerable<TSource, TResult>(
            source,
            maxConcurrency,
            (item, _) => selector(item));
    }

    /// <summary>
    /// Asynchronously maps source items with at most <paramref name="maxConcurrency"/> selectors in flight
    /// and yields the results in source order.
    /// </summary>
    /// <typeparam name="TSource">The source item type.</typeparam>
    /// <typeparam name="TResult">The selected result type.</typeparam>
    /// <param name="source">The asynchronous sequence to process.</param>
    /// <param name="maxConcurrency">The maximum number of started but not yet delivered selectors.</param>
    /// <param name="selector">The cancellation-aware ValueTask-based selector to apply to each source item.</param>
    /// <returns>
    /// A deferred asynchronous sequence of selected values in source order. The first observed source or selector
    /// failure stops admission, cancels the linked operation, drains started work, and then propagates.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/> or <paramref name="selector"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="maxConcurrency"/> is less than one.
    /// </exception>
    public static IAsyncEnumerable<TResult> SelectParallelValueAsync<TSource, TResult>(
        this IAsyncEnumerable<TSource> source,
        int maxConcurrency,
        Func<TSource, CancellationToken, ValueTask<TResult>> selector)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxConcurrency, 1);
        ArgumentNullException.ThrowIfNull(selector);

        return new ParallelSelectAsyncEnumerable<TSource, TResult>(source, maxConcurrency, selector);
    }

    private sealed class ParallelSelectAsyncEnumerable<TSource, TResult>(
        IAsyncEnumerable<TSource> source,
        int maxConcurrency,
        Func<TSource, CancellationToken, ValueTask<TResult>> selector) : IAsyncEnumerable<TResult>
    {
        public IAsyncEnumerator<TResult> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
            new Enumerator(source, maxConcurrency, selector, cancellationToken);

        private sealed class Enumerator(
            IAsyncEnumerable<TSource> source,
            int maxConcurrency,
            Func<TSource, CancellationToken, ValueTask<TResult>> selector,
            CancellationToken enumerationCancellationToken) : IAsyncEnumerator<TResult>, IValueTaskSource<bool>
        {
            private readonly object gate = new();
            private readonly object moveNextGate = new();
            private readonly List<StartedWork> started = [];
            private readonly List<Exception> backgroundFailures = [];
            private ManualResetValueTaskSourceCore<bool> moveNextSource = new()
            {
                RunContinuationsAsynchronously = true,
            };
            private Channel<Task<TResult>>? channel;
            private CancellationTokenSource? operationCancellation;
            private IAsyncEnumerator<TSource>? sourceEnumerator;
            private SemaphoreSlim? window;
            private Task? producer;
            private Task<List<Exception>>? cleanup;
            private ValueTask<bool> pendingMoveNext;
            private Action? moveNextContinuation;
            private Exception? terminalFailure;
            private bool initialized;
            private int disposed;
            private int moveNextInProgress;

            public TResult Current { get; private set; } = default!;

            public ValueTask<bool> MoveNextAsync()
            {
                lock (moveNextGate)
                {
                    if (disposed != 0)
                    {
                        return ValueTask.FromException<bool>(new ObjectDisposedException(nameof(Enumerator)));
                    }

                    if (moveNextInProgress != 0)
                    {
                        return ValueTask.FromException<bool>(
                            new InvalidOperationException("Concurrent MoveNextAsync calls are not supported."));
                    }

                    moveNextInProgress = 1;
                }

                moveNextSource.Reset();
                var version = moveNextSource.Version;
                pendingMoveNext = MoveNextCoreAsync();
                if (pendingMoveNext.IsCompleted)
                {
                    CompleteMoveNext();
                }
                else
                {
                    pendingMoveNext.ConfigureAwait(false).GetAwaiter()
                        .UnsafeOnCompleted(moveNextContinuation ??= CompleteMoveNext);
                }

                return new ValueTask<bool>(this, version);
            }

            public async ValueTask DisposeAsync()
            {
                lock (moveNextGate)
                {
                    if (disposed != 0)
                    {
                        return;
                    }

                    disposed = 1;
                }

                var failures = await StopAndCleanupAsync(null).ConfigureAwait(false);
                ThrowCleanupFailures(failures);
            }

            bool IValueTaskSource<bool>.GetResult(short token)
            {
                var hasValue = false;
                try
                {
                    hasValue = moveNextSource.GetResult(token);
                    return hasValue;
                }
                finally
                {
                    lock (moveNextGate)
                    {
                        if (hasValue && disposed == 0)
                        {
                            window!.Release();
                        }

                        moveNextInProgress = 0;
                    }
                }
            }

            ValueTaskSourceStatus IValueTaskSource<bool>.GetStatus(short token) =>
                moveNextSource.GetStatus(token);

            void IValueTaskSource<bool>.OnCompleted(
                Action<object?> continuation,
                object? state,
                short token,
                ValueTaskSourceOnCompletedFlags flags) =>
                moveNextSource.OnCompleted(continuation, state, token, flags);

            private void CompleteMoveNext()
            {
                try
                {
                    moveNextSource.SetResult(pendingMoveNext.GetAwaiter().GetResult());
                }
                catch (Exception exception)
                {
                    moveNextSource.SetException(exception);
                }
            }

            private async ValueTask<bool> MoveNextCoreAsync()
            {
                try
                {
                    EnsureInitialized();

                    if (!await channel!.Reader.WaitToReadAsync(operationCancellation!.Token).ConfigureAwait(false))
                    {
                        MarkDisposed();
                        var failures = await StopAndCleanupAsync(null).ConfigureAwait(false);
                        ThrowCleanupFailures(failures);
                        return false;
                    }

                    var next = await channel.Reader.ReadAsync(operationCancellation.Token).ConfigureAwait(false);
                    Current = await next.ConfigureAwait(false);
                    lock (gate)
                    {
                        for (var index = 0; index < started.Count; index++)
                        {
                            if (ReferenceEquals(started[index].Task, next))
                            {
                                started.RemoveAt(index);
                                break;
                            }
                        }
                    }

                    return true;
                }
                catch (Exception exception)
                {
                    var primaryFailure = enumerationCancellationToken.IsCancellationRequested
                        ? new OperationCanceledException(enumerationCancellationToken)
                        : Volatile.Read(ref terminalFailure) ?? exception;
                    MarkDisposed();
                    var failures = await StopAndCleanupAsync(primaryFailure).ConfigureAwait(false);
                    ThrowPrimaryFailure(primaryFailure, failures);
                    return false;
                }
            }

            private void MarkDisposed()
            {
                lock (moveNextGate)
                {
                    disposed = 1;
                }
            }

            private void EnsureInitialized()
            {
                if (initialized)
                {
                    return;
                }

                initialized = true;
                operationCancellation = CancellationTokenSource.CreateLinkedTokenSource(enumerationCancellationToken);
                channel = Channel.CreateBounded<Task<TResult>>(new BoundedChannelOptions(maxConcurrency)
                {
                    FullMode = BoundedChannelFullMode.Wait,
                    SingleReader = true,
                    SingleWriter = true,
                });
                window = new SemaphoreSlim(maxConcurrency, maxConcurrency);
                producer = ProduceAsync();
            }

            private async Task ProduceAsync()
            {
                var ownsWindowSlot = false;

                try
                {
                    var cancellationToken = operationCancellation!.Token;
                    sourceEnumerator = source.GetAsyncEnumerator(cancellationToken);

                    while (true)
                    {
                        await window!.WaitAsync(cancellationToken).ConfigureAwait(false);
                        ownsWindowSlot = true;

                        if (!await sourceEnumerator.MoveNextAsync().ConfigureAwait(false))
                        {
                            window.Release();
                            ownsWindowSlot = false;
                            break;
                        }

                        cancellationToken.ThrowIfCancellationRequested();

                        var work = Effect.Invoke(selector, sourceEnumerator.Current, cancellationToken).AsTask();
                        var observer = ObserveSelectorAsync(work);
                        lock (gate)
                        {
                            started.Add(new StartedWork(work, observer));
                        }

                        await channel!.Writer.WriteAsync(work, cancellationToken).ConfigureAwait(false);
                        ownsWindowSlot = false;
                    }

                    channel!.Writer.TryComplete();
                }
                catch (Exception exception)
                {
                    if (ownsWindowSlot)
                    {
                        window!.Release();
                    }

                    if (!operationCancellation!.IsCancellationRequested)
                    {
                        Interlocked.CompareExchange(ref terminalFailure, exception, null);
                        channel!.Writer.TryComplete(exception);
                        operationCancellation.Cancel();
                    }
                    else
                    {
                        if (!IsOperationCancellation(exception))
                        {
                            lock (gate)
                            {
                                backgroundFailures.Add(exception);
                            }
                        }

                        channel!.Writer.TryComplete();
                    }
                }
            }

            private async Task ObserveSelectorAsync(Task<TResult> work)
            {
                try
                {
                    _ = await work.ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    if (IsOperationCancellation(exception) ||
                        Interlocked.CompareExchange(ref terminalFailure, exception, null) is not null)
                    {
                        return;
                    }

                    try
                    {
                        operationCancellation!.Cancel();
                    }
                    catch (Exception cancellationFailure)
                    {
                        lock (gate)
                        {
                            backgroundFailures.Add(cancellationFailure);
                        }
                    }
                }
            }

            private Task<List<Exception>> StopAndCleanupAsync(Exception? primaryFailure)
            {
                lock (gate)
                {
                    return cleanup ??= StopAndCleanupCoreAsync(primaryFailure);
                }
            }

            private async Task<List<Exception>> StopAndCleanupCoreAsync(Exception? primaryFailure)
            {
                var failures = new List<Exception>();

                if (!initialized)
                {
                    return failures;
                }

                try
                {
                    try
                    {
                        operationCancellation!.Cancel();
                    }
                    catch (Exception exception)
                    {
                        AddFailure(failures, exception, primaryFailure, suppressOperationCancellation: false);
                    }

                    channel!.Writer.TryComplete();

                    try
                    {
                        await producer!.ConfigureAwait(false);
                    }
                    catch (Exception exception)
                    {
                        AddFailure(failures, exception, primaryFailure, suppressOperationCancellation: false);
                    }

                    StartedWork[] pending;
                    lock (gate)
                    {
                        pending = [.. started];
                    }

                    foreach (var work in pending)
                    {
                        try
                        {
                            _ = await work.Task.ConfigureAwait(false);
                        }
                        catch (Exception exception)
                        {
                            AddFailure(failures, exception, primaryFailure, suppressOperationCancellation: true);
                        }
                    }

                    foreach (var work in pending)
                    {
                        await work.Observer.ConfigureAwait(false);
                    }

                    lock (gate)
                    {
                        failures.AddRange(backgroundFailures);
                    }

                    if (sourceEnumerator is not null)
                    {
                        try
                        {
                            await sourceEnumerator.DisposeAsync().ConfigureAwait(false);
                        }
                        catch (Exception exception)
                        {
                            AddFailure(failures, exception, primaryFailure, suppressOperationCancellation: false);
                        }
                    }
                }
                finally
                {
                    operationCancellation!.Dispose();
                    window!.Dispose();
                }
                return failures;
            }

            private void AddFailure(
                ICollection<Exception> failures,
                Exception exception,
                Exception? primaryFailure,
                bool suppressOperationCancellation)
            {
                if (suppressOperationCancellation && IsOperationCancellation(exception))
                {
                    return;
                }

                if (ReferenceEquals(exception, primaryFailure))
                {
                    return;
                }

                if (exception is AggregateException aggregateException)
                {
                    foreach (var innerException in aggregateException.Flatten().InnerExceptions)
                    {
                        AddFailure(failures, innerException, primaryFailure, suppressOperationCancellation);
                    }

                    return;
                }

                failures.Add(exception);
            }

            private bool IsOperationCancellation(Exception exception) =>
                exception is OperationCanceledException cancellation &&
                operationCancellation is { IsCancellationRequested: true } &&
                cancellation.CancellationToken == operationCancellation.Token;

            private static void ThrowPrimaryFailure(Exception primaryFailure, IReadOnlyList<Exception> cleanupFailures)
            {
                List<Exception>? additionalFailures = null;
                for (var index = 0; index < cleanupFailures.Count; index++)
                {
                    var cleanupFailure = cleanupFailures[index];
                    if (!ReferenceEquals(cleanupFailure, primaryFailure))
                    {
                        (additionalFailures ??= []).Add(cleanupFailure);
                    }
                }

                if (additionalFailures is null)
                {
                    ExceptionDispatchInfo.Capture(primaryFailure).Throw();
                }

                var failures = new List<Exception>(additionalFailures.Count + 1) { primaryFailure };
                failures.AddRange(additionalFailures);
                throw new AggregateException(failures);
            }

            private static void ThrowCleanupFailures(IReadOnlyList<Exception> failures)
            {
                if (failures.Count == 0)
                {
                    return;
                }

                if (failures.Count == 1)
                {
                    ExceptionDispatchInfo.Capture(failures[0]).Throw();
                }

                throw new AggregateException(failures);
            }

            private readonly record struct StartedWork(Task<TResult> Task, Task Observer);
        }
    }
}
