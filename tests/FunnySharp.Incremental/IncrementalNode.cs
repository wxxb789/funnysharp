namespace FunnySharp.Incremental;

/// <summary>
/// The non-generic graph node contract. Public only so generic nodes can share one dependent and
/// observer registry; graph construction is exposed through <see cref="IncrementalGraph"/>.
/// </summary>
public abstract class IncrementalNode
{
    private protected IncrementalNode(IncrementalGraph graph, string name)
    {
        Graph = graph;
        Name = name;
    }

    /// <summary>The diagnostic name supplied by the caller.</summary>
    public string Name { get; }

    internal IncrementalGraph Graph { get; }

    internal int Height { get; set; }

    internal int CreationOrder { get; set; }

    internal bool IsDirty { get; set; }

    internal bool HasCurrentValue { get; set; }

    internal List<IncrementalNode> Dependents { get; } = [];

    internal List<ObserverRegistration> Observers { get; } = [];

    internal abstract bool Recompute();

    internal abstract void Initialize();

    internal abstract bool TryPublishPendingInput();

    internal abstract void CancelPendingInput();

    internal void AddDependent(IncrementalNode dependent)
    {
        Dependents.Add(dependent);
        Graph.RecordEdge();
    }
}

/// <summary>A typed value node. Values are retained by the graph until a dependency changes.</summary>
public abstract class IncrementalNode<T> : IncrementalNode
{
    private T _currentValue = default!;

    private protected IncrementalNode(
        IncrementalGraph graph,
        string name,
        IEqualityComparer<T>? comparer)
        : base(graph, name)
    {
        Comparer = comparer ?? EqualityComparer<T>.Default;
    }

    /// <summary>The retained published value.</summary>
    public T Value => _currentValue;

    internal IEqualityComparer<T> Comparer { get; }

    internal T CurrentValue => _currentValue;

    internal override bool Recompute()
    {
        var next = Compute();

        if (HasCurrentValue && Comparer.Equals(_currentValue, next))
        {
            return false;
        }

        _currentValue = next;
        HasCurrentValue = true;
        return true;
    }

    internal override void Initialize() => _ = Recompute();

    internal override bool TryPublishPendingInput() => false;

    internal override void CancelPendingInput()
    {
    }

    internal void Publish(T value)
    {
        _currentValue = value;
        HasCurrentValue = true;
    }

    protected abstract T Compute();
}