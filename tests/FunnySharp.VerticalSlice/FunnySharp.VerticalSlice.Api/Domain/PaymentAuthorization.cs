namespace FunnySharp.VerticalSlice.Domain;

/// <summary>The payment gateway's decision, carried by the payment event so the machine stays pure.</summary>
public readonly record struct PaymentAuthorization(bool Approved, string Reference, string? DeclineReason)
{
    public static PaymentAuthorization Granted(string reference) => new(true, reference, null);

    public static PaymentAuthorization Declined(string reason) => new(false, string.Empty, reason);
}
