namespace FunnySharp.Incremental;

/// <summary>
/// A caller-visible assertion that a graph computation is deterministic and side-effect free.
/// Construct it only through <see cref="Of"/>; the graph may skip or reuse its result after an
/// equality cutoff and does not detect impurity.
/// </summary>
public readonly struct Pure<TInput, TResult>
{
    private readonly Func<TInput, TResult>? _compute;

    private Pure(Func<TInput, TResult> compute) => _compute = compute;

    internal bool IsInitialized => _compute is not null;

    /// <summary>Wraps a deterministic, side-effect-free computation.</summary>
    public static Pure<TInput, TResult> Of(Func<TInput, TResult> compute)
    {
        ArgumentNullException.ThrowIfNull(compute);
        return new Pure<TInput, TResult>(compute);
    }

    internal TResult Invoke(TInput input) =>
        (_compute ?? throw new InvalidOperationException(
            "The pure computation was not initialized. Create it with Pure<TInput, TResult>.Of."))(input);
}