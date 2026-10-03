using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public static class SensorStream
{
    public static async Task<TemperatureReport> ProcessAsync(
        IAsyncEnumerable<SensorReading> source,
        ReferenceService reference,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(reference);
        cancellationToken.ThrowIfCancellationRequested();

        var temperatures = new List<TemperatureValue>();
        var runningMaxima = new List<double>();

        await foreach (var reading in source.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!string.Equals(reading.Kind, "temperature", StringComparison.Ordinal))
            {
                continue;
            }

            double celsius = Math.Round((reading.Value - 32) * 5 / 9, 2);
            double maximum = runningMaxima.Count == 0
                ? celsius
                : Math.Max(runningMaxima[^1], celsius);

            temperatures.Add(new TemperatureValue(reading.SensorId, celsius));
            runningMaxima.Add(maximum);
        }

        // Enumeration and source disposal finish before validation begins.
        cancellationToken.ThrowIfCancellationRequested();
        var errors = new List<string>();
        foreach (var temperature in temperatures)
        {
            cancellationToken.ThrowIfCancellationRequested();
            bool plausible = await reference.IsPlausibleAsync(
                temperature.Celsius, cancellationToken).ConfigureAwait(false);

            if (!plausible)
            {
                errors.Add($"implausible:{temperature.SensorId}");
            }
        }

        double[] validCelsius = errors.Count == 0
            ? temperatures.Select(temperature => temperature.Celsius).ToArray()
            : Array.Empty<double>();

        cancellationToken.ThrowIfCancellationRequested();
        return new TemperatureReport(temperatures, runningMaxima, validCelsius, errors);
    }
}
