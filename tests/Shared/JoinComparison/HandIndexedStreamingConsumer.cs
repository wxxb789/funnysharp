namespace FunnySharp.TestSupport.JoinComparison;

internal static class HandIndexedStreamingConsumer
{
    internal static IReadOnlyList<StreamOutcome> Stream(JoinInput input)
    {
        var byId = new Dictionary<string, JoinCustomer>(input.Comparer);
        var firstIndices = new Dictionary<string, int>(input.Comparer);
        var secondIndices = new Dictionary<string, int>(input.Comparer);

        for (var index = 0; index < input.Customers.Count; index++)
        {
            var customer = input.Customers[index];
            if (customer.Id is null)
            {
                continue;
            }

            if (byId.TryAdd(customer.Id, customer))
            {
                firstIndices[customer.Id] = index;
            }
            else
            {
                secondIndices.TryAdd(customer.Id, index);
            }
        }

        var outcomes = new List<StreamOutcome>(input.Orders.Count);

        for (var index = 0; index < input.Orders.Count; index++)
        {
            var order = input.Orders[index];
            var key = order.CustomerId;

            if (key is null)
            {
                outcomes.Add(
                    StreamOutcome.Failed(
                        new JoinFailure(LookupErrorKind.NullKey, null, index, null, null)));
            }
            else if (!byId.TryGetValue(key, out var customer))
            {
                outcomes.Add(
                    StreamOutcome.Failed(
                        new JoinFailure(LookupErrorKind.MissingKey, key, index, null, null)));
            }
            else if (secondIndices.TryGetValue(key, out var secondIndex))
            {
                outcomes.Add(
                    StreamOutcome.Failed(
                        new JoinFailure(
                            LookupErrorKind.DuplicateKey,
                            key,
                            index,
                            secondIndex,
                            firstIndices[key])));
            }
            else
            {
                outcomes.Add(StreamOutcome.Success(new JoinedOrder(order.Id, customer.Name)));
            }
        }

        return outcomes;
    }
}
