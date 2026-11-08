using FunnySharp;

namespace FunnySharp.TestSupport.ProgramComparison;

internal sealed class FunnySharpOrderSimulation : IOrderSimulation
{
    private readonly ProgramSimulation<OrderModel, OrderMessage, OrderCommand, OrderError> simulation;
    private readonly List<EmittedOrderCommand> emitted = [];
    private readonly List<OrderDelivery> deliveries = [];
    private readonly List<OrderFailure> failures = [];

    internal FunnySharpOrderSimulation(
        DomainProgram<OrderModel, OrderMessage, OrderCommand, OrderError> program,
        OrderModel initialModel,
        DateTimeOffset startedAt,
        Random? random = null)
    {
        simulation = new ProgramSimulation<OrderModel, OrderMessage, OrderCommand, OrderError>(
            program,
            initialModel,
            startedAt,
            random);
    }

    public OrderModel Model => simulation.Model;

    public DateTimeOffset Now => simulation.Now;

    public Random? Random => simulation.Random;

    public int PendingCount => simulation.PendingCommands.Count;

    public IReadOnlyList<EmittedOrderCommand> EmittedCommands
    {
        get
        {
            Sync();
            return emitted;
        }
    }

    public IReadOnlyList<OrderDelivery> Deliveries
    {
        get
        {
            Sync();
            return deliveries;
        }
    }

    public IReadOnlyList<OrderFailure> CommandFailures
    {
        get
        {
            Sync();
            return failures;
        }
    }

    public long PeekPendingSequence() => simulation.PendingCommands[0].Sequence;

    public long PeekPendingSequence(int index) => simulation.PendingCommands[index].Sequence;

    public OrderCommand PeekPendingCommand() => simulation.PendingCommands[0].Command;

    public OrderCommand PeekPendingCommand(int index) => simulation.PendingCommands[index].Command;

    public void AdvanceTo(DateTimeOffset at) => simulation.AdvanceTo(at);

    public void AdvanceBy(TimeSpan by) => simulation.AdvanceBy(by);

    public TransitionResult<OrderModel, OrderCommand, OrderError> Deliver(
        OrderMessage message) =>
        simulation.Deliver(message);

    public TransitionResult<OrderModel, OrderCommand, OrderError> Deliver(
        OrderMessage message,
        DateTimeOffset at) =>
        simulation.Deliver(message, at);

    public TransitionResult<OrderModel, OrderCommand, OrderError> Reply(
        long commandSequence,
        OrderMessage message) =>
        simulation.Reply(commandSequence, message);

    public TransitionResult<OrderModel, OrderCommand, OrderError> Reply(
        long commandSequence,
        OrderMessage message,
        DateTimeOffset at) =>
        simulation.Reply(commandSequence, message, at);

    public OrderFailure FailCommand(long commandSequence, OrderError error)
    {
        var failure = simulation.FailCommand(commandSequence, error);
        Sync();
        return new OrderFailure(failure.Command.Sequence, failure.Error, failure.At);
    }

    public TransitionResult<OrderModel, OrderCommand, OrderError> Replay() =>
        simulation.Replay();

    private void Sync()
    {
        while (emitted.Count < simulation.EmittedCommands.Count)
        {
            var command = simulation.EmittedCommands[emitted.Count];
            emitted.Add(new EmittedOrderCommand(
                command.Sequence,
                command.Command,
                command.EmittedAt));
        }

        while (deliveries.Count < simulation.Deliveries.Count)
        {
            var delivery = simulation.Deliveries[deliveries.Count];
            deliveries.Add(new OrderDelivery(
                delivery.Message,
                delivery.At,
                delivery.CommandSequence,
                delivery.Result.Status,
                delivery.Result.TryGetError(out var error) ? error : null));
        }

        while (failures.Count < simulation.CommandFailures.Count)
        {
            var failure = simulation.CommandFailures[failures.Count];
            failures.Add(new OrderFailure(
                failure.Command.Sequence,
                failure.Error,
                failure.At));
        }
    }
}
