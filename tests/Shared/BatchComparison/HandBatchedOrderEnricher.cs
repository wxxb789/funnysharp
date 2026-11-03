using FunnySharp;

namespace FunnySharp.TestSupport.Batching;

internal static class HandBatchedOrderEnricher
{
    internal static async Task<IReadOnlyList<Result<EnrichedOrder, OrderFailure>>> EnrichAsync(
        IReadOnlyList<Order> orders,
        DeterministicOrderSources sources,
        CancellationToken cancellationToken)
    {
        var customerKeys = DistinctInOrder(orders.Select(order => order.CustomerKey));
        var preferenceKeys = DistinctInOrder(orders.Select(order => order.CustomerKey));

        var customerResponse = await sources.FetchCustomersAsync(customerKeys, cancellationToken);
        var preferenceResponse = await sources.FetchPreferencesAsync(preferenceKeys, cancellationToken);

        var customers = Interpret(customerKeys, customerResponse);
        var preferences = Interpret(preferenceKeys, preferenceResponse);

        var regionKeys = new List<string>();
        var seenRegions = new HashSet<string>(StringComparer.Ordinal);
        foreach (var order in orders)
        {
            if (customers[order.CustomerKey].TryGetValue(out var customer)
                && preferences[order.CustomerKey].TryGetValue(out _)
                && seenRegions.Add(customer.RegionKey))
            {
                regionKeys.Add(customer.RegionKey);
            }
        }

        var regions = regionKeys.Count == 0
            ? new Dictionary<string, Result<Region, OrderFailure>>(StringComparer.Ordinal)
            : Interpret(regionKeys, await sources.FetchRegionsAsync(regionKeys, cancellationToken));

        var results = new Result<EnrichedOrder, OrderFailure>[orders.Count];
        for (var index = 0; index < orders.Count; index++)
        {
            var order = orders[index];
            var customer = customers[order.CustomerKey];
            var preference = preferences[order.CustomerKey];

            if (!customer.TryGetValue(out var customerValue))
            {
                customer.TryGetError(out var customerError);
                results[index] = Result<EnrichedOrder, OrderFailure>.Failure(customerError!);
                continue;
            }

            if (!preference.TryGetValue(out var preferenceValue))
            {
                preference.TryGetError(out var preferenceError);
                results[index] = Result<EnrichedOrder, OrderFailure>.Failure(preferenceError!);
                continue;
            }

            var region = regions[customerValue.RegionKey];
            if (!region.TryGetValue(out var regionValue))
            {
                region.TryGetError(out var regionError);
                results[index] = Result<EnrichedOrder, OrderFailure>.Failure(regionError!);
                continue;
            }

            results[index] = Result<EnrichedOrder, OrderFailure>.Success(
                new EnrichedOrder(
                    order,
                    customerValue,
                    preferenceValue,
                    regionValue,
                    Total(order, preferenceValue, regionValue)));
        }

        return results;
    }

    private static Dictionary<string, Result<TValue, OrderFailure>> Interpret<TValue>(
        IReadOnlyList<string> keys,
        Result<IReadOnlyList<BatchSourceEntry<string, TValue, OrderFailure>>, OrderFailure> response)
    {
        var results = new Dictionary<string, Result<TValue, OrderFailure>>(StringComparer.Ordinal);

        if (response.IsFailure)
        {
            response.TryGetError(out var batchError);
            foreach (var key in keys)
            {
                results[key] = Result<TValue, OrderFailure>.Failure(batchError!);
            }

            return results;
        }

        if (!response.TryGetValue(out var entries) || entries is null)
        {
            return Unexpected<TValue>(keys, "the source returned a null entry collection.");
        }

        if (entries.Count > keys.Count)
        {
            return Unexpected<TValue>(keys, "the source returned more entries than requested.");
        }

        var requested = new HashSet<string>(keys, StringComparer.Ordinal);
        var byKey = new Dictionary<string, BatchSourceEntry<string, TValue, OrderFailure>>(
            StringComparer.Ordinal);

        foreach (var entry in entries)
        {
            if (entry.Kind == BatchSourceEntryKind.Unset)
            {
                return Unexpected<TValue>(keys, "the source returned an uninitialized entry.");
            }

            if (entry.Key is null)
            {
                return Unexpected<TValue>(keys, "the source returned a null key.");
            }

            if (!requested.Contains(entry.Key))
            {
                return Unexpected<TValue>(keys, "the source returned a key that was not requested.");
            }

            if (!byKey.TryAdd(entry.Key, entry))
            {
                return Unexpected<TValue>(keys, "the source returned a key more than once.");
            }
        }

        foreach (var key in keys)
        {
            if (!byKey.TryGetValue(key, out var entry))
            {
                results[key] = Result<TValue, OrderFailure>.Failure(OrderFailure.MissingRecord);
            }
            else if (entry.TryGetValue(out var value))
            {
                results[key] = Result<TValue, OrderFailure>.Success(value);
            }
            else if (entry.TryGetError(out var keyError))
            {
                results[key] = Result<TValue, OrderFailure>.Failure(keyError);
            }
            else
            {
                return Unexpected<TValue>(keys, "the source returned an unexpected entry kind.");
            }
        }

        return results;
    }

    private static Dictionary<string, Result<TValue, OrderFailure>> Unexpected<TValue>(
        IReadOnlyList<string> keys,
        string detail)
    {
        var results = new Dictionary<string, Result<TValue, OrderFailure>>(StringComparer.Ordinal);
        var error = OrderFailure.UnexpectedSourceResponse(detail);
        foreach (var key in keys)
        {
            results[key] = Result<TValue, OrderFailure>.Failure(error);
        }

        return results;
    }

    private static IReadOnlyList<string> DistinctInOrder(IEnumerable<string> keys)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<string>();
        foreach (var key in keys)
        {
            if (seen.Add(key))
            {
                result.Add(key);
            }
        }

        return result;
    }

    private static decimal Total(Order order, Preference preference, Region region) =>
        decimal.Round(
            (order.UnitPrice * order.Units * (1m + region.TaxRate))
                + (preference.GiftWrap ? 2m : 0m),
            2,
            MidpointRounding.AwayFromZero);
}
