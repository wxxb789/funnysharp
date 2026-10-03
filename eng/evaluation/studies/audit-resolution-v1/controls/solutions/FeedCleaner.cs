using System.Globalization;

public static class FeedCleaner
{
    public static CleanOutcome Clean(IReadOnlyList<RawRow> rows, IReadOnlyList<string> categories, SkuCatalog catalog)
    {
        if (rows.Count != categories.Count) return new([], [], ["length-mismatch"], null);
        var products = new List<Product>();
        var dropped = new List<DroppedRow>();
        var imported = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            string? reason = null;
            var sku = row.Sku?.Trim().ToUpperInvariant();
            decimal price = 0;
            if (string.IsNullOrWhiteSpace(row.Sku)) reason = "blank-sku";
            else if (string.IsNullOrWhiteSpace(row.Name)) reason = "blank-name";
            else if (!catalog.IsKnown(sku!)) reason = "unknown-sku";
            else if (!decimal.TryParse(row.PriceText?.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out price)) reason = "invalid-price";
            else if (!imported.Add(sku!)) reason = "duplicate-sku";
            if (reason is not null) dropped.Add(new(i + 1, reason));
            else products.Add(new(sku!, row.Name!.Trim(), price, categories[i]));
        }
        decimal? average = products.Count == 0 ? null
            : Math.Round(products.Sum(product => product.Price) / products.Count, 2, MidpointRounding.AwayFromZero);
        return new(products, dropped, [], average);
    }
}
