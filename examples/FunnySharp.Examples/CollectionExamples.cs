#pragma warning disable FS0017

using FunnySharp;

// The Goal 17 collection-safety examples. Verify walks one order-import cleanup from raw
// export lines to the fulfillment queue, and VerifyAsync walks a nightly CRM batch
// validation whose accumulated errors carry composed locations such as
// customers[2].addresses[0].postalCode. Kept separate from Program.cs so the runnable
// example file stays navigable and short.
internal static class CollectionExamples
{
    public static void Verify()
    {
        // The legacy order system exports raw lines: rows can be missing entirely, blank,
        // ragged (the D-400 row lost its status column), or carry an unparseable quantity.
        string?[] rawExport =
        [
            "orderId,sku,quantity,status",
            " A-100 , book , 2 , paid ",
            null,
            "   ",
            "B-200,pen,not-a-number,paid",
            "C-300,notebook,3,cancelled",
            "D-400,notebook,4",
            "E-500,pen,5,paid",
            "F-600,book,1,paid",
            "E-500,pen,6,paid",
        ];

        // Stage 1: blank lines normalize to null and WhereNotNull drops every missing line
        // in one pass; the surviving lines are trimmed.
        var presentLines = rawExport
            .Select(static line => string.IsNullOrWhiteSpace(line) ? null : line.Trim())
            .WhereNotNull()
            .ToArray();

        ExampleAssertions.SequenceEqual(
            [
                "orderId,sku,quantity,status",
                "A-100 , book , 2 , paid",
                "B-200,pen,not-a-number,paid",
                "C-300,notebook,3,cancelled",
                "D-400,notebook,4",
                "E-500,pen,5,paid",
                "F-600,book,1,paid",
                "E-500,pen,6,paid",
            ],
            presentLines);

        var header = presentLines[0].Split(',', StringSplitOptions.TrimEntries);

        // Stage 2: every data row must pair exactly with the header. A ragged row cannot
        // pair (ZipExactOrNone is None) and a row whose quantity is not a number cannot
        // resolve (ParseIntOrNone is None); the option partition separates rows from skips.
        var parsePartition = presentLines
            .Skip(1)
            .Select(line => ParseOrderRow(header, line))
            .Partition();

        ExampleAssertions.Equal(2, parsePartition.Nones);
        ExampleAssertions.SequenceEqual(
            [
                new OrderRow("A-100", "book", 2, "paid"),
                new OrderRow("C-300", "notebook", 3, "cancelled"),
                new OrderRow("E-500", "pen", 5, "paid"),
                new OrderRow("F-600", "book", 1, "paid"),
                new OrderRow("E-500", "pen", 6, "paid"),
            ],
            parsePartition.Somes);

        // Stage 3: the parsed rows split into shippable paid orders and the cancelled rest
        // in a single enumeration.
        var statusPartition = parsePartition.Somes.Partition(
            static row => string.Equals(row.Status, "paid", StringComparison.OrdinalIgnoreCase));

        ExampleAssertions.SequenceEqual(
            [new OrderRow("C-300", "notebook", 3, "cancelled")],
            statusPartition.False);
        ExampleAssertions.Equal(4, statusPartition.True.Count);

        // Stage 4: the paid rows are indexed by order id. E-500 arrives twice with
        // conflicting quantities: GetOption sees the first copy, both copies are
        // quarantined for review, and RemoveOrNone drops the key from the import in one
        // operation instead of a ContainsKey/Remove pair.
        var ordersById = new Dictionary<string, OrderRow>();
        var quarantined = new List<OrderRow>();
        var removedFromIndex = new List<OrderRow>();
        foreach (var row in statusPartition.True)
        {
            if (ordersById.GetOption(row.OrderId).TryGetValue(out var earlier))
            {
                quarantined.Add(earlier);
                quarantined.Add(row);
                removedFromIndex.Add(ordersById.RemoveOrNone(row.OrderId).GetValueOrDefault()!);
            }
            else
            {
                ordersById.Add(row.OrderId, row);
            }
        }

        ExampleAssertions.SequenceEqual(
            [
                new OrderRow("E-500", "pen", 5, "paid"),
                new OrderRow("E-500", "pen", 6, "paid"),
            ],
            quarantined);
        ExampleAssertions.SequenceEqual(
            [new OrderRow("E-500", "pen", 5, "paid")],
            removedFromIndex);
        ExampleAssertions.True(
            ordersById.RemoveOrNone("Z-999").IsNone,
            "Removing an absent order id must report absence without touching the index.");

        // Stage 5: the surviving import ships to fulfillment with normalized skus.
        var shipped = ordersById.Values
            .OrderBy(static row => row.OrderId)
            .Select(static row => new OrderRecord(row.OrderId, row.Sku.ToUpperInvariant(), row.Quantity))
            .ToArray();

        ExampleAssertions.SequenceEqual(
            [
                new OrderRecord("A-100", "BOOK", 2),
                new OrderRecord("F-600", "BOOK", 1),
            ],
            shipped);

        // Stage 6: the nightly report needs the total quantity. The seedless fold runs
        // only behind the non-empty guarantee: ToNonEmptyOrNone is Some because the
        // cleaned import kept at least one order.
        var quantities = shipped.Select(static order => order.Quantity).ToArray();
        var totalQuantity = quantities.ToNonEmptyOrNone()
            .Match(
                static nonEmpty => nonEmpty.Aggregate(static (left, right) => left + right),
                static () => 0);

        ExampleAssertions.Equal(3, totalQuantity);
        ExampleAssertions.True(
            Array.Empty<int>().ToNonEmptyOrNone().IsNone,
            "An empty import must never reach the seedless fold.");

        // Cardinality replaces sentinel defaults: the report asks for the smallest and
        // largest cleaned quantity, and an empty import stays distinguishable from a zero.
        ExampleAssertions.Equal(Option.Some(1), quantities.MinOrNone());
        ExampleAssertions.Equal(Option.Some(2), quantities.MaxOrNone());
        ExampleAssertions.True(
            Array.Empty<int>().MinOrNone().IsNone,
            "An empty import has no smallest quantity.");

        // The E-500 conflict must not resolve to a single row, and the quarantined copies
        // reduce to exactly one duplicated order reference for the ops report.
        ExampleAssertions.True(
            statusPartition.True.SingleOrNone(static row => row.OrderId == "E-500").IsNone,
            "A duplicated order id must not resolve to a single row.");
        ExampleAssertions.Equal(
            Option.Some("E-500"),
            quarantined.Select(static row => row.OrderId).Distinct().SingleOrNone());

        // A row-indexed spot check reads through the cleaned import without index guards:
        // the second line is F-600, and a line past the end is absence, not an exception.
        ExampleAssertions.Equal(
            Option.Some(new OrderRecord("F-600", "BOOK", 1)),
            shipped.ElementAtOrNone(1));
        ExampleAssertions.True(
            shipped.ElementAtOrNone(shipped.Length).IsNone,
            "A line past the end of the import must be absence, not an exception.");

        // Stage 7: fulfillment consumes the import as a queue: the head is confirmed, the
        // drain dequeues every order, and the emptied queue reports absence instead of
        // throwing like Dequeue.
        var fulfillment = new Queue<OrderRecord>(shipped);
        ExampleAssertions.True(
            fulfillment.PeekOrNone().TryGetValue(out var head) && head == new OrderRecord("A-100", "BOOK", 2),
            "The fulfillment queue must confirm its head order before draining.");

        var drained = new List<OrderRecord>();
        while (fulfillment.DequeueOrNone().TryGetValue(out var order))
        {
            drained.Add(order);
        }

        ExampleAssertions.SequenceEqual(shipped, drained);
        ExampleAssertions.True(
            fulfillment.DequeueOrNone().IsNone,
            "An emptied fulfillment queue must report absence.");

        // The conflicting E-500 copies go to a review stack: the most recent export is
        // reviewed first, and the emptied stack reports absence.
        var review = new Stack<OrderRow>(quarantined);
        ExampleAssertions.True(
            review.PopOrNone().TryGetValue(out var latestConflict) && latestConflict.Quantity == 6,
            "Review must see the most recently exported conflicting row first.");
        ExampleAssertions.True(
            review.PopOrNone().TryGetValue(out var earlierConflict) && earlierConflict.Quantity == 5,
            "The earlier conflicting row is reviewed second.");
        ExampleAssertions.True(
            review.PopOrNone().IsNone,
            "An emptied review stack must report absence.");
    }

    public static async Task VerifyAsync()
    {
        // The nightly CRM export streams customer records with nested addresses. Feed
        // hiccups arrive as null rows, and the async WhereNotNull pass drops them before
        // validation spends a registry call on them. The dropped row never reaches a
        // traversal, so the customers after it keep their shifted indexes.
        var feed = AsyncCustomerRows().WhereNotNull();

        // Report pass: the located batch validation threads each customer's composed
        // location through validation, and the loyalty tier is parsed against the async
        // registry, so the outer traversal is the ValueTask-selector form.
        var batch = await feed.TraverseValueAsync(
            Location.Root.Property("customers"),
            ValidateCustomerAsync,
            CancellationToken.None);

        ExampleAssertions.True(
            batch.TryGetErrors(out var errors),
            "The invalid batch must accumulate every located error.");
        ExampleAssertions.True(
            errors!.All(static error => error.Location is not null),
            "Every accumulated batch error must carry its composed location.");
        ExampleAssertions.Equal(3, errors!.Count);
        ExampleAssertions.Equal("unknown loyalty tier 'wood'", errors![0].Message);

        // Each level named only its own segments: the outer root named customers, the
        // inner root named .addresses under the composed customers[2], and the innermost
        // error decorator named .postalCode and .city. The composed paths render without
        // the application assembling them.
        ExampleAssertions.SequenceEqual(
            [
                "customers[1].tier",
                "customers[2].addresses[0].postalCode",
                "customers[2].addresses[1].city",
            ],
            errors!.Select(static error => error.Location!.ToString()).ToArray());

        // Import pass: the job still ships every row that individually validates. The
        // per-row outcomes materialize as results, and one sync Partition over the
        // materialized outcomes splits imported rows from rejected rows.
        var rowOutcomes = new List<Result<CustomerRecord, ImportError>>();
        var rowIndex = 0;
        await foreach (var customer in feed)
        {
            var validation = await ValidateCustomerAsync(
                Location.Root.Property("customers").At(rowIndex++),
                customer);
            rowOutcomes.Add(validation.Match(
                static imported => Result<CustomerRecord, ImportError>.Success(imported),
                static rowErrors => Result<CustomerRecord, ImportError>.Failure(rowErrors[0])));
        }

        // The feed yielded five rows; the import pass saw four because the null row was
        // dropped before any traversal reached it.
        ExampleAssertions.Equal(4, rowOutcomes.Count);

        var importPartition = rowOutcomes.Partition();
        ExampleAssertions.SequenceEqual(
            ["ada@example.com", "alan@example.com"],
            importPartition.Passed.Select(static imported => imported.Email).ToArray());
        ExampleAssertions.Equal(
            new AddressRecord("London", "EC1A 1BB"),
            importPartition.Passed[0].Addresses[0]);
        ExampleAssertions.Equal(LoyaltyTier.Gold, importPartition.Passed[0].Tier);

        // A rejected row keeps its first blocking error, and that error carries the same
        // composed path the batch report rendered.
        ExampleAssertions.SequenceEqual(
            ["customers[1].tier", "customers[2].addresses[0].postalCode"],
            importPartition.Failed.Select(static error => error.Location!.ToString()).ToArray());
    }

    private static Option<OrderRow> ParseOrderRow(string[] header, string line)
    {
        var columns = line.Split(',', StringSplitOptions.TrimEntries);

        // The row must pair exactly with the header: a ragged row is skipped instead of
        // being silently truncated to the shorter column count.
        return header
            .ZipExactOrNone(columns)
            .Bind(pairs => Field(pairs, "orderId").Zip(
                Field(pairs, "sku"),
                Field(pairs, "quantity").Bind(static text => text.ParseIntOrNone()),
                Field(pairs, "status"),
                static (orderId, sku, quantity, status) => new OrderRow(orderId, sku, quantity, status)));
    }

    private static Option<string> Field(IReadOnlyList<(string First, string Second)> columns, string name) =>
        columns
            .FirstOrNone(pair => string.Equals(pair.First, name, StringComparison.Ordinal))
            .Map(static pair => pair.Second);

    private static async ValueTask<Validation<CustomerRecord, ImportError>> ValidateCustomerAsync(
        Location customerLocation,
        CustomerRow customer)
    {
        // The loyalty tier is parsed against the remote registry, so the row's own
        // validation crosses an await boundary.
        var tier = await ParseTierAsync(customer.TierText);

        return ValidateEmail(customer.Email)
            .MapErrors(error => error.At(customerLocation.Property("email")))
            .Zip(
                tier.MapErrors(error => error.At(customerLocation.Property("tier"))),
                customer.Addresses.Traverse(customerLocation.Property("addresses"), ValidateAddress),
                static (email, tierValue, addresses) => new CustomerRecord(email, tierValue, [.. addresses]));
    }

    private static Validation<AddressRecord, ImportError> ValidateAddress(
        Location addressLocation,
        AddressRow address) =>
        ValidateRequired(address.City, "city")
            .MapErrors(error => error.At(addressLocation.Property("city")))
            .Zip(
                ValidateRequired(address.PostalCode, "postalCode")
                    .MapErrors(error => error.At(addressLocation.Property("postalCode"))),
                static (city, postalCode) => new AddressRecord(city, postalCode));

    private static Validation<string, ImportError> ValidateEmail(string? email) =>
        string.IsNullOrWhiteSpace(email) || !email.Contains('@')
            ? Validation<string, ImportError>.Invalid(new ImportError("a valid email address is required"))
            : Validation<string, ImportError>.Valid(email.Trim());

    private static Validation<string, ImportError> ValidateRequired(string? value, string field) =>
        string.IsNullOrWhiteSpace(value)
            ? Validation<string, ImportError>.Invalid(new ImportError($"{field} is required"))
            : Validation<string, ImportError>.Valid(value.Trim());

    // The remote loyalty registry answers asynchronously; the deterministic stand-in
    // still crosses an await boundary before the tier text resolves.
    private static async ValueTask<Validation<LoyaltyTier, ImportError>> ParseTierAsync(string tierText)
    {
        await Task.Yield();

        var normalized = tierText.Trim();
        return normalized switch
        {
            "gold" => Validation<LoyaltyTier, ImportError>.Valid(LoyaltyTier.Gold),
            "silver" => Validation<LoyaltyTier, ImportError>.Valid(LoyaltyTier.Silver),
            _ => Validation<LoyaltyTier, ImportError>.Invalid(
                new ImportError($"unknown loyalty tier '{normalized}'")),
        };
    }

    private static async IAsyncEnumerable<CustomerRow?> AsyncCustomerRows()
    {
        yield return new CustomerRow("ada@example.com", "gold", [new AddressRow("London", "EC1A 1BB")]);
        await Task.Yield();
        yield return null;
        yield return new CustomerRow("barbara@example.com", "wood", [new AddressRow("Paris", "75001")]);
        await Task.Yield();
        yield return new CustomerRow(
            "grace@example.com",
            "silver",
            [new AddressRow("Berlin", ""), new AddressRow(null, "10115")]);
        yield return new CustomerRow("alan@example.com", "gold", [new AddressRow("Oslo", "0150")]);
    }

    private sealed record OrderRow(string OrderId, string Sku, int Quantity, string Status);

    private sealed record OrderRecord(string OrderId, string Sku, int Quantity);

    private sealed record CustomerRow(string? Email, string TierText, IReadOnlyList<AddressRow> Addresses);

    private sealed record AddressRow(string? City, string? PostalCode);

    private sealed record CustomerRecord(
        string Email,
        LoyaltyTier Tier,
        IReadOnlyList<AddressRecord> Addresses);

    private sealed record AddressRecord(string City, string PostalCode);

    // The caller-owned error decoration: ImportError attaches its own location, and
    // FunnySharp never attaches a location to a caller-typed error itself.
    private sealed record ImportError(string Message, Location? Location = null)
    {
        internal ImportError At(Location location) => this with { Location = location };
    }

    private enum LoyaltyTier
    {
        Gold,
        Silver,
    }
}
