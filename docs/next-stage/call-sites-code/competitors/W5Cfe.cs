using CSharpFunctionalExtensions;

namespace CallSites.Competitors;

// W5 competitor: CFE 3.7.0 Maybe + Tap observation.
public static class W5Cfe
{
    public static decimal TotalWithAudit(OrderLine line, Action<string> audit) =>
        Maybe<decimal>.From(line.Quantity * line.UnitPrice)
            .Tap(total => audit($"line={line.Sku} total={total}"))
            .Map(total => total * Discount(line.Sku))
            .Tap(discounted => audit($"discounted={discounted}"))
            .GetValueOrDefault();

    private static decimal Discount(string sku) =>
        sku.StartsWith("SAVE", StringComparison.Ordinal) ? 0.9m : 1m;
}
