namespace FunnySharp;

/// <summary>
/// Provides asynchronous composition and focused bridges for result values.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Converts an option to a result with an eager failure value.
    /// </summary>
    /// <typeparam name="TValue">The option value type.</typeparam>
    /// <typeparam name="TError">The failure value type.</typeparam>
    /// <param name="option">The option to convert.</param>
    /// <param name="error">The failure value to use when <paramref name="option"/> is absent.</param>
    /// <returns>A success containing the present option value, or a failure containing <paramref name="error"/>.</returns>
    public static Result<TValue, TError> ToResult<TValue, TError>(
        this Option<TValue> option,
        TError error) =>
        option.TryGetValue(out var value)
            ? Result<TValue, TError>.Success(value)
            : Result<TValue, TError>.Failure(error);

    /// <summary>
    /// Converts an option to a result with a lazy failure factory.
    /// </summary>
    /// <typeparam name="TValue">The option value type.</typeparam>
    /// <typeparam name="TError">The failure value type.</typeparam>
    /// <param name="option">The option to convert.</param>
    /// <param name="errorFactory">The factory invoked when <paramref name="option"/> is absent.</param>
    /// <returns>A success containing the present option value, or a failure containing the factory result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="errorFactory"/> is null.</exception>
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
    /// <typeparam name="TValue">The successful value type.</typeparam>
    /// <typeparam name="TError">The failure value type.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <returns>An option containing a non-null successful value, or <c>None</c> for a failure or a null successful value.</returns>
    public static Option<TValue> ToOption<TValue, TError>(
        this Result<TValue, TError> result) =>
        result.TryGetValue(out var value)
            ? Option<TValue>.FromNullable(value)
            : Option<TValue>.None;

    /// <summary>
    /// Asynchronously transforms a successful value with a task-returning selector.
    /// </summary>
    /// <typeparam name="TValue">The successful value type.</typeparam>
    /// <typeparam name="TError">The failure value type.</typeparam>
    /// <typeparam name="TResult">The transformed successful value type.</typeparam>
    /// <param name="result">The result to transform.</param>
    /// <param name="selector">The asynchronous transformation to invoke for a success.</param>
    /// <returns>A task that produces the transformed success or preserves the existing failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="selector"/> is null.</exception>
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
    /// <typeparam name="TValue">The successful value type.</typeparam>
    /// <typeparam name="TError">The failure value type.</typeparam>
    /// <typeparam name="TResult">The transformed successful value type.</typeparam>
    /// <param name="result">The result to transform.</param>
    /// <param name="selector">The asynchronous transformation to invoke for a success.</param>
    /// <param name="cancellationToken">The token passed unchanged to <paramref name="selector"/> when the result is successful.</param>
    /// <returns>A task that produces the transformed success or preserves the existing failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="selector"/> is null.</exception>
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
    /// <typeparam name="TValue">The successful value type.</typeparam>
    /// <typeparam name="TError">The failure value type.</typeparam>
    /// <typeparam name="TResult">The bound successful value type.</typeparam>
    /// <param name="result">The result to bind.</param>
    /// <param name="binder">The asynchronous result-producing function to invoke for a success.</param>
    /// <returns>A task that produces the bound result or preserves the existing failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="binder"/> is null.</exception>
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
    /// <typeparam name="TValue">The successful value type.</typeparam>
    /// <typeparam name="TError">The failure value type.</typeparam>
    /// <typeparam name="TResult">The bound successful value type.</typeparam>
    /// <param name="result">The result to bind.</param>
    /// <param name="binder">The asynchronous result-producing function to invoke for a success.</param>
    /// <param name="cancellationToken">The token passed unchanged to <paramref name="binder"/> when the result is successful.</param>
    /// <returns>A task that produces the bound result or preserves the existing failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="binder"/> is null.</exception>
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
    /// <typeparam name="TValue">The successful value type.</typeparam>
    /// <typeparam name="TError">The failure value type.</typeparam>
    /// <typeparam name="TResult">The transformed successful value type.</typeparam>
    /// <param name="result">The result to transform.</param>
    /// <param name="selector">The asynchronous transformation to invoke for a success.</param>
    /// <returns>A value task that produces the transformed success or preserves the existing failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="selector"/> is null.</exception>
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
    /// <typeparam name="TValue">The successful value type.</typeparam>
    /// <typeparam name="TError">The failure value type.</typeparam>
    /// <typeparam name="TResult">The transformed successful value type.</typeparam>
    /// <param name="result">The result to transform.</param>
    /// <param name="selector">The asynchronous transformation to invoke for a success.</param>
    /// <param name="cancellationToken">The token passed unchanged to <paramref name="selector"/> when the result is successful.</param>
    /// <returns>A value task that produces the transformed success or preserves the existing failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="selector"/> is null.</exception>
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
    /// <typeparam name="TValue">The successful value type.</typeparam>
    /// <typeparam name="TError">The failure value type.</typeparam>
    /// <typeparam name="TResult">The bound successful value type.</typeparam>
    /// <param name="result">The result to bind.</param>
    /// <param name="binder">The asynchronous result-producing function to invoke for a success.</param>
    /// <returns>A value task that produces the bound result or preserves the existing failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="binder"/> is null.</exception>
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
    /// <typeparam name="TValue">The successful value type.</typeparam>
    /// <typeparam name="TError">The failure value type.</typeparam>
    /// <typeparam name="TResult">The bound successful value type.</typeparam>
    /// <param name="result">The result to bind.</param>
    /// <param name="binder">The asynchronous result-producing function to invoke for a success.</param>
    /// <param name="cancellationToken">The token passed unchanged to <paramref name="binder"/> when the result is successful.</param>
    /// <returns>A value task that produces the bound result or preserves the existing failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="binder"/> is null.</exception>
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
