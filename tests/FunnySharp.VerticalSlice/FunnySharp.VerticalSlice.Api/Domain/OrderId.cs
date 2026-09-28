namespace FunnySharp.VerticalSlice.Domain;

/// <summary>
/// A refined order identifier. Instances exist only through <see cref="Create"/> or
/// <see cref="New"/>, so any <see cref="OrderId"/> a handler receives is already validated.
/// </summary>
public readonly record struct OrderId
{
    private static int sequence;

    private readonly string? value;

    private OrderId(string value) => this.value = value;

    public string Value => value ?? throw new InvalidOperationException("The order id is uninitialized.");

    public static OrderId New() => new($"ORD-{Interlocked.Increment(ref sequence)}");

    public static Result<OrderId, InputError> Create(string? candidate)
    {
        if (!IsWellFormed(candidate))
        {
            return Result<OrderId, InputError>.Failure(
                new InputError("orderId", "format", "An order id must look like 'ORD-123'."));
        }

        return Result<OrderId, InputError>.Success(new OrderId(candidate!));
    }

    private static bool IsWellFormed(string? candidate) =>
        candidate is { Length: >= 5 and <= 16 }
        && candidate.StartsWith("ORD-", StringComparison.Ordinal)
        && candidate.AsSpan(4).ContainsAnyExcept("0123456789") is false;

    public override string ToString() => value ?? "OrderId(uninitialized)";
}
