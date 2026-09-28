namespace FunnySharp.VerticalSlice.Tests;

/// <summary>
/// Reads one line with the test's cancellation token and a bounded wait, so a stalled stream fails the
/// test instead of hanging it.
/// </summary>
internal static class StreamReaderExtensions
{
    internal static async Task<string?> ReadLineBoundedAsync(this StreamReader reader) =>
        await reader.ReadLineAsync(TestContext.Current.CancellationToken)
            .AsTask()
            .WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
}
