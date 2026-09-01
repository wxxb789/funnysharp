using FunnySharp;
using System.Globalization;

VerifySynchronousFunctions();
VerifyOptions();
VerifyResults();
VerifyValidations();
await VerifyAsynchronousFunctions();
await VerifyAsynchronousOptions();
await VerifyAsynchronousResults();
await VerifyAsynchronousValidationTraversal();

Console.WriteLine("FunnySharp examples passed.");

static void VerifySynchronousFunctions()
{
    AssertEqual(6, 5.Pipe(value => value + 1));

    Func<string, string> trim = value => value.Trim();
    Func<string, string> emphasize = value => $"{value}!";
    AssertEqual("hello!", trim.Compose(emphasize)("  hello  "));

    Func<int, int, int> subtract = (left, right) => left - right;
    var curried = subtract.Curry();
    AssertEqual(5, curried(9)(4));
    AssertEqual(5, curried.Uncurry()(9, 4));
    AssertEqual(5, subtract.Partial(9)(4));
    AssertEqual(5, subtract.Flip()(4, 9));

    var original = new object();
    object? observed = null;
    var tapped = original.Tap(value => observed = value);
    Assert(ReferenceEquals(original, tapped), "Tap must return the original reference.");
    Assert(ReferenceEquals(original, observed), "Tap must observe the original reference.");

}

static void VerifyOptions()
{
    Option<int> absent = default;
    Assert(absent.IsNone, "default(Option<T>) must be None.");

    var zero = Option.Some(0);
    Assert(zero.TryGetValue(out var value), "Some(0) must be present.");
    AssertEqual(0, value);

    string? configuredPort = "8080";
    int? retryCount = 0;
    Assert(configuredPort.ToOption().IsSome, "A non-null reference must convert to Some.");
    Assert(((string?)null).ToOption().IsNone, "A null reference must convert to None.");
    Assert(retryCount.ToOption().TryGetValue(out var retries), "A nullable value containing 0 must be Some.");
    AssertEqual(0, retries);

    IReadOnlyDictionary<int, string> environments = new Dictionary<int, string>
    {
        [8080] = "development",
    };

    var resolvedEnvironment = configuredPort
        .ToOption()
        .Bind(text => Option.FromTry<int>((out int port) => int.TryParse(text, out port)))
        .Bind(port => environments.GetOption(port));
    AssertEqual("development", resolvedEnvironment.GetValueOr("unknown"));
    Assert(environments.GetOption(404).IsNone, "A missing dictionary key must be None.");

    var lazyFallbackCalled = false;
    var mapped = Option.Some(20)
        .Map(number => number + 1)
        .Bind(number => number > 20 ? Option.Some(number * 2) : Option.None<int>())
        .OrElseWith(() =>
        {
            lazyFallbackCalled = true;
            return Option.Some(-1);
        });
    AssertEqual(42, mapped.GetValueOrDefault());
    Assert(!lazyFallbackCalled, "OrElseWith must not invoke the fallback for Some.");
}

static void VerifyResults()
{
    IReadOnlyDictionary<string, decimal> prices = new Dictionary<string, decimal>
    {
        ["book"] = 12.50m,
    };

    var request = new CheckoutRequest("book", "2");
    var total = request.QuantityText
        .Pipe(ParseQuantity)
        .ZipWith(() => prices.GetOption(request.Sku).ToResult(new CheckoutError("unknown-sku")))
        .Map(values => values.First * values.Second);
    AssertEqual(Result<decimal, CheckoutError>.Success(25.00m), total);

    var invalidQuantity = new CheckoutRequest("book", "zero").QuantityText
        .Pipe(ParseQuantity)
        .ZipWith(() => prices.GetOption("book").ToResult(new CheckoutError("unknown-sku")));
    AssertEqual("invalid-quantity", invalidQuantity.Match(_ => "success", error => error.Code));

    var invalidDeadline = Result.Try<DateTimeOffset, CheckoutError>(
        () => DateTimeOffset.Parse("not-a-date", CultureInfo.InvariantCulture),
        exception => new CheckoutError("invalid-deadline", exception));
    var deadlineError = invalidDeadline.Match(
        _ => throw new InvalidOperationException("Expected deadline parsing to fail."),
        error => error);
    AssertEqual("invalid-deadline", deadlineError.Code);
    Assert(deadlineError.Cause is FormatException, "The explicit boundary must retain the original exception.");

    var recovered = Result<decimal, CheckoutError>
        .Failure(new CheckoutError("pricing-unavailable"))
        .Recover(_ => 0m);
    AssertEqual(Result<decimal, CheckoutError>.Success(0m), recovered);
}

static void VerifyValidations()
{
    var invalidRequest = new CreateAccountRequest(" ", "not-an-email", 15);
    var invalidAccount = ValidateAccount(invalidRequest);

    Assert(invalidAccount.TryGetErrors(out var errors), "The invalid account must expose errors.");
    AssertSequenceEqual(
        [
            new AccountValidationError("displayName", "required"),
            new AccountValidationError("email", "invalid"),
            new AccountValidationError("age", "must-be-adult"),
        ],
        errors!);

    var requests = new[]
    {
        new CreateAccountRequest("Ada", "ada@example.com", 36),
        invalidRequest,
        new CreateAccountRequest("Grace", "grace@example.com", 30),
    };
    var batch = requests.Traverse(ValidateAccount);

    Assert(batch.TryGetErrors(out var batchErrors), "Batch validation must retain every account error.");
    AssertSequenceEqual(errors!, batchErrors!);

    var account = ValidateAccount(requests[0]).Match(
        valid => valid,
        _ => throw new InvalidOperationException("Expected the account request to be valid."));
    AssertEqual(new Account("Ada", "ada@example.com", 36), account);
}

static Validation<Account, AccountValidationError> ValidateAccount(CreateAccountRequest request) =>
    ValidateDisplayName(request.DisplayName)
        .Zip(ValidateEmail(request.Email))
        .Zip(ValidateAge(request.Age))
        .Map(values => new Account(values.First.First, values.First.Second, values.Second));

static Validation<string, AccountValidationError> ValidateDisplayName(string displayName) =>
    string.IsNullOrWhiteSpace(displayName)
        ? Validation<string, AccountValidationError>.Invalid(
            new AccountValidationError("displayName", "required"))
        : Validation<string, AccountValidationError>.Valid(displayName.Trim());

static Validation<string, AccountValidationError> ValidateEmail(string email) =>
    string.IsNullOrWhiteSpace(email) || !email.Contains('@')
        ? Validation<string, AccountValidationError>.Invalid(new AccountValidationError("email", "invalid"))
        : Validation<string, AccountValidationError>.Valid(email);

static Validation<int, AccountValidationError> ValidateAge(int age) =>
    age < 18
        ? Validation<int, AccountValidationError>.Invalid(new AccountValidationError("age", "must-be-adult"))
        : Validation<int, AccountValidationError>.Valid(age);

static Result<int, CheckoutError> ParseQuantity(string text) =>
    int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var quantity)
        ? Result<int, CheckoutError>
            .Success(quantity)
            .Ensure(value => value > 0, new CheckoutError("quantity-not-positive"))
        : Result<int, CheckoutError>.Failure(new CheckoutError("invalid-quantity"));

static async Task VerifyAsynchronousFunctions()
{
    // Pipe returns the awaitable unchanged, so async delegates need no special Pipe overload.
    var pipedAsyncResult = await 4.Pipe(async value =>
    {
        await Task.Yield();
        return value * 3;
    });
    AssertEqual(12, pipedAsyncResult);

    Func<int, Task<int>> incrementAsync = value => Task.FromResult(value + 1);
    Func<int, Task<int>> doubleAsync = value => Task.FromResult(value * 2);
    AssertEqual(8, await incrementAsync.ComposeAsync(doubleAsync)(3));

    using var cancellationSource = new CancellationTokenSource();
    cancellationSource.Cancel();
    var observedTokens = new List<CancellationToken>();
    Func<int, CancellationToken, Task<int>> incrementWithToken = (value, token) =>
    {
        observedTokens.Add(token);
        return Task.FromResult(value + 1);
    };
    Func<int, CancellationToken, Task<int>> doubleWithToken = (value, token) =>
    {
        observedTokens.Add(token);
        return Task.FromResult(value * 2);
    };

    var cancelledTokenResult = await incrementWithToken.ComposeAsync(doubleWithToken)(3, cancellationSource.Token);
    AssertEqual(8, cancelledTokenResult);
    Assert(
        observedTokens.Count == 2 && observedTokens.All(token => token == cancellationSource.Token),
        "ComposeAsync must pass the supplied token to both stages without eager cancellation.");

    Func<int, ValueTask<int>> incrementValueAsync = value => ValueTask.FromResult(value + 1);
    Func<int, ValueTask<int>> doubleValueAsync = value => ValueTask.FromResult(value * 2);
    AssertEqual(8, await incrementValueAsync.ComposeAsync(doubleValueAsync)(3));

    var taskTapObserved = 0;
    AssertEqual(
        7,
        await 7.TapAsync(value =>
        {
            taskTapObserved = value;
            return Task.CompletedTask;
        }));
    AssertEqual(7, taskTapObserved);

    var valueTaskTapObserved = 0;
    AssertEqual(
        11,
        await 11.TapValueAsync(value =>
        {
            valueTaskTapObserved = value;
            return ValueTask.CompletedTask;
        }));
    AssertEqual(11, valueTaskTapObserved);
}

static async Task VerifyAsynchronousOptions()
{
    var taskOption = await Task.FromResult<string?>("42").ToOptionAsync();
    var taskMapped = await taskOption.MapAsync(text => Task.FromResult(int.Parse(text)));
    var taskBound = await taskMapped.BindAsync(number =>
        Task.FromResult(number > 0 ? Option.Some(number * 2) : Option.None<int>()));
    AssertEqual(84, taskBound.GetValueOrDefault());

    var valueTaskOption = await ValueTask.FromResult<string?>("ready").ToOptionAsync();
    var valueTaskMapped = await valueTaskOption.MapValueAsync(text =>
        ValueTask.FromResult(text.ToUpperInvariant()));
    var valueTaskBound = await valueTaskMapped.BindValueAsync(text =>
        ValueTask.FromResult(text == "READY" ? Option.Some(text.Length) : Option.None<int>()));
    AssertEqual(5, valueTaskBound.GetValueOrDefault());

    using var cancellationSource = new CancellationTokenSource();
    cancellationSource.Cancel();
    var callbackRan = false;
    var shortCircuited = await Option.None<int>().MapAsync(
        (value, token) =>
        {
            callbackRan = true;
            return Task.FromResult(value);
        },
        cancellationSource.Token);
    Assert(shortCircuited.IsNone && !callbackRan, "None must short-circuit without eagerly observing cancellation.");

    var expectedFailure = new InvalidOperationException("expected failure");
    await AssertFaultIsPreserved(Task.FromException<string?>(expectedFailure).ToOptionAsync(), expectedFailure);
    await AssertCancellationIsPreserved(Task.FromCanceled<string?>(cancellationSource.Token).ToOptionAsync());
}

static async Task VerifyAsynchronousResults()
{
    var price = await Result<string, CheckoutError>
        .Success("book")
        .BindAsync(LookUpPriceAsync);
    var discounted = await price.MapValueAsync(value => ValueTask.FromResult(value * 0.9m));
    AssertEqual(Result<decimal, CheckoutError>.Success(11.25m), discounted);

    var expectedFailure = new InvalidOperationException("pricing service failed");
    var boundaryFailure = await Result.TryAsync<decimal>(
        () => Task.FromException<decimal>(expectedFailure));
    var actualFailure = boundaryFailure.Match(
        _ => throw new InvalidOperationException("Expected the pricing boundary to fail."),
        error => error);
    Assert(ReferenceEquals(expectedFailure, actualFailure), "The boundary must retain exception identity.");

    using var cancellationSource = new CancellationTokenSource();
    cancellationSource.Cancel();
    await AssertCancellationIsPreserved(
        Result.TryAsync(() => Task.FromCanceled<decimal>(cancellationSource.Token)));
}

static async Task VerifyAsynchronousValidationTraversal()
{
    var validation = await AsyncAccountRequests().TraverseValueAsync(async request =>
    {
        await Task.Yield();
        return ValidateAccount(request);
    });

    Assert(validation.TryGetValue(out var accounts), "The asynchronous batch must be valid.");
    AssertEqual(2, accounts!.Count);
    AssertEqual("Ada", accounts[0].DisplayName);
    AssertEqual("Grace", accounts[1].DisplayName);
}

static async IAsyncEnumerable<CreateAccountRequest> AsyncAccountRequests()
{
    yield return new CreateAccountRequest("Ada", "ada@example.com", 36);
    await Task.Yield();
    yield return new CreateAccountRequest("Grace", "grace@example.com", 30);
}

static Task<Result<decimal, CheckoutError>> LookUpPriceAsync(string sku) =>
    Task.FromResult(
        sku == "book"
            ? Result<decimal, CheckoutError>.Success(12.50m)
            : Result<decimal, CheckoutError>.Failure(new CheckoutError("unknown-sku")));

static async Task AssertFaultIsPreserved(Task operation, Exception expected)
{
    try
    {
        await operation;
        throw new InvalidOperationException("Expected the option bridge to preserve the task failure.");
    }
    catch (Exception actual) when (ReferenceEquals(actual, expected))
    {
    }
}

static async Task AssertCancellationIsPreserved(Task operation)
{
    try
    {
        await operation;
        throw new InvalidOperationException("Expected the option bridge to preserve task cancellation.");
    }
    catch (OperationCanceledException)
    {
    }
}

static void AssertEqual<T>(T expected, T actual) where T : notnull
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"Expected '{expected}', but received '{actual}'.");
    }
}

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

static void AssertSequenceEqual<T>(IReadOnlyList<T> expected, IReadOnlyList<T> actual)
{
    if (!expected.SequenceEqual(actual))
    {
        throw new InvalidOperationException("The sequences were not equal.");
    }
}

internal sealed record CheckoutRequest(string Sku, string QuantityText);

internal sealed record CheckoutError(string Code, Exception? Cause = null);

internal sealed record CreateAccountRequest(string DisplayName, string Email, int Age);

internal sealed record Account(string DisplayName, string Email, int Age);

internal sealed record AccountValidationError(string Field, string Code);
