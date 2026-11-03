using FunnySharp;

namespace FunnySharp.TestSupport.Batching;

internal static class BatchComparison
{
    internal static async Task VerifyAsync()
    {
        var defaultDataset = BatchDataset.CreateDefault();
        var orders = BatchOrderFixtures.Orders;

        await VerifyScenarioAsync("happy path with repeated keys", orders, defaultDataset);
        await VerifyScenarioAsync("missing customer", orders, defaultDataset.WithoutCustomer("c3"));
        await VerifyScenarioAsync("per-key customer failure", orders, defaultDataset.WithCustomerFailure("c2"));
        await VerifyScenarioAsync("per-key preference failure", orders, defaultDataset.WithPreferenceFailure("c3"));
        await VerifyScenarioAsync("missing region", orders, defaultDataset.WithoutRegion("r2"));
        await VerifyScenarioAsync("per-key region failure", orders, defaultDataset.WithRegionFailure("r2"));
        await VerifyScenarioAsync("whole customer batch failure", orders, defaultDataset with { FailCustomerBatch = true });
        await VerifyScenarioAsync("whole preference batch failure", orders, defaultDataset with { FailPreferenceBatch = true });
        await VerifyScenarioAsync("unexpected duplicate customer response", orders, defaultDataset with
        {
            CustomerUnexpected = BatchUnexpectedShape.Duplicate,
        });
        await VerifyScenarioAsync("unexpected null customer response", orders, defaultDataset with
        {
            CustomerUnexpected = BatchUnexpectedShape.NullList,
        });

        await VerifyFewerCallsThanPerItemAsync(orders, defaultDataset);
    }

    internal static async Task VerifyFewerCallsThanPerItemAsync(
        IReadOnlyList<Order> orders,
        BatchDataset dataset)
    {
        var log = new BatchInvocationLog();
        var sources = new DeterministicOrderSources(dataset, log);
        _ = await FunnySharpOrderEnricher.EnrichAsync(orders, sources, CancellationToken.None);

        var perItemCalls = orders.Count * 3;
        if (log.Invocations.Count >= perItemCalls)
        {
            throw new InvalidOperationException(
                $"Expected fewer source calls than the per-item path ({perItemCalls}), but observed {log.Invocations.Count}.");
        }

        if (log.CountFor("customers") != 1
            || log.CountFor("preferences") != 1
            || log.CountFor("regions") != 1)
        {
            throw new InvalidOperationException("The expected one batch per source was not observed.");
        }

        if (!log.BatchesFor("customers")[0].SequenceEqual(["c1", "c2", "c3"], StringComparer.Ordinal))
        {
            throw new InvalidOperationException("Customer keys were not deduplicated in first-request order.");
        }
    }

    private static async Task VerifyScenarioAsync(
        string name,
        IReadOnlyList<Order> orders,
        BatchDataset dataset)
    {
        var funnyLog = new BatchInvocationLog();
        var handLog = new BatchInvocationLog();
        var funnySources = new DeterministicOrderSources(dataset, funnyLog);
        var handSources = new DeterministicOrderSources(dataset, handLog);

        var funny = await FunnySharpOrderEnricher.EnrichAsync(
            orders,
            funnySources,
            CancellationToken.None);
        var hand = await HandBatchedOrderEnricher.EnrichAsync(
            orders,
            handSources,
            CancellationToken.None);

        if (funny.Count != hand.Count)
        {
            throw new InvalidOperationException($"{name}: outcome counts differ.");
        }

        for (var index = 0; index < funny.Count; index++)
        {
            if (!SameOutcome(funny[index], hand[index]))
            {
                throw new InvalidOperationException(
                    $"{name}: order '{orders[index].OrderId}' differed between consumers.");
            }
        }

        CompareInvocations(name, funnyLog.Invocations, handLog.Invocations);
    }

    private static bool SameOutcome(
        Result<EnrichedOrder, OrderFailure> funny,
        Result<EnrichedOrder, OrderFailure> hand)
    {
        if (funny.TryGetValue(out var funnyValue))
        {
            return hand.TryGetValue(out var handValue) && funnyValue == handValue;
        }

        return funny.TryGetError(out var funnyError)
            && hand.TryGetError(out var handError)
            && funnyError == handError;
    }

    private static void CompareInvocations(
        string name,
        IReadOnlyList<BatchInvocation> funny,
        IReadOnlyList<BatchInvocation> hand)
    {
        if (funny.Count != hand.Count)
        {
            throw new InvalidOperationException(
                $"{name}: invocation counts differ ({funny.Count} vs {hand.Count}).");
        }

        for (var index = 0; index < funny.Count; index++)
        {
            if (funny[index].Source != hand[index].Source
                || !funny[index].Keys.SequenceEqual(hand[index].Keys, StringComparer.Ordinal))
            {
                throw new InvalidOperationException(
                    $"{name}: invocation {index} differed between consumers.");
            }
        }
    }
}
