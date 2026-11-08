namespace FunnySharp;

/// <summary>
/// Creates <see cref="DomainProgram{TModel, TMessage, TCommand, TError}"/> descriptions.
/// </summary>
/// <remarks>
/// <para>
/// A domain program is a pure business-program description over the existing
/// <see cref="StateMachine{TState, TEvent, TOutput, TError}"/> delegate. It names the model,
/// message, command, and error roles without adding an outcome carrier or a runtime: the update
/// computes a next model and descriptions of work, and the caller owns execution, persistence,
/// time, and external replies.
/// </para>
/// <para>
/// The default <see cref="DomainProgram{TModel, TMessage, TCommand, TError}"/> value is
/// uninitialized. Create a program through this factory; every operation on a default value fails
/// with an explicit <see cref="InvalidOperationException"/>.
/// </para>
/// </remarks>
public static class DomainProgram
{
    /// <summary>
    /// Creates a pure domain program from a state-machine update.
    /// </summary>
    /// <typeparam name="TModel">The business model type.</typeparam>
    /// <typeparam name="TMessage">The input message type.</typeparam>
    /// <typeparam name="TCommand">The output command type.</typeparam>
    /// <typeparam name="TError">The shared rejection and failure error type.</typeparam>
    /// <param name="update">The pure update; it is validated and invoked, never executed as an effect.</param>
    /// <returns>A reusable program description wrapping <paramref name="update"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="update"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// Creating or composing a program does not invoke the update or any command. The same
    /// delegate may be wrapped in more than one program and may be invoked concurrently when the
    /// caller's model and callbacks are safe for concurrent use.
    /// </remarks>
    public static DomainProgram<TModel, TMessage, TCommand, TError> Create<TModel, TMessage, TCommand, TError>(
        StateMachine<TModel, TMessage, TCommand, TError> update)
    {
        ArgumentNullException.ThrowIfNull(update);
        return DomainProgram<TModel, TMessage, TCommand, TError>.FromUpdate(update);
    }
}

/// <summary>
/// Represents a pure business program: a model and message produce a next model and output
/// commands, with an explicit applied, rejected, failed, or undefined result.
/// </summary>
/// <typeparam name="TModel">The business model type.</typeparam>
/// <typeparam name="TMessage">The input message type.</typeparam>
/// <typeparam name="TCommand">The output command type.</typeparam>
/// <typeparam name="TError">The shared rejection and failure error type.</typeparam>
/// <remarks>
/// <para>
/// A program is a description, not a runtime. <see cref="Update"/> is pure from the library's
/// perspective: it invokes the supplied update once, does not mutate the model, and returns the
/// update's <see cref="TransitionResult{TState, TOutput, TError}"/>. Outputs are command values;
/// constructing or inspecting them does not execute work. The caller owns persistence, external
/// I/O, time, randomness, and command execution.
/// </para>
/// <para>
/// <see cref="OrElse"/> and <see cref="Replay"/> preserve the existing
/// <see cref="StateMachine{TState, TEvent, TOutput, TError}"/> meanings. <see cref="Lift{TParentModel,
/// TParentMessage, TParentCommand}"/> embeds a child program into a parent model: the parent
/// message is routed through an <see cref="Option{T}"/>, the child update sees only its substate,
/// and child commands are lifted into parent commands in order.
/// </para>
/// <para>
/// The default value is uninitialized. Use <see cref="DomainProgram.Create{TModel, TMessage,
/// TCommand, TError}"/>; <see cref="Update"/>, <see cref="AsStateMachine"/>, <see cref="OrElse"/>,
/// <see cref="Replay"/>, and <see cref="Lift{TParentModel, TParentMessage, TParentCommand}"/>
/// fail explicitly on a default value.
/// </para>
/// </remarks>
public readonly struct DomainProgram<TModel, TMessage, TCommand, TError>
{
    private readonly StateMachine<TModel, TMessage, TCommand, TError>? update;

    private DomainProgram(StateMachine<TModel, TMessage, TCommand, TError> update) =>
        this.update = update;

    internal static DomainProgram<TModel, TMessage, TCommand, TError> FromUpdate(
        StateMachine<TModel, TMessage, TCommand, TError> update) =>
        new(update);

    /// <summary>
    /// Runs the pure program once for a model and message.
    /// </summary>
    /// <param name="model">The current model. It is not mutated by the program.</param>
    /// <param name="message">The message to handle.</param>
    /// <returns>
    /// The update's applied state change, rejection, failure, or undefined result. The result is
    /// returned unchanged; the program does not execute its outputs.
    /// </returns>
    /// <exception cref="InvalidOperationException">This program is uninitialized.</exception>
    /// <remarks>
    /// The update is invoked exactly once. Its exceptions propagate unchanged; the library does
    /// not convert them into domain results.
    /// </remarks>
    public TransitionResult<TModel, TCommand, TError> Update(TModel model, TMessage message) =>
        AsStateMachine()(model, message);

    /// <summary>
    /// Exposes the underlying pure state-machine delegate.
    /// </summary>
    /// <returns>The exact update delegate supplied to <see cref="DomainProgram.Create{TModel,
    /// TMessage, TCommand, TError}"/>.</returns>
    /// <exception cref="InvalidOperationException">This program is uninitialized.</exception>
    /// <remarks>
    /// The delegate can be passed to the existing <see cref="StateMachineExtensions.OrElse"/> and
    /// <see cref="StateMachineExtensions.Replay"/> operations without wrapping or copying it.
    /// </remarks>
    public StateMachine<TModel, TMessage, TCommand, TError> AsStateMachine() =>
        update ?? throw new InvalidOperationException("The domain program has not been initialized.");

    /// <summary>
    /// Combines this program with a fallback that runs only when this program is undefined.
    /// </summary>
    /// <param name="fallback">The program to invoke after an undefined result.</param>
    /// <returns>
    /// A program that preserves applied, rejected, and failed results and otherwise invokes
    /// <paramref name="fallback"/> with the same model and message.
    /// </returns>
    /// <exception cref="InvalidOperationException">This program or <paramref name="fallback"/> is
    /// uninitialized.</exception>
    /// <remarks>
    /// This operation delegates to <see cref="StateMachineExtensions.OrElse"/>; a rejected or
    /// failed result is a defined answer and never falls through.
    /// </remarks>
    public DomainProgram<TModel, TMessage, TCommand, TError> OrElse(
        DomainProgram<TModel, TMessage, TCommand, TError> fallback)
    {
        var first = AsStateMachine();
        var second = fallback.AsStateMachine();
        return DomainProgram.Create(first.OrElse(second));
    }

    /// <summary>
    /// Replays a message history through this program in source order.
    /// </summary>
    /// <param name="initialModel">The model before the first message.</param>
    /// <param name="messages">The message history to replay.</param>
    /// <returns>
    /// An applied result containing the final model and all emitted commands in order, or the
    /// first non-applied result with its status and error.
    /// </returns>
    /// <exception cref="InvalidOperationException">This program is uninitialized.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="messages"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// <para>
    /// This operation delegates to <see cref="StateMachineExtensions.Replay"/>: the history is
    /// enumerated once, an empty history succeeds with the initial model and no outputs, and the
    /// first <see cref="TransitionStatus.Rejected"/>, <see cref="TransitionStatus.Failed"/>, or
    /// <see cref="TransitionStatus.Undefined"/> result stops the replay and is returned unchanged.
    /// </para>
    /// <para>
    /// Replay invokes only the pure update. It never executes, retries, records, or otherwise
    /// touches an emitted command, and it never reads external state, time, or randomness.
    /// </para>
    /// <para>
    /// The initial model and the message values are retained by reference; they are not deep-copied.
    /// Mutating a history message or the initial model can therefore change a later replay, so
    /// reproducibility relies on value-immutable models and messages.
    /// </para>
    /// </remarks>
    public TransitionResult<TModel, TCommand, TError> Replay(
        TModel initialModel,
        IEnumerable<TMessage> messages) =>
        AsStateMachine().Replay(initialModel, messages);

    /// <summary>
    /// Embeds this child program into a parent model.
    /// </summary>
    /// <typeparam name="TParentModel">The parent model type.</typeparam>
    /// <typeparam name="TParentMessage">The parent message type.</typeparam>
    /// <typeparam name="TParentCommand">The parent command type.</typeparam>
    /// <param name="getChild">Reads the child substate from a parent model.</param>
    /// <param name="setChild">Replaces the child substate in a parent model.</param>
    /// <param name="route">
    /// Maps a parent message to the child message it owns, or <see cref="Option{T}.None"/> when
    /// this child does not own the message.
    /// </param>
    /// <param name="liftCommand">
    /// Maps one child command to one parent command. It receives the parent model before the
    /// child's next state is applied, so it can add aggregate identity or other parent context.
    /// </param>
    /// <returns>A parent program that routes, updates, and lifts commands for this child.</returns>
    /// <exception cref="ArgumentNullException">Any delegate is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">This child program is uninitialized.</exception>
    /// <remarks>
    /// <para>
    /// The route runs once per parent message. <see cref="Option{T}.None"/> returns
    /// <see cref="TransitionStatus.Undefined"/> without invoking the getter, the child update, the
    /// command lifter, or the setter. When the route is present, the getter and child update each
    /// run once. An applied child change runs <paramref name="liftCommand"/> once per child output
    /// in child output order with the parent model before the update, then <paramref name="setChild"/>
    /// once with the child's next state.
    /// A rejected, failed, or undefined child result passes through unchanged and invokes neither
    /// the command lifter nor the setter.
    /// </para>
    /// <para>
    /// The parent setter is the only parent write; the library does not copy the parent itself.
    /// The child update receives the substate reference from the getter and must not mutate it.
    /// The child and parent programs share <typeparamref name="TError"/>.
    /// </para>
    /// <para>
    /// The same child program can be lifted into different parents with different getters,
    /// setters, routes, and command lifters; its update behavior is unchanged.
    /// </para>
    /// </remarks>
    public DomainProgram<TParentModel, TParentMessage, TParentCommand, TError> Lift<TParentModel, TParentMessage, TParentCommand>(
        Func<TParentModel, TModel> getChild,
        Func<TParentModel, TModel, TParentModel> setChild,
        Func<TParentMessage, Option<TMessage>> route,
        Func<TParentModel, TCommand, TParentCommand> liftCommand)
    {
        ArgumentNullException.ThrowIfNull(getChild);
        ArgumentNullException.ThrowIfNull(setChild);
        ArgumentNullException.ThrowIfNull(route);
        ArgumentNullException.ThrowIfNull(liftCommand);

        var child = AsStateMachine();
        return DomainProgram.Create<TParentModel, TParentMessage, TParentCommand, TError>(
            (parent, message) =>
            {
                if (!route(message).TryGetValue(out var childMessage))
                {
                    return TransitionResult<TParentModel, TParentCommand, TError>.Undefined();
                }

                var childResult = child(getChild(parent), childMessage);
                if (childResult.TryGetChange(out var change))
                {
                    return LiftChange(parent, change, setChild, liftCommand);
                }

                if (childResult.TryGetError(out var error))
                {
                    return childResult.IsRejected
                        ? TransitionResult<TParentModel, TParentCommand, TError>.Rejected(error)
                        : TransitionResult<TParentModel, TParentCommand, TError>.Failed(error);
                }

                return TransitionResult<TParentModel, TParentCommand, TError>.Undefined();
            });
    }

    private static TransitionResult<TParentModel, TParentCommand, TError> LiftChange<TParentModel, TParentCommand>(
        TParentModel parent,
        StateChange<TModel, TCommand> change,
        Func<TParentModel, TModel, TParentModel> setChild,
        Func<TParentModel, TCommand, TParentCommand> liftCommand)
    {
        var outputs = change.RawOutputs;
        TParentCommand[] parentCommands;

        if (outputs.Length == 0)
        {
            parentCommands = Array.Empty<TParentCommand>();
        }
        else
        {
            parentCommands = new TParentCommand[outputs.Length];
            for (var index = 0; index < outputs.Length; index++)
            {
                parentCommands[index] = liftCommand(parent, outputs[index]);
            }
        }

        var nextParent = setChild(parent, change.State);
        return TransitionResult<TParentModel, TParentCommand, TError>.Applied(
            StateChange<TParentModel, TParentCommand>.FromOwnedOutputs(nextParent, parentCommands));
    }
}
