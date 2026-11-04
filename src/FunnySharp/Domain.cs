namespace FunnySharp;

/// <summary>
/// Creates validated domain boundaries for caller-declared distinct value types.
/// </summary>
/// <remarks>
/// A domain boundary groups an underlying <see cref="Contract{T}"/> with a marker type. The same
/// contract can then validate, decode, generate, shrink, and describe the tagged value without a
/// hand-written wrapper type, equality implementation, parser, or decoder adapter.
/// </remarks>
public static class Domain
{
    /// <summary>
    /// Creates a validated boundary for <typeparamref name="TValue"/> and the marker
    /// <typeparamref name="TTag"/>.
    /// </summary>
    /// <typeparam name="TValue">The underlying value type.</typeparam>
    /// <typeparam name="TTag">The caller-declared marker type that distinguishes this domain value.</typeparam>
    /// <param name="contract">The non-null contract that every wrapped value must satisfy.</param>
    /// <param name="name">The non-empty human-readable name used in contract diagnostics.</param>
    /// <returns>A reusable, immutable domain boundary.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="contract"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty or whitespace.</exception>
    public static Domain<TValue, TTag> For<TValue, TTag>(
        Contract<TValue> contract,
        string name)
        where TValue : notnull
        where TTag : notnull
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new Domain<TValue, TTag>(contract, name);
    }
}

/// <summary>
/// A reusable validated boundary that constructs <see cref="DomainValue{TValue, TTag}"/> values.
/// </summary>
/// <typeparam name="TValue">The underlying value type.</typeparam>
/// <typeparam name="TTag">The caller-declared marker type that distinguishes this domain value.</typeparam>
/// <remarks>
/// <para>
/// The boundary holds one underlying contract and derives a contract for the tagged value through
/// <see cref="Contract{T}.Widen{TBase}"/>. Validation, decoding, generation, shrinking, and schema
/// description therefore stay aligned with the same declared rule; an unavailable derivation
/// remains an explicit <see cref="ContractLimitation"/>.
/// </para>
/// <para>
/// Construction and validation never mutate the underlying value. A mutable payload remains the
/// caller's responsibility: the boundary records that the payload satisfied the contract when it
/// was wrapped, not that later external mutation preserves that fact.
/// </para>
/// </remarks>
public sealed class Domain<TValue, TTag>
    where TValue : notnull
    where TTag : notnull
{
    private readonly Contract<TValue> underlying;

    internal Domain(Contract<TValue> contract, string name)
    {
        underlying = contract;
        Name = name;
        Contract = contract.Widen(
            static value => DomainValue<TValue, TTag>.FromValidated(value),
            static value => value.TryGetValue(out var raw)
                ? Option.Some(raw)
                : Option.None<TValue>(),
            "not a validated " + name);
    }

    /// <summary>Gets the non-empty human-readable boundary name used in diagnostics.</summary>
    public string Name { get; }

    /// <summary>
    /// Gets the contract for the tagged value.
    /// </summary>
    /// <remarks>
    /// The derived contract validates the underlying value, accepts only initialized wrappers,
    /// and reuses the underlying contract's decoding, generation, shrinking, and schema
    /// descriptions. A default or uninitialized wrapper is an <see cref="DecodeErrorKind.InvalidValue"/>
    /// at the root rather than a valid value.
    /// </remarks>
    public Contract<DomainValue<TValue, TTag>> Contract { get; }

    /// <summary>
    /// Validates an underlying value and wraps it when it satisfies the boundary.
    /// </summary>
    /// <param name="value">The underlying value to check; a runtime null is a <see cref="DecodeErrorKind.Null"/> error.</param>
    /// <returns>
    /// A valid validation containing the tagged value, or every accumulated contract error in
    /// deterministic order.
    /// </returns>
    /// <remarks>
    /// Validation checks the existing value; it does not normalize or copy it. The boundary's
    /// guarantee begins only when this call returns a valid result or a decoder succeeds.
    /// </remarks>
    public Validation<DomainValue<TValue, TTag>, DecodeError> Validate(TValue value) =>
        underlying.Validate(value).Map(
            static raw => DomainValue<TValue, TTag>.FromValidated(raw));

    /// <summary>
    /// Creates a decoder that validates the underlying decoded value and returns the tagged value.
    /// </summary>
    /// <returns>
    /// A successful result containing the decoder, or the underlying contract's
    /// <see cref="ContractLimitation"/> when no decoder can be derived.
    /// </returns>
    /// <remarks>
    /// The decoder is the underlying contract's decoder, so field and element locations, missing,
    /// null, wrong-kind, malformed, empty, and invalid-value diagnostics are preserved. The tag is
    /// added only after the underlying decode and contract validation succeed.
    /// </remarks>
    public Result<Decoder<DomainValue<TValue, TTag>>, ContractLimitation> CreateDecoder()
    {
        var created = underlying.CreateDecoder();
        if (created.TryGetError(out var limitation))
        {
            return Result<Decoder<DomainValue<TValue, TTag>>, ContractLimitation>.Failure(limitation);
        }

        created.TryGetValue(out var decoder);
        return Result<Decoder<DomainValue<TValue, TTag>>, ContractLimitation>.Success(
            decoder!.Map(static value => DomainValue<TValue, TTag>.FromValidated(value)));
    }
}
