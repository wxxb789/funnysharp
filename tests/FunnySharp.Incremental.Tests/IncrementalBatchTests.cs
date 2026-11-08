using FunnySharp.Incremental;

namespace FunnySharp.Incremental.Tests;

public sealed class IncrementalBatchTests
{
    [Fact]
    public void BatchPublishesAllInputsBeforeObserversSeeTheResult()
    {
        var graph = new IncrementalGraph();
        var left = graph.Input("left", 1);
        var right = graph.Input("right", 2);
        var sum = graph.Combine(
            "sum",
            left,
            right,
            Pure<(int First, int Second), int>.Of(static pair => pair.First + pair.Second));
        var observed = new List<int>();
        using var registration = graph.ObserveChanges(sum, observed.Add);

        graph.Batch(() =>
        {
            left.Set(10);
            right.Set(20);
            Assert.Equal(3, sum.Value);
        });

        Assert.Equal(30, sum.Value);
        Assert.Equal(new[] { 30 }, observed);
        Assert.Equal(1L, graph.Statistics.Commits);
        Assert.Equal(1L, graph.Statistics.NodeEvaluations);
    }

    [Fact]
    public void BatchRecomputesASharedDependentOnce()
    {
        var graph = new IncrementalGraph();
        var left = graph.Input("left", 1);
        var right = graph.Input("right", 2);
        var calls = 0;
        var sum = graph.Combine(
            "sum",
            left,
            right,
            Pure<(int First, int Second), int>.Of(pair =>
            {
                calls++;
                return pair.First + pair.Second;
            }));

        graph.ResetStatistics();
        graph.Batch(() =>
        {
            left.Set(10);
            right.Set(20);
        });

        Assert.Equal(30, sum.Value);
        Assert.Equal(2, calls);
        Assert.Equal(1L, graph.Statistics.NodeEvaluations);
        Assert.Equal(2L, graph.Statistics.InputChanges);
    }

    [Fact]
    public void NestedBatchesCommitOnlyAtTheOutermostSuccessfulBoundary()
    {
        var graph = new IncrementalGraph();
        var left = graph.Input("left", 1);
        var right = graph.Input("right", 2);
        var observed = 0;
        using var registration = graph.ObserveChanges(left, _ => observed++);

        graph.Batch(() =>
        {
            left.Set(5);
            graph.Batch(() =>
            {
                right.Set(6);
                Assert.Equal(0, observed);
            });

            Assert.Equal(0, observed);
            Assert.Equal(1, left.Value);
        });

        Assert.Equal(5, left.Value);
        Assert.Equal(1, observed);
        Assert.Equal(1L, graph.Statistics.Commits);
    }

    [Fact]
    public void ThrowingBatchDiscardsStagedInputsAndRunsNoObserver()
    {
        var graph = new IncrementalGraph();
        var input = graph.Input("input", 1);
        var observed = 0;
        using var registration = graph.ObserveChanges(input, _ => observed++);

        Assert.Throws<InvalidOperationException>(() =>
            graph.Batch(() =>
            {
                input.Set(2);
                throw new InvalidOperationException("abort");
            }));

        Assert.Equal(1, input.Value);
        Assert.Equal(0, observed);
        Assert.Equal(0L, graph.Statistics.Commits);

        input.Set(3);

        Assert.Equal(3, input.Value);
        Assert.Equal(1, observed);
    }

    [Fact]
    public void NestedBatchFailureDiscardsTheOuterStagedValues()
    {
        var graph = new IncrementalGraph();
        var left = graph.Input("left", 1);
        var right = graph.Input("right", 2);

        Assert.Throws<InvalidOperationException>(() =>
            graph.Batch(() =>
            {
                left.Set(5);
                graph.Batch(() =>
                {
                    right.Set(6);
                    throw new InvalidOperationException("inner abort");
                });
            }));

        Assert.Equal(1, left.Value);
        Assert.Equal(2, right.Value);
    }

    [Fact]
    public void NoOpBatchDoesNotCommitOrNotify()
    {
        var graph = new IncrementalGraph();
        var input = graph.Input("input", 1);
        var observed = 0;
        using var registration = graph.ObserveChanges(input, _ => observed++);

        graph.Batch(() => input.Set(1));

        Assert.Equal(0, observed);
        Assert.Equal(0L, graph.Statistics.Commits);
    }

    [Fact]
    public void ObserverCannotPublishDuringCommit()
    {
        var graph = new IncrementalGraph();
        var input = graph.Input("input", 1);
        using var registration = graph.ObserveChanges(input, _ => input.Set(99));

        Assert.Throws<InvalidOperationException>(() => input.Set(2));
    }

    [Fact]
    public void PureComputationCannotPublishDuringCommit()
    {
        var graph = new IncrementalGraph();
        var input = graph.Input("input", 0);
        var mutate = false;
        _ = graph.Map(
            "mutating",
            input,
            Pure<int, int>.Of(value =>
            {
                if (mutate)
                {
                    input.Set(value + 1);
                }

                return value;
            }));

        mutate = true;

        Assert.Throws<InvalidOperationException>(() => input.Set(2));
    }

    [Fact]
    public void ObserverRegistrationCannotHappenDuringCommit()
    {
        var graph = new IncrementalGraph();
        var input = graph.Input("input", 1);
        using var registration = graph.ObserveChanges(input, _ => graph.ObserveChanges(input, _ => { }));

        Assert.Throws<InvalidOperationException>(() => input.Set(2));
    }
}