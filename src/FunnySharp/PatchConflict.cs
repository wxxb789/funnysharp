namespace FunnySharp;

/// <summary>
/// Describes two partial changes that both write the same field.
/// </summary>
/// <remarks>
/// Combining changes has no last-writer-wins or merge policy. Any field name present in both
/// operands is a conflict, even when the replacement values are equal. The conflict retains both
/// inspected entries so a caller can explain the disagreement before deciding how to proceed.
/// Equality compares the field name and both entries.
/// </remarks>
public sealed class PatchConflict : IEquatable<PatchConflict>
{
    internal PatchConflict(PatchEntry first, PatchEntry second)
    {
        First = first;
        Second = second;
    }

    /// <summary>Gets the conflicting case-sensitive field name.</summary>
    public string Field => First.Name;

    /// <summary>Gets the entry from the left-hand patch.</summary>
    public PatchEntry First { get; }

    /// <summary>Gets the entry from the right-hand patch.</summary>
    public PatchEntry Second { get; }

    /// <summary>
    /// Returns a value indicating whether this conflict and <paramref name="other"/> identify the
    /// same field and entries.
    /// </summary>
    /// <param name="other">The conflict to compare, or <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when the conflicts are structurally equal.</returns>
    public bool Equals(PatchConflict? other) =>
        other is not null
        && string.Equals(Field, other.Field, StringComparison.Ordinal)
        && First.Equals(other.First)
        && Second.Equals(other.Second);

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as PatchConflict);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Field, First, Second);

    /// <summary>Returns deterministic diagnostic text for the conflict.</summary>
    /// <returns>The field name and both writes.</returns>
    public override string ToString() => $"{Field}: {First} conflicts with {Second}";
}
