using Microsoft.Data.Sqlite;

using FunnySharp;

namespace FunnySharp.TestSupport.OperationComparison;

internal static class OrderOperations
{
    internal static async Task<Result<OrderState, OrderError>> LoadCustomerAsync(
        OrderEnvironment environment,
        OrderState state,
        CancellationToken cancellationToken)
    {
        environment.Faults.OnStepEnter("load-customer");

        var customer = await ShopSql
            .LoadCustomerAsync(
                environment.Connection,
                environment.Transaction,
                state.Request.CustomerId,
                cancellationToken)
            .ConfigureAwait(false);

        if (!customer.TryGetValue(out var value))
        {
            return OrderStepResult.Failure(new OrderError.CustomerNotFound(state.Request.CustomerId));
        }

        if (!value.Active)
        {
            return OrderStepResult.Failure(new OrderError.CustomerInactive(state.Request.CustomerId));
        }

        environment.Faults.OnStepExit("load-customer");
        return OrderStepResult.Success(state with { Customer = value });
    }

    internal static async Task<Result<OrderState, OrderError>> LoadCatalogAsync(
        OrderEnvironment environment,
        OrderState state,
        CancellationToken cancellationToken)
    {
        environment.Faults.OnStepEnter("load-catalog");

        var skus = new HashSet<string>(StringComparer.Ordinal);

        foreach (var line in state.Request.Lines)
        {
            skus.Add(line.Sku);
        }

        var catalog = new Dictionary<string, CatalogItem>(skus.Count, StringComparer.Ordinal);

        foreach (var sku in skus)
        {
            var item = await ShopSql
                .LoadCatalogItemAsync(
                    environment.Connection,
                    environment.Transaction,
                    sku,
                    cancellationToken)
                .ConfigureAwait(false);

            if (!item.TryGetValue(out var value))
            {
                return OrderStepResult.Failure(new OrderError.CatalogItemNotFound(sku));
            }

            if (!value.Active)
            {
                return OrderStepResult.Failure(new OrderError.CatalogItemInactive(sku));
            }

            catalog.Add(sku, value);
        }

        environment.Faults.OnStepExit("load-catalog");
        return OrderStepResult.Success(state with { Catalog = catalog });
    }

    internal static async Task<Result<OrderState, OrderError>> CreateOrderAsync(
        OrderEnvironment environment,
        OrderState state,
        CancellationToken cancellationToken)
    {
        environment.Faults.OnStepEnter("create-order");

        var priced = OrderPricing.PriceLines(state);

        if (!priced.TryGetValue(out var lines))
        {
            priced.TryGetError(out var error);
            return OrderStepResult.Failure(error!);
        }

        var total = OrderPricing.Total(lines);

        var orderId = await ShopSql
            .InsertOrderAsync(
                environment.Connection,
                environment.Transaction,
                state.Request.CustomerId,
                total,
                cancellationToken)
            .ConfigureAwait(false);

        foreach (var line in lines)
        {
            await ShopSql
                .InsertOrderLineAsync(
                    environment.Connection,
                    environment.Transaction,
                    orderId,
                    line,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        environment.Faults.OnStepExit("create-order");
        return OrderStepResult.Success(
            state with
            {
                OrderId = orderId,
                TotalCents = total,
                Lines = lines,
            });
    }

    internal static Task<Result<OrderState, OrderError>> CheckCreditAsync(
        OrderEnvironment environment,
        OrderState state,
        CancellationToken cancellationToken)
    {
        environment.Faults.OnStepEnter("check-credit");

        var result = OrderPricing.CheckCredit(state);

        if (result.IsSuccess)
        {
            environment.Faults.OnStepExit("check-credit");
        }

        return Task.FromResult(result);
    }

    internal static async Task<Result<OrderState, OrderError>> ReserveInventoryAsync(
        OrderEnvironment environment,
        OrderState state,
        CancellationToken cancellationToken)
    {
        environment.Faults.OnStepEnter("reserve-inventory");

        var lines = state.Lines;

        if (lines is null)
        {
            return OrderStepResult.Failure(new OrderError.EmptyOrder());
        }

        foreach (var line in lines)
        {
            var rows = await ShopSql
                .TryReserveInventoryAsync(
                    environment.Connection,
                    environment.Transaction,
                    line,
                    cancellationToken)
                .ConfigureAwait(false);

            if (rows == 0)
            {
                var available = await ShopSql
                    .GetAvailableAsync(
                        environment.Connection,
                        environment.Transaction,
                        line.Sku,
                        cancellationToken)
                    .ConfigureAwait(false);

                return OrderStepResult.Failure(
                    new OrderError.InsufficientStock(line.Sku, line.Quantity, available));
            }
        }

        environment.Faults.OnStepExit("reserve-inventory");
        return OrderStepResult.Success(state);
    }

    internal static async Task<Result<OrderState, OrderError>> AuthorizePaymentAsync(
        OrderEnvironment environment,
        OrderState state,
        CancellationToken cancellationToken)
    {
        environment.Faults.OnStepEnter("authorize-payment");

        var result = await OrderPayment
            .AuthorizeAsync(state, environment.Gateway, cancellationToken)
            .ConfigureAwait(false);

        if (result.IsSuccess)
        {
            environment.Faults.OnStepExit("authorize-payment");
        }

        return result;
    }

    internal static async Task<Result<OrderState, OrderError>> RecordPaymentAsync(
        OrderEnvironment environment,
        OrderState state,
        CancellationToken cancellationToken)
    {
        environment.Faults.OnStepEnter("record-payment");

        var reference = state.PaymentReference;

        if (reference is null)
        {
            return OrderStepResult.Failure(new OrderError.PaymentReferenceMissing());
        }

        try
        {
            await ShopSql
                .InsertPaymentAsync(
                    environment.Connection,
                    environment.Transaction,
                    state.OrderId,
                    state.TotalCents,
                    reference,
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (SqliteException exception) when (exception.SqliteErrorCode == 19)
        {
            return OrderStepResult.Failure(new OrderError.PaymentRecordConflict(reference));
        }

        environment.Faults.OnStepExit("record-payment");
        return OrderStepResult.Success(state);
    }

    internal static async Task<Result<OrderState, OrderError>> RecordOutboxAsync(
        OrderEnvironment environment,
        OrderState state,
        CancellationToken cancellationToken)
    {
        environment.Faults.OnStepEnter("record-outbox");
        environment.Faults.OnOutbox();

        await ShopSql
            .InsertOutboxAsync(
                environment.Connection,
                environment.Transaction,
                state.OrderId,
                OrderPricing.BuildOutboxPayload(state),
                cancellationToken)
            .ConfigureAwait(false);

        environment.Faults.OnStepExit("record-outbox");
        return OrderStepResult.Success(state);
    }

    internal static async Task<Result<OrderState, OrderError>> QueueReviewAsync(
        OrderEnvironment environment,
        OrderState state,
        CancellationToken cancellationToken)
    {
        environment.Faults.OnStepEnter("queue-review");

        if (state.TotalCents >= ShopFixtures.ReviewThresholdCents)
        {
            await ShopSql
                .InsertReviewAsync(
                    environment.Connection,
                    environment.Transaction,
                    state.OrderId,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        environment.Faults.OnStepExit("queue-review");
        return OrderStepResult.Success(state);
    }
}
