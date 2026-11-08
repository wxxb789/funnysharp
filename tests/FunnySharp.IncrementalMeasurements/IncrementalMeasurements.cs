using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using FunnySharp;
using FunnySharp.Incremental;
using FunnySharp.TestSupport;
using FunnySharp.TestSupport.IncrementalComparison;
using static FunnySharp.TestSupport.MeasurementHarness;

namespace FunnySharp.IncrementalMeasurements;

internal static class IncrementalMeasurements
{
    private enum Kind
    {
        Full,
        Hand,
        Graph,
        Collection,
        Cache,
    }

    private readonly record struct SetupObservation(
        double Milliseconds,
        long AllocatedBytes,
        int ProductEvaluations,
        int QuoteEvaluations,
        int LineEvaluations,
        int NodeCount,
        int EdgeCount);

    private readonly record struct LatencyObservation(
        double P50Microseconds,
        double P95Microseconds,
        double P99Microseconds,
        double MaxMicroseconds);

    private static readonly Product[] Products = PricingFixtures.CreateProducts();
    private static readonly Order[] Orders = PricingFixtures.CreateOrders();

    internal static void Run()
    {
        WriteEnvironment();
        var checks = IncrementalPricingComparison.Verify();
        Console.WriteLine($"parity: verified {checks} checks across full, hand-maintained, per-order graph, collection-node graph, and cache consumers.");
        Console.WriteLine();
        WriteGraphShape();
        WriteWorkAvoidance();
        WriteSetup();
        WriteRetention();
        WriteRuntime();
        WriteLatency();
    }

    private static void WriteEnvironment()
    {
        Console.WriteLine("## Environment");
        Console.WriteLine();
        Console.WriteLine($"runtime: {RuntimeInformation.FrameworkDescription}");
        Console.WriteLine($"os: {RuntimeInformation.OSDescription}");
        Console.WriteLine($"architecture: {RuntimeInformation.ProcessArchitecture}");
        Console.WriteLine($"cores: {Environment.ProcessorCount}");
        Console.WriteLine($"utc: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"stopwatch frequency: {Stopwatch.Frequency.ToString(CultureInfo.InvariantCulture)}");
        Console.WriteLine();
    }

    private static void WriteGraphShape()
    {
        Console.WriteLine("## Graph shape");
        Console.WriteLine();
        Console.WriteLine("| Graph consumer | Nodes | Edges |");
        Console.WriteLine("| --- | ---: | ---: |");

        foreach (var kind in new[] { Kind.Graph, Kind.Collection })
        {
            var consumer = Create(kind);
            var statistics = consumer.GraphStatistics!.Value;
            Console.WriteLine($"| {consumer.Name} | {statistics.NodeCount} | {statistics.EdgeCount} |");
            Dispose(consumer);
        }

        Console.WriteLine();
    }

    private static void WriteWorkAvoidance()
    {
        Console.WriteLine("## Work avoided for one complete update plus full quote retrieval");
        Console.WriteLine();
        Console.WriteLine("| Scenario | Consumer | Product evals | Quote evals | Line evals | Node evals | Skipped nodes | Changed nodes | Input changes | Observer notifications |");
        Console.WriteLine("| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |");

        WriteWorkRow("Product cost update", Kind.Full, static consumer => consumer.SetProductCost(0, 120m));
        WriteWorkRow("Product cost update", Kind.Hand, static consumer => consumer.SetProductCost(0, 120m));
        WriteWorkRow("Product cost update", Kind.Graph, static consumer => consumer.SetProductCost(0, 120m));
        WriteWorkRow("Product cost update", Kind.Collection, static consumer => consumer.SetProductCost(0, 120m));
        WriteWorkRow("Product cost update", Kind.Cache, static consumer => consumer.SetProductCost(0, 120m));

        WriteWorkRow("Order quantity update", Kind.Full, static consumer => consumer.SetOrderQuantity(5, 3, 7));
        WriteWorkRow("Order quantity update", Kind.Hand, static consumer => consumer.SetOrderQuantity(5, 3, 7));
        WriteWorkRow("Order quantity update", Kind.Graph, static consumer => consumer.SetOrderQuantity(5, 3, 7));
        WriteWorkRow("Order quantity update", Kind.Collection, static consumer => consumer.SetOrderQuantity(5, 3, 7));
        WriteWorkRow("Order quantity update", Kind.Cache, static consumer => consumer.SetOrderQuantity(5, 3, 7));

        WriteBatchWorkRow("Mixed batch", Kind.Full);
        WriteBatchWorkRow("Mixed batch", Kind.Hand);
        WriteBatchWorkRow("Mixed batch", Kind.Graph);
        WriteBatchWorkRow("Mixed batch", Kind.Collection);
        WriteBatchWorkRow("Mixed batch", Kind.Cache);

        WriteWorkRow("No-op product cost", Kind.Hand, static consumer => consumer.SetProductCost(3, Products[3].UnitCost));
        WriteWorkRow("No-op product cost", Kind.Graph, static consumer => consumer.SetProductCost(3, Products[3].UnitCost));

        var additional = PricingFixtures.CreateAdditionalOrder(10_000);
        WriteWorkRow("Add one order", Kind.Full, consumer => consumer.AddOrder(additional));
        WriteWorkRow("Add one order", Kind.Hand, consumer => consumer.AddOrder(additional));
        WriteWorkRow("Add one order", Kind.Collection, consumer => consumer.AddOrder(additional));
        WriteWorkRow("Add one order", Kind.Cache, consumer => consumer.AddOrder(additional));
        WriteUnsupportedRow("Add one order", "Per-order incremental graph", "fixed topology");

        WriteWorkRow("Remove last order", Kind.Full, static consumer => consumer.RemoveLastOrder());
        WriteWorkRow("Remove last order", Kind.Hand, static consumer => consumer.RemoveLastOrder());
        WriteWorkRow("Remove last order", Kind.Collection, static consumer => consumer.RemoveLastOrder());
        WriteWorkRow("Remove last order", Kind.Cache, static consumer => consumer.RemoveLastOrder());
        WriteUnsupportedRow("Remove last order", "Per-order incremental graph", "fixed topology");

        Console.WriteLine();
    }

    private static void WriteWorkRow(string scenario, Kind kind, Action<IPricingConsumer> update)
    {
        var consumer = Create(kind);
        consumer.ResetObservability();
        update(consumer);
        _ = Digest(consumer.Evaluate());
        WriteWorkValues(scenario, consumer);
        Dispose(consumer);
    }

    private static void WriteBatchWorkRow(string scenario, Kind kind)
    {
        var consumer = Create(kind);
        consumer.ResetObservability();

        consumer.Batch(() =>
        {
            consumer.SetProductCost(1, 130m);
            consumer.SetProductCost(2, 140m);
            consumer.SetOrderQuantity(10, 2, 4);
        });

        _ = Digest(consumer.Evaluate());
        WriteWorkValues(scenario, consumer);
        Dispose(consumer);
    }

    private static void WriteWorkValues(string scenario, IPricingConsumer consumer)
    {
        var work = consumer.Work;
        var statistics = consumer.GraphStatistics;

        Console.WriteLine(
            $"| {scenario} | {consumer.Name} | {work.ProductEvaluations} | {work.QuoteEvaluations} | "
            + $"{work.LineEvaluations} | {GraphValue(statistics, s => s.NodeEvaluations)} | "
            + $"{GraphValue(statistics, s => s.SkippedNodeEvaluations)} | "
            + $"{GraphValue(statistics, s => s.ChangedNodes)} | "
            + $"{GraphValue(statistics, s => s.InputChanges)} | "
            + $"{GraphValue(statistics, s => s.ObserverNotifications)} |");
    }

    private static string GraphValue(
        IncrementalGraphStatistics? statistics,
        Func<IncrementalGraphStatistics, long> selector) =>
        statistics is { } value ? selector(value).ToString(CultureInfo.InvariantCulture) : "-";

    private static void WriteUnsupportedRow(string scenario, string consumer, string reason) =>
        Console.WriteLine($"| {scenario} | {consumer} | - | - | - | - | - | - | - | - ({reason}) |");

    private static void WriteSetup()
    {
        Console.WriteLine("## Setup: construction plus initial complete evaluation");
        Console.WriteLine();
        Console.WriteLine("| Consumer | Setup ms (best of 3) | Allocated B/setup | Product evals | Quote evals | Line evals | Nodes | Edges |");
        Console.WriteLine("| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |");

        foreach (var kind in Enum.GetValues<Kind>())
        {
            var setup = MeasureSetup(kind);
            var consumer = Create(kind);
            var statistics = consumer.GraphStatistics;
            Dispose(consumer);

            Console.WriteLine(
                $"| {NameOf(kind)} | {setup.Milliseconds.ToString("F3", CultureInfo.InvariantCulture)} | "
                + $"{setup.AllocatedBytes} | {setup.ProductEvaluations} | {setup.QuoteEvaluations} | "
                + $"{setup.LineEvaluations} | {statistics?.NodeCount.ToString(CultureInfo.InvariantCulture) ?? "-"} | "
                + $"{statistics?.EdgeCount.ToString(CultureInfo.InvariantCulture) ?? "-"} |");
        }

        Console.WriteLine();
    }

    private static SetupObservation MeasureSetup(Kind kind)
    {
        var warm = Create(kind);
        _ = Digest(warm.Evaluate());
        Dispose(warm);

        var bestMilliseconds = double.MaxValue;
        var bestAllocated = long.MaxValue;
        var productEvaluations = 0;
        var quoteEvaluations = 0;
        var lineEvaluations = 0;
        var nodeCount = 0;
        var edgeCount = 0;

        for (var run = 0; run < 3; run++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var before = GC.GetAllocatedBytesForCurrentThread();
            var stopwatch = Stopwatch.StartNew();
            var consumer = Create(kind);
            _ = Digest(consumer.Evaluate());
            stopwatch.Stop();
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            var work = consumer.Work;
            var statistics = consumer.GraphStatistics;

            if (stopwatch.Elapsed.TotalMilliseconds < bestMilliseconds)
            {
                bestMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
                productEvaluations = work.ProductEvaluations;
                quoteEvaluations = work.QuoteEvaluations;
                lineEvaluations = work.LineEvaluations;
                nodeCount = statistics?.NodeCount ?? 0;
                edgeCount = statistics?.EdgeCount ?? 0;
            }

            bestAllocated = Math.Min(bestAllocated, allocated);
            Dispose(consumer);
        }

        return new SetupObservation(
            bestMilliseconds,
            bestAllocated,
            productEvaluations,
            quoteEvaluations,
            lineEvaluations,
            nodeCount,
            edgeCount);
    }

    private static void WriteRetention()
    {
        Console.WriteLine("## Retained memory after setup and after 200 product updates");
        Console.WriteLine();
        Console.WriteLine("| Consumer | Retained B after setup | Retained B after 200 updates | Delta B |");
        Console.WriteLine("| --- | ---: | ---: | ---: |");

        foreach (var kind in Enum.GetValues<Kind>())
        {
            var setupRetained = MeasureRetained(kind, updates: 0);
            var updatedRetained = MeasureRetained(kind, updates: 200);

            Console.WriteLine(
                $"| {NameOf(kind)} | {setupRetained} | {updatedRetained} | {updatedRetained - setupRetained} |");
        }

        Console.WriteLine();
    }

    private static long MeasureRetained(Kind kind, int updates)
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var before = GC.GetTotalMemory(forceFullCollection: true);

        var consumer = Create(kind);
        _ = Digest(consumer.Evaluate());

        for (var index = 0; index < updates; index++)
        {
            consumer.SetProductCost(0, 100m + ((index & 1) * 5m));
            _ = Digest(consumer.Evaluate());
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var after = GC.GetTotalMemory(forceFullCollection: true);
        GC.KeepAlive(consumer);
        Dispose(consumer);
        return Math.Max(0, after - before);
    }

    private static void WriteRuntime()
    {
        Console.WriteLine("## Complete update plus quote retrieval: allocations and directional timing");
        Console.WriteLine();
        Console.WriteLine("| Scenario | Consumer | Allocated B/op | Time ns/op |");
        Console.WriteLine("| --- | --- | ---: | ---: |");

        WriteRuntimeRow("Product cost update", Kind.Full, static (consumer, index) =>
            consumer.SetProductCost(0, 100m + ((index & 1) * 5m)), iterations: 100, warmup: 10);
        WriteRuntimeRow("Product cost update", Kind.Hand, static (consumer, index) =>
            consumer.SetProductCost(0, 100m + ((index & 1) * 5m)), iterations: 5_000, warmup: 100);
        WriteRuntimeRow("Product cost update", Kind.Graph, static (consumer, index) =>
            consumer.SetProductCost(0, 100m + ((index & 1) * 5m)), iterations: 5_000, warmup: 100);
        WriteRuntimeRow("Product cost update", Kind.Collection, static (consumer, index) =>
            consumer.SetProductCost(0, 100m + ((index & 1) * 5m)), iterations: 200, warmup: 20);
        WriteRuntimeRow("Product cost update", Kind.Cache, static (consumer, index) =>
            consumer.SetProductCost(0, 100m + ((index & 1) * 5m)), iterations: 200, warmup: 20);

        WriteRuntimeRow("Order quantity update", Kind.Full, static (consumer, index) =>
            consumer.SetOrderQuantity(5, 3, 4 + (index & 1)), iterations: 100, warmup: 10);
        WriteRuntimeRow("Order quantity update", Kind.Hand, static (consumer, index) =>
            consumer.SetOrderQuantity(5, 3, 4 + (index & 1)), iterations: 5_000, warmup: 100);
        WriteRuntimeRow("Order quantity update", Kind.Graph, static (consumer, index) =>
            consumer.SetOrderQuantity(5, 3, 4 + (index & 1)), iterations: 5_000, warmup: 100);
        WriteRuntimeRow("Order quantity update", Kind.Collection, static (consumer, index) =>
            consumer.SetOrderQuantity(5, 3, 4 + (index & 1)), iterations: 200, warmup: 20);
        WriteRuntimeRow("Order quantity update", Kind.Cache, static (consumer, index) =>
            consumer.SetOrderQuantity(5, 3, 4 + (index & 1)), iterations: 200, warmup: 20);

        WriteRuntimeRow("Mixed batch", Kind.Full, static (consumer, index) =>
            consumer.Batch(() =>
            {
                consumer.SetProductCost(1, 130m + ((index & 1) * 5m));
                consumer.SetProductCost(2, 140m + ((index & 1) * 5m));
                consumer.SetOrderQuantity(10, 2, 4 + (index & 1));
            }), iterations: 100, warmup: 10);
        WriteRuntimeRow("Mixed batch", Kind.Hand, static (consumer, index) =>
            consumer.Batch(() =>
            {
                consumer.SetProductCost(1, 130m + ((index & 1) * 5m));
                consumer.SetProductCost(2, 140m + ((index & 1) * 5m));
                consumer.SetOrderQuantity(10, 2, 4 + (index & 1));
            }), iterations: 3_000, warmup: 100);
        WriteRuntimeRow("Mixed batch", Kind.Graph, static (consumer, index) =>
            consumer.Batch(() =>
            {
                consumer.SetProductCost(1, 130m + ((index & 1) * 5m));
                consumer.SetProductCost(2, 140m + ((index & 1) * 5m));
                consumer.SetOrderQuantity(10, 2, 4 + (index & 1));
            }), iterations: 3_000, warmup: 100);
        WriteRuntimeRow("Mixed batch", Kind.Collection, static (consumer, index) =>
            consumer.Batch(() =>
            {
                consumer.SetProductCost(1, 130m + ((index & 1) * 5m));
                consumer.SetProductCost(2, 140m + ((index & 1) * 5m));
                consumer.SetOrderQuantity(10, 2, 4 + (index & 1));
            }), iterations: 200, warmup: 20);
        WriteRuntimeRow("Mixed batch", Kind.Cache, static (consumer, index) =>
            consumer.Batch(() =>
            {
                consumer.SetProductCost(1, 130m + ((index & 1) * 5m));
                consumer.SetProductCost(2, 140m + ((index & 1) * 5m));
                consumer.SetOrderQuantity(10, 2, 4 + (index & 1));
            }), iterations: 200, warmup: 20);

        WriteRuntimeRow("No-op product cost", Kind.Hand, static consumer =>
            consumer.SetProductCost(3, Products[3].UnitCost), iterations: 5_000, warmup: 100);
        WriteRuntimeRow("No-op product cost", Kind.Graph, static consumer =>
            consumer.SetProductCost(3, Products[3].UnitCost), iterations: 5_000, warmup: 100);

        Console.WriteLine();
    }

    private static void WriteRuntimeRow(
        string scenario,
        Kind kind,
        Action<IPricingConsumer, int> update,
        int iterations,
        int warmup)
    {
        var consumer = Create(kind);
        consumer.ResetObservability();
        var counter = 0;

        Func<int> operation = () =>
        {
            counter++;
            update(consumer, counter);
            return Digest(consumer.Evaluate());
        };

        _ = operation();
        var measurement = Measure(operation, iterations, warmup);

        Console.WriteLine(
            $"| {scenario} | {consumer.Name} | {Format(measurement.BytesPerOperation)} | {Format(measurement.NanosecondsPerOperation)} |");

        Dispose(consumer);
    }

    private static void WriteRuntimeRow(
        string scenario,
        Kind kind,
        Action<IPricingConsumer> update,
        int iterations,
        int warmup)
    {
        var consumer = Create(kind);
        consumer.ResetObservability();

        Func<int> operation = () =>
        {
            update(consumer);
            return Digest(consumer.Evaluate());
        };

        _ = operation();
        var measurement = Measure(operation, iterations, warmup);

        Console.WriteLine(
            $"| {scenario} | {consumer.Name} | {Format(measurement.BytesPerOperation)} | {Format(measurement.NanosecondsPerOperation)} |");

        Dispose(consumer);
    }

    private static void WriteLatency()
    {
        Console.WriteLine("## Update latency distribution (microseconds)");
        Console.WriteLine();
        Console.WriteLine("| Scenario | Consumer | p50 | p95 | p99 | max | Samples |");
        Console.WriteLine("| --- | --- | ---: | ---: | ---: | ---: | ---: |");

        foreach (var kind in new[] { Kind.Full, Kind.Hand, Kind.Graph, Kind.Collection, Kind.Cache })
        {
            var observation = MeasureLatency(
                kind,
                static (consumer, index) => consumer.SetProductCost(0, 100m + ((index & 1) * 5m)),
                samples: kind is Kind.Full ? 200 : 1_000,
                warmup: kind is Kind.Full ? 20 : 100);

            Console.WriteLine(
                $"| Product cost update | {NameOf(kind)} | "
                + $"{observation.P50Microseconds.ToString("F2", CultureInfo.InvariantCulture)} | "
                + $"{observation.P95Microseconds.ToString("F2", CultureInfo.InvariantCulture)} | "
                + $"{observation.P99Microseconds.ToString("F2", CultureInfo.InvariantCulture)} | "
                + $"{observation.MaxMicroseconds.ToString("F2", CultureInfo.InvariantCulture)} | "
                + $"{(kind is Kind.Full ? 200 : 1_000)} |");
        }

        Console.WriteLine();
    }

    private static LatencyObservation MeasureLatency(
        Kind kind,
        Action<IPricingConsumer, int> update,
        int samples,
        int warmup)
    {
        var consumer = Create(kind);
        consumer.ResetObservability();
        var counter = 0;

        for (var index = 0; index < warmup; index++)
        {
            counter++;
            update(consumer, counter);
            _ = Digest(consumer.Evaluate());
        }

        var values = new double[samples];

        for (var index = 0; index < samples; index++)
        {
            counter++;
            var start = Stopwatch.GetTimestamp();
            update(consumer, counter);
            _ = Digest(consumer.Evaluate());
            var end = Stopwatch.GetTimestamp();
            values[index] = (end - start) * 1_000_000.0 / Stopwatch.Frequency;
        }

        Array.Sort(values);
        Dispose(consumer);

        return new LatencyObservation(
            Percentile(values, 0.50),
            Percentile(values, 0.95),
            Percentile(values, 0.99),
            values[^1]);
    }

    private static double Percentile(double[] sortedValues, double percentile)
    {
        var index = (int)Math.Ceiling((sortedValues.Length * percentile) - 1);
        return sortedValues[Math.Clamp(index, 0, sortedValues.Length - 1)];
    }

    private static IPricingConsumer Create(Kind kind) =>
        kind switch
        {
            Kind.Full => new FullRecomputePricingConsumer(Products, Orders),
            Kind.Hand => new HandIncrementalPricingConsumer(Products, Orders),
            Kind.Graph => new IncrementalPricingConsumer(Products, Orders),
            Kind.Collection => new CollectionNodePricingConsumer(Products, Orders),
            Kind.Cache => new CacheAssistedPricingConsumer(Products, Orders),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };

    private static string NameOf(Kind kind) =>
        kind switch
        {
            Kind.Full => "Full recomputation",
            Kind.Hand => "Hand-maintained incremental",
            Kind.Graph => "Per-order incremental graph",
            Kind.Collection => "Collection-as-one-node graph",
            Kind.Cache => "MemoryCache product cache",
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };

    private static void Dispose(IPricingConsumer consumer)
    {
        if (consumer is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    private static int Digest(IReadOnlyList<Result<Quote, PricingError>> quotes)
    {
        var total = 0m;
        var failures = 0;

        for (var index = 0; index < quotes.Count; index++)
        {
            if (quotes[index].TryGetValue(out var quote))
            {
                total += quote.Total;
            }
            else
            {
                failures++;
            }
        }

        return total.GetHashCode() ^ failures;
    }
}