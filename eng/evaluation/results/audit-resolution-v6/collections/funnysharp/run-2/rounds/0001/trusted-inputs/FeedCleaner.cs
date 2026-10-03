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

        // Check counts before pairing: malformed feeds must not examine any row.
        if (rows.Count != categories.Count)
        {
            return LengthMismatch();
        }

        return rows.ZipExactOrNone(categories).Match(
            pairs => CleanPairs(pairs, catalog),
            LengthMismatch);
    }

    private static CleanOutcome CleanPairs(
        IReadOnlyList<(RawRow First, string Second)> pairs,
        SkuCatalog catalog)
    {
        var importedSkus = new HashSet<string>(StringComparer.Ordinal);

        // Partition enumerates once, preserving order on both sides. Only successful
        // rows reserve their normalized SKU in this invocation's set.
        var partition = pairs
            .Select((pair, index) => CleanRow(
                pair.First, pair.Second, index + 1, catalog, importedSkus))
            .Partition();

        decimal? averagePrice = partition.Passed
            .Select(product => product.Price)
            .ToNonEmptyOrNone()
            .Match<decimal?>(
                prices => decimal.Round(
                    prices.Aggregate(static (total, price) => total + price) / prices.Count,
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
        // Each return prevents all later checks for this row from running.
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

        // This is the final check, so dropped rows never reserve a SKU.
        if (!importedSkus.Add(normalizedSku))
        {
            return Drop(line, "duplicate-sku");
        }

        return Result<Product, DroppedRow>.Success(
            new Product(normalizedSku, row.Name.Trim(), price, category));
    }

    private static Result<Product, DroppedRow> Drop(int line, string reason) =>
        Result<Product, DroppedRow>.Failure(new DroppedRow(line, reason));

    private static CleanOutcome LengthMismatch() =>
        new CleanOutcome(
            Array.Empty<Product>(),
            Array.Empty<DroppedRow>(),
            new[] { "length-mismatch" },
            null);
}
