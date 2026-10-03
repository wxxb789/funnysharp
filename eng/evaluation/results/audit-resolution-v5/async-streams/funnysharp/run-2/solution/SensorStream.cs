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

        // Finish enumeration and source disposal before consulting the reference.
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

        // Scan yields only inclusive maxima, not its seed.
        var runningMaxima = temperatures
            .Select(static temperature => temperature.Celsius)
            .Scan(double.NegativeInfinity,
                static (maximum, celsius) => Math.Max(maximum, celsius))
            .ToArray();

        var errors = new List<string>();
        foreach (var temperature in temperatures)
        {
            cancellationToken.ThrowIfCancellationRequested();
            bool plausible = await reference
                .IsPlausibleAsync(temperature.Celsius, cancellationToken)
                .ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

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
            runningMaxima,
            validCelsius,
            errors.AsReadOnly());
    }
}
