namespace FunnySharp;

/// <summary>
/// Provides focused bridges and asynchronous composition for unit results.
/// </summary>
public static class UnitResultExtensions
{
    /// <summary>
    /// Converts a result to a unit result, discarding the successful value and preserving the failure object.
    /// </summary>
    /// <typeparam name="TValue">The source successful value type.</typeparam>
    /// <typeparam name="TError">The failure value type.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <returns>A successful unit result, or a failure containing the source failure object.</returns>
    /// <exception cref="InvalidOperationException"><paramref name="result"/> is the default value.</exception>
    public static UnitResult<TError> ToUnitResult<TValue, TError>(
        this Result<TValue, TError> result)
    {
        if (result.TryGetError(out var error))
        {
            return UnitResult<TError>.Failure(error!);
        }

        return UnitResult<TError>.Success();
    }

    /// <summary>
    /// Asynchronously converts a result task to a unit result task, discarding the successful value and
    /// preserving the failure object.
    /// </summary>
    /// <typeparam name="TValue">The source successful value type.</typeparam>
    /// <typeparam name="TError">The failure value type.</typeparam>
    /// <param name="result">The result task to convert.</param>
    /// <returns>A task that produces a successful unit result, or a failure containing the source failure object.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="result"/> is null.</exception>
    public static Task<UnitResult<TError>> ToUnitResultAsync<TValue, TError>(
        this Task<Result<TValue, TError>> result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return ToUnitResultAsyncCore(result);
    }

    /// <summary>
    /// Asynchronously converts a result value task to a unit result value task, discarding the successful value
    /// and preserving the failure object.
    /// </summary>
    /// <typeparam name="TValue">The source successful value type.</typeparam>
    /// <typeparam name="TError">The failure value type.</typeparam>
    /// <param name="result">The result value task to convert.</param>
    /// <returns>
    /// A value task that produces a successful unit result, or a failure containing the source failure object.
    /// </returns>
    public static ValueTask<UnitResult<TError>> ToUnitResultAsync<TValue, TError>(
        this ValueTask<Result<TValue, TError>> result) =>
        ToUnitResultValueAsyncCore(result);

    /// <summary>
    /// Converts an option to a unit result with an eager failure value.
    /// </summary>
    /// <typeparam name="TValue">The option value type.</typeparam>
    /// <typeparam name="TError">The failure value type.</typeparam>
    /// <param name="option">The option to convert.</param>
    /// <param name="error">The failure value to use when <paramref name="option"/> is absent.</param>
    /// <returns>Success when the option is present, or a failure containing <paramref name="error"/>.</returns>
    public static UnitResult<TError> ToUnitResult<TValue, TError>(
        this Option<TValue> option,
        TError error) =>
        option.TryGetValue(out _)
            ? UnitResult<TError>.Success()
            : UnitResult<TError>.Failure(error);

    /// <summary>
    /// Converts an option to a unit result with a lazy failure factory.
    /// </summary>
    /// <typeparam name="TValue">The option value type.</typeparam>
    /// <typeparam name="TError">The failure value type.</typeparam>
    /// <param name="option">The option to convert.</param>
    /// <param name="errorFactory">The factory invoked when <paramref name="option"/> is absent.</param>
    /// <returns>Success when the option is present, or a failure containing the factory result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="errorFactory"/> is null.</exception>
    public static UnitResult<TError> ToUnitResult<TValue, TError>(
        this Option<TValue> option,
        Func<TError> errorFactory)
    {
        ArgumentNullException.ThrowIfNull(errorFactory);

        return option.TryGetValue(out _)
            ? UnitResult<TError>.Success()
            : UnitResult<TError>.Failure(errorFactory());
    }

    /// <summary>
    /// Asynchronously transforms a success with a task-returning selector.
    /// </summary>
    /// <typeparam name="TError">The failure value type.</typeparam>
    /// <typeparam name="TResult">The transformed successful value type.</typeparam>
    /// <param name="result">The unit result to transform.</param>
    /// <param name="selector">The asynchronous transformation to invoke for a success.</param>
    /// <returns>A task that produces the transformed success or preserves the existing failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="selector"/> is null.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="result"/> is the default value.</exception>
    public static Task<Result<TResult, TError>> ToResultAsync<TError, TResult>(
        this UnitResult<TError> result,
        Func<Task<TResult>> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        if (result.TryGetError(out var error))
        {
            return Task.FromResult(Result<TResult, TError>.Failure(error!));
        }

        return ToResultAsyncCore<TError, TResult>(selector);
    }

    /// <summary>
    /// Asynchronously transforms a success with a cancellation-aware task selector.
    /// </summary>
    /// <typeparam name="TError">The failure value type.</typeparam>
    /// <typeparam name="TResult">The transformed successful value type.</typeparam>
    /// <param name="result">The unit result to transform.</param>
    /// <param name="selector">The asynchronous transformation to invoke for a success.</param>
    /// <param name="cancellationToken">The token passed unchanged to <paramref name="selector"/> when successful.</param>
    /// <returns>A task that produces the transformed success or preserves the existing failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="selector"/> is null.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="result"/> is the default value.</exception>
    public static Task<Result<TResult, TError>> ToResultAsync<TError, TResult>(
        this UnitResult<TError> result,
        Func<CancellationToken, Task<TResult>> selector,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(selector);

        if (result.TryGetError(out var error))
        {
            return Task.FromResult(Result<TResult, TError>.Failure(error!));
        }

        return ToResultAsyncCore<TError, TResult>(selector, cancellationToken);
    }

    /// <summary>
    /// Asynchronously transforms a success with a value-task-returning selector.
    /// </summary>
    /// <typeparam name="TError">The failure value type.</typeparam>
    /// <typeparam name="TResult">The transformed successful value type.</typeparam>
    /// <param name="result">The unit result to transform.</param>
    /// <param name="selector">The asynchronous transformation to invoke for a success.</param>
    /// <returns>A value task that produces the transformed success or preserves the existing failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="selector"/> is null.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="result"/> is the default value.</exception>
    public static ValueTask<Result<TResult, TError>> ToResultValueAsync<TError, TResult>(
        this UnitResult<TError> result,
        Func<ValueTask<TResult>> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        if (result.TryGetError(out var error))
        {
            return ValueTask.FromResult(Result<TResult, TError>.Failure(error!));
        }

        return ToResultValueAsyncCore<TError, TResult>(selector);
    }

    /// <summary>
    /// Asynchronously transforms a success with a cancellation-aware value-task selector.
    /// </summary>
    /// <typeparam name="TError">The failure value type.</typeparam>
    /// <typeparam name="TResult">The transformed successful value type.</typeparam>
    /// <param name="result">The unit result to transform.</param>
    /// <param name="selector">The asynchronous transformation to invoke for a success.</param>
    /// <param name="cancellationToken">The token passed unchanged to <paramref name="selector"/> when successful.</param>
    /// <returns>A value task that produces the transformed success or preserves the existing failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="selector"/> is null.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="result"/> is the default value.</exception>
    public static ValueTask<Result<TResult, TError>> ToResultValueAsync<TError, TResult>(
        this UnitResult<TError> result,
        Func<CancellationToken, ValueTask<TResult>> selector,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(selector);

        if (result.TryGetError(out var error))
        {
            return ValueTask.FromResult(Result<TResult, TError>.Failure(error!));
        }

        return ToResultValueAsyncCore<TError, TResult>(selector, cancellationToken);
    }

    /// <summary>
    /// Asynchronously binds a success with a task-returning binder.
    /// </summary>
    /// <typeparam name="TError">The failure value type.</typeparam>
    /// <param name="result">The unit result to bind.</param>
    /// <param name="binder">The asynchronous unit-result-producing function to invoke for a success.</param>
    /// <returns>A task that produces the bound unit result or preserves the existing failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="binder"/> is null.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="result"/> is the default value.</exception>
    public static Task<UnitResult<TError>> BindAsync<TError>(
        this UnitResult<TError> result,
        Func<Task<UnitResult<TError>>> binder)
    {
        ArgumentNullException.ThrowIfNull(binder);

        if (result.TryGetError(out var error))
        {
            return Task.FromResult(UnitResult<TError>.Failure(error!));
        }

        return BindAsyncCore(binder);
    }

    /// <summary>
    /// Asynchronously binds a success with a cancellation-aware task binder.
    /// </summary>
    /// <typeparam name="TError">The failure value type.</typeparam>
    /// <param name="result">The unit result to bind.</param>
    /// <param name="binder">The asynchronous unit-result-producing function to invoke for a success.</param>
    /// <param name="cancellationToken">The token passed unchanged to <paramref name="binder"/> when successful.</param>
    /// <returns>A task that produces the bound unit result or preserves the existing failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="binder"/> is null.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="result"/> is the default value.</exception>
    public static Task<UnitResult<TError>> BindAsync<TError>(
        this UnitResult<TError> result,
        Func<CancellationToken, Task<UnitResult<TError>>> binder,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(binder);

        if (result.TryGetError(out var error))
        {
            return Task.FromResult(UnitResult<TError>.Failure(error!));
        }

        return BindAsyncCore(binder, cancellationToken);
    }

    /// <summary>
    /// Asynchronously binds a success with a value-task-returning binder.
    /// </summary>
    /// <typeparam name="TError">The failure value type.</typeparam>
    /// <param name="result">The unit result to bind.</param>
    /// <param name="binder">The asynchronous unit-result-producing function to invoke for a success.</param>
    /// <returns>A value task that produces the bound unit result or preserves the existing failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="binder"/> is null.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="result"/> is the default value.</exception>
    public static ValueTask<UnitResult<TError>> BindValueAsync<TError>(
        this UnitResult<TError> result,
        Func<ValueTask<UnitResult<TError>>> binder)
    {
        ArgumentNullException.ThrowIfNull(binder);

        if (result.TryGetError(out var error))
        {
            return ValueTask.FromResult(UnitResult<TError>.Failure(error!));
        }

        return BindValueAsyncCore(binder);
    }

    /// <summary>
    /// Asynchronously binds a success with a cancellation-aware value-task binder.
    /// </summary>
    /// <typeparam name="TError">The failure value type.</typeparam>
    /// <param name="result">The unit result to bind.</param>
    /// <param name="binder">The asynchronous unit-result-producing function to invoke for a success.</param>
    /// <param name="cancellationToken">The token passed unchanged to <paramref name="binder"/> when successful.</param>
    /// <returns>A value task that produces the bound unit result or preserves the existing failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="binder"/> is null.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="result"/> is the default value.</exception>
    public static ValueTask<UnitResult<TError>> BindValueAsync<TError>(
        this UnitResult<TError> result,
        Func<CancellationToken, ValueTask<UnitResult<TError>>> binder,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(binder);

        if (result.TryGetError(out var error))
        {
            return ValueTask.FromResult(UnitResult<TError>.Failure(error!));
        }

        return BindValueAsyncCore(binder, cancellationToken);
    }

    private static async Task<UnitResult<TError>> ToUnitResultAsyncCore<TValue, TError>(
        Task<Result<TValue, TError>> result) =>
        (await result.ConfigureAwait(false)).ToUnitResult<TValue, TError>();

    private static async ValueTask<UnitResult<TError>> ToUnitResultValueAsyncCore<TValue, TError>(
        ValueTask<Result<TValue, TError>> result) =>
        (await result.ConfigureAwait(false)).ToUnitResult<TValue, TError>();

    private static Task<Result<TResult, TError>> ToResultAsyncCore<TError, TResult>(
        Func<Task<TResult>> selector)
    {
        try
        {
            return Result.TransformTask(
                selector(),
                static value => Result<TResult, TError>.Success(value));
        }
        catch (Exception exception)
        {
            return Result.FromException<Result<TResult, TError>>(exception);
        }
    }

    private static Task<Result<TResult, TError>> ToResultAsyncCore<TError, TResult>(
        Func<CancellationToken, Task<TResult>> selector,
        CancellationToken cancellationToken)
    {
        try
        {
            return Result.TransformTask(
                selector(cancellationToken),
                static value => Result<TResult, TError>.Success(value));
        }
        catch (Exception exception)
        {
            return Result.FromException<Result<TResult, TError>>(exception);
        }
    }

    private static ValueTask<Result<TResult, TError>> ToResultValueAsyncCore<TError, TResult>(
        Func<ValueTask<TResult>> selector)
    {
        try
        {
            return Result.TransformValueTask(
                selector(),
                static value => Result<TResult, TError>.Success(value));
        }
        catch (Exception exception)
        {
            return new ValueTask<Result<TResult, TError>>(
                Result.FromException<Result<TResult, TError>>(exception));
        }
    }

    private static ValueTask<Result<TResult, TError>> ToResultValueAsyncCore<TError, TResult>(
        Func<CancellationToken, ValueTask<TResult>> selector,
        CancellationToken cancellationToken)
    {
        try
        {
            return Result.TransformValueTask(
                selector(cancellationToken),
                static value => Result<TResult, TError>.Success(value));
        }
        catch (Exception exception)
        {
            return new ValueTask<Result<TResult, TError>>(
                Result.FromException<Result<TResult, TError>>(exception));
        }
    }

    private static Task<UnitResult<TError>> BindAsyncCore<TError>(
        Func<Task<UnitResult<TError>>> binder)
    {
        try
        {
            return Result.TransformTask(binder(), static unit => unit);
        }
        catch (Exception exception)
        {
            return Result.FromException<UnitResult<TError>>(exception);
        }
    }

    private static Task<UnitResult<TError>> BindAsyncCore<TError>(
        Func<CancellationToken, Task<UnitResult<TError>>> binder,
        CancellationToken cancellationToken)
    {
        try
        {
            return Result.TransformTask(binder(cancellationToken), static unit => unit);
        }
        catch (Exception exception)
        {
            return Result.FromException<UnitResult<TError>>(exception);
        }
    }

    private static ValueTask<UnitResult<TError>> BindValueAsyncCore<TError>(
        Func<ValueTask<UnitResult<TError>>> binder)
    {
        try
        {
            return Result.TransformValueTask(binder(), static unit => unit);
        }
        catch (Exception exception)
        {
            return new ValueTask<UnitResult<TError>>(
                Result.FromException<UnitResult<TError>>(exception));
        }
    }

    private static ValueTask<UnitResult<TError>> BindValueAsyncCore<TError>(
        Func<CancellationToken, ValueTask<UnitResult<TError>>> binder,
        CancellationToken cancellationToken)
    {
        try
        {
            return Result.TransformValueTask(binder(cancellationToken), static unit => unit);
        }
        catch (Exception exception)
        {
            return new ValueTask<UnitResult<TError>>(
                Result.FromException<UnitResult<TError>>(exception));
        }
    }
}
