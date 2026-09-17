using System.Diagnostics.CodeAnalysis;

using System.Runtime.ExceptionServices;

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
    /// <typeparam name="TValue">The successful value type.</typeparam>
    /// <param name="operation">The tokenless asynchronous operation to invoke.</param>
    /// <returns>A task that produces the operation result or a failure containing the original exception.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="operation"/> is null.</exception>
    /// <remarks>
    /// The tokenless delegate is intentional: this exception boundary does not own cancellation. Callers that need
    /// cancellation must capture and pass their <see cref="CancellationToken"/> inside <paramref name="operation"/>.
    /// Cancellation remains cancellation of the returned task, and a faulted task containing an
    /// <see cref="OperationCanceledException"/> remains faulted. Neither is converted to a failure.
    /// A null task returned by <paramref name="operation"/> faults the returned task with
    /// <see cref="InvalidOperationException"/>.
    /// </remarks>
    public static Task<Result<TValue, Exception>> TryAsync<TValue>(Func<Task<TValue>> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        return TryAsyncCore(operation, PreserveException);
    }

    /// <summary>
    /// Invokes a task-returning operation and maps a non-cancellation exception to a typed failure.
    /// </summary>
    /// <typeparam name="TValue">The successful value type.</typeparam>
    /// <typeparam name="TError">The failure value type.</typeparam>
    /// <param name="operation">The tokenless asynchronous operation to invoke.</param>
    /// <param name="errorMapper">The exception-to-failure mapping for non-cancellation exceptions.</param>
    /// <returns>A task that produces the operation result or a mapped failure.</returns>
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
    /// <typeparam name="TValue">The successful value type.</typeparam>
    /// <param name="operation">The tokenless asynchronous operation to invoke.</param>
    /// <returns>A value task that produces the operation result or a failure containing the original exception.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="operation"/> is null.</exception>
    /// <remarks>
    /// The tokenless delegate is intentional: this exception boundary does not own cancellation. Callers that need
    /// cancellation must capture and pass their <see cref="CancellationToken"/> inside <paramref name="operation"/>.
    /// Cancellation remains cancellation of the returned value task, and a faulted value task containing an
    /// <see cref="OperationCanceledException"/> remains faulted. Neither is converted to a failure.
    /// The operation result is observed once; callers must follow the normal single-consumption rule for
    /// <c>ValueTask</c>.
    /// </remarks>
    public static ValueTask<Result<TValue, Exception>> TryValueAsync<TValue>(
        Func<ValueTask<TValue>> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        return TryValueAsyncCore(operation, PreserveException);
    }

    /// <summary>
    /// Invokes a value-task-returning operation and maps a non-cancellation exception to a typed failure.
    /// </summary>
    /// <typeparam name="TValue">The successful value type.</typeparam>
    /// <typeparam name="TError">The failure value type.</typeparam>
    /// <param name="operation">The tokenless asynchronous operation to invoke.</param>
    /// <param name="errorMapper">The exception-to-failure mapping for non-cancellation exceptions.</param>
    /// <returns>A value task that produces the operation result or a mapped failure.</returns>
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
    public static ValueTask<Result<TValue, TError>> TryValueAsync<TValue, TError>(
        Func<ValueTask<TValue>> operation,
        Func<Exception, TError> errorMapper)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(errorMapper);
        return TryValueAsyncCore(operation, errorMapper);
    }

    private static Exception PreserveException(Exception exception) => exception;

    internal static Task<TResult> TransformTask<TValue, TResult>(
        Task<TValue> task,
        Func<TValue, TResult> success,
        Func<Exception, TResult>? fault = null)
    {
        if (task.IsCompletedSuccessfully)
        {
            try
            {
                return Task.FromResult(success(task.GetAwaiter().GetResult()));
            }
            catch (Exception exception)
            {
                return FromException<TResult>(exception);
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

    internal static ValueTask<TResult> TransformValueTask<TValue, TResult>(
        ValueTask<TValue> task,
        Func<TValue, TResult> success,
        Func<Exception, TResult>? fault = null)
    {
        if (task.IsCompletedSuccessfully)
        {
            try
            {
                return ValueTask.FromResult(success(task.Result));
            }
            catch (Exception exception)
            {
                return new ValueTask<TResult>(FromException<TResult>(exception));
            }
        }

        return new ValueTask<TResult>(TransformTask(task.AsTask(), success, fault));
    }

    internal static Task<TResult> FromException<TResult>(Exception exception) =>
        exception is OperationCanceledException cancellation
            ? CreateCanceledTask<TResult>(cancellation)
            : Task.FromException<TResult>(exception);

    private static void CompleteTask<TValue, TResult>(
        Task<TValue> task,
        TaskCompletionSource<TResult> completion,
        Func<TValue, TResult> success,
        Func<Exception, TResult>? fault)
    {
        if (task.IsCompletedSuccessfully)
        {
            CompleteResult(completion, () => success(task.GetAwaiter().GetResult()));
            return;
        }

        if (task.IsCanceled)
        {
            completion.TrySetFromTask(CreateCanceledTask<TResult>(GetCancellationException(task)));
            return;
        }

        if (fault is null)
        {
            completion.TrySetException(task.Exception!.InnerExceptions);
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

    private static Task<Result<TValue, TError>> TryAsyncCore<TValue, TError>(
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
            return MapException<TValue, TError>(exception, errorMapper);
        }

        catch (OperationCanceledException exception)
        {
            return FromException<Result<TValue, TError>>(exception);
        }

        if (task is null)
        {
            return Task.FromException<Result<TValue, TError>>(
                new InvalidOperationException("The operation returned a null task."));
        }

        return TransformTask(
            task,
            static value => Result<TValue, TError>.Success(value),
            exception => Result<TValue, TError>.Failure(errorMapper(exception)));
    }

    private static ValueTask<Result<TValue, TError>> TryValueAsyncCore<TValue, TError>(
        Func<ValueTask<TValue>> operation,
        Func<Exception, TError> errorMapper)
    {
        try
        {
            return TransformValueTask(
                operation(),
                static value => Result<TValue, TError>.Success(value),
                exception => Result<TValue, TError>.Failure(errorMapper(exception)));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return new ValueTask<Result<TValue, TError>>(
                MapException<TValue, TError>(exception, errorMapper));
        }
        catch (OperationCanceledException exception)
        {
            return new ValueTask<Result<TValue, TError>>(
                FromException<Result<TValue, TError>>(exception));
        }
    }

    private static Task<Result<TValue, TError>> MapException<TValue, TError>(
        Exception exception,
        Func<Exception, TError> errorMapper)
    {
        try
        {
            return Task.FromResult(Result<TValue, TError>.Failure(errorMapper(exception)));
        }
        catch (Exception mapperException)
        {
            return FromException<Result<TValue, TError>>(mapperException);
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
    private const byte UninitializedState = 0;
    private const byte SuccessState = 1;
    private const byte FailureState = 2;

    private readonly TValue? value;
    private readonly TError? error;
    private readonly byte state;

    private Result(TValue? value, TError? error, byte state)
    {
        this.value = value;
        this.error = error;
        this.state = state;
    }

    /// <summary>
    /// Gets a value indicating whether this result is successful.
    /// </summary>
    /// <exception cref="InvalidOperationException">This result is uninitialized.</exception>
    public bool IsSuccess
    {
        get
        {
            EnsureInitialized();
            return state == SuccessState;
        }
    }

    /// <summary>
    /// Gets a value indicating whether this result is a failure.
    /// </summary>
    /// <exception cref="InvalidOperationException">This result is uninitialized.</exception>
    public bool IsFailure
    {
        get
        {
            EnsureInitialized();
            return state == FailureState;
        }
    }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="value">The successful value.</param>
    /// <returns>A successful result containing <paramref name="value"/>.</returns>
    public static Result<TValue, TError> Success(TValue value) => new(value, default, SuccessState);

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="error">The failure value.</param>
    /// <returns>A failed result containing <paramref name="error"/>.</returns>
    public static Result<TValue, TError> Failure(TError error) => new(default, error, FailureState);

    /// <summary>
    /// Attempts to retrieve the successful value.
    /// </summary>
    /// <param name="value">The successful value, or <see langword="default"/> when failed.</param>
    /// <returns><see langword="true"/> when successful; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="InvalidOperationException">This result is uninitialized.</exception>
    public bool TryGetValue([MaybeNull] out TValue value)
    {
        EnsureInitialized();
        value = this.value;
        return state == SuccessState;
    }

    /// <summary>
    /// Attempts to retrieve the failure value.
    /// </summary>
    /// <param name="error">The failure value, or <see langword="default"/> when successful.</param>
    /// <returns><see langword="true"/> when failed; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="InvalidOperationException">This result is uninitialized.</exception>
    public bool TryGetError([MaybeNull] out TError error)
    {
        EnsureInitialized();
        error = this.error;
        return state == FailureState;
    }

    /// <summary>
    /// Matches this result and returns the value from the selected branch.
    /// </summary>
    /// <typeparam name="TResult">The branch result type.</typeparam>
    /// <param name="success">The branch invoked with a successful value.</param>
    /// <param name="failure">The branch invoked with a failure value.</param>
    /// <returns>The selected branch result.</returns>
    /// <exception cref="InvalidOperationException">This result is uninitialized.</exception>
    public TResult Match<TResult>(Func<TValue, TResult> success, Func<TError, TResult> failure)
    {
        ArgumentNullException.ThrowIfNull(success);
        ArgumentNullException.ThrowIfNull(failure);
        EnsureInitialized();

        return state == SuccessState ? success(value!) : failure(error!);
    }

    /// <summary>
    /// Matches this result and invokes the selected branch.
    /// </summary>
    /// <param name="success">The branch invoked with a successful value.</param>
    /// <param name="failure">The branch invoked with a failure value.</param>
    /// <exception cref="InvalidOperationException">This result is uninitialized.</exception>
    public void Match(Action<TValue> success, Action<TError> failure)
    {
        ArgumentNullException.ThrowIfNull(success);
        ArgumentNullException.ThrowIfNull(failure);
        EnsureInitialized();

        if (state == SuccessState)
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
    /// <exception cref="InvalidOperationException">This result is uninitialized.</exception>
    public Result<TResult, TError> Map<TResult>(Func<TValue, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        EnsureInitialized();

        return state == SuccessState
            ? Result<TResult, TError>.Success(selector(value!))
            : Result<TResult, TError>.Failure(error!);
    }

    /// <summary>
    /// Binds a successful value to another result and preserves failure.
    /// </summary>
    /// <typeparam name="TResult">The bound value type.</typeparam>
    /// <param name="binder">The result-returning function to apply.</param>
    /// <returns>The bound result, or the existing failure.</returns>
    /// <exception cref="InvalidOperationException">This result is uninitialized.</exception>
    public Result<TResult, TError> Bind<TResult>(Func<TValue, Result<TResult, TError>> binder)
    {
        ArgumentNullException.ThrowIfNull(binder);
        EnsureInitialized();

        return state == SuccessState
            ? binder(value!)
            : Result<TResult, TError>.Failure(error!);
    }

    /// <summary>
    /// Transforms a failure value and preserves success.
    /// </summary>
    /// <typeparam name="TResultError">The transformed failure type.</typeparam>
    /// <param name="selector">The failure transformation to apply.</param>
    /// <returns>The transformed failure, or the existing success.</returns>
    /// <exception cref="InvalidOperationException">This result is uninitialized.</exception>
    public Result<TValue, TResultError> MapError<TResultError>(Func<TError, TResultError> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        EnsureInitialized();

        return state == SuccessState
            ? Result<TValue, TResultError>.Success(value!)
            : Result<TValue, TResultError>.Failure(selector(error!));
    }

    /// <summary>
    /// Keeps a successful value only when it satisfies <paramref name="predicate"/>.
    /// </summary>
    /// <param name="predicate">The predicate to evaluate.</param>
    /// <param name="error">The failure returned when the predicate is false.</param>
    /// <returns>This result when already failed or when the value matches; otherwise, a new failure.</returns>
    /// <exception cref="InvalidOperationException">This result is uninitialized.</exception>
    public Result<TValue, TError> Ensure(Func<TValue, bool> predicate, TError error)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        EnsureInitialized();

        return state == FailureState || predicate(value!) ? this : Failure(error);
    }

    /// <summary>
    /// Keeps a successful value only when it satisfies <paramref name="predicate"/>.
    /// </summary>
    /// <param name="predicate">The predicate to evaluate.</param>
    /// <param name="errorFactory">The failure factory invoked for an unsuccessful validation.</param>
    /// <returns>This result when already failed or when the value matches; otherwise, a new failure.</returns>
    /// <exception cref="InvalidOperationException">This result is uninitialized.</exception>
    public Result<TValue, TError> Ensure(
        Func<TValue, bool> predicate,
        Func<TValue, TError> errorFactory)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(errorFactory);
        EnsureInitialized();

        if (state == FailureState || predicate(value!))
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
    /// <exception cref="InvalidOperationException">This result is uninitialized.</exception>
    public Result<TValue, TError> Recover(Func<TError, TValue> recovery)
    {
        ArgumentNullException.ThrowIfNull(recovery);
        EnsureInitialized();

        return state == SuccessState ? this : Success(recovery(error!));
    }

    /// <summary>
    /// Recovers a failure by producing another result.
    /// </summary>
    /// <param name="recovery">The result-returning recovery function.</param>
    /// <returns>This result when successful; otherwise, the recovery result.</returns>
    /// <exception cref="InvalidOperationException">This result is uninitialized.</exception>
    public Result<TValue, TError> RecoverWith(Func<TError, Result<TValue, TError>> recovery)
    {
        ArgumentNullException.ThrowIfNull(recovery);
        EnsureInitialized();

        return state == SuccessState ? this : recovery(error!);
    }

    /// <summary>
    /// Combines this result with another result, returning the first failure.
    /// </summary>
    /// <typeparam name="TSecond">The second successful value type.</typeparam>
    /// <param name="second">The result to combine with.</param>
    /// <returns>Both successful values, or the first failure in left-to-right order.</returns>
    /// <exception cref="InvalidOperationException">This result or <paramref name="second"/> is uninitialized.</exception>
    public Result<(TValue First, TSecond Second), TError> Zip<TSecond>(
        Result<TSecond, TError> second)
    {
        EnsureInitialized();
        second.EnsureInitialized();

        if (state == FailureState)
        {
            return Result<(TValue First, TSecond Second), TError>.Failure(error!);
        }

        return second.state == SuccessState
            ? Result<(TValue First, TSecond Second), TError>.Success((value!, second.value!))
            : Result<(TValue First, TSecond Second), TError>.Failure(second.error!);
    }

    /// <summary>
    /// Combines this result with another result through a combining function.
    /// </summary>
    /// <typeparam name="TSecond">The second successful value type.</typeparam>
    /// <typeparam name="TResult">The combined successful value type.</typeparam>
    /// <param name="second">The result to combine with.</param>
    /// <param name="combine">The function invoked with both successful values.</param>
    /// <returns>The combined success, or the first failure in left-to-right order.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="combine"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">This result or <paramref name="second"/> is uninitialized.</exception>
    public Result<TResult, TError> Zip<TSecond, TResult>(
        Result<TSecond, TError> second,
        Func<TValue, TSecond, TResult> combine)
    {
        ArgumentNullException.ThrowIfNull(combine);
        EnsureInitialized();
        second.EnsureInitialized();

        if (state == FailureState)
        {
            return Result<TResult, TError>.Failure(error!);
        }

        return second.state == SuccessState
            ? Result<TResult, TError>.Success(combine(value!, second.value!))
            : Result<TResult, TError>.Failure(second.error!);
    }

    /// <summary>
    /// Combines this result with two more results through a combining function.
    /// </summary>
    /// <typeparam name="TSecond">The second successful value type.</typeparam>
    /// <typeparam name="TThird">The third successful value type.</typeparam>
    /// <typeparam name="TResult">The combined successful value type.</typeparam>
    /// <param name="second">The second result to combine with.</param>
    /// <param name="third">The third result to combine with.</param>
    /// <param name="combine">The function invoked with all three successful values.</param>
    /// <returns>The combined success, or the first failure in left-to-right order.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="combine"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">This result or an operand is uninitialized.</exception>
    public Result<TResult, TError> Zip<TSecond, TThird, TResult>(
        Result<TSecond, TError> second,
        Result<TThird, TError> third,
        Func<TValue, TSecond, TThird, TResult> combine)
    {
        ArgumentNullException.ThrowIfNull(combine);
        EnsureInitialized();
        second.EnsureInitialized();
        third.EnsureInitialized();

        if (state == FailureState)
        {
            return Result<TResult, TError>.Failure(error!);
        }

        if (second.state == FailureState)
        {
            return Result<TResult, TError>.Failure(second.error!);
        }

        return third.state == SuccessState
            ? Result<TResult, TError>.Success(combine(value!, second.value!, third.value!))
            : Result<TResult, TError>.Failure(third.error!);
    }

    /// <summary>
    /// Lazily combines this result with another result, skipping the factory after a failure.
    /// </summary>
    /// <typeparam name="TSecond">The second successful value type.</typeparam>
    /// <param name="secondFactory">The result factory to invoke after success.</param>
    /// <returns>Both successful values, or the first failure in left-to-right order.</returns>
    /// <exception cref="InvalidOperationException">This result is uninitialized.</exception>
    public Result<(TValue First, TSecond Second), TError> ZipWith<TSecond>(
        Func<Result<TSecond, TError>> secondFactory)
    {
        ArgumentNullException.ThrowIfNull(secondFactory);
        EnsureInitialized();

        return state == SuccessState
            ? Zip(secondFactory())
            : Result<(TValue First, TSecond Second), TError>.Failure(error!);
    }

    /// <summary>
    /// Projects a successful value for LINQ query syntax.
    /// </summary>
    /// <typeparam name="TResult">The projected successful value type.</typeparam>
    /// <param name="selector">The projection to invoke for a successful value.</param>
    /// <returns>The projected success or the existing failure.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="selector"/> is null.</exception>
    public Result<TResult, TError> Select<TResult>(Func<TValue, TResult> selector) => Map(selector);

    /// <summary>
    /// Binds and projects successful values for LINQ query syntax.
    /// </summary>
    /// <typeparam name="TIntermediate">The successful value type of the bound result.</typeparam>
    /// <typeparam name="TResult">The projected successful value type.</typeparam>
    /// <param name="binder">The result-producing function to invoke for a successful value.</param>
    /// <param name="projector">The projection to invoke when both results are successful.</param>
    /// <returns>The projected success, the existing failure, or the failure returned by <paramref name="binder"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="binder"/> or <paramref name="projector"/> is null.</exception>
    public Result<TResult, TError> SelectMany<TIntermediate, TResult>(
        Func<TValue, Result<TIntermediate, TError>> binder,
        Func<TValue, TIntermediate, TResult> projector)
    {
        ArgumentNullException.ThrowIfNull(binder);
        ArgumentNullException.ThrowIfNull(projector);
        EnsureInitialized();

        if (state == FailureState)
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
    /// <exception cref="InvalidOperationException">This result or <paramref name="other"/> is uninitialized.</exception>
    public bool Equals(Result<TValue, TError> other)
    {
        EnsureInitialized();
        other.EnsureInitialized();

        if (state != other.state)
        {
            return false;
        }

        return state == SuccessState
            ? EqualityComparer<TValue>.Default.Equals(value!, other.value!)
            : EqualityComparer<TError>.Default.Equals(error!, other.error!);
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">This result is uninitialized.</exception>
    public override bool Equals(object? obj)
    {
        EnsureInitialized();
        return obj is Result<TValue, TError> other && Equals(other);
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">This result is uninitialized.</exception>
    public override int GetHashCode()
    {
        EnsureInitialized();
        return state == SuccessState
            ? HashCode.Combine(true, EqualityComparer<TValue>.Default.GetHashCode(value!))
            : HashCode.Combine(false, EqualityComparer<TError>.Default.GetHashCode(error!));
    }

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
    public override string ToString() =>
        state switch
        {
            SuccessState => $"Success({value})",
            FailureState => $"Failure({error})",
            _ => "Uninitialized",
        };

    private void EnsureInitialized()
    {
        if (state == UninitializedState)
        {
            throw new InvalidOperationException("The result has not been initialized.");
        }
    }
}
