using System.Globalization;

using FunnySharp;

// The Goal 16 function-grammar examples: Scan running aggregates, the Option LINQ bridge,
// arity-4 Validation combination, ComposeValueAsync, and fallible composition through Bind.
// Kept separate from Program.cs so the runnable example file stays navigable and short.
internal static class FunctionGrammarSamples
{
    internal static void Verify()
    {
        VerifyRunningBalance();
        VerifyOptionQueryBridge();
        VerifyFourFieldForm();
        VerifyFallibleComposition();
    }

    internal static async Task VerifyAsync()
    {
        await VerifyAsyncRunningBalance();
        await VerifyComposeValueAsyncTokens();
    }

    private static void VerifyRunningBalance()
    {
        var ledger = new[] { 1200, -450, -300, 180 };

        // Scan yields the running balance after each entry; the seed is never yielded.
        var runningBalance = ledger.Scan(0m, static (balance, amount) => balance + amount).ToArray();

        ExampleAssertions.SequenceEqual([1200m, 750m, 450m, 630m], runningBalance);

        // The last running value equals the BCL fold for a non-empty source.
        ExampleAssertions.Equal(
            ledger.Aggregate(0m, static (balance, amount) => balance + amount),
            runningBalance[^1]);

        ExampleAssertions.True(
            !Array.Empty<int>().Scan(0m, static (balance, amount) => balance + amount).Any(),
            "An empty source must yield no running values.");
    }

    private static void VerifyOptionQueryBridge()
    {
        var configuredPort = Option.Some(" 8080 ");

        // The member-centric vocabulary is the canonical form.
        var memberForm = configuredPort
            .Map(static text => text.Trim())
            .Bind(ParsePort);

        // The LINQ bridge is the secondary query-syntax alias of the same Map/Bind chain.
        var queryForm =
            from text in configuredPort
            from port in ParsePort(text.Trim())
            select port;

        ExampleAssertions.Equal(Option.Some(8080), memberForm);
        ExampleAssertions.Equal(memberForm, queryForm);
    }

    private static void VerifyFourFieldForm()
    {
        var invalid = ValidateRegistration(new RegistrationRequest(" ", "not-an-email", 15, ""));
        ExampleAssertions.True(invalid.TryGetErrors(out var errors), "Every invalid field must contribute its error.");
        ExampleAssertions.SequenceEqual(
            [
                new AccountValidationError("displayName", "required"),
                new AccountValidationError("email", "invalid"),
                new AccountValidationError("age", "must-be-adult"),
                new AccountValidationError("country", "required"),
            ],
            errors!);

        var valid = ValidateRegistration(new RegistrationRequest("Ada", "ada@example.com", 36, "GB"));
        ExampleAssertions.Equal(
            new Registration("Ada", "ada@example.com", 36, "GB"),
            valid.Match(
                value => value,
                _ => throw new InvalidOperationException("Expected the valid registration to combine.")));
    }

    private static void VerifyFallibleComposition()
    {
        // Fallible-function composition stays Bind: the stages chain through the carrier,
        // and a failure stops the chain with the first failure's error.
        Func<string, Result<int, CheckoutError>> parseQuantity = ParseQuantityText;
        Func<int, Result<decimal, CheckoutError>> lookupUnitPrice = LookupUnitPrice;

        var lineTotal = parseQuantity("2")
            .Bind(lookupUnitPrice)
            .Map(static unitPrice => unitPrice * 3);

        ExampleAssertions.Equal(Result<decimal, CheckoutError>.Success(37.50m), lineTotal);

        var stopped = parseQuantity("zero")
            .Bind(lookupUnitPrice)
            .Map<decimal>(_ => throw new InvalidOperationException("Map must be skipped after a failure."));

        ExampleAssertions.Equal("invalid-quantity", stopped.Match(_ => "success", error => error.Code));
    }

    private static async Task VerifyAsyncRunningBalance()
    {
        // The async family mirrors Choose: bare Scan takes a synchronous accumulator.
        var runningBalance = await AsyncLedgerEntries()
            .Scan(0m, static (balance, amount) => balance + amount)
            .ToListAsync();

        ExampleAssertions.SequenceEqual([250m, 150m, 400m], runningBalance);

        using var cancellationSource = new CancellationTokenSource();
        var observedTokens = new List<CancellationToken>();

        // ScanValueAsync takes a ValueTask accumulator; the cancellation-aware overload
        // receives the consumer's enumeration token for every accumulate call.
        var runningTotal = await AsyncLedgerEntries()
            .ScanValueAsync(0m, (balance, amount, token) =>
            {
                observedTokens.Add(token);
                return ValueTask.FromResult(balance + amount);
            })
            .ToListAsync(cancellationSource.Token);

        ExampleAssertions.SequenceEqual([250m, 150m, 400m], runningTotal);
        ExampleAssertions.True(
            observedTokens.Count == 3 && observedTokens.All(token => token == cancellationSource.Token),
            "ScanValueAsync must forward the enumeration token to every accumulator call.");
    }

    private static async Task VerifyComposeValueAsyncTokens()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var observedTokens = new List<CancellationToken>();
        Func<int, CancellationToken, ValueTask<int>> incrementWithToken = (value, token) =>
        {
            observedTokens.Add(token);
            return ValueTask.FromResult(value + 1);
        };
        Func<int, CancellationToken, ValueTask<int>> doubleWithToken = (value, token) =>
        {
            observedTokens.Add(token);
            return ValueTask.FromResult(value * 2);
        };

        var result = await incrementWithToken.ComposeValueAsync(doubleWithToken)(3, cancellationSource.Token);
        ExampleAssertions.Equal(8, result);
        ExampleAssertions.True(
            observedTokens.Count == 2 && observedTokens.All(token => token == cancellationSource.Token),
            "ComposeValueAsync must pass the supplied token to both stages without eager cancellation.");
    }

    // The arity-4 Zip combine composes a four-field form in one expression; the combine
    // arity is bounded at 4 and the error order is the argument order.
    private static Validation<Registration, AccountValidationError> ValidateRegistration(
        RegistrationRequest request) =>
        ValidateDisplayName(request.DisplayName)
            .Zip(
                ValidateEmail(request.Email),
                ValidateAge(request.Age),
                ValidateCountry(request.Country),
                (displayName, email, age, country) => new Registration(displayName, email, age, country));

    private static Option<int> ParsePort(string text) =>
        int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var port)
            ? Option.Some(port)
            : Option.None<int>();

    private static Result<int, CheckoutError> ParseQuantityText(string text) =>
        int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var quantity) && quantity > 0
            ? Result<int, CheckoutError>.Success(quantity)
            : Result<int, CheckoutError>.Failure(new CheckoutError("invalid-quantity"));

    private static Result<decimal, CheckoutError> LookupUnitPrice(int quantity) =>
        Result<decimal, CheckoutError>.Success(12.50m);

    private static Validation<string, AccountValidationError> ValidateDisplayName(string displayName) =>
        string.IsNullOrWhiteSpace(displayName)
            ? Validation<string, AccountValidationError>.Invalid(
                new AccountValidationError("displayName", "required"))
            : Validation<string, AccountValidationError>.Valid(displayName.Trim());

    private static Validation<string, AccountValidationError> ValidateEmail(string email) =>
        string.IsNullOrWhiteSpace(email) || !email.Contains('@')
            ? Validation<string, AccountValidationError>.Invalid(new AccountValidationError("email", "invalid"))
            : Validation<string, AccountValidationError>.Valid(email);

    private static Validation<int, AccountValidationError> ValidateAge(int age) =>
        age < 18
            ? Validation<int, AccountValidationError>.Invalid(new AccountValidationError("age", "must-be-adult"))
            : Validation<int, AccountValidationError>.Valid(age);

    private static Validation<string, AccountValidationError> ValidateCountry(string country) =>
        string.IsNullOrWhiteSpace(country)
            ? Validation<string, AccountValidationError>.Invalid(new AccountValidationError("country", "required"))
            : Validation<string, AccountValidationError>.Valid(country.Trim().ToUpperInvariant());

    private static async IAsyncEnumerable<decimal> AsyncLedgerEntries()
    {
        yield return 250m;
        await Task.Yield();
        yield return -100m;
        yield return 250m;
    }
}

internal sealed record RegistrationRequest(string DisplayName, string Email, int Age, string Country);

internal sealed record Registration(string DisplayName, string Email, int Age, string Country);
