using FunnySharp;

namespace FunnySharp.TestSupport.Batching;

internal static class FunnySharpOrderEnricher
{
    internal static async Task<IReadOnlyList<Result<EnrichedOrder, OrderFailure>>> EnrichAsync(
        IReadOnlyList<Order> orders,
        DeterministicOrderSources sources,
        CancellationToken cancellationToken)
    {
        await using var scope = BatchScope.Create();
        return await EnrichAsync(orders, sources, scope, cancellationToken);
    }

    internal static Task<IReadOnlyList<Result<EnrichedOrder, OrderFailure>>> EnrichAsync(
        IReadOnlyList<Order> orders,
        DeterministicOrderSources sources,
        BatchScope scope,
        CancellationToken cancellationToken) =>
        ExecuteAsync(BuildPlan(orders, sources), scope, cancellationToken);

    internal static BatchRequirement<
        IReadOnlyList<Result<EnrichedOrder, BatchReadError<OrderFailure>>>,
        OrderFailure> BuildPlan(
        IReadOnlyList<Order> orders,
        DeterministicOrderSources sources) =>
        BatchRequirement.All(orders.Select(order => Plan(order, sources)));

    internal static async Task<IReadOnlyList<Result<EnrichedOrder, OrderFailure>>> ExecuteAsync(
        BatchRequirement<
            IReadOnlyList<Result<EnrichedOrder, BatchReadError<OrderFailure>>>,
            OrderFailure> requirement,
        BatchScope scope,
        CancellationToken cancellationToken)
    {
        var outer = await scope.ExecuteAsync(requirement, cancellationToken);
        if (!outer.TryGetValue(out var outcomes))
        {
            throw new InvalidOperationException("The batch plan itself failed.");
        }

        var results = new Result<EnrichedOrder, OrderFailure>[outcomes.Count];
        for (var index = 0; index < outcomes.Count; index++)
        {
            results[index] = MapOutcome(outcomes[index]);
        }

        return results;
    }

    private static BatchRequirement<EnrichedOrder, OrderFailure> Plan(
        Order order,
        DeterministicOrderSources sources) =>
        BatchRequirement
            .Read(sources.Customers, order.CustomerKey)
            .Zip(BatchRequirement.Read(sources.Preferences, order.CustomerKey))
            .Bind(pair => BatchRequirement
                .Read(sources.Regions, pair.First.RegionKey)
                .Map(region => new EnrichedOrder(
                    order,
                    pair.First,
                    pair.Second,
                    region,
                    Total(order, pair.Second, region))));

    private static Result<EnrichedOrder, OrderFailure> MapOutcome(
        Result<EnrichedOrder, BatchReadError<OrderFailure>> outcome) =>
        outcome.MapError(static error =>
            error.TryGetSourceError(out var sourceError)
                ? sourceError
                : error.Kind switch
                {
                    BatchReadErrorKind.Missing => OrderFailure.MissingRecord,
                    BatchReadErrorKind.UnexpectedResponse =>
                        OrderFailure.UnexpectedSourceResponse(error.Detail),
                    _ => OrderFailure.UnexpectedSourceResponse(error.ToString()),
                });

    private static decimal Total(Order order, Preference preference, Region region) =>
        decimal.Round(
            (order.UnitPrice * order.Units * (1m + region.TaxRate))
                + (preference.GiftWrap ? 2m : 0m),
            2,
            MidpointRounding.AwayFromZero);
}
