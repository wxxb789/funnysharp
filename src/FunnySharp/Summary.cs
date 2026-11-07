using System.Numerics;

namespace FunnySharp;

/// <summary>
/// Provides reusable, independently defined sequence summaries and the factories that compose
/// them into one evaluation.
/// </summary>
/// <remarks>
/// <para>
/// A summary is a description, not an outcome carrier. A leaf summary holds a seed or seed factory,
/// an accumulate delegate, and a result delegate; a <c>Zip</c>/<c>MapResult</c> summary is composed
/// of component descriptions and has no top-level seed or accumulate delegate. Evaluating any
/// summary creates execution state local to that call. Its only input is a BCL
/// <see cref="IEnumerable{T}"/> or <see cref="IAsyncEnumerable{T}"/>, and its only output is the
/// caller-selected result type.
/// </para>
/// <para>
/// Summaries are total folds. Every component observes every source item, so composing a
/// component whose result is already determined cannot stop a source another component still
/// needs. A component that must stop the source belongs to <c>FirstOrNone</c>,
/// <c>AggregateResult</c>, or ordinary control flow instead.
/// </para>
/// <para>
/// Composition is runtime delegate composition. It adds no typeclass or interface hierarchy, no
/// automatic parallelism, no LINQ query surface, and no compile-time fusion.
/// </para>
/// </remarks>
public static class Summary
{
    /// <summary>
    /// Creates a summary whose result is its final state, initialized from a seed value.
    /// </summary>
    /// <typeparam name="TSource">The source item type.</typeparam>
    /// <typeparam name="TState">The execution state type.</typeparam>
    /// <param name="seed">
    /// The initial state. It is not validated. The exact value, including a null reference, is
    /// assigned to each evaluation's local state and is never cloned.
    /// </param>
    /// <param name="accumulate">
    /// The function that combines the current state with one source item and returns the next
    /// state.
    /// </param>
    /// <returns>A summary that returns its final state.</returns>
    /// <remarks>
    /// A mutable reference seed is shared by every evaluation of this definition and by every
    /// component that uses the same seed value. Use
    /// <see cref="Create{TSource, TState}(Func{TState}, Func{TState, TSource, TState})"/> when each
    /// evaluation or component needs its own mutable state.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="accumulate"/> is <see langword="null"/>.</exception>
    public static Summary<TSource, TState, TState> Create<TSource, TState>(
        TState seed,
        Func<TState, TSource, TState> accumulate) =>
        Create(() => seed, accumulate);

    /// <summary>
    /// Creates a summary whose result is its final state, initialized from a caller-supplied
    /// factory.
    /// </summary>
    /// <typeparam name="TSource">The source item type.</typeparam>
    /// <typeparam name="TState">The execution state type.</typeparam>
    /// <param name="seedFactory">
    /// The factory invoked once for each component at the start of each evaluation. Its result is
    /// the component's local state.
    /// </param>
    /// <param name="accumulate">
    /// The function that combines the current state with one source item and returns the next
    /// state.
    /// </param>
    /// <returns>A summary that returns its final state.</returns>
    /// <remarks>
    /// The factory is the isolation form for mutable state: two evaluations, and two components in
    /// the same composition, receive separate factory results. The factory result is not cloned or
    /// validated.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="seedFactory"/> or <paramref name="accumulate"/> is <see langword="null"/>.
    /// </exception>
    public static Summary<TSource, TState, TState> Create<TSource, TState>(
        Func<TState> seedFactory,
        Func<TState, TSource, TState> accumulate)
    {
        ArgumentNullException.ThrowIfNull(accumulate);

        return Create(seedFactory, accumulate, static state => state);
    }

    /// <summary>
    /// Creates a summary that projects its final state into a result, initialized from a seed
    /// value.
    /// </summary>
    /// <typeparam name="TSource">The source item type.</typeparam>
    /// <typeparam name="TState">The execution state type.</typeparam>
    /// <typeparam name="TResult">The summary result type.</typeparam>
    /// <param name="seed">
    /// The initial state. It is not validated. The exact value, including a null reference, is
    /// assigned to each evaluation's local state and is never cloned.
    /// </param>
    /// <param name="accumulate">
    /// The function that combines the current state with one source item and returns the next
    /// state.
    /// </param>
    /// <param name="result">
    /// The function that projects the final state. It runs once after traversal, or immediately for
    /// an empty source.
    /// </param>
    /// <returns>A summary that projects its final state into <typeparamref name="TResult"/>.</returns>
    /// <remarks>
    /// A mutable reference seed is shared by every evaluation of this definition and by every
    /// component that uses the same seed value. Use the factory overload when each evaluation or
    /// component needs its own mutable state.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="accumulate"/> or <paramref name="result"/> is <see langword="null"/>.
    /// </exception>
    public static Summary<TSource, TState, TResult> Create<TSource, TState, TResult>(
        TState seed,
        Func<TState, TSource, TState> accumulate,
        Func<TState, TResult> result) =>
        Create(() => seed, accumulate, result);

    /// <summary>
    /// Creates a summary that projects its final state into a result, initialized from a
    /// caller-supplied factory.
    /// </summary>
    /// <typeparam name="TSource">The source item type.</typeparam>
    /// <typeparam name="TState">The execution state type.</typeparam>
    /// <typeparam name="TResult">The summary result type.</typeparam>
    /// <param name="seedFactory">
    /// The factory invoked once for each component at the start of each evaluation. Its result is
    /// the component's local state.
    /// </param>
    /// <param name="accumulate">
    /// The function that combines the current state with one source item and returns the next
    /// state.
    /// </param>
    /// <param name="result">
    /// The function that projects the final state. It runs once after traversal, or immediately for
    /// an empty source.
    /// </param>
    /// <returns>A summary that projects its final state into <typeparamref name="TResult"/>.</returns>
    /// <remarks>
    /// The factory is the isolation form for mutable state: two evaluations, and two components in
    /// the same composition, receive separate factory results. The factory result is not cloned or
    /// validated.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="seedFactory"/>, <paramref name="accumulate"/>, or
    /// <paramref name="result"/> is <see langword="null"/>.
    /// </exception>
    public static Summary<TSource, TState, TResult> Create<TSource, TState, TResult>(
        Func<TState> seedFactory,
        Func<TState, TSource, TState> accumulate,
        Func<TState, TResult> result)
    {
        ArgumentNullException.ThrowIfNull(seedFactory);
        ArgumentNullException.ThrowIfNull(accumulate);
        ArgumentNullException.ThrowIfNull(result);

        return new Summary<TSource, TState, TResult>(
            new LeafPlan<TSource, TState, TResult>(seedFactory, accumulate, result));
    }

    /// <summary>
    /// Combines two independent summaries into one evaluation with a flat pair state and result.
    /// </summary>
    /// <typeparam name="TSource">The source item type.</typeparam>
    /// <typeparam name="TFirstState">The first summary's state type.</typeparam>
    /// <typeparam name="TFirstResult">The first summary's result type.</typeparam>
    /// <typeparam name="TSecondState">The second summary's state type.</typeparam>
    /// <typeparam name="TSecondResult">The second summary's result type.</typeparam>
    /// <param name="first">The first summary.</param>
    /// <param name="second">The second summary.</param>
    /// <returns>
    /// A summary whose state is a pair of the component states and whose result is a pair of the
    /// component results in operand order.
    /// </returns>
    /// <remarks>
    /// Each component initializes its own state, so the composition never merges unrelated
    /// accumulators into one hand-written accumulator. Components initialize, accumulate, and
    /// finalize in operand order. A component exception stops the later components for that item
    /// and the final result projection; no component is skipped because another component's result
    /// is already determined.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="first"/> or <paramref name="second"/> is <see langword="null"/>.
    /// </exception>
    public static Summary<TSource, (TFirstState First, TSecondState Second), (TFirstResult First, TSecondResult Second)>
        Zip<TSource, TFirstState, TFirstResult, TSecondState, TSecondResult>(
            Summary<TSource, TFirstState, TFirstResult> first,
            Summary<TSource, TSecondState, TSecondResult> second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        return new Summary<TSource, (TFirstState First, TSecondState Second), (TFirstResult First, TSecondResult Second)>(
            new ZipPlan<TSource, TFirstResult, TSecondResult>(first.Plan, second.Plan));
    }

    /// <summary>
    /// Combines three independent summaries into one evaluation with a flat triple state and
    /// result.
    /// </summary>
    /// <typeparam name="TSource">The source item type.</typeparam>
    /// <typeparam name="TFirstState">The first summary's state type.</typeparam>
    /// <typeparam name="TFirstResult">The first summary's result type.</typeparam>
    /// <typeparam name="TSecondState">The second summary's state type.</typeparam>
    /// <typeparam name="TSecondResult">The second summary's result type.</typeparam>
    /// <typeparam name="TThirdState">The third summary's state type.</typeparam>
    /// <typeparam name="TThirdResult">The third summary's result type.</typeparam>
    /// <param name="first">The first summary.</param>
    /// <param name="second">The second summary.</param>
    /// <param name="third">The third summary.</param>
    /// <returns>
    /// A summary whose state and result are flat triples in operand order.
    /// </returns>
    /// <remarks>
    /// Each component initializes its own state, so the composition never merges unrelated
    /// accumulators into one hand-written accumulator. Components initialize, accumulate, and
    /// finalize in operand order. A component exception stops the later components for that item
    /// and the final result projection; no component is skipped because another component's result
    /// is already determined.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="first"/>, <paramref name="second"/>, or <paramref name="third"/> is
    /// <see langword="null"/>.
    /// </exception>
    public static Summary<TSource, (TFirstState First, TSecondState Second, TThirdState Third), (TFirstResult First, TSecondResult Second, TThirdResult Third)>
        Zip<TSource, TFirstState, TFirstResult, TSecondState, TSecondResult, TThirdState, TThirdResult>(
            Summary<TSource, TFirstState, TFirstResult> first,
            Summary<TSource, TSecondState, TSecondResult> second,
            Summary<TSource, TThirdState, TThirdResult> third)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);
        ArgumentNullException.ThrowIfNull(third);

        return new Summary<TSource, (TFirstState First, TSecondState Second, TThirdState Third), (TFirstResult First, TSecondResult Second, TThirdResult Third)>(
            new ZipPlan<TSource, TFirstResult, TSecondResult, TThirdResult>(
                first.Plan,
                second.Plan,
                third.Plan));
    }

    /// <summary>
    /// Combines four independent summaries into one evaluation with a flat quadruple state and
    /// result.
    /// </summary>
    /// <typeparam name="TSource">The source item type.</typeparam>
    /// <typeparam name="TFirstState">The first summary's state type.</typeparam>
    /// <typeparam name="TFirstResult">The first summary's result type.</typeparam>
    /// <typeparam name="TSecondState">The second summary's state type.</typeparam>
    /// <typeparam name="TSecondResult">The second summary's result type.</typeparam>
    /// <typeparam name="TThirdState">The third summary's state type.</typeparam>
    /// <typeparam name="TThirdResult">The third summary's result type.</typeparam>
    /// <typeparam name="TFourthState">The fourth summary's state type.</typeparam>
    /// <typeparam name="TFourthResult">The fourth summary's result type.</typeparam>
    /// <param name="first">The first summary.</param>
    /// <param name="second">The second summary.</param>
    /// <param name="third">The third summary.</param>
    /// <param name="fourth">The fourth summary.</param>
    /// <returns>
    /// A summary whose state and result are flat quadruples in operand order.
    /// </returns>
    /// <remarks>
    /// Each component initializes its own state, so the composition never merges unrelated
    /// accumulators into one hand-written accumulator. Components initialize, accumulate, and
    /// finalize in operand order. A component exception stops the later components for that item
    /// and the final result projection; no component is skipped because another component's result
    /// is already determined.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="first"/>, <paramref name="second"/>, <paramref name="third"/>, or
    /// <paramref name="fourth"/> is <see langword="null"/>.
    /// </exception>
    public static Summary<TSource, (TFirstState First, TSecondState Second, TThirdState Third, TFourthState Fourth), (TFirstResult First, TSecondResult Second, TThirdResult Third, TFourthResult Fourth)>
        Zip<TSource, TFirstState, TFirstResult, TSecondState, TSecondResult, TThirdState, TThirdResult, TFourthState, TFourthResult>(
            Summary<TSource, TFirstState, TFirstResult> first,
            Summary<TSource, TSecondState, TSecondResult> second,
            Summary<TSource, TThirdState, TThirdResult> third,
            Summary<TSource, TFourthState, TFourthResult> fourth)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);
        ArgumentNullException.ThrowIfNull(third);
        ArgumentNullException.ThrowIfNull(fourth);

        return new Summary<TSource, (TFirstState First, TSecondState Second, TThirdState Third, TFourthState Fourth), (TFirstResult First, TSecondResult Second, TThirdResult Third, TFourthResult Fourth)>(
            new ZipPlan<TSource, TFirstResult, TSecondResult, TThirdResult, TFourthResult>(
                first.Plan,
                second.Plan,
                third.Plan,
                fourth.Plan));
    }
}

/// <summary>
/// Provides inference-friendly built-in summary factories for one source type.
/// </summary>
/// <typeparam name="TSource">The source item type.</typeparam>
/// <remarks>
/// These factories encode the repository's edge semantics for their result family: checked count
/// and sum overflow, source-ordered addition, empty extrema as <see cref="Option{T}"/>, null-key
/// skipping, and first-key retention on comparer ties. Any other summary remains an ordinary
/// <see cref="Summary.Create{TSource, TState}(TState, Func{TState, TSource, TState})"/> call.
/// </remarks>
public static class Summary<TSource>
{
    /// <summary>
    /// Creates a summary that counts source items in a checked <see cref="int"/>.
    /// </summary>
    /// <returns>A summary whose result is the item count, or <c>0</c> for an empty source.</returns>
    /// <remarks>
    /// The count is incremented with checked arithmetic, so a source with more than
    /// <see cref="int.MaxValue"/> reached items throws <see cref="OverflowException"/> during
    /// evaluation. The source is enumerated once and no intermediate collection is created.
    /// </remarks>
    public static Summary<TSource, int, int> Count() =>
        new(new CountPlan<TSource>());

    /// <summary>
    /// Creates a summary that sums a selected numeric value with checked, source-ordered addition.
    /// </summary>
    /// <typeparam name="TNumber">The numeric value type.</typeparam>
    /// <param name="selector">The function that selects one numeric value from each source item.</param>
    /// <returns>
    /// A summary whose result is the sum, or <typeparamref name="TNumber"/>.Zero for an empty
    /// source.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The selector runs once per reached item in source order, and the addition uses
    /// <c>checked</c> arithmetic. Integral overflow throws <see cref="OverflowException"/> during
    /// evaluation and is never converted to a result value. Floating-point and
    /// <see cref="decimal"/> addition keep their own machine semantics; values are added strictly in
    /// source order and are never reassociated or evaluated in parallel.
    /// </para>
    /// <para>
    /// The accumulation step is <c>checked(total + selector(item))</c>. A selector that throws
    /// stops the evaluation and propagates its exception unchanged.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="selector"/> is <see langword="null"/>.</exception>
    public static Summary<TSource, TNumber, TNumber> Sum<TNumber>(
        Func<TSource, TNumber> selector)
        where TNumber : INumber<TNumber>
    {
        ArgumentNullException.ThrowIfNull(selector);

        return new Summary<TSource, TNumber, TNumber>(
            new SumPlan<TSource, TNumber>(selector));
    }

    /// <summary>
    /// Creates a summary that finds the smallest selected key under the default comparer.
    /// </summary>
    /// <typeparam name="TKey">The selected key type.</typeparam>
    /// <param name="selector">The function that selects one key from each source item.</param>
    /// <returns>
    /// A summary whose result contains the smallest key, or <c>None</c> when the source is empty or
    /// every selected key is null.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The selector runs once per reached item in source order. Null keys are skipped before the
    /// comparer is invoked, so the comparer never observes a null key. When two keys compare equal,
    /// the first encountered key is retained.
    /// </para>
    /// <para>
    /// The source is enumerated once. A selector or comparer exception propagates unchanged.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="selector"/> is <see langword="null"/>.</exception>
    public static Summary<TSource, Option<TKey>, Option<TKey>> Min<TKey>(
        Func<TSource, TKey> selector)
        where TKey : IComparable<TKey> =>
        Min(selector, Comparer<TKey>.Default);

    /// <summary>
    /// Creates a summary that finds the smallest selected key under a caller-supplied comparer.
    /// </summary>
    /// <typeparam name="TKey">The selected key type.</typeparam>
    /// <param name="selector">The function that selects one key from each source item.</param>
    /// <param name="comparer">The comparer that orders the keys.</param>
    /// <returns>
    /// A summary whose result contains the smallest key, or <c>None</c> when the source is empty or
    /// every selected key is null.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The selector runs once per reached item in source order. Null keys are skipped before the
    /// comparer is invoked, so the comparer never observes a null key. When two keys compare equal,
    /// the first encountered key is retained.
    /// </para>
    /// <para>
    /// The source is enumerated once. A selector or comparer exception propagates unchanged.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="selector"/> or <paramref name="comparer"/> is <see langword="null"/>.
    /// </exception>
    public static Summary<TSource, Option<TKey>, Option<TKey>> Min<TKey>(
        Func<TSource, TKey> selector,
        IComparer<TKey> comparer)
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(comparer);

        return new Summary<TSource, Option<TKey>, Option<TKey>>(
            new ExtremumPlan<TSource, TKey>(selector, comparer, selectLargest: false));
    }

    /// <summary>
    /// Creates a summary that finds the largest selected key under the default comparer.
    /// </summary>
    /// <typeparam name="TKey">The selected key type.</typeparam>
    /// <param name="selector">The function that selects one key from each source item.</param>
    /// <returns>
    /// A summary whose result contains the largest key, or <c>None</c> when the source is empty or
    /// every selected key is null.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The selector runs once per reached item in source order. Null keys are skipped before the
    /// comparer is invoked, so the comparer never observes a null key. When two keys compare equal,
    /// the first encountered key is retained.
    /// </para>
    /// <para>
    /// The source is enumerated once. A selector or comparer exception propagates unchanged.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="selector"/> is <see langword="null"/>.</exception>
    public static Summary<TSource, Option<TKey>, Option<TKey>> Max<TKey>(
        Func<TSource, TKey> selector)
        where TKey : IComparable<TKey> =>
        Max(selector, Comparer<TKey>.Default);

    /// <summary>
    /// Creates a summary that finds the largest selected key under a caller-supplied comparer.
    /// </summary>
    /// <typeparam name="TKey">The selected key type.</typeparam>
    /// <param name="selector">The function that selects one key from each source item.</param>
    /// <param name="comparer">The comparer that orders the keys.</param>
    /// <returns>
    /// A summary whose result contains the largest key, or <c>None</c> when the source is empty or
    /// every selected key is null.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The selector runs once per reached item in source order. Null keys are skipped before the
    /// comparer is invoked, so the comparer never observes a null key. When two keys compare equal,
    /// the first encountered key is retained.
    /// </para>
    /// <para>
    /// The source is enumerated once. A selector or comparer exception propagates unchanged.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="selector"/> or <paramref name="comparer"/> is <see langword="null"/>.
    /// </exception>
    public static Summary<TSource, Option<TKey>, Option<TKey>> Max<TKey>(
        Func<TSource, TKey> selector,
        IComparer<TKey> comparer)
    {
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(comparer);

        return new Summary<TSource, Option<TKey>, Option<TKey>>(
            new ExtremumPlan<TSource, TKey>(selector, comparer, selectLargest: true));
    }
}

/// <summary>
/// Describes a reusable sequence summary with explicit execution state and result types.
/// </summary>
/// <typeparam name="TSource">The source item type.</typeparam>
/// <typeparam name="TState">The execution state type.</typeparam>
/// <typeparam name="TResult">The summary result type.</typeparam>
/// <remarks>
/// <para>
/// A leaf summary holds only delegates. A <c>Zip</c>/<c>MapResult</c> summary holds component plans;
/// evaluating it creates each component's state rather than one top-level accumulator. In either
/// shape, evaluation uses state local to that call, invokes each component's accumulate logic once
/// per reached item in source order, and invokes the result projection once after traversal.
/// Reusing a summary never reuses accumulator state unless the caller's seed value or seed factory
/// explicitly shares an object.
/// </para>
/// <para>
/// The definition is safe to reuse across evaluations and across synchronous and asynchronous
/// sources. Concurrent evaluations are isolated only when the seed factory returns fresh state for
/// each evaluation; the library adds no locks, cloning, or snapshotting.
/// </para>
/// </remarks>
public sealed class Summary<TSource, TState, TResult>
{
    private readonly SummaryPlan<TSource, TResult> plan;

    internal Summary(SummaryPlan<TSource, TResult> plan)
    {
        this.plan = plan;
    }

    internal SummaryPlan<TSource, TResult> Plan => plan;

    /// <summary>
    /// Projects the final result through a caller-supplied selector.
    /// </summary>
    /// <typeparam name="TResultNext">The projected result type.</typeparam>
    /// <param name="selector">
    /// The function that transforms the final result. It runs once after traversal, or immediately
    /// for an empty source.
    /// </param>
    /// <returns>A summary with the same state and accumulation but a projected result.</returns>
    /// <remarks>
    /// The selector is not an accumulation step and does not observe source items. A component
    /// accumulate exception prevents the result and this selector from running; a selector
    /// exception propagates unchanged.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="selector"/> is <see langword="null"/>.</exception>
    public Summary<TSource, TState, TResultNext> MapResult<TResultNext>(
        Func<TResult, TResultNext> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        return new Summary<TSource, TState, TResultNext>(
            new MappedPlan<TSource, TResult, TResultNext>(plan, selector));
    }

    /// <summary>
    /// Evaluates the summary over a synchronous sequence.
    /// </summary>
    /// <param name="source">The sequence to summarize.</param>
    /// <returns>The summary result.</returns>
    /// <remarks>
    /// <para>
    /// The operation is eager and enumerates <paramref name="source"/> exactly once in source order.
    /// It does not materialize, buffer, cache, or defer an intermediate sequence. Each reached item
    /// updates the state before the next source item is requested.
    /// </para>
    /// <para>
    /// Empty input invokes each component's seed factory once and its result selector once, with
    /// no accumulate call. A source, seed-factory, accumulate, or result exception propagates
    /// unchanged; the acquired enumerator is disposed exactly once. Normal <c>foreach</c> precedence applies: an
    /// exception from <see cref="IDisposable.Dispose"/> can replace a pending outcome.
    /// </para>
    /// <para>
    /// A component whose result is already determined still observes every later item. There is no
    /// early-stop or skip protocol.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    public TResult Evaluate(IEnumerable<TSource> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return plan.Evaluate(source);
    }

    /// <summary>
    /// Evaluates the same summary over an asynchronous sequence.
    /// </summary>
    /// <param name="source">The asynchronous sequence to summarize.</param>
    /// <param name="cancellationToken">
    /// The token forwarded unchanged to the asynchronous enumerator. It is not inspected eagerly.
    /// </param>
    /// <returns>A value task that produces the summary result.</returns>
    /// <remarks>
    /// <para>
    /// The summary calculation remains synchronous; only input acquisition is asynchronous. The
    /// operations process one item at a time, never prefetch, never run items in parallel, and never
    /// block. Each source item updates the state before the next <c>MoveNextAsync</c>.
    /// </para>
    /// <para>
    /// Empty input invokes each component's seed factory once and its result selector once, with
    /// no accumulate call. Seed-factory, source, and accumulate exceptions keep normal
    /// asynchronous behavior and are never converted to a result value; a seed-factory exception
    /// faults the returned operation before enumeration. The exact caller token is forwarded to
    /// <c>GetAsyncEnumerator</c>; cancellation remains cancellation. The enumerator is disposed with
    /// <c>DisposeAsync</c> on every exit; a disposal exception can replace a pending outcome.
    /// </para>
    /// <para>
    /// The operation begins enumerating up to the first incomplete await when called, like an
    /// ordinary <c>async</c> method. A component whose result is already determined still observes
    /// every later item; there is no early-stop or skip protocol.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    public ValueTask<TResult> EvaluateAsync(
        IAsyncEnumerable<TSource> source,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);

        return plan.EvaluateAsync(source, cancellationToken);
    }
}
