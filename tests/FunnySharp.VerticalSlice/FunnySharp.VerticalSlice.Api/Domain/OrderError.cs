namespace FunnySharp.VerticalSlice.Domain;

/// <summary>
/// A typed domain failure. Every case is an expected outcome the HTTP boundary maps explicitly;
/// unexpected exceptions never become one of these cases.
/// </summary>
public abstract record OrderError
{
    private OrderError()
    {
    }

    public sealed record OrderNotFound(OrderId OrderId) : OrderError;

    public sealed record TransitionRejected(OrderId OrderId, OrderStatus Status, OrderEventKind Event, string Code)
        : OrderError;

    public sealed record PaymentDeclined(OrderId OrderId, Money Amount, string Reason) : OrderError;

    public sealed record InventoryShortfall(Sku Sku, Quantity Requested, int Available) : OrderError;

    public sealed record ConcurrencyConflict(OrderId OrderId, int ExpectedRevision, int ActualRevision) : OrderError;

    public sealed record SupplierUnavailable(string Supplier, Sku Sku, string Reason) : OrderError;

    public sealed record NoSupplierQuote(Sku Sku, IReadOnlyList<string> Suppliers) : OrderError;

    public sealed record DependencyUnavailable(string Dependency, string Operation) : OrderError;

    public sealed record UndefinedTransition(OrderId OrderId, OrderStatus Status, OrderEventKind Event) : OrderError;

    public sealed record HistoryDiverged(OrderId OrderId, OrderEventKind Event, OrderError Cause) : OrderError;
}
