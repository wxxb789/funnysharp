using System.Diagnostics.CodeAnalysis;

namespace FunnySharp;

/// <summary>
/// Describes the outcome of a state-machine transition.
/// </summary>
public enum TransitionStatus
{
    /// <summary>
    /// The machine has no defined transition for the state and event.
    /// </summary>
    Undefined = 0,

    /// <summary>
    /// The machine applied a transition and produced a state change.
    /// </summary>
    Applied,

    /// <summary>
    /// The machine recognized the event but rejected it for the current state.
    /// </summary>
    Rejected,

    /// <summary>
    /// The machine recognized the event but could not complete its transition.
    /// </summary>
    Failed,
}

/// <summary>
/// Represents the explicit outcome of a state-machine transition.
/// </summary>
/// <typeparam name="TState">The state type.</typeparam>
/// <typeparam name="TOutput">The output type.</typeparam>
/// <typeparam name="TError">The rejection and failure error type.</typeparam>
public readonly struct TransitionResult<TState, TOutput, TError> :
    IEquatable<TransitionResult<TState, TOutput, TError>>
{
    private readonly StateChange<TState, TOutput>? change;
    private readonly TError? error;
    private readonly TransitionStatus status;

    private TransitionResult(
        TransitionStatus status,
        StateChange<TState, TOutput>? change,
        TError? error)
    {
        this.status = status;
        this.change = change;
        this.error = error;
    }

    /// <summary>
    /// Gets the transition outcome status.
    /// </summary>
    public TransitionStatus Status => status;

    /// <summary>
    /// Gets a value indicating whether this result contains an applied state change.
    /// </summary>
    public bool IsApplied => status == TransitionStatus.Applied;

    /// <summary>
    /// Gets a value indicating whether this result contains a rejected-transition error.
    /// </summary>
    public bool IsRejected => status == TransitionStatus.Rejected;

    /// <summary>
    /// Gets a value indicating whether this result contains a failed-transition error.
    /// </summary>
    public bool IsFailed => status == TransitionStatus.Failed;

    /// <summary>
    /// Gets a value indicating whether this result has no defined transition.
    /// </summary>
    public bool IsUndefined => status == TransitionStatus.Undefined;

    /// <summary>
    /// Creates an applied transition result.
    /// </summary>
    /// <param name="change">The state change produced by the transition.</param>
    /// <returns>An applied result containing <paramref name="change"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="change"/> is <see langword="null"/>.</exception>
    public static TransitionResult<TState, TOutput, TError> Applied(
        StateChange<TState, TOutput> change)
    {
        ArgumentNullException.ThrowIfNull(change);
        return new TransitionResult<TState, TOutput, TError>(TransitionStatus.Applied, change, default);
    }

    /// <summary>
    /// Creates a rejected transition result.
    /// </summary>
    /// <param name="error">The rejection error.</param>
    /// <returns>A rejected result containing <paramref name="error"/>.</returns>
    public static TransitionResult<TState, TOutput, TError> Rejected(TError error) =>
        new(TransitionStatus.Rejected, default, error);

    /// <summary>
    /// Creates a failed transition result.
    /// </summary>
    /// <param name="error">The transition failure error.</param>
    /// <returns>A failed result containing <paramref name="error"/>.</returns>
    public static TransitionResult<TState, TOutput, TError> Failed(TError error) =>
        new(TransitionStatus.Failed, default, error);

    /// <summary>
    /// Creates an undefined transition result.
    /// </summary>
    /// <returns>An undefined result with no state change or error.</returns>
    public static TransitionResult<TState, TOutput, TError> Undefined() => default;

    /// <summary>
    /// Attempts to retrieve the applied state change.
    /// </summary>
    /// <param name="change">The applied state change, or <see langword="null"/> when not applied.</param>
    /// <returns><see langword="true"/> when this result is applied; otherwise, <see langword="false"/>.</returns>
    public bool TryGetChange([NotNullWhen(true)] out StateChange<TState, TOutput>? change)
    {
        if (IsApplied)
        {
            change = this.change!;
            return true;
        }

        change = null;
        return false;
    }

    /// <summary>
    /// Attempts to retrieve the rejection or failure error.
    /// </summary>
    /// <param name="error">The transition error, or <see langword="default"/> when no error is present.</param>
    /// <returns><see langword="true"/> when this result is rejected or failed; otherwise, <see langword="false"/>.</returns>
    public bool TryGetError([MaybeNull] out TError error)
    {
        error = this.error;
        return IsRejected || IsFailed;
    }

    /// <summary>
    /// Matches this result and returns the value from the selected branch.
    /// </summary>
    /// <typeparam name="TResult">The branch result type.</typeparam>
    /// <param name="applied">The branch invoked with an applied state change.</param>
    /// <param name="rejected">The branch invoked with a rejection error.</param>
    /// <param name="failed">The branch invoked with a failure error.</param>
    /// <param name="undefined">The branch invoked when no transition is defined.</param>
    /// <returns>The value returned by the selected branch.</returns>
    /// <exception cref="ArgumentNullException">Any branch delegate is <see langword="null"/>.</exception>
    public TResult Match<TResult>(
        Func<StateChange<TState, TOutput>, TResult> applied,
        Func<TError, TResult> rejected,
        Func<TError, TResult> failed,
        Func<TResult> undefined)
    {
        ArgumentNullException.ThrowIfNull(applied);
        ArgumentNullException.ThrowIfNull(rejected);
        ArgumentNullException.ThrowIfNull(failed);
        ArgumentNullException.ThrowIfNull(undefined);

        return status switch
        {
            TransitionStatus.Applied => applied(change!),
            TransitionStatus.Rejected => rejected(error!),
            TransitionStatus.Failed => failed(error!),
            _ => undefined(),
        };
    }

    /// <inheritdoc />
    public bool Equals(TransitionResult<TState, TOutput, TError> other) =>
        status == other.status &&
        status switch
        {
            TransitionStatus.Applied => EqualityComparer<StateChange<TState, TOutput>?>.Default.Equals(change, other.change),
            TransitionStatus.Rejected or TransitionStatus.Failed => EqualityComparer<TError>.Default.Equals(error!, other.error!),
            _ => true,
        };

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        obj is TransitionResult<TState, TOutput, TError> other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() =>
        status switch
        {
            TransitionStatus.Applied => HashCode.Combine(status, change),
            TransitionStatus.Rejected or TransitionStatus.Failed => HashCode.Combine(status, error),
            _ => HashCode.Combine(status),
        };

    /// <summary>
    /// Determines whether two transition results are equal.
    /// </summary>
    public static bool operator ==(
        TransitionResult<TState, TOutput, TError> left,
        TransitionResult<TState, TOutput, TError> right) =>
        left.Equals(right);

    /// <summary>
    /// Determines whether two transition results are unequal.
    /// </summary>
    public static bool operator !=(
        TransitionResult<TState, TOutput, TError> left,
        TransitionResult<TState, TOutput, TError> right) =>
        !left.Equals(right);

    /// <inheritdoc />
    public override string ToString() =>
        status switch
        {
            TransitionStatus.Applied => $"Applied({change})",
            TransitionStatus.Rejected => $"Rejected({error})",
            TransitionStatus.Failed => $"Failed({error})",
            _ => "Undefined",
        };
}

/// <summary>
/// Represents a pure state-machine transition for an event.
/// </summary>
/// <typeparam name="TState">The state type.</typeparam>
/// <typeparam name="TEvent">The event type.</typeparam>
/// <typeparam name="TOutput">The output type.</typeparam>
/// <typeparam name="TError">The rejection and failure error type.</typeparam>
/// <param name="state">The current state.</param>
/// <param name="event">The event to handle.</param>
/// <returns>The explicit transition result.</returns>
public delegate TransitionResult<TState, TOutput, TError> StateMachine<TState, TEvent, TOutput, TError>(
    TState state,
    TEvent @event);

/// <summary>
/// Provides composition and replay operations for <see cref="StateMachine{TState, TEvent, TOutput, TError}"/> delegates.
/// </summary>
public static class StateMachineExtensions
{
    /// <summary>
    /// Combines two state machines, invoking the fallback only when this machine is undefined.
    /// </summary>
    /// <typeparam name="TState">The state type.</typeparam>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <typeparam name="TOutput">The output type.</typeparam>
    /// <typeparam name="TError">The rejection and failure error type.</typeparam>
    /// <param name="machine">The machine to invoke first.</param>
    /// <param name="fallback">The machine to invoke after an undefined result.</param>
    /// <returns>A machine that preserves defined results and otherwise invokes <paramref name="fallback"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="machine"/> or <paramref name="fallback"/> is <see langword="null"/>.</exception>
    public static StateMachine<TState, TEvent, TOutput, TError> OrElse<TState, TEvent, TOutput, TError>(
        this StateMachine<TState, TEvent, TOutput, TError> machine,
        StateMachine<TState, TEvent, TOutput, TError> fallback)
    {
        ArgumentNullException.ThrowIfNull(machine);
        ArgumentNullException.ThrowIfNull(fallback);

        return (state, @event) =>
        {
            var result = machine(state, @event);
            return result.IsUndefined ? fallback(state, @event) : result;
        };
    }

    /// <summary>
    /// Replays a history of events through a state machine.
    /// </summary>
    /// <typeparam name="TState">The state type.</typeparam>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <typeparam name="TOutput">The output type.</typeparam>
    /// <typeparam name="TError">The rejection and failure error type.</typeparam>
    /// <param name="machine">The machine that handles each event.</param>
    /// <param name="initialState">The state before the first event.</param>
    /// <param name="events">The event history to replay.</param>
    /// <returns>An applied result containing the final state and all outputs, or the first non-applied result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="machine"/> or <paramref name="events"/> is <see langword="null"/>.</exception>
    public static TransitionResult<TState, TOutput, TError> Replay<TState, TEvent, TOutput, TError>(
        this StateMachine<TState, TEvent, TOutput, TError> machine,
        TState initialState,
        IEnumerable<TEvent> events)
    {
        ArgumentNullException.ThrowIfNull(machine);
        ArgumentNullException.ThrowIfNull(events);

        var state = initialState;
        List<TOutput>? outputs = null;

        foreach (var @event in events)
        {
            var result = machine(state, @event);
            if (!result.TryGetChange(out var change))
            {
                return result;
            }

            state = change.State;
            if (change.Outputs.Count > 0)
            {
                outputs ??= [];
                outputs.AddRange(change.Outputs);
            }
        }

        return TransitionResult<TState, TOutput, TError>.Applied(
            StateChange<TState, TOutput>.FromOwnedOutputs(state, outputs?.ToArray() ?? []));
    }
}
