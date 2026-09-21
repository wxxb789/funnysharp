namespace FunnySharp.Tests;

public sealed class NonEmptyTests
{
    private static readonly NonEmpty<int> DefaultNonEmptyField = default;

    [Fact]
    public void ToNonEmptyOrNoneReturnsNoneForAnEmptySource()
    {
        Assert.True(Enumerable.Empty<int>().ToNonEmptyOrNone().IsNone);
    }

    [Fact]
    public void ToNonEmptyOrNoneWrapsASingletonSource()
    {
        Assert.True(new[] { 42 }.ToNonEmptyOrNone().TryGetValue(out var nonEmpty));

        Assert.Equal(42, nonEmpty.First);
        Assert.Empty(nonEmpty.Rest);
        Assert.Equal(1, nonEmpty.Count);
        Assert.Equal(new[] { 42 }, nonEmpty.ToReadOnlyList());
    }

    [Fact]
    public void ToNonEmptyOrNonePreservesOrderForMultipleItems()
    {
        Assert.True(new[] { 1, 2, 3 }.ToNonEmptyOrNone().TryGetValue(out var nonEmpty));

        Assert.Equal(1, nonEmpty.First);
        Assert.Equal(new[] { 2, 3 }, nonEmpty.Rest);
        Assert.Equal(3, nonEmpty.Count);
        Assert.Equal(new[] { 1, 2, 3 }, nonEmpty.ToReadOnlyList());
    }

    [Fact]
    public void AggregateFoldsLeftToRightAndCannotRunOnAnEmptySource()
    {
        Assert.True(new[] { 7 }.ToNonEmptyOrNone().TryGetValue(out var singleton));
        Assert.True(new[] { 10, 3, 2 }.ToNonEmptyOrNone().TryGetValue(out var triple));
        Assert.True(new[] { "a", "b", "c" }.ToNonEmptyOrNone().TryGetValue(out var letters));

        Assert.Equal(7, singleton.Aggregate(static (left, right) => left - right));
        Assert.Equal(5, triple.Aggregate(static (left, right) => left - right));
        Assert.Equal("abc", letters.Aggregate(static (left, right) => left + right));

        Assert.Throws<InvalidOperationException>(() =>
            Array.Empty<int>().Aggregate(static (left, right) => left + right));
    }

    [Fact]
    public void AggregateRejectsNullFuncEagerly()
    {
        Assert.True(new[] { 1, 2 }.ToNonEmptyOrNone().TryGetValue(out var nonEmpty));

        Assert.Throws<ArgumentNullException>(() => nonEmpty.Aggregate((Func<int, int, int>)null!));
    }

    [Fact]
    public void DefaultNonEmptyThrowsForEveryMemberAccess()
    {
        NonEmpty<int> nonEmpty = DefaultNonEmptyField;

        Assert.Throws<InvalidOperationException>(() => nonEmpty.First);
        Assert.Throws<InvalidOperationException>(() => { _ = nonEmpty.Rest; });
        Assert.Throws<InvalidOperationException>(() => nonEmpty.Count);
        Assert.Throws<InvalidOperationException>(() =>
            nonEmpty.Aggregate(static (left, right) => left + right));
        Assert.Throws<InvalidOperationException>(() => { _ = nonEmpty.ToReadOnlyList(); });
        Assert.Equal("Uninitialized", nonEmpty.ToString());
    }

    [Fact]
    public void ToNonEmptyOrNoneRoundTripsSourceListsThroughToReadOnlyList()
    {
        var items = new[] { 1, 2, 3 };

        Assert.True(Array.Empty<int>().ToNonEmptyOrNone()
            .Map(static nonEmpty => nonEmpty.ToReadOnlyList())
            .IsNone);
        Assert.True(items.ToNonEmptyOrNone()
            .Map(static nonEmpty => nonEmpty.ToReadOnlyList())
            .TryGetValue(out var roundTripped));
        Assert.Equal(items, roundTripped!);
    }
}
