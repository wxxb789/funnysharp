public sealed class ConcurrencyTests
{
    private static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task ResultsComeBackInSourceOrderWhenEarlyItemsFinishLast()
    {
        var gateway = new WarehouseGateway(new Dictionary<string, WarehouseStock>
        {
            // The last sku is unknown; two admitted inputs finish in the opposite source order.
            ["keyboard"] = new(40),
            ["mouse"] = new(15),
            ["monitor"] = new(8),
            ["trackball"] = new(3),
        });
        var items = new StockItem[]
        {
            new("keyboard", 2),
            new("mouse", 1),
            new("monitor", 9),
            new("trackball", 1),
            new("webcam", 1),
        };

        var entered = items.ToDictionary(item => item.Sku, item => gateway.CheckEntered(item.Sku));
        var completed = items.ToDictionary(item => item.Sku, item => gateway.CheckCompleted(item.Sku));
        var pending = AvailabilityCoordinator.CheckAvailabilityAsync(items, gateway, 3, CancellationToken.None);
        IReadOnlyList<ItemAvailability> results;
        string earlier;
        string later;
        try
        {
            Assert.False(pending.IsCompleted);
            var remaining = new Dictionary<string, Task>(entered);
            var firstEntered = await Task.WhenAny(remaining.Values).WaitAsync(Deadline, TestContext.Current.CancellationToken);
            var first = remaining.Single(entry => ReferenceEquals(entry.Value, firstEntered)).Key;
            remaining.Remove(first);
            var secondEntered = await Task.WhenAny(remaining.Values).WaitAsync(Deadline, TestContext.Current.CancellationToken);
            var second = remaining.Single(entry => ReferenceEquals(entry.Value, secondEntered)).Key;
            remaining.Remove(second);
            (earlier, later) = Array.FindIndex(items, item => item.Sku == first) < Array.FindIndex(items, item => item.Sku == second)
                ? (first, second)
                : (second, first);
            gateway.ReleaseCheck(later);
            await completed[later].WaitAsync(Deadline, TestContext.Current.CancellationToken);
            gateway.ReleaseCheck(earlier);
            await completed[earlier].WaitAsync(Deadline, TestContext.Current.CancellationToken);

            while (remaining.Count != 0)
            {
                var nextEntered = await Task.WhenAny(remaining.Values).WaitAsync(Deadline, TestContext.Current.CancellationToken);
                var sku = remaining.Single(entry => ReferenceEquals(entry.Value, nextEntered)).Key;
                gateway.ReleaseCheck(sku);
                await completed[sku].WaitAsync(Deadline, TestContext.Current.CancellationToken);
                remaining.Remove(sku);
            }

            results = await pending.WaitAsync(Deadline, TestContext.Current.CancellationToken);
        }
        finally
        {
            foreach (var item in items) { gateway.ReleaseCheck(item.Sku); }
            await DrainAsync(pending);
        }

        Assert.Equal(items.Select(item => item.Sku).Order(StringComparer.Ordinal), gateway.StartedSkus.Order(StringComparer.Ordinal));
        Assert.Equal(0, gateway.InFlightChecks);
        var completionOrder = gateway.CompletedSkus.ToList();
        Assert.True(completionOrder.IndexOf(later) < completionOrder.IndexOf(earlier));

        Assert.Equal(["keyboard", "mouse", "monitor", "trackball", "webcam"], results.Select(result => result.Sku));
        Assert.Equal([40, 15, 8, 3, 0], results.Select(result => result.OnHand));
        Assert.Equal([true, true, false, true, false], results.Select(result => result.Sufficient));
    }

    [Fact]
    public async Task ChecksOverlapButNeverExceedMaxConcurrency()
    {
        var gateway = new WarehouseGateway(new Dictionary<string, WarehouseStock>
        {
            ["keyboard"] = new(40),
            ["mouse"] = new(15),
            ["monitor"] = new(8),
            ["trackball"] = new(3),
            ["dock"] = new(9),
            ["hub"] = new(12),
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

        var entered = items.ToDictionary(item => item.Sku, item => gateway.CheckEntered(item.Sku));
        var completed = items.ToDictionary(item => item.Sku, item => gateway.CheckCompleted(item.Sku));
        var pending = AvailabilityCoordinator.CheckAvailabilityAsync(items, gateway, 3, CancellationToken.None);
        IReadOnlyList<ItemAvailability> results;
        try
        {
            // A serial implementation cannot enter a second check while the first is held.
            var firstEntered = await Task.WhenAny(entered.Values).WaitAsync(Deadline, TestContext.Current.CancellationToken);
            await Task.WhenAny(entered.Values.Where(task => !ReferenceEquals(task, firstEntered))).WaitAsync(Deadline, TestContext.Current.CancellationToken);
            Assert.InRange(gateway.InFlightChecks, 2, 3);
            foreach (var item in items) { gateway.ReleaseCheck(item.Sku); }

            results = await pending.WaitAsync(Deadline, TestContext.Current.CancellationToken);
        }
        finally
        {
            foreach (var item in items) { gateway.ReleaseCheck(item.Sku); }
            await DrainAsync(pending);
        }

        Assert.Equal(items.Select(item => item.Sku).Order(StringComparer.Ordinal), gateway.StartedSkus.Order(StringComparer.Ordinal));
        Assert.Equal(0, gateway.InFlightChecks);

        Assert.Equal(["keyboard", "mouse", "monitor", "trackball", "dock", "hub"], results.Select(result => result.Sku));
        Assert.True(gateway.MaxInFlightChecks >= 2, $"expected overlapping checks, max in flight was {gateway.MaxInFlightChecks}");
        Assert.True(gateway.MaxInFlightChecks <= 3, $"expected at most 3 checks in flight, was {gateway.MaxInFlightChecks}");
    }

    [Fact]
    public async Task MaxConcurrencyOfOneRunsOneCheckAtATime()
    {
        var gateway = new WarehouseGateway(new Dictionary<string, WarehouseStock>
        {
            ["keyboard"] = new(40),
            ["mouse"] = new(15),
            ["monitor"] = new(8),
            ["trackball"] = new(3),
        });
        var items = new StockItem[]
        {
            new("keyboard", 1),
            new("mouse", 2),
            new("monitor", 1),
            new("trackball", 1),
        };

        var entered = items.ToDictionary(item => item.Sku, item => gateway.CheckEntered(item.Sku));
        var completed = items.ToDictionary(item => item.Sku, item => gateway.CheckCompleted(item.Sku));
        var pending = AvailabilityCoordinator.CheckAvailabilityAsync(items, gateway, 1, CancellationToken.None);
        IReadOnlyList<ItemAvailability> results;
        try
        {
            var remaining = new Dictionary<string, Task>(entered);
            while (remaining.Count != 0)
            {
                var nextEntered = await Task.WhenAny(remaining.Values).WaitAsync(Deadline, TestContext.Current.CancellationToken);
                var sku = remaining.Single(entry => ReferenceEquals(entry.Value, nextEntered)).Key;
                Assert.Equal(1, gateway.InFlightChecks);
                gateway.ReleaseCheck(sku);
                await completed[sku].WaitAsync(Deadline, TestContext.Current.CancellationToken);
                remaining.Remove(sku);
            }

            results = await pending.WaitAsync(Deadline, TestContext.Current.CancellationToken);
        }
        finally
        {
            foreach (var item in items) { gateway.ReleaseCheck(item.Sku); }
            await DrainAsync(pending);
        }

        Assert.Equal(items.Select(item => item.Sku).Order(StringComparer.Ordinal), gateway.StartedSkus.Order(StringComparer.Ordinal));
        Assert.Equal(0, gateway.InFlightChecks);

        Assert.Equal(1, gateway.MaxInFlightChecks);
        Assert.Equal(4, gateway.StartedChecks);
        Assert.Equal(["keyboard", "mouse", "monitor", "trackball"], results.Select(result => result.Sku));
    }

    [Fact]
    public async Task FirstAcceptingSupplierWinsEvenWhenDeclinesFinishFirst()
    {
        var gateway = new SupplierGateway(new Dictionary<string, SupplierOffer>
        {
            ["north"] = new(false, null),
            ["south"] = new(false, null),
            ["east"] = new(true, "res-3003"),
        });

        string[] suppliers = ["north", "south", "east"];
        var entered = suppliers.ToDictionary(supplier => supplier, gateway.ProbeEntered);
        var completed = suppliers.ToDictionary(supplier => supplier, gateway.ProbeCompleted);
        var pending = AvailabilityCoordinator.ReserveFirstAsync(suppliers, gateway, CancellationToken.None);
        ReservationOutcome outcome;
        try
        {
            await Task.WhenAll(entered.Values).WaitAsync(Deadline, TestContext.Current.CancellationToken);
            Assert.False(pending.IsCompleted);
            foreach (var supplier in suppliers)
            {
                gateway.ReleaseProbe(supplier);
                await completed[supplier].WaitAsync(Deadline, TestContext.Current.CancellationToken);
            }

            outcome = await pending.WaitAsync(Deadline, TestContext.Current.CancellationToken);
        }
        finally
        {
            foreach (var supplier in suppliers) { gateway.ReleaseProbe(supplier); }
            await DrainAsync(pending);
        }

        Assert.Equal(suppliers.Order(StringComparer.Ordinal), gateway.StartedSuppliers.Order(StringComparer.Ordinal));
        Assert.Equal(0, gateway.InFlightProbes);
        Assert.Equal(suppliers, gateway.CompletedSuppliers);

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
            ["north"] = new(false, null),
            ["south"] = new(false, null),
            ["east"] = new(false, null),
        });

        string[] suppliers = ["north", "south", "east"];
        var entered = suppliers.ToDictionary(supplier => supplier, gateway.ProbeEntered);
        var completed = suppliers.ToDictionary(supplier => supplier, gateway.ProbeCompleted);
        var pending = AvailabilityCoordinator.ReserveFirstAsync(suppliers, gateway, CancellationToken.None);
        ReservationOutcome outcome;
        try
        {
            await Task.WhenAll(entered.Values).WaitAsync(Deadline, TestContext.Current.CancellationToken);
            Assert.False(pending.IsCompleted);
            foreach (var supplier in new[] { "south", "east", "north" })
            {
                gateway.ReleaseProbe(supplier);
                await completed[supplier].WaitAsync(Deadline, TestContext.Current.CancellationToken);
            }

            outcome = await pending.WaitAsync(Deadline, TestContext.Current.CancellationToken);
        }
        finally
        {
            foreach (var supplier in suppliers) { gateway.ReleaseProbe(supplier); }
            await DrainAsync(pending);
        }

        Assert.Equal(suppliers.Order(StringComparer.Ordinal), gateway.StartedSuppliers.Order(StringComparer.Ordinal));
        Assert.Equal(0, gateway.InFlightProbes);
        Assert.Equal(new[] { "south", "east", "north" }, gateway.CompletedSuppliers);

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
            ["keyboard"] = new(40),
            ["mouse"] = new(15),
            ["monitor"] = new(8),
        });
        var items = new StockItem[] { new("keyboard", 1), new("mouse", 1), new("monitor", 1) };
        using var cts = new CancellationTokenSource();

        var entered = items.ToDictionary(item => item.Sku, item => gateway.CheckEntered(item.Sku));
        var completed = items.ToDictionary(item => item.Sku, item => gateway.CheckCompleted(item.Sku));
        var pending = AvailabilityCoordinator.CheckAvailabilityAsync(items, gateway, 2, cts.Token);
        try
        {
            var firstEntered = await Task.WhenAny(entered.Values).WaitAsync(Deadline, TestContext.Current.CancellationToken);
            await Task.WhenAny(entered.Values.Where(task => !ReferenceEquals(task, firstEntered))).WaitAsync(Deadline, TestContext.Current.CancellationToken);
            Assert.Equal(2, gateway.StartedChecks);
            Assert.Equal(2, gateway.InFlightChecks);
            Assert.Empty(gateway.CompletedSkus);
            Assert.False(pending.IsCompleted);
            var active = gateway.StartedSkus;
            cts.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending.WaitAsync(Deadline, TestContext.Current.CancellationToken));
            foreach (var sku in active)
            {
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => completed[sku].WaitAsync(Deadline, TestContext.Current.CancellationToken));
                Assert.True(completed[sku].IsCanceled);
            }

            Assert.Empty(gateway.CompletedSkus);
            var started = gateway.StartedSkus;
            Assert.InRange(started.Count, 2, items.Length);
            Assert.Equal(started.Count, started.Distinct(StringComparer.Ordinal).Count());
            Assert.All(started, sku => Assert.Contains(sku, items.Select(item => item.Sku)));
            Assert.True(gateway.MaxInFlightChecks <= 2);
        }
        finally
        {
            foreach (var item in items) { gateway.ReleaseCheck(item.Sku); }
            await DrainAsync(pending);
            foreach (var sku in gateway.StartedSkus) { await DrainAsync(completed[sku]); }
        }
    }

    [Fact]
    public async Task CancelledReservationSurfacesOperationCanceledException()
    {
        var gateway = new SupplierGateway(new Dictionary<string, SupplierOffer>
        {
            ["north"] = new(false, null),
            ["south"] = new(true, "res-2"),
        });
        using var cts = new CancellationTokenSource();

        string[] suppliers = ["north", "south"];
        var entered = suppliers.ToDictionary(supplier => supplier, gateway.ProbeEntered);
        var completed = suppliers.ToDictionary(supplier => supplier, gateway.ProbeCompleted);
        var pending = AvailabilityCoordinator.ReserveFirstAsync(suppliers, gateway, cts.Token);
        try
        {
            await Task.WhenAll(entered.Values).WaitAsync(Deadline, TestContext.Current.CancellationToken);
            Assert.Equal(suppliers.Order(StringComparer.Ordinal), gateway.StartedSuppliers.Order(StringComparer.Ordinal));
            Assert.Empty(gateway.CompletedSuppliers);
            Assert.False(pending.IsCompleted);
            cts.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending.WaitAsync(Deadline, TestContext.Current.CancellationToken));
            foreach (var supplier in suppliers)
            {
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => completed[supplier].WaitAsync(Deadline, TestContext.Current.CancellationToken));
                Assert.True(completed[supplier].IsCanceled);
            }

            Assert.Empty(gateway.CompletedSuppliers);
            Assert.Equal(suppliers.Order(StringComparer.Ordinal), gateway.StartedSuppliers.Order(StringComparer.Ordinal));
            Assert.Equal(0, gateway.InFlightProbes);
        }
        finally
        {
            foreach (var supplier in suppliers) { gateway.ReleaseProbe(supplier); }
            await DrainAsync(pending);
            foreach (var supplier in gateway.StartedSuppliers) { await DrainAsync(completed[supplier]); }
        }
    }

    [Fact]
    public async Task EmptyItemListStartsNoChecksAndReturnsNoResults()
    {
        var gateway = new WarehouseGateway(new Dictionary<string, WarehouseStock> { ["keyboard"] = new(40) });

        var results = await AvailabilityCoordinator.CheckAvailabilityAsync([], gateway, 3, CancellationToken.None).WaitAsync(Deadline, TestContext.Current.CancellationToken);

        Assert.Empty(results);
        Assert.Equal(0, gateway.StartedChecks);
    }

    [Fact]
    public async Task EmptySupplierListStartsNoProbesAndReservesNothing()
    {
        var gateway = new SupplierGateway(new Dictionary<string, SupplierOffer> { ["north"] = new(true, "res-1") });

        var outcome = await AvailabilityCoordinator.ReserveFirstAsync([], gateway, CancellationToken.None).WaitAsync(Deadline, TestContext.Current.CancellationToken);

        Assert.False(outcome.Reserved);
        Assert.Null(outcome.SupplierId);
        Assert.Null(outcome.ReservationId);
        Assert.Empty(outcome.FailedSuppliers);
        Assert.Equal(0, gateway.StartedProbes);
    }
    private static async Task DrainAsync(Task pending)
    {
        try
        {
            await pending.WaitAsync(Deadline);
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation, including a faulty premature winner, drains held calls.
        }
    }
}
