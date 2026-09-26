namespace FunnySharp.Benchmarks;

/// <summary>
/// Writes main-suite performance receipts through the shared <see cref="ReceiptExporterCore"/>, bound to
/// <c>eng/performance/baseline.json</c>.
/// </summary>
internal sealed class AllocationReceiptExporter : ReceiptExporterCore
{
    public AllocationReceiptExporter()
        : base(
            nameof(AllocationReceiptExporter),
            "eng/performance/baseline.json",
            "Performance receipt")
    {
    }
}
