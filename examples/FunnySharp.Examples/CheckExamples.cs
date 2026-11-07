using FunnySharp;

internal static class CheckExamples
{
    internal static void Verify()
    {
        var order = new ExampleOrder("ORD-100", 2, "CUS-100", false);
        var result = Result<ExampleOrder, ExampleError>.Success(order);
        var existing = new ExampleError("existing", "the order was already rejected");

        var retained = result.Check(HasStock);
        var deniedOrder = order with { CustomerId = "BLOCKED" };
        var rejected = Result<ExampleOrder, ExampleError>
            .Success(deniedOrder)
            .Check(IsPermitted);

        ExampleAssertions.True(
            retained.TryGetValue(out var retainedOrder) && ReferenceEquals(order, retainedOrder),
            "Check must retain the exact successful order.");
        ExampleAssertions.True(
            rejected.TryGetError(out var rejectedError),
            "A failed check must replace the success.");
        ExampleAssertions.Equal(
            new ExampleError("permission-denied", "BLOCKED"),
            rejectedError!);

        var failingOrder = new ExampleOrder("ORD-200", 9, "BLOCKED", true);
        var invalid = Result<ExampleOrder, ExampleError>
            .Success(failingOrder)
            .CheckAll([HasStock, IsPermitted, IsNotBlocked]);
        var valid = result.CheckAll([HasStock, IsPermitted]);

        ExampleAssertions.True(invalid.TryGetErrors(out var errors), "The accumulating checks must fail.");
        ExampleAssertions.SequenceEqual(
            [
                new ExampleError("out-of-stock", "ORD-200|9"),
                new ExampleError("permission-denied", "BLOCKED"),
                new ExampleError("fraud", "blocked"),
            ],
            errors!);
        ExampleAssertions.True(
            valid.TryGetValue(out var validOrder) && ReferenceEquals(order, validOrder),
            "A valid CheckAll must retain the exact order.");

        var calls = 0;
        var failedInput = Result<ExampleOrder, ExampleError>.Failure(existing);
        var failedInputValidation = failedInput.CheckAll(
        [
            _ =>
            {
                calls++;
                return UnitResult<ExampleError>.Success();
            },
        ]);
        var empty = result.CheckAll(Array.Empty<Func<ExampleOrder, UnitResult<ExampleError>>>());

        ExampleAssertions.True(
            failedInputValidation.TryGetErrors(out var failedErrors),
            "An already-failed input must remain invalid.");
        ExampleAssertions.SequenceEqual([existing], failedErrors!);
        ExampleAssertions.Equal(0, calls);
        ExampleAssertions.True(empty.IsValid, "An empty rule set must keep the value.");
    }

    internal static async Task VerifyAsync()
    {
        var order = new ExampleOrder("ORD-100", 2, "CUS-100", false);
        var result = Result<ExampleOrder, ExampleError>.Success(order);

        var taskRetained = await result.CheckAsync(HasStockAsync);
        var valueRetained = await result.CheckValueAsync(HasStockValueAsync);
        var taskValidation = await result.CheckAllAsync([HasStockAsync, IsPermittedAsync, IsNotBlockedAsync]);
        var valueValidation = await result.CheckAllValueAsync(
            [HasStockValueAsync, IsPermittedValueAsync, IsNotBlockedValueAsync]);

        ExampleAssertions.True(
            taskRetained.TryGetValue(out var taskOrder) && ReferenceEquals(order, taskOrder),
            "The asynchronous check must retain the exact order.");
        ExampleAssertions.True(
            valueRetained.TryGetValue(out var valueOrder) && ReferenceEquals(order, valueOrder),
            "The value-task check must retain the exact order.");
        ExampleAssertions.True(taskValidation.IsValid, "The asynchronous accumulating checks must pass.");
        ExampleAssertions.True(valueValidation.IsValid, "The value-task accumulating checks must pass.");

        using var cancellationSource = new CancellationTokenSource();
        var observedToken = default(CancellationToken);
        var tokenRetained = await result.CheckAsync(
            (_, token) =>
            {
                observedToken = token;
                return Task.FromResult(UnitResult<ExampleError>.Success());
            },
            cancellationSource.Token);

        ExampleAssertions.True(tokenRetained.IsSuccess, "The cancellation-aware check must succeed.");
        ExampleAssertions.Equal(cancellationSource.Token, observedToken);

        var failing = new ExampleOrder("ORD-200", 2, "BLOCKED", true);
        var failingValidation = await Result<ExampleOrder, ExampleError>
            .Success(failing)
            .CheckAllAsync([HasStockAsync, IsPermittedAsync, IsNotBlockedAsync]);

        ExampleAssertions.True(
            failingValidation.TryGetErrors(out var errors),
            "The checks must fail.");
        ExampleAssertions.SequenceEqual(
            [new ExampleError("permission-denied", "BLOCKED"), new ExampleError("fraud", "blocked")],
            errors!);
    }

    private static UnitResult<ExampleError> HasStock(ExampleOrder order) =>
        order.Quantity <= 8
            ? UnitResult<ExampleError>.Success()
            : UnitResult<ExampleError>.Failure(
                new ExampleError("out-of-stock", $"{order.OrderId}|{order.Quantity}"));

    private static UnitResult<ExampleError> IsPermitted(ExampleOrder order) =>
        order.CustomerId.StartsWith("CUS-", StringComparison.Ordinal)
            ? UnitResult<ExampleError>.Success()
            : UnitResult<ExampleError>.Failure(
                new ExampleError("permission-denied", order.CustomerId));

    private static UnitResult<ExampleError> IsNotBlocked(ExampleOrder order) =>
        !order.Blocked
            ? UnitResult<ExampleError>.Success()
            : UnitResult<ExampleError>.Failure(new ExampleError("fraud", "blocked"));

    private static Task<UnitResult<ExampleError>> HasStockAsync(ExampleOrder order) =>
        Task.FromResult(HasStock(order));

    private static Task<UnitResult<ExampleError>> IsPermittedAsync(ExampleOrder order) =>
        Task.FromResult(IsPermitted(order));

    private static Task<UnitResult<ExampleError>> IsNotBlockedAsync(ExampleOrder order) =>
        Task.FromResult(IsNotBlocked(order));

    private static ValueTask<UnitResult<ExampleError>> HasStockValueAsync(ExampleOrder order) =>
        ValueTask.FromResult(HasStock(order));

    private static ValueTask<UnitResult<ExampleError>> IsPermittedValueAsync(ExampleOrder order) =>
        ValueTask.FromResult(IsPermitted(order));

    private static ValueTask<UnitResult<ExampleError>> IsNotBlockedValueAsync(ExampleOrder order) =>
        ValueTask.FromResult(IsNotBlocked(order));

    private sealed record ExampleOrder(
        string OrderId,
        int Quantity,
        string CustomerId,
        bool Blocked);

    private sealed record ExampleError(string Code, string Detail);
}
