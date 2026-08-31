using System.Diagnostics.CodeAnalysis;

namespace FunnySharp;

/// <summary>
/// Represents a Boolean-returning operation that writes a value through an <c>out</c> parameter.
/// </summary>
/// <typeparam name="T">The output value type.</typeparam>
/// <param name="value">The value produced by the operation.</param>
/// <returns><see langword="true"/> when the operation reports success; otherwise, <see langword="false"/>.</returns>
public delegate bool TryOperation<T>([MaybeNull] out T value);

/// <summary>
/// Provides inference-friendly factories for <see cref="Option{T}"/> values.
/// </summary>
public static class Option
{
    /// <summary>
    /// Creates an option that contains <paramref name="value"/>.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="value">The non-null value to contain.</param>
    /// <returns>An option containing <paramref name="value"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public static Option<T> Some<T>([DisallowNull] T value) => Option<T>.Some(value);

    /// <summary>
    /// Creates an option that represents absence.
    /// </summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>An absent option.</returns>
    public static Option<T> None<T>() => Option<T>.None;

    /// <summary>
    /// Converts a nullable reference to an option.
    /// </summary>
    /// <typeparam name="T">The reference type.</typeparam>
    /// <param name="value">The nullable reference.</param>
    /// <returns>An option containing the reference, or <c>None</c> when it is null.</returns>
    public static Option<T> FromNullable<T>(T? value)
        where T : class =>
        Option<T>.FromNullable(value);

    /// <summary>
    /// Converts a nullable value type to an option.
    /// </summary>
    /// <typeparam name="T">The underlying value type.</typeparam>
    /// <param name="value">The nullable value.</param>
    /// <returns>An option containing the value, or <c>None</c> when it has no value.</returns>
    public static Option<T> FromNullable<T>(T? value)
        where T : struct =>
        value.HasValue ? Option<T>.Some(value.GetValueOrDefault()) : Option<T>.None;

    /// <summary>
    /// Invokes a Boolean-returning Try-pattern operation and converts its output to an option.
    /// </summary>
    /// <typeparam name="T">The output value type.</typeparam>
    /// <param name="operation">The operation to invoke.</param>
    /// <returns>
    /// An option containing a non-null successful output; otherwise, <c>None</c>.
    /// </returns>
    public static Option<T> FromTry<T>(TryOperation<T> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        return operation(out var value) ? Option<T>.FromNullable(value) : Option<T>.None;
    }
}

/// <summary>
/// Represents a value that may be present or absent.
/// </summary>
/// <typeparam name="T">The contained value type.</typeparam>
public readonly struct Option<T> : IEquatable<Option<T>>
{
    private readonly T? value;

    private Option(T value)
    {
        this.value = value;
        IsSome = true;
    }

    /// <summary>
    /// Gets the absent option.
    /// </summary>
    public static Option<T> None => default;

    /// <summary>
    /// Gets a value indicating whether this option contains a value.
    /// </summary>
    public bool IsSome { get; }

    /// <summary>
    /// Gets a value indicating whether this option is absent.
    /// </summary>
    public bool IsNone => !IsSome;

    /// <summary>
    /// Creates an option that contains <paramref name="value"/>.
    /// </summary>
    /// <param name="value">The non-null value to contain.</param>
    /// <returns>An option containing <paramref name="value"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public static Option<T> Some([DisallowNull] T value)
    {
        return new(EnsureNotNull(value, nameof(value)));
    }

    /// <summary>
    /// Attempts to retrieve the contained value.
    /// </summary>
    /// <param name="value">The contained value, or <see langword="default"/> when absent.</param>
    /// <returns><see langword="true"/> when this option contains a value; otherwise, <see langword="false"/>.</returns>
    public bool TryGetValue([NotNullWhen(true)] out T? value)
    {
        value = this.value;
        return IsSome;
    }

    /// <summary>
    /// Matches this option and returns the result of the selected branch.
    /// </summary>
    /// <typeparam name="TResult">The branch result type.</typeparam>
    /// <param name="some">The branch invoked with a present value.</param>
    /// <param name="none">The branch invoked when absent.</param>
    /// <returns>The selected branch result.</returns>
    public TResult Match<TResult>(Func<T, TResult> some, Func<TResult> none)
    {
        ArgumentNullException.ThrowIfNull(some);
        ArgumentNullException.ThrowIfNull(none);

        return IsSome ? some(value!) : none();
    }

    /// <summary>
    /// Matches this option and invokes the selected branch.
    /// </summary>
    /// <param name="some">The branch invoked with a present value.</param>
    /// <param name="none">The branch invoked when absent.</param>
    public void Match(Action<T> some, Action none)
    {
        ArgumentNullException.ThrowIfNull(some);
        ArgumentNullException.ThrowIfNull(none);

        if (IsSome)
        {
            some(value!);
        }
        else
        {
            none();
        }
    }

    /// <summary>
    /// Transforms a present value and preserves absence.
    /// </summary>
    /// <typeparam name="TResult">The transformed value type.</typeparam>
    /// <param name="selector">The transformation to apply.</param>
    /// <returns>The transformed option, or <c>None</c> when absent or when the result is null.</returns>
    public Option<TResult> Map<TResult>(Func<T, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        return IsSome ? Option<TResult>.FromNullable(selector(value!)) : Option<TResult>.None;
    }

    /// <summary>
    /// Binds a present value to another option and preserves absence.
    /// </summary>
    /// <typeparam name="TResult">The bound value type.</typeparam>
    /// <param name="binder">The option-returning function to apply.</param>
    /// <returns>The bound option, or <c>None</c> when absent.</returns>
    public Option<TResult> Bind<TResult>(Func<T, Option<TResult>> binder)
    {
        ArgumentNullException.ThrowIfNull(binder);

        return IsSome ? binder(value!) : Option<TResult>.None;
    }

    /// <summary>
    /// Keeps a present value only when it satisfies <paramref name="predicate"/>.
    /// </summary>
    /// <param name="predicate">The predicate to evaluate.</param>
    /// <returns>This option when present and matching; otherwise, <c>None</c>.</returns>
    public Option<T> Filter(Func<T, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        return IsSome && predicate(value!) ? this : None;
    }

    /// <summary>
    /// Combines this option with another option.
    /// </summary>
    /// <typeparam name="TSecond">The second value type.</typeparam>
    /// <param name="second">The option to combine with.</param>
    /// <returns>An option containing both values when both are present; otherwise, <c>None</c>.</returns>
    public Option<(T First, TSecond Second)> Zip<TSecond>(Option<TSecond> second) =>
        IsSome && second.IsSome
            ? Option<(T First, TSecond Second)>.Some((value!, second.value!))
            : Option<(T First, TSecond Second)>.None;

    /// <summary>
    /// Returns the contained value or an eager fallback.
    /// </summary>
    /// <param name="fallback">The non-null fallback value.</param>
    /// <returns>The contained value, or <paramref name="fallback"/> when absent.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="fallback"/> is <see langword="null"/>.</exception>
    [return: NotNull]
    public T GetValueOr([DisallowNull] T fallback)
    {
        var nonNullFallback = EnsureNotNull(fallback, nameof(fallback));
        return IsSome ? value! : nonNullFallback;
    }

    /// <summary>
    /// Returns the contained value or invokes a fallback factory.
    /// </summary>
    /// <param name="fallbackFactory">The fallback factory.</param>
    /// <returns>The contained value, or the non-null factory result when absent.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="fallbackFactory"/> is null or returns null when invoked.
    /// </exception>
    [return: NotNull]
    public T GetValueOrElse(Func<T> fallbackFactory)
    {
        ArgumentNullException.ThrowIfNull(fallbackFactory);

        if (IsSome)
        {
            return value!;
        }

        var fallback = fallbackFactory();
        return EnsureNotNull(fallback, nameof(fallbackFactory), "The fallback factory returned null.");
    }

    /// <summary>
    /// Returns the contained value or <see langword="default"/> when absent.
    /// </summary>
    /// <returns>The contained value, or <see langword="default"/>.</returns>
    [return: MaybeNull]
    public T GetValueOrDefault() => IsSome ? value! : default;

    /// <summary>
    /// Returns this option when present or an eager fallback option when absent.
    /// </summary>
    /// <param name="fallback">The fallback option.</param>
    /// <returns>This option when present; otherwise, <paramref name="fallback"/>.</returns>
    public Option<T> OrElse(Option<T> fallback) => IsSome ? this : fallback;

    /// <summary>
    /// Returns this option when present or invokes an option factory when absent.
    /// </summary>
    /// <param name="fallbackFactory">The fallback option factory.</param>
    /// <returns>This option when present; otherwise, the factory result.</returns>
    public Option<T> OrElseWith(Func<Option<T>> fallbackFactory)
    {
        ArgumentNullException.ThrowIfNull(fallbackFactory);
        return IsSome ? this : fallbackFactory();
    }

    /// <inheritdoc />
    public bool Equals(Option<T> other) =>
        IsSome == other.IsSome &&
        (!IsSome || EqualityComparer<T>.Default.Equals(value!, other.value!));

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Option<T> other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() =>
        IsSome
            ? HashCode.Combine(true, EqualityComparer<T>.Default.GetHashCode(value!))
            : HashCode.Combine(false);

    /// <summary>
    /// Determines whether two options are equal.
    /// </summary>
    public static bool operator ==(Option<T> left, Option<T> right) => left.Equals(right);

    /// <summary>
    /// Determines whether two options are unequal.
    /// </summary>
    public static bool operator !=(Option<T> left, Option<T> right) => !left.Equals(right);

    /// <inheritdoc />
    public override string ToString() => IsSome ? $"Some({value})" : "None";

    internal static Option<T> FromNullable([AllowNull] T value) =>
        default(T) is null && value is null ? None : new(value!);

    [return: NotNull]
    private static T EnsureNotNull(
        [AllowNull] T candidate,
        string parameterName,
        string? message = null)
    {
        if (default(T) is null && candidate is null)
        {
            throw new ArgumentNullException(parameterName, message);
        }

        return candidate!;
    }
}
