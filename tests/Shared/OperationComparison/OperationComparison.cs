using FunnySharp;
using FunnySharp.OperationPlans;

using Microsoft.Data.Sqlite;

namespace FunnySharp.TestSupport.OperationComparison;

internal static class OperationComparison
{
    private static readonly string[] s_allNames =
    [
        "load-customer",
        "load-catalog",
        "create-order",
        "check-credit",
        "reserve-inventory",
        "authorize-payment",
        "record-payment",
        "record-outbox",
        "queue-review",
    ];

    private static int s_checks;

    internal static async Task<int> VerifyAsync(CancellationToken cancellationToken = default)
    {
        s_checks = 0;
        ComparisonDescriptors.Validate();
        VerifyMetadata();
        VerifyInspectionHasNoSideEffects();
        await VerifyHappyPathAsync(cancellationToken).ConfigureAwait(false);
        await VerifySmallOrderAsync(cancellationToken).ConfigureAwait(false);
        await VerifyCustomerFailuresAsync(cancellationToken).ConfigureAwait(false);
        await VerifyCatalogFailuresAsync(cancellationToken).ConfigureAwait(false);
        await VerifyCreditLimitAsync(cancellationToken).ConfigureAwait(false);
        await VerifyStockShortfallAsync(cancellationToken).ConfigureAwait(false);
        await VerifyPaymentDeclineAsync(cancellationToken).ConfigureAwait(false);
        await VerifyOutboxFaultAsync(cancellationToken).ConfigureAwait(false);
        await VerifyCancellationAsync(cancellationToken).ConfigureAwait(false);
        await VerifyTransactionOpenCancellationAsync(cancellationToken).ConfigureAwait(false);
        await VerifyCommitCancellationAsync(cancellationToken).ConfigureAwait(false);
        await VerifyCommitFailureMappingAsync(cancellationToken).ConfigureAwait(false);
        await VerifyCommitFailureAsync(cancellationToken).ConfigureAwait(false);
        await VerifyRollbackFailureAsync(cancellationToken).ConfigureAwait(false);
        await VerifyMultipleRunsAsync(cancellationToken).ConfigureAwait(false);
        return s_checks;
    }

    private static IEnumerable<IOrderConsumer> CreateConsumers() =>
        [new PlanOrderConsumer(), new DirectOrderConsumer(), new EfOrderConsumer()];

    private static void VerifyMetadata()
    {
        var expected = ComparisonDescriptors.Describe;

        foreach (var consumer in CreateConsumers())
        {
            var actual = consumer.Describe;

            CheckEqual(expected.Count, actual.Count, $"{consumer.Name}: descriptor count");

            for (var index = 0; index < Math.Min(expected.Count, actual.Count); index++)
            {
                var expectedInfo = expected[index];
                var actualInfo = actual[index];

                CheckEqual(expectedInfo.Name, actualInfo.Name, $"{consumer.Name}: descriptor {index} name");
                CheckEqual(expectedInfo.Kind, actualInfo.Kind, $"{consumer.Name}: descriptor {index} kind");
                CheckSequence(actualInfo.DependsOn, expectedInfo.DependsOn, $"{consumer.Name}: descriptor {index} dependencies");
                CheckEqual(expectedInfo.RequiresPriorResult, actualInfo.RequiresPriorResult, $"{consumer.Name}: descriptor {index} RequiresPriorResult");
                CheckEqual(expectedInfo.DynamicWork, actualInfo.DynamicWork, $"{consumer.Name}: descriptor {index} DynamicWork");
                CheckEqual(expectedInfo.Conditional, actualInfo.Conditional, $"{consumer.Name}: descriptor {index} Conditional");
                CheckEqual(expectedInfo.Note, actualInfo.Note, $"{consumer.Name}: descriptor {index} note");
            }
        }
    }

    private static void VerifyInspectionHasNoSideEffects()
    {
        var executions = 0;
        var plan = OperationPlan<object?, int, string>.Create(
        [
            new OperationStep<object?, int, string>(
                new OperationInfo(
                    "first",
                    OperationKind.Read,
                    Array.Empty<string>(),
                    requiresPriorResult: false,
                    dynamicWork: false,
                    conditional: false,
                    "inspection only"),
                (_, state, _) =>
                {
                    executions++;
                    return Task.FromResult(Result<int, string>.Success(state));
                }),
        ]);

        var description = plan.Describe;

        CheckEqual(0, executions, "Describe must not execute an operation");
        CheckEqual(1, description.Count, "Describe exposes the operation");
        CheckEqual("first", description[0].Name, "Describe exposes the name");
    }

    private static async Task VerifyHappyPathAsync(CancellationToken cancellationToken)
    {
        foreach (var consumer in CreateConsumers())
        {
            await using var sandbox = await SqliteSandbox.CreateAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            var gateway = new SimulatedPaymentGateway();
            var faults = new FaultInjector();

            var result = await consumer
                .PlaceOrderAsync(
                    sandbox.Connection,
                    ShopFixtures.StandardRequest(),
                    gateway,
                    faults,
                    cancellationToken)
                .ConfigureAwait(false);

            Check(result.IsSuccess, $"{consumer.Name}: happy path success");

            if (!result.TryGetValue(out var success))
            {
                continue;
            }

            CheckSequence(success.Executed, s_allNames, $"{consumer.Name}: happy path executed");
            CheckEqual(0, success.Skipped.Count, $"{consumer.Name}: happy path skipped");
            CheckEqual(1L, success.State.OrderId, $"{consumer.Name}: happy path order id");
            CheckEqual(40_000, success.State.TotalCents, $"{consumer.Name}: happy path total");
            CheckEqual("AUTH-1-40000", success.State.PaymentReference, $"{consumer.Name}: happy path reference");
            CheckEqual(3, success.State.Lines?.Count ?? 0, $"{consumer.Name}: happy path lines");
            CheckEqual(3, success.State.Catalog?.Count ?? 0, $"{consumer.Name}: happy path catalog");

            await CheckPersistedSuccessAsync(sandbox.Connection, 40_000, "AUTH-1-40000", cancellationToken)
                .ConfigureAwait(false);
            await CheckOrderLinesAsync(
                sandbox.Connection,
                [("SKU-A", 2, 10_000), ("SKU-B", 1, 15_000), ("SKU-C", 1, 5_000)],
                cancellationToken).ConfigureAwait(false);
            CheckEqual(1L, await CountAsync(sandbox.Connection, "reviews", cancellationToken).ConfigureAwait(false), $"{consumer.Name}: happy path review");
            CheckEqual("high-value", await ShopSql.ScalarStringAsync(sandbox.Connection, "SELECT reason FROM reviews;", cancellationToken).ConfigureAwait(false), $"{consumer.Name}: happy path review reason");
            await CheckInventoryAsync(sandbox.Connection, "SKU-A", 8, 2, cancellationToken).ConfigureAwait(false);
            await CheckInventoryAsync(sandbox.Connection, "SKU-B", 4, 1, cancellationToken).ConfigureAwait(false);
            await CheckInventoryAsync(sandbox.Connection, "SKU-C", 3, 1, cancellationToken).ConfigureAwait(false);
            CheckGateway(gateway, 1, authorized: true, "AUTH-1-40000", $"{consumer.Name}: happy path gateway");
        }
    }

    private static async Task VerifySmallOrderAsync(CancellationToken cancellationToken)
    {
        var expectedExecuted = s_allNames[..^1];

        foreach (var consumer in CreateConsumers())
        {
            await using var sandbox = await SqliteSandbox.CreateAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            var gateway = new SimulatedPaymentGateway();
            var faults = new FaultInjector();

            var result = await consumer
                .PlaceOrderAsync(
                    sandbox.Connection,
                    ShopFixtures.SmallRequest(),
                    gateway,
                    faults,
                    cancellationToken)
                .ConfigureAwait(false);

            Check(result.IsSuccess, $"{consumer.Name}: small order success");

            if (!result.TryGetValue(out var success))
            {
                continue;
            }

            CheckSequence(success.Executed, expectedExecuted, $"{consumer.Name}: small order executed");
            CheckSequence(success.Skipped, ["queue-review"], $"{consumer.Name}: small order skipped");
            CheckEqual(1L, success.State.OrderId, $"{consumer.Name}: small order order id");
            CheckEqual(10_000, success.State.TotalCents, $"{consumer.Name}: small order total");

            await CheckPersistedSuccessAsync(sandbox.Connection, 10_000, "AUTH-1-10000", cancellationToken)
                .ConfigureAwait(false);
            await CheckOrderLinesAsync(
                sandbox.Connection,
                [("SKU-A", 1, 10_000)],
                cancellationToken).ConfigureAwait(false);
            CheckEqual(0L, await CountAsync(sandbox.Connection, "reviews", cancellationToken).ConfigureAwait(false), $"{consumer.Name}: small order review");
            CheckEqual(1L, await CountAsync(sandbox.Connection, "order_lines", cancellationToken).ConfigureAwait(false), $"{consumer.Name}: small order lines");
            await CheckInventoryAsync(sandbox.Connection, "SKU-A", 9, 1, cancellationToken).ConfigureAwait(false);
            CheckGateway(gateway, 1, authorized: true, "AUTH-1-10000", $"{consumer.Name}: small order gateway");
        }
    }

    private static async Task VerifyCustomerFailuresAsync(CancellationToken cancellationToken)
    {
        foreach (var consumer in CreateConsumers())
        {
            await using (var sandbox = await SqliteSandbox.CreateAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false))
            {
                var gateway = new SimulatedPaymentGateway();
                var result = await consumer
                    .PlaceOrderAsync(
                        sandbox.Connection,
                        ShopFixtures.MissingCustomerRequest(),
                        gateway,
                        new FaultInjector(),
                        cancellationToken)
                    .ConfigureAwait(false);
                var failure = ExpectFailure(result, consumer.Name, RunFailureKind.OperationFailed, "load-customer");

                Check(failure.Error is OrderError.CustomerNotFound, $"{consumer.Name}: missing customer error");
                CheckEqual(0, failure.Completed.Count, $"{consumer.Name}: missing customer completed");
                CheckEqual(0L, failure.State.OrderId, $"{consumer.Name}: missing customer state");
                Check(failure.State.Customer is null, $"{consumer.Name}: missing customer state customer");
                await CheckNoWritesAsync(sandbox.Connection, cancellationToken).ConfigureAwait(false);
                await CheckSeedInventoryAsync(sandbox.Connection, cancellationToken).ConfigureAwait(false);
                CheckGateway(gateway, 0, authorized: false, null, $"{consumer.Name}: missing customer gateway");
            }

            await using (var sandbox = await SqliteSandbox.CreateAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false))
            {
                var gateway = new SimulatedPaymentGateway();
                var result = await consumer
                    .PlaceOrderAsync(
                        sandbox.Connection,
                        ShopFixtures.InactiveCustomerRequest(),
                        gateway,
                        new FaultInjector(),
                        cancellationToken)
                    .ConfigureAwait(false);
                var failure = ExpectFailure(result, consumer.Name, RunFailureKind.OperationFailed, "load-customer");

                Check(failure.Error is OrderError.CustomerInactive, $"{consumer.Name}: inactive customer error");
                CheckEqual(0, failure.Completed.Count, $"{consumer.Name}: inactive customer completed");
                await CheckNoWritesAsync(sandbox.Connection, cancellationToken).ConfigureAwait(false);
                CheckGateway(gateway, 0, authorized: false, null, $"{consumer.Name}: inactive customer gateway");
            }
        }
    }

    private static async Task VerifyCatalogFailuresAsync(CancellationToken cancellationToken)
    {
        foreach (var consumer in CreateConsumers())
        {
            await using (var sandbox = await SqliteSandbox.CreateAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false))
            {
                var gateway = new SimulatedPaymentGateway();
                var result = await consumer
                    .PlaceOrderAsync(
                        sandbox.Connection,
                        ShopFixtures.CatalogMissingRequest(),
                        gateway,
                        new FaultInjector(),
                        cancellationToken)
                    .ConfigureAwait(false);
                var failure = ExpectFailure(result, consumer.Name, RunFailureKind.OperationFailed, "load-catalog");

                Check(failure.Error is OrderError.CatalogItemNotFound, $"{consumer.Name}: missing catalog error");
                CheckSequence(failure.Completed, ["load-customer"], $"{consumer.Name}: missing catalog completed");
                Check(failure.State.Customer is not null, $"{consumer.Name}: missing catalog state customer");
                Check(failure.State.Catalog is null, $"{consumer.Name}: missing catalog state catalog");
                await CheckNoWritesAsync(sandbox.Connection, cancellationToken).ConfigureAwait(false);
                await CheckSeedInventoryAsync(sandbox.Connection, cancellationToken).ConfigureAwait(false);
                CheckGateway(gateway, 0, authorized: false, null, $"{consumer.Name}: missing catalog gateway");
            }

            await using (var sandbox = await SqliteSandbox.CreateAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false))
            {
                var gateway = new SimulatedPaymentGateway();
                var result = await consumer
                    .PlaceOrderAsync(
                        sandbox.Connection,
                        ShopFixtures.CatalogInactiveRequest(),
                        gateway,
                        new FaultInjector(),
                        cancellationToken)
                    .ConfigureAwait(false);
                var failure = ExpectFailure(result, consumer.Name, RunFailureKind.OperationFailed, "load-catalog");

                Check(failure.Error is OrderError.CatalogItemInactive, $"{consumer.Name}: inactive catalog error");
                CheckSequence(failure.Completed, ["load-customer"], $"{consumer.Name}: inactive catalog completed");
                await CheckNoWritesAsync(sandbox.Connection, cancellationToken).ConfigureAwait(false);
                CheckGateway(gateway, 0, authorized: false, null, $"{consumer.Name}: inactive catalog gateway");
            }
        }
    }

    private static async Task VerifyCreditLimitAsync(CancellationToken cancellationToken)
    {
        foreach (var consumer in CreateConsumers())
        {
            await using var sandbox = await SqliteSandbox.CreateAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            var gateway = new SimulatedPaymentGateway();
            var result = await consumer
                .PlaceOrderAsync(
                    sandbox.Connection,
                    ShopFixtures.CreditLimitRequest(),
                    gateway,
                    new FaultInjector(),
                    cancellationToken)
                .ConfigureAwait(false);
            var failure = ExpectFailure(result, consumer.Name, RunFailureKind.OperationFailed, "check-credit");

            Check(failure.Error is OrderError.CreditLimitExceeded, $"{consumer.Name}: credit limit error");
            CheckSequence(
                failure.Completed,
                ["load-customer", "load-catalog", "create-order"],
                $"{consumer.Name}: credit limit completed");
            CheckEqual(1L, failure.State.OrderId, $"{consumer.Name}: credit limit state order id");
            CheckEqual(40_000, failure.State.TotalCents, $"{consumer.Name}: credit limit state total");
            CheckEqual(3, failure.State.Lines?.Count ?? 0, $"{consumer.Name}: credit limit state lines");
            await CheckNoWritesAsync(sandbox.Connection, cancellationToken).ConfigureAwait(false);
            await CheckSeedInventoryAsync(sandbox.Connection, cancellationToken).ConfigureAwait(false);
            CheckGateway(gateway, 0, authorized: false, null, $"{consumer.Name}: credit limit gateway");
        }
    }

    private static async Task VerifyStockShortfallAsync(CancellationToken cancellationToken)
    {
        foreach (var consumer in CreateConsumers())
        {
            await using var sandbox = await SqliteSandbox.CreateAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            var gateway = new SimulatedPaymentGateway();
            var result = await consumer
                .PlaceOrderAsync(
                    sandbox.Connection,
                    ShopFixtures.StockShortfallRequest(),
                    gateway,
                    new FaultInjector(),
                    cancellationToken)
                .ConfigureAwait(false);
            var failure = ExpectFailure(result, consumer.Name, RunFailureKind.OperationFailed, "reserve-inventory");

            Check(failure.Error is OrderError.InsufficientStock, $"{consumer.Name}: stock error");

            if (failure.Error is OrderError.InsufficientStock stock)
            {
                CheckEqual("SKU-B", stock.Sku, $"{consumer.Name}: stock error sku");
                CheckEqual(6, stock.Requested, $"{consumer.Name}: stock error requested");
                CheckEqual(5, stock.Available, $"{consumer.Name}: stock error available");
            }

            CheckSequence(
                failure.Completed,
                ["load-customer", "load-catalog", "create-order", "check-credit"],
                $"{consumer.Name}: stock completed");
            CheckEqual(1L, failure.State.OrderId, $"{consumer.Name}: stock state order id");
            CheckEqual(100_000, failure.State.TotalCents, $"{consumer.Name}: stock state total");
            await CheckNoWritesAsync(sandbox.Connection, cancellationToken).ConfigureAwait(false);
            await CheckSeedInventoryAsync(sandbox.Connection, cancellationToken).ConfigureAwait(false);
            CheckGateway(gateway, 0, authorized: false, null, $"{consumer.Name}: stock gateway");
        }
    }

    private static async Task VerifyPaymentDeclineAsync(CancellationToken cancellationToken)
    {
        foreach (var consumer in CreateConsumers())
        {
            await using var sandbox = await SqliteSandbox.CreateAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            var gateway = new SimulatedPaymentGateway { DeclineNext = true };
            var result = await consumer
                .PlaceOrderAsync(
                    sandbox.Connection,
                    ShopFixtures.StandardRequest(),
                    gateway,
                    new FaultInjector(),
                    cancellationToken)
                .ConfigureAwait(false);
            var failure = ExpectFailure(result, consumer.Name, RunFailureKind.OperationFailed, "authorize-payment");

            Check(failure.Error is OrderError.PaymentDeclined, $"{consumer.Name}: payment decline error");
            CheckSequence(
                failure.Completed,
                ["load-customer", "load-catalog", "create-order", "check-credit", "reserve-inventory"],
                $"{consumer.Name}: payment decline completed");
            CheckEqual(1L, failure.State.OrderId, $"{consumer.Name}: payment decline state order id");
            CheckEqual(40_000, failure.State.TotalCents, $"{consumer.Name}: payment decline state total");
            Check(failure.State.PaymentReference is null, $"{consumer.Name}: payment decline state reference");
            await CheckNoWritesAsync(sandbox.Connection, cancellationToken).ConfigureAwait(false);
            await CheckSeedInventoryAsync(sandbox.Connection, cancellationToken).ConfigureAwait(false);
            CheckGateway(gateway, 1, authorized: false, null, $"{consumer.Name}: payment decline gateway");
        }
    }

    private static async Task VerifyOutboxFaultAsync(CancellationToken cancellationToken)
    {
        foreach (var consumer in CreateConsumers())
        {
            await using var sandbox = await SqliteSandbox.CreateAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            var gateway = new SimulatedPaymentGateway();
            var faults = new FaultInjector { FailOutbox = true };
            var result = await consumer
                .PlaceOrderAsync(
                    sandbox.Connection,
                    ShopFixtures.StandardRequest(),
                    gateway,
                    faults,
                    cancellationToken)
                .ConfigureAwait(false);
            var failure = ExpectFailure(result, consumer.Name, RunFailureKind.OperationFaulted, "record-outbox");

            Check(failure.Exception is InvalidOperationException, $"{consumer.Name}: outbox fault exception");
            CheckSequence(
                failure.Completed,
                ["load-customer", "load-catalog", "create-order", "check-credit", "reserve-inventory", "authorize-payment", "record-payment"],
                $"{consumer.Name}: outbox fault completed");
            CheckEqual(1L, failure.State.OrderId, $"{consumer.Name}: outbox fault state order id");
            CheckEqual(40_000, failure.State.TotalCents, $"{consumer.Name}: outbox fault state total");
            CheckEqual("AUTH-1-40000", failure.State.PaymentReference, $"{consumer.Name}: outbox fault state reference");
            await CheckNoWritesAsync(sandbox.Connection, cancellationToken).ConfigureAwait(false);
            await CheckSeedInventoryAsync(sandbox.Connection, cancellationToken).ConfigureAwait(false);
            CheckGateway(gateway, 1, authorized: true, "AUTH-1-40000", $"{consumer.Name}: outbox fault gateway");
        }
    }

    private static async Task VerifyCancellationAsync(CancellationToken cancellationToken)
    {
        foreach (var consumer in CreateConsumers())
        {
            await using (var sandbox = await SqliteSandbox.CreateAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false))
            {
                using var cts = new CancellationTokenSource();
                cts.Cancel();
                var gateway = new SimulatedPaymentGateway();
                var result = await consumer
                    .PlaceOrderAsync(
                        sandbox.Connection,
                        ShopFixtures.StandardRequest(),
                        gateway,
                        new FaultInjector { Cancellation = cts },
                        cts.Token)
                    .ConfigureAwait(false);
                var failure = ExpectFailure(result, consumer.Name, RunFailureKind.Canceled, "load-customer");

                CheckEqual(0, failure.Completed.Count, $"{consumer.Name}: pre-cancel completed");
                Check(failure.Exception is null, $"{consumer.Name}: pre-cancel exception");
                await CheckNoWritesAsync(sandbox.Connection, cancellationToken).ConfigureAwait(false);
                CheckGateway(gateway, 0, authorized: false, null, $"{consumer.Name}: pre-cancel gateway");
            }

            await using (var sandbox = await SqliteSandbox.CreateAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false))
            {
                using var cts = new CancellationTokenSource();
                var gateway = new SimulatedPaymentGateway();
                var faults = new FaultInjector { Cancellation = cts, CancelAtStep = "record-outbox" };
                var result = await consumer
                    .PlaceOrderAsync(
                        sandbox.Connection,
                        ShopFixtures.StandardRequest(),
                        gateway,
                        faults,
                        cts.Token)
                    .ConfigureAwait(false);
                var failure = ExpectFailure(result, consumer.Name, RunFailureKind.Canceled, "queue-review");

                CheckSequence(
                    failure.Completed,
                    ["load-customer", "load-catalog", "create-order", "check-credit", "reserve-inventory", "authorize-payment", "record-payment", "record-outbox"],
                    $"{consumer.Name}: between-step cancel completed");
                Check(failure.Exception is null, $"{consumer.Name}: between-step cancel exception");
                CheckEqual(1L, failure.State.OrderId, $"{consumer.Name}: between-step cancel state order id");
                await CheckNoWritesAsync(sandbox.Connection, cancellationToken).ConfigureAwait(false);
                CheckGateway(gateway, 1, authorized: true, "AUTH-1-40000", $"{consumer.Name}: between-step cancel gateway");
            }

            await using (var sandbox = await SqliteSandbox.CreateAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false))
            {
                using var cts = new CancellationTokenSource();
                var gateway = new SimulatedPaymentGateway();
                var faults = new FaultInjector { Cancellation = cts, CancelInsideStep = "reserve-inventory" };
                var result = await consumer
                    .PlaceOrderAsync(
                        sandbox.Connection,
                        ShopFixtures.StandardRequest(),
                        gateway,
                        faults,
                        cts.Token)
                    .ConfigureAwait(false);
                var failure = ExpectFailure(result, consumer.Name, RunFailureKind.Canceled, "reserve-inventory");

                CheckSequence(
                    failure.Completed,
                    ["load-customer", "load-catalog", "create-order", "check-credit"],
                    $"{consumer.Name}: inside-step cancel completed");
                Check(failure.Exception is OperationCanceledException, $"{consumer.Name}: inside-step cancel exception");
                CheckEqual(1L, failure.State.OrderId, $"{consumer.Name}: inside-step cancel state order id");
                await CheckNoWritesAsync(sandbox.Connection, cancellationToken).ConfigureAwait(false);
                await CheckSeedInventoryAsync(sandbox.Connection, cancellationToken).ConfigureAwait(false);
                CheckGateway(gateway, 0, authorized: false, null, $"{consumer.Name}: inside-step cancel gateway");
            }
        }
    }

    private static async Task VerifyTransactionOpenCancellationAsync(CancellationToken cancellationToken)
    {
        var consumer = new EfOrderConsumer();

        await using var sandbox = await SqliteSandbox.CreateAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        var gateway = new SimulatedPaymentGateway();
        var openException = new OperationCanceledException("transaction open canceled");
        var faults = new FaultInjector { TransactionOpenException = openException };

        var result = await consumer
            .PlaceOrderAsync(
                sandbox.Connection,
                ShopFixtures.StandardRequest(),
                gateway,
                faults,
                cancellationToken)
            .ConfigureAwait(false);
        var failure = ExpectFailure(result, consumer.Name, RunFailureKind.Canceled, "load-customer");

        Check(failure.OriginalKind is null, $"{consumer.Name}: transaction-open cancel original kind");
        Check(ReferenceEquals(openException, failure.Exception), $"{consumer.Name}: transaction-open cancel exception");
        CheckEqual(0, failure.Completed.Count, $"{consumer.Name}: transaction-open cancel completed");
        await CheckNoWritesAsync(sandbox.Connection, cancellationToken).ConfigureAwait(false);
        CheckGateway(gateway, 0, authorized: false, null, $"{consumer.Name}: transaction-open cancel gateway");
    }

    private static async Task VerifyCommitCancellationAsync(CancellationToken cancellationToken)
    {
        foreach (var consumer in CreateConsumers())
        {
            await using var sandbox = await SqliteSandbox.CreateAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            var gateway = new SimulatedPaymentGateway();
            var commitException = new OperationCanceledException("commit canceled");
            var faults = new FaultInjector { CommitException = commitException };

            var result = await consumer
                .PlaceOrderAsync(
                    sandbox.Connection,
                    ShopFixtures.StandardRequest(),
                    gateway,
                    faults,
                    cancellationToken)
                .ConfigureAwait(false);
            var failure = ExpectFailure(result, consumer.Name, RunFailureKind.Canceled, null);

            Check(failure.OriginalKind is null, $"{consumer.Name}: commit cancel original kind");
            Check(ReferenceEquals(commitException, failure.Exception), $"{consumer.Name}: commit cancel exception");
            Check(failure.Error is null, $"{consumer.Name}: commit cancel error");
            CheckSequence(failure.Completed, s_allNames, $"{consumer.Name}: commit cancel completed");
            CheckEqual(40_000, failure.State.TotalCents, $"{consumer.Name}: commit cancel state total");
            await CheckNoWritesAsync(sandbox.Connection, cancellationToken).ConfigureAwait(false);
            CheckGateway(gateway, 1, authorized: true, "AUTH-1-40000", $"{consumer.Name}: commit cancel gateway");
        }
    }

    private static async Task VerifyCommitFailureMappingAsync(CancellationToken cancellationToken)
    {
        foreach (var consumer in CreateConsumers())
        {
            await using var sandbox = await SqliteSandbox.CreateAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            var gateway = new SimulatedPaymentGateway();
            var commitException = new InvalidOperationException("commit");
            var faults = new FaultInjector { CommitException = commitException };

            var result = await consumer
                .PlaceOrderAsync(
                    sandbox.Connection,
                    ShopFixtures.StandardRequest(),
                    gateway,
                    faults,
                    cancellationToken)
                .ConfigureAwait(false);
            var failure = ExpectFailure(result, consumer.Name, RunFailureKind.CommitFailed, null);

            Check(failure.OriginalKind is null, $"{consumer.Name}: commit failure original kind");
            Check(ReferenceEquals(commitException, failure.Exception), $"{consumer.Name}: commit failure exception");
            Check(failure.Error is null, $"{consumer.Name}: commit failure error");
            CheckSequence(failure.Completed, s_allNames, $"{consumer.Name}: commit failure completed");
            CheckEqual(40_000, failure.State.TotalCents, $"{consumer.Name}: commit failure state total");
            await CheckNoWritesAsync(sandbox.Connection, cancellationToken).ConfigureAwait(false);
            CheckGateway(gateway, 1, authorized: true, "AUTH-1-40000", $"{consumer.Name}: commit failure gateway");
        }
    }

    private static async Task VerifyRollbackFailureAsync(CancellationToken cancellationToken)
    {
        foreach (var consumer in CreateConsumers())
        {
            var domainRollback = new InvalidOperationException("rollback");
            var domain = await RunRollbackFailureCaseAsync(
                consumer,
                ShopFixtures.StockShortfallRequest(),
                new FaultInjector { RollbackException = domainRollback },
                domainRollback,
                RunFailureKind.OperationFailed,
                "reserve-inventory",
                4,
                cancellationToken).ConfigureAwait(false);

            Check(domain.Error is OrderError.InsufficientStock, $"{consumer.Name}: rollback failure stock error");
            Check(domain.Exception is null, $"{consumer.Name}: rollback failure domain exception");
            CheckEqual(1L, domain.State.OrderId, $"{consumer.Name}: rollback failure domain order id");
            CheckEqual(100_000, domain.State.TotalCents, $"{consumer.Name}: rollback failure domain total");

            var callbackRollback = new InvalidOperationException("rollback");
            var callback = await RunRollbackFailureCaseAsync(
                consumer,
                ShopFixtures.StandardRequest(),
                new FaultInjector { FailOutbox = true, RollbackException = callbackRollback },
                callbackRollback,
                RunFailureKind.OperationFaulted,
                "record-outbox",
                7,
                cancellationToken).ConfigureAwait(false);

            Check(callback.Exception is InvalidOperationException, $"{consumer.Name}: rollback failure callback exception");
            Check(callback.Error is null, $"{consumer.Name}: rollback failure callback error");

            using var cancellation = new CancellationTokenSource();
            var cancellationRollback = new InvalidOperationException("rollback");
            var canceled = await RunRollbackFailureCaseAsync(
                consumer,
                ShopFixtures.StandardRequest(),
                new FaultInjector
                {
                    Cancellation = cancellation,
                    CancelInsideStep = "reserve-inventory",
                    RollbackException = cancellationRollback,
                },
                cancellationRollback,
                RunFailureKind.Canceled,
                "reserve-inventory",
                4,
                cancellationToken,
                cancellation.Token).ConfigureAwait(false);

            Check(canceled.Exception is OperationCanceledException, $"{consumer.Name}: rollback failure cancellation exception");
            Check(canceled.Error is null, $"{consumer.Name}: rollback failure cancellation error");

            var commitFailure = new InvalidOperationException("commit");
            var commitRollback = new InvalidOperationException("rollback");
            var commit = await RunRollbackFailureCaseAsync(
                consumer,
                ShopFixtures.StandardRequest(),
                new FaultInjector { CommitException = commitFailure, RollbackException = commitRollback },
                commitRollback,
                RunFailureKind.CommitFailed,
                null,
                9,
                cancellationToken).ConfigureAwait(false);

            Check(ReferenceEquals(commitFailure, commit.Exception), $"{consumer.Name}: rollback failure commit exception");
            Check(commit.Error is null, $"{consumer.Name}: rollback failure commit error");

            var commitCancellation = new OperationCanceledException("commit canceled");
            var commitCancelRollback = new InvalidOperationException("rollback");
            var commitCancel = await RunRollbackFailureCaseAsync(
                consumer,
                ShopFixtures.StandardRequest(),
                new FaultInjector
                {
                    CommitException = commitCancellation,
                    RollbackException = commitCancelRollback,
                },
                commitCancelRollback,
                RunFailureKind.Canceled,
                null,
                9,
                cancellationToken).ConfigureAwait(false);

            Check(ReferenceEquals(commitCancellation, commitCancel.Exception), $"{consumer.Name}: rollback failure commit cancel exception");
            Check(commitCancel.Error is null, $"{consumer.Name}: rollback failure commit cancel error");
        }
    }

    private static async Task<RunFailure<OrderState, OrderError>> RunRollbackFailureCaseAsync(
        IOrderConsumer consumer,
        PlaceOrderRequest request,
        FaultInjector faults,
        Exception rollbackException,
        RunFailureKind originalKind,
        string? operation,
        int completed,
        CancellationToken cancellationToken,
        CancellationToken callToken = default)
    {
        await using var sandbox = await SqliteSandbox.CreateAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        var gateway = new SimulatedPaymentGateway();
        var result = await consumer
            .PlaceOrderAsync(
                sandbox.Connection,
                request,
                gateway,
                faults,
                callToken)
            .ConfigureAwait(false);
        var failure = ExpectRollbackFailure(result, consumer.Name, operation);

        CheckEqual(originalKind, failure.OriginalKind, $"{consumer.Name}: rollback failure original kind");
        CheckEqual(completed, failure.Completed.Count, $"{consumer.Name}: rollback failure completed");
        Check(ReferenceEquals(rollbackException, failure.RollbackException), $"{consumer.Name}: rollback failure exception identity");
        await CheckNoWritesAsync(sandbox.Connection, cancellationToken).ConfigureAwait(false);

        return failure;
    }

    private static async Task VerifyCommitFailureAsync(CancellationToken cancellationToken)
    {
        await using var sandbox = await SqliteSandbox.CreateAsync(includeAuditLinks: true, cancellationToken)
            .ConfigureAwait(false);
        var transaction = sandbox.Connection.BeginTransaction(deferred: false);
        var faults = new FaultInjector();
        var environment = new OrderEnvironment(
            sandbox.Connection,
            transaction,
            new SimulatedPaymentGateway(),
            faults);
        var adapter = new SqlitePlanTransaction(transaction, faults);
        var plan = OperationPlan<OrderEnvironment, OrderState, OrderError>.Create(
        [
            new OperationStep<OrderEnvironment, OrderState, OrderError>(
                new OperationInfo(
                    "load-customer",
                    OperationKind.Read,
                    Array.Empty<string>(),
                    requiresPriorResult: false,
                    dynamicWork: false,
                    conditional: false,
                    "loads the customer"),
                OrderOperations.LoadCustomerAsync),
            new OperationStep<OrderEnvironment, OrderState, OrderError>(
                new OperationInfo(
                    "insert-invalid-audit",
                    OperationKind.Write,
                    new[] { "load-customer" },
                    requiresPriorResult: true,
                    dynamicWork: false,
                    conditional: false,
                    "inserts a deferred foreign-key link to a missing order"),
                InsertInvalidAuditAsync),
        ]);

        var result = await plan
            .RunAsync(
                environment,
                new OrderState(ShopFixtures.StandardRequest()),
                adapter,
                cancellationToken)
            .ConfigureAwait(false);
        var failure = ExpectFailure(result, "Typed plan", RunFailureKind.CommitFailed, null);

        Check(failure.Exception is SqliteException, "commit failure exception");
        CheckEqual(2, failure.Completed.Count, "commit failure completed");
        CheckEqual(0L, failure.State.OrderId, "commit failure state order id");
        await CheckNoWritesAsync(sandbox.Connection, cancellationToken).ConfigureAwait(false);
        CheckEqual(0L, await CountAsync(sandbox.Connection, "audit_links", cancellationToken).ConfigureAwait(false), "commit failure audit link");
        transaction.Dispose();
    }

    private static async Task VerifyMultipleRunsAsync(CancellationToken cancellationToken)
    {
        var consumer = new PlanOrderConsumer();

        await using (var first = await SqliteSandbox.CreateAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false))
        {
            var gateway = new SimulatedPaymentGateway();
            var result = await consumer
                .PlaceOrderAsync(first.Connection, ShopFixtures.StandardRequest(), gateway, new FaultInjector(), cancellationToken)
                .ConfigureAwait(false);
            Check(result.IsSuccess, "Typed plan: first run success");
            CheckEqual(1L, await CountAsync(first.Connection, "orders", cancellationToken).ConfigureAwait(false), "Typed plan: first run orders");
        }

        await using (var second = await SqliteSandbox.CreateAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false))
        {
            var gateway = new SimulatedPaymentGateway();
            var result = await consumer
                .PlaceOrderAsync(second.Connection, ShopFixtures.StandardRequest(), gateway, new FaultInjector(), cancellationToken)
                .ConfigureAwait(false);
            Check(result.IsSuccess, "Typed plan: second run success");
            CheckEqual(1L, await CountAsync(second.Connection, "orders", cancellationToken).ConfigureAwait(false), "Typed plan: second run orders");
        }

        CheckEqual(9, consumer.Describe.Count, "Typed plan: describe after runs");
    }

    private static async Task<Result<OrderState, OrderError>> InsertInvalidAuditAsync(
        OrderEnvironment environment,
        OrderState state,
        CancellationToken cancellationToken)
    {
        await ShopSql
            .InsertAuditLinkAsync(environment.Connection, environment.Transaction, 999, cancellationToken)
            .ConfigureAwait(false);
        return OrderStepResult.Success(state);
    }

    private static async Task CheckPersistedSuccessAsync(
        SqliteConnection connection,
        int totalCents,
        string reference,
        CancellationToken cancellationToken)
    {
        CheckEqual(1L, await CountAsync(connection, "orders", cancellationToken).ConfigureAwait(false), "persisted orders");
        CheckEqual(totalCents, await ShopSql.ScalarLongAsync(connection, "SELECT total_cents FROM orders;", cancellationToken).ConfigureAwait(false), "persisted total");
        CheckEqual(1L, await CountAsync(connection, "payments", cancellationToken).ConfigureAwait(false), "persisted payments");
        CheckEqual(totalCents, await ShopSql.ScalarLongAsync(connection, "SELECT amount_cents FROM payments;", cancellationToken).ConfigureAwait(false), "persisted payment amount");
        CheckEqual(reference, await ShopSql.ScalarStringAsync(connection, "SELECT reference FROM payments;", cancellationToken).ConfigureAwait(false), "persisted payment reference");
        CheckEqual(1L, await CountAsync(connection, "outbox", cancellationToken).ConfigureAwait(false), "persisted outbox");
        CheckEqual(ShopFixtures.OutboxTopic, await ShopSql.ScalarStringAsync(connection, "SELECT topic FROM outbox;", cancellationToken).ConfigureAwait(false), "persisted outbox topic");
        CheckEqual(
            $"{{\"orderId\":1,\"payment\":\"{reference}\",\"totalCents\":{totalCents}}}",
            await ShopSql.ScalarStringAsync(connection, "SELECT payload FROM outbox;", cancellationToken).ConfigureAwait(false),
            "persisted outbox payload");
    }

    private static async Task CheckOrderLinesAsync(
        SqliteConnection connection,
        IReadOnlyList<(string Sku, int Quantity, int UnitPriceCents)> expected,
        CancellationToken cancellationToken)
    {
        var actual = new List<(string Sku, int Quantity, int UnitPriceCents)>();

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT sku, quantity, unit_price_cents FROM order_lines ORDER BY id;";

            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                actual.Add((reader.GetString(0), reader.GetInt32(1), reader.GetInt32(2)));
            }
        }

        CheckEqual(expected.Count, actual.Count, "persisted order line count");

        for (var index = 0; index < Math.Min(expected.Count, actual.Count); index++)
        {
            CheckEqual(expected[index].Sku, actual[index].Sku, $"persisted order line {index} sku");
            CheckEqual(expected[index].Quantity, actual[index].Quantity, $"persisted order line {index} quantity");
            CheckEqual(expected[index].UnitPriceCents, actual[index].UnitPriceCents, $"persisted order line {index} price");
        }
    }

    private static async Task CheckNoWritesAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        CheckEqual(0L, await CountAsync(connection, "orders", cancellationToken).ConfigureAwait(false), "rolled-back orders");
        CheckEqual(0L, await CountAsync(connection, "order_lines", cancellationToken).ConfigureAwait(false), "rolled-back order lines");
        CheckEqual(0L, await CountAsync(connection, "payments", cancellationToken).ConfigureAwait(false), "rolled-back payments");
        CheckEqual(0L, await CountAsync(connection, "outbox", cancellationToken).ConfigureAwait(false), "rolled-back outbox");
        CheckEqual(0L, await CountAsync(connection, "reviews", cancellationToken).ConfigureAwait(false), "rolled-back reviews");
    }

    private static async Task CheckSeedInventoryAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        await CheckInventoryAsync(connection, "SKU-A", 10, 0, cancellationToken).ConfigureAwait(false);
        await CheckInventoryAsync(connection, "SKU-B", 5, 0, cancellationToken).ConfigureAwait(false);
        await CheckInventoryAsync(connection, "SKU-C", 4, 0, cancellationToken).ConfigureAwait(false);
        await CheckInventoryAsync(connection, "SKU-OFF", 10, 0, cancellationToken).ConfigureAwait(false);
    }

    private static async Task CheckInventoryAsync(
        SqliteConnection connection,
        string sku,
        int available,
        int reserved,
        CancellationToken cancellationToken)
    {
        var inventory = await ShopSql
            .GetInventoryAsync(connection, sku, cancellationToken)
            .ConfigureAwait(false);
        CheckEqual(available, inventory.Available, $"{sku} available");
        CheckEqual(reserved, inventory.Reserved, $"{sku} reserved");
    }

    private static Task<long> CountAsync(
        SqliteConnection connection,
        string table,
        CancellationToken cancellationToken) =>
        ShopSql.ScalarLongAsync(connection, $"SELECT COUNT(*) FROM {table};", cancellationToken);

    private static RunFailure<OrderState, OrderError> ExpectFailure(
        Result<OperationSuccess<OrderState>, RunFailure<OrderState, OrderError>> result,
        string consumer,
        RunFailureKind kind,
        string? operation)
    {
        Check(result.IsFailure, $"{consumer}: expected failure");

        if (!result.TryGetError(out var failure))
        {
            throw new InvalidOperationException($"{consumer}: expected a failed result.");
        }

        CheckEqual(kind, failure.Kind, $"{consumer}: failure kind");
        CheckEqual(operation, failure.Operation, $"{consumer}: responsible operation");
        Check(failure.RollbackException is null, $"{consumer}: rollback exception");

        return failure;
    }

    private static RunFailure<OrderState, OrderError> ExpectRollbackFailure(
        Result<OperationSuccess<OrderState>, RunFailure<OrderState, OrderError>> result,
        string consumer,
        string? operation)
    {
        Check(result.IsFailure, $"{consumer}: expected failure");

        if (!result.TryGetError(out var failure))
        {
            throw new InvalidOperationException($"{consumer}: expected a failed result.");
        }

        CheckEqual(RunFailureKind.RollbackFailed, failure.Kind, $"{consumer}: failure kind");
        CheckEqual(operation, failure.Operation, $"{consumer}: responsible operation");
        Check(failure.RollbackException is not null, $"{consumer}: rollback exception");

        return failure;
    }

    private static void CheckGateway(
        SimulatedPaymentGateway gateway,
        int attempts,
        bool authorized,
        string? reference,
        string message)
    {
        CheckEqual(attempts, gateway.Attempts.Count, $"{message}: attempts");

        if (gateway.Attempts.Count == 0)
        {
            return;
        }

        CheckEqual(authorized, gateway.Attempts[0].Authorized, $"{message}: authorized");
        CheckEqual(reference, gateway.Attempts[0].Reference, $"{message}: reference");
    }

    private static void CheckSequence(
        IReadOnlyList<string> actual,
        IReadOnlyList<string> expected,
        string message)
    {
        CheckEqual(expected.Count, actual.Count, $"{message}: count");

        for (var index = 0; index < Math.Min(actual.Count, expected.Count); index++)
        {
            CheckEqual(expected[index], actual[index], $"{message}: item {index}");
        }
    }

    private static void Check(bool condition, string message)
    {
        s_checks++;

        if (!condition)
        {
            throw new InvalidOperationException($"Comparison check failed: {message}.");
        }
    }

    private static void CheckEqual<T>(T expected, T actual, string message)
    {
        s_checks++;

        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException(
                $"Comparison check failed: {message}. Expected '{expected}', actual '{actual}'.");
        }
    }
}
