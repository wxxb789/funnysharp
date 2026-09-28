namespace FunnySharp.VerticalSlice.Domain;

/// <summary>The pure decision for one event: the next state plus the commands it emits.</summary>
public sealed record OrderPlan(Order Order, IReadOnlyList<OrderCommand> Commands);
