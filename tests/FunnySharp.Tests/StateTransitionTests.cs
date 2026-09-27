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
    public void ThenThreadsStateWhenOnlyTheFirstTransitionProducesOutputs()
    {
        StateTransition<int, string> doubleAndEmit = state =>
            StateChange<int, string>.To(state * 2, $"doubled:{state}");
        StateTransition<int, string> increment = state => StateChange<int, string>.To(state + 1);

        var result = doubleAndEmit.Then(increment)(3);

        Assert.Equal(7, result.State);
        Assert.Equal(["doubled:3"], result.Outputs);
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

    [Fact]
    public void ThenChainsEvaluateEveryTransitionOnceInOrderAcrossLongLeftAssociatedChains()
    {
        const int chainLength = 10_000;
        StateTransition<int, int> transition = state =>
            StateChange<int, int>.To(state + 1, 0);

        for (var index = 1; index < chainLength; index++)
        {
            var step = index;
            transition = transition.Then(state => StateChange<int, int>.To(state + 1, step));
        }

        var result = transition(0);

        Assert.Equal(chainLength, result.State);
        Assert.Equal(Enumerable.Range(0, chainLength), result.Outputs);
    }

    [Fact]
    public void ThenChainsEvaluateRightAssociatedAndMixedChainsInExecutionOrder()
    {
        StateTransition<int, string> a = state => StateChange<int, string>.To(state + 1, "a");
        StateTransition<int, string> b = state => StateChange<int, string>.To(state + 1, "b");
        StateTransition<int, string> c = state => StateChange<int, string>.To(state + 1, "c");

        var leftAssociated = a.Then(b).Then(c);
        var rightAssociated = a.Then(b.Then(c));
        var mixed = a.Then(b).Then(a.Then(b).Then(c)).Then(c);

        var left = leftAssociated(0);
        var right = rightAssociated(0);
        var chained = mixed(0);

        Assert.Equal(3, left.State);
        Assert.Equal(["a", "b", "c"], left.Outputs);
        Assert.Equal(left.State, right.State);
        Assert.Equal(left.Outputs, right.Outputs);
        Assert.Equal(6, chained.State);
        Assert.Equal(["a", "b", "a", "b", "c", "c"], chained.Outputs);
    }

    [Fact]
    public void ThenCompositionIsImmutableWhenPreviouslyComposedTransitionsAreExtended()
    {
        var bCalls = 0;
        var cCalls = 0;
        StateTransition<int, string> a = state => StateChange<int, string>.To(state + 1, "a");
        StateTransition<int, string> b = state =>
        {
            bCalls++;
            return StateChange<int, string>.To(state + 1, "b");
        };
        StateTransition<int, string> c = state =>
        {
            cCalls++;
            return StateChange<int, string>.To(state + 1, "c");
        };

        var ab = a.Then(b);
        var abc = ab.Then(c);
        var abResult = ab(0);
        var abcResult = abc(0);

        Assert.Equal(2, abResult.State);
        Assert.Equal(["a", "b"], abResult.Outputs);
        Assert.Equal(3, abcResult.State);
        Assert.Equal(["a", "b", "c"], abcResult.Outputs);
        Assert.Equal(2, bCalls);
        Assert.Equal(1, cCalls);
    }

    [Fact]
    public void ThenCompositionProducesEqualResultsAcrossRepeatedInvocations()
    {
        StateTransition<int, int> transition = state => StateChange<int, int>.To(state + 1, state + 1);
        for (var index = 1; index < 64; index++)
        {
            transition = transition.Then(state => StateChange<int, int>.To(state + 1, state + 1));
        }

        var expected = transition(0);
        for (var invocation = 0; invocation < 100; invocation++)
        {
            Assert.Equal(expected, transition(0));
        }
    }

    [Fact]
    public async Task ThenCompositionSupportsConcurrentInvocation()
    {
        StateTransition<int, int> transition = state => StateChange<int, int>.To(state + 1, state + 1);
        for (var index = 1; index < 256; index++)
        {
            transition = transition.Then(state => StateChange<int, int>.To(state + 1, state + 1));
        }

        var expected = transition(0);
        var results = await Task.WhenAll(
            Enumerable.Range(0, Environment.ProcessorCount * 2)
                .Select(_ => Task.Run(() => transition(0))));

        Assert.All(results, result =>
        {
            Assert.Equal(expected.State, result.State);
            Assert.Equal(expected.Outputs, result.Outputs);
        });
    }

    [Fact]
    public void ThenChainsRejectNullStateChangesFromTheMiddleOfTheChain()
    {
        var executed = new List<int>();
        StateTransition<int, string> valid(int step) => state =>
        {
            executed.Add(step);
            return StateChange<int, string>.To(state + 1, $"step:{step}");
        };

        StateTransition<int, string> chain = valid(0).Then(valid(1));
        StateTransition<int, string> nullMiddle = _ => null!;
        var composed = chain.Then(nullMiddle).Then(valid(2));

        Assert.Throws<InvalidOperationException>(() => composed(0));
        Assert.Equal([0, 1], executed);
    }

    [Fact]
    public void ThenChainsPreserveExceptionIdentityFromTheMiddleOfTheChain()
    {
        var executed = new List<int>();
        var exception = new InvalidOperationException("middle failed");
        StateTransition<int, string> valid(int step) => state =>
        {
            executed.Add(step);
            return StateChange<int, string>.To(state + 1, $"step:{step}");
        };
        StateTransition<int, string> failing = _ => throw exception;

        var composed = valid(0).Then(valid(1)).Then(failing).Then(valid(2));
        var actual = Assert.Throws<InvalidOperationException>(() => composed(0));

        Assert.Same(exception, actual);
        Assert.Equal([0, 1], executed);
    }

    [Fact]
    public void ThenChainsConcatenateMixedOutputCountsInExecutionOrder()
    {
        StateTransition<int, int> none = state => StateChange<int, int>.To(state + 1);
        StateTransition<int, int> single = state => StateChange<int, int>.To(state + 1, state + 1);
        StateTransition<int, int> triple = state => StateChange<int, int>.To(
            state + 1, state + 1, state + 10, state + 100);

        var composed = none.Then(single).Then(none).Then(triple).Then(none);
        var result = composed(0);

        Assert.Equal(5, result.State);
        Assert.Equal([2, 4, 13, 103], result.Outputs);
    }

    [Fact]
    public void ThenPreservesEveryInvocationListEntryOfMulticastTransitions()
    {
        var executed = new List<string>();
        StateTransition<int, string> track(string name) => state =>
        {
            executed.Add($"{name}:{state}");
            return StateChange<int, string>.To(state + 1, $"{name}:{state}");
        };

        var composed = track("composed-a").Then(track("composed-b"));

        StateTransition<int, string> leadingMulticast = track("leading") + composed;
        var result = leadingMulticast.Then(track("trailing"))(0);

        Assert.Equal(["leading:0", "composed-a:0", "composed-b:1", "trailing:2"], executed);
        Assert.Equal(3, result.State);
        Assert.Equal(["composed-a:0", "composed-b:1", "trailing:2"], result.Outputs);

        executed.Clear();

        StateTransition<int, string> trailingMulticast = composed + track("following");
        var reverseResult = trailingMulticast.Then(track("trailing"))(0);

        Assert.Equal(["composed-a:0", "composed-b:1", "following:0", "trailing:1"], executed);
        Assert.Equal(2, reverseResult.State);
        Assert.Equal(["following:0", "trailing:1"], reverseResult.Outputs);
    }
}
