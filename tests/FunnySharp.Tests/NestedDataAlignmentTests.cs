using FunnySharp.TestSupport.FallibleReduction;

namespace FunnySharp.Tests;

public sealed class NestedDataAlignmentTests
{
    [Fact]
    public void AlignedExpansionPairsElementsByIndex()
    {
        var records = new[]
        {
            new AlignmentRecord("A", ["a1", "a2"], [10, 20]),
            new AlignmentRecord("B", ["b1"], [30]),
        };
        var observed = new List<RowOrigin>();

        var result = records.ExpandAligned(
            static record => record.First,
            static record => record.Second,
            (record, first, second, origin) =>
            {
                observed.Add(origin);
                return $"{record.Id}:{first}:{second}";
            });

        Assert.True(result.TryGetValue(out var rows));
        Assert.Equal(["A:a1:10", "A:a2:20", "B:b1:30"], rows!);
        Assert.Equal(new[] { 0, 0, 1 }, observed.Select(static origin => origin.RecordIndex));
        Assert.Equal(new[] { 0, 1, 0 }, observed.Select(static origin => origin.ElementIndex));
    }

    [Fact]
    public void UnequalAlignedLengthsProduceOneMismatchAndNoRows()
    {
        var records = new[]
        {
            new AlignmentRecord("A", ["a1", "a2"], [10]),
        };

        var result = records.ExpandAligned(
            static record => record.First,
            static record => record.Second,
            static (record, first, second, _) => $"{record.Id}:{first}:{second}");

        Assert.True(result.TryGetErrors(out var errors));
        Assert.Equal([NestedRowError.ChildCountMismatch(0, 2, 1)], errors!);
        Assert.Equal(
            "ChildCountMismatch at record[0]: first has 2, second has 1",
            errors![0].ToString());
    }

    [Fact]
    public void TwoEmptyAlignedListsProduceAnEmptySuccess()
    {
        var records = new[]
        {
            new AlignmentRecord("A", [], []),
        };

        var result = records.ExpandAligned(
            static record => record.First,
            static record => record.Second,
            static (record, first, second, _) => $"{record.Id}:{first}:{second}");

        Assert.True(result.TryGetValue(out var rows));
        Assert.Empty(rows!);
    }

    [Fact]
    public void OneEmptyAlignedListIsAMismatch()
    {
        var records = new[]
        {
            new AlignmentRecord("A", [], [10]),
        };

        var result = records.ExpandAligned(
            static record => record.First,
            static record => record.Second,
            static (record, first, second, _) => $"{record.Id}:{first}:{second}");

        Assert.True(result.TryGetErrors(out var errors));
        Assert.Equal([NestedRowError.ChildCountMismatch(0, 0, 1)], errors!);
    }

    [Fact]
    public void BclZipTruncatesWhileAlignedExpansionReportsTheMismatch()
    {
        var records = new[]
        {
            new AlignmentRecord("A", ["a1", "a2"], [10]),
        };

        var truncated = records
            .SelectMany(static record => record.First.Zip(record.Second, (first, second) => $"{record.Id}:{first}:{second}"))
            .ToArray();
        var aligned = records.ExpandAligned(
            static record => record.First,
            static record => record.Second,
            static (record, first, second, _) => $"{record.Id}:{first}:{second}");

        Assert.Equal(["A:a1:10"], truncated);
        Assert.True(aligned.TryGetErrors(out var errors));
        Assert.Single(errors!);
        Assert.Equal(NestedRowErrorKind.ChildCountMismatch, errors![0].Kind);
    }

    [Fact]
    public void NullAlignedSelectorsReportBothFailuresAndSkipCombine()
    {
        var records = new[]
        {
            new AlignmentRecord("A", null!, null!),
        };
        var combines = 0;

        var result = records.ExpandAligned(
            static record => record.First,
            static record => record.Second,
            (record, first, second, _) =>
            {
                combines++;
                return $"{record.Id}:{first}:{second}";
            });

        Assert.True(result.TryGetErrors(out var errors));
        Assert.Equal(
            [
                NestedRowError.NullFirstChildSequence(0),
                NestedRowError.NullSecondChildSequence(0),
            ],
            errors!);
        Assert.Equal(0, combines);
    }

    [Fact]
    public void MultipleAlignedMismatchesAreReportedInRecordOrder()
    {
        var records = new[]
        {
            new AlignmentRecord("A", ["a1"], [10, 20]),
            new AlignmentRecord("B", ["b1", "b2", "b3"], [30, 40]),
            new AlignmentRecord("C", ["c1"], [50]),
        };
        var combines = 0;

        var result = records.ExpandAligned(
            static record => record.First,
            static record => record.Second,
            (record, first, second, _) =>
            {
                combines++;
                return $"{record.Id}:{first}:{second}";
            });

        Assert.True(result.TryGetErrors(out var errors));
        Assert.Equal(
            [
                NestedRowError.ChildCountMismatch(0, 1, 2),
                NestedRowError.ChildCountMismatch(1, 3, 2),
            ],
            errors!);
        Assert.Equal(0, combines);
    }

    [Fact]
    public void NullAlignedListsAreCheckedOnEveryLaterRecord()
    {
        var records = new[]
        {
            new AlignmentRecord("A", null!, [10]),
            new AlignmentRecord("B", ["b1"], [20]),
            new AlignmentRecord("C", ["c1"], null!),
        };
        var combines = 0;

        var result = records.ExpandAligned(
            static record => record.First,
            static record => record.Second,
            (record, first, second, _) =>
            {
                combines++;
                return $"{record.Id}:{first}:{second}";
            });

        Assert.True(result.TryGetErrors(out var errors));
        Assert.Equal(
            [
                NestedRowError.NullFirstChildSequence(0),
                NestedRowError.NullSecondChildSequence(2),
            ],
            errors!);
        Assert.Equal(0, combines);
    }

    [Fact]
    public void AlignedCombineRunsBeforeTheFirstErrorAndNotAfter()
    {
        var records = new[]
        {
            new AlignmentRecord("A", ["a1"], [10]),
            new AlignmentRecord("B", ["b1", "b2"], [20]),
            new AlignmentRecord("C", ["c1"], [30]),
        };
        var combines = 0;

        var result = records.ExpandAligned(
            static record => record.First,
            static record => record.Second,
            (record, first, second, _) =>
            {
                combines++;
                return $"{record.Id}:{first}:{second}";
            });

        Assert.True(result.TryGetErrors(out var errors));
        Assert.Equal([NestedRowError.ChildCountMismatch(1, 2, 1)], errors!);
        Assert.Equal(1, combines);
    }

    [Fact]
    public void CartesianExpansionYieldsTheExplicitProductInFirstThenSecondOrder()
    {
        var records = new[]
        {
            new AlignmentRecord("A", ["a1", "a2"], [10, 20]),
        };
        var origins = new List<CartesianRowOrigin>();

        var result = records.ExpandCartesian(
            static record => record.First,
            static record => record.Second,
            (record, first, second, origin) =>
            {
                origins.Add(origin);
                return $"{record.Id}:{first}:{second}";
            });

        Assert.True(result.TryGetValue(out var rows));
        Assert.Equal(
            ["A:a1:10", "A:a1:20", "A:a2:10", "A:a2:20"],
            rows!);
        Assert.Equal(new[] { 0, 0, 0, 0 }, origins.Select(static origin => origin.RecordIndex));
        Assert.Equal(new[] { 0, 0, 1, 1 }, origins.Select(static origin => origin.FirstElementIndex));
        Assert.Equal(new[] { 0, 1, 0, 1 }, origins.Select(static origin => origin.SecondElementIndex));
    }

    [Fact]
    public void CartesianExpansionAcceptsUnequalLengthsWhereAlignedRejectsThem()
    {
        var records = new[]
        {
            new AlignmentRecord("A", ["a1", "a2"], [10]),
        };

        var aligned = records.ExpandAligned(
            static record => record.First,
            static record => record.Second,
            static (record, first, second, _) => $"{record.Id}:{first}:{second}");
        var cartesian = records.ExpandCartesian(
            static record => record.First,
            static record => record.Second,
            static (record, first, second, _) => $"{record.Id}:{first}:{second}");

        Assert.True(aligned.TryGetErrors(out var errors));
        Assert.Equal([NestedRowError.ChildCountMismatch(0, 2, 1)], errors!);
        Assert.True(cartesian.TryGetValue(out var rows));
        Assert.Equal(["A:a1:10", "A:a2:10"], rows!);
    }

    [Fact]
    public void AlignedAndCartesianExpansionProduceDistinguishableRowCounts()
    {
        var records = new[]
        {
            new AlignmentRecord("A", ["S", "M"], [10, 20]),
        };

        var aligned = records.ExpandAligned(
            static record => record.First,
            static record => record.Second,
            static (record, first, second, _) => $"{record.Id}:{first}:{second}");
        var cartesian = records.ExpandCartesian(
            static record => record.First,
            static record => record.Second,
            static (record, first, second, _) => $"{record.Id}:{first}:{second}");

        Assert.True(aligned.TryGetValue(out var alignedRows));
        Assert.True(cartesian.TryGetValue(out var cartesianRows));
        Assert.Equal(["A:S:10", "A:M:20"], alignedRows!);
        Assert.Equal(
            ["A:S:10", "A:S:20", "A:M:10", "A:M:20"],
            cartesianRows!);
    }

    [Fact]
    public void CartesianExpansionAllowsAnEmptySideToContributeNoRows()
    {
        var records = new[]
        {
            new AlignmentRecord("A", [], [10]),
            new AlignmentRecord("B", ["S"], [10]),
        };

        var result = records.ExpandCartesian(
            static record => record.First,
            static record => record.Second,
            static (record, first, second, _) => $"{record.Id}:{first}:{second}");

        Assert.True(result.TryGetValue(out var rows));
        Assert.Equal(["B:S:10"], rows!);
    }

    [Fact]
    public void CartesianNullSelectorsReportBothFailuresAndSkipCombine()
    {
        var records = new[]
        {
            new AlignmentRecord("A", null!, null!),
        };
        var combines = 0;

        var result = records.ExpandCartesian(
            static record => record.First,
            static record => record.Second,
            (record, first, second, _) =>
            {
                combines++;
                return $"{record.Id}:{first}:{second}";
            });

        Assert.True(result.TryGetErrors(out var errors));
        Assert.Equal(
            [
                NestedRowError.NullFirstChildSequence(0),
                NestedRowError.NullSecondChildSequence(0),
            ],
            errors!);
        Assert.Equal(0, combines);
    }

    [Fact]
    public void AlignedExpansionEnumeratesTheSourceOnce()
    {
        var records = new ReductionProbe<AlignmentRecord>(
        [
            new AlignmentRecord("A", ["a1", "a2"], [10, 20]),
            new AlignmentRecord("B", ["b1"], [30]),
        ]);

        var result = records.ExpandAligned(
            static record => record.First,
            static record => record.Second,
            static (record, first, second, _) => $"{record.Id}:{first}:{second}");

        Assert.True(result.TryGetValue(out var rows));
        Assert.Equal(3, rows!.Count);
        Assert.Equal(1, records.GetEnumeratorCalls);
        Assert.Equal(3, records.MoveNextCalls);
        Assert.Equal(2, records.CurrentReads);
        Assert.Equal(1, records.DisposeCalls);
    }

    [Fact]
    public void CartesianExpansionEnumeratesTheSourceOnce()
    {
        var records = new ReductionProbe<AlignmentRecord>(
        [
            new AlignmentRecord("A", ["a1", "a2"], [10, 20]),
        ]);

        var result = records.ExpandCartesian(
            static record => record.First,
            static record => record.Second,
            static (record, first, second, _) => $"{record.Id}:{first}:{second}");

        Assert.True(result.TryGetValue(out var rows));
        Assert.Equal(4, rows!.Count);
        Assert.Equal(1, records.GetEnumeratorCalls);
        Assert.Equal(2, records.MoveNextCalls);
        Assert.Equal(1, records.CurrentReads);
        Assert.Equal(1, records.DisposeCalls);
    }

    [Fact]
    public void AlignmentSelectorAndCombineExceptionsPropagateUnchanged()
    {
        var selectorException = new InvalidOperationException("selector");
        var combineException = new InvalidOperationException("combine");
        var records = new[] { new AlignmentRecord("A", ["a"], [1]), };

        var selectorThrown = Assert.Throws<InvalidOperationException>(
            () => records.ExpandAligned<AlignmentRecord, string, int, string>(
                (AlignmentRecord _) => throw selectorException,
                static record => record.Second,
                static (record, first, second, _) => $"{record.Id}:{first}:{second}"));
        var combineThrown = Assert.Throws<InvalidOperationException>(
            () => records.ExpandAligned<AlignmentRecord, string, int, string>(
                static record => record.First,
                static record => record.Second,
                (AlignmentRecord _, string _, int _, RowOrigin _) => throw combineException));

        Assert.Same(selectorException, selectorThrown);
        Assert.Same(combineException, combineThrown);
    }

    [Fact]
    public void AlignmentNullArgumentsAreRejectedEagerly()
    {
        Assert.Throws<ArgumentNullException>(
            static () => NestedDataExtensions.ExpandAligned(
                (IEnumerable<AlignmentRecord>)null!,
                static record => record.First,
                static record => record.Second,
                static (record, first, second, _) => $"{record.Id}:{first}:{second}"));
        Assert.Throws<ArgumentNullException>(
            static () => new AlignmentRecord[0].ExpandAligned(
                (Func<AlignmentRecord, IReadOnlyList<string>>)null!,
                static record => record.Second,
                static (record, first, second, _) => $"{record.Id}:{first}:{second}"));
        Assert.Throws<ArgumentNullException>(
            static () => new AlignmentRecord[0].ExpandAligned(
                static record => record.First,
                (Func<AlignmentRecord, IReadOnlyList<int>>)null!,
                static (record, first, second, _) => $"{record.Id}:{first}:{second}"));
        Assert.Throws<ArgumentNullException>(
            static () => new AlignmentRecord[0].ExpandAligned(
                static record => record.First,
                static record => record.Second,
                (Func<AlignmentRecord, string, int, RowOrigin, string>)null!));
    }

    [Fact]
    public void CartesianSelectorAndCombineExceptionsPropagateUnchanged()
    {
        var selectorException = new InvalidOperationException("selector");
        var combineException = new InvalidOperationException("combine");
        var records = new[] { new AlignmentRecord("A", ["a"], [1]), };

        var selectorThrown = Assert.Throws<InvalidOperationException>(
            () => records.ExpandCartesian<AlignmentRecord, string, int, string>(
                (AlignmentRecord _) => throw selectorException,
                static record => record.Second,
                static (record, first, second, _) => $"{record.Id}:{first}:{second}"));
        var combineThrown = Assert.Throws<InvalidOperationException>(
            () => records.ExpandCartesian<AlignmentRecord, string, int, string>(
                static record => record.First,
                static record => record.Second,
                (AlignmentRecord _, string _, int _, CartesianRowOrigin _) => throw combineException));

        Assert.Same(selectorException, selectorThrown);
        Assert.Same(combineException, combineThrown);
    }

    [Fact]
    public void CartesianNullArgumentsAreRejectedEagerly()
    {
        Assert.Throws<ArgumentNullException>(
            static () => NestedDataExtensions.ExpandCartesian(
                (IEnumerable<AlignmentRecord>)null!,
                static record => record.First,
                static record => record.Second,
                static (record, first, second, _) => $"{record.Id}:{first}:{second}"));
        Assert.Throws<ArgumentNullException>(
            static () => new AlignmentRecord[0].ExpandCartesian(
                (Func<AlignmentRecord, IReadOnlyList<string>>)null!,
                static record => record.Second,
                static (record, first, second, _) => $"{record.Id}:{first}:{second}"));
        Assert.Throws<ArgumentNullException>(
            static () => new AlignmentRecord[0].ExpandCartesian(
                static record => record.First,
                (Func<AlignmentRecord, IReadOnlyList<int>>)null!,
                static (record, first, second, _) => $"{record.Id}:{first}:{second}"));
        Assert.Throws<ArgumentNullException>(
            static () => new AlignmentRecord[0].ExpandCartesian(
                static record => record.First,
                static record => record.Second,
                (Func<AlignmentRecord, string, int, CartesianRowOrigin, string>)null!));
    }

    internal sealed record AlignmentRecord(
        string Id,
        IReadOnlyList<string> First,
        IReadOnlyList<int> Second);
}
