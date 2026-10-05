public static class SensorStream
{
    public static async Task<TemperatureReport> ProcessAsync(IAsyncEnumerable<SensorReading> source,
        ReferenceService reference, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var temperatures = new List<TemperatureValue>();
        var maxima = new List<double>();
        var errors = new List<string>();
        await foreach (var reading in source.WithCancellation(cancellationToken))
        {
            if (reading.Kind != "temperature") continue;
            var celsius = Math.Round((reading.Value - 32) * 5 / 9, 2);
            temperatures.Add(new(reading.SensorId, celsius));
            maxima.Add(maxima.Count == 0 ? celsius : Math.Max(maxima[^1], celsius));
#if STREAMING_VALIDATION
            // Deliberate control defect: reference work precedes source disposal.
            if (!await reference.IsPlausibleAsync(celsius, cancellationToken)) errors.Add("implausible:" + reading.SensorId);
#endif
        }
#if !STREAMING_VALIDATION
        foreach (var temperature in temperatures)
            if (!await reference.IsPlausibleAsync(temperature.Celsius, cancellationToken)) errors.Add("implausible:" + temperature.SensorId);
#endif
        cancellationToken.ThrowIfCancellationRequested();
        return new(temperatures, maxima, errors.Count == 0 ? temperatures.Select(value => value.Celsius).ToArray() : [], errors);
    }
}
