using FunnySharp.VerticalSlice.Domain;

namespace FunnySharp.VerticalSlice.Application;

/// <summary>
/// Coordinates the slice: async I/O, pricing, the pure lifecycle machine, and the command
/// interpreter. It returns carriers only; it never mentions an HTTP concept.
/// </summary>
public sealed class OrderService(
    IOrderStore store,
    IInventoryService inventory,
    SupplierQuoting quoting,
    IPaymentGateway payments,
    OrderCommandExecutor executor,
    TimeProvider clock,
    VerticalSliceOptions options)
{
    public ReconciliationEnvironment Environment => new(store, inventory, options.QuoteConcurrency);

    public ValueTask<Option<OrderRecord>> FindAsync(OrderId id, CancellationToken cancellationToken) =>
        store.FindAsync(id, cancellationToken);

    public async ValueTask<Result<Order, OrderError>> PlaceAsync(
        PlaceOrderCommand command,
        CancellationToken cancellationToken)
    {
        var sourced = await quoting.SourceAsync(command.Lines, cancellationToken);
        if (!sourced.TryGetValue(out var lines))
        {
            _ = sourced.TryGetError(out var sourcingError);
            return Result<Order, OrderError>.Failure(sourcingError!);
        }

        var now = clock.GetUtcNow();
        var draft = Order.Draft(OrderId.New(), command.CustomerId, lines, now);
        await store.CreateAsync(draft, cancellationToken);
        var placed = await ApplyResultAsync(draft, new OrderEvent.Place(now), cancellationToken);
        if (placed.TryGetError(out var placementError))
        {
            // A rejected placement must not leave the draft behind: its id was never handed to a caller,
            // so a stored draft would be unreachable state (and state the comparison app never keeps).
            await store.RemoveAsync(draft.Id, cancellationToken);
            return Result<Order, OrderError>.Failure(placementError);
        }

        return placed;
    }

    public async ValueTask<Result<Order, OrderError>> PayAsync(
        PayOrderCommand command,
        CancellationToken cancellationToken)
    {
        var found = await store.FindAsync(command.OrderId, cancellationToken);
        if (!found.TryGetValue(out var record))
        {
            return Result<Order, OrderError>.Failure(new OrderError.OrderNotFound(command.OrderId));
        }

        if (record.Order.Status != OrderStatus.Placed)
        {
            // The machine owns transition validity: an unpayable order is rejected with a typed code
            // instead of being decided by an ad-hoc check here.
            return await ApplyResultAsync(
                record.Order,
                new OrderEvent.Pay(PaymentAuthorization.Declined("not-requested"), record.Order.Total, clock.GetUtcNow()),
                cancellationToken);
        }

        var authorized = await payments.AuthorizeAsync(
            record.Order.Id,
            record.Order.Total,
            command.PaymentMethod,
            cancellationToken);
        if (!authorized.TryGetValue(out var authorization))
        {
            _ = authorized.TryGetError(out var gatewayError);
            return Result<Order, OrderError>.Failure(gatewayError!);
        }

        return await ApplyResultAsync(
            record.Order,
            new OrderEvent.Pay(authorization, record.Order.Total, clock.GetUtcNow()),
            cancellationToken);
    }

    public ValueTask<Result<Order, OrderError>> ShipAsync(
        ShipOrderCommand command,
        CancellationToken cancellationToken) =>
        ApplyResultAsync(
            command.OrderId,
            new OrderEvent.Ship(command.TrackingCode, clock.GetUtcNow()),
            cancellationToken);

    public async ValueTask<UnitResult<OrderError>> CancelAsync(
        CancelOrderCommand command,
        CancellationToken cancellationToken)
    {
        var planned = await ApplyResultAsync(
            command.OrderId,
            new OrderEvent.Cancel(command.Reason, clock.GetUtcNow()),
            cancellationToken);
        return planned.Match(
            static _ => UnitResult<OrderError>.Success(),
            static error => UnitResult<OrderError>.Failure(error));
    }

    public async ValueTask<Result<TimelineProjection, OrderError>> TimelineAsync(
        OrderId id,
        CancellationToken cancellationToken)
    {
        var found = await store.FindAsync(id, cancellationToken);
        if (!found.TryGetValue(out var record))
        {
            return Result<TimelineProjection, OrderError>.Failure(new OrderError.OrderNotFound(id));
        }

        return OrderTimeline.Project(record);
    }

    /// <summary>Builds the deferred reconciliation for one order; nothing runs until the boundary runs it.</summary>
    public Effect<ReconciliationEnvironment, Result<ReconciliationReport, OrderError>> Reconciliation(OrderId id) =>
        Effect.FromValueTask<ReconciliationEnvironment, Result<ReconciliationReport, OrderError>>(
            (environment, cancellationToken) => ReconcileAsync(environment, id, cancellationToken));

    private async ValueTask<Result<Order, OrderError>> ApplyResultAsync(
        OrderId id,
        OrderEvent @event,
        CancellationToken cancellationToken)
    {
        var found = await store.FindAsync(id, cancellationToken);
        if (!found.TryGetValue(out var record))
        {
            return Result<Order, OrderError>.Failure(new OrderError.OrderNotFound(id));
        }

        return await ApplyResultAsync(record.Order, @event, cancellationToken);
    }

    private async ValueTask<Result<Order, OrderError>> ApplyResultAsync(
        Order state,
        OrderEvent @event,
        CancellationToken cancellationToken)
    {
        var plan = OrderLifecycle.Plan(state, @event);
        if (!plan.TryGetValue(out var planned))
        {
            _ = plan.TryGetError(out var planningError);
            return Result<Order, OrderError>.Failure(planningError!);
        }

        var executed = await executor.ExecuteAsync(planned.Commands, cancellationToken);
        return executed.TryGetError(out var executionError)
            ? Result<Order, OrderError>.Failure(executionError)
            : Result<Order, OrderError>.Success(planned.Order);
    }

    private static async ValueTask<Result<ReconciliationReport, OrderError>> ReconcileAsync(
        ReconciliationEnvironment environment,
        OrderId id,
        CancellationToken cancellationToken)
    {
        var found = await environment.Store.FindAsync(id, cancellationToken);
        if (!found.TryGetValue(out var record))
        {
            return Result<ReconciliationReport, OrderError>.Failure(new OrderError.OrderNotFound(id));
        }

        var lines = record.Order.Lines.ToReadOnlyList();
        var availability = await lines.AsAsyncEnumerable(cancellationToken)
            .TraverseParallelValueAsync(
                environment.MaxConcurrency,
                (line, token) => environment.Inventory.AvailableAsync(line.Sku, token),
                cancellationToken);
        if (!availability.TryGetValue(out var available))
        {
            return Result<ReconciliationReport, OrderError>.Failure(
                new OrderError.DependencyUnavailable("inventory", "reconcile"));
        }

        var reconciled = new List<ReconciliationLine>(lines.Count);
        for (var index = 0; index < lines.Count; index++)
        {
            reconciled.Add(new ReconciliationLine(lines[index].Sku.Value, lines[index].Quantity.Value, available[index]));
        }

        var projected = OrderTimeline.Project(record);
        if (!projected.TryGetValue(out var timeline))
        {
            _ = projected.TryGetError(out var divergence);
            return Result<ReconciliationReport, OrderError>.Failure(divergence!);
        }

        return Result<ReconciliationReport, OrderError>.Success(
            new ReconciliationReport(
                record.Order.Id,
                record.Order.Status,
                record.Order.Revision,
                timeline.MatchesStored,
                reconciled));
    }
}
