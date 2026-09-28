namespace FunnySharp.VerticalSlice.Infrastructure;

/// <summary>Applies the configured simulated latency, or nothing when a test sets it to zero.</summary>
internal static class SimulatedDelay
{
    internal static ValueTask WaitAsync(TimeSpan latency, CancellationToken cancellationToken) =>
        latency > TimeSpan.Zero
            ? new ValueTask(Task.Delay(latency, cancellationToken))
            : ValueTask.CompletedTask;
}
