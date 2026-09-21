#pragma warning disable FS0017

using System.Collections.Generic;

namespace FunnySharp.Tests;

#pragma warning disable xUnit1051

public sealed class AsyncCollectionTests
{
    [Fact]
    public void CardinalityAsyncMembersRejectNullSourcesEagerly()
    {
        IAsyncEnumerable<int>? source = null;

        Assert.Throws<ArgumentNullException>(() => source!.FirstOrNoneAsync());
        Assert.Throws<ArgumentNullException>(() => source!.LastOrNoneAsync());
        Assert.Throws<ArgumentNullException>(() => source!.SingleOrNoneAsync());
        Assert.Throws<ArgumentNullException>(() => source!.ElementAtOrNoneAsync(0));
        Assert.Throws<ArgumentNullException>(() => source!.MinOrNoneAsync());
        Assert.Throws<ArgumentNullException>(() => source!.MaxOrNoneAsync());
        Assert.Throws<ArgumentNullException>(() => source!.ToNonEmptyOrNoneAsync());
    }

    [Fact]
    public void PartitionAsyncRejectsNullArgumentsEagerly()
    {
        IAsyncEnumerable<int>? source = null;
        IAsyncEnumerable<Result<int, string>>? results = null;

        Assert.Throws<ArgumentNullException>(() => source!.PartitionAsync(static value => value > 0));
        Assert.Throws<ArgumentNullException>(() => results!.PartitionAsync());
        Assert.Throws<ArgumentNullException>(() => AsyncValues(1).PartitionAsync((Func<int, bool>)null!));
    }

    [Fact]
    public void WhereNotNullRejectsNullSourcesEagerly()
    {
        IAsyncEnumerable<string?>? strings = null;
        IAsyncEnumerable<int?>? numbers = null;

        Assert.Throws<ArgumentNullException>(() => strings!.WhereNotNull());
        Assert.Throws<ArgumentNullException>(() => numbers!.WhereNotNull());
    }

    [Fact]
    public void LocatedTraverseAsyncRejectsNullArgumentsEagerly()
    {
        IAsyncEnumerable<int>? source = null;

        Assert.Throws<ArgumentNullException>(() => source!.TraverseAsync(
            Location.Root,
            static (location, value) => Option.Some(value)));
        Assert.Throws<ArgumentNullException>(() => source!.TraverseAsync(
            Location.Root,
            static (location, value) => Result<int, string>.Success(value)));
        Assert.Throws<ArgumentNullException>(() => source!.TraverseAsync(
            Location.Root,
            static (location, value) => UnitResult<string>.Success()));
        Assert.Throws<ArgumentNullException>(() => source!.TraverseAsync(
            Location.Root,
            static (location, value) => Validation<int, string>.Valid(value)));
        Assert.Throws<ArgumentNullException>(() => AsyncValues(1).TraverseAsync(
            (Location)null!,
            static (location, value) => Option.Some(value)));
        Assert.Throws<ArgumentNullException>(() => AsyncValues(1).TraverseAsync(
            (Location)null!,
            static (location, value) => Result<int, string>.Success(value)));
        Assert.Throws<ArgumentNullException>(() => AsyncValues(1).TraverseAsync(
            (Location)null!,
            static (location, value) => UnitResult<string>.Success()));
        Assert.Throws<ArgumentNullException>(() => AsyncValues(1).TraverseAsync(
            (Location)null!,
            static (location, value) => Validation<int, string>.Valid(value)));
        Assert.Throws<ArgumentNullException>(() => AsyncValues(1).TraverseAsync(
            Location.Root,
            (Func<Location, int, Option<int>>)null!));
        Assert.Throws<ArgumentNullException>(() => AsyncValues(1).TraverseAsync(
            Location.Root,
            (Func<Location, int, Result<int, string>>)null!));
        Assert.Throws<ArgumentNullException>(() => AsyncValues(1).TraverseAsync(
            Location.Root,
            (Func<Location, int, UnitResult<string>>)null!));
        Assert.Throws<ArgumentNullException>(() => AsyncValues(1).TraverseAsync(
            Location.Root,
            (Func<Location, int, Validation<int, string>>)null!));
    }

    [Fact]
    public void LocatedTraverseValueAsyncRejectsNullArgumentsEagerly()
    {
        IAsyncEnumerable<int>? source = null;

        Assert.Throws<ArgumentNullException>(() => source!.TraverseValueAsync(
            Location.Root,
            static (location, value) => ValueTask.FromResult(Option.Some(value))));
        Assert.Throws<ArgumentNullException>(() => source!.TraverseValueAsync(
            Location.Root,
            static (location, value) => ValueTask.FromResult(Result<int, string>.Success(value))));
        Assert.Throws<ArgumentNullException>(() => source!.TraverseValueAsync(
            Location.Root,
            static (location, value) => ValueTask.FromResult(UnitResult<string>.Success())));
        Assert.Throws<ArgumentNullException>(() => source!.TraverseValueAsync(
            Location.Root,
            static (location, value) => ValueTask.FromResult(Validation<int, string>.Valid(value))));
        Assert.Throws<ArgumentNullException>(() => AsyncValues(1).TraverseValueAsync(
            (Location)null!,
            static (location, value) => ValueTask.FromResult(Option.Some(value))));
        Assert.Throws<ArgumentNullException>(() => AsyncValues(1).TraverseValueAsync(
            (Location)null!,
            static (location, value) => ValueTask.FromResult(Result<int, string>.Success(value))));
        Assert.Throws<ArgumentNullException>(() => AsyncValues(1).TraverseValueAsync(
            (Location)null!,
            static (location, value) => ValueTask.FromResult(UnitResult<string>.Success())));
        Assert.Throws<ArgumentNullException>(() => AsyncValues(1).TraverseValueAsync(
            (Location)null!,
            static (location, value) => ValueTask.FromResult(Validation<int, string>.Valid(value))));
        Assert.Throws<ArgumentNullException>(() => AsyncValues(1).TraverseValueAsync(
            Location.Root,
            (Func<Location, int, ValueTask<Option<int>>>)null!));
        Assert.Throws<ArgumentNullException>(() => AsyncValues(1).TraverseValueAsync(
            Location.Root,
            (Func<Location, int, ValueTask<Result<int, string>>>)null!));
        Assert.Throws<ArgumentNullException>(() => AsyncValues(1).TraverseValueAsync(
            Location.Root,
            (Func<Location, int, ValueTask<UnitResult<string>>>)null!));
        Assert.Throws<ArgumentNullException>(() => AsyncValues(1).TraverseValueAsync(
            Location.Root,
            (Func<Location, int, ValueTask<Validation<int, string>>>)null!));
    }

    [Fact]
    public async Task CardinalityAsyncMembersReturnNoneForEmptySources()
    {
        var source = AsyncValues<int>();

        Assert.True((await source.FirstOrNoneAsync()).IsNone);
        Assert.True((await source.LastOrNoneAsync()).IsNone);
        Assert.True((await source.SingleOrNoneAsync()).IsNone);
        Assert.True((await source.ElementAtOrNoneAsync(0)).IsNone);
        Assert.True((await source.MinOrNoneAsync()).IsNone);
        Assert.True((await source.MaxOrNoneAsync()).IsNone);
        Assert.True((await source.ToNonEmptyOrNoneAsync()).IsNone);
    }

    [Fact]
    public async Task CardinalityAsyncMembersReturnSomeForSingletonSources()
    {
        var source = AsyncValues(42);

        Assert.True((await source.FirstOrNoneAsync()).TryGetValue(out var first));
        Assert.Equal(42, first);
        Assert.True((await source.LastOrNoneAsync()).TryGetValue(out var last));
        Assert.Equal(42, last);
        Assert.True((await source.SingleOrNoneAsync()).TryGetValue(out var single));
        Assert.Equal(42, single);
        Assert.True((await source.ElementAtOrNoneAsync(0)).TryGetValue(out var elementAt));
        Assert.Equal(42, elementAt);
        Assert.True((await source.MinOrNoneAsync()).TryGetValue(out var min));
        Assert.Equal(42, min);
        Assert.True((await source.MaxOrNoneAsync()).TryGetValue(out var max));
        Assert.Equal(42, max);
        Assert.True((await source.ToNonEmptyOrNoneAsync()).TryGetValue(out var nonEmpty));
        var singleton = nonEmpty!;
        Assert.Equal(42, singleton.First);
        Assert.Empty(singleton.Rest);
        Assert.Equal(1, singleton.Count);
    }

    [Fact]
    public async Task CardinalityAsyncMembersPreserveSourceOrderForMultipleItems()
    {
        var source = AsyncValues(3, 1, 2);

        Assert.True((await source.FirstOrNoneAsync()).TryGetValue(out var first));
        Assert.Equal(3, first);
        Assert.True((await source.LastOrNoneAsync()).TryGetValue(out var last));
        Assert.Equal(2, last);
        Assert.True((await source.SingleOrNoneAsync()).IsNone);
        Assert.True((await source.ElementAtOrNoneAsync(1)).TryGetValue(out var elementAt));
        Assert.Equal(1, elementAt);
        Assert.True((await source.MinOrNoneAsync()).TryGetValue(out var min));
        Assert.Equal(1, min);
        Assert.True((await source.MaxOrNoneAsync()).TryGetValue(out var max));
        Assert.Equal(3, max);
    }

    [Fact]
    public async Task ElementAtOrNoneAsyncReturnsNoneForOutOfRangeIndexes()
    {
        var source = AsyncValues(1, 2, 3);

        Assert.True((await source.ElementAtOrNoneAsync(-1)).IsNone);
        Assert.True((await source.ElementAtOrNoneAsync(3)).IsNone);
        Assert.True((await source.ElementAtOrNoneAsync(100)).IsNone);
    }

    [Fact]
    public async Task MinAndMaxAsyncSkipNullItemsAndReturnNoneWhenAllItemsAreNull()
    {
        var mixed = AsyncValues<string?>(null, "pear", null, "apple", null);

        Assert.True((await mixed.MinOrNoneAsync()).TryGetValue(out var min));
        Assert.Equal("apple", min);
        Assert.True((await mixed.MaxOrNoneAsync()).TryGetValue(out var max));
        Assert.Equal("pear", max);
        Assert.True((await AsyncValues<string?>(null, null).MinOrNoneAsync()).IsNone);
        Assert.True((await AsyncValues<string?>(null, null).MaxOrNoneAsync()).IsNone);
    }

    [Fact]
    public async Task LastOrNoneAsyncReturnsTheFinalItemWhenEarlierItemsAreNull()
    {
        var source = AsyncValues<string?>(null, "last");

        Assert.True((await source.LastOrNoneAsync()).TryGetValue(out var last));
        Assert.Equal("last", last);
    }

    [Fact]
    public async Task LastOrNoneAsyncThrowsWhenTheSelectedItemIsNull()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await AsyncValues<string?>("first", null).LastOrNoneAsync());
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await AsyncValues<string?>(null, null).LastOrNoneAsync());
    }

    [Fact]
    public async Task ToNonEmptyOrNoneAsyncExposesFirstRestCountAndFolds()
    {
        var option = await AsyncValues(1, 2, 3).ToNonEmptyOrNoneAsync();

        Assert.True(option.TryGetValue(out var nonEmpty));
        var items = nonEmpty!;
        Assert.Equal(1, items.First);
        Assert.Equal([2, 3], items.Rest);
        Assert.Equal(3, items.Count);
        Assert.Equal([1, 2, 3], items.ToReadOnlyList());
        Assert.Equal(6, items.Aggregate(static (left, right) => left + right));
    }

    [Fact]
    public async Task CardinalityAsyncMembersEnumerateOnceAndDisposeTheirEnumerators()
    {
        var first = new ProbeAsyncEnumerable<int>([42]);
        var last = new ProbeAsyncEnumerable<int>([1, 2, 3]);
        var single = new ProbeAsyncEnumerable<int>([1, 2]);
        var elementAt = new ProbeAsyncEnumerable<int>([1, 2, 3]);
        var min = new ProbeAsyncEnumerable<int>([3, 1, 2]);
        var max = new ProbeAsyncEnumerable<int>([1, 3, 2]);
        var toNonEmpty = new ProbeAsyncEnumerable<int>([1, 2, 3]);

        Assert.True((await first.FirstOrNoneAsync()).TryGetValue(out var firstValue));
        Assert.Equal(42, firstValue);
        Assert.True((await last.LastOrNoneAsync()).TryGetValue(out var lastValue));
        Assert.Equal(3, lastValue);
        Assert.True((await single.SingleOrNoneAsync()).IsNone);
        Assert.True((await elementAt.ElementAtOrNoneAsync(2)).TryGetValue(out var elementAtValue));
        Assert.Equal(3, elementAtValue);
        Assert.True((await min.MinOrNoneAsync()).TryGetValue(out var minValue));
        Assert.Equal(1, minValue);
        Assert.True((await max.MaxOrNoneAsync()).TryGetValue(out var maxValue));
        Assert.Equal(3, maxValue);
        Assert.True((await toNonEmpty.ToNonEmptyOrNoneAsync()).IsSome);

        AssertProbeCompleted(first, 1);
        AssertProbeCompleted(last, 3);
        AssertProbeCompleted(single, 2);
        AssertProbeCompleted(elementAt, 3);
        AssertProbeCompleted(min, 3);
        AssertProbeCompleted(max, 3);
        AssertProbeCompleted(toNonEmpty, 3);
    }

    [Fact]
    public async Task AsyncCollectionMembersForwardTheCallerTokenToTheEnumerator()
    {
        using var cancellationSource = new CancellationTokenSource();
        var token = cancellationSource.Token;
        var first = new ProbeAsyncEnumerable<int>([1, 2, 3]);
        var last = new ProbeAsyncEnumerable<int>([1, 2, 3]);
        var single = new ProbeAsyncEnumerable<int>([1, 2, 3]);
        var elementAt = new ProbeAsyncEnumerable<int>([1, 2, 3]);
        var min = new ProbeAsyncEnumerable<int>([1, 2, 3]);
        var max = new ProbeAsyncEnumerable<int>([1, 2, 3]);
        var toNonEmpty = new ProbeAsyncEnumerable<int>([1, 2, 3]);
        var partition = new ProbeAsyncEnumerable<int>([1, 2, 3]);
        var resultPartition = new ProbeAsyncEnumerable<Result<int, string>>(
            [Result<int, string>.Success(1)]);
        var traverse = new ProbeAsyncEnumerable<int>([1, 2]);
        var traverseValue = new ProbeAsyncEnumerable<int>([1, 2]);

        _ = await first.FirstOrNoneAsync(token);
        _ = await last.LastOrNoneAsync(token);
        _ = await single.SingleOrNoneAsync(token);
        _ = await elementAt.ElementAtOrNoneAsync(1, token);
        _ = await min.MinOrNoneAsync(token);
        _ = await max.MaxOrNoneAsync(token);
        _ = await toNonEmpty.ToNonEmptyOrNoneAsync(token);
        _ = await partition.PartitionAsync(static value => value > 1, token);
        _ = await resultPartition.PartitionAsync(token);
        _ = await traverse.TraverseAsync(
            Location.Root,
            static (location, value) => Validation<int, string>.Valid(value),
            token);
        _ = await traverseValue.TraverseValueAsync(
            Location.Root,
            static (location, value) => ValueTask.FromResult(Validation<int, string>.Valid(value)),
            token);

        AssertTokenForwarded(first, token);
        AssertTokenForwarded(last, token);
        AssertTokenForwarded(single, token);
        AssertTokenForwarded(elementAt, token);
        AssertTokenForwarded(min, token);
        AssertTokenForwarded(max, token);
        AssertTokenForwarded(toNonEmpty, token);
        AssertTokenForwarded(partition, token);
        AssertTokenForwarded(resultPartition, token);
        AssertTokenForwarded(traverse, token);
        AssertTokenForwarded(traverseValue, token);
    }

    [Fact]
    public async Task PreCanceledTokenSurfacesOperationCanceledExceptionAndNeverAbsence()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var token = cancellationSource.Token;
        var first = new ProbeAsyncEnumerable<int>([1, 2, 3], cancelOnMoveNextToken: token);
        var partition = new ProbeAsyncEnumerable<int>([1, 2], cancelOnMoveNextToken: token);
        var resultPartition = new ProbeAsyncEnumerable<Result<int, string>>(
            [Result<int, string>.Success(1)],
            cancelOnMoveNextToken: token);
        var traverse = new ProbeAsyncEnumerable<int>([1, 2], cancelOnMoveNextToken: token);
        var traverseValue = new ProbeAsyncEnumerable<int>([1, 2], cancelOnMoveNextToken: token);

        await AssertCanceledAsync(first.FirstOrNoneAsync(token).AsTask(), token);
        await AssertCanceledAsync(
            partition.PartitionAsync(static value => value > 0, token).AsTask(),
            token);
        await AssertCanceledAsync(resultPartition.PartitionAsync(token).AsTask(), token);
        await AssertCanceledAsync(
            traverse.TraverseAsync(
                Location.Root,
                static (location, value) => Validation<int, string>.Valid(value),
                token).AsTask(),
            token);
        await AssertCanceledAsync(
            traverseValue.TraverseValueAsync(
                Location.Root,
                static (location, value) => ValueTask.FromResult(Validation<int, string>.Valid(value)),
                token).AsTask(),
            token);
    }

    [Fact]
    public async Task PartitionAsyncSplitsByPredicateInSourceOrderWithOneEnumeration()
    {
        var source = new ProbeAsyncEnumerable<int>([1, 2, 3, 4, 5]);

        var partition = await source.PartitionAsync(static value => value % 2 == 1);

        Assert.Equal([1, 3, 5], partition.True);
        Assert.Equal([2, 4], partition.False);
        AssertProbeCompleted(source, 5);
    }

    [Fact]
    public async Task PartitionAsyncSplitsResultsIntoPassedAndFailedInSourceOrder()
    {
        var source = new ProbeAsyncEnumerable<Result<int, string>>(
        [
            Result<int, string>.Success(1),
            Result<int, string>.Failure("a"),
            Result<int, string>.Success(2),
            Result<int, string>.Failure("b"),
        ]);

        var partition = await source.PartitionAsync();

        Assert.Equal([1, 2], partition.Passed);
        Assert.Equal(["a", "b"], partition.Failed);
        AssertProbeCompleted(source, 4);
    }

    [Fact]
    public async Task LocatedTraverseAsyncPassesIndexedLocationsToTheSelector()
    {
        var locations = new List<string>();
        var source = new ProbeAsyncEnumerable<int>([10, 20, 30]);

        var validation = await source.TraverseAsync(
            Location.Root.Property("root"),
            (location, value) =>
            {
                locations.Add(location.ToString());
                return Validation<int, string>.Valid(value);
            });

        Assert.Equal(["root[0]", "root[1]", "root[2]"], locations);
        Assert.True(validation.TryGetValue(out var values));
        Assert.Equal([10, 20, 30], values!);
        AssertProbeCompleted(source, 3);
    }

    [Fact]
    public async Task LocatedTraverseAsyncAccumulatesValidationErrorsInSourceOrder()
    {
        var source = new ProbeAsyncEnumerable<int>([1, 2, 3, 4]);

        var validation = await source.TraverseAsync(
            Location.Root.Property("root"),
            (location, value) => value % 2 == 0
                ? Validation<int, string>.Invalid($"{location}:even")
                : Validation<int, string>.Valid(value));

        Assert.True(validation.TryGetErrors(out var errors));
        Assert.Equal(["root[1]:even", "root[3]:even"], errors);
        AssertProbeCompleted(source, 4);
    }

    [Fact]
    public async Task LocatedTraverseAsyncReturnsAnEmptyValidListForAnEmptySource()
    {
        var validation = await AsyncValues<int>().TraverseAsync(
            Location.Root.Property("root"),
            static (location, value) => Validation<int, string>.Valid(value));

        Assert.True(validation.TryGetValue(out var values));
        Assert.Empty(values!);
    }

    [Fact]
    public async Task LocatedTraverseAsyncResultFormFailsFastAtTheFirstFailure()
    {
        var selectorValues = new List<int>();
        var source = new ProbeAsyncEnumerable<int>([1, 2, 3]);

        var result = await source.TraverseAsync(
            Location.Root.Property("root"),
            (location, value) =>
            {
                selectorValues.Add(value);
                return value == 2
                    ? Result<int, string>.Failure($"{location}:bad")
                    : Result<int, string>.Success(value);
            });

        Assert.True(result.TryGetError(out var error));
        Assert.Equal("root[1]:bad", error);
        Assert.Equal([1, 2], selectorValues);
        AssertProbeCompleted(source, 2);
    }

    [Fact]
    public async Task LocatedTraverseAsyncOptionAndUnitResultFormsFailFast()
    {
        var some = await AsyncValues(1, 2).TraverseAsync(
            Location.Root.Property("root"),
            static (location, value) => Option<int>.Some(value * 10));
        var none = await AsyncValues(1, 2, 3).TraverseAsync(
            Location.Root.Property("root"),
            static (location, value) => value == 2 ? Option<int>.None : Option<int>.Some(value));
        var unitSuccess = await AsyncValues(1, 2).TraverseAsync(
            Location.Root.Property("root"),
            static (location, value) => UnitResult<string>.Success());
        var unitFailure = await AsyncValues(1, 2, 3).TraverseAsync(
            Location.Root.Property("root"),
            static (location, value) => value == 2
                ? UnitResult<string>.Failure($"{location}:bad")
                : UnitResult<string>.Success());

        Assert.True(some.TryGetValue(out var someValues));
        Assert.Equal([10, 20], someValues!);
        Assert.True(none.IsNone);
        Assert.True(unitSuccess.IsSuccess);
        Assert.True(unitFailure.TryGetError(out var error));
        Assert.Equal("root[1]:bad", error);
    }

    [Fact]
    public async Task LocatedTraverseValueAsyncConsumesEachSelectorValueTaskOnce()
    {
        var selectorValue = new CountingValueTaskSource<Validation<int, string>>(
            Validation<int, string>.Valid(7));
        var source = new ProbeAsyncEnumerable<int>([1]);

        var validation = await source.TraverseValueAsync(
            Location.Root,
            (location, value) => selectorValue.CreateValueTask());

        Assert.True(validation.TryGetValue(out var values));
        Assert.Equal([7], values!);
        Assert.Equal(1, selectorValue.GetResultCount);
        AssertProbeCompleted(source, 1);
    }

    [Fact]
    public async Task LocatedTraverseValueAsyncAccumulatesInSourceOrderWithCorrectLocations()
    {
        var selectorValues = new List<int>();
        var locations = new List<string>();
        var source = new ProbeAsyncEnumerable<int>([1, 2, 3]);

        var validation = await source.TraverseValueAsync(
            Location.Root.Property("root"),
            (location, value) =>
            {
                selectorValues.Add(value);
                locations.Add(location.ToString());
                return ValueTask.FromResult(
                    value % 2 == 1
                        ? Validation<int, string>.Invalid($"{location}:odd")
                        : Validation<int, string>.Valid(value));
            });

        Assert.True(validation.TryGetErrors(out var errors));
        Assert.Equal(["root[0]:odd", "root[2]:odd"], errors);
        Assert.Equal([1, 2, 3], selectorValues);
        Assert.Equal(["root[0]", "root[1]", "root[2]"], locations);
        AssertProbeCompleted(source, 3);
    }

    [Fact]
    public async Task WhereNotNullPreservesFilteredOrderForClassesAndNullableStructs()
    {
        var strings = await AsyncValues<string?>("a", null, "b", null, "c").WhereNotNull().ToListAsync();
        var numbers = await AsyncValues<int?>(1, null, 2, null, 3).WhereNotNull().ToListAsync();

        Assert.Equal(["a", "b", "c"], strings);
        Assert.Equal([1, 2, 3], numbers);
    }

    [Fact]
    public async Task WhereNotNullDefersEnumerationUntilFirstPull()
    {
        var strings = new ProbeAsyncEnumerable<string?>(["a", null, "b"]);
        var numbers = new ProbeAsyncEnumerable<int?>([1, null, 2]);
        var stringPipeline = strings.WhereNotNull();
        var numberPipeline = numbers.WhereNotNull();

        Assert.Equal(0, strings.EnumeratorCount);
        Assert.Equal(0, numbers.EnumeratorCount);

        var stringEnumerator = stringPipeline.GetAsyncEnumerator();
        try
        {
            Assert.Equal(0, strings.EnumeratorCount);
            Assert.True(await stringEnumerator.MoveNextAsync());
            Assert.Equal("a", stringEnumerator.Current);
            Assert.Equal(1, strings.EnumeratorCount);
            Assert.Equal(1, strings.ItemsYielded);
        }
        finally
        {
            await stringEnumerator.DisposeAsync();
        }

        Assert.Equal(1, strings.DisposeCount);

        var numberEnumerator = numberPipeline.GetAsyncEnumerator();
        try
        {
            Assert.Equal(0, numbers.EnumeratorCount);
            Assert.True(await numberEnumerator.MoveNextAsync());
            Assert.Equal(1, numberEnumerator.Current);
            Assert.Equal(1, numbers.EnumeratorCount);
        }
        finally
        {
            await numberEnumerator.DisposeAsync();
        }

        Assert.Equal(1, numbers.DisposeCount);
    }

    [Fact]
    public async Task WhereNotNullEnumeratesOncePerConsumption()
    {
        var strings = new ProbeAsyncEnumerable<string?>(["a", null, "b"]);
        var numbers = new ProbeAsyncEnumerable<int?>([1, null, 2]);
        var stringPipeline = strings.WhereNotNull();
        var numberPipeline = numbers.WhereNotNull();

        Assert.Equal(["a", "b"], await stringPipeline.ToListAsync());
        Assert.Equal(["a", "b"], await stringPipeline.ToListAsync());
        Assert.Equal([1, 2], await numberPipeline.ToListAsync());
        Assert.Equal([1, 2], await numberPipeline.ToListAsync());

        Assert.Equal(2, strings.EnumeratorCount);
        Assert.Equal(2, strings.DisposeCount);
        Assert.Equal(2, numbers.EnumeratorCount);
        Assert.Equal(2, numbers.DisposeCount);
    }

    [Fact]
    public async Task LargeAsyncSourcesCompleteIteratively()
    {
        const int count = 100_000;
        var source = AsyncRange(count);

        var nonEmpty = await source.ToNonEmptyOrNoneAsync();
        var max = await source.MaxOrNoneAsync();
        var validation = await source.TraverseAsync(
            Location.Root.Property("values"),
            static (location, value) => Validation<int, string>.Valid(value));

        Assert.True(nonEmpty.TryGetValue(out var nonEmptyItems));
        var items = nonEmptyItems!;
        Assert.True(max.TryGetValue(out var maxValue));
        Assert.True(validation.TryGetValue(out var validValues));
        var traversed = validValues!;

        Assert.Equal(count, items.Count);
        Assert.Equal(0, items.First);
        Assert.Equal(count - 1, items.Rest.Count);
        Assert.Equal(count - 1, items.Rest[^1]);
        Assert.Equal(count - 1, maxValue);
        Assert.Equal(count, traversed.Count);
        Assert.Equal(0, traversed[0]);
        Assert.Equal(count - 1, traversed[^1]);
    }

    [Fact]
    public async Task AsyncBatchValidationComposesNestedErrorLocations()
    {
        var customers = AsyncValues(
            new CustomerRow([new AddressRow("12345")]),
            new CustomerRow([new AddressRow(""), new AddressRow("67890")]));

        var validation = await customers.TraverseValueAsync(
            Location.Root.Property("customers"),
            ValidateCustomerAsync);

        Assert.True(validation.TryGetErrors(out var errors));
        var error = Assert.Single(errors!);
        Assert.Equal("customers[1].addresses[0].postalCode", error.Location.ToString());
        Assert.Equal("The postal code must be five digits.", error.Message);

        var valid = await AsyncValues(
                new CustomerRow([new AddressRow("12345"), new AddressRow("67890")]))
            .TraverseValueAsync(Location.Root.Property("customers"), ValidateCustomerAsync);

        Assert.True(valid.TryGetValue(out var records));
        var record = Assert.Single(records!);
        Assert.Equal(["12345", "67890"], record.Addresses.Select(static address => address.PostalCode));
    }

    private static async IAsyncEnumerable<T> AsyncValues<T>(params T[] values)
    {
        foreach (var value in values)
        {
            yield return value;
            await Task.Yield();
        }
    }

    private static async IAsyncEnumerable<int> AsyncRange(int count)
    {
        await Task.Yield();
        for (var value = 0; value < count; value++)
        {
            yield return value;
        }
    }

    private static void AssertProbeCompleted<T>(ProbeAsyncEnumerable<T> probe, int yieldedItems)
    {
        Assert.Equal(1, probe.EnumeratorCount);
        Assert.Equal(yieldedItems, probe.ItemsYielded);
        Assert.Equal(1, probe.DisposeCount);
    }

    private static void AssertTokenForwarded<T>(
        ProbeAsyncEnumerable<T> source,
        CancellationToken cancellationToken)
    {
        Assert.Equal(cancellationToken, source.ReceivedToken);
        Assert.Equal(1, source.DisposeCount);
    }

    private static async Task AssertCanceledAsync(Task operation, CancellationToken cancellationToken)
    {
        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation);

        Assert.True(operation.IsCanceled);
        Assert.Equal(cancellationToken, exception.CancellationToken);
    }

    private static async ValueTask<Validation<CustomerRecord, ImportError>> ValidateCustomerAsync(
        Location customerLocation,
        CustomerRow customer)
    {
        var addresses = await AsyncValues(customer.Addresses).TraverseAsync(
            customerLocation.Property("addresses"),
            ValidateAddress);

        return addresses.Map(values => new CustomerRecord(values));
    }

    private static Validation<AddressRecord, ImportError> ValidateAddress(
        Location addressLocation,
        AddressRow address) =>
        ParsePostalCode(address.PostalCode)
            .MapErrors(error => error.At(addressLocation.Property("postalCode")))
            .Map(postalCode => new AddressRecord(postalCode));

    private static Validation<string, ImportError> ParsePostalCode(string postalCode) =>
        postalCode.Length == 5 && postalCode.All(char.IsDigit)
            ? Validation<string, ImportError>.Valid(postalCode)
            : Validation<string, ImportError>.Invalid(new ImportError(
                "The postal code must be five digits.",
                Location.Root));

    private sealed record AddressRow(string PostalCode);

    private sealed record CustomerRow(AddressRow[] Addresses);

    private sealed record AddressRecord(string PostalCode);

    private sealed record CustomerRecord(IReadOnlyList<AddressRecord> Addresses);

    private sealed record ImportError(string Message, Location Location)
    {
        public ImportError At(Location location) => new(Message, location);
    }

    private sealed class ProbeAsyncEnumerable<T>(
        IReadOnlyList<T> values,
        CancellationToken? cancelOnMoveNextToken = null,
        Exception? moveNextException = null,
        Exception? disposeException = null) : IAsyncEnumerable<T>
    {
        public int EnumeratorCount { get; private set; }

        public int MoveNextCount { get; private set; }

        public int ItemsYielded { get; private set; }

        public int DisposeCount { get; private set; }

        public CancellationToken ReceivedToken { get; private set; }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            EnumeratorCount++;
            ReceivedToken = cancellationToken;
            return new Enumerator(this, values, cancelOnMoveNextToken, moveNextException, disposeException);
        }

        private sealed class Enumerator(
            ProbeAsyncEnumerable<T> owner,
            IReadOnlyList<T> values,
            CancellationToken? cancelOnMoveNextToken,
            Exception? moveNextException,
            Exception? disposeException) : IAsyncEnumerator<T>
        {
            private int index = -1;

            public T Current => values[index];

            public ValueTask<bool> MoveNextAsync()
            {
                owner.MoveNextCount++;
                if (cancelOnMoveNextToken is { } token)
                {
                    return ValueTask.FromCanceled<bool>(token);
                }

                if (moveNextException is not null)
                {
                    return ValueTask.FromException<bool>(moveNextException);
                }

                var nextIndex = index + 1;
                if (nextIndex == values.Count)
                {
                    return ValueTask.FromResult(false);
                }

                index = nextIndex;
                owner.ItemsYielded++;
                return ValueTask.FromResult(true);
            }

            public ValueTask DisposeAsync()
            {
                owner.DisposeCount++;
                return disposeException is null
                    ? ValueTask.CompletedTask
                    : ValueTask.FromException(disposeException);
            }
        }
    }
}

#pragma warning restore xUnit1051
#pragma warning restore FS0017
