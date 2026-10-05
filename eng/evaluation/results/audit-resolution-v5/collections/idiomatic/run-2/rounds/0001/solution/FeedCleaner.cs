using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

public static class FeedCleaner
{
    public static CleanOutcome Clean(
        IReadOnlyList<RawRow> rows,
        IReadOnlyList<string> categories,
        SkuCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentNullException.ThrowIfNull(categories);
        ArgumentNullException.ThrowIfNull(catalog);

        // Exact positional pairing is required before examining any row.
        if (rows.Count != categories.Count)
        {
            return new CleanOutcome(
                Array.Empty<Product>(),
                Array.Empty<DroppedRow>(),
                new[] { "length-mismatch" },
                null);
        }

        var products = new List<Product>();
        var dropped = new List<DroppedRow>();
        var importedSkus = new HashSet<string>(StringComparer.Ordinal);

        for (int index = 0; index < rows.Count; index++)
        {
            RawRow row = rows[index];
            int line = index + 1;

            // Each failure ends processing for this row; later checks are skipped.
            if (string.IsNullOrWhiteSpace(row.Sku))
            {
                dropped.Add(new DroppedRow(line, "blank-sku"));
                continue;
            }

            if (string.IsNullOrWhiteSpace(row.Name))
            {
                dropped.Add(new DroppedRow(line, "blank-name"));
                continue;
            }

            string sku = row.Sku.Trim().ToUpperInvariant();
            if (!catalog.IsKnown(sku))
            {
                dropped.Add(new DroppedRow(line, "unknown-sku"));
                continue;
            }

            string? priceText = row.PriceText?.Trim();
            if (!decimal.TryParse(
                priceText,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out decimal price))
            {
                dropped.Add(new DroppedRow(line, "invalid-price"));
                continue;
            }

            // Only surviving rows reserve a normalized SKU.
            if (!importedSkus.Add(sku))
            {
                dropped.Add(new DroppedRow(line, "duplicate-sku"));
                continue;
            }

            products.Add(new Product(sku, row.Name.Trim(), price, categories[index]));
        }

        decimal? averagePrice = products.Count == 0
            ? null
            : decimal.Round(
                products.Average(product => product.Price),
                2,
                MidpointRounding.AwayFromZero);

        return new CleanOutcome(
            products.AsReadOnly(),
            dropped.AsReadOnly(),
            Array.Empty<string>(),
            averagePrice);
    }
}
