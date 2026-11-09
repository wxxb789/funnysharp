using System.Collections.ObjectModel;

using FunnySharp.OperationPlans;

namespace FunnySharp.TestSupport.OperationComparison;

internal static class ComparisonDescriptors
{
    private static readonly ReadOnlyCollection<OperationInfo> s_description = Array.AsReadOnly(
    [
        new OperationInfo(
            "load-customer",
            OperationKind.Read,
            Array.Empty<string>(),
            requiresPriorResult: false,
            dynamicWork: false,
            conditional: false,
            "Reads the customer by request id; missing or inactive is a domain failure."),
        new OperationInfo(
            "load-catalog",
            OperationKind.Read,
            Array.Empty<string>(),
            requiresPriorResult: false,
            dynamicWork: false,
            conditional: false,
            "Reads active catalog prices for the requested SKUs."),
        new OperationInfo(
            "create-order",
            OperationKind.Write,
            new[] { "load-customer", "load-catalog" },
            requiresPriorResult: true,
            dynamicWork: true,
            conditional: false,
            "Computes the total, inserts the order, and inserts one line per requested SKU."),
        new OperationInfo(
            "check-credit",
            OperationKind.Read,
            new[] { "load-customer", "create-order" },
            requiresPriorResult: true,
            dynamicWork: false,
            conditional: false,
            "Compares the computed total with the loaded credit limit."),
        new OperationInfo(
            "reserve-inventory",
            OperationKind.Write,
            new[] { "create-order", "load-catalog" },
            requiresPriorResult: true,
            dynamicWork: true,
            conditional: false,
            "Runs one conditional update per priced line; stored availability decides success."),
        new OperationInfo(
            "authorize-payment",
            OperationKind.External,
            new[] { "create-order" },
            requiresPriorResult: true,
            dynamicWork: false,
            conditional: false,
            "Calls the simulated gateway; the gateway ledger is outside the transaction."),
        new OperationInfo(
            "record-payment",
            OperationKind.Write,
            new[] { "create-order", "authorize-payment" },
            requiresPriorResult: true,
            dynamicWork: false,
            conditional: false,
            "Inserts the payment row with the gateway reference."),
        new OperationInfo(
            "record-outbox",
            OperationKind.Write,
            new[] { "create-order", "authorize-payment" },
            requiresPriorResult: true,
            dynamicWork: false,
            conditional: false,
            "Inserts the transactional integration event; delivery is not performed."),
        new OperationInfo(
            "queue-review",
            OperationKind.Write,
            new[] { "create-order" },
            requiresPriorResult: true,
            dynamicWork: false,
            conditional: true,
            "Runs only when the computed total reaches the review threshold."),
    ]);

    internal static IReadOnlyList<OperationInfo> Describe => s_description;

    internal static void Validate()
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var info in s_description)
        {
            foreach (var dependency in info.DependsOn)
            {
                if (!seen.Contains(dependency))
                {
                    throw new ArgumentException(
                        $"Operation '{info.Name}' depends on '{dependency}', which is not an earlier operation.");
                }
            }

            if (!seen.Add(info.Name))
            {
                throw new ArgumentException($"Operation name '{info.Name}' is duplicated.");
            }
        }
    }
}
