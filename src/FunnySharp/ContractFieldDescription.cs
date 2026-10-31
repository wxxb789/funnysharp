namespace FunnySharp;

/// <summary>
/// Describes one object field inside a <see cref="ContractDescription"/>.
/// </summary>
/// <remarks>
/// The name, presence policy, and nested description are the single declaration reused by object
/// validation, decoding, generation, shrinking, and explanation. The member contract is not
/// duplicated for each interpretation.
/// </remarks>
public sealed class ContractFieldDescription
{
    /// <summary>Creates a field description.</summary>
    /// <param name="name">The non-empty case-sensitive property name.</param>
    /// <param name="contract">The non-null member description.</param>
    /// <param name="presence">The missing/null policy.</param>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="presence"/> is not defined.</exception>
    public ContractFieldDescription(
        string name,
        ContractDescription contract,
        ContractFieldPresence presence)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(contract);
        if (!Enum.IsDefined(presence))
        {
            throw new ArgumentOutOfRangeException(nameof(presence));
        }

        Name = name;
        Contract = contract;
        Presence = presence;
    }

    /// <summary>Gets the case-sensitive property name.</summary>
    public string Name { get; }

    /// <summary>Gets the member description.</summary>
    public ContractDescription Contract { get; }

    /// <summary>Gets the missing/null policy for this field.</summary>
    public ContractFieldPresence Presence { get; }

    /// <summary>Returns a compact deterministic summary of this field.</summary>
    /// <returns>The summary text.</returns>
    public override string ToString() =>
        $"{Name}: {Contract} ({Presence})";
}
