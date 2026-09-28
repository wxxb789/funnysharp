using FunnySharp.VerticalSlice.Domain;

namespace FunnySharp.VerticalSlice.Application;

/// <summary>
/// The single interpreter for the commands a pure transition emits. Policy: commands run in the
/// emitted order, one at a time; the first typed failure stops the rest, and a cancellation stops the
/// rest as well. Either way the inventory effects the earlier commands already applied are undone, so a
/// rejected or abandoned request strands no stock. Undoing runs with <see cref="CancellationToken.None"/>
/// because the request token may already be canceled. The authoritative persist is emitted before
/// customer-facing effects, so an effect failure surfaces with the state already advanced (there is no
/// distributed transaction here).
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
        try
        {
            foreach (var command in commands)
            {
                var outcome = await ExecuteAsync(command, cancellationToken);
                if (outcome.TryGetError(out var error))
                {
                    await UndoAsync(applied, "a later command failed");
                    return UnitResult<OrderError>.Failure(error);
                }

                applied.Add(command);
            }
        }
        catch (OperationCanceledException)
        {
            // A disconnect that lands between two commands must not strand what the earlier ones did.
            await UndoAsync(applied, "the request was canceled");
            throw;
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
    /// Reverses the inventory effects this command list already applied, newest first: a reservation is
    /// released again and a release is reserved again, so the world matches "this command list never
    /// ran". Best-effort: a failure while undoing is logged and the caller still sees the original
    /// outcome.
    /// </summary>
    private async ValueTask UndoAsync(IReadOnlyList<OrderCommand> applied, string reason)
    {
        for (var index = applied.Count - 1; index >= 0; index--)
        {
            var outcome = applied[index] switch
            {
                OrderCommand.ReserveInventory reservation =>
                    await inventory.ReleaseAsync(reservation.Sku, reservation.Quantity, CancellationToken.None),
                OrderCommand.ReleaseInventory release =>
                    await inventory.ReserveAsync(release.Sku, release.Quantity, CancellationToken.None),
                _ => UnitResult<OrderError>.Success(),
            };

            if (outcome.TryGetError(out var error))
            {
                logger.LogError(
                    "Undoing {Command} because {Reason} failed with {Failure}.",
                    applied[index].GetType().Name,
                    reason,
                    error.GetType().Name);
            }
        }
    }
}
