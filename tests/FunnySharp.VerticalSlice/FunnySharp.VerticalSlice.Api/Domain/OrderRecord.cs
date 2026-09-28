namespace FunnySharp.VerticalSlice.Domain;

/// <summary>The stored projection plus the event history it was built from.</summary>
public sealed record OrderRecord(Order Order, IReadOnlyList<OrderEvent> History);
