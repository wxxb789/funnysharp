using System.Collections.Generic;
using System.Threading.Channels;

namespace FunnySharp.TestSupport.KeyedConcurrencyComparison;

internal static class BclKeyedConsumer
{
    internal static async Task<KeyedConsumerReport> RunAsync(
        IReadOnlyList<AccountEvent> events,
        int maxConcurrency,
        int maxBufferedItems,
        CancellationToken cancellationToken = default)
    {
        var processor = new AccountProcessor();
        var outcomes = new List<KeyedOutcome>(events.Count);
        var delivered = 0;
        var source = new CountingAsyncSource<AccountEvent>(
            events,
            () => Volatile.Read(ref delivered));
        var output = Channel.CreateBounded<KeyedOutcome>(
            new BoundedChannelOptions(maxConcurrency)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false,
            });
        var stateGate = new object();
        var tails = new Dictionary<string, Task<Result<decimal, AccountError>>>(StringComparer.Ordinal);
        var active = new HashSet<WorkEntry>();
        // Two limits, matching the FunnySharp coordinator: an unstarted item holds one pending
        // permit until its operation starts, and a started item holds one active permit until its
        // outcome is delivered to the consumer. Pending + active is therefore at most
        // maxBufferedItems + maxConcurrency, and source reading stops while the pending buffer is
        // full. No separate total-retained semaphore is needed.
        var unstarted = new SemaphoreSlim(maxBufferedItems, maxBufferedItems);
        var workSlots = new SemaphoreSlim(maxConcurrency, maxConcurrency);
        using var operationCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var operationToken = operationCancellation.Token;
        var sourceCompleted = false;
        var writerCompleted = false;

        bool IsOperationCancellation(Exception exception) =>
            exception is OperationCanceledException cancellation &&
            operationCancellation.IsCancellationRequested &&
            cancellation.CancellationToken == operationToken;

        bool IsShutdownArtifact(Exception exception) =>
            IsOperationCancellation(exception) ||
            exception is ChannelClosedException;

        void CompleteWriter(Exception? failure)
        {
            lock (stateGate)
            {
                if (writerCompleted)
                {
                    return;
                }

                writerCompleted = true;
            }

            if (failure is null)
            {
                output.Writer.TryComplete();
            }
            else
            {
                output.Writer.TryComplete(failure);
            }
        }

        void TryCompleteIfDone()
        {
            lock (stateGate)
            {
                if (!sourceCompleted || active.Count != 0 || writerCompleted)
                {
                    return;
                }
            }

            CompleteWriter(null);
        }

        async Task ObserveAsync(WorkEntry work, AccountEvent accountEvent)
        {
            try
            {
                _ = await work.Task.ConfigureAwait(false);
            }
            catch (Exception exception) when (IsShutdownArtifact(exception))
            {
            }
            catch (Exception exception)
            {
                CompleteWriter(exception);
            }
            finally
            {
                lock (stateGate)
                {
                    active.Remove(work);
                    if (tails.TryGetValue(accountEvent.AccountId, out var tail) &&
                        ReferenceEquals(tail, work.Task))
                    {
                        tails.Remove(accountEvent.AccountId);
                    }
                }

                TryCompleteIfDone();
            }
        }

        async Task ProduceAsync()
        {
            try
            {
                await using var enumerator = source.GetAsyncEnumerator(operationToken);
                while (true)
                {
                    await unstarted.WaitAsync(operationToken).ConfigureAwait(false);

                    bool hasItem;
                    try
                    {
                        hasItem = await enumerator.MoveNextAsync().ConfigureAwait(false);
                    }
                    catch
                    {
                        unstarted.Release();
                        throw;
                    }

                    if (!hasItem)
                    {
                        unstarted.Release();
                        break;
                    }

                    var accountEvent = enumerator.Current;
                    Task<Result<decimal, AccountError>>? previous;
                    lock (stateGate)
                    {
                        tails.TryGetValue(accountEvent.AccountId, out previous);
                    }

                    var current = RunAfterAsync(
                        previous,
                        accountEvent,
                        processor,
                        unstarted,
                        workSlots,
                        output.Writer,
                        operationToken);
                    var work = new WorkEntry(current);
                    lock (stateGate)
                    {
                        tails[accountEvent.AccountId] = current;
                        active.Add(work);
                        work.Observer = ObserveAsync(work, accountEvent);
                    }
                }

                lock (stateGate)
                {
                    sourceCompleted = true;
                }

                TryCompleteIfDone();
            }
            catch (Exception exception)
            {
                if (!operationCancellation.IsCancellationRequested)
                {
                    CompleteWriter(exception);
                    operationCancellation.Cancel();
                }
                else if (!IsShutdownArtifact(exception))
                {
                    CompleteWriter(exception);
                }
                else
                {
                    CompleteWriter(null);
                }
            }
        }

        async Task DrainAsync()
        {
            while (true)
            {
                WorkEntry[] snapshot;
                lock (stateGate)
                {
                    snapshot = [.. active];
                }

                if (snapshot.Length == 0)
                {
                    break;
                }

                foreach (var work in snapshot)
                {
                    try
                    {
                        _ = await work.Task.ConfigureAwait(false);
                    }
                    catch (Exception exception) when (IsShutdownArtifact(exception))
                    {
                    }
                    catch (Exception)
                    {
                    }

                    try
                    {
                        await work.Observer.ConfigureAwait(false);
                    }
                    catch (Exception)
                    {
                    }
                }
            }

            unstarted.Dispose();
            workSlots.Dispose();
        }

        var producer = ProduceAsync();
        try
        {
            await foreach (var outcome in output.Reader.ReadAllAsync(operationToken).ConfigureAwait(false))
            {
                outcomes.Add(outcome);
                Volatile.Write(ref delivered, outcomes.Count);
                // The active slot covers an operation that has started but not yet been delivered.
                // Releasing it here, after the delivery is observed, keeps the started-but-not-
                // delivered count at or below maxConcurrency.
                workSlots.Release();
            }

            await producer.ConfigureAwait(false);
        }
        catch
        {
            operationCancellation.Cancel();
            CompleteWriter(null);
            try
            {
                await producer.ConfigureAwait(false);
            }
            catch (Exception)
            {
            }

            await DrainAsync().ConfigureAwait(false);
            throw;
        }

        await DrainAsync().ConfigureAwait(false);

        return new KeyedConsumerReport(
            outcomes,
            processor.Snapshot(),
            processor.MaxInFlight,
            processor.PerKeyOverlapViolations,
            source.MaxRetained,
            source.ItemsYielded,
            processor.SnapshotTransitions());
    }

    private static async Task<Result<decimal, AccountError>> RunAfterAsync(
        Task<Result<decimal, AccountError>>? previous,
        AccountEvent accountEvent,
        AccountProcessor processor,
        SemaphoreSlim unstarted,
        SemaphoreSlim workSlots,
        ChannelWriter<KeyedOutcome> writer,
        CancellationToken cancellationToken)
    {
        var started = false;
        try
        {
            if (previous is not null)
            {
                _ = await previous.ConfigureAwait(false);
            }

            cancellationToken.ThrowIfCancellationRequested();
            await workSlots.WaitAsync(cancellationToken).ConfigureAwait(false);

            // The item leaves the pending buffer exactly when its operation starts, matching
            // the FunnySharp coordinator's maxBufferedItems admission split.
            unstarted.Release();
            started = true;

            // The active slot stays held until the consumer observes the delivery. It is released
            // by the consumer loop, not here, so `maxConcurrency` bounds started-but-not-delivered
            // operations exactly as the FunnySharp coordinator does.
            var outcome = await processor.ProcessAsync(accountEvent, cancellationToken).ConfigureAwait(false);

            _ = outcome.IsSuccess;

            // Delivery is part of the chain so a later item for the same key cannot publish
            // before its predecessor's outcome has been written.
            await writer.WriteAsync(
                    new KeyedOutcome(accountEvent.AccountId, accountEvent, outcome),
                    cancellationToken)
                .ConfigureAwait(false);
            return outcome;
        }
        finally
        {
            if (!started)
            {
                unstarted.Release();
            }
        }
    }

    private sealed class WorkEntry(Task<Result<decimal, AccountError>> task)
    {
        internal Task<Result<decimal, AccountError>> Task { get; } = task;

        internal Task Observer { get; set; } = System.Threading.Tasks.Task.CompletedTask;
    }
}
