namespace FunctionGrammarComparisons;

// Supplied neutral DTOs and external-service seams, not workflow algorithms.
// No FunnySharp types cross these boundaries.
public sealed record OrderLine(int Quantity, decimal UnitPrice);

public sealed record Transaction(string Reference, decimal Amount);

public sealed record AccountForm(string Email, string Password, int Age, string Country);

public sealed record FormOutput(AccountForm? Value, string[] Errors);

public sealed record CatalogItem(PromoCampaign? Promo);

public sealed record PromoCampaign(string? Label);

public interface IRateBook
{
    ValueTask<decimal> RateForValueAsync(string customerId, CancellationToken cancellationToken);

    Task<decimal> RateForAsync(string customerId, CancellationToken cancellationToken);
}

public interface IMarginRules
{
    ValueTask<decimal> ApplyValueAsync(decimal rate, CancellationToken cancellationToken);

    Task<decimal> ApplyAsync(decimal rate, CancellationToken cancellationToken);
}
