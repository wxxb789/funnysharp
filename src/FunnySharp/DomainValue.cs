using System.Diagnostics.CodeAnalysis;

namespace FunnySharp;

/// <summary>
/// A value that passed a caller-declared domain boundary for the distinct marker
/// <typeparamref name="TTag"/>.
/// </summary>
/// <typeparam name="TValue">The underlying value type.</typeparam>
/// <typeparam name="TTag">
/// A caller-declared marker type that distinguishes this domain value from another domain value
/// with the same underlying type. The marker is never instantiated at runtime.
/// </typeparam>
/// <remarks>
/// <para>
/// A domain value is created only by <see cref="Domain{TValue,TTag}.Validate"/> or by a decoder
/// derived from <see cref="Domain{TValue,TTag}.CreateDecoder"/>. No public constructor accepts the
/// underlying value and no implicit conversion erases the marker, so two identifiers with the same
/// underlying type cannot be assigned to each other or passed where the other is required.
/// </para>
/// <para>
/// The wrapper is a <see langword="readonly"/> <see langword="struct"/>. A default or
/// <c>new DomainValue&lt;TValue, TTag&gt;()</c> value is uninitialized, not a valid domain value:
/// <see cref="Value"/> throws <see cref="InvalidOperationException"/> and
/// <see cref="TryGetValue"/> returns <see langword="false"/>. Equality treats two uninitialized
/// values as equal and never equal to an initialized value.
/// </para>
/// <para>
/// The boundary guarantees that the wrapped value satisfied the domain contract at the moment it
/// was created. It does not copy a mutable payload and does not promise that the payload remains
/// valid after external mutation. It is not proof that a stored record is unchanged, that an
/// operation ran exactly once, that a caller is authorized, or that a row is not concurrently
/// modified; those facts depend on storage, authorization, and concurrency.
/// </para>
/// <para>
/// No <see cref="System.Text.Json"/> converter ships for this type. Deserializing the wrapper
/// directly produces an uninitialized value rather than an unchecked valid value; use a DTO plus
/// the domain's decoder for wire input.
/// </para>
/// </remarks>
public readonly struct DomainValue<TValue, TTag> : IEquatable<DomainValue<TValue, TTag>>
    where TValue : notnull
    where TTag : notnull
{
    private readonly TValue? value;
    private readonly bool initialized;

    private DomainValue(TValue value)
    {
        this.value = value;
        initialized = true;
    }

    /// <summary>
    /// Gets a value indicating whether this wrapper was created through its domain boundary.
    /// </summary>
    /// <remarks>
    /// A default value, a <c>new DomainValue&lt;TValue, TTag&gt;()</c> value, or a value produced
    /// by deserializing the wrapper directly is uninitialized.
    /// </remarks>
    public bool IsInitialized => initialized;

    /// <summary>
    /// Gets the validated underlying value.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// This wrapper is uninitialized; create it through its <see cref="Domain{TValue,TTag}"/> boundary.
    /// </exception>
    public TValue Value => initialized
        ? value!
        : throw new InvalidOperationException(
            $"The domain value '{typeof(TTag).Name}' is uninitialized. " +
            "Create it through its Domain boundary.");

    /// <summary>
    /// Reads the validated underlying value without throwing for an uninitialized wrapper.
    /// </summary>
    /// <param name="value">The underlying value when the method returns <see langword="true"/>.</param>
    /// <returns><see langword="true"/> when this wrapper was created through its domain boundary.</returns>
    public bool TryGetValue([MaybeNullWhen(false)] out TValue value)
    {
        value = this.value;
        return initialized;
    }

    internal static DomainValue<TValue, TTag> FromValidated(TValue value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        return new(value);
    }

    /// <summary>
    /// Returns a value indicating whether this wrapper and <paramref name="other"/> wrap the same
    /// underlying value and have the same initialization state.
    /// </summary>
    /// <param name="other">The domain value to compare.</param>
    /// <returns><see langword="true"/> when both wrappers are structurally equal.</returns>
    public bool Equals(DomainValue<TValue, TTag> other) =>
        initialized == other.initialized
        && (!initialized || EqualityComparer<TValue>.Default.Equals(value!, other.value!));

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        obj is DomainValue<TValue, TTag> other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => initialized ? value!.GetHashCode() : 0;

    /// <summary>Compares two domain values for structural equality.</summary>
    /// <param name="left">The first domain value.</param>
    /// <param name="right">The second domain value.</param>
    /// <returns><see langword="true"/> when the wrappers are structurally equal.</returns>
    public static bool operator ==(DomainValue<TValue, TTag> left, DomainValue<TValue, TTag> right) =>
        left.Equals(right);

    /// <summary>Compares two domain values for structural inequality.</summary>
    /// <param name="left">The first domain value.</param>
    /// <param name="right">The second domain value.</param>
    /// <returns><see langword="true"/> when the wrappers are structurally unequal.</returns>
    public static bool operator !=(DomainValue<TValue, TTag> left, DomainValue<TValue, TTag> right) =>
        !left.Equals(right);

    /// <summary>
    /// Returns diagnostic text for this domain value.
    /// </summary>
    /// <returns>
    /// The underlying value for an initialized wrapper, or a marker naming the uninitialized
    /// marker type. The text is not a serialization contract.
    /// </returns>
    public override string ToString() =>
        initialized
            ? value!.ToString() ?? string.Empty
            : $"<uninitialized {typeof(TTag).Name}>";
}
