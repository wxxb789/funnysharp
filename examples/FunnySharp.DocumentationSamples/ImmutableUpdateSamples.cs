using FunnySharp;
using System.Collections.Frozen;
using System.Collections.Immutable;

namespace FunnySharp.DocumentationSamples;

internal static class ImmutableUpdateSamples
{
    private static void SetNestedLens()
    {
        var customer = CreateCustomer();

        // <snippet DocumentationSamples.ImmutableUpdates.SetNestedLens>
        var profile = Lens.Create<Customer, Profile>(
            customer => customer.Profile,
            (customer, value) => customer with { Profile = value });
        var address = Lens.Create<Profile, Address>(
            value => value.Address,
            (value, next) => value with { Address = next });
        var city = Lens.Create<Address, string>(
            value => value.City,
            (value, next) => value with { City = next });

        var customerCity = profile.Compose(address).Compose(city);
        var updated = customerCity.Set(customer, "Paris");
        // </snippet>
    }

    private static void UpdateNestedRecord()
    {
        var customer = CreateCustomer();

        // <snippet DocumentationSamples.ImmutableUpdates.UpdateNestedRecord>
        var updated = customer with
        {
            Profile = customer.Profile with
            {
                Address = customer.Profile.Address with { City = "Paris" },
            },
        };
        // </snippet>
    }

    private static void UpdateOptionalFocus()
    {
        var customer = CreateCustomer();
        var addressBook = Lens.Create<Customer, ImmutableDictionary<string, Address>>(
            value => value.Addresses,
            (value, next) => value with { Addresses = next });
        var city = Lens.Create<Address, string>(
            value => value.City,
            (value, next) => value with { City = next });

        // <snippet DocumentationSamples.ImmutableUpdates.UpdateOptionalFocus>
        var home = Optional.Create<ImmutableDictionary<string, Address>, Address>(
            addresses => addresses.TryGetValue("home", out var value)
                ? Option.Some(value)
                : Option.None<Address>(),
            (addresses, value) => addresses.SetItem("home", value));

        var homeCity = addressBook.Compose(home).Compose(city);
        var updated = homeCity.Update(customer, value => value.ToUpperInvariant());
        // </snippet>
    }

    private static void UpdateImmutableDictionary()
    {
        var customer = CreateCustomer();

        // <snippet DocumentationSamples.ImmutableUpdates.UpdateImmutableDictionary>
        var addresses = Lens.Create<Customer, ImmutableDictionary<string, Address>>(
            customer => customer.Addresses,
            (customer, value) => customer with { Addresses = value });

        var moved = addresses.Update(customer, values =>
            values.SetItem("home", new Address("Paris")));
        // </snippet>
    }

    private static void UpdateImmutableList()
    {
        var order = new Order(ImmutableList.Create(new LineItem("SKU-1", 1)));

        // <snippet DocumentationSamples.ImmutableUpdates.UpdateImmutableList>
        var lineItems = Lens.Create<Order, ImmutableList<LineItem>>(
            order => order.LineItems,
            (order, value) => order with { LineItems = value });

        var updated = lineItems.Update(order, items =>
        {
            var builder = items.ToBuilder();
            builder[0] = builder[0] with { Quantity = 2 };
            builder.Add(new LineItem("SKU-2", 1));
            return builder.ToImmutable();
        });
        // </snippet>
    }

    private static void ReplaceFrozenDictionary()
    {
        var snapshot = new Snapshot(
            new Dictionary<string, int> { ["answer"] = 42 }.ToFrozenDictionary());

        // <snippet DocumentationSamples.ImmutableUpdates.ReplaceFrozenDictionary>
        var numbers = Lens.Create<Snapshot, FrozenDictionary<string, int>>(
            snapshot => snapshot.Numbers,
            (snapshot, value) => snapshot with { Numbers = value });

        var replacement = snapshot.Numbers
            .Select(pair => pair.Key == "answer"
                ? new KeyValuePair<string, int>(pair.Key, 43)
                : pair)
            .ToFrozenDictionary();
        var updated = numbers.Set(snapshot, replacement);
        // </snippet>
    }

    private static Customer CreateCustomer() => new(
        new Profile(new Address("London")),
        ImmutableDictionary<string, Address>.Empty.Add("home", new Address("London")));

    private sealed record Customer(Profile Profile, ImmutableDictionary<string, Address> Addresses);

    private sealed record Profile(Address Address);

    private sealed record Address(string City);

    private sealed record Order(ImmutableList<LineItem> LineItems);

    private sealed record LineItem(string Sku, int Quantity);

    private sealed record Snapshot(FrozenDictionary<string, int> Numbers);
}
