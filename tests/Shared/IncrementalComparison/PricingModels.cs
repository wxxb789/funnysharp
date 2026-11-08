using FunnySharp;

namespace FunnySharp.TestSupport.IncrementalComparison;

internal sealed record Product(int Id, decimal UnitCost, decimal TaxRate, string Category);

internal readonly record struct OrderLine(int ProductIndex, int Quantity);

internal sealed record Order(int Id, string Region, IReadOnlyList<OrderLine> Lines);

internal sealed record ProductEconomics(int Id, decimal UnitPrice, decimal TaxRate, string Category);

internal sealed record Quote(int OrderId, decimal Subtotal, decimal Discount, decimal Tax, decimal Total);

internal sealed record PricingError(int OrderId, string Code, string Detail);

internal sealed class PricingWork
{
    internal int ProductEvaluations { get; set; }

    internal int QuoteEvaluations { get; set; }

    internal int LineEvaluations { get; set; }

    internal void Reset()
    {
        ProductEvaluations = 0;
        QuoteEvaluations = 0;
        LineEvaluations = 0;
    }
}