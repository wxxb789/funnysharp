using System.Buffers;

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
    /// Gets the owned output array. The array is never mutated after construction and is shared,
    /// not copied, when a composed transition reuses it.
    /// </summary>
    internal TOutput[] RawOutputs => outputs;

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
    /// <remarks>
    /// <para>
    /// Composition is an immutable, shared tree: building it with <see langword="Then"/> costs one node per call
    /// regardless of how many transitions are already composed, and evaluating it runs every transition exactly once
    /// in execution order and materializes the concatenated outputs exactly once. Nested <see langword="Then"/>
    /// calls in any association order produce the same execution order and outputs, so long chains stay linear in
    /// the number of transitions rather than copying intermediate output arrays at each composition level.
    /// </para>
    /// <para>
    /// Evaluation uses pooled scratch buffers, copies each transition's outputs once into the final output array,
    /// and never recurses, so evaluation depth is bounded by pool capacity rather than the call stack. The
    /// composition itself adds one output array materialization per evaluation; each inner transition still
    /// materializes its own <see cref="StateChange{TState, TOutput}"/> as its visible per-step result.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="first"/> or <paramref name="second"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">A transition returns <see langword="null"/>.</exception>
    public static StateTransition<TState, TOutput> Then<TState, TOutput>(
        this StateTransition<TState, TOutput> first,
        StateTransition<TState, TOutput> second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        var composition = new Composition<TState, TOutput>(first, second);
        return composition.Invoke;
    }

    /// <summary>
    /// An immutable composition tree of transitions. Leaves are plain transition delegates; inner nodes are
    /// compositions, so a chain of <see langword="Then"/> calls shares structure instead of nesting closures
    /// that re-copy outputs at every level.
    /// </summary>
    private sealed class Composition<TState, TOutput>
    {
        private readonly object left;
        private readonly object right;

        private Composition(object left, object right)
        {
            this.left = left;
            this.right = right;
        }

        public Composition(StateTransition<TState, TOutput> first, StateTransition<TState, TOutput> second)
        {
            left = AsStep(first);
            right = AsStep(second);
        }

        /// <summary>
        /// Unwraps a transition that was itself produced by <see langword="Then"/>, sharing its tree, and keeps
        /// any other delegate as a leaf.
        /// </summary>
        private static object AsStep(StateTransition<TState, TOutput> transition) =>
            transition.Target is Composition<TState, TOutput> composed ? composed : transition;

        public StateChange<TState, TOutput> Invoke(TState state)
        {
            if (left is StateTransition<TState, TOutput> firstTransition &&
                right is StateTransition<TState, TOutput> secondTransition)
            {
                return InvokeBoth(state, firstTransition, secondTransition);
            }

            return Walk(state);
        }

        /// <summary>Runs two plain transitions and concatenates their outputs in execution order.</summary>
        private static StateChange<TState, TOutput> InvokeBoth(
            TState state,
            StateTransition<TState, TOutput> first,
            StateTransition<TState, TOutput> second)
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

            var firstOutputs = firstChange.RawOutputs;
            var secondOutputs = secondChange.RawOutputs;
            if (firstOutputs.Length == 0)
            {
                return StateChange<TState, TOutput>.FromOwnedOutputs(secondChange.State, secondOutputs);
            }

            if (secondOutputs.Length == 0)
            {
                return StateChange<TState, TOutput>.FromOwnedOutputs(secondChange.State, firstOutputs);
            }

            var outputs = new TOutput[firstOutputs.Length + secondOutputs.Length];
            firstOutputs.CopyTo(outputs, 0);
            secondOutputs.CopyTo(outputs, firstOutputs.Length);
            return StateChange<TState, TOutput>.FromOwnedOutputs(secondChange.State, outputs);
        }

        /// <summary>
        /// Evaluates the composition tree iteratively, left to right, without recursion. Outputs accumulate in a
        /// pooled buffer and are materialized once into an exact-size array; each transition still runs exactly
        /// once and receives the state produced by its predecessor.
        /// </summary>
        private StateChange<TState, TOutput> Walk(TState state)
        {
            var outputPool = ArrayPool<TOutput>.Shared;
            var stepPool = ArrayPool<object>.Shared;
            var outputs = outputPool.Rent(8);
            var pending = stepPool.Rent(8);
            var outputCount = 0;
            var depth = 0;
            TOutput[] materialized;
            try
            {
                pending[depth++] = this;
                while (depth > 0)
                {
                    var step = pending[--depth];
                    while (step is Composition<TState, TOutput> composition)
                    {
                        if (depth == pending.Length)
                        {
                            pending = Grow(pending, stepPool, depth);
                        }

                        pending[depth++] = composition.right;
                        step = composition.left;
                    }

                    var change = ((StateTransition<TState, TOutput>)step)(state);
                    if (change is null)
                    {
                        throw new InvalidOperationException("A composed state transition returned null.");
                    }

                    state = change.State;
                    var changeOutputs = change.RawOutputs;
                    if (changeOutputs.Length > 0)
                    {
                        if (outputCount + changeOutputs.Length > outputs.Length)
                        {
                            outputs = Grow(outputs, outputPool, outputCount + changeOutputs.Length);
                        }

                        changeOutputs.CopyTo(outputs, outputCount);
                        outputCount += changeOutputs.Length;
                    }
                }

                materialized = outputCount == 0 ? [] : new TOutput[outputCount];
                Array.Copy(outputs, materialized, outputCount);
            }
            finally
            {
                outputPool.Return(outputs);
                stepPool.Return(pending);
            }

            return StateChange<TState, TOutput>.FromOwnedOutputs(state, materialized);
        }

        private static T[] Grow<T>(T[] buffer, ArrayPool<T> pool, int minimumLength)
        {
            var next = pool.Rent(minimumLength * 2);
            buffer.CopyTo(next, 0);
            pool.Return(buffer);
            return next;
        }
    }
}
