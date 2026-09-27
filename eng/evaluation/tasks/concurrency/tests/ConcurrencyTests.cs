public sealed class ConcurrencyTests
{
    [Fact]
    public async Task ResultsComeBackInSourceOrderWhenEarlyItemsFinishLast()
    {
        var gateway = new WarehouseGateway(new Dictionary<string, WarehouseStock>
        {
            // The first item is the slowest check on purpose; the last sku is unknown.
            ["keyboard"] = new(40, 60),
            ["mouse"] = new(15, 45),
            ["monitor"] = new(8, 30),
            ["trackball"] = new(3, 15),
        });
        var items = new StockItem[]
        {
            new("keyboard", 2),
            new("mouse", 1),
            new("monitor", 9),
            new("trackball", 1),
            new("webcam", 1),
        };

        var results = await AvailabilityCoordinator.CheckAvailabilityAsync(items, gateway, 5, CancellationToken.None);

        Assert.Equal(["keyboard", "mouse", "monitor", "trackball", "webcam"], results.Select(result => result.Sku));
        Assert.Equal([40, 15, 8, 3, 0], results.Select(result => result.OnHand));
        Assert.Equal([true, true, false, true, false], results.Select(result => result.Sufficient));
    }

    [Fact]
    public async Task ChecksOverlapButNeverExceedMaxConcurrency()
    {
        var gateway = new WarehouseGateway(new Dictionary<string, WarehouseStock>
        {
            ["keyboard"] = new(40, 40),
            ["mouse"] = new(15, 40),
            ["monitor"] = new(8, 40),
            ["trackball"] = new(3, 40),
            ["dock"] = new(9, 40),
            ["hub"] = new(12, 40),
        });
        var items = new StockItem[]
        {
            new("keyboard", 1),
            new("mouse", 1),
            new("monitor", 1),
            new("trackball", 1),
            new("dock", 1),
            new("hub", 1),
        };

        var results = await AvailabilityCoordinator.CheckAvailabilityAsync(items, gateway, 3, CancellationToken.None);

        Assert.Equal(["keyboard", "mouse", "monitor", "trackball", "dock", "hub"], results.Select(result => result.Sku));
        Assert.True(gateway.MaxInFlightChecks >= 2, $"expected overlapping checks, max in flight was {gateway.MaxInFlightChecks}");
        Assert.True(gateway.MaxInFlightChecks <= 3, $"expected at most 3 checks in flight, was {gateway.MaxInFlightChecks}");
    }

    [Fact]
    public async Task MaxConcurrencyOfOneRunsOneCheckAtATime()
    {
        var gateway = new WarehouseGateway(new Dictionary<string, WarehouseStock>
        {
            ["keyboard"] = new(40, 10),
            ["mouse"] = new(15, 10),
            ["monitor"] = new(8, 10),
            ["trackball"] = new(3, 10),
        });
        var items = new StockItem[]
        {
            new("keyboard", 1),
            new("mouse", 2),
            new("monitor", 1),
            new("trackball", 1),
        };

        var results = await AvailabilityCoordinator.CheckAvailabilityAsync(items, gateway, 1, CancellationToken.None);

        Assert.Equal(1, gateway.MaxInFlightChecks);
        Assert.Equal(4, gateway.StartedChecks);
        Assert.Equal(["keyboard", "mouse", "monitor", "trackball"], results.Select(result => result.Sku));
    }

    [Fact]
    public async Task FirstAcceptingSupplierWinsEvenWhenDeclinesFinishFirst()
    {
        var gateway = new SupplierGateway(new Dictionary<string, SupplierOffer>
        {
            ["north"] = new(false, null, 5), // declines first
            ["south"] = new(false, null, 20), // declines second
            ["east"] = new(true, "res-3003", 35), // accepts last
        });

        var outcome = await AvailabilityCoordinator.ReserveFirstAsync(["north", "south", "east"], gateway, CancellationToken.None);

        Assert.True(outcome.Reserved);
        Assert.Equal("east", outcome.SupplierId);
        Assert.Equal("res-3003", outcome.ReservationId);
        Assert.Empty(outcome.FailedSuppliers);
        Assert.Equal(3, gateway.StartedProbes);
        Assert.True(gateway.MaxInFlightProbes >= 2, $"expected overlapping probes, max in flight was {gateway.MaxInFlightProbes}");
    }

    [Fact]
    public async Task EveryDeclineIsListedInInputOrderWhenAllSuppliersFail()
    {
        var gateway = new SupplierGateway(new Dictionary<string, SupplierOffer>
        {
            ["north"] = new(false, null, 20), // finishes last
            ["south"] = new(false, null, 5), // finishes first
            ["east"] = new(false, null, 10), // finishes second
        });

        var outcome = await AvailabilityCoordinator.ReserveFirstAsync(["north", "south", "east"], gateway, CancellationToken.None);

        Assert.False(outcome.Reserved);
        Assert.Null(outcome.SupplierId);
        Assert.Null(outcome.ReservationId);
        Assert.Equal(["north", "south", "east"], outcome.FailedSuppliers);
    }

    [Fact]
    public async Task CancelledAvailabilityCheckSurfacesOperationCanceledException()
    {
        var gateway = new WarehouseGateway(new Dictionary<string, WarehouseStock>
        {
            ["keyboard"] = new(40, 60),
            ["mouse"] = new(15, 60),
            ["monitor"] = new(8, 60),
        });
        var items = new StockItem[] { new("keyboard", 1), new("mouse", 1), new("monitor", 1) };
        using var cts = new CancellationTokenSource();

        var pending = AvailabilityCoordinator.CheckAvailabilityAsync(items, gateway, 2, cts.Token);
        await gateway.FirstCheckEntered;
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
    }

    [Fact]
    public async Task CancelledReservationSurfacesOperationCanceledException()
    {
        var gateway = new SupplierGateway(new Dictionary<string, SupplierOffer>
        {
            ["north"] = new(false, null, 60),
            ["south"] = new(true, "res-2", 60),
        });
        using var cts = new CancellationTokenSource();

        var pending = AvailabilityCoordinator.ReserveFirstAsync(["north", "south"], gateway, cts.Token);
        await gateway.FirstProbeStarted;
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
    }

    [Fact]
    public async Task EmptyItemListStartsNoChecksAndReturnsNoResults()
    {
        var gateway = new WarehouseGateway(new Dictionary<string, WarehouseStock> { ["keyboard"] = new(40, 10) });

        var results = await AvailabilityCoordinator.CheckAvailabilityAsync([], gateway, 3, CancellationToken.None);

        Assert.Empty(results);
        Assert.Equal(0, gateway.StartedChecks);
    }

    [Fact]
    public async Task EmptySupplierListStartsNoProbesAndReservesNothing()
    {
        var gateway = new SupplierGateway(new Dictionary<string, SupplierOffer> { ["north"] = new(true, "res-1", 10) });

        var outcome = await AvailabilityCoordinator.ReserveFirstAsync([], gateway, CancellationToken.None);

        Assert.False(outcome.Reserved);
        Assert.Null(outcome.SupplierId);
        Assert.Null(outcome.ReservationId);
        Assert.Empty(outcome.FailedSuppliers);
        Assert.Equal(0, gateway.StartedProbes);
    }
}
