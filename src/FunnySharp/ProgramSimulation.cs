namespace FunnySharp;

/// <summary>
/// Drives a <see cref="DomainProgram{TModel, TMessage, TCommand, TError}"/> through an explicit,
/// single-threaded deterministic scenario and records the messages, emitted commands, deliveries,
/// and command execution failures.
/// </summary>
/// <typeparam name="TModel">The business model type.</typeparam>
/// <typeparam name="TMessage">The input message type.</typeparam>
/// <typeparam name="TCommand">The output command type.</typeparam>
/// <typeparam name="TError">The shared rejection and failure error type.</typeparam>
/// <remarks>
/// <para>
/// The simulation is a test and inspection driver, not a runtime. It never executes a command,
/// never reads an ambient clock or random source, and never schedules, retries, or dispatches
/// work. A caller executes emitted commands with ordinary .NET code and delivers the resulting
/// reply or records the execution failure explicitly.
/// </para>
/// <para>
/// Time is an explicit <see cref="DateTimeOffset"/> value. <see cref="AdvanceTo"/> and
/// <see cref="AdvanceBy"/> move it forward; every delivery, emitted command, and execution failure
/// records the time in effect. The program sees time only through its model and messages, so
/// replay does not need the simulation clock. An <c>...At</c> overload validates its arguments
/// before moving the clock, and restores the previous time if the program's update throws, so a
/// failed call leaves the model and every transcript unchanged.
/// </para>
/// <para>
/// Randomness is an optional caller-supplied <see cref="System.Random"/>. The simulation never
/// consumes it; a scenario uses it to choose external replies deterministically. Passing a seeded
/// instance makes the scenario reproducible.
/// </para>
/// <para>
/// The simulation is not thread-safe. Its transcript lists are live views owned by the simulation;
/// do not mutate or enumerate them while an operation is adding to them. The transcript retains
/// every delivery and emitted command, so it is not a bounded production event store.
/// </para>
/// </remarks>
public sealed class ProgramSimulation<TModel, TMessage, TCommand, TError>
{
    private readonly StateMachine<TModel, TMessage, TCommand, TError> update;
    private readonly TModel initialModel;
    private readonly Random? random;
    private readonly List<EmittedCommand<TCommand>> emittedCommands = [];
    private readonly List<EmittedCommand<TCommand>> pendingCommands = [];
    private readonly List<SimulationDelivery<TModel, TMessage, TCommand, TError>> deliveries = [];
    private readonly List<CommandExecutionFailure<TCommand, TError>> commandFailures = [];
    private TModel model;
    private DateTimeOffset now;
    private long nextCommandSequence = 1;

    /// <summary>
    /// Starts a deterministic simulation.
    /// </summary>
    /// <param name="program">The pure program to drive.</param>
    /// <param name="initialModel">The model before the first message.</param>
    /// <param name="startedAt">The explicit initial simulation time.</param>
    /// <param name="random">
    /// An optional caller-supplied deterministic random source for the scenario script. The
    /// simulation never consumes it; it exists so a scenario can choose external replies
    /// reproducibly. <see langword="null"/> means the scenario has no random source.
    /// </param>
    /// <exception cref="InvalidOperationException"><paramref name="program"/> is uninitialized.</exception>
    public ProgramSimulation(
        DomainProgram<TModel, TMessage, TCommand, TError> program,
        TModel initialModel,
        DateTimeOffset startedAt,
        Random? random = null)
    {
        update = program.AsStateMachine();
        this.initialModel = initialModel;
        model = initialModel;
        now = startedAt;
        this.random = random;
    }

    /// <summary>
    /// Gets the current model. It changes only when a delivered message is applied.
    /// </summary>
    public TModel Model => model;

    /// <summary>
    /// Gets the explicit simulation time used for the next operation.
    /// </summary>
    public DateTimeOffset Now => now;

    /// <summary>
    /// Gets the caller-supplied random source, or <see langword="null"/> when the simulation has none.
    /// </summary>
    /// <remarks>The simulation never consumes this source; it exists for the scenario script.</remarks>
    public Random? Random => random;

    /// <summary>
    /// Gets every emitted command in monotonic emission order.
    /// </summary>
    /// <remarks>The list is append-only and is a live view owned by the simulation.</remarks>
    public IReadOnlyList<EmittedCommand<TCommand>> EmittedCommands => emittedCommands;

    /// <summary>
    /// Gets the commands that have not yet been replied to or failed, in emission order.
    /// </summary>
    /// <remarks>
    /// A reply or failure removes its sequence from this list. New commands emitted by a reply are
    /// appended after the commands already pending, so the list is a FIFO emission queue. The list
    /// is a live view owned by the simulation; read it again after an operation rather than holding
    /// an enumerator across one.
    /// </remarks>
    public IReadOnlyList<EmittedCommand<TCommand>> PendingCommands => pendingCommands;

    /// <summary>
    /// Gets every message delivery in operation order.
    /// </summary>
    public IReadOnlyList<SimulationDelivery<TModel, TMessage, TCommand, TError>> Deliveries =>
        deliveries;

    /// <summary>
    /// Gets every recorded command execution failure in operation order.
    /// </summary>
    public IReadOnlyList<CommandExecutionFailure<TCommand, TError>> CommandFailures =>
        commandFailures;

    /// <summary>
    /// Moves the explicit simulation time to a later or equal instant.
    /// </summary>
    /// <param name="at">The new simulation time.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="at"/> is earlier than
    /// <see cref="Now"/>.</exception>
    public void AdvanceTo(DateTimeOffset at)
    {
        ValidateAt(at);
        now = at;
    }

    /// <summary>
    /// Moves the explicit simulation time forward by a non-negative interval.
    /// </summary>
    /// <param name="by">The interval to add.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="by"/> is negative.</exception>
    public void AdvanceBy(TimeSpan by)
    {
        if (by < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(by),
                by,
                "Simulation time cannot move backwards.");
        }

        now = now.Add(by);
    }

    /// <summary>
    /// Delivers one message to the program at the current simulation time.
    /// </summary>
    /// <param name="message">The message to deliver.</param>
    /// <returns>The program's transition result. The delivery is also recorded.</returns>
    /// <remarks>
    /// The message is never deduplicated. The program decides whether it is applied, rejected,
    /// failed, or undefined.
    /// </remarks>
    public TransitionResult<TModel, TCommand, TError> Deliver(TMessage message) =>
        DeliverCore(message);

    /// <summary>
    /// Advances the simulation time and delivers one message.
    /// </summary>
    /// <param name="message">The message to deliver.</param>
    /// <param name="at">The explicit delivery time; it must not be earlier than <see cref="Now"/>.</param>
    /// <returns>The program's transition result. The delivery is also recorded.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="at"/> is earlier than
    /// <see cref="Now"/>.</exception>
    /// <remarks>
    /// The clock moves before the update runs. If the update throws, the previous time is restored
    /// and no delivery is recorded.
    /// </remarks>
    public TransitionResult<TModel, TCommand, TError> Deliver(TMessage message, DateTimeOffset at)
    {
        ValidateAt(at);
        var previous = now;
        now = at;
        try
        {
            return DeliverCore(message);
        }
        catch
        {
            now = previous;
            throw;
        }
    }

    /// <summary>
    /// Delivers an external reply correlated to an emitted command at the current simulation time.
    /// </summary>
    /// <param name="commandSequence">The emitted command's sequence.</param>
    /// <param name="message">The reply message to deliver.</param>
    /// <returns>The program's transition result. The delivery is recorded with the correlation.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="commandSequence"/> was not
    /// emitted by this simulation.</exception>
    /// <remarks>
    /// A reply to a command that was already replied to or failed is allowed and is the
    /// duplicate/late-response path; the program decides whether the reply is stale, duplicate,
    /// unexpected, rejected, or applied. The simulation never deduplicates replies.
    /// </remarks>
    public TransitionResult<TModel, TCommand, TError> Reply(
        long commandSequence,
        TMessage message) =>
        ReplyCore(commandSequence, message);

    /// <summary>
    /// Advances the simulation time and delivers an external reply correlated to an emitted command.
    /// </summary>
    /// <param name="commandSequence">The emitted command's sequence.</param>
    /// <param name="message">The reply message to deliver.</param>
    /// <param name="at">The explicit delivery time; it must not be earlier than <see cref="Now"/>.</param>
    /// <returns>The program's transition result. The delivery is recorded with the correlation.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="commandSequence"/> was not
    /// emitted by this simulation, or <paramref name="at"/> is earlier than <see cref="Now"/>.</exception>
    /// <remarks>
    /// The time and the emitted sequence are validated before the clock moves; the pending state is
    /// not checked because duplicate and late replies are allowed. If the update throws, the
    /// previous time is restored and no delivery is recorded.
    /// </remarks>
    public TransitionResult<TModel, TCommand, TError> Reply(
        long commandSequence,
        TMessage message,
        DateTimeOffset at)
    {
        ValidateAt(at);
        _ = GetEmittedCommand(commandSequence);
        var previous = now;
        now = at;
        try
        {
            return ReplyCore(commandSequence, message);
        }
        catch
        {
            now = previous;
            throw;
        }
    }

    /// <summary>
    /// Records that executing an emitted command failed at the current simulation time.
    /// </summary>
    /// <param name="commandSequence">The emitted command's sequence.</param>
    /// <param name="error">The typed execution error supplied by the caller's boundary.</param>
    /// <returns>The recorded execution failure.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="commandSequence"/> was not
    /// emitted by this simulation.</exception>
    /// <exception cref="InvalidOperationException">The command was already replied to or failed.</exception>
    /// <remarks>
    /// The model is unchanged and no delivery is recorded. This is an execution failure, not a
    /// domain transition; the caller may deliver a domain message describing the failure.
    /// </remarks>
    public CommandExecutionFailure<TCommand, TError> FailCommand(
        long commandSequence,
        TError error) =>
        FailCommandCore(commandSequence, error);

    /// <summary>
    /// Advances the simulation time and records that executing an emitted command failed.
    /// </summary>
    /// <param name="commandSequence">The emitted command's sequence.</param>
    /// <param name="error">The typed execution error supplied by the caller's boundary.</param>
    /// <param name="at">The explicit failure time; it must not be earlier than <see cref="Now"/>.</param>
    /// <returns>The recorded execution failure.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="commandSequence"/> was not
    /// emitted by this simulation, or <paramref name="at"/> is earlier than <see cref="Now"/>.</exception>
    /// <exception cref="InvalidOperationException">The command was already replied to or failed.</exception>
    /// <remarks>
    /// The time, the emitted sequence, and the pending state are validated before the clock moves;
    /// a rejected call leaves <see cref="Now"/> unchanged.
    /// </remarks>
    public CommandExecutionFailure<TCommand, TError> FailCommand(
        long commandSequence,
        TError error,
        DateTimeOffset at)
    {
        ValidateAt(at);
        _ = GetEmittedCommand(commandSequence);
        RequirePending(commandSequence);
        now = at;
        return FailCommandCore(commandSequence, error);
    }

    /// <summary>
    /// Replays the recorded message deliveries through the pure program from the initial model.
    /// </summary>
    /// <returns>
    /// An applied result containing the replayed final model and all emitted commands in order, or
    /// the first non-applied result with its status and error.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This is the existing <see cref="StateMachineExtensions.Replay"/> semantics: the recorded
    /// messages are enumerated once in delivery order, an empty transcript succeeds with the
    /// initial model and no outputs, and the first rejected, failed, or undefined delivery stops
    /// the replay. The simulation transcript remains the record of any later steps.
    /// </para>
    /// <para>
    /// Replay invokes only the pure update. It never executes, retries, or re-records commands or
    /// execution failures. Emitted commands are outputs, not replay inputs, so mutating an emitted
    /// command after the simulation cannot change the replayed state or command shape. The
    /// recorded messages and the initial model are retained by reference; mutating them can change
    /// a later replay, so callers rely on value-immutable messages and models for reproducibility.
    /// </para>
    /// </remarks>
    public TransitionResult<TModel, TCommand, TError> Replay() =>
        update.Replay(initialModel, deliveries.Select(static delivery => delivery.Message));

    private TransitionResult<TModel, TCommand, TError> DeliverCore(TMessage message)
    {
        var result = update(model, message);
        deliveries.Add(new SimulationDelivery<TModel, TMessage, TCommand, TError>(
            message,
            now,
            null,
            result));

        if (result.TryGetChange(out var change))
        {
            model = change.State;
            EmitCommands(change);
        }

        return result;
    }

    private TransitionResult<TModel, TCommand, TError> ReplyCore(
        long commandSequence,
        TMessage message)
    {
        _ = GetEmittedCommand(commandSequence);

        var result = update(model, message);
        deliveries.Add(new SimulationDelivery<TModel, TMessage, TCommand, TError>(
            message,
            now,
            commandSequence,
            result));

        if (result.TryGetChange(out var change))
        {
            model = change.State;
            EmitCommands(change);
        }

        _ = RemovePending(commandSequence);
        return result;
    }

    private CommandExecutionFailure<TCommand, TError> FailCommandCore(
        long commandSequence,
        TError error)
    {
        var command = GetEmittedCommand(commandSequence);
        if (!RemovePending(commandSequence))
        {
            throw new InvalidOperationException(
                $"Command sequence {commandSequence} has already been completed or failed.");
        }

        var failure = new CommandExecutionFailure<TCommand, TError>(command, error, now);
        commandFailures.Add(failure);
        return failure;
    }

    private void ValidateAt(DateTimeOffset at)
    {
        if (at < now)
        {
            throw new ArgumentOutOfRangeException(
                nameof(at),
                at,
                "Simulation time cannot move backwards.");
        }
    }

    private void RequirePending(long commandSequence)
    {
        for (var index = 0; index < pendingCommands.Count; index++)
        {
            if (pendingCommands[index].Sequence == commandSequence)
            {
                return;
            }
        }

        throw new InvalidOperationException(
            $"Command sequence {commandSequence} has already been completed or failed.");
    }

    private EmittedCommand<TCommand> GetEmittedCommand(long commandSequence)
    {
        if (commandSequence < 1 || commandSequence > emittedCommands.Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(commandSequence),
                commandSequence,
                "The command sequence was not emitted by this simulation.");
        }

        return emittedCommands[(int)(commandSequence - 1)];
    }

    private void EmitCommands(StateChange<TModel, TCommand> change)
    {
        var outputs = change.RawOutputs;
        for (var index = 0; index < outputs.Length; index++)
        {
            var emitted = new EmittedCommand<TCommand>(
                nextCommandSequence++,
                outputs[index],
                now);
            emittedCommands.Add(emitted);
            pendingCommands.Add(emitted);
        }
    }

    private bool RemovePending(long commandSequence)
    {
        var index = pendingCommands.FindIndex(
            command => command.Sequence == commandSequence);
        if (index < 0)
        {
            return false;
        }

        pendingCommands.RemoveAt(index);
        return true;
    }
}
