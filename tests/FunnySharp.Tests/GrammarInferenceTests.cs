namespace FunnySharp.Tests;

#pragma warning disable xUnit1051

/// <summary>
/// Compile-time grammar evidence for the function-grammar surface. Every call shape below
/// must resolve to exactly one overload and infer every type argument from its arguments;
/// a grammar change that makes a shape ambiguous (CS0121) or removes its inference (CS0411)
/// stops this file from compiling. Recorded inference friction is pinned with its
/// documented working shape: <c>Option.ToResult</c> states the intended error type
/// explicitly when the error factory returns a subtype of it; instance method groups used
/// as extension receivers are lifted into an explicitly typed delegate
/// (<see href="https://github.com/wxxb789/funnysharp/blob/main/docs/next-stage/call-sites-goal-16.md">call-sites-goal-16.md</see>, WF-3).
/// </summary>
public sealed class GrammarInferenceTests
{
    [Fact]
    public async Task ComposeValueAsyncAcceptsInstanceMethodGroups()
    {
        var stages = new InstanceStages();
        var composed = new Func<string, ValueTask<int>>(stages.ParseAsync)
            .ComposeValueAsync(stages.FormatAsync);

        Assert.Equal("n5", await composed("hello"));
    }

    private sealed class InstanceStages
    {
        public ValueTask<int> ParseAsync(string text) => ValueTask.FromResult(text.Length);

        public ValueTask<string> FormatAsync(int value) => ValueTask.FromResult($"n{value}");
    }

    [Fact]
    public void ZipArityTwoThroughFourInfersCombinerTypesOnOption()
    {
        var paired = Option.Some(2).Zip(Option.Some("dual"));
        var two = Option.Some(2).Zip(
            Option.Some("dual"),
            static (number, word) => (number, word));
        var three = Option.Some(2).Zip(
            Option.Some("dual"),
            Option.Some(3.5m),
            static (number, word, scale) => (number, word, scale));
        var four = Option.Some(2).Zip(
            Option.Some("dual"),
            Option.Some(3.5m),
            Option.Some(true),
            static (number, word, scale, flag) => (number, word, scale, flag));

        Assert.Equal(Option.Some((2, "dual")), paired);
        Assert.Equal(Option.Some((2, "dual")), two);
        Assert.Equal(Option.Some((2, "dual", 3.5m)), three);
        Assert.Equal(Option.Some((2, "dual", 3.5m, true)), four);
    }

    [Fact]
    public void ZipArityFourInfersCombinerTypesOnResultAndValidation()
    {
        Result<int, string> number = Result<int, string>.Success(2);
        Result<string, string> word = Result<string, string>.Success("dual");
        Result<decimal, string> scale = Result<decimal, string>.Success(3.5m);
        Result<bool, string> flag = Result<bool, string>.Success(true);
        var combined = number.Zip(
            word,
            scale,
            flag,
            static (n, w, s, f) => (n, w, s, f));
        Assert.Equal(Result<(int, string, decimal, bool), string>.Success((2, "dual", 3.5m, true)), combined);

        Validation<int, string> validNumber = Validation<int, string>.Valid(2);
        Validation<string, string> validWord = Validation<string, string>.Valid("dual");
        Validation<decimal, string> validScale = Validation<decimal, string>.Valid(3.5m);
        Validation<bool, string> validFlag = Validation<bool, string>.Valid(true);
        var validated = validNumber.Zip(
            validWord,
            validScale,
            validFlag,
            static (n, w, s, f) => (n, w, s, f));
        Assert.Equal(Validation<(int, string, decimal, bool), string>.Valid((2, "dual", 3.5m, true)), validated);
    }

    [Fact]
    public void ComposeInfersBothStagesAndTheResultType()
    {
        Func<string, int> parse = static text => text.Length;
        Func<int, string> format = static value => $"n{value}";
        Func<string, string> composed = parse.Compose(format);

        Assert.Equal("n5", composed("hello"));
    }

    [Fact]
    public async Task ComposeAsyncVerbsResolveByCallbackReturnKindWithFullInference()
    {
        Func<string, Task<int>> taskParse = static text => Task.FromResult(text.Length);
        Func<int, Task<string>> taskFormat = static value => Task.FromResult($"n{value}");
        Func<string, Task<string>> taskBoth = taskParse.ComposeAsync(taskFormat);
        Assert.Equal("n5", await taskBoth("hello"));

        Func<string, CancellationToken, Task<int>> tokenParse =
            static (text, token) => Task.FromResult(text.Length);
        Func<int, CancellationToken, Task<string>> tokenFormat =
            static (value, token) => Task.FromResult($"n{value}");
        Func<string, CancellationToken, Task<string>> tokenBoth = tokenParse.ComposeAsync(tokenFormat);
        Assert.Equal("n5", await tokenBoth("hello", CancellationToken.None));

        Func<string, ValueTask<int>> valueParse = static text => new ValueTask<int>(text.Length);
        Func<int, ValueTask<string>> valueFormat = static value => new ValueTask<string>($"n{value}");
        Func<string, ValueTask<string>> valueBoth = valueParse.ComposeValueAsync(valueFormat);
        Assert.Equal("n5", await valueBoth("hello"));

        Func<string, CancellationToken, ValueTask<int>> tokenValueParse =
            static (text, token) => new ValueTask<int>(text.Length);
        Func<int, CancellationToken, ValueTask<string>> tokenValueFormat =
            static (value, token) => new ValueTask<string>($"n{value}");
        Func<string, CancellationToken, ValueTask<string>> tokenValueBoth =
            tokenValueParse.ComposeValueAsync(tokenValueFormat);
        Assert.Equal("n5", await tokenValueBoth("hello", CancellationToken.None));
    }

    [Fact]
    public void FunctionAdaptersInferBinaryShapesAndArgumentPositions()
    {
        Func<string, int, decimal> scale = static (text, factor) => text.Length * factor;
        var curried = scale.Curry();
        var uncurried = curried.Uncurry();
        var partiallyApplied = scale.Partial("dual");
        var flipped = scale.Flip();

        Assert.Equal(8m, curried("dual")(2));
        Assert.Equal(6m, uncurried("abc", 2));
        Assert.Equal(8m, partiallyApplied(2));
        Assert.Equal(6m, flipped(2, "abc"));
    }

    [Fact]
    public void ToResultFamilyInfersProducedValueAndErrorTypes()
    {
        var produced = UnitResult<string>.Success().ToResult(static () => 42);
        Assert.Equal(Result<int, string>.Success(42), produced);

        var preserved = UnitResult<string>.Failure("boom").ToResult(static () => 42);
        Assert.Equal(Result<int, string>.Failure("boom"), preserved);

        var recovered = Option<int>.None.ToResult(static () => "missing");
        Assert.Equal(Result<int, string>.Failure("missing"), recovered);

        // The error factory returns a subtype of the intended error type, so the
        // intended TError is stated explicitly.
        var widened = Option<int>.None.ToResult<int, Exception>(
            static () => new FormatException("missing"));
        Assert.True(widened.TryGetError(out var error));
        Assert.IsType<FormatException>(error);
    }

    [Fact]
    public void ScanInfersAnAccumulatorTypeDistinctFromTheElementType()
    {
        var lengths = new[] { "a", "abc", "ab" }.Scan(
            0,
            static (total, text) => total + text.Length);

        Assert.Equal([1, 4, 6], lengths.ToArray());
    }

    [Fact]
    public async Task ScanAsyncFormsInferTheAccumulatorAndResolveTheTokenAwareOverload()
    {
        var scanned = await AsyncValues("a", "abc")
            .Scan(0, static (total, text) => total + text.Length)
            .ToListAsync();
        var scannedAsync = await AsyncValues("a", "abc")
            .ScanValueAsync(0, (total, text) => ValueTask.FromResult(total + text.Length))
            .ToListAsync();
        var scannedTokenAware = await AsyncValues("a", "abc")
            .ScanValueAsync(0, (total, text, token) => ValueTask.FromResult(total + text.Length))
            .ToListAsync();

        Assert.Equal([1, 4], scanned);
        Assert.Equal([1, 4], scannedAsync);
        Assert.Equal([1, 4], scannedTokenAware);
    }

    private static async IAsyncEnumerable<T> AsyncValues<T>(params T[] values)
    {
        foreach (var value in values)
        {
            yield return value;
            await Task.Yield();
        }
    }
}

#pragma warning restore xUnit1051
