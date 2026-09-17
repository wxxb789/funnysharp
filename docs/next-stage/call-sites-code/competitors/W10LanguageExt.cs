using System.Collections.Immutable;
using LanguageExt;
using static LanguageExt.Prelude;

namespace CallSites.Competitors;

// W10 competitor: language-ext 4.4.9 Lens + Prelude.lens composition.
public static class W10LanguageExt
{
    public static Order ReviewPostalCode(Order order, string postalCode) =>
        OrderTags.Update(
            tags => tags.Add("address-reviewed"),
            lens(OrderAddress, AddressPostalCode).Update(_ => postalCode, order));

    private static readonly Lens<Order, Address> OrderAddress =
        Lens<Order, Address>.New(
            order => order.Customer.PrimaryAddress,
            address => order => order with
            {
                Customer = order.Customer with { PrimaryAddress = address },
            });

    private static readonly Lens<Address, string> AddressPostalCode =
        Lens<Address, string>.New(
            address => address.PostalCode,
            postalCode => address => address with { PostalCode = postalCode });

    private static readonly Lens<Order, ImmutableArray<string>> OrderTags =
        Lens<Order, ImmutableArray<string>>.New(
            order => order.Customer.Tags,
            tags => order => order with
            {
                Customer = order.Customer with { Tags = tags },
            });
}
