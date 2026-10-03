using System.Globalization;

namespace FunctionGrammarComparisons;

public static class IdiomaticWorkflows
{
    // WF-1
    public static string ProductIdWithAudit(
        IReadOnlyDictionary<string, string> catalog,
        string rawSku,
        Action<string> audit)
    {
        var sku = rawSku.Trim().ToUpperInvariant();
        audit($"normalized sku={sku}");
        return catalog.TryGetValue(sku, out var productId)
            ? productId ?? "UNKNOWN"
            : "UNKNOWN";
    }

    // WF-2: the caller constructs once and passes the same function to both sites.
    public static Func<decimal, decimal> CampaignAdjustment(decimal discountRate, decimal taxRate) =>
        amount => RoundTo(Scale(taxRate, Scale(discountRate, amount)), 2);

    public static decimal[] PriceCampaignLines(
        Func<decimal, decimal> adjust,
        IReadOnlyList<OrderLine> lines) =>
        lines.Select(line => adjust(LineTotal(line))).ToArray();

    public static decimal QuoteCampaignLine(Func<decimal, decimal> adjust, OrderLine line) =>
        adjust(LineTotal(line));

    private static decimal LineTotal(OrderLine line) => line.Quantity * line.UnitPrice;

    private static decimal Scale(decimal factor, decimal amount) => amount * factor;

    private static decimal RoundTo(decimal amount, int digits) =>
        Math.Round(amount, digits, MidpointRounding.AwayFromZero);

    // WF-3: only a parse FormatException becomes the default quote.
    public static decimal ShippingQuote(
        IReadOnlyDictionary<string, decimal> zoneRates,
        string rawWeight,
        string zone,
        Action<string> audit)
    {
        decimal weight;
        try
        {
            weight = decimal.Parse(rawWeight, CultureInfo.InvariantCulture);
        }
        catch (FormatException)
        {
            audit($"quote failed: weight '{rawWeight}' is not a number");
            return 4.90m;
        }

        if (!zoneRates.TryGetValue(zone, out var rate))
        {
            audit($"quote failed: zone '{zone}' is not configured; pricing at the flat rate");
            rate = 0.90m;
        }

        return Math.Round(rate * weight, 2, MidpointRounding.AwayFromZero);
    }

    // WF-4: eager array boundary; no seed element.
    public static decimal[] RunningBalances(IEnumerable<Transaction> transactions, decimal openingBalance)
    {
        var balances = new List<decimal>();
        var balance = openingBalance;
        foreach (var transaction in transactions)
        {
            balance += transaction.Amount;
            balances.Add(balance);
        }

        return balances.ToArray();
    }

    public static decimal ClosingBalance(IEnumerable<Transaction> transactions, decimal openingBalance) =>
        transactions.Aggregate(openingBalance, (balance, transaction) => balance + transaction.Amount);

    // WF-5: both async function kinds are constructed once and remain reusable.
    public static Func<string, CancellationToken, ValueTask<decimal>> QuoteValuePipeline(
        IRateBook rates,
        IMarginRules margins) =>
        async (customerId, token) =>
        {
            var rate = await rates.RateForValueAsync(customerId, token).ConfigureAwait(false);
            return await margins.ApplyValueAsync(rate, token).ConfigureAwait(false);
        };

    public static Func<string, CancellationToken, Task<decimal>> QuoteAuditPipeline(
        IRateBook rates,
        IMarginRules margins) =>
        async (customerId, token) =>
        {
            var rate = await rates.RateForAsync(customerId, token).ConfigureAwait(false);
            return await margins.ApplyAsync(rate, token).ConfigureAwait(false);
        };

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

    // WF-6: ordered error codes and the validated value share one public boundary.
    public static FormOutput ValidateAccount(AccountForm form)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(form.Email) || !form.Email.Contains('@'))
        {
            errors.Add("E_EMAIL");
        }

        if (form.Password.Length < 12)
        {
            errors.Add("E_PASSWORD");
        }

        if (form.Age < 18 || form.Age > 130)
        {
            errors.Add("E_AGE");
        }

        if (form.Country.Length != 2 || !form.Country.All(char.IsUpper))
        {
            errors.Add("E_COUNTRY");
        }

        return new FormOutput(
            errors.Count == 0
                ? new AccountForm(form.Email, form.Password, form.Age, form.Country)
                : null,
            errors.ToArray());
    }

    // WF-7: nullable label boundary, including runtime-null stored items.
    public static string? PromoLabel(IReadOnlyDictionary<string, CatalogItem> catalog, string sku) =>
        catalog.GetValueOrDefault(sku)?.Promo?.Label;
}
