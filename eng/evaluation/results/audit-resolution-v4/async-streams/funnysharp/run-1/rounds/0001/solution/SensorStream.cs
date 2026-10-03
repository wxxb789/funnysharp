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

        var temperatures = new List<TemperatureValue>();
        var converted = source.Choose(static reading =>
            string.Equals(reading.Kind, "temperature", StringComparison.Ordinal)
                ? Option.Some(new TemperatureValue(
                    reading.SensorId,
                    Math.Round((reading.Value - 32) * 5 / 9, 2)))
                : Option.None<TemperatureValue>());

        // Finish enumeration and source disposal before consulting the reference.
        await foreach (var temperature in converted
            .WithCancellation(cancellationToken)
            .ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();
            temperatures.Add(temperature);
        }

        cancellationToken.ThrowIfCancellationRequested();

        var celsius = temperatures.Select(static value => value.Celsius).ToArray();
        var runningMaxima = celsius
            .Scan(double.NegativeInfinity,
                static (maximum, value) => Math.Max(maximum, value))
            .ToArray();

        var errors = new List<string>();
        foreach (var temperature in temperatures)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var plausible = await reference
                .IsPlausibleAsync(temperature.Celsius, cancellationToken)
                .ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            if (!plausible)
            {
                errors.Add($"implausible:{temperature.SensorId}");
            }
        }

        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<double> validCelsius = errors.Count == 0
            ? Array.AsReadOnly(celsius)
            : Array.Empty<double>();

        return new TemperatureReport(
            temperatures.AsReadOnly(),
            Array.AsReadOnly(runningMaxima),
            validCelsius,
            errors.AsReadOnly());
    }
}
