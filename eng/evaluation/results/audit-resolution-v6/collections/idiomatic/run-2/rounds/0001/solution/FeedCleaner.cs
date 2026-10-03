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

        // Exact pairing is a feed-level gate: do not inspect any row on mismatch.
        if (rows.Count != categories.Count)
        {
            return new CleanOutcome(
                Array.Empty<Product>(),
                Array.Empty<DroppedRow>(),
                new[] { "length-mismatch" },
                null);
        }

        ArgumentNullException.ThrowIfNull(catalog);

        var products = new List<Product>();
        var dropped = new List<DroppedRow>();
        var importedSkus = new HashSet<string>(StringComparer.Ordinal);

        for (int index = 0; index < rows.Count; index++)
        {
            var row = rows[index];
            int line = index + 1;

            string? skuText = row.Sku;
            if (string.IsNullOrWhiteSpace(skuText))
            {
                dropped.Add(new DroppedRow(line, "blank-sku"));
                continue;
            }

            string? nameText = row.Name;
            if (string.IsNullOrWhiteSpace(nameText))
            {
                dropped.Add(new DroppedRow(line, "blank-name"));
                continue;
            }

            string normalizedSku = skuText.Trim().ToUpperInvariant();
            if (!catalog.IsKnown(normalizedSku))
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

            // Reserve a sku only after every preceding check has passed.
            if (!importedSkus.Add(normalizedSku))
            {
                dropped.Add(new DroppedRow(line, "duplicate-sku"));
                continue;
            }

            products.Add(new Product(
                normalizedSku,
                nameText.Trim(),
                price,
                categories[index]));
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
