using FunnySharp.TestSupport;
using FunnySharp.TestSupport.FallibleReduction;

namespace FunnySharp.TestSupport.JoinComparison;

internal static class JoinComparisonVerifier
{
    internal static void Verify()
    {
        foreach (var fixture in JoinFixtures.All)
        {
            VerifyFixture(fixture);
        }

        VerifyConsumptionLedger();
        VerifyCombineLedger();
        VerifyNullLookupKey();
    }

    private static void VerifyFixture(JoinFixture fixture)
    {
        var input = fixture.Input;
        var hand = HandIndexedJoinConsumer.Join(input);
        var funny = FunnySharpJoinConsumer.Join(input);

        Equal(fixture.ExpectedOutputs, funny.Outputs, $"{input.Name}: buffered outputs");
        Equal(fixture.ExpectedFailures, funny.Failures, $"{input.Name}: buffered failures");
        Equal(hand.Outputs, funny.Outputs, $"{input.Name}: hand outputs");
        Equal(hand.Failures, funny.Failures, $"{input.Name}: hand failures");

        if (fixture.ExpectedFailures.Count == 0)
        {
            var ordinary = OrdinaryJoinConsumer.Join(input);
            Equal(funny.Outputs, ordinary.Outputs, $"{input.Name}: ordinary outputs");
            Equal(0, ordinary.Failures.Count, $"{input.Name}: ordinary failures");
        }
        else
        {
            var expectedException = ExpectedOrdinaryException(fixture);
            var exception = Capture(() => OrdinaryJoinConsumer.Join(input));
            True(
                exception.GetType() == expectedException,
                $"{input.Name}: ordinary form threw {exception.GetType().Name} instead of "
                + $"{expectedException.Name}");
        }

        var handStream = HandIndexedStreamingConsumer.Stream(input);
        var funnyStream = FunnySharpJoinConsumer.Stream(input);

        Equal(fixture.ExpectedStream, funnyStream, $"{input.Name}: streaming outcomes");
        Equal(handStream, funnyStream, $"{input.Name}: hand streaming outcomes");
    }

    private static void VerifyConsumptionLedger()
    {
        var orders = new ReductionProbe<JoinOrder>(
        [
            new("O-1", "C-1"),
            new("O-2", "C-2"),
            new("O-3", "C-1"),
        ]);
        var customers = new ReductionProbe<JoinCustomer>(
        [
            new("C-1", "Ada"),
            new("C-2", "Grace"),
        ]);

        var result = orders.JoinExactlyOne(
            customers,
            static order => order.CustomerId!,
            static customer => customer.Id,
            static (order, customer) => new JoinedOrder(order.Id, customer.Name));

        True(result.TryGetValue(out var outputs), "buffered ledger join must succeed");
        Equal(3, outputs!.Count, "buffered ledger output count");

        Equal(1, customers.GetEnumeratorCalls, "buffered lookup GetEnumerator");
        Equal(3, customers.MoveNextCalls, "buffered lookup MoveNext");
        Equal(2, customers.CurrentReads, "buffered lookup Current");
        Equal(1, customers.DisposeCalls, "buffered lookup Dispose");

        Equal(1, orders.GetEnumeratorCalls, "buffered source GetEnumerator");
        Equal(4, orders.MoveNextCalls, "buffered source MoveNext");
        Equal(3, orders.CurrentReads, "buffered source Current");
        Equal(1, orders.DisposeCalls, "buffered source Dispose");

        var streamOrders = new ReductionProbe<JoinOrder>(
        [
            new("O-1", "C-1"),
            new("O-2", "C-2"),
            new("O-3", "C-1"),
        ]);
        var streamCustomers = new ReductionProbe<JoinCustomer>(
        [
            new("C-1", "Ada"),
            new("C-2", "Grace"),
        ]);
        LookupIndex<string, JoinCustomer> index = streamCustomers
            .ToLookupIndex(static customer => customer.Id)
            .Require();

        Equal(1, streamCustomers.GetEnumeratorCalls, "stream lookup GetEnumerator");
        Equal(3, streamCustomers.MoveNextCalls, "stream lookup MoveNext");
        Equal(2, streamCustomers.CurrentReads, "stream lookup Current");
        Equal(1, streamCustomers.DisposeCalls, "stream lookup Dispose");

        var stream = streamOrders.JoinExactlyOneResults(
            index,
            static order => order.CustomerId!,
            static (order, customer) => new JoinedOrder(order.Id, customer.Name));

        Equal(0, streamOrders.GetEnumeratorCalls, "stream must be deferred until first enumeration");

        var first = stream.ToArray();
        Equal(3, first.Length, "stream first output count");
        Equal(1, streamOrders.GetEnumeratorCalls, "stream first GetEnumerator");
        Equal(4, streamOrders.MoveNextCalls, "stream first MoveNext");
        Equal(3, streamOrders.CurrentReads, "stream first Current");
        Equal(1, streamOrders.DisposeCalls, "stream first Dispose");

        var second = stream.ToArray();
        Equal(3, second.Length, "stream second output count");
        Equal(2, streamOrders.GetEnumeratorCalls, "stream second GetEnumerator");
        Equal(8, streamOrders.MoveNextCalls, "stream second MoveNext");
        Equal(6, streamOrders.CurrentReads, "stream second Current");
        Equal(2, streamOrders.DisposeCalls, "stream second Dispose");
        Equal(1, streamCustomers.GetEnumeratorCalls, "stream must not reenumerate lookup");
    }

    private static void VerifyCombineLedger()
    {
        var orders = new[]
        {
            new JoinOrder("O-1", "C-1"),
            new JoinOrder("O-2", "C-9"),
            new JoinOrder("O-3", "C-8"),
            new JoinOrder("O-4", "C-2"),
        };
        var customers = new[]
        {
            new JoinCustomer("C-1", "Ada"),
            new JoinCustomer("C-2", "Grace"),
        };

        var bufferedCombines = 0;
        var buffered = orders.JoinExactlyOne(
            customers,
            static order => order.CustomerId!,
            static customer => customer.Id,
            (order, customer) =>
            {
                bufferedCombines++;
                return new JoinedOrder(order.Id, customer.Name);
            });

        True(buffered.TryGetErrors(out var bufferedErrors), "buffered ledger join must fail");
        Equal(2, bufferedErrors!.Count, "buffered ledger failure count");
        Equal(1, bufferedCombines, "buffered combine runs only before the first error");

        LookupIndex<string, JoinCustomer> index = customers
            .ToLookupIndex(static customer => customer.Id)
            .Require();

        var streamingCombines = 0;
        var outcomes = orders.JoinExactlyOneResults(
                index,
                static order => order.CustomerId!,
                (order, customer) =>
                {
                    streamingCombines++;
                    return new JoinedOrder(order.Id, customer.Name);
                })
            .ToArray();

        Equal(4, outcomes.Length, "streaming ledger outcome count");
        Equal(2, streamingCombines, "streaming combine runs for each success after failures");

        var duplicateCustomers = new[]
        {
            new JoinCustomer("C-1", "Ada"),
            new JoinCustomer("C-2", "Grace"),
            new JoinCustomer("C-2", "Duplicate"),
        };
        var indexCombines = 0;
        var indexFailure = new[] { new JoinOrder("O-1", "C-2") }.JoinExactlyOne(
            duplicateCustomers,
            static order => order.CustomerId!,
            static customer => customer.Id,
            (order, customer) =>
            {
                indexCombines++;
                return new JoinedOrder(order.Id, customer.Name);
            });

        True(indexFailure.TryGetErrors(out _), "complete-input duplicate must fail");
        Equal(0, indexCombines, "combine must not run when the lookup index is already invalid");
    }

    private static void VerifyNullLookupKey()
    {
        var input = new JoinInput(
            "null lookup key",
            [new("O-1", "C-1")],
            [new(null!, "No id"), new("C-1", "Ada")],
            StringComparer.Ordinal);
        var hand = HandIndexedJoinConsumer.Join(input);
        var funny = FunnySharpJoinConsumer.Join(input);

        Equal(hand.Outputs, funny.Outputs, "null lookup hand outputs");
        Equal(hand.Failures, funny.Failures, "null lookup hand failures");
        True(
            funny.Failures.Count == 1
            && funny.Failures[0].Kind == LookupErrorKind.NullKey
            && funny.Failures[0].LookupSourceIndex == 0,
            "null lookup must report one NullKey failure at lookup position 0");
    }

    private static Type ExpectedOrdinaryException(JoinFixture fixture)
    {
        if (fixture.ExpectedFailures.Any(
                static failure =>
                    failure.Kind == LookupErrorKind.NullKey && failure.SourceIndex is not null))
        {
            return typeof(ArgumentNullException);
        }

        return fixture.ExpectedFailures.Any(
            static failure => failure.Kind == LookupErrorKind.DuplicateKey)
            ? typeof(ArgumentException)
            : typeof(KeyNotFoundException);
    }

    private static Exception Capture(Action action)
    {
        try
        {
            action();
        }
        catch (Exception exception)
        {
            return exception;
        }

        throw new InvalidOperationException("The operation was expected to throw.");
    }

    private static void Equal<T>(IReadOnlyList<T> expected, IReadOnlyList<T> actual, string description)
    {
        if (!expected.SequenceEqual(actual))
        {
            throw new InvalidOperationException(
                $"{description}: expected [{string.Join(", ", expected)}] but observed "
                + $"[{string.Join(", ", actual)}].");
        }
    }

    private static void Equal(int expected, int actual, string description)
    {
        if (expected != actual)
        {
            throw new InvalidOperationException(
                $"{description}: expected {expected} but observed {actual}.");
        }
    }

    private static void True(bool condition, string description)
    {
        if (!condition)
        {
            throw new InvalidOperationException(description);
        }
    }
}
