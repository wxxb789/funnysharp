using System.Threading.Tasks.Sources;

namespace FunnySharp.AspNetCore.Tests;

internal sealed class CountingValueTaskSource<T> : IValueTaskSource<T>
{
    private ManualResetValueTaskSourceCore<T> source;

    internal CountingValueTaskSource(T result)
    {
        source.RunContinuationsAsynchronously = true;
        source.SetResult(result);
    }

    internal int GetResultCount { get; private set; }

    internal ValueTask<T> CreateValueTask() => new(this, source.Version);

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
