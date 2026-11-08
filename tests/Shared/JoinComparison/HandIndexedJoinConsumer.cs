namespace FunnySharp.TestSupport.JoinComparison;

internal static class HandIndexedJoinConsumer
{
    internal static JoinReport Join(JoinInput input)
    {
        var byId = new Dictionary<string, JoinCustomer>(input.Comparer);
        var firstIndices = new Dictionary<string, int>(input.Comparer);
        var indexFailures = new List<JoinFailure>();

        for (var index = 0; index < input.Customers.Count; index++)
        {
            var customer = input.Customers[index];
            if (customer.Id is null)
            {
                indexFailures.Add(
                    new JoinFailure(LookupErrorKind.NullKey, null, null, index, null));
            }
            else if (!byId.TryAdd(customer.Id, customer))
            {
                var first = byId[customer.Id];
                indexFailures.Add(
                    new JoinFailure(
                        LookupErrorKind.DuplicateKey,
                        first.Id,
                        null,
                        index,
                        firstIndices[customer.Id]));
            }
            else
            {
                firstIndices[customer.Id] = index;
            }
        }

        var outputs = new List<JoinedOrder>();
        var sourceFailures = new List<JoinFailure>();
        var recordedError = indexFailures.Count > 0;

        for (var index = 0; index < input.Orders.Count; index++)
        {
            var order = input.Orders[index];
            var key = order.CustomerId;

            if (key is null)
            {
                sourceFailures.Add(new JoinFailure(LookupErrorKind.NullKey, null, index, null, null));
                recordedError = true;
            }
            else if (!byId.TryGetValue(key, out var customer))
            {
                sourceFailures.Add(new JoinFailure(LookupErrorKind.MissingKey, key, index, null, null));
                recordedError = true;
            }
            else if (!recordedError)
            {
                outputs.Add(new JoinedOrder(order.Id, customer.Name));
            }
        }

        if (indexFailures.Count == 0 && sourceFailures.Count == 0)
        {
            return new JoinReport(outputs, Array.Empty<JoinFailure>());
        }

        if (indexFailures.Count == 0)
        {
            return new JoinReport(Array.Empty<JoinedOrder>(), sourceFailures);
        }

        indexFailures.AddRange(sourceFailures);
        return new JoinReport(Array.Empty<JoinedOrder>(), indexFailures);
    }
}
