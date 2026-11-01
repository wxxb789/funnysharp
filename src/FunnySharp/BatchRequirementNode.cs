using System.Runtime.ExceptionServices;

namespace FunnySharp;

internal abstract class BatchRequirementNode<TValue, TError>
{
    internal abstract ValueTask<Result<TValue, BatchReadError<TError>>> EvaluateAsync(
        BatchExecution execution,
        CancellationToken cancellationToken);
}

internal sealed class BatchValueNode<TValue, TError>(TValue value) : BatchRequirementNode<TValue, TError>
{
    internal override ValueTask<Result<TValue, BatchReadError<TError>>> EvaluateAsync(
        BatchExecution execution,
        CancellationToken cancellationToken) =>
        new(Result<TValue, BatchReadError<TError>>.Success(value));
}

internal sealed class BatchFailureNode<TValue, TError>(BatchReadError<TError> error)
    : BatchRequirementNode<TValue, TError>
{
    internal override ValueTask<Result<TValue, BatchReadError<TError>>> EvaluateAsync(
        BatchExecution execution,
        CancellationToken cancellationToken) =>
        new(Result<TValue, BatchReadError<TError>>.Failure(error));
}

internal sealed class BatchReadNode<TKey, TValue, TError>(
    BatchSource<TKey, TValue, TError> source,
    TKey key) : BatchRequirementNode<TValue, TError>
    where TKey : notnull
{
    internal override ValueTask<Result<TValue, BatchReadError<TError>>> EvaluateAsync(
        BatchExecution execution,
        CancellationToken cancellationToken) =>
        execution.ReadAsync(source, key, cancellationToken);
}

internal sealed class BatchMapNode<TValue, TResult, TError>(
    BatchRequirementNode<TValue, TError> inner,
    Func<TValue, TResult> selector) : BatchRequirementNode<TResult, TError>
{
    internal override async ValueTask<Result<TResult, BatchReadError<TError>>> EvaluateAsync(
        BatchExecution execution,
        CancellationToken cancellationToken) =>
        (await inner.EvaluateAsync(execution, cancellationToken).ConfigureAwait(false)).Map(selector);
}

internal sealed class BatchBindNode<TValue, TResult, TError>(
    BatchRequirementNode<TValue, TError> inner,
    Func<TValue, BatchRequirement<TResult, TError>> binder) : BatchRequirementNode<TResult, TError>
{
    internal override async ValueTask<Result<TResult, BatchReadError<TError>>> EvaluateAsync(
        BatchExecution execution,
        CancellationToken cancellationToken)
    {
        var result = await inner.EvaluateAsync(execution, cancellationToken).ConfigureAwait(false);
        if (!result.TryGetValue(out var value))
        {
            result.TryGetError(out var error);
            return Result<TResult, BatchReadError<TError>>.Failure(error!);
        }

        var next = binder(value)
            ?? throw new InvalidOperationException("The batch requirement binder returned null.");
        return await next.EvaluateAsync(execution, cancellationToken).ConfigureAwait(false);
    }
}

internal sealed class BatchAllNode<TValue, TError>(
    BatchRequirement<TValue, TError>[] requirements) : BatchRequirementNode<IReadOnlyList<Result<TValue, BatchReadError<TError>>>, TError>
{
    internal override async ValueTask<Result<IReadOnlyList<Result<TValue, BatchReadError<TError>>>, BatchReadError<TError>>>
        EvaluateAsync(BatchExecution execution, CancellationToken cancellationToken)
    {
        var tasks = new ValueTask<Result<TValue, BatchReadError<TError>>>[requirements.Length];
        for (var index = 0; index < requirements.Length; index++)
        {
            tasks[index] = requirements[index].EvaluateAsync(execution, cancellationToken);
        }

        var results = new Result<TValue, BatchReadError<TError>>[requirements.Length];
        Exception? firstException = null;
        for (var index = 0; index < tasks.Length; index++)
        {
            try
            {
                results[index] = await tasks[index].ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                firstException ??= exception;
            }
        }

        if (firstException is not null)
        {
            ExceptionDispatchInfo.Throw(firstException);
        }

        return Result<IReadOnlyList<Result<TValue, BatchReadError<TError>>>, BatchReadError<TError>>.Success(results);
    }
}

internal sealed class BatchZipNode<TFirst, TSecond, TError>(
    BatchRequirement<TFirst, TError> first,
    BatchRequirement<TSecond, TError> second) : BatchRequirementNode<(TFirst First, TSecond Second), TError>
{
    internal override async ValueTask<Result<(TFirst First, TSecond Second), BatchReadError<TError>>>
        EvaluateAsync(BatchExecution execution, CancellationToken cancellationToken)
    {
        var firstTask = first.EvaluateAsync(execution, cancellationToken);
        var secondTask = second.EvaluateAsync(execution, cancellationToken);

        Result<TFirst, BatchReadError<TError>> firstResult = default;
        Exception? firstException = null;
        try
        {
            firstResult = await firstTask.ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            firstException = exception;
        }

        Result<TSecond, BatchReadError<TError>> secondResult = default;
        Exception? secondException = null;
        try
        {
            secondResult = await secondTask.ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            secondException = exception;
        }

        if (firstException is not null)
        {
            ExceptionDispatchInfo.Capture(firstException).Throw();
        }

        if (secondException is not null)
        {
            ExceptionDispatchInfo.Capture(secondException).Throw();
        }

        return firstResult.Zip(secondResult);
    }
}
