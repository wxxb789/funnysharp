namespace FunnySharp;

/// <summary>
/// Identifies one interpretation a <see cref="Contract{T}"/> can derive from its description.
/// </summary>
/// <remarks>
/// Validation is intrinsic to every contract and therefore has no member here. The other
/// interpretations are derived only when the described structure supports them; an unavailable
/// derivation is reported as a <see cref="ContractLimitation"/>.
/// </remarks>
public enum ContractDerivation
{
    /// <summary>Creating a <see cref="Decoder{T}"/> from the contract.</summary>
    Decoding,

    /// <summary>Generating values that satisfy the contract.</summary>
    Generation,

    /// <summary>Producing smaller values that still satisfy the contract.</summary>
    Shrinking,

    /// <summary>Exposing the contract's structural metadata.</summary>
    Schema,
}
