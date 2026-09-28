using FunnySharp.VerticalSlice.Domain;

namespace FunnySharp.VerticalSlice.Application;

/// <summary>
/// The single interpreter for the commands a pure transition emits. Policy: commands run in the
/// emitted order, one at a time; the first typed failure stops the rest and releases the inventory
/// reservations already made. The authoritative persist is emitted before customer-facing effects, so
/// an effect failure surfaces with the state already advanced (there is no distributed transaction here).
/// </summary>
public sealed class OrderCommandExecutor(
    IOrderStore store,
    IInventoryService inventory,
    IOrderEventPublisher publisher,
    ILogger<OrderCommandExecutor> logger)
{
    public async ValueTask<UnitResult<OrderError>> ExecuteAsync(
        IReadOnlyList<OrderCommand> commands,
        CancellationToken cancellationToken)
    {
        var applied = new List<OrderCommand>(commands.Count);
        foreach (var command in commands)
        {
            var outcome = await ExecuteAsync(command, cancellationToken);
            if (outcome.TryGetError(out var error))
            {
                await CompensateAsync(applied, cancellationToken);
                return UnitResult<OrderError>.Failure(error);
            }

            applied.Add(command);
        }

        return UnitResult<OrderError>.Success();
    }

    private async ValueTask<UnitResult<OrderError>> ExecuteAsync(
        OrderCommand command,
        CancellationToken cancellationToken) =>
        command switch
        {
            OrderCommand.ReserveInventory reserve =>
                await inventory.ReserveAsync(reserve.Sku, reserve.Quantity, cancellationToken),
            OrderCommand.ReleaseInventory release =>
                await inventory.ReleaseAsync(release.Sku, release.Quantity, cancellationToken),
            OrderCommand.Persist persist =>
                await store.PersistAsync(persist.Order, persist.Change, cancellationToken),
            OrderCommand.NotifyCustomer notify =>
                await publisher.NotifyCustomerAsync(notify.CustomerId, notify.OrderId, notify.Kind, cancellationToken),
            OrderCommand.PublishShipment shipment =>
                await publisher.PublishShipmentAsync(shipment.OrderId, shipment.TrackingCode, cancellationToken),
            _ => UnitResult<OrderError>.Failure(
                new OrderError.DependencyUnavailable("interpreter", "unknown command")),
        };

    /// <summary>
    /// Releases the reservations this command list already made, newest first. Best-effort: a failure
    /// while undoing is logged and the original error is still the outcome the caller sees.
    /// </summary>
    private async ValueTask CompensateAsync(
        IReadOnlyList<OrderCommand> applied,
        CancellationToken cancellationToken)
    {
        for (var index = applied.Count - 1; index >= 0; index--)
        {
            if (applied[index] is not OrderCommand.ReserveInventory reservation)
            {
                continue;
            }

            var outcome = await inventory.ReleaseAsync(reservation.Sku, reservation.Quantity, cancellationToken);
            if (outcome.TryGetError(out var error))
            {
                logger.LogError(
                    "Compensating a reservation of {Quantity} x {Sku} failed with {Failure}.",
                    reservation.Quantity.Value,
                    reservation.Sku.Value,
                    error.GetType().Name);
            }
        }
    }
}
