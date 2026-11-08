using FunnySharp.TestSupport;
using FunnySharp.TestSupport.FallibleReduction;

namespace FunnySharp.Tests;

public sealed class NestedDataExpansionTests
{
    [Fact]
    public void EmptyRecordSourceProducesAnEmptySuccess()
    {
        var result = Array.Empty<ExpansionRecord>().Expand(
            static record => record.Children,
            static (record, child, origin) => $"{record.Id}:{child}:{origin.ElementIndex}");

        Assert.True(result.TryGetValue(out var rows));
        Assert.Empty(rows!);
    }

    [Fact]
    public void EmptyChildListProducesNoRowsAndNoError()
    {
        var records = new[]
        {
            new ExpansionRecord("A", []),
            new ExpansionRecord("B", ["x"]),
        };

        var result = records.Expand(
            static record => record.Children,
            static (record, child, _) => $"{record.Id}:{child}");

        Assert.True(result.TryGetValue(out var rows));
        Assert.Equal(["B:x"], rows!);
    }

    [Fact]
    public void ExpandYieldsOneRowPerChildInRecordThenChildOrder()
    {
        var records = new[]
        {
            new ExpansionRecord("A", ["a1", "a2", "a3"]),
            new ExpansionRecord("B", []),
            new ExpansionRecord("C", ["c1"]),
        };
        var observed = new List<RowOrigin>();

        var result = records.Expand(
            static record => record.Children,
            (record, child, origin) =>
            {
                observed.Add(origin);
                return $"{record.Id}:{child}";
            });

        Assert.True(result.TryGetValue(out var rows));
        Assert.Equal(["A:a1", "A:a2", "A:a3", "C:c1"], rows!);
        Assert.Equal(new[] { 0, 0, 0, 2 }, observed.Select(static origin => origin.RecordIndex));
        Assert.Equal(new[] { 0, 1, 2, 0 }, observed.Select(static origin => origin.ElementIndex));
    }

    [Fact]
    public void ExpandRetainsParentContextForEveryChild()
    {
        var records = new[]
        {
            new ExpansionRecord("A", ["x", "y"]),
        };

        var result = records.Expand(
            static record => record.Children,
            static (record, child, _) => new ExpandedRow(record.Id, child));

        Assert.True(result.TryGetValue(out var rows));
        Assert.Equal(
            [new ExpandedRow("A", "x"), new ExpandedRow("A", "y")],
            rows!);
    }

    [Fact]
    public void NullChildListIsAStructuredFailureWithTheRecordPosition()
    {
        var records = new[]
        {
            new ExpansionRecord("A", ["x"]),
            new ExpansionRecord("B", null!),
        };

        var result = records.Expand(
            static record => record.Children,
            static (record, child, _) => $"{record.Id}:{child}");

        Assert.True(result.TryGetErrors(out var errors));
        Assert.Equal([NestedRowError.NullChildSequence(1)], errors!);
    }

    [Fact]
    public void RecordsAfterTheFirstErrorAreStillCheckedForNullChildren()
    {
        var records = new[]
        {
            new ExpansionRecord("A", null!),
            new ExpansionRecord("B", ["b1"]),
            new ExpansionRecord("C", null!),
        };
        var combines = 0;

        var result = records.Expand(
            static record => record.Children,
            (record, child, _) =>
            {
                combines++;
                return $"{record.Id}:{child}";
            });

        Assert.True(result.TryGetErrors(out var errors));
        Assert.Equal(
            [NestedRowError.NullChildSequence(0), NestedRowError.NullChildSequence(2)],
            errors!);
        Assert.Equal(0, combines);
    }

    [Fact]
    public void CombineStopsAfterTheFirstRecordedErrorAndEarlierRowsAreDiscarded()
    {
        var records = new[]
        {
            new ExpansionRecord("A", ["a1"]),
            new ExpansionRecord("B", null!),
            new ExpansionRecord("C", ["c1"]),
        };
        var combines = 0;

        var result = records.Expand(
            static record => record.Children,
            (record, child, _) =>
            {
                combines++;
                return $"{record.Id}:{child}";
            });

        Assert.True(result.TryGetErrors(out var errors));
        Assert.Equal([NestedRowError.NullChildSequence(1)], errors!);
        Assert.Equal(1, combines);
    }

    [Fact]
    public void ExpandEnumeratesTheRecordSourceOnce()
    {
        var records = new ReductionProbe<ExpansionRecord>(
        [
            new ExpansionRecord("A", ["a1", "a2"]),
            new ExpansionRecord("B", ["b1"]),
        ]);

        var result = records.Expand(
            static record => record.Children,
            static (record, child, _) => $"{record.Id}:{child}");

        Assert.True(result.TryGetValue(out var rows));
        Assert.Equal(3, rows!.Count);
        Assert.Equal(1, records.GetEnumeratorCalls);
        Assert.Equal(3, records.MoveNextCalls);
        Assert.Equal(2, records.CurrentReads);
        Assert.Equal(1, records.DisposeCalls);
    }

    [Fact]
    public void ExpandReenumeratesTheSourceOnEveryCall()
    {
        var records = new ReductionProbe<ExpansionRecord>(
        [
            new ExpansionRecord("A", ["a1"]),
        ]);

        var first = records.Expand(
            static record => record.Children,
            static (record, child, _) => $"{record.Id}:{child}");
        var second = records.Expand(
            static record => record.Children,
            static (record, child, _) => $"{record.Id}:{child}");

        Assert.True(first.TryGetValue(out _));
        Assert.True(second.TryGetValue(out _));
        Assert.Equal(2, records.GetEnumeratorCalls);
        Assert.Equal(4, records.MoveNextCalls);
        Assert.Equal(2, records.CurrentReads);
        Assert.Equal(2, records.DisposeCalls);
    }

    [Fact]
    public void NullArgumentsAreRejectedEagerly()
    {
        IEnumerable<ExpansionRecord>? records = null;
        Func<ExpansionRecord, IReadOnlyList<string>>? children = null;
        Func<ExpansionRecord, string, RowOrigin, string>? combine = null;

        Assert.Throws<ArgumentNullException>(
            static () => NestedDataExtensions.Expand(
                (IEnumerable<ExpansionRecord>)null!,
                static record => record.Children,
                static (record, child, _) => $"{record.Id}:{child}"));
        Assert.Throws<ArgumentNullException>(
            static () => new ExpansionRecord[0].Expand(
                (Func<ExpansionRecord, IReadOnlyList<string>>)null!,
                static (record, child, _) => $"{record.Id}:{child}"));
        Assert.Throws<ArgumentNullException>(
            static () => new ExpansionRecord[0].Expand(
                static record => record.Children,
                (Func<ExpansionRecord, string, RowOrigin, string>)null!));

        Assert.Null(records);
        Assert.Null(children);
        Assert.Null(combine);
    }

    [Fact]
    public void SelectorExceptionsPropagateUnchanged()
    {
        var expected = new InvalidOperationException("selector");
        var records = new[] { new ExpansionRecord("A", ["a"]), };

        var exception = Assert.Throws<InvalidOperationException>(
            () => records.Expand<ExpansionRecord, string, string>(
                (ExpansionRecord _) => throw expected,
                static (record, child, _) => $"{record.Id}:{child}"));

        Assert.Same(expected, exception);
    }

    [Fact]
    public void CombineExceptionsPropagateUnchanged()
    {
        var expected = new InvalidOperationException("combine");
        var records = new[] { new ExpansionRecord("A", ["a"]), };

        var exception = Assert.Throws<InvalidOperationException>(
            () => records.Expand<ExpansionRecord, string, string>(
                static record => record.Children,
                (ExpansionRecord _, string _, RowOrigin _) => throw expected));

        Assert.Same(expected, exception);
    }

    [Fact]
    public void ChildListMutationsAreObservedDuringTheCall()
    {
        var children = new List<string> { "a" };
        var records = new[] { new ExpansionRecord("A", children), };

        var result = records.Expand(
            static record => record.Children,
            (record, child, _) =>
            {
                if (child == "a")
                {
                    children.Add("b");
                }

                return $"{record.Id}:{child}";
            });

        Assert.True(result.TryGetValue(out var rows));
        Assert.Equal(["A:a", "A:b"], rows!);
    }

    [Fact]
    public void NullChildElementsArePassedToCombineUnchanged()
    {
        var records = new[] { new ExpansionRecord("A", [null!]), };

        var result = records.Expand(
            static record => record.Children,
            static (record, child, _) => child is null ? $"<null>:{record.Id}" : $"{record.Id}:{child}");

        Assert.True(result.TryGetValue(out var rows));
        Assert.Equal(["<null>:A"], rows!);
    }

    [Fact]
    public void NullParentRecordsArePassedToCombineUnchanged()
    {
        var records = new ExpansionRecord?[] { null, new ExpansionRecord("A", ["a"]), };

        var result = records.Expand(
            static record => record is null ? new[] { "x" } : record.Children,
            static (record, child, _) => record is null ? $"<null>:{child}" : $"{record.Id}:{child}");

        Assert.True(result.TryGetValue(out var rows));
        Assert.Equal(["<null>:x", "A:a"], rows!);
    }

    internal sealed record ExpansionRecord(string Id, IReadOnlyList<string> Children);

    internal sealed record ExpandedRow(string Id, string Child);
}
