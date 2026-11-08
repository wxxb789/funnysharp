using FunnySharp.Incremental;

namespace FunnySharp.Incremental.Tests;

public sealed class IncrementalObserverTests
{
    [Fact]
    public void ObserveInvokesImmediatelyWhileObserveChangesDoesNot()
    {
        var graph = new IncrementalGraph();
        var input = graph.Input("input", 1);
        var immediate = new List<int>();
        var changes = new List<int>();
        using var first = graph.Observe(input, immediate.Add);
        using var second = graph.ObserveChanges(input, changes.Add);

        Assert.Equal(new[] { 1 }, immediate);
        Assert.Empty(changes);

        input.Set(2);

        Assert.Equal(new[] { 1, 2 }, immediate);
        Assert.Equal(new[] { 2 }, changes);
    }

    [Fact]
    public void DisposeStopsNotificationsAndIsIdempotent()
    {
        var graph = new IncrementalGraph();
        var input = graph.Input("input", 1);
        var observed = 0;
        var registration = graph.ObserveChanges(input, _ => observed++);

        registration.Dispose();
        registration.Dispose();
        input.Set(2);

        Assert.Equal(0, observed);
        Assert.Equal(0, graph.ObserverCount);
    }

    [Fact]
    public void DisposingOneObserverDoesNotDisturbAnother()
    {
        var graph = new IncrementalGraph();
        var input = graph.Input("input", 1);
        var firstObserved = 0;
        var secondObserved = 0;
        var first = graph.ObserveChanges(input, _ => firstObserved++);
        using var second = graph.ObserveChanges(input, _ => secondObserved++);

        first.Dispose();
        input.Set(2);

        Assert.Equal(0, firstObserved);
        Assert.Equal(1, secondObserved);
        Assert.Equal(1, graph.ObserverCount);
    }

    [Fact]
    public void DisposingAnObserverDuringNotificationSkipsItsPendingCallback()
    {
        var graph = new IncrementalGraph();
        var input = graph.Input("input", 1);
        IDisposable? second = null;
        var secondObserved = 0;
        using var first = graph.ObserveChanges(input, _ => second!.Dispose());
        second = graph.ObserveChanges(input, _ => secondObserved++);

        input.Set(2);

        Assert.Equal(0, secondObserved);
        Assert.Equal(1, graph.ObserverCount);
    }

    [Fact]
    public void ObserverFaultPropagatesAndStopsLaterObservers()
    {
        var graph = new IncrementalGraph();
        var input = graph.Input("input", 1);
        var secondObserved = 0;
        using var first = graph.ObserveChanges(input, _ => throw new InvalidOperationException("observer"));
        using var second = graph.ObserveChanges(input, _ => secondObserved++);

        Assert.Throws<InvalidOperationException>(() => input.Set(2));
        Assert.Equal(0, secondObserved);
    }

    [Fact]
    public void ImmediateObserverFaultDoesNotLeaveARegistration()
    {
        var graph = new IncrementalGraph();
        var input = graph.Input("input", 1);

        Assert.Throws<InvalidOperationException>(() =>
            graph.Observe(input, _ => throw new InvalidOperationException("immediate")));

        Assert.Equal(0, graph.ObserverCount);
    }

    [Fact]
    public void TwoGraphsFromTheSamePureDefinitionKeepIndependentState()
    {
        var compute = Pure<int, int>.Of(static value => value * 2);
        var firstGraph = new IncrementalGraph();
        var secondGraph = new IncrementalGraph();
        var firstInput = firstGraph.Input("input", 1);
        var secondInput = secondGraph.Input("input", 1);
        var firstNode = firstGraph.Map("mapped", firstInput, compute);
        var secondNode = secondGraph.Map("mapped", secondInput, compute);

        firstInput.Set(3);

        Assert.Equal(6, firstNode.Value);
        Assert.Equal(2, secondNode.Value);
        Assert.Equal(2, firstGraph.NodeCount);
        Assert.Equal(2, secondGraph.NodeCount);
    }

    [Fact]
    public void RepeatedUpdatesDoNotGrowTheGraphOrObservers()
    {
        var graph = new IncrementalGraph();
        var input = graph.Input("input", 0);
        var node = graph.Map("mapped", input, Pure<int, int>.Of(static value => value + 1));
        var observed = 0;
        using var registration = graph.ObserveChanges(node, _ => observed++);

        for (var index = 1; index <= 100; index++)
        {
            input.Set(index);
        }

        Assert.Equal(2, graph.NodeCount);
        Assert.Equal(1, graph.EdgeCount);
        Assert.Equal(1, graph.ObserverCount);
        Assert.Equal(100, observed);
        Assert.Equal(100L, graph.Statistics.Commits);
    }
}