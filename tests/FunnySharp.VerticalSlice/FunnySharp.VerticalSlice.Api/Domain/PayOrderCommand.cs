namespace FunnySharp.VerticalSlice.Domain;

/// <summary>A validated payment command.</summary>
public sealed record PayOrderCommand(OrderId OrderId, string PaymentMethod);
