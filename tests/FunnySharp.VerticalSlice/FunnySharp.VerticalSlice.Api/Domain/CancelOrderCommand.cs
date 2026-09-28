namespace FunnySharp.VerticalSlice.Domain;

/// <summary>A validated cancellation command.</summary>
public sealed record CancelOrderCommand(OrderId OrderId, string Reason);
