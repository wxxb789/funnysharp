using FunnySharp;

namespace FunnySharp.TestSupport.FallibleReduction;

internal static class OrdinaryReductionConsumer
{
    internal static Result<ReductionTotal, ReductionError> Run(
        IEnumerable<ReductionLine> source,
        Func<ReductionTotal, ReductionLine, Result<ReductionTotal, ReductionError>> step) =>
        Run<ReductionLine, ReductionTotal, ReductionError>(source, new ReductionTotal(), step);

    internal static Result<TAccumulate, TError> Run<TSource, TAccumulate, TError>(
        IEnumerable<TSource> source,
        TAccumulate seed,
        Func<TAccumulate, TSource, Result<TAccumulate, TError>> step)
    {
        var accumulator = seed;
        foreach (var item in source)
        {
            var result = step(accumulator, item);
            if (!result.TryGetValue(out var next))
            {
                return result;
            }

            accumulator = next;
        }

        return Result<TAccumulate, TError>.Success(accumulator);
    }

    internal static ValueTask<Result<ReductionTotal, ReductionError>> RunAsync(
        IAsyncEnumerable<ReductionLine> source,
        Func<ReductionTotal, ReductionLine, Result<ReductionTotal, ReductionError>> step,
        CancellationToken cancellationToken) =>
        RunAsync<ReductionLine, ReductionTotal, ReductionError>(
            source,
            new ReductionTotal(),
            step,
            cancellationToken);

    internal static async ValueTask<Result<TAccumulate, TError>> RunAsync<TSource, TAccumulate, TError>(
        IAsyncEnumerable<TSource> source,
        TAccumulate seed,
        Func<TAccumulate, TSource, Result<TAccumulate, TError>> step,
        CancellationToken cancellationToken)
    {
        var accumulator = seed;
        await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            var result = step(accumulator, item);
            if (!result.TryGetValue(out var next))
            {
                return result;
            }

            accumulator = next;
        }

        return Result<TAccumulate, TError>.Success(accumulator);
    }

    internal static ValueTask<Result<ReductionTotal, ReductionError>> RunValueAsync(
        IAsyncEnumerable<ReductionLine> source,
        Func<ReductionTotal, ReductionLine, ValueTask<Result<ReductionTotal, ReductionError>>> step,
        CancellationToken cancellationToken) =>
        RunValueAsync<ReductionLine, ReductionTotal, ReductionError>(
            source,
            new ReductionTotal(),
            step,
            cancellationToken);

    internal static async ValueTask<Result<TAccumulate, TError>> RunValueAsync<TSource, TAccumulate, TError>(
        IAsyncEnumerable<TSource> source,
        TAccumulate seed,
        Func<TAccumulate, TSource, ValueTask<Result<TAccumulate, TError>>> step,
        CancellationToken cancellationToken)
    {
        var accumulator = seed;
        await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            var result = await step(accumulator, item).ConfigureAwait(false);
            if (!result.TryGetValue(out var next))
            {
                return result;
            }

            accumulator = next;
        }

        return Result<TAccumulate, TError>.Success(accumulator);
    }
}
