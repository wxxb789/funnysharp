using FunnySharp;

namespace FunnySharp.DocumentationSamples;

internal static class ConcurrencySamples
{
    private static async Task SelectParallelAsync(
        IAsyncEnumerable<Order> orders,
        CancellationToken cancellationToken)
    {
        // <snippet DocumentationSamples.Concurrency.SelectParallel>
        var quotedOrders = orders.SelectParallelValueAsync(
            maxConcurrency: 4,
            (order, cancellationToken) =>
                new ValueTask<ShippingQuote>(GetShippingQuoteAsync(order, cancellationToken)));

        await foreach (var quote in quotedOrders.WithCancellation(cancellationToken))
        {
            Process(quote);
        }
        // </snippet>
    }

    private static async Task TraverseParallelAsync(
        IAsyncEnumerable<Order> orders,
        CancellationToken cancellationToken)
    {
        // <snippet DocumentationSamples.Concurrency.TraverseParallel>
        var checkedOrders = await orders.TraverseParallelValueAsync(
            maxConcurrency: 4,
            (order, cancellationToken) => ValidateOrderAsync(order, cancellationToken),
            cancellationToken);
        // </snippet>
    }

    private static async Task FirstSuccessAsync(CancellationToken cancellationToken)
    {
        // <snippet DocumentationSamples.Concurrency.FirstSuccess>
        var providers = new[]
        {
            Effect.FromTask<Result<ShippingQuote, QuoteError>>(
                cancellationToken => GetQuoteFromCarrierAsync("north", cancellationToken)),
            Effect.FromTask<Result<ShippingQuote, QuoteError>>(
                cancellationToken => GetQuoteFromCarrierAsync("south", cancellationToken)),
        };

        var firstQuote = await providers.FirstSuccessAsync(
            TimeSpan.FromSeconds(2),
            TimeProvider.System,
            cancellationToken);
        // </snippet>
    }

    private static Task<ShippingQuote> GetShippingQuoteAsync(Order order, CancellationToken cancellationToken) =>
        Task.FromResult(new ShippingQuote(order.Id));

    private static ValueTask<Validation<ShippingQuote, QuoteError>> ValidateOrderAsync(
        Order order,
        CancellationToken cancellationToken) =>
        ValueTask.FromResult(Validation<ShippingQuote, QuoteError>.Valid(new ShippingQuote(order.Id)));

    private static Task<Result<ShippingQuote, QuoteError>> GetQuoteFromCarrierAsync(
        string carrier,
        CancellationToken cancellationToken) =>
        Task.FromResult(Result<ShippingQuote, QuoteError>.Success(new ShippingQuote(carrier)));

    private static void Process(ShippingQuote quote)
    {
    }

    private sealed record Order(string Id);

    private sealed record ShippingQuote(string Provider);

    private sealed record QuoteError(string Code);
}
