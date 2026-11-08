using System.Collections.Concurrent;
using System.Collections.Generic;

namespace FunnySharp.TestSupport.KeyedConcurrencyComparison;

/// <summary>
/// An existing-facility baseline. It serializes each key with a semaphore, but
/// <c>Parallel.ForEachAsync</c> dispatches source items to workers without a per-key start-order
/// guarantee. It is reported as a facility comparison, not as an equivalent implementation of the
/// FunnySharp contract.
/// </summary>
internal static class ParallelFacilityKeyedConsumer
{
    internal static async Task<KeyedConsumerReport> RunAsync(
        IReadOnlyList<AccountEvent> events,
        int maxConcurrency,
        CancellationToken cancellationToken = default)
    {
        var processor = new AccountProcessor();
        var outcomes = new List<KeyedOutcome>(events.Count);
        var delivered = 0;
        var source = new CountingAsyncSource<AccountEvent>(
            events,
            () => Volatile.Read(ref delivered));
        var gates = new ConcurrentDictionary<string, SemaphoreSlim>(StringComparer.Ordinal);
        var outcomesGate = new object();

        await Parallel.ForEachAsync(
            source,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = maxConcurrency,
                CancellationToken = cancellationToken,
            },
            async (accountEvent, token) =>
            {
                var keyGate = gates.GetOrAdd(
                    accountEvent.AccountId,
                    static _ => new SemaphoreSlim(1, 1));
                await keyGate.WaitAsync(token).ConfigureAwait(false);
                try
                {
                    var outcome = await processor.ProcessAsync(accountEvent, token).ConfigureAwait(false);
                    lock (outcomesGate)
                    {
                        outcomes.Add(new KeyedOutcome(accountEvent.AccountId, accountEvent, outcome));
                        Volatile.Write(ref delivered, outcomes.Count);
                    }
                }
                finally
                {
                    keyGate.Release();
                }
            }).ConfigureAwait(false);

        return new KeyedConsumerReport(
            outcomes,
            processor.Snapshot(),
            processor.MaxInFlight,
            processor.PerKeyOverlapViolations,
            source.MaxRetained,
            source.ItemsYielded,
            processor.SnapshotTransitions());
    }
}
