// Temperature-processing pipeline for the sensor monitoring service.
//
// ProcessAsync runs three explicit stages, in order, and each stage fills its own part
// of the report: filtering and conversion produce Temperatures, the running aggregate
// produces RunningMaxima, and validation produces ValidCelsius / Errors.

public static class SensorStream
{
    public static async Task<TemperatureReport> ProcessAsync(
        IAsyncEnumerable<SensorReading> source,
        ReferenceService reference,
        CancellationToken cancellationToken)
    {
        // A canceled token surfaces before the source is enumerated or any check runs.
        cancellationToken.ThrowIfCancellationRequested();

        // Stage 1: keep only "temperature" readings, converted to Celsius.
        var temperatures = await CollectTemperaturesAsync(source, cancellationToken);

        // Stage 2: the highest Celsius value among the readings so far.
        var runningMaxima = ComputeRunningMaxima(temperatures);

        // Stage 3: check every materialized reading against the reference service.
        var (validCelsius, errors) = await ValidateTemperaturesAsync(temperatures, reference, cancellationToken);

        return new TemperatureReport(temperatures, runningMaxima, validCelsius, errors);
    }

    // Stage 1: enumerates the source exactly once, with the caller's token forwarded to
    // it, and materializes the temperature readings as Celsius values.
    private static async Task<List<TemperatureValue>> CollectTemperaturesAsync(
        IAsyncEnumerable<SensorReading> source,
        CancellationToken cancellationToken)
    {
        var temperatures = new List<TemperatureValue>();

        await foreach (var reading in source.WithCancellation(cancellationToken))
        {
            if (reading.Kind != "temperature")
            {
                continue; // Other kinds are skipped without a reference call.
            }

            var celsius = Math.Round((reading.Value - 32) * 5 / 9, 2);
            temperatures.Add(new TemperatureValue(reading.SensorId, celsius));
        }

        return temperatures;
    }

    // Stage 2: RunningMaxima[i] is the maximum Celsius value of the first i + 1
    // readings; an empty input yields an empty output.
    private static List<double> ComputeRunningMaxima(IReadOnlyList<TemperatureValue> temperatures)
    {
        var runningMaxima = new List<double>(temperatures.Count);
        var highest = double.MinValue;

        foreach (var temperature in temperatures)
        {
            highest = Math.Max(highest, temperature.Celsius);
            runningMaxima.Add(highest);
        }

        return runningMaxima;
    }

    // Stage 3: checks every reading in source order - the first failure does not stop
    // the remaining checks - and reports either the whole valid batch or every failure.
    private static async Task<(List<double> ValidCelsius, List<string> Errors)> ValidateTemperaturesAsync(
        IReadOnlyList<TemperatureValue> temperatures,
        ReferenceService reference,
        CancellationToken cancellationToken)
    {
        // A token canceled during materialization surfaces before any check runs.
        cancellationToken.ThrowIfCancellationRequested();

        var errors = new List<string>();

        foreach (var temperature in temperatures)
        {
            var plausible = await reference.IsPlausibleAsync(temperature.Celsius, cancellationToken);
            if (!plausible)
            {
                errors.Add($"implausible:{temperature.SensorId}");
            }
        }

        // Any implausible reading empties the valid batch: the report carries either
        // every Celsius value or every error, never a mix.
        List<double> validCelsius = [];
        if (errors.Count == 0)
        {
            validCelsius.AddRange(temperatures.Select(temperature => temperature.Celsius));
        }

        return (validCelsius, errors);
    }
}
