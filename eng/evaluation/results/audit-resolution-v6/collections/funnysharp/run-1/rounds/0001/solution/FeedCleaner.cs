using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FunnySharp;

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

        // Reject malformed pairing before enumerating or inspecting any row.
        if (rows.Count != categories.Count)
        {
            return new CleanOutcome(
                Array.Empty<Product>(),
                Array.Empty<DroppedRow>(),
                new[] { "length-mismatch" },
                null);
        }

        var importedSkus = new HashSet<string>(StringComparer.Ordinal);

        // Equal lengths guarantee exact positional pairing, without truncation.
        // Partition enumerates once, retaining source order on both sides.
        var partition = rows
            .Select((row, index) => CleanRow(
                row, categories[index], index + 1, catalog, importedSkus))
            .Partition();

        // The lazy factory never calls Average on an empty collection.
        decimal? averagePrice = Option.FromBoolean(
                partition.Passed.Count > 0,
                () => decimal.Round(
                    partition.Passed.Average(product => product.Price),
                    2,
                    MidpointRounding.AwayFromZero))
            .ToNullable();

        return new CleanOutcome(
            partition.Passed,
            partition.Failed,
            Array.Empty<string>(),
            averagePrice);
    }

    private static Result<Product, DroppedRow> CleanRow(
        RawRow row,
        string category,
        int line,
        SkuCatalog catalog,
        HashSet<string> importedSkus)
    {
        var skuText = row.Sku;
        if (string.IsNullOrWhiteSpace(skuText))
        {
            return Drop(line, "blank-sku");
        }

        var nameText = row.Name;
        if (string.IsNullOrWhiteSpace(nameText))
        {
            return Drop(line, "blank-name");
        }

        var sku = skuText.Trim().ToUpperInvariant();
        if (!catalog.IsKnown(sku))
        {
            return Drop(line, "unknown-sku");
        }

        var priceText = row.PriceText?.Trim();
        if (string.IsNullOrEmpty(priceText) ||
            !decimal.TryParse(
                priceText,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var price))
        {
            return Drop(line, "invalid-price");
        }

        // Only rows passing all preceding checks reserve their normalized SKU.
        if (!importedSkus.Add(sku))
        {
            return Drop(line, "duplicate-sku");
        }

        return Result<Product, DroppedRow>.Success(
            new Product(sku, nameText.Trim(), price, category));
    }

    private static Result<Product, DroppedRow> Drop(int line, string reason) =>
        Result<Product, DroppedRow>.Failure(new DroppedRow(line, reason));
}
