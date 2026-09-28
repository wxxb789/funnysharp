// Temperature-processing pipeline behind the SensorStream seam: filter and convert the raw
// sensor readings, track the running maximum, then validate every temperature against the
// reference service.

public static class SensorStream
{
    public static async Task<TemperatureReport> ProcessAsync(
        IAsyncEnumerable<SensorReading> source,
        ReferenceService reference,
        CancellationToken cancellationToken)
    {
        // Stage 1 - filter, convert, materialize: keep only temperature readings, converted to
        // rounded Celsius, in source order. The source is enumerated exactly once, with the
        // caller's cancellation token forwarded to it.
        var temperatures = new List<TemperatureValue>();
        await foreach (var reading in source.WithCancellation(cancellationToken))
        {
            if (reading.Kind != "temperature")
            {
                continue;
            }

            var celsius = Math.Round((reading.Value - 32) * 5 / 9, 2);
            temperatures.Add(new TemperatureValue(reading.SensorId, celsius));
        }

        // Stage 2 - running maximum: element i is the largest Celsius of the first i + 1
        // materialized readings, so a falling reading keeps the previous maximum. This stage
        // never calls the reference service.
        var runningMaxima = new List<double>(temperatures.Count);
        var maximum = double.MinValue;
        foreach (var temperature in temperatures)
        {
            maximum = Math.Max(maximum, temperature.Celsius);
            runningMaxima.Add(maximum);
        }

        // Stage 3 - validation: check every materialized reading in source order, forwarding the
        // caller's token unchanged; the first failure does not stop the remaining checks.
        var errors = new List<string>();
        foreach (var temperature in temperatures)
        {
            if (!await reference.IsPlausibleAsync(temperature.Celsius, cancellationToken))
            {
                errors.Add($"implausible:{temperature.SensorId}");
            }
        }

        // Exactly one batch is reported: all-plausible runs list every Celsius value, while any
        // rejection empties the valid batch and reports the errors instead.
        var validCelsius = errors.Count == 0
            ? temperatures.Select(temperature => temperature.Celsius).ToArray()
            : Array.Empty<double>();

        return new TemperatureReport(temperatures, runningMaxima, validCelsius, errors);
    }
}
