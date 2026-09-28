namespace FunnySharp.VerticalSlice.Domain;

/// <summary>A supplier that can be asked for a quote.</summary>
public readonly record struct SupplierRef(string Name, int Rank);

/// <summary>A priced offer for one SKU and quantity.</summary>
public readonly record struct SupplierQuote(string Supplier, Sku Sku, Quantity Quantity, Money UnitPrice, TimeSpan LeadTime);

/// <summary>The best quote for a SKU plus the suppliers that could not answer.</summary>
public sealed record QuoteBoard(
    Sku Sku,
    Quantity Quantity,
    SupplierQuote Best,
    IReadOnlyList<string> UnavailableSuppliers);
