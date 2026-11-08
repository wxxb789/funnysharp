namespace FunnySharp.Tests;

public sealed class DomainProgramTests
{
    private sealed record Unknown : CounterMessage;

    [Fact]
    public void CreateRejectsANullUpdate()
    {
        Assert.Throws<ArgumentNullException>(
            () => DomainProgram.Create<CounterModel, CounterMessage, CounterCommand, CounterError>(
                null!));
    }

    [Fact]
    public void DefaultProgramFailsExplicitlyOnEveryOperation()
    {
        var program = default(DomainProgram<CounterModel, CounterMessage, CounterCommand, CounterError>);
        var fallback = ProgramTestSupport.CreateProgram();
        var model = new CounterModel(0, 0);

        Assert.Throws<InvalidOperationException>(() => program.Update(model, new Add(1, 1)));
        Assert.Throws<InvalidOperationException>(() => program.AsStateMachine());
        Assert.Throws<InvalidOperationException>(() => program.OrElse(fallback));
        Assert.Throws<InvalidOperationException>(
            () => program.Replay(model, Array.Empty<CounterMessage>()));
        Assert.Throws<InvalidOperationException>(
            () => program.Lift<ParentModel, ParentMessage, ParentCommand>(
                static parent => parent.Child,
                static (parent, child) => parent with { Child = child },
                static message => Option.None<CounterMessage>(),
                static (_, command) => command switch
                {
                    Emit emit => new ParentEmit(emit.Amount, emit.Version),
                    _ => new ParentEmit(0, 0),
                }));
    }

    [Fact]
    public void DefaultFallbackInOrElseFailsExplicitly()
    {
        var program = ProgramTestSupport.CreateProgram();
        var fallback = default(DomainProgram<CounterModel, CounterMessage, CounterCommand, CounterError>);

        Assert.Throws<InvalidOperationException>(() => program.OrElse(fallback));
    }

    [Fact]
    public void UpdateInvokesThePureUpdateOnceAndReturnsItsResult()
    {
        var invocations = 0;
        var program = DomainProgram.Create<CounterModel, CounterMessage, CounterCommand, CounterError>(
            (model, message) =>
            {
                invocations++;
                return ProgramTestSupport.Update(model, message);
            });

        var result = program.Update(new CounterModel(1, 1), new Add(2, 2));

        Assert.Equal(1, invocations);
        Assert.True(result.TryGetChange(out var change));
        Assert.Equal(new CounterModel(3, 2), change.State);
        Assert.Equal([new Emit(2, 2)], change.Outputs);
    }

    [Fact]
    public void AsStateMachineExposesTheSameUpdate()
    {
        var program = ProgramTestSupport.CreateProgram();
        var machine = program.AsStateMachine();

        var direct = program.Update(new CounterModel(0, 0), new Add(4, 7));
        var exposed = machine(new CounterModel(0, 0), new Add(4, 7));

        Assert.Equal(direct, exposed);
    }

    [Fact]
    public void OrElseRunsTheFallbackOnlyAfterUndefined()
    {
        var firstCalls = 0;
        var fallbackCalls = 0;
        var first = DomainProgram.Create<CounterModel, CounterMessage, CounterCommand, CounterError>(
            (model, message) =>
            {
                firstCalls++;
                return ProgramTestSupport.Update(model, message);
            });
        var fallback = DomainProgram.Create<CounterModel, CounterMessage, CounterCommand, CounterError>(
            (model, message) =>
            {
                fallbackCalls++;
                return message is Unknown
                    ? TransitionResult<CounterModel, CounterCommand, CounterError>.Applied(
                        StateChange<CounterModel, CounterCommand>.To(
                            model with { Value = 99, Version = 99 },
                            new Emit(99, 99)))
                    : TransitionResult<CounterModel, CounterCommand, CounterError>.Undefined();
            });
        var combined = first.OrElse(fallback);

        var applied = combined.Update(new CounterModel(0, 0), new Add(1, 1));
        Assert.True(applied.IsApplied);
        Assert.Equal(1, firstCalls);
        Assert.Equal(0, fallbackCalls);

        var undefined = combined.Update(new CounterModel(0, 0), new Unknown());
        Assert.True(undefined.TryGetChange(out var change));
        Assert.Equal(99, change.State.Value);
        Assert.Equal(2, firstCalls);
        Assert.Equal(1, fallbackCalls);
    }

    [Fact]
    public void OrElsePreservesRejectedAndFailedResultsWithoutCallingTheFallback()
    {
        var fallbackCalls = 0;
        var fallback = DomainProgram.Create<CounterModel, CounterMessage, CounterCommand, CounterError>(
            (model, message) =>
            {
                fallbackCalls++;
                return ProgramTestSupport.Update(model, message);
            });
        var combined = ProgramTestSupport.CreateProgram().OrElse(fallback);
        var model = new CounterModel(0, 0);

        var rejected = combined.Update(model, new Reject("no"));
        Assert.True(rejected.IsRejected);
        Assert.True(rejected.TryGetError(out var rejection));
        Assert.Equal(new RejectedError("no"), rejection);

        var failed = combined.Update(model, new Fail("broken"));
        Assert.True(failed.IsFailed);
        Assert.True(failed.TryGetError(out var failure));
        Assert.Equal(new FailedError("broken"), failure);

        Assert.Equal(0, fallbackCalls);
    }

    [Fact]
    public void ReplayOfEmptyHistoryIsAppliedWithTheInitialModelAndNoOutputs()
    {
        var program = ProgramTestSupport.CreateProgram();
        var model = new CounterModel(5, 9);

        var replayed = program.Replay(model, Array.Empty<CounterMessage>());

        Assert.True(replayed.TryGetChange(out var change));
        Assert.Equal(model, change.State);
        Assert.Empty(change.Outputs);
    }

    [Fact]
    public void ReplayAppliesMessagesInOrderOnceAndCollectsOrderedCommands()
    {
        var program = ProgramTestSupport.CreateProgram();
        var history = new ProbeEnumerable<CounterMessage>(
            [new Add(1, 1), new AddMany(2, 3, 2)]);

        var replayed = program.Replay(new CounterModel(0, 0), history);

        Assert.True(replayed.TryGetChange(out var change));
        Assert.Equal(new CounterModel(6, 2), change.State);
        Assert.Equal(
            [new Emit(1, 1), new EmitMany(2, 3, 2)],
            change.Outputs);
        Assert.Equal(1, history.EnumeratorCount);
        Assert.Equal(2, history.ItemsYielded);
        Assert.Equal(1, history.DisposeCount);
    }

    [Fact]
    public void ReplayStopsAtTheFirstNonAppliedResultAndPreservesItsStatusAndError()
    {
        var program = ProgramTestSupport.CreateProgram();
        var rejectedHistory = new ProbeEnumerable<CounterMessage>(
            [new Add(1, 1), new Reject("stop"), new Add(2, 2)]);

        var rejected = program.Replay(new CounterModel(0, 0), rejectedHistory);

        Assert.True(rejected.IsRejected);
        Assert.True(rejected.TryGetError(out var rejection));
        Assert.Equal(new RejectedError("stop"), rejection);
        Assert.Equal(1, rejectedHistory.EnumeratorCount);
        Assert.Equal(2, rejectedHistory.ItemsYielded);
        Assert.Equal(1, rejectedHistory.DisposeCount);

        var failed = program.Replay(
            new CounterModel(0, 0),
            new CounterMessage[] { new Add(1, 1), new Fail("broken"), new Add(2, 2) });
        Assert.True(failed.IsFailed);
        Assert.True(failed.TryGetError(out var failure));
        Assert.Equal(new FailedError("broken"), failure);

        var undefined = program.Replay(
            new CounterModel(0, 0),
            new CounterMessage[] { new Add(1, 1), new Unknown(), new Add(2, 2) });
        Assert.True(undefined.IsUndefined);
    }

    [Fact]
    public void ReplayPropagatesUpdateExceptionsUnchanged()
    {
        var expected = new InvalidOperationException("boom");
        var program = DomainProgram.Create<CounterModel, CounterMessage, CounterCommand, CounterError>(
            (model, message) => throw expected);

        var actual = Assert.Throws<InvalidOperationException>(
            () => program.Replay(new CounterModel(0, 0), new CounterMessage[] { new Add(1, 1) }));

        Assert.Same(expected, actual);
    }

    [Fact]
    public void ReplayRejectsANullHistory()
    {
        var program = ProgramTestSupport.CreateProgram();

        Assert.Throws<ArgumentNullException>(
            () => program.Replay(new CounterModel(0, 0), null!));
    }
}
