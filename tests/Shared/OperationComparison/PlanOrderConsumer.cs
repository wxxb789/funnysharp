using FunnySharp;
using FunnySharp.OperationPlans;

using Microsoft.Data.Sqlite;

namespace FunnySharp.TestSupport.OperationComparison;

internal sealed class PlanOrderConsumer : IOrderConsumer
{
    private static readonly OperationPlan<OrderEnvironment, OrderState, OrderError> s_plan = CreatePlan();

    internal static OperationPlan<OrderEnvironment, OrderState, OrderError> CreatePlan() =>
        OperationPlan<OrderEnvironment, OrderState, OrderError>.Create(
        [
            new OperationStep<OrderEnvironment, OrderState, OrderError>(
                Info(
                    "load-customer",
                    OperationKind.Read,
                    Array.Empty<string>(),
                    requiresPriorResult: false,
                    dynamicWork: false,
                    conditional: false,
                    "Reads the customer by request id; missing or inactive is a domain failure."),
                OrderOperations.LoadCustomerAsync),
            new OperationStep<OrderEnvironment, OrderState, OrderError>(
                Info(
                    "load-catalog",
                    OperationKind.Read,
                    Array.Empty<string>(),
                    requiresPriorResult: false,
                    dynamicWork: false,
                    conditional: false,
                    "Reads active catalog prices for the requested SKUs."),
                OrderOperations.LoadCatalogAsync),
            new OperationStep<OrderEnvironment, OrderState, OrderError>(
                Info(
                    "create-order",
                    OperationKind.Write,
                    new[] { "load-customer", "load-catalog" },
                    requiresPriorResult: true,
                    dynamicWork: true,
                    conditional: false,
                    "Computes the total, inserts the order, and inserts one line per requested SKU."),
                OrderOperations.CreateOrderAsync),
            new OperationStep<OrderEnvironment, OrderState, OrderError>(
                Info(
                    "check-credit",
                    OperationKind.Read,
                    new[] { "load-customer", "create-order" },
                    requiresPriorResult: true,
                    dynamicWork: false,
                    conditional: false,
                    "Compares the computed total with the loaded credit limit."),
                OrderOperations.CheckCreditAsync),
            new OperationStep<OrderEnvironment, OrderState, OrderError>(
                Info(
                    "reserve-inventory",
                    OperationKind.Write,
                    new[] { "create-order", "load-catalog" },
                    requiresPriorResult: true,
                    dynamicWork: true,
                    conditional: false,
                    "Runs one conditional update per priced line; stored availability decides success."),
                OrderOperations.ReserveInventoryAsync),
            new OperationStep<OrderEnvironment, OrderState, OrderError>(
                Info(
                    "authorize-payment",
                    OperationKind.External,
                    new[] { "create-order" },
                    requiresPriorResult: true,
                    dynamicWork: false,
                    conditional: false,
                    "Calls the simulated gateway; the gateway ledger is outside the transaction."),
                OrderOperations.AuthorizePaymentAsync),
            new OperationStep<OrderEnvironment, OrderState, OrderError>(
                Info(
                    "record-payment",
                    OperationKind.Write,
                    new[] { "create-order", "authorize-payment" },
                    requiresPriorResult: true,
                    dynamicWork: false,
                    conditional: false,
                    "Inserts the payment row with the gateway reference."),
                OrderOperations.RecordPaymentAsync),
            new OperationStep<OrderEnvironment, OrderState, OrderError>(
                Info(
                    "record-outbox",
                    OperationKind.Write,
                    new[] { "create-order", "authorize-payment" },
                    requiresPriorResult: true,
                    dynamicWork: false,
                    conditional: false,
                    "Inserts the transactional integration event; delivery is not performed."),
                OrderOperations.RecordOutboxAsync),
            new OperationStep<OrderEnvironment, OrderState, OrderError>(
                Info(
                    "queue-review",
                    OperationKind.Write,
                    new[] { "create-order" },
                    requiresPriorResult: true,
                    dynamicWork: false,
                    conditional: true,
                    "Runs only when the computed total reaches the review threshold."),
                OrderOperations.QueueReviewAsync,
                static state => state.TotalCents >= ShopFixtures.ReviewThresholdCents),
        ]);

    public string Name => "Typed plan";

    public IReadOnlyList<OperationInfo> Describe => s_plan.Describe;

    public async Task<Result<OperationSuccess<OrderState>, RunFailure<OrderState, OrderError>>> PlaceOrderAsync(
        SqliteConnection connection,
        PlaceOrderRequest request,
        SimulatedPaymentGateway gateway,
        FaultInjector faults,
        CancellationToken cancellationToken)
    {
        using var transaction = connection.BeginTransaction(deferred: false);
        var environment = new OrderEnvironment(connection, transaction, gateway, faults);
        var adapter = new SqlitePlanTransaction(transaction, faults);

        return await s_plan
            .RunAsync(environment, new OrderState(request), adapter, cancellationToken)
            .ConfigureAwait(false);
    }

    private static OperationInfo Info(
        string name,
        OperationKind kind,
        IReadOnlyList<string> dependsOn,
        bool requiresPriorResult,
        bool dynamicWork,
        bool conditional,
        string note) =>
        new(name, kind, dependsOn, requiresPriorResult, dynamicWork, conditional, note);
}
