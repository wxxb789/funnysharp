using FunnySharp;
using System.Globalization;

VerifySynchronousFunctions();
VerifyDataPipelines();
VerifyOptions();
VerifyResults();
VerifyValidations();
await VerifyAsynchronousFunctions();
await VerifyAsynchronousDataPipelines();
await VerifyAsynchronousOptions();
await VerifyAsynchronousResults();
await VerifyAsynchronousValidationTraversal();
await VerifyEffects();
await VerifyConcurrentOrderWorkflow();
await VerifyStateMachines();

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

static void VerifyDataPipelines()
{
    var rows = new[]
    {
        "order_id,sku,quantity,status",
        " A-100 , book , 2 , paid ",
        "malformed",
        "B-200,pen,0,paid",
        "C-300,notebook,3,cancelled",
        "D-400,notebook,4,paid",
    };

    var cleanedOrders = rows
        .Skip(1)
        .Choose(ParseOrderRow)
        .OrderBy(order => order.OrderId)
        .ToArray();

    AssertSequenceEqual(
        [
            new CleanOrder("A-100", "BOOK", 2),
            new CleanOrder("D-400", "NOTEBOOK", 4),
        ],
        cleanedOrders);

    ReadOnlySpan<int> rawQuantities = [2, 0, 3, -1, 4];
    Span<int> normalizedStorage = stackalloc int[rawQuantities.Length];
    var normalized = rawQuantities.ChooseTo(
        normalizedStorage,
        static quantity => quantity > 0 ? Option.Some(quantity * 10) : Option.None<int>());
    AssertSequenceEqual([20, 30, 40], normalized.ToArray());

    var mutableStorage = new[] { 1, 2, 3, 4 }.AsMemory();
    var compacted = mutableStorage
        .SelectInPlace(static quantity => quantity * 10)
        .WhereInPlace(static quantity => quantity >= 30);
    AssertSequenceEqual([30, 40], compacted.ToArray());
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

static async Task VerifyAsynchronousDataPipelines()
{
    using var cancellationSource = new CancellationTokenSource();
    var observedTokens = new List<CancellationToken>();

    var cleanedOrders = await AsyncOrderRows()
        .ChooseValueAsync(async (row, token) =>
        {
            observedTokens.Add(token);
            await Task.Yield();
            return ParseOrderRow(row);
        })
        .ToListAsync(cancellationSource.Token);

    AssertSequenceEqual(
        [
            new CleanOrder("A-100", "BOOK", 2),
            new CleanOrder("D-400", "NOTEBOOK", 4),
        ],
        cleanedOrders);
    Assert(
        observedTokens.All(token => token == cancellationSource.Token),
        "ChooseValueAsync must receive the enumeration token for every reached row.");
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

static async Task VerifyEffects()
{
    var trace = new List<string>();
    var composed = Effect.FromSync(() =>
        {
            trace.Add("source");
            return 2;
        })
        .Map(value =>
        {
            trace.Add("map");
            return value + 1;
        })
        .Bind(value => Effect.FromSync(() =>
        {
            trace.Add("bind");
            return value * 3;
        }));

    AssertEqual(0, trace.Count);
    AssertEqual(9, await composed.RunAsync());
    AssertSequenceEqual(["source", "map", "bind"], trace);

    var fromTask = Effect.FromTask(() => Task.FromResult(5));
    var fromValueTask = Effect.FromValueTask(() => ValueTask.FromResult(6));
    AssertEqual(5, await fromTask.RunAsync());
    AssertEqual(6, await fromValueTask.RunAsync());

    var environment = new EffectExampleEnvironment(4);
    var configured = Effect.FromSync<EffectExampleEnvironment, int>(current => current.Offset)
        .Map(value => value * 2)
        .Provide(environment);
    AssertEqual(8, await configured.RunAsync());

    var currentTime = Effect.FromSync((TimeProvider clock) => clock.GetUtcNow())
        .Provide(TimeProvider.System);
    AssertEqual(TimeSpan.Zero, (await currentTime.RunAsync()).Offset);

    var failure = Result<int, string>.Failure("denied");
    AssertEqual(failure, await Effect.FromResult(failure).RunAsync());

    ExampleDisposable? disposable = null;
    AssertEqual(7, await Effect.FromSync(() => disposable = new ExampleDisposable())
        .Using(_ => Effect.FromValue(7))
        .RunAsync());
    AssertEqual(1, disposable!.DisposeCount);

    ExampleAsyncDisposable? asyncDisposable = null;
    AssertEqual(8, await Effect.FromSync(() => asyncDisposable = new ExampleAsyncDisposable())
        .UsingAsync(_ => Effect.FromValue(8))
        .RunAsync());
    AssertEqual(1, asyncDisposable!.DisposeAsyncCount);
}

static async Task VerifyConcurrentOrderWorkflow()
{
    using var cancellationSource = new CancellationTokenSource();
    var quotes = await AsyncShippingOrders()
        .SelectParallelValueAsync(2, GetShippingQuoteAsync)
        .ToListAsync(cancellationSource.Token);

    AssertSequenceEqual(
        [
            new ShippingQuote("ORD-100", "north", 12.50m),
            new ShippingQuote("ORD-200", "north", 8.75m),
            new ShippingQuote("ORD-300", "north", 16.25m),
        ],
        quotes);

    var capacityValidation = await AsyncShippingOrders().TraverseParallelValueAsync(
        2,
        static (order, token) =>
        {
            token.ThrowIfCancellationRequested();
            return ValueTask.FromResult(
                order.ItemCount <= 2
                    ? Validation<ShippingOrder, ShippingError>.Valid(order)
                    : Validation<ShippingOrder, ShippingError>.Invalid(
                        new ShippingError(order.OrderId, "capacity-exceeded")));
        },
        cancellationSource.Token);

    Assert(capacityValidation.TryGetErrors(out var capacityErrors), "Capacity errors must accumulate.");
    AssertSequenceEqual(
        [
            new ShippingError("ORD-100", "capacity-exceeded"),
            new ShippingError("ORD-300", "capacity-exceeded"),
        ],
        capacityErrors!);

    var reservation = await new[]
    {
        Effect.FromTask<Result<CarrierReservation, ShippingError>>(
            token => ReserveCarrierAsync("north", available: false, token)),
        Effect.FromTask<Result<CarrierReservation, ShippingError>>(
            token => ReserveCarrierAsync("south", available: true, token)),
    }.FirstSuccessAsync(TimeSpan.FromSeconds(1), cancellationSource.Token);

    Assert(reservation.TryGetValue(out var confirmed), "One available carrier must confirm the order.");
    AssertEqual(new CarrierReservation("south", "RSV-900"), confirmed!);
}

static async Task VerifyStateMachines()
{
    var draft = new AccessRequest("AR-100", "ada", AccessRequestStatus.Draft, null);
    StateMachine<AccessRequest, AccessRequestEvent, AccessRequestCommand, AccessRequestError> lifecycle =
        HandleAccessRequestLifecycle;
    StateMachine<AccessRequest, AccessRequestEvent, AccessRequestCommand, AccessRequestError> provisioning =
        HandleAccessProvisioning;
    var machine = lifecycle.OrElse(provisioning);

    var submitted = machine(draft, new SubmitAccessRequest());
    var commands = submitted.Match(
        change => change.Outputs,
        rejection => throw new InvalidOperationException($"Unexpected rejection: {rejection.Code}"),
        failure => throw new InvalidOperationException($"Unexpected failure: {failure.Code}"),
        () => throw new InvalidOperationException("Expected the submission transition to be defined."));
    AssertEqual(2, commands.Count);
    Assert(submitted.TryGetChange(out var submittedChange), "A valid submission must produce a state change.");
    var submittedState = submittedChange!.State;
    AssertEqual(AccessRequestStatus.Submitted, submittedState.Status);

    var rejected = machine(draft, new ApproveAccessRequest("grace", true));
    Assert(rejected.IsRejected, "Approving a draft request must be rejected explicitly.");

    var failed = machine(submittedState, new ApproveAccessRequest("grace", false));
    Assert(failed.IsFailed, "An unavailable approver directory must be a typed transition failure.");

    var undefined = machine(draft, new ArchiveAccessRequest());
    Assert(undefined.IsUndefined, "An event with no owning handler must remain detectable.");

    var replay = machine.Replay(
        draft,
        [new SubmitAccessRequest(), new ApproveAccessRequest("grace", true)]);
    Assert(replay.TryGetChange(out var replayChange), "A valid history must replay successfully.");
    AssertEqual(AccessRequestStatus.Approved, replayChange!.State.Status);

    using var cancellationSource = new CancellationTokenSource();
    foreach (var command in commands)
    {
        await ExecuteAccessRequestCommandAsync(command, cancellationSource.Token);
    }
}

static TransitionResult<AccessRequest, AccessRequestCommand, AccessRequestError> HandleAccessRequestLifecycle(
    AccessRequest request,
    AccessRequestEvent @event) =>
    (request.Status, @event) switch
    {
        (AccessRequestStatus.Draft, SubmitAccessRequest) when string.IsNullOrWhiteSpace(request.EmployeeId) =>
            TransitionResult<AccessRequest, AccessRequestCommand, AccessRequestError>.Rejected(
                new AccessRequestError("employee-required")),
        (AccessRequestStatus.Draft, SubmitAccessRequest) =>
            TransitionResult<AccessRequest, AccessRequestCommand, AccessRequestError>.Applied(
                SubmitForApproval(request)),
        (AccessRequestStatus.Draft, ApproveAccessRequest) =>
            TransitionResult<AccessRequest, AccessRequestCommand, AccessRequestError>.Rejected(
                new AccessRequestError("approval-requires-submission")),
        (AccessRequestStatus.Submitted, ApproveAccessRequest { DirectoryAvailable: false }) =>
            TransitionResult<AccessRequest, AccessRequestCommand, AccessRequestError>.Failed(
                new AccessRequestError("approver-directory-unavailable")),
        (AccessRequestStatus.Submitted, ApproveAccessRequest { Approver: var approver }) =>
            TransitionResult<AccessRequest, AccessRequestCommand, AccessRequestError>.Applied(
                StateChange<AccessRequest, AccessRequestCommand>.To(
                    request with { Status = AccessRequestStatus.Approved, Approver = approver },
                    new StoreAccessRequest(request.Id),
                    new ProvisionAccess(request.Id, request.EmployeeId))),
        (AccessRequestStatus.Submitted, RejectAccessRequest { Approver: var approver }) =>
            TransitionResult<AccessRequest, AccessRequestCommand, AccessRequestError>.Applied(
                StateChange<AccessRequest, AccessRequestCommand>.To(
                    request with { Status = AccessRequestStatus.Rejected, Approver = approver },
                    new StoreAccessRequest(request.Id))),
        _ => TransitionResult<AccessRequest, AccessRequestCommand, AccessRequestError>.Undefined(),
    };

static TransitionResult<AccessRequest, AccessRequestCommand, AccessRequestError> HandleAccessProvisioning(
    AccessRequest request,
    AccessRequestEvent @event) =>
    (request.Status, @event) switch
    {
        (AccessRequestStatus.Approved, RevokeAccessRequest { RequestedBy: var requestedBy }) =>
            TransitionResult<AccessRequest, AccessRequestCommand, AccessRequestError>.Applied(
                StateChange<AccessRequest, AccessRequestCommand>.To(
                    request with { Status = AccessRequestStatus.Revoked, Approver = requestedBy },
                    new StoreAccessRequest(request.Id),
                    new RevokeAccess(request.Id, request.EmployeeId))),
        _ => TransitionResult<AccessRequest, AccessRequestCommand, AccessRequestError>.Undefined(),
    };

static StateChange<AccessRequest, AccessRequestCommand> SubmitForApproval(AccessRequest request)
{
    StateTransition<AccessRequest, AccessRequestCommand> markSubmitted = MarkSubmitted;
    StateTransition<AccessRequest, AccessRequestCommand> notifyApprover = QueueApproverNotification;
    return markSubmitted.Then(notifyApprover)(request);
}

static StateChange<AccessRequest, AccessRequestCommand> MarkSubmitted(AccessRequest request) =>
    StateChange<AccessRequest, AccessRequestCommand>.To(
        request with { Status = AccessRequestStatus.Submitted },
        new StoreAccessRequest(request.Id));

static StateChange<AccessRequest, AccessRequestCommand> QueueApproverNotification(AccessRequest request) =>
    StateChange<AccessRequest, AccessRequestCommand>.To(
        request,
        new NotifyApprover(request.Id));

static async ValueTask ExecuteAccessRequestCommandAsync(
    AccessRequestCommand command,
    CancellationToken cancellationToken)
{
    cancellationToken.ThrowIfCancellationRequested();
    await Task.Yield();

    _ = command switch
    {
        StoreAccessRequest => "stored",
        NotifyApprover => "notified",
        ProvisionAccess => "provisioned",
        RevokeAccess => "revoked",
        _ => throw new ArgumentOutOfRangeException(nameof(command)),
    };
}

static async IAsyncEnumerable<CreateAccountRequest> AsyncAccountRequests()
{
    yield return new CreateAccountRequest("Ada", "ada@example.com", 36);
    await Task.Yield();
    yield return new CreateAccountRequest("Grace", "grace@example.com", 30);
}

static async IAsyncEnumerable<string> AsyncOrderRows()
{
    yield return " A-100 , book , 2 , paid ";
    await Task.Yield();
    yield return "malformed";
    yield return "C-300,notebook,3,cancelled";
    yield return "D-400,notebook,4,paid";
}

static async IAsyncEnumerable<ShippingOrder> AsyncShippingOrders()
{
    yield return new ShippingOrder("ORD-100", 5);
    await Task.Yield();
    yield return new ShippingOrder("ORD-200", 2);
    await Task.Yield();
    yield return new ShippingOrder("ORD-300", 8);
}

static async ValueTask<ShippingQuote> GetShippingQuoteAsync(
    ShippingOrder order,
    CancellationToken cancellationToken)
{
    cancellationToken.ThrowIfCancellationRequested();
    await Task.Yield();
    return new ShippingQuote(order.OrderId, "north", order.ItemCount * 1.25m + 6.25m);
}

static async Task<Result<CarrierReservation, ShippingError>> ReserveCarrierAsync(
    string carrier,
    bool available,
    CancellationToken cancellationToken)
{
    cancellationToken.ThrowIfCancellationRequested();
    await Task.Yield();
    return available
        ? Result<CarrierReservation, ShippingError>.Success(new CarrierReservation(carrier, "RSV-900"))
        : Result<CarrierReservation, ShippingError>.Failure(new ShippingError(carrier, "unavailable"));
}

static Option<CleanOrder> ParseOrderRow(string row)
{
    var columns = row.Split(',', StringSplitOptions.TrimEntries);
    if (columns.Length != 4
        || !int.TryParse(columns[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var quantity)
        || quantity <= 0
        || !string.Equals(columns[3], "paid", StringComparison.OrdinalIgnoreCase))
    {
        return Option.None<CleanOrder>();
    }

    return Option.Some(new CleanOrder(
        columns[0].ToUpperInvariant(),
        columns[1].ToUpperInvariant(),
        quantity));
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

internal sealed record CleanOrder(string OrderId, string Sku, int Quantity);

internal sealed record ShippingOrder(string OrderId, int ItemCount);

internal sealed record ShippingQuote(string OrderId, string Carrier, decimal Price);

internal sealed record CarrierReservation(string Carrier, string ConfirmationCode);

internal sealed record ShippingError(string SubjectId, string Code);

internal sealed record EffectExampleEnvironment(int Offset);

internal sealed class ExampleDisposable : IDisposable
{
    internal int DisposeCount { get; private set; }

    public void Dispose() => DisposeCount++;
}

internal sealed class ExampleAsyncDisposable : IAsyncDisposable
{
    internal int DisposeAsyncCount { get; private set; }

    public ValueTask DisposeAsync()
    {
        DisposeAsyncCount++;
        return ValueTask.CompletedTask;
    }
}

internal sealed record AccessRequest(
    string Id,
    string EmployeeId,
    AccessRequestStatus Status,
    string? Approver);

internal enum AccessRequestStatus
{
    Draft,
    Submitted,
    Approved,
    Rejected,
    Revoked,
}

internal abstract record AccessRequestEvent;

internal sealed record SubmitAccessRequest : AccessRequestEvent;

internal sealed record ApproveAccessRequest(string Approver, bool DirectoryAvailable) : AccessRequestEvent;

internal sealed record RejectAccessRequest(string Approver) : AccessRequestEvent;

internal sealed record RevokeAccessRequest(string RequestedBy) : AccessRequestEvent;

internal sealed record ArchiveAccessRequest : AccessRequestEvent;

internal abstract record AccessRequestCommand;

internal sealed record StoreAccessRequest(string RequestId) : AccessRequestCommand;

internal sealed record NotifyApprover(string RequestId) : AccessRequestCommand;

internal sealed record ProvisionAccess(string RequestId, string EmployeeId) : AccessRequestCommand;

internal sealed record RevokeAccess(string RequestId, string EmployeeId) : AccessRequestCommand;

internal sealed record AccessRequestError(string Code);
