using System.Threading.Tasks.Sources;

namespace FunnySharp.Tests;

internal sealed class CountingValueTaskSource<T> : IValueTaskSource<T>
{
    private ManualResetValueTaskSourceCore<T> source;

    public CountingValueTaskSource()
    {
        source.RunContinuationsAsynchronously = true;
    }

    public CountingValueTaskSource(T result) : this()
    {
        source.SetResult(result);
    }

    public int GetResultCount { get; private set; }

    public ValueTask<T> CreateValueTask() => new(this, source.Version);

    public void SetResult(T result) => source.SetResult(result);

    public T GetResult(short token)
    {
        GetResultCount++;
        return source.GetResult(token);
    }

    public ValueTaskSourceStatus GetStatus(short token) => source.GetStatus(token);

    public void OnCompleted(
        Action<object?> continuation,
        object? state,
        short token,
        ValueTaskSourceOnCompletedFlags flags) =>
        source.OnCompleted(continuation, state, token, flags);
}
