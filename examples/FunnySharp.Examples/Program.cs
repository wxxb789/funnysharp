using FunnySharp;

VerifySynchronousFunctions();
await VerifyAsynchronousFunctions();

Console.WriteLine("FunnySharp function-composition examples passed.");

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
