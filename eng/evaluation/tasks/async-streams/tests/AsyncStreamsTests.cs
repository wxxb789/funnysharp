using System.Runtime.CompilerServices;

// The suite deliberately passes its own tokens (CancellationToken.None, a fresh token whose
// exact forwarding is asserted, and a pre-canceled token), so the xUnit1051 guidance to use
// TestContext.Current.CancellationToken does not apply here.
#pragma warning disable xUnit1051

public sealed class AsyncStreamsTests
{
    private static ReferenceService Reference() => new() { MinCelsius = 0, MaxCelsius = 45 };

    private static async IAsyncEnumerable<SensorReading> Stream(
        IReadOnlyList<SensorReading> readings,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var reading in readings)
        {
            await Task.Delay(1);
            yield return reading;
        }
    }

    private static async IAsyncEnumerable<SensorReading> TokenAwareStream(
        IReadOnlyList<SensorReading> readings,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var reading in readings)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                throw new OperationCanceledException(cancellationToken);
            }

            await Task.Delay(1);
            yield return reading;
        }
    }

    private static async IAsyncEnumerable<SensorReading> CancellingStream(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.Delay(1);
        yield return new SensorReading("temp-1", "temperature", 68);
        throw new OperationCanceledException(cancellationToken);
    }

    private sealed class RecordingSource
    {
        private readonly IReadOnlyList<SensorReading> readings;

        public RecordingSource(IReadOnlyList<SensorReading> readings) => this.readings = readings;

        public int Yields { get; private set; }

        public async IAsyncEnumerable<SensorReading> Enumerate(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var reading in this.readings)
            {
                this.Yields++;
                await Task.Delay(1);
                yield return reading;
            }
        }
    }

    [Fact]
    public async Task FilterMapKeepsOnlyTemperatureReadingsInCelsius()
    {
        var source = new RecordingSource(
        [
            new SensorReading("hum-1", "humidity", 55),
            new SensorReading("temp-1", "temperature", 68),
            new SensorReading("pres-1", "pressure", 1013),
            new SensorReading("temp-2", "temperature", 100),
            new SensorReading("temp-3", "temperature", 86),
        ]);
        var reference = Reference();
        var report = await SensorStream.ProcessAsync(source.Enumerate(), reference, CancellationToken.None);
        // 68F -> 20C, 100F -> 37.78C rounded to two decimals, 86F -> 30C; other kinds are skipped.
        Assert.Equal(
            [new TemperatureValue("temp-1", 20), new TemperatureValue("temp-2", 37.78), new TemperatureValue("temp-3", 30)],
            report.Temperatures);
        Assert.Equal(3, reference.Checks.Count);
        // The source is enumerated exactly once.
        Assert.Equal(5, source.Yields);
    }

    [Fact]
    public async Task RunningMaximaTrackTheMaximumSoFar()
    {
        var report = await SensorStream.ProcessAsync(
            Stream(
            [
                new SensorReading("temp-1", "temperature", 50),
                new SensorReading("temp-2", "temperature", 86),
                new SensorReading("temp-3", "temperature", 41),
                new SensorReading("temp-4", "temperature", 212),
            ]),
            new ReferenceService { MinCelsius = 0, MaxCelsius = 100 },
            CancellationToken.None);
        // 50F -> 10C, 86F -> 30C, 41F -> 5C, 212F -> 100C: a falling reading keeps the maximum.
        Assert.Equal([10, 30, 5, 100], report.ValidCelsius);
        Assert.Equal([10, 30, 30, 100], report.RunningMaxima);
        Assert.Empty(report.Errors);
    }

    [Fact]
    public async Task AllPlausibleReadingsCollectTheValidBatch()
    {
        using var cts = new CancellationTokenSource();
        var reference = new ReferenceService { MinCelsius = 0, MaxCelsius = 45 };
        var report = await SensorStream.ProcessAsync(
            Stream([new SensorReading("temp-1", "temperature", 68), new SensorReading("temp-2", "temperature", 113)]),
            reference,
            cts.Token);
        // 68F -> 20C and 113F -> 45C; the reference bounds are inclusive.
        Assert.Equal([20, 45], report.ValidCelsius);
        Assert.Empty(report.Errors);
        Assert.Equal([20, 45], reference.Checks.Select(check => check.Celsius));
        Assert.All(reference.Checks, check => Assert.Equal(cts.Token, check.Token));
    }

    [Fact]
    public async Task ImplausibleReadingsAccumulateEveryFailure()
    {
        var reference = Reference();
        var report = await SensorStream.ProcessAsync(
            Stream(
            [
                new SensorReading("temp-1", "temperature", 14),
                new SensorReading("temp-2", "temperature", 68),
                new SensorReading("temp-3", "temperature", -13),
                new SensorReading("temp-4", "temperature", 86),
            ]),
            reference,
            CancellationToken.None);
        // 14F -> -10C and -13F -> -25C fall below the reference range.
        Assert.Equal(["implausible:temp-1", "implausible:temp-3"], report.Errors);
        Assert.Empty(report.ValidCelsius);
        // The first failure does not stop validation: every reading is checked in source order.
        Assert.Equal([-10, 20, -25, 30], reference.Checks.Select(check => check.Celsius));
        // The running maximum is independent of validation and starts at the first value.
        Assert.Equal([-10, 20, 20, 30], report.RunningMaxima);
    }

    [Fact]
    public async Task NonTemperatureReadingsProduceAnEmptyReport()
    {
        var reference = Reference();
        var report = await SensorStream.ProcessAsync(
            Stream([new SensorReading("hum-1", "humidity", 55), new SensorReading("pres-1", "pressure", 1013)]),
            reference,
            CancellationToken.None);
        Assert.Empty(report.Temperatures);
        Assert.Empty(report.RunningMaxima);
        Assert.Empty(report.ValidCelsius);
        Assert.Empty(report.Errors);
        Assert.Empty(reference.Checks);
    }

    [Fact]
    public async Task EmptyStreamProducesAnEmptyReport()
    {
        var reference = Reference();
        var report = await SensorStream.ProcessAsync(Stream([]), reference, CancellationToken.None);
        Assert.Empty(report.Temperatures);
        Assert.Empty(report.RunningMaxima);
        Assert.Empty(report.ValidCelsius);
        Assert.Empty(report.Errors);
        Assert.Empty(reference.Checks);
    }

    [Fact]
    public async Task CancelledTokenSurfacesBeforeAnyReferenceCall()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var reference = Reference();
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => SensorStream.ProcessAsync(
                TokenAwareStream([new SensorReading("temp-1", "temperature", 68)]),
                reference,
                cts.Token));
        Assert.Empty(reference.Checks);
    }

    [Fact]
    public async Task SourceCancellationSurfacesAsOperationCanceledException()
    {
        var reference = Reference();
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => SensorStream.ProcessAsync(CancellingStream(), reference, CancellationToken.None));
        // The source faults during materialization, before validation starts.
        Assert.Empty(reference.Checks);
    }
}
