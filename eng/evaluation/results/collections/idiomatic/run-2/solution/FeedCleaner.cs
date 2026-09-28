using System.Globalization;

/// <summary>
/// Cleans the legacy exporter's product feed: pairs each raw row with its category label,
/// drops the rows that fail the business rules (the first failing rule wins), and aggregates
/// the prices of the rows that survive.
/// </summary>
public static class FeedCleaner
{
    public static CleanOutcome Clean(
        IReadOnlyList<RawRow> rows,
        IReadOnlyList<string> categories,
        SkuCatalog catalog)
    {
        // The feed is a pair of parallel lists: row i carries category label i. Differing
        // lengths mean the feed itself is malformed, so no row is examined at all.
        if (rows.Count != categories.Count)
        {
            return new CleanOutcome([], [], ["length-mismatch"], AveragePrice: null);
        }

        var products = new List<Product>();
        var dropped = new List<DroppedRow>();

        // Normalized skus of the rows that survived: a dropped row does not reserve its sku.
        var importedSkus = new HashSet<string>();

        for (var index = 0; index < rows.Count; index++)
        {
            var line = index + 1;
            var (sku, name, priceText) = rows[index];

            if (string.IsNullOrWhiteSpace(sku))
            {
                dropped.Add(new DroppedRow(line, "blank-sku"));
                continue;
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                dropped.Add(new DroppedRow(line, "blank-name"));
                continue;
            }

            // Both the catalog lookup and the duplicate check use the normalized sku.
            var normalizedSku = sku.Trim().ToUpperInvariant();

            if (!catalog.IsKnown(normalizedSku))
            {
                dropped.Add(new DroppedRow(line, "unknown-sku"));
                continue;
            }

            if (!TryParsePrice(priceText, out var price))
            {
                dropped.Add(new DroppedRow(line, "invalid-price"));
                continue;
            }

            if (!importedSkus.Add(normalizedSku))
            {
                dropped.Add(new DroppedRow(line, "duplicate-sku"));
                continue;
            }

            // The paired category label is carried through unchanged.
            products.Add(new Product(normalizedSku, name.Trim(), price, categories[index]));
        }

        return new CleanOutcome(products, dropped, [], ComputeAveragePrice(products));
    }

    /// <summary>
    /// Parses the trimmed price text in the invariant culture; null, empty, or unparsable
    /// text does not yield a price.
    /// </summary>
    private static bool TryParsePrice(string? priceText, out decimal price)
    {
        price = 0m;
        var text = priceText?.Trim();

        return !string.IsNullOrEmpty(text)
            && decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out price);
    }

    /// <summary>
    /// The arithmetic mean of the imported prices, rounded half away from zero to two
    /// decimals; null when nothing was imported.
    /// </summary>
    private static decimal? ComputeAveragePrice(IReadOnlyList<Product> products) =>
        products.Count == 0
            ? null
            : Math.Round(products.Sum(product => product.Price) / products.Count, 2, MidpointRounding.AwayFromZero);
}
