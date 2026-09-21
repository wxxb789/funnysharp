namespace FunnySharp.Tests;

public sealed class CardinalityOrNoneTests
{
    [Fact]
    public void CardinalityMembersRejectNullSourcesEagerly()
    {
        IEnumerable<int>? source = null;

        Assert.Throws<ArgumentNullException>(() => source!.FirstOrNone());
        Assert.Throws<ArgumentNullException>(() => source!.FirstOrNone(static value => value > 0));
        Assert.Throws<ArgumentNullException>(() => source!.LastOrNone());
        Assert.Throws<ArgumentNullException>(() => source!.LastOrNone(static value => value > 0));
        Assert.Throws<ArgumentNullException>(() => source!.SingleOrNone());
        Assert.Throws<ArgumentNullException>(() => source!.SingleOrNone(static value => value > 0));
        Assert.Throws<ArgumentNullException>(() => source!.ElementAtOrNone(0));
        Assert.Throws<ArgumentNullException>(() => source!.MinOrNone());
        Assert.Throws<ArgumentNullException>(() => source!.MinOrNone(Comparer<int>.Default));
        Assert.Throws<ArgumentNullException>(() => source!.MaxOrNone());
        Assert.Throws<ArgumentNullException>(() => source!.MaxOrNone(Comparer<int>.Default));
        Assert.Throws<ArgumentNullException>(() => source!.ToNonEmptyOrNone());
    }

    [Fact]
    public void PredicateAndComparerOverloadsRejectNullArgumentsBeforeEnumeratingTheSource()
    {
        var source = new ProbeEnumerable<int>([1, 2, 3]);

        Assert.Throws<ArgumentNullException>(() => source.FirstOrNone((Func<int, bool>)null!));
        Assert.Throws<ArgumentNullException>(() => source.LastOrNone((Func<int, bool>)null!));
        Assert.Throws<ArgumentNullException>(() => source.SingleOrNone((Func<int, bool>)null!));
        Assert.Throws<ArgumentNullException>(() => source.MinOrNone((IComparer<int>)null!));
        Assert.Throws<ArgumentNullException>(() => source.MaxOrNone((IComparer<int>)null!));

        Assert.Equal(0, source.EnumeratorCount);
        Assert.Equal(0, source.ItemsYielded);
        Assert.Equal(0, source.DisposeCount);
    }

    [Fact]
    public void EmptySourcesReturnNoneForEveryMember()
    {
        var empty = Enumerable.Empty<int>();

        Assert.True(empty.FirstOrNone().IsNone);
        Assert.True(empty.FirstOrNone(static value => value > 0).IsNone);
        Assert.True(empty.LastOrNone().IsNone);
        Assert.True(empty.LastOrNone(static value => value > 0).IsNone);
        Assert.True(empty.SingleOrNone().IsNone);
        Assert.True(empty.SingleOrNone(static value => value > 0).IsNone);
        Assert.True(empty.ElementAtOrNone(0).IsNone);
        Assert.True(empty.ElementAtOrNone(-1).IsNone);
        Assert.True(empty.MinOrNone().IsNone);
        Assert.True(empty.MinOrNone(Comparer<int>.Default).IsNone);
        Assert.True(empty.MaxOrNone().IsNone);
        Assert.True(empty.MaxOrNone(Comparer<int>.Default).IsNone);
        Assert.True(empty.ToNonEmptyOrNone().IsNone);
    }

    [Fact]
    public void SingletonSourcesReturnSomeForEveryMember()
    {
        var singleton = new[] { 42 };

        Assert.Equal(Option.Some(42), singleton.FirstOrNone());
        Assert.Equal(Option.Some(42), singleton.LastOrNone());
        Assert.Equal(Option.Some(42), singleton.SingleOrNone());
        Assert.Equal(Option.Some(42), singleton.ElementAtOrNone(0));
        Assert.Equal(Option.Some(42), singleton.MinOrNone());
        Assert.Equal(Option.Some(42), singleton.MaxOrNone());
        Assert.True(singleton.ToNonEmptyOrNone().TryGetValue(out var nonEmpty));
        Assert.Equal(42, nonEmpty.First);
        Assert.Empty(nonEmpty.Rest);
        Assert.Equal(1, nonEmpty.Count);
    }

    [Fact]
    public void MultipleItemsCollapseSingleOrNoneAndPreserveFirstLastMinAndMax()
    {
        var items = new[] { 3, 1, 2 };

        Assert.True(items.SingleOrNone().IsNone);
        Assert.Equal(Option.Some(3), items.FirstOrNone());
        Assert.Equal(Option.Some(2), items.LastOrNone());
        Assert.Equal(Option.Some(1), items.MinOrNone());
        Assert.Equal(Option.Some(3), items.MaxOrNone());
        Assert.True(items.ToNonEmptyOrNone().TryGetValue(out var nonEmpty));
        Assert.Equal(3, nonEmpty.First);
        Assert.Equal(new[] { 1, 2 }, nonEmpty.Rest);
        Assert.Equal(3, nonEmpty.Count);
    }

    [Fact]
    public void PredicateOverloadsReturnFirstLastAndSingleMatches()
    {
        var items = new[] { 1, 2, 3, 4, 5 };

        Assert.Equal(Option.Some(2), items.FirstOrNone(static value => value % 2 == 0));
        Assert.True(items.FirstOrNone(static value => value > 5).IsNone);
        Assert.Equal(Option.Some(4), items.LastOrNone(static value => value % 2 == 0));
        Assert.True(items.LastOrNone(static value => value > 5).IsNone);
        Assert.Equal(Option.Some(3), items.SingleOrNone(static value => value == 3));
        Assert.True(items.SingleOrNone(static value => value > 5).IsNone);
        Assert.True(items.SingleOrNone(static value => value % 2 == 0).IsNone);
    }

    [Fact]
    public void WhereToNonEmptyOrNoneDistinguishesZeroOneAndManyMatches()
    {
        var items = new[] { 1, 2, 3, 4 };

        Assert.True(items.Where(static value => value > 5).ToNonEmptyOrNone().IsNone);
        Assert.True(items
            .Where(static value => value == 2)
            .ToNonEmptyOrNone()
            .TryGetValue(out var single));
        Assert.Equal(2, single.First);
        Assert.Empty(single.Rest);
        Assert.True(items
            .Where(static value => value % 2 == 0)
            .ToNonEmptyOrNone()
            .TryGetValue(out var many));
        Assert.Equal(2, many.First);
        Assert.True(many.Rest.Count > 0);
        Assert.Equal(new[] { 2, 4 }, many.ToReadOnlyList());
    }

    [Fact]
    public void MinOrNoneAndMaxOrNoneSkipNullItemsLikeTheBcl()
    {
        string?[] withNull = ["b", null, "a"];
        string?[] allNull = [null, null];

        Assert.True(withNull.MinOrNone().TryGetValue(out var min));
        Assert.Equal("a", min);
        Assert.True(withNull.MaxOrNone().TryGetValue(out var max));
        Assert.Equal("b", max);
        Assert.True(allNull.MinOrNone().IsNone);
        Assert.True(allNull.MaxOrNone().IsNone);
    }

    [Fact]
    public void MinOrNoneAndMaxOrNoneUseTheComparerAndNeverObserveNullItems()
    {
        string?[] withNull = ["b", null, "a"];
        string?[] allNull = [null, null];
        var reverse = new ReverseStringComparer();

        Assert.True(withNull.MinOrNone(reverse).TryGetValue(out var min));
        Assert.Equal("b", min);
        Assert.True(withNull.MaxOrNone(reverse).TryGetValue(out var max));
        Assert.Equal("a", max);
        Assert.True(allNull.MinOrNone(reverse).IsNone);
        Assert.True(allNull.MaxOrNone(reverse).IsNone);
    }

    [Fact]
    public void EachMemberEnumeratesTheSourceExactlyOnceAndDisposesTheEnumerator()
    {
        var firstSource = new ProbeEnumerable<int>([1, 2, 3]);
        var firstMatchSource = new ProbeEnumerable<int>([1, 2, 3]);
        var lastSource = new ProbeEnumerable<int>([1, 2, 3]);
        var lastMatchSource = new ProbeEnumerable<int>([1, 2, 3]);
        var singleSource = new ProbeEnumerable<int>([1, 2, 3]);
        var singleMatchSource = new ProbeEnumerable<int>([1, 2, 3]);
        var elementAtSource = new ProbeEnumerable<int>([1, 2, 3]);
        var elementAtOutOfRangeSource = new ProbeEnumerable<int>([1, 2, 3]);
        var minSource = new ProbeEnumerable<int>([1, 2, 3]);
        var maxSource = new ProbeEnumerable<int>([1, 2, 3]);
        var nonEmptySource = new ProbeEnumerable<int>([1, 2, 3]);

        Assert.Equal(Option.Some(1), firstSource.FirstOrNone());
        Assert.Equal(Option.Some(2), firstMatchSource.FirstOrNone(static value => value == 2));
        Assert.Equal(Option.Some(3), lastSource.LastOrNone());
        Assert.Equal(Option.Some(1), lastMatchSource.LastOrNone(static value => value < 2));
        Assert.True(singleSource.SingleOrNone().IsNone);
        Assert.True(singleMatchSource.SingleOrNone(static value => value < 3).IsNone);
        Assert.Equal(Option.Some(2), elementAtSource.ElementAtOrNone(1));
        Assert.True(elementAtOutOfRangeSource.ElementAtOrNone(3).IsNone);
        Assert.Equal(Option.Some(1), minSource.MinOrNone());
        Assert.Equal(Option.Some(3), maxSource.MaxOrNone());
        Assert.True(nonEmptySource.ToNonEmptyOrNone().IsSome);

        AssertProbeCompleted(firstSource, 1);
        AssertProbeCompleted(firstMatchSource, 2);
        AssertProbeCompleted(lastSource, 3);
        AssertProbeCompleted(lastMatchSource, 3);
        AssertProbeCompleted(singleSource, 2);
        AssertProbeCompleted(singleMatchSource, 2);
        AssertProbeCompleted(elementAtSource, 2);
        AssertProbeCompleted(elementAtOutOfRangeSource, 3);
        AssertProbeCompleted(minSource, 3);
        AssertProbeCompleted(maxSource, 3);
        AssertProbeCompleted(nonEmptySource, 3);
    }

    [Fact]
    public void PredicateOverloadsStopInvokingPredicatesAtTheDecisiveMatch()
    {
        var items = new[] { 1, 2, 3, 4, 5 };
        var firstCalls = 0;
        var lastCalls = 0;
        var singleCalls = 0;
        var uniqueCalls = 0;

        Assert.Equal(Option.Some(2), items.FirstOrNone(value =>
        {
            firstCalls++;
            return value == 2;
        }));
        Assert.Equal(Option.Some(4), items.LastOrNone(value =>
        {
            lastCalls++;
            return value % 2 == 0;
        }));
        Assert.True(items.SingleOrNone(value =>
        {
            singleCalls++;
            return true;
        }).IsNone);
        Assert.Equal(Option.Some(3), items.SingleOrNone(value =>
        {
            uniqueCalls++;
            return value == 3;
        }));

        Assert.Equal(2, firstCalls);
        Assert.Equal(5, lastCalls);
        Assert.Equal(2, singleCalls);
        Assert.Equal(5, uniqueCalls);
    }

    [Fact]
    public void LargeSourcesAreHandledIterativelyWithoutStackOverflow()
    {
        const int count = 100_000;
        var source = Enumerable.Range(0, count);

        Assert.Equal(Option.Some(count - 1), source.LastOrNone());
        Assert.True(source.SingleOrNone().IsNone);
        Assert.Equal(Option.Some(0), source.MinOrNone());
        Assert.Equal(Option.Some(count - 1), source.MaxOrNone());
        Assert.True(source.ToNonEmptyOrNone().TryGetValue(out var nonEmpty));
        Assert.Equal(0, nonEmpty.First);
        Assert.Equal(count, nonEmpty.Count);
        Assert.Equal(count - 1, nonEmpty.Rest.Count);
        Assert.Equal(count - 1, nonEmpty.ToReadOnlyList()[^1]);
    }

    [Fact]
    public void ElementAtOrNoneSupportsArrayAndLazySources()
    {
        var array = new[] { 5, 6, 7 };
        var lazy = Enumerable.Range(10, 3);

        Assert.Equal(Option.Some(5), array.ElementAtOrNone(0));
        Assert.Equal(Option.Some(7), array.ElementAtOrNone(2));
        Assert.True(array.ElementAtOrNone(3).IsNone);
        Assert.True(array.ElementAtOrNone(-1).IsNone);
        Assert.True(array.ElementAtOrNone(int.MinValue).IsNone);

        Assert.Equal(Option.Some(10), lazy.ElementAtOrNone(0));
        Assert.Equal(Option.Some(12), lazy.ElementAtOrNone(2));
        Assert.True(lazy.ElementAtOrNone(3).IsNone);
        Assert.True(lazy.ElementAtOrNone(-1).IsNone);
        Assert.True(lazy.ElementAtOrNone(int.MinValue).IsNone);
    }

    private static void AssertProbeCompleted<T>(ProbeEnumerable<T> probe, int expectedItemsYielded)
    {
        Assert.Equal(1, probe.EnumeratorCount);
        Assert.Equal(expectedItemsYielded, probe.ItemsYielded);
        Assert.Equal(1, probe.DisposeCount);
    }

    private sealed class ReverseStringComparer : IComparer<string?>
    {
        public int Compare(string? left, string? right)
        {
            if (left is null || right is null)
            {
                throw new InvalidOperationException("The comparer must never observe a null item.");
            }

            return string.CompareOrdinal(right, left);
        }
    }
}
