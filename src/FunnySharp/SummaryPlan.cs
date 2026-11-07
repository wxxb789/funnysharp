using System.Numerics;

namespace FunnySharp;

internal abstract class SummaryPlan<TSource, TResult>
{
    internal abstract SummaryEvaluation<TSource, TResult> CreateEvaluation();

    internal virtual TResult Evaluate(IEnumerable<TSource> source)
    {
        var evaluation = CreateEvaluation();
        foreach (var item in source)
        {
            evaluation.Accumulate(item);
        }

        return evaluation.CreateResult();
    }

    internal virtual async ValueTask<TResult> EvaluateAsync(
        IAsyncEnumerable<TSource> source,
        CancellationToken cancellationToken)
    {
        var evaluation = CreateEvaluation();
        await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            evaluation.Accumulate(item);
        }

        return evaluation.CreateResult();
    }
}

internal abstract class SummaryEvaluation<TSource, TResult>
{
    internal abstract void Accumulate(TSource item);

    internal abstract TResult CreateResult();
}

internal sealed class LeafPlan<TSource, TState, TResult> : SummaryPlan<TSource, TResult>
{
    private readonly Func<TState> seedFactory;
    private readonly Func<TState, TSource, TState> accumulate;
    private readonly Func<TState, TResult> result;

    internal LeafPlan(
        Func<TState> seedFactory,
        Func<TState, TSource, TState> accumulate,
        Func<TState, TResult> result)
    {
        this.seedFactory = seedFactory;
        this.accumulate = accumulate;
        this.result = result;
    }

    internal override TResult Evaluate(IEnumerable<TSource> source)
    {
        var state = seedFactory();
        foreach (var item in source)
        {
            state = accumulate(state, item);
        }

        return result(state);
    }

    internal override async ValueTask<TResult> EvaluateAsync(
        IAsyncEnumerable<TSource> source,
        CancellationToken cancellationToken)
    {
        var state = seedFactory();
        await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            state = accumulate(state, item);
        }

        return result(state);
    }

    internal override SummaryEvaluation<TSource, TResult> CreateEvaluation() =>
        new Evaluation(this);

    private sealed class Evaluation(LeafPlan<TSource, TState, TResult> plan)
        : SummaryEvaluation<TSource, TResult>
    {
        private TState state = plan.seedFactory();

        internal override void Accumulate(TSource item) =>
            state = plan.accumulate(state, item);

        internal override TResult CreateResult() => plan.result(state);
    }
}

internal sealed class MappedPlan<TSource, TResult, TResultNext>
    : SummaryPlan<TSource, TResultNext>
{
    private readonly SummaryPlan<TSource, TResult> inner;
    private readonly Func<TResult, TResultNext> selector;

    internal MappedPlan(
        SummaryPlan<TSource, TResult> inner,
        Func<TResult, TResultNext> selector)
    {
        this.inner = inner;
        this.selector = selector;
    }

    internal override TResultNext Evaluate(IEnumerable<TSource> source) =>
        selector(inner.Evaluate(source));

    internal override async ValueTask<TResultNext> EvaluateAsync(
        IAsyncEnumerable<TSource> source,
        CancellationToken cancellationToken) =>
        selector(await inner.EvaluateAsync(source, cancellationToken).ConfigureAwait(false));

    internal override SummaryEvaluation<TSource, TResultNext> CreateEvaluation() =>
        new Evaluation(inner.CreateEvaluation(), selector);

    private sealed class Evaluation(
        SummaryEvaluation<TSource, TResult> inner,
        Func<TResult, TResultNext> selector)
        : SummaryEvaluation<TSource, TResultNext>
    {
        internal override void Accumulate(TSource item) => inner.Accumulate(item);

        internal override TResultNext CreateResult() => selector(inner.CreateResult());
    }
}

internal sealed class CountPlan<TSource> : SummaryPlan<TSource, int>
{
    internal override int Evaluate(IEnumerable<TSource> source)
    {
        var count = 0;
        foreach (var _ in source)
        {
            count = Step(count);
        }

        return count;
    }

    internal override async ValueTask<int> EvaluateAsync(
        IAsyncEnumerable<TSource> source,
        CancellationToken cancellationToken)
    {
        var count = 0;
        await foreach (var _ in source.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            count = Step(count);
        }

        return count;
    }

    internal override SummaryEvaluation<TSource, int> CreateEvaluation() => new Evaluation();

    private sealed class Evaluation : SummaryEvaluation<TSource, int>
    {
        private int count;

        internal override void Accumulate(TSource item) => count = Step(count);

        internal override int CreateResult() => count;
    }

    private static int Step(int count) => checked(count + 1);
}

internal sealed class SumPlan<TSource, TNumber> : SummaryPlan<TSource, TNumber>
    where TNumber : INumber<TNumber>
{
    private readonly Func<TSource, TNumber> selector;

    internal SumPlan(Func<TSource, TNumber> selector)
    {
        this.selector = selector;
    }

    internal override TNumber Evaluate(IEnumerable<TSource> source)
    {
        var total = TNumber.Zero;
        foreach (var item in source)
        {
            total = Step(total, item);
        }

        return total;
    }

    internal override async ValueTask<TNumber> EvaluateAsync(
        IAsyncEnumerable<TSource> source,
        CancellationToken cancellationToken)
    {
        var total = TNumber.Zero;
        await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            total = Step(total, item);
        }

        return total;
    }

    internal override SummaryEvaluation<TSource, TNumber> CreateEvaluation() =>
        new Evaluation(this);

    private sealed class Evaluation(SumPlan<TSource, TNumber> plan)
        : SummaryEvaluation<TSource, TNumber>
    {
        private TNumber total = TNumber.Zero;

        internal override void Accumulate(TSource item) => total = plan.Step(total, item);

        internal override TNumber CreateResult() => total;
    }

    private TNumber Step(TNumber total, TSource item) => checked(total + selector(item));
}

internal sealed class ExtremumPlan<TSource, TKey> : SummaryPlan<TSource, Option<TKey>>
{
    private readonly Func<TSource, TKey> selector;
    private readonly IComparer<TKey> comparer;
    private readonly bool selectLargest;

    internal ExtremumPlan(
        Func<TSource, TKey> selector,
        IComparer<TKey> comparer,
        bool selectLargest)
    {
        this.selector = selector;
        this.comparer = comparer;
        this.selectLargest = selectLargest;
    }

    internal override Option<TKey> Evaluate(IEnumerable<TSource> source)
    {
        var state = Option<TKey>.None;
        foreach (var item in source)
        {
            state = Accumulate(state, item);
        }

        return state;
    }

    internal override async ValueTask<Option<TKey>> EvaluateAsync(
        IAsyncEnumerable<TSource> source,
        CancellationToken cancellationToken)
    {
        var state = Option<TKey>.None;
        await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            state = Accumulate(state, item);
        }

        return state;
    }

    internal override SummaryEvaluation<TSource, Option<TKey>> CreateEvaluation() =>
        new Evaluation(this);

    private Option<TKey> Accumulate(Option<TKey> state, TSource item)
    {
        var key = selector(item);
        if (key is null)
        {
            return state;
        }

        if (state.TryGetValue(out var current)
            && !ExtremumPolicy.Replaces(
                hasCurrent: true,
                key,
                current,
                comparer,
                selectLargest))
        {
            return state;
        }

        return Option<TKey>.Some(key);
    }

    private sealed class Evaluation(ExtremumPlan<TSource, TKey> plan)
        : SummaryEvaluation<TSource, Option<TKey>>
    {
        private Option<TKey> state = Option<TKey>.None;

        internal override void Accumulate(TSource item) => state = plan.Accumulate(state, item);

        internal override Option<TKey> CreateResult() => state;
    }
}

internal sealed class ZipPlan<TSource, TFirstResult, TSecondResult>
    : SummaryPlan<TSource, (TFirstResult First, TSecondResult Second)>
{
    private readonly SummaryPlan<TSource, TFirstResult> first;
    private readonly SummaryPlan<TSource, TSecondResult> second;

    internal ZipPlan(
        SummaryPlan<TSource, TFirstResult> first,
        SummaryPlan<TSource, TSecondResult> second)
    {
        this.first = first;
        this.second = second;
    }

    internal override SummaryEvaluation<TSource, (TFirstResult First, TSecondResult Second)>
        CreateEvaluation() =>
        new Evaluation(first.CreateEvaluation(), second.CreateEvaluation());

    private sealed class Evaluation(
        SummaryEvaluation<TSource, TFirstResult> first,
        SummaryEvaluation<TSource, TSecondResult> second)
        : SummaryEvaluation<TSource, (TFirstResult First, TSecondResult Second)>
    {
        internal override void Accumulate(TSource item)
        {
            first.Accumulate(item);
            second.Accumulate(item);
        }

        internal override (TFirstResult First, TSecondResult Second) CreateResult() =>
            (first.CreateResult(), second.CreateResult());
    }
}

internal sealed class ZipPlan<TSource, TFirstResult, TSecondResult, TThirdResult>
    : SummaryPlan<TSource, (TFirstResult First, TSecondResult Second, TThirdResult Third)>
{
    private readonly SummaryPlan<TSource, TFirstResult> first;
    private readonly SummaryPlan<TSource, TSecondResult> second;
    private readonly SummaryPlan<TSource, TThirdResult> third;

    internal ZipPlan(
        SummaryPlan<TSource, TFirstResult> first,
        SummaryPlan<TSource, TSecondResult> second,
        SummaryPlan<TSource, TThirdResult> third)
    {
        this.first = first;
        this.second = second;
        this.third = third;
    }

    internal override SummaryEvaluation<TSource, (TFirstResult First, TSecondResult Second, TThirdResult Third)>
        CreateEvaluation() =>
        new Evaluation(
            first.CreateEvaluation(),
            second.CreateEvaluation(),
            third.CreateEvaluation());

    private sealed class Evaluation(
        SummaryEvaluation<TSource, TFirstResult> first,
        SummaryEvaluation<TSource, TSecondResult> second,
        SummaryEvaluation<TSource, TThirdResult> third)
        : SummaryEvaluation<TSource, (TFirstResult First, TSecondResult Second, TThirdResult Third)>
    {
        internal override void Accumulate(TSource item)
        {
            first.Accumulate(item);
            second.Accumulate(item);
            third.Accumulate(item);
        }

        internal override (TFirstResult First, TSecondResult Second, TThirdResult Third) CreateResult() =>
            (first.CreateResult(), second.CreateResult(), third.CreateResult());
    }
}

internal sealed class ZipPlan<TSource, TFirstResult, TSecondResult, TThirdResult, TFourthResult>
    : SummaryPlan<TSource, (TFirstResult First, TSecondResult Second, TThirdResult Third, TFourthResult Fourth)>
{
    private readonly SummaryPlan<TSource, TFirstResult> first;
    private readonly SummaryPlan<TSource, TSecondResult> second;
    private readonly SummaryPlan<TSource, TThirdResult> third;
    private readonly SummaryPlan<TSource, TFourthResult> fourth;

    internal ZipPlan(
        SummaryPlan<TSource, TFirstResult> first,
        SummaryPlan<TSource, TSecondResult> second,
        SummaryPlan<TSource, TThirdResult> third,
        SummaryPlan<TSource, TFourthResult> fourth)
    {
        this.first = first;
        this.second = second;
        this.third = third;
        this.fourth = fourth;
    }

    internal override SummaryEvaluation<TSource, (TFirstResult First, TSecondResult Second, TThirdResult Third, TFourthResult Fourth)>
        CreateEvaluation() =>
        new Evaluation(
            first.CreateEvaluation(),
            second.CreateEvaluation(),
            third.CreateEvaluation(),
            fourth.CreateEvaluation());

    private sealed class Evaluation(
        SummaryEvaluation<TSource, TFirstResult> first,
        SummaryEvaluation<TSource, TSecondResult> second,
        SummaryEvaluation<TSource, TThirdResult> third,
        SummaryEvaluation<TSource, TFourthResult> fourth)
        : SummaryEvaluation<TSource, (TFirstResult First, TSecondResult Second, TThirdResult Third, TFourthResult Fourth)>
    {
        internal override void Accumulate(TSource item)
        {
            first.Accumulate(item);
            second.Accumulate(item);
            third.Accumulate(item);
            fourth.Accumulate(item);
        }

        internal override (TFirstResult First, TSecondResult Second, TThirdResult Third, TFourthResult Fourth) CreateResult() =>
            (first.CreateResult(), second.CreateResult(), third.CreateResult(), fourth.CreateResult());
    }
}
