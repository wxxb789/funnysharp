using FunnySharp;
using FunnySharp.OperationPlans;

using Microsoft.Data.Sqlite;

namespace FunnySharp.TestSupport.OperationComparison;

internal interface IOrderConsumer
{
    string Name { get; }

    IReadOnlyList<OperationInfo> Describe { get; }

    Task<Result<OperationSuccess<OrderState>, RunFailure<OrderState, OrderError>>> PlaceOrderAsync(
        SqliteConnection connection,
        PlaceOrderRequest request,
        SimulatedPaymentGateway gateway,
        FaultInjector faults,
        CancellationToken cancellationToken);
}
