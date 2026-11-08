using FunnySharp;

namespace FunnySharp.DocumentationSamples;

internal static class CardinalityJoinSamples
{
    private static UniqueLookupIndex<string, Customer> BuildUniqueIndex(
        IEnumerable<Customer> customers)
    {
        // <snippet DocumentationSamples.CardinalityJoins.BuildUniqueIndex>
        var customersById = customers
            .ToUniqueLookupIndex(
                static customer => customer.Id,
                StringComparer.OrdinalIgnoreCase)
            .Match(
                static index => index,
                static errors => throw new InvalidOperationException(
                    string.Join("; ", errors.Select(static error => error.ToString()))));
        // </snippet>

        return customersById;
    }

    private static void JoinOrders(
        IEnumerable<Order> orders,
        IEnumerable<Customer> customers)
    {
        // <snippet DocumentationSamples.CardinalityJoins.JoinOrders>
        var enriched = orders.JoinExactlyOne(
            customers,
            static order => order.CustomerId,
            static customer => customer.Id,
            static (order, customer) => new EnrichedOrder(order.Id, customer.Name),
            StringComparer.OrdinalIgnoreCase);

        enriched.Match(
            static outputs => Console.WriteLine($"{outputs.Count} orders enriched."),
            static errors => Console.WriteLine($"{errors.Count} relation failures."));
        // </snippet>
    }

    private static void StreamRows(
        IEnumerable<Order> orders,
        IEnumerable<Customer> customers)
    {
        // <snippet DocumentationSamples.CardinalityJoins.StreamRows>
        var index = customers
            .ToLookupIndex(static customer => customer.Id, StringComparer.OrdinalIgnoreCase)
            .Match(
                static value => value,
                static errors => throw new InvalidOperationException(
                    string.Join("; ", errors.Select(static error => error.ToString()))));

        foreach (var outcome in orders.JoinExactlyOneResults(
            index,
            static order => order.CustomerId,
            static (order, customer) => new EnrichedOrder(order.Id, customer.Name)))
        {
            outcome.Match(
                static enriched => Console.WriteLine($"enriched {enriched.Id}"),
                static error => Console.WriteLine(error));
        }
        // </snippet>
    }

    internal sealed record Customer(string Id, string Name);

    internal sealed record Order(string Id, string CustomerId);

    internal sealed record EnrichedOrder(string Id, string CustomerName);
}
