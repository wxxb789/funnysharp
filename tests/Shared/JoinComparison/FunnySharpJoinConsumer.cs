using FunnySharp.TestSupport;

namespace FunnySharp.TestSupport.JoinComparison;

internal static class FunnySharpJoinConsumer
{
    internal static JoinReport Join(JoinInput input)
    {
        var result = input.Orders.JoinExactlyOne(
            input.Customers,
            static order => order.CustomerId!,
            static customer => customer.Id,
            static (order, customer) => new JoinedOrder(order.Id, customer.Name),
            input.Comparer);

        return result.Match(
            static outputs => new JoinReport(outputs, Array.Empty<JoinFailure>()),
            static errors => new JoinReport(
                Array.Empty<JoinedOrder>(),
                errors.Select(MapFailure).ToArray()));
    }

    internal static IReadOnlyList<StreamOutcome> Stream(JoinInput input)
    {
        LookupIndex<string, JoinCustomer> index = input.Customers
            .ToLookupIndex(static customer => customer.Id, input.Comparer)
            .Require();

        return input.Orders.JoinExactlyOneResults(
                index,
                static order => order.CustomerId!,
                static (order, customer) => new JoinedOrder(order.Id, customer.Name))
            .Select(
                static outcome => outcome.Match(
                    static value => StreamOutcome.Success(value),
                    static error => StreamOutcome.Failed(MapFailure(error))))
            .ToArray();
    }

    private static JoinFailure MapFailure(LookupError<string> error) =>
        new(
            error.Kind,
            error.Key,
            error.SourceIndex,
            error.LookupSourceIndex,
            error.FirstLookupSourceIndex);
}
