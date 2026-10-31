#pragma warning disable FS0017

using System.Text.Json;

namespace FunnySharp;

using DiagnosticLocation = Location;

/// <summary>
/// Binds one allowed patch field to a wire name, a value contract, and an immutable update.
/// </summary>
/// <typeparam name="T">The type of the value being changed.</typeparam>
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
        Func<JsonElement, Validation<Patch<T>.IEntry, DecodeError>>,
        ContractLimitation> CreateDecoder();

    internal abstract Patch<T>.IEntry CreateClearEntry();

    internal PatchFieldDescription Describe() =>
        new(Name, ValueDescription, AllowsClear);
}

/// <summary>
/// Binds one allowed patch field to a typed value contract and a total lens.
/// </summary>
/// <typeparam name="T">The type of the value being changed.</typeparam>
/// <typeparam name="TValue">The replacement value type.</typeparam>
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
        Func<JsonElement, Validation<Patch<T>.IEntry, DecodeError>>,
        ContractLimitation> CreateDecoder() =>
        contract.CreateDecoder().Map(
            decoder => (Func<JsonElement, Validation<Patch<T>.IEntry, DecodeError>>)(
                input => DecodeInto(decoder, input)));

    internal override Patch<T>.IEntry CreateClearEntry()
    {
        if (clear is null)
        {
            throw new InvalidOperationException($"The patch field '{Name}' does not allow clearing.");
        }

        return Patch<T>.CreateEntry(this, PatchChange<TValue>.Clear);
    }

    private Validation<Patch<T>.IEntry, DecodeError> DecodeInto(
        Decoder<TValue> decoder,
        JsonElement input)
    {
        var decoded = decoder.Decode(input);
        if (decoded.TryGetValue(out var value))
        {
            return Validation<Patch<T>.IEntry, DecodeError>.Valid(
                Patch<T>.CreateEntry(this, PatchChange<TValue>.Set(value!)));
        }

        decoded.TryGetErrors(out var errors);
        var located = new DecodeError[errors!.Count];
        for (var index = 0; index < located.Length; index++)
        {
            located[index] = errors[index].WithLocation(DiagnosticLocation.Root.Property(Name));
        }

        return Validation<Patch<T>.IEntry, DecodeError>.InvalidMany(located);
    }

    internal T ApplySet(T source, TValue value) => lens.Set(source, value);

    internal T ApplyClear(T source) => clear!(source);
}

/// <summary>
/// Provides parent-typed patch field factories for inference-friendly construction.
/// </summary>
/// <typeparam name="T">The type of the value being changed.</typeparam>
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
