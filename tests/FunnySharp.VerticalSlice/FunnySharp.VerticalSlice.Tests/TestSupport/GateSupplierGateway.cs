using System.Runtime.CompilerServices;
using FunnySharp.VerticalSlice.Application;
using FunnySharp.VerticalSlice.Domain;

namespace FunnySharp.VerticalSlice.Tests;

/// <summary>
/// A supplier gateway with explicit control over each supplier's answer and its completion order.
/// It counts how many quotes are in flight so a test can prove the concurrency cap is real, and it
/// never sleeps: a supplier that should finish last waits on a signal the test releases.
/// </summary>
internal sealed class GateSupplierGateway : ISupplierGateway
{
    private readonly Dictionary<string, TaskCompletionSource> gates = new(StringComparer.Ordinal);
    private readonly List<(int Count, TaskCompletionSource Waiter)> startWaiters = [];
    private int inFlight;
    private int peakInFlight;

    public GateSupplierGateway(params string[] suppliers) => Suppliers = suppliers;

    public string[] Suppliers { get; }

    public List<string> Started { get; } = [];

    public int PeakInFlight => Volatile.Read(ref peakInFlight);

    public Dictionary<string, OrderError> Failures { get; } = new(StringComparer.Ordinal);

    public Dictionary<string, decimal> Prices { get; } = new(StringComparer.Ordinal);

    public async IAsyncEnumerable<SupplierRef> FindSuppliersAsync(
        Sku sku,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (var index = 0; index < Suppliers.Length; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return new SupplierRef(Suppliers[index], index);
            await Task.Yield();
        }
    }

    public async ValueTask<Result<SupplierQuote, OrderError>> QuoteAsync(
        SupplierRef supplier,
        Sku sku,
        Quantity quantity,
        CancellationToken cancellationToken)
    {
        var current = Interlocked.Increment(ref inFlight);
        UpdatePeak(current);
        lock (Started)
        {
            Started.Add(supplier.Name);
            SatisfyStartWaiters();
        }

        try
        {
            if (Gate(supplier.Name) is { } gate)
            {
                await gate.Task.WaitAsync(cancellationToken);
            }

            if (Failures.TryGetValue(supplier.Name, out var failure))
            {
                return Result<SupplierQuote, OrderError>.Failure(failure);
            }

            var amount = Prices.TryGetValue(supplier.Name, out var price) ? price : 10m;
            var money = Money.Create(amount, "EUR");
            _ = money.TryGetValue(out var unitPrice);
            return Result<SupplierQuote, OrderError>.Success(
                new SupplierQuote(supplier.Name, sku, quantity, unitPrice, TimeSpan.FromDays(1)));
        }
        finally
        {
            Interlocked.Decrement(ref inFlight);
        }
    }

    /// <summary>Completes once at least <paramref name="count"/> quotes have started.</summary>
    public Task StartedAtLeast(int count)
    {
        lock (Started)
        {
            if (Started.Count >= count)
            {
                return Task.CompletedTask;
            }

            var waiter = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            startWaiters.Add((count, waiter));
            return waiter.Task;
        }
    }

    /// <summary>Makes one supplier wait until <see cref="Release"/> is called for it.</summary>
    public TaskCompletionSource Hold(string supplier)
    {
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (gates)
        {
            gates[supplier] = gate;
        }

        return gate;
    }

    public void Release(string supplier)
    {
        TaskCompletionSource? gate;
        lock (gates)
        {
            gates.TryGetValue(supplier, out gate);
        }

        gate?.TrySetResult();
    }

    private TaskCompletionSource? Gate(string supplier)
    {
        lock (gates)
        {
            return gates.TryGetValue(supplier, out var gate) ? gate : null;
        }
    }

    private void SatisfyStartWaiters()
    {
        for (var index = startWaiters.Count - 1; index >= 0; index--)
        {
            if (startWaiters[index].Count <= Started.Count)
            {
                startWaiters[index].Waiter.TrySetResult();
                startWaiters.RemoveAt(index);
            }
        }
    }

    private void UpdatePeak(int current)
    {
        while (true)
        {
            var peak = Volatile.Read(ref peakInFlight);
            if (current <= peak || Interlocked.CompareExchange(ref peakInFlight, current, peak) == peak)
            {
                return;
            }
        }
    }
}
