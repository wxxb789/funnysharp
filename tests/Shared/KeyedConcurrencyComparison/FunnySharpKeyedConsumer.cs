namespace FunnySharp.TestSupport.KeyedConcurrencyComparison;

internal static class FunnySharpKeyedConsumer
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

        await foreach (var outcome in source
            .SelectParallelByKeyCompletionOrderValueAsync(
                static accountEvent => accountEvent.AccountId,
                maxConcurrency,
                maxBufferedItems,
                processor.ProcessAsync,
                StringComparer.Ordinal)
            .WithCancellation(cancellationToken)
            .ConfigureAwait(false))
        {
            outcomes.Add(new KeyedOutcome(outcome.Key, outcome.Source, outcome.Outcome));
            Volatile.Write(ref delivered, outcomes.Count);
        }

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
