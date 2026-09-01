namespace FunnySharp.Tests;

public sealed class StateTransitionTests
{
    [Fact]
    public void ToPreservesStateAndSnapshotsOutputs()
    {
        var source = new[] { "created", "queued" };

        var change = StateChange<int, string>.To(42, source);
        source[0] = "changed";
        source[1] = "removed";

        Assert.Equal(42, change.State);
        Assert.Equal(["created", "queued"], change.Outputs);
    }

    [Fact]
    public void StateChangesUseStructuralEqualityAndHashing()
    {
        var first = StateChange<int, string>.To(3, "created", "queued");
        var same = StateChange<int, string>.To(3, "created", "queued");
        var differentState = StateChange<int, string>.To(4, "created", "queued");
        var differentOutputs = StateChange<int, string>.To(3, "queued", "created");

        Assert.True(typeof(StateChange<int, string>).IsSealed);
        Assert.Equal(first, same);
        Assert.Equal(first.GetHashCode(), same.GetHashCode());
        Assert.NotEqual(first, differentState);
        Assert.NotEqual(first, differentOutputs);
    }

    [Fact]
    public void ToRejectsNullOutputArrays()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = StateChange<int, string>.To(0, null!);
        });
    }

    [Fact]
    public void ThenThreadsStateAndConcatenatesOutputsInExecutionOrder()
    {
        var calls = new List<string>();
        StateTransition<int, string> first = state =>
        {
            calls.Add($"first:{state}");
            return StateChange<int, string>.To(state + 1, $"incremented:{state}");
        };
        StateTransition<int, string> second = state =>
        {
            calls.Add($"second:{state}");
            return StateChange<int, string>.To(state * 2, $"doubled:{state}");
        };

        var result = first.Then(second)(3);

        Assert.Equal(8, result.State);
        Assert.Equal(["incremented:3", "doubled:4"], result.Outputs);
        Assert.Equal(["first:3", "second:4"], calls);
    }

    [Fact]
    public void ThenIsDeterministicForDeterministicTransitions()
    {
        StateTransition<int, string> first = state =>
            StateChange<int, string>.To(state + 2, $"add-two:{state}");
        StateTransition<int, string> second = state =>
            StateChange<int, string>.To(state * 3, $"triple:{state}");

        var transition = first.Then(second);

        Assert.Equal(transition(5), transition(5));
    }

    [Fact]
    public void ThenThreadsStateAcrossTransitionsWithoutOutputs()
    {
        StateTransition<int, string> increment = state => StateChange<int, string>.To(state + 1);
        StateTransition<int, string> doubleAndEmit = state =>
            StateChange<int, string>.To(state * 2, $"doubled:{state}");

        var result = increment.Then(doubleAndEmit)(3);

        Assert.Equal(8, result.State);
        Assert.Equal(["doubled:4"], result.Outputs);
    }

    [Fact]
    public void ThenRejectsNullDelegatesEagerly()
    {
        StateTransition<int, string> transition = state => StateChange<int, string>.To(state);

        Assert.Throws<ArgumentNullException>(() => ((StateTransition<int, string>)null!).Then(transition));
        Assert.Throws<ArgumentNullException>(() => transition.Then(null!));
    }

    [Fact]
    public void ThenPreservesExceptionIdentityAndShortCircuitsAfterFirstFailure()
    {
        var firstException = new InvalidOperationException("first failed");
        var secondCalled = false;
        StateTransition<int, string> failingFirst = _ => throw firstException;
        StateTransition<int, string> skippedSecond = state =>
        {
            secondCalled = true;
            return StateChange<int, string>.To(state, "unexpected");
        };

        var firstActual = Assert.Throws<InvalidOperationException>(() => failingFirst.Then(skippedSecond)(1));

        Assert.Same(firstException, firstActual);
        Assert.False(secondCalled);

        var secondException = new InvalidOperationException("second failed");
        StateTransition<int, string> succeedingFirst = state => StateChange<int, string>.To(state + 1, "first");
        StateTransition<int, string> failingSecond = _ => throw secondException;

        var secondActual = Assert.Throws<InvalidOperationException>(() => succeedingFirst.Then(failingSecond)(1));

        Assert.Same(secondException, secondActual);
    }

    [Fact]
    public void ThenRejectsNullStateChangesFromEitherTransition()
    {
        var secondCalled = false;
        StateTransition<int, string> nullFirst = _ => null!;
        StateTransition<int, string> trackedSecond = state =>
        {
            secondCalled = true;
            return StateChange<int, string>.To(state, "unexpected");
        };

        Assert.Throws<InvalidOperationException>(() => nullFirst.Then(trackedSecond)(1));
        Assert.False(secondCalled);

        StateTransition<int, string> validFirst = state => StateChange<int, string>.To(state + 1, "first");
        StateTransition<int, string> nullSecond = _ => null!;

        Assert.Throws<InvalidOperationException>(() => validFirst.Then(nullSecond)(1));
    }
}
