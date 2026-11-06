using FunnySharp;

namespace FunnySharp.TestSupport.Checks;

internal sealed class CheckRuleSet
{
    private readonly Action<string>? onInvoked;

    internal CheckRuleSet(Action<string>? onInvoked = null)
    {
        this.onInvoked = onInvoked;
    }

    internal UnitResult<CheckError> HasStock(CheckOrder order)
    {
        onInvoked?.Invoke("stock");
        return order.Quantity <= 8
            ? UnitResult<CheckError>.Success()
            : UnitResult<CheckError>.Failure(
                new CheckError("out-of-stock", $"{order.Sku}|{order.Quantity}"));
    }

    internal UnitResult<CheckError> IsPermitted(CheckOrder order)
    {
        onInvoked?.Invoke("permission");
        return order.CustomerId.StartsWith("CUS-", StringComparison.Ordinal)
            ? UnitResult<CheckError>.Success()
            : UnitResult<CheckError>.Failure(
                new CheckError("permission-denied", order.CustomerId));
    }

    internal UnitResult<CheckError> NotFraudulent(CheckOrder order)
    {
        onInvoked?.Invoke("fraud");
        return order.Token != "tok-blocked"
            ? UnitResult<CheckError>.Success()
            : UnitResult<CheckError>.Failure(
                new CheckError("fraud-suspected", order.Token));
    }

    internal Task<UnitResult<CheckError>> HasStockAsync(CheckOrder order) =>
        Task.FromResult(HasStock(order));

    internal Task<UnitResult<CheckError>> IsPermittedAsync(CheckOrder order) =>
        Task.FromResult(IsPermitted(order));

    internal Task<UnitResult<CheckError>> NotFraudulentAsync(CheckOrder order) =>
        Task.FromResult(NotFraudulent(order));

    internal ValueTask<UnitResult<CheckError>> HasStockValueAsync(CheckOrder order) =>
        ValueTask.FromResult(HasStock(order));

    internal ValueTask<UnitResult<CheckError>> IsPermittedValueAsync(CheckOrder order) =>
        ValueTask.FromResult(IsPermitted(order));

    internal ValueTask<UnitResult<CheckError>> NotFraudulentValueAsync(CheckOrder order) =>
        ValueTask.FromResult(NotFraudulent(order));

    internal Func<CheckOrder, UnitResult<CheckError>>[] SyncRules() =>
        [HasStock, IsPermitted, NotFraudulent];

    internal Func<CheckOrder, Task<UnitResult<CheckError>>>[] TaskRules() =>
        [HasStockAsync, IsPermittedAsync, NotFraudulentAsync];

    internal Func<CheckOrder, ValueTask<UnitResult<CheckError>>>[] ValueRules() =>
        [HasStockValueAsync, IsPermittedValueAsync, NotFraudulentValueAsync];
}
