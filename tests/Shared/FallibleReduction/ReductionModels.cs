namespace FunnySharp.TestSupport.FallibleReduction;

internal readonly record struct ReductionLine(string Sku, int Quantity, decimal UnitPrice)
{
    internal decimal Amount => Quantity * UnitPrice;
}

internal readonly record struct ReductionError(string Code, string Detail);

internal readonly record struct ReductionTotal(int Count, decimal Amount)
{
    internal ReductionTotal Add(ReductionLine line) =>
        new(Count + 1, Amount + line.Amount);
}

internal sealed class MutableReductionTotal
{
    private readonly List<ReductionLine> lines = [];

    internal IReadOnlyList<ReductionLine> Lines => lines;

    internal decimal Amount { get; private set; }

    internal int Count => lines.Count;

    internal void Add(ReductionLine line)
    {
        lines.Add(line);
        Amount += line.Amount;
    }
}
