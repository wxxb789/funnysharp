#pragma warning disable FS0017

using System.Text.Json;

namespace FunnySharp;

using DiagnosticLocation = Location;

/// <summary>
/// Binds one allowed patch field to a wire name, a value contract, and an immutable update.
/// </summary>
/// <typeparam name="T">The type of the value being changed.</typeparam>
/// <remarks>
/// A field is created through <see cref="Patch.Field{T, TValue}"/>,
/// <see cref="Patch.ClearableField{T, TValue}"/>, or the matching members on
/// <see cref="PatchFieldSet{T}"/>. The field stores the name, value contract, lens, and optional
/// clear callback exactly once; a <see cref="PatchSchema{T}"/> reuses them for decoding,
/// validation, explanation, and application.
/// </remarks>
public abstract class PatchField<T>
{
    internal PatchField(string name, ContractDescription valueDescription, bool allowsClear)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(valueDescription);

        Name = name;
        ValueDescription = valueDescription;
        AllowsClear = allowsClear;
    }

    /// <summary>Gets the case-sensitive JSON property name.</summary>
    public string Name { get; }

    /// <summary>Gets the description of the replacement value contract.</summary>
    public ContractDescription ValueDescription { get; }

    /// <summary>Gets a value indicating whether an explicit JSON null clears this field.</summary>
    public bool AllowsClear { get; }

    internal abstract Result<
        Func<Patch<T>, JsonElement, Validation<Patch<T>, DecodeError>>,
        ContractLimitation> CreateDecoder();

    internal abstract Patch<T> WithClear(Patch<T> patch);

    internal PatchFieldDescription Describe() =>
        new(Name, ValueDescription, AllowsClear);
}

/// <summary>
/// Binds one allowed patch field to a typed value contract and a total lens.
/// </summary>
/// <typeparam name="T">The type of the value being changed.</typeparam>
/// <typeparam name="TValue">The replacement value type.</typeparam>
/// <remarks>
/// A replacement invokes <see cref="Lens{TSource,TFocus}.Set"/> on the current candidate. A
/// clearing instruction invokes the optional clear callback instead. The field does not read or
/// clone the source, check a version, or persist anything.
/// </remarks>
public sealed class PatchField<T, TValue> : PatchField<T>
{
    private readonly Contract<TValue> contract;
    private readonly Lens<T, TValue> lens;
    private readonly Func<T, T>? clear;

    internal PatchField(
        string name,
        Contract<TValue> contract,
        Lens<T, TValue> lens,
        Func<T, T>? clear)
        : base(name, contract.Description, clear is not null)
    {
        lens.EnsureInitialized();

        this.contract = contract;
        this.lens = lens;
        this.clear = clear;
    }

    internal override Result<
        Func<Patch<T>, JsonElement, Validation<Patch<T>, DecodeError>>,
        ContractLimitation> CreateDecoder() =>
        contract.CreateDecoder().Map(
            decoder => (Func<Patch<T>, JsonElement, Validation<Patch<T>, DecodeError>>)(
                (patch, input) => decoder
                    .Decode(input)
                    .MapErrors(error => error.WithLocation(DiagnosticLocation.Root.Property(Name)))
                    .Map(value => patch.With(this, PatchChange<TValue>.Set(value!)))));

    internal override Patch<T> WithClear(Patch<T> patch)
    {
        if (clear is null)
        {
            throw new InvalidOperationException($"The patch field '{Name}' does not allow clearing.");
        }

        return patch.With(this, PatchChange<TValue>.Clear);
    }

    internal T ApplySet(T source, TValue value) => lens.Set(source, value);

    internal T ApplyClear(T source) => clear!(source);
}

/// <summary>
/// Provides parent-typed patch field factories for inference-friendly construction.
/// </summary>
/// <typeparam name="T">The type of the value being changed.</typeparam>
/// <remarks>
/// The field set is stateless. Each method fixes the parent type so accessor and setter types are
/// inferred from the lens and contract.
/// </remarks>
public sealed class PatchFieldSet<T>
{
    internal PatchFieldSet()
    {
    }

    /// <summary>Binds an allowed field that accepts replacements but cannot be cleared.</summary>
    /// <typeparam name="TValue">The replacement value type.</typeparam>
    /// <param name="name">The non-empty case-sensitive JSON property name.</param>
    /// <param name="contract">The non-null contract used to decode and validate replacement values.</param>
    /// <param name="lens">The initialized lens that reads and replaces the field.</param>
    /// <returns>The field binding.</returns>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="lens"/> is uninitialized.</exception>
    public PatchField<T, TValue> Field<TValue>(
        string name,
        Contract<TValue> contract,
        Lens<T, TValue> lens) =>
        Patch.Field(name, contract, lens);

    /// <summary>Binds an allowed field that accepts replacements and an explicit clearing instruction.</summary>
    /// <typeparam name="TValue">The replacement value type.</typeparam>
    /// <param name="name">The non-empty case-sensitive JSON property name.</param>
    /// <param name="contract">The non-null contract used to decode and validate replacement values.</param>
    /// <param name="lens">The initialized lens that reads and replaces the field.</param>
    /// <param name="clear">The non-null callback that returns a candidate with the field cleared.</param>
    /// <returns>The field binding.</returns>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="lens"/> is uninitialized.</exception>
    public PatchField<T, TValue> ClearableField<TValue>(
        string name,
        Contract<TValue> contract,
        Lens<T, TValue> lens,
        Func<T, T> clear) =>
        Patch.ClearableField(name, contract, lens, clear);
}
