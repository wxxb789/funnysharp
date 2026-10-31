namespace FunnySharp;

/// <summary>
/// The result of applying a <see cref="Patch{T}"/> to a source value.
/// </summary>
/// <typeparam name="T">The changed value type.</typeparam>
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
    public IReadOnlyList<PatchEntry> Changes => patch.Entries;

    /// <summary>
    /// Gets a value indicating whether the source and candidate are equal under
    /// <see cref="EqualityComparer{T}.Default"/>.
    /// </summary>
    public bool IsIdentity =>
        EqualityComparer<T>.Default.Equals(Source, Candidate);
}
