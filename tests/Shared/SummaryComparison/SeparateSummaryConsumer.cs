using FunnySharp;

namespace FunnySharp.TestSupport.SummaryComparison;

internal static class SeparateSummaryConsumer
{
    internal static OrderReport Evaluate(IEnumerable<OrderLine> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return new OrderReport(
            new OrderTotals(
                SummaryRules.Count.Evaluate(source),
                SummaryRules.Total.Evaluate(source),
                SummaryRules.Cheapest.Evaluate(source),
                SummaryRules.MostExpensive.Evaluate(source)),
            SummaryRules.Categories.Evaluate(source),
            SummaryRules.FirstSku.Evaluate(source));
    }

    internal static async ValueTask<OrderReport> EvaluateAsync(
        IAsyncEnumerable<OrderLine> source,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);

        return new OrderReport(
            new OrderTotals(
                await SummaryRules.Count.EvaluateAsync(source, cancellationToken).ConfigureAwait(false),
                await SummaryRules.Total.EvaluateAsync(source, cancellationToken).ConfigureAwait(false),
                await SummaryRules.Cheapest.EvaluateAsync(source, cancellationToken).ConfigureAwait(false),
                await SummaryRules.MostExpensive.EvaluateAsync(source, cancellationToken).ConfigureAwait(false)),
            await SummaryRules.Categories.EvaluateAsync(source, cancellationToken).ConfigureAwait(false),
            await SummaryRules.FirstSku.EvaluateAsync(source, cancellationToken).ConfigureAwait(false));
    }
}
