using System.Runtime.ExceptionServices;

namespace FunnySharp;

/// <summary>
/// Provides inference-friendly factories for deferred effectful computations.
/// </summary>
public static class Effect
{
    /// <summary>
    /// Creates an effect that returns <paramref name="value"/> when run.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="value">The value returned by the effect.</param>
    /// <returns>A deferred effect that returns <paramref name="value"/>.</returns>
    public static Effect<T> FromValue<T>(T value) =>
        new(_ => ValueTask.FromResult(value));

    /// <summary>
    /// Creates an effect that returns <paramref name="result"/> as an ordinary value when run.
    /// </summary>
    /// <typeparam name="TValue">The result value type.</typeparam>
    /// <typeparam name="TError">The result error type.</typeparam>
    /// <param name="result">The result returned by the effect.</param>
    /// <returns>A deferred effect that returns <paramref name="result"/>.</returns>
    public static Effect<Result<TValue, TError>> FromResult<TValue, TError>(
        Result<TValue, TError> result) =>
        FromValue(result);

    /// <summary>
    /// Creates an effect from a synchronous operation.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="operation">The operation invoked each time the effect is run.</param>
    /// <returns>A deferred effect for <paramref name="operation"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="operation"/> is <see langword="null"/>.</exception>
    public static Effect<T> FromSync<T>(Func<T> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        return new(_ => new ValueTask<T>(operation()));
    }

    /// <summary>
    /// Creates an effect from a cancellation-aware synchronous operation.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="operation">The operation invoked with the caller's cancellation token on every run.</param>
    /// <returns>A deferred effect for <paramref name="operation"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="operation"/> is <see langword="null"/>.</exception>
    public static Effect<T> FromSync<T>(Func<CancellationToken, T> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        return new(cancellationToken => new ValueTask<T>(operation(cancellationToken)));
    }

    /// <summary>
    /// Creates an effect from a task-returning operation.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="operation">The operation invoked each time the effect is run.</param>
    /// <returns>A deferred effect for <paramref name="operation"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="operation"/> is <see langword="null"/>.</exception>
    public static Effect<T> FromTask<T>(Func<Task<T>> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        return new(_ => new ValueTask<T>(operation()));
    }

    /// <summary>
    /// Creates an effect from a cancellation-aware task-returning operation.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="operation">The operation invoked with the caller's cancellation token on every run.</param>
    /// <returns>A deferred effect for <paramref name="operation"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="operation"/> is <see langword="null"/>.</exception>
    public static Effect<T> FromTask<T>(Func<CancellationToken, Task<T>> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        return new(cancellationToken => new ValueTask<T>(operation(cancellationToken)));
    }

    /// <summary>
    /// Creates an effect from a value-task-returning operation.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="operation">The operation invoked each time the effect is run.</param>
    /// <returns>A deferred effect for <paramref name="operation"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="operation"/> is <see langword="null"/>.</exception>
    public static Effect<T> FromValueTask<T>(Func<ValueTask<T>> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        return new(_ => operation());
    }

    /// <summary>
    /// Creates an effect from a cancellation-aware value-task-returning operation.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="operation">The operation invoked with the caller's cancellation token on every run.</param>
    /// <returns>A deferred effect for <paramref name="operation"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="operation"/> is <see langword="null"/>.</exception>
    public static Effect<T> FromValueTask<T>(Func<CancellationToken, ValueTask<T>> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        return new(operation);
    }

    /// <summary>
    /// Creates an environment-dependent effect from a synchronous operation.
    /// </summary>
    /// <typeparam name="TEnvironment">The required environment type.</typeparam>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="operation">The operation invoked with the supplied environment on every run.</param>
    /// <returns>A deferred environment-dependent effect for <paramref name="operation"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="operation"/> is <see langword="null"/>.</exception>
    public static Effect<TEnvironment, T> FromSync<TEnvironment, T>(Func<TEnvironment, T> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        return new((environment, _) => new ValueTask<T>(operation(environment)));
    }

    /// <summary>
    /// Creates an environment-dependent effect from a cancellation-aware synchronous operation.
    /// </summary>
    /// <typeparam name="TEnvironment">The required environment type.</typeparam>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="operation">The operation invoked with the environment and caller's cancellation token.</param>
    /// <returns>A deferred environment-dependent effect for <paramref name="operation"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="operation"/> is <see langword="null"/>.</exception>
    public static Effect<TEnvironment, T> FromSync<TEnvironment, T>(
        Func<TEnvironment, CancellationToken, T> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        return new((environment, cancellationToken) => new ValueTask<T>(operation(environment, cancellationToken)));
    }

    /// <summary>
    /// Creates an environment-dependent effect from a task-returning operation.
    /// </summary>
    /// <typeparam name="TEnvironment">The required environment type.</typeparam>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="operation">The operation invoked with the supplied environment on every run.</param>
    /// <returns>A deferred environment-dependent effect for <paramref name="operation"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="operation"/> is <see langword="null"/>.</exception>
    public static Effect<TEnvironment, T> FromTask<TEnvironment, T>(Func<TEnvironment, Task<T>> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        return new((environment, _) => new ValueTask<T>(operation(environment)));
    }

    /// <summary>
    /// Creates an environment-dependent effect from a cancellation-aware task-returning operation.
    /// </summary>
    /// <typeparam name="TEnvironment">The required environment type.</typeparam>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="operation">The operation invoked with the environment and caller's cancellation token.</param>
    /// <returns>A deferred environment-dependent effect for <paramref name="operation"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="operation"/> is <see langword="null"/>.</exception>
    public static Effect<TEnvironment, T> FromTask<TEnvironment, T>(
        Func<TEnvironment, CancellationToken, Task<T>> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        return new((environment, cancellationToken) => new ValueTask<T>(operation(environment, cancellationToken)));
    }

    /// <summary>
    /// Creates an environment-dependent effect from a value-task-returning operation.
    /// </summary>
    /// <typeparam name="TEnvironment">The required environment type.</typeparam>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="operation">The operation invoked with the supplied environment on every run.</param>
    /// <returns>A deferred environment-dependent effect for <paramref name="operation"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="operation"/> is <see langword="null"/>.</exception>
    public static Effect<TEnvironment, T> FromValueTask<TEnvironment, T>(
        Func<TEnvironment, ValueTask<T>> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        return new((environment, _) => operation(environment));
    }

    /// <summary>
    /// Creates an environment-dependent effect from a cancellation-aware value-task-returning operation.
    /// </summary>
    /// <typeparam name="TEnvironment">The required environment type.</typeparam>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="operation">The operation invoked with the environment and caller's cancellation token.</param>
    /// <returns>A deferred environment-dependent effect for <paramref name="operation"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="operation"/> is <see langword="null"/>.</exception>
    public static Effect<TEnvironment, T> FromValueTask<TEnvironment, T>(
        Func<TEnvironment, CancellationToken, ValueTask<T>> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        return new(operation);
    }

    internal static ValueTask<TResult> MapCore<T, TResult>(
        ValueTask<T> operation,
        Func<T, TResult> selector)
    {
        return operation.IsCompletedSuccessfully
            ? ValueTask.FromResult(selector(operation.Result))
            : Awaited(operation, selector);

        static async ValueTask<TResult> Awaited(
            ValueTask<T> operation,
            Func<T, TResult> selector) =>
            selector(await operation.ConfigureAwait(false));
    }

    internal static ValueTask<T> Invoke<T>(
        Func<CancellationToken, ValueTask<T>> runner,
        CancellationToken cancellationToken)
    {
        try
        {
            return runner(cancellationToken);
        }
        catch (Exception exception)
        {
            return CaptureSynchronousFailure<T>(exception);
        }
    }

    internal static ValueTask<T> Invoke<TEnvironment, T>(
        Func<TEnvironment, CancellationToken, ValueTask<T>> runner,
        TEnvironment environment,
        CancellationToken cancellationToken)
    {
        try
        {
            return runner(environment, cancellationToken);
        }
        catch (Exception exception)
        {
            return CaptureSynchronousFailure<T>(exception);
        }
    }

    private static async ValueTask<T> CaptureSynchronousFailure<T>(Exception exception)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        ExceptionDispatchInfo.Capture(exception).Throw();
        return default!;
    }
}

/// <summary>
/// Represents a deferred computation that produces a value or propagates ordinary .NET failure and cancellation.
/// </summary>
/// <typeparam name="T">The produced value type.</typeparam>
public readonly struct Effect<T>
{
    private readonly Func<CancellationToken, ValueTask<T>>? runner;

    internal Effect(Func<CancellationToken, ValueTask<T>> runner) => this.runner = runner;

    /// <summary>
    /// Runs this effect with the supplied cancellation token.
    /// </summary>
    /// <param name="cancellationToken">The token forwarded to cancellation-aware operations.</param>
    /// <returns>An awaitable operation that produces the effect value.</returns>
    /// <exception cref="InvalidOperationException">This effect is the default value.</exception>
    public ValueTask<T> RunAsync(CancellationToken cancellationToken = default) =>
        runner is null
            ? ValueTask.FromException<T>(new InvalidOperationException("The effect has not been initialized."))
            : Effect.Invoke(runner, cancellationToken);

    /// <summary>
    /// Transforms this effect's value when it is run.
    /// </summary>
    /// <typeparam name="TResult">The transformed value type.</typeparam>
    /// <param name="selector">The transformation invoked after this effect completes.</param>
    /// <returns>A deferred effect that applies <paramref name="selector"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="selector"/> is <see langword="null"/>.</exception>
    public Effect<TResult> Map<TResult>(Func<T, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        var source = this;
        return new(cancellationToken => Effect.MapCore(source.RunAsync(cancellationToken), selector));
    }

    /// <summary>
    /// Binds this effect's value to another effect when it is run.
    /// </summary>
    /// <typeparam name="TResult">The bound effect value type.</typeparam>
    /// <param name="binder">The effect-producing function invoked after this effect completes.</param>
    /// <returns>A deferred effect that runs the bound effect.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="binder"/> is <see langword="null"/>.</exception>
    public Effect<TResult> Bind<TResult>(Func<T, Effect<TResult>> binder)
    {
        ArgumentNullException.ThrowIfNull(binder);
        var source = this;
        return new(cancellationToken => BindCore(source.RunAsync(cancellationToken), binder, cancellationToken));
    }

    /// <summary>
    /// Lifts this environment-independent effect to one that accepts and ignores an environment.
    /// </summary>
    /// <typeparam name="TEnvironment">The environment type accepted by the returned effect.</typeparam>
    /// <returns>A deferred environment-dependent view of this effect.</returns>
    public Effect<TEnvironment, T> WithEnvironment<TEnvironment>()
    {
        var source = this;
        return new((_, cancellationToken) => source.RunAsync(cancellationToken));
    }

    /// <summary>
    /// Transforms this effect's value for query-expression syntax.
    /// </summary>
    /// <typeparam name="TResult">The transformed value type.</typeparam>
    /// <param name="selector">The transformation invoked after this effect completes.</param>
    /// <returns>A deferred effect that applies <paramref name="selector"/>.</returns>
    public Effect<TResult> Select<TResult>(Func<T, TResult> selector) => Map(selector);

    /// <summary>
    /// Composes this effect with another effect for query-expression syntax.
    /// </summary>
    /// <typeparam name="TIntermediate">The intermediate effect value type.</typeparam>
    /// <typeparam name="TResult">The projected value type.</typeparam>
    /// <param name="binder">The effect-producing function invoked after this effect completes.</param>
    /// <param name="projector">The function that combines both effect values.</param>
    /// <returns>A deferred effect that runs both effects in order and projects their values.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="binder"/> or <paramref name="projector"/> is <see langword="null"/>.
    /// </exception>
    public Effect<TResult> SelectMany<TIntermediate, TResult>(
        Func<T, Effect<TIntermediate>> binder,
        Func<T, TIntermediate, TResult> projector)
    {
        ArgumentNullException.ThrowIfNull(binder);
        ArgumentNullException.ThrowIfNull(projector);
        var source = this;
        return new(cancellationToken => SelectManyCore(
            source.RunAsync(cancellationToken),
            binder,
            projector,
            cancellationToken));
    }

    private static ValueTask<TResult> BindCore<TResult>(
        ValueTask<T> operation,
        Func<T, Effect<TResult>> binder,
        CancellationToken cancellationToken)
    {
        return operation.IsCompletedSuccessfully
            ? binder(operation.Result).RunAsync(cancellationToken)
            : Awaited(operation, binder, cancellationToken);

        static async ValueTask<TResult> Awaited(
            ValueTask<T> operation,
            Func<T, Effect<TResult>> binder,
            CancellationToken cancellationToken)
        {
            var value = await operation.ConfigureAwait(false);
            return await binder(value).RunAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private static async ValueTask<TResult> SelectManyCore<TIntermediate, TResult>(
        ValueTask<T> operation,
        Func<T, Effect<TIntermediate>> binder,
        Func<T, TIntermediate, TResult> projector,
        CancellationToken cancellationToken)
    {
        var value = await operation.ConfigureAwait(false);
        var intermediate = await binder(value).RunAsync(cancellationToken).ConfigureAwait(false);
        return projector(value, intermediate);
    }
}

/// <summary>
/// Represents a deferred computation that requires an explicit environment and produces a value.
/// </summary>
/// <typeparam name="TEnvironment">The required environment type.</typeparam>
/// <typeparam name="T">The produced value type.</typeparam>
public readonly struct Effect<TEnvironment, T>
{
    private readonly Func<TEnvironment, CancellationToken, ValueTask<T>>? runner;

    internal Effect(Func<TEnvironment, CancellationToken, ValueTask<T>> runner) => this.runner = runner;

    /// <summary>
    /// Runs this effect with an environment and cancellation token.
    /// </summary>
    /// <param name="environment">The environment supplied to the computation.</param>
    /// <param name="cancellationToken">The token forwarded to cancellation-aware operations.</param>
    /// <returns>An awaitable operation that produces the effect value.</returns>
    /// <exception cref="InvalidOperationException">This effect is the default value.</exception>
    public ValueTask<T> RunAsync(TEnvironment environment, CancellationToken cancellationToken = default) =>
        runner is null
            ? ValueTask.FromException<T>(new InvalidOperationException("The effect has not been initialized."))
            : Effect.Invoke(runner, environment, cancellationToken);

    /// <summary>
    /// Supplies a fixed environment to this effect.
    /// </summary>
    /// <param name="environment">The environment supplied whenever the returned effect is run.</param>
    /// <returns>A deferred effect that no longer requires an environment.</returns>
    public Effect<T> Provide(TEnvironment environment)
    {
        var source = this;
        return new(cancellationToken => source.RunAsync(environment, cancellationToken));
    }

    /// <summary>
    /// Transforms this effect's value when it is run.
    /// </summary>
    /// <typeparam name="TResult">The transformed value type.</typeparam>
    /// <param name="selector">The transformation invoked after this effect completes.</param>
    /// <returns>A deferred environment-dependent effect that applies <paramref name="selector"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="selector"/> is <see langword="null"/>.</exception>
    public Effect<TEnvironment, TResult> Map<TResult>(Func<T, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        var source = this;
        return new((environment, cancellationToken) =>
            Effect.MapCore(source.RunAsync(environment, cancellationToken), selector));
    }

    /// <summary>
    /// Binds this effect's value to another effect that uses the same environment.
    /// </summary>
    /// <typeparam name="TResult">The bound effect value type.</typeparam>
    /// <param name="binder">The effect-producing function invoked after this effect completes.</param>
    /// <returns>A deferred environment-dependent effect that runs the bound effect.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="binder"/> is <see langword="null"/>.</exception>
    public Effect<TEnvironment, TResult> Bind<TResult>(Func<T, Effect<TEnvironment, TResult>> binder)
    {
        ArgumentNullException.ThrowIfNull(binder);
        var source = this;
        return new((environment, cancellationToken) =>
            BindCore(source.RunAsync(environment, cancellationToken), binder, environment, cancellationToken));
    }

    /// <summary>
    /// Transforms this effect's value for query-expression syntax.
    /// </summary>
    /// <typeparam name="TResult">The transformed value type.</typeparam>
    /// <param name="selector">The transformation invoked after this effect completes.</param>
    /// <returns>A deferred environment-dependent effect that applies <paramref name="selector"/>.</returns>
    public Effect<TEnvironment, TResult> Select<TResult>(Func<T, TResult> selector) => Map(selector);

    /// <summary>
    /// Composes this effect with another effect that uses the same environment for query-expression syntax.
    /// </summary>
    /// <typeparam name="TIntermediate">The intermediate effect value type.</typeparam>
    /// <typeparam name="TResult">The projected value type.</typeparam>
    /// <param name="binder">The effect-producing function invoked after this effect completes.</param>
    /// <param name="projector">The function that combines both effect values.</param>
    /// <returns>A deferred environment-dependent effect that runs both effects in order.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="binder"/> or <paramref name="projector"/> is <see langword="null"/>.
    /// </exception>
    public Effect<TEnvironment, TResult> SelectMany<TIntermediate, TResult>(
        Func<T, Effect<TEnvironment, TIntermediate>> binder,
        Func<T, TIntermediate, TResult> projector)
    {
        ArgumentNullException.ThrowIfNull(binder);
        ArgumentNullException.ThrowIfNull(projector);
        var source = this;
        return new((environment, cancellationToken) => SelectManyCore(
            source.RunAsync(environment, cancellationToken),
            binder,
            projector,
            environment,
            cancellationToken));
    }

    private static ValueTask<TResult> BindCore<TResult>(
        ValueTask<T> operation,
        Func<T, Effect<TEnvironment, TResult>> binder,
        TEnvironment environment,
        CancellationToken cancellationToken)
    {
        return operation.IsCompletedSuccessfully
            ? binder(operation.Result).RunAsync(environment, cancellationToken)
            : Awaited(operation, binder, environment, cancellationToken);

        static async ValueTask<TResult> Awaited(
            ValueTask<T> operation,
            Func<T, Effect<TEnvironment, TResult>> binder,
            TEnvironment environment,
            CancellationToken cancellationToken)
        {
            var value = await operation.ConfigureAwait(false);
            return await binder(value).RunAsync(environment, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async ValueTask<TResult> SelectManyCore<TIntermediate, TResult>(
        ValueTask<T> operation,
        Func<T, Effect<TEnvironment, TIntermediate>> binder,
        Func<T, TIntermediate, TResult> projector,
        TEnvironment environment,
        CancellationToken cancellationToken)
    {
        var value = await operation.ConfigureAwait(false);
        var intermediate = await binder(value)
            .RunAsync(environment, cancellationToken)
            .ConfigureAwait(false);
        return projector(value, intermediate);
    }
}
