using System.Diagnostics.CodeAnalysis;
using System.Globalization;

/// <summary>
/// Cleans the legacy exporter's product feed: pairs each raw row with its category label by
/// position, drops the row at the first rule it violates, normalizes the survivors, and
/// averages the imported prices.
/// </summary>
public static class FeedCleaner
{
    /// <summary>Cleans a feed of raw rows paired with per-row category labels.</summary>
    /// <param name="rows">The raw rows, in feed order.</param>
    /// <param name="categories">One category label per row, paired by position.</param>
    /// <param name="catalog">The catalog of known, already-normalized skus.</param>
    /// <returns>The imported products, the dropped rows, feed-level errors, and the mean price.</returns>
    public static CleanOutcome Clean(
        IReadOnlyList<RawRow> rows,
        IReadOnlyList<string> categories,
        SkuCatalog catalog)
    {
        if (rows.Count != categories.Count)
        {
            // The parallel lists pair by position and never truncate to the shorter one, so a
            // length mismatch means the feed itself is malformed: report it and examine no row.
            return new CleanOutcome([], [], ["length-mismatch"], null);
        }

        List<Product> products = [];
        List<DroppedRow> dropped = [];
        HashSet<string> importedSkus = [];

        for (var index = 0; index < rows.Count; index++)
        {
            // Lines are 1-based, and the category label at index i belongs to the row at index i.
            if (TryImport(rows[index], categories[index], catalog, importedSkus, out var product, out var reason))
            {
                products.Add(product);
                importedSkus.Add(product.Sku);
            }
            else
            {
                dropped.Add(new DroppedRow(index + 1, reason));
            }
        }

        return new CleanOutcome(products, dropped, [], AveragePrice(products));
    }

    /// <summary>
    /// Applies the row checks in rule order. The first violated rule drops the row and the later
    /// checks do not run; a row that passes everything imports as a normalized product.
    /// </summary>
    private static bool TryImport(
        RawRow row,
        string category,
        SkuCatalog catalog,
        IReadOnlySet<string> importedSkus,
        [NotNullWhen(true)] out Product? product,
        [NotNullWhen(false)] out string? reason)
    {
        var (skuText, nameText, priceText) = row;

        if (string.IsNullOrWhiteSpace(skuText))
        {
            reason = "blank-sku";
            product = null;
            return false;
        }

        if (string.IsNullOrWhiteSpace(nameText))
        {
            reason = "blank-name";
            product = null;
            return false;
        }

        var sku = skuText.Trim().ToUpperInvariant();
        if (!catalog.IsKnown(sku))
        {
            reason = "unknown-sku";
            product = null;
            return false;
        }

        if (!decimal.TryParse(priceText?.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var price))
        {
            reason = "invalid-price";
            product = null;
            return false;
        }

        // Only skus of earlier surviving rows are reserved; a dropped row does not reserve its sku.
        if (importedSkus.Contains(sku))
        {
            reason = "duplicate-sku";
            product = null;
            return false;
        }

        reason = null;
        product = new Product(sku, nameText.Trim(), price, category);
        return true;
    }

    /// <summary>
    /// The arithmetic mean of the imported prices, computed in decimal and rounded half away from
    /// zero to two decimals; null when nothing was imported.
    /// </summary>
    private static decimal? AveragePrice(IReadOnlyList<Product> products)
        => products.Count == 0
            ? null
            : Math.Round(products.Average(product => product.Price), 2, MidpointRounding.AwayFromZero);
}
