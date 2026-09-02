using System.Collections.Frozen;
using System.Collections.Immutable;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using FunnySharp;

namespace FunnySharp.Benchmarks;

[ShortRunJob]
[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class ImmutableUpdateBenchmarks
{
    private Customer customer = null!;
    private string cityReplacement = null!;
    private Lens<Customer, string> customerCity;

    private Order order = null!;
    private int firstQuantityReplacement;
    private int secondQuantityReplacement;
    private Lens<Order, ImmutableList<LineItem>> orderLineItems;
    private Func<ImmutableList<LineItem>, ImmutableList<LineItem>> collectionBatchUpdater = null!;

    private ImmutableDictionary<string, Address> addressBook = null!;
    private string existingAddressKey = null!;
    private Address existingAddressReplacement = null!;
    private Optional<ImmutableDictionary<string, Address>, Address> existingAddress;
    private Func<Address, Address> existingAddressUpdater = null!;

    private ImmutableDictionary<string, Address> missingAddressBook = null!;
    private string missingAddressKey = null!;
    private Optional<ImmutableDictionary<string, Address>, Address> missingAddress;
    private Func<Address, Address> missingAddressUpdater = null!;

    private FrozenDictionary<string, int> frozenNumbers = null!;
    private string frozenExistingKey = null!;
    private int frozenFallbackValue;

    [GlobalSetup]
    public void Setup()
    {
        cityReplacement = "Paris";
        customer = new Customer(new Profile(new Address("London")));

        var profile = Lens.Create<Customer, Profile>(
            source => source.Profile,
            (source, value) => source with { Profile = value });
        var address = Lens.Create<Profile, Address>(
            source => source.Address,
            (source, value) => source with { Address = value });
        var city = Lens.Create<Address, string>(
            source => source.City,
            (source, value) => source with { City = value });
        customerCity = profile.Compose(address).Compose(city);

        firstQuantityReplacement = 4;
        secondQuantityReplacement = 7;
        order = new Order(ImmutableList.Create(
            new LineItem("SKU-1", 1),
            new LineItem("SKU-2", 2),
            new LineItem("SKU-3", 3)));
        orderLineItems = Lens.Create<Order, ImmutableList<LineItem>>(
            source => source.LineItems,
            (source, value) => source with { LineItems = value });
        collectionBatchUpdater = ApplyLineItemBatch;

        existingAddressKey = "home";
        existingAddressReplacement = new Address("Berlin");
        addressBook = ImmutableDictionary<string, Address>.Empty.Add(
            existingAddressKey,
            new Address("London"));
        existingAddress = Optional.Create<ImmutableDictionary<string, Address>, Address>(
            source => source.GetOption(existingAddressKey),
            (source, value) => source.SetItem(existingAddressKey, value));
        existingAddressUpdater = _ => existingAddressReplacement;

        missingAddressKey = "office";
        missingAddressBook = ImmutableDictionary<string, Address>.Empty.Add(
            existingAddressKey,
            new Address("London"));
        missingAddress = Optional.Create<ImmutableDictionary<string, Address>, Address>(
            source => source.GetOption(missingAddressKey),
            (source, value) => source.SetItem(missingAddressKey, value));
        missingAddressUpdater = _ => existingAddressReplacement;

        frozenExistingKey = "answer";
        frozenFallbackValue = -1;
        frozenNumbers = new Dictionary<string, int>
        {
            [frozenExistingKey] = 42,
        }.ToFrozenDictionary();
    }

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Nested record replacement")]
    public Customer DirectNestedRecordReplacement() => customer with
    {
        Profile = customer.Profile with
        {
            Address = customer.Profile.Address with { City = cityReplacement },
        },
    };

    [Benchmark]
    [BenchmarkCategory("Nested record replacement")]
    public Customer FunnySharpNestedRecordReplacement() => customerCity.Set(customer, cityReplacement);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Immutable collection batch update")]
    public Order DirectImmutableCollectionBatchUpdate() =>
        order with { LineItems = ApplyLineItemBatch(order.LineItems) };

    [Benchmark]
    [BenchmarkCategory("Immutable collection batch update")]
    public Order FunnySharpImmutableCollectionBatchUpdate() =>
        orderLineItems.Update(order, collectionBatchUpdater);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("ImmutableDictionary existing-key update")]
    public ImmutableDictionary<string, Address> DirectExistingKeyUpdate() =>
        addressBook.TryGetValue(existingAddressKey, out var current)
            ? addressBook.SetItem(existingAddressKey, existingAddressUpdater(current))
            : addressBook;

    [Benchmark]
    [BenchmarkCategory("ImmutableDictionary existing-key update")]
    public ImmutableDictionary<string, Address> FunnySharpExistingKeyUpdate() =>
        existingAddress.Update(addressBook, existingAddressUpdater);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Missing optional update")]
    public bool DirectMissingOptionalUpdate()
    {
        var updated = missingAddressBook.TryGetValue(missingAddressKey, out var value)
            ? missingAddressBook.SetItem(missingAddressKey, missingAddressUpdater(value))
            : missingAddressBook;
        return ReferenceEquals(missingAddressBook, updated);
    }

    [Benchmark]
    [BenchmarkCategory("Missing optional update")]
    public bool FunnySharpMissingOptionalUpdate() =>
        ReferenceEquals(missingAddressBook, missingAddress.Update(missingAddressBook, missingAddressUpdater));

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("FrozenDictionary lookup")]
    public int DirectFrozenDictionaryLookup() =>
        frozenNumbers.TryGetValue(frozenExistingKey, out var value) ? value : frozenFallbackValue;

    [Benchmark]
    [BenchmarkCategory("FrozenDictionary lookup")]
    public int FunnySharpFrozenDictionaryLookup() =>
        frozenNumbers.GetOption(frozenExistingKey).GetValueOr(frozenFallbackValue);

    private ImmutableList<LineItem> ApplyLineItemBatch(ImmutableList<LineItem> items)
    {
        var builder = items.ToBuilder();
        builder[0] = builder[0] with { Quantity = firstQuantityReplacement };
        builder[1] = builder[1] with { Quantity = secondQuantityReplacement };
        return builder.ToImmutable();
    }

    public sealed record Customer(Profile Profile);

    public sealed record Profile(Address Address);

    public sealed record Address(string City);

    public sealed record Order(ImmutableList<LineItem> LineItems);

    public sealed record LineItem(string Sku, int Quantity);
}
