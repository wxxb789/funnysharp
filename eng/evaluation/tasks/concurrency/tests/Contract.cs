// Style-neutral contract shared by both evaluation variants: the data types, the deterministic
// fakes with controllable delays, and the neutral outcome records the solution must produce. No
// FunnySharp types appear here on purpose: both styles end at the same seam.
public sealed record StockItem(string Sku, int Quantity);

public sealed record WarehouseStock(int OnHand, int CheckDelayMs);

public sealed record WarehouseReply(string Sku, int OnHand);

public sealed record ItemAvailability(string Sku, int OnHand, bool Sufficient);

public sealed class WarehouseGateway
{
    private readonly IReadOnlyDictionary<string, WarehouseStock> stock;
    private readonly object guard = new();
    private readonly TaskCompletionSource firstCheckEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public WarehouseGateway(IReadOnlyDictionary<string, WarehouseStock> stock) => this.stock = stock;

    public int StartedChecks { get; private set; }

    public int InFlightChecks { get; private set; }

    public int MaxInFlightChecks { get; private set; }

    public Task FirstCheckEntered => this.firstCheckEntered.Task;

    public async Task<WarehouseReply> CheckAsync(string sku, CancellationToken cancellationToken)
    {
        lock (this.guard)
        {
            this.StartedChecks++;
            this.InFlightChecks++;
            this.MaxInFlightChecks = Math.Max(this.MaxInFlightChecks, this.InFlightChecks);
        }

        this.firstCheckEntered.TrySetResult();
        try
        {
            var entry = this.stock.TryGetValue(sku, out var found) ? found : new WarehouseStock(0, 0);
            await Task.Delay(entry.CheckDelayMs, cancellationToken);
            return new WarehouseReply(sku, entry.OnHand);
        }
        finally
        {
            lock (this.guard)
            {
                this.InFlightChecks--;
            }
        }
    }
}

public sealed record SupplierOffer(bool Accepts, string? ReservationId, int ProbeDelayMs);

public sealed record SupplierReply(bool Accepts, string? ReservationId);

public sealed class SupplierGateway
{
    private readonly IReadOnlyDictionary<string, SupplierOffer> offers;
    private readonly object guard = new();
    private readonly TaskCompletionSource firstProbeStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public SupplierGateway(IReadOnlyDictionary<string, SupplierOffer> offers) => this.offers = offers;

    public int StartedProbes { get; private set; }

    public int InFlightProbes { get; private set; }

    public int MaxInFlightProbes { get; private set; }

    public Task FirstProbeStarted => this.firstProbeStarted.Task;

    public async Task<SupplierReply> ProbeAsync(string supplierId, CancellationToken cancellationToken)
    {
        lock (this.guard)
        {
            this.StartedProbes++;
            this.InFlightProbes++;
            this.MaxInFlightProbes = Math.Max(this.MaxInFlightProbes, this.InFlightProbes);
        }

        this.firstProbeStarted.TrySetResult();
        try
        {
            var offer = this.offers.TryGetValue(supplierId, out var found) ? found : new SupplierOffer(false, null, 0);
            await Task.Delay(offer.ProbeDelayMs, cancellationToken);
            return new SupplierReply(offer.Accepts, offer.ReservationId);
        }
        finally
        {
            lock (this.guard)
            {
                this.InFlightProbes--;
            }
        }
    }
}

public sealed record ReservationOutcome(
    bool Reserved,
    string? SupplierId,
    string? ReservationId,
    IReadOnlyList<string> FailedSuppliers);
