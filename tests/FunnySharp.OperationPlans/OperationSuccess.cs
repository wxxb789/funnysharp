using System.Collections.ObjectModel;

namespace FunnySharp.OperationPlans;

/// <summary>
/// Describes a completed plan run: final typed state, executed names, and skipped names.
/// </summary>
/// <typeparam name="TState">The caller-owned typed state.</typeparam>
public sealed class OperationSuccess<TState>
{
    private readonly ReadOnlyCollection<string> _executed;
    private readonly ReadOnlyCollection<string> _skipped;

    /// <summary>
    /// Creates a successful run description.
    /// </summary>
    /// <param name="state">The state after the last executed operation.</param>
    /// <param name="executed">Executed operation names in order.</param>
    /// <param name="skipped">Skipped operation names in order.</param>
    /// <exception cref="ArgumentNullException"><paramref name="executed"/> or <paramref name="skipped"/> is null.</exception>
    public OperationSuccess(TState state, IReadOnlyList<string> executed, IReadOnlyList<string> skipped)
    {
        ArgumentNullException.ThrowIfNull(executed);
        ArgumentNullException.ThrowIfNull(skipped);

        State = state;
        _executed = Array.AsReadOnly(executed.ToArray());
        _skipped = Array.AsReadOnly(skipped.ToArray());
    }

    /// <summary>The state after the last executed operation.</summary>
    public TState State { get; }

    /// <summary>Executed operation names in order.</summary>
    public IReadOnlyList<string> Executed => _executed;

    /// <summary>Skipped operation names in order.</summary>
    public IReadOnlyList<string> Skipped => _skipped;
}
