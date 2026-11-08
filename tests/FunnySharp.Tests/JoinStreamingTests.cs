using FunnySharp.TestSupport;

namespace FunnySharp.Tests;

public sealed class JoinStreamingTests
{
    [Fact]
    public void StreamingJoinYieldsOneOutcomePerSourceRowInSourceOrder()
    {
        LookupIndex<string, Customer> index = BuildIndex(
            new Customer("C-1", "Ada"),
            new Customer("C-2", "Grace"));
        var orders = new[]
        {
            new Order("O-1", "C-1"),
            new Order("O-2", "C-9"),
            new Order("O-3", "C-2"),
        };
        var combines = 0;

        var outcomes = orders.JoinExactlyOneResults(
                index,
                static order => order.CustomerId!,
                (order, customer) =>
                {
                    combines++;
                    return new EnrichedOrder(order.Id, customer.Name);
                })
            .ToArray();

        Assert.Equal(3, outcomes.Length);
        Assert.True(outcomes[0].TryGetValue(out var first));
        Assert.Equal(new EnrichedOrder("O-1", "Ada"), first);
        Assert.True(outcomes[1].TryGetError(out var missing));
        Assert.Equal(LookupErrorKind.MissingKey, missing.Kind);
        Assert.Equal("C-9", missing.Key);
        Assert.Equal(1, missing.SourceIndex);
        Assert.True(outcomes[2].TryGetValue(out var third));
        Assert.Equal(new EnrichedOrder("O-3", "Grace"), third);
        Assert.Equal(2, combines);
    }

    [Fact]
    public void StreamingJoinReportsAMatchedDuplicateAsOneFailureWithBothLookupPositions()
    {
        LookupIndex<string, Customer> index = BuildIndex(
            new Customer("C-1", "Ada"),
            new Customer("C-2", "Grace"),
            new Customer("C-2", "Duplicate"));
        var orders = new[]
        {
            new Order("O-1", "C-2"),
            new Order("O-2", "C-1"),
        };
        var combines = 0;

        var outcomes = orders.JoinExactlyOneResults(
                index,
                static order => order.CustomerId!,
                (order, customer) =>
                {
                    combines++;
                    return new EnrichedOrder(order.Id, customer.Name);
                })
            .ToArray();

        Assert.Equal(2, outcomes.Length);
        Assert.True(outcomes[0].TryGetError(out var duplicate));
        Assert.Equal(LookupErrorKind.DuplicateKey, duplicate.Kind);
        Assert.Equal("C-2", duplicate.Key);
        Assert.Equal(0, duplicate.SourceIndex);
        Assert.Equal(2, duplicate.LookupSourceIndex);
        Assert.Equal(1, duplicate.FirstLookupSourceIndex);
        Assert.True(outcomes[1].TryGetValue(out var second));
        Assert.Equal(new EnrichedOrder("O-2", "Ada"), second);
        Assert.Equal(1, combines);
    }

    [Fact]
    public void StreamingJoinDoesNotFailUnusedDuplicates()
    {
        LookupIndex<string, Customer> index = BuildIndex(
            new Customer("C-1", "Ada"),
            new Customer("C-2", "Grace"),
            new Customer("C-2", "Unused duplicate"));
        var orders = new[]
        {
            new Order("O-1", "C-1"),
            new Order("O-2", "C-9"),
        };

        var outcomes = orders.JoinExactlyOneResults(
                index,
                static order => order.CustomerId!,
                static (order, customer) => new EnrichedOrder(order.Id, customer.Name))
            .ToArray();

        Assert.Equal(2, outcomes.Length);
        Assert.True(outcomes[0].TryGetValue(out var first));
        Assert.Equal(new EnrichedOrder("O-1", "Ada"), first);
        Assert.True(outcomes[1].TryGetError(out var missing));
        Assert.Equal(LookupErrorKind.MissingKey, missing.Kind);
        Assert.DoesNotContain(
            outcomes,
            static outcome =>
                outcome.TryGetError(out var error) && error.Kind == LookupErrorKind.DuplicateKey);
    }

    [Fact]
    public void StreamingJoinReportsNullSourceKeysAndPassesNullIndexedValuesToCombine()
    {
        LookupIndex<string, Customer?> index = new Customer?[] { null }
            .ToLookupIndex(static _ => "C-1")
            .Require();
        var orders = new[]
        {
            new Order("O-1", "C-1"),
            new Order("O-2", null),
        };
        var combines = 0;

        var outcomes = orders.JoinExactlyOneResults(
                index,
                static order => order.CustomerId!,
                (order, customer) =>
                {
                    combines++;
                    return new EnrichedOrder(order.Id, customer?.Name ?? "none");
                })
            .ToArray();

        Assert.Equal(2, outcomes.Length);
        Assert.True(outcomes[0].TryGetValue(out var first));
        Assert.Equal(new EnrichedOrder("O-1", "none"), first);
        Assert.True(outcomes[1].TryGetError(out var nullKey));
        Assert.Equal(LookupErrorKind.NullKey, nullKey.Kind);
        Assert.Equal(1, nullKey.SourceIndex);
        Assert.Null(nullKey.LookupSourceIndex);
        Assert.Equal(1, combines);
    }

    [Fact]
    public void StreamingJoinIsDeferredAndReenumeratesOnlyTheSource()
    {
        var orders = new ProbeEnumerable<Order>(
        [
            new("O-1", "C-1"),
            new("O-2", "C-2"),
        ]);
        var customers = new ProbeEnumerable<Customer>(
        [
            new("C-1", "Ada"),
            new("C-2", "Grace"),
        ]);
        LookupIndex<string, Customer> index = customers.ToLookupIndex(static customer => customer.Id)
            .Require();

        var outcomes = orders.JoinExactlyOneResults(
            index,
            static order => order.CustomerId!,
            static (order, customer) => new EnrichedOrder(order.Id, customer.Name));

        Assert.Equal(0, orders.EnumeratorCount);
        Assert.Equal(1, customers.EnumeratorCount);

        Assert.Equal(2, outcomes.Count());
        Assert.Equal(1, orders.EnumeratorCount);
        Assert.Equal(2, orders.ItemsYielded);
        Assert.Equal(1, orders.DisposeCount);

        Assert.Equal(2, outcomes.Count());
        Assert.Equal(2, orders.EnumeratorCount);
        Assert.Equal(4, orders.ItemsYielded);
        Assert.Equal(2, orders.DisposeCount);
        Assert.Equal(1, customers.EnumeratorCount);
    }

    [Fact]
    public void StreamingJoinDisposesTheSourceOnEarlyExit()
    {
        var orders = new ProbeEnumerable<Order>(
        [
            new("O-1", "C-1"),
            new("O-2", "C-2"),
            new("O-3", "C-1"),
        ]);
        LookupIndex<string, Customer> index = BuildIndex(
            new Customer("C-1", "Ada"),
            new Customer("C-2", "Grace"));

        var outcomes = orders.JoinExactlyOneResults(
            index,
            static order => order.CustomerId!,
            static (order, customer) => new EnrichedOrder(order.Id, customer.Name));

        using var enumerator = outcomes.GetEnumerator();
        Assert.True(enumerator.MoveNext());
        enumerator.Dispose();

        Assert.Equal(1, orders.EnumeratorCount);
        Assert.Equal(1, orders.ItemsYielded);
        Assert.Equal(1, orders.DisposeCount);
    }

    [Fact]
    public void StreamingJoinRejectsNullArgumentsAtTheCallSite()
    {
        LookupIndex<string, Customer> index = BuildIndex(new Customer("C-1", "Ada"));
        var orders = Array.Empty<Order>();
        IEnumerable<Order>? nullOrders = null;
        LookupIndex<string, Customer>? nullIndex = null;

        Assert.Throws<ArgumentNullException>(
            () => nullOrders!.JoinExactlyOneResults(
                index,
                static order => order.CustomerId!,
                static (order, customer) => new EnrichedOrder(order.Id, customer.Name)));
        Assert.Throws<ArgumentNullException>(
            () => orders.JoinExactlyOneResults(
                nullIndex!,
                static order => order.CustomerId!,
                static (order, customer) => new EnrichedOrder(order.Id, customer.Name)));
        Assert.Throws<ArgumentNullException>(
            () => orders.JoinExactlyOneResults<Order, string, Customer, EnrichedOrder>(
                index,
                null!,
                static (order, customer) => new EnrichedOrder(order.Id, customer.Name)));
        Assert.Throws<ArgumentNullException>(
            () => orders.JoinExactlyOneResults<Order, string, Customer, EnrichedOrder>(
                index,
                static order => order.CustomerId!,
                null!));
    }

    [Fact]
    public void StreamingJoinPropagatesCombineExceptionsAndDisposesTheSource()
    {
        LookupIndex<string, Customer> index = BuildIndex(new Customer("C-1", "Ada"));
        var orders = new ProbeEnumerable<Order>(
        [
            new("O-1", "C-1"),
            new("O-2", "C-1"),
        ]);
        var expected = new InvalidOperationException("combine failed");
        var calls = 0;

        var outcomes = orders.JoinExactlyOneResults(
            index,
            static order => order.CustomerId!,
            (order, customer) =>
            {
                calls++;
                return calls == 1
                    ? new EnrichedOrder(order.Id, customer.Name)
                    : throw expected;
            });

        var actual = Assert.Throws<InvalidOperationException>(() => outcomes.ToArray());
        Assert.Same(expected, actual);
        Assert.Equal(1, orders.DisposeCount);
        Assert.Equal(2, calls);
    }

    [Fact]
    public void StreamingJoinEmptySourceYieldsNothingAndRepeatedSourceKeysRepeatOutputs()
    {
        LookupIndex<string, Customer> index = BuildIndex(new Customer("C-1", "Ada"));

        Assert.Empty(
            Array.Empty<Order>().JoinExactlyOneResults(
                index,
                static order => order.CustomerId!,
                static (order, customer) => new EnrichedOrder(order.Id, customer.Name)));

        var repeated = new[]
        {
            new Order("O-1", "C-1"),
            new Order("O-2", "C-1"),
            new Order("O-3", "C-1"),
        };
        var outcomes = repeated.JoinExactlyOneResults(
                index,
                static order => order.CustomerId!,
                static (order, customer) => new EnrichedOrder(order.Id, customer.Name))
            .ToArray();

        Assert.Equal(3, outcomes.Length);
        Assert.All(outcomes, static outcome => Assert.True(outcome.TryGetValue(out _)));
    }

    [Fact]
    public void StreamingJoinReportsOneDuplicatePerMatchingSourceRow()
    {
        LookupIndex<string, Customer> index = BuildIndex(
            new Customer("C-1", "Ada"),
            new Customer("C-2", "Grace"),
            new Customer("C-2", "Duplicate"));
        var orders = new[]
        {
            new Order("O-1", "C-2"),
            new Order("O-2", "C-2"),
            new Order("O-3", "C-1"),
        };
        var combines = 0;

        var outcomes = orders.JoinExactlyOneResults(
                index,
                static order => order.CustomerId!,
                (order, customer) =>
                {
                    combines++;
                    return new EnrichedOrder(order.Id, customer.Name);
                })
            .ToArray();

        Assert.Equal(orders.Length, outcomes.Length);
        Assert.All(
            outcomes.Take(2),
            static outcome =>
            {
                Assert.True(outcome.TryGetError(out var error));
                Assert.Equal(LookupErrorKind.DuplicateKey, error.Kind);
            });
        Assert.Equal(1, combines);
    }

    [Fact]
    public void StreamingJoinKeepsEarlierOutcomesWhenSourceSelectorThrowsAfterAnError()
    {
        LookupIndex<string, Customer> index = BuildIndex(new Customer("C-1", "Ada"));
        var orders = new[]
        {
            new Order("O-1", "C-1"),
            new Order("O-2", "C-9"),
            new Order("O-3", "C-1"),
        };
        var combines = 0;
        var expected = new InvalidOperationException("source selector failed");

        var outcomes = orders.JoinExactlyOneResults(
            index,
            order => order.Id == "O-3" ? throw expected : order.CustomerId!,
            (order, customer) =>
            {
                combines++;
                return new EnrichedOrder(order.Id, customer.Name);
            });

        using var enumerator = outcomes.GetEnumerator();
        Assert.True(
            enumerator.MoveNext()
            && enumerator.Current.TryGetValue(out var first)
            && first!.CustomerName == "Ada"
            && combines == 1);
        Assert.True(
            enumerator.MoveNext()
            && enumerator.Current.TryGetError(out var missing)
            && missing.Kind == LookupErrorKind.MissingKey);
        Assert.Same(expected, Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext()));
    }

    [Fact]
    public void StreamingJoinReportsDuplicateWhenMatchedValuesAreNull()
    {
        LookupIndex<string, Customer?> index = new Customer?[] { null, null }
            .ToLookupIndex(static _ => "C-1")
            .Require();
        var combines = 0;

        var outcomes = new[] { new Order("O-1", "C-1") }
            .JoinExactlyOneResults(
                index,
                static order => order.CustomerId!,
                (order, customer) =>
                {
                    combines++;
                    return new EnrichedOrder(order.Id, customer?.Name ?? "none");
                })
            .ToArray();

        Assert.Single(outcomes);
        Assert.True(
            outcomes[0].TryGetError(out var error)
            && error.Kind == LookupErrorKind.DuplicateKey);
        Assert.Equal(0, combines);
    }

    private static LookupIndex<string, Customer> BuildIndex(params Customer[] customers) =>
        customers.ToLookupIndex(static customer => customer.Id)
            .Require();

    private sealed record Order(string Id, string? CustomerId);

    private sealed record Customer(string Id, string Name);

    private sealed record EnrichedOrder(string Id, string CustomerName);
}
