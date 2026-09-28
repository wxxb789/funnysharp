namespace FunnySharp.VerticalSlice.Domain;

/// <summary>
/// The order projection. It is built only from refined values, and its revision is part of the
/// state so optimistic concurrency stays a pure transition property.
/// </summary>
public sealed record Order
{
    public required OrderId Id { get; init; }

    public required CustomerId CustomerId { get; init; }

    public required NonEmpty<OrderLine> Lines { get; init; }

    public required Money Total { get; init; }

    public required OrderStatus Status { get; init; }

    public required int Revision { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }

    public required DateTimeOffset UpdatedAt { get; init; }

    public string? PaymentReference { get; init; }

    public string? TrackingCode { get; init; }

    public string? CancellationReason { get; init; }

    public static Order Draft(
        OrderId id,
        CustomerId customerId,
        NonEmpty<OrderLine> lines,
        DateTimeOffset createdAt) =>
        new()
        {
            Id = id,
            CustomerId = customerId,
            Lines = lines,
            Total = TotalOf(lines),
            Status = OrderStatus.Draft,
            Revision = 0,
            CreatedAt = createdAt,
            UpdatedAt = createdAt,
        };

    /// <summary>Returns this order as it was before any lifecycle event was applied.</summary>
    public Order AsDraft() => this with
    {
        Status = OrderStatus.Draft,
        Revision = 0,
        UpdatedAt = CreatedAt,
        PaymentReference = null,
        TrackingCode = null,
        CancellationReason = null,
    };

    public static Money TotalOf(NonEmpty<OrderLine> lines)
    {
        var total = Money.Zero(lines.First.UnitPrice.Currency).Add(lines.First.LineTotal);
        foreach (var line in lines.Rest)
        {
            total = total.Add(line.LineTotal);
        }

        return total;
    }
}
