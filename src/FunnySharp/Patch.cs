using System.Runtime.CompilerServices;

namespace FunnySharp;

/// <summary>
/// Provides factories for partial-update fields and schemas.
/// </summary>
/// <remarks>
/// A patch is an immutable, ordered set of typed field changes. The static methods here bind
/// allowed fields; <see cref="PatchSchema{T}"/> decodes and validates them, and
/// <see cref="Patch{T}"/> records programmatic changes and detects conflicting writes.
/// </remarks>
public static class Patch
{
    /// <summary>Creates a parent-typed field set for concise field binding.</summary>
    /// <typeparam name="T">The type of the value being changed.</typeparam>
    /// <returns>A stateless field set whose methods fix the parent type.</returns>
    public static PatchFieldSet<T> For<T>() => new();

    /// <summary>Binds an allowed field that accepts replacements but cannot be cleared.</summary>
    /// <typeparam name="T">The type of the value being changed.</typeparam>
    /// <typeparam name="TValue">The replacement value type.</typeparam>
    /// <param name="name">The non-empty case-sensitive JSON property name.</param>
    /// <param name="contract">The non-null contract used to decode and validate replacement values.</param>
    /// <param name="lens">The initialized lens that reads and replaces the field.</param>
    /// <returns>The field binding, reused by decoding, validation, explanation, and application.</returns>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="lens"/> is uninitialized.</exception>
    public static PatchField<T, TValue> Field<T, TValue>(
        string name,
        Contract<TValue> contract,
        Lens<T, TValue> lens)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(contract);
        lens.EnsureInitialized();

        return new PatchField<T, TValue>(name, contract, lens, clear: null);
    }

    /// <summary>Binds an allowed field that accepts replacements and an explicit clearing instruction.</summary>
    /// <typeparam name="T">The type of the value being changed.</typeparam>
    /// <typeparam name="TValue">The replacement value type.</typeparam>
    /// <param name="name">The non-empty case-sensitive JSON property name.</param>
    /// <param name="contract">The non-null contract used to decode and validate replacement values.</param>
    /// <param name="lens">The initialized lens that reads and replaces the field.</param>
    /// <param name="clear">The non-null callback that returns a candidate with the field cleared.</param>
    /// <returns>The field binding, reused by decoding, validation, explanation, and application.</returns>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is empty.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="lens"/> is uninitialized.</exception>
    public static PatchField<T, TValue> ClearableField<T, TValue>(
        string name,
        Contract<TValue> contract,
        Lens<T, TValue> lens,
        Func<T, T> clear)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(clear);
        lens.EnsureInitialized();

        return new PatchField<T, TValue>(name, contract, lens, clear);
    }
}

/// <summary>
/// An immutable, ordered set of typed field changes.
/// </summary>
/// <typeparam name="T">The type of the value being changed.</typeparam>
/// <remarks>
/// <para>
/// <see cref="Empty"/> and <c>default(Patch&lt;T&gt;)</c> are the same empty patch. Adding the
/// same field name twice is rejected; combining two patches reports every repeated field as a
/// <see cref="PatchConflict"/> rather than choosing a merge policy. Patch equality compares the
/// ordered field bindings, change states, and replacement values; two patches built from
/// equivalent but separately constructed field descriptors are not guaranteed to be equal.
/// </para>
/// <para>
/// Applying a patch invokes each field's setter or clear callback once, in insertion order. The
/// library never mutates or clones the source itself. Callback exceptions propagate unchanged and
/// are not rolled back, so a setter with observable side effects must be treated like any other
/// caller-owned callback.
/// </para>
/// </remarks>
public readonly struct Patch<T> : IEquatable<Patch<T>>
{
    private readonly IEntry[]? entries;

    private Patch(IEntry[] entries) => this.entries = entries;

    /// <summary>Gets the empty patch; this is also the default value.</summary>
    public static Patch<T> Empty => default;

    /// <summary>Gets the number of recorded field changes.</summary>
    public int Count => entries?.Length ?? 0;

    /// <summary>Gets a value indicating whether this patch records no changes.</summary>
    public bool IsEmpty => Count == 0;

    /// <summary>
    /// Gets the recorded changes in application order.
    /// </summary>
    /// <remarks>
    /// Reading this property materializes an inspection array and boxes each replacement value.
    /// Use <see cref="TryGet{TValue}"/> for typed access without materializing the view.
    /// </remarks>
    public IReadOnlyList<PatchEntry> Entries
    {
        get
        {
            if (entries is null || entries.Length == 0)
            {
                return Array.Empty<PatchEntry>();
            }

            var result = new PatchEntry[entries.Length];
            for (var index = 0; index < entries.Length; index++)
            {
                result[index] = entries[index].Describe();
            }

            return result;
        }
    }

    /// <summary>
    /// Records one field change.
    /// </summary>
    /// <typeparam name="TValue">The replacement value type.</typeparam>
    /// <param name="field">The non-null field binding.</param>
    /// <param name="change">A set or clearing change; <see cref="PatchChangeKind.Unchanged"/> is not recorded.</param>
    /// <returns>A new patch containing the existing changes followed by <paramref name="change"/>.</returns>
    /// <remarks>
    /// The returned patch is independent of this value. Recording the same field name twice is a
    /// programming error; combine separately built patches with <see cref="Combine"/> when a
    /// conflict must be reported as data.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="field"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="change"/> is unchanged, the field does not allow clearing, or the patch
    /// already contains a change for <paramref name="field"/>'s name.
    /// </exception>
    public Patch<T> With<TValue>(PatchField<T, TValue> field, PatchChange<TValue> change)
    {
        ArgumentNullException.ThrowIfNull(field);

        if (change.IsUnchanged)
        {
            throw new ArgumentException(
                "An unchanged change is not recorded in a patch.",
                nameof(change));
        }

        if (change.IsClear && !field.AllowsClear)
        {
            throw new ArgumentException(
                $"The patch field '{field.Name}' does not allow clearing.",
                nameof(change));
        }

        if (Contains(field.Name))
        {
            throw new ArgumentException(
                $"The patch already writes the field '{field.Name}'.",
                nameof(field));
        }

        var next = new IEntry[Count + 1];
        if (entries is not null)
        {
            Array.Copy(entries, next, entries.Length);
        }

        next[^1] = new Entry<TValue>(field, change);
        return new Patch<T>(next);
    }

    /// <summary>Attempts to read the change recorded for <paramref name="field"/>.</summary>
    /// <typeparam name="TValue">The replacement value type.</typeparam>
    /// <param name="field">The non-null field binding to look up.</param>
    /// <param name="change">The recorded change, or <see cref="PatchChange{T}.Unchanged"/>.</param>
    /// <returns><see langword="true"/> when the patch records this exact field binding.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="field"/> is <see langword="null"/>.</exception>
    public bool TryGet<TValue>(PatchField<T, TValue> field, out PatchChange<TValue> change)
    {
        ArgumentNullException.ThrowIfNull(field);

        if (entries is not null)
        {
            for (var index = 0; index < entries.Length; index++)
            {
                if (entries[index].TryGet(field, out change))
                {
                    return true;
                }
            }
        }

        change = PatchChange<TValue>.Unchanged;
        return false;
    }

    /// <summary>
    /// Applies the recorded changes to <paramref name="source"/> in insertion order.
    /// </summary>
    /// <param name="source">The source value; it is not mutated or cloned by the library.</param>
    /// <returns>The exact source and the produced candidate, with an inspection view of the changes.</returns>
    /// <remarks>
    /// A setter or clear callback that throws propagates unchanged; no application result is
    /// returned and earlier callbacks are not undone. An empty patch does not invoke any callback
    /// and returns the source as the exact candidate reference.
    /// </remarks>
    public PatchApplication<T> ApplyTo(T source)
    {
        if (entries is null || entries.Length == 0)
        {
            return new PatchApplication<T>(source, source, this);
        }

        var candidate = source;
        for (var index = 0; index < entries.Length; index++)
        {
            candidate = entries[index].Apply(candidate);
        }

        return new PatchApplication<T>(source, candidate, this);
    }

    /// <summary>
    /// Combines two patches, reporting every field written by both operands as a conflict.
    /// </summary>
    /// <param name="other">The other patch.</param>
    /// <returns>
    /// A valid merged patch when the operands write disjoint fields, or every conflict when they
    /// overlap. A conflicted combine returns no partial merged patch.
    /// </returns>
    /// <remarks>
    /// A field present in both operands is a conflict even when both write the same value. The
    /// method deliberately implies no universal merge policy, last-writer-wins rule, or inverse.
    /// </remarks>
    public Validation<Patch<T>, PatchConflict> Combine(Patch<T> other)
    {
        if (IsEmpty)
        {
            return Validation<Patch<T>, PatchConflict>.Valid(other);
        }

        if (other.IsEmpty)
        {
            return Validation<Patch<T>, PatchConflict>.Valid(this);
        }

        var conflicts = new List<PatchConflict>();
        foreach (var entry in entries!)
        {
            if (other.TryGetEntry(entry.Name, out var right))
            {
                conflicts.Add(new PatchConflict(entry.Describe(), right.Describe()));
            }
        }

        if (conflicts.Count != 0)
        {
            return Validation<Patch<T>, PatchConflict>.InvalidMany(conflicts);
        }

        var merged = new IEntry[entries.Length + other.entries!.Length];
        Array.Copy(entries, merged, entries.Length);
        Array.Copy(other.entries, 0, merged, entries.Length, other.entries.Length);
        return Validation<Patch<T>, PatchConflict>.Valid(new Patch<T>(merged));
    }

    /// <summary>
    /// Returns a value indicating whether this patch and <paramref name="other"/> record the same
    /// field bindings and changes in the same order.
    /// </summary>
    /// <param name="other">The patch to compare.</param>
    /// <returns><see langword="true"/> when the ordered changes are structurally equal.</returns>
    public bool Equals(Patch<T> other)
    {
        if (Count != other.Count)
        {
            return false;
        }

        if (entries is null)
        {
            return true;
        }

        for (var index = 0; index < entries.Length; index++)
        {
            if (!entries[index].ContentEquals(other.entries![index]))
            {
                return false;
            }
        }

        return true;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Patch<T> other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Count);
        if (entries is not null)
        {
            foreach (var entry in entries)
            {
                hash.Add(entry.ContentHashCode());
            }
        }

        return hash.ToHashCode();
    }

    /// <summary>Returns a deterministic description of the recorded changes.</summary>
    /// <returns>The ordered field changes, or <c>{ }</c> for an empty patch.</returns>
    public override string ToString() =>
        IsEmpty
            ? "{ }"
            : "{ " + string.Join(", ", Entries) + " }";

    /// <summary>Returns a value indicating whether two patches are structurally equal.</summary>
    /// <param name="left">The first patch.</param>
    /// <param name="right">The second patch.</param>
    /// <returns><see langword="true"/> when the patches are equal.</returns>
    public static bool operator ==(Patch<T> left, Patch<T> right) => left.Equals(right);

    /// <summary>Returns a value indicating whether two patches differ.</summary>
    /// <param name="left">The first patch.</param>
    /// <param name="right">The second patch.</param>
    /// <returns><see langword="true"/> when the patches differ.</returns>
    public static bool operator !=(Patch<T> left, Patch<T> right) => !left.Equals(right);

    private bool Contains(string name)
    {
        if (entries is null)
        {
            return false;
        }

        foreach (var entry in entries)
        {
            if (string.Equals(entry.Name, name, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private bool TryGetEntry(string name, out IEntry entry)
    {
        if (entries is not null)
        {
            foreach (var candidate in entries)
            {
                if (string.Equals(candidate.Name, name, StringComparison.Ordinal))
                {
                    entry = candidate;
                    return true;
                }
            }
        }

        entry = null!;
        return false;
    }

    internal string? FindUnbound(PatchField<T>[] allowed)
    {
        if (entries is null)
        {
            return null;
        }

        foreach (var entry in entries)
        {
            var bound = false;
            foreach (var field in allowed)
            {
                if (ReferenceEquals(entry.Binding, field))
                {
                    bound = true;
                    break;
                }
            }

            if (!bound)
            {
                return entry.Name;
            }
        }

        return null;
    }

    private interface IEntry
    {
        string Name { get; }

        PatchField<T> Binding { get; }

        PatchChangeKind Kind { get; }

        object? Value { get; }

        PatchEntry Describe();

        T Apply(T source);

        bool TryGet<TValue>(PatchField<T, TValue> field, out PatchChange<TValue> change);

        bool ContentEquals(IEntry other);

        int ContentHashCode();
    }

    private sealed class Entry<TValue> : IEntry
    {
        private readonly PatchField<T, TValue> binding;
        private readonly PatchChange<TValue> change;

        internal Entry(PatchField<T, TValue> field, PatchChange<TValue> change)
        {
            binding = field;
            this.change = change;
        }

        public string Name => binding.Name;

        public PatchField<T> Binding => binding;

        public PatchChangeKind Kind => change.Kind;

        public object? Value =>
            change.TryGetValue(out var value) ? value : null;

        public PatchEntry Describe() => new(binding.Name, change.Kind, Value);

        public T Apply(T source) =>
            change.IsClear
                ? binding.ApplyClear(source)
                : binding.ApplySet(source, change.Value.GetValueOrDefault()!);

        public bool TryGet<TRequested>(
            PatchField<T, TRequested> requested,
            out PatchChange<TRequested> result)
        {
            if (binding is PatchField<T, TRequested> typed
                && ReferenceEquals(typed, requested))
            {
                result = (PatchChange<TRequested>)(object)change;
                return true;
            }

            result = PatchChange<TRequested>.Unchanged;
            return false;
        }

        public bool ContentEquals(IEntry other) =>
            other is Entry<TValue> typed
            && ReferenceEquals(binding, typed.binding)
            && change.Equals(typed.change);

        public int ContentHashCode() =>
            HashCode.Combine(RuntimeHelpers.GetHashCode(binding), change);
    }
}
