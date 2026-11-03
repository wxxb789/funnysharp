using FunnySharp;
using FunnySharp.TestSupport.Batching;
using GreenDonut;
using KeyNotFoundException = System.Collections.Generic.KeyNotFoundException;

namespace FunnySharp.DataLoaderComparison;

internal sealed class OrderLoaderException(OrderFailure failure) : Exception(failure.Detail)
{
    internal OrderFailure Failure { get; } = failure;
}

internal static class GreenDonutOrderEnricher
{
    internal static async Task<IReadOnlyList<Result<EnrichedOrder, OrderFailure>>> EnrichAsync(
        IReadOnlyList<Order> orders,
        DeterministicOrderSources sources,
        CancellationToken cancellationToken)
    {
        var customers = new CustomerLoader(sources);
        var preferences = new PreferenceLoader(sources);
        var regions = new RegionLoader(sources);

        var customerTasks = new Task<Customer?>[orders.Count];
        var preferenceTasks = new Task<Preference?>[orders.Count];
        for (var index = 0; index < orders.Count; index++)
        {
            customerTasks[index] = customers.LoadAsync(orders[index].CustomerKey, cancellationToken);
            preferenceTasks[index] = preferences.LoadAsync(orders[index].CustomerKey, cancellationToken);
        }

        var customerOutcomes = new Result<Customer, OrderFailure>[orders.Count];
        var preferenceOutcomes = new Result<Preference, OrderFailure>[orders.Count];
        for (var index = 0; index < orders.Count; index++)
        {
            customerOutcomes[index] = await AwaitAsync(customerTasks[index]);
            preferenceOutcomes[index] = await AwaitAsync(preferenceTasks[index]);
        }

        var regionTasks = new Task<Region?>?[orders.Count];
        for (var index = 0; index < orders.Count; index++)
        {
            if (customerOutcomes[index].TryGetValue(out var customer)
                && preferenceOutcomes[index].TryGetValue(out _))
            {
                regionTasks[index] = regions.LoadAsync(customer.RegionKey, cancellationToken);
            }
        }

        var regionOutcomes = new Result<Region, OrderFailure>[orders.Count];
        for (var index = 0; index < orders.Count; index++)
        {
            var regionTask = regionTasks[index];
            if (regionTask is not null)
            {
                regionOutcomes[index] = await AwaitAsync(regionTask);
            }
        }

        var results = new Result<EnrichedOrder, OrderFailure>[orders.Count];
        for (var index = 0; index < orders.Count; index++)
        {
            var order = orders[index];
            if (!customerOutcomes[index].TryGetValue(out var customer))
            {
                customerOutcomes[index].TryGetError(out var customerError);
                results[index] = Result<EnrichedOrder, OrderFailure>.Failure(customerError!);
                continue;
            }

            if (!preferenceOutcomes[index].TryGetValue(out var preference))
            {
                preferenceOutcomes[index].TryGetError(out var preferenceError);
                results[index] = Result<EnrichedOrder, OrderFailure>.Failure(preferenceError!);
                continue;
            }

            if (!regionOutcomes[index].TryGetValue(out var region))
            {
                regionOutcomes[index].TryGetError(out var regionError);
                results[index] = Result<EnrichedOrder, OrderFailure>.Failure(regionError!);
                continue;
            }

            results[index] = Result<EnrichedOrder, OrderFailure>.Success(
                new EnrichedOrder(
                    order,
                    customer,
                    preference,
                    region,
                    Total(order, preference, region)));
        }

        return results;
    }

    private static async Task<Result<TValue, OrderFailure>> AwaitAsync<TValue>(Task<TValue?> task)
        where TValue : class
    {
        try
        {
            return Result<TValue, OrderFailure>.Success((await task)!);
        }
        catch (OrderLoaderException exception)
        {
            return Result<TValue, OrderFailure>.Failure(exception.Failure);
        }
        catch (KeyNotFoundException)
        {
            return Result<TValue, OrderFailure>.Failure(OrderFailure.MissingRecord);
        }
    }

    private static decimal Total(Order order, Preference preference, Region region) =>
        decimal.Round(
            (order.UnitPrice * order.Units * (1m + region.TaxRate))
                + (preference.GiftWrap ? 2m : 0m),
            2,
            MidpointRounding.AwayFromZero);

    private abstract class OrderLoader<TValue>(
        Func<IReadOnlyList<string>, CancellationToken, ValueTask<Result<IReadOnlyList<BatchSourceEntry<string, TValue, OrderFailure>>, OrderFailure>>> fetch)
        : DataLoaderBase<string, TValue>(AutoBatchScheduler.Default, new DataLoaderOptions { MaxBatchSize = 128 })
        where TValue : class
    {
        protected override async ValueTask FetchAsync(
            IReadOnlyList<string> keys,
            Memory<GreenDonut.Result<TValue?>> results,
            DataLoaderFetchContext<TValue> context,
            CancellationToken cancellationToken)
        {
            var response = await fetch(keys, cancellationToken);
            var values = Interpret(keys, response);
            var span = results.Span;
            for (var index = 0; index < keys.Count; index++)
            {
                span[index] = values[index];
            }
        }

        private static IReadOnlyList<GreenDonut.Result<TValue?>> Interpret(
            IReadOnlyList<string> keys,
            Result<IReadOnlyList<BatchSourceEntry<string, TValue, OrderFailure>>, OrderFailure> response)
        {
            if (response.IsFailure)
            {
                response.TryGetError(out var error);
                var exception = new OrderLoaderException(error!);
                return keys.Select(_ => new GreenDonut.Result<TValue?>(exception)).ToArray();
            }

            if (!response.TryGetValue(out var entries) || entries is null)
            {
                return Unexpected(keys, "the source returned a null entry collection.");
            }

            if (entries.Count > keys.Count)
            {
                return Unexpected(keys, "the source returned more entries than requested.");
            }

            var requested = new HashSet<string>(keys, StringComparer.Ordinal);
            var byKey = new Dictionary<string, BatchSourceEntry<string, TValue, OrderFailure>>(
                StringComparer.Ordinal);
            foreach (var entry in entries)
            {
                if (entry.Kind == BatchSourceEntryKind.Unset)
                {
                    return Unexpected(keys, "the source returned an uninitialized entry.");
                }

                if (entry.Key is null)
                {
                    return Unexpected(keys, "the source returned a null key.");
                }

                if (!requested.Contains(entry.Key))
                {
                    return Unexpected(keys, "the source returned a key that was not requested.");
                }

                if (!byKey.TryAdd(entry.Key, entry))
                {
                    return Unexpected(keys, "the source returned a key more than once.");
                }
            }

            var results = new GreenDonut.Result<TValue?>[keys.Count];
            for (var index = 0; index < keys.Count; index++)
            {
                if (!byKey.TryGetValue(keys[index], out var entry))
                {
                    results[index] = new GreenDonut.Result<TValue?>(
                        new KeyNotFoundException(
                            $"The key '{keys[index]}' was not returned."));
                }
                else if (entry.TryGetValue(out var value))
                {
                    results[index] = new GreenDonut.Result<TValue?>(value);
                }
                else if (entry.TryGetError(out var error))
                {
                    results[index] = new GreenDonut.Result<TValue?>(
                        new OrderLoaderException(error));
                }
                else
                {
                    return Unexpected(keys, "the source returned an unexpected entry kind.");
                }
            }

            return results;
        }

        private static IReadOnlyList<GreenDonut.Result<TValue?>> Unexpected(
            IReadOnlyList<string> keys,
            string detail)
        {
            var exception = new OrderLoaderException(OrderFailure.UnexpectedSourceResponse(detail));
            return keys.Select(_ => new GreenDonut.Result<TValue?>(exception)).ToArray();
        }
    }

    private sealed class CustomerLoader(DeterministicOrderSources sources)
        : OrderLoader<Customer>(sources.FetchCustomersAsync);

    private sealed class PreferenceLoader(DeterministicOrderSources sources)
        : OrderLoader<Preference>(sources.FetchPreferencesAsync);

    private sealed class RegionLoader(DeterministicOrderSources sources)
        : OrderLoader<Region>(sources.FetchRegionsAsync);
}
