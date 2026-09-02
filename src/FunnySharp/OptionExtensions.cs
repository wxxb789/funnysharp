namespace FunnySharp;

/// <summary>
/// Provides bridges between options and common .NET absence conventions.
/// </summary>
public static class OptionExtensions
{
    /// <summary>
    /// Converts a nullable reference to an option.
    /// </summary>
    /// <typeparam name="T">The reference type.</typeparam>
    /// <param name="value">The nullable reference.</param>
    /// <returns>An option containing the reference, or <c>None</c> when it is null.</returns>
    public static Option<T> ToOption<T>(this T? value)
        where T : class =>
        Option.FromNullable(value);

    /// <summary>
    /// Converts a nullable value type to an option.
    /// </summary>
    /// <typeparam name="T">The underlying value type.</typeparam>
    /// <param name="value">The nullable value.</param>
    /// <returns>An option containing the value, or <c>None</c> when it has no value.</returns>
    public static Option<T> ToOption<T>(this T? value)
        where T : struct =>
        Option.FromNullable(value);

    /// <summary>
    /// Looks up a key in a read-only dictionary and converts the result to an option.
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <param name="source">The dictionary to search.</param>
    /// <param name="key">The key to locate.</param>
    /// <returns>An option containing a non-null found value; otherwise, <c>None</c>.</returns>
    public static Option<TValue> GetOption<TKey, TValue>(
        this IReadOnlyDictionary<TKey, TValue> source,
        TKey key)
    {
        ArgumentNullException.ThrowIfNull(source);

        return source.TryGetValue(key, out var value)
            ? Option<TValue>.FromNullable(value)
            : Option<TValue>.None;
    }

    /// <summary>
    /// Asynchronously transforms a present value with a task-returning selector.
    /// </summary>
    /// <typeparam name="T">The source value type.</typeparam>
    /// <typeparam name="TResult">The transformed value type.</typeparam>
    /// <param name="option">The option to transform.</param>
    /// <param name="selector">The asynchronous transformation to invoke for a present value.</param>
    /// <returns>A task that produces the transformed option, or <c>None</c> when <paramref name="option"/> is absent or the transformed value is null.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="selector"/> is null.</exception>
    public static Task<Option<TResult>> MapAsync<T, TResult>(
        this Option<T> option,
        Func<T, Task<TResult>> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        return option.TryGetValue(out var value)
            ? MapAsyncCore(value, selector)
            : Task.FromResult(Option<TResult>.None);
    }

    /// <summary>
    /// Asynchronously transforms a present value with a cancellation-aware task-returning selector.
    /// </summary>
    /// <typeparam name="T">The source value type.</typeparam>
    /// <typeparam name="TResult">The transformed value type.</typeparam>
    /// <param name="option">The option to transform.</param>
    /// <param name="selector">The asynchronous transformation to invoke for a present value.</param>
    /// <param name="cancellationToken">The token passed unchanged to <paramref name="selector"/> when the option is present.</param>
    /// <returns>A task that produces the transformed option, or <c>None</c> when <paramref name="option"/> is absent or the transformed value is null.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="selector"/> is null.</exception>
    public static Task<Option<TResult>> MapAsync<T, TResult>(
        this Option<T> option,
        Func<T, CancellationToken, Task<TResult>> selector,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(selector);

        return option.TryGetValue(out var value)
            ? MapAsyncCore(value, selector, cancellationToken)
            : Task.FromResult(Option<TResult>.None);
    }

    /// <summary>
    /// Asynchronously binds a present value with a task-returning binder.
    /// </summary>
    /// <typeparam name="T">The source value type.</typeparam>
    /// <typeparam name="TResult">The bound value type.</typeparam>
    /// <param name="option">The option to bind.</param>
    /// <param name="binder">The asynchronous option-producing function to invoke for a present value.</param>
    /// <returns>A task that produces the bound option, or <c>None</c> when <paramref name="option"/> is absent.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="binder"/> is null.</exception>
    public static Task<Option<TResult>> BindAsync<T, TResult>(
        this Option<T> option,
        Func<T, Task<Option<TResult>>> binder)
    {
        ArgumentNullException.ThrowIfNull(binder);

        return option.TryGetValue(out var value)
            ? BindAsyncCore(value, binder)
            : Task.FromResult(Option<TResult>.None);
    }

    /// <summary>
    /// Asynchronously binds a present value with a cancellation-aware task-returning binder.
    /// </summary>
    /// <typeparam name="T">The source value type.</typeparam>
    /// <typeparam name="TResult">The bound value type.</typeparam>
    /// <param name="option">The option to bind.</param>
    /// <param name="binder">The asynchronous option-producing function to invoke for a present value.</param>
    /// <param name="cancellationToken">The token passed unchanged to <paramref name="binder"/> when the option is present.</param>
    /// <returns>A task that produces the bound option, or <c>None</c> when <paramref name="option"/> is absent.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="binder"/> is null.</exception>
    public static Task<Option<TResult>> BindAsync<T, TResult>(
        this Option<T> option,
        Func<T, CancellationToken, Task<Option<TResult>>> binder,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(binder);

        return option.TryGetValue(out var value)
            ? BindAsyncCore(value, binder, cancellationToken)
            : Task.FromResult(Option<TResult>.None);
    }

    /// <summary>
    /// Asynchronously transforms a present value with a value-task-returning selector.
    /// </summary>
    /// <typeparam name="T">The source value type.</typeparam>
    /// <typeparam name="TResult">The transformed value type.</typeparam>
    /// <param name="option">The option to transform.</param>
    /// <param name="selector">The asynchronous transformation to invoke for a present value.</param>
    /// <returns>A value task that produces the transformed option, or <c>None</c> when <paramref name="option"/> is absent or the transformed value is null.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="selector"/> is null.</exception>
    public static ValueTask<Option<TResult>> MapValueAsync<T, TResult>(
        this Option<T> option,
        Func<T, ValueTask<TResult>> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        return option.TryGetValue(out var value)
            ? MapValueAsyncCore(value, selector)
            : ValueTask.FromResult(Option<TResult>.None);
    }

    /// <summary>
    /// Asynchronously transforms a present value with a cancellation-aware value-task-returning selector.
    /// </summary>
    /// <typeparam name="T">The source value type.</typeparam>
    /// <typeparam name="TResult">The transformed value type.</typeparam>
    /// <param name="option">The option to transform.</param>
    /// <param name="selector">The asynchronous transformation to invoke for a present value.</param>
    /// <param name="cancellationToken">The token passed unchanged to <paramref name="selector"/> when the option is present.</param>
    /// <returns>A value task that produces the transformed option, or <c>None</c> when <paramref name="option"/> is absent or the transformed value is null.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="selector"/> is null.</exception>
    public static ValueTask<Option<TResult>> MapValueAsync<T, TResult>(
        this Option<T> option,
        Func<T, CancellationToken, ValueTask<TResult>> selector,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(selector);

        return option.TryGetValue(out var value)
            ? MapValueAsyncCore(value, selector, cancellationToken)
            : ValueTask.FromResult(Option<TResult>.None);
    }

    /// <summary>
    /// Asynchronously binds a present value with a value-task-returning binder.
    /// </summary>
    /// <typeparam name="T">The source value type.</typeparam>
    /// <typeparam name="TResult">The bound value type.</typeparam>
    /// <param name="option">The option to bind.</param>
    /// <param name="binder">The asynchronous option-producing function to invoke for a present value.</param>
    /// <returns>A value task that produces the bound option, or <c>None</c> when <paramref name="option"/> is absent.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="binder"/> is null.</exception>
    public static ValueTask<Option<TResult>> BindValueAsync<T, TResult>(
        this Option<T> option,
        Func<T, ValueTask<Option<TResult>>> binder)
    {
        ArgumentNullException.ThrowIfNull(binder);

        return option.TryGetValue(out var value)
            ? BindValueAsyncCore(value, binder)
            : ValueTask.FromResult(Option<TResult>.None);
    }

    /// <summary>
    /// Asynchronously binds a present value with a cancellation-aware value-task-returning binder.
    /// </summary>
    /// <typeparam name="T">The source value type.</typeparam>
    /// <typeparam name="TResult">The bound value type.</typeparam>
    /// <param name="option">The option to bind.</param>
    /// <param name="binder">The asynchronous option-producing function to invoke for a present value.</param>
    /// <param name="cancellationToken">The token passed unchanged to <paramref name="binder"/> when the option is present.</param>
    /// <returns>A value task that produces the bound option, or <c>None</c> when <paramref name="option"/> is absent.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="binder"/> is null.</exception>
    public static ValueTask<Option<TResult>> BindValueAsync<T, TResult>(
        this Option<T> option,
        Func<T, CancellationToken, ValueTask<Option<TResult>>> binder,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(binder);

        return option.TryGetValue(out var value)
            ? BindValueAsyncCore(value, binder, cancellationToken)
            : ValueTask.FromResult(Option<TResult>.None);
    }

    /// <summary>
    /// Converts a task that produces a nullable reference to a task that produces an option.
    /// </summary>
    /// <typeparam name="T">The reference type.</typeparam>
    /// <param name="task">The task producing the nullable reference.</param>
    /// <returns>A task that produces an option containing the completed value, or <c>None</c> when it is null.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="task"/> is null.</exception>
    public static Task<Option<T>> ToOptionAsync<T>(this Task<T?> task)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(task);
        return ReferenceTaskToOptionAsyncCore(task);
    }

    /// <summary>
    /// Converts a task that produces a nullable value type to a task that produces an option.
    /// </summary>
    /// <typeparam name="T">The underlying value type.</typeparam>
    /// <param name="task">The task producing the nullable value.</param>
    /// <returns>A task that produces an option containing the completed value, or <c>None</c> when it has no value.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="task"/> is null.</exception>
    public static Task<Option<T>> ToOptionAsync<T>(this Task<T?> task)
        where T : struct
    {
        ArgumentNullException.ThrowIfNull(task);
        return NullableTaskToOptionAsyncCore(task);
    }

    /// <summary>
    /// Converts a value task that produces a nullable reference to a value task that produces an option.
    /// </summary>
    /// <typeparam name="T">The reference type.</typeparam>
    /// <param name="task">The value task producing the nullable reference.</param>
    /// <returns>A value task that produces an option containing the completed value, or <c>None</c> when it is null.</returns>
    public static ValueTask<Option<T>> ToOptionAsync<T>(this ValueTask<T?> task)
        where T : class =>
        ReferenceValueTaskToOptionAsyncCore(task);

    /// <summary>
    /// Converts a value task that produces a nullable value type to a value task that produces an option.
    /// </summary>
    /// <typeparam name="T">The underlying value type.</typeparam>
    /// <param name="task">The value task producing the nullable value.</param>
    /// <returns>A value task that produces an option containing the completed value, or <c>None</c> when it has no value.</returns>
    public static ValueTask<Option<T>> ToOptionAsync<T>(this ValueTask<T?> task)
        where T : struct =>
        NullableValueTaskToOptionAsyncCore(task);

    private static async Task<Option<TResult>> MapAsyncCore<T, TResult>(
        T value,
        Func<T, Task<TResult>> selector)
    {
        var result = await selector(value).ConfigureAwait(false);
        return Option<TResult>.FromNullable(result);
    }

    private static async Task<Option<TResult>> MapAsyncCore<T, TResult>(
        T value,
        Func<T, CancellationToken, Task<TResult>> selector,
        CancellationToken cancellationToken)
    {
        var result = await selector(value, cancellationToken).ConfigureAwait(false);
        return Option<TResult>.FromNullable(result);
    }

    private static async Task<Option<TResult>> BindAsyncCore<T, TResult>(
        T value,
        Func<T, Task<Option<TResult>>> binder) =>
        await binder(value).ConfigureAwait(false);

    private static async Task<Option<TResult>> BindAsyncCore<T, TResult>(
        T value,
        Func<T, CancellationToken, Task<Option<TResult>>> binder,
        CancellationToken cancellationToken) =>
        await binder(value, cancellationToken).ConfigureAwait(false);

    private static async ValueTask<Option<TResult>> MapValueAsyncCore<T, TResult>(
        T value,
        Func<T, ValueTask<TResult>> selector)
    {
        var result = await selector(value).ConfigureAwait(false);
        return Option<TResult>.FromNullable(result);
    }

    private static async ValueTask<Option<TResult>> MapValueAsyncCore<T, TResult>(
        T value,
        Func<T, CancellationToken, ValueTask<TResult>> selector,
        CancellationToken cancellationToken)
    {
        var result = await selector(value, cancellationToken).ConfigureAwait(false);
        return Option<TResult>.FromNullable(result);
    }

    private static async ValueTask<Option<TResult>> BindValueAsyncCore<T, TResult>(
        T value,
        Func<T, ValueTask<Option<TResult>>> binder) =>
        await binder(value).ConfigureAwait(false);

    private static async ValueTask<Option<TResult>> BindValueAsyncCore<T, TResult>(
        T value,
        Func<T, CancellationToken, ValueTask<Option<TResult>>> binder,
        CancellationToken cancellationToken) =>
        await binder(value, cancellationToken).ConfigureAwait(false);

    private static async Task<Option<T>> ReferenceTaskToOptionAsyncCore<T>(Task<T?> task)
        where T : class =>
        Option.FromNullable(await task.ConfigureAwait(false));

    private static async Task<Option<T>> NullableTaskToOptionAsyncCore<T>(Task<T?> task)
        where T : struct =>
        Option.FromNullable(await task.ConfigureAwait(false));

    private static async ValueTask<Option<T>> ReferenceValueTaskToOptionAsyncCore<T>(ValueTask<T?> task)
        where T : class =>
        Option.FromNullable(await task.ConfigureAwait(false));

    private static async ValueTask<Option<T>> NullableValueTaskToOptionAsyncCore<T>(ValueTask<T?> task)
        where T : struct =>
        Option.FromNullable(await task.ConfigureAwait(false));
}
