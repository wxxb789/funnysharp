using System.Runtime.CompilerServices;

public sealed class AsyncStreamsTests
{
    private static readonly TimeSpan FailureBound = TimeSpan.FromSeconds(10);
    private static Task Signal(Task task) => task.WaitAsync(FailureBound, TestContext.Current.CancellationToken);
    private sealed class Source(IReadOnlyList<SensorReading> readings)
    {
        public TaskCompletionSource Held { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool HoldAfterFirst { get; init; }
        public Exception? Fault { get; init; }
        public int Enumerations { get; private set; }
        public int Yields { get; private set; }
        public int Disposals { get; private set; }
        public CancellationToken Token { get; private set; }
        public async IAsyncEnumerable<SensorReading> Enumerate([EnumeratorCancellation] CancellationToken token = default)
        {
            this.Enumerations++;
            this.Token = token;
            try
            {
                foreach (var reading in readings)
                {
                    token.ThrowIfCancellationRequested();
                    this.Yields++;
                    yield return reading;
                    if (this.HoldAfterFirst && this.Yields == 1)
                    {
                        this.Held.TrySetResult();
                        await this.Release.Task.WaitAsync(token);
                    }
                }
                await Task.CompletedTask;
                if (this.Fault is not null) throw this.Fault;
            }
            finally { this.Disposals++; }
        }
    }
    private static ReferenceService Reference(double min = 0, double max = 45) => new() { MinCelsius = min, MaxCelsius = max };

    [Fact]
    public async Task MaterializationFinishesBeforeValidationAndDisposesOnce()
    {
        var source = new Source([new("t1", "temperature", 68), new("h1", "Temperature", 99), new("t2", "temperature", 86)]) { HoldAfterFirst = true };
        var reference = new ReferenceService
        {
            MinCelsius = 0,
            MaxCelsius = 45,
            BeforeCheck = () => Assert.Equal(1, source.Disposals),
        };
        using var caller = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var pending = SensorStream.ProcessAsync(source.Enumerate(caller.Token), reference, caller.Token);
        try
        {
            await Signal(source.Held.Task);
            source.Release.TrySetResult();
            var report = await pending.WaitAsync(FailureBound, TestContext.Current.CancellationToken);
            Assert.Equal([new TemperatureValue("t1", 20), new("t2", 30)], report.Temperatures);
            Assert.Equal([20d, 30d], report.ValidCelsius);
            Assert.Equal([20d, 30d], reference.Checks.Select(check => check.Celsius));
            Assert.All(reference.Checks, check => Assert.Equal(caller.Token, check.Token));
            Assert.Equal(caller.Token, source.Token);
            Assert.Equal(1, source.Enumerations);
            Assert.Equal(3, source.Yields);
            Assert.Equal(1, source.Disposals);
        }
        finally { source.Release.TrySetResult(); caller.Cancel(); await Signal(pending); }
    }

    [Fact]
    public async Task NegativeFirstMaximumAndRoundingAreCorrect()
    {
        var source = new Source([new("a", "temperature", 14), new("b", "temperature", -13), new("c", "temperature", 100)]);
        var report = await SensorStream.ProcessAsync(source.Enumerate(TestContext.Current.CancellationToken), Reference(-30, 45), TestContext.Current.CancellationToken).WaitAsync(FailureBound, TestContext.Current.CancellationToken);
        Assert.Equal([-10d, -10d, 37.78], report.RunningMaxima);
        Assert.Equal([-10d, -25d, 37.78], report.ValidCelsius);
        Assert.Empty(report.Errors);
    }

    [Fact]
    public async Task InclusiveBoundsAndOncePerValueTokensArePreserved()
    {
        using var caller = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var reference = Reference();
        var source = new Source([new("a", "temperature", 32), new("b", "temperature", 113)]);
        var report = await SensorStream.ProcessAsync(source.Enumerate(caller.Token), reference, caller.Token).WaitAsync(FailureBound, TestContext.Current.CancellationToken);
        Assert.Equal([0d, 45d], report.ValidCelsius);
        Assert.Empty(report.Errors);
        Assert.Equal([0d, 45d], reference.Checks.Select(check => check.Celsius));
        Assert.All(reference.Checks, check => Assert.Equal(caller.Token, check.Token));
    }

    [Fact]
    public async Task EveryImplausibleValueIsReportedWithoutShortCircuit()
    {
        var reference = Reference();
        var source = new Source([new("a", "temperature", 14), new("b", "temperature", 68), new("c", "temperature", -13), new("d", "temperature", 86)]);
        var report = await SensorStream.ProcessAsync(source.Enumerate(TestContext.Current.CancellationToken), reference, TestContext.Current.CancellationToken).WaitAsync(FailureBound, TestContext.Current.CancellationToken);
        Assert.Equal(["implausible:a", "implausible:c"], report.Errors);
        Assert.Empty(report.ValidCelsius);
        Assert.Equal([-10d, 20d, -25d, 30d], reference.Checks.Select(check => check.Celsius));
        Assert.Equal([-10d, 20d, 20d, 30d], report.RunningMaxima);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task EmptyOrFilteredStreamHasNoReferenceSideEffects(bool empty)
    {
        var reference = Reference();
        var source = new Source(empty ? [] : [new("h", "humidity", 55), new("c", "Temperature", 86)]);
        var report = await SensorStream.ProcessAsync(source.Enumerate(TestContext.Current.CancellationToken), reference, TestContext.Current.CancellationToken).WaitAsync(FailureBound, TestContext.Current.CancellationToken);
        Assert.Empty(report.Temperatures); Assert.Empty(report.RunningMaxima); Assert.Empty(report.ValidCelsius); Assert.Empty(report.Errors);
        Assert.Empty(reference.Checks);
        Assert.Equal(1, source.Enumerations); Assert.Equal(1, source.Disposals);
    }

    [Fact]
    public async Task PreCanceledTokenMakesNoReferenceCalls()
    {
        using var caller = new CancellationTokenSource(); caller.Cancel();
        var reference = Reference();
        var source = new Source([new("a", "temperature", 68)]);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => SensorStream.ProcessAsync(source.Enumerate(caller.Token), reference, caller.Token).WaitAsync(FailureBound, TestContext.Current.CancellationToken));
        Assert.Empty(reference.Checks);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SourceFaultOrCancellationPreservesIdentityAndSkipsValidation(bool cancel)
    {
        Exception fault = cancel ? new OperationCanceledException("source-sentinel") : new InvalidOperationException("source-sentinel");
        var source = new Source([new("a", "temperature", 68)]) { Fault = fault };
        var reference = Reference();
        var caught = await Record.ExceptionAsync(() => SensorStream.ProcessAsync(source.Enumerate(TestContext.Current.CancellationToken), reference, TestContext.Current.CancellationToken).WaitAsync(FailureBound, TestContext.Current.CancellationToken));
        Assert.Same(fault, caught);
        Assert.Empty(reference.Checks);
        Assert.Equal(1, source.Enumerations); Assert.Equal(1, source.Disposals);
    }

    [Fact]
    public async Task CancellationDuringHeldReferenceExitsWithoutPartialReport()
    {
        using var caller = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var reference = new ReferenceService { MinCelsius = 0, MaxCelsius = 45, Held = true };
        var source = new Source([new("a", "temperature", 68), new("b", "temperature", 86)]);
        var pending = SensorStream.ProcessAsync(source.Enumerate(caller.Token), reference, caller.Token);
        try
        {
            await Signal(reference.Entered.Task);
            Assert.Equal(1, source.Disposals);
            Assert.Single(reference.Checks);
            caller.Cancel();
            await Signal(reference.Canceled.Task);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending.WaitAsync(FailureBound, TestContext.Current.CancellationToken));
            Assert.True(reference.Exited.Task.IsCompletedSuccessfully);
            Assert.Single(reference.Checks);
        }
        finally
        {
            caller.Cancel(); reference.Release.TrySetResult();
            try { await Signal(pending); } catch (OperationCanceledException) { }
        }
    }
}
