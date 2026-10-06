// Style-neutral contract shared by both evaluation variants. Gateway completion is controlled
// by per-input entered/release signals; no FunnySharp types appear at this seam.
public sealed record StockItem(string Sku, int Quantity);

public sealed record WarehouseStock(int OnHand);

public sealed record WarehouseReply(string Sku, int OnHand);

public sealed record ItemAvailability(string Sku, int OnHand, bool Sufficient);

public sealed class WarehouseGateway
{
    private readonly IReadOnlyDictionary<string, WarehouseStock> stock;
    private readonly object guard = new();
    private readonly Dictionary<string, (TaskCompletionSource<Task<WarehouseReply>> Entered, TaskCompletionSource Release)> calls = [];
    private readonly List<string> started = [];
    private readonly List<string> completed = [];

    public WarehouseGateway(IReadOnlyDictionary<string, WarehouseStock> stock) => this.stock = stock;

    public int StartedChecks { get; private set; }

    public int InFlightChecks { get; private set; }

    public int MaxInFlightChecks { get; private set; }

    public IReadOnlyList<string> StartedSkus { get { lock (this.guard) { return this.started.ToArray(); } } }

    public IReadOnlyList<string> CompletedSkus { get { lock (this.guard) { return this.completed.ToArray(); } } }

    public Task CheckEntered(string sku) => this.GetCall(sku).Entered.Task;

    public async Task CheckCompleted(string sku) => await await this.GetCall(sku).Entered.Task;

    public void ReleaseCheck(string sku) => this.GetCall(sku).Release.TrySetResult();

    public Task<WarehouseReply> CheckAsync(string sku, CancellationToken cancellationToken)
    {
        var call = this.GetCall(sku);
        lock (this.guard)
        {
            this.StartedChecks++;
            this.InFlightChecks++;
            this.MaxInFlightChecks = Math.Max(this.MaxInFlightChecks, this.InFlightChecks);
            this.started.Add(sku);
        }

        var pending = this.CompleteCheckAsync(sku, call.Release.Task, cancellationToken);
        call.Entered.TrySetResult(pending);
        return pending;
    }

    private (TaskCompletionSource<Task<WarehouseReply>> Entered, TaskCompletionSource Release) GetCall(string sku)
    {
        lock (this.guard)
        {
            if (!this.calls.TryGetValue(sku, out var call))
            {
                call = (new(TaskCreationOptions.RunContinuationsAsynchronously), new(TaskCreationOptions.RunContinuationsAsynchronously));
                this.calls.Add(sku, call);
            }

            return call;
        }
    }

    private async Task<WarehouseReply> CompleteCheckAsync(string sku, Task release, CancellationToken cancellationToken)
    {
        try
        {
            await release.WaitAsync(cancellationToken);
            var entry = this.stock.TryGetValue(sku, out var found) ? found : new WarehouseStock(0);
            lock (this.guard)
            {
                this.completed.Add(sku);
            }

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

public sealed record SupplierOffer(bool Accepts, string? ReservationId);

public sealed record SupplierReply(bool Accepts, string? ReservationId);

public sealed class SupplierGateway
{
    private readonly IReadOnlyDictionary<string, SupplierOffer> offers;
    private readonly object guard = new();
    private readonly Dictionary<string, (TaskCompletionSource<Task<SupplierReply>> Entered, TaskCompletionSource Release)> calls = [];
    private readonly List<string> started = [];
    private readonly List<string> completed = [];

    public SupplierGateway(IReadOnlyDictionary<string, SupplierOffer> offers) => this.offers = offers;

    public int StartedProbes { get; private set; }

    public int InFlightProbes { get; private set; }

    public int MaxInFlightProbes { get; private set; }

    public IReadOnlyList<string> StartedSuppliers { get { lock (this.guard) { return this.started.ToArray(); } } }

    public IReadOnlyList<string> CompletedSuppliers { get { lock (this.guard) { return this.completed.ToArray(); } } }

    public Task ProbeEntered(string supplierId) => this.GetCall(supplierId).Entered.Task;

    public async Task ProbeCompleted(string supplierId) => await await this.GetCall(supplierId).Entered.Task;

    public void ReleaseProbe(string supplierId) => this.GetCall(supplierId).Release.TrySetResult();

    public Task<SupplierReply> ProbeAsync(string supplierId, CancellationToken cancellationToken)
    {
        var call = this.GetCall(supplierId);
        lock (this.guard)
        {
            this.StartedProbes++;
            this.InFlightProbes++;
            this.MaxInFlightProbes = Math.Max(this.MaxInFlightProbes, this.InFlightProbes);
            this.started.Add(supplierId);
        }

        var pending = this.CompleteProbeAsync(supplierId, call.Release.Task, cancellationToken);
        call.Entered.TrySetResult(pending);
        return pending;
    }

    private (TaskCompletionSource<Task<SupplierReply>> Entered, TaskCompletionSource Release) GetCall(string supplierId)
    {
        lock (this.guard)
        {
            if (!this.calls.TryGetValue(supplierId, out var call))
            {
                call = (new(TaskCreationOptions.RunContinuationsAsynchronously), new(TaskCreationOptions.RunContinuationsAsynchronously));
                this.calls.Add(supplierId, call);
            }

            return call;
        }
    }

    private async Task<SupplierReply> CompleteProbeAsync(string supplierId, Task release, CancellationToken cancellationToken)
    {
        try
        {
            await release.WaitAsync(cancellationToken);
            var entry = this.offers.TryGetValue(supplierId, out var found) ? found : new SupplierOffer(false, null);
            lock (this.guard)
            {
                this.completed.Add(supplierId);
            }

            return new SupplierReply(entry.Accepts, entry.ReservationId);
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
