using System.Diagnostics.CodeAnalysis;

using System.Runtime.ExceptionServices;

namespace FunnySharp;

/// <summary>
/// Provides exception-boundary helpers for creating unit result values.
/// </summary>
public static class UnitResult
{
    private const string NullTaskMessage = "The operation returned a null task.";

    /// <summary>
    /// Creates a successful unit result.
    /// </summary>
    /// <typeparam name="TError">The failure value type.</typeparam>
    /// <returns>A successful unit result.</returns>
    public static UnitResult<TError> Success<TError>() => UnitResult<TError>.Success();

    /// <summary>
    /// Creates a failed unit result.
    /// </summary>
    /// <typeparam name="TError">The failure value type.</typeparam>
    /// <param name="error">The failure value.</param>
    /// <returns>A failed unit result containing <paramref name="error"/>.</returns>
    public static UnitResult<TError> Failure<TError>(TError error) => UnitResult<TError>.Failure(error);

    /// <summary>
    /// Invokes an operation and converts a non-cancellation exception to a failed unit result.
    /// </summary>
    /// <param name="operation">The operation to invoke.</param>
    /// <returns>A successful unit result or a failure containing the original exception.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="operation"/> is null.</exception>
    public static UnitResult<Exception> Try(Action operation) =>
        Try(operation, PreserveException);

    /// <summary>
    /// Invokes an operation and maps a non-cancellation exception to a typed unit failure.
    /// </summary>
    /// <typeparam name="TError">The failure value type.</typeparam>
    /// <param name="operation">The operation to invoke.</param>
    /// <param name="errorMapper">The explicit exception-to-failure mapping.</param>
    /// <returns>A successful unit result or a mapped failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="operation"/> or <paramref name="errorMapper"/> is null.</exception>
    public static UnitResult<TError> Try<TError>(
        Action operation,
        Func<Exception, TError> errorMapper)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(errorMapper);

        try
        {
            operation();
            return UnitResult<TError>.Success();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return UnitResult<TError>.Failure(errorMapper(exception));
        }
    }

    /// <summary>
    /// Invokes a task-returning operation and converts a non-cancellation exception to a failed unit result.
    /// </summary>
    /// <param name="operation">The tokenless asynchronous operation to invoke.</param>
    /// <returns>A task that produces a successful unit result or a failure containing the original exception.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="operation"/> is null.</exception>
    /// <remarks>
    /// The tokenless delegate is intentional: this exception boundary does not own cancellation. Callers that need
    /// cancellation must capture and pass their <see cref="CancellationToken"/> inside <paramref name="operation"/>.
    /// Cancellation remains cancellation of the returned task, and a faulted task containing an
    /// <see cref="OperationCanceledException"/> remains faulted. Neither is converted to a failure.
    /// A null task returned by <paramref name="operation"/> faults the returned task with
    /// <see cref="InvalidOperationException"/>.
    /// </remarks>
    public static Task<UnitResult<Exception>> TryAsync(Func<Task> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        return TryAsyncCore(operation, PreserveException);
    }

    /// <summary>
    /// Invokes a task-returning operation and maps a non-cancellation exception to a typed unit failure.
    /// </summary>
    /// <typeparam name="TError">The failure value type.</typeparam>
    /// <param name="operation">The tokenless asynchronous operation to invoke.</param>
    /// <param name="errorMapper">The exception-to-failure mapping for non-cancellation exceptions.</param>
    /// <returns>A task that produces a successful unit result or a mapped failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="operation"/> or <paramref name="errorMapper"/> is null.</exception>
    /// <remarks>
    /// The tokenless delegate is intentional: this exception boundary does not own cancellation. Callers that need
    /// cancellation must capture and pass their <see cref="CancellationToken"/> inside <paramref name="operation"/>.
    /// Cancellation remains cancellation of the returned task, and a faulted task containing an
    /// <see cref="OperationCanceledException"/> remains faulted. Neither is converted to a failure or sent to
    /// <paramref name="errorMapper"/>.
    /// A null task returned by <paramref name="operation"/> faults the returned task with
    /// <see cref="InvalidOperationException"/> and is not sent to <paramref name="errorMapper"/>.
    /// </remarks>
    public static Task<UnitResult<TError>> TryAsync<TError>(
        Func<Task> operation,
        Func<Exception, TError> errorMapper)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(errorMapper);
        return TryAsyncCore(operation, errorMapper);
    }

    /// <summary>
    /// Invokes a value-task-returning operation and converts a non-cancellation exception to a failed unit result.
    /// </summary>
    /// <param name="operation">The tokenless asynchronous operation to invoke.</param>
    /// <returns>A value task that produces a successful unit result or a failure containing the original exception.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="operation"/> is null.</exception>
    /// <remarks>
    /// The tokenless delegate is intentional: this exception boundary does not own cancellation. Callers that need
    /// cancellation must capture and pass their <see cref="CancellationToken"/> inside <paramref name="operation"/>.
    /// Cancellation remains cancellation of the returned value task, and a faulted value task containing an
    /// <see cref="OperationCanceledException"/> remains faulted. Neither is converted to a failure.
    /// The operation result is observed once; callers must follow the normal single-consumption rule for
    /// <c>ValueTask</c>.
    /// </remarks>
    public static ValueTask<UnitResult<Exception>> TryValueAsync(Func<ValueTask> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        return TryValueAsyncCore(operation, PreserveException);
    }

    /// <summary>
    /// Invokes a value-task-returning operation and maps a non-cancellation exception to a typed unit failure.
    /// </summary>
    /// <typeparam name="TError">The failure value type.</typeparam>
    /// <param name="operation">The tokenless asynchronous operation to invoke.</param>
    /// <param name="errorMapper">The exception-to-failure mapping for non-cancellation exceptions.</param>
    /// <returns>A value task that produces a successful unit result or a mapped failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="operation"/> or <paramref name="errorMapper"/> is null.</exception>
    /// <remarks>
    /// The tokenless delegate is intentional: this exception boundary does not own cancellation. Callers that need
    /// cancellation must capture and pass their <see cref="CancellationToken"/> inside <paramref name="operation"/>.
    /// Cancellation remains cancellation of the returned value task, and a faulted value task containing an
    /// <see cref="OperationCanceledException"/> remains faulted. Neither is converted to a failure or sent to
    /// <paramref name="errorMapper"/>.
    /// The operation result is observed once; callers must follow the normal single-consumption rule for
    /// <c>ValueTask</c>.
    /// </remarks>
    public static ValueTask<UnitResult<TError>> TryValueAsync<TError>(
        Func<ValueTask> operation,
        Func<Exception, TError> errorMapper)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(errorMapper);
        return TryValueAsyncCore(operation, errorMapper);
    }

    private static Exception PreserveException(Exception exception) => exception;

    private static Task<UnitResult<TError>> TryAsyncCore<TError>(
        Func<Task> operation,
        Func<Exception, TError> errorMapper)
    {
        Task task;

        try
        {
            task = operation();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return MapException(exception, errorMapper);
        }
        catch (OperationCanceledException exception)
        {
            return Result.FromException<UnitResult<TError>>(exception);
        }

        if (task is null)
        {
            return Task.FromException<UnitResult<TError>>(
                new InvalidOperationException(NullTaskMessage));
        }

        return TransformTask(
            task,
            static () => UnitResult<TError>.Success(),
            exception => UnitResult<TError>.Failure(errorMapper(exception)));
    }

    private static ValueTask<UnitResult<TError>> TryValueAsyncCore<TError>(
        Func<ValueTask> operation,
        Func<Exception, TError> errorMapper)
    {
        try
        {
            return TransformValueTask(
                operation(),
                static () => UnitResult<TError>.Success(),
                exception => UnitResult<TError>.Failure(errorMapper(exception)));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return new ValueTask<UnitResult<TError>>(MapException(exception, errorMapper));
        }
        catch (OperationCanceledException exception)
        {
            return new ValueTask<UnitResult<TError>>(
                Result.FromException<UnitResult<TError>>(exception));
        }
    }

    private static Task<UnitResult<TError>> MapException<TError>(
        Exception exception,
        Func<Exception, TError> errorMapper)
    {
        try
        {
            return Task.FromResult(UnitResult<TError>.Failure(errorMapper(exception)));
        }
        catch (Exception mapperException)
        {
            return Result.FromException<UnitResult<TError>>(mapperException);
        }
    }

    private static Task<TResult> TransformTask<TResult>(
        Task task,
        Func<TResult> success,
        Func<Exception, TResult> fault)
    {
        if (task.IsCompletedSuccessfully)
        {
            try
            {
                return Task.FromResult(success());
            }
            catch (Exception exception)
            {
                return Result.FromException<TResult>(exception);
            }
        }

        var completion = new TaskCompletionSource<TResult>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        if (task.IsCompleted)
        {
            CompleteTask(task, completion, success, fault);
        }
        else
        {
            _ = task.ContinueWith(
                completed => CompleteTask(completed, completion, success, fault),
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }

        return completion.Task;
    }

    private static ValueTask<TResult> TransformValueTask<TResult>(
        ValueTask task,
        Func<TResult> success,
        Func<Exception, TResult> fault)
    {
        if (task.IsCompletedSuccessfully)
        {
            try
            {
                return ValueTask.FromResult(success());
            }
            catch (Exception exception)
            {
                return new ValueTask<TResult>(Result.FromException<TResult>(exception));
            }
        }

        return new ValueTask<TResult>(TransformTask(task.AsTask(), success, fault));
    }

    private static void CompleteTask<TResult>(
        Task task,
        TaskCompletionSource<TResult> completion,
        Func<TResult> success,
        Func<Exception, TResult> fault)
    {
        if (task.IsCompletedSuccessfully)
        {
            CompleteResult(completion, success);
            return;
        }

        if (task.IsCanceled)
        {
            completion.TrySetFromTask(CreateCanceledTask<TResult>(GetCancellationException(task)));
            return;
        }

        try
        {
            task.GetAwaiter().GetResult();
        }
        catch (OperationCanceledException cancellation)
        {
            completion.TrySetException(cancellation);
        }
        catch (Exception exception)
        {
            CompleteResult(completion, () => fault(exception));
        }
    }

    private static void CompleteResult<TResult>(
        TaskCompletionSource<TResult> completion,
        Func<TResult> resultFactory)
    {
        try
        {
            completion.TrySetResult(resultFactory());
        }
        catch (Exception exception) when (exception is OperationCanceledException cancellation)
        {
            completion.TrySetFromTask(CreateCanceledTask<TResult>(cancellation));
        }
        catch (Exception exception)
        {
            completion.TrySetException(exception);
        }
    }

    private static OperationCanceledException GetCancellationException(Task task)
    {
        try
        {
            task.GetAwaiter().GetResult();
        }
        catch (OperationCanceledException cancellation)
        {
            return cancellation;
        }

        throw new InvalidOperationException("The task was expected to be canceled.");
    }

    private static async Task<TResult> CreateCanceledTask<TResult>(OperationCanceledException cancellation)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        ExceptionDispatchInfo.Capture(cancellation).Throw();
        return default!;
    }
}

/// <summary>
/// Represents an outcome that either succeeds without a produced value or fails with a typed error.
/// </summary>
/// <typeparam name="TError">The failure value type.</typeparam>
/// <remarks>
/// The default value is uninitialized. Every member that reads the case or payload throws
/// <see cref="InvalidOperationException"/>; only <see cref="ToString"/> returns diagnostic text for it.
/// </remarks>
public readonly struct UnitResult<TError> : IEquatable<UnitResult<TError>>
{
    private const byte UninitializedState = 0;
    private const byte SuccessState = 1;
    private const byte FailureState = 2;
    private const string UninitializedMessage = "The unit result has not been initialized.";

    private readonly byte state;
    private readonly TError? error;

    private UnitResult(byte state, TError? error)
    {
        this.state = state;
        this.error = error;
    }

    /// <summary>
    /// Gets a value indicating whether this unit result is successful.
    /// </summary>
    /// <exception cref="InvalidOperationException">This unit result is the default value.</exception>
    public bool IsSuccess
    {
        get
        {
            ThrowIfUninitialized();
            return state == SuccessState;
        }
    }

    /// <summary>
    /// Gets a value indicating whether this unit result is a failure.
    /// </summary>
    /// <exception cref="InvalidOperationException">This unit result is the default value.</exception>
    public bool IsFailure
    {
        get
        {
            ThrowIfUninitialized();
            return state == FailureState;
        }
    }

    /// <summary>
    /// Creates a successful unit result.
    /// </summary>
    /// <returns>A successful unit result.</returns>
    public static UnitResult<TError> Success() => new(SuccessState, default);

    /// <summary>
    /// Creates a failed unit result.
    /// </summary>
    /// <param name="error">The failure value.</param>
    /// <returns>A failed unit result containing <paramref name="error"/>.</returns>
    public static UnitResult<TError> Failure(TError error) => new(FailureState, error);

    /// <summary>
    /// Attempts to retrieve the failure value.
    /// </summary>
    /// <param name="error">The failure value, or <see langword="default"/> when successful.</param>
    /// <returns><see langword="true"/> when failed; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="InvalidOperationException">This unit result is the default value.</exception>
    public bool TryGetError([MaybeNull] out TError error)
    {
        ThrowIfUninitialized();
        error = this.error;
        return state == FailureState;
    }

    /// <summary>
    /// Matches this unit result and returns the value from the selected branch.
    /// </summary>
    /// <typeparam name="TResult">The branch result type.</typeparam>
    /// <param name="success">The branch invoked for a success.</param>
    /// <param name="failure">The branch invoked with the failure value.</param>
    /// <returns>The selected branch result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="success"/> or <paramref name="failure"/> is null.</exception>
    /// <exception cref="InvalidOperationException">This unit result is the default value.</exception>
    public TResult Match<TResult>(Func<TResult> success, Func<TError, TResult> failure)
    {
        ArgumentNullException.ThrowIfNull(success);
        ArgumentNullException.ThrowIfNull(failure);
        ThrowIfUninitialized();

        return state == SuccessState ? success() : failure(error!);
    }

    /// <summary>
    /// Matches this unit result and invokes the selected branch.
    /// </summary>
    /// <param name="success">The branch invoked for a success.</param>
    /// <param name="failure">The branch invoked with the failure value.</param>
    /// <exception cref="ArgumentNullException"><paramref name="success"/> or <paramref name="failure"/> is null.</exception>
    /// <exception cref="InvalidOperationException">This unit result is the default value.</exception>
    public void Match(Action success, Action<TError> failure)
    {
        ArgumentNullException.ThrowIfNull(success);
        ArgumentNullException.ThrowIfNull(failure);
        ThrowIfUninitialized();

        if (state == SuccessState)
        {
            success();
        }
        else
        {
            failure(error!);
        }
    }

    /// <summary>
    /// Transforms a failure value and preserves success.
    /// </summary>
    /// <typeparam name="TResultError">The transformed failure type.</typeparam>
    /// <param name="selector">The failure transformation to apply.</param>
    /// <returns>The transformed failure, or the existing success.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="selector"/> is null.</exception>
    /// <exception cref="InvalidOperationException">This unit result is the default value.</exception>
    public UnitResult<TResultError> MapError<TResultError>(Func<TError, TResultError> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        ThrowIfUninitialized();

        return state == SuccessState
            ? UnitResult<TResultError>.Success()
            : UnitResult<TResultError>.Failure(selector(error!));
    }

    /// <summary>
    /// Transforms a success into a successful result value and preserves failure.
    /// </summary>
    /// <typeparam name="TValue">The transformed successful value type.</typeparam>
    /// <param name="selector">The transformation invoked for a success.</param>
    /// <returns>The transformed result, or the existing failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="selector"/> is null.</exception>
    /// <exception cref="InvalidOperationException">This unit result is the default value.</exception>
    public Result<TValue, TError> Map<TValue>(Func<TValue> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        ThrowIfUninitialized();

        return state == SuccessState
            ? Result<TValue, TError>.Success(selector())
            : Result<TValue, TError>.Failure(error!);
    }

    /// <summary>
    /// Binds a success to another unit result and preserves failure.
    /// </summary>
    /// <param name="binder">The unit-result-producing function to apply.</param>
    /// <returns>The bound unit result, or the existing failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="binder"/> is null.</exception>
    /// <exception cref="InvalidOperationException">This unit result is the default value.</exception>
    public UnitResult<TError> Bind(Func<UnitResult<TError>> binder)
    {
        ArgumentNullException.ThrowIfNull(binder);
        ThrowIfUninitialized();

        return state == SuccessState ? binder() : this;
    }

    /// <summary>
    /// Keeps a success only when it satisfies <paramref name="predicate"/>.
    /// </summary>
    /// <param name="predicate">The predicate to evaluate for a success.</param>
    /// <param name="error">The failure returned when the predicate is false.</param>
    /// <returns>This unit result when already failed or when the predicate is true; otherwise, a new failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="predicate"/> is null.</exception>
    /// <exception cref="InvalidOperationException">This unit result is the default value.</exception>
    public UnitResult<TError> Ensure(Func<bool> predicate, TError error)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ThrowIfUninitialized();

        return state == FailureState || predicate() ? this : Failure(error);
    }

    /// <summary>
    /// Keeps a success only when it satisfies <paramref name="predicate"/>.
    /// </summary>
    /// <param name="predicate">The predicate to evaluate for a success.</param>
    /// <param name="errorFactory">The failure factory invoked when the predicate is false.</param>
    /// <returns>This unit result when already failed or when the predicate is true; otherwise, a new failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="predicate"/> or <paramref name="errorFactory"/> is null.</exception>
    /// <exception cref="InvalidOperationException">This unit result is the default value.</exception>
    public UnitResult<TError> Ensure(Func<bool> predicate, Func<TError> errorFactory)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(errorFactory);
        ThrowIfUninitialized();

        if (state == FailureState || predicate())
        {
            return this;
        }

        return Failure(errorFactory());
    }

    /// <summary>
    /// Recovers a failure by producing another unit result.
    /// </summary>
    /// <param name="recovery">The unit-result-producing recovery function.</param>
    /// <returns>This unit result when successful; otherwise, the recovery unit result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="recovery"/> is null.</exception>
    /// <exception cref="InvalidOperationException">This unit result is the default value.</exception>
    public UnitResult<TError> RecoverWith(Func<TError, UnitResult<TError>> recovery)
    {
        ArgumentNullException.ThrowIfNull(recovery);
        ThrowIfUninitialized();

        return state == SuccessState ? this : recovery(error!);
    }

    /// <summary>
    /// Combines this unit result with another unit result, returning the first failure.
    /// </summary>
    /// <param name="second">The unit result to combine with.</param>
    /// <returns>Success when both are successful, or the first failure in left-to-right order.</returns>
    /// <exception cref="InvalidOperationException">This unit result or <paramref name="second"/> is the default value.</exception>
    public UnitResult<TError> Zip(UnitResult<TError> second)
    {
        ThrowIfUninitialized();
        second.ThrowIfUninitialized();

        if (state == FailureState)
        {
            return this;
        }

        return second.IsFailure ? second : Success();
    }

    /// <summary>
    /// Lazily combines this unit result with another unit result, skipping the factory after a failure.
    /// </summary>
    /// <param name="secondFactory">The unit result factory to invoke after a success.</param>
    /// <returns>Success when both are successful, or the first failure in left-to-right order.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="secondFactory"/> is null.</exception>
    /// <exception cref="InvalidOperationException">This unit result is the default value.</exception>
    public UnitResult<TError> ZipWith(Func<UnitResult<TError>> secondFactory)
    {
        ArgumentNullException.ThrowIfNull(secondFactory);
        ThrowIfUninitialized();

        return state == SuccessState ? Zip(secondFactory()) : this;
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">This unit result or <paramref name="other"/> is the default value.</exception>
    public bool Equals(UnitResult<TError> other)
    {
        ThrowIfUninitialized();
        other.ThrowIfUninitialized();

        if (state != other.state)
        {
            return false;
        }

        return state == SuccessState ||
            EqualityComparer<TError>.Default.Equals(error!, other.error!);
    }

    /// <summary>
    /// Determines whether this unit result equals another object.
    /// </summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns>
    /// <see langword="true"/> when <paramref name="obj"/> is an initialized unit result with the same case and
    /// payload; otherwise, <see langword="false"/> for <see langword="null"/> and unrelated objects.
    /// </returns>
    /// <exception cref="InvalidOperationException">This unit result is the default value.</exception>
    public override bool Equals(object? obj)
    {
        ThrowIfUninitialized();
        return obj is UnitResult<TError> other && Equals(other);
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">This unit result is the default value.</exception>
    public override int GetHashCode()
    {
        ThrowIfUninitialized();

        return state == SuccessState
            ? HashCode.Combine(SuccessState)
            : HashCode.Combine(FailureState, EqualityComparer<TError>.Default.GetHashCode(error!));
    }

    /// <summary>
    /// Determines whether two unit results are equal.
    /// </summary>
    /// <param name="left">The left unit result.</param>
    /// <param name="right">The right unit result.</param>
    /// <returns><see langword="true"/> when both unit results have the same case and payload; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="InvalidOperationException"><paramref name="left"/> or <paramref name="right"/> is the default value.</exception>
    public static bool operator ==(UnitResult<TError> left, UnitResult<TError> right) =>
        left.Equals(right);

    /// <summary>
    /// Determines whether two unit results are unequal.
    /// </summary>
    /// <param name="left">The left unit result.</param>
    /// <param name="right">The right unit result.</param>
    /// <returns><see langword="true"/> when the unit results differ; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="InvalidOperationException"><paramref name="left"/> or <paramref name="right"/> is the default value.</exception>
    public static bool operator !=(UnitResult<TError> left, UnitResult<TError> right) =>
        !left.Equals(right);

    /// <summary>
    /// Returns diagnostic text for this unit result.
    /// </summary>
    /// <returns><c>Success</c>, <c>Failure(error)</c>, or <c>Uninitialized</c> for the default value.</returns>
    public override string ToString() =>
        state switch
        {
            SuccessState => "Success",
            FailureState => $"Failure({error})",
            _ => "Uninitialized",
        };

    private void ThrowIfUninitialized()
    {
        if (state == UninitializedState)
        {
            throw new InvalidOperationException(UninitializedMessage);
        }
    }
}
