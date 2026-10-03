namespace FunnySharp;

/// <summary>
/// Provides applicative operations for validations.
/// </summary>
public static class ValidationExtensions
{
    /// <summary>
    /// Applies a valid function to a valid argument, accumulating errors from the function before errors
    /// from the argument.
    /// </summary>
    /// <typeparam name="TValue">The argument value type.</typeparam>
    /// <typeparam name="TResult">The result value type.</typeparam>
    /// <typeparam name="TError">The validation error type.</typeparam>
    /// <param name="function">The validation containing the function.</param>
    /// <param name="argument">The validation containing the argument.</param>
    /// <returns>
    /// A valid result when both validations are valid; otherwise, an invalid validation containing all
    /// errors in function-then-argument order.
    /// </returns>
    /// <remarks>
    /// When both operands are valid, the function payload is invoked once without a null-payload
    /// guard. An invalid operand skips invocation. Callback exceptions propagate unchanged.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Either operand is uninitialized.</exception>
    /// <exception cref="NullReferenceException">Both operands are valid and the function payload is null.</exception>
    public static Validation<TResult, TError> Apply<TValue, TResult, TError>(
        this Validation<Func<TValue, TResult>, TError> function,
        Validation<TValue, TError> argument) =>
        function.Zip(argument).Map(pair => pair.First(pair.Second));

    /// <summary>
    /// Asynchronously transforms a valid value with a task-returning selector and preserves validation errors.
    /// </summary>
    /// <typeparam name="TValue">The valid value type.</typeparam>
    /// <typeparam name="TResult">The transformed valid value type.</typeparam>
    /// <typeparam name="TError">The validation error type.</typeparam>
    /// <param name="validation">The validation to transform.</param>
    /// <param name="selector">The asynchronous transformation to invoke for a valid value.</param>
    /// <returns>A task that produces the transformed validation or preserves the existing errors.</returns>
    /// <remarks>
    /// The callback is guarded synchronously even when its branch will not invoke it.
    /// The callback is invoked once only for the successful branch; failure skips it. Its operation
    /// is consumed once. Synchronous callback exceptions are captured in the returned operation,
    /// and a synchronously thrown OperationCanceledException produces cancellation. A faulted callback
    /// operation remains faulted, including a faulted OperationCanceledException; a canceled callback
    /// operation remains canceled. These exceptions are not converted to typed carrier failures.
    /// A selected callback returning a null Task faults with NullReferenceException.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="selector"/> is null.</exception>
    /// <exception cref="InvalidOperationException">This validation is uninitialized.</exception>
    public static Task<Validation<TResult, TError>> MapAsync<TValue, TResult, TError>(
        this Validation<TValue, TError> validation,
        Func<TValue, Task<TResult>> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        if (validation.TryGetValue(out var value))
        {
            return MapAsyncCore<TValue, TResult, TError>(value!, selector);
        }

        validation.TryGetErrors(out var errors);
        return Task.FromResult(Validation<TResult, TError>.InvalidFromOwnedErrors(errors!));
    }

    /// <summary>
    /// Asynchronously transforms a valid value with a cancellation-aware task-returning selector and preserves
    /// validation errors.
    /// </summary>
    /// <typeparam name="TValue">The valid value type.</typeparam>
    /// <typeparam name="TResult">The transformed valid value type.</typeparam>
    /// <typeparam name="TError">The validation error type.</typeparam>
    /// <param name="validation">The validation to transform.</param>
    /// <param name="selector">The asynchronous transformation to invoke for a valid value.</param>
    /// <param name="cancellationToken">The token passed unchanged to <paramref name="selector"/> when the validation is valid.</param>
    /// <returns>A task that produces the transformed validation or preserves the existing errors.</returns>
    /// <remarks>
    /// The callback is guarded synchronously even when its branch will not invoke it.
    /// The callback is invoked once only for the successful branch; failure skips it. Its operation
    /// is consumed once. Synchronous callback exceptions are captured in the returned operation,
    /// and a synchronously thrown OperationCanceledException produces cancellation. A faulted callback
    /// operation remains faulted, including a faulted OperationCanceledException; a canceled callback
    /// operation remains canceled. These exceptions are not converted to typed carrier failures.
    /// The token is forwarded unchanged to the selected callback, not checked independently.
    /// A selected callback returning a null Task faults with NullReferenceException.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="selector"/> is null.</exception>
    /// <exception cref="InvalidOperationException">This validation is uninitialized.</exception>
    public static Task<Validation<TResult, TError>> MapAsync<TValue, TResult, TError>(
        this Validation<TValue, TError> validation,
        Func<TValue, CancellationToken, Task<TResult>> selector,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(selector);

        if (validation.TryGetValue(out var value))
        {
            return MapAsyncCore<TValue, TResult, TError>(value!, selector, cancellationToken);
        }

        validation.TryGetErrors(out var errors);
        return Task.FromResult(Validation<TResult, TError>.InvalidFromOwnedErrors(errors!));
    }

    /// <summary>
    /// Asynchronously transforms a valid value with a value-task-returning selector and preserves validation
    /// errors.
    /// </summary>
    /// <typeparam name="TValue">The valid value type.</typeparam>
    /// <typeparam name="TResult">The transformed valid value type.</typeparam>
    /// <typeparam name="TError">The validation error type.</typeparam>
    /// <param name="validation">The validation to transform.</param>
    /// <param name="selector">The asynchronous transformation to invoke for a valid value.</param>
    /// <returns>A value task that produces the transformed validation or preserves the existing errors.</returns>
    /// <remarks>
    /// The callback is guarded synchronously even when its branch will not invoke it.
    /// The callback is invoked once only for the successful branch; failure skips it. Its operation
    /// is consumed once. Synchronous callback exceptions are captured in the returned operation,
    /// and a synchronously thrown OperationCanceledException produces cancellation. A faulted callback
    /// operation remains faulted, including a faulted OperationCanceledException; a canceled callback
    /// operation remains canceled. These exceptions are not converted to typed carrier failures.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="selector"/> is null.</exception>
    /// <exception cref="InvalidOperationException">This validation is uninitialized.</exception>
    public static ValueTask<Validation<TResult, TError>> MapValueAsync<TValue, TResult, TError>(
        this Validation<TValue, TError> validation,
        Func<TValue, ValueTask<TResult>> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        if (validation.TryGetValue(out var value))
        {
            return MapValueAsyncCore<TValue, TResult, TError>(value!, selector);
        }

        validation.TryGetErrors(out var errors);
        return ValueTask.FromResult(
            Validation<TResult, TError>.InvalidFromOwnedErrors(errors!));
    }

    /// <summary>
    /// Asynchronously transforms a valid value with a cancellation-aware value-task-returning selector and
    /// preserves validation errors.
    /// </summary>
    /// <typeparam name="TValue">The valid value type.</typeparam>
    /// <typeparam name="TResult">The transformed valid value type.</typeparam>
    /// <typeparam name="TError">The validation error type.</typeparam>
    /// <param name="validation">The validation to transform.</param>
    /// <param name="selector">The asynchronous transformation to invoke for a valid value.</param>
    /// <param name="cancellationToken">The token passed unchanged to <paramref name="selector"/> when the validation is valid.</param>
    /// <returns>A value task that produces the transformed validation or preserves the existing errors.</returns>
    /// <remarks>
    /// The callback is guarded synchronously even when its branch will not invoke it.
    /// The callback is invoked once only for the successful branch; failure skips it. Its operation
    /// is consumed once. Synchronous callback exceptions are captured in the returned operation,
    /// and a synchronously thrown OperationCanceledException produces cancellation. A faulted callback
    /// operation remains faulted, including a faulted OperationCanceledException; a canceled callback
    /// operation remains canceled. These exceptions are not converted to typed carrier failures.
    /// The token is forwarded unchanged to the selected callback, not checked independently.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="selector"/> is null.</exception>
    /// <exception cref="InvalidOperationException">This validation is uninitialized.</exception>
    public static ValueTask<Validation<TResult, TError>> MapValueAsync<TValue, TResult, TError>(
        this Validation<TValue, TError> validation,
        Func<TValue, CancellationToken, ValueTask<TResult>> selector,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(selector);

        if (validation.TryGetValue(out var value))
        {
            return MapValueAsyncCore<TValue, TResult, TError>(
                value!,
                selector,
                cancellationToken);
        }

        validation.TryGetErrors(out var errors);
        return ValueTask.FromResult(
            Validation<TResult, TError>.InvalidFromOwnedErrors(errors!));
    }

    private static Task<Validation<TResult, TError>> MapAsyncCore<TValue, TResult, TError>(
        TValue value,
        Func<TValue, Task<TResult>> selector)
    {
        try
        {
            return Result.TransformTask(
                selector(value),
                static result => Validation<TResult, TError>.Valid(result));
        }
        catch (Exception exception)
        {
            return Result.FromException<Validation<TResult, TError>>(exception);
        }
    }

    private static Task<Validation<TResult, TError>> MapAsyncCore<TValue, TResult, TError>(
        TValue value,
        Func<TValue, CancellationToken, Task<TResult>> selector,
        CancellationToken cancellationToken)
    {
        try
        {
            return Result.TransformTask(
                selector(value, cancellationToken),
                static result => Validation<TResult, TError>.Valid(result));
        }
        catch (Exception exception)
        {
            return Result.FromException<Validation<TResult, TError>>(exception);
        }
    }

    private static ValueTask<Validation<TResult, TError>> MapValueAsyncCore<TValue, TResult, TError>(
        TValue value,
        Func<TValue, ValueTask<TResult>> selector)
    {
        try
        {
            return Result.TransformValueTask(
                selector(value),
                static result => Validation<TResult, TError>.Valid(result));
        }
        catch (Exception exception)
        {
            return new ValueTask<Validation<TResult, TError>>(
                Result.FromException<Validation<TResult, TError>>(exception));
        }
    }

    private static ValueTask<Validation<TResult, TError>> MapValueAsyncCore<TValue, TResult, TError>(
        TValue value,
        Func<TValue, CancellationToken, ValueTask<TResult>> selector,
        CancellationToken cancellationToken)
    {
        try
        {
            return Result.TransformValueTask(
                selector(value, cancellationToken),
                static result => Validation<TResult, TError>.Valid(result));
        }
        catch (Exception exception)
        {
            return new ValueTask<Validation<TResult, TError>>(
                Result.FromException<Validation<TResult, TError>>(exception));
        }
    }
}
