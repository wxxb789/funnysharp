using System.Diagnostics.CodeAnalysis;

namespace FunnySharp;

/// <summary>
/// Represents either a valid value or one or more validation errors.
/// </summary>
/// <typeparam name="TValue">The valid value type.</typeparam>
/// <typeparam name="TError">The validation error type.</typeparam>
public readonly struct Validation<TValue, TError> : IEquatable<Validation<TValue, TError>>
{
    private const byte UninitializedState = 0;
    private const byte ValidState = 1;
    private const byte InvalidState = 2;

    private readonly TValue? value;
    private readonly IReadOnlyList<TError>? errors;
    private readonly byte state;

    private Validation(TValue? value, IReadOnlyList<TError>? errors, byte state)
    {
        this.value = value;
        this.errors = errors;
        this.state = state;
    }

    /// <summary>
    /// Gets a value indicating whether this validation is valid.
    /// </summary>
    /// <exception cref="InvalidOperationException">This validation is uninitialized.</exception>
    public bool IsValid
    {
        get
        {
            EnsureInitialized();
            return state == ValidState;
        }
    }

    /// <summary>
    /// Gets a value indicating whether this validation is invalid.
    /// </summary>
    /// <exception cref="InvalidOperationException">This validation is uninitialized.</exception>
    public bool IsInvalid
    {
        get
        {
            EnsureInitialized();
            return state == InvalidState;
        }
    }

    /// <summary>
    /// Creates a valid validation.
    /// </summary>
    /// <param name="value">The valid value.</param>
    /// <returns>A valid validation containing <paramref name="value"/>.</returns>
    public static Validation<TValue, TError> Valid(TValue value) => new(value, default, ValidState);

    /// <summary>
    /// Creates an invalid validation containing one error.
    /// </summary>
    /// <param name="error">The validation error.</param>
    /// <returns>An invalid validation containing <paramref name="error"/>.</returns>
    public static Validation<TValue, TError> Invalid(TError error) =>
        InvalidFromOwnedErrors(new[] { error });

    /// <summary>
    /// Creates an invalid validation containing a snapshot of the supplied errors.
    /// </summary>
    /// <param name="errors">The validation errors. The sequence must contain at least one error.</param>
    /// <returns>An invalid validation containing the supplied errors.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="errors"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="errors"/> is empty.</exception>
    public static Validation<TValue, TError> InvalidMany(IEnumerable<TError> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        var snapshot = errors.ToArray();
        if (snapshot.Length == 0)
        {
            throw new ArgumentException("At least one validation error is required.", nameof(errors));
        }

        return InvalidFromOwnedErrors(snapshot);
    }

    /// <summary>
    /// Attempts to retrieve the valid value.
    /// </summary>
    /// <param name="value">The valid value, or <see langword="default"/> when invalid.</param>
    /// <returns><see langword="true"/> when valid; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="InvalidOperationException">This validation is uninitialized.</exception>
    public bool TryGetValue([MaybeNull] out TValue value)
    {
        EnsureInitialized();
        value = this.value;
        return state == ValidState;
    }

    /// <summary>
    /// Attempts to retrieve the validation errors.
    /// </summary>
    /// <param name="errors">The validation errors, or <see langword="null"/> when valid.</param>
    /// <returns><see langword="true"/> when invalid; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="InvalidOperationException">This validation is uninitialized.</exception>
    public bool TryGetErrors([NotNullWhen(true)] out IReadOnlyList<TError>? errors)
    {
        EnsureInitialized();
        errors = state == InvalidState ? Errors : default;
        return state == InvalidState;
    }

    /// <summary>
    /// Matches this validation and returns the value from the selected branch.
    /// </summary>
    /// <typeparam name="TResult">The branch result type.</typeparam>
    /// <param name="valid">The branch invoked with a valid value.</param>
    /// <param name="invalid">The branch invoked with validation errors.</param>
    /// <returns>The selected branch result.</returns>
    /// <exception cref="InvalidOperationException">This validation is uninitialized.</exception>
    public TResult Match<TResult>(
        Func<TValue, TResult> valid,
        Func<IReadOnlyList<TError>, TResult> invalid)
    {
        ArgumentNullException.ThrowIfNull(valid);
        ArgumentNullException.ThrowIfNull(invalid);
        EnsureInitialized();

        return state == ValidState ? valid(value!) : invalid(Errors);
    }

    /// <summary>
    /// Matches this validation and invokes the selected branch.
    /// </summary>
    /// <param name="valid">The branch invoked with a valid value.</param>
    /// <param name="invalid">The branch invoked with validation errors.</param>
    /// <exception cref="InvalidOperationException">This validation is uninitialized.</exception>
    public void Match(Action<TValue> valid, Action<IReadOnlyList<TError>> invalid)
    {
        ArgumentNullException.ThrowIfNull(valid);
        ArgumentNullException.ThrowIfNull(invalid);
        EnsureInitialized();

        if (state == ValidState)
        {
            valid(value!);
        }
        else
        {
            invalid(Errors);
        }
    }

    /// <summary>
    /// Transforms a valid value and preserves validation errors.
    /// </summary>
    /// <typeparam name="TResult">The transformed value type.</typeparam>
    /// <param name="selector">The transformation to apply.</param>
    /// <returns>The transformed validation, or the existing errors.</returns>
    /// <exception cref="InvalidOperationException">This validation is uninitialized.</exception>
    public Validation<TResult, TError> Map<TResult>(Func<TValue, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        EnsureInitialized();

        return state == ValidState
            ? Validation<TResult, TError>.Valid(selector(value!))
            : new Validation<TResult, TError>(default, errors, InvalidState);
    }

    /// <summary>
    /// Transforms validation errors and preserves a valid value.
    /// </summary>
    /// <typeparam name="TResultError">The transformed error type.</typeparam>
    /// <param name="selector">The transformation to apply to each validation error.</param>
    /// <returns>The transformed validation, or the existing valid value.</returns>
    /// <exception cref="InvalidOperationException">This validation is uninitialized.</exception>
    public Validation<TValue, TResultError> MapErrors<TResultError>(Func<TError, TResultError> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        EnsureInitialized();

        if (state == ValidState)
        {
            return Validation<TValue, TResultError>.Valid(value!);
        }

        var mappedErrors = new TResultError[Errors.Count];
        for (var index = 0; index < mappedErrors.Length; index++)
        {
            mappedErrors[index] = selector(Errors[index]);
        }

        return Validation<TValue, TResultError>.InvalidFromOwnedErrors(mappedErrors);
    }

    /// <summary>
    /// Combines this validation with another validation, accumulating errors from left to right.
    /// </summary>
    /// <typeparam name="TSecond">The second valid value type.</typeparam>
    /// <param name="second">The validation to combine with.</param>
    /// <returns>
    /// A valid pair when both validations are valid; otherwise, an invalid validation containing all errors
    /// in left-to-right order.
    /// </returns>
    /// <exception cref="InvalidOperationException">This validation or <paramref name="second"/> is uninitialized.</exception>
    public Validation<(TValue First, TSecond Second), TError> Zip<TSecond>(
        Validation<TSecond, TError> second)
    {
        EnsureInitialized();
        second.EnsureInitialized();

        if (state == ValidState && second.state == ValidState)
        {
            return Validation<(TValue First, TSecond Second), TError>.Valid((value!, second.value!));
        }

        if (state == InvalidState && second.state == InvalidState)
        {
            return Validation<(TValue First, TSecond Second), TError>.InvalidFromOwnedErrors(
                ConcatErrors(Errors, second.Errors));
        }

        return state == InvalidState
            ? new Validation<(TValue First, TSecond Second), TError>(default, errors, InvalidState)
            : new Validation<(TValue First, TSecond Second), TError>(
                default,
                second.errors,
                InvalidState);
    }

    /// <summary>
    /// Combines this validation with another validation through a combining function.
    /// </summary>
    /// <typeparam name="TSecond">The second valid value type.</typeparam>
    /// <typeparam name="TResult">The combined valid value type.</typeparam>
    /// <param name="second">The validation to combine with.</param>
    /// <param name="combine">The function invoked with both valid values.</param>
    /// <returns>A valid combined value when both validations are valid; otherwise, all errors in
    /// left-to-right order.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="combine"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">This validation or <paramref name="second"/> is uninitialized.</exception>
    public Validation<TResult, TError> Zip<TSecond, TResult>(
        Validation<TSecond, TError> second,
        Func<TValue, TSecond, TResult> combine)
    {
        ArgumentNullException.ThrowIfNull(combine);
        EnsureInitialized();
        second.EnsureInitialized();

        if (state == ValidState && second.state == ValidState)
        {
            return Validation<TResult, TError>.Valid(combine(value!, second.value!));
        }

        if (state == InvalidState && second.state == InvalidState)
        {
            return Validation<TResult, TError>.InvalidFromOwnedErrors(
                ConcatErrors(Errors, second.Errors));
        }

        return state == InvalidState
            ? new Validation<TResult, TError>(default, errors, InvalidState)
            : new Validation<TResult, TError>(default, second.errors, InvalidState);
    }

    /// <summary>
    /// Combines this validation with two more validations through a combining function.
    /// </summary>
    /// <typeparam name="TSecond">The second valid value type.</typeparam>
    /// <typeparam name="TThird">The third valid value type.</typeparam>
    /// <typeparam name="TResult">The combined valid value type.</typeparam>
    /// <param name="second">The second validation to combine with.</param>
    /// <param name="third">The third validation to combine with.</param>
    /// <param name="combine">The function invoked with all three valid values.</param>
    /// <returns>A valid combined value when all validations are valid; otherwise, all errors in
    /// left-to-right operand order.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="combine"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">This validation or an operand is uninitialized.</exception>
    public Validation<TResult, TError> Zip<TSecond, TThird, TResult>(
        Validation<TSecond, TError> second,
        Validation<TThird, TError> third,
        Func<TValue, TSecond, TThird, TResult> combine)
    {
        ArgumentNullException.ThrowIfNull(combine);
        EnsureInitialized();
        second.EnsureInitialized();
        third.EnsureInitialized();

        if (state == ValidState && second.state == ValidState && third.state == ValidState)
        {
            return Validation<TResult, TError>.Valid(
                combine(value!, second.value!, third.value!));
        }

        IReadOnlyList<TError> firstErrors =
            state == InvalidState ? Errors : Array.Empty<TError>();
        IReadOnlyList<TError> secondErrors =
            second.state == InvalidState ? second.Errors : Array.Empty<TError>();
        IReadOnlyList<TError> thirdErrors =
            third.state == InvalidState ? third.Errors : Array.Empty<TError>();

        return Validation<TResult, TError>.InvalidFromOwnedErrors(
            ConcatErrors(firstErrors, secondErrors, thirdErrors));
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">This validation or <paramref name="other"/> is uninitialized.</exception>
    public bool Equals(Validation<TValue, TError> other)
    {
        EnsureInitialized();
        other.EnsureInitialized();

        if (state != other.state)
        {
            return false;
        }

        if (state == ValidState)
        {
            return EqualityComparer<TValue>.Default.Equals(value!, other.value!);
        }

        var firstErrors = Errors;
        var secondErrors = other.Errors;
        if (firstErrors.Count != secondErrors.Count)
        {
            return false;
        }

        for (var index = 0; index < firstErrors.Count; index++)
        {
            if (!EqualityComparer<TError>.Default.Equals(firstErrors[index], secondErrors[index]))
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">This validation is uninitialized.</exception>
    public override bool Equals(object? obj)
    {
        EnsureInitialized();
        return obj is Validation<TValue, TError> other && Equals(other);
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">This validation is uninitialized.</exception>
    public override int GetHashCode()
    {
        EnsureInitialized();
        var hash = new HashCode();
        hash.Add(state == ValidState);

        if (state == ValidState)
        {
            hash.Add(value!, EqualityComparer<TValue>.Default);
        }
        else
        {
            var currentErrors = Errors;
            var comparer = EqualityComparer<TError>.Default;
            for (var index = 0; index < currentErrors.Count; index++)
            {
                hash.Add(currentErrors[index], comparer);
            }
        }

        return hash.ToHashCode();
    }

    /// <summary>
    /// Determines whether two validations are equal.
    /// </summary>
    public static bool operator ==(
        Validation<TValue, TError> left,
        Validation<TValue, TError> right) =>
        left.Equals(right);

    /// <summary>
    /// Determines whether two validations are unequal.
    /// </summary>
    public static bool operator !=(
        Validation<TValue, TError> left,
        Validation<TValue, TError> right) =>
        !left.Equals(right);

    /// <inheritdoc />
    public override string ToString() =>
        state switch
        {
            ValidState => $"Valid({value})",
            InvalidState => $"Invalid([{string.Join(", ", Errors)}])",
            _ => "Uninitialized",
        };

    private IReadOnlyList<TError> Errors => errors!;

    private static TError[] ConcatErrors(
        IReadOnlyList<TError> first,
        IReadOnlyList<TError> second)
    {
        var combined = new TError[first.Count + second.Count];
        var offset = 0;
        CopyErrors(first, combined, ref offset);
        CopyErrors(second, combined, ref offset);
        return combined;
    }

    private static TError[] ConcatErrors(
        IReadOnlyList<TError> first,
        IReadOnlyList<TError> second,
        IReadOnlyList<TError> third)
    {
        var combined = new TError[first.Count + second.Count + third.Count];
        var offset = 0;
        CopyErrors(first, combined, ref offset);
        CopyErrors(second, combined, ref offset);
        CopyErrors(third, combined, ref offset);
        return combined;
    }

    private static void CopyErrors(
        IReadOnlyList<TError> source,
        TError[] destination,
        ref int offset)
    {
        for (var index = 0; index < source.Count; index++)
        {
            destination[offset + index] = source[index];
        }

        offset += source.Count;
    }

    private static Validation<TValue, TError> InvalidFromOwnedErrors(TError[] errors) =>
        new(default, Array.AsReadOnly(errors), InvalidState);

    internal static Validation<TValue, TError> InvalidFromOwnedErrors(List<TError> errors) =>
        new(default, errors.AsReadOnly(), InvalidState);

    internal static Validation<TValue, TError> InvalidFromOwnedErrors(IReadOnlyList<TError> errors) =>
        new(default, errors, InvalidState);

    private void EnsureInitialized()
    {
        if (state == UninitializedState)
        {
            throw new InvalidOperationException("The validation has not been initialized.");
        }
    }
}
