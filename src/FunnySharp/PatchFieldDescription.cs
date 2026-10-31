namespace FunnySharp;

/// <summary>
/// Describes one allowed field of a <see cref="PatchSchema{T}"/>.
/// </summary>
public sealed class PatchFieldDescription
{
    internal PatchFieldDescription(string name, ContractDescription value, bool allowsClear)
    {
        Name = name;
        Value = value;
        AllowsClear = allowsClear;
    }

    /// <summary>Gets the case-sensitive JSON property name.</summary>
    public string Name { get; }

    /// <summary>Gets the description of the replacement value contract.</summary>
    public ContractDescription Value { get; }

    /// <summary>Gets a value indicating whether an explicit JSON null clears the field.</summary>
    public bool AllowsClear { get; }

    /// <summary>Returns a compact deterministic summary of this field.</summary>
    /// <returns>The field name, value description, and clearing policy.</returns>
    public override string ToString() =>
        $"{Name}: {Value} ({(AllowsClear ? "clearable" : "replacement only")})";
}
