public sealed record StockItem(string Sku, int Quantity);
public sealed record WarehouseStock(int OnHand);
public sealed record WarehouseReply(string Sku, int OnHand);
public sealed record ItemAvailability(string Sku, int OnHand, bool Sufficient);
public sealed record SupplierOffer(bool Accepts, string? ReservationId);
public sealed record SupplierReply(bool Accepts, string? ReservationId);
public sealed record ReservationOutcome(bool Reserved, string? SupplierId, string? ReservationId, IReadOnlyList<string> FailedSuppliers);

// Supplied neutral fake infrastructure, shared by both styles. Every signal is
// installed before invocation. Timeouts in the oracle only detect failure/hangs.
public sealed class CallGate
{
    public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource Canceled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource CleanupEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public TaskCompletionSource CleanupRelease { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    // Carries the exact Task returned to the coordinator, not a cleanup callback.
    // Await both levels to observe terminal completion, including final accounting.
    public TaskCompletionSource<Task> Returned { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public bool LateReplyOnCancel { get; init; }
    public Exception? CleanupFault { get; init; }
    public bool HoldCleanup { get; init; }
    public int Calls;

    public async Task WaitAsync(CancellationToken token)
    {
        Interlocked.Increment(ref this.Calls);
        this.Entered.TrySetResult();
        try { await this.Release.Task.WaitAsync(token); }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            this.Canceled.TrySetResult();
            if (!this.LateReplyOnCancel) throw;
        }
    }

    public async Task CleanupAsync()
    {
        this.CleanupEntered.TrySetResult();
        if (this.HoldCleanup) await this.CleanupRelease.Task;
        if (this.CleanupFault is not null) throw this.CleanupFault;
    }
}

public sealed class WarehouseGateway(IReadOnlyDictionary<string, WarehouseStock> stock, IReadOnlyDictionary<string, CallGate> gates)
{
    private int inFlight;
    private int maxInFlight;
    private int started;
    private readonly object guard = new();
    public int InFlightChecks => Volatile.Read(ref this.inFlight);
    public int MaxInFlightChecks => Volatile.Read(ref this.maxInFlight);
    public int StartedChecks => Volatile.Read(ref this.started);

    public Task<WarehouseReply> CheckAsync(string sku, CancellationToken cancellationToken)
    {
        var pending = this.CheckCoreAsync(sku, cancellationToken);
        gates[sku].Returned.TrySetResult(pending);
        return pending;
    }

    private async Task<WarehouseReply> CheckCoreAsync(string sku, CancellationToken cancellationToken)
    {
        var gate = gates[sku];
        lock (this.guard)
        {
            this.started++;
            this.inFlight++;
            this.maxInFlight = Math.Max(this.maxInFlight, this.inFlight);
        }
        try
        {
            await gate.WaitAsync(cancellationToken);
            return new(sku, stock.TryGetValue(sku, out var value) ? value.OnHand : 0);
        }
        finally
        {
            try { await gate.CleanupAsync(); }
            finally { lock (this.guard) { this.inFlight--; } }
        }
    }
}

public sealed class SupplierGateway(IReadOnlyDictionary<string, SupplierOffer> offers, IReadOnlyDictionary<string, CallGate> gates)
{
    private int inFlight;
    private int started;
    public int InFlightProbes => Volatile.Read(ref this.inFlight);
    public int StartedProbes => Volatile.Read(ref this.started);

    public Task<SupplierReply> ProbeAsync(string supplierId, CancellationToken cancellationToken)
    {
        var pending = this.ProbeCoreAsync(supplierId, cancellationToken);
        gates[supplierId].Returned.TrySetResult(pending);
        return pending;
    }

    private async Task<SupplierReply> ProbeCoreAsync(string supplierId, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref this.started);
        Interlocked.Increment(ref this.inFlight);
        var gate = gates[supplierId];
        try
        {
            await gate.WaitAsync(cancellationToken);
            var offer = offers[supplierId];
            return new(offer.Accepts, offer.ReservationId);
        }
        finally
        {
            try { await gate.CleanupAsync(); }
            finally { Interlocked.Decrement(ref this.inFlight); }
        }
    }
}
