namespace FunnySharp;

/// <summary>
/// Records that an emitted command could not be executed by the caller's external boundary.
/// </summary>
/// <typeparam name="TCommand">The command type.</typeparam>
/// <typeparam name="TError">The shared rejection and failure error type.</typeparam>
/// <remarks>
/// <para>
/// An execution failure is not a domain transition. The program accepted a transition and emitted
/// a command; the caller's adapter failed to perform that work. The simulation records this value,
/// removes the command from its pending list, and leaves the model unchanged. The caller may then
/// deliver a domain message describing the failure if the workflow has one.
/// </para>
/// <para>
/// This is deliberately distinct from <see cref="TransitionStatus.Failed"/>, which means a
/// defined domain transition could not complete, and from
/// <see cref="TransitionStatus.Rejected"/>, which means the program refused the message.
/// </para>
/// <para>
/// This is an execution and diagnostic value, not an outcome carrier.
/// </para>
/// </remarks>
public readonly struct CommandExecutionFailure<TCommand, TError> :
    IEquatable<CommandExecutionFailure<TCommand, TError>>
{
    internal CommandExecutionFailure(
        EmittedCommand<TCommand> command,
        TError error,
        DateTimeOffset at)
    {
        Command = command;
        Error = error;
        At = at;
    }

    /// <summary>
    /// Gets the emitted command whose execution failed.
    /// </summary>
    public EmittedCommand<TCommand> Command { get; }

    /// <summary>
    /// Gets the typed execution error supplied by the caller's boundary.
    /// </summary>
    public TError Error { get; }

    /// <summary>
    /// Gets the explicit simulation time at which the failure was recorded.
    /// </summary>
    public DateTimeOffset At { get; }

    /// <summary>
    /// Compares two execution failures by command, error, and time.
    /// </summary>
    /// <param name="other">The other execution failure.</param>
    /// <returns><see langword="true"/> when all three values are equal.</returns>
    public bool Equals(CommandExecutionFailure<TCommand, TError> other) =>
        Command.Equals(other.Command) &&
        EqualityComparer<TError>.Default.Equals(Error, other.Error) &&
        At == other.At;

    /// <inheritdoc/>
    public override bool Equals(object? obj) =>
        obj is CommandExecutionFailure<TCommand, TError> other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Command, Error, At);

    /// <summary>
    /// Renders the failed command sequence, time, and typed error deterministically.
    /// </summary>
    /// <returns>Text such as <c>Command #1 failed at 2026-01-01T00:00:00.0000000+00:00: …</c>.</returns>
    /// <remarks>The error formatting may invoke caller behavior.</remarks>
    public override string ToString() =>
        $"Command #{Command.Sequence} failed at {At:O}: {Error}";
}
