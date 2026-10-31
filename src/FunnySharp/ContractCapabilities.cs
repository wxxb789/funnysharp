namespace FunnySharp;

/// <summary>
/// Reports the interpretations a contract can derive and the limitations that prevent others.
/// </summary>
/// <remarks>
/// Validation is intrinsic to every <see cref="Contract{T}"/> and is always available. The flags
/// describe complete derivations: a derivation is available only when every rule in the contract
/// supports it. A caller-supplied extension (<c>WithDecoder</c>, <c>WithGenerator</c>,
/// <c>WithShrinker</c>, or <c>WithSchema</c>) replaces the limitation for that derivation at the
/// extended contract. <see cref="Limitations"/> is deterministic; it is ordered by derivation and
/// then by the contract's field, element, and alternative order.
/// </remarks>
public sealed class ContractCapabilities
{
    internal ContractCapabilities(IReadOnlyList<ContractLimitation> limitations)
    {
        Limitations = Array.AsReadOnly(limitations.ToArray());
        CanDecode = !HasLimitation(ContractDerivation.Decoding);
        CanGenerate = !HasLimitation(ContractDerivation.Generation);
        CanShrink = !HasLimitation(ContractDerivation.Shrinking);
        HasCompleteSchema = !HasLimitation(ContractDerivation.Schema);
    }

    private bool HasLimitation(ContractDerivation derivation)
    {
        foreach (var limitation in Limitations)
        {
            if (limitation.Derivation == derivation)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Gets a value indicating whether the contract can create a decoder.</summary>
    public bool CanDecode { get; }

    /// <summary>Gets a value indicating whether the contract can generate values.</summary>
    public bool CanGenerate { get; }

    /// <summary>Gets a value indicating whether the contract can shrink values.</summary>
    public bool CanShrink { get; }

    /// <summary>
    /// Gets a value indicating whether the contract's structural metadata is complete.
    /// </summary>
    /// <remarks>
    /// Arbitrary predicates, transformations, and unexpanded recursive references make the schema
    /// incomplete even though <see cref="Contract{T}.Description"/> and
    /// <see cref="Contract{T}.Explain"/> remain available. Supplying <c>WithSchema</c> makes the
    /// schema complete at that contract.
    /// </remarks>
    public bool HasCompleteSchema { get; }

    /// <summary>Gets every limitation that the contract reports.</summary>
    public IReadOnlyList<ContractLimitation> Limitations { get; }
}
