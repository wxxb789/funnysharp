namespace FunnySharp.VerticalSlice.Domain;

/// <summary>A validated shipment command.</summary>
public sealed record ShipOrderCommand(OrderId OrderId, TrackingCode TrackingCode);
