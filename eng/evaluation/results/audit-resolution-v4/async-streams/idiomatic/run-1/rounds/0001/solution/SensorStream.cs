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

        await foreach (var reading in source.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.Equals(reading.Kind, "temperature", StringComparison.Ordinal))
            {
                double celsius = Math.Round((reading.Value - 32) * 5 / 9, 2);
                temperatures.Add(new TemperatureValue(reading.SensorId, celsius));
            }
        }

        // Enumeration and disposal finish before any reference calls begin.
        cancellationToken.ThrowIfCancellationRequested();

        var values = new double[temperatures.Count];
        var runningMaxima = new double[temperatures.Count];
        var errors = new List<string>();
        double maximum = 0;

        for (int i = 0; i < temperatures.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var temperature = temperatures[i];
            values[i] = temperature.Celsius;
            maximum = i == 0
                ? temperature.Celsius
                : Math.Max(maximum, temperature.Celsius);
            runningMaxima[i] = maximum;

            bool plausible = await reference
                .IsPlausibleAsync(temperature.Celsius, cancellationToken)
                .ConfigureAwait(false);

            if (!plausible)
            {
                errors.Add($"implausible:{temperature.SensorId}");
            }
        }

        cancellationToken.ThrowIfCancellationRequested();

        return new TemperatureReport(
            temperatures.ToArray(),
            runningMaxima,
            errors.Count == 0 ? values : Array.Empty<double>(),
            errors.ToArray());
    }
}
