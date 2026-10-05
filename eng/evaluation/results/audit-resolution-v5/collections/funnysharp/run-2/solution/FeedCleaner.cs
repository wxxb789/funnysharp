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

        // Gate positional pairing before reading any row; never truncate the feed.
        if (rows.Count != categories.Count)
        {
            return new CleanOutcome(
                Array.Empty<Product>(),
                Array.Empty<DroppedRow>(),
                new[] { "length-mismatch" },
                null);
        }

        var importedSkus = new HashSet<string>(StringComparer.Ordinal);

        // Partition enumerates once, preserving row order within both outcomes.
        var partition = rows
            .Select((row, index) => CleanRow(
                row,
                categories[index],
                index + 1,
                catalog,
                importedSkus))
            .Partition();

        // Explicit absence avoids a seedless fold on an empty collection.
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
        // Early returns make the first-failing-check policy explicit.
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

        string? priceText = row.PriceText?.Trim();
        if (!decimal.TryParse(
            priceText,
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out decimal price))
        {
            return Drop(line, "invalid-price");
        }

        // Only a row that passed every preceding check can reserve its sku.
        if (!importedSkus.Add(sku))
        {
            return Drop(line, "duplicate-sku");
        }

        return Result<Product, DroppedRow>.Success(
            new Product(sku, row.Name.Trim(), price, category));
    }

    private static Result<Product, DroppedRow> Drop(int line, string reason) =>
        Result<Product, DroppedRow>.Failure(new DroppedRow(line, reason));
}
