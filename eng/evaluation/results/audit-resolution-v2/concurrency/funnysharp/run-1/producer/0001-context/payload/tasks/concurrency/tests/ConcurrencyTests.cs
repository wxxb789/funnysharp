// Timeouts bound failures only. Cleanup entry is not coordinator quiescence.
// Exact late-fault propagation witnesses a terminal dependency; successful
// outcome checks do not claim to prove the instant of coordinator publication.
public sealed class ConcurrencyTests
{
    private static readonly TimeSpan FailureBound = TimeSpan.FromSeconds(10);
    private static Task Signal(Task signal) => signal.WaitAsync(FailureBound, TestContext.Current.CancellationToken);
    private static Dictionary<string, CallGate> Gates() => new() { ["a"] = new(), ["b"] = new(), ["c"] = new() };
    private static async Task Exit(CallGate gate)
    {
        var returned = await gate.Returned.Task.WaitAsync(FailureBound, TestContext.Current.CancellationToken);
        await Signal(returned);
    }
    private static Dictionary<string, Task> Exits(IReadOnlyDictionary<string, CallGate> gates) =>
        gates.ToDictionary(pair => pair.Key, pair => Exit(pair.Value));
    private static void ReleaseAll(IReadOnlyDictionary<string, CallGate> gates)
    {
        foreach (var gate in gates.Values) { gate.Release.TrySetResult(); gate.CleanupRelease.TrySetResult(); }
    }
    private static async Task Drain(Task pending, IReadOnlyDictionary<string, CallGate> gates,
        IReadOnlyDictionary<string, Task> exits, Exception? expectedFault = null)
    {
        ReleaseAll(gates);
        try { await Signal(pending); }
        catch (OperationCanceledException) { }
        catch (Exception error) when (ReferenceEquals(error, expectedFault)) { } // already asserted sentinel
        finally
        {
            // Also observe the actual gateway tasks if the coordinator returned early.
            try { await Signal(Task.WhenAll(exits.Values)); }
            catch (OperationCanceledException) { }
            catch (Exception error) when (ReferenceEquals(error, expectedFault)) { }
        }
    }

    [Fact]
    public async Task AvailabilityIsSourceOrderedAndCallsEachGatewayOnce()
    {
        var gates = Gates();
        var gateway = new WarehouseGateway(new Dictionary<string, WarehouseStock> { ["a"] = new(7), ["b"] = new(1) }, gates);
        var entered = Signal(Task.WhenAll(gates.Values.Select(g => g.Entered.Task)));
        var exits = Exits(gates);
        var pending = AvailabilityCoordinator.CheckAvailabilityAsync([new("a", 2), new("b", 2), new("c", 1)], gateway, 3, TestContext.Current.CancellationToken);
        try
        {
            await entered;
            gates["c"].Release.TrySetResult(); await exits["c"];
            gates["b"].Release.TrySetResult(); await exits["b"];
            gates["a"].Release.TrySetResult();
            var results = await pending.WaitAsync(FailureBound, TestContext.Current.CancellationToken);
            await Signal(Task.WhenAll(exits.Values));
            Assert.Equal([new ItemAvailability("a", 7, true), new("b", 1, false), new("c", 0, false)], results);
            Assert.Equal(3, gateway.StartedChecks);
            Assert.Equal(0, gateway.InFlightChecks);
            Assert.All(gates.Values, g => Assert.Equal(1, g.Calls));
        }
        finally { await Drain(pending, gates, exits); }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task AvailabilityReachesButNeverExceedsTheBound(int bound)
    {
        var gates = Gates();
        var gateway = new WarehouseGateway(new Dictionary<string, WarehouseStock>(), gates);
        var entered = Signal(Task.WhenAll(gates.Take(bound).Select(pair => pair.Value.Entered.Task)));
        var refilled = Signal(gates.ElementAt(bound).Value.Entered.Task);
        var exits = Exits(gates);
        var pending = AvailabilityCoordinator.CheckAvailabilityAsync([new("a", 1), new("b", 1), new("c", 1)], gateway, bound, TestContext.Current.CancellationToken);
        try
        {
            await entered;
            gates["a"].Release.TrySetResult();
            await refilled;
            ReleaseAll(gates);
            await Signal(pending);
            await Signal(Task.WhenAll(exits.Values));
            // A retained maximum, not an absence snapshot while admission may run.
            Assert.Equal(bound, gateway.MaxInFlightChecks);
            Assert.Equal(3, gateway.StartedChecks);
            Assert.Equal(0, gateway.InFlightChecks);
            Assert.All(gates.Values, g => Assert.Equal(1, g.Calls));
        }
        finally { await Drain(pending, gates, exits); }
    }

    [Fact]
    public async Task CallerCancellationReachesHeldAvailabilityChecks()
    {
        var gates = Gates();
        var gateway = new WarehouseGateway(new Dictionary<string, WarehouseStock>(), gates);
        using var caller = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var entered = Signal(Task.WhenAll(gates["a"].Entered.Task, gates["b"].Entered.Task));
        var canceled = Signal(Task.WhenAll(gates["a"].Canceled.Task, gates["b"].Canceled.Task));
        var exits = Exits(gates.Where(pair => pair.Key != "c").ToDictionary(pair => pair.Key, pair => pair.Value));
        var pending = AvailabilityCoordinator.CheckAvailabilityAsync([new("a", 1), new("b", 1), new("c", 1)], gateway, 2, caller.Token);
        try
        {
            await entered;
            caller.Cancel();
            await canceled;
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending.WaitAsync(FailureBound, TestContext.Current.CancellationToken));
            foreach (var exit in exits.Values)
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Signal(exit));
            Assert.Equal(0, gateway.InFlightChecks);
            Assert.Equal(0, gates["c"].Calls);
            Assert.Equal(1, gates["a"].Calls);
            Assert.Equal(1, gates["b"].Calls);
        }
        finally { caller.Cancel(); await Drain(pending, gates, exits); }
    }

    [Theory]
    [InlineData("a")]
    [InlineData("c")]
    public async Task SelectedWinnerSurvivesCanceledLosersLateSuccess(string selected)
    {
        var names = new[] { "a", "b", "c" };
        var gates = names.ToDictionary(name => name, name => new CallGate { LateReplyOnCancel = true, HoldCleanup = name != selected });
        var offers = names.ToDictionary(name => name, name => new SupplierOffer(true, "reservation-" + name));
        var gateway = new SupplierGateway(offers, gates);
        var losers = gates.Where(pair => pair.Key != selected).Select(pair => pair.Value).ToArray();
        var entered = Signal(Task.WhenAll(gates.Values.Select(g => g.Entered.Task)));
        var canceled = Signal(Task.WhenAll(losers.Select(g => g.Canceled.Task)));
        var cleanup = Signal(Task.WhenAll(losers.Select(g => g.CleanupEntered.Task)));
        var exits = Exits(gates);
        var pending = AvailabilityCoordinator.ReserveFirstAsync(names, gateway, TestContext.Current.CancellationToken);
        try
        {
            await entered;
            gates[selected].Release.TrySetResult();
            // Only selected can have supplied a reply when losers are canceled.
            // This witnesses cancellation, not a parked/draining coordinator.
            await canceled;
            await cleanup;
            foreach (var loser in losers) loser.CleanupRelease.TrySetResult();
            var outcome = await pending.WaitAsync(FailureBound, TestContext.Current.CancellationToken);
            await Signal(Task.WhenAll(exits.Values));
            Assert.True(outcome.Reserved);
            Assert.Equal(selected, outcome.SupplierId);
            Assert.Equal("reservation-" + selected, outcome.ReservationId);
            Assert.Empty(outcome.FailedSuppliers);
            Assert.Equal(3, gateway.StartedProbes);
            Assert.Equal(0, gateway.InFlightProbes);
            Assert.All(gates.Values, g => Assert.Equal(1, g.Calls));
        }
        finally { await Drain(pending, gates, exits); }
    }

    [Fact]
    public async Task AllDeclinesStayInputOrderedWhenReleasedInReverse()
    {
        var gates = Gates();
        var gateway = new SupplierGateway(gates.Keys.ToDictionary(name => name, _ => new SupplierOffer(false, null)), gates);
        var entered = Signal(Task.WhenAll(gates.Values.Select(g => g.Entered.Task)));
        var exits = Exits(gates);
        var pending = AvailabilityCoordinator.ReserveFirstAsync(["a", "b", "c"], gateway, TestContext.Current.CancellationToken);
        try
        {
            await entered;
            foreach (var name in new[] { "c", "b", "a" }) { gates[name].Release.TrySetResult(); await exits[name]; }
            var outcome = await pending.WaitAsync(FailureBound, TestContext.Current.CancellationToken);
            Assert.False(outcome.Reserved);
            Assert.Null(outcome.SupplierId);
            Assert.Null(outcome.ReservationId);
            Assert.Equal(["a", "b", "c"], outcome.FailedSuppliers);
            Assert.Equal(0, gateway.InFlightProbes);
            Assert.All(gates.Values, g => Assert.Equal(1, g.Calls));
        }
        finally { await Drain(pending, gates, exits); }
    }

    [Fact]
    public async Task CallerCancellationReachesReservationProbes()
    {
        var gates = Gates();
        var gateway = new SupplierGateway(gates.Keys.ToDictionary(name => name, _ => new SupplierOffer(true, "r")), gates);
        using var caller = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var entered = Signal(Task.WhenAll(gates.Values.Select(g => g.Entered.Task)));
        var canceled = Signal(Task.WhenAll(gates.Values.Select(g => g.Canceled.Task)));
        var exits = Exits(gates);
        var pending = AvailabilityCoordinator.ReserveFirstAsync(["a", "b", "c"], gateway, caller.Token);
        try
        {
            await entered;
            caller.Cancel();
            await canceled;
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending.WaitAsync(FailureBound, TestContext.Current.CancellationToken));
            foreach (var exit in exits.Values)
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Signal(exit));
            Assert.Equal(0, gateway.InFlightProbes);
            Assert.All(gates.Values, g => Assert.Equal(1, g.Calls));
        }
        finally { caller.Cancel(); await Drain(pending, gates, exits); }
    }

    [Fact]
    public async Task IndependentLoserCleanupFaultCannotHideBehindSuccess()
    {
        var names = new[] { "a", "b", "c" };
        // Four bounded cases, not scheduler retries: each opposite winner and each loser.
        foreach (var selected in new[] { "a", "c" })
        foreach (var faulted in names.Where(name => name != selected))
        {
            var fault = new InvalidOperationException("independent-cleanup-" + selected + "-" + faulted);
            var gates = names.ToDictionary(name => name, name => new CallGate
            {
                LateReplyOnCancel = true, HoldCleanup = name != selected,
                CleanupFault = name == faulted ? fault : null,
            });
            var gateway = new SupplierGateway(names.ToDictionary(name => name, name => new SupplierOffer(true, "r-" + name)), gates);
            var losers = gates.Where(pair => pair.Key != selected).Select(pair => pair.Value).ToArray();
            var entered = Signal(Task.WhenAll(gates.Values.Select(g => g.Entered.Task)));
            var canceled = Signal(Task.WhenAll(losers.Select(g => g.Canceled.Task)));
            var cleanup = Signal(Task.WhenAll(losers.Select(g => g.CleanupEntered.Task)));
            var exits = Exits(gates);
            var pending = AvailabilityCoordinator.ReserveFirstAsync(names, gateway, TestContext.Current.CancellationToken);
            try
            {
                await entered;
                gates[selected].Release.TrySetResult();
                await canceled;
                await cleanup;
                foreach (var name in names.Where(name => name != faulted))
                    gates[name].CleanupRelease.TrySetResult();
                await Signal(Task.WhenAll(exits.Where(pair => pair.Key != faulted).Select(pair => pair.Value)));

                // The sole outstanding gateway task will fault. Success cannot later
                // be amended to this exact exception, however continuations are ordered.
                var outcome = Assert.ThrowsAsync<InvalidOperationException>(() => pending.WaitAsync(FailureBound, TestContext.Current.CancellationToken));
                gates[faulted].CleanupRelease.TrySetResult();
                Assert.Same(fault, await outcome);
                var gatewayFault = await Assert.ThrowsAsync<InvalidOperationException>(() => Signal(exits[faulted]));
                Assert.Same(fault, gatewayFault);
                Assert.Equal(3, gateway.StartedProbes);
                Assert.Equal(0, gateway.InFlightProbes);
                Assert.All(gates.Values, g => Assert.Equal(1, g.Calls));
            }
            finally { await Drain(pending, gates, exits, fault); }
        }
    }

    [Fact]
    public async Task EmptyInputsStartNothing()
    {
        var gates = Gates();
        var warehouse = new WarehouseGateway(new Dictionary<string, WarehouseStock>(), gates);
        var suppliers = new SupplierGateway(new Dictionary<string, SupplierOffer>(), gates);
        Assert.Empty(await AvailabilityCoordinator.CheckAvailabilityAsync([], warehouse, 2, TestContext.Current.CancellationToken).WaitAsync(FailureBound, TestContext.Current.CancellationToken));
        var outcome = await AvailabilityCoordinator.ReserveFirstAsync([], suppliers, TestContext.Current.CancellationToken).WaitAsync(FailureBound, TestContext.Current.CancellationToken);
        Assert.False(outcome.Reserved);
        Assert.Null(outcome.SupplierId);
        Assert.Null(outcome.ReservationId);
        Assert.Empty(outcome.FailedSuppliers);
        Assert.Equal(0, warehouse.StartedChecks);
        Assert.Equal(0, suppliers.StartedProbes);
    }

    [Fact]
    public async Task AlreadyCompletedSuccessesChooseInputOrder()
    {
        var gates = Gates(); ReleaseAll(gates);
        var gateway = new SupplierGateway(gates.Keys.ToDictionary(name => name, name => new SupplierOffer(true, "r-" + name)), gates);
        var exits = Exits(gates);
        var outcome = await AvailabilityCoordinator.ReserveFirstAsync(["a", "b", "c"], gateway, TestContext.Current.CancellationToken).WaitAsync(FailureBound, TestContext.Current.CancellationToken);
        await Signal(Task.WhenAll(exits.Values));
        Assert.True(outcome.Reserved); Assert.Equal("a", outcome.SupplierId); Assert.Equal("r-a", outcome.ReservationId);
        Assert.Empty(outcome.FailedSuppliers); Assert.Equal(0, gateway.InFlightProbes);
        Assert.Equal(3, gateway.StartedProbes); Assert.All(gates.Values, g => Assert.Equal(1, g.Calls));
    }

    [Fact]
    public async Task PreCanceledCallerStartsNoGatewaySideEffects()
    {
        var gates = Gates();
        var warehouse = new WarehouseGateway(new Dictionary<string, WarehouseStock>(), gates);
        var suppliers = new SupplierGateway(new Dictionary<string, SupplierOffer>(), gates);
        using var caller = new CancellationTokenSource(); caller.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => AvailabilityCoordinator.CheckAvailabilityAsync([new("a", 1)], warehouse, 1, caller.Token).WaitAsync(FailureBound, TestContext.Current.CancellationToken));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => AvailabilityCoordinator.ReserveFirstAsync(["a"], suppliers, caller.Token).WaitAsync(FailureBound, TestContext.Current.CancellationToken));
        Assert.Equal(0, warehouse.StartedChecks);
        Assert.Equal(0, suppliers.StartedProbes);
    }
}
