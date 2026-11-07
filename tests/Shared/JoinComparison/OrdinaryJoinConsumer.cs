namespace FunnySharp.TestSupport.JoinComparison;

internal static class OrdinaryJoinConsumer
{
    internal static JoinReport Join(JoinInput input)
    {
        var byId = input.Customers.ToDictionary(static customer => customer.Id, input.Comparer);
        var outputs = new JoinedOrder[input.Orders.Count];

        for (var index = 0; index < input.Orders.Count; index++)
        {
            var order = input.Orders[index];
            outputs[index] = new JoinedOrder(order.Id, byId[order.CustomerId!].Name);
        }

        return new JoinReport(outputs, Array.Empty<JoinFailure>());
    }
}
