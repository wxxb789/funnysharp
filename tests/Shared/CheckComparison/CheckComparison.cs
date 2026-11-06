using FunnySharp;

namespace FunnySharp.TestSupport.Checks;

internal static class CheckComparison
{
    internal static void Verify()
    {
        foreach (var fixture in CheckFixtures.All)
        {
            VerifyFixture(fixture);
        }

        VerifyEmptyRules();
        VerifyRepeatedInvocation();
    }

    internal static async Task VerifyAsync()
    {
        foreach (var fixture in CheckFixtures.All)
        {
            await VerifyFixtureAsync(fixture).ConfigureAwait(false);
        }

        await VerifyEmptyRulesAsync().ConfigureAwait(false);
    }

    private static void VerifyFixture(CheckFixture fixture)
    {
        var input = Input(fixture);

        var currentLog = new CheckLog();
        var currentRules = new CheckRuleSet(currentLog.Add);
        var current = OrdinaryCheckConsumer.FailFast(
            input,
            currentRules.HasStock,
            currentRules.IsPermitted,
            currentRules.NotFraudulent);

        var newLog = new CheckLog();
        var newRules = new CheckRuleSet(newLog.Add);
        var actual = FunnySharpCheckConsumer.FailFast(
            input,
            newRules.HasStock,
            newRules.IsPermitted,
            newRules.NotFraudulent);

        AssertSameResult(current, actual, fixture.Name + " fail-fast");
        AssertSameLog(currentLog, newLog, fixture.Name + " fail-fast callbacks");

        currentLog.Clear();
        newLog.Clear();

        var currentValidation = OrdinaryCheckConsumer.Accumulate(
            input,
            currentRules.SyncRules());
        var newValidation = FunnySharpCheckConsumer.Accumulate(
            input,
            newRules.SyncRules());

        AssertSameValidation(currentValidation, newValidation, fixture.Name + " accumulation");
        AssertSameLog(currentLog, newLog, fixture.Name + " accumulation callbacks");
    }

    private static async Task VerifyFixtureAsync(CheckFixture fixture)
    {
        var input = Input(fixture);

        var currentLog = new CheckLog();
        var currentRules = new CheckRuleSet(currentLog.Add);
        var current = await OrdinaryCheckConsumer
            .FailFastAsync(
                input,
                currentRules.HasStockAsync,
                currentRules.IsPermittedAsync,
                currentRules.NotFraudulentAsync)
            .ConfigureAwait(false);

        var newLog = new CheckLog();
        var newRules = new CheckRuleSet(newLog.Add);
        var actual = await FunnySharpCheckConsumer
            .FailFastAsync(
                input,
                newRules.HasStockAsync,
                newRules.IsPermittedAsync,
                newRules.NotFraudulentAsync)
            .ConfigureAwait(false);

        AssertSameResult(current, actual, fixture.Name + " async fail-fast");
        AssertSameLog(currentLog, newLog, fixture.Name + " async fail-fast callbacks");

        currentLog.Clear();
        newLog.Clear();

        var currentValidation = await OrdinaryCheckConsumer
            .AccumulateAsync(input, currentRules.TaskRules())
            .ConfigureAwait(false);
        var newValidation = await FunnySharpCheckConsumer
            .AccumulateAsync(input, newRules.TaskRules())
            .ConfigureAwait(false);

        AssertSameValidation(
            currentValidation,
            newValidation,
            fixture.Name + " async accumulation");
        AssertSameLog(currentLog, newLog, fixture.Name + " async accumulation callbacks");

        currentLog.Clear();
        newLog.Clear();

        var currentValueValidation = await OrdinaryCheckConsumer
            .AccumulateValueAsync(input, currentRules.ValueRules())
            .ConfigureAwait(false);
        var newValueValidation = await FunnySharpCheckConsumer
            .AccumulateValueAsync(input, newRules.ValueRules())
            .ConfigureAwait(false);

        AssertSameValidation(
            currentValueValidation,
            newValueValidation,
            fixture.Name + " value-task accumulation");
        AssertSameLog(currentLog, newLog, fixture.Name + " value-task accumulation callbacks");
    }

    private static void VerifyEmptyRules()
    {
        foreach (var fixture in CheckFixtures.All)
        {
            var input = Input(fixture);

            var current = OrdinaryCheckConsumer.Accumulate(
                input,
                Array.Empty<Func<CheckOrder, UnitResult<CheckError>>>());
            var actual = FunnySharpCheckConsumer.Accumulate(
                input,
                Array.Empty<Func<CheckOrder, UnitResult<CheckError>>>());

            AssertSameValidation(current, actual, fixture.Name + " empty rules");

            var validatedCurrent = Validation<CheckOrder, CheckError>.Valid(CheckFixtures.ValidOrder);
            var validatedActual = validatedCurrent.CheckAll(
                Array.Empty<Func<CheckOrder, UnitResult<CheckError>>>());

            AssertSameValidation(
                validatedCurrent,
                validatedActual,
                fixture.Name + " empty validation rules");
        }

        var invalid = Validation<CheckOrder, CheckError>.Invalid(
            new CheckError("existing", "the order was already rejected"));
        var invalidActual = invalid.CheckAll(
            Array.Empty<Func<CheckOrder, UnitResult<CheckError>>>());

        AssertSameValidation(invalid, invalidActual, "invalid validation with empty rules");
    }

    private static async Task VerifyEmptyRulesAsync()
    {
        foreach (var fixture in CheckFixtures.All)
        {
            var input = Input(fixture);

            var current = await OrdinaryCheckConsumer
                .AccumulateAsync(input, Array.Empty<Func<CheckOrder, Task<UnitResult<CheckError>>>>())
                .ConfigureAwait(false);
            var actual = await FunnySharpCheckConsumer
                .AccumulateAsync(input, Array.Empty<Func<CheckOrder, Task<UnitResult<CheckError>>>>())
                .ConfigureAwait(false);

            AssertSameValidation(current, actual, fixture.Name + " async empty rules");

            var currentValue = await OrdinaryCheckConsumer
                .AccumulateValueAsync(
                    input,
                    Array.Empty<Func<CheckOrder, ValueTask<UnitResult<CheckError>>>>())
                .ConfigureAwait(false);
            var actualValue = await FunnySharpCheckConsumer
                .AccumulateValueAsync(
                    input,
                    Array.Empty<Func<CheckOrder, ValueTask<UnitResult<CheckError>>>>())
                .ConfigureAwait(false);

            AssertSameValidation(currentValue, actualValue, fixture.Name + " value-task empty rules");
        }
    }

    private static void VerifyRepeatedInvocation()
    {
        var log = new CheckLog();
        var rules = new CheckRuleSet(log.Add);
        var input = Result<CheckOrder, CheckError>.Success(CheckFixtures.ValidOrder);

        var first = FunnySharpCheckConsumer.Accumulate(input, rules.SyncRules());
        var second = FunnySharpCheckConsumer.Accumulate(input, rules.SyncRules());

        AssertSameValidation(first, second, "repeated invocation outcome");

        string[] expected = ["stock", "permission", "fraud", "stock", "permission", "fraud"];
        if (!log.Entries.SequenceEqual(expected, StringComparer.Ordinal))
        {
            throw new InvalidOperationException(
                "Repeated invocation did not run every rule once per call: ["
                + string.Join(", ", log.Entries)
                + "].");
        }
    }

    private static Result<CheckOrder, CheckError> Input(CheckFixture fixture) =>
        fixture.ExistingError is null
            ? Result<CheckOrder, CheckError>.Success(fixture.Order)
            : Result<CheckOrder, CheckError>.Failure(fixture.ExistingError);

    private static void AssertSameResult(
        Result<CheckOrder, CheckError> expected,
        Result<CheckOrder, CheckError> actual,
        string context)
    {
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException(
                $"{context}: results differ. Expected {expected}; actual {actual}.");
        }

        var expectedSuccess = expected.TryGetValue(out var expectedValue);
        var actualSuccess = actual.TryGetValue(out var actualValue);

        if (expectedSuccess != actualSuccess)
        {
            throw new InvalidOperationException($"{context}: success cases differ.");
        }

        if (expectedSuccess && !ReferenceEquals(expectedValue, actualValue))
        {
            throw new InvalidOperationException(
                $"{context}: successful input value identity was not preserved.");
        }
    }

    private static void AssertSameValidation(
        Validation<CheckOrder, CheckError> expected,
        Validation<CheckOrder, CheckError> actual,
        string context)
    {
        if (!expected.Equals(actual))
        {
            throw new InvalidOperationException(
                $"{context}: validations differ. Expected {expected}; actual {actual}.");
        }

        var expectedValid = expected.TryGetValue(out var expectedValue);
        var actualValid = actual.TryGetValue(out var actualValue);

        if (expectedValid != actualValid)
        {
            throw new InvalidOperationException($"{context}: validity cases differ.");
        }

        if (expectedValid && !ReferenceEquals(expectedValue, actualValue))
        {
            throw new InvalidOperationException(
                $"{context}: successful input value identity was not preserved.");
        }
    }

    private static void AssertSameLog(CheckLog expected, CheckLog actual, string context)
    {
        if (!expected.Entries.SequenceEqual(actual.Entries, StringComparer.Ordinal))
        {
            throw new InvalidOperationException(
                $"{context}: callback order differs. Expected [{string.Join(", ", expected.Entries)}]; "
                + $"actual [{string.Join(", ", actual.Entries)}].");
        }
    }
}
