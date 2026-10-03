## Business brief: product import feed cleaning

You are implementing the feed-cleaning step of a small shop's product import as a library
consumed by a fixed xUnit test suite. The test suite and the neutral domain contract are
already provided in the build directory; you implement the cleaning behind the seam below and
the tests must pass.

Seam (you must define this exactly):

public static class FeedCleaner
{
    public static CleanOutcome Clean(
        IReadOnlyList<RawRow> rows,
        IReadOnlyList<string> categories,
        SkuCatalog catalog)
}

Declare your types in the global namespace (no namespace declaration) in the files you write.
The provided contract defines RawRow, Product, DroppedRow, CleanOutcome, and SkuCatalog; do
not redefine them. The provided fake is deterministic:

- SkuCatalog.IsKnown returns true only for the skus it was constructed with.

The legacy exporter ships the feed as two parallel lists: the raw rows and one category label
per row. The two lists pair up by position - the category label at index i belongs to the row
at index i - and the pairing is exact: it never truncates to the shorter list.

Business rules, in evaluation order:

1. Pairing - if rows and categories differ in length, the feed is malformed: Errors is exactly
   [length-mismatch] (the only feed-level error), Products and Dropped are empty, AveragePrice
   is null, and no row is examined at all.
2. Cleaning - scan the rows in order (the first row is line 1). Check each row against the
   checks below in this order; the first failing check drops the row as DroppedRow(Line,
   Reason) with that reason, and the row's later checks do not run:
   - blank-sku: Sku is null, empty, or whitespace-only.
   - blank-name: Name is null, empty, or whitespace-only.
   - unknown-sku: the normalized sku is not in the catalog (SkuCatalog.IsKnown).
   - invalid-price: PriceText, trimmed, is null, empty, or does not parse as a decimal in the
     invariant culture with the standard number style (decimal.TryParse(text,
     NumberStyles.Number, CultureInfo.InvariantCulture, out _) is the canonical call).
   - duplicate-sku: the normalized sku was already imported by an earlier surviving row; a
     dropped row does not reserve its sku.
3. Normalization - a surviving row imports as Product(NormalizedSku, Name, Price, Category):
   - NormalizedSku is Sku trimmed and uppercased with the invariant culture; the catalog
     lookup and the duplicate check both use this normalized sku.
   - Name is trimmed; its case and inner spacing are untouched.
   - Price is the parsed decimal exactly as parsed, not rounded.
   - Category is the row's paired category label, carried through unchanged: not validated,
     not trimmed.
4. Partition - every row of a well-formed feed lands on exactly one side: Products holds the
   imported rows in row order, Dropped holds the dropped rows in row order, and no row is
   both or neither.
5. Aggregate - AveragePrice is the arithmetic mean of the imported products' prices, computed
   in decimal and rounded half away from zero to two decimals; when no row survives it is
   null - absence, never an exception.

Requirements:

- Correctness under the provided tests is the acceptance bar; the tests are visible to you.
- Keep the pairing, drop, and aggregate semantics explicit and readable: a maintainer should
  see at a glance which rule dropped a row and why.
- Do not read or copy the FunnySharp repository source code.


## Style

Use idiomatic C#, .NET 10 BCL only.

Deliver C# files only in solution/. Do not read repository source, historical solutions, audits, plans or other sessions.
