namespace FunnySharp;

/// <summary>
/// An explicit execution scope for typed batch read requirements.
/// </summary>
/// <remarks>
/// A scope owns the only result cache and all admission bounds. Create one per request or unit of
/// work; it is never an ambient cross-request cache. The scope executes one requirement at a time,
/// groups independent reads from the same <see cref="BatchSource{TKey, TValue, TError}"/> by key,
/// and dispatches source batches up to <see cref="BatchScopeOptions.MaxBatchSize"/> and
/// <see cref="BatchScopeOptions.MaxConcurrentBatches"/>.
/// <para>
/// Resolved key results, including missing and failed outcomes, are retained for the scope lifetime
/// when <see cref="BatchScopeOptions.CacheResults"/> is enabled. Call <see cref="Invalidate{TKey, TValue, TError}(BatchSource{TKey, TValue, TError}, TKey)"/>
/// after a mutation, or <see cref="ClearCache"/> to drop every retained result. Disposing the
/// scope clears the cache. Cancellation and disposal request cooperative cancellation and wait for
/// started source batches to finish; they never claim to forcibly terminate an arbitrary Task.
/// </para>
/// </remarks>
public sealed class BatchScope : IAsyncDisposable
{
    private readonly object _gate = new();
    private readonly BatchScopeOptions _options;
    private readonly Dictionary<object, BatchCacheTable> _cacheTables =
        new(ReferenceEqualityComparer.Instance);
    private readonly CancellationTokenSource _disposeCts = new();
    private readonly TaskCompletionSource<bool> _disposeCompletion =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private BatchExecution? _activeExecution;
    private bool _disposed;

    private BatchScope(BatchScopeOptions options)
    {
        _options = options;
    }

    /// <summary>Creates a scope with explicit bounds and cache policy.</summary>
    /// <param name="options">The bounds and cache policy; defaults are used when null.</param>
    /// <exception cref="ArgumentOutOfRangeException">An option is outside its supported range.</exception>
    public static BatchScope Create(BatchScopeOptions? options = null)
    {
        options ??= new BatchScopeOptions();
        options.Validate();
        return new BatchScope(options);
    }

    /// <summary>Gets the bounds and cache policy this scope was created with.</summary>
    public BatchScopeOptions Options => _options;

    internal CancellationToken DisposeToken => _disposeCts.Token;

    /// <summary>Executes one requirement and returns its typed outcome.</summary>
    /// <typeparam name="TValue">The successful value type.</typeparam>
    /// <typeparam name="TError">The source's typed failure value.</typeparam>
    /// <param name="requirement">The requirement to execute.</param>
    /// <param name="cancellationToken">The caller cancellation token.</param>
    /// <returns>The requirement outcome. Source exceptions and cancellation are not converted into
    /// <see cref="BatchReadError{TError}"/> values.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="requirement"/> is null.</exception>
    /// <exception cref="InvalidOperationException">The scope already has an active execution.</exception>
    /// <exception cref="ObjectDisposedException">The scope has been disposed.</exception>
    /// <exception cref="OperationCanceledException">The caller token or scope disposal canceled the
    /// execution after started source batches were drained.</exception>
    /// <remarks>
    /// Only one execution may run in a scope. The token is forwarded through a linked source that
    /// also observes scope disposal. A canceled source batch still returns only after its delegate
    /// completes or observes cancellation; no arbitrary Task is terminated forcibly.
    /// </remarks>
    public ValueTask<Result<TValue, BatchReadError<TError>>> ExecuteAsync<TValue, TError>(
        BatchRequirement<TValue, TError> requirement,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requirement);

        BatchExecution execution;
        lock (_gate)
        {
            ThrowIfDisposed();
            if (_activeExecution is not null)
            {
                throw new InvalidOperationException(
                    "The batch scope already has an active execution. Use a separate scope for a concurrent request.");
            }

            execution = new BatchExecution(this, _options, cancellationToken);
            _activeExecution = execution;
        }

        return new ValueTask<Result<TValue, BatchReadError<TError>>>(
            execution.RunAsync(requirement, cancellationToken));
    }

    /// <summary>Removes one cached key result for a source.</summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The successful value type.</typeparam>
    /// <typeparam name="TError">The source's typed failure value.</typeparam>
    /// <param name="source">The source whose key is invalidated.</param>
    /// <param name="key">The invalidated key.</param>
    /// <returns>The number of removed cache entries (zero or one).</returns>
    /// <exception cref="InvalidOperationException">An execution is active.</exception>
    /// <exception cref="ObjectDisposedException">The scope has been disposed.</exception>
    public int Invalidate<TKey, TValue, TError>(
        BatchSource<TKey, TValue, TError> source,
        TKey key)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(key);

        lock (_gate)
        {
            ThrowIfDisposed();
            ThrowIfExecuting();
            return _cacheTables.TryGetValue(source, out var table)
                ? table.Invalidate(key)
                : 0;
        }
    }

    /// <summary>Removes every cached key result for one source.</summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The successful value type.</typeparam>
    /// <typeparam name="TError">The source's typed failure value.</typeparam>
    /// <param name="source">The source whose cache is cleared.</param>
    /// <returns>The number of removed cache entries.</returns>
    /// <exception cref="InvalidOperationException">An execution is active.</exception>
    /// <exception cref="ObjectDisposedException">The scope has been disposed.</exception>
    public int Invalidate<TKey, TValue, TError>(BatchSource<TKey, TValue, TError> source)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(source);

        lock (_gate)
        {
            ThrowIfDisposed();
            ThrowIfExecuting();
            return _cacheTables.TryGetValue(source, out var table) ? table.Clear() : 0;
        }
    }

    /// <summary>Removes every cached key result in the scope.</summary>
    /// <returns>The number of removed cache entries.</returns>
    /// <exception cref="InvalidOperationException">An execution is active.</exception>
    /// <exception cref="ObjectDisposedException">The scope has been disposed.</exception>
    public int ClearCache()
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            ThrowIfExecuting();

            var count = 0;
            foreach (var table in _cacheTables.Values)
            {
                count += table.Clear();
            }

            return count;
        }
    }

    /// <summary>Stops new work, waits for started source batches, and clears the cache.</summary>
    /// <returns>A task that completes after started work is accounted for.</returns>
    /// <remarks>Disposal is idempotent. It requests cancellation but cannot terminate a source
    /// delegate that ignores its token; disposal waits for that work to finish.</remarks>
    public ValueTask DisposeAsync()
    {
        var start = false;
        lock (_gate)
        {
            if (!_disposed)
            {
                _disposed = true;
                start = true;
            }
        }

        if (start)
        {
            _ = DisposeCoreAsync();
        }

        return new ValueTask(_disposeCompletion.Task);
    }

    internal BatchCacheTable<TKey, TValue, TError> GetOrCreateCacheTable<TKey, TValue, TError>(
        BatchSource<TKey, TValue, TError> source)
        where TKey : notnull
    {
        lock (_gate)
        {
            if (_cacheTables.TryGetValue(source, out var existing))
            {
                return (BatchCacheTable<TKey, TValue, TError>)existing;
            }

            var table = new BatchCacheTable<TKey, TValue, TError>(
                source.KeyComparer,
                _options.MaxCachedEntries,
                _options.CacheResults);
            _cacheTables.Add(source, table);
            return table;
        }
    }

    internal void OnExecutionCompleted(BatchExecution execution)
    {
        lock (_gate)
        {
            if (ReferenceEquals(_activeExecution, execution))
            {
                _activeExecution = null;
            }
        }
    }

    private async Task DisposeCoreAsync()
    {
        try
        {
            BatchExecution? execution;
            lock (_gate)
            {
                execution = _activeExecution;
            }

            _disposeCts.Cancel();

            if (execution is not null)
            {
                await execution.Completion.ConfigureAwait(false);
            }

            lock (_gate)
            {
                _cacheTables.Clear();
            }

            _disposeCts.Dispose();
            _disposeCompletion.TrySetResult(true);
        }
        catch (Exception exception)
        {
            _disposeCompletion.TrySetException(exception);
        }
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);

    private void ThrowIfExecuting()
    {
        if (_activeExecution is not null)
        {
            throw new InvalidOperationException(
                "The batch scope cache cannot be invalidated during an active execution.");
        }
    }
}
