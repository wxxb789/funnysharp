namespace FunnySharp.VerticalSlice.Domain;

/// <summary>A non-negative money amount in one ISO-4217 currency.</summary>
public readonly record struct Money
{
    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public decimal Amount { get; }

    public string Currency { get; } = string.Empty;

    public static Money Zero(string currency) => new(0m, currency);

    public static Result<Money, InputError> Create(decimal amount, string? currency)
    {
        if (amount < 0m)
        {
            return Result<Money, InputError>.Failure(
                new InputError("amount", "non-negative", "An amount cannot be negative."));
        }

        if (currency is not { Length: 3 } || !currency.All(char.IsAsciiLetterUpper))
        {
            return Result<Money, InputError>.Failure(
                new InputError("currency", "iso-4217", "A three-letter upper-case currency code is required."));
        }

        return Result<Money, InputError>.Success(
            new Money(decimal.Round(amount, 2, MidpointRounding.ToEven), currency));
    }

    public Money Add(Money other) => Mixes(other)
        ? throw new InvalidOperationException($"Cannot add {other.Currency} to {Currency}.")
        : new Money(Amount + other.Amount, Currency);

    public Money Multiply(int factor) => new(Amount * factor, Currency);

    private bool Mixes(Money other) => !string.Equals(Currency, other.Currency, StringComparison.Ordinal);

    public override string ToString() => $"{Amount:0.00} {Currency}";
}
