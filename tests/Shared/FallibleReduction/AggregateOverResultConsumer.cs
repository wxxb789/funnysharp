using FunnySharp;

namespace FunnySharp.TestSupport.FallibleReduction;

internal static class AggregateOverResultConsumer
{
    internal static Result<ReductionTotal, ReductionError> Run(
        IEnumerable<ReductionLine> source,
        Func<ReductionTotal, ReductionLine, Result<ReductionTotal, ReductionError>> step) =>
        Run<ReductionLine, ReductionTotal, ReductionError>(source, new ReductionTotal(), step);

    internal static Result<TAccumulate, TError> Run<TSource, TAccumulate, TError>(
        IEnumerable<TSource> source,
        TAccumulate seed,
        Func<TAccumulate, TSource, Result<TAccumulate, TError>> step) =>
        source.Aggregate(
            Result<TAccumulate, TError>.Success(seed),
            (accumulator, item) => accumulator.Bind(current => step(current, item)));

    internal static ValueTask<Result<ReductionTotal, ReductionError>> RunAsync(
        IAsyncEnumerable<ReductionLine> source,
        Func<ReductionTotal, ReductionLine, Result<ReductionTotal, ReductionError>> step,
        CancellationToken cancellationToken) =>
        RunAsync<ReductionLine, ReductionTotal, ReductionError>(
            source,
            new ReductionTotal(),
            step,
            cancellationToken);

    internal static ValueTask<Result<TAccumulate, TError>> RunAsync<TSource, TAccumulate, TError>(
        IAsyncEnumerable<TSource> source,
        TAccumulate seed,
        Func<TAccumulate, TSource, Result<TAccumulate, TError>> step,
        CancellationToken cancellationToken) =>
        source.AggregateAsync(
            Result<TAccumulate, TError>.Success(seed),
            (accumulator, item) => accumulator.Bind(current => step(current, item)),
            cancellationToken);
}
