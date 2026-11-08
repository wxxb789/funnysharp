using FunnySharp.Incremental;

namespace FunnySharp.Incremental.Tests;

public sealed class IncrementalCutoffTests
{
    [Fact]
    public void CustomOutputComparerStopsDownstreamWhenValuesAreEquivalent()
    {
        var graph = new IncrementalGraph();
        var source = graph.Input("source", "a");
        var normalized = graph.Map(
            "normalized",
            source,
            Pure<string, string>.Of(static value => value),
            StringComparer.OrdinalIgnoreCase);
        var calls = 0;
        var downstream = graph.Map(
            "downstream",
            normalized,
            Pure<string, int>.Of(value =>
            {
                calls++;
                return value.Length;
            }));

        graph.ResetStatistics();
        source.Set("A");

        Assert.Equal(1, downstream.Value);
        Assert.Equal(1, calls);
        Assert.Equal(1L, graph.Statistics.CutoffStops);
        Assert.Equal(1L, graph.Statistics.NodeEvaluations);
    }

    [Fact]
    public void BatchRoundTripToThePublishedValueDoesNotCommit()
    {
        var graph = new IncrementalGraph();
        var input = graph.Input("input", 1);
        var observed = 0;
        using var registration = graph.ObserveChanges(input, _ => observed++);

        graph.Batch(() =>
        {
            input.Set(2);
            input.Set(1);
        });

        Assert.Equal(1, input.Value);
        Assert.Equal(0, observed);
        Assert.Equal(0L, graph.Statistics.Commits);
        Assert.Equal(0L, graph.Statistics.NodeEvaluations);
    }

    [Fact]
    public void SetChangedAfterSetWithinABatchPublishesThePendingValue()
    {
        var graph = new IncrementalGraph();
        var input = graph.Input("input", 1);
        var observed = 0;
        using var registration = graph.ObserveChanges(input, _ => observed++);

        graph.Batch(() =>
        {
            input.Set(2);
            input.SetChanged(1);
        });

        Assert.Equal(1, input.Value);
        Assert.Equal(1, observed);
        Assert.Equal(1L, graph.Statistics.Commits);
    }

    [Fact]
    public void UnchangedOutputDoesNotNotifyDownstreamObserver()
    {
        var graph = new IncrementalGraph();
        var source = graph.Input("source", 1);
        var parity = graph.Map(
            "parity",
            source,
            Pure<int, bool>.Of(static value => value % 2 == 0));
        var observed = new List<bool>();
        using var registration = graph.ObserveChanges(parity, observed.Add);

        source.Set(3);

        Assert.Empty(observed);
    }
}