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

        // Reject malformed feeds before reading even the first row.
        if (rows.Count != categories.Count)
        {
            return LengthMismatch();
        }

        // Exact pairing never silently truncates either input.
        return rows.ZipExactOrNone(categories).Match(
            pairs => CleanPairs(pairs, catalog),
            LengthMismatch);
    }

    private static CleanOutcome CleanPairs(
        IReadOnlyList<(RawRow First, string Second)> pairs,
        SkuCatalog catalog)
    {
        var importedSkus = new HashSet<string>(StringComparer.Ordinal);

        // Partition enumerates once, preserving order on both sides. Only a
        // successful row reserves its normalized SKU.
        var partition = pairs
            .Select((pair, index) => CleanRow(
                pair.First, pair.Second, index + 1, catalog, importedSkus))
            .Partition();

        // The non-empty guarantee makes the seedless sum safe; absence becomes
        // a nullable average rather than an empty-sequence exception.
        decimal? averagePrice = partition.Passed
            .Select(product => product.Price)
            .ToNonEmptyOrNone()
            .Map(prices => decimal.Round(
                prices.Aggregate(static (left, right) => left + right) / prices.Count,
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

        var normalizedSku = skuText.Trim().ToUpperInvariant();
        if (!catalog.IsKnown(normalizedSku))
        {
            return Drop(line, "unknown-sku");
        }

        var priceText = row.PriceText?.Trim();
        if (!decimal.TryParse(
            priceText,
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out var price))
        {
            return Drop(line, "invalid-price");
        }

        if (!importedSkus.Add(normalizedSku))
        {
            return Drop(line, "duplicate-sku");
        }

        return Result<Product, DroppedRow>.Success(
            new Product(normalizedSku, nameText.Trim(), price, category));
    }

    private static Result<Product, DroppedRow> Drop(int line, string reason) =>
        Result<Product, DroppedRow>.Failure(new DroppedRow(line, reason));

    private static CleanOutcome LengthMismatch() => new(
        Array.Empty<Product>(),
        Array.Empty<DroppedRow>(),
        new[] { "length-mismatch" },
        null);
}
