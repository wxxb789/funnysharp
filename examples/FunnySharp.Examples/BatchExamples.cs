using FunnySharp;

internal static class BatchExamples
{
    internal static async Task VerifyAsync()
    {
        var customerBatches = 0;
        var preferenceBatches = 0;
        var regionBatches = 0;

        var customers = new BatchSource<string, Customer, string>(
            "customers",
            "demo/tenant-a",
            StringComparer.Ordinal,
            (keys, _) =>
            {
                customerBatches++;
                return new(Result<IReadOnlyList<BatchSourceEntry<string, Customer, string>>, string>.Success(
                    FindEntries(keys, CustomerData)));
            });
        var preferences = new BatchSource<string, Preference, string>(
            "preferences",
            "demo/tenant-a",
            StringComparer.Ordinal,
            (keys, _) =>
            {
                preferenceBatches++;
                return new(Result<IReadOnlyList<BatchSourceEntry<string, Preference, string>>, string>.Success(
                    FindEntries(keys, PreferenceData)));
            });
        var regions = new BatchSource<string, Region, string>(
            "regions",
            "demo/tenant-a",
            StringComparer.Ordinal,
            (keys, _) =>
            {
                regionBatches++;
                return new(Result<IReadOnlyList<BatchSourceEntry<string, Region, string>>, string>.Success(
                    FindEntries(keys, RegionData)));
            });

        var orders = new[]
        {
            new Order("O1", "c1", 2, 10m),
            new Order("O2", "c1", 1, 12m),
            new Order("O3", "c2", 3, 8m),
        };

        var plan = BatchRequirement.All(orders.Select(order =>
            BatchRequirement
                .Read(customers, order.CustomerKey)
                .Zip(BatchRequirement.Read(preferences, order.CustomerKey))
                .Bind(pair => BatchRequirement
                    .Read(regions, pair.First.RegionKey)
                    .Map(region => new EnrichedOrder(
                        order,
                        pair.First,
                        pair.Second,
                        region,
                        Total(order, pair.Second, region))))));

        await using (var scope = BatchScope.Create())
        {
            var execution = await scope.ExecuteAsync(plan);
            ExampleAssertions.True(
                execution.TryGetValue(out var outcomes),
                "The independent order plans must execute.");

            ExampleAssertions.Equal(3, outcomes!.Count);
            ExampleAssertions.True(
                outcomes[0].TryGetValue(out var first),
                "The first order must be enriched.");
            ExampleAssertions.Equal("Ada", first!.Customer.Name);
            ExampleAssertions.Equal("North", first.Region.Name);
        }

        ExampleAssertions.Equal(1, customerBatches);
        ExampleAssertions.Equal(1, preferenceBatches);
        ExampleAssertions.Equal(1, regionBatches);

        var missing = new BatchSource<string, Customer, string>(
            "customers",
            "demo/tenant-a",
            StringComparer.Ordinal,
            static (keys, _) => new(Result<IReadOnlyList<BatchSourceEntry<string, Customer, string>>, string>.Success([])));

        await using (var scope = BatchScope.Create())
        {
            var execution = await scope.ExecuteAsync(BatchRequirement.Read(missing, "c9"));
            ExampleAssertions.True(
                execution.TryGetError(out var error),
                "A source omission must be a typed missing outcome.");
            ExampleAssertions.Equal(BatchReadErrorKind.Missing, error.Kind);
        }

        var rejected = new BatchSource<string, Customer, string>(
            "customers",
            "demo/tenant-a",
            StringComparer.Ordinal,
            static (keys, _) => new(Result<IReadOnlyList<BatchSourceEntry<string, Customer, string>>, string>.Success(
            [
                BatchSourceEntry<string, Customer, string>.Failed("c9", "customer is unavailable"),
            ])));

        await using (var scope = BatchScope.Create())
        {
            var execution = await scope.ExecuteAsync(BatchRequirement.Read(rejected, "c9"));
            ExampleAssertions.True(
                execution.TryGetError(out var error),
                "A per-key source failure must remain a typed outcome.");
            ExampleAssertions.Equal(BatchReadErrorKind.KeyFailure, error.Kind);
            ExampleAssertions.True(
                error.TryGetSourceError(out var sourceError),
                "The source error must be retained.");
            ExampleAssertions.Equal("customer is unavailable", sourceError!);
        }
    }

    private static BatchSourceEntry<string, TValue, string>[] FindEntries<TValue>(
        IReadOnlyList<string> keys,
        IReadOnlyDictionary<string, TValue> values)
    {
        var entries = new List<BatchSourceEntry<string, TValue, string>>();
        foreach (var key in keys)
        {
            if (values.TryGetValue(key, out var value))
            {
                entries.Add(BatchSourceEntry<string, TValue, string>.Found(key, value));
            }
        }

        return entries.ToArray();
    }

    private static decimal Total(Order order, Preference preference, Region region) =>
        decimal.Round(
            (order.UnitPrice * order.Units * (1m + region.TaxRate))
                + (preference.GiftWrap ? 2m : 0m),
            2,
            MidpointRounding.AwayFromZero);

    private static readonly Dictionary<string, Customer> CustomerData = new(StringComparer.Ordinal)
    {
        ["c1"] = new Customer("c1", "Ada", "r1"),
        ["c2"] = new Customer("c2", "Grace", "r2"),
    };

    private static readonly Dictionary<string, Preference> PreferenceData = new(StringComparer.Ordinal)
    {
        ["c1"] = new Preference("c1", "Standard", false),
        ["c2"] = new Preference("c2", "Gift", true),
    };

    private static readonly Dictionary<string, Region> RegionData = new(StringComparer.Ordinal)
    {
        ["r1"] = new Region("r1", "North", 0.10m),
        ["r2"] = new Region("r2", "South", 0.20m),
    };

    private sealed record Order(string OrderId, string CustomerKey, int Units, decimal UnitPrice);

    private sealed record Customer(string Key, string Name, string RegionKey);

    private sealed record Preference(string Key, string DisplayName, bool GiftWrap);

    private sealed record Region(string Key, string Name, decimal TaxRate);

    private sealed record EnrichedOrder(
        Order Order,
        Customer Customer,
        Preference Preference,
        Region Region,
        decimal Total);
}
