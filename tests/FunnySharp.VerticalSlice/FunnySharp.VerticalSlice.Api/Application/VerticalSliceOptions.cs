namespace FunnySharp.VerticalSlice.Application;

/// <summary>
/// Simulated cost and fan-out settings. Tests and measurements replace latency with zero to keep a
/// run deterministic while production-like hosts keep the defaults.
/// </summary>
public sealed class VerticalSliceOptions
{
    public const string SectionName = "VerticalSlice";

    public TimeSpan StoreLatency { get; set; } = TimeSpan.FromMilliseconds(2);

    public TimeSpan DependencyLatency { get; set; } = TimeSpan.FromMilliseconds(2);

    public TimeSpan PaymentLatency { get; set; } = TimeSpan.FromMilliseconds(3);

    public TimeSpan SupplierLatency { get; set; } = TimeSpan.FromMilliseconds(4);

    public int SupplierCount { get; set; } = 6;

    public int QuoteConcurrency { get; set; } = 4;

    public int MaximumOrderLines { get; set; } = 10;

    public string Currency { get; set; } = "EUR";
}
