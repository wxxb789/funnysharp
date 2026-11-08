using System.Runtime.ExceptionServices;
using System.Threading.Channels;

namespace FunnySharp;

/// <summary>
/// Provides bounded concurrent processing of asynchronous sequences with per-key ordering.
/// </summary>
public static class KeyedParallelAsyncEnumerableExtensions
{
    /// <summary>
    /// Processes source items with at most <paramref name="maxConcurrency"/> started operations and at most
    /// one operation at a time per key, yielding each typed outcome when its operation completes.
    /// </summary>
    /// <typeparam name="TSource">The source item type.</typeparam>
    /// <typeparam name="TKey">The key that identifies the serialization partition.</typeparam>
    /// <typeparam name="TResult">The successful result value type.</typeparam>
    /// <typeparam name="TError">The typed error type.</typeparam>
    /// <param name="source">The asynchronous sequence to process.</param>
    /// <param name="keySelector">Selects the key that orders operations; the selector is invoked once per read item.</param>
    /// <param name="maxConcurrency">The maximum number of started but not yet delivered operations.</param>
    /// <param name="maxBufferedItems">
    /// The maximum number of source items read but not yet started. Must be at least one because a key is known
    /// only after its source item has been read.
    /// </param>
    /// <param name="selector">The ValueTask-based operation to run for each source item.</param>
    /// <param name="comparer">
    /// The key equality policy. <see langword="null"/> uses <see cref="EqualityComparer{T}.Default"/>.
    /// </param>
    /// <returns>
    /// A deferred asynchronous sequence of key, source item, and typed outcome tuples in operation-completion order.
    /// Outcomes for one key are yielded in source order. A later operation for a key starts only after the previous
    /// operation for that key has completed. The first observed source or callback fault stops admission, cancels and
    /// drains started work, disposes the source enumerator, and propagates. A non-cooperating callback is awaited
    /// during cleanup and cannot be forcibly terminated.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/>, <paramref name="keySelector"/>, or <paramref name="selector"/> is
    /// <see langword="null"/>. During enumeration, the same exception reports a key selector that returned
    /// <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="maxConcurrency"/> or <paramref name="maxBufferedItems"/> is less than one.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// A callback returns an uninitialized <see cref="Result{TValue, TError}"/> value. The fault is terminal and
    /// follows the callback-fault cleanup contract.
    /// </exception>
    public static IAsyncEnumerable<(TKey Key, TSource Source, Result<TResult, TError> Outcome)>
        SelectParallelByKeyCompletionOrderValueAsync<TSource, TKey, TResult, TError>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            int maxConcurrency,
            int maxBufferedItems,
            Func<TSource, ValueTask<Result<TResult, TError>>> selector,
            IEqualityComparer<TKey>? comparer = null)
            where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(keySelector);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxConcurrency, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxBufferedItems, 1);
        ArgumentNullException.ThrowIfNull(selector);

        return new KeyedParallelAsyncEnumerable<TSource, TKey, TResult, TError>(
            source,
            keySelector,
            maxConcurrency,
            maxBufferedItems,
            (item, _) => selector(item),
            comparer ?? EqualityComparer<TKey>.Default);
    }

    /// <summary>
    /// Processes source items with at most <paramref name="maxConcurrency"/> started operations and at most
    /// one operation at a time per key, yielding each typed outcome when its operation completes.
    /// </summary>
    /// <typeparam name="TSource">The source item type.</typeparam>
    /// <typeparam name="TKey">The key that identifies the serialization partition.</typeparam>
    /// <typeparam name="TResult">The successful result value type.</typeparam>
    /// <typeparam name="TError">The typed error type.</typeparam>
    /// <param name="source">The asynchronous sequence to process.</param>
    /// <param name="keySelector">Selects the key that orders operations; the selector is invoked once per read item.</param>
    /// <param name="maxConcurrency">The maximum number of started but not yet delivered operations.</param>
    /// <param name="maxBufferedItems">
    /// The maximum number of source items read but not yet started. Must be at least one because a key is known
    /// only after its source item has been read.
    /// </param>
    /// <param name="selector">The cancellation-aware ValueTask-based operation to run for each source item.</param>
    /// <param name="comparer">
    /// The key equality policy. <see langword="null"/> uses <see cref="EqualityComparer{T}.Default"/>.
    /// </param>
    /// <returns>
    /// A deferred asynchronous sequence of key, source item, and typed outcome tuples in operation-completion order.
    /// Outcomes for one key are yielded in source order. A later operation for a key starts only after the previous
    /// operation for that key has completed. The selector and source enumerator receive one linked operation token.
    /// The first observed source or callback fault stops admission, cancels and drains started work, disposes the
    /// source enumerator, and propagates. A non-cooperating callback is awaited during cleanup and cannot be forcibly
    /// terminated.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="source"/>, <paramref name="keySelector"/>, or <paramref name="selector"/> is
    /// <see langword="null"/>. During enumeration, the same exception reports a key selector that returned
    /// <see langword="null"/>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="maxConcurrency"/> or <paramref name="maxBufferedItems"/> is less than one.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// A callback returns an uninitialized <see cref="Result{TValue, TError}"/> value. The fault is terminal and
    /// follows the callback-fault cleanup contract.
    /// </exception>
    public static IAsyncEnumerable<(TKey Key, TSource Source, Result<TResult, TError> Outcome)>
        SelectParallelByKeyCompletionOrderValueAsync<TSource, TKey, TResult, TError>(
            this IAsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            int maxConcurrency,
            int maxBufferedItems,
            Func<TSource, CancellationToken, ValueTask<Result<TResult, TError>>> selector,
            IEqualityComparer<TKey>? comparer = null)
            where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(keySelector);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxConcurrency, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxBufferedItems, 1);
        ArgumentNullException.ThrowIfNull(selector);

        return new KeyedParallelAsyncEnumerable<TSource, TKey, TResult, TError>(
            source,
            keySelector,
            maxConcurrency,
            maxBufferedItems,
            selector,
            comparer ?? EqualityComparer<TKey>.Default);
    }

    private sealed class KeyedParallelAsyncEnumerable<TSource, TKey, TResult, TError>(
        IAsyncEnumerable<TSource> source,
        Func<TSource, TKey> keySelector,
        int maxConcurrency,
        int maxBufferedItems,
        Func<TSource, CancellationToken, ValueTask<Result<TResult, TError>>> selector,
        IEqualityComparer<TKey> comparer) : IAsyncEnumerable<(TKey Key, TSource Source, Result<TResult, TError> Outcome)>
        where TKey : notnull
    {
        public IAsyncEnumerator<(TKey Key, TSource Source, Result<TResult, TError> Outcome)> GetAsyncEnumerator(
            CancellationToken cancellationToken = default) =>
            new Enumerator(
                source,
                keySelector,
                maxConcurrency,
                maxBufferedItems,
                selector,
                comparer,
                cancellationToken);

        private sealed class Enumerator(
            IAsyncEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            int maxConcurrency,
            int maxBufferedItems,
            Func<TSource, CancellationToken, ValueTask<Result<TResult, TError>>> selector,
            IEqualityComparer<TKey> comparer,
            CancellationToken enumerationCancellationToken)
            : IAsyncEnumerator<(TKey Key, TSource Source, Result<TResult, TError> Outcome)>
        {
            private readonly object gate = new();
            private readonly object moveNextGate = new();
            private readonly Dictionary<TKey, KeyState> keyStates = new(comparer);
            private readonly Queue<KeyState> readyKeys = [];
            private readonly List<WorkEntry> started = [];
            private readonly List<Exception> backgroundFailures = [];
            private readonly List<Exception> callbackFailures = [];
            private Channel<Delivery>? deliveries;
            private CancellationTokenSource? operationCancellation;
            private CancellationToken operationToken;
            private SemaphoreSlim? pendingSlots;
            private IAsyncEnumerator<TSource>? sourceEnumerator;
            private Task? producer;
            private Task<List<Exception>>? cleanup;
            private Task<List<Exception>>? disposal;
            private TaskCompletionSource? startsSettled;
            private (TKey Key, TSource Source, Result<TResult, TError> Outcome) current;
            private int active;
            private int pending;
            private int unsettledStarts;
            private bool sourceCompleted;
            private bool writerCompleted;
            private bool initialized;
            private bool stopAdmission;
            private int disposed;
            private int moveNextInProgress;
            private Exception? terminalFailure;

            public (TKey Key, TSource Source, Result<TResult, TError> Outcome) Current => current;

            public ValueTask<bool> MoveNextAsync()
            {
                lock (moveNextGate)
                {
                    if (disposed != 0)
                    {
                        return ValueTask.FromException<bool>(
                            new ObjectDisposedException(nameof(Enumerator)));
                    }

                    if (moveNextInProgress != 0)
                    {
                        return ValueTask.FromException<bool>(
                            new InvalidOperationException("Concurrent MoveNextAsync calls are not supported."));
                    }

                    moveNextInProgress = 1;
                }

                return MoveNextCoreAsync();
            }

            public ValueTask DisposeAsync()
            {
                Task<List<Exception>> cleanupTask;
                lock (moveNextGate)
                {
                    if (disposal is null)
                    {
                        disposed = 1;

                        // Cleanup starts outside the move-next lock: cancellation callbacks and
                        // observers can re-enter the enumerator while the source is stopped.
                        disposal = DisposeCoreAsync();
                    }

                    cleanupTask = disposal;
                }

                return AwaitCleanupAsync(cleanupTask);
            }

            private async Task<List<Exception>> DisposeCoreAsync()
            {
                await Task.Yield();
                return await StopAndCleanupAsync(null).ConfigureAwait(false);
            }

            private static async ValueTask AwaitCleanupAsync(Task<List<Exception>> cleanupTask)
            {
                var failures = await cleanupTask.ConfigureAwait(false);
                ThrowCleanupFailures(failures);
            }

            private async ValueTask<bool> MoveNextCoreAsync()
            {
                try
                {
                    EnsureInitialized();

                    var hasDelivery = await deliveries!.Reader.WaitToReadAsync(enumerationCancellationToken)
                        .ConfigureAwait(false);

                    // A concurrent disposal completes the writer successfully; that must wake a
                    // pending consumer as cancellation rather than a clean end of sequence.
                    if (disposed != 0 && !enumerationCancellationToken.IsCancellationRequested)
                    {
                        throw new OperationCanceledException(operationToken);
                    }

                    if (!hasDelivery)
                    {
                        if (enumerationCancellationToken.IsCancellationRequested)
                        {
                            throw new OperationCanceledException(enumerationCancellationToken);
                        }

                        MarkDisposed();
                        var failures = await StopAndCleanupAsync(null).ConfigureAwait(false);
                        ThrowCleanupFailures(failures);
                        return false;
                    }

                    // Terminal callback and source faults cancel the operation token. Delivery
                    // reads observe only caller cancellation so outcomes already buffered when a
                    // sibling or source faulted are still drained before the terminal failure.
                    var delivery = await deliveries.Reader.ReadAsync(enumerationCancellationToken)
                        .ConfigureAwait(false);
                    ReleaseDelivery(delivery);
                    current = (delivery.Key, delivery.Source, delivery.Outcome);
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
                finally
                {
                    lock (moveNextGate)
                    {
                        moveNextInProgress = 0;
                    }
                }
            }

            private void EnsureInitialized()
            {
                lock (gate)
                {
                    if (initialized)
                    {
                        return;
                    }

                    if (disposed != 0)
                    {
                        throw new ObjectDisposedException(nameof(Enumerator));
                    }

                    initialized = true;
                    operationCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                        enumerationCancellationToken);
                    operationToken = operationCancellation.Token;
                    deliveries = Channel.CreateBounded<Delivery>(
                        new BoundedChannelOptions(maxConcurrency)
                        {
                            FullMode = BoundedChannelFullMode.Wait,
                            SingleReader = true,
                            SingleWriter = false,
                        });
                    pendingSlots = new SemaphoreSlim(maxBufferedItems, maxBufferedItems);
                    producer = ProduceAsync();
                }
            }

            private async Task ProduceAsync()
            {
                try
                {
                    var cancellationToken = operationToken;
                    sourceEnumerator = source.GetAsyncEnumerator(cancellationToken);

                    while (true)
                    {
                        await pendingSlots!.WaitAsync(cancellationToken).ConfigureAwait(false);

                        bool hasItem;
                        try
                        {
                            hasItem = await sourceEnumerator.MoveNextAsync().ConfigureAwait(false);
                        }
                        catch
                        {
                            pendingSlots.Release();
                            throw;
                        }

                        if (!hasItem)
                        {
                            pendingSlots.Release();
                            break;
                        }

                        var sourceItem = sourceEnumerator.Current;
                        TKey key;
                        try
                        {
                            key = keySelector(sourceItem);
                            if (key is null)
                            {
                                throw new ArgumentNullException(
                                    nameof(keySelector),
                                    "The key selector returned null.");
                            }
                        }
                        catch
                        {
                            pendingSlots.Release();
                            throw;
                        }

                        List<StartRequest>? starts;
                        lock (gate)
                        {
                            if (stopAdmission)
                            {
                                pendingSlots.Release();
                                break;
                            }

                            var keyState = GetOrAddKeyLocked(key);
                            keyState.Items.Enqueue(new PendingItem(sourceItem, key));
                            pending++;

                            if (!keyState.Active && !keyState.InReady)
                            {
                                keyState.InReady = true;
                                readyKeys.Enqueue(keyState);
                            }

                            starts = PumpLocked();
                        }

                        StartOperations(starts);
                    }

                    lock (gate)
                    {
                        sourceCompleted = true;
                        TryCompleteDeliveryLocked();
                    }
                }
                catch (Exception exception)
                {
                    if (!operationCancellation!.IsCancellationRequested)
                    {
                        // Stop admission before the fault becomes observable to delivery
                        // handling. A success already buffered in the channel is still drained,
                        // and that drain releases a concurrency slot; without this flag the pump
                        // would start another callback after the terminal source fault.
                        lock (gate)
                        {
                            stopAdmission = true;
                        }

                        Interlocked.CompareExchange(ref terminalFailure, exception, null);
                        try
                        {
                            operationCancellation.Cancel();
                        }
                        catch (Exception cancellationFailure)
                        {
                            RecordBackgroundFailure(cancellationFailure);
                        }

                        CompleteWriter(exception);
                    }
                    else
                    {
                        if (!IsShutdownArtifact(exception))
                        {
                            RecordBackgroundFailure(exception);
                        }

                        CompleteWriter(null);
                    }
                }
            }

            private KeyState GetOrAddKeyLocked(TKey key)
            {
                if (keyStates.TryGetValue(key, out var existing))
                {
                    return existing;
                }

                var created = new KeyState(key);
                keyStates.Add(key, created);
                return created;
            }

            private List<StartRequest>? PumpLocked()
            {
                if (stopAdmission)
                {
                    return null;
                }

                List<StartRequest>? starts = null;
                while (active < maxConcurrency && readyKeys.Count > 0)
                {
                    var keyState = readyKeys.Dequeue();
                    keyState.InReady = false;
                    var item = keyState.Items.Dequeue();
                    keyState.Active = true;
                    pending--;
                    active++;
                    unsettledStarts++;
                    (starts ??= []).Add(new StartRequest(keyState, item));
                }

                return starts;
            }

            private void StartOperations(List<StartRequest>? starts)
            {
                if (starts is null)
                {
                    return;
                }

                var index = 0;
                try
                {
                    for (; index < starts.Count; index++)
                    {
                        StartOperation(starts[index]);
                    }
                }
                catch
                {
                    // StartOperation settles the request that failed in its own finally. Settle
                    // and drop the requests the loop did not reach so cleanup cannot wait forever.
                    for (index++; index < starts.Count; index++)
                    {
                        lock (gate)
                        {
                            DropStartLocked(starts[index]);
                        }

                        SettleStart();
                    }

                    throw;
                }
            }

            private void StartOperation(StartRequest start)
            {
                try
                {
                    lock (gate)
                    {
                        if (stopAdmission)
                        {
                            DropStartLocked(start);
                            return;
                        }

                        pendingSlots!.Release();
                    }

                    WorkEntry work;
                    try
                    {
                        // Never invoke caller code under the coordinator gate: a selector that
                        // synchronously waits on pipeline progress would deadlock with it.
                        var operation = Effect.Invoke(
                            selector,
                            start.Item.Source,
                            operationToken);
                        work = new WorkEntry(operation.AsTask());
                    }
                    catch
                    {
                        lock (gate)
                        {
                            AbandonStartedSlotLocked(start);
                        }

                        throw;
                    }

                    lock (gate)
                    {
                        started.Add(work);
                        start.Work = work;
                    }

                    start.Work!.Observer = ObserveAsync(start, start.Work);
                }
                finally
                {
                    SettleStart();
                }
            }

            private void DropStartLocked(StartRequest start)
            {
                active--;
                pendingSlots!.Release();
                start.Key.Active = false;
                if (start.Key.Items.Count == 0)
                {
                    RemoveKeyLocked(start.Key);
                }
            }

            private void AbandonStartedSlotLocked(StartRequest start)
            {
                // The pending permit and the active slot were already claimed. No operation was
                // produced by a malformed ValueTask, so release the coordinator slot without
                // releasing the pending permit a second time.
                active--;
                start.Key.Active = false;
                if (start.Key.Items.Count == 0)
                {
                    RemoveKeyLocked(start.Key);
                }
            }

            private void SettleStart()
            {
                TaskCompletionSource? signal = null;
                lock (gate)
                {
                    if (--unsettledStarts == 0)
                    {
                        signal = startsSettled;
                        startsSettled = null;
                    }
                }

                signal?.TrySetResult();
            }

            private async Task WaitForStartsSettledAsync()
            {
                while (true)
                {
                    Task wait;
                    lock (gate)
                    {
                        if (unsettledStarts == 0)
                        {
                            return;
                        }

                        wait = (startsSettled ??= new TaskCompletionSource(
                            TaskCreationOptions.RunContinuationsAsynchronously)).Task;
                    }

                    await wait.ConfigureAwait(false);
                }
            }

            private async Task ObserveAsync(StartRequest start, WorkEntry work)
            {
                try
                {
                    Result<TResult, TError> outcome;
                    try
                    {
                        outcome = await work.Task.ConfigureAwait(false);

                        // Inspect the initialized state once. A default Result is a programming fault,
                        // not a domain failure, and must not be delivered as if it were an outcome.
                        _ = outcome.IsSuccess;
                    }
                    catch (Exception exception)
                    {
                        RecordTerminalFailure(exception);
                        return;
                    }

                    try
                    {
                        await deliveries!.Writer.WriteAsync(
                                new Delivery(start.Key, start.Item.Key, start.Item.Source, outcome),
                                operationToken)
                            .ConfigureAwait(false);
                    }
                    catch (Exception exception) when (IsShutdownArtifact(exception))
                    {
                    }
                    catch (Exception exception)
                    {
                        RecordBackgroundFailure(exception);
                    }
                }
                finally
                {
                    // Completed work must not accumulate for the lifetime of a long stream.
                    // Cleanup only needs to await entries that are still present.
                    lock (gate)
                    {
                        for (var index = 0; index < started.Count; index++)
                        {
                            if (ReferenceEquals(started[index], work))
                            {
                                started.RemoveAt(index);
                                break;
                            }
                        }
                    }
                }
            }

            private void RecordTerminalFailure(Exception exception)
            {
                if (IsOperationCancellation(exception))
                {
                    return;
                }

                // Every independent fault is retained before the terminal compare-exchange. A
                // second fault can otherwise be lost when its observer removes the work entry
                // before cleanup snapshots the started list, and cleanup initiated by disposal or
                // caller cancellation has no primary failure of its own to recover.
                //
                // A terminal callback fault also stops admission immediately: outcomes already
                // buffered in the delivery channel are still drained, and draining a success
                // releases a concurrency slot. Without this flag the pump would start another
                // callback after the fault, before cleanup runs.
                lock (gate)
                {
                    stopAdmission = true;
                    callbackFailures.Add(exception);
                }

                if (Interlocked.CompareExchange(ref terminalFailure, exception, null) is not null)
                {
                    return;
                }

                try
                {
                    operationCancellation!.Cancel();
                }
                catch (Exception cancellationFailure)
                {
                    RecordBackgroundFailure(cancellationFailure);
                }

                CompleteWriter(exception);
            }

            private void ReleaseDelivery(Delivery delivery)
            {
                List<StartRequest>? starts;
                lock (gate)
                {
                    active--;
                    var keyState = delivery.State;
                    keyState.Active = false;

                    if (keyState.Items.Count > 0)
                    {
                        if (!keyState.InReady)
                        {
                            keyState.InReady = true;
                            readyKeys.Enqueue(keyState);
                        }
                    }
                    else
                    {
                        RemoveKeyLocked(keyState);
                    }

                    starts = PumpLocked();
                    TryCompleteDeliveryLocked();
                }

                StartOperations(starts);
            }

            private void RemoveKeyLocked(KeyState keyState)
            {
                if (keyStates.TryGetValue(keyState.Key, out var current) &&
                    ReferenceEquals(current, keyState))
                {
                    keyStates.Remove(keyState.Key);
                }
            }

            private void TryCompleteDeliveryLocked()
            {
                if (!writerCompleted && sourceCompleted && active == 0 && pending == 0)
                {
                    writerCompleted = true;
                    deliveries!.Writer.TryComplete();
                }
            }

            private void CompleteWriter(Exception? failure)
            {
                lock (gate)
                {
                    if (writerCompleted)
                    {
                        return;
                    }

                    writerCompleted = true;
                    if (failure is null)
                    {
                        deliveries!.Writer.TryComplete();
                    }
                    else
                    {
                        deliveries!.Writer.TryComplete(failure);
                    }
                }
            }

            private void RecordBackgroundFailure(Exception exception)
            {
                lock (gate)
                {
                    backgroundFailures.Add(exception);
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
                    lock (gate)
                    {
                        stopAdmission = true;
                    }

                    try
                    {
                        operationCancellation!.Cancel();
                    }
                    catch (Exception exception)
                    {
                        AddFailure(failures, exception, primaryFailure, suppressOperationCancellation: false);
                    }

                    CompleteWriter(null);

                    try
                    {
                        await producer!.ConfigureAwait(false);
                    }
                    catch (Exception exception)
                    {
                        AddFailure(failures, exception, primaryFailure, suppressOperationCancellation: false);
                    }

                    await WaitForStartsSettledAsync().ConfigureAwait(false);

                    WorkEntry[] pendingWork;
                    lock (gate)
                    {
                        pendingWork = [.. started];
                    }

                    foreach (var work in pendingWork)
                    {
                        try
                        {
                            _ = await work.Task.ConfigureAwait(false);
                        }
                        catch (Exception exception)
                        {
                            AddFailure(
                                failures,
                                exception,
                                primaryFailure,
                                suppressOperationCancellation: true);
                        }
                    }

                    // Observers register deliveries and faults; no observer may outlive the enumeration.
                    foreach (var work in pendingWork)
                    {
                        try
                        {
                            await work.Observer.ConfigureAwait(false);
                        }
                        catch (Exception exception)
                        {
                            AddFailure(
                                failures,
                                exception,
                                primaryFailure,
                                suppressOperationCancellation: false);
                        }
                    }

                    List<Exception> recordedFaults;
                    Exception? terminalSnapshot;
                    lock (gate)
                    {
                        recordedFaults = [.. backgroundFailures, .. callbackFailures];
                        terminalSnapshot = terminalFailure;
                    }

                    foreach (var recordedFault in recordedFaults)
                    {
                        AddFailure(
                            failures,
                            recordedFault,
                            primaryFailure,
                            suppressOperationCancellation: false);
                    }

                    // Disposal and caller cancellation can initiate cleanup without the terminal
                    // fault as primary, so always preserve it; reference deduplication keeps a
                    // callback fault from appearing twice.
                    if (terminalSnapshot is not null)
                    {
                        AddFailure(
                            failures,
                            terminalSnapshot,
                            primaryFailure,
                            suppressOperationCancellation: false);
                    }

                    if (sourceEnumerator is not null)
                    {
                        try
                        {
                            await sourceEnumerator.DisposeAsync().ConfigureAwait(false);
                        }
                        catch (Exception exception)
                        {
                            AddFailure(
                                failures,
                                exception,
                                primaryFailure,
                                suppressOperationCancellation: false);
                        }
                    }
                }
                finally
                {
                    operationCancellation!.Dispose();
                    pendingSlots!.Dispose();
                }

                return failures;
            }

            private void AddFailure(
                List<Exception> failures,
                Exception exception,
                Exception? primaryFailure,
                bool suppressOperationCancellation)
            {
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

                if (suppressOperationCancellation && IsOperationCancellation(exception))
                {
                    return;
                }

                for (var index = 0; index < failures.Count; index++)
                {
                    if (ReferenceEquals(failures[index], exception))
                    {
                        return;
                    }
                }

                failures.Add(exception);
            }

            private bool IsOperationCancellation(Exception exception) =>
                exception is OperationCanceledException cancellation &&
                operationToken.IsCancellationRequested &&
                cancellation.CancellationToken == operationToken;

            private bool IsShutdownArtifact(Exception exception) =>
                IsOperationCancellation(exception) || exception is ChannelClosedException;

            private void MarkDisposed()
            {
                lock (moveNextGate)
                {
                    disposed = 1;
                }
            }

            private static void ThrowPrimaryFailure(
                Exception primaryFailure,
                IReadOnlyList<Exception> cleanupFailures)
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

            private sealed class KeyState(TKey key)
            {
                public TKey Key { get; } = key;

                public Queue<PendingItem> Items { get; } = new();

                public bool Active { get; set; }

                public bool InReady { get; set; }
            }

            private readonly record struct PendingItem(TSource Source, TKey Key);

            private sealed class StartRequest(KeyState key, PendingItem item)
            {
                public KeyState Key { get; } = key;

                public PendingItem Item { get; } = item;

                public WorkEntry? Work { get; set; }
            }

            private sealed class WorkEntry(Task<Result<TResult, TError>> task)
            {
                public Task<Result<TResult, TError>> Task { get; } = task;

                public Task Observer { get; set; } = System.Threading.Tasks.Task.CompletedTask;
            }

            private readonly record struct Delivery(
                KeyState State,
                TKey Key,
                TSource Source,
                Result<TResult, TError> Outcome);
        }
    }
}
