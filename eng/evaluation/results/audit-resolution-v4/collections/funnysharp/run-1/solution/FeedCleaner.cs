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

        // Reject malformed feeds before reading or validating any row.
        if (rows.Count != categories.Count)
        {
            return LengthMismatch();
        }

        ArgumentNullException.ThrowIfNull(catalog);

        return rows.ZipExactOrNone(categories).Match(
            pairs =>
            {
                var importedSkus = new HashSet<string>(StringComparer.Ordinal);

                // Partition consumes this projection once, preserving order on both sides.
                var partition = pairs
                    .Select((pair, index) => CleanRow(
                        pair.First,
                        pair.Second,
                        index + 1,
                        catalog,
                        importedSkus))
                    .Partition();

                decimal? averagePrice = partition.Passed
                    .Select(product => product.Price)
                    .ToNonEmptyOrNone()
                    .Match(
                        prices => (decimal?)decimal.Round(
                            prices.Aggregate(static (left, right) => left + right) / prices.Count,
                            2,
                            MidpointRounding.AwayFromZero),
                        static () => (decimal?)null);

                return new CleanOutcome(
                    partition.Passed,
                    partition.Failed,
                    Array.Empty<string>(),
                    averagePrice);
            },
            static () => LengthMismatch());
    }

    private static Result<Product, DroppedRow> CleanRow(
        RawRow row,
        string category,
        int line,
        SkuCatalog catalog,
        HashSet<string> importedSkus)
    {
        // Each return prevents every later check from running for this row.
        if (string.IsNullOrWhiteSpace(row.Sku))
        {
            return Drop(line, "blank-sku");
        }

        if (string.IsNullOrWhiteSpace(row.Name))
        {
            return Drop(line, "blank-name");
        }

        string sku = row.Sku.Trim().ToUpperInvariant();
        if (!catalog.IsKnown(sku))
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

        // Reserve a sku only after all earlier checks have passed.
        if (!importedSkus.Add(sku))
        {
            return Drop(line, "duplicate-sku");
        }

        return Result<Product, DroppedRow>.Success(
            new Product(sku, row.Name.Trim(), price, category));
    }

    private static Result<Product, DroppedRow> Drop(int line, string reason) =>
        Result<Product, DroppedRow>.Failure(new DroppedRow(line, reason));

    private static CleanOutcome LengthMismatch() => new(
        Array.Empty<Product>(),
        Array.Empty<DroppedRow>(),
        new[] { "length-mismatch" },
        null);
}
