namespace FunnySharp.Tests;

public sealed class NestedDataErrorTests
{
    [Fact]
    public void OriginCarriesRecordAndElementPositions()
    {
        var records = new[]
        {
            new OriginRecord("A", ["x", "y"]),
            new OriginRecord("B", ["z"]),
        };
        RowOrigin observed = default;

        var result = records.Expand(
            static record => record.Children,
            (record, child, origin) =>
            {
                if (record.Id == "B")
                {
                    observed = origin;
                }

                return $"{record.Id}:{child}";
            });

        Assert.True(result.TryGetValue(out var rows));
        Assert.Equal(["A:x", "A:y", "B:z"], rows);
        Assert.Equal(1, observed.RecordIndex);
        Assert.Equal(0, observed.ElementIndex);
        Assert.Equal("record[1].element[0]", observed.ToString());
    }

    [Fact]
    public void CartesianOriginCarriesRecordAndBothElementPositions()
    {
        var records = new[]
        {
            new OriginRecord("A", ["x"]),
            new OriginRecord("B", ["y", "z"]),
        };
        CartesianRowOrigin observed = default;

        var result = records.ExpandCartesian(
            static record => record.Children,
            static _ => new[] { "first", "second" },
            (record, first, second, origin) =>
            {
                if (record.Id == "B" && first == "z" && second == "second")
                {
                    observed = origin;
                }

                return $"{record.Id}:{first}:{second}";
            });

        Assert.True(result.TryGetValue(out _));
        Assert.Equal(1, observed.RecordIndex);
        Assert.Equal(1, observed.FirstElementIndex);
        Assert.Equal(1, observed.SecondElementIndex);
        Assert.Equal("record[1].elements[1,1]", observed.ToString());
    }

    [Fact]
    public void OriginsCompareByPositionAndShareHashCodes()
    {
        RowOrigin first = Origin(3, 4);
        RowOrigin same = Origin(3, 4);
        RowOrigin differentRecord = Origin(2, 4);
        RowOrigin differentElement = Origin(3, 5);
        CartesianRowOrigin cartesian = CartesianOrigin(3, 4, 5);
        CartesianRowOrigin sameCartesian = CartesianOrigin(3, 4, 5);
        CartesianRowOrigin differentCartesian = CartesianOrigin(3, 5, 4);

        Assert.True(first.Equals(same));
        Assert.True(first.Equals((object)same));
        Assert.False(first.Equals(differentRecord));
        Assert.False(first.Equals(differentElement));
        Assert.False(first.Equals(null));
        Assert.Equal(first.GetHashCode(), same.GetHashCode());

        Assert.True(cartesian.Equals(sameCartesian));
        Assert.True(cartesian.Equals((object)sameCartesian));
        Assert.False(cartesian.Equals(differentCartesian));
        Assert.False(cartesian.Equals(null));
        Assert.Equal(cartesian.GetHashCode(), sameCartesian.GetHashCode());
    }

    [Fact]
    public void ErrorFactoriesRejectNegativePositions()
    {
        Assert.Throws<ArgumentOutOfRangeException>(static () => NestedRowError.NullChildSequence(-1));
        Assert.Throws<ArgumentOutOfRangeException>(static () => NestedRowError.NullFirstChildSequence(-1));
        Assert.Throws<ArgumentOutOfRangeException>(static () => NestedRowError.NullSecondChildSequence(-1));
        Assert.Throws<ArgumentOutOfRangeException>(
            static () => NestedRowError.ChildCountMismatch(-1, 1, 2));
        Assert.Throws<ArgumentOutOfRangeException>(
            static () => NestedRowError.ChildCountMismatch(0, -1, 2));
        Assert.Throws<ArgumentOutOfRangeException>(
            static () => NestedRowError.ChildCountMismatch(0, 1, -2));
    }

    [Fact]
    public void MismatchFactoryRejectsEqualCounts()
    {
        Assert.Throws<ArgumentException>(
            static () => NestedRowError.ChildCountMismatch(0, 2, 2));
    }

    [Fact]
    public void ErrorValuesCompareAndRenderDeterministically()
    {
        NestedRowError nullChild = NestedRowError.NullChildSequence(3);
        NestedRowError sameNull = NestedRowError.NullChildSequence(3);
        NestedRowError nullFirst = NestedRowError.NullFirstChildSequence(3);
        NestedRowError nullSecond = NestedRowError.NullSecondChildSequence(3);
        NestedRowError mismatch = NestedRowError.ChildCountMismatch(2, 1, 3);
        NestedRowError sameMismatch = NestedRowError.ChildCountMismatch(2, 1, 3);
        NestedRowError mirroredMismatch = NestedRowError.ChildCountMismatch(2, 3, 1);

        Assert.Equal(NestedRowErrorKind.NullChildSequence, nullChild.Kind);
        Assert.Equal(3, nullChild.RecordIndex);
        Assert.Null(nullChild.FirstElementCount);
        Assert.Null(nullChild.SecondElementCount);
        Assert.Equal("NullChildSequence at record[3]", nullChild.ToString());

        Assert.Equal(NestedRowErrorKind.NullFirstChildSequence, nullFirst.Kind);
        Assert.Equal("NullFirstChildSequence at record[3]", nullFirst.ToString());
        Assert.Equal(NestedRowErrorKind.NullSecondChildSequence, nullSecond.Kind);
        Assert.Equal("NullSecondChildSequence at record[3]", nullSecond.ToString());

        Assert.Equal(NestedRowErrorKind.ChildCountMismatch, mismatch.Kind);
        Assert.Equal(2, mismatch.RecordIndex);
        Assert.Equal(1, mismatch.FirstElementCount);
        Assert.Equal(3, mismatch.SecondElementCount);
        Assert.Equal(
            "ChildCountMismatch at record[2]: first has 1, second has 3",
            mismatch.ToString());

        Assert.True(nullChild.Equals(sameNull));
        Assert.True(nullChild.Equals((object)sameNull));
        Assert.False(nullChild.Equals(nullFirst));
        Assert.False(nullChild.Equals(nullSecond));
        Assert.True(mismatch.Equals(sameMismatch));
        Assert.False(mismatch.Equals(mirroredMismatch));
        Assert.False(mismatch.Equals(nullChild));
        Assert.False(mismatch.Equals(null));
        Assert.Equal(nullChild.GetHashCode(), sameNull.GetHashCode());
        Assert.Equal(mismatch.GetHashCode(), sameMismatch.GetHashCode());
    }

    private static RowOrigin Origin(int recordIndex, int elementIndex)
    {
        var records = new[]
        {
            new OriginRecord("A", ["0", "1", "2", "3", "4", "5"]),
            new OriginRecord("B", ["0", "1", "2", "3", "4", "5"]),
            new OriginRecord("C", ["0", "1", "2", "3", "4", "5"]),
            new OriginRecord("D", ["0", "1", "2", "3", "4", "5"]),
            new OriginRecord("E", ["0", "1", "2", "3", "4", "5"]),
        };
        RowOrigin result = default;
        var outcome = records.Expand(
            static record => record.Children,
            (record, _, origin) =>
            {
                if (origin.RecordIndex == recordIndex && origin.ElementIndex == elementIndex)
                {
                    result = origin;
                }

                return record.Id;
            });

        Assert.True(outcome.TryGetValue(out _));
        return result;
    }

    private static CartesianRowOrigin CartesianOrigin(
        int recordIndex,
        int firstElementIndex,
        int secondElementIndex)
    {
        var records = new[]
        {
            new OriginRecord("A", ["0", "1", "2", "3", "4", "5"]),
            new OriginRecord("B", ["0", "1", "2", "3", "4", "5"]),
            new OriginRecord("C", ["0", "1", "2", "3", "4", "5"]),
            new OriginRecord("D", ["0", "1", "2", "3", "4", "5"]),
            new OriginRecord("E", ["0", "1", "2", "3", "4", "5"]),
        };
        CartesianRowOrigin result = default;
        var outcome = records.ExpandCartesian(
            static record => record.Children,
            static _ => new[] { "0", "1", "2", "3", "4", "5" },
            (record, _, _, origin) =>
            {
                if (origin.RecordIndex == recordIndex
                    && origin.FirstElementIndex == firstElementIndex
                    && origin.SecondElementIndex == secondElementIndex)
                {
                    result = origin;
                }

                return record.Id;
            });

        Assert.True(outcome.TryGetValue(out _));
        return result;
    }

    internal sealed record OriginRecord(string Id, IReadOnlyList<string> Children);
}
