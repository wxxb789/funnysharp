namespace FunnySharp.Incremental;

internal abstract class ObserverRegistration : IDisposable
{
    private protected ObserverRegistration(IncrementalNode node)
    {
        Node = node;
    }

    internal IncrementalNode Node { get; }

    internal int RegistrationOrder { get; set; }

    internal abstract bool IsActive { get; }

    internal abstract void Invoke();

    public abstract void Dispose();
}

internal sealed class ObserverRegistration<T> : ObserverRegistration
{
    private readonly Action<T> _observer;
    private bool _active = true;

    internal ObserverRegistration(IncrementalNode<T> node, Action<T> observer)
        : base(node)
    {
        _observer = observer;
    }

    internal override bool IsActive => _active;

    internal override void Invoke() => _observer(((IncrementalNode<T>)Node).Value);

    public override void Dispose()
    {
        if (!_active)
        {
            return;
        }

        _active = false;
        Node.Observers.Remove(this);
        Node.Graph.RecordObserverRemoved();
    }
}