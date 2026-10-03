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

        // Check exact pairing before reading any row or looking up any SKU.
        if (rows.Count != categories.Count)
        {
            return new CleanOutcome(
                Array.Empty<Product>(),
                Array.Empty<DroppedRow>(),
                new[] { "length-mismatch" },
                null);
        }

        var importedSkus = new HashSet<string>(StringComparer.Ordinal);

        // Equal lengths guarantee a category at every row's index. Partition
        // evaluates each row once, preserving order on both outcome sides.
        var partition = rows
            .Select((row, index) => CleanRow(
                row,
                categories[index],
                index + 1,
                catalog,
                importedSkus))
            .Partition();

        decimal? averagePrice = partition.Passed
            .Select(product => product.Price)
            .ToNonEmptyOrNone()
            .Match<decimal?>(
                prices => decimal.Round(
                    prices.Aggregate(static (left, right) => left + right) / prices.Count,
                    2,
                    MidpointRounding.AwayFromZero),
                () => null);

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
        // Each early return prevents the later checks from running.
        if (string.IsNullOrWhiteSpace(row.Sku))
        {
            return Drop(line, "blank-sku");
        }

        if (string.IsNullOrWhiteSpace(row.Name))
        {
            return Drop(line, "blank-name");
        }

        string normalizedSku = row.Sku.Trim().ToUpperInvariant();
        if (!catalog.IsKnown(normalizedSku))
        {
            return Drop(line, "unknown-sku");
        }

        if (!decimal.TryParse(
            row.PriceText?.Trim(),
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out decimal price))
        {
            return Drop(line, "invalid-price");
        }

        // Only a row that passed all earlier checks may reserve its SKU.
        if (!importedSkus.Add(normalizedSku))
        {
            return Drop(line, "duplicate-sku");
        }

        return Result<Product, DroppedRow>.Success(
            new Product(normalizedSku, row.Name.Trim(), price, category));
    }

    private static Result<Product, DroppedRow> Drop(int line, string reason) =>
        Result<Product, DroppedRow>.Failure(new DroppedRow(line, reason));
}
