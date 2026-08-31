using System.Diagnostics.CodeAnalysis;

namespace FunnySharp;

/// <summary>
/// Provides exception-boundary helpers for creating result values.
/// </summary>
public static class Result
{
    /// <summary>
    /// Invokes an operation and converts a non-cancellation exception to a failed result.
    /// </summary>
    /// <typeparam name="TValue">The successful value type.</typeparam>
    /// <param name="operation">The operation to invoke.</param>
    /// <returns>The operation result or the original exception.</returns>
    public static Result<TValue, Exception> Try<TValue>(Func<TValue> operation) =>
        Try(operation, PreserveException);

    /// <summary>
    /// Invokes an operation and maps a non-cancellation exception to a typed failure.
    /// </summary>
    /// <typeparam name="TValue">The successful value type.</typeparam>
    /// <typeparam name="TError">The failure value type.</typeparam>
    /// <param name="operation">The operation to invoke.</param>
    /// <param name="errorMapper">The explicit exception-to-failure mapping.</param>
    /// <returns>The operation result or a mapped failure.</returns>
    public static Result<TValue, TError> Try<TValue, TError>(
        Func<TValue> operation,
        Func<Exception, TError> errorMapper)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(errorMapper);

        try
        {
            return Result<TValue, TError>.Success(operation());
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return Result<TValue, TError>.Failure(errorMapper(exception));
        }
    }

    /// <summary>
    /// Invokes a task-returning operation and converts a non-cancellation exception to a failed result.
    /// </summary>
    public static Task<Result<TValue, Exception>> TryAsync<TValue>(Func<Task<TValue>> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        return TryAsyncCore(operation, PreserveException);
    }

    /// <summary>
    /// Invokes a task-returning operation and maps a non-cancellation exception to a typed failure.
    /// </summary>
    public static Task<Result<TValue, TError>> TryAsync<TValue, TError>(
        Func<Task<TValue>> operation,
        Func<Exception, TError> errorMapper)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(errorMapper);
        return TryAsyncCore(operation, errorMapper);
    }

    /// <summary>
    /// Invokes a value-task-returning operation and converts a non-cancellation exception to a failed result.
    /// </summary>
    public static ValueTask<Result<TValue, Exception>> TryValueAsync<TValue>(
        Func<ValueTask<TValue>> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        return TryValueAsyncCore(operation, PreserveException);
    }

    /// <summary>
    /// Invokes a value-task-returning operation and maps a non-cancellation exception to a typed failure.
    /// </summary>
    public static ValueTask<Result<TValue, TError>> TryValueAsync<TValue, TError>(
        Func<ValueTask<TValue>> operation,
        Func<Exception, TError> errorMapper)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(errorMapper);
        return TryValueAsyncCore(operation, errorMapper);
    }

    private static Exception PreserveException(Exception exception) => exception;

    private static async Task<Result<TValue, TError>> TryAsyncCore<TValue, TError>(
        Func<Task<TValue>> operation,
        Func<Exception, TError> errorMapper)
    {
        Task<TValue> task;

        try
        {
            task = operation();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return Result<TValue, TError>.Failure(errorMapper(exception));
        }

        if (task is null)
        {
            throw new InvalidOperationException("The operation returned a null task.");
        }

        try
        {
            return Result<TValue, TError>.Success(await task.ConfigureAwait(false));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return Result<TValue, TError>.Failure(errorMapper(exception));
        }
    }

    private static async ValueTask<Result<TValue, TError>> TryValueAsyncCore<TValue, TError>(
        Func<ValueTask<TValue>> operation,
        Func<Exception, TError> errorMapper)
    {
        try
        {
            return Result<TValue, TError>.Success(
                await operation().ConfigureAwait(false));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return Result<TValue, TError>.Failure(errorMapper(exception));
        }
    }
}

/// <summary>
/// Represents either a successful value or a typed failure.
/// </summary>
/// <typeparam name="TValue">The successful value type.</typeparam>
/// <typeparam name="TError">The failure value type.</typeparam>
public readonly struct Result<TValue, TError> : IEquatable<Result<TValue, TError>>
{
    private readonly TValue? value;
    private readonly TError? error;

    private Result(TValue? value, TError? error, bool isSuccess)
    {
        this.value = value;
        this.error = error;
        IsSuccess = isSuccess;
    }

    /// <summary>
    /// Gets a value indicating whether this result is successful.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether this result is a failure.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="value">The successful value.</param>
    /// <returns>A successful result containing <paramref name="value"/>.</returns>
    public static Result<TValue, TError> Success(TValue value) => new(value, default, true);

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="error">The failure value.</param>
    /// <returns>A failed result containing <paramref name="error"/>.</returns>
    public static Result<TValue, TError> Failure(TError error) => new(default, error, false);

    /// <summary>
    /// Attempts to retrieve the successful value.
    /// </summary>
    /// <param name="value">The successful value, or <see langword="default"/> when failed.</param>
    /// <returns><see langword="true"/> when successful; otherwise, <see langword="false"/>.</returns>
    public bool TryGetValue([MaybeNull] out TValue value)
    {
        value = this.value;
        return IsSuccess;
    }

    /// <summary>
    /// Attempts to retrieve the failure value.
    /// </summary>
    /// <param name="error">The failure value, or <see langword="default"/> when successful.</param>
    /// <returns><see langword="true"/> when failed; otherwise, <see langword="false"/>.</returns>
    public bool TryGetError([MaybeNull] out TError error)
    {
        error = this.error;
        return IsFailure;
    }

    /// <summary>
    /// Matches this result and returns the value from the selected branch.
    /// </summary>
    /// <typeparam name="TResult">The branch result type.</typeparam>
    /// <param name="success">The branch invoked with a successful value.</param>
    /// <param name="failure">The branch invoked with a failure value.</param>
    /// <returns>The selected branch result.</returns>
    public TResult Match<TResult>(Func<TValue, TResult> success, Func<TError, TResult> failure)
    {
        ArgumentNullException.ThrowIfNull(success);
        ArgumentNullException.ThrowIfNull(failure);

        return IsSuccess ? success(value!) : failure(error!);
    }

    /// <summary>
    /// Matches this result and invokes the selected branch.
    /// </summary>
    /// <param name="success">The branch invoked with a successful value.</param>
    /// <param name="failure">The branch invoked with a failure value.</param>
    public void Match(Action<TValue> success, Action<TError> failure)
    {
        ArgumentNullException.ThrowIfNull(success);
        ArgumentNullException.ThrowIfNull(failure);

        if (IsSuccess)
        {
            success(value!);
        }
        else
        {
            failure(error!);
        }
    }

    /// <summary>
    /// Transforms a successful value and preserves failure.
    /// </summary>
    /// <typeparam name="TResult">The transformed value type.</typeparam>
    /// <param name="selector">The transformation to apply.</param>
    /// <returns>The transformed result, or the existing failure.</returns>
    public Result<TResult, TError> Map<TResult>(Func<TValue, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        return IsSuccess
            ? Result<TResult, TError>.Success(selector(value!))
            : Result<TResult, TError>.Failure(error!);
    }

    /// <summary>
    /// Binds a successful value to another result and preserves failure.
    /// </summary>
    /// <typeparam name="TResult">The bound value type.</typeparam>
    /// <param name="binder">The result-returning function to apply.</param>
    /// <returns>The bound result, or the existing failure.</returns>
    public Result<TResult, TError> Bind<TResult>(Func<TValue, Result<TResult, TError>> binder)
    {
        ArgumentNullException.ThrowIfNull(binder);

        return IsSuccess
            ? binder(value!)
            : Result<TResult, TError>.Failure(error!);
    }

    /// <summary>
    /// Transforms a failure value and preserves success.
    /// </summary>
    /// <typeparam name="TResultError">The transformed failure type.</typeparam>
    /// <param name="selector">The failure transformation to apply.</param>
    /// <returns>The transformed failure, or the existing success.</returns>
    public Result<TValue, TResultError> MapError<TResultError>(Func<TError, TResultError> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        return IsSuccess
            ? Result<TValue, TResultError>.Success(value!)
            : Result<TValue, TResultError>.Failure(selector(error!));
    }

    /// <summary>
    /// Keeps a successful value only when it satisfies <paramref name="predicate"/>.
    /// </summary>
    /// <param name="predicate">The predicate to evaluate.</param>
    /// <param name="error">The failure returned when the predicate is false.</param>
    /// <returns>This result when already failed or when the value matches; otherwise, a new failure.</returns>
    public Result<TValue, TError> Ensure(Func<TValue, bool> predicate, TError error)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        return IsFailure || predicate(value!) ? this : Failure(error);
    }

    /// <summary>
    /// Keeps a successful value only when it satisfies <paramref name="predicate"/>.
    /// </summary>
    /// <param name="predicate">The predicate to evaluate.</param>
    /// <param name="errorFactory">The failure factory invoked for an unsuccessful validation.</param>
    /// <returns>This result when already failed or when the value matches; otherwise, a new failure.</returns>
    public Result<TValue, TError> Ensure(
        Func<TValue, bool> predicate,
        Func<TValue, TError> errorFactory)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(errorFactory);

        if (IsFailure || predicate(value!))
        {
            return this;
        }

        return Failure(errorFactory(value!));
    }

    /// <summary>
    /// Recovers a failure by producing a successful value.
    /// </summary>
    /// <param name="recovery">The recovery function.</param>
    /// <returns>This result when successful; otherwise, the recovered success.</returns>
    public Result<TValue, TError> Recover(Func<TError, TValue> recovery)
    {
        ArgumentNullException.ThrowIfNull(recovery);

        return IsSuccess ? this : Success(recovery(error!));
    }

    /// <summary>
    /// Recovers a failure by producing another result.
    /// </summary>
    /// <param name="recovery">The result-returning recovery function.</param>
    /// <returns>This result when successful; otherwise, the recovery result.</returns>
    public Result<TValue, TError> RecoverWith(Func<TError, Result<TValue, TError>> recovery)
    {
        ArgumentNullException.ThrowIfNull(recovery);

        return IsSuccess ? this : recovery(error!);
    }

    /// <summary>
    /// Combines this result with another result, returning the first failure.
    /// </summary>
    /// <typeparam name="TSecond">The second successful value type.</typeparam>
    /// <param name="second">The result to combine with.</param>
    /// <returns>Both successful values, or the first failure in left-to-right order.</returns>
    public Result<(TValue First, TSecond Second), TError> Zip<TSecond>(
        Result<TSecond, TError> second)
    {
        if (IsFailure)
        {
            return Result<(TValue First, TSecond Second), TError>.Failure(error!);
        }

        if (second.TryGetValue(out var secondValue))
        {
            return Result<(TValue First, TSecond Second), TError>.Success((value!, secondValue!));
        }

        second.TryGetError(out var secondError);
        return Result<(TValue First, TSecond Second), TError>.Failure(secondError!);
    }

    /// <summary>
    /// Lazily combines this result with another result, skipping the factory after a failure.
    /// </summary>
    /// <typeparam name="TSecond">The second successful value type.</typeparam>
    /// <param name="secondFactory">The result factory to invoke after success.</param>
    /// <returns>Both successful values, or the first failure in left-to-right order.</returns>
    public Result<(TValue First, TSecond Second), TError> ZipWith<TSecond>(
        Func<Result<TSecond, TError>> secondFactory)
    {
        ArgumentNullException.ThrowIfNull(secondFactory);

        return IsSuccess
            ? Zip(secondFactory())
            : Result<(TValue First, TSecond Second), TError>.Failure(error!);
    }

    /// <summary>
    /// Projects a successful value for LINQ query syntax.
    /// </summary>
    public Result<TResult, TError> Select<TResult>(Func<TValue, TResult> selector) => Map(selector);

    /// <summary>
    /// Binds and projects successful values for LINQ query syntax.
    /// </summary>
    public Result<TResult, TError> SelectMany<TIntermediate, TResult>(
        Func<TValue, Result<TIntermediate, TError>> binder,
        Func<TValue, TIntermediate, TResult> projector)
    {
        ArgumentNullException.ThrowIfNull(binder);
        ArgumentNullException.ThrowIfNull(projector);

        if (IsFailure)
        {
            return Result<TResult, TError>.Failure(error!);
        }

        var intermediate = binder(value!);
        if (intermediate.TryGetValue(out var intermediateValue))
        {
            return Result<TResult, TError>.Success(projector(value!, intermediateValue!));
        }

        intermediate.TryGetError(out var intermediateError);
        return Result<TResult, TError>.Failure(intermediateError!);
    }

    /// <inheritdoc />
    public bool Equals(Result<TValue, TError> other) =>
        IsSuccess == other.IsSuccess &&
        (IsSuccess
            ? EqualityComparer<TValue>.Default.Equals(value!, other.value!)
            : EqualityComparer<TError>.Default.Equals(error!, other.error!));

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Result<TValue, TError> other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() =>
        IsSuccess
            ? HashCode.Combine(true, EqualityComparer<TValue>.Default.GetHashCode(value!))
            : HashCode.Combine(false, EqualityComparer<TError>.Default.GetHashCode(error!));

    /// <summary>
    /// Determines whether two results are equal.
    /// </summary>
    public static bool operator ==(
        Result<TValue, TError> left,
        Result<TValue, TError> right) =>
        left.Equals(right);

    /// <summary>
    /// Determines whether two results are unequal.
    /// </summary>
    public static bool operator !=(
        Result<TValue, TError> left,
        Result<TValue, TError> right) =>
        !left.Equals(right);

    /// <inheritdoc />
    public override string ToString() => IsSuccess ? $"Success({value})" : $"Failure({error})";
}
