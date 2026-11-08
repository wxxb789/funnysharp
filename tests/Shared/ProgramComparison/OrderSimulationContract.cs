using FunnySharp;

namespace FunnySharp.TestSupport.ProgramComparison;

internal interface IOrderSimulation
{
    OrderModel Model { get; }

    DateTimeOffset Now { get; }

    Random? Random { get; }

    int PendingCount { get; }

    long PeekPendingSequence();

    long PeekPendingSequence(int index);

    OrderCommand PeekPendingCommand();

    OrderCommand PeekPendingCommand(int index);

    IReadOnlyList<EmittedOrderCommand> EmittedCommands { get; }

    IReadOnlyList<OrderDelivery> Deliveries { get; }

    IReadOnlyList<OrderFailure> CommandFailures { get; }

    void AdvanceTo(DateTimeOffset at);

    void AdvanceBy(TimeSpan by);

    TransitionResult<OrderModel, OrderCommand, OrderError> Deliver(OrderMessage message);

    TransitionResult<OrderModel, OrderCommand, OrderError> Deliver(
        OrderMessage message,
        DateTimeOffset at);

    TransitionResult<OrderModel, OrderCommand, OrderError> Reply(
        long commandSequence,
        OrderMessage message);

    TransitionResult<OrderModel, OrderCommand, OrderError> Reply(
        long commandSequence,
        OrderMessage message,
        DateTimeOffset at);

    OrderFailure FailCommand(long commandSequence, OrderError error);

    TransitionResult<OrderModel, OrderCommand, OrderError> Replay();
}

internal sealed record EmittedOrderCommand(
    long Sequence,
    OrderCommand Command,
    DateTimeOffset EmittedAt);

internal sealed record OrderDelivery(
    OrderMessage Message,
    DateTimeOffset At,
    long? CommandSequence,
    TransitionStatus Status,
    OrderError? Error);

internal sealed record OrderFailure(
    long CommandSequence,
    OrderError Error,
    DateTimeOffset At);

internal sealed record OrderRunReport(
    OrderModel Model,
    DateTimeOffset Now,
    int PendingCount,
    IReadOnlyList<EmittedOrderCommand> EmittedCommands,
    IReadOnlyList<OrderDelivery> Deliveries,
    IReadOnlyList<OrderFailure> CommandFailures);

internal static class OrderRunReports
{
    internal static OrderRunReport Capture(IOrderSimulation simulation) =>
        new(
            simulation.Model,
            simulation.Now,
            simulation.PendingCount,
            [.. simulation.EmittedCommands],
            [.. simulation.Deliveries],
            [.. simulation.CommandFailures]);

    internal static void AssertEqual(
        OrderRunReport expected,
        OrderRunReport actual,
        string description)
    {
        OrderAssert.Equal(expected.Model, actual.Model, description + " model");
        OrderAssert.Equal(expected.Now, actual.Now, description + " now");
        OrderAssert.Equal(expected.PendingCount, actual.PendingCount, description + " pending count");
        OrderAssert.SequenceEqual(
            expected.EmittedCommands,
            actual.EmittedCommands,
            description + " emitted commands");
        OrderAssert.SequenceEqual(
            expected.Deliveries,
            actual.Deliveries,
            description + " deliveries");
        OrderAssert.SequenceEqual(
            expected.CommandFailures,
            actual.CommandFailures,
            description + " command failures");
    }
}

internal static class OrderAssert
{
    internal static void True(bool condition, string description)
    {
        if (!condition)
        {
            throw new InvalidOperationException(description);
        }
    }

    internal static void Equal<T>(T expected, T actual, string description)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException(
                $"{description}: expected {expected}, actual {actual}.");
        }
    }

    internal static void SequenceEqual<T>(
        IReadOnlyList<T> expected,
        IReadOnlyList<T> actual,
        string description)
    {
        if (expected.Count != actual.Count)
        {
            throw new InvalidOperationException(
                $"{description}: expected {expected.Count} items, actual {actual.Count}.");
        }

        for (var index = 0; index < expected.Count; index++)
        {
            if (!EqualityComparer<T>.Default.Equals(expected[index], actual[index]))
            {
                throw new InvalidOperationException(
                    $"{description}: item {index} expected {expected[index]}, actual {actual[index]}.");
            }
        }
    }

    internal static T IsType<T>(object? value, string description)
        where T : class
    {
        if (value is not T typed)
        {
            throw new InvalidOperationException(
                $"{description}: expected {typeof(T).Name}, actual {value?.GetType().Name ?? "null"}.");
        }

        return typed;
    }
}
