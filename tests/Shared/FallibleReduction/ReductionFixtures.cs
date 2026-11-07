namespace FunnySharp.TestSupport.FallibleReduction;

internal sealed record ReductionFixture(string Name, IReadOnlyList<ReductionLine> Lines);

internal static class ReductionFixtures
{
    internal static readonly ReductionLine Book = new("SKU-BOOK", 2, 12.5m);

    internal static readonly ReductionLine Pen = new("SKU-PEN", 1, 4m);

    internal static readonly ReductionLine InvalidQuantity = new("SKU-BAD", 0, 9m);

    internal static readonly ReductionLine OverLimit = new("SKU-LARGE", 101, 1m);

    internal static readonly ReductionLine Notebook = new("SKU-NOTE", 3, 7.5m);

    internal static readonly IReadOnlyList<ReductionFixture> All =
    [
        new("valid", [Book, Pen]),
        new("invalid-middle", [Book, InvalidQuantity, Notebook]),
        new("invalid-first", [InvalidQuantity, Book]),
        new("over-limit", [Book, OverLimit, Notebook]),
        new("empty", []),
    ];

    internal static readonly IReadOnlyList<ReductionLine> SourceFaultAfterFailure =
        [Book, Pen, InvalidQuantity, Notebook];

    internal const int SourceFaultIndex = 3;
}
