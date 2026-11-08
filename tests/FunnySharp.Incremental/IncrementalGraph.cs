namespace FunnySharp.Incremental;

/// <summary>
/// A caller-owned, single-threaded incremental graph. Inputs publish values through an explicit
/// batch boundary; statically declared nodes retain values and recompute only after a changed
/// dependency survives equality cutoff.
/// </summary>
public sealed class IncrementalGraph
{
    private readonly List<IncrementalNode> _nodes = [];
    private readonly PriorityQueue<IncrementalNode, int> _queue = new();
    private readonly List<IncrementalNode> _pendingInputs = [];
    private readonly List<IncrementalNode> _changedNodes = [];
    private readonly List<ObserverRegistration> _notifications = [];
    private int _batchDepth;
    private bool _isCommitting;
    private bool _isCallingObservers;
    private int _nextObserverOrder;
    private int _edgeCount;
    private int _observerCount;
    private long _commits;
    private long _inputChanges;
    private long _nodeEvaluations;
    private long _cutoffStops;
    private long _changedNodesCount;
    private long _observerNotifications;

    /// <summary>The number of committed input-change publications.</summary>
    public long Version { get; private set; }

    /// <summary>The number of registered nodes, including inputs.</summary>
    public int NodeCount => _nodes.Count;

    /// <summary>The number of declared dependency edges.</summary>
    public int EdgeCount => _edgeCount;

    /// <summary>The number of currently active observer registrations.</summary>
    public int ObserverCount => _observerCount;

    /// <summary>Returns cumulative commit statistics.</summary>
    public IncrementalGraphStatistics Statistics => new(
        _commits,
        _inputChanges,
        _nodeEvaluations,
        _cutoffStops,
        _changedNodesCount,
        _observerNotifications,
        _nodes.Count,
        _edgeCount,
        _observerCount);

    /// <summary>Resets cumulative commit statistics. Node and edge counts are unchanged.</summary>
    public void ResetStatistics()
    {
        _commits = 0;
        _inputChanges = 0;
        _nodeEvaluations = 0;
        _cutoffStops = 0;
        _changedNodesCount = 0;
        _observerNotifications = 0;
    }

    /// <summary>Creates a root input with the supplied initial published value.</summary>
    public Input<T> Input<T>(string name, T initial, IEqualityComparer<T>? comparer = null)
    {
        ThrowIfCallbackActive("create a node");
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var node = new Input<T>(this, name, initial, comparer);
        Register(node, height: 0);
        node.Initialize();
        return node;
    }

    /// <summary>Creates a static one-source computation.</summary>
    public IncrementalNode<TResult> Map<TInput, TResult>(
        string name,
        IncrementalNode<TInput> source,
        Pure<TInput, TResult> compute,
        IEqualityComparer<TResult>? comparer = null)
    {
        ThrowIfCallbackActive("create a node");
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ValidateSource(source, nameof(source));

        if (!compute.IsInitialized)
        {
            throw new ArgumentException(
                "The pure computation must be created with Pure<TInput, TResult>.Of.",
                nameof(compute));
        }

        var node = new MapNode<TInput, TResult>(this, name, source, compute, comparer);
        Register(node, source.Height + 1);
        source.AddDependent(node);
        node.Initialize();
        return node;
    }

    /// <summary>Creates a static two-source computation.</summary>
    public IncrementalNode<TResult> Combine<TFirst, TSecond, TResult>(
        string name,
        IncrementalNode<TFirst> first,
        IncrementalNode<TSecond> second,
        Pure<(TFirst First, TSecond Second), TResult> compute,
        IEqualityComparer<TResult>? comparer = null)
    {
        ThrowIfCallbackActive("create a node");
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ValidateSource(first, nameof(first));
        ValidateSource(second, nameof(second));

        if (!compute.IsInitialized)
        {
            throw new ArgumentException(
                "The pure computation must be created with Pure<(TFirst First, TSecond Second), TResult>.Of.",
                nameof(compute));
        }

        var height = Math.Max(first.Height, second.Height) + 1;
        var node = new CombineNode<TFirst, TSecond, TResult>(
            this,
            name,
            first,
            second,
            compute,
            comparer);

        Register(node, height);
        first.AddDependent(node);
        second.AddDependent(node);
        node.Initialize();
        return node;
    }

    /// <summary>
    /// Creates a static fan-in node that snapshots the values of the supplied homogeneous sources.
    /// The returned list is read-only; the node is a value dependency, not a collection delta.
    /// </summary>
    public IncrementalNode<IReadOnlyList<T>> Collect<T>(
        string name,
        IReadOnlyList<IncrementalNode<T>> sources)
    {
        ThrowIfCallbackActive("create a node");
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(sources);

        if (sources.Count == 0)
        {
            throw new ArgumentException("Collect requires at least one source.", nameof(sources));
        }

        var snapshot = new IncrementalNode<T>[sources.Count];
        var height = 0;

        for (var index = 0; index < sources.Count; index++)
        {
            var source = sources[index];
            ValidateSource(source, nameof(sources));
            snapshot[index] = source;
            height = Math.Max(height, source.Height);
        }

        var node = new CollectNode<T>(this, name, snapshot);
        Register(node, height + 1);

        foreach (var source in snapshot)
        {
            source.AddDependent(node);
        }

        node.Initialize();
        return node;
    }

    /// <summary>
    /// Registers an observer and invokes it immediately with the current value. The observer is a
    /// side-effecting callback, unlike a <see cref="Pure{TInput, TResult}"/> computation.
    /// </summary>
    public IDisposable Observe<T>(IncrementalNode<T> node, Action<T> observer) =>
        RegisterObserver(node, observer, invokeImmediately: true);

    /// <summary>Registers an observer without an initial invocation.</summary>
    public IDisposable ObserveChanges<T>(IncrementalNode<T> node, Action<T> observer) =>
        RegisterObserver(node, observer, invokeImmediately: false);

    /// <summary>
    /// Stages input publications and commits them only when the outermost action returns
    /// successfully. A throwing action discards every staged input value and runs no observer.
    /// </summary>
    public void Batch(Action action)
    {
        ThrowIfCallbackActive("start a batch");
        ArgumentNullException.ThrowIfNull(action);

        _batchDepth++;

        try
        {
            action();
        }
        catch
        {
            if (--_batchDepth == 0)
            {
                DiscardPendingInputs();
            }

            throw;
        }

        if (--_batchDepth == 0)
        {
            CommitPending();
        }
    }

    internal void PublishInput<T>(Input<T> input, T value, bool force)
    {
        ThrowIfCallbackActive("publish an input");
        input.Stage(value, force);

        if (input.IsPending && !_pendingInputs.Contains(input))
        {
            _pendingInputs.Add(input);
        }

        if (_batchDepth == 0)
        {
            CommitPending();
        }
    }

    internal void RecordEdge() => _edgeCount++;

    internal void RecordObserverRemoved() => _observerCount--;

    private IDisposable RegisterObserver<T>(
        IncrementalNode<T> node,
        Action<T> observer,
        bool invokeImmediately)
    {
        ThrowIfCallbackActive("register an observer");
        ValidateSource(node, nameof(node));
        ArgumentNullException.ThrowIfNull(observer);

        var registration = new ObserverRegistration<T>(node, observer)
        {
            RegistrationOrder = _nextObserverOrder++,
        };

        node.Observers.Add(registration);
        _observerCount++;

        if (!invokeImmediately)
        {
            return registration;
        }

        _isCallingObservers = true;

        try
        {
            observer(node.Value);
        }
        catch
        {
            registration.Dispose();
            throw;
        }
        finally
        {
            _isCallingObservers = false;
        }

        return registration;
    }

    private void CommitPending()
    {
        if (_pendingInputs.Count == 0)
        {
            return;
        }

        _isCommitting = true;

        try
        {
            _changedNodes.Clear();
            var changedInputs = 0;

            foreach (var node in _pendingInputs)
            {
                if (node.TryPublishPendingInput())
                {
                    _changedNodes.Add(node);
                    changedInputs++;
                }
            }

            _pendingInputs.Clear();

            if (changedInputs == 0)
            {
                return;
            }

            _commits++;
            _inputChanges += changedInputs;
            Version++;

            foreach (var node in _changedNodes)
            {
                EnqueueDependents(node);
            }

            while (_queue.TryDequeue(out var node, out _))
            {
                if (!node.IsDirty)
                {
                    continue;
                }

                node.IsDirty = false;
                var changed = node.Recompute();
                _nodeEvaluations++;

                if (changed)
                {
                    _changedNodes.Add(node);
                    EnqueueDependents(node);
                }
                else
                {
                    _cutoffStops++;
                }
            }

            NotifyObservers();
        }
        finally
        {
            _isCommitting = false;
            _queue.Clear();
            _pendingInputs.Clear();
            _changedNodes.Clear();
            _notifications.Clear();
        }
    }

    private void EnqueueDependents(IncrementalNode node)
    {
        foreach (var dependent in node.Dependents)
        {
            if (dependent.IsDirty)
            {
                continue;
            }

            dependent.IsDirty = true;
            _queue.Enqueue(dependent, dependent.Height);
        }
    }

    private void NotifyObservers()
    {
        _notifications.Clear();
        _changedNodes.Sort(static (left, right) =>
            left.CreationOrder.CompareTo(right.CreationOrder));

        foreach (var node in _changedNodes)
        {
            foreach (var registration in node.Observers)
            {
                if (registration.IsActive)
                {
                    _notifications.Add(registration);
                }
            }
        }

        _changedNodesCount += _changedNodes.Count;
        _isCallingObservers = true;

        try
        {
            foreach (var registration in _notifications)
            {
                if (!registration.IsActive)
                {
                    continue;
                }

                registration.Invoke();
                _observerNotifications++;
            }
        }
        finally
        {
            _isCallingObservers = false;
            _changedNodes.Clear();
            _notifications.Clear();
        }
    }

    private void DiscardPendingInputs()
    {
        foreach (var node in _pendingInputs)
        {
            node.CancelPendingInput();
        }

        _pendingInputs.Clear();
    }

    private void ValidateSource(IncrementalNode? source, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(source, parameterName);

        if (!ReferenceEquals(source.Graph, this))
        {
            throw new ArgumentException("The source node belongs to another graph.", parameterName);
        }
    }

    private void Register(IncrementalNode node, int height)
    {
        node.CreationOrder = _nodes.Count;
        node.Height = height;
        _nodes.Add(node);
    }

    private void ThrowIfCallbackActive(string operation)
    {
        if (_isCommitting || _isCallingObservers)
        {
            throw new InvalidOperationException(
                $"Cannot {operation} while a graph commit or observer callback is running.");
        }
    }
}