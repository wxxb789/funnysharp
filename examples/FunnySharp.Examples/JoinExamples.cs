using FunnySharp;

internal static class JoinExamples
{
    internal static void Verify()
    {
        VerifyCompleteInputFailure();
        VerifyBufferedSuccessAndPrebuiltIndex();
        VerifyLookupCardinality();
        VerifyStreamingPartialOutput();
    }

    private static void VerifyCompleteInputFailure()
    {
        var customers = new[]
        {
            new JoinCustomerRow("C-1", "Ada"),
            new JoinCustomerRow("C-2", "Grace"),
            new JoinCustomerRow("c-2", "Case duplicate"),
        };
        var orders = new[]
        {
            new JoinOrderRow("O-1", "c-1"),
            new JoinOrderRow("O-2", "C-3"),
            new JoinOrderRow("O-3", "C-2"),
        };
        var combines = 0;

        var result = orders.JoinExactlyOne(
            customers,
            static order => order.CustomerId!,
            static customer => customer.Id,
            (order, customer) =>
            {
                combines++;
                return new JoinedOrderRow(order.Id, customer.Name);
            },
            StringComparer.OrdinalIgnoreCase);

        ExampleAssertions.True(result.TryGetErrors(out var errors), "The duplicate and missing data must fail.");
        ExampleAssertions.Equal(2, errors!.Count);
        ExampleAssertions.Equal(LookupErrorKind.DuplicateKey, errors[0].Kind);
        ExampleAssertions.Equal("C-2", errors[0].Key!);
        ExampleAssertions.Equal(2, errors[0].LookupSourceIndex!.Value);
        ExampleAssertions.Equal(1, errors[0].FirstLookupSourceIndex!.Value);
        ExampleAssertions.Equal(LookupErrorKind.MissingKey, errors[1].Kind);
        ExampleAssertions.Equal("C-3", errors[1].Key!);
        ExampleAssertions.Equal(1, errors[1].SourceIndex!.Value);
        ExampleAssertions.Equal(0, combines);
    }

    private static void VerifyBufferedSuccessAndPrebuiltIndex()
    {
        var customers = new[]
        {
            new JoinCustomerRow("C-1", "Ada"),
            new JoinCustomerRow("C-2", "Grace"),
        };
        var orders = new[]
        {
            new JoinOrderRow("O-1", "c-1"),
            new JoinOrderRow("O-2", "C-2"),
        };

        var unique = customers.ToUniqueLookupIndex(
                static customer => customer.Id,
                StringComparer.OrdinalIgnoreCase)
            .Match(
                static index => index,
                static errors => throw new InvalidOperationException(
                    string.Join("; ", errors.Select(static error => error.ToString()))));

        ExampleAssertions.True(
            ReferenceEquals(unique.Comparer, StringComparer.OrdinalIgnoreCase),
            "The index must expose the explicit equality policy.");
        ExampleAssertions.Equal(
            "Ada",
            unique.GetOption("c-1").Match(static customer => customer.Name, static () => "none"));

        var joined = orders.JoinExactlyOne(
            unique,
            static order => order.CustomerId!,
            static (order, customer) => new JoinedOrderRow(order.Id, customer.Name));

        ExampleAssertions.True(joined.TryGetValue(out var outputs), "Exactly-one joins must succeed.");
        ExampleAssertions.SequenceEqual(
            [
                new JoinedOrderRow("O-1", "Ada"),
                new JoinedOrderRow("O-2", "Grace"),
            ],
            outputs!);
    }

    private static void VerifyLookupCardinality()
    {
        var index = new[]
        {
            new JoinCustomerRow("C-2", "Grace"),
            new JoinCustomerRow("C-2", "Duplicate"),
        }.ToLookupIndex(static customer => customer.Id)
            .Match(
                static value => value,
                static errors => throw new InvalidOperationException(
                    string.Join("; ", errors.Select(static error => error.ToString()))));

        ExampleAssertions.Equal(2, index.GetMany("C-2").Count);
        ExampleAssertions.True(
            index.GetZeroOrOne("C-2").TryGetError(out var duplicate),
            "A matched duplicate must not collapse into absence.");
        ExampleAssertions.Equal(LookupErrorKind.DuplicateKey, duplicate.Kind);
        ExampleAssertions.Equal(1, duplicate.LookupSourceIndex!.Value);
        ExampleAssertions.Equal(0, duplicate.FirstLookupSourceIndex!.Value);

        var invalid = index.RequireUniqueKeys();
        ExampleAssertions.True(
            invalid.TryGetErrors(out var errors),
            "The complete index must reject the duplicate even if a later caller does not read it.");
        ExampleAssertions.Equal(LookupErrorKind.DuplicateKey, errors![0].Kind);
    }

    private static void VerifyStreamingPartialOutput()
    {
        var index = new[]
        {
            new JoinCustomerRow("C-1", "Ada"),
            new JoinCustomerRow("C-2", "Grace"),
        }.ToLookupIndex(static customer => customer.Id)
            .Match(
                static value => value,
                static errors => throw new InvalidOperationException(
                    string.Join("; ", errors.Select(static error => error.ToString()))));

        var orders = new[]
        {
            new JoinOrderRow("O-1", "C-1"),
            new JoinOrderRow("O-2", "C-9"),
            new JoinOrderRow("O-3", null),
            new JoinOrderRow("O-4", "C-2"),
        };

        var outcomes = orders.JoinExactlyOneResults(
                index,
                static order => order.CustomerId!,
                static (order, customer) => new JoinedOrderRow(order.Id, customer.Name))
            .ToArray();

        ExampleAssertions.Equal(4, outcomes.Length);
        ExampleAssertions.True(outcomes[0].TryGetValue(out var first), "The earlier success must be observable.");
        ExampleAssertions.Equal(new JoinedOrderRow("O-1", "Ada"), first!);
        ExampleAssertions.True(outcomes[1].TryGetError(out var missing), "A later failure must be an item.");
        ExampleAssertions.Equal(LookupErrorKind.MissingKey, missing.Kind);
        ExampleAssertions.Equal(1, missing.SourceIndex!.Value);
        ExampleAssertions.True(outcomes[2].TryGetError(out var nullKey), "A null source key must be explicit.");
        ExampleAssertions.Equal(LookupErrorKind.NullKey, nullKey.Kind);
        ExampleAssertions.Equal(2, nullKey.SourceIndex!.Value);
        ExampleAssertions.True(outcomes[3].TryGetValue(out var later), "A later success must still be emitted.");
        ExampleAssertions.Equal(new JoinedOrderRow("O-4", "Grace"), later!);
    }

    internal sealed record JoinCustomerRow(string Id, string Name);

    internal sealed record JoinOrderRow(string Id, string? CustomerId);

    internal sealed record JoinedOrderRow(string Id, string CustomerName);
}
