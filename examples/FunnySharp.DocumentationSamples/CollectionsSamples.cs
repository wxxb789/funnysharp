using System.Globalization;
using FunnySharp;

namespace FunnySharp.DocumentationSamples;

internal static class CollectionsSamples
{
    private static void NonEmptyAggregate()
    {
        var quantities = new[] { 1, 2, 3 };

        // <snippet DocumentationSamples.Collections.NonEmptyAggregate>
        Option<NonEmpty<int>> amounts = quantities.ToNonEmptyOrNone();
        int total = amounts.Match(
            nonEmpty => nonEmpty.Aggregate(static (left, right) => left + right),
            () => 0);
        // </snippet>
    }

    private static void PartitionOrders()
    {
        var orders = new[]
        {
            new Order("sku-1", true),
            new Order("sku-2", false),
        };

        // <snippet DocumentationSamples.Collections.Partition>
        var (shippable, cancelled) = orders.Partition(order => order.IsPaid);
        // </snippet>
    }

    private static void ZipExact()
    {
        var skus = new[] { "sku-1", "sku-2" };
        var quantities = new[] { 1, 2 };

        // <snippet DocumentationSamples.Collections.ZipExact>
        Option<IReadOnlyList<(string First, int Second)>> paired = skus.ZipExactOrNone(quantities);

        Result<IReadOnlyList<(string First, int Second)>, string> strict = skus.ZipExact(
            quantities,
            (skuCount, quantityCount) => $"skus ({skuCount}) and quantities ({quantityCount}) differ");
        // </snippet>
    }

    private static void ParseBridges()
    {
        var text = "123456";

        // <snippet DocumentationSamples.Collections.ParseBridges>
        Option<int> quantity = text.ParseIntOrNone();
        Option<DateTime> stamped = text.ParseOrNone<DateTime>(CultureInfo.InvariantCulture);
        // </snippet>
    }

    private static void LocatedTraverse()
    {
        var customers = new[]
        {
            new CustomerRow([new AddressRow("12345")]),
            new CustomerRow([new AddressRow(""), new AddressRow("67890")]),
        };

        // <snippet DocumentationSamples.Collections.LocatedTraverse>
        #pragma warning disable FS0017

        Validation<IReadOnlyList<CustomerRecord>, ImportError> validated = customers.Traverse(
            Location.Root.Property("customers"),
            (customerLocation, customer) => customer.Addresses
                .Traverse(customerLocation.Property("addresses"), ValidateAddress)
                .Map(addresses => new CustomerRecord([.. addresses])));

        static Validation<AddressRecord, ImportError> ValidateAddress(
            Location addressLocation, AddressRow address) =>
            ParsePostalCode(address.PostalCode)
                .MapErrors(error => error.At(addressLocation.Property("postalCode")))
                .Map(postalCode => new AddressRecord(postalCode));
        // </snippet>
    }

    private static Validation<string, ImportError> ParsePostalCode(string postalCode) =>
        postalCode.Length == 5 && postalCode.All(char.IsDigit)
            ? Validation<string, ImportError>.Valid(postalCode)
            : Validation<string, ImportError>.Invalid(new ImportError(
                "The postal code must be five digits.",
                Location.Root));

    private sealed record Order(string Sku, bool IsPaid);

    private sealed record AddressRow(string PostalCode);

    private sealed record AddressRecord(string PostalCode);

    private sealed record CustomerRow(AddressRow[] Addresses);

    private sealed record CustomerRecord(IReadOnlyList<AddressRecord> Addresses);

    private sealed record ImportError(string Message, Location Location)
    {
        public ImportError At(Location location) => new(Message, location);
    }

#pragma warning restore FS0017
}
