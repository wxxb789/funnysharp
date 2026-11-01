using System.Globalization;
using FunnySharp;
using FunnySharp.TestSupport;
using FunnySharp.TestSupport.Batching;

internal static class BatchMeasurements
{
    private const int OrderCount = 200;
    private const int CustomerCount = 64;
    private const int RegionCount = 16;

    internal static async Task RunAsync()
    {
        var dataset = BatchMeasurementFixtures.CreateDataset(CustomerCount, RegionCount);
        var orders = BatchMeasurementFixtures.CreateOrders(OrderCount, CustomerCount);
        var sources = new DeterministicOrderSources(dataset, null);

        var planConstruction = MeasurementHarness.MeasureTotal(
            () =>
            {
                _ = FunnySharpOrderEnricher.BuildPlan(orders, sources);
                return 1;
            },
            iterations: 200,
            warmupIterations: 20);

        var prebuiltPlan = FunnySharpOrderEnricher.BuildPlan(orders, sources);
        var prebuiltExecution = await MeasurementHarness.MeasureAsync(
            async () =>
            {
                await using var scope = BatchScope.Create();
                var results = await FunnySharpOrderEnricher.ExecuteAsync(
                    prebuiltPlan,
                    scope,
                    CancellationToken.None);
                return results.Count;
            },
            iterations: 200,
            warmupIterations: 20);

        var funnyCold = await MeasurementHarness.MeasureAsync(
            async () =>
            {
                await using var scope = BatchScope.Create();
                var results = await FunnySharpOrderEnricher.EnrichAsync(
                    orders,
                    sources,
                    scope,
                    CancellationToken.None);
                return results.Count;
            },
            iterations: 200,
            warmupIterations: 20);

        var handBatched = await MeasurementHarness.MeasureAsync(
            async () =>
            {
                var results = await HandBatchedOrderEnricher.EnrichAsync(
                    orders,
                    sources,
                    CancellationToken.None);
                return results.Count;
            },
            iterations: 200,
            warmupIterations: 20);

        await using var warmScope = BatchScope.Create();
        _ = await FunnySharpOrderEnricher.EnrichAsync(
            orders,
            sources,
            warmScope,
            CancellationToken.None);
        var funnyWarm = await MeasurementHarness.MeasureAsync(
            async () =>
            {
                var results = await FunnySharpOrderEnricher.EnrichAsync(
                    orders,
                    sources,
                    warmScope,
                    CancellationToken.None);
                return results.Count;
            },
            iterations: 200,
            warmupIterations: 20);
        var prebuiltWarm = await MeasurementHarness.MeasureAsync(
            async () =>
            {
                var results = await FunnySharpOrderEnricher.ExecuteAsync(
                    prebuiltPlan,
                    warmScope,
                    CancellationToken.None);
                return results.Count;
            },
            iterations: 200,
            warmupIterations: 20);

        var coldLog = new BatchInvocationLog();
        var coldSources = new DeterministicOrderSources(dataset, coldLog);
        await using (var scope = BatchScope.Create())
        {
            _ = await FunnySharpOrderEnricher.EnrichAsync(
                orders,
                coldSources,
                scope,
                CancellationToken.None);
        }

        var handLog = new BatchInvocationLog();
        var handSources = new DeterministicOrderSources(dataset, handLog);
        _ = await HandBatchedOrderEnricher.EnrichAsync(
            orders,
            handSources,
            CancellationToken.None);

        Console.WriteLine(
            string.Create(
                CultureInfo.InvariantCulture,
                $"Orders: {OrderCount}; distinct customers: {CustomerCount}; distinct regions: {RegionCount}"));
        Console.WriteLine("Operation | Source calls | Allocated B/op | Time ns/op");
        WriteRow("Plan construction only (200 orders)", 0, planConstruction);
        WriteRow("Prebuilt plan (new scope per execution)", coldLog.Invocations.Count, prebuiltExecution);
        WriteRow("FunnySharp consumer (new scope per execution)", coldLog.Invocations.Count, funnyCold);
        WriteRow("Hand-batched BCL consumer", handLog.Invocations.Count, handBatched);
        WriteRow("Prebuilt plan (scope cache warm)", 0, prebuiltWarm);
        WriteRow("FunnySharp consumer (scope cache warm)", 0, funnyWarm);
    }

    private static void WriteRow(string name, int sourceCalls, Measurement measurement)
    {
        Console.WriteLine(
            string.Create(
                CultureInfo.InvariantCulture,
                $"{name} | {sourceCalls} | {MeasurementHarness.Format(measurement.BytesPerOperation)} | {MeasurementHarness.Format(measurement.NanosecondsPerOperation)}"));
    }

}
