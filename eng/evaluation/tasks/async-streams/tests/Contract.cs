// Style-neutral contract shared by both evaluation variants: the data types, the deterministic
// async reference fake, and the neutral report record the solution must produce. No FunnySharp
// types appear here on purpose: both styles end at the same seam.
public sealed record SensorReading(string SensorId, string Kind, double Value);

public sealed record TemperatureValue(string SensorId, double Celsius);

public sealed record TemperatureReport(
    IReadOnlyList<TemperatureValue> Temperatures,
    IReadOnlyList<double> RunningMaxima,
    IReadOnlyList<double> ValidCelsius,
    IReadOnlyList<string> Errors);

public sealed class ReferenceService
{
    public required double MinCelsius { get; init; }

    public required double MaxCelsius { get; init; }

    public List<(double Celsius, CancellationToken Token)> Checks { get; } = [];

    public async ValueTask<bool> IsPlausibleAsync(double celsius, CancellationToken cancellationToken)
    {
        Checks.Add((celsius, cancellationToken));
        cancellationToken.ThrowIfCancellationRequested();
        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();
        return celsius >= MinCelsius && celsius <= MaxCelsius;
    }
}
