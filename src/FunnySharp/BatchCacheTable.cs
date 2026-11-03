namespace FunnySharp;

internal abstract class BatchCacheTable
{
    internal abstract int Clear();

    internal abstract int Invalidate(object key);
}

internal sealed class BatchCacheTable<TKey, TValue, TError> : BatchCacheTable
    where TKey : notnull
{
    private sealed class Entry(Result<TValue, BatchReadError<TError>> result, LinkedListNode<TKey> node)
    {
        internal Result<TValue, BatchReadError<TError>> Result { get; set; } = result;

        internal LinkedListNode<TKey> Node { get; } = node;
    }

    private readonly Dictionary<TKey, Entry> _entries;
    private readonly LinkedList<TKey> _order = new();
    private readonly int _capacity;

    internal BatchCacheTable(IEqualityComparer<TKey> comparer, int capacity, bool enabled)
    {
        _entries = new Dictionary<TKey, Entry>(comparer);
        _capacity = enabled ? capacity : 0;
    }

    internal bool TryGet(TKey key, out Result<TValue, BatchReadError<TError>> result)
    {
        if (_entries.TryGetValue(key, out var entry))
        {
            result = entry.Result;
            return true;
        }

        result = default;
        return false;
    }

    internal void Store(TKey key, Result<TValue, BatchReadError<TError>> result)
    {
        if (_capacity == 0)
        {
            return;
        }

        if (_entries.TryGetValue(key, out var existing))
        {
            existing.Result = result;
            return;
        }

        var node = _order.AddLast(key);
        _entries.Add(key, new Entry(result, node));

        if (_entries.Count > _capacity)
        {
            var oldest = _order.First!;
            _order.RemoveFirst();
            _entries.Remove(oldest.Value);
        }
    }

    internal override int Clear()
    {
        var count = _entries.Count;
        _entries.Clear();
        _order.Clear();
        return count;
    }

    internal override int Invalidate(object key)
    {
        if (key is not TKey typed || !_entries.Remove(typed, out var entry))
        {
            return 0;
        }

        _order.Remove(entry.Node);
        return 1;
    }
}
