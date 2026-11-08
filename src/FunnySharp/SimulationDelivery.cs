namespace FunnySharp;

/// <summary>
/// Records one message delivered to a <see cref="ProgramSimulation{TModel, TMessage, TCommand,
/// TError}"/>, its explicit simulation time, its optional command correlation, and the program's
/// transition result.
/// </summary>
/// <typeparam name="TModel">The business model type.</typeparam>
/// <typeparam name="TMessage">The delivered message type.</typeparam>
/// <typeparam name="TCommand">The command type.</typeparam>
/// <typeparam name="TError">The shared rejection and failure error type.</typeparam>
/// <remarks>
/// <para>
/// <see cref="CommandSequence"/> is <see langword="null"/> for a direct <c>Deliver</c> and the
/// emitted command's sequence for a correlated <c>Reply</c>. The delivery records the result even
/// when the program rejects, fails, or does not define the message; the model changes only for an
/// applied result.
/// </para>
/// <para>
/// This is a transcript and diagnostic value, not an outcome carrier. The canonical transition
/// outcome remains <see cref="TransitionResult{TState, TOutput, TError}"/>.
/// </para>
/// </remarks>
public readonly struct SimulationDelivery<TModel, TMessage, TCommand, TError> :
    IEquatable<SimulationDelivery<TModel, TMessage, TCommand, TError>>
{
    internal SimulationDelivery(
        TMessage message,
        DateTimeOffset at,
        long? commandSequence,
        TransitionResult<TModel, TCommand, TError> result)
    {
        Message = message;
        At = at;
        CommandSequence = commandSequence;
        Result = result;
    }

    /// <summary>
    /// Gets the delivered message.
    /// </summary>
    public TMessage Message { get; }

    /// <summary>
    /// Gets the explicit simulation time at which the message was delivered.
    /// </summary>
    public DateTimeOffset At { get; }

    /// <summary>
    /// Gets the correlated emitted-command sequence, or <see langword="null"/> for a direct message.
    /// </summary>
    public long? CommandSequence { get; }

    /// <summary>
    /// Gets the program's transition result for the delivered message.
    /// </summary>
    public TransitionResult<TModel, TCommand, TError> Result { get; }

    /// <summary>
    /// Compares two deliveries by message, time, correlation, and result.
    /// </summary>
    /// <param name="other">The other delivery.</param>
    /// <returns><see langword="true"/> when all four values are equal.</returns>
    public bool Equals(SimulationDelivery<TModel, TMessage, TCommand, TError> other) =>
        EqualityComparer<TMessage>.Default.Equals(Message, other.Message) &&
        At == other.At &&
        CommandSequence == other.CommandSequence &&
        Result.Equals(other.Result);

    /// <inheritdoc/>
    public override bool Equals(object? obj) =>
        obj is SimulationDelivery<TModel, TMessage, TCommand, TError> other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() =>
        HashCode.Combine(Message, At, CommandSequence, Result);

    /// <summary>
    /// Renders the delivery as a direct message or a correlated reply and its result.
    /// </summary>
    /// <returns>Text such as <c>Reply #1 at 2026-01-01T00:00:00.0000000+00:00: Applied(…)</c>.</returns>
    /// <remarks>The message and result formatting may invoke caller behavior.</remarks>
    public override string ToString() =>
        CommandSequence is { } sequence
            ? $"Reply #{sequence} at {At:O}: {Result}"
            : $"Message at {At:O}: {Result}";
}
