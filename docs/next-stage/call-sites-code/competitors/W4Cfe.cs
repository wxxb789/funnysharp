using CSharpFunctionalExtensions;

namespace CallSites.Competitors;

// W4 competitor: CFE 3.7.0 UnitResult<E>.
public static class W4Cfe
{
    public static async Task<UnitResult<OrderError>> DeleteOrderAsync(
        string orderId,
        IOrderStore store,
        INotifier notifier,
        CancellationToken cancellationToken)
    {
        var existing = await store.FindAsync(orderId, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            return UnitResult.Failure<OrderError>(new CustomerNotFound(orderId));
        }

        await store.DeleteAsync(orderId, cancellationToken).ConfigureAwait(false);
        await notifier.NotifyAsync($"order {orderId} deleted", cancellationToken).ConfigureAwait(false);
        return UnitResult.Success<OrderError>();
    }
}
