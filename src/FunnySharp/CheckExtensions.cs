namespace FunnySharp;

/// <summary>
/// Provides value-preserving fallible checks and ordered independent check accumulation.
/// </summary>
/// <remarks>
/// <para>
/// A check is a caller-supplied <c>Func&lt;TValue, UnitResult&lt;TError&gt;&gt;</c> (or its
/// <see cref="Task"/> or <see cref="ValueTask"/> form) that validates a value without producing a
/// new one. <c>Check</c> runs one check in a fail-fast workflow and retains the exact successful
/// input value. <c>CheckAll</c> runs an ordered set of independent checks against the same input
/// and returns a <see cref="Validation{TValue, TError}"/> containing the input value or every
/// failed check's error in rule order.
/// </para>
/// </remarks>
public static class CheckExtensions
{
    /// <summary>
    /// Runs a fallible check against a successful result and preserves the exact successful value.
    /// </summary>
    /// <typeparam name="TValue">The successful value type.</typeparam>
    /// <typeparam name="TError">The failure value type.</typeparam>
    /// <param name="result">The result to check.</param>
    /// <param name="check">The no-value business check to run for a successful result.</param>
    /// <returns>
    /// <paramref name="result"/> itself when it is successful and the check succeeds; otherwise, a
    /// failure containing the existing result error or the check's error.
    /// </returns>
    /// <remarks>
    /// The delegate is validated at entry even when <paramref name="result"/> has already failed.
    /// The check is invoked at most once and only for a successful result. A successful check
    /// returns the original result without invoking a value factory or reconstructing the value;
    /// a failed check replaces the successful value with its typed error. Check exceptions
    /// propagate unchanged, and an uninitialized unit result returned by the check throws
    /// <see cref="InvalidOperationException"/>.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="check"/> is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="result"/> is the default value, or the check returned an uninitialized unit
    /// result for a successful input.
    /// </exception>
    public static Result<TValue, TError> Check<TValue, TError>(
        this Result<TValue, TError> result,
        Func<TValue, UnitResult<TError>> check)
    {
        ArgumentNullException.ThrowIfNull(check);

        if (!result.TryGetValue(out var value))
        {
            return result;
        }

        return CheckedOrFailure(result, check(value!));
    }

    /// <summary>
    /// Runs an ordered set of independent fallible checks against a successful result and
    /// accumulates every check failure.
    /// </summary>
    /// <typeparam name="TValue">The successful value type.</typeparam>
    /// <typeparam name="TError">The failure value type.</typeparam>
    /// <param name="result">The result supplying the input value or an existing failure.</param>
    /// <param name="checks">The checks to run in order against the successful value.</param>
    /// <returns>
    /// A valid validation containing the exact successful value when every check succeeds; an
    /// invalid validation containing the existing result error when <paramref name="result"/>
    /// already failed; otherwise, an invalid validation containing every check error in rule
    /// order.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The checks sequence is rejected when it is null and is snapshotted exactly once after the
    /// result is found successful. A null check element is rejected with
    /// <see cref="ArgumentException"/> before any callback is invoked; enumeration exceptions also
    /// propagate before any callback. An already-failed result does not enumerate or validate the
    /// checks and produces an invalid validation containing only the existing error.
    /// </para>
    /// <para>
    /// Every snapshot check is invoked exactly once in rule order with the same original value,
    /// even after an earlier check fails. A failed check contributes its error; a successful check
    /// contributes none. Duplicate check delegates are invoked once per position and duplicate
    /// errors are preserved. An empty sequence is valid and invokes nothing. Callback exceptions
    /// propagate unchanged and prevent later checks from running; an uninitialized unit result
    /// returned by a check throws <see cref="InvalidOperationException"/>.
    /// </para>
    /// <para>
    /// The input value is passed by value and is not copied. A reference-type input is the same
    /// object for every check, and mutation performed by one check is observable by later checks;
    /// callers that require isolated rules must supply immutable input or copy it themselves.
    /// This operation never treats a check's success as creating a new value for a later check.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="checks"/> is null.</exception>
    /// <exception cref="ArgumentException">The checks snapshot contains a null check element.</exception>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="result"/> is the default value, or a reached check returned an uninitialized
    /// unit result.
    /// </exception>
    public static Validation<TValue, TError> CheckAll<TValue, TError>(
        this Result<TValue, TError> result,
        IEnumerable<Func<TValue, UnitResult<TError>>> checks)
    {
        ArgumentNullException.ThrowIfNull(checks);

        if (result.TryGetValue(out var value))
        {
            return RunChecks(value!, Snapshot(checks));
        }

        result.TryGetError(out var error);
        return Validation<TValue, TError>.Invalid(error!);
    }

    /// <summary>
    /// Runs an ordered set of independent fallible checks against a valid validation and
    /// accumulates every check failure.
    /// </summary>
    /// <typeparam name="TValue">The valid value type.</typeparam>
    /// <typeparam name="TError">The validation error type.</typeparam>
    /// <param name="validation">The validation supplying the input value or existing errors.</param>
    /// <param name="checks">The checks to run in order against the valid value.</param>
    /// <returns>
    /// The original validation when it is invalid; a valid validation containing the exact valid
    /// value when every check succeeds; otherwise, an invalid validation containing every check
    /// error in rule order.
    /// </returns>
    /// <remarks>
    /// The checks sequence is rejected when it is null and is snapshotted exactly once after the
    /// validation is found valid. An invalid validation does not enumerate or validate the checks
    /// and returns its existing error snapshot unchanged. A valid validation with an empty check
    /// sequence returns the original validation unchanged.
    /// <para>
    /// Every snapshot check is invoked exactly once in rule order with the same original value,
    /// even after an earlier check fails. A failed check contributes its error; a successful check
    /// contributes none. Duplicate check delegates are invoked once per position and duplicate
    /// errors are preserved. Callback exceptions propagate unchanged and prevent later checks from
    /// running; an uninitialized unit result returned by a check throws
    /// <see cref="InvalidOperationException"/>.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="checks"/> is null.</exception>
    /// <exception cref="ArgumentException">The checks snapshot contains a null check element.</exception>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="validation"/> is the default value, or a reached check returned an
    /// uninitialized unit result.
    /// </exception>
    public static Validation<TValue, TError> CheckAll<TValue, TError>(
        this Validation<TValue, TError> validation,
        IEnumerable<Func<TValue, UnitResult<TError>>> checks)
    {
        ArgumentNullException.ThrowIfNull(checks);

        if (!validation.TryGetValue(out var value))
        {
            return validation;
        }

        var snapshot = Snapshot(checks);
        return snapshot.Length == 0 ? validation : RunChecks(value!, snapshot);
    }

    /// <summary>
    /// Runs a fallible check against a successful result and preserves the exact successful value.
    /// </summary>
    /// <typeparam name="TValue">The successful value type.</typeparam>
    /// <typeparam name="TError">The failure value type.</typeparam>
    /// <param name="result">The result to check.</param>
    /// <param name="check">The asynchronous no-value business check to run for a successful result.</param>
    /// <remarks>
    /// The delegate is guarded synchronously. An already-failed result returns an
    /// already-completed failed task without invoking the check or inspecting a cancellation token.
    /// A successful result invokes the check once and observes the returned task once. The check
    /// result is interpreted after completion; a successful check retains the original result and
    /// a failed check produces its typed error.
    /// <para>
    /// Synchronous exceptions from the delegate, a null returned task, and an uninitialized unit
    /// result are represented by the returned task. Faults and cancellation retain their normal
    /// asynchronous status and are not converted to typed carrier failures. A faulted task that
    /// contains an <see cref="OperationCanceledException"/> remains faulted; only a canceled task
    /// produces cancellation.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="check"/> is null.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="result"/> is the default value; thrown synchronously.</exception>
    public static Task<Result<TValue, TError>> CheckAsync<TValue, TError>(
        this Result<TValue, TError> result,
        Func<TValue, Task<UnitResult<TError>>> check)
    {
        ArgumentNullException.ThrowIfNull(check);

        if (!result.TryGetValue(out var value))
        {
            return Task.FromResult(result);
        }

        return CheckTask(result, Invoke(check, value!));
    }

    /// <summary>
    /// Runs a cancellation-aware fallible check against a successful result and preserves the
    /// exact successful value.
    /// </summary>
    /// <typeparam name="TValue">The successful value type.</typeparam>
    /// <typeparam name="TError">The failure value type.</typeparam>
    /// <param name="result">The result to check.</param>
    /// <param name="check">The asynchronous no-value business check to run for a successful result.</param>
    /// <param name="cancellationToken">The token forwarded unchanged to <paramref name="check"/>.</param>
    /// <remarks>
    /// The token is forwarded unchanged and is not inspected independently. An already-failed
    /// result returns an already-completed failed task without invoking the check or inspecting
    /// the token. See <c>CheckAsync</c>
    /// for the callback, fault, cancellation, and null-task behavior.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="check"/> is null.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="result"/> is the default value; thrown synchronously.</exception>
    public static Task<Result<TValue, TError>> CheckAsync<TValue, TError>(
        this Result<TValue, TError> result,
        Func<TValue, CancellationToken, Task<UnitResult<TError>>> check,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(check);

        if (!result.TryGetValue(out var value))
        {
            return Task.FromResult(result);
        }

        return CheckTask(result, Invoke(check, value!, cancellationToken));
    }

    /// <summary>
    /// Runs a value-task-returning fallible check against a successful result and preserves the
    /// exact successful value.
    /// </summary>
    /// <typeparam name="TValue">The successful value type.</typeparam>
    /// <typeparam name="TError">The failure value type.</typeparam>
    /// <param name="result">The result to check.</param>
    /// <param name="check">The no-value business check to run for a successful result.</param>
    /// <remarks>
    /// The delegate is guarded synchronously. An already-failed result returns an
    /// already-completed failed value task without invoking the check. A successful result invokes
    /// the check once and observes the returned value task once. A synchronously completed check
    /// result is interpreted without creating a task. Faults, cancellation, null tasks, and
    /// uninitialized check results follow the same rules as the task-returning overload.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="check"/> is null.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="result"/> is the default value; thrown synchronously.</exception>
    public static ValueTask<Result<TValue, TError>> CheckValueAsync<TValue, TError>(
        this Result<TValue, TError> result,
        Func<TValue, ValueTask<UnitResult<TError>>> check)
    {
        ArgumentNullException.ThrowIfNull(check);

        if (!result.TryGetValue(out var value))
        {
            return ValueTask.FromResult(result);
        }

        var task = Invoke(check, value!);

        return CompleteValueTask(result, task);
    }

    /// <summary>
    /// Runs a cancellation-aware value-task-returning fallible check against a successful result
    /// and preserves the exact successful value.
    /// </summary>
    /// <typeparam name="TValue">The successful value type.</typeparam>
    /// <typeparam name="TError">The failure value type.</typeparam>
    /// <param name="result">The result to check.</param>
    /// <param name="check">The no-value business check to run for a successful result.</param>
    /// <param name="cancellationToken">The token forwarded unchanged to <paramref name="check"/>.</param>
    /// <remarks>
    /// The token is forwarded unchanged and is not inspected independently. See
    /// <c>CheckValueAsync</c>
    /// for the single-consumption, fault, cancellation, and already-failed behavior.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="check"/> is null.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="result"/> is the default value; thrown synchronously.</exception>
    public static ValueTask<Result<TValue, TError>> CheckValueAsync<TValue, TError>(
        this Result<TValue, TError> result,
        Func<TValue, CancellationToken, ValueTask<UnitResult<TError>>> check,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(check);

        if (!result.TryGetValue(out var value))
        {
            return ValueTask.FromResult(result);
        }

        var task = Invoke(check, value!, cancellationToken);

        return CompleteValueTask(result, task);
    }

    /// <summary>
    /// Runs an ordered set of independent asynchronous checks against a successful result and
    /// accumulates every check failure.
    /// </summary>
    /// <typeparam name="TValue">The successful value type.</typeparam>
    /// <typeparam name="TError">The failure value type.</typeparam>
    /// <param name="result">The result supplying the input value or an existing failure.</param>
    /// <param name="checks">The checks to run sequentially in order against the successful value.</param>
    /// <returns>A task that produces the valid value or every ordered check error.</returns>
    /// <remarks>
    /// <para>
    /// The checks sequence is rejected when it is null and is snapshotted and validated
    /// synchronously after the result is found successful. Null check elements and enumeration
    /// exceptions are argument errors thrown before any callback or returned task exists. An
    /// already-failed result returns an already-completed invalid validation without enumerating
    /// the checks or inspecting a token.
    /// </para>
    /// <para>
    /// Checks run sequentially in rule order. A check is invoked only after every earlier check's
    /// task has completed. A failed check contributes its error and does not stop later checks; a
    /// successful check contributes none. The returned task faults or cancels immediately when a
    /// reached check faults or cancels, and later checks are not invoked; no partial validation is
    /// returned. A faulted task containing an <see cref="OperationCanceledException"/> remains
    /// faulted. Callback exceptions are represented by the returned task, while rule-source
    /// materialization exceptions are thrown synchronously.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="checks"/> is null; thrown synchronously.</exception>
    /// <exception cref="ArgumentException">The checks snapshot contains a null check element; thrown synchronously.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="result"/> is the default value; thrown synchronously.</exception>
    public static Task<Validation<TValue, TError>> CheckAllAsync<TValue, TError>(
        this Result<TValue, TError> result,
        IEnumerable<Func<TValue, Task<UnitResult<TError>>>> checks)
    {
        ArgumentNullException.ThrowIfNull(checks);

        if (result.TryGetValue(out var value))
        {
            return CheckAllAsyncCore(value!, Snapshot(checks));
        }

        result.TryGetError(out var error);
        return Task.FromResult(Validation<TValue, TError>.Invalid(error!));
    }

    /// <summary>
    /// Runs an ordered set of independent cancellation-aware asynchronous checks against a
    /// successful result and accumulates every check failure.
    /// </summary>
    /// <typeparam name="TValue">The successful value type.</typeparam>
    /// <typeparam name="TError">The failure value type.</typeparam>
    /// <param name="result">The result supplying the input value or an existing failure.</param>
    /// <param name="checks">The checks to run sequentially in order against the successful value.</param>
    /// <param name="cancellationToken">The token forwarded unchanged to every reached check.</param>
    /// <returns>A task that produces the valid value or every ordered check error.</returns>
    /// <remarks>
    /// The token is forwarded unchanged to each reached check and is not inspected independently.
    /// Already-failed, empty-rule, callback, fault, cancellation, and rule-order behavior matches
    /// <c>CheckAllAsync</c>.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="checks"/> is null; thrown synchronously.</exception>
    /// <exception cref="ArgumentException">The checks snapshot contains a null check element; thrown synchronously.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="result"/> is the default value; thrown synchronously.</exception>
    public static Task<Validation<TValue, TError>> CheckAllAsync<TValue, TError>(
        this Result<TValue, TError> result,
        IEnumerable<Func<TValue, CancellationToken, Task<UnitResult<TError>>>> checks,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(checks);

        if (result.TryGetValue(out var value))
        {
            return CheckAllAsyncCore(value!, Snapshot(checks), cancellationToken);
        }

        result.TryGetError(out var error);
        return Task.FromResult(Validation<TValue, TError>.Invalid(error!));
    }

    /// <summary>
    /// Runs an ordered set of independent asynchronous checks against a valid validation and
    /// accumulates every check failure.
    /// </summary>
    /// <typeparam name="TValue">The valid value type.</typeparam>
    /// <typeparam name="TError">The validation error type.</typeparam>
    /// <param name="validation">The validation supplying the input value or existing errors.</param>
    /// <param name="checks">The checks to run sequentially in order against the valid value.</param>
    /// <returns>A task that produces the original invalid validation or the valid value and ordered check errors.</returns>
    /// <remarks>
    /// The checks sequence is rejected when it is null and is snapshotted and validated
    /// synchronously after the validation is found valid. An invalid validation returns an
    /// already-completed invalid validation without enumerating the checks. Callback, fault,
    /// cancellation, and rule-order behavior matches the result-receiver overload.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="checks"/> is null; thrown synchronously.</exception>
    /// <exception cref="ArgumentException">The checks snapshot contains a null check element; thrown synchronously.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="validation"/> is the default value; thrown synchronously.</exception>
    public static Task<Validation<TValue, TError>> CheckAllAsync<TValue, TError>(
        this Validation<TValue, TError> validation,
        IEnumerable<Func<TValue, Task<UnitResult<TError>>>> checks)
    {
        ArgumentNullException.ThrowIfNull(checks);

        if (!validation.TryGetValue(out var value))
        {
            return Task.FromResult(validation);
        }

        return CheckAllAsyncCore(value!, Snapshot(checks));
    }

    /// <summary>
    /// Runs an ordered set of independent cancellation-aware asynchronous checks against a valid
    /// validation and accumulates every check failure.
    /// </summary>
    /// <typeparam name="TValue">The valid value type.</typeparam>
    /// <typeparam name="TError">The validation error type.</typeparam>
    /// <param name="validation">The validation supplying the input value or existing errors.</param>
    /// <param name="checks">The checks to run sequentially in order against the valid value.</param>
    /// <param name="cancellationToken">The token forwarded unchanged to every reached check.</param>
    /// <returns>A task that produces the original invalid validation or the valid value and ordered check errors.</returns>
    /// <remarks>
    /// The token is forwarded unchanged to each reached check and is not inspected independently.
    /// See the non-token overload for already-invalid, empty-rule, callback, fault, and rule-order
    /// behavior.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="checks"/> is null; thrown synchronously.</exception>
    /// <exception cref="ArgumentException">The checks snapshot contains a null check element; thrown synchronously.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="validation"/> is the default value; thrown synchronously.</exception>
    public static Task<Validation<TValue, TError>> CheckAllAsync<TValue, TError>(
        this Validation<TValue, TError> validation,
        IEnumerable<Func<TValue, CancellationToken, Task<UnitResult<TError>>>> checks,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(checks);

        if (!validation.TryGetValue(out var value))
        {
            return Task.FromResult(validation);
        }

        return CheckAllAsyncCore(value!, Snapshot(checks), cancellationToken);
    }

    /// <summary>
    /// Runs an ordered set of independent value-task-returning checks against a successful result
    /// and accumulates every check failure.
    /// </summary>
    /// <typeparam name="TValue">The successful value type.</typeparam>
    /// <typeparam name="TError">The failure value type.</typeparam>
    /// <param name="result">The result supplying the input value or an existing failure.</param>
    /// <param name="checks">The checks to run sequentially in order against the successful value.</param>
    /// <returns>A value task that produces the valid value or every ordered check error.</returns>
    /// <remarks>
    /// The checks sequence is rejected when it is null and is snapshotted and validated
    /// synchronously after the result is found successful. An already-failed result returns an
    /// already-completed invalid validation without enumerating the checks. Each reached check's
    /// value task is consumed once; a synchronously completed value task is processed without
    /// creating a task. Sequential rule order, error order, callback faults, cancellation, and
    /// empty rules match the task-receiver overload.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="checks"/> is null; thrown synchronously.</exception>
    /// <exception cref="ArgumentException">The checks snapshot contains a null check element; thrown synchronously.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="result"/> is the default value; thrown synchronously.</exception>
    public static ValueTask<Validation<TValue, TError>> CheckAllValueAsync<TValue, TError>(
        this Result<TValue, TError> result,
        IEnumerable<Func<TValue, ValueTask<UnitResult<TError>>>> checks)
    {
        ArgumentNullException.ThrowIfNull(checks);

        if (result.TryGetValue(out var value))
        {
            return CheckAllValueAsyncCore(value!, Snapshot(checks));
        }

        result.TryGetError(out var error);
        return ValueTask.FromResult(Validation<TValue, TError>.Invalid(error!));
    }

    /// <summary>
    /// Runs an ordered set of independent cancellation-aware value-task-returning checks against
    /// a successful result and accumulates every check failure.
    /// </summary>
    /// <typeparam name="TValue">The successful value type.</typeparam>
    /// <typeparam name="TError">The failure value type.</typeparam>
    /// <param name="result">The result supplying the input value or an existing failure.</param>
    /// <param name="checks">The checks to run sequentially in order against the successful value.</param>
    /// <param name="cancellationToken">The token forwarded unchanged to every reached check.</param>
    /// <returns>A value task that produces the valid value or every ordered check error.</returns>
    /// <remarks>
    /// The token is forwarded unchanged to each reached check and is not inspected independently.
    /// Single-consumption, fault, cancellation, already-failed, and rule-order behavior matches
    /// the non-token value-task overload.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="checks"/> is null; thrown synchronously.</exception>
    /// <exception cref="ArgumentException">The checks snapshot contains a null check element; thrown synchronously.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="result"/> is the default value; thrown synchronously.</exception>
    public static ValueTask<Validation<TValue, TError>> CheckAllValueAsync<TValue, TError>(
        this Result<TValue, TError> result,
        IEnumerable<Func<TValue, CancellationToken, ValueTask<UnitResult<TError>>>> checks,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(checks);

        if (result.TryGetValue(out var value))
        {
            return CheckAllValueAsyncCore(value!, Snapshot(checks), cancellationToken);
        }

        result.TryGetError(out var error);
        return ValueTask.FromResult(Validation<TValue, TError>.Invalid(error!));
    }

    /// <summary>
    /// Runs an ordered set of independent value-task-returning checks against a valid validation
    /// and accumulates every check failure.
    /// </summary>
    /// <typeparam name="TValue">The valid value type.</typeparam>
    /// <typeparam name="TError">The validation error type.</typeparam>
    /// <param name="validation">The validation supplying the input value or existing errors.</param>
    /// <param name="checks">The checks to run sequentially in order against the valid value.</param>
    /// <returns>A value task that produces the original invalid validation or the valid value and ordered check errors.</returns>
    /// <remarks>
    /// The checks sequence is rejected when it is null and is snapshotted and validated
    /// synchronously after the validation is found valid. An invalid validation returns an
    /// already-completed invalid validation without enumerating the checks. Each reached check's
    /// value task is consumed once. Sequential rule order, error order, callback faults,
    /// cancellation, and empty rules match the task-receiver overload.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="checks"/> is null; thrown synchronously.</exception>
    /// <exception cref="ArgumentException">The checks snapshot contains a null check element; thrown synchronously.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="validation"/> is the default value; thrown synchronously.</exception>
    public static ValueTask<Validation<TValue, TError>> CheckAllValueAsync<TValue, TError>(
        this Validation<TValue, TError> validation,
        IEnumerable<Func<TValue, ValueTask<UnitResult<TError>>>> checks)
    {
        ArgumentNullException.ThrowIfNull(checks);

        if (!validation.TryGetValue(out var value))
        {
            return ValueTask.FromResult(validation);
        }

        return CheckAllValueAsyncCore(value!, Snapshot(checks));
    }

    /// <summary>
    /// Runs an ordered set of independent cancellation-aware value-task-returning checks against a
    /// valid validation and accumulates every check failure.
    /// </summary>
    /// <typeparam name="TValue">The valid value type.</typeparam>
    /// <typeparam name="TError">The validation error type.</typeparam>
    /// <param name="validation">The validation supplying the input value or existing errors.</param>
    /// <param name="checks">The checks to run sequentially in order against the valid value.</param>
    /// <param name="cancellationToken">The token forwarded unchanged to every reached check.</param>
    /// <returns>A value task that produces the original invalid validation or the valid value and ordered check errors.</returns>
    /// <remarks>
    /// The token is forwarded unchanged to each reached check and is not inspected independently.
    /// See the non-token value-task overload for already-invalid, single-consumption, callback,
    /// fault, and rule-order behavior.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="checks"/> is null; thrown synchronously.</exception>
    /// <exception cref="ArgumentException">The checks snapshot contains a null check element; thrown synchronously.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="validation"/> is the default value; thrown synchronously.</exception>
    public static ValueTask<Validation<TValue, TError>> CheckAllValueAsync<TValue, TError>(
        this Validation<TValue, TError> validation,
        IEnumerable<Func<TValue, CancellationToken, ValueTask<UnitResult<TError>>>> checks,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(checks);

        if (!validation.TryGetValue(out var value))
        {
            return ValueTask.FromResult(validation);
        }

        return CheckAllValueAsyncCore(value!, Snapshot(checks), cancellationToken);
    }

    private static Result<TValue, TError> CheckedOrFailure<TValue, TError>(
        Result<TValue, TError> result,
        UnitResult<TError> check) =>
        check.TryGetError(out var error)
            ? Result<TValue, TError>.Failure(error!)
            : result;

    private static Validation<TValue, TError> RunChecks<TValue, TError>(
        TValue value,
        Func<TValue, UnitResult<TError>>[] checks)
    {
        List<TError>? errors = null;

        foreach (var check in checks)
        {
            AddError(check(value), ref errors);
        }

        return BuildValidation(value, errors);
    }

    private static Validation<TValue, TError> BuildValidation<TValue, TError>(
        TValue value,
        List<TError>? errors) =>
        errors is null
            ? Validation<TValue, TError>.Valid(value)
            : Validation<TValue, TError>.InvalidFromOwnedErrors(errors);

    private static TCheck[] Snapshot<TCheck>(IEnumerable<TCheck> checks)
        where TCheck : class
    {
        var snapshot = checks.ToArray();
        for (var index = 0; index < snapshot.Length; index++)
        {
            if (snapshot[index] is null)
            {
                throw new ArgumentException(
                    $"The check at index {index} is null.",
                    nameof(checks));
            }
        }

        return snapshot;
    }

    private static Task<Result<TValue, TError>> CheckTask<TValue, TError>(
        Result<TValue, TError> result,
        Task<UnitResult<TError>> task)
    {
        if (task is null)
        {
            return Task.FromException<Result<TValue, TError>>(
                new NullReferenceException("The check returned a null task."));
        }

        if (task.IsCompletedSuccessfully)
        {
            try
            {
                return Task.FromResult(
                    CheckedOrFailure(result, task.GetAwaiter().GetResult()));
            }
            catch (Exception exception)
            {
                return Result.FromException<Result<TValue, TError>>(exception);
            }
        }

        if (task.IsCanceled)
        {
            return CanceledTask<Result<TValue, TError>>(task);
        }

        if (task.IsFaulted)
        {
            return FaultedTask<Result<TValue, TError>>(task);
        }

        return new CheckTaskState<TValue, TError>(result).Continue(task);
    }

    private static ValueTask<Result<TValue, TError>> CompleteValueTask<TValue, TError>(
        Result<TValue, TError> result,
        ValueTask<UnitResult<TError>> task)
    {
        if (task.IsCompletedSuccessfully)
        {
            try
            {
                return ValueTask.FromResult(
                    CheckedOrFailure(result, task.Result));
            }
            catch (Exception exception)
            {
                return new ValueTask<Result<TValue, TError>>(
                    Result.FromException<Result<TValue, TError>>(exception));
            }
        }

        return new ValueTask<Result<TValue, TError>>(
            CheckTask(result, AsTask(task)));
    }

    private static Task<Validation<TValue, TError>> CheckAllAsyncCore<TValue, TError>(
        TValue value,
        Func<TValue, Task<UnitResult<TError>>>[] checks)
    {
        List<TError>? errors = null;

        for (var index = 0; index < checks.Length; index++)
        {
            var task = Invoke(checks[index], value);

            if (task is null)
            {
                return Task.FromException<Validation<TValue, TError>>(
                    new NullReferenceException("The check returned a null task."));
            }

            if (task.IsCompletedSuccessfully)
            {
                try
                {
                    AddError(task.GetAwaiter().GetResult(), ref errors);
                }
                catch (Exception exception)
                {
                    return Result.FromException<Validation<TValue, TError>>(exception);
                }

                continue;
            }

            if (task.IsCanceled)
            {
                return CanceledTask<Validation<TValue, TError>>(task);
            }

            if (task.IsFaulted)
            {
                return FaultedTask<Validation<TValue, TError>>(task);
            }

            return new CheckAllState<TValue, TError>(
                    value,
                    checks.Length,
                    index,
                    errors,
                    next => AsValueTask(checks[next](value)))
                .Continue(task);
        }

        return Task.FromResult(BuildValidation(value, errors));
    }

    private static Task<Validation<TValue, TError>> CheckAllAsyncCore<TValue, TError>(
        TValue value,
        Func<TValue, CancellationToken, Task<UnitResult<TError>>>[] checks,
        CancellationToken cancellationToken)
    {
        List<TError>? errors = null;

        for (var index = 0; index < checks.Length; index++)
        {
            var task = Invoke(checks[index], value, cancellationToken);

            if (task is null)
            {
                return Task.FromException<Validation<TValue, TError>>(
                    new NullReferenceException("The check returned a null task."));
            }

            if (task.IsCompletedSuccessfully)
            {
                try
                {
                    AddError(task.GetAwaiter().GetResult(), ref errors);
                }
                catch (Exception exception)
                {
                    return Result.FromException<Validation<TValue, TError>>(exception);
                }

                continue;
            }

            if (task.IsCanceled)
            {
                return CanceledTask<Validation<TValue, TError>>(task);
            }

            if (task.IsFaulted)
            {
                return FaultedTask<Validation<TValue, TError>>(task);
            }

            return new CheckAllState<TValue, TError>(
                    value,
                    checks.Length,
                    index,
                    errors,
                    next => AsValueTask(checks[next](value, cancellationToken)))
                .Continue(task);
        }

        return Task.FromResult(BuildValidation(value, errors));
    }

    private static ValueTask<Validation<TValue, TError>> CheckAllValueAsyncCore<TValue, TError>(
        TValue value,
        Func<TValue, ValueTask<UnitResult<TError>>>[] checks)
    {
        List<TError>? errors = null;

        for (var index = 0; index < checks.Length; index++)
        {
            var task = Invoke(checks[index], value);

            if (task.IsCompletedSuccessfully)
            {
                try
                {
                    AddError(task.Result, ref errors);
                }
                catch (Exception exception)
                {
                    return new ValueTask<Validation<TValue, TError>>(
                        Result.FromException<Validation<TValue, TError>>(exception));
                }

                continue;
            }

            var asTask = AsTask(task);

            if (asTask.IsCompletedSuccessfully)
            {
                try
                {
                    AddError(asTask.GetAwaiter().GetResult(), ref errors);
                }
                catch (Exception exception)
                {
                    return new ValueTask<Validation<TValue, TError>>(
                        Result.FromException<Validation<TValue, TError>>(exception));
                }

                continue;
            }

            if (asTask.IsCanceled)
            {
                return new ValueTask<Validation<TValue, TError>>(
                    CanceledTask<Validation<TValue, TError>>(asTask));
            }

            if (asTask.IsFaulted)
            {
                return new ValueTask<Validation<TValue, TError>>(
                    FaultedTask<Validation<TValue, TError>>(asTask));
            }

            return new ValueTask<Validation<TValue, TError>>(
                new CheckAllState<TValue, TError>(
                        value,
                        checks.Length,
                        index,
                        errors,
                        next => checks[next](value))
                    .Continue(asTask));
        }

        return ValueTask.FromResult(BuildValidation(value, errors));
    }

    private static ValueTask<Validation<TValue, TError>> CheckAllValueAsyncCore<TValue, TError>(
        TValue value,
        Func<TValue, CancellationToken, ValueTask<UnitResult<TError>>>[] checks,
        CancellationToken cancellationToken)
    {
        List<TError>? errors = null;

        for (var index = 0; index < checks.Length; index++)
        {
            var task = Invoke(checks[index], value, cancellationToken);

            if (task.IsCompletedSuccessfully)
            {
                try
                {
                    AddError(task.Result, ref errors);
                }
                catch (Exception exception)
                {
                    return new ValueTask<Validation<TValue, TError>>(
                        Result.FromException<Validation<TValue, TError>>(exception));
                }

                continue;
            }

            var asTask = AsTask(task);

            if (asTask.IsCompletedSuccessfully)
            {
                try
                {
                    AddError(asTask.GetAwaiter().GetResult(), ref errors);
                }
                catch (Exception exception)
                {
                    return new ValueTask<Validation<TValue, TError>>(
                        Result.FromException<Validation<TValue, TError>>(exception));
                }

                continue;
            }

            if (asTask.IsCanceled)
            {
                return new ValueTask<Validation<TValue, TError>>(
                    CanceledTask<Validation<TValue, TError>>(asTask));
            }

            if (asTask.IsFaulted)
            {
                return new ValueTask<Validation<TValue, TError>>(
                    FaultedTask<Validation<TValue, TError>>(asTask));
            }

            return new ValueTask<Validation<TValue, TError>>(
                new CheckAllState<TValue, TError>(
                        value,
                        checks.Length,
                        index,
                        errors,
                        next => checks[next](value, cancellationToken))
                    .Continue(asTask));
        }

        return ValueTask.FromResult(BuildValidation(value, errors));
    }

    private static void AddError<TError>(
        UnitResult<TError> check,
        ref List<TError>? errors)
    {
        if (check.TryGetError(out var error))
        {
            (errors ??= new List<TError>()).Add(error!);
        }
    }

    private static Task<UnitResult<TError>> Invoke<TValue, TError>(
        Func<TValue, Task<UnitResult<TError>>> check,
        TValue value)
    {
        try
        {
            return check(value);
        }
        catch (OperationCanceledException cancellation)
        {
            return Result.FromException<UnitResult<TError>>(cancellation);
        }
        catch (Exception exception)
        {
            return Task.FromException<UnitResult<TError>>(exception);
        }
    }

    private static Task<UnitResult<TError>> Invoke<TValue, TError>(
        Func<TValue, CancellationToken, Task<UnitResult<TError>>> check,
        TValue value,
        CancellationToken cancellationToken)
    {
        try
        {
            return check(value, cancellationToken);
        }
        catch (OperationCanceledException cancellation)
        {
            return Result.FromException<UnitResult<TError>>(cancellation);
        }
        catch (Exception exception)
        {
            return Task.FromException<UnitResult<TError>>(exception);
        }
    }

    private static ValueTask<UnitResult<TError>> Invoke<TValue, TError>(
        Func<TValue, ValueTask<UnitResult<TError>>> check,
        TValue value)
    {
        try
        {
            return check(value);
        }
        catch (OperationCanceledException cancellation)
        {
            return new ValueTask<UnitResult<TError>>(
                Result.FromException<UnitResult<TError>>(cancellation));
        }
        catch (Exception exception)
        {
            return new ValueTask<UnitResult<TError>>(
                Task.FromException<UnitResult<TError>>(exception));
        }
    }

    private static ValueTask<UnitResult<TError>> Invoke<TValue, TError>(
        Func<TValue, CancellationToken, ValueTask<UnitResult<TError>>> check,
        TValue value,
        CancellationToken cancellationToken)
    {
        try
        {
            return check(value, cancellationToken);
        }
        catch (OperationCanceledException cancellation)
        {
            return new ValueTask<UnitResult<TError>>(
                Result.FromException<UnitResult<TError>>(cancellation));
        }
        catch (Exception exception)
        {
            return new ValueTask<UnitResult<TError>>(
                Task.FromException<UnitResult<TError>>(exception));
        }
    }

    private static Task<T> AsTask<T>(ValueTask<T> task)
    {
        try
        {
            return task.AsTask();
        }
        catch (OperationCanceledException cancellation)
        {
            return Result.FromException<T>(cancellation);
        }
        catch (Exception exception)
        {
            return Task.FromException<T>(exception);
        }
    }

    private static ValueTask<UnitResult<TError>> AsValueTask<TError>(
        Task<UnitResult<TError>>? task) =>
        task is null
            ? new ValueTask<UnitResult<TError>>(
                Task.FromException<UnitResult<TError>>(
                    new NullReferenceException("The check returned a null task.")))
            : new ValueTask<UnitResult<TError>>(task);

    private static Task<TResult> CanceledTask<TResult>(Task task)
    {
        try
        {
            task.GetAwaiter().GetResult();
        }
        catch (OperationCanceledException cancellation)
        {
            return Result.FromException<TResult>(cancellation);
        }

        return Task.FromException<TResult>(
            new InvalidOperationException("The task was expected to be canceled."));
    }

    private static Task<TResult> FaultedTask<TResult>(Task task)
    {
        var completion = new TaskCompletionSource<TResult>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        completion.TrySetException(task.Exception!.InnerExceptions);
        return completion.Task;
    }

    private static CancellationToken GetCancellationToken(Task task)
    {
        try
        {
            task.GetAwaiter().GetResult();
        }
        catch (OperationCanceledException cancellation)
        {
            return cancellation.CancellationToken;
        }

        return CancellationToken.None;
    }

    private static void CompleteFailure<T>(
        TaskCompletionSource<T> completion,
        Exception exception)
    {
        if (exception is OperationCanceledException cancellation)
        {
            completion.TrySetCanceled(cancellation.CancellationToken);
        }
        else
        {
            completion.TrySetException(exception);
        }
    }

    private sealed class CheckTaskState<TValue, TError>
    {
        private readonly Result<TValue, TError> result;
        private readonly TaskCompletionSource<Result<TValue, TError>> completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        internal CheckTaskState(Result<TValue, TError> result)
        {
            this.result = result;
        }

        internal Task<Result<TValue, TError>> Continue(Task<UnitResult<TError>> task)
        {
            try
            {
                _ = task.ContinueWith(
                    static (completed, state) =>
                        ((CheckTaskState<TValue, TError>)state!).Complete(completed),
                    this,
                    CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
            }
            catch (Exception exception)
            {
                CompleteFailure(completion, exception);
            }

            return completion.Task;
        }

        private void Complete(Task<UnitResult<TError>> task)
        {
            try
            {
                if (task.IsCanceled)
                {
                    completion.TrySetCanceled(GetCancellationToken(task));
                    return;
                }

                if (task.IsFaulted)
                {
                    completion.TrySetException(task.Exception!.InnerExceptions);
                    return;
                }

                completion.TrySetResult(
                    CheckedOrFailure(result, task.GetAwaiter().GetResult()));
            }
            catch (OperationCanceledException cancellation)
            {
                completion.TrySetCanceled(cancellation.CancellationToken);
            }
            catch (Exception exception)
            {
                completion.TrySetException(exception);
            }
        }
    }

    private sealed class CheckAllState<TValue, TError>
    {
        private readonly TValue value;
        private readonly int checkCount;
        private readonly Func<int, ValueTask<UnitResult<TError>>> invoke;
        private readonly TaskCompletionSource<Validation<TValue, TError>> completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        private List<TError>? errors;
        private int index;

        internal CheckAllState(
            TValue value,
            int checkCount,
            int index,
            List<TError>? errors,
            Func<int, ValueTask<UnitResult<TError>>> invoke)
        {
            this.value = value;
            this.checkCount = checkCount;
            this.index = index;
            this.errors = errors;
            this.invoke = invoke;
        }

        internal Task<Validation<TValue, TError>> Continue(Task<UnitResult<TError>> task)
        {
            try
            {
                Observe(task);
            }
            catch (Exception exception)
            {
                CompleteFailure(completion, exception);
            }

            return completion.Task;
        }

        private void Observe(Task<UnitResult<TError>> task)
        {
            if (task.IsCompletedSuccessfully)
            {
                Resume(task);
                return;
            }

            if (task.IsCanceled)
            {
                completion.TrySetCanceled(GetCancellationToken(task));
                return;
            }

            if (task.IsFaulted)
            {
                completion.TrySetException(task.Exception!.InnerExceptions);
                return;
            }

            _ = task.ContinueWith(
                static (completed, state) =>
                    ((CheckAllState<TValue, TError>)state!).OnCompleted(completed),
                this,
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }

        private void OnCompleted(Task<UnitResult<TError>> task)
        {
            try
            {
                if (task.IsCanceled)
                {
                    completion.TrySetCanceled(GetCancellationToken(task));
                    return;
                }

                if (task.IsFaulted)
                {
                    completion.TrySetException(task.Exception!.InnerExceptions);
                    return;
                }

                Resume(task);
            }
            catch (Exception exception)
            {
                CompleteFailure(completion, exception);
            }
        }

        private void Resume(Task<UnitResult<TError>> task)
        {
            var check = task.GetAwaiter().GetResult();
            AddError(check, ref errors);

            for (var next = index + 1; next < checkCount; next++)
            {
                index = next;
                var taskNext = invoke(next);

                if (taskNext.IsCompletedSuccessfully)
                {
                    AddError(taskNext.Result, ref errors);
                    continue;
                }

                var asTask = AsTask(taskNext);

                if (asTask.IsCompletedSuccessfully)
                {
                    AddError(asTask.GetAwaiter().GetResult(), ref errors);
                    continue;
                }

                if (asTask.IsCanceled)
                {
                    completion.TrySetCanceled(GetCancellationToken(asTask));
                    return;
                }

                if (asTask.IsFaulted)
                {
                    completion.TrySetException(asTask.Exception!.InnerExceptions);
                    return;
                }

                Observe(asTask);
                return;
            }

            completion.TrySetResult(BuildValidation(value, errors));
        }
    }
}
