namespace FunnySharp;

/// <summary>
/// Binds one named object field to a member contract, an accessor on the parent value, and a
/// missing/null policy.
/// </summary>
/// <typeparam name="TParent">The object type that owns the field.</typeparam>
/// <typeparam name="T">The member value type.</typeparam>
/// <remarks>
/// A field is created through <see cref="Contract.Field{TParent, T}"/>,
/// <see cref="Contract.OptionalField{TParent, T}"/>, or
/// <see cref="Contract.NullableField{TParent, T}"/>. The field stores the name, member contract,
/// presence policy, and accessor exactly once; <see cref="Contract.Object{T1, TResult}"/> reuses
/// them for every interpretation.
/// </remarks>
public sealed class ContractField<TParent, T>
{
    private readonly Func<TParent, T> getter;
    private readonly Func<Result<Decoder<T>, ContractLimitation>> decoderFactory;

    internal ContractField(
        string name,
        Contract<T> contract,
        ContractFieldPresence presence,
        Func<TParent, T> getter,
        Func<Result<Decoder<T>, ContractLimitation>> decoderFactory)
    {
        Name = name;
        Contract = contract;
        Presence = presence;
        this.getter = getter;
        this.decoderFactory = decoderFactory;
    }

    /// <summary>Gets the case-sensitive property name.</summary>
    public string Name { get; }

    /// <summary>Gets the member contract.</summary>
    public Contract<T> Contract { get; }

    /// <summary>Gets the missing/null policy for this field.</summary>
    public ContractFieldPresence Presence { get; }

    internal T Get(TParent parent) => getter(parent);

    internal Result<Decoder<T>, ContractLimitation> CreateDecoder() => decoderFactory();

    internal ContractFieldDescription Describe() =>
        new(Name, Contract.Description, Presence);
}
