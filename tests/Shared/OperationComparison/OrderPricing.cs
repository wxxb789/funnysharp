using System.Globalization;

using FunnySharp;

namespace FunnySharp.TestSupport.OperationComparison;

internal static class OrderPricing
{
    internal static Result<PricedLine[], OrderError> PriceLines(OrderState state)
    {
        var catalog = state.Catalog;

        if (catalog is null || state.Request.Lines.Count == 0)
        {
            return Result<PricedLine[], OrderError>.Failure(new OrderError.EmptyOrder());
        }

        var lines = new PricedLine[state.Request.Lines.Count];

        for (var index = 0; index < state.Request.Lines.Count; index++)
        {
            var requestLine = state.Request.Lines[index];

            if (!catalog.TryGetValue(requestLine.Sku, out var item))
            {
                return Result<PricedLine[], OrderError>.Failure(
                    new OrderError.CatalogItemNotFound(requestLine.Sku));
            }

            lines[index] = new PricedLine(requestLine.Sku, requestLine.Quantity, item.UnitPriceCents);
        }

        return Result<PricedLine[], OrderError>.Success(lines);
    }

    internal static int Total(PricedLine[] lines)
    {
        var total = 0;

        foreach (var line in lines)
        {
            total = checked(total + line.LineTotalCents);
        }

        return total;
    }

    internal static Result<OrderState, OrderError> CheckCredit(OrderState state)
    {
        if (state.Customer is null)
        {
            return Result<OrderState, OrderError>.Failure(
                new OrderError.CustomerNotFound(state.Request.CustomerId));
        }

        return state.TotalCents > state.Customer.CreditLimitCents
            ? Result<OrderState, OrderError>.Failure(
                new OrderError.CreditLimitExceeded(state.TotalCents, state.Customer.CreditLimitCents))
            : Result<OrderState, OrderError>.Success(state);
    }

    internal static string BuildOutboxPayload(OrderState state)
    {
        var reference = state.PaymentReference ?? string.Empty;

        return $"{{\"orderId\":{state.OrderId.ToString(CultureInfo.InvariantCulture)},\"payment\":\"{reference}\",\"totalCents\":{state.TotalCents.ToString(CultureInfo.InvariantCulture)}}}";
    }
}

internal static class OrderPayment
{
    internal static async Task<Result<OrderState, OrderError>> AuthorizeAsync(
        OrderState state,
        SimulatedPaymentGateway gateway,
        CancellationToken cancellationToken)
    {
        var result = await gateway
            .AuthorizeAsync(state.OrderId, state.TotalCents, cancellationToken)
            .ConfigureAwait(false);

        if (!result.TryGetValue(out var reference))
        {
            result.TryGetError(out var error);
            return Result<OrderState, OrderError>.Failure(error!);
        }

        return Result<OrderState, OrderError>.Success(state with { PaymentReference = reference });
    }
}

internal static class OrderStepResult
{
    internal static Result<OrderState, OrderError> Success(OrderState state) =>
        Result<OrderState, OrderError>.Success(state);

    internal static Result<OrderState, OrderError> Failure(OrderError error) =>
        Result<OrderState, OrderError>.Failure(error);
}
