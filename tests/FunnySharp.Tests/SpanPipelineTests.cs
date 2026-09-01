namespace FunnySharp.Tests;

public sealed class SpanPipelineTests
{
    [Fact]
    public void SpanPipelinesRejectNullDelegatesEagerly()
    {
        var source = new[] { 1 };
        var destination = new int[1];

        Assert.Throws<ArgumentNullException>(() =>
            source.AsSpan().SelectTo<int, int>(destination.AsSpan(), null!));
        Assert.Throws<ArgumentNullException>(() =>
            source.AsSpan().WhereTo(destination.AsSpan(), null!));
        Assert.Throws<ArgumentNullException>(() =>
            source.AsSpan().ChooseTo<int, int>(destination.AsSpan(), null!));
        Assert.Throws<ArgumentNullException>(() => source.AsSpan().SelectInPlace(null!));
        Assert.Throws<ArgumentNullException>(() => source.AsSpan().WhereInPlace(null!));
    }

    [Fact]
    public void SelectToReturnsWrittenPrefixInOrderAndLeavesDestinationTailUntouched()
    {
        var source = new[] { 1, 2, 3 };
        var destination = new[] { -1, -1, -1, -1 };

        var written = source.AsSpan().SelectTo(destination.AsSpan(), static value => value * 10);

        Assert.Equal([10, 20, 30], written.ToArray());
        Assert.Equal([10, 20, 30, -1], destination);

        written[1] = 99;
        Assert.Equal(99, destination[1]);
    }

    [Fact]
    public void WhereToStablyFiltersIntoTheDestinationPrefix()
    {
        var source = new[] { 5, 2, 7, 4, 9 };
        var destination = new[] { -1, -1, -1, -1, -1, -1 };

        var written = source.AsSpan().WhereTo(destination.AsSpan(), static value => value % 2 == 1);

        Assert.Equal([5, 7, 9], written.ToArray());
        Assert.Equal([5, 7, 9, -1, -1, -1], destination);
    }

    [Fact]
    public void ChooseToFusesFilteringAndMappingInSourceOrder()
    {
        var source = new[] { 1, 2, 3, 4 };
        var destination = new[] { -1, -1, -1, -1, -1 };
        var calls = new List<int>();

        var written = source.AsSpan().ChooseTo(destination.AsSpan(), value =>
        {
            calls.Add(value);
            return value % 2 == 0 ? Option.Some(value * 10) : Option.None<int>();
        });

        Assert.Equal([20, 40], written.ToArray());
        Assert.Equal([20, 40, -1, -1, -1], destination);
        Assert.Equal([1, 2, 3, 4], calls);
    }

    [Fact]
    public void EmptySpansReturnEmptyViewsWithoutWriting()
    {
        var source = Array.Empty<int>();
        var destination = new[] { -1 };
        var inPlace = Array.Empty<int>();

        Assert.True(source.AsSpan().SelectTo(destination.AsSpan(), static value => value).IsEmpty);
        Assert.True(source.AsSpan().WhereTo(destination.AsSpan(), static _ => true).IsEmpty);
        Assert.True(source.AsSpan().ChooseTo(
            destination.AsSpan(),
            static value => Option.Some(value)).IsEmpty);
        Assert.True(inPlace.AsSpan().SelectInPlace(static value => value).IsEmpty);
        Assert.True(inPlace.AsSpan().WhereInPlace(static _ => true).IsEmpty);
        Assert.Equal([-1], destination);
    }

    [Fact]
    public void DestinationCapacityIsValidatedBeforeAnyDelegateOrWrite()
    {
        var source = new[] { 1, 2, 3 };
        var selectDestination = new[] { -1, -1 };
        var whereDestination = new[] { -1, -1 };
        var chooseDestination = new[] { -1, -1 };
        var selectCalls = 0;
        var whereCalls = 0;
        var chooseCalls = 0;

        Assert.Throws<ArgumentException>(() => source.AsSpan().SelectTo(selectDestination.AsSpan(), value =>
        {
            selectCalls++;
            return value;
        }));
        Assert.Throws<ArgumentException>(() => source.AsSpan().WhereTo(whereDestination.AsSpan(), value =>
        {
            whereCalls++;
            return value % 2 == 0;
        }));
        Assert.Throws<ArgumentException>(() => source.AsSpan().ChooseTo(chooseDestination.AsSpan(), value =>
        {
            chooseCalls++;
            return Option.Some(value);
        }));

        Assert.Equal(0, selectCalls);
        Assert.Equal(0, whereCalls);
        Assert.Equal(0, chooseCalls);
        Assert.Equal([-1, -1], selectDestination);
        Assert.Equal([-1, -1], whereDestination);
        Assert.Equal([-1, -1], chooseDestination);
    }

    [Fact]
    public void DelegateExceptionsPropagateByIdentityWithoutRollingBackWrittenPrefix()
    {
        var source = new[] { 1, 2, 3 };
        var destination = new[] { -1, -1, -1, -1 };
        var exception = new InvalidOperationException("selector failure");

        var thrown = Assert.Throws<InvalidOperationException>(() => source.AsSpan().SelectTo(
            destination.AsSpan(),
            value => value == 2 ? throw exception : value * 10));

        Assert.Same(exception, thrown);
        Assert.Equal([10, -1, -1, -1], destination);
    }

    [Fact]
    public void SelectInPlaceReturnsASameLengthViewOverTheOriginalStorage()
    {
        var values = new[] { 1, 2, 3 };

        var written = values.AsSpan().SelectInPlace(static value => value * 10);

        Assert.Equal(values.Length, written.Length);
        Assert.Equal([10, 20, 30], written.ToArray());
        written[2] = 99;
        Assert.Equal([10, 20, 99], values);
    }

    [Fact]
    public void WhereInPlaceStablyCompactsAndReturnsTheValidPrefix()
    {
        var values = new[] { 5, 2, 7, 4, 9 };

        var written = values.AsSpan().WhereInPlace(static value => value % 2 == 1);

        Assert.Equal([5, 7, 9], written.ToArray());
        written[1] = 70;
        Assert.Equal(70, values[1]);
    }

    [Fact]
    public void WhereInPlaceClearsReferenceContainingTailSlots()
    {
        var keptFirst = new object();
        var removed = new object();
        var keptSecond = new object();
        object?[] values = [keptFirst, removed, keptSecond];

        var written = values.AsSpan().WhereInPlace(value => !ReferenceEquals(value, removed));

        Assert.Equal(2, written.Length);
        Assert.Same(keptFirst, values[0]);
        Assert.Same(keptSecond, values[1]);
        Assert.Null(values[2]);
    }

    [Fact]
    public void ReadOnlyMemoryAndMemoryForwardToSpanPipelinesAndShareBackingStorage()
    {
        ReadOnlyMemory<int> readOnlySource = new[] { 1, 2, 3, 4 };
        var selectStorage = new[] { -1, -1, -1, -1, -1 };
        var whereStorage = new[] { -1, -1, -1, -1, -1 };
        var chooseStorage = new[] { -1, -1, -1, -1, -1 };
        var mappedStorage = new[] { 1, 2, 3, 4 };
        var compactedStorage = new[] { 10, 20, 30, 40 };

        var selected = readOnlySource.SelectTo(selectStorage.AsMemory(), static value => value * 10);
        var filtered = readOnlySource.WhereTo(whereStorage.AsMemory(), static value => value % 2 == 0);
        var chosen = readOnlySource.ChooseTo(
            chooseStorage.AsMemory(),
            static value => value % 2 == 0 ? Option.Some(value * 10) : Option.None<int>());
        var mapped = mappedStorage.AsMemory().SelectInPlace(static value => value * 10);
        var compacted = compactedStorage.AsMemory().WhereInPlace(static value => value >= 30);

        Assert.Equal([10, 20, 30, 40], selected.ToArray());
        Assert.Equal([2, 4], filtered.ToArray());
        Assert.Equal([20, 40], chosen.ToArray());
        Assert.Equal([10, 20, 30, 40], mapped.ToArray());
        Assert.Equal([30, 40], compacted.ToArray());
        Assert.Equal([2, 4, -1, -1, -1], whereStorage);
        Assert.Equal([20, 40, -1, -1, -1], chooseStorage);

        selected.Span[0] = 99;
        compacted.Span[1] = 77;

        Assert.Equal(99, selectStorage[0]);
        Assert.Equal(77, compactedStorage[1]);
    }

    [Fact]
    public void ReadOnlySpanAndWritableMemoryOverloadsAreDirectlyUsable()
    {
        ReadOnlySpan<int> spanSource = [1, 2, 3];
        var spanDestination = new int[3];
        var memorySource = new[] { 4, 5, 6 }.AsMemory();
        var memoryDestination = new int[3];

        var spanResult = spanSource.SelectTo(spanDestination, static value => value * 2);
        var memoryResult = memorySource.WhereTo(
            memoryDestination,
            static value => value % 2 == 0);

        Assert.Equal([2, 4, 6], spanResult.ToArray());
        Assert.Equal([4, 6], memoryResult.ToArray());
    }

    [Fact]
    public void DestinationPrefixCanOutliveAStackAllocatedSource()
    {
        var destination = new int[3];

        var written = SelectFromStackSource(destination);

        Assert.Equal([10, 20, 30], written.ToArray());
        written[0] = 99;
        Assert.Equal(99, destination[0]);
    }

    private static Span<int> SelectFromStackSource(Span<int> destination)
    {
        ReadOnlySpan<int> source = stackalloc int[] { 1, 2, 3 };
        return source.SelectTo(destination, static value => value * 10);
    }
}
