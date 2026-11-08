using FunnySharp.TestSupport;
using FunnySharp.TestSupport.FallibleReduction;

namespace FunnySharp.Tests;

public sealed class JoinTests
{
    [Fact]
    public void BufferedJoinReturnsOneOutputPerSourceRowInSourceOrder()
    {
        var orders = new[]
        {
            new Order("O-1", "C-1"),
            new Order("O-2", "C-2"),
            new Order("O-3", "C-1"),
        };
        var customers = new[]
        {
            new Customer("C-1", "Ada"),
            new Customer("C-2", "Grace"),
        };
        var combines = 0;

        var result = orders.JoinExactlyOne(
            customers,
            static order => order.CustomerId,
            static customer => customer.Id,
            (order, customer) =>
            {
                combines++;
                return new EnrichedOrder(order.Id, customer.Name);
            });

        Assert.True(result.TryGetValue(out var outputs));
        Assert.Equal(3, outputs.Count);
        Assert.Equal(
            [
                new EnrichedOrder("O-1", "Ada"),
                new EnrichedOrder("O-2", "Grace"),
                new EnrichedOrder("O-3", "Ada"),
            ],
            outputs);
        Assert.Equal(3, combines);
    }

    [Fact]
    public void BufferedJoinEnumeratesLookupAndSourceOnceAndNeverReenumeratesThemForQueries()
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

        var result = orders.JoinExactlyOne(
            customers,
            static order => order.CustomerId,
            static customer => customer.Id,
            static (order, customer) => new EnrichedOrder(order.Id, customer.Name));

        Assert.True(result.TryGetValue(out _));
        Assert.Equal(1, orders.EnumeratorCount);
        Assert.Equal(2, orders.ItemsYielded);
        Assert.Equal(1, orders.DisposeCount);
        Assert.Equal(1, customers.EnumeratorCount);
        Assert.Equal(2, customers.ItemsYielded);
        Assert.Equal(1, customers.DisposeCount);
    }

    [Fact]
    public void BufferedJoinReportsMissingRowsAtTheirSourcePositionsAndReturnsNoOutputs()
    {
        var orders = new[]
        {
            new Order("O-1", "C-1"),
            new Order("O-2", "C-9"),
            new Order("O-3", "C-8"),
            new Order("O-4", "C-2"),
        };
        var customers = new[]
        {
            new Customer("C-1", "Ada"),
            new Customer("C-2", "Grace"),
        };
        var combines = 0;

        var result = orders.JoinExactlyOne(
            customers,
            static order => order.CustomerId,
            static customer => customer.Id,
            (order, customer) =>
            {
                combines++;
                return new EnrichedOrder(order.Id, customer.Name);
            });

        Assert.True(result.TryGetErrors(out var errors));
        Assert.Equal(2, errors.Count);
        Assert.Equal(LookupErrorKind.MissingKey, errors[0].Kind);
        Assert.Equal("C-9", errors[0].Key);
        Assert.Equal(1, errors[0].SourceIndex);
        Assert.Equal(LookupErrorKind.MissingKey, errors[1].Kind);
        Assert.Equal("C-8", errors[1].Key);
        Assert.Equal(2, errors[1].SourceIndex);
        Assert.Equal(1, combines);
    }

    [Fact]
    public void BufferedJoinReportsACompleteInputDuplicateEvenWhenNoRowUsesTheKey()
    {
        var orders = new[] { new Order("O-1", "C-1") };
        var customers = new[]
        {
            new Customer("C-1", "Ada"),
            new Customer("C-2", "Grace"),
            new Customer("C-2", "Duplicate"),
        };
        var combines = 0;

        var result = orders.JoinExactlyOne(
            customers,
            static order => order.CustomerId,
            static customer => customer.Id,
            (order, customer) =>
            {
                combines++;
                return new EnrichedOrder(order.Id, customer.Name);
            });

        Assert.True(result.TryGetErrors(out var errors));
        var error = Assert.Single(errors);
        Assert.Equal(LookupErrorKind.DuplicateKey, error.Kind);
        Assert.Equal("C-2", error.Key);
        Assert.Equal(2, error.LookupSourceIndex);
        Assert.Equal(1, error.FirstLookupSourceIndex);
        Assert.Equal(0, combines);
    }

    [Fact]
    public void BufferedJoinReportsLookupErrorsBeforeSourceErrorsInDeterministicOrder()
    {
        var orders = new[]
        {
            new Order("O-1", "C-9"),
            new Order("O-2", "C-2"),
        };
        var customers = new[]
        {
            new Customer("C-1", "Ada"),
            new Customer("C-2", "Grace"),
            new Customer("C-2", "Duplicate"),
        };

        var result = orders.JoinExactlyOne(
            customers,
            static order => order.CustomerId,
            static customer => customer.Id,
            static (order, customer) => new EnrichedOrder(order.Id, customer.Name));

        Assert.True(result.TryGetErrors(out var errors));
        Assert.Equal(2, errors.Count);
        Assert.Equal(LookupErrorKind.DuplicateKey, errors[0].Kind);
        Assert.Equal(2, errors[0].LookupSourceIndex);
        Assert.Equal(1, errors[0].FirstLookupSourceIndex);
        Assert.Equal(LookupErrorKind.MissingKey, errors[1].Kind);
        Assert.Equal("C-9", errors[1].Key);
        Assert.Equal(0, errors[1].SourceIndex);
    }

    [Fact]
    public void BufferedJoinReportsNullLookupAndSourceKeysAtTheirPositions()
    {
        var orders = new (string? CustomerId, string OrderId)[]
        {
            (null, "O-0"),
            ("C-1", "O-1"),
        };
        var customers = new (string? Id, string Name)[]
        {
            ("C-1", "Ada"),
            (null, "NoId"),
        };
        var combines = 0;

        var result = orders.JoinExactlyOne(
            customers,
            static order => order.CustomerId!,
            static customer => customer.Id!,
            (order, customer) =>
            {
                combines++;
                return new EnrichedOrder(order.OrderId, customer.Name);
            });

        Assert.True(result.TryGetErrors(out var errors));
        Assert.Equal(2, errors.Count);
        Assert.Equal(LookupErrorKind.NullKey, errors[0].Kind);
        Assert.Equal(1, errors[0].LookupSourceIndex);
        Assert.Null(errors[0].SourceIndex);
        Assert.Equal(LookupErrorKind.NullKey, errors[1].Kind);
        Assert.Equal(0, errors[1].SourceIndex);
        Assert.Null(errors[1].LookupSourceIndex);
        Assert.Equal(0, combines);
    }

    [Fact]
    public void BufferedJoinOverAPrebuiltUniqueIndexReturnsOrderedOutputsAndMissingErrors()
    {
        UniqueLookupIndex<string, Customer> index = new[]
        {
            new Customer("C-1", "Ada"),
            new Customer("C-2", "Grace"),
        }.ToUniqueLookupIndex(static customer => customer.Id)
            .Require();

        var orders = new[]
        {
            new Order("O-1", "C-2"),
            new Order("O-2", "C-1"),
        };

        var valid = orders.JoinExactlyOne(
            index,
            static order => order.CustomerId,
            static (order, customer) => new EnrichedOrder(order.Id, customer.Name));
        Assert.True(valid.TryGetValue(out var outputs));
        Assert.Equal(
            [
                new EnrichedOrder("O-1", "Grace"),
                new EnrichedOrder("O-2", "Ada"),
            ],
            outputs);

        var missingOrders = new[]
        {
            new Order("O-1", "C-1"),
            new Order("O-2", "C-9"),
            new Order("O-3", "C-2"),
        };
        var combines = 0;
        var invalid = missingOrders.JoinExactlyOne(
            index,
            static order => order.CustomerId,
            (order, customer) =>
            {
                combines++;
                return new EnrichedOrder(order.Id, customer.Name);
            });
        Assert.True(invalid.TryGetErrors(out var errors));
        var error = Assert.Single(errors);
        Assert.Equal(LookupErrorKind.MissingKey, error.Kind);
        Assert.Equal(1, error.SourceIndex);
        Assert.Equal(1, combines);
    }

    [Fact]
    public void BufferedJoinUsesAnExplicitNonDefaultComparer()
    {
        var orders = new[] { new Order("O-1", "c-1") };
        var customers = new[] { new Customer("C-1", "Ada") };

        var insensitive = orders.JoinExactlyOne(
            customers,
            static order => order.CustomerId,
            static customer => customer.Id,
            static (order, customer) => new EnrichedOrder(order.Id, customer.Name),
            StringComparer.OrdinalIgnoreCase);
        Assert.True(insensitive.TryGetValue(out var outputs));
        Assert.Equal([new EnrichedOrder("O-1", "Ada")], outputs);

        var ordinal = orders.JoinExactlyOne(
            customers,
            static order => order.CustomerId,
            static customer => customer.Id,
            static (order, customer) => new EnrichedOrder(order.Id, customer.Name),
            StringComparer.Ordinal);
        Assert.True(ordinal.TryGetErrors(out var errors));
        Assert.Equal(LookupErrorKind.MissingKey, Assert.Single(errors).Kind);
    }

    [Fact]
    public void BufferedJoinPreservesEmptySourceAndEmptyLookupSemantics()
    {
        var empty = Array.Empty<Order>().JoinExactlyOne(
            Array.Empty<Customer>(),
            static order => order.CustomerId,
            static customer => customer.Id,
            static (order, customer) => new EnrichedOrder(order.Id, customer.Name));
        Assert.True(empty.TryGetValue(out var outputs));
        Assert.Empty(outputs);

        var orders = new[]
        {
            new Order("O-1", "C-1"),
            new Order("O-2", "C-2"),
        };
        var missing = orders.JoinExactlyOne(
            Array.Empty<Customer>(),
            static order => order.CustomerId,
            static customer => customer.Id,
            static (order, customer) => new EnrichedOrder(order.Id, customer.Name));
        Assert.True(missing.TryGetErrors(out var errors));
        Assert.Equal(2, errors.Count);
        Assert.All(errors, static error => Assert.Equal(LookupErrorKind.MissingKey, error.Kind));

        var emptyKey = new[] { new Order("O-1", "") }.JoinExactlyOne(
            new[] { new Customer("", "EmptyKey") },
            static order => order.CustomerId,
            static customer => customer.Id,
            static (order, customer) => new EnrichedOrder(order.Id, customer.Name));
        Assert.True(emptyKey.TryGetValue(out var emptyKeyOutputs));
        Assert.Equal([new EnrichedOrder("O-1", "EmptyKey")], emptyKeyOutputs);
    }

    [Fact]
    public void BufferedJoinRejectsNullArgumentsAndDelegatesEagerly()
    {
        var orders = Array.Empty<Order>();
        var customers = Array.Empty<Customer>();
        IEnumerable<Order>? nullOrders = null;
        IEnumerable<Customer>? nullCustomers = null;

        Assert.Throws<ArgumentNullException>(
            () => nullOrders!.JoinExactlyOne(
                customers,
                static order => order.CustomerId,
                static customer => customer.Id,
                static (order, customer) => new EnrichedOrder(order.Id, customer.Name)));
        Assert.Throws<ArgumentNullException>(
            () => orders.JoinExactlyOne(
                nullCustomers!,
                static order => order.CustomerId,
                static customer => customer.Id,
                static (order, customer) => new EnrichedOrder(order.Id, customer.Name)));
        Assert.Throws<ArgumentNullException>(
            () => orders.JoinExactlyOne(
                customers,
                null!,
                static customer => customer.Id,
                static (order, customer) => new EnrichedOrder(order.Id, customer.Name)));
        Assert.Throws<ArgumentNullException>(
            () => orders.JoinExactlyOne(
                customers,
                static order => order.CustomerId,
                null!,
                static (order, customer) => new EnrichedOrder(order.Id, customer.Name)));
        Assert.Throws<ArgumentNullException>(
            () => orders.JoinExactlyOne<Order, Customer, string, EnrichedOrder>(
                customers,
                static order => order.CustomerId,
                static customer => customer.Id,
                null!));

        UniqueLookupIndex<string, Customer> index = customers
            .ToUniqueLookupIndex(static customer => customer.Id)
            .Require();

        UniqueLookupIndex<string, Customer>? nullIndex = null;
        Assert.Throws<ArgumentNullException>(
            () => nullOrders!.JoinExactlyOne(
                index,
                static order => order.CustomerId,
                static (order, customer) => new EnrichedOrder(order.Id, customer.Name)));
        Assert.Throws<ArgumentNullException>(
            () => orders.JoinExactlyOne(
                nullIndex!,
                static order => order.CustomerId,
                static (order, customer) => new EnrichedOrder(order.Id, customer.Name)));
        Assert.Throws<ArgumentNullException>(
            () => orders.JoinExactlyOne(
                index,
                null!,
                static (order, customer) => new EnrichedOrder(order.Id, customer.Name)));
        Assert.Throws<ArgumentNullException>(
            () => orders.JoinExactlyOne<Order, string, Customer, EnrichedOrder>(
                index,
                static order => order.CustomerId,
                null!));
    }

    [Fact]
    public void BufferedJoinPropagatesCombineExceptionsAndDisposesBothEnumerators()
    {
        var orders = new ProbeEnumerable<Order>([new("O-1", "C-1")]);
        var customers = new ProbeEnumerable<Customer>([new("C-1", "Ada")]);
        var expected = new InvalidOperationException("combine failed");

        var actual = Assert.Throws<InvalidOperationException>(
            () => orders.JoinExactlyOne<Order, Customer, string, EnrichedOrder>(
                customers,
                static order => order.CustomerId,
                static customer => customer.Id,
                (_, _) => throw expected));

        Assert.Same(expected, actual);
        Assert.Equal(1, orders.DisposeCount);
        Assert.Equal(1, customers.DisposeCount);
    }

    [Fact]
    public void BufferedJoinPropagatesSourceSelectorExceptionsAndDisposesTheSource()
    {
        var orders = new ProbeEnumerable<Order>([new("O-1", "C-1")]);
        var customers = new ProbeEnumerable<Customer>([new("C-1", "Ada")]);
        var expected = new InvalidOperationException("selector failed");

        var actual = Assert.Throws<InvalidOperationException>(
            () => orders.JoinExactlyOne(
                customers,
                _ => throw expected,
                static customer => customer.Id,
                static (order, customer) => new EnrichedOrder(order.Id, customer.Name)));

        Assert.Same(expected, actual);
        Assert.Equal(1, orders.DisposeCount);
        Assert.Equal(1, customers.DisposeCount);
    }

    [Fact]
    public void PrebuiltUniqueBufferedJoinEnumeratesOnlyTheSource()
    {
        var customers = new ReductionProbe<Customer>(
        [
            new("C-1", "Ada"),
            new("C-2", "Grace"),
        ]);
        UniqueLookupIndex<string, Customer> index = customers
            .ToUniqueLookupIndex(static customer => customer.Id)
            .Require();
        var orders = new ReductionProbe<Order>(
        [
            new("O-1", "C-1"),
            new("O-2", "C-2"),
            new("O-3", "C-1"),
        ]);

        var result = orders.JoinExactlyOne(
            index,
            static order => order.CustomerId,
            static (order, customer) => new EnrichedOrder(order.Id, customer.Name));

        Assert.True(result.TryGetValue(out _));
        Assert.Equal(1, orders.GetEnumeratorCalls);
        Assert.Equal(4, orders.MoveNextCalls);
        Assert.Equal(1, customers.GetEnumeratorCalls);
    }

    [Fact]
    public void BufferedJoinPropagatesLookupKeySelectorExceptionsAndDisposesLookup()
    {
        var orders = new ReductionProbe<Order>([new("O-1", "C-1")]);
        var customers = new ReductionProbe<Customer>([new("C-1", "Ada")]);
        var expected = new InvalidOperationException("lookup selector failed");

        var actual = Assert.Throws<InvalidOperationException>(
            () => orders.JoinExactlyOne(
                customers,
                static order => order.CustomerId,
                (Customer _) => throw expected,
                static (order, customer) => new EnrichedOrder(order.Id, customer.Name)));

        Assert.Same(expected, actual);
        Assert.Equal(1, customers.DisposeCalls);
        Assert.Equal(0, orders.GetEnumeratorCalls);
    }

    [Fact]
    public void InvalidIndexBufferedJoinConsumesSourceOnceAndSkipsCombine()
    {
        var orders = new ReductionProbe<Order>(
        [
            new("O-1", "C-2"),
            new("O-2", "C-1"),
        ]);
        var customers = new[]
        {
            new Customer("C-2", "Grace"),
            new Customer("C-2", "Duplicate"),
            new Customer("C-1", "Ada"),
        };

        var result = orders.JoinExactlyOne(
            customers,
            static order => order.CustomerId,
            static customer => customer.Id,
            static (order, customer) => new EnrichedOrder(order.Id, customer.Name));

        Assert.True(result.TryGetErrors(out _));
        Assert.Equal(1, orders.GetEnumeratorCalls);
        Assert.Equal(3, orders.MoveNextCalls);
        Assert.Equal(2, orders.CurrentReads);
    }

    private sealed record Order(string Id, string CustomerId);

    private sealed record Customer(string Id, string Name);

    private sealed record EnrichedOrder(string Id, string CustomerName);
}
