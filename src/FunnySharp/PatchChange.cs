using System.Diagnostics.CodeAnalysis;

namespace FunnySharp;

/// <summary>
/// Represents one field's contribution to a partial change.
/// </summary>
/// <typeparam name="T">The field value type.</typeparam>
/// <remarks>
/// <see cref="Unchanged"/> is the default value and means the field is absent from the change.
/// <see cref="Set"/> carries a replacement value; <see cref="Clear"/> asks the owning field to
/// remove its value through the clear callback supplied at field construction. Clearing is not a
/// null replacement: <see cref="Set"/> rejects a runtime-null value, preserving
/// <see cref="Option{T}"/>'s existing null contract. Equality compares the state and, for
/// <see cref="Set"/>, the value using <see cref="EqualityComparer{T}.Default"/>; the default and
/// cleared states ignore the stored default value.
/// </remarks>
public readonly struct PatchChange<T> : IEquatable<PatchChange<T>>
{
    private readonly PatchChangeKind kind;
    private readonly T? value;

    private PatchChange(PatchChangeKind kind, T? value)
    {
        this.kind = kind;
        this.value = value;
    }

    /// <summary>Gets the change that keeps the current field value.</summary>
    public static PatchChange<T> Unchanged => default;

    /// <summary>Gets the change that clears the current field value.</summary>
    public static PatchChange<T> Clear => new(PatchChangeKind.Clear, default);

    /// <summary>Gets the state of this change.</summary>
    public PatchChangeKind Kind => kind;

    /// <summary>Gets a value indicating whether this change leaves the field unchanged.</summary>
    public bool IsUnchanged => kind == PatchChangeKind.Unchanged;

    /// <summary>Gets a value indicating whether this change supplies a replacement value.</summary>
    public bool IsSet => kind == PatchChangeKind.Set;

    /// <summary>Gets a value indicating whether this change clears the field.</summary>
    public bool IsClear => kind == PatchChangeKind.Clear;

    /// <summary>
    /// Gets the replacement value as an option; unavailable for <see cref="Unchanged"/> and
    /// <see cref="Clear"/>.
    /// </summary>
    public Option<T> Value =>
        kind == PatchChangeKind.Set ? Option.Some(value!) : Option<T>.None;

    /// <summary>Creates a replacement change.</summary>
    /// <param name="value">The non-null replacement value.</param>
    /// <returns>A change whose kind is <see cref="PatchChangeKind.Set"/>.</returns>
    /// <remarks>
    /// A runtime-null reference or nullable value is rejected. To remove a value, use
    /// <see cref="Clear"/> on a clearable field; use an <see cref="Option{T}"/> field contract when
    /// the domain represents absence explicitly.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public static PatchChange<T> Set([DisallowNull] T value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        return new PatchChange<T>(PatchChangeKind.Set, value);
    }

    /// <summary>Attempts to retrieve the replacement value.</summary>
    /// <param name="value">The replacement value, or <see langword="default"/> for any other state.</param>
    /// <returns><see langword="true"/> only when this change is <see cref="PatchChangeKind.Set"/>.</returns>
    public bool TryGetValue([MaybeNullWhen(false)] out T value)
    {
        value = this.value;
        return kind == PatchChangeKind.Set;
    }

    /// <summary>Matches the change and returns the selected branch result.</summary>
    /// <typeparam name="TResult">The branch result type.</typeparam>
    /// <param name="set">Invoked with the replacement value for <see cref="PatchChangeKind.Set"/>.</param>
    /// <param name="clear">Invoked for <see cref="PatchChangeKind.Clear"/>.</param>
    /// <param name="unchanged">Invoked for <see cref="PatchChangeKind.Unchanged"/>.</param>
    /// <returns>The selected branch result.</returns>
    /// <exception cref="ArgumentNullException">A branch is <see langword="null"/>.</exception>
    public TResult Match<TResult>(
        Func<T, TResult> set,
        Func<TResult> clear,
        Func<TResult> unchanged)
    {
        ArgumentNullException.ThrowIfNull(set);
        ArgumentNullException.ThrowIfNull(clear);
        ArgumentNullException.ThrowIfNull(unchanged);

        return kind switch
        {
            PatchChangeKind.Set => set(value!),
            PatchChangeKind.Clear => clear(),
            _ => unchanged(),
        };
    }

    /// <summary>
    /// Returns a value indicating whether this change and <paramref name="other"/> have the same
    /// state and, for a set change, the same replacement value.
    /// </summary>
    /// <param name="other">The change to compare.</param>
    /// <returns><see langword="true"/> when the changes are structurally equal.</returns>
    public bool Equals(PatchChange<T> other) =>
        kind == other.kind
        && (kind != PatchChangeKind.Set
            || EqualityComparer<T>.Default.Equals(value!, other.value!));

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        obj is PatchChange<T> other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() =>
        kind == PatchChangeKind.Set
            ? HashCode.Combine(kind, value is null ? 0 : EqualityComparer<T>.Default.GetHashCode(value))
            : HashCode.Combine(kind);

    /// <summary>Returns a deterministic description of the change state.</summary>
    /// <returns><c>Unchanged</c>, <c>Clear</c>, or <c>Set(value)</c>.</returns>
    public override string ToString() =>
        kind switch
        {
            PatchChangeKind.Set => $"Set({value})",
            PatchChangeKind.Clear => "Clear",
            _ => "Unchanged",
        };

    /// <summary>Returns a value indicating whether two changes are structurally equal.</summary>
    /// <param name="left">The first change.</param>
    /// <param name="right">The second change.</param>
    /// <returns><see langword="true"/> when the changes are equal.</returns>
    public static bool operator ==(PatchChange<T> left, PatchChange<T> right) => left.Equals(right);

    /// <summary>Returns a value indicating whether two changes differ.</summary>
    /// <param name="left">The first change.</param>
    /// <param name="right">The second change.</param>
    /// <returns><see langword="true"/> when the changes differ.</returns>
    public static bool operator !=(PatchChange<T> left, PatchChange<T> right) => !left.Equals(right);
}
