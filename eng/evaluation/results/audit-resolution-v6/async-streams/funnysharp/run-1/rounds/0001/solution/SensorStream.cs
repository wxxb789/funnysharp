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

        // Complete enumeration and disposal before consulting the reference.
        await foreach (var temperature in converted
            .WithCancellation(cancellationToken)
            .ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();
            temperatures.Add(temperature);
        }

        cancellationToken.ThrowIfCancellationRequested();

        var runningMaxima = temperatures
            .Select(static temperature => temperature.Celsius)
            .Scan(double.NegativeInfinity,
                static (maximum, current) => Math.Max(maximum, current))
            .ToArray();

        var validations = new List<Validation<double, string>>(temperatures.Count);
        foreach (var temperature in temperatures)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var plausible = await reference
                .IsPlausibleAsync(temperature.Celsius, cancellationToken)
                .ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();

            validations.Add(plausible
                ? Validation<double, string>.Valid(temperature.Celsius)
                : Validation<double, string>.Invalid(
                    "implausible:" + temperature.SensorId));
        }

        cancellationToken.ThrowIfCancellationRequested();

        // Validation traversal accumulates every error in input order.
        return validations.Sequence().Match(
            values => new TemperatureReport(
                temperatures.AsReadOnly(),
                runningMaxima,
                values,
                Array.Empty<string>()),
            errors => new TemperatureReport(
                temperatures.AsReadOnly(),
                runningMaxima,
                Array.Empty<double>(),
                errors));
    }
}
