namespace FunnySharp;

internal abstract class BatchRequestTable
{
    internal bool Ready { get; set; }

    internal abstract bool HasQueued { get; }

    internal abstract BatchWork? TryTakeBatch(int maxBatchSize);

    internal abstract void CancelQueued();
}

internal abstract class BatchWork
{
    internal abstract Task RunAsync(CancellationToken cancellationToken);
}

internal sealed class BatchRequestTable<TKey, TValue, TError> : BatchRequestTable
    where TKey : notnull
{
    private readonly BatchExecution _execution;
    private readonly BatchSource<TKey, TValue, TError> _source;
    private readonly BatchCacheTable<TKey, TValue, TError> _cache;
    private readonly BatchScopeOptions _options;
    private readonly Dictionary<TKey, BatchRequestSlot<TKey, TValue, TError>> _slots;
    private readonly Queue<BatchRequestSlot<TKey, TValue, TError>> _queued = new();

    internal BatchRequestTable(
        BatchExecution execution,
        BatchSource<TKey, TValue, TError> source,
        BatchCacheTable<TKey, TValue, TError> cache,
        BatchScopeOptions options)
    {
        _execution = execution;
        _source = source;
        _cache = cache;
        _options = options;
        _slots = new Dictionary<TKey, BatchRequestSlot<TKey, TValue, TError>>(source.KeyComparer);
    }

    internal BatchSource<TKey, TValue, TError> Source => _source;

    internal override bool HasQueued => _queued.Count > 0;

    internal ValueTask<Result<TValue, BatchReadError<TError>>> ReadAsync(
        TKey key,
        CancellationToken cancellationToken)
    {
        lock (_execution.Gate)
        {
            return ReadLocked(key, cancellationToken);
        }
    }

    internal ValueTask<Result<TValue, BatchReadError<TError>>> ReadLocked(
        TKey key,
        CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return ValueTask.FromCanceled<Result<TValue, BatchReadError<TError>>>(cancellationToken);
        }

        if (_cache.TryGet(key, out var cached))
        {
            return new ValueTask<Result<TValue, BatchReadError<TError>>>(cached);
        }

        if (_slots.TryGetValue(key, out var existing))
        {
            return new ValueTask<Result<TValue, BatchReadError<TError>>>(existing.Completion.Task);
        }

        if (_execution.PendingCount >= _options.MaxPendingRequests)
        {
            throw new InvalidOperationException(
                $"The batch execution exceeded the maximum of {_options.MaxPendingRequests} pending requests.");
        }

        var slot = new BatchRequestSlot<TKey, TValue, TError>(key);
        _slots.Add(key, slot);
        _queued.Enqueue(slot);
        _execution.OnSlotQueued(this);
        return new ValueTask<Result<TValue, BatchReadError<TError>>>(slot.Completion.Task);
    }

    internal IReadOnlyList<Result<TValue, BatchReadError<TError>>> Interpret(
        TKey[] keys,
        Result<IReadOnlyList<BatchSourceEntry<TKey, TValue, TError>>, TError> response)
    {
        var results = new Result<TValue, BatchReadError<TError>>[keys.Length];

        if (response.IsFailure)
        {
            response.TryGetError(out var batchError);
            var failure = Result<TValue, BatchReadError<TError>>.Failure(
                BatchReadError<TError>.BatchFailure(batchError!));
            Array.Fill(results, failure);
            return results;
        }

        if (!response.TryGetValue(out var entries) || entries is null)
        {
            return Unexpected(keys, "the source returned a null entry collection.");
        }

        if (entries.Count > keys.Length)
        {
            return Unexpected(keys, "the source returned more entries than requested.");
        }

        var entriesByKey = new Dictionary<TKey, BatchSourceEntry<TKey, TValue, TError>>(
            keys.Length,
            _source.KeyComparer);
        for (var index = 0; index < keys.Length; index++)
        {
            entriesByKey.Add(keys[index], default);
        }

        for (var index = 0; index < entries.Count; index++)
        {
            var entry = entries[index];
            if (entry.Kind == BatchSourceEntryKind.Unset)
            {
                return Unexpected(keys, "the source returned an uninitialized entry.");
            }

            if (entry.Key is null)
            {
                return Unexpected(keys, "the source returned a null key.");
            }

            if (!entriesByKey.TryGetValue(entry.Key, out var existing))
            {
                return Unexpected(keys, "the source returned a key that was not requested.");
            }

            if (existing.Kind != BatchSourceEntryKind.Unset)
            {
                return Unexpected(keys, "the source returned a key more than once.");
            }

            entriesByKey[entry.Key] = entry;
        }

        for (var index = 0; index < keys.Length; index++)
        {
            var entry = entriesByKey[keys[index]];
            if (entry.Kind == BatchSourceEntryKind.Unset)
            {
                results[index] = Result<TValue, BatchReadError<TError>>.Failure(
                    BatchReadError<TError>.Missing());
            }
            else if (entry.TryGetValue(out var value))
            {
                results[index] = Result<TValue, BatchReadError<TError>>.Success(value);
            }
            else if (entry.TryGetError(out var keyError))
            {
                results[index] = Result<TValue, BatchReadError<TError>>.Failure(
                    BatchReadError<TError>.KeyFailure(keyError));
            }
            else
            {
                return Unexpected(keys, "the source returned an unexpected entry kind.");
            }
        }

        return results;
    }

    internal void Complete(
        IReadOnlyList<BatchRequestSlot<TKey, TValue, TError>> slots,
        IReadOnlyList<Result<TValue, BatchReadError<TError>>> results)
    {
        lock (_execution.Gate)
        {
            for (var index = 0; index < slots.Count; index++)
            {
                var slot = slots[index];
                _cache.Store(slot.Key, results[index]);
                _slots.Remove(slot.Key);
                _execution.OnSlotCompleted();
            }
        }

        for (var index = 0; index < slots.Count; index++)
        {
            slots[index].Completion.TrySetResult(results[index]);
        }

        _execution.Signal();
    }

    internal void Fail(
        IReadOnlyList<BatchRequestSlot<TKey, TValue, TError>> slots,
        Exception exception) =>
        Finish(slots, exception, cancellationToken: null);

    internal void Cancel(
        IReadOnlyList<BatchRequestSlot<TKey, TValue, TError>> slots,
        CancellationToken cancellationToken) =>
        Finish(slots, exception: null, cancellationToken);

    private void Finish(
        IReadOnlyList<BatchRequestSlot<TKey, TValue, TError>> slots,
        Exception? exception,
        CancellationToken? cancellationToken)
    {
        RemoveSlots(slots);
        for (var index = 0; index < slots.Count; index++)
        {
            if (cancellationToken is { } token)
            {
                slots[index].Completion.TrySetCanceled(token);
            }
            else
            {
                slots[index].Completion.TrySetException(exception!);
            }
        }

        _execution.Signal();
    }

    internal override void CancelQueued()
    {
        List<BatchRequestSlot<TKey, TValue, TError>> slots;
        lock (_execution.Gate)
        {
            slots = new List<BatchRequestSlot<TKey, TValue, TError>>(_queued.Count);
            while (_queued.Count > 0)
            {
                var slot = _queued.Dequeue();
                if (_slots.Remove(slot.Key))
                {
                    _execution.OnSlotCompleted();
                    slots.Add(slot);
                }
            }

            Ready = false;
        }

        var token = _execution.CancellationToken;
        for (var index = 0; index < slots.Count; index++)
        {
            slots[index].Completion.TrySetCanceled(token);
        }

        _execution.Signal();
    }

    internal override BatchWork? TryTakeBatch(int maxBatchSize)
    {
        if (_queued.Count == 0)
        {
            return null;
        }

        var count = Math.Min(maxBatchSize, _queued.Count);
        var slots = new BatchRequestSlot<TKey, TValue, TError>[count];
        for (var index = 0; index < count; index++)
        {
            slots[index] = _queued.Dequeue();
        }

        return new BatchWork<TKey, TValue, TError>(this, slots);
    }

    private void RemoveSlots(IReadOnlyList<BatchRequestSlot<TKey, TValue, TError>> slots)
    {
        lock (_execution.Gate)
        {
            for (var index = 0; index < slots.Count; index++)
            {
                if (_slots.Remove(slots[index].Key))
                {
                    _execution.OnSlotCompleted();
                }
            }
        }
    }

    private static IReadOnlyList<Result<TValue, BatchReadError<TError>>> Unexpected(
        TKey[] keys,
        string detail)
    {
        var results = new Result<TValue, BatchReadError<TError>>[keys.Length];
        var failure = Result<TValue, BatchReadError<TError>>.Failure(
            BatchReadError<TError>.UnexpectedResponse(detail));
        Array.Fill(results, failure);
        return results;
    }
}

internal sealed class BatchRequestSlot<TKey, TValue, TError>
    where TKey : notnull
{
    internal BatchRequestSlot(TKey key)
    {
        Key = key;
    }

    internal TKey Key { get; }

    internal TaskCompletionSource<Result<TValue, BatchReadError<TError>>> Completion { get; } =
        new(TaskCreationOptions.None);
}

internal sealed class BatchWork<TKey, TValue, TError>(
    BatchRequestTable<TKey, TValue, TError> table,
    BatchRequestSlot<TKey, TValue, TError>[] slots) : BatchWork
    where TKey : notnull
{
    internal override async Task RunAsync(CancellationToken cancellationToken)
    {
        try
        {
            var keys = new TKey[slots.Length];
            for (var index = 0; index < slots.Length; index++)
            {
                keys[index] = slots[index].Key;
            }

            var response = await table.Source.FetchAsync(keys, cancellationToken).ConfigureAwait(false);
            var results = table.Interpret(keys, response);
            table.Complete(slots, results);
        }
        catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
        {
            table.Cancel(slots, exception.CancellationToken);
        }
        catch (Exception exception)
        {
            table.Fail(slots, exception);
        }
    }
}
