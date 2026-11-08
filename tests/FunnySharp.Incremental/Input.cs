namespace FunnySharp.Incremental;

/// <summary>
/// A caller-owned graph input. The initial value is published at construction. Values are retained
/// by reference; mutable contents must be republished through <see cref="SetChanged"/> after local
/// mutation.
/// </summary>
public sealed class Input<T> : IncrementalNode<T>
{
    private T _pendingValue = default!;
    private bool _pendingForced;

    internal Input(
        IncrementalGraph graph,
        string name,
        T initial,
        IEqualityComparer<T>? comparer)
        : base(graph, name, comparer)
    {
        Publish(initial);
    }

    internal bool IsPending { get; private set; }

    /// <summary>
    /// The value a pending publication would install, or the current published value when nothing is
    /// staged. Use this to compose several edits to the same input inside one batch; reading
    /// <see cref="IncrementalNode{T}.Value"/> instead would silently drop every earlier staged edit.
    /// </summary>
    public T PendingOrPublishedValue => IsPending ? _pendingValue : Value;

    /// <summary>Publishes a value when the configured input equality finds a difference.</summary>
    public void Set(T value) => Graph.PublishInput(this, value, force: false);

    /// <summary>
    /// Publishes a value unconditionally. Use this for caller-mutated storage whose reference
    /// equality would otherwise hide the change.
    /// </summary>
    public void SetChanged(T value) => Graph.PublishInput(this, value, force: true);

    internal void Stage(T value, bool force)
    {
        _pendingValue = value;
        _pendingForced |= force;
        IsPending = true;
    }

    internal override bool TryPublishPendingInput()
    {
        if (!IsPending)
        {
            return false;
        }

        var value = _pendingValue;
        var force = _pendingForced;
        IsPending = false;
        _pendingValue = default!;
        _pendingForced = false;

        if (!force && Comparer.Equals(CurrentValue, value))
        {
            return false;
        }

        Publish(value);
        return true;
    }

    internal override void CancelPendingInput()
    {
        IsPending = false;
        _pendingValue = default!;
        _pendingForced = false;
    }

    protected override T Compute() => CurrentValue;
}