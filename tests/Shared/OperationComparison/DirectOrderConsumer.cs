using FunnySharp;
using FunnySharp.OperationPlans;

using Microsoft.Data.Sqlite;

namespace FunnySharp.TestSupport.OperationComparison;

internal sealed class DirectOrderConsumer : IOrderConsumer
{
    public string Name => "Direct await";

    public IReadOnlyList<OperationInfo> Describe => ComparisonDescriptors.Describe;

    public async Task<Result<OperationSuccess<OrderState>, RunFailure<OrderState, OrderError>>> PlaceOrderAsync(
        SqliteConnection connection,
        PlaceOrderRequest request,
        SimulatedPaymentGateway gateway,
        FaultInjector faults,
        CancellationToken cancellationToken)
    {
        using var transaction = connection.BeginTransaction(deferred: false);
        var environment = new OrderEnvironment(connection, transaction, gateway, faults);

        return await RunAsync(environment, request, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<Result<OperationSuccess<OrderState>, RunFailure<OrderState, OrderError>>> RunAsync(
        OrderEnvironment environment,
        PlaceOrderRequest request,
        CancellationToken cancellationToken)
    {
        var state = new OrderState(request);
        var completed = new List<string>();
        var skipped = new List<string>();

        var result = await NamedStep
            .RunAsync("load-customer", environment, state, completed, cancellationToken, OrderOperations.LoadCustomerAsync)
            .ConfigureAwait(false);

        if (!result.TryGetValue(out var next))
        {
            return FailAfterRollback(environment, result);
        }

        state = next;

        result = await NamedStep
            .RunAsync("load-catalog", environment, state, completed, cancellationToken, OrderOperations.LoadCatalogAsync)
            .ConfigureAwait(false);

        if (!result.TryGetValue(out next))
        {
            return FailAfterRollback(environment, result);
        }

        state = next;

        result = await NamedStep
            .RunAsync("create-order", environment, state, completed, cancellationToken, OrderOperations.CreateOrderAsync)
            .ConfigureAwait(false);

        if (!result.TryGetValue(out next))
        {
            return FailAfterRollback(environment, result);
        }

        state = next;

        result = await NamedStep
            .RunAsync("check-credit", environment, state, completed, cancellationToken, OrderOperations.CheckCreditAsync)
            .ConfigureAwait(false);

        if (!result.TryGetValue(out next))
        {
            return FailAfterRollback(environment, result);
        }

        state = next;

        result = await NamedStep
            .RunAsync("reserve-inventory", environment, state, completed, cancellationToken, OrderOperations.ReserveInventoryAsync)
            .ConfigureAwait(false);

        if (!result.TryGetValue(out next))
        {
            return FailAfterRollback(environment, result);
        }

        state = next;

        result = await NamedStep
            .RunAsync("authorize-payment", environment, state, completed, cancellationToken, OrderOperations.AuthorizePaymentAsync)
            .ConfigureAwait(false);

        if (!result.TryGetValue(out next))
        {
            return FailAfterRollback(environment, result);
        }

        state = next;

        result = await NamedStep
            .RunAsync("record-payment", environment, state, completed, cancellationToken, OrderOperations.RecordPaymentAsync)
            .ConfigureAwait(false);

        if (!result.TryGetValue(out next))
        {
            return FailAfterRollback(environment, result);
        }

        state = next;

        result = await NamedStep
            .RunAsync("record-outbox", environment, state, completed, cancellationToken, OrderOperations.RecordOutboxAsync)
            .ConfigureAwait(false);

        if (!result.TryGetValue(out next))
        {
            return FailAfterRollback(environment, result);
        }

        state = next;

        if (cancellationToken.IsCancellationRequested)
        {
            return FailAfterRollback(
                environment,
                AlternativeFailures.Canceled("queue-review", completed, state));
        }

        if (state.TotalCents >= ShopFixtures.ReviewThresholdCents)
        {
            result = await NamedStep
                .RunAsync("queue-review", environment, state, completed, cancellationToken, OrderOperations.QueueReviewAsync)
                .ConfigureAwait(false);

            if (!result.TryGetValue(out next))
            {
                return FailAfterRollback(environment, result);
            }

            state = next;
        }
        else
        {
            skipped.Add("queue-review");
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return FailAfterRollback(
                environment,
                AlternativeFailures.Canceled(null, completed, state));
        }

        try
        {
            environment.Faults.OnCommit();
            environment.Transaction.Commit();
        }
        catch (OperationCanceledException exception)
        {
            return FailCommit(environment, RunFailureKind.Canceled, exception, completed, state);
        }
        catch (Exception exception)
        {
            return FailCommit(environment, RunFailureKind.CommitFailed, exception, completed, state);
        }

        return Result<OperationSuccess<OrderState>, RunFailure<OrderState, OrderError>>.Success(
            new OperationSuccess<OrderState>(state, completed, skipped));
    }

    private static Result<OperationSuccess<OrderState>, RunFailure<OrderState, OrderError>> FailAfterRollback(
        OrderEnvironment environment,
        Result<OrderState, RunFailure<OrderState, OrderError>> failure)
    {
        if (!failure.TryGetError(out var error))
        {
            throw new InvalidOperationException("Expected a failed result.");
        }

        try
        {
            environment.Faults.OnRollback();
            environment.Transaction.Rollback();
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

    private static Result<OperationSuccess<OrderState>, RunFailure<OrderState, OrderError>> FailCommit(
        OrderEnvironment environment,
        RunFailureKind originalKind,
        Exception commitException,
        IReadOnlyList<string> completed,
        OrderState state)
    {
        try
        {
            environment.Faults.OnRollback();
            environment.Transaction.Rollback();
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
