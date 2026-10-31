namespace FunnySharp;

/// <summary>
/// A labelled alternative in a one-of contract.
/// </summary>
/// <typeparam name="T">The value type shared by every alternative.</typeparam>
/// <remarks>
/// Create an alternative with <see cref="Contract.Alternative{T}"/>. The label participates in
/// schema metadata, diagnostics, and generation limitation paths; the selector supplied to
/// <see cref="Contract.OneOf{T}"/> decides which contract applies.
/// </remarks>
public sealed class ContractAlternative<T>
{
    internal ContractAlternative(string label, Contract<T> contract)
    {
        Label = label;
        Contract = contract;
    }

    /// <summary>Gets the non-empty alternative label.</summary>
    public string Label { get; }

    /// <summary>Gets the alternative contract.</summary>
    public Contract<T> Contract { get; }

    internal ContractAlternativeDescription Describe() =>
        new(Label, Contract.Description);
}
