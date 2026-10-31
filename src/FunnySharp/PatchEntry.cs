namespace FunnySharp;

/// <summary>
/// Describes one recorded field change for inspection.
/// </summary>
/// <remarks>
/// Entries are produced by <see cref="Patch{T}.Entries"/> and <see cref="PatchApplication{T}.Changes"/>.
/// They expose a boxed replacement value so a heterogeneous patch can be inspected without
/// reflection. Value-type replacements are boxed when <see cref="Value"/> is read; use
/// <see cref="Patch{T}.TryGet{TValue}"/> when typed access without boxing is required. Equality
/// uses the boxed value's <see cref="object.Equals(object, object)"/> implementation, so mutable
/// collections compare by reference unless their type supplies value equality.
/// </remarks>
public readonly struct PatchEntry : IEquatable<PatchEntry>
{
    private readonly string? name;
    private readonly object? value;

    internal PatchEntry(string name, PatchChangeKind kind, object? value)
    {
        this.name = name;
        this.value = value;
        Kind = kind;
    }

    /// <summary>Gets the case-sensitive field name; the default entry has an empty name.</summary>
    public string Name => name ?? string.Empty;

    /// <summary>Gets the change state.</summary>
    public PatchChangeKind Kind { get; }

    /// <summary>
    /// Gets the replacement value for a <see cref="PatchChangeKind.Set"/> entry, or
    /// <see langword="null"/> for an unchanged or cleared entry.
    /// </summary>
    public object? Value => Kind == PatchChangeKind.Set ? value : null;

    /// <summary>
    /// Returns a value indicating whether this entry and <paramref name="other"/> have the same
    /// name, state, and replacement value.
    /// </summary>
    /// <param name="other">The entry to compare.</param>
    /// <returns><see langword="true"/> when the entries are structurally equal.</returns>
    public bool Equals(PatchEntry other) =>
        string.Equals(Name, other.Name, StringComparison.Ordinal)
        && Kind == other.Kind
        && Equals(Value, other.Value);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is PatchEntry other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() =>
        HashCode.Combine(Name, Kind, Value);

    /// <summary>Returns a deterministic description of the entry.</summary>
    /// <returns>The field name and change state, including a replacement value for a set entry.</returns>
    public override string ToString() =>
        Kind switch
        {
            PatchChangeKind.Set => $"{Name}: set {Value}",
            PatchChangeKind.Clear => $"{Name}: clear",
            _ => $"{Name}: unchanged",
        };

    /// <summary>Returns a value indicating whether two entries are structurally equal.</summary>
    /// <param name="left">The first entry.</param>
    /// <param name="right">The second entry.</param>
    /// <returns><see langword="true"/> when the entries are equal.</returns>
    public static bool operator ==(PatchEntry left, PatchEntry right) => left.Equals(right);

    /// <summary>Returns a value indicating whether two entries differ.</summary>
    /// <param name="left">The first entry.</param>
    /// <param name="right">The second entry.</param>
    /// <returns><see langword="true"/> when the entries differ.</returns>
    public static bool operator !=(PatchEntry left, PatchEntry right) => !left.Equals(right);
}
