namespace FunnySharp.Tests;

public sealed class ProgramSimulationTests
{
    private static readonly DateTimeOffset StartedAt =
        new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private sealed record Unknown : CounterMessage;

    private sealed record MutableCommand(List<string> Values) : CounterCommand;

    private sealed record AppendValues(List<string> Values) : MutableStateMessage;

    [Fact]
    public void ConstructorRejectsAnUninitializedProgram()
    {
        var program = default(DomainProgram<CounterModel, CounterMessage, CounterCommand, CounterError>);

        Assert.Throws<InvalidOperationException>(
            () => new ProgramSimulation<CounterModel, CounterMessage, CounterCommand, CounterError>(
                program,
                new CounterModel(0, 0),
                StartedAt));
    }

    [Fact]
    public void TimeStartsAtTheExplicitInstantAndMovesForwardOnly()
    {
        var simulation = NewSimulation();

        Assert.Equal(StartedAt, simulation.Now);

        var later = StartedAt.AddMinutes(5);
        simulation.AdvanceTo(later);
        Assert.Equal(later, simulation.Now);

        simulation.AdvanceBy(TimeSpan.FromMinutes(2));
        Assert.Equal(later.AddMinutes(2), simulation.Now);

        simulation.AdvanceBy(TimeSpan.Zero);
        Assert.Equal(later.AddMinutes(2), simulation.Now);

        Assert.Throws<ArgumentOutOfRangeException>(() => simulation.AdvanceTo(StartedAt));
        Assert.Throws<ArgumentOutOfRangeException>(() => simulation.AdvanceBy(TimeSpan.FromTicks(-1)));
    }

    [Fact]
    public void DeliverAppliesAMessageAndEmitsCommandsInOrder()
    {
        var simulation = NewSimulation();

        var first = simulation.Deliver(new AddMany(1, 2, 7));

        Assert.True(first.TryGetChange(out var firstChange));
        Assert.Equal(new CounterModel(3, 7), simulation.Model);
        Assert.Equal(new CounterModel(3, 7), firstChange!.State);
        var delivery = Assert.Single(simulation.Deliveries);
        Assert.Null(delivery.CommandSequence);
        Assert.Equal(StartedAt, delivery.At);
        Assert.Equal(first, delivery.Result);
        var emitted = Assert.Single(simulation.EmittedCommands);
        Assert.Equal(1, emitted.Sequence);
        Assert.Equal(new EmitMany(1, 2, 7), emitted.Command);
        Assert.Equal(StartedAt, emitted.EmittedAt);
        Assert.Single(simulation.PendingCommands);

        var second = simulation.Deliver(new Split(4, 5, 8));

        Assert.True(second.TryGetChange(out var secondChange));
        Assert.Equal(new CounterModel(12, 8), simulation.Model);
        Assert.Equal(new CounterModel(12, 8), secondChange!.State);
        Assert.Equal(new long[] { 1, 2, 3 }, simulation.EmittedCommands.Select(static c => c.Sequence));
        Assert.Equal(
            new CounterCommand[] { new EmitMany(1, 2, 7), new Emit(4, 8), new Emit(5, 8) },
            simulation.EmittedCommands.Select(static c => c.Command));
        Assert.Equal(3, simulation.PendingCommands.Count);
    }

    [Fact]
    public void DeliverPreservesRejectedFailedAndUndefinedResultsWithoutChangingTheModel()
    {
        var simulation = NewSimulation();
        var initial = simulation.Model;

        var rejected = simulation.Deliver(new Reject("no"));
        var failed = simulation.Deliver(new Fail("broken"));
        var undefined = simulation.Deliver(new Unknown());

        Assert.True(rejected.IsRejected);
        Assert.True(rejected.TryGetError(out var rejection));
        Assert.Equal(new RejectedError("no"), rejection);
        Assert.True(failed.IsFailed);
        Assert.True(failed.TryGetError(out var failure));
        Assert.Equal(new FailedError("broken"), failure);
        Assert.True(undefined.IsUndefined);
        Assert.Equal(initial, simulation.Model);
        Assert.Empty(simulation.EmittedCommands);
        Assert.Empty(simulation.PendingCommands);
        Assert.Equal(3, simulation.Deliveries.Count);
        Assert.True(simulation.Deliveries[0].Result.IsRejected);
        Assert.True(simulation.Deliveries[1].Result.IsFailed);
        Assert.True(simulation.Deliveries[2].Result.IsUndefined);
    }

    [Fact]
    public void NonAppliedDeliveriesPreserveExistingPendingCommands()
    {
        var simulation = NewSimulation();
        simulation.Deliver(new Add(1, 1));
        simulation.Deliver(new Add(2, 2));

        var rejected = simulation.Deliver(new Reject("no"));
        var failed = simulation.Deliver(new Fail("broken"));
        var undefined = simulation.Deliver(new Unknown());

        Assert.True(rejected.IsRejected);
        Assert.True(failed.IsFailed);
        Assert.True(undefined.IsUndefined);
        Assert.Equal(
            new long[] { 1, 2 },
            simulation.PendingCommands.Select(static command => command.Sequence));
        Assert.Equal(new CounterModel(3, 2), simulation.Model);
        Assert.Equal(5, simulation.Deliveries.Count);
    }

    [Fact]
    public void DeliverAtAdvancesTheClockAndStampsTheDeliveryAndCommands()
    {
        var simulation = NewSimulation();
        var later = StartedAt.AddMinutes(5);

        var result = simulation.Deliver(new Add(1, 1), later);

        Assert.True(result.IsApplied);
        Assert.Equal(later, simulation.Now);
        Assert.Equal(later, simulation.Deliveries[0].At);
        Assert.Equal(later, simulation.EmittedCommands[0].EmittedAt);

        Assert.Throws<ArgumentOutOfRangeException>(() => simulation.Deliver(new Add(1, 1), StartedAt));
        Assert.Single(simulation.Deliveries);
        Assert.Single(simulation.EmittedCommands);
    }

    [Fact]
    public void ReplyCorrelatesTheDeliveryToTheEmittedCommand()
    {
        var simulation = NewSimulation();
        simulation.Deliver(new Add(1, 1));
        var later = StartedAt.AddMinutes(1);

        var result = simulation.Reply(1, new NoOp(), later);

        Assert.True(result.IsApplied);
        Assert.Equal(later, simulation.Now);
        Assert.Equal(2, simulation.Deliveries.Count);
        Assert.Equal(1, simulation.Deliveries[1].CommandSequence);
        Assert.Equal(later, simulation.Deliveries[1].At);
        Assert.Empty(simulation.PendingCommands);

        Assert.Throws<ArgumentOutOfRangeException>(() => simulation.Reply(99, new NoOp()));
        Assert.Equal(2, simulation.Deliveries.Count);
    }

    [Fact]
    public void ReplyAllowsDuplicateLateAndPostFailureRepliesForTheProgramToDecide()
    {
        var simulation = NewSimulation();
        simulation.Deliver(new Add(1, 1));
        simulation.Deliver(new Add(2, 2));

        var first = simulation.Reply(1, new NoOp());
        var duplicate = simulation.Reply(1, new Reject("duplicate"));
        var late = simulation.Reply(1, new NoOp());

        Assert.True(first.IsApplied);
        Assert.True(duplicate.IsRejected);
        Assert.True(late.IsApplied);
        Assert.Equal(3, simulation.Deliveries.Count(static d => d.CommandSequence == 1));
        Assert.Equal(5, simulation.Deliveries.Count);
        Assert.Single(simulation.PendingCommands);

        var failureSimulation = NewSimulation();
        failureSimulation.Deliver(new Add(1, 1));
        _ = failureSimulation.FailCommand(1, new RejectedError("gateway"));

        var afterFailure = failureSimulation.Reply(1, new NoOp());

        Assert.True(afterFailure.IsApplied);
        Assert.Equal(2, failureSimulation.Deliveries.Count);
        Assert.Equal(1, failureSimulation.Deliveries[1].CommandSequence);
    }

    [Fact]
    public void FailCommandRecordsAnExecutionFailureWithoutChangingTheModelOrDeliveringAMessage()
    {
        var simulation = NewSimulation();
        simulation.Deliver(new Add(1, 1));
        var modelBeforeFailure = simulation.Model;

        var failure = simulation.FailCommand(1, new RejectedError("gateway"));

        Assert.Equal(1, failure.Command.Sequence);
        Assert.Equal(new Emit(1, 1), failure.Command.Command);
        Assert.Equal(new RejectedError("gateway"), failure.Error);
        Assert.Equal(StartedAt, failure.At);
        Assert.Equal(modelBeforeFailure, simulation.Model);
        Assert.Single(simulation.CommandFailures);
        Assert.Empty(simulation.PendingCommands);
        Assert.Single(simulation.Deliveries);
        Assert.Single(simulation.EmittedCommands);

        Assert.Throws<InvalidOperationException>(
            () => simulation.FailCommand(1, new RejectedError("again")));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => simulation.FailCommand(99, new RejectedError("unknown")));
    }

    [Fact]
    public void FailCommandRemovesOnlyTheFailedPendingCommand()
    {
        var simulation = NewSimulation();
        simulation.Deliver(new Add(1, 1));
        simulation.Deliver(new Add(2, 2));

        var failure = simulation.FailCommand(1, new RejectedError("gateway"));

        Assert.Equal(1, failure.Command.Sequence);
        Assert.Equal(
            new long[] { 2 },
            simulation.PendingCommands.Select(static command => command.Sequence));
        Assert.Equal(2, simulation.EmittedCommands.Count);
        Assert.Single(simulation.CommandFailures);
        Assert.Equal(new CounterModel(3, 2), simulation.Model);
    }

    [Fact]
    public void FailCommandRejectsACommandThatWasAlreadyRepliedTo()
    {
        var simulation = NewSimulation();
        simulation.Deliver(new Add(1, 1));
        simulation.Reply(1, new NoOp());

        Assert.Throws<InvalidOperationException>(
            () => simulation.FailCommand(1, new RejectedError("too late")));
    }

    [Fact]
    public void FailCommandAtAdvancesTheClockAndStampsTheFailure()
    {
        var simulation = NewSimulation();
        simulation.Deliver(new Add(1, 1));
        var later = StartedAt.AddMinutes(3);

        var failure = simulation.FailCommand(1, new RejectedError("gateway"), later);

        Assert.Equal(later, simulation.Now);
        Assert.Equal(later, failure.At);

        var second = NewSimulation();
        second.Deliver(new Add(1, 1));
        second.AdvanceTo(StartedAt.AddMinutes(1));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => second.FailCommand(1, new RejectedError("gateway"), StartedAt));
        Assert.Empty(second.CommandFailures);
        Assert.Single(second.PendingCommands);
    }

    [Fact]
    public void CommandsEmittedByAReplyAppendAfterExistingPendingCommands()
    {
        var simulation = NewSimulation();
        simulation.Deliver(new Add(1, 1));
        simulation.Deliver(new Add(2, 2));

        var result = simulation.Reply(1, new Add(3, 3));

        Assert.True(result.IsApplied);
        Assert.Equal(new CounterModel(6, 3), simulation.Model);
        Assert.Equal(new long[] { 2, 3 }, simulation.PendingCommands.Select(static c => c.Sequence));
        Assert.Equal(
            new CounterCommand[] { new Emit(2, 2), new Emit(3, 3) },
            simulation.PendingCommands.Select(static c => c.Command));
    }

    [Fact]
    public void ProgramExceptionsLeaveTheSimulationConsistent()
    {
        var program = DomainProgram.Create<CounterModel, CounterMessage, CounterCommand, CounterError>(
            (model, message) => message switch
            {
                Add add => TransitionResult<CounterModel, CounterCommand, CounterError>.Applied(
                    StateChange<CounterModel, CounterCommand>.To(
                        model with { Value = model.Value + add.Amount, Version = add.Version },
                        new Emit(add.Amount, add.Version))),
                Fail fail => throw new InvalidOperationException(fail.Reason),
                _ => TransitionResult<CounterModel, CounterCommand, CounterError>.Undefined(),
            });
        var simulation = new ProgramSimulation<CounterModel, CounterMessage, CounterCommand, CounterError>(
            program,
            new CounterModel(0, 0),
            StartedAt);
        simulation.Deliver(new Add(1, 1));

        var deliveryException = Assert.Throws<InvalidOperationException>(
            () => simulation.Deliver(new Fail("delivery")));
        Assert.Equal("delivery", deliveryException.Message);
        Assert.Single(simulation.Deliveries);
        Assert.Single(simulation.PendingCommands);
        Assert.Equal(new CounterModel(1, 1), simulation.Model);

        var replyException = Assert.Throws<InvalidOperationException>(
            () => simulation.Reply(1, new Fail("reply")));
        Assert.Equal("reply", replyException.Message);
        Assert.Single(simulation.Deliveries);
        Assert.Single(simulation.PendingCommands);
        Assert.Equal(new CounterModel(1, 1), simulation.Model);

        var later = StartedAt.AddMinutes(2);

        var deliveryAtException = Assert.Throws<InvalidOperationException>(
            () => simulation.Deliver(new Fail("delivery at"), later));
        Assert.Equal("delivery at", deliveryAtException.Message);
        Assert.Equal(StartedAt, simulation.Now);
        Assert.Single(simulation.Deliveries);
        Assert.Single(simulation.PendingCommands);
        Assert.Equal(new CounterModel(1, 1), simulation.Model);

        var replyAtException = Assert.Throws<InvalidOperationException>(
            () => simulation.Reply(1, new Fail("reply at"), later));
        Assert.Equal("reply at", replyAtException.Message);
        Assert.Equal(StartedAt, simulation.Now);
        Assert.Single(simulation.Deliveries);
        Assert.Single(simulation.PendingCommands);
        Assert.Equal(new CounterModel(1, 1), simulation.Model);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => simulation.Reply(99, new NoOp(), later));
        Assert.Equal(StartedAt, simulation.Now);
        Assert.Single(simulation.PendingCommands);

        _ = simulation.FailCommand(1, new RejectedError("gateway"), later);
        Assert.Equal(later, simulation.Now);
        Assert.Empty(simulation.PendingCommands);
        Assert.Single(simulation.CommandFailures);

        var evenLater = later.AddMinutes(1);
        Assert.Throws<InvalidOperationException>(
            () => simulation.FailCommand(1, new RejectedError("again"), evenLater));
        Assert.Equal(later, simulation.Now);
        Assert.Single(simulation.CommandFailures);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => simulation.FailCommand(99, new RejectedError("unknown"), evenLater));
        Assert.Equal(later, simulation.Now);
        Assert.Single(simulation.CommandFailures);
    }

    [Fact]
    public void ReplayOfAnEmptyTranscriptReturnsTheInitialModel()
    {
        var simulation = NewSimulation();
        var initial = simulation.Model;

        var replayed = simulation.Replay();

        Assert.True(replayed.TryGetChange(out var change));
        Assert.Equal(initial, change!.State);
        Assert.Empty(change.Outputs);
    }

    [Fact]
    public void ReplayAppliesRecordedDeliveriesInOrderAndDoesNotReuseExecutedCommands()
    {
        var simulation = NewSimulation();
        simulation.Deliver(new Add(1, 1));
        simulation.Deliver(new AddMany(2, 3, 2));
        var emittedCommand = simulation.EmittedCommands[0].Command;
        var deliveryCount = simulation.Deliveries.Count;
        var pendingCount = simulation.PendingCommands.Count;

        var replayed = simulation.Replay();

        Assert.True(replayed.TryGetChange(out var change));
        Assert.Equal(new CounterModel(6, 2), change!.State);
        Assert.Equal(new CounterCommand[] { new Emit(1, 1), new EmitMany(2, 3, 2) }, change.Outputs);
        Assert.NotSame(emittedCommand, change.Outputs[0]);
        Assert.Equal(deliveryCount, simulation.Deliveries.Count);
        Assert.Equal(pendingCount, simulation.PendingCommands.Count);
    }

    [Fact]
    public void ReplayStopsAtTheFirstNonAppliedDelivery()
    {
        var simulation = NewSimulation();
        simulation.Deliver(new Add(1, 1));
        simulation.Deliver(new Reject("stop"));
        simulation.Deliver(new Add(2, 2));

        var replayed = simulation.Replay();

        Assert.True(replayed.IsRejected);
        Assert.True(replayed.TryGetError(out var rejection));
        Assert.Equal(new RejectedError("stop"), rejection);
        Assert.Equal(new CounterModel(3, 2), simulation.Model);
        Assert.Equal(3, simulation.Deliveries.Count);
    }

    [Fact]
    public void ReplayIgnoresCommandExecutionFailures()
    {
        var simulation = NewSimulation();
        simulation.Deliver(new Add(1, 1));
        _ = simulation.FailCommand(1, new RejectedError("gateway"));
        simulation.Deliver(new NoOp());

        var replayed = simulation.Replay();

        Assert.True(replayed.TryGetChange(out var change));
        Assert.Equal(new CounterModel(1, 1), change!.State);
        Assert.Equal(new CounterCommand[] { new Emit(1, 1) }, change.Outputs);
        Assert.Single(simulation.CommandFailures);
        Assert.Equal(2, simulation.Deliveries.Count);
    }

    [Fact]
    public void MutatingAnEmittedCommandPayloadDoesNotChangeReplay()
    {
        var program = DomainProgram.Create<CounterModel, CounterMessage, CounterCommand, CounterError>(
            (model, message) => message is Add add
                ? TransitionResult<CounterModel, CounterCommand, CounterError>.Applied(
                    StateChange<CounterModel, CounterCommand>.To(
                        model with { Value = model.Value + add.Amount, Version = add.Version },
                        new MutableCommand(["original"])))
                : TransitionResult<CounterModel, CounterCommand, CounterError>.Undefined());
        var simulation = new ProgramSimulation<CounterModel, CounterMessage, CounterCommand, CounterError>(
            program,
            new CounterModel(0, 0),
            StartedAt);
        simulation.Deliver(new Add(1, 1));

        var emitted = Assert.IsType<MutableCommand>(simulation.EmittedCommands[0].Command);
        emitted.Values.Add("mutated");

        var replayed = simulation.Replay();

        Assert.True(replayed.TryGetChange(out var change));
        Assert.Equal(new CounterModel(1, 1), change!.State);
        var replayedCommand = Assert.IsType<MutableCommand>(change.Outputs[0]);
        Assert.Single(replayedCommand.Values);
        Assert.Equal("original", replayedCommand.Values[0]);
        Assert.Equal(2, emitted.Values.Count);
        Assert.Equal("original", emitted.Values[0]);
        Assert.Equal("mutated", emitted.Values[1]);
    }

    [Fact]
    public void MutatingAReturnedStateSnapshotDoesNotChangeReplay()
    {
        var program = DomainProgram.Create<MutableStateModel, MutableStateMessage,
            CounterCommand, CounterError>(
            (model, message) => message is AppendValue append
                ? TransitionResult<MutableStateModel, CounterCommand, CounterError>.Applied(
                    StateChange<MutableStateModel, CounterCommand>.To(
                        model with { Values = [.. model.Values, append.Value] }))
                : TransitionResult<MutableStateModel, CounterCommand, CounterError>.Undefined());
        var initial = new MutableStateModel([], 0);
        var simulation = new ProgramSimulation<MutableStateModel, MutableStateMessage,
            CounterCommand, CounterError>(program, initial, StartedAt);
        Assert.True(simulation.Deliver(new AppendValue("a")).IsApplied);

        simulation.Model.Values.Add("mutated");

        var replayed = simulation.Replay();

        Assert.True(replayed.TryGetChange(out var change));
        Assert.Single(change!.State.Values);
        Assert.Equal("a", change.State.Values[0]);
        Assert.Equal(2, simulation.Model.Values.Count);
        Assert.Equal("a", simulation.Model.Values[0]);
        Assert.Equal("mutated", simulation.Model.Values[1]);
        Assert.Empty(initial.Values);
    }

    [Fact]
    public void ReplayObservesCallerMutationOfRecordedMessagesAndTheInitialModel()
    {
        var program = DomainProgram.Create<MutableStateModel, MutableStateMessage,
            CounterCommand, CounterError>(
            (model, message) => message is AppendValues append
                ? TransitionResult<MutableStateModel, CounterCommand, CounterError>.Applied(
                    StateChange<MutableStateModel, CounterCommand>.To(
                        model with { Values = [.. model.Values, .. append.Values] }))
                : TransitionResult<MutableStateModel, CounterCommand, CounterError>.Undefined());
        var initial = new MutableStateModel(["seed"], 0);
        var message = new AppendValues(["a"]);
        var simulation = new ProgramSimulation<MutableStateModel, MutableStateMessage,
            CounterCommand, CounterError>(program, initial, StartedAt);
        Assert.True(simulation.Deliver(message).IsApplied);
        Assert.Equal(new[] { "seed", "a" }, simulation.Model.Values);

        message.Values.Add("message-mutation");
        initial.Values.Add("initial-mutation");

        var replayed = simulation.Replay();

        Assert.True(replayed.TryGetChange(out var change));
        Assert.Equal(
            new[] { "seed", "initial-mutation", "a", "message-mutation" },
            change!.State.Values);
    }

    [Fact]
    public void TheSimulationNeverConsumesTheCallerSuppliedRandom()
    {
        var random = new CountingRandom(42);
        var simulation = new ProgramSimulation<CounterModel, CounterMessage, CounterCommand, CounterError>(
            ProgramTestSupport.CreateProgram(),
            new CounterModel(0, 0),
            StartedAt,
            random);

        Assert.Same(random, simulation.Random);

        simulation.Deliver(new Add(1, 1));
        simulation.Reply(1, new NoOp());

        Assert.Equal(0, random.Calls);
    }

    [Fact]
    public void ASimulationWithoutARandomSourceExposesNull()
    {
        var simulation = NewSimulation();

        Assert.Null(simulation.Random);
    }

    [Fact]
    public void TranscriptValuesExposeValueEqualityAndDeterministicText()
    {
        var first = NewSimulation();
        var second = NewSimulation();

        first.Deliver(new Add(1, 1));
        second.Deliver(new Add(1, 1));
        first.Reply(1, new NoOp());
        second.Reply(1, new NoOp());

        Assert.Equal(first.EmittedCommands[0], second.EmittedCommands[0]);
        Assert.Equal(
            first.EmittedCommands[0].GetHashCode(),
            second.EmittedCommands[0].GetHashCode());
        Assert.Contains("#1", first.EmittedCommands[0].ToString(), StringComparison.Ordinal);

        Assert.Equal(first.Deliveries[0], second.Deliveries[0]);
        Assert.Equal(first.Deliveries[0].GetHashCode(), second.Deliveries[0].GetHashCode());
        Assert.Contains("Message at", first.Deliveries[0].ToString(), StringComparison.Ordinal);
        Assert.Equal(first.Deliveries[1], second.Deliveries[1]);
        Assert.Contains("Reply #1", first.Deliveries[1].ToString(), StringComparison.Ordinal);

        var failureFirst = NewSimulation();
        var failureSecond = NewSimulation();
        failureFirst.Deliver(new Add(1, 1));
        failureSecond.Deliver(new Add(1, 1));

        var firstFailure = failureFirst.FailCommand(1, new RejectedError("gateway"));
        var secondFailure = failureSecond.FailCommand(1, new RejectedError("gateway"));

        Assert.Equal(firstFailure, secondFailure);
        Assert.Equal(firstFailure.GetHashCode(), secondFailure.GetHashCode());
        Assert.Contains("Command #1 failed", firstFailure.ToString(), StringComparison.Ordinal);
    }

    private static ProgramSimulation<CounterModel, CounterMessage, CounterCommand, CounterError>
        NewSimulation() =>
        new(ProgramTestSupport.CreateProgram(), new CounterModel(0, 0), StartedAt);
}
