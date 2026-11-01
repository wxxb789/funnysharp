using FunnySharp.DataLoaderComparison;
using FunnySharp.TestSupport;
using FunnySharp.TestSupport.Batching;

await VerifyAsync();
await MeasureAsync();

static async Task VerifyAsync()
{
    var defaultDataset = BatchDataset.CreateDefault();
    var orders = BatchOrderFixtures.Orders;

    await VerifyScenarioAsync("happy path", orders, defaultDataset);
    await VerifyScenarioAsync("missing customer", orders, defaultDataset.WithoutCustomer("c3"));
    await VerifyScenarioAsync("per-key customer failure", orders, defaultDataset.WithCustomerFailure("c2"));
    await VerifyScenarioAsync("per-key preference failure", orders, defaultDataset.WithPreferenceFailure("c3"));
    await VerifyScenarioAsync("missing region", orders, defaultDataset.WithoutRegion("r2"));
    await VerifyScenarioAsync("whole customer batch failure", orders, defaultDataset with { FailCustomerBatch = true });
}

static async Task VerifyScenarioAsync(
    string name,
    IReadOnlyList<Order> orders,
    BatchDataset dataset)
{
    var funnyLog = new BatchInvocationLog();
    var handLog = new BatchInvocationLog();
    var greenLog = new BatchInvocationLog();

    var funny = await FunnySharpOrderEnricher.EnrichAsync(
        orders,
        new DeterministicOrderSources(dataset, funnyLog),
        CancellationToken.None);
    var hand = await HandBatchedOrderEnricher.EnrichAsync(
        orders,
        new DeterministicOrderSources(dataset, handLog),
        CancellationToken.None);
    var green = await GreenDonutOrderEnricher.EnrichAsync(
        orders,
        new DeterministicOrderSources(dataset, greenLog),
        CancellationToken.None);

    for (var index = 0; index < orders.Count; index++)
    {
        if (funny[index] != hand[index])
        {
            throw new InvalidOperationException(
                $"{name}: FunnySharp and hand-batched consumers disagreed at {orders[index].OrderId}.");
        }

        if (funny[index] != green[index])
        {
            throw new InvalidOperationException(
                $"{name}: FunnySharp and GreenDonut consumers disagreed at {orders[index].OrderId}.");
        }
    }

    Console.WriteLine(
        $"{name}: funny={funnyLog.Invocations.Count} hand={handLog.Invocations.Count} green={greenLog.Invocations.Count}");
}

static async Task MeasureAsync()
{
    var dataset = BatchMeasurementFixtures.CreateDataset(customerCount: 64, regionCount: 16);
    var orders = BatchMeasurementFixtures.CreateOrders(orderCount: 200, customerCount: 64);
    var sources = new DeterministicOrderSources(dataset, null);

    var funnyCold = await MeasurementHarness.MeasureAsync(
        async () =>
        {
            await using var scope = FunnySharp.BatchScope.Create();
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

    var greenDonut = await MeasurementHarness.MeasureAsync(
        async () =>
        {
            var results = await GreenDonutOrderEnricher.EnrichAsync(
                orders,
                sources,
                CancellationToken.None);
            return results.Count;
        },
        iterations: 200,
        warmupIterations: 20);

    Console.WriteLine("Operation | Allocated B/op | Time ns/op");
    WriteRow("FunnySharp consumer (new scope per execution)", funnyCold);
    WriteRow("Hand-batched BCL consumer", handBatched);
    WriteRow("GreenDonut consumer (new loaders per execution)", greenDonut);
}

static void WriteRow(string name, Measurement measurement)
{
    Console.WriteLine(
        $"{name} | {MeasurementHarness.Format(measurement.BytesPerOperation)} | {MeasurementHarness.Format(measurement.NanosecondsPerOperation)}");
}


