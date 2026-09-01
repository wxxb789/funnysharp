namespace FunnySharp.Tests;

public sealed class StateMachineTests
{
    [Fact]
    public void TransitionResultExposesAllFourStatesAndOnlyTheirActivePayload()
    {
        var applied = TransitionResult<int, string, string>.Applied(
            StateChange<int, string>.To(2, "incremented"));
        var rejected = TransitionResult<int, string, string>.Rejected("invalid-event");
        var failed = TransitionResult<int, string, string>.Failed("persistence-failed");
        var undefined = TransitionResult<int, string, string>.Undefined();
        TransitionResult<int, string, string> defaultResult = default;

        Assert.Equal(TransitionStatus.Applied, applied.Status);
        Assert.True(applied.IsApplied);
        Assert.False(applied.IsRejected);
        Assert.False(applied.IsFailed);
        Assert.False(applied.IsUndefined);
        Assert.True(applied.TryGetChange(out var appliedChange));
        Assert.Equal(2, appliedChange.State);
        Assert.Equal(["incremented"], appliedChange.Outputs);
        Assert.False(applied.TryGetError(out _));

        Assert.Equal(TransitionStatus.Rejected, rejected.Status);
        Assert.True(rejected.IsRejected);
        Assert.True(rejected.TryGetError(out var rejection));
        Assert.Equal("invalid-event", rejection);
        Assert.False(rejected.TryGetChange(out _));

        Assert.Equal(TransitionStatus.Failed, failed.Status);
        Assert.True(failed.IsFailed);
        Assert.True(failed.TryGetError(out var failure));
        Assert.Equal("persistence-failed", failure);
        Assert.False(failed.TryGetChange(out _));

        Assert.Equal(TransitionStatus.Undefined, undefined.Status);
        Assert.True(undefined.IsUndefined);
        Assert.False(undefined.TryGetChange(out _));
        Assert.False(undefined.TryGetError(out _));
        Assert.Equal(undefined, defaultResult);
        Assert.Equal(TransitionStatus.Undefined, defaultResult.Status);
    }

    [Fact]
    public void TransitionResultMatchSelectsExactlyOneBranch()
    {
        var applied = TransitionResult<int, string, string>.Applied(
            StateChange<int, string>.To(3, "created"));
        var rejected = TransitionResult<int, string, string>.Rejected("invalid-event");
        var failed = TransitionResult<int, string, string>.Failed("store-down");
        var undefined = TransitionResult<int, string, string>.Undefined();
        var appliedCalls = 0;
        var rejectedCalls = 0;
        var failedCalls = 0;
        var undefinedCalls = 0;

        string Match(TransitionResult<int, string, string> result) => result.Match(
            change =>
            {
                appliedCalls++;
                return $"applied:{change.State}:{change.Outputs[0]}";
            },
            error =>
            {
                rejectedCalls++;
                return $"rejected:{error}";
            },
            error =>
            {
                failedCalls++;
                return $"failed:{error}";
            },
            () =>
            {
                undefinedCalls++;
                return "undefined";
            });

        Assert.Equal("applied:3:created", Match(applied));
        Assert.Equal("rejected:invalid-event", Match(rejected));
        Assert.Equal("failed:store-down", Match(failed));
        Assert.Equal("undefined", Match(undefined));
        Assert.Equal(1, appliedCalls);
        Assert.Equal(1, rejectedCalls);
        Assert.Equal(1, failedCalls);
        Assert.Equal(1, undefinedCalls);
    }

    [Fact]
    public void TransitionResultValidatesRequiredCallbacksAndAppliedChanges()
    {
        var result = TransitionResult<int, string, string>.Undefined();
        Func<StateChange<int, string>, string> applied = _ => "applied";
        Func<string, string> rejected = _ => "rejected";
        Func<string, string> failed = _ => "failed";
        Func<string> undefined = () => "undefined";

        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = TransitionResult<int, string, string>.Applied(null!);
        });
        Assert.Throws<ArgumentNullException>(() => result.Match(null!, rejected, failed, undefined));
        Assert.Throws<ArgumentNullException>(() => result.Match(applied, null!, failed, undefined));
        Assert.Throws<ArgumentNullException>(() => result.Match(applied, rejected, null!, undefined));
        Assert.Throws<ArgumentNullException>(() => result.Match(applied, rejected, failed, null!));
    }

    [Fact]
    public void TransitionResultUsesStructuralEqualityAndHashesItsActiveState()
    {
        var firstApplied = TransitionResult<int, string, string>.Applied(
            StateChange<int, string>.To(1, "opened"));
        var secondApplied = TransitionResult<int, string, string>.Applied(
            StateChange<int, string>.To(1, "opened"));
        var rejected = TransitionResult<int, string, string>.Rejected("opened");
        var failed = TransitionResult<int, string, string>.Failed("opened");
        var undefined = TransitionResult<int, string, string>.Undefined();

        Assert.Equal(firstApplied, secondApplied);
        Assert.Equal(firstApplied.GetHashCode(), secondApplied.GetHashCode());
        Assert.NotEqual(firstApplied, rejected);
        Assert.NotEqual(rejected, failed);
        Assert.NotEqual(failed, undefined);
    }

    [Fact]
    public void OrElseUsesFallbackOnlyForUndefinedTransitions()
    {
        var fallbackCalls = 0;
        StateMachine<int, WorkflowEvent, string, string> fallback = (state, @event) =>
        {
            fallbackCalls++;
            return TransitionResult<int, string, string>.Applied(
                StateChange<int, string>.To(state + 100, $"fallback:{@event}"));
        };

        StateMachine<int, WorkflowEvent, string, string> applied = (state, _) =>
            TransitionResult<int, string, string>.Applied(
                StateChange<int, string>.To(state + 1, "applied"));
        StateMachine<int, WorkflowEvent, string, string> rejected = (_, _) =>
            TransitionResult<int, string, string>.Rejected("invalid-event");
        StateMachine<int, WorkflowEvent, string, string> failed = (_, _) =>
            TransitionResult<int, string, string>.Failed("store-down");
        StateMachine<int, WorkflowEvent, string, string> undefined = (_, _) =>
            TransitionResult<int, string, string>.Undefined();

        Assert.Equal(
            TransitionResult<int, string, string>.Applied(
                StateChange<int, string>.To(2, "applied")),
            applied.OrElse(fallback)(1, WorkflowEvent.Open));
        Assert.Equal(
            TransitionResult<int, string, string>.Rejected("invalid-event"),
            rejected.OrElse(fallback)(1, WorkflowEvent.Open));
        Assert.Equal(
            TransitionResult<int, string, string>.Failed("store-down"),
            failed.OrElse(fallback)(1, WorkflowEvent.Open));
        Assert.Equal(
            TransitionResult<int, string, string>.Applied(
                StateChange<int, string>.To(101, "fallback:Open")),
            undefined.OrElse(fallback)(1, WorkflowEvent.Open));
        Assert.Equal(1, fallbackCalls);
    }

    [Fact]
    public void ReplayAppliesEventsInOrderOnceAndCollectsOrderedOutputs()
    {
        var calls = new List<(int State, WorkflowEvent Event)>();
        StateMachine<int, WorkflowEvent, string, string> machine = (state, @event) =>
        {
            calls.Add((state, @event));
            return @event switch
            {
                WorkflowEvent.Open => TransitionResult<int, string, string>.Applied(
                    StateChange<int, string>.To(state + 1, "opened")),
                WorkflowEvent.Close => TransitionResult<int, string, string>.Applied(
                    StateChange<int, string>.To(state - 1, "closed")),
                _ => TransitionResult<int, string, string>.Undefined(),
            };
        };
        var history = new EnumeratedOnce<WorkflowEvent>([WorkflowEvent.Open, WorkflowEvent.Open, WorkflowEvent.Close]);

        var replay = machine.Replay(0, history);

        Assert.True(replay.IsApplied);
        Assert.True(replay.TryGetChange(out var change));
        Assert.Equal(1, change.State);
        Assert.Equal(["opened", "opened", "closed"], change.Outputs);
        Assert.Equal([(0, WorkflowEvent.Open), (1, WorkflowEvent.Open), (2, WorkflowEvent.Close)], calls);
        Assert.Equal(1, history.EnumerationCount);
    }

    [Fact]
    public void ReplayOfEmptyHistoryIsAppliedWithInitialStateAndNoOutputs()
    {
        var calls = 0;
        StateMachine<int, WorkflowEvent, string, string> machine = (_, _) =>
        {
            calls++;
            return TransitionResult<int, string, string>.Undefined();
        };

        var replay = machine.Replay(42, Array.Empty<WorkflowEvent>());

        Assert.Equal(
            TransitionResult<int, string, string>.Applied(
                StateChange<int, string>.To(42)),
            replay);
        Assert.Equal(0, calls);
    }

    [Fact]
    public void ReplayThreadsStateAcrossAppliedEventsWithoutOutputs()
    {
        StateMachine<int, WorkflowEvent, string, string> machine = (state, @event) => @event switch
        {
            WorkflowEvent.Open => TransitionResult<int, string, string>.Applied(
                StateChange<int, string>.To(state + 1)),
            WorkflowEvent.Close => TransitionResult<int, string, string>.Applied(
                StateChange<int, string>.To(state * 2, $"closed:{state}")),
            _ => TransitionResult<int, string, string>.Undefined(),
        };

        var replay = machine.Replay(3, [WorkflowEvent.Open, WorkflowEvent.Close]);

        Assert.True(replay.TryGetChange(out var change));
        Assert.Equal(8, change.State);
        Assert.Equal(["closed:4"], change.Outputs);
    }

    [Fact]
    public void ReplayStopsAtTheFirstNonAppliedTransitionAndPreservesItsStatusAndError()
    {
        var processed = new List<WorkflowEvent>();
        StateMachine<int, WorkflowEvent, string, string> rejectedMachine = (state, @event) =>
        {
            processed.Add(@event);
            return @event == WorkflowEvent.Reject
                ? TransitionResult<int, string, string>.Rejected("invalid-event")
                : TransitionResult<int, string, string>.Applied(
                    StateChange<int, string>.To(state + 1, "ok"));
        };
        var history = new EnumeratedOnce<WorkflowEvent>(
            [WorkflowEvent.Open, WorkflowEvent.Reject, WorkflowEvent.Close]);

        var rejected = rejectedMachine.Replay(0, history);

        Assert.Equal(TransitionStatus.Rejected, rejected.Status);
        Assert.True(rejected.TryGetError(out var rejectedError));
        Assert.Equal("invalid-event", rejectedError);
        Assert.Equal([WorkflowEvent.Open, WorkflowEvent.Reject], processed);
        Assert.Equal(1, history.EnumerationCount);

        StateMachine<int, WorkflowEvent, string, string> failedMachine = (_, @event) =>
            @event == WorkflowEvent.Fail
                ? TransitionResult<int, string, string>.Failed("store-down")
                : TransitionResult<int, string, string>.Applied(
                    StateChange<int, string>.To(1, "ok"));

        var failed = failedMachine.Replay(0, [WorkflowEvent.Fail, WorkflowEvent.Open]);

        Assert.Equal(TransitionStatus.Failed, failed.Status);
        Assert.True(failed.TryGetError(out var failedError));
        Assert.Equal("store-down", failedError);
    }

    [Fact]
    public void ReplayStopsAtUndefinedAndProducesDeterministicResults()
    {
        StateMachine<int, WorkflowEvent, string, string> machine = (state, @event) => @event switch
        {
            WorkflowEvent.Open => TransitionResult<int, string, string>.Applied(
                StateChange<int, string>.To(state + 1, "opened")),
            _ => TransitionResult<int, string, string>.Undefined(),
        };
        var history = new[] { WorkflowEvent.Open, WorkflowEvent.Unknown, WorkflowEvent.Open };

        var first = machine.Replay(0, history);
        var second = machine.Replay(0, history);

        Assert.Equal(TransitionStatus.Undefined, first.Status);
        Assert.Equal(first, second);
        Assert.False(first.TryGetChange(out _));
        Assert.False(first.TryGetError(out _));
    }

    [Fact]
    public void ReplayPreservesOrdinaryExceptionsByIdentity()
    {
        var transitionException = new InvalidOperationException("transition exploded");
        StateMachine<int, WorkflowEvent, string, string> throwingMachine = (_, _) => throw transitionException;
        var historyException = new InvalidOperationException("history exploded");

        var transitionActual = Assert.Throws<InvalidOperationException>(
            () => throwingMachine.Replay(0, [WorkflowEvent.Open]));
        var historyActual = Assert.Throws<InvalidOperationException>(
            () => ((StateMachine<int, WorkflowEvent, string, string>)((_, _) =>
                TransitionResult<int, string, string>.Undefined())).Replay(0, ThrowDuringEnumeration(historyException)));

        Assert.Same(transitionException, transitionActual);
        Assert.Same(historyException, historyActual);
    }

    [Fact]
    public void StateMachineExtensionsRejectNullInputs()
    {
        StateMachine<int, WorkflowEvent, string, string>? machine = null;
        StateMachine<int, WorkflowEvent, string, string> defined = (_, _) =>
            TransitionResult<int, string, string>.Undefined();

        Assert.Throws<ArgumentNullException>(() => machine!.OrElse(defined));
        Assert.Throws<ArgumentNullException>(() => defined.OrElse(null!));
        Assert.Throws<ArgumentNullException>(() => machine!.Replay(0, Array.Empty<WorkflowEvent>()));
        Assert.Throws<ArgumentNullException>(() => defined.Replay(0, null!));
    }

    [Fact]
    public async Task PureTransitionsDoNotExecuteOutputsAndAsyncExecutionReceivesTheCallerToken()
    {
        var outputCalls = 0;
        CancellationToken observedToken = default;
        Func<CancellationToken, ValueTask<string>> output = token =>
        {
            outputCalls++;
            observedToken = token;
            return ValueTask.FromResult("published");
        };
        StateMachine<int, WorkflowEvent, Func<CancellationToken, ValueTask<string>>, string> machine = (state, _) =>
            TransitionResult<int, Func<CancellationToken, ValueTask<string>>, string>.Applied(
                StateChange<int, Func<CancellationToken, ValueTask<string>>>.To(state + 1, output));
        using var cancellationSource = new CancellationTokenSource();

        var transition = machine(0, WorkflowEvent.Open);

        Assert.Equal(0, outputCalls);
        Assert.True(transition.TryGetChange(out var change));
        Assert.Equal(1, change.State);
        var emittedOutput = Assert.Single(change.Outputs);
        Assert.Equal("published", await emittedOutput(cancellationSource.Token));
        Assert.Equal(1, outputCalls);
        Assert.Equal(cancellationSource.Token, observedToken);
    }

    [Fact]
    public async Task OutputCancellationRemainsOutsideThePureTransitionCore()
    {
        Func<CancellationToken, ValueTask> output = ValueTask.FromCanceled;
        StateMachine<int, WorkflowEvent, Func<CancellationToken, ValueTask>, string> machine = (state, _) =>
            TransitionResult<int, Func<CancellationToken, ValueTask>, string>.Applied(
                StateChange<int, Func<CancellationToken, ValueTask>>.To(state + 1, output));
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        var transition = machine(0, WorkflowEvent.Open);

        Assert.True(transition.IsApplied);
        Assert.True(transition.TryGetChange(out var change));
        var operation = Assert.Single(change.Outputs)(cancellationSource.Token);
        var actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await operation);
        Assert.True(operation.IsCanceled);
        Assert.Equal(cancellationSource.Token, actual.CancellationToken);
    }

    private static IEnumerable<WorkflowEvent> ThrowDuringEnumeration(Exception exception)
    {
        throw exception;
#pragma warning disable CS0162
        yield break;
#pragma warning restore CS0162
    }

    private enum WorkflowEvent
    {
        Open,
        Close,
        Reject,
        Fail,
        Unknown,
    }

    private sealed class EnumeratedOnce<T>(IReadOnlyList<T> values) : IEnumerable<T>
    {
        public int EnumerationCount { get; private set; }

        public IEnumerator<T> GetEnumerator()
        {
            EnumerationCount++;
            if (EnumerationCount > 1)
            {
                throw new InvalidOperationException("The history was enumerated more than once.");
            }

            return values.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
