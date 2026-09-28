namespace FunnySharp.VerticalSlice.Domain;

/// <summary>One requested line before pricing.</summary>
public sealed record PlaceOrderLine(Sku Sku, Quantity Quantity);

/// <summary>A validated placement command: every member is already refined.</summary>
public sealed record PlaceOrderCommand(CustomerId CustomerId, NonEmpty<PlaceOrderLine> Lines);
