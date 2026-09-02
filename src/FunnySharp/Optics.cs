namespace FunnySharp;

/// <summary>
/// Provides inference-friendly factories for <see cref="Lens{TSource, TFocus}"/> values.
/// </summary>
public static class Lens
{
    /// <summary>
    /// Creates a lens from getter and setter delegates.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TFocus">The focused value type.</typeparam>
    /// <param name="get">The delegate that reads the focus from a source.</param>
    /// <param name="set">The delegate that replaces the focus in a source.</param>
    /// <returns>A lens backed by <paramref name="get"/> and <paramref name="set"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="get"/> or <paramref name="set"/> is null.</exception>
    public static Lens<TSource, TFocus> Create<TSource, TFocus>(
        Func<TSource, TFocus> get,
        Func<TSource, TFocus, TSource> set)
    {
        ArgumentNullException.ThrowIfNull(get);
        ArgumentNullException.ThrowIfNull(set);

        return new(get, set);
    }

    /// <summary>
    /// Creates a lens focused on its entire source.
    /// </summary>
    /// <typeparam name="T">The source and focus type.</typeparam>
    /// <returns>An identity lens.</returns>
    public static Lens<T, T> Identity<T>() => new(static value => value, static (_, value) => value);
}

/// <summary>
/// Represents a total, composable read and replacement of a focus within a source.
/// </summary>
/// <typeparam name="TSource">The source type.</typeparam>
/// <typeparam name="TFocus">The focused value type.</typeparam>
public readonly struct Lens<TSource, TFocus>
{
    internal readonly Func<TSource, TFocus>? get;
    internal readonly Func<TSource, TFocus, TSource>? set;

    internal Lens(Func<TSource, TFocus> get, Func<TSource, TFocus, TSource> set)
    {
        this.get = get;
        this.set = set;
    }

    /// <summary>
    /// Reads the focus from <paramref name="source"/>.
    /// </summary>
    /// <param name="source">The source to read.</param>
    /// <returns>The focus read from <paramref name="source"/>.</returns>
    /// <exception cref="InvalidOperationException">This lens is uninitialized.</exception>
    public TFocus Get(TSource source)
    {
        EnsureInitialized();
        return get!(source);
    }

    /// <summary>
    /// Replaces the focus in <paramref name="source"/> with <paramref name="focus"/>.
    /// </summary>
    /// <param name="source">The source to update.</param>
    /// <param name="focus">The replacement focus.</param>
    /// <returns>The source returned by the configured setter.</returns>
    /// <exception cref="InvalidOperationException">This lens is uninitialized.</exception>
    public TSource Set(TSource source, TFocus focus)
    {
        EnsureInitialized();
        return set!(source, focus);
    }

    /// <summary>
    /// Transforms the focus in <paramref name="source"/>.
    /// </summary>
    /// <param name="source">The source to update.</param>
    /// <param name="update">The transformation to apply to the focus.</param>
    /// <returns>The source returned by the configured setter.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="update"/> is null.</exception>
    /// <exception cref="InvalidOperationException">This lens is uninitialized.</exception>
    public TSource Update(TSource source, Func<TFocus, TFocus> update)
    {
        EnsureInitialized();
        ArgumentNullException.ThrowIfNull(update);

        return set!(source, update(get!(source)));
    }

    /// <summary>
    /// Composes this lens with <paramref name="next"/>.
    /// </summary>
    /// <typeparam name="TNext">The next focus type.</typeparam>
    /// <param name="next">The lens applied after this lens.</param>
    /// <returns>A lens from this source to <paramref name="next"/>'s focus.</returns>
    /// <exception cref="InvalidOperationException">This lens or <paramref name="next"/> is uninitialized.</exception>
    public Lens<TSource, TNext> Compose<TNext>(Lens<TFocus, TNext> next)
    {
        EnsureInitialized();
        next.EnsureInitialized();

        var currentGet = get!;
        var currentSet = set!;
        var nextGet = next.get!;
        var nextSet = next.set!;

        return new(
            source => nextGet(currentGet(source)),
            (source, focus) => currentSet(source, nextSet(currentGet(source), focus)));
    }

    /// <summary>
    /// Composes this lens with <paramref name="next"/>.
    /// </summary>
    /// <typeparam name="TNext">The next focus type.</typeparam>
    /// <param name="next">The optional focus applied after this lens.</param>
    /// <returns>An optional focus from this source to <paramref name="next"/>'s focus.</returns>
    /// <exception cref="InvalidOperationException">This lens or <paramref name="next"/> is uninitialized.</exception>
    public Optional<TSource, TNext> Compose<TNext>(Optional<TFocus, TNext> next)
    {
        EnsureInitialized();
        next.EnsureInitialized();

        var currentGet = get!;
        var currentSet = set!;
        var nextGetOption = next.getOption!;
        var nextSetPresent = next.setPresent!;

        return new(
            source => nextGetOption(currentGet(source)),
            (source, focus) =>
            {
                var current = currentGet(source);
                return currentSet(source, nextSetPresent(current, focus));
            });
    }

    internal void EnsureInitialized()
    {
        if (get is null || set is null)
        {
            throw new InvalidOperationException("The lens is uninitialized.");
        }
    }
}

/// <summary>
/// Provides inference-friendly factories for <see cref="Optional{TSource, TFocus}"/> values.
/// </summary>
public static class Optional
{
    /// <summary>
    /// Creates an optional focus from getter and setter delegates.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TFocus">The focused value type.</typeparam>
    /// <param name="getOption">The delegate that reads an optional focus from a source.</param>
    /// <param name="set">The delegate that replaces a present focus in a source.</param>
    /// <returns>An optional focus backed by <paramref name="getOption"/> and <paramref name="set"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="getOption"/> or <paramref name="set"/> is null.</exception>
    public static Optional<TSource, TFocus> Create<TSource, TFocus>(
        Func<TSource, Option<TFocus>> getOption,
        Func<TSource, TFocus, TSource> set)
    {
        ArgumentNullException.ThrowIfNull(getOption);
        ArgumentNullException.ThrowIfNull(set);

        return new(getOption, set);
    }
}

/// <summary>
/// Represents a composable read and replacement of a focus that may be absent.
/// </summary>
/// <typeparam name="TSource">The source type.</typeparam>
/// <typeparam name="TFocus">The focused value type.</typeparam>
public readonly struct Optional<TSource, TFocus>
{
    internal readonly Func<TSource, Option<TFocus>>? getOption;
    // Public operations verify presence before invoking this delegate.
    internal readonly Func<TSource, TFocus, TSource>? setPresent;

    internal Optional(Func<TSource, Option<TFocus>> getOption, Func<TSource, TFocus, TSource> set)
    {
        this.getOption = getOption;
        setPresent = set;
    }

    /// <summary>
    /// Reads the optional focus from <paramref name="source"/>.
    /// </summary>
    /// <param name="source">The source to read.</param>
    /// <returns>The optional focus read from <paramref name="source"/>.</returns>
    /// <exception cref="InvalidOperationException">This optional focus is uninitialized.</exception>
    public Option<TFocus> GetOption(TSource source)
    {
        EnsureInitialized();
        return getOption!(source);
    }

    /// <summary>
    /// Replaces the focus when it is present in <paramref name="source"/>.
    /// </summary>
    /// <param name="source">The source to update.</param>
    /// <param name="focus">The replacement focus.</param>
    /// <returns>The source returned by the configured setter, or <paramref name="source"/> when the focus is absent.</returns>
    /// <exception cref="InvalidOperationException">This optional focus is uninitialized.</exception>
    public TSource Set(TSource source, TFocus focus)
    {
        EnsureInitialized();

        var currentGetOption = getOption!;
        var currentSetPresent = setPresent!;

        if (!currentGetOption(source).TryGetValue(out _))
        {
            return source;
        }

        return currentSetPresent(source, focus);
    }

    /// <summary>
    /// Transforms the focus when it is present in <paramref name="source"/>.
    /// </summary>
    /// <param name="source">The source to update.</param>
    /// <param name="update">The transformation to apply to a present focus.</param>
    /// <returns>The source returned by the configured setter, or <paramref name="source"/> when the focus is absent.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="update"/> is null.</exception>
    /// <exception cref="InvalidOperationException">This optional focus is uninitialized.</exception>
    public TSource Update(TSource source, Func<TFocus, TFocus> update)
    {
        EnsureInitialized();
        ArgumentNullException.ThrowIfNull(update);

        var currentGetOption = getOption!;
        var currentSetPresent = setPresent!;

        if (!currentGetOption(source).TryGetValue(out var current))
        {
            return source;
        }

        return currentSetPresent(source, update(current));
    }

    /// <summary>
    /// Composes this optional focus with <paramref name="next"/>.
    /// </summary>
    /// <typeparam name="TNext">The next focus type.</typeparam>
    /// <param name="next">The lens applied after this optional focus.</param>
    /// <returns>An optional focus from this source to <paramref name="next"/>'s focus.</returns>
    /// <exception cref="InvalidOperationException">This optional focus or <paramref name="next"/> is uninitialized.</exception>
    public Optional<TSource, TNext> Compose<TNext>(Lens<TFocus, TNext> next)
    {
        EnsureInitialized();
        next.EnsureInitialized();

        var currentGetOption = getOption!;
        var currentSetPresent = setPresent!;
        var nextGet = next.get!;
        var nextSet = next.set!;

        return new(
            source => currentGetOption(source).Map(nextGet),
            (source, focus) =>
            {
                if (!currentGetOption(source).TryGetValue(out var current))
                {
                    return source;
                }

                return currentSetPresent(source, nextSet(current, focus));
            });
    }

    /// <summary>
    /// Composes this optional focus with <paramref name="next"/>.
    /// </summary>
    /// <typeparam name="TNext">The next focus type.</typeparam>
    /// <param name="next">The optional focus applied after this optional focus.</param>
    /// <returns>An optional focus from this source to <paramref name="next"/>'s focus.</returns>
    /// <exception cref="InvalidOperationException">This optional focus or <paramref name="next"/> is uninitialized.</exception>
    public Optional<TSource, TNext> Compose<TNext>(Optional<TFocus, TNext> next)
    {
        EnsureInitialized();
        next.EnsureInitialized();

        var currentGetOption = getOption!;
        var currentSetPresent = setPresent!;
        var nextGetOption = next.getOption!;
        var nextSetPresent = next.setPresent!;

        return new(
            source => currentGetOption(source).Bind(nextGetOption),
            (source, focus) =>
            {
                if (!currentGetOption(source).TryGetValue(out var current))
                {
                    return source;
                }

                return currentSetPresent(source, nextSetPresent(current, focus));
            });
    }

    internal void EnsureInitialized()
    {
        if (getOption is null || setPresent is null)
        {
            throw new InvalidOperationException("The optional focus is uninitialized.");
        }
    }
}
