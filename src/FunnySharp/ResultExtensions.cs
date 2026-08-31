namespace FunnySharp;

/// <summary>
/// Provides asynchronous composition and focused bridges for result values.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Converts an option to a result with an eager failure value.
    /// </summary>
    public static Result<TValue, TError> ToResult<TValue, TError>(
        this Option<TValue> option,
        TError error) =>
        option.TryGetValue(out var value)
            ? Result<TValue, TError>.Success(value)
            : Result<TValue, TError>.Failure(error);

    /// <summary>
    /// Converts an option to a result with a lazy failure factory.
    /// </summary>
    public static Result<TValue, TError> ToResult<TValue, TError>(
        this Option<TValue> option,
        Func<TError> errorFactory)
    {
        ArgumentNullException.ThrowIfNull(errorFactory);

        return option.TryGetValue(out var value)
            ? Result<TValue, TError>.Success(value)
            : Result<TValue, TError>.Failure(errorFactory());
    }

    /// <summary>
    /// Converts a successful result to an option and discards failure details.
    /// </summary>
    public static Option<TValue> ToOption<TValue, TError>(
        this Result<TValue, TError> result) =>
        result.TryGetValue(out var value)
            ? Option<TValue>.FromNullable(value)
            : Option<TValue>.None;

    /// <summary>
    /// Asynchronously transforms a successful value with a task-returning selector.
    /// </summary>
    public static Task<Result<TResult, TError>> MapAsync<TValue, TError, TResult>(
        this Result<TValue, TError> result,
        Func<TValue, Task<TResult>> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        if (result.TryGetValue(out var value))
        {
            return MapAsyncCore<TValue, TError, TResult>(value!, selector);
        }

        result.TryGetError(out var error);
        return Task.FromResult(Result<TResult, TError>.Failure(error!));
    }

    /// <summary>
    /// Asynchronously transforms a successful value with a cancellation-aware task selector.
    /// </summary>
    public static Task<Result<TResult, TError>> MapAsync<TValue, TError, TResult>(
        this Result<TValue, TError> result,
        Func<TValue, CancellationToken, Task<TResult>> selector,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(selector);

        if (result.TryGetValue(out var value))
        {
            return MapAsyncCore<TValue, TError, TResult>(value!, selector, cancellationToken);
        }

        result.TryGetError(out var error);
        return Task.FromResult(Result<TResult, TError>.Failure(error!));
    }

    /// <summary>
    /// Asynchronously binds a successful value with a task-returning binder.
    /// </summary>
    public static Task<Result<TResult, TError>> BindAsync<TValue, TError, TResult>(
        this Result<TValue, TError> result,
        Func<TValue, Task<Result<TResult, TError>>> binder)
    {
        ArgumentNullException.ThrowIfNull(binder);

        if (result.TryGetValue(out var value))
        {
            return BindAsyncCore(value!, binder);
        }

        result.TryGetError(out var error);
        return Task.FromResult(Result<TResult, TError>.Failure(error!));
    }

    /// <summary>
    /// Asynchronously binds a successful value with a cancellation-aware task binder.
    /// </summary>
    public static Task<Result<TResult, TError>> BindAsync<TValue, TError, TResult>(
        this Result<TValue, TError> result,
        Func<TValue, CancellationToken, Task<Result<TResult, TError>>> binder,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(binder);

        if (result.TryGetValue(out var value))
        {
            return BindAsyncCore(value!, binder, cancellationToken);
        }

        result.TryGetError(out var error);
        return Task.FromResult(Result<TResult, TError>.Failure(error!));
    }

    /// <summary>
    /// Asynchronously transforms a successful value with a value-task-returning selector.
    /// </summary>
    public static ValueTask<Result<TResult, TError>> MapValueAsync<TValue, TError, TResult>(
        this Result<TValue, TError> result,
        Func<TValue, ValueTask<TResult>> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        if (result.TryGetValue(out var value))
        {
            return MapValueAsyncCore<TValue, TError, TResult>(value!, selector);
        }

        result.TryGetError(out var error);
        return ValueTask.FromResult(Result<TResult, TError>.Failure(error!));
    }

    /// <summary>
    /// Asynchronously transforms a successful value with a cancellation-aware value-task selector.
    /// </summary>
    public static ValueTask<Result<TResult, TError>> MapValueAsync<TValue, TError, TResult>(
        this Result<TValue, TError> result,
        Func<TValue, CancellationToken, ValueTask<TResult>> selector,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(selector);

        if (result.TryGetValue(out var value))
        {
            return MapValueAsyncCore<TValue, TError, TResult>(
                value!,
                selector,
                cancellationToken);
        }

        result.TryGetError(out var error);
        return ValueTask.FromResult(Result<TResult, TError>.Failure(error!));
    }

    /// <summary>
    /// Asynchronously binds a successful value with a value-task-returning binder.
    /// </summary>
    public static ValueTask<Result<TResult, TError>> BindValueAsync<TValue, TError, TResult>(
        this Result<TValue, TError> result,
        Func<TValue, ValueTask<Result<TResult, TError>>> binder)
    {
        ArgumentNullException.ThrowIfNull(binder);

        if (result.TryGetValue(out var value))
        {
            return BindValueAsyncCore(value!, binder);
        }

        result.TryGetError(out var error);
        return ValueTask.FromResult(Result<TResult, TError>.Failure(error!));
    }

    /// <summary>
    /// Asynchronously binds a successful value with a cancellation-aware value-task binder.
    /// </summary>
    public static ValueTask<Result<TResult, TError>> BindValueAsync<TValue, TError, TResult>(
        this Result<TValue, TError> result,
        Func<TValue, CancellationToken, ValueTask<Result<TResult, TError>>> binder,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(binder);

        if (result.TryGetValue(out var value))
        {
            return BindValueAsyncCore(value!, binder, cancellationToken);
        }

        result.TryGetError(out var error);
        return ValueTask.FromResult(Result<TResult, TError>.Failure(error!));
    }

    private static Task<Result<TResult, TError>> MapAsyncCore<TValue, TError, TResult>(
        TValue value,
        Func<TValue, Task<TResult>> selector)
    {
        try
        {
            return Result.TransformTask(
                selector(value),
                static result => Result<TResult, TError>.Success(result));
        }
        catch (Exception exception)
        {
            return Result.FromException<Result<TResult, TError>>(exception);
        }
    }

    private static Task<Result<TResult, TError>> MapAsyncCore<TValue, TError, TResult>(
        TValue value,
        Func<TValue, CancellationToken, Task<TResult>> selector,
        CancellationToken cancellationToken)
    {
        try
        {
            return Result.TransformTask(
                selector(value, cancellationToken),
                static result => Result<TResult, TError>.Success(result));
        }
        catch (Exception exception)
        {
            return Result.FromException<Result<TResult, TError>>(exception);
        }
    }

    private static Task<Result<TResult, TError>> BindAsyncCore<TValue, TError, TResult>(
        TValue value,
        Func<TValue, Task<Result<TResult, TError>>> binder)
    {
        try
        {
            return Result.TransformTask(
                binder(value),
                static result => result);
        }
        catch (Exception exception)
        {
            return Result.FromException<Result<TResult, TError>>(exception);
        }
    }

    private static Task<Result<TResult, TError>> BindAsyncCore<TValue, TError, TResult>(
        TValue value,
        Func<TValue, CancellationToken, Task<Result<TResult, TError>>> binder,
        CancellationToken cancellationToken)
    {
        try
        {
            return Result.TransformTask(
                binder(value, cancellationToken),
                static result => result);
        }
        catch (Exception exception)
        {
            return Result.FromException<Result<TResult, TError>>(exception);
        }
    }

    private static ValueTask<Result<TResult, TError>> MapValueAsyncCore<TValue, TError, TResult>(
        TValue value,
        Func<TValue, ValueTask<TResult>> selector)
    {
        try
        {
            return Result.TransformValueTask(
                selector(value),
                static result => Result<TResult, TError>.Success(result));
        }
        catch (Exception exception)
        {
            return new ValueTask<Result<TResult, TError>>(
                Result.FromException<Result<TResult, TError>>(exception));
        }
    }

    private static ValueTask<Result<TResult, TError>> MapValueAsyncCore<TValue, TError, TResult>(
        TValue value,
        Func<TValue, CancellationToken, ValueTask<TResult>> selector,
        CancellationToken cancellationToken)
    {
        try
        {
            return Result.TransformValueTask(
                selector(value, cancellationToken),
                static result => Result<TResult, TError>.Success(result));
        }
        catch (Exception exception)
        {
            return new ValueTask<Result<TResult, TError>>(
                Result.FromException<Result<TResult, TError>>(exception));
        }
    }

    private static ValueTask<Result<TResult, TError>> BindValueAsyncCore<TValue, TError, TResult>(
        TValue value,
        Func<TValue, ValueTask<Result<TResult, TError>>> binder)
    {
        try
        {
            return Result.TransformValueTask(
                binder(value),
                static result => result);
        }
        catch (Exception exception)
        {
            return new ValueTask<Result<TResult, TError>>(
                Result.FromException<Result<TResult, TError>>(exception));
        }
    }

    private static ValueTask<Result<TResult, TError>> BindValueAsyncCore<TValue, TError, TResult>(
        TValue value,
        Func<TValue, CancellationToken, ValueTask<Result<TResult, TError>>> binder,
        CancellationToken cancellationToken)
    {
        try
        {
            return Result.TransformValueTask(
                binder(value, cancellationToken),
                static result => result);
        }
        catch (Exception exception)
        {
            return new ValueTask<Result<TResult, TError>>(
                Result.FromException<Result<TResult, TError>>(exception));
        }
    }
}
