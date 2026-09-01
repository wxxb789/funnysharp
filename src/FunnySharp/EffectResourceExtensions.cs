namespace FunnySharp;

/// <summary>
/// Provides resource-scoping operations for effects.
/// </summary>
public static class EffectResourceExtensions
{
    /// <summary>
    /// Acquires a synchronous disposable resource, runs a dependent effect, and disposes the resource afterward.
    /// </summary>
    /// <typeparam name="TResource">The disposable resource type.</typeparam>
    /// <typeparam name="TResult">The value type produced by the dependent effect.</typeparam>
    /// <param name="acquire">The effect that acquires the resource.</param>
    /// <param name="use">The effect-producing function that uses the acquired resource.</param>
    /// <returns>A deferred effect that scopes the resource lifetime to <paramref name="use"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="use"/> is <see langword="null"/>.</exception>
    public static Effect<TResult> Using<TResource, TResult>(
        this Effect<TResource> acquire,
        Func<TResource, Effect<TResult>> use)
        where TResource : IDisposable?
    {
        ArgumentNullException.ThrowIfNull(use);
        return new(cancellationToken => UsingCore(acquire.RunAsync(cancellationToken), use, cancellationToken));
    }

    /// <summary>
    /// Acquires an asynchronous disposable resource, runs a dependent effect, and disposes the resource afterward.
    /// </summary>
    /// <typeparam name="TResource">The asynchronous disposable resource type.</typeparam>
    /// <typeparam name="TResult">The value type produced by the dependent effect.</typeparam>
    /// <param name="acquire">The effect that acquires the resource.</param>
    /// <param name="use">The effect-producing function that uses the acquired resource.</param>
    /// <returns>A deferred effect that scopes the resource lifetime to <paramref name="use"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="use"/> is <see langword="null"/>.</exception>
    public static Effect<TResult> UsingAsync<TResource, TResult>(
        this Effect<TResource> acquire,
        Func<TResource, Effect<TResult>> use)
        where TResource : IAsyncDisposable?
    {
        ArgumentNullException.ThrowIfNull(use);
        return new(cancellationToken => UsingAsyncCore(acquire.RunAsync(cancellationToken), use, cancellationToken));
    }

    /// <summary>
    /// Acquires a synchronous disposable resource from an environment-dependent effect and disposes it afterward.
    /// </summary>
    /// <typeparam name="TEnvironment">The required environment type.</typeparam>
    /// <typeparam name="TResource">The disposable resource type.</typeparam>
    /// <typeparam name="TResult">The value type produced by the dependent effect.</typeparam>
    /// <param name="acquire">The effect that acquires the resource.</param>
    /// <param name="use">The environment-dependent effect-producing function that uses the acquired resource.</param>
    /// <returns>A deferred environment-dependent effect that scopes the resource lifetime to <paramref name="use"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="use"/> is <see langword="null"/>.</exception>
    public static Effect<TEnvironment, TResult> Using<TEnvironment, TResource, TResult>(
        this Effect<TEnvironment, TResource> acquire,
        Func<TResource, Effect<TEnvironment, TResult>> use)
        where TResource : IDisposable?
    {
        ArgumentNullException.ThrowIfNull(use);
        return new((environment, cancellationToken) =>
            UsingCore(acquire.RunAsync(environment, cancellationToken), use, environment, cancellationToken));
    }

    /// <summary>
    /// Acquires an asynchronous disposable resource from an environment-dependent effect and disposes it afterward.
    /// </summary>
    /// <typeparam name="TEnvironment">The required environment type.</typeparam>
    /// <typeparam name="TResource">The asynchronous disposable resource type.</typeparam>
    /// <typeparam name="TResult">The value type produced by the dependent effect.</typeparam>
    /// <param name="acquire">The effect that acquires the resource.</param>
    /// <param name="use">The environment-dependent effect-producing function that uses the acquired resource.</param>
    /// <returns>A deferred environment-dependent effect that scopes the resource lifetime to <paramref name="use"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="use"/> is <see langword="null"/>.</exception>
    public static Effect<TEnvironment, TResult> UsingAsync<TEnvironment, TResource, TResult>(
        this Effect<TEnvironment, TResource> acquire,
        Func<TResource, Effect<TEnvironment, TResult>> use)
        where TResource : IAsyncDisposable?
    {
        ArgumentNullException.ThrowIfNull(use);
        return new((environment, cancellationToken) =>
            UsingAsyncCore(acquire.RunAsync(environment, cancellationToken), use, environment, cancellationToken));
    }

    private static async ValueTask<TResult> UsingCore<TResource, TResult>(
        ValueTask<TResource> acquisition,
        Func<TResource, Effect<TResult>> use,
        CancellationToken cancellationToken)
        where TResource : IDisposable?
    {
        var resource = await acquisition.ConfigureAwait(false);
        if (resource is null)
        {
            throw new InvalidOperationException("The resource acquisition effect returned null.");
        }

        using (resource)
        {
            return await use(resource).RunAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private static async ValueTask<TResult> UsingAsyncCore<TResource, TResult>(
        ValueTask<TResource> acquisition,
        Func<TResource, Effect<TResult>> use,
        CancellationToken cancellationToken)
        where TResource : IAsyncDisposable?
    {
        var resource = await acquisition.ConfigureAwait(false);
        if (resource is null)
        {
            throw new InvalidOperationException("The resource acquisition effect returned null.");
        }

        try
        {
            return await use(resource).RunAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            await resource.DisposeAsync().ConfigureAwait(false);
        }
    }

    private static async ValueTask<TResult> UsingCore<TEnvironment, TResource, TResult>(
        ValueTask<TResource> acquisition,
        Func<TResource, Effect<TEnvironment, TResult>> use,
        TEnvironment environment,
        CancellationToken cancellationToken)
        where TResource : IDisposable?
    {
        var resource = await acquisition.ConfigureAwait(false);
        if (resource is null)
        {
            throw new InvalidOperationException("The resource acquisition effect returned null.");
        }

        using (resource)
        {
            return await use(resource).RunAsync(environment, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async ValueTask<TResult> UsingAsyncCore<TEnvironment, TResource, TResult>(
        ValueTask<TResource> acquisition,
        Func<TResource, Effect<TEnvironment, TResult>> use,
        TEnvironment environment,
        CancellationToken cancellationToken)
        where TResource : IAsyncDisposable?
    {
        var resource = await acquisition.ConfigureAwait(false);
        if (resource is null)
        {
            throw new InvalidOperationException("The resource acquisition effect returned null.");
        }

        try
        {
            return await use(resource).RunAsync(environment, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            await resource.DisposeAsync().ConfigureAwait(false);
        }
    }
}
