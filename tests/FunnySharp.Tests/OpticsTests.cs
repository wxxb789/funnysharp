using System.Collections.Frozen;
using System.Collections.Immutable;

namespace FunnySharp.Tests;

public sealed class OpticsTests
{
    [Fact]
    public void LensObeysGetPutPutGetPutPutAndIdentityLaws()
    {
        var lens = Lens.Create<Profile, string>(
            profile => profile.DisplayName,
            (profile, displayName) => profile with { DisplayName = displayName });
        var source = new Profile("Ada", new Address("London"));
        var first = "Grace";
        var second = "Lin";

        Assert.Equal(source, lens.Set(source, lens.Get(source)));
        Assert.Equal(first, lens.Get(lens.Set(source, first)));
        Assert.Equal(lens.Set(source, second), lens.Set(lens.Set(source, first), second));

        var identity = Lens.Identity<Profile>();
        var replacement = new Profile("Barbara", new Address("New York"));
        Assert.Equal(source, identity.Get(source));
        Assert.Equal(replacement, identity.Set(source, replacement));
        Assert.Equal(replacement, identity.Update(source, _ => replacement));
    }

    [Fact]
    public void LensCompositionReadsAndUpdatesNestedRecordsFromLeftToRight()
    {
        var profile = Lens.Create<Customer, Profile>(
            customer => customer.Profile,
            (customer, value) => customer with { Profile = value });
        var address = Lens.Create<Profile, Address>(
            value => value.Address,
            (value, next) => value with { Address = next });
        var city = Lens.Create<Address, string>(
            value => value.City,
            (value, next) => value with { City = next });
        var source = new Customer(new Profile("Ada", new Address("London")));
        var leftAssociated = profile.Compose(address).Compose(city);
        var rightAssociated = profile.Compose(address.Compose(city));

        Assert.Equal("London", leftAssociated.Get(source));
        Assert.Equal(leftAssociated.Get(source), rightAssociated.Get(source));
        Assert.Equal(leftAssociated.Set(source, "Paris"), rightAssociated.Set(source, "Paris"));
        Assert.Equal("Paris", leftAssociated.Get(leftAssociated.Set(source, "Paris")));
        Assert.Equal("LONDON", leftAssociated.Get(leftAssociated.Update(source, value => value.ToUpperInvariant())));
    }

    [Fact]
    public void OptionalReadsAndUpdatesOnlyPresentFocus()
    {
        var setterCalls = 0;
        var updaterCalls = 0;
        var addresses = ImmutableDictionary<string, Address>.Empty.Add("home", new Address("London"));
        var home = Optional.Create<ImmutableDictionary<string, Address>, Address>(
            source => source.GetOption("home"),
            (source, value) =>
            {
                setterCalls++;
                return source.SetItem("home", value);
            });
        var missing = Optional.Create<ImmutableDictionary<string, Address>, Address>(
            source => source.GetOption("office"),
            (_, _) => throw new InvalidOperationException("The missing setter must not run."));

        Assert.Equal(Option.Some(new Address("London")), home.GetOption(addresses));
        var updated = home.Set(addresses, new Address("Paris"));
        Assert.Equal(Option.Some(new Address("Paris")), home.GetOption(updated));
        Assert.Equal(new Address("London"), addresses["home"]);
        Assert.Equal(1, setterCalls);

        var afterMissingSet = missing.Set(addresses, new Address("Berlin"));
        var afterMissingUpdate = missing.Update(addresses, _ =>
        {
            updaterCalls++;
            return new Address("Berlin");
        });
        Assert.Same(addresses, afterMissingSet);
        Assert.Same(addresses, afterMissingUpdate);
        Assert.Equal(0, updaterCalls);
    }

    [Fact]
    public void OptionalCompositionSupportsRecordsAndOptionalDictionaryKeys()
    {
        var addressBook = Lens.Create<Customer, ImmutableDictionary<string, Address>>(
            customer => customer.Addresses,
            (customer, value) => customer with { Addresses = value });
        var home = Optional.Create<ImmutableDictionary<string, Address>, Address>(
            source => source.GetOption("home"),
            (source, value) => source.SetItem("home", value));
        var city = Lens.Create<Address, string>(
            address => address.City,
            (address, value) => address with { City = value });
        var country = Optional.Create<Address, string>(
            address => Option.FromNullable(address.Country),
            (address, value) => address with { Country = value });
        var source = new Customer(new Profile("Ada", new Address("London")))
        {
            Addresses = ImmutableDictionary<string, Address>.Empty.Add("home", new Address("London", "UK")),
        };
        var missingCountry = source with
        {
            Addresses = source.Addresses.SetItem("home", new Address("London")),
        };

        var homeCity = addressBook.Compose(home).Compose(city);
        var homeCountry = addressBook.Compose(home).Compose(country);
        var missingHome = source with { Addresses = ImmutableDictionary<string, Address>.Empty };
        var guardedAddressBook = Lens.Create<Customer, ImmutableDictionary<string, Address>>(
            customer => customer.Addresses,
            (_, _) => throw new InvalidOperationException("The outer setter must not run."));
        var guardedHome = guardedAddressBook.Compose(home);
        var updaterCalls = 0;

        Assert.Equal(Option.Some("London"), homeCity.GetOption(source));
        Assert.Equal(Option.Some("Paris"), homeCity.GetOption(homeCity.Set(source, "Paris")));
        Assert.Equal(Option.Some("UK"), homeCountry.GetOption(source));
        Assert.Same(missingCountry, homeCountry.Set(missingCountry, "France"));
        Assert.Same(missingHome, homeCity.Set(missingHome, "Paris"));
        Assert.Same(missingHome, guardedHome.Set(missingHome, new Address("Paris")));
        Assert.Same(missingHome, guardedHome.Update(missingHome, value =>
        {
            updaterCalls++;
            return value with { City = "Paris" };
        }));
        Assert.Equal(0, updaterCalls);
    }

    [Fact]
    public void OpticsInteroperateWithImmutableAndFrozenCollections()
    {
        var first = Lens.Create<ImmutableList<int>, int>(
            values => values[0],
            (values, value) => values.SetItem(0, value));
        var frozenDictionary = Lens.Create<FrozenSnapshot, FrozenDictionary<string, int>>(
            snapshot => snapshot.Numbers,
            (snapshot, value) => snapshot with { Numbers = value });
        var frozenSet = Lens.Create<FrozenSnapshot, FrozenSet<string>>(
            snapshot => snapshot.Tags,
            (snapshot, value) => snapshot with { Tags = value });

        Assert.Equal(9, first.Get(first.Set(ImmutableList.Create(1, 2, 3), 9)));

        var numbers = new Dictionary<string, int> { ["answer"] = 42 }.ToFrozenDictionary();
        var tags = new[] { "first", "second" }.ToFrozenSet(StringComparer.Ordinal);
        var snapshot = new FrozenSnapshot(numbers, tags);
        Assert.Equal(Option.Some(42), frozenDictionary.Get(snapshot).GetOption("answer"));
        Assert.Contains("first", (IEnumerable<string>)frozenSet.Get(snapshot));

        var replacementNumbers = new Dictionary<string, int> { ["answer"] = 43 }.ToFrozenDictionary();
        var replacementTags = new[] { "updated" }.ToFrozenSet(StringComparer.Ordinal);
        var updatedSnapshot = frozenSet.Set(frozenDictionary.Set(snapshot, replacementNumbers), replacementTags);
        Assert.Same(numbers, snapshot.Numbers);
        Assert.Same(tags, snapshot.Tags);
        Assert.Same(replacementNumbers, updatedSnapshot.Numbers);
        Assert.Same(replacementTags, updatedSnapshot.Tags);
    }

    [Fact]
    public void LensDoesNotCopyFreezeOrPreventMutableLeafAliasing()
    {
        var tags = new List<string> { "original" };
        var source = new TaggedProfile("Ada", tags);
        var lens = Lens.Create<TaggedProfile, List<string>>(
            profile => profile.Tags,
            (profile, value) => profile with { Tags = value });

        var updated = lens.Update(source, value =>
        {
            value.Add("mutated");
            return value;
        });

        Assert.NotSame(source, updated);
        Assert.Same(tags, source.Tags);
        Assert.Same(tags, updated.Tags);
        Assert.Equal(["original", "mutated"], source.Tags);
    }

    [Fact]
    public void FactoriesAndUpdatesValidateDelegatesAndDefaultOpticsRejectOperations()
    {
        Func<int, int> get = value => value;
        Func<int, int, int> set = (_, value) => value;
        Func<int, Option<int>> getOption = Option.Some;

        Assert.Throws<ArgumentNullException>(() => Lens.Create<int, int>(null!, set));
        Assert.Throws<ArgumentNullException>(() => Lens.Create<int, int>(get, null!));
        Assert.Throws<ArgumentNullException>(() => Optional.Create<int, int>(null!, set));
        Assert.Throws<ArgumentNullException>(() => Optional.Create<int, int>(getOption, null!));
        Assert.Throws<ArgumentNullException>(() => Lens.Identity<string>().Update("value", null!));
        Assert.Throws<ArgumentNullException>(() => Optional.Create<int, int>(getOption, set).Update(1, null!));

        Lens<int, int> defaultLens = default;
        Optional<int, int> defaultOptional = default;
        Assert.Throws<InvalidOperationException>(() => defaultLens.Get(1));
        Assert.Throws<InvalidOperationException>(() => defaultLens.Set(1, 2));
        Assert.Throws<InvalidOperationException>(() => defaultLens.Update(1, value => value));
        Assert.Throws<InvalidOperationException>(() => defaultLens.Update(1, null!));
        Assert.Throws<InvalidOperationException>(() => defaultLens.Compose(Lens.Identity<int>()));
        Assert.Throws<InvalidOperationException>(() => defaultLens.Compose(Optional.Create<int, int>(getOption, set)));
        Assert.Throws<InvalidOperationException>(() => defaultOptional.GetOption(1));
        Assert.Throws<InvalidOperationException>(() => defaultOptional.Set(1, 2));
        Assert.Throws<InvalidOperationException>(() => defaultOptional.Update(1, value => value));
        Assert.Throws<InvalidOperationException>(() => defaultOptional.Update(1, null!));
        Assert.Throws<InvalidOperationException>(() => defaultOptional.Compose(Lens.Identity<int>()));
        Assert.Throws<InvalidOperationException>(() => defaultOptional.Compose(Optional.Create<int, int>(getOption, set)));
        Assert.Throws<InvalidOperationException>(() => Lens.Identity<int>().Compose(defaultLens));
        Assert.Throws<InvalidOperationException>(() => Optional.Create<int, int>(getOption, set).Compose(defaultOptional));
    }

    [Fact]
    public void OpticsPreserveDelegateExceptionIdentity()
    {
        var expected = new InvalidOperationException("callback failed");
        var throwingGet = Lens.Create<int, int>(
            _ => throw expected,
            (_, value) => value);
        var throwingSet = Lens.Create<int, int>(
            value => value,
            (_, _) => throw expected);
        var throwingOptional = Optional.Create<int, int>(
            _ => Option.Some(1),
            (_, _) => throw expected);

        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() => throwingGet.Get(1)));
        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() => throwingSet.Set(1, 2)));
        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() => throwingSet.Update(1, value => value + 1)));
        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() => throwingOptional.Set(1, 2)));
    }

    [Fact]
    public void FactoriesProvideStaticTypeInferenceWitnesses()
    {
        Func<Profile, string> getName = profile => profile.DisplayName;
        Func<Profile, string, Profile> setName = (profile, value) => profile with { DisplayName = value };
        Func<ImmutableDictionary<string, Address>, Option<Address>> getHome = source => source.GetOption("home");
        Func<ImmutableDictionary<string, Address>, Address, ImmutableDictionary<string, Address>> setHome =
            (source, value) => source.SetItem("home", value);

        AssertLens(Lens.Create(getName, setName));
        AssertOptional(Optional.Create(getHome, setHome));
        AssertLens(Lens.Identity<Profile>());
    }

    private static void AssertLens<TSource, TFocus>(Lens<TSource, TFocus> lens) => _ = lens;

    private static void AssertOptional<TSource, TFocus>(Optional<TSource, TFocus> optional) => _ = optional;

    private sealed record Customer(Profile Profile)
    {
        public ImmutableDictionary<string, Address> Addresses { get; init; } = ImmutableDictionary<string, Address>.Empty;
    }

    private sealed record Profile(string DisplayName, Address Address);

    private sealed record Address(string City, string? Country = null);

    private sealed record TaggedProfile(string DisplayName, List<string> Tags);

    private sealed record FrozenSnapshot(
        FrozenDictionary<string, int> Numbers,
        FrozenSet<string> Tags);
}
