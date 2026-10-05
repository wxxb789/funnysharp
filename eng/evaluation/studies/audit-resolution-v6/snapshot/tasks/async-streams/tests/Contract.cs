public sealed record SensorReading(string SensorId, string Kind, double Value);
public sealed record TemperatureValue(string SensorId, double Celsius);
public sealed record TemperatureReport(IReadOnlyList<TemperatureValue> Temperatures,
    IReadOnlyList<double> RunningMaxima, IReadOnlyList<double> ValidCelsius, IReadOnlyList<string> Errors);

public sealed class ReferenceService
{
    public required double MinCelsius { get; init; }
    public required double MaxCelsius { get; init; }
    public List<(double Celsius, CancellationToken Token)> Checks { get; } = [];
    public Action? BeforeCheck { get; init; }
    public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource Canceled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource Exited { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public bool Held { get; init; }

    public async ValueTask<bool> IsPlausibleAsync(double celsius, CancellationToken cancellationToken)
    {
        this.BeforeCheck?.Invoke();
        cancellationToken.ThrowIfCancellationRequested();
        this.Checks.Add((celsius, cancellationToken));
        this.Entered.TrySetResult();
        try
        {
            if (this.Held) await this.Release.Task.WaitAsync(cancellationToken);
            return celsius >= this.MinCelsius && celsius <= this.MaxCelsius;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            this.Canceled.TrySetResult();
            throw;
        }
        finally { this.Exited.TrySetResult(); }
    }
}
