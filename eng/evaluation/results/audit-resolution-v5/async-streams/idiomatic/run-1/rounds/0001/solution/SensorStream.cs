using System;
using System.Collections.Generic;
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
        double maximum = 0;

        await foreach (var reading in source.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!string.Equals(reading.Kind, "temperature", StringComparison.Ordinal))
            {
                continue;
            }

            double celsius = Math.Round((reading.Value - 32) * 5 / 9, 2);
            maximum = temperatures.Count == 0 ? celsius : Math.Max(maximum, celsius);
            temperatures.Add(new TemperatureValue(reading.SensorId, celsius));
            runningMaxima.Add(maximum);
        }

        // Enumeration and source disposal finish before reference validation starts.
        cancellationToken.ThrowIfCancellationRequested();
        var validCelsius = new List<double>(temperatures.Count);
        var errors = new List<string>();

        foreach (var temperature in temperatures)
        {
            cancellationToken.ThrowIfCancellationRequested();
            bool plausible = await reference
                .IsPlausibleAsync(temperature.Celsius, cancellationToken)
                .ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            if (plausible)
            {
                validCelsius.Add(temperature.Celsius);
            }
            else
            {
                errors.Add($"implausible:{temperature.SensorId}");
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        return new TemperatureReport(
            temperatures,
            runningMaxima,
            errors.Count == 0 ? validCelsius : Array.Empty<double>(),
            errors);
    }
}
