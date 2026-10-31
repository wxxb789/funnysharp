namespace FunnySharp;

/// <summary>
/// Describes one labelled alternative inside a one-of <see cref="ContractDescription"/>.
/// </summary>
/// <remarks>
/// The label is used for schema metadata, validation and decoding diagnostics, and generation
/// limitation paths. Alternative contracts are not assumed to be mutually exclusive; the
/// consumer-supplied selector chooses the applicable alternative.
/// </remarks>
public sealed class ContractAlternativeDescription
{
    /// <summary>Creates an alternative description.</summary>
    /// <param name="label">The non-empty alternative label.</param>
    /// <param name="contract">The non-null alternative description.</param>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="label"/> is empty.</exception>
    public ContractAlternativeDescription(string label, ContractDescription contract)
    {
        ArgumentException.ThrowIfNullOrEmpty(label);
        ArgumentNullException.ThrowIfNull(contract);

        Label = label;
        Contract = contract;
    }

    /// <summary>Gets the alternative label.</summary>
    public string Label { get; }

    /// <summary>Gets the alternative description.</summary>
    public ContractDescription Contract { get; }

    /// <summary>Returns a compact deterministic summary of this alternative.</summary>
    /// <returns>The summary text.</returns>
    public override string ToString() => $"{Label}: {Contract}";
}
