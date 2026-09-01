namespace FunnySharp;

/// <summary>
/// Represents the next state and outputs produced by a state transition.
/// </summary>
/// <typeparam name="TState">The state type.</typeparam>
/// <typeparam name="TOutput">The output type.</typeparam>
public sealed class StateChange<TState, TOutput> : IEquatable<StateChange<TState, TOutput>>
{
    private readonly TOutput[] outputs;

    private StateChange(TState state, TOutput[] outputs)
    {
        State = state;
        this.outputs = outputs;
        Outputs = Array.AsReadOnly(outputs);
    }

    /// <summary>
    /// Gets the state produced by the transition.
    /// </summary>
    public TState State { get; }

    /// <summary>
    /// Gets the outputs produced by the transition.
    /// </summary>
    public IReadOnlyList<TOutput> Outputs { get; }

    /// <summary>
    /// Creates a state change with a snapshot of the supplied outputs.
    /// </summary>
    /// <param name="state">The state produced by the transition.</param>
    /// <param name="outputs">The outputs produced by the transition. Individual outputs may be <see langword="null"/>.</param>
    /// <returns>A state change containing <paramref name="state"/> and a snapshot of <paramref name="outputs"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="outputs"/> is <see langword="null"/>.</exception>
    public static StateChange<TState, TOutput> To(TState state, params TOutput[] outputs)
    {
        ArgumentNullException.ThrowIfNull(outputs);
        return FromOwnedOutputs(state, (TOutput[])outputs.Clone());
    }

    internal static StateChange<TState, TOutput> FromOwnedOutputs(TState state, TOutput[] outputs) =>
        new(state, outputs);

    /// <summary>
    /// Determines whether this state change and <paramref name="other"/> have equal states and outputs.
    /// </summary>
    /// <param name="other">The state change to compare with this instance.</param>
    /// <returns><see langword="true"/> when both state changes have structurally equal values; otherwise, <see langword="false"/>.</returns>
    public bool Equals(StateChange<TState, TOutput>? other)
    {
        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (other is null ||
            !EqualityComparer<TState>.Default.Equals(State, other.State) ||
            outputs.Length != other.outputs.Length)
        {
            return false;
        }

        for (var index = 0; index < outputs.Length; index++)
        {
            if (!EqualityComparer<TOutput>.Default.Equals(outputs[index], other.outputs[index]))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Determines whether the specified object is structurally equal to this state change.
    /// </summary>
    /// <param name="obj">The object to compare with this instance.</param>
    /// <returns><see langword="true"/> when <paramref name="obj"/> is an equal state change; otherwise, <see langword="false"/>.</returns>
    public override bool Equals(object? obj) => Equals(obj as StateChange<TState, TOutput>);

    /// <summary>
    /// Returns a hash code based on the state and outputs.
    /// </summary>
    /// <returns>A hash code for this state change.</returns>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(State);

        foreach (var output in outputs)
        {
            hash.Add(output);
        }

        return hash.ToHashCode();
    }

    /// <summary>
    /// Returns a string representation of this state change.
    /// </summary>
    /// <returns>A string representation of the state and outputs.</returns>
    public override string ToString() => $"StateChange({State}, [{string.Join(", ", outputs)}])";
}

/// <summary>
/// Represents a total, pure transformation from a state to its next state and outputs.
/// </summary>
/// <typeparam name="TState">The state type.</typeparam>
/// <typeparam name="TOutput">The output type.</typeparam>
/// <param name="state">The current state.</param>
/// <returns>The next state and outputs.</returns>
public delegate StateChange<TState, TOutput> StateTransition<TState, TOutput>(TState state);

/// <summary>
/// Provides composition operations for <see cref="StateTransition{TState, TOutput}"/> delegates.
/// </summary>
public static class StateTransitionExtensions
{
    /// <summary>
    /// Composes two transitions so that the second receives the state produced by the first.
    /// </summary>
    /// <typeparam name="TState">The state type.</typeparam>
    /// <typeparam name="TOutput">The output type.</typeparam>
    /// <param name="first">The transition to execute first.</param>
    /// <param name="second">The transition to execute after <paramref name="first"/>.</param>
    /// <returns>A transition that runs both transitions and concatenates their outputs in execution order.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="first"/> or <paramref name="second"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">A transition returns <see langword="null"/>.</exception>
    public static StateTransition<TState, TOutput> Then<TState, TOutput>(
        this StateTransition<TState, TOutput> first,
        StateTransition<TState, TOutput> second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        return state =>
        {
            var firstChange = first(state);
            if (firstChange is null)
            {
                throw new InvalidOperationException("The first state transition returned null.");
            }

            var secondChange = second(firstChange.State);
            if (secondChange is null)
            {
                throw new InvalidOperationException("The second state transition returned null.");
            }

            var firstOutputCount = firstChange.Outputs.Count;
            var outputs = new TOutput[firstOutputCount + secondChange.Outputs.Count];
            for (var index = 0; index < firstOutputCount; index++)
            {
                outputs[index] = firstChange.Outputs[index];
            }

            for (var index = 0; index < secondChange.Outputs.Count; index++)
            {
                outputs[firstOutputCount + index] = secondChange.Outputs[index];
            }

            return StateChange<TState, TOutput>.FromOwnedOutputs(secondChange.State, outputs);
        };
    }
}
