namespace FunnySharp.VerticalSlice.Domain;

/// <summary>One priced order line: refined values only, so the line needs no re-validation.</summary>
public readonly record struct OrderLine(Sku Sku, Quantity Quantity, Money UnitPrice)
{
    public Money LineTotal => UnitPrice.Multiply(Quantity.Value);
}
