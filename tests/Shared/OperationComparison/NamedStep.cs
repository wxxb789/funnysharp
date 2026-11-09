using FunnySharp;
using FunnySharp.OperationPlans;

namespace FunnySharp.TestSupport.OperationComparison;

internal static class NamedStep
{
    internal static async Task<Result<OrderState, RunFailure<OrderState, OrderError>>> RunAsync<TEnvironment>(
        string name,
        TEnvironment environment,
        OrderState state,
        List<string> completed,
        CancellationToken cancellationToken,
        Func<TEnvironment, OrderState, CancellationToken, Task<Result<OrderState, OrderError>>> step)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return AlternativeFailures.Canceled(name, completed, state);
        }

        try
        {
            var task = step(environment, state, cancellationToken);

            if (task is null)
            {
                throw new InvalidOperationException($"Operation '{name}' returned a null task.");
            }

            var result = await task.ConfigureAwait(false);

            if (result.TryGetValue(out var next))
            {
                completed.Add(name);
                return Result<OrderState, RunFailure<OrderState, OrderError>>.Success(next);
            }

            result.TryGetError(out var error);
            return AlternativeFailures.Failed(name, error, completed, state);
        }
        catch (OperationCanceledException exception)
        {
            return AlternativeFailures.Canceled(name, completed, state, exception);
        }
        catch (Exception exception)
        {
            return AlternativeFailures.Faulted(name, exception, completed, state);
        }
    }
}

internal static class AlternativeFailures
{
    internal static Result<OrderState, RunFailure<OrderState, OrderError>> Failed(
        string operation,
        OrderError? error,
        IReadOnlyList<string> completed,
        OrderState state) =>
        Result<OrderState, RunFailure<OrderState, OrderError>>.Failure(
            new RunFailure<OrderState, OrderError>(
                RunFailureKind.OperationFailed,
                null,
                operation,
                error,
                null,
                null,
                completed,
                state));

    internal static Result<OrderState, RunFailure<OrderState, OrderError>> Faulted(
        string operation,
        Exception exception,
        IReadOnlyList<string> completed,
        OrderState state) =>
        Result<OrderState, RunFailure<OrderState, OrderError>>.Failure(
            new RunFailure<OrderState, OrderError>(
                RunFailureKind.OperationFaulted,
                null,
                operation,
                default,
                exception,
                null,
                completed,
                state));

    internal static Result<OrderState, RunFailure<OrderState, OrderError>> Canceled(
        string? operation,
        IReadOnlyList<string> completed,
        OrderState state,
        Exception? exception = null) =>
        Result<OrderState, RunFailure<OrderState, OrderError>>.Failure(
            CanceledFailure(operation, completed, state, exception));

    internal static RunFailure<OrderState, OrderError> CanceledFailure(
        string? operation,
        IReadOnlyList<string> completed,
        OrderState state,
        Exception? exception = null) =>
        new(
            RunFailureKind.Canceled,
            null,
            operation,
            default,
            exception,
            null,
            completed,
            state);

    internal static RunFailure<OrderState, OrderError> CommitFailed(
        Exception exception,
        IReadOnlyList<string> completed,
        OrderState state) =>
        new(
            RunFailureKind.CommitFailed,
            null,
            null,
            default,
            exception,
            null,
            completed,
            state);

    internal static RunFailure<OrderState, OrderError> RollbackFailed(
        RunFailureKind originalKind,
        Exception? exception,
        Exception rollbackException,
        string? operation,
        OrderError? error,
        IReadOnlyList<string> completed,
        OrderState state) =>
        new(
            RunFailureKind.RollbackFailed,
            originalKind,
            operation,
            error,
            exception,
            rollbackException,
            completed,
            state);
}
