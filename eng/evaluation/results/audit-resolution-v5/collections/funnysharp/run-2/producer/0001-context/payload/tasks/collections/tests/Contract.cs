// Style-neutral contract shared by both evaluation variants: the data types, the deterministic
// domain fakes, and the neutral outcome record the solution must produce. No FunnySharp types
// appear here on purpose: both styles end at the same seam.
public sealed record RawRow(string? Sku, string? Name, string? PriceText);

public sealed record Product(string Sku, string Name, decimal Price, string Category);

public sealed record DroppedRow(int Line, string Reason);

public sealed record CleanOutcome(
    IReadOnlyList<Product> Products,
    IReadOnlyList<DroppedRow> Dropped,
    IReadOnlyList<string> Errors,
    decimal? AveragePrice);

public sealed class SkuCatalog
{
    private readonly IReadOnlySet<string> skus;

    public SkuCatalog(IReadOnlySet<string> skus) => this.skus = skus;

    public bool IsKnown(string sku) => skus.Contains(sku);
}
