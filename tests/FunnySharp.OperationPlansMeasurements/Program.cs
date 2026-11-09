using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

using FunnySharp;
using FunnySharp.OperationPlans;
using FunnySharp.TestSupport;
using FunnySharp.TestSupport.OperationComparison;

using Microsoft.Data.Sqlite;

namespace FunnySharp.OperationPlansMeasurements;

internal static class Program
{
    private const int DatabaseIterations = 100;
    private const int DatabaseWarmup = 5;
    private const int MeasurementRounds = 3;

    private static readonly IOrderConsumer[] s_consumers =
    [
        new PlanOrderConsumer(),
        new DirectOrderConsumer(),
        new EfOrderConsumer(),
    ];

    private static readonly Scenario[] s_scenarios =
    [
        new("happy path", ShopFixtures.StandardRequest(), false, false, false, false, null, null),
        new("small order", ShopFixtures.SmallRequest(), false, false, false, false, null, null),
        new("credit limit", ShopFixtures.CreditLimitRequest(), false, false, false, false, RunFailureKind.OperationFailed, "check-credit"),
        new("stock shortfall", ShopFixtures.StockShortfallRequest(), false, false, false, false, RunFailureKind.OperationFailed, "reserve-inventory"),
        new("payment decline", ShopFixtures.StandardRequest(), true, false, false, false, RunFailureKind.OperationFailed, "authorize-payment"),
        new("outbox fault", ShopFixtures.StandardRequest(), false, true, false, false, RunFailureKind.OperationFaulted, "record-outbox"),
        new("cancel before start", ShopFixtures.StandardRequest(), false, false, true, false, RunFailureKind.Canceled, "load-customer"),
        new("cancel between steps", ShopFixtures.StandardRequest(), false, false, false, true, RunFailureKind.Canceled, "queue-review"),
    ];

    private static readonly PlanOrderConsumer s_planConsumer = new();

    private static async Task<int> Main()
    {
        try
        {
            Console.WriteLine("Goal 0042 typed operation plans measurement");
            Console.WriteLine($"started: {DateTimeOffset.UtcNow:O}");
            WriteEnvironment();

            var checks = await OperationComparison
                .VerifyAsync(CancellationToken.None)
                .ConfigureAwait(false);
            Console.WriteLine($"parity: verified {checks} checks across typed plan, direct await, and EF Core consumers.");
            Console.WriteLine();

            WriteWorkloadShape();
            WriteLineCounts();
            WriteInspection();
            await WriteWorkLedgerAsync().ConfigureAwait(false);
            await WriteDatabaseMeasurementsAsync().ConfigureAwait(false);

            Console.WriteLine($"finished: {DateTimeOffset.UtcNow:O}");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
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

        var tiered = AppContext.TryGetSwitch("System.Runtime.TieredCompilation", out var enabled) && enabled;
        Console.WriteLine($"tiered compilation: {tiered}");
        Console.WriteLine();
    }

    private static void WriteWorkloadShape()
    {
        Console.WriteLine("## Workload shape");
        Console.WriteLine();
        Console.WriteLine("schema tables: customers, catalog, inventory, orders, order_lines, payments, outbox, reviews");
        Console.WriteLine("operations: 9; request lines: 3; review threshold: 40000; gateway: simulated ledger");
        Console.WriteLine();
        Console.WriteLine("| Operation | Kind | DependsOn | RequiresPriorResult | DynamicWork | Conditional |");
        Console.WriteLine("| --- | --- | --- | --- | --- | --- |");

        foreach (var info in ComparisonDescriptors.Describe)
        {
            Console.WriteLine(
                $"| {info.Name} | {info.Kind} | {(info.DependsOn.Count == 0 ? "-" : string.Join(", ", info.DependsOn))} | {info.RequiresPriorResult} | {info.DynamicWork} | {info.Conditional} |");
        }

        Console.WriteLine();
    }

    private static void WriteLineCounts()
    {
        Console.WriteLine("## Consumer source volume (raw lines, braces and comments included)");
        Console.WriteLine();
        Console.WriteLine("| Artifact | Lines |");
        Console.WriteLine("| --- | ---: |");

        var root = TestRepositoryRoot.Find();

        if (root is null)
        {
            Console.WriteLine("| unavailable | - |");
            Console.WriteLine();
            return;
        }

        var comparison = Path.Combine(root, "tests/Shared/OperationComparison");
        var entries = new (string Name, string[] Paths)[]
        {
            ("Prototype core", [Path.Combine(root, "tests/FunnySharp.OperationPlans")]),
            (
                "Typed plan consumer",
                [
                    Path.Combine(comparison, "PlanOrderConsumer.cs"),
                    Path.Combine(comparison, "SqlitePlanTransaction.cs"),
                    Path.Combine(comparison, "OrderOperations.cs"),
                    Path.Combine(comparison, "ShopSql.cs"),
                ]),
            (
                "Direct await consumer",
                [
                    Path.Combine(comparison, "DirectOrderConsumer.cs"),
                    Path.Combine(comparison, "NamedStep.cs"),
                    Path.Combine(comparison, "ComparisonDescriptors.cs"),
                    Path.Combine(comparison, "OrderOperations.cs"),
                    Path.Combine(comparison, "ShopSql.cs"),
                ]),
            (
                "EF Core consumer",
                [
                    Path.Combine(comparison, "EfShopDbContext.cs"),
                    Path.Combine(comparison, "EfOrderConsumer.cs"),
                    Path.Combine(comparison, "NamedStep.cs"),
                    Path.Combine(comparison, "ComparisonDescriptors.cs"),
                ]),
            (
                "Shared workload and verifier",
                [
                    Path.Combine(comparison, "OrderModels.cs"),
                    Path.Combine(comparison, "OrderEnvironment.cs"),
                    Path.Combine(comparison, "OrderPricing.cs"),
                    Path.Combine(comparison, "ShopSchema.cs"),
                    Path.Combine(comparison, "ShopFixtures.cs"),
                    Path.Combine(comparison, "SimulatedPaymentGateway.cs"),
                    Path.Combine(comparison, "SqliteSandbox.cs"),
                    Path.Combine(comparison, "IOrderConsumer.cs"),
                    Path.Combine(comparison, "OperationComparison.cs"),
                ]),
            ("Tests", [Path.Combine(root, "tests/FunnySharp.OperationPlans.Tests")]),
            ("Measurement harness", [Path.Combine(root, "tests/FunnySharp.OperationPlansMeasurements")]),
        };

        foreach (var (name, paths) in entries)
        {
            Console.WriteLine($"| {name} | {CountLines(paths)} |");
        }

        Console.WriteLine();
        Console.WriteLine("The plan row includes the plan-only SQLite adapter and excludes the shared OrderEnvironment record, which is reported once in the shared row and charged to neither consumer. The plan and direct rows each include the shared raw SQL helpers they use; the direct and EF rows each include the shared named-step helper and descriptor array. Shared models, fixtures, schema, gateway, and verifier are reported once.");
        Console.WriteLine();
    }

    private static void WriteInspection()
    {
        Console.WriteLine("## Inspection and construction cost");
        Console.WriteLine();
        Console.WriteLine("A plan can be built once and inspected without executing any operation; the table separates construction from cached access. The direct descriptors are a static type-initialized array.");
        Console.WriteLine();
        Console.WriteLine("| Operation | Allocated B/op | Time ns/op |");
        Console.WriteLine("| --- | ---: | ---: |");

        var construction = MeasurementHarness.Measure(
            static () => PlanOrderConsumer.CreatePlan().Describe.Count,
            iterations: 1_000,
            warmupIterations: 100);
        var planDescribe = MeasurementHarness.Measure(
            static () => s_planConsumer.Describe.Count,
            iterations: 100_000,
            warmupIterations: 1_000);
        var directDescribe = MeasurementHarness.Measure(
            static () => ComparisonDescriptors.Describe.Count,
            iterations: 100_000,
            warmupIterations: 1_000);

        Console.WriteLine(
            $"| Plan build + Describe (9 steps) | {MeasurementHarness.Format(construction.BytesPerOperation)} | {MeasurementHarness.Format(construction.NanosecondsPerOperation)} |");
        Console.WriteLine(
            $"| Cached plan Describe | {MeasurementHarness.Format(planDescribe.BytesPerOperation)} | {MeasurementHarness.Format(planDescribe.NanosecondsPerOperation)} |");
        Console.WriteLine(
            $"| Static direct descriptor access | {MeasurementHarness.Format(directDescribe.BytesPerOperation)} | {MeasurementHarness.Format(directDescribe.NanosecondsPerOperation)} |");
        Console.WriteLine();
    }

    private static async Task WriteWorkLedgerAsync()
    {
        Console.WriteLine("## Work ledger and diagnostics");
        Console.WriteLine();
        Console.WriteLine("One complete operation per row on a fresh database. Completed is the number of operations that returned success before the outcome; Orders, Payments, Outbox, and Reviews are persisted row counts; Gateway is the external attempt count.");
        Console.WriteLine();
        Console.WriteLine("| Scenario | Consumer | Outcome | Operation | Completed | Orders | Payments | Outbox | Reviews | Gateway |");
        Console.WriteLine("| --- | --- | --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |");

        foreach (var scenario in s_scenarios)
        {
            foreach (var consumer in s_consumers)
            {
                await using var sandbox = await SqliteSandbox
                    .CreateAsync(cancellationToken: CancellationToken.None)
                    .ConfigureAwait(false);

                var gateway = new SimulatedPaymentGateway { DeclineNext = scenario.DeclinePayment };
                var faults = new FaultInjector { FailOutbox = scenario.FailOutbox };
                CancellationTokenSource? cancellation = null;
                var token = CancellationToken.None;

                if (scenario.CancelBeforeStart)
                {
                    cancellation = new CancellationTokenSource();
                    cancellation.Cancel();
                    token = cancellation.Token;
                }
                else if (scenario.CancelBetweenSteps)
                {
                    cancellation = new CancellationTokenSource();
                    faults.Cancellation = cancellation;
                    faults.CancelAtStep = "record-outbox";
                    token = cancellation.Token;
                }

                Result<OperationSuccess<OrderState>, RunFailure<OrderState, OrderError>> result;

                try
                {
                    result = await consumer
                        .PlaceOrderAsync(sandbox.Connection, scenario.Request, gateway, faults, token)
                        .ConfigureAwait(false);
                }
                finally
                {
                    cancellation?.Dispose();
                }

                CheckScenario(scenario, consumer, result);

                var outcome = result.TryGetValue(out _)
                    ? "success"
                    : result.TryGetError(out var failure)
                        ? failure.Kind.ToString()
                        : "invalid";
                var operation = result.TryGetError(out var failed)
                    ? failed.Operation ?? "<commit>"
                    : "-";
                var completed = result.TryGetValue(out var success)
                    ? success.Executed.Count
                    : result.TryGetError(out var failedFailure)
                        ? failedFailure.Completed.Count
                        : 0;

                Console.WriteLine(
                    $"| {scenario.Name} | {consumer.Name} | {outcome} | {operation} | {completed} | "
                    + $"{await CountAsync(sandbox.Connection, "orders").ConfigureAwait(false)} | "
                    + $"{await CountAsync(sandbox.Connection, "payments").ConfigureAwait(false)} | "
                    + $"{await CountAsync(sandbox.Connection, "outbox").ConfigureAwait(false)} | "
                    + $"{await CountAsync(sandbox.Connection, "reviews").ConfigureAwait(false)} | "
                    + $"{gateway.Attempts.Count} |");
            }
        }

        Console.WriteLine();
    }

    private static async Task WriteDatabaseMeasurementsAsync()
    {
        Console.WriteLine("## Complete operation allocation and directional time");
        Console.WriteLine();
        Console.WriteLine("The database is created and seeded before each measured operation and disposed after; its allocation and time are excluded. Setup and teardown are outside the snapshot. Allocation is the blocking comparison; timing is directional on this shared host.");
        Console.WriteLine();
        Console.WriteLine("| Scenario | Consumer | Allocated B/op | Time ns/op |");
        Console.WriteLine("| --- | --- | ---: | ---: |");

        foreach (var scenario in s_scenarios)
        {
            foreach (var consumer in s_consumers)
            {
                var measurement = await MeasureDatabaseAsync(consumer, scenario).ConfigureAwait(false);

                Console.WriteLine(
                    $"| {scenario.Name} | {consumer.Name} | {MeasurementHarness.Format(measurement.BytesPerOperation)} | {MeasurementHarness.Format(measurement.NanosecondsPerOperation)} |");
            }
        }

        Console.WriteLine();
    }

    private static async Task<Measurement> MeasureDatabaseAsync(IOrderConsumer consumer, Scenario scenario)
    {
        for (var warmup = 0; warmup < DatabaseWarmup; warmup++)
        {
            await using var sandbox = await SqliteSandbox
                .CreateAsync(cancellationToken: CancellationToken.None)
                .ConfigureAwait(false);
            await RunScenarioAsync(consumer, sandbox, scenario, CancellationToken.None).ConfigureAwait(false);
        }

        var bytesPerOperation = double.MaxValue;
        var nanosecondsPerOperation = double.MaxValue;

        for (var round = 0; round < MeasurementRounds; round++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long allocated = 0;
            long ticks = 0;
            var checksum = 0;

            for (var index = 0; index < DatabaseIterations; index++)
            {
                await using var sandbox = await SqliteSandbox
                    .CreateAsync(cancellationToken: CancellationToken.None)
                    .ConfigureAwait(false);
                var bytesBefore = GC.GetTotalAllocatedBytes(precise: true);
                var started = Stopwatch.GetTimestamp();

                checksum += Checksum(
                    await RunScenarioAsync(consumer, sandbox, scenario, CancellationToken.None)
                        .ConfigureAwait(false));

                ticks += Stopwatch.GetTimestamp() - started;
                allocated += GC.GetTotalAllocatedBytes(precise: true) - bytesBefore;
            }

            GC.KeepAlive(checksum);
            bytesPerOperation = Math.Min(bytesPerOperation, allocated / (double)DatabaseIterations);
            nanosecondsPerOperation = Math.Min(
                nanosecondsPerOperation,
                ticks * 1_000_000_000d / Stopwatch.Frequency / DatabaseIterations);
        }

        return new Measurement(bytesPerOperation, nanosecondsPerOperation);
    }

    private static async Task<Result<OperationSuccess<OrderState>, RunFailure<OrderState, OrderError>>> RunScenarioAsync(
        IOrderConsumer consumer,
        SqliteSandbox sandbox,
        Scenario scenario,
        CancellationToken cancellationToken)
    {
        var gateway = new SimulatedPaymentGateway { DeclineNext = scenario.DeclinePayment };
        var faults = new FaultInjector { FailOutbox = scenario.FailOutbox };
        CancellationTokenSource? cancellation = null;
        var token = cancellationToken;

        if (scenario.CancelBeforeStart)
        {
            cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            token = cancellation.Token;
        }
        else if (scenario.CancelBetweenSteps)
        {
            cancellation = new CancellationTokenSource();
            faults.Cancellation = cancellation;
            faults.CancelAtStep = "record-outbox";
            token = cancellation.Token;
        }

        try
        {
            return await consumer
                .PlaceOrderAsync(sandbox.Connection, scenario.Request, gateway, faults, token)
                .ConfigureAwait(false);
        }
        finally
        {
            cancellation?.Dispose();
        }
    }

    private static void CheckScenario(
        Scenario scenario,
        IOrderConsumer consumer,
        Result<OperationSuccess<OrderState>, RunFailure<OrderState, OrderError>> result)
    {
        if (scenario.ExpectedKind is null)
        {
            if (!result.IsSuccess)
            {
                throw new InvalidOperationException($"{scenario.Name}/{consumer.Name}: expected success.");
            }

            return;
        }

        if (!result.TryGetError(out var failure)
            || failure.Kind != scenario.ExpectedKind
            || !string.Equals(failure.Operation, scenario.ExpectedOperation, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"{scenario.Name}/{consumer.Name}: expected {scenario.ExpectedKind} at {scenario.ExpectedOperation}.");
        }
    }

    private static int Checksum(
        Result<OperationSuccess<OrderState>, RunFailure<OrderState, OrderError>> result)
    {
        if (result.TryGetValue(out var success))
        {
            return success.Executed.Count + (success.Skipped.Count * 10);
        }

        if (!result.TryGetError(out var failure))
        {
            throw new InvalidOperationException("Expected a success or failure result.");
        }

        return 100 + (int)failure.Kind + (failure.Completed.Count * 10);
    }

    private static Task<long> CountAsync(SqliteConnection connection, string table) =>
        ShopSql.ScalarLongAsync(connection, $"SELECT COUNT(*) FROM {table};", CancellationToken.None);

    private static int CountLines(string[] paths)
    {
        var total = 0;

        foreach (var path in paths)
        {
            total += Directory.Exists(path)
                ? Directory
                    .GetFiles(path, "*.cs", SearchOption.AllDirectories)
                    .Where(static file => !file.Contains(
                        $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}",
                        StringComparison.Ordinal))
                    .Where(static file => !file.Contains(
                        $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}",
                        StringComparison.Ordinal))
                    .Sum(static file => File.ReadAllLines(file).Length)
                : File.ReadAllLines(path).Length;
        }

        return total;
    }

    private sealed record Scenario(
        string Name,
        PlaceOrderRequest Request,
        bool DeclinePayment,
        bool FailOutbox,
        bool CancelBeforeStart,
        bool CancelBetweenSteps,
        RunFailureKind? ExpectedKind,
        string? ExpectedOperation);
}
