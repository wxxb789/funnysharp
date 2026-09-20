namespace FunnySharp.Tests;

public sealed class PartitionTests
{
    [Fact]
    public void PartitionRejectsNullArgumentsEagerly()
    {
        IEnumerable<int>? source = null;
        IEnumerable<Option<int>>? options = null;
        IEnumerable<Result<int, string>>? results = null;
        IEnumerable<UnitResult<string>>? unitResults = null;

        Assert.Throws<ArgumentNullException>(() => source!.Partition(static value => value % 2 == 0));
        Assert.Throws<ArgumentNullException>(() =>
            Enumerable.Empty<int>().Partition((Func<int, bool>)null!));
        Assert.Throws<ArgumentNullException>(() => options!.Partition());
        Assert.Throws<ArgumentNullException>(() => results!.Partition());
        Assert.Throws<ArgumentNullException>(() => unitResults!.Partition());
    }

    [Fact]
    public void PartitionSplitsItemsIntoTrueAndFalseSidesInSourceOrder()
    {
        var empty = Enumerable.Empty<int>().Partition(static value => value % 2 == 0);
        var mixed = new[] { 1, 2, 3, 4, 5, 6 }.Partition(static value => value % 2 == 0);
        var allTrue = new[] { 1, 2, 3 }.Partition(static _ => true);
        var allFalse = new[] { 1, 2, 3 }.Partition(static _ => false);

        Assert.Empty(empty.True);
        Assert.Empty(empty.False);
        Assert.Equal([2, 4, 6], mixed.True);
        Assert.Equal([1, 3, 5], mixed.False);
        Assert.Equal(6, mixed.True.Count + mixed.False.Count);
        Assert.Equal([1, 2, 3], allTrue.True);
        Assert.Empty(allTrue.False);
        Assert.Empty(allFalse.True);
        Assert.Equal([1, 2, 3], allFalse.False);
    }

    [Fact]
    public void PartitionSupportsPositionalDeconstruction()
    {
        var (evens, odds) = new[] { 1, 2, 3, 4, 5, 6 }.Partition(static value => value % 2 == 0);

        Assert.Equal([2, 4, 6], evens);
        Assert.Equal([1, 3, 5], odds);
    }

    [Fact]
    public void PartitionOfOptionsCollectsSomesInOrderAndCountsNones()
    {
        var mixed = new[]
        {
            Option.Some(1),
            Option.None<int>(),
            Option.Some(3),
            Option.None<int>(),
            Option.Some(5),
        }.Partition();
        var allSome = new[] { Option.Some(1), Option.Some(2) }.Partition();
        var allNone = new[] { Option.None<int>(), Option.None<int>() }.Partition();
        var empty = Enumerable.Empty<Option<int>>().Partition();

        Assert.Equal([1, 3, 5], mixed.Somes);
        Assert.Equal(2, mixed.Nones);
        Assert.Equal([1, 2], allSome.Somes);
        Assert.Equal(0, allSome.Nones);
        Assert.Empty(allNone.Somes);
        Assert.Equal(2, allNone.Nones);
        Assert.Empty(empty.Somes);
        Assert.Equal(0, empty.Nones);
    }

    [Fact]
    public void PartitionOfResultsCollectsPassedValuesAndFailedErrorsInSourceOrder()
    {
        var mixed = new[]
        {
            Result<int, string>.Success(1),
            Result<int, string>.Failure("first"),
            Result<int, string>.Success(3),
            Result<int, string>.Failure("second"),
        }.Partition();
        var empty = Enumerable.Empty<Result<int, string>>().Partition();
        var allFailed = new[]
        {
            Result<int, string>.Failure("first"),
            Result<int, string>.Failure("second"),
        }.Partition();

        Assert.Equal([1, 3], mixed.Passed);
        Assert.Equal(["first", "second"], mixed.Failed);
        Assert.Empty(empty.Passed);
        Assert.Empty(empty.Failed);
        Assert.Empty(allFailed.Passed);
        Assert.Equal(["first", "second"], allFailed.Failed);
    }

    [Fact]
    public void PartitionOfUnitResultsCountsSucceededAndCollectsFailedErrorsInOrder()
    {
        var mixed = new[]
        {
            UnitResult<string>.Success(),
            UnitResult<string>.Failure("first"),
            UnitResult<string>.Success(),
            UnitResult<string>.Failure("second"),
        }.Partition();
        var empty = Enumerable.Empty<UnitResult<string>>().Partition();
        var allSuccess = new[] { UnitResult<string>.Success(), UnitResult<string>.Success() }.Partition();

        Assert.Equal(2, mixed.Succeeded);
        Assert.Equal(["first", "second"], mixed.Failed);
        Assert.Equal(0, empty.Succeeded);
        Assert.Empty(empty.Failed);
        Assert.Equal(2, allSuccess.Succeeded);
        Assert.Empty(allSuccess.Failed);
    }

    [Fact]
    public void PartitionEnumeratesEachSourceOnceInvokesThePredicateOncePerItemAndDisposes()
    {
        var predicateSource = new ProbeEnumerable<int>([1, 2, 3, 4]);
        var optionSource = new ProbeEnumerable<Option<int>>([Option.Some(1), Option.None<int>()]);
        var resultSource = new ProbeEnumerable<Result<int, string>>(
            [Result<int, string>.Success(1), Result<int, string>.Failure("bad")]);
        var unitResultSource = new ProbeEnumerable<UnitResult<string>>(
            [UnitResult<string>.Success(), UnitResult<string>.Failure("bad")]);
        var predicateCalls = 0;

        var predicatePartition = predicateSource.Partition(value =>
        {
            predicateCalls++;
            return value % 2 == 0;
        });
        _ = optionSource.Partition();
        _ = resultSource.Partition();
        _ = unitResultSource.Partition();

        Assert.Equal([2, 4], predicatePartition.True);
        Assert.Equal([1, 3], predicatePartition.False);
        Assert.Equal(4, predicateCalls);
        AssertProbeCompleted(predicateSource, 4);
        AssertProbeCompleted(optionSource, 2);
        AssertProbeCompleted(resultSource, 2);
        AssertProbeCompleted(unitResultSource, 2);
    }

    [Fact]
    public void PartitionPropagatesPredicateExceptionsByIdentity()
    {
        var exception = new InvalidOperationException("predicate failed");
        var source = new ProbeEnumerable<int>([1, 2, 3]);

        Assert.Same(
            exception,
            Assert.Throws<InvalidOperationException>(() => source.Partition(
                value => value == 2 ? throw exception : true)));

        Assert.Equal(1, source.EnumeratorCount);
        Assert.Equal(2, source.ItemsYielded);
        Assert.Equal(1, source.DisposeCount);
    }

    [Fact]
    public async Task PartitionAsyncSplitsItemsInSourceOrderWithOnePredicateCallPerItem()
    {
        var source = new ProbeAsyncEnumerable<int>([1, 2, 3, 4]);
        var emptySource = new ProbeAsyncEnumerable<int>([]);
        var predicateCalls = 0;

        var partition = await source.PartitionAsync(value =>
        {
            predicateCalls++;
            return value % 2 == 0;
        }, TestContext.Current.CancellationToken);
        var empty = await emptySource.PartitionAsync(static _ => true, CancellationToken.None);

        Assert.Equal([2, 4], partition.True);
        Assert.Equal([1, 3], partition.False);
        Assert.Empty(empty.True);
        Assert.Empty(empty.False);
        Assert.Equal(4, predicateCalls);
        AssertProbeCompleted(source, 4);
        AssertProbeCompleted(emptySource, 0);
    }

    [Fact]
    public async Task PartitionAsyncOfResultsCollectsPassedValuesAndFailedErrorsInOrder()
    {
        var source = new ProbeAsyncEnumerable<Result<int, string>>(
        [
            Result<int, string>.Success(1),
            Result<int, string>.Failure("first"),
            Result<int, string>.Success(3),
            Result<int, string>.Failure("second"),
        ]);

        var partition = await source.PartitionAsync(TestContext.Current.CancellationToken);

        Assert.Equal([1, 3], partition.Passed);
        Assert.Equal(["first", "second"], partition.Failed);
        AssertProbeCompleted(source, 4);
    }

    [Fact]
    public void LargeSourcesArePartitionedInASinglePass()
    {
        const int count = 100_000;
        var source = new ProbeEnumerable<int>(Enumerable.Range(0, count).ToArray());

        var partition = source.Partition(static value => value % 2 == 0);

        Assert.Equal(count / 2, partition.True.Count);
        Assert.Equal(count / 2, partition.False.Count);
        Assert.Equal(0, partition.True[0]);
        Assert.Equal(count - 2, partition.True[^1]);
        Assert.Equal(1, partition.False[0]);
        Assert.Equal(count - 1, partition.False[^1]);
        AssertProbeCompleted(source, count);
    }

    private static void AssertProbeCompleted<T>(ProbeEnumerable<T> probe, int itemsYielded)
    {
        Assert.Equal(1, probe.EnumeratorCount);
        Assert.Equal(itemsYielded, probe.ItemsYielded);
        Assert.Equal(1, probe.DisposeCount);
    }

    private static void AssertProbeCompleted<T>(ProbeAsyncEnumerable<T> probe, int itemsYielded)
    {
        Assert.Equal(1, probe.EnumeratorCount);
        Assert.Equal(itemsYielded, probe.ItemsYielded);
        Assert.Equal(1, probe.DisposeCount);
    }

    private sealed class ProbeAsyncEnumerable<T>(IReadOnlyList<T> values) : IAsyncEnumerable<T>
    {
        public int EnumeratorCount { get; private set; }

        public int ItemsYielded { get; private set; }

        public int DisposeCount { get; private set; }

        public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            EnumeratorCount++;
            try
            {
                foreach (var value in values)
                {
                    ItemsYielded++;
                    await Task.Yield();
                    yield return value;
                }
            }
            finally
            {
                DisposeCount++;
            }
        }
    }
}
