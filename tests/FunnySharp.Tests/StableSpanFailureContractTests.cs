namespace FunnySharp.Tests;

public sealed class StableSpanFailureContractTests
{
    [Fact]
    public void WhereToRetainsTheWrittenPrefixWhenALaterPredicateThrows()
    {
        int[] source = [1, 2, 3];
        int[] destination = [-1, -1, -1, -1];
        var expected = new InvalidOperationException("predicate");
        var calls = 0;
        var actual = Assert.Throws<InvalidOperationException>(() =>
        {
            _ = ((ReadOnlySpan<int>)source).WhereTo(destination.AsSpan(), value =>
            {
                calls++;
                return value == 2 ? throw expected : true;
            });
        });
        Assert.Same(expected, actual);
        Assert.Equal([1, -1, -1, -1], destination);
        Assert.Equal(2, calls);
    }

    [Fact]
    public void ChooseToRetainsTheWrittenPrefixWhenALaterChooserThrows()
    {
        int[] source = [1, 2, 3];
        int[] destination = [-1, -1, -1, -1];
        var expected = new InvalidOperationException("chooser");
        var calls = 0;
        var actual = Assert.Throws<InvalidOperationException>(() =>
        {
            _ = ((ReadOnlySpan<int>)source).ChooseTo(destination.AsSpan(), value =>
            {
                calls++;
                return value == 2 ? throw expected : Option.Some(value * 10);
            });
        });
        Assert.Same(expected, actual);
        Assert.Equal([10, -1, -1, -1], destination);
        Assert.Equal(2, calls);
    }

    [Fact]
    public void SelectInPlaceRetainsCompletedProjectionsWhenALaterSelectorThrows()
    {
        int[] source = [1, 2, 3];
        var expected = new InvalidOperationException("selector");
        var calls = 0;
        var actual = Assert.Throws<InvalidOperationException>(() =>
        {
            _ = source.AsSpan().SelectInPlace(value =>
            {
                calls++;
                return value == 2 ? throw expected : value * 10;
            });
        });
        Assert.Same(expected, actual);
        Assert.Equal([10, 2, 3], source);
        Assert.Equal(2, calls);
    }

    [Fact]
    public void WhereInPlaceRetainsCompactionAndDoesNotClearTheTailOnPredicateFailure()
    {
        var removed = new object();
        var kept = new object();
        var fault = new object();
        var tail = new object();
        object[] source = [removed, kept, fault, tail];
        var expected = new InvalidOperationException("predicate");
        var calls = 0;
        var actual = Assert.Throws<InvalidOperationException>(() =>
        {
            _ = source.AsSpan().WhereInPlace(value =>
            {
                calls++;
                if (ReferenceEquals(value, fault)) throw expected;
                return !ReferenceEquals(value, removed);
            });
        });
        Assert.Same(expected, actual);
        Assert.Same(kept, source[0]);
        Assert.Same(kept, source[1]);
        Assert.Same(fault, source[2]);
        Assert.Same(tail, source[3]);
        Assert.Equal(3, calls);
    }
}
