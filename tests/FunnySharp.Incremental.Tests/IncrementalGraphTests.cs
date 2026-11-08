using FunnySharp;
using FunnySharp.Incremental;

namespace FunnySharp.Incremental.Tests;

public sealed class IncrementalGraphTests
{
    [Fact]
    public void MapCombineAndCollectPublishInitialValuesAndRecomputeAfterAnInputChange()
    {
        var graph = new IncrementalGraph();
        var left = graph.Input("left", 2);
        var right = graph.Input("right", 3);
        var sum = graph.Combine(
            "sum",
            left,
            right,
            Pure<(int First, int Second), int>.Of(static pair => pair.First + pair.Second));
        var doubled = graph.Map(
            "doubled",
            sum,
            Pure<int, int>.Of(static value => value * 2));
        var collected = graph.Collect("collected", [left, right]);

        Assert.Equal(5, sum.Value);
        Assert.Equal(10, doubled.Value);
        Assert.Equal(new[] { 2, 3 }, collected.Value);

        left.Set(4);

        Assert.Equal(7, sum.Value);
        Assert.Equal(14, doubled.Value);
        Assert.Equal(new[] { 4, 3 }, collected.Value);
    }

    [Fact]
    public void OutputEqualityCutoffStopsDownstreamRecomputation()
    {
        var graph = new IncrementalGraph();
        var source = graph.Input("source", 1);
        var parity = graph.Map(
            "parity",
            source,
            Pure<int, bool>.Of(static value => value % 2 == 0));
        var calls = 0;
        var downstream = graph.Map(
            "downstream",
            parity,
            Pure<bool, int>.Of(value =>
            {
                calls++;
                return value ? 1 : 0;
            }));

        Assert.Equal(0, downstream.Value);
        Assert.Equal(1, calls);

        graph.ResetStatistics();
        source.Set(2);

        Assert.Equal(1, downstream.Value);
        Assert.Equal(2, calls);

        graph.ResetStatistics();
        source.Set(4);

        Assert.Equal(1, downstream.Value);
        Assert.Equal(2, calls);
        Assert.Equal(1L, graph.Statistics.CutoffStops);
        Assert.Equal(1L, graph.Statistics.NodeEvaluations);
    }

    [Fact]
    public void InputEqualitySkipsPublicationAndSetChangedForcesIt()
    {
        var graph = new IncrementalGraph();
        var source = graph.Input("source", 5);
        var calls = 0;
        var mapped = graph.Map(
            "mapped",
            source,
            Pure<int, int>.Of(value =>
            {
                calls++;
                return value;
            }));

        graph.ResetStatistics();
        source.Set(5);

        Assert.Equal(0L, graph.Statistics.Commits);
        Assert.Equal(0, graph.Statistics.NodeEvaluations);
        Assert.Equal(1, calls);

        source.SetChanged(5);

        Assert.Equal(1L, graph.Statistics.Commits);
        Assert.Equal(1L, graph.Statistics.NodeEvaluations);
        Assert.Equal(2, calls);
        Assert.Equal(5, mapped.Value);
    }

    [Fact]
    public void CustomInputComparerControlsPublication()
    {
        var graph = new IncrementalGraph();
        var source = graph.Input("source", "A", StringComparer.OrdinalIgnoreCase);
        var calls = 0;
        _ = graph.Map(
            "length",
            source,
            Pure<string, int>.Of(value =>
            {
                calls++;
                return value.Length;
            }));

        graph.ResetStatistics();
        source.Set("a");

        Assert.Equal(0L, graph.Statistics.Commits);
        Assert.Equal(1, calls);

        source.Set("b");

        Assert.Equal(1L, graph.Statistics.Commits);
        Assert.Equal(2, calls);
    }

    [Fact]
    public void MutableInputRequiresSetChangedToCommunicateLocalMutation()
    {
        var graph = new IncrementalGraph();
        var values = new List<int> { 1, 2 };
        var source = graph.Input("values", values);
        var count = graph.Map(
            "count",
            source,
            Pure<List<int>, int>.Of(static list => list.Count));

        Assert.Equal(2, count.Value);

        values.Add(3);
        graph.ResetStatistics();
        source.Set(values);

        Assert.Equal(2, count.Value);
        Assert.Equal(0L, graph.Statistics.Commits);

        source.SetChanged(values);

        Assert.Equal(3, count.Value);
        Assert.Equal(1L, graph.Statistics.Commits);
    }

    [Fact]
    public void ResultAndValidationRemainOrdinaryNodeValues()
    {
        var graph = new IncrementalGraph();
        var result = graph.Input("result", Result<int, string>.Success(4));
        var validation = graph.Map(
            "validation",
            result,
            Pure<Result<int, string>, Validation<int, string>>.Of(
                value => value.TryGetValue(out var number)
                    ? Validation<int, string>.Valid(number * 2)
                    : Validation<int, string>.Invalid(value.TryGetError(out var error) ? error! : "unknown")));

        Assert.True(validation.Value.TryGetValue(out var valid));
        Assert.Equal(8, valid);

        result.Set(Result<int, string>.Failure("bad"));

        Assert.True(validation.Value.TryGetErrors(out var errors));
        Assert.Equal(new[] { "bad" }, errors);
    }

    [Fact]
    public void NullAndForeignGraphSourcesAreRejectedAtConstruction()
    {
        var graph = new IncrementalGraph();
        var other = new IncrementalGraph();
        var source = graph.Input("source", 1);
        var foreign = other.Input("foreign", 2);
        var compute = Pure<int, int>.Of(static value => value);

        Assert.Throws<ArgumentNullException>(() => graph.Map<int, int>("map", null!, compute));
        Assert.Throws<ArgumentException>(() => graph.Map("map", foreign, compute));
        Assert.Throws<ArgumentException>(() => graph.Collect("collect", Array.Empty<IncrementalNode<int>>()));
        Assert.Throws<ArgumentException>(() =>
            graph.Map("map", source, default(Pure<int, int>)));
        Assert.ThrowsAny<ArgumentException>(() => graph.Input<int>(" ", 1));
    }

    [Fact]
    public void CallbackExceptionPropagatesWithoutRunningObservers()
    {
        var graph = new IncrementalGraph();
        var input = graph.Input("input", 0);
        var shouldThrow = false;
        var node = graph.Map(
            "fault",
            input,
            Pure<int, int>.Of(value =>
            {
                if (shouldThrow)
                {
                    throw new InvalidOperationException("fault");
                }

                return value;
            }));
        var observed = 0;
        using var registration = graph.ObserveChanges(node, _ => observed++);

        shouldThrow = true;

        Assert.Throws<InvalidOperationException>(() => input.Set(1));
        Assert.Equal(0, observed);
    }
}