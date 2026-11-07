using FunnySharp;

namespace FunnySharp.TestSupport.SummaryComparison;

internal sealed record OrderLine(string Sku, string Category, int Quantity, decimal UnitPrice)
{
    internal decimal Amount => Quantity * UnitPrice;
}

internal sealed record CategoryCount(string Category, int Count);

internal sealed record OrderTotals(
    int LineCount,
    decimal Total,
    Option<decimal> CheapestUnitPrice,
    Option<decimal> MostExpensiveUnitPrice);

internal sealed record OrderReport(
    OrderTotals Totals,
    IReadOnlyList<CategoryCount> Categories,
    Option<string> FirstSku);
