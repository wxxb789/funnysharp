using System.Collections.ObjectModel;

namespace FunnySharp.OperationPlans;

/// <summary>
/// Classifies why a plan run did not succeed.
/// </summary>
public enum RunFailureKind
{
    /// <summary>An operation returned a typed domain failure.</summary>
    OperationFailed,

    /// <summary>An operation or condition threw a non-cancellation exception.</summary>
    OperationFaulted,

    /// <summary>Cancellation was observed at a step boundary, before commit, or inside an operation.</summary>
    Canceled,

    /// <summary>The commit failed.</summary>
    CommitFailed,

    /// <summary>Rollback failed; the original failure is preserved alongside the rollback exception.</summary>
    RollbackFailed,
}

/// <summary>
/// Preserves the responsible operation, the typed failure, and the last good context.
/// </summary>
/// <typeparam name="TState">The caller-owned typed state.</typeparam>
/// <typeparam name="TError">The caller's domain failure type.</typeparam>
public sealed class RunFailure<TState, TError>
{
    private readonly ReadOnlyCollection<string> _completed;

    /// <summary>
    /// Creates a run failure.
    /// </summary>
    /// <param name="kind">The failure class.</param>
    /// <param name="originalKind">The pre-rollback failure kind for <see cref="RunFailureKind.RollbackFailed"/>; otherwise null.</param>
    /// <param name="operation">The responsible or unstarted operation, when one applies.</param>
    /// <param name="error">The typed domain error for <see cref="RunFailureKind.OperationFailed"/>.</param>
    /// <param name="exception">The observed exception for faults, cancellation, or commit failure.</param>
    /// <param name="rollbackException">The rollback exception for <see cref="RunFailureKind.RollbackFailed"/>.</param>
    /// <param name="completed">Operations that returned success before the failure.</param>
    /// <param name="state">The last good typed state.</param>
    /// <exception cref="ArgumentNullException"><paramref name="completed"/> is null.</exception>
    public RunFailure(
        RunFailureKind kind,
        RunFailureKind? originalKind,
        string? operation,
        TError? error,
        Exception? exception,
        Exception? rollbackException,
        IReadOnlyList<string> completed,
        TState state)
    {
        ArgumentNullException.ThrowIfNull(completed);

        Kind = kind;
        OriginalKind = originalKind;
        Operation = operation;
        Error = error;
        Exception = exception;
        RollbackException = rollbackException;
        _completed = Array.AsReadOnly(completed.ToArray());
        State = state;
    }

    /// <summary>The failure class.</summary>
    public RunFailureKind Kind { get; }

    /// <summary>The pre-rollback failure kind for <see cref="RunFailureKind.RollbackFailed"/>.</summary>
    public RunFailureKind? OriginalKind { get; }

    /// <summary>The responsible or unstarted operation, when one applies.</summary>
    public string? Operation { get; }

    /// <summary>The typed domain error for <see cref="RunFailureKind.OperationFailed"/>.</summary>
    public TError? Error { get; }

    /// <summary>The observed exception for faults, cancellation, or commit failure.</summary>
    public Exception? Exception { get; }

    /// <summary>The rollback exception for <see cref="RunFailureKind.RollbackFailed"/>.</summary>
    public Exception? RollbackException { get; }

    /// <summary>Operations that returned success before the failure.</summary>
    public IReadOnlyList<string> Completed => _completed;

    /// <summary>The last good typed state.</summary>
    public TState State { get; }
}
