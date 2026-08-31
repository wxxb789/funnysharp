namespace FunnySharp;

/// <summary>
/// Provides small, composable helpers for synchronous and asynchronous functions.
/// </summary>
public static class FunctionExtensions
{
    /// <summary>
    /// Applies <paramref name="function"/> to <paramref name="value"/>.
    /// </summary>
    /// <typeparam name="T">The input value type.</typeparam>
    /// <typeparam name="TResult">The function result type.</typeparam>
    /// <param name="value">The value to pass to <paramref name="function"/>.</param>
    /// <param name="function">The function to apply.</param>
    /// <returns>The result produced by <paramref name="function"/>.</returns>
    public static TResult Pipe<T, TResult>(this T value, Func<T, TResult> function)
    {
        ArgumentNullException.ThrowIfNull(function);
        return function(value);
    }

    /// <summary>
    /// Composes two functions from left to right.
    /// </summary>
    /// <typeparam name="T">The first function input type.</typeparam>
    /// <typeparam name="TIntermediate">The shared intermediate type.</typeparam>
    /// <typeparam name="TResult">The second function result type.</typeparam>
    /// <param name="first">The function applied first.</param>
    /// <param name="second">The function applied after <paramref name="first"/>.</param>
    /// <returns>A function that applies <paramref name="first"/> and then <paramref name="second"/>.</returns>
    public static Func<T, TResult> Compose<T, TIntermediate, TResult>(
        this Func<T, TIntermediate> first,
        Func<TIntermediate, TResult> second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        return value => second(first(value));
    }

    /// <summary>
    /// Converts a binary function into a curried function.
    /// </summary>
    /// <typeparam name="TFirst">The first argument type.</typeparam>
    /// <typeparam name="TSecond">The second argument type.</typeparam>
    /// <typeparam name="TResult">The function result type.</typeparam>
    /// <param name="function">The binary function to curry.</param>
    /// <returns>A function that accepts the first argument and returns a function for the second argument.</returns>
    public static Func<TFirst, Func<TSecond, TResult>> Curry<TFirst, TSecond, TResult>(
        this Func<TFirst, TSecond, TResult> function)
    {
        ArgumentNullException.ThrowIfNull(function);

        return first => second => function(first, second);
    }

    /// <summary>
    /// Converts a curried binary function into a binary function.
    /// </summary>
    /// <typeparam name="TFirst">The first argument type.</typeparam>
    /// <typeparam name="TSecond">The second argument type.</typeparam>
    /// <typeparam name="TResult">The function result type.</typeparam>
    /// <param name="function">The curried function to uncurry.</param>
    /// <returns>A function that accepts both arguments.</returns>
    public static Func<TFirst, TSecond, TResult> Uncurry<TFirst, TSecond, TResult>(
        this Func<TFirst, Func<TSecond, TResult>> function)
    {
        ArgumentNullException.ThrowIfNull(function);

        return (first, second) => function(first)(second);
    }

    /// <summary>
    /// Binds the first argument of a binary function.
    /// </summary>
    /// <typeparam name="TFirst">The first argument type.</typeparam>
    /// <typeparam name="TSecond">The second argument type.</typeparam>
    /// <typeparam name="TResult">The function result type.</typeparam>
    /// <param name="function">The binary function to partially apply.</param>
    /// <param name="first">The value to bind as the first argument.</param>
    /// <returns>A function that accepts the remaining argument.</returns>
    public static Func<TSecond, TResult> Partial<TFirst, TSecond, TResult>(
        this Func<TFirst, TSecond, TResult> function,
        TFirst first)
    {
        ArgumentNullException.ThrowIfNull(function);

        return second => function(first, second);
    }

    /// <summary>
    /// Reverses the argument order of a binary function.
    /// </summary>
    /// <typeparam name="TFirst">The first argument type.</typeparam>
    /// <typeparam name="TSecond">The second argument type.</typeparam>
    /// <typeparam name="TResult">The function result type.</typeparam>
    /// <param name="function">The binary function to reverse.</param>
    /// <returns>A function that accepts the second argument before the first.</returns>
    public static Func<TSecond, TFirst, TResult> Flip<TFirst, TSecond, TResult>(
        this Func<TFirst, TSecond, TResult> function)
    {
        ArgumentNullException.ThrowIfNull(function);

        return (second, first) => function(first, second);
    }

    /// <summary>
    /// Observes a value and returns that same value.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="value">The value to observe.</param>
    /// <param name="observer">The action that observes <paramref name="value"/>.</param>
    /// <returns><paramref name="value"/>.</returns>
    public static T Tap<T>(this T value, Action<T> observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        observer(value);
        return value;
    }

    /// <summary>
    /// Composes two task-returning functions from left to right.
    /// </summary>
    /// <typeparam name="T">The first function input type.</typeparam>
    /// <typeparam name="TIntermediate">The shared intermediate type.</typeparam>
    /// <typeparam name="TResult">The second function result type.</typeparam>
    /// <param name="first">The function applied first.</param>
    /// <param name="second">The function applied after <paramref name="first"/> completes.</param>
    /// <returns>A task-returning function that applies both functions in order.</returns>
    public static Func<T, Task<TResult>> ComposeAsync<T, TIntermediate, TResult>(
        this Func<T, Task<TIntermediate>> first,
        Func<TIntermediate, Task<TResult>> second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        return async value =>
        {
            var intermediate = await first(value).ConfigureAwait(false);
            return await second(intermediate).ConfigureAwait(false);
        };
    }

    /// <summary>
    /// Composes two value-task-returning functions from left to right.
    /// </summary>
    /// <typeparam name="T">The first function input type.</typeparam>
    /// <typeparam name="TIntermediate">The shared intermediate type.</typeparam>
    /// <typeparam name="TResult">The second function result type.</typeparam>
    /// <param name="first">The function applied first.</param>
    /// <param name="second">The function applied after <paramref name="first"/> completes.</param>
    /// <returns>A value-task-returning function that applies both functions in order.</returns>
    public static Func<T, ValueTask<TResult>> ComposeAsync<T, TIntermediate, TResult>(
        this Func<T, ValueTask<TIntermediate>> first,
        Func<TIntermediate, ValueTask<TResult>> second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        return async value =>
        {
            var intermediate = await first(value).ConfigureAwait(false);
            return await second(intermediate).ConfigureAwait(false);
        };
    }

    /// <summary>
    /// Composes two cancellation-aware task-returning functions from left to right.
    /// </summary>
    /// <typeparam name="T">The first function input type.</typeparam>
    /// <typeparam name="TIntermediate">The shared intermediate type.</typeparam>
    /// <typeparam name="TResult">The second function result type.</typeparam>
    /// <param name="first">The function applied first.</param>
    /// <param name="second">The function applied after <paramref name="first"/> completes.</param>
    /// <returns>A cancellation-aware task-returning function that applies both functions in order.</returns>
    public static Func<T, CancellationToken, Task<TResult>> ComposeAsync<T, TIntermediate, TResult>(
        this Func<T, CancellationToken, Task<TIntermediate>> first,
        Func<TIntermediate, CancellationToken, Task<TResult>> second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        return async (value, cancellationToken) =>
        {
            var intermediate = await first(value, cancellationToken).ConfigureAwait(false);
            return await second(intermediate, cancellationToken).ConfigureAwait(false);
        };
    }

    /// <summary>
    /// Composes two cancellation-aware value-task-returning functions from left to right.
    /// </summary>
    /// <typeparam name="T">The first function input type.</typeparam>
    /// <typeparam name="TIntermediate">The shared intermediate type.</typeparam>
    /// <typeparam name="TResult">The second function result type.</typeparam>
    /// <param name="first">The function applied first.</param>
    /// <param name="second">The function applied after <paramref name="first"/> completes.</param>
    /// <returns>A cancellation-aware value-task-returning function that applies both functions in order.</returns>
    public static Func<T, CancellationToken, ValueTask<TResult>> ComposeAsync<T, TIntermediate, TResult>(
        this Func<T, CancellationToken, ValueTask<TIntermediate>> first,
        Func<TIntermediate, CancellationToken, ValueTask<TResult>> second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        return async (value, cancellationToken) =>
        {
            var intermediate = await first(value, cancellationToken).ConfigureAwait(false);
            return await second(intermediate, cancellationToken).ConfigureAwait(false);
        };
    }

    /// <summary>
    /// Asynchronously observes a value with a task-returning observer and returns that same value.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="value">The value to observe.</param>
    /// <param name="observer">The asynchronous observer.</param>
    /// <returns>A task that completes with <paramref name="value"/> after observation.</returns>
    public static Task<T> TapAsync<T>(this T value, Func<T, Task> observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return TapAsyncCore(value, observer);
    }

    /// <summary>
    /// Asynchronously observes a value with a cancellation-aware task-returning observer and returns that same value.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="value">The value to observe.</param>
    /// <param name="observer">The cancellation-aware asynchronous observer.</param>
    /// <param name="cancellationToken">The token passed to <paramref name="observer"/>.</param>
    /// <returns>A task that completes with <paramref name="value"/> after observation.</returns>
    public static Task<T> TapAsync<T>(
        this T value,
        Func<T, CancellationToken, Task> observer,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return TapAsyncCore(value, observer, cancellationToken);
    }

    /// <summary>
    /// Asynchronously observes a value with a value-task-returning observer and returns that same value.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="value">The value to observe.</param>
    /// <param name="observer">The asynchronous observer.</param>
    /// <returns>A value task that completes with <paramref name="value"/> after observation.</returns>
    public static ValueTask<T> TapValueAsync<T>(this T value, Func<T, ValueTask> observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return TapValueAsyncCore(value, observer);
    }

    /// <summary>
    /// Asynchronously observes a value with a cancellation-aware value-task-returning observer and returns that same value.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="value">The value to observe.</param>
    /// <param name="observer">The cancellation-aware asynchronous observer.</param>
    /// <param name="cancellationToken">The token passed to <paramref name="observer"/>.</param>
    /// <returns>A value task that completes with <paramref name="value"/> after observation.</returns>
    public static ValueTask<T> TapValueAsync<T>(
        this T value,
        Func<T, CancellationToken, ValueTask> observer,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return TapValueAsyncCore(value, observer, cancellationToken);
    }

    private static async Task<T> TapAsyncCore<T>(T value, Func<T, Task> observer)
    {
        await observer(value).ConfigureAwait(false);
        return value;
    }

    private static async Task<T> TapAsyncCore<T>(
        T value,
        Func<T, CancellationToken, Task> observer,
        CancellationToken cancellationToken)
    {
        await observer(value, cancellationToken).ConfigureAwait(false);
        return value;
    }

    private static async ValueTask<T> TapValueAsyncCore<T>(T value, Func<T, ValueTask> observer)
    {
        await observer(value).ConfigureAwait(false);
        return value;
    }

    private static async ValueTask<T> TapValueAsyncCore<T>(
        T value,
        Func<T, CancellationToken, ValueTask> observer,
        CancellationToken cancellationToken)
    {
        await observer(value, cancellationToken).ConfigureAwait(false);
        return value;
    }
}
