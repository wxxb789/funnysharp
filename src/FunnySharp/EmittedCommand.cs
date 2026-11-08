namespace FunnySharp;

/// <summary>
/// Identifies one command emitted by a <see cref="ProgramSimulation{TModel, TMessage, TCommand,
/// TError}"/> and the explicit simulation time at which it was emitted.
/// </summary>
/// <typeparam name="TCommand">The command type.</typeparam>
/// <remarks>
/// <para>
/// A simulation assigns a monotonic <see cref="Sequence"/> starting at one in emission order.
/// The sequence is the simulation's execution-correlation identity; it is not the domain request
/// version carried inside a command. A reply or execution failure refers to the sequence, while a
/// program's stale-response decision refers to the domain version.
/// </para>
/// <para>
/// This is an execution and diagnostic value, not an outcome carrier. It carries a command
/// description only; constructing or inspecting it never executes the command.
/// </para>
/// <para>
/// A default value has sequence zero and no meaningful command. Simulations are the only
/// producers of populated values.
/// </para>
/// </remarks>
public readonly struct EmittedCommand<TCommand> : IEquatable<EmittedCommand<TCommand>>
{
    internal EmittedCommand(long sequence, TCommand command, DateTimeOffset emittedAt)
    {
        Sequence = sequence;
        Command = command;
        EmittedAt = emittedAt;
    }

    /// <summary>
    /// Gets the one-based emission sequence assigned by the simulation.
    /// </summary>
    public long Sequence { get; }

    /// <summary>
    /// Gets the emitted command value.
    /// </summary>
    public TCommand Command { get; }

    /// <summary>
    /// Gets the explicit simulation time at which the command was emitted.
    /// </summary>
    public DateTimeOffset EmittedAt { get; }

    /// <summary>
    /// Compares two emitted commands by sequence, command value, and emission time.
    /// </summary>
    /// <param name="other">The other emitted command.</param>
    /// <returns><see langword="true"/> when all three values are equal.</returns>
    public bool Equals(EmittedCommand<TCommand> other) =>
        Sequence == other.Sequence &&
        EqualityComparer<TCommand>.Default.Equals(Command, other.Command) &&
        EmittedAt == other.EmittedAt;

    /// <inheritdoc/>
    public override bool Equals(object? obj) =>
        obj is EmittedCommand<TCommand> other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Sequence, Command, EmittedAt);

    /// <summary>
    /// Renders the sequence, emission time, and command deterministically.
    /// </summary>
    /// <returns>Text such as <c>#1 at 2026-01-01T00:00:00.0000000+00:00: Charge(…)</c>.</returns>
    /// <remarks>The command's own formatting may invoke caller behavior.</remarks>
    public override string ToString() => $"#{Sequence} at {EmittedAt:O}: {Command}";
}
