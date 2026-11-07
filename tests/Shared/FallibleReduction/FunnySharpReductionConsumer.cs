using FunnySharp;

namespace FunnySharp.TestSupport.FallibleReduction;

internal static class FunnySharpReductionConsumer
{
    internal static Result<ReductionTotal, ReductionError> Run(
        IEnumerable<ReductionLine> source,
        Func<ReductionTotal, ReductionLine, Result<ReductionTotal, ReductionError>> step) =>
        source.AggregateResult(new ReductionTotal(), step);

    internal static ValueTask<Result<ReductionTotal, ReductionError>> RunAsync(
        IAsyncEnumerable<ReductionLine> source,
        Func<ReductionTotal, ReductionLine, Result<ReductionTotal, ReductionError>> step,
        CancellationToken cancellationToken) =>
        source.AggregateResultAsync(new ReductionTotal(), step, cancellationToken);

    internal static ValueTask<Result<ReductionTotal, ReductionError>> RunValueAsync(
        IAsyncEnumerable<ReductionLine> source,
        Func<ReductionTotal, ReductionLine, ValueTask<Result<ReductionTotal, ReductionError>>> step,
        CancellationToken cancellationToken) =>
        source.AggregateResultValueAsync(new ReductionTotal(), step, cancellationToken);

    internal static ValueTask<Result<ReductionTotal, ReductionError>> RunTokenAwareValueAsync(
        IAsyncEnumerable<ReductionLine> source,
        Func<ReductionTotal, ReductionLine, CancellationToken, ValueTask<Result<ReductionTotal, ReductionError>>> step,
        CancellationToken cancellationToken) =>
        source.AggregateResultValueAsync(new ReductionTotal(), step, cancellationToken);
}
