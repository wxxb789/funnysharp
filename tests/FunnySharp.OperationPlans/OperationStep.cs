using FunnySharp;

namespace FunnySharp.OperationPlans;

/// <summary>
/// Couples one operation's description to its typed delegate so the two cannot drift.
/// </summary>
/// <typeparam name="TEnvironment">The caller-owned execution context.</typeparam>
/// <typeparam name="TState">The caller-owned typed state carried between operations.</typeparam>
/// <typeparam name="TError">The caller's domain failure type.</typeparam>
public sealed class OperationStep<TEnvironment, TState, TError>
{
    /// <summary>
    /// Creates an operation step.
    /// </summary>
    /// <param name="info">The description exposed before execution.</param>
    /// <param name="run">The typed operation.</param>
    /// <param name="condition">An optional predicate over the current state; null always runs.</param>
    /// <exception cref="ArgumentNullException"><paramref name="info"/> or <paramref name="run"/> is null.</exception>
    public OperationStep(
        OperationInfo info,
        Func<TEnvironment, TState, CancellationToken, Task<Result<TState, TError>>> run,
        Func<TState, bool>? condition = null)
    {
        ArgumentNullException.ThrowIfNull(info);
        ArgumentNullException.ThrowIfNull(run);

        Info = info;
        Run = run;
        Condition = condition;
    }

    /// <summary>The description exposed before execution.</summary>
    internal OperationInfo Info { get; }

    /// <summary>The typed operation.</summary>
    internal Func<TEnvironment, TState, CancellationToken, Task<Result<TState, TError>>> Run { get; }

    /// <summary>An optional predicate over the current state; null always runs.</summary>
    internal Func<TState, bool>? Condition { get; }
}
