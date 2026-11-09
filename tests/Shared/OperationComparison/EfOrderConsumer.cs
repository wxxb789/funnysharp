using FunnySharp;
using FunnySharp.OperationPlans;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace FunnySharp.TestSupport.OperationComparison;

internal sealed record EfOrderEnvironment(
    ShopDbContext Context,
    IDbContextTransaction Transaction,
    SimulatedPaymentGateway Gateway,
    FaultInjector Faults);

internal static class EfOrderOperations
{
    internal static async Task<Result<OrderState, OrderError>> LoadCustomerAsync(
        EfOrderEnvironment environment,
        OrderState state,
        CancellationToken cancellationToken)
    {
        environment.Faults.OnStepEnter("load-customer");

        var entity = await environment.Context.Customers
            .SingleOrDefaultAsync(
                customer => customer.Id == state.Request.CustomerId,
                cancellationToken)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return OrderStepResult.Failure(new OrderError.CustomerNotFound(state.Request.CustomerId));
        }

        if (!entity.Active)
        {
            return OrderStepResult.Failure(new OrderError.CustomerInactive(state.Request.CustomerId));
        }

        environment.Faults.OnStepExit("load-customer");
        return OrderStepResult.Success(
            state with
            {
                Customer = new Customer(
                    entity.Id,
                    entity.Name,
                    entity.CreditLimitCents,
                    entity.Active),
            });
    }

    internal static async Task<Result<OrderState, OrderError>> LoadCatalogAsync(
        EfOrderEnvironment environment,
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
            var entity = await environment.Context.Catalog
                .SingleOrDefaultAsync(item => item.Sku == sku, cancellationToken)
                .ConfigureAwait(false);

            if (entity is null)
            {
                return OrderStepResult.Failure(new OrderError.CatalogItemNotFound(sku));
            }

            if (!entity.Active)
            {
                return OrderStepResult.Failure(new OrderError.CatalogItemInactive(sku));
            }

            catalog.Add(sku, new CatalogItem(entity.Sku, entity.UnitPriceCents, entity.Active));
        }

        environment.Faults.OnStepExit("load-catalog");
        return OrderStepResult.Success(state with { Catalog = catalog });
    }

    internal static async Task<Result<OrderState, OrderError>> CreateOrderAsync(
        EfOrderEnvironment environment,
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

        var order = new OrderEntity
        {
            CustomerId = state.Request.CustomerId,
            TotalCents = total,
            Status = "pending",
        };

        foreach (var line in lines)
        {
            order.Lines.Add(
                new OrderLineEntity
                {
                    Sku = line.Sku,
                    Quantity = line.Quantity,
                    UnitPriceCents = line.UnitPriceCents,
                });
        }

        environment.Context.Orders.Add(order);
        await environment.Context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        environment.Faults.OnStepExit("create-order");
        return OrderStepResult.Success(
            state with
            {
                OrderId = order.Id,
                TotalCents = total,
                Lines = lines,
            });
    }

    internal static Task<Result<OrderState, OrderError>> CheckCreditAsync(
        EfOrderEnvironment environment,
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
        EfOrderEnvironment environment,
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
            var entity = await environment.Context.Inventory
                .SingleOrDefaultAsync(item => item.Sku == line.Sku, cancellationToken)
                .ConfigureAwait(false);

            if (entity is null)
            {
                return OrderStepResult.Failure(
                    new OrderError.InsufficientStock(line.Sku, line.Quantity, 0));
            }

            if (entity.Available < line.Quantity)
            {
                return OrderStepResult.Failure(
                    new OrderError.InsufficientStock(line.Sku, line.Quantity, entity.Available));
            }

            entity.Available -= line.Quantity;
            entity.Reserved += line.Quantity;
        }

        await environment.Context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        environment.Faults.OnStepExit("reserve-inventory");
        return OrderStepResult.Success(state);
    }

    internal static async Task<Result<OrderState, OrderError>> AuthorizePaymentAsync(
        EfOrderEnvironment environment,
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
        EfOrderEnvironment environment,
        OrderState state,
        CancellationToken cancellationToken)
    {
        environment.Faults.OnStepEnter("record-payment");

        var reference = state.PaymentReference;

        if (reference is null)
        {
            return OrderStepResult.Failure(new OrderError.PaymentReferenceMissing());
        }

        environment.Context.Payments.Add(
            new PaymentEntity
            {
                OrderId = state.OrderId,
                AmountCents = state.TotalCents,
                Reference = reference,
            });

        try
        {
            await environment.Context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is SqliteException sqlite && sqlite.SqliteErrorCode == 19)
        {
            return OrderStepResult.Failure(new OrderError.PaymentRecordConflict(reference));
        }

        environment.Faults.OnStepExit("record-payment");
        return OrderStepResult.Success(state);
    }

    internal static async Task<Result<OrderState, OrderError>> RecordOutboxAsync(
        EfOrderEnvironment environment,
        OrderState state,
        CancellationToken cancellationToken)
    {
        environment.Faults.OnStepEnter("record-outbox");
        environment.Faults.OnOutbox();

        environment.Context.Outbox.Add(
            new OutboxEntity
            {
                OrderId = state.OrderId,
                Topic = ShopFixtures.OutboxTopic,
                Payload = OrderPricing.BuildOutboxPayload(state),
            });

        await environment.Context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        environment.Faults.OnStepExit("record-outbox");
        return OrderStepResult.Success(state);
    }

    internal static async Task<Result<OrderState, OrderError>> QueueReviewAsync(
        EfOrderEnvironment environment,
        OrderState state,
        CancellationToken cancellationToken)
    {
        environment.Faults.OnStepEnter("queue-review");

        if (state.TotalCents >= ShopFixtures.ReviewThresholdCents)
        {
            environment.Context.Reviews.Add(
                new ReviewEntity
                {
                    OrderId = state.OrderId,
                    Reason = ShopFixtures.ReviewReason,
                });

            await environment.Context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        environment.Faults.OnStepExit("queue-review");
        return OrderStepResult.Success(state);
    }
}

internal sealed class EfOrderConsumer : IOrderConsumer
{
    public string Name => "EF Core transaction";

    public IReadOnlyList<OperationInfo> Describe => ComparisonDescriptors.Describe;

    public async Task<Result<OperationSuccess<OrderState>, RunFailure<OrderState, OrderError>>> PlaceOrderAsync(
        SqliteConnection connection,
        PlaceOrderRequest request,
        SimulatedPaymentGateway gateway,
        FaultInjector faults,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Result<OperationSuccess<OrderState>, RunFailure<OrderState, OrderError>>.Failure(
                AlternativeFailures.CanceledFailure(
                    "load-customer",
                    Array.Empty<string>(),
                    new OrderState(request)));
        }

        await using var context = new ShopDbContext(connection);
        IDbContextTransaction transaction;

        try
        {
            faults.OnTransactionOpen();
            transaction = await context.Database
                .BeginTransactionAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException exception)
        {
            return Result<OperationSuccess<OrderState>, RunFailure<OrderState, OrderError>>.Failure(
                AlternativeFailures.CanceledFailure(
                    "load-customer",
                    Array.Empty<string>(),
                    new OrderState(request),
                    exception));
        }

        await using (transaction)
        {
            var environment = new EfOrderEnvironment(context, transaction, gateway, faults);

            return await RunAsync(environment, request, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task<Result<OperationSuccess<OrderState>, RunFailure<OrderState, OrderError>>> RunAsync(
        EfOrderEnvironment environment,
        PlaceOrderRequest request,
        CancellationToken cancellationToken)
    {
        var state = new OrderState(request);
        var completed = new List<string>();
        var skipped = new List<string>();

        var result = await NamedStep
            .RunAsync("load-customer", environment, state, completed, cancellationToken, EfOrderOperations.LoadCustomerAsync)
            .ConfigureAwait(false);

        if (!result.TryGetValue(out var next))
        {
            return await FailAfterRollbackAsync(environment, result).ConfigureAwait(false);
        }

        state = next;

        result = await NamedStep
            .RunAsync("load-catalog", environment, state, completed, cancellationToken, EfOrderOperations.LoadCatalogAsync)
            .ConfigureAwait(false);

        if (!result.TryGetValue(out next))
        {
            return await FailAfterRollbackAsync(environment, result).ConfigureAwait(false);
        }

        state = next;

        result = await NamedStep
            .RunAsync("create-order", environment, state, completed, cancellationToken, EfOrderOperations.CreateOrderAsync)
            .ConfigureAwait(false);

        if (!result.TryGetValue(out next))
        {
            return await FailAfterRollbackAsync(environment, result).ConfigureAwait(false);
        }

        state = next;

        result = await NamedStep
            .RunAsync("check-credit", environment, state, completed, cancellationToken, EfOrderOperations.CheckCreditAsync)
            .ConfigureAwait(false);

        if (!result.TryGetValue(out next))
        {
            return await FailAfterRollbackAsync(environment, result).ConfigureAwait(false);
        }

        state = next;

        result = await NamedStep
            .RunAsync("reserve-inventory", environment, state, completed, cancellationToken, EfOrderOperations.ReserveInventoryAsync)
            .ConfigureAwait(false);

        if (!result.TryGetValue(out next))
        {
            return await FailAfterRollbackAsync(environment, result).ConfigureAwait(false);
        }

        state = next;

        result = await NamedStep
            .RunAsync("authorize-payment", environment, state, completed, cancellationToken, EfOrderOperations.AuthorizePaymentAsync)
            .ConfigureAwait(false);

        if (!result.TryGetValue(out next))
        {
            return await FailAfterRollbackAsync(environment, result).ConfigureAwait(false);
        }

        state = next;

        result = await NamedStep
            .RunAsync("record-payment", environment, state, completed, cancellationToken, EfOrderOperations.RecordPaymentAsync)
            .ConfigureAwait(false);

        if (!result.TryGetValue(out next))
        {
            return await FailAfterRollbackAsync(environment, result).ConfigureAwait(false);
        }

        state = next;

        result = await NamedStep
            .RunAsync("record-outbox", environment, state, completed, cancellationToken, EfOrderOperations.RecordOutboxAsync)
            .ConfigureAwait(false);

        if (!result.TryGetValue(out next))
        {
            return await FailAfterRollbackAsync(environment, result).ConfigureAwait(false);
        }

        state = next;

        if (cancellationToken.IsCancellationRequested)
        {
            return await FailAfterRollbackAsync(
                environment,
                AlternativeFailures.Canceled("queue-review", completed, state)).ConfigureAwait(false);
        }

        if (state.TotalCents >= ShopFixtures.ReviewThresholdCents)
        {
            result = await NamedStep
                .RunAsync("queue-review", environment, state, completed, cancellationToken, EfOrderOperations.QueueReviewAsync)
                .ConfigureAwait(false);

            if (!result.TryGetValue(out next))
            {
                return await FailAfterRollbackAsync(environment, result).ConfigureAwait(false);
            }

            state = next;
        }
        else
        {
            skipped.Add("queue-review");
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return await FailAfterRollbackAsync(
                environment,
                AlternativeFailures.Canceled(null, completed, state)).ConfigureAwait(false);
        }

        try
        {
            environment.Faults.OnCommit();
            await environment.Transaction
                .CommitAsync(CancellationToken.None)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException exception)
        {
            return await FailCommitAsync(
                environment,
                RunFailureKind.Canceled,
                exception,
                completed,
                state).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            return await FailCommitAsync(
                environment,
                RunFailureKind.CommitFailed,
                exception,
                completed,
                state).ConfigureAwait(false);
        }

        return Result<OperationSuccess<OrderState>, RunFailure<OrderState, OrderError>>.Success(
            new OperationSuccess<OrderState>(state, completed, skipped));
    }

    private static async Task<Result<OperationSuccess<OrderState>, RunFailure<OrderState, OrderError>>> FailAfterRollbackAsync(
        EfOrderEnvironment environment,
        Result<OrderState, RunFailure<OrderState, OrderError>> failure)
    {
        if (!failure.TryGetError(out var error))
        {
            throw new InvalidOperationException("Expected a failed result.");
        }

        try
        {
            environment.Faults.OnRollback();
            await environment.Transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception rollbackException)
        {
            return Result<OperationSuccess<OrderState>, RunFailure<OrderState, OrderError>>.Failure(
                AlternativeFailures.RollbackFailed(
                    error.Kind,
                    error.Exception,
                    rollbackException,
                    error.Operation,
                    error.Error,
                    error.Completed,
                    error.State));
        }

        return Result<OperationSuccess<OrderState>, RunFailure<OrderState, OrderError>>.Failure(error);
    }

    private static async Task<Result<OperationSuccess<OrderState>, RunFailure<OrderState, OrderError>>> FailCommitAsync(
        EfOrderEnvironment environment,
        RunFailureKind originalKind,
        Exception commitException,
        IReadOnlyList<string> completed,
        OrderState state)
    {
        try
        {
            environment.Faults.OnRollback();
            await environment.Transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception rollbackException)
        {
            return Result<OperationSuccess<OrderState>, RunFailure<OrderState, OrderError>>.Failure(
                AlternativeFailures.RollbackFailed(
                    originalKind,
                    commitException,
                    rollbackException,
                    null,
                    default,
                    completed,
                    state));
        }

        return Result<OperationSuccess<OrderState>, RunFailure<OrderState, OrderError>>.Failure(
            originalKind == RunFailureKind.Canceled
                ? AlternativeFailures.CanceledFailure(null, completed, state, commitException)
                : AlternativeFailures.CommitFailed(commitException, completed, state));
    }
}
