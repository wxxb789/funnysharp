using FunnySharp;
using FunnySharp.TestSupport;
using static FunnySharp.TestSupport.MeasurementHarness;

namespace FunnySharp.TestSupport.JoinComparison;

internal static class JoinMeasurements
{
    private const int ItemCount = 1024;
    private const int Iterations = 100;
    private const int WarmupIterations = 10;
    private static readonly IEqualityComparer<string> Comparer = StringComparer.Ordinal;

    internal static void Run()
    {
        JoinComparisonVerifier.Verify();

        var customers = Enumerable.Range(0, ItemCount)
            .Select(static index => new JoinCustomer($"C-{index}", $"Customer {index}"))
            .ToArray();
        var orders = Enumerable.Range(0, ItemCount)
            .Select(static index => new JoinOrder($"O-{index}", $"C-{index}"))
            .ToArray();
        var missingOrders = orders.ToArray();
        missingOrders[^1] = new JoinOrder("O-missing", "C-MISSING");
        var duplicateCustomers = customers.Append(new JoinCustomer("C-1022", "Duplicate")).ToArray();
        var duplicateOrders = orders;
        var manyFailureOrders = Enumerable.Range(0, ItemCount)
            .Select(static index => new JoinOrder($"O-failure-{index}", $"M-{index}"))
            .ToArray();

        var uniqueInput = CreateInput("unique", orders, customers);
        var missingInput = CreateInput("missing", missingOrders, customers);
        var duplicateInput = CreateInput("duplicate", duplicateOrders, duplicateCustomers);
        var emptyInput = CreateInput("empty", [], []);

        var uniqueIndex = BuildUnique(customers);
        var manyIndex = BuildMany(customers);
        var duplicateIndex = BuildMany(duplicateCustomers);

        var rows = new (string Name, Measurement Value)[]
        {
            (
                "Index build - ordinary ToDictionary",
                MeasureRow(
                    () =>
                    {
                        var byId = customers.ToDictionary(static customer => customer.Id, Comparer);
                        return byId.Count;
                    })),
            (
                "Index build - hand-indexed with positions",
                MeasureRow(
                    () => CountHandIndex(customers))),
            (
                "Index build - LookupIndex",
                MeasureRow(
                    () => customers
                        .ToLookupIndex(static customer => customer.Id, Comparer)
                        .Match(static index => index.DistinctCount, static _ => -1))),
            (
                "Index build - UniqueLookupIndex",
                MeasureRow(
                    () => customers
                        .ToUniqueLookupIndex(static customer => customer.Id, Comparer)
                        .Match(static index => index.Count, static _ => -1))),
            (
                "Join success - ordinary",
                MeasureRow(
                    () => OrdinaryJoinConsumer.Join(uniqueInput).Outputs.Count)),
            (
                "Join success - hand-indexed",
                MeasureRow(
                    () => HandIndexedJoinConsumer.Join(uniqueInput).Outputs.Count)),
            (
                "Join success - JoinExactlyOne raw",
                MeasureRow(
                    () => CountJoinOutputs(orders, customers))),
            (
                "Join success - JoinExactlyOne prebuilt unique",
                MeasureRow(
                    () => CountPrebuiltOutputs(orders, uniqueIndex))),
            (
                "Join missing - hand-indexed",
                MeasureRow(
                    () => HandIndexedJoinConsumer.Join(missingInput).Failures.Count)),
            (
                "Join missing - JoinExactlyOne raw",
                MeasureRow(
                    () => CountJoinErrors(missingOrders, customers))),
            (
                "Join missing - JoinExactlyOne prebuilt unique",
                MeasureRow(
                    () => CountPrebuiltErrors(missingOrders, uniqueIndex))),
            (
                "Join duplicate - hand-indexed",
                MeasureRow(
                    () => HandIndexedJoinConsumer.Join(duplicateInput).Failures.Count)),
            (
                "Join duplicate - JoinExactlyOne raw",
                MeasureRow(
                    () => CountJoinErrors(duplicateOrders, duplicateCustomers))),
            (
                "Join many failures - JoinExactlyOne raw",
                MeasureRow(
                    () => CountJoinErrors(manyFailureOrders, customers))),
            (
                "Streaming success - hand-indexed collect",
                MeasureRow(
                    () => HandIndexedStreamingConsumer.Stream(uniqueInput).Count)),
            (
                "Streaming success - JoinExactlyOneResults collect",
                MeasureRow(
                    () => CountStreaming(orders, manyIndex))),
            (
                "Streaming matched duplicate - hand-indexed collect",
                MeasureRow(
                    () => HandIndexedStreamingConsumer.Stream(duplicateInput).Count)),
            (
                "Streaming matched duplicate - JoinExactlyOneResults collect",
                MeasureRow(
                    () => CountStreaming(duplicateOrders, duplicateIndex))),
            (
                "Empty - hand-indexed join",
                MeasureRow(
                    () => HandIndexedJoinConsumer.Join(emptyInput).Outputs.Count)),
            (
                "Empty - JoinExactlyOne raw",
                MeasureRow(
                    () => CountJoinOutputs([], []))),
        };

        Console.WriteLine("Strict cardinality join measurements");
        Console.WriteLine(
            $"Items: {ItemCount}; iterations: {Iterations}; warmup: {WarmupIterations}; min of 3 rounds.");
        Console.WriteLine("Allocation is the stable comparison; timing is directional on this host.");
        Console.WriteLine();
        Console.WriteLine("| Operation | Allocated B/op | Time ns/op |");
        Console.WriteLine("| --- | ---: | ---: |");

        foreach (var row in rows)
        {
            Console.WriteLine(
                $"| {row.Name} | {Format(row.Value.BytesPerOperation)} | {Format(row.Value.NanosecondsPerOperation)} |");
        }
    }

    private static Measurement MeasureRow(Func<int> operation) =>
        Measure(operation, Iterations, WarmupIterations);

    private static JoinInput CreateInput(
        string name,
        IReadOnlyList<JoinOrder> orders,
        IReadOnlyList<JoinCustomer> customers) =>
        new(name, orders, customers, Comparer);

    private static UniqueLookupIndex<string, JoinCustomer> BuildUnique(
        IReadOnlyList<JoinCustomer> customers) =>
        customers.ToUniqueLookupIndex(static customer => customer.Id, Comparer)
            .Require();

    private static LookupIndex<string, JoinCustomer> BuildMany(
        IReadOnlyList<JoinCustomer> customers) =>
        customers.ToLookupIndex(static customer => customer.Id, Comparer)
            .Require();

    private static int CountHandIndex(IReadOnlyList<JoinCustomer> customers)
    {
        var byId = new Dictionary<string, JoinCustomer>(Comparer);
        var firstIndices = new Dictionary<string, int>(Comparer);

        for (var index = 0; index < customers.Count; index++)
        {
            var customer = customers[index];
            if (byId.TryAdd(customer.Id, customer))
            {
                firstIndices[customer.Id] = index;
            }
        }

        return byId.Count;
    }

    private static int CountJoinOutputs(
        IReadOnlyList<JoinOrder> orders,
        IReadOnlyList<JoinCustomer> customers) =>
        orders.JoinExactlyOne(
                customers,
                static order => order.CustomerId!,
                static customer => customer.Id,
                static (order, customer) => new JoinedOrder(order.Id, customer.Name),
                Comparer)
            .Match(static outputs => outputs.Count, static _ => 0);

    private static int CountJoinErrors(
        IReadOnlyList<JoinOrder> orders,
        IReadOnlyList<JoinCustomer> customers) =>
        orders.JoinExactlyOne(
                customers,
                static order => order.CustomerId!,
                static customer => customer.Id,
                static (order, customer) => new JoinedOrder(order.Id, customer.Name),
                Comparer)
            .Match(static _ => 0, static errors => errors.Count);

    private static int CountPrebuiltOutputs(
        IReadOnlyList<JoinOrder> orders,
        UniqueLookupIndex<string, JoinCustomer> index) =>
        orders.JoinExactlyOne(
                index,
                static order => order.CustomerId!,
                static (order, customer) => new JoinedOrder(order.Id, customer.Name))
            .Match(static outputs => outputs.Count, static _ => 0);

    private static int CountPrebuiltErrors(
        IReadOnlyList<JoinOrder> orders,
        UniqueLookupIndex<string, JoinCustomer> index) =>
        orders.JoinExactlyOne(
                index,
                static order => order.CustomerId!,
                static (order, customer) => new JoinedOrder(order.Id, customer.Name))
            .Match(static _ => 0, static errors => errors.Count);

    private static int CountStreaming(
        IReadOnlyList<JoinOrder> orders,
        LookupIndex<string, JoinCustomer> index) =>
        orders.JoinExactlyOneResults(
                index,
                static order => order.CustomerId!,
                static (order, customer) => new JoinedOrder(order.Id, customer.Name))
            .ToArray()
            .Length;
}
