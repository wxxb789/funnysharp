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

    private static async Task SelectParallelCompletionOrderAsync(
        IAsyncEnumerable<Order> orders,
        CancellationToken cancellationToken)
    {
        // <snippet DocumentationSamples.Concurrency.SelectParallelCompletionOrder>
        var quotedOrders = orders.SelectParallelCompletionOrderValueAsync(
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


    private static async Task SelectParallelByKeyAsync(
        IAsyncEnumerable<AccountEvent> accountEvents,
        CancellationToken cancellationToken)
    {
        // <snippet DocumentationSamples.Concurrency.SelectParallelByKey>
        var processed = accountEvents.SelectParallelByKeyCompletionOrderValueAsync(
            accountEvent => accountEvent.AccountId,
            maxConcurrency: 4,
            maxBufferedItems: 8,
            (accountEvent, cancellationToken) =>
                ApplyAccountEventAsync(accountEvent, cancellationToken));

        await foreach (var (accountId, accountEvent, outcome) in processed.WithCancellation(cancellationToken))
        {
            Observe(accountId, accountEvent.Sequence, outcome);
        }
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

    private static ValueTask<Result<decimal, AccountError>> ApplyAccountEventAsync(
        AccountEvent accountEvent,
        CancellationToken cancellationToken) =>
        ValueTask.FromResult(accountEvent.Amount < 0
            ? Result<decimal, AccountError>.Failure(new AccountError("insufficient-funds"))
            : Result<decimal, AccountError>.Success(accountEvent.Amount));

    private static void Process(ShippingQuote quote)
    {
    }

    private static void Observe(
        string accountId,
        int sequence,
        Result<decimal, AccountError> outcome)
    {
    }

    private sealed record AccountEvent(string AccountId, int Sequence, decimal Amount);

    private sealed record AccountError(string Code);

    private sealed record Order(string Id);

    private sealed record ShippingQuote(string Provider);

    private sealed record QuoteError(string Code);
}
