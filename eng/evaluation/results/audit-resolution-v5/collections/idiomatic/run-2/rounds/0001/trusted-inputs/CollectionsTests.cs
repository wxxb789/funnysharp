public sealed class CollectionsTests
{
    private static readonly IReadOnlySet<string> KnownSkus = new HashSet<string>
    {
        "KB-1",
        "KB-2",
        "MS-2",
        "MN-3",
        "TB-4",
    };

    private static SkuCatalog Catalog() => new(KnownSkus);

    [Fact]
    public void ValidFeedIsNormalizedAndImportedInOrder()
    {
        var outcome = FeedCleaner.Clean(
            [
                new RawRow(" kb-1 ", "  Mechanical  Keyboard ", " 40.00 "),
                new RawRow("MS-2", "Wireless Mouse", "15.5"),
            ],
            [" peripherals ", "input"],
            Catalog());
        // The sku is trimmed and uppercased, the name is trimmed, and the inner spacing and
        // the paired category label are carried through unchanged.
        Assert.Equal(
            [
                new Product("KB-1", "Mechanical  Keyboard", 40.00m, " peripherals "),
                new Product("MS-2", "Wireless Mouse", 15.5m, "input"),
            ],
            outcome.Products);
        Assert.Empty(outcome.Dropped);
        Assert.Empty(outcome.Errors);
        // (40.00 + 15.5) / 2.
        Assert.Equal(27.75m, outcome.AveragePrice);
    }

    [Fact]
    public void BlankRequiredFieldsAreDroppedInLineOrder()
    {
        var outcome = FeedCleaner.Clean(
            [
                new RawRow(null, "Keyboard", "40.00"),
                new RawRow("KB-1", "   ", "40.00"),
                new RawRow("KB-1", "Keyboard", "40.00"),
                new RawRow("", "", ""),
            ],
            ["a", "b", "c", "d"],
            Catalog());
        // A row blank on both fields reports blank-sku: the sku check runs first.
        Assert.Equal([new Product("KB-1", "Keyboard", 40.00m, "c")], outcome.Products);
        Assert.Equal(
            [
                new DroppedRow(1, "blank-sku"),
                new DroppedRow(2, "blank-name"),
                new DroppedRow(4, "blank-sku"),
            ],
            outcome.Dropped);
        // Every row lands on exactly one side of the partition.
        Assert.Equal(4, outcome.Products.Count + outcome.Dropped.Count);
        Assert.Empty(outcome.Errors);
        Assert.Equal(40.00m, outcome.AveragePrice);
    }

    [Fact]
    public void UnparsablePricesAreDroppedDeterministically()
    {
        var outcome = FeedCleaner.Clean(
            [
                new RawRow("KB-1", "Keyboard", "free"),
                new RawRow("KB-2", "Spare keyboard", null),
                new RawRow("MN-3", "Monitor", " "),
                new RawRow("TB-4", "Trackball", "25.00"),
            ],
            ["a", "b", "c", "d"],
            Catalog());
        Assert.Equal([new Product("TB-4", "Trackball", 25.00m, "d")], outcome.Products);
        Assert.Equal(
            [
                new DroppedRow(1, "invalid-price"),
                new DroppedRow(2, "invalid-price"),
                new DroppedRow(3, "invalid-price"),
            ],
            outcome.Dropped);
        Assert.Empty(outcome.Errors);
        Assert.Equal(25.00m, outcome.AveragePrice);
    }

    [Fact]
    public void UnknownSkusAreDroppedAfterNormalization()
    {
        var outcome = FeedCleaner.Clean(
            [
                new RawRow(" kb-1 ", "Keyboard", "40.00"),
                new RawRow("kb-9", "Mystery knob", "10.00"),
                new RawRow("ZZ-1", "Zilch", "5.00"),
            ],
            ["a", "b", "c"],
            Catalog());
        // The padded lowercase kb-1 resolves against the catalog after normalization.
        Assert.Equal([new Product("KB-1", "Keyboard", 40.00m, "a")], outcome.Products);
        Assert.Equal(
            [new DroppedRow(2, "unknown-sku"), new DroppedRow(3, "unknown-sku")],
            outcome.Dropped);
        Assert.Empty(outcome.Errors);
        Assert.Equal(40.00m, outcome.AveragePrice);
    }

    [Fact]
    public void FirstRowPerSkuIsKeptAndLaterDuplicatesAreDropped()
    {
        var outcome = FeedCleaner.Clean(
            [
                new RawRow("KB-1", "Keyboard v1", "40.00"),
                new RawRow("kb-1", "Keyboard v2", "35.00"),
                new RawRow("MS-2", "Mouse", "free"),
                new RawRow("MS-2", "Mouse", "15.00"),
            ],
            ["a", "b", "c", "d"],
            Catalog());
        // The kb-1 duplicate is caught on the normalized sku, and the dropped MS-2 row does
        // not reserve its sku, so the later MS-2 row still imports.
        Assert.Equal(
            [
                new Product("KB-1", "Keyboard v1", 40.00m, "a"),
                new Product("MS-2", "Mouse", 15.00m, "d"),
            ],
            outcome.Products);
        Assert.Equal(
            [new DroppedRow(2, "duplicate-sku"), new DroppedRow(3, "invalid-price")],
            outcome.Dropped);
        Assert.Empty(outcome.Errors);
        Assert.Equal(27.50m, outcome.AveragePrice);
    }

    [Fact]
    public void LengthMismatchIsReportedAndStopsTheClean()
    {
        var tooFewCategories = FeedCleaner.Clean(
            [new RawRow("KB-1", "Keyboard", "40.00"), new RawRow("MS-2", "Mouse", "15.00")],
            ["a"],
            Catalog());
        Assert.Equal(["length-mismatch"], tooFewCategories.Errors);
        Assert.Empty(tooFewCategories.Products);
        Assert.Empty(tooFewCategories.Dropped);
        Assert.Null(tooFewCategories.AveragePrice);

        var tooManyCategories = FeedCleaner.Clean(
            [new RawRow("KB-1", "Keyboard", "40.00")],
            ["a", "b"],
            Catalog());
        Assert.Equal(["length-mismatch"], tooManyCategories.Errors);
        Assert.Empty(tooManyCategories.Products);
        Assert.Empty(tooManyCategories.Dropped);
        Assert.Null(tooManyCategories.AveragePrice);
    }

    [Fact]
    public void EmptyFeedProducesNoProductsAndNoAverage()
    {
        var outcome = FeedCleaner.Clean([], [], Catalog());
        Assert.Empty(outcome.Products);
        Assert.Empty(outcome.Dropped);
        Assert.Empty(outcome.Errors);
        Assert.Null(outcome.AveragePrice);
    }

    [Fact]
    public void FeedWhereNothingSurvivesLeavesTheAverageAbsent()
    {
        var outcome = FeedCleaner.Clean(
            [new RawRow("KB-1", "Keyboard", "free"), new RawRow("kb-9", "Mystery knob", "10.00")],
            ["a", "b"],
            Catalog());
        Assert.Empty(outcome.Products);
        Assert.Equal(
            [new DroppedRow(1, "invalid-price"), new DroppedRow(2, "unknown-sku")],
            outcome.Dropped);
        Assert.Empty(outcome.Errors);
        Assert.Null(outcome.AveragePrice);
    }

    [Fact]
    public void MixedFeedPartitionsEveryRowAndRoundsTheAverage()
    {
        var outcome = FeedCleaner.Clean(
            [
                new RawRow(null, "Keyboard", "40.00"),
                new RawRow("KB-1", "  ", "40.00"),
                new RawRow("kb-9", "Mystery knob", "40.00"),
                new RawRow("KB-1", "Keyboard", "soon"),
                new RawRow("KB-1", "Keyboard", "10.00"),
                new RawRow("kb-1", "Keyboard again", "99.00"),
                new RawRow("MS-2", "Mouse", "10.01"),
            ],
            ["a", "b", "c", "d", "e", "f", "g"],
            Catalog());
        Assert.Equal(
            [
                new Product("KB-1", "Keyboard", 10.00m, "e"),
                new Product("MS-2", "Mouse", 10.01m, "g"),
            ],
            outcome.Products);
        Assert.Equal(
            [
                new DroppedRow(1, "blank-sku"),
                new DroppedRow(2, "blank-name"),
                new DroppedRow(3, "unknown-sku"),
                new DroppedRow(4, "invalid-price"),
                new DroppedRow(6, "duplicate-sku"),
            ],
            outcome.Dropped);
        // Every row landed on exactly one side: 2 imported + 5 dropped = 7 rows.
        Assert.Equal(7, outcome.Products.Count + outcome.Dropped.Count);
        Assert.Empty(outcome.Errors);
        // The mean of 10.00 and 10.01 is 10.005, rounded half away from zero.
        Assert.Equal(10.01m, outcome.AveragePrice);
    }
}
