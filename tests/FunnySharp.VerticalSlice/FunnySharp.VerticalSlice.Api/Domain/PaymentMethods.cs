namespace FunnySharp.VerticalSlice.Domain;

/// <summary>
/// The payment methods this slice accepts. The two special names are the payment simulator's
/// deterministic failure switches, so the validator and the simulator read them from one place: a
/// rename cannot leave the validator accepting a method the simulator treats as unknown.
/// </summary>
public static class PaymentMethods
{
    public const string Card = "card";

    public const string CardDeclined = "card-declined";

    public const string CardUnavailable = "card-unavailable";

    public const string Invoice = "invoice";

    public static readonly string[] All = [Card, CardDeclined, CardUnavailable, Invoice];
}
