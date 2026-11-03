using FunnySharp;

namespace FunnySharp.TestSupport.Batching;

public enum BatchUnexpectedShape
{
    None = 0,
    Duplicate = 1,
    Unknown = 2,
    Unset = 3,
    NullList = 4,
    TooMany = 5,
}

internal sealed record BatchDataset(
    IReadOnlyDictionary<string, Customer> Customers,
    IReadOnlyDictionary<string, Preference> Preferences,
    IReadOnlyDictionary<string, Region> Regions,
    IReadOnlySet<string> CustomerFailures,
    IReadOnlySet<string> PreferenceFailures,
    IReadOnlySet<string> RegionFailures,
    bool FailCustomerBatch = false,
    bool FailPreferenceBatch = false,
    BatchUnexpectedShape CustomerUnexpected = BatchUnexpectedShape.None)
{
    internal static BatchDataset CreateDefault() => new(
        new Dictionary<string, Customer>(StringComparer.Ordinal)
        {
            ["c1"] = new Customer("c1", "Ada", "r1"),
            ["c2"] = new Customer("c2", "Grace", "r2"),
            ["c3"] = new Customer("c3", "Linus", "r1"),
        },
        new Dictionary<string, Preference>(StringComparer.Ordinal)
        {
            ["c1"] = new Preference("c1", "Standard", false),
            ["c2"] = new Preference("c2", "Gift", true),
            ["c3"] = new Preference("c3", "Compact", false),
        },
        new Dictionary<string, Region>(StringComparer.Ordinal)
        {
            ["r1"] = new Region("r1", "North", 0.10m),
            ["r2"] = new Region("r2", "South", 0.20m),
        },
        new HashSet<string>(StringComparer.Ordinal),
        new HashSet<string>(StringComparer.Ordinal),
        new HashSet<string>(StringComparer.Ordinal));

    internal BatchDataset WithoutCustomer(string key) => this with
    {
        Customers = CopyWithout(Customers, key),
    };

    internal BatchDataset WithoutRegion(string key) => this with
    {
        Regions = CopyWithout(Regions, key),
    };

    internal BatchDataset WithCustomerFailure(string key) => this with
    {
        CustomerFailures = CopyWith(CustomerFailures, key),
    };

    internal BatchDataset WithPreferenceFailure(string key) => this with
    {
        PreferenceFailures = CopyWith(PreferenceFailures, key),
    };

    internal BatchDataset WithRegionFailure(string key) => this with
    {
        RegionFailures = CopyWith(RegionFailures, key),
    };

    private static IReadOnlyDictionary<string, TValue> CopyWithout<TValue>(
        IReadOnlyDictionary<string, TValue> source,
        string key)
    {
        return source
            .Where(pair => pair.Key != key)
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
    }

    private static IReadOnlySet<string> CopyWith(IReadOnlySet<string> source, string key)
    {
        var copy = new HashSet<string>(source, StringComparer.Ordinal);
        copy.Add(key);
        return copy;
    }
}

internal sealed record BatchInvocation(string Source, IReadOnlyList<string> Keys);

internal sealed class BatchInvocationLog
{
    private readonly object _gate = new();
    private readonly List<BatchInvocation> _invocations = new();

    internal void Add(string source, IReadOnlyList<string> keys)
    {
        lock (_gate)
        {
            _invocations.Add(new BatchInvocation(source, keys.ToArray()));
        }
    }

    internal IReadOnlyList<BatchInvocation> Invocations
    {
        get
        {
            lock (_gate)
            {
                return _invocations.ToArray();
            }
        }
    }

    internal int CountFor(string source)
    {
        lock (_gate)
        {
            return _invocations.Count(invocation => invocation.Source == source);
        }
    }

    internal IReadOnlyList<IReadOnlyList<string>> BatchesFor(string source)
    {
        lock (_gate)
        {
            return _invocations
                .Where(invocation => invocation.Source == source)
                .Select(invocation => invocation.Keys)
                .ToArray();
        }
    }
}

internal sealed class DeterministicOrderSources
{
    private readonly BatchDataset _dataset;
    private readonly BatchInvocationLog? _log;

    internal DeterministicOrderSources(
        BatchDataset dataset,
        BatchInvocationLog? log)
    {
        const string partition = "tenant-a";
        _dataset = dataset;
        _log = log;
        Customers = new BatchSource<string, Customer, OrderFailure>(
            "customers",
            partition,
            StringComparer.Ordinal,
            FetchCustomersAsync);
        Preferences = new BatchSource<string, Preference, OrderFailure>(
            "preferences",
            partition,
            StringComparer.Ordinal,
            FetchPreferencesAsync);
        Regions = new BatchSource<string, Region, OrderFailure>(
            "regions",
            partition,
            StringComparer.Ordinal,
            FetchRegionsAsync);
    }

    internal BatchSource<string, Customer, OrderFailure> Customers { get; }

    internal BatchSource<string, Preference, OrderFailure> Preferences { get; }

    internal BatchSource<string, Region, OrderFailure> Regions { get; }

    internal ValueTask<Result<IReadOnlyList<BatchSourceEntry<string, Customer, OrderFailure>>, OrderFailure>>
        FetchCustomersAsync(IReadOnlyList<string> keys, CancellationToken cancellationToken) =>
        FetchAsync(
            "customers",
            keys,
            _dataset.Customers,
            _dataset.CustomerFailures,
            _dataset.FailCustomerBatch,
            _dataset.CustomerUnexpected,
            cancellationToken);

    internal ValueTask<Result<IReadOnlyList<BatchSourceEntry<string, Preference, OrderFailure>>, OrderFailure>>
        FetchPreferencesAsync(IReadOnlyList<string> keys, CancellationToken cancellationToken) =>
        FetchAsync(
            "preferences",
            keys,
            _dataset.Preferences,
            _dataset.PreferenceFailures,
            _dataset.FailPreferenceBatch,
            BatchUnexpectedShape.None,
            cancellationToken);

    internal ValueTask<Result<IReadOnlyList<BatchSourceEntry<string, Region, OrderFailure>>, OrderFailure>>
        FetchRegionsAsync(IReadOnlyList<string> keys, CancellationToken cancellationToken) =>
        FetchAsync(
            "regions",
            keys,
            _dataset.Regions,
            _dataset.RegionFailures,
            failBatch: false,
            BatchUnexpectedShape.None,
            cancellationToken);

    private ValueTask<Result<IReadOnlyList<BatchSourceEntry<string, TValue, OrderFailure>>, OrderFailure>>
        FetchAsync<TValue>(
            string source,
            IReadOnlyList<string> keys,
            IReadOnlyDictionary<string, TValue> values,
            IReadOnlySet<string> failures,
            bool failBatch,
            BatchUnexpectedShape unexpected,
            CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _log?.Add(source, keys);

        if (failBatch)
        {
            return new(Result<IReadOnlyList<BatchSourceEntry<string, TValue, OrderFailure>>, OrderFailure>.Failure(
                OrderFailure.BatchFailure(source)));
        }

        if (unexpected == BatchUnexpectedShape.NullList)
        {
            return new(Result<IReadOnlyList<BatchSourceEntry<string, TValue, OrderFailure>>, OrderFailure>.Success(
                null!));
        }

        if (unexpected == BatchUnexpectedShape.Unset)
        {
            return new(Result<IReadOnlyList<BatchSourceEntry<string, TValue, OrderFailure>>, OrderFailure>.Success(
            [
                default(BatchSourceEntry<string, TValue, OrderFailure>),
            ]));
        }

        if (unexpected == BatchUnexpectedShape.Unknown)
        {
            return new(Result<IReadOnlyList<BatchSourceEntry<string, TValue, OrderFailure>>, OrderFailure>.Success(
            [
                BatchSourceEntry<string, TValue, OrderFailure>.Found(
                    "not-requested",
                    values.Count > 0 ? values.Values.First() : default!),
            ]));
        }

        var entries = new List<BatchSourceEntry<string, TValue, OrderFailure>>();
        foreach (var key in keys)
        {
            if (failures.Contains(key))
            {
                entries.Add(BatchSourceEntry<string, TValue, OrderFailure>.Failed(
                    key,
                    OrderFailure.SourceFailure(source, key)));
            }
            else if (values.TryGetValue(key, out var value))
            {
                entries.Add(BatchSourceEntry<string, TValue, OrderFailure>.Found(key, value));
            }
        }

        if (unexpected == BatchUnexpectedShape.Duplicate && entries.Count > 0)
        {
            entries.Add(entries[0]);
        }
        else if (unexpected == BatchUnexpectedShape.TooMany && entries.Count > 0)
        {
            do
            {
                entries.Add(entries[0]);
            }
            while (entries.Count <= keys.Count);
        }

        return new(Result<IReadOnlyList<BatchSourceEntry<string, TValue, OrderFailure>>, OrderFailure>.Success(
            entries));
    }
}

internal static class BatchOrderFixtures
{
    internal static IReadOnlyList<Order> Orders { get; } =
    [
        new Order("O1", "c1", 2, 10m),
        new Order("O2", "c1", 1, 12m),
        new Order("O3", "c2", 3, 8m),
        new Order("O4", "c3", 1, 20m),
        new Order("O5", "c2", 2, 11m),
        new Order("O6", "c1", 4, 5m),
        new Order("O7", "c3", 2, 9m),
        new Order("O8", "c2", 1, 15m),
    ];
}
