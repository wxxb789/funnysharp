using System.Globalization;

using FunnySharp;

namespace CallSites.FunnySharp;

// Goal 16 versions of the call sites that exercise the function-grammar surface:
// Pipe/Tap, Compose/Partial/Flip, Result.Try/Bind/MapError/Recover, Scan, the
// async composition verbs, the arity-4 Zip combiner, and the Option LINQ bridge.
// The harness helpers and types at the bottom of this file are excluded from
// every count, like the shared Domain.cs model.
public static class Goal16Workflows
{
    // WF-1: multi-stage transform pipeline with observation. Five stages:
    // trim, uppercase, audit, catalog lookup, fallback.
    public static string ProductIdWithAudit(
        IReadOnlyDictionary<string, string> catalog,
        string rawSku,
        Action<string> audit) =>
        rawSku
            .Pipe(sku => sku.Trim())
            .Pipe(sku => sku.ToUpperInvariant())
            .Tap(sku => audit($"normalized sku={sku}"))
            .Pipe(sku => catalog.GetOption(sku))
            .GetValueOr("UNKNOWN");

    // WF-2: reusable composed transform, built once from the shared binary
    // helpers and applied at every call site. Scale is config-first, so
    // Partial binds the rate directly; RoundTo is data-first, so Flip
    // reorders before Partial binds the digits.
    public static IReadOnlyList<decimal> PriceCampaignLines(
        IReadOnlyList<OrderLine> lines,
        decimal discountRate,
        decimal taxRate)
    {
        var adjust = CampaignAdjustment(discountRate, taxRate);
        return lines.Select(line => adjust(LineTotal(line))).ToArray();
    }

    public static decimal QuoteCampaignLine(
        OrderLine line,
        decimal discountRate,
        decimal taxRate) =>
        CampaignAdjustment(discountRate, taxRate)(LineTotal(line));

    private static Func<decimal, decimal> CampaignAdjustment(decimal discountRate, decimal taxRate) =>
        Scale.Partial(discountRate)
            .Compose(Scale.Partial(taxRate))
            .Compose(RoundTo.Flip().Partial(2));

    // WF-3: fallible pipeline with recovery and error mapping. Fallible
    // composition stays Bind (decision E74): Try parses at the exception
    // boundary, Bind composes the zone lookup (which recovers an unknown zone
    // to the flat rate), MapError maps the typed error to a message, and
    // Match is the single boundary back to a plain value.
    public static decimal ShippingQuote(
        IReadOnlyDictionary<string, decimal> zoneRates,
        string rawWeight,
        string zone,
        Action<string> audit) =>
        Result.Try<decimal, QuoteError>(
                () => decimal.Parse(rawWeight, CultureInfo.InvariantCulture),
                _ => new UnparsableWeight($"weight '{rawWeight}' is not a number"))
            .Bind(weight => zoneRates.GetOption(zone)
                .ToResult<decimal, QuoteError>(() => new UnknownZone($"zone '{zone}' is not configured"))
                .Recover(error =>
                {
                    audit($"quote failed: {error.Message}; pricing at the flat rate");
                    return 0.90m;
                })
                .Map(rate => Math.Round(rate * weight, 2, MidpointRounding.AwayFromZero)))
            .MapError(error => error.Message)
            .Match(quote => quote, message =>
            {
                audit($"quote failed: {message}");
                return 4.90m;
            });

    // WF-4: running aggregate report. Scan yields the accumulator after each
    // transaction; the seed is never yielded, and for a non-empty ledger the
    // last element equals what Aggregate produces (see ClosingBalance).
    public static IReadOnlyList<decimal> RunningBalances(
        IReadOnlyList<Transaction> transactions,
        decimal openingBalance) =>
        transactions
            .Scan(openingBalance, (balance, transaction) => balance + transaction.Amount)
            .ToArray();

    public static decimal ClosingBalance(
        IReadOnlyList<Transaction> transactions,
        decimal openingBalance) =>
        transactions.Aggregate(openingBalance, (balance, transaction) => balance + transaction.Amount);

    // WF-5: async composed stages, built once as function values. The naming
    // rule is visible at the call site: ComposeValueAsync joins the
    // ValueTask-returning stages and ComposeAsync joins the Task-returning
    // audit variant.
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

    public static async Task<IReadOnlyList<decimal>> QuoteAllAsync(
        IRateBook rates,
        IMarginRules margins,
        IReadOnlyList<string> customerIds,
        CancellationToken cancellationToken)
    {
        var quote = QuoteValuePipeline(rates, margins);
        var quotes = new List<decimal>(customerIds.Count);
        foreach (var customerId in customerIds)
        {
            quotes.Add(await quote(customerId, cancellationToken).ConfigureAwait(false));
        }

        return quotes;
    }

    public static Task<decimal> QuoteForAuditAsync(
        IRateBook rates,
        IMarginRules margins,
        string customerId,
        CancellationToken cancellationToken) =>
        QuoteAuditPipeline(rates, margins)(customerId, cancellationToken);

    // WF-6: four-field form validation with the arity-4 Zip combiner. The
    // combiner arguments are the accumulation order, left to right; four
    // fields is the bound of the applicative helper (api-decisions AD-3).
    public static Validation<AccountForm, string> ValidateAccount(AccountForm form) =>
        ValidateEmail(form.Email).Zip(
            ValidatePassword(form.Password),
            ValidateAge(form.Age),
            ValidateCountry(form.Country),
            (email, password, age, country) => new AccountForm(email, password, age, country));

    // WF-7: optional chain resolution. The member form (Map/Bind) is the
    // canonical vocabulary; the query form below is the LINQ bridge.
    public static Option<string> PromoLabel(
        IReadOnlyDictionary<string, CatalogItem> catalog,
        string sku) =>
        catalog.GetOption(sku)
            .Bind(item => item.Promo.ToOption())
            .Map(promo => promo.Label);

    public static Option<string> PromoLabelQuery(
        IReadOnlyDictionary<string, CatalogItem> catalog,
        string sku) =>
        from item in catalog.GetOption(sku)
        from promo in item.Promo.ToOption()
        select promo.Label;

    private static Validation<string, string> ValidateEmail(string email) =>
        !string.IsNullOrWhiteSpace(email) && email.Contains('@')
            ? Validation<string, string>.Valid(email)
            : Validation<string, string>.Invalid("email must contain '@'");

    private static Validation<string, string> ValidatePassword(string password) =>
        password.Length is >= 12
            ? Validation<string, string>.Valid(password)
            : Validation<string, string>.Invalid("password must have at least 12 characters");

    private static Validation<int, string> ValidateAge(int age) =>
        age is >= 18 and <= 130
            ? Validation<int, string>.Valid(age)
            : Validation<int, string>.Invalid("age must be between 18 and 130");

    private static Validation<string, string> ValidateCountry(string country) =>
        country.Length == 2 && country.All(char.IsUpper)
            ? Validation<string, string>.Valid(country)
            : Validation<string, string>.Invalid("country must be a two-letter uppercase code");

    // Harness (excluded from counts): binary helpers as function values so
    // Partial/Flip/Compose apply directly, plus the shared line total.
    private static readonly Func<OrderLine, decimal> LineTotal =
        line => line.Quantity * line.UnitPrice;

    private static readonly Func<decimal, decimal, decimal> Scale =
        (factor, amount) => amount * factor;

    private static readonly Func<decimal, int, decimal> RoundTo =
        (amount, digits) => Math.Round(amount, digits, MidpointRounding.AwayFromZero);
}

// Harness (excluded from counts): types shared with the idiomatic baselines.
public sealed record Transaction(string Reference, decimal Amount);

public sealed record AccountForm(string Email, string Password, int Age, string Country);

public sealed record CatalogItem(string Sku, PromoCampaign? Promo);

public sealed record PromoCampaign(string Code, string Label);

public abstract record QuoteError(string Message);

public sealed record UnparsableWeight(string Message) : QuoteError(Message);

public sealed record UnknownZone(string Message) : QuoteError(Message);

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
