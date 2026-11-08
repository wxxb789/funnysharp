using FunnySharp;

namespace FunnySharp.TestSupport.ProgramComparison;

internal sealed class HandWrittenOrderSimulation : IOrderSimulation
{
    private readonly OrderModel initialModel;
    private readonly Random? random;
    private readonly List<EmittedOrderCommand> emitted = [];
    private readonly List<EmittedOrderCommand> pending = [];
    private readonly List<OrderDelivery> deliveries = [];
    private readonly List<OrderFailure> failures = [];
    private OrderModel model;
    private DateTimeOffset now;
    private long nextSequence = 1;

    internal HandWrittenOrderSimulation(
        OrderModel initialModel,
        DateTimeOffset startedAt,
        Random? random = null)
    {
        this.initialModel = initialModel;
        model = initialModel;
        now = startedAt;
        this.random = random;
    }

    public OrderModel Model => model;

    public DateTimeOffset Now => now;

    public Random? Random => random;

    public int PendingCount => pending.Count;

    public IReadOnlyList<EmittedOrderCommand> EmittedCommands => emitted;

    public IReadOnlyList<OrderDelivery> Deliveries => deliveries;

    public IReadOnlyList<OrderFailure> CommandFailures => failures;

    public long PeekPendingSequence() => pending[0].Sequence;

    public long PeekPendingSequence(int index) => pending[index].Sequence;

    public OrderCommand PeekPendingCommand() => pending[0].Command;

    public OrderCommand PeekPendingCommand(int index) => pending[index].Command;

    public void AdvanceTo(DateTimeOffset at)
    {
        if (at < now)
        {
            throw new ArgumentOutOfRangeException(
                nameof(at),
                at,
                "Simulation time cannot move backwards.");
        }

        now = at;
    }

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

    public TransitionResult<OrderModel, OrderCommand, OrderError> Deliver(
        OrderMessage message) =>
        DeliverCore(message);

    public TransitionResult<OrderModel, OrderCommand, OrderError> Deliver(
        OrderMessage message,
        DateTimeOffset at)
    {
        AdvanceTo(at);
        return DeliverCore(message);
    }

    public TransitionResult<OrderModel, OrderCommand, OrderError> Reply(
        long commandSequence,
        OrderMessage message) =>
        ReplyCore(commandSequence, message);

    public TransitionResult<OrderModel, OrderCommand, OrderError> Reply(
        long commandSequence,
        OrderMessage message,
        DateTimeOffset at)
    {
        AdvanceTo(at);
        return ReplyCore(commandSequence, message);
    }

    public OrderFailure FailCommand(long commandSequence, OrderError error)
    {
        var command = GetEmitted(commandSequence);
        if (!RemovePending(commandSequence))
        {
            throw new InvalidOperationException(
                $"Command sequence {commandSequence} has already been completed or failed.");
        }

        var failure = new OrderFailure(command.Sequence, error, now);
        failures.Add(failure);
        return failure;
    }

    public TransitionResult<OrderModel, OrderCommand, OrderError> Replay() =>
        HandWrittenOrderWorkflow.Update.Replay(
            initialModel,
            deliveries.Select(static delivery => delivery.Message));

    private TransitionResult<OrderModel, OrderCommand, OrderError> DeliverCore(
        OrderMessage message)
    {
        var result = HandWrittenOrderWorkflow.Update(model, message);
        deliveries.Add(new OrderDelivery(
            message,
            now,
            null,
            result.Status,
            ErrorOf(result)));

        if (result.TryGetChange(out var change))
        {
            model = change.State;
            Emit(change.Outputs);
        }

        return result;
    }

    private TransitionResult<OrderModel, OrderCommand, OrderError> ReplyCore(
        long commandSequence,
        OrderMessage message)
    {
        _ = GetEmitted(commandSequence);

        var result = HandWrittenOrderWorkflow.Update(model, message);
        deliveries.Add(new OrderDelivery(
            message,
            now,
            commandSequence,
            result.Status,
            ErrorOf(result)));

        if (result.TryGetChange(out var change))
        {
            model = change.State;
            Emit(change.Outputs);
        }

        _ = RemovePending(commandSequence);
        return result;
    }

    private void Emit(IReadOnlyList<OrderCommand> outputs)
    {
        for (var index = 0; index < outputs.Count; index++)
        {
            var command = new EmittedOrderCommand(nextSequence++, outputs[index], now);
            emitted.Add(command);
            pending.Add(command);
        }
    }

    private EmittedOrderCommand GetEmitted(long commandSequence)
    {
        if (commandSequence < 1 || commandSequence > emitted.Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(commandSequence),
                commandSequence,
                "The command sequence was not emitted by this simulation.");
        }

        return emitted[(int)(commandSequence - 1)];
    }

    private bool RemovePending(long commandSequence)
    {
        var index = pending.FindIndex(command => command.Sequence == commandSequence);
        if (index < 0)
        {
            return false;
        }

        pending.RemoveAt(index);
        return true;
    }

    private static OrderError? ErrorOf(
        TransitionResult<OrderModel, OrderCommand, OrderError> result) =>
        result.TryGetError(out var error) ? error : null;
}
