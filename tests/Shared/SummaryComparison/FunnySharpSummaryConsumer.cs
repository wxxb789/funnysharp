using FunnySharp;

namespace FunnySharp.TestSupport.SummaryComparison;

internal static class FunnySharpSummaryConsumer
{
    internal static OrderReport Evaluate(IEnumerable<OrderLine> source) =>
        SummaryRules.Composed.Evaluate(source);

    internal static ValueTask<OrderReport> EvaluateAsync(
        IAsyncEnumerable<OrderLine> source,
        CancellationToken cancellationToken = default) =>
        SummaryRules.Composed.EvaluateAsync(source, cancellationToken);
}
