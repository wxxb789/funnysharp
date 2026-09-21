#pragma warning disable FS0017

namespace FunnySharp.Tests;

public sealed class LocationTests
{
    [Fact]
    public void LocationRendersRootPropertyIndexAndTheCanonicalNestedPath()
    {
        Assert.Equal(string.Empty, Location.Root.ToString());
        Assert.Equal("customers", Location.Root.Property("customers").ToString());
        Assert.Equal("customers[17]", Location.Root.Property("customers").At(17).ToString());
        Assert.Equal(
            "customers[17].addresses",
            Location.Root.Property("customers").At(17).Property("addresses").ToString());
        Assert.Equal(
            "customers[17].addresses[2].postalCode",
            Location.Root.Property("customers").At(17).Property("addresses").At(2).Property("postalCode").ToString());
    }

    [Fact]
    public void LocationRendersQuotedStringKeysAndKeyStringRepresentations()
    {
        Assert.Equal("[\"alice\"]", Location.Root.Key("alice").ToString());
        Assert.Equal("[42]", Location.Root.Key((object)42).ToString());
        Assert.Equal("users[\"alice\"]", Location.Root.Property("users").Key("alice").ToString());
        Assert.Equal("[\"a\"][\"b\"]", Location.Root.Key("a").Key("b").ToString());
        Assert.Equal("[\"17\"]", Location.Root.Key((object)"17").ToString());
        Assert.Equal("[17]", Location.Root.At(17).ToString());
    }

    [Fact]
    public void NestComposesOuterAndInnerPaths()
    {
        var outer = Location.Root.Property("a").At(1);
        var inner = Location.Root.Property("b").At(2);

        Assert.Equal("a[1].b[2]", outer.Nest(inner).ToString());
        Assert.Equal("a[1]", outer.Nest(Location.Root).ToString());
        Assert.True(outer.Nest(Location.Root) == outer);
        Assert.True(Location.Root.Nest(inner) == inner);
        Assert.Equal("b[2]", Location.Root.Nest(inner).ToString());
    }

    [Fact]
    public void NestingIsAssociative()
    {
        var first = Location.Root.Property("a").At(1);
        var second = Location.Root.Key("k");
        var third = Location.Root.Property("c");
        var leftAssociated = first.Nest(second).Nest(third);
        var rightAssociated = first.Nest(second.Nest(third));

        Assert.Equal("a[1][\"k\"].c", leftAssociated.ToString());
        Assert.Equal("a[1][\"k\"].c", rightAssociated.ToString());
        Assert.True(leftAssociated == rightAssociated);
        Assert.True(leftAssociated.Equals(rightAssociated));
    }

    [Fact]
    public void AtPropertyKeyAndNestReturnNewLocationsAndLeaveTheOriginalUnchanged()
    {
        var original = Location.Root.Property("customers");
        var before = original.ToString();

        var at = original.At(17);
        var property = original.Property("name");
        var key = original.Key("alice");
        var nested = original.Nest(Location.Root.Property("other"));

        Assert.Equal("customers", before);
        Assert.Equal("customers", original.ToString());
        Assert.NotSame(original, at);
        Assert.NotSame(original, property);
        Assert.NotSame(original, key);
        Assert.NotSame(original, nested);
        Assert.Equal("customers[17]", at.ToString());
        Assert.Equal("customers.name", property.ToString());
        Assert.Equal("customers[\"alice\"]", key.ToString());
        Assert.Equal("customers.other", nested.ToString());
    }

    [Fact]
    public void LocationsCompareBySegmentsAndEqualLocationsShareHashCodes()
    {
        var first = Location.Root.Property("customers").At(17);
        var second = Location.Root.Property("customers").At(17);
        var different = Location.Root.Property("customers").At(18);

        Assert.True(first == second);
        Assert.True(first.Equals(second));
        Assert.True(first.Equals((object)second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.True(first != different);
        Assert.True(first != Location.Root.Key((object)17));
        Assert.True(Location.Root.At(1) != Location.Root.Key((object)1));
        Assert.True(Location.Root == Location.Root);
    }

    [Fact]
    public void EqualLocationsWithDifferentlyRenderedKeysShareHashCodesAndHashSetLookups()
    {
        var first = Location.Root.Property("customers").Key(new LabelledKey(1, "alpha"));
        var second = Location.Root.Property("customers").Key(new LabelledKey(1, "beta"));

        Assert.True(first.Equals(second));
        Assert.Equal("customers[alpha]", first.ToString());
        Assert.Equal("customers[beta]", second.ToString());
        Assert.Equal(first.GetHashCode(), second.GetHashCode());

        var set = new HashSet<Location> { first };
        Assert.Contains(second, set);
        Assert.False(set.Add(second));
    }

    [Fact]
    public void LocationRejectsNegativeIndexesAndNullOrEmptyNamesAndKeys()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Location.Root.At(-1));
        Assert.Throws<ArgumentNullException>(() => Location.Root.Property(null!));
        Assert.Throws<ArgumentNullException>(() => Location.Root.Key((string?)null!));
        Assert.Throws<ArgumentNullException>(() => Location.Root.Key((object?)null!));
        Assert.Throws<ArgumentException>(() => Location.Root.Property(""));
        Assert.Throws<ArgumentException>(() => Location.Root.Key(""));
        Assert.Throws<ArgumentNullException>(() => Location.Root.Nest(null!));
    }

    [Fact]
    public void ToStringCachesTheRenderedPath()
    {
        var location = Location.Root.Property("customers").At(17).Property("addresses");

        Assert.Same(location.ToString(), location.ToString());
        Assert.Same(Location.Root.ToString(), Location.Root.ToString());
    }

    private sealed class LabelledKey(int id, string label)
    {
        public int Id { get; } = id;

        public override bool Equals(object? obj) => obj is LabelledKey other && Id == other.Id;

        public override int GetHashCode() => Id;

        public override string ToString() => label;
    }
}
