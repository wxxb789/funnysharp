namespace FunnySharp;

internal sealed class BatchExecution
{
    private readonly BatchScope _scope;
    private readonly BatchScopeOptions _options;
    private readonly CancellationTokenSource _linkedCts;
    private readonly CancellationTokenRegistration _cancellationRegistration;
    private readonly TaskCompletionSource<bool> _completion =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly object _gate = new();
    private readonly Dictionary<object, object> _requestTables =
        new(ReferenceEqualityComparer.Instance);
    private readonly List<BatchRequestTable> _tables = new();
    private readonly Queue<BatchRequestTable> _ready = new();
    private int _pending;
    private int _inFlight;
    private bool _draining;
    private TaskCompletionSource<bool>? _progress;

    internal BatchExecution(BatchScope scope, BatchScopeOptions options, CancellationToken callerToken)
    {
        _scope = scope;
        _options = options;
        _linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            callerToken,
            scope.DisposeToken);
        _cancellationRegistration = _linkedCts.Token.Register(
            static state => ((BatchExecution)state!).OnCancellation(),
            this);
    }

    internal object Gate => _gate;

    internal int PendingCount => _pending;

    internal CancellationToken CancellationToken => _linkedCts.Token;

    internal Task Completion => _completion.Task;

    internal async Task<Result<TValue, BatchReadError<TError>>> RunAsync<TValue, TError>(
        BatchRequirement<TValue, TError> requirement,
        CancellationToken callerToken)
    {
        try
        {
            _linkedCts.Token.ThrowIfCancellationRequested();
            var rootValueTask = requirement.EvaluateAsync(this, _linkedCts.Token);
            Result<TValue, BatchReadError<TError>> result;
            if (rootValueTask.IsCompletedSuccessfully)
            {
                result = rootValueTask.Result;
            }
            else
            {
                var root = rootValueTask.AsTask();
                await DriveAsync(root).ConfigureAwait(false);
                result = await root.ConfigureAwait(false);
            }

            if (_linkedCts.IsCancellationRequested)
            {
                throw new OperationCanceledException(
                    callerToken.IsCancellationRequested ? callerToken : _linkedCts.Token);
            }

            return result;
        }
        catch (OperationCanceledException) when (callerToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(callerToken);
        }
        finally
        {
            _cancellationRegistration.Dispose();
            _linkedCts.Dispose();
            _completion.TrySetResult(true);
            _scope.OnExecutionCompleted(this);
        }
    }

    internal ValueTask<Result<TValue, BatchReadError<TError>>> ReadAsync<TKey, TValue, TError>(
        BatchSource<TKey, TValue, TError> source,
        TKey key,
        CancellationToken cancellationToken)
        where TKey : notnull
    {
        lock (_gate)
        {
            if (_requestTables.TryGetValue(source, out var existing))
            {
                return ((BatchRequestTable<TKey, TValue, TError>)existing).ReadLocked(
                    key,
                    cancellationToken);
            }
        }

        return GetOrCreateRequestTable(source).ReadAsync(key, cancellationToken);
    }

    internal void OnSlotQueued(BatchRequestTable table)
    {
        _pending++;
        if (!table.Ready)
        {
            table.Ready = true;
            _ready.Enqueue(table);
        }

        SignalLocked();
    }

    internal void OnSlotCompleted() => _pending--;

    internal void Signal()
    {
        lock (_gate)
        {
            SignalLocked();
        }
    }

    private void SignalLocked()
    {
        var progress = _progress;
        _progress = null;
        progress?.TrySetResult(true);
    }

    private BatchRequestTable<TKey, TValue, TError> GetOrCreateRequestTable<TKey, TValue, TError>(
        BatchSource<TKey, TValue, TError> source)
        where TKey : notnull
    {
        lock (_gate)
        {
            if (_requestTables.TryGetValue(source, out var existing))
            {
                return (BatchRequestTable<TKey, TValue, TError>)existing;
            }
        }

        // Acquire the scope lock before the execution lock, never while holding the execution
        // lock, so disposal cannot invert the lock order while it cancels the execution.
        var cache = _scope.GetOrCreateCacheTable(source);

        lock (_gate)
        {
            if (_requestTables.TryGetValue(source, out var existing))
            {
                return (BatchRequestTable<TKey, TValue, TError>)existing;
            }

            var table = new BatchRequestTable<TKey, TValue, TError>(
                this,
                source,
                cache,
                _options);
            _requestTables.Add(source, table);
            _tables.Add(table);
            return table;
        }
    }

    private async Task DriveAsync(Task root)
    {
        while (!root.IsCompleted)
        {
            if (TryDispatchAvailable())
            {
                continue;
            }

            await WaitForProgressAsync(root).ConfigureAwait(false);
        }

        await DrainAsync().ConfigureAwait(false);
    }

    private bool TryDispatchAvailable()
    {
        var dispatched = false;

        while (true)
        {
            BatchRequestTable? table;
            BatchWork? work;

            lock (_gate)
            {
                if (!CanDispatchLocked())
                {
                    return dispatched;
                }

                table = _ready.Dequeue();
                if (!table.HasQueued)
                {
                    table.Ready = false;
                    continue;
                }

                work = table.TryTakeBatch(_options.MaxBatchSize);
                if (work is null)
                {
                    table.Ready = false;
                    continue;
                }

                _inFlight++;
                if (table.HasQueued)
                {
                    _ready.Enqueue(table);
                }
                else
                {
                    table.Ready = false;
                }
            }

            dispatched = true;
            _ = CompleteBatchAsync(work.RunAsync(_linkedCts.Token));
        }
    }

    private async Task CompleteBatchAsync(Task work)
    {
        try
        {
            await work.ConfigureAwait(false);
        }
        finally
        {
            lock (_gate)
            {
                _inFlight--;
            }

            Signal();
        }
    }

    private async Task WaitForProgressAsync(Task root)
    {
        Task signal;
        lock (_gate)
        {
            if (root.IsCompleted || CanDispatchLocked())
            {
                return;
            }

            _progress ??= new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            signal = _progress.Task;
        }

        await Task.WhenAny(signal, root).ConfigureAwait(false);
    }

    private bool CanDispatchLocked() =>
        !_draining
        && !_linkedCts.IsCancellationRequested
        && _inFlight < _options.MaxConcurrentBatches
        && _ready.Count > 0;

    private async Task DrainAsync()
    {
        BatchRequestTable[] tables;
        lock (_gate)
        {
            if (_pending == 0 && _inFlight == 0)
            {
                return;
            }

            _draining = true;
            _ready.Clear();
            tables = _tables.ToArray();
        }

        // Queued reads that were never dispatched cannot run once draining begins. Cancelling
        // them is not a caller cancellation: started source batches keep the execution token and
        // are awaited below, so a faulted execution never yanks cancellation from under work it
        // already started.
        for (var index = 0; index < tables.Length; index++)
        {
            tables[index].CancelQueued();
        }

        while (true)
        {
            Task signal;
            lock (_gate)
            {
                if (_inFlight == 0)
                {
                    break;
                }

                _progress ??= new TaskCompletionSource<bool>(
                    TaskCreationOptions.RunContinuationsAsynchronously);
                signal = _progress.Task;
            }

            await signal.ConfigureAwait(false);
        }
    }

    private void OnCancellation()
    {
        BatchRequestTable[] tables;
        lock (_gate)
        {
            tables = _tables.ToArray();
            _ready.Clear();
            for (var index = 0; index < tables.Length; index++)
            {
                tables[index].Ready = false;
            }
        }

        for (var index = 0; index < tables.Length; index++)
        {
            tables[index].CancelQueued();
        }

        Signal();
    }
}
