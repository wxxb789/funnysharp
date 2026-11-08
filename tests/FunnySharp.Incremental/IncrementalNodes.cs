namespace FunnySharp.Incremental;

internal sealed class MapNode<TInput, TResult> : IncrementalNode<TResult>
{
    private readonly IncrementalNode<TInput> _source;
    private readonly Pure<TInput, TResult> _compute;

    internal MapNode(
        IncrementalGraph graph,
        string name,
        IncrementalNode<TInput> source,
        Pure<TInput, TResult> compute,
        IEqualityComparer<TResult>? comparer)
        : base(graph, name, comparer)
    {
        _source = source;
        _compute = compute;
    }

    protected override TResult Compute() => _compute.Invoke(_source.Value);
}

internal sealed class CombineNode<TFirst, TSecond, TResult> : IncrementalNode<TResult>
{
    private readonly IncrementalNode<TFirst> _first;
    private readonly IncrementalNode<TSecond> _second;
    private readonly Pure<(TFirst First, TSecond Second), TResult> _compute;

    internal CombineNode(
        IncrementalGraph graph,
        string name,
        IncrementalNode<TFirst> first,
        IncrementalNode<TSecond> second,
        Pure<(TFirst First, TSecond Second), TResult> compute,
        IEqualityComparer<TResult>? comparer)
        : base(graph, name, comparer)
    {
        _first = first;
        _second = second;
        _compute = compute;
    }

    protected override TResult Compute() =>
        _compute.Invoke((_first.Value, _second.Value));
}

internal sealed class CollectNode<T> : IncrementalNode<IReadOnlyList<T>>
{
    private readonly IncrementalNode<T>[] _sources;

    internal CollectNode(
        IncrementalGraph graph,
        string name,
        IncrementalNode<T>[] sources)
        : base(graph, name, comparer: null)
    {
        _sources = sources;
    }

    protected override IReadOnlyList<T> Compute()
    {
        var values = new T[_sources.Length];

        for (var index = 0; index < _sources.Length; index++)
        {
            values[index] = _sources[index].Value;
        }

        return Array.AsReadOnly(values);
    }
}