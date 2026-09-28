using FunnySharp;

// Temperature-processing pipeline of the monitoring service, in the FunnySharp style:
// Option carries absence (a non-temperature reading), Validation accumulates the reference
// checks, and ordinary exceptions - including cancellation - stay exceptions, never caught.

public static class SensorStream
{
    private const string TemperatureKind = "temperature";

    public static async Task<TemperatureReport> ProcessAsync(
        IAsyncEnumerable<SensorReading> source,
        ReferenceService reference,
        CancellationToken cancellationToken)
    {
        // Stage 1: filter, convert, materialize. Choose fuses the filter and the map: a
        // temperature reading becomes Some(Celsius value), every other kind is None and is
        // skipped without a reference call. ToListAsync forwards the caller's token to the
        // source and enumerates it exactly once, preserving source order.
        List<TemperatureValue> temperatures = await source
            .Choose(ToTemperature)
            .ToListAsync(cancellationToken);

        // Stage 2: running maximum of the materialized Celsius values; this stage never
        // calls the reference service. The seed is never yielded, so a negative first
        // reading is kept as-is, a falling later reading keeps the previous maximum, and
        // an empty stage-1 list yields an empty RunningMaxima.
        List<double> runningMaxima = temperatures
            .Scan(double.NegativeInfinity, (max, temperature) => Math.Max(max, temperature.Celsius))
            .ToList();

        // Stage 3: validation. Every materialized reading is checked exactly once, in
        // source order, forwarding the caller's token unchanged to the reference service.
        // A canceled token surfaces as OperationCanceledException before any reference
        // call, and the first failure does not stop the remaining checks: Validation
        // accumulates every error in source order.
        cancellationToken.ThrowIfCancellationRequested();
        Validation<IReadOnlyList<double>, string> validation = await temperatures
            .ToAsyncEnumerable()
            .TraverseValueAsync(
                temperature => CheckPlausibilityAsync(temperature, reference, cancellationToken),
                cancellationToken);

        // Exactly one of the two batches: every Celsius value when all readings are
        // plausible, or every "implausible:<SensorId>" error when any reading is rejected.
        return validation.Match(
            valid => new TemperatureReport(temperatures, runningMaxima, valid, []),
            errors => new TemperatureReport(temperatures, runningMaxima, [], errors));
    }

    // A temperature reading converts to Celsius (rounded to two decimals); every other
    // kind of reading is absent.
    private static Option<TemperatureValue> ToTemperature(SensorReading reading) =>
        reading.Kind == TemperatureKind
            ? Option.Some(new TemperatureValue(reading.SensorId, Math.Round((reading.Value - 32) * 5 / 9, 2)))
            : Option<TemperatureValue>.None;

    // One reference check: a plausible reading keeps its Celsius value, an implausible
    // one records the sensor id.
    private static async ValueTask<Validation<double, string>> CheckPlausibilityAsync(
        TemperatureValue temperature,
        ReferenceService reference,
        CancellationToken cancellationToken)
    {
        bool plausible = await reference.IsPlausibleAsync(temperature.Celsius, cancellationToken);
        return plausible
            ? Validation<double, string>.Valid(temperature.Celsius)
            : Validation<double, string>.Invalid($"implausible:{temperature.SensorId}");
    }
}
