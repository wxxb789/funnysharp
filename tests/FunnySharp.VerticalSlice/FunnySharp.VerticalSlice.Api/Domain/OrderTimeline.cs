namespace FunnySharp.VerticalSlice.Domain;

/// <summary>One replayed lifecycle step of an order.</summary>
public readonly record struct TimelineRow(
    OrderEventKind Event,
    OrderStatus Status,
    DateTimeOffset OccurredAt,
    IReadOnlyList<OrderCommand> Commands);

/// <summary>
/// The projection recomputed from history, the steps that produced it, and whether replay still
/// agrees with the stored projection.
/// </summary>
public sealed record TimelineProjection(Order Recomputed, IReadOnlyList<TimelineRow> Rows, bool MatchesStored);

/// <summary>
/// Replays an order's history through the pure machine. Replay is how the service proves the stored
/// projection still follows from its events; it performs no I/O and reads no clock.
/// </summary>
public static class OrderTimeline
{
    public static Result<TimelineProjection, OrderError> Project(OrderRecord record)
    {
        var state = record.Order.AsDraft();
        var rows = new List<TimelineRow>(record.History.Count);
        foreach (var @event in record.History)
        {
            var plan = OrderLifecycle.Plan(state, @event);
            if (!plan.TryGetValue(out var planned))
            {
                _ = plan.TryGetError(out var cause);
                return Result<TimelineProjection, OrderError>.Failure(
                    new OrderError.HistoryDiverged(record.Order.Id, @event.Kind, cause!));
            }

            state = planned.Order;
            rows.Add(new TimelineRow(@event.Kind, state.Status, @event.OccurredAt, planned.Commands));
        }

        // Every field a lifecycle event can write takes part in the check, so a corrupted payment
        // reference, tracking code, or cancellation reason is reported as a divergence instead of a
        // clean replay.
        var matches = state.Status == record.Order.Status
            && state.Revision == record.Order.Revision
            && state.Total == record.Order.Total
            && state.UpdatedAt == record.Order.UpdatedAt
            && string.Equals(state.PaymentReference, record.Order.PaymentReference, StringComparison.Ordinal)
            && string.Equals(state.TrackingCode, record.Order.TrackingCode, StringComparison.Ordinal)
            && string.Equals(state.CancellationReason, record.Order.CancellationReason, StringComparison.Ordinal);
        return Result<TimelineProjection, OrderError>.Success(new TimelineProjection(state, rows, matches));
    }
}
