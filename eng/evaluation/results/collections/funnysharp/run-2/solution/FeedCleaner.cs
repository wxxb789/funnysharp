using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FunnySharp;

// Feed cleaning for the legacy product-import feed. The exporter ships the raw rows and
// one category label per row as two parallel lists that pair up by position, exactly;
// this seam cleans them into the imported products, the dropped rows, and the average
// price of the imports.
//
// FunnySharp carries the outcomes: Option<T> for absence (a blank field, an unparsable
// price, no average at all), Result<TValue, TError> for the fail-fast ordered row
// checks, and the collection bridges (ZipExactOrNone, Partition, ParseOrNone,
// ToNonEmptyOrNone) for the feed-shaped work.

public static class FeedCleaner
{
    // The drop reasons, in evaluation order, and the only feed-level error.
    private const string LengthMismatch = "length-mismatch";
    private const string BlankSku = "blank-sku";
    private const string BlankName = "blank-name";
    private const string UnknownSku = "unknown-sku";
    private const string InvalidPrice = "invalid-price";
    private const string DuplicateSku = "duplicate-sku";

    /// <summary>Cleans one product-import feed into its imports, drops, errors, and average.</summary>
    /// <param name="rows">The raw rows, in feed order.</param>
    /// <param name="categories">One category label per row, paired by position.</param>
    /// <param name="catalog">The catalog of known, already-normalized skus.</param>
    public static CleanOutcome Clean(
        IReadOnlyList<RawRow> rows,
        IReadOnlyList<string> categories,
        SkuCatalog catalog) =>
        rows
            .ZipExactOrNone(categories)
            .Match(
                paired => CleanPairedFeed(paired, catalog),
                () => MalformedFeed());

    // Rule 1 - pairing: rows and category labels pair by position, exactly. Unequal
    // lengths make the feed malformed before any row is examined.
    private static CleanOutcome MalformedFeed() => new([], [], [LengthMismatch], null);

    // Rules 2-5 over a well-formed feed: scan the paired rows in order (the first row
    // is line 1), so the sku reservation below sees earlier rows first.
    private static CleanOutcome CleanPairedFeed(
        IReadOnlyList<(RawRow First, string Second)> paired,
        SkuCatalog catalog)
    {
        List<Result<Product, DroppedRow>> outcomes = [];
        HashSet<string> importedSkus = [];

        for (int index = 0; index < paired.Count; index++)
        {
            (RawRow row, string category) = paired[index];
            outcomes.Add(ImportRow(index + 1, row, category, catalog, importedSkus));
        }

        // Rule 4 - partition: every row lands on exactly one side, each in row order.
        ResultPartition<Product, DroppedRow> partition = outcomes.Partition();

        // Rule 5 - aggregate: the arithmetic mean of the imported prices, computed in
        // decimal and rounded half away from zero to two decimals; when no row survives
        // the average is absent, never an exception.
        return new CleanOutcome(
            partition.Passed,
            partition.Failed,
            [],
            AveragePrice(partition.Passed).ToNullable());
    }

    // Rules 2 and 3 - one row's outcome: a surviving row imports as a Product, a
    // dropped row reports its line and the first check it failed.
    private static Result<Product, DroppedRow> ImportRow(
        int line,
        RawRow row,
        string category,
        SkuCatalog catalog,
        ISet<string> importedSkus) =>
        CheckRow(row, category, catalog, importedSkus)
            .MapError(reason => new DroppedRow(line, reason));

    // The ordered checks: blank-sku, blank-name, unknown-sku, invalid-price,
    // duplicate-sku. The chain is fail-fast, so the first failure carries its reason
    // and no later check runs for that row. The last reservation is what makes
    // duplicate-sku order-sensitive: Add returns false exactly when the normalized sku
    // was already imported by an earlier surviving row, and a row dropped by an
    // earlier check never reaches the Add, so it reserves nothing.
    private static Result<Product, string> CheckRow(
        RawRow row,
        string category,
        SkuCatalog catalog,
        ISet<string> importedSkus) =>
        NormalizeSku(row.Sku)
            .ToResult(BlankSku)
            .Bind(sku => NormalizeName(row.Name)
                .ToResult(BlankName)
                .Map(name => (Sku: sku, Name: name)))
            .Ensure(known => catalog.IsKnown(known.Sku), UnknownSku)
            .Bind(known => ParsePrice(row.PriceText)
                .ToResult(InvalidPrice)
                .Map(price => (known.Sku, known.Name, Price: price)))
            .Ensure(priced => importedSkus.Add(priced.Sku), DuplicateSku)
            .Map(priced => new Product(priced.Sku, priced.Name, priced.Price, category));

    // Normalization - a blank field is absence, not a value. The sku is trimmed and
    // uppercased with the invariant culture (the catalog lookup and the duplicate
    // check both see this normalized sku); the name is trimmed only, with its case and
    // inner spacing untouched.
    private static Option<string> NormalizeSku(string? sku) =>
        Option
            .FromNullable(sku?.Trim())
            .Filter(trimmed => trimmed.Length > 0)
            .Map(trimmed => trimmed.ToUpperInvariant());

    private static Option<string> NormalizeName(string? name) =>
        Option
            .FromNullable(name?.Trim())
            .Filter(trimmed => trimmed.Length > 0);

    // invalid-price - the trimmed text must parse as an invariant-culture decimal
    // with the standard number style; null, empty, or unparsable text is absence.
    private static Option<decimal> ParsePrice(string? priceText) =>
        Option
            .FromNullable(priceText?.Trim())
            .Filter(trimmed => trimmed.Length > 0)
            .Bind(trimmed => trimmed.ParseOrNone<decimal>(CultureInfo.InvariantCulture));

    // Rule 5 - the mean needs a count, so the non-empty proof carries both the
    // cannot-be-empty fold and the number of imported prices.
    private static Option<decimal> AveragePrice(IEnumerable<Product> products) =>
        products
            .Select(product => product.Price)
            .ToNonEmptyOrNone()
            .Map(prices => Math.Round(
                prices.Aggregate(static (left, right) => left + right) / prices.Count,
                2,
                MidpointRounding.AwayFromZero));
}
