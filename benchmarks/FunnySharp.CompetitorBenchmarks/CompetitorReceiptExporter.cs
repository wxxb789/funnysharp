namespace FunnySharp.Benchmarks;

/// <summary>
/// Writes competitor-comparison performance receipts in the same schema as the main suite, bound to
/// <c>eng/performance/competitor-baseline.json</c> instead of the main manifest, through the shared
/// <see cref="ReceiptExporterCore"/>. This project is isolated, non-packable, and excluded from
/// FunnySharp.slnx and every release artifact: pinned competitor packages are performance evidence
/// only, never API-compatibility evidence.
/// </summary>
internal sealed class CompetitorReceiptExporter : ReceiptExporterCore
{
    public CompetitorReceiptExporter()
        : base(
            nameof(CompetitorReceiptExporter),
            "eng/performance/competitor-baseline.json",
            "Competitor performance receipt")
    {
    }
}
