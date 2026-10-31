namespace FunnySharp;

/// <summary>
/// The result of applying a <see cref="Patch{T}"/> to a source value.
/// </summary>
/// <typeparam name="T">The changed value type.</typeparam>
/// <remarks>
/// <para>
/// <see cref="Candidate"/> is the value produced by invoking each recorded field's lens setter or
/// clear callback once, in patch order. The library never mutates or clones <see cref="Source"/>;
/// record <c>with</c> setters create a new value, while a caller-supplied setter that mutates its
/// input is the caller's policy. When the patch has no recorded changes, the candidate is the
/// exact source reference.
/// </para>
/// <para>
/// Applying a patch copies once per changed field: each immutable setter receives the candidate
/// produced by the previous setter. An ordinary single <c>with</c> expression copies once for all
/// fields; a patch trades that single copy for the typed, reusable field composition.
/// </para>
/// <para>
/// <see cref="Source"/> is the exact argument validated by
/// <see cref="PatchSchema{T}.Validate"/>. No snapshot, version, or concurrency token is read or
/// retained. A caller that needs optimistic concurrency must compare its own version before
/// validating, or include the version in the candidate contract.
/// </para>
/// <para>
/// <see cref="IsIdentity"/> uses <see cref="EqualityComparer{T}.Default"/>: an empty patch, or a
/// replacement equal to the current value, is value-identical even when a setter allocated a new
/// instance. <see cref="Changes"/> records what the patch attempted, not whether the attempt
/// changed the value.
/// </para>
/// </remarks>
public readonly struct PatchApplication<T>
{
    private readonly Patch<T> patch;

    internal PatchApplication(T source, T candidate, Patch<T> patch)
    {
        Source = source;
        Candidate = candidate;
        this.patch = patch;
    }

    /// <summary>Gets the exact source value supplied to the application.</summary>
    public T Source { get; }

    /// <summary>Gets the candidate produced by applying the recorded changes in order.</summary>
    public T Candidate { get; }

    /// <summary>
    /// Gets the inspected changes in application order.
    /// </summary>
    /// <remarks>
    /// Reading this property materializes an inspection array and boxes each replacement value;
    /// application itself does not materialize it.
    /// </remarks>
    public IReadOnlyList<PatchEntry> Changes => patch.Entries;

    /// <summary>
    /// Gets a value indicating whether the source and candidate are equal under
    /// <see cref="EqualityComparer{T}.Default"/>.
    /// </summary>
    public bool IsIdentity =>
        EqualityComparer<T>.Default.Equals(Source, Candidate);
}
