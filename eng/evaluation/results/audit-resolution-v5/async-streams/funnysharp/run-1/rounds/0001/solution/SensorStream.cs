using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FunnySharp;

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

        // Finish enumeration and disposal before making any reference calls.
        var temperatures = await source
            .Choose(static reading =>
                string.Equals(reading.Kind, "temperature", StringComparison.Ordinal)
                    ? Option.Some(new TemperatureValue(
                        reading.SensorId,
                        Math.Round((reading.Value - 32) * 5 / 9, 2)))
                    : Option.None<TemperatureValue>())
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();

        var runningMaxima = temperatures
            .Select(static temperature => temperature.Celsius)
            .Scan(double.NegativeInfinity,
                static (maximum, value) => Math.Max(maximum, value))
            .ToArray();

        var errors = new List<string>();
        foreach (var temperature in temperatures)
        {
            cancellationToken.ThrowIfCancellationRequested();
            bool plausible = await reference
                .IsPlausibleAsync(temperature.Celsius, cancellationToken)
                .ConfigureAwait(false);

            if (!plausible)
            {
                errors.Add($"implausible:{temperature.SensorId}");
            }
        }

        cancellationToken.ThrowIfCancellationRequested();

        var validCelsius = errors.Count == 0
            ? temperatures.Select(static temperature => temperature.Celsius).ToArray()
            : Array.Empty<double>();

        return new TemperatureReport(
            temperatures.AsReadOnly(),
            Array.AsReadOnly(runningMaxima),
            Array.AsReadOnly(validCelsius),
            errors.AsReadOnly());
    }
}
