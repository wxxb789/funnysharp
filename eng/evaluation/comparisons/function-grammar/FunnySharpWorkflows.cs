using System.Globalization;

using FunnySharp;

namespace FunctionGrammarComparisons;

public static class FunnySharpWorkflows
{
    // WF-1: ordinary string operations stay together; no gratuitous Pipe stages.
    public static string ProductIdWithAudit(
        IReadOnlyDictionary<string, string> catalog,
        string rawSku,
        Action<string> audit) =>
        rawSku
            .Pipe(sku => sku.Trim().ToUpperInvariant())
            .Tap(sku => audit($"normalized sku={sku}"))
            .Pipe(sku => catalog.GetOption(sku))
            .GetValueOr("UNKNOWN");

    // WF-2: same externally owned construction lifetime as the BCL implementation.
    public static Func<decimal, decimal> CampaignAdjustment(decimal discountRate, decimal taxRate) =>
        Scale.Partial(discountRate)
            .Compose(Scale.Partial(taxRate))
            .Compose(RoundTo.Flip().Partial(2));

    public static decimal[] PriceCampaignLines(
        Func<decimal, decimal> adjust,
        IReadOnlyList<OrderLine> lines) =>
        lines.Select(line => adjust(LineTotal(line))).ToArray();

    public static decimal QuoteCampaignLine(Func<decimal, decimal> adjust, OrderLine line) =>
        adjust(LineTotal(line));

    private static decimal LineTotal(OrderLine line) => line.Quantity * line.UnitPrice;

    private static readonly Func<decimal, decimal, decimal> Scale =
        (factor, amount) => amount * factor;

    private static readonly Func<decimal, int, decimal> RoundTo =
        (amount, digits) => Math.Round(amount, digits, MidpointRounding.AwayFromZero);

    // WF-3: Result.Try would catch more exceptions than the frozen parse contract.
    // This consumer-owned helper is included in the count, not hidden as harness.
    private static Result<decimal, string> ParseWeight(string rawWeight)
    {
        try
        {
            return Result<decimal, string>.Success(
                decimal.Parse(rawWeight, CultureInfo.InvariantCulture));
        }
        catch (FormatException)
        {
            return Result<decimal, string>.Failure($"weight '{rawWeight}' is not a number");
        }
    }

    public static decimal ShippingQuote(
        IReadOnlyDictionary<string, decimal> zoneRates,
        string rawWeight,
        string zone,
        Action<string> audit) =>
        ParseWeight(rawWeight)
            .Bind(weight => zoneRates.GetOption(zone)
                .ToResult(() => $"zone '{zone}' is not configured")
                .Recover(error =>
                {
                    audit($"quote failed: {error}; pricing at the flat rate");
                    return 0.90m;
                })
                .Map(rate => Math.Round(rate * weight, 2, MidpointRounding.AwayFromZero)))
            .Match(quote => quote, error =>
            {
                audit($"quote failed: {error}");
                return 4.90m;
            });

    // WF-4: Scan excludes the seed; ToArray supplies the same eager boundary.
    public static decimal[] RunningBalances(IEnumerable<Transaction> transactions, decimal openingBalance) =>
        transactions.Scan(openingBalance, (balance, transaction) => balance + transaction.Amount).ToArray();

    public static decimal ClosingBalance(IEnumerable<Transaction> transactions, decimal openingBalance) =>
        transactions.Aggregate(openingBalance, (balance, transaction) => balance + transaction.Amount);

    // WF-5
    public static Func<string, CancellationToken, ValueTask<decimal>> QuoteValuePipeline(
        IRateBook rates,
        IMarginRules margins) =>
        new Func<string, CancellationToken, ValueTask<decimal>>(rates.RateForValueAsync)
            .ComposeValueAsync(margins.ApplyValueAsync);

    public static Func<string, CancellationToken, Task<decimal>> QuoteAuditPipeline(
        IRateBook rates,
        IMarginRules margins) =>
        new Func<string, CancellationToken, Task<decimal>>(rates.RateForAsync)
            .ComposeAsync(margins.ApplyAsync);

    public static async Task<decimal[]> QuoteAllAsync(
        Func<string, CancellationToken, ValueTask<decimal>> quote,
        IReadOnlyList<string> customerIds,
        CancellationToken cancellationToken)
    {
        var quotes = new List<decimal>(customerIds.Count);
        foreach (var customerId in customerIds)
        {
            quotes.Add(await quote(customerId, cancellationToken).ConfigureAwait(false));
        }

        return quotes.ToArray();
    }

    public static Task<decimal> QuoteForAuditAsync(
        Func<string, CancellationToken, Task<decimal>> quote,
        string customerId,
        CancellationToken cancellationToken) =>
        quote(customerId, cancellationToken);

    // WF-6: validators, value construction and elimination are all counted.
    public static FormOutput ValidateAccount(AccountForm form) =>
        ValidateEmail(form.Email).Zip(
            ValidatePassword(form.Password),
            ValidateAge(form.Age),
            ValidateCountry(form.Country),
            (email, password, age, country) => new AccountForm(email, password, age, country))
        .Match(
            value => new FormOutput(value, []),
            errors => new FormOutput(null, errors.ToArray()));

    private static Validation<string, string> ValidateEmail(string email) =>
        !string.IsNullOrWhiteSpace(email) && email.Contains('@')
            ? Validation<string, string>.Valid(email)
            : Validation<string, string>.Invalid("E_EMAIL");

    private static Validation<string, string> ValidatePassword(string password) =>
        password.Length >= 12
            ? Validation<string, string>.Valid(password)
            : Validation<string, string>.Invalid("E_PASSWORD");

    private static Validation<int, string> ValidateAge(int age) =>
        age >= 18 && age <= 130
            ? Validation<int, string>.Valid(age)
            : Validation<int, string>.Invalid("E_AGE");

    private static Validation<string, string> ValidateCountry(string country) =>
        country.Length == 2 && country.All(char.IsUpper)
            ? Validation<string, string>.Valid(country)
            : Validation<string, string>.Invalid("E_COUNTRY");

    // WF-7: member grammar is primary; nullable elimination is included.
    public static string? PromoLabel(IReadOnlyDictionary<string, CatalogItem> catalog, string sku) =>
        catalog.GetOption(sku)
            .Bind(item => item.Promo.ToOption())
            .Map(promo => promo.Label)
            .GetValueOrDefault();
}
