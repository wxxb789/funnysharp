using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using FunnySharp;

// Feed cleaning for the legacy product import: the exporter ships the feed as two parallel
// lists that pair by position, and this step turns them into imported products, dropped rows
// carrying the reason of the first failing rule, and the rounded mean price. Every drop rule
// is one named step of a fail-fast Result chain, so a maintainer sees at a glance which rule
// dropped a row and why; the two absence shapes (an unparsable price, an empty aggregate)
// stay Options.
public static class FeedCleaner
{
    private const string LengthMismatch = "length-mismatch";
    private const string BlankSku = "blank-sku";
    private const string BlankName = "blank-name";
    private const string UnknownSku = "unknown-sku";
    private const string InvalidPrice = "invalid-price";
    private const string DuplicateSku = "duplicate-sku";

    public static CleanOutcome Clean(
        IReadOnlyList<RawRow> rows,
        IReadOnlyList<string> categories,
        SkuCatalog catalog) =>
        rows
            // Rule 1, pairing: the two lists pair by position and never truncate to the
            // shorter one, so an unequal length fails the whole feed before a single row is
            // examined. Whichever side is longer, the only feed-level error is
            // length-mismatch.
            .ZipExact(categories, (rowCount, categoryCount) => LengthMismatch)
            .Match(
                pairs => CleanRows(pairs, catalog),
                error => new CleanOutcome([], [], [error], null));

    // Rules 2 to 4, cleaning, normalization, and partition: every row of a well-formed feed
    // lands on exactly one side, in row order.
    private static CleanOutcome CleanRows(
        IReadOnlyList<(RawRow Row, string Category)> pairs,
        SkuCatalog catalog)
    {
        var products = new List<Product>();
        var dropped = new List<DroppedRow>();
        var importedSkus = new HashSet<string>();

        for (var index = 0; index < pairs.Count; index++)
        {
            var (row, category) = pairs[index];

            // Every row lands on exactly one side of the partition, in row order; the drop
            // reason names the first failing check for this line.
            ImportRow(row, category, catalog, importedSkus).Match(
                product =>
                {
                    products.Add(product);

                    // Only a surviving row reserves its normalized sku; a dropped row never
                    // does.
                    importedSkus.Add(product.Sku);
                },
                reason => dropped.Add(new DroppedRow(index + 1, reason)));
        }

        return new CleanOutcome(products, dropped, [], AveragePrice(products));
    }

    // The per-row checks in evaluation order: blank-sku, blank-name, unknown-sku,
    // invalid-price, duplicate-sku. The chain is fail-fast, so the first failing check drops
    // the row and the row's later checks never run.
    private static Result<Product, string> ImportRow(
        RawRow row,
        string category,
        SkuCatalog catalog,
        IReadOnlySet<string> importedSkus) =>
        NormalizeSku(row.Sku)
            .Bind(sku => TrimName(row.Name).Map(name => (Sku: sku, Name: name)))
            .Ensure(fields => catalog.IsKnown(fields.Sku), UnknownSku)
            .Bind(fields => ParsePrice(row.PriceText)
                .ToResult(InvalidPrice)
                .Map(price => (Sku: fields.Sku, Name: fields.Name, Price: price)))
            .Ensure(priced => !importedSkus.Contains(priced.Sku), DuplicateSku)
            .Map(priced => new Product(priced.Sku, priced.Name, priced.Price, category));

    // blank-sku: the sku must carry content. The trimmed, invariant-uppercased form drives
    // both the catalog lookup and the duplicate check.
    private static Result<string, string> NormalizeSku(string? sku) =>
        Option.FromNullable(sku)
            .Map(text => text.Trim().ToUpperInvariant())
            .Filter(text => text.Length > 0)
            .ToResult(BlankSku);

    // blank-name: the display name is required; its case and inner spacing stay untouched.
    private static Result<string, string> TrimName(string? name) =>
        Option.FromNullable(name)
            .Map(text => text.Trim())
            .Filter(text => text.Length > 0)
            .ToResult(BlankName);

    // invalid-price: the trimmed text must parse as an invariant-culture decimal with the
    // standard number style; the parsed value travels on exactly as parsed.
    private static Option<decimal> ParsePrice(string? priceText) =>
        Option.FromNullable(priceText)
            .Map(text => text.Trim())
            .Filter(text => text.Length > 0)
            .Bind(text => text.ParseOrNone<decimal>(CultureInfo.InvariantCulture));

    // Rule 5, aggregate: the arithmetic mean of the surviving prices, computed in decimal
    // and rounded half away from zero to two decimals; absent when nothing survives, never
    // an exception.
    private static decimal? AveragePrice(IReadOnlyList<Product> products) =>
        products
            .Select(product => product.Price)
            .ToNonEmptyOrNone()
            .Map(prices => Math.Round(
                prices.Aggregate(static (sum, price) => sum + price) / prices.Count,
                2,
                MidpointRounding.AwayFromZero))
            .ToNullable();
}
