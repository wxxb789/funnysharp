using FunnySharp;

// Temperature-processing pipeline behind the SensorStream.ProcessAsync seam. The three stages run
// in order and each one owns its part of the report: stage 1 filters and converts the sensor stream
// into the materialized Temperatures, stage 2 tracks the RunningMaxima, and stage 3 validates every
// reading against the reference service to decide between ValidCelsius and Errors.

public static class SensorStream
{
    private const string TemperatureKind = "temperature";

    public static async Task<TemperatureReport> ProcessAsync(
        IAsyncEnumerable<SensorReading> source,
        ReferenceService reference,
        CancellationToken cancellationToken)
    {
        // A canceled token surfaces as OperationCanceledException before any reference call.
        cancellationToken.ThrowIfCancellationRequested();

        // Stage 1 - filter, convert, materialize: only temperature readings, converted to Celsius.
        IReadOnlyList<TemperatureValue> temperatures =
            await MaterializeTemperaturesAsync(source, cancellationToken);

        // Stage 2 - running maximum: RunningMaxima[i] is the maximum of the first i + 1 readings.
        IReadOnlyList<double> runningMaxima = ComputeRunningMaxima(temperatures);

        // Stage 3 - validation: every reading is checked; any implausible reading rejects the batch.
        Validation<IReadOnlyList<double>, string> batch =
            await ValidateTemperaturesAsync(temperatures, reference, cancellationToken);

        return batch.Match(
            validCelsius => new TemperatureReport(temperatures, runningMaxima, validCelsius, []),
            errors => new TemperatureReport(temperatures, runningMaxima, [], errors));
    }

    // Enumerates the source exactly once with the caller's token. A non-temperature reading is
    // absence, so Choose skips it without a reference call; a kept reading becomes a TemperatureValue.
    private static async Task<IReadOnlyList<TemperatureValue>> MaterializeTemperaturesAsync(
        IAsyncEnumerable<SensorReading> source,
        CancellationToken cancellationToken) =>
        await source
            .Choose(ToTemperature)
            .ToListAsync(cancellationToken);

    private static Option<TemperatureValue> ToTemperature(SensorReading reading) =>
        reading.Kind == TemperatureKind
            ? Option<TemperatureValue>.Some(new TemperatureValue(reading.SensorId, ToCelsius(reading.Value)))
            : Option<TemperatureValue>.None;

    private static double ToCelsius(double fahrenheit) =>
        Math.Round((fahrenheit - 32) * 5 / 9, 2);

    // The running aggregate never calls the reference service; an empty input yields no maxima.
    private static IReadOnlyList<double> ComputeRunningMaxima(IReadOnlyList<TemperatureValue> temperatures) =>
        temperatures
            .Select(temperature => temperature.Celsius)
            .Scan(double.MinValue, (maximum, celsius) => Math.Max(maximum, celsius))
            .ToList();

    // Calls the reference service exactly once per materialized reading, in source order, with the
    // caller's token unchanged. Each check is a Validation, so the first failure does not stop the
    // remaining checks; Sequence accumulates every error in source order or collects the whole batch.
    private static async Task<Validation<IReadOnlyList<double>, string>> ValidateTemperaturesAsync(
        IReadOnlyList<TemperatureValue> temperatures,
        ReferenceService reference,
        CancellationToken cancellationToken)
    {
        var checks = new List<Validation<double, string>>(temperatures.Count);
        foreach (var temperature in temperatures)
        {
            var plausible = await reference.IsPlausibleAsync(temperature.Celsius, cancellationToken);
            checks.Add(plausible
                ? Validation<double, string>.Valid(temperature.Celsius)
                : Validation<double, string>.Invalid($"implausible:{temperature.SensorId}"));
        }

        return checks.Sequence();
    }
}
