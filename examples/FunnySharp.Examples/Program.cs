using FunnySharp;

VerifySynchronousFunctions();
VerifyOptions();
await VerifyAsynchronousFunctions();
await VerifyAsynchronousOptions();

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
