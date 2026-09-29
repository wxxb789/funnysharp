using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using FunnySharp.VerticalSlice.Application;
using FunnySharp.VerticalSlice.Baseline;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace FunnySharp.VerticalSlice.Measurements;

/// <summary>
/// End-to-end measurement and equivalence harness for Goal 22. It hosts the carrier slice and the
/// idiomatic-C# comparison on the same in-process server, asserts that both answer the same requests
/// with the same status and the same payload (modulo identity and timestamps), and then measures
/// allocated bytes, latency, and throughput per scenario. Numbers here are application end-to-end
/// observations, never a release claim about the mapping helpers.
/// </summary>
public static class Program
{
    public const string VerifyMarker = "FunnySharp vertical slice measurements ready.";

    private const int WarmupIterations = 20;
    private const int MeasuredIterations = 200;
    private const int StreamWarmupIterations = 3;
    private const int StreamMeasuredIterations = 15;
    private const int SeededOrders = 20;

    public static async Task<int> Main(string[] args)
    {
        if (args is ["--verify"])
        {
            Console.WriteLine(VerifyMarker);
            return 0;
        }

        var output = "artifacts/vertical-slice/measurements/measurements.json";
        if (args is ["--output", var requested])
        {
            output = requested;
        }

        await using var slice = await HostedApp.StartSliceAsync();
        await using var baseline = await HostedApp.StartBaselineAsync();
        await slice.SeedAsync(SeededOrders);
        await baseline.SeedAsync(SeededOrders);

        var scenarios = new (string Name, bool Seeded, bool Stream)[]
        {
            ("place-order-valid", false, false),
            ("place-order-invalid", false, false),
            ("get-order-found", true, false),
            ("get-order-missing", false, false),
            ("pay-order-declined", true, false),
            ("cancel-order", false, false),
            ("quote-fanout", false, false),
            ("quote-unavailable", false, false),
            ("reconcile-order", true, false),
            ("export-stream", true, true),
        };

        var results = new List<ScenarioResult>();
        foreach (var scenario in scenarios)
        {
            var sliceRun = await MeasureAsync(slice, scenario.Name, scenario.Stream);
            var baselineRun = await MeasureAsync(baseline, scenario.Name, scenario.Stream);
            results.Add(new ScenarioResult(scenario.Name, sliceRun, baselineRun, Equivalence.Same(sliceRun, baselineRun)));
        }

        var receipt = new
        {
            schemaVersion = 1,
            objective = "docs/goals/archive/0022-goal.md",
            measuredAtUtc = DateTimeOffset.UtcNow.ToString("O"),
            runtime = Environment.Version.ToString(),
            framework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
            processArchitecture = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString(),
            warmupIterations = WarmupIterations,
            measuredIterations = MeasuredIterations,
            streamWarmupIterations = StreamWarmupIterations,
            streamMeasuredIterations = StreamMeasuredIterations,
            seededOrders = SeededOrders,
            note = "Application end-to-end observations through TestServer; not a release claim about the HTTP mapping helpers.",
            scenarios = results,
        };

        var path = Path.GetFullPath(output);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(receipt, new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true }) + "\n");

        Console.WriteLine($"{"scenario",-22}{"slice alloc",-14}{"base alloc",-14}{"slice p50 us",-14}{"base p50 us",-14}{"slice ops/s",-13}{"base ops/s",-13}equivalent");
        foreach (var result in results)
        {
            Console.WriteLine(
                $"{result.Name,-22}{result.Slice.AllocatedBytesPerOperation,-14}{result.Baseline.AllocatedBytesPerOperation,-14}" +
                $"{result.Slice.P50Microseconds,-14:0.0}{result.Baseline.P50Microseconds,-14:0.0}" +
                $"{result.Slice.OperationsPerSecond,-13:0}{result.Baseline.OperationsPerSecond,-13:0}{result.Equivalent}");
        }

        Console.WriteLine($"receipt: {path}");
        var mismatches = results.Where(static result => !result.Equivalent).ToArray();
        if (mismatches.Length > 0)
        {
            foreach (var mismatch in mismatches)
            {
                Console.Error.WriteLine($"differ: {mismatch.Name}");
                Console.Error.WriteLine($"  slice    ({(int)mismatch.Slice.StatusCode}): {Truncate(mismatch.Slice.CanonicalResponse)}");
                Console.Error.WriteLine($"  baseline ({(int)mismatch.Baseline.StatusCode}): {Truncate(mismatch.Baseline.CanonicalResponse)}");
            }

            Console.Error.WriteLine($"error: behaviors differ for: {string.Join(", ", mismatches.Select(static mismatch => mismatch.Name))}");
            return 1;
        }

        return 0;
    }

    private static string Truncate(string value) =>
        value.Length <= 600 ? value : string.Concat(value.AsSpan(0, 600), "...");

    private static async Task<Measurement> MeasureAsync(HostedApp app, string scenario, bool stream)
    {
        var warmup = stream ? StreamWarmupIterations : WarmupIterations;
        var iterations = stream ? StreamMeasuredIterations : MeasuredIterations;

        string? canonical = null;
        var status = 0;
        for (var index = 0; index < warmup; index++)
        {
            var (response, body) = await app.SendAsync(scenario, index);
            canonical ??= Canonicalize(body);
            status = (int)response;
        }

        var latencies = new double[iterations];
        var before = GC.GetTotalAllocatedBytes(precise: true);
        var stopwatch = Stopwatch.StartNew();
        for (var index = 0; index < iterations; index++)
        {
            var started = Stopwatch.GetTimestamp();
            var (response, body) = await app.SendAsync(scenario, warmup + index);
            latencies[index] = Stopwatch.GetElapsedTime(started).TotalMicroseconds;
            status = (int)response;
            if (index == 0)
            {
                canonical ??= Canonicalize(body);
            }
        }

        stopwatch.Stop();
        var allocated = GC.GetTotalAllocatedBytes(precise: true) - before;
        Array.Sort(latencies);

        return new Measurement(
            status,
            (long)(allocated / (double)iterations),
            latencies.Average(),
            latencies[latencies.Length / 2],
            latencies[(int)(latencies.Length * 0.95)],
            iterations / stopwatch.Elapsed.TotalSeconds,
            canonical ?? string.Empty);
    }

    /// <summary>
    /// Normalizes a response so the two applications can be compared field by field. A body may be a
    /// single JSON document, an NDJSON stream, or empty (204), so it is canonicalized line by line.
    /// </summary>
    private static string Canonicalize(string body)
    {
        var lines = body.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (lines.Length == 0)
        {
            return "<empty>";
        }

        var canonical = new List<string>(lines.Length);
        foreach (var line in lines)
        {
            using var document = JsonDocument.Parse(line);
            var builder = new StringBuilder();
            Append(document.RootElement, builder);
            canonical.Add(builder.ToString());
        }

        return string.Join("|", canonical);
    }

    private static void Append(JsonElement element, StringBuilder builder)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                builder.Append('{');
                var first = true;
                foreach (var property in element.EnumerateObject().OrderBy(static property => property.Name, StringComparer.Ordinal))
                {
                    if (!first)
                    {
                        builder.Append(',');
                    }

                    first = false;
                    builder.Append(property.Name).Append(':');
                    if (IdentityKeys.Contains(property.Name))
                    {
                        builder.Append("\"<id>\"");
                    }
                    else
                    {
                        Append(property.Value, builder);
                    }
                }

                builder.Append('}');
                break;
            case JsonValueKind.Array:
                builder.Append('[');
                var firstItem = true;
                foreach (var item in element.EnumerateArray())
                {
                    if (!firstItem)
                    {
                        builder.Append(',');
                    }

                    firstItem = false;
                    Append(item, builder);
                }

                builder.Append(']');
                break;
            case JsonValueKind.String:
                builder.Append('"').Append(element.GetString() ?? string.Empty).Append('"');
                break;
            default:
                builder.Append(element.GetRawText());
                break;
        }
    }

    /// <summary>Fields whose values are per-run identity, time, or a trace correlation.</summary>
    private static readonly HashSet<string> IdentityKeys = new(StringComparer.Ordinal)
    {
        "orderId",
        "paymentReference",
        "createdAt",
        "updatedAt",
        "occurredAt",
        "traceId",
    };
}

/// <summary>
/// The one rule that decides whether the two applications behaved the same for a scenario. Both axes
/// count: a body that matches while the status code differs is a behavioral difference, not a pass.
/// </summary>
public static class Equivalence
{
    public static bool Same(Measurement slice, Measurement baseline) =>
        slice.StatusCode == baseline.StatusCode
        && string.Equals(slice.CanonicalResponse, baseline.CanonicalResponse, StringComparison.Ordinal);
}

public sealed record Measurement(
    int StatusCode,
    long AllocatedBytesPerOperation,
    double MeanMicroseconds,
    double P50Microseconds,
    double P95Microseconds,
    double OperationsPerSecond,
    string CanonicalResponse);

public sealed record ScenarioResult(string Name, Measurement Slice, Measurement Baseline, bool Equivalent);

/// <summary>One hosted application with the scenario requests the harness sends to both apps.</summary>
internal sealed class HostedApp : IAsyncDisposable
{
    private readonly WebApplication app;

    private HostedApp(WebApplication app, HttpClient client)
    {
        this.app = app;
        Client = client;
    }

    private HttpClient Client { get; }

    private int OrderSequence { get; set; }

    internal static async Task<HostedApp> StartSliceAsync()
    {
        var options = new VerticalSliceOptions
        {
            StoreLatency = TimeSpan.Zero,
            DependencyLatency = TimeSpan.Zero,
            PaymentLatency = TimeSpan.Zero,
            SupplierLatency = TimeSpan.Zero,
            SupplierCount = 4,
            QuoteConcurrency = 2,
            Currency = "EUR",
        };
        var app = VerticalSliceApp.Build(
            [],
            services => services.AddSingleton(options),
            host => host.UseTestServer());
        await app.StartAsync();
        return new HostedApp(app, app.GetTestClient());
    }

    internal static async Task<HostedApp> StartBaselineAsync()
    {
        var options = new BaselineOptions { SupplierCount = 4, QuoteConcurrency = 2, Currency = "EUR" };
        var app = BaselineApp.Build(
            [],
            services => services.AddSingleton(options),
            host => host.UseTestServer());
        await app.StartAsync();
        return new HostedApp(app, app.GetTestClient());
    }

    internal async Task SeedAsync(int count)
    {
        for (var index = 0; index < count; index++)
        {
            var (status, _) = await SendRawAsync(
                HttpMethod.Post,
                "/orders",
                $$"""{"customerId":"customer-42","lines":[{"sku":"SKU-{{index % 20:000}}","quantity":1}]}""");
            if (status != HttpStatusCode.Created)
            {
                throw new InvalidOperationException($"Seeding failed with {(int)status}.");
            }

            OrderSequence++;
        }
    }

    internal async Task<(HttpStatusCode Status, string Body)> SendAsync(string scenario, int index)
    {
        // Every measured iteration uses a SKU that is reserved at most once, so neither application
        // can run out of the simulated stock while the other still succeeds.
        var sku = $"SKU-{500 + (index % 400):000}";
        return scenario switch
        {
            "place-order-valid" => await SendRawAsync(
                HttpMethod.Post,
                "/orders",
                $$"""{"customerId":"customer-42","lines":[{"sku":"{{sku}}","quantity":1}]}"""),
            "place-order-invalid" => await SendRawAsync(
                HttpMethod.Post,
                "/orders",
                """{"customerId":"x","lines":[{"sku":"bad sku!","quantity":0},{"sku":"SKU-BOOK","quantity":99}]}"""),
            "get-order-found" => await SendRawAsync(HttpMethod.Get, $"/orders/ORD-{OrderSequence}"),
            "get-order-missing" => await SendRawAsync(HttpMethod.Get, "/orders/ORD-999999"),
            "pay-order-declined" => await SendRawAsync(
                HttpMethod.Post,
                $"/orders/ORD-{OrderSequence}/payments",
                """{"paymentMethod":"card-declined"}"""),
            "cancel-order" => await CancelAsync(sku),
            "quote-fanout" => await SendRawAsync(HttpMethod.Get, $"/suppliers/{sku}/quotes?quantity=1"),
            "quote-unavailable" => await SendRawAsync(HttpMethod.Get, "/suppliers/SKU-UNLISTED/quotes?quantity=1"),
            "reconcile-order" => await SendRawAsync(HttpMethod.Get, $"/orders/ORD-{OrderSequence}/reconcile"),
            "export-stream" => await SendRawAsync(HttpMethod.Get, "/orders/export"),
            _ => throw new ArgumentOutOfRangeException(nameof(scenario), scenario, "Unknown scenario."),
        };
    }

    private async Task<(HttpStatusCode Status, string Body)> CancelAsync(string sku)
    {
        var (placed, body) = await SendRawAsync(
            HttpMethod.Post,
            "/orders",
            $$"""{"customerId":"customer-42","lines":[{"sku":"{{sku}}","quantity":1}]}""");
        if (placed != HttpStatusCode.Created)
        {
            return (placed, body);
        }

        using var document = JsonDocument.Parse(body);
        var orderId = document.RootElement.GetProperty("orderId").GetString();
        return await SendRawAsync(
            HttpMethod.Post,
            $"/orders/{orderId}/cancellation",
            """{"reason":"customer changed their mind"}""");
    }

    private async Task<(HttpStatusCode Status, string Body)> SendRawAsync(HttpMethod method, string path, string? json = null)
    {
        using var request = new HttpRequestMessage(method, path);
        if (json is not null)
        {
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        using var response = await Client.SendAsync(request);
        return (response.StatusCode, await response.Content.ReadAsStringAsync());
    }

    public async ValueTask DisposeAsync()
    {
        await app.StopAsync();
        await app.DisposeAsync();
    }
}
