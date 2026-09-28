namespace FunnySharp.VerticalSlice.Domain;

/// <summary>The lifecycle status carried by an order projection.</summary>
public enum OrderStatus
{
    Draft,
    Placed,
    Paid,
    Shipped,
    Cancelled,
}

/// <summary>The event kinds the order lifecycle machine dispatches on.</summary>
public enum OrderEventKind
{
    Place,
    Pay,
    Ship,
    Cancel,
}
