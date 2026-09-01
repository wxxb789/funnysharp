using System.Diagnostics.CodeAnalysis;

namespace FunnySharp;

/// <summary>
/// Represents either a valid value or one or more validation errors.
/// </summary>
/// <typeparam name="TValue">The valid value type.</typeparam>
/// <typeparam name="TError">The validation error type.</typeparam>
public readonly struct Validation<TValue, TError> : IEquatable<Validation<TValue, TError>>
{
    private static readonly IReadOnlyList<TError> DefaultErrors =
        Array.AsReadOnly(new TError[] { default! });

    private readonly TValue? value;
    private readonly IReadOnlyList<TError>? errors;

    private Validation(TValue? value, IReadOnlyList<TError>? errors, bool isValid)
    {
        this.value = value;
        this.errors = errors;
        IsValid = isValid;
    }

    /// <summary>
    /// Gets a value indicating whether this validation is valid.
    /// </summary>
    public bool IsValid { get; }

    /// <summary>
    /// Gets a value indicating whether this validation is invalid.
    /// </summary>
    public bool IsInvalid => !IsValid;

    /// <summary>
    /// Creates a valid validation.
    /// </summary>
    /// <param name="value">The valid value.</param>
    /// <returns>A valid validation containing <paramref name="value"/>.</returns>
    public static Validation<TValue, TError> Valid(TValue value) => new(value, default, true);

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
    public bool TryGetValue([MaybeNull] out TValue value)
    {
        value = this.value;
        return IsValid;
    }

    /// <summary>
    /// Attempts to retrieve the validation errors.
    /// </summary>
    /// <param name="errors">The validation errors, or <see langword="null"/> when valid.</param>
    /// <returns><see langword="true"/> when invalid; otherwise, <see langword="false"/>.</returns>
    public bool TryGetErrors([NotNullWhen(true)] out IReadOnlyList<TError>? errors)
    {
        errors = IsInvalid ? Errors : default;
        return IsInvalid;
    }

    /// <summary>
    /// Matches this validation and returns the value from the selected branch.
    /// </summary>
    /// <typeparam name="TResult">The branch result type.</typeparam>
    /// <param name="valid">The branch invoked with a valid value.</param>
    /// <param name="invalid">The branch invoked with validation errors.</param>
    /// <returns>The selected branch result.</returns>
    public TResult Match<TResult>(
        Func<TValue, TResult> valid,
        Func<IReadOnlyList<TError>, TResult> invalid)
    {
        ArgumentNullException.ThrowIfNull(valid);
        ArgumentNullException.ThrowIfNull(invalid);

        return IsValid ? valid(value!) : invalid(Errors);
    }

    /// <summary>
    /// Matches this validation and invokes the selected branch.
    /// </summary>
    /// <param name="valid">The branch invoked with a valid value.</param>
    /// <param name="invalid">The branch invoked with validation errors.</param>
    public void Match(Action<TValue> valid, Action<IReadOnlyList<TError>> invalid)
    {
        ArgumentNullException.ThrowIfNull(valid);
        ArgumentNullException.ThrowIfNull(invalid);

        if (IsValid)
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
    public Validation<TResult, TError> Map<TResult>(Func<TValue, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        return IsValid
            ? Validation<TResult, TError>.Valid(selector(value!))
            : new Validation<TResult, TError>(default, Errors, false);
    }

    /// <summary>
    /// Transforms validation errors and preserves a valid value.
    /// </summary>
    /// <typeparam name="TResultError">The transformed error type.</typeparam>
    /// <param name="selector">The transformation to apply to each validation error.</param>
    /// <returns>The transformed validation, or the existing valid value.</returns>
    public Validation<TValue, TResultError> MapErrors<TResultError>(Func<TError, TResultError> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        if (IsValid)
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
    public Validation<(TValue First, TSecond Second), TError> Zip<TSecond>(
        Validation<TSecond, TError> second)
    {
        if (IsValid && second.IsValid)
        {
            second.TryGetValue(out var secondValue);
            return Validation<(TValue First, TSecond Second), TError>.Valid((value!, secondValue!));
        }

        if (IsInvalid && second.IsInvalid)
        {
            second.TryGetErrors(out var secondErrors);
            var combinedErrors = new TError[Errors.Count + secondErrors!.Count];
            for (var index = 0; index < Errors.Count; index++)
            {
                combinedErrors[index] = Errors[index];
            }

            for (var index = 0; index < secondErrors.Count; index++)
            {
                combinedErrors[Errors.Count + index] = secondErrors[index];
            }

            return Validation<(TValue First, TSecond Second), TError>.InvalidFromOwnedErrors(
                combinedErrors);
        }

        if (IsInvalid)
        {
            return new Validation<(TValue First, TSecond Second), TError>(default, Errors, false);
        }

        second.TryGetErrors(out var errors);
        return new Validation<(TValue First, TSecond Second), TError>(default, errors, false);
    }

    /// <inheritdoc />
    public bool Equals(Validation<TValue, TError> other)
    {
        if (IsValid != other.IsValid)
        {
            return false;
        }

        if (IsValid)
        {
            return EqualityComparer<TValue>.Default.Equals(value!, other.value!);
        }

        if (Errors.Count != other.Errors.Count)
        {
            return false;
        }

        for (var index = 0; index < Errors.Count; index++)
        {
            if (!EqualityComparer<TError>.Default.Equals(Errors[index], other.Errors[index]))
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        obj is Validation<TValue, TError> other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(IsValid);

        if (IsValid)
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
        IsValid ? $"Valid({value})" : $"Invalid([{string.Join(", ", Errors)}])";

    private IReadOnlyList<TError> Errors => errors ?? DefaultErrors;

    private static Validation<TValue, TError> InvalidFromOwnedErrors(TError[] errors) =>
        new(default, Array.AsReadOnly(errors), false);

    internal static Validation<TValue, TError> InvalidFromOwnedErrors(List<TError> errors) =>
        new(default, errors.AsReadOnly(), false);
}
