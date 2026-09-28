using System.Runtime.CompilerServices;

namespace FunnySharp.VerticalSlice.Application;

/// <summary>
/// Bridges an in-memory list into an asynchronous stream so the bounded-parallel operators can be
/// applied to it. FunnySharp deliberately ships no synchronous-to-asynchronous adapter, because the
/// conversion is the caller's statement about where asynchronous work begins.
/// </summary>
internal static class AsyncEnumerableBridge
{
    internal static async IAsyncEnumerable<T> AsAsyncEnumerable<T>(
        this IEnumerable<T> source,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var item in source)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return item;
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }
}
