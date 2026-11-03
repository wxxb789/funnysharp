using FunnySharp;

namespace FunnySharp.Tests;

internal sealed class RecordingBatchSource<TKey, TValue, TError>
    where TKey : notnull
{
    private readonly object _gate = new();
    private readonly List<IReadOnlyList<TKey>> _batches = new();
    private readonly Func<
        IReadOnlyList<TKey>,
        int,
        CancellationToken,
        ValueTask<Result<IReadOnlyList<BatchSourceEntry<TKey, TValue, TError>>, TError>>> _respond;
    private int _invocations;

    internal RecordingBatchSource(
        string name,
        string partition,
        IEqualityComparer<TKey> keyComparer,
        Func<
            IReadOnlyList<TKey>,
            int,
            CancellationToken,
            ValueTask<Result<IReadOnlyList<BatchSourceEntry<TKey, TValue, TError>>, TError>>> respond)
    {
        _respond = respond;
        Source = new BatchSource<TKey, TValue, TError>(name, partition, keyComparer, FetchAsync);
    }

    internal BatchSource<TKey, TValue, TError> Source { get; }

    internal int InvocationCount
    {
        get
        {
            lock (_gate)
            {
                return _invocations;
            }
        }
    }

    internal IReadOnlyList<IReadOnlyList<TKey>> Batches
    {
        get
        {
            lock (_gate)
            {
                return _batches.ToArray();
            }
        }
    }

    private ValueTask<Result<IReadOnlyList<BatchSourceEntry<TKey, TValue, TError>>, TError>> FetchAsync(
        IReadOnlyList<TKey> keys,
        CancellationToken cancellationToken)
    {
        var snapshot = keys.ToArray();
        int invocation;
        lock (_gate)
        {
            _batches.Add(snapshot);
            invocation = ++_invocations;
        }

        return _respond(snapshot, invocation, cancellationToken);
    }
}
