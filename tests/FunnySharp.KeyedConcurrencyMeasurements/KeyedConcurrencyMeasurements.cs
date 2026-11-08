using System.Globalization;
using FunnySharp.TestSupport.KeyedConcurrencyComparison;

namespace FunnySharp.TestSupport;

internal static class KeyedConcurrencyMeasurements
{
    private const int MaxConcurrency = 4;
    private const int MaxBufferedItems = 8;
    private const int FixedIterations = 40;
    private const int SkewedIterations = 12;

    internal static void Run()
    {
        var fixedEvents = AccountScript.FixedEvents;
        var skewedEvents = AccountScript.CreateSkewed(hotEvents: 120, coldAccounts: 16, coldEvents: 4);
        var interleavedEvents = AccountScript.CreateInterleavedSkew(
            hotEvents: 120,
            coldAccounts: 16,
            coldEvents: 4);

        VerifyFixedOutcomes(fixedEvents);
        VerifySkewedOutcomes(skewedEvents, "skewed");
        VerifySkewedOutcomes(interleavedEvents, "interleaved");

        Console.WriteLine("Keyed concurrency comparison (one allocation counter, TieredCompilation=false)");
        Console.WriteLine(
            $"Fixed script: {fixedEvents.Count} events, maxConcurrency={MaxConcurrency}, maxBufferedItems={MaxBufferedItems}");
        Console.WriteLine(
            $"Skewed script: {skewedEvents.Count} events (hot key first), maxConcurrency={MaxConcurrency}, maxBufferedItems={MaxBufferedItems}");
        Console.WriteLine(
            $"Interleaved skew: {interleavedEvents.Count} events, maxConcurrency={MaxConcurrency}, maxBufferedItems={MaxBufferedItems}");
        Console.WriteLine();

        MeasureRunner("Fixed | FunnySharp", FixedIterations, () => RunFunnySharp(fixedEvents));
        MeasureRunner("Fixed | Competent BCL Task/Channel", FixedIterations, () => RunBcl(fixedEvents));
        MeasureRunner("Fixed | Parallel.ForEachAsync facility", FixedIterations, () => RunFacility(fixedEvents));
        MeasureRunner("Skewed | FunnySharp", SkewedIterations, () => RunFunnySharp(skewedEvents));
        MeasureRunner("Skewed | Competent BCL Task/Channel", SkewedIterations, () => RunBcl(skewedEvents));
        MeasureRunner("Skewed | Parallel.ForEachAsync facility", SkewedIterations, () => RunFacility(skewedEvents));
        MeasureRunner("Interleaved | FunnySharp", SkewedIterations, () => RunFunnySharp(interleavedEvents));
        MeasureRunner("Interleaved | Competent BCL Task/Channel", SkewedIterations, () => RunBcl(interleavedEvents));
        MeasureRunner("Interleaved | Parallel.ForEachAsync facility", SkewedIterations, () => RunFacility(interleavedEvents));

        Console.WriteLine();
        Console.WriteLine("Scheduling evidence (single non-measured run per row):");
        PrintScheduling("Fixed | FunnySharp", FunnySharpKeyedConsumer.RunAsync(fixedEvents, MaxConcurrency, MaxBufferedItems).GetAwaiter().GetResult());
        PrintScheduling("Fixed | Competent BCL Task/Channel", BclKeyedConsumer.RunAsync(fixedEvents, MaxConcurrency, MaxBufferedItems).GetAwaiter().GetResult());
        PrintScheduling("Fixed | Parallel.ForEachAsync facility", ParallelFacilityKeyedConsumer.RunAsync(fixedEvents, MaxConcurrency).GetAwaiter().GetResult());
        PrintScheduling("Skewed | FunnySharp", FunnySharpKeyedConsumer.RunAsync(skewedEvents, MaxConcurrency, MaxBufferedItems).GetAwaiter().GetResult());
        PrintScheduling("Skewed | Competent BCL Task/Channel", BclKeyedConsumer.RunAsync(skewedEvents, MaxConcurrency, MaxBufferedItems).GetAwaiter().GetResult());
        PrintScheduling("Skewed | Parallel.ForEachAsync facility", ParallelFacilityKeyedConsumer.RunAsync(skewedEvents, MaxConcurrency).GetAwaiter().GetResult());
        PrintScheduling("Interleaved | FunnySharp", FunnySharpKeyedConsumer.RunAsync(interleavedEvents, MaxConcurrency, MaxBufferedItems).GetAwaiter().GetResult());
        PrintScheduling("Interleaved | Competent BCL Task/Channel", BclKeyedConsumer.RunAsync(interleavedEvents, MaxConcurrency, MaxBufferedItems).GetAwaiter().GetResult());
        PrintScheduling("Interleaved | Parallel.ForEachAsync facility", ParallelFacilityKeyedConsumer.RunAsync(interleavedEvents, MaxConcurrency).GetAwaiter().GetResult());
    }

    private static void MeasureRunner(string name, int iterations, Func<Task<int>> operation)
    {
        var measurement = MeasurementHarness
            .MeasureAsync(operation, iterations, warmupIterations: 2)
            .GetAwaiter()
            .GetResult();
        Console.WriteLine(
            string.Create(
                CultureInfo.InvariantCulture,
                $"{name,-44} {measurement.BytesPerOperation,10:N0} B/op {measurement.NanosecondsPerOperation,14:N0} ns/op"));
    }

    private static async Task<int> RunFunnySharp(IReadOnlyList<AccountEvent> events)
    {
        var report = await FunnySharpKeyedConsumer
            .RunAsync(events, MaxConcurrency, MaxBufferedItems)
            .ConfigureAwait(false);
        return Checksum(report);
    }

    private static async Task<int> RunBcl(IReadOnlyList<AccountEvent> events)
    {
        var report = await BclKeyedConsumer
            .RunAsync(events, MaxConcurrency, MaxBufferedItems)
            .ConfigureAwait(false);
        return Checksum(report);
    }

    private static async Task<int> RunFacility(IReadOnlyList<AccountEvent> events)
    {
        var report = await ParallelFacilityKeyedConsumer
            .RunAsync(events, MaxConcurrency)
            .ConfigureAwait(false);
        return Checksum(report);
    }

    private static int Checksum(KeyedConsumerReport report)
    {
        var checksum = report.Outcomes.Count;
        foreach (var outcome in report.Outcomes)
        {
            checksum = unchecked(
                checksum +
                (outcome.Event.AccountId.Length * 31) +
                (outcome.Event.Sequence * 7) +
                (outcome.Outcome.IsSuccess ? 1 : 0));
        }

        foreach (var balance in report.FinalBalances.Values)
        {
            checksum = unchecked((checksum * 31) + (int)balance);
        }

        return checksum;
    }

    private static void PrintScheduling(string name, KeyedConsumerReport report)
    {
        var deliveryOrder = HasPerKeyOrder(
            report.Outcomes
                .Select(static outcome => new KeyTransition(
                    outcome.AccountId,
                    outcome.Event.Sequence,
                    Started: false))
                .ToArray(),
            started: false);
        var startOrder = HasPerKeyOrder(report.Transitions, started: true);
        var completionOrder = HasPerKeyOrder(report.Transitions, started: false);
        Console.WriteLine(
            string.Create(
                CultureInfo.InvariantCulture,
                $"{name,-44} outcomes={report.Outcomes.Count,4} maxInFlight={report.MaxInFlight,2} overlapViolations={report.PerKeyOverlapViolations,2} maxRetained={report.MaxRetainedSourceItems,3} sourceItems={report.SourceItemsYielded,4} perKeyDeliveryOrder={(deliveryOrder ? "OK" : "VIOLATION")} perKeyStartOrder={(startOrder ? "OK" : "VIOLATION")} perKeyCompletionOrder={(completionOrder ? "OK" : "VIOLATION")}"));

        if (report.Outcomes.Count > 64)
        {
            foreach (var group in report.Transitions
                .GroupBy(static transition => transition.AccountId, StringComparer.Ordinal)
                .OrderBy(static group => group.Key, StringComparer.Ordinal))
            {
                var starts = group
                    .Where(static transition => transition.Started)
                    .Select(static transition => transition.Sequence);
                var completions = group
                    .Where(static transition => !transition.Started)
                    .Select(static transition => transition.Sequence);
                Console.WriteLine(
                    $"  {group.Key} starts=[{string.Join(",", starts)}] completions=[{string.Join(",", completions)}]");
            }
        }
    }

    private static bool HasPerKeyOrder(IReadOnlyList<KeyTransition> transitions, bool started) =>
        transitions
            .Where(transition => transition.Started == started)
            .GroupBy(static transition => transition.AccountId, StringComparer.Ordinal)
            .All(static group => group
                .Select(static transition => transition.Sequence)
                .SequenceEqual(Enumerable.Range(0, group.Count())));

    private static void VerifyFixedOutcomes(IReadOnlyList<AccountEvent> events)
    {
        VerifyEquivalentConsumers(events, "fixed");
        VerifyFacilityCoverage(events, "fixed");
    }

    private static void VerifySkewedOutcomes(IReadOnlyList<AccountEvent> events, string label)
    {
        VerifyEquivalentConsumers(events, label);
        VerifyFacilityCoverage(events, label);
    }

    private static void VerifyEquivalentConsumers(IReadOnlyList<AccountEvent> events, string label)
    {
        var funny = FunnySharpKeyedConsumer
            .RunAsync(events, MaxConcurrency, MaxBufferedItems)
            .GetAwaiter()
            .GetResult();
        // The coordinator releases its active slot just before the consumer's delivery counter
        // advances, so a source read can observe one extra retained item during that handoff.
        AssertReport(funny, events, label + " FunnySharp", sourceRetainedSlack: 1);

        var bcl = BclKeyedConsumer
            .RunAsync(events, MaxConcurrency, MaxBufferedItems)
            .GetAwaiter()
            .GetResult();
        // The baseline advances its delivery counter before releasing the active slot, so its
        // source-side view reaches the exact maxConcurrency + maxBufferedItems bound.
        AssertReport(bcl, events, label + " BCL", sourceRetainedSlack: 0);
        Require(Checksum(funny) == Checksum(bcl), label + " baseline checksum parity");
    }

    private static void VerifyFacilityCoverage(IReadOnlyList<AccountEvent> events, string label)
    {
        var facility = ParallelFacilityKeyedConsumer
            .RunAsync(events, MaxConcurrency)
            .GetAwaiter()
            .GetResult();
        Require(facility.Outcomes.Count == events.Count, label + " facility outcome count");
        Require(facility.PerKeyOverlapViolations == 0, label + " facility per-key overlap");
        Require(facility.MaxInFlight <= MaxConcurrency, label + " facility in-flight bound");

        var seen = new HashSet<(string AccountId, int Sequence)>();
        foreach (var outcome in facility.Outcomes)
        {
            Require(
                seen.Add((outcome.AccountId, outcome.Event.Sequence)),
                label + " facility duplicate outcome");
        }

        Require(seen.Count == events.Count, label + " facility event coverage");
    }

    private static void AssertReport(
        KeyedConsumerReport report,
        IReadOnlyList<AccountEvent> events,
        string description,
        int sourceRetainedSlack)
    {
        Require(report.Outcomes.Count == events.Count, description + ": outcome count");
        Require(report.SourceItemsYielded == events.Count, description + ": source items");
        Require(report.PerKeyOverlapViolations == 0, description + ": per-key overlap");
        Require(report.MaxInFlight <= MaxConcurrency, description + ": in-flight bound");
        Require(
            report.MaxRetainedSourceItems <= MaxConcurrency + MaxBufferedItems + sourceRetainedSlack,
            description + ": retained source bound (actual " + report.MaxRetainedSourceItems + ")");

        var sequentialBalances = new Dictionary<string, decimal>(StringComparer.Ordinal);
        var expected = new Dictionary<string, List<(AccountEvent Event, Result<decimal, AccountError> Outcome)>>(
            StringComparer.Ordinal);
        foreach (var accountEvent in events)
        {
            var outcome = AccountProcessor.ApplySequentially(sequentialBalances, accountEvent);
            if (!expected.TryGetValue(accountEvent.AccountId, out var outcomes))
            {
                outcomes = [];
                expected.Add(accountEvent.AccountId, outcomes);
            }

            outcomes.Add((accountEvent, outcome));
        }

        foreach (var pair in expected)
        {
            var actual = report.Outcomes
                .Where(outcome => string.Equals(outcome.AccountId, pair.Key, StringComparison.Ordinal))
                .ToArray();
            Require(actual.Length == pair.Value.Count, description + ": per-key count");
            for (var index = 0; index < actual.Length; index++)
            {
                Require(
                    actual[index].Event.Sequence == pair.Value[index].Event.Sequence,
                    description + ": source order");
                Require(
                    actual[index].Outcome.IsSuccess == pair.Value[index].Outcome.IsSuccess,
                    description + ": outcome success");

                if (!actual[index].Outcome.IsSuccess)
                {
                    _ = actual[index].Outcome.TryGetError(out var error);
                    _ = pair.Value[index].Outcome.TryGetError(out var expectedError);
                    Require(
                        string.Equals(error?.Code, expectedError?.Code, StringComparison.Ordinal),
                        description + ": error code");
                }
            }

            Require(
                report.FinalBalances[pair.Key] == sequentialBalances[pair.Key],
                description + ": final balance");
        }

        Require(HasPerKeyOrder(report.Transitions, started: true), description + ": per-key start order");
        Require(
            HasPerKeyOrder(report.Transitions, started: false),
            description + ": per-key completion order");
    }

    private static void Require(bool condition, string description)
    {
        if (!condition)
        {
            throw new InvalidOperationException(description);
        }
    }
}
