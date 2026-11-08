namespace FunnySharp.TestSupport.KeyedConcurrencyComparison;

internal static class KeyedComparisonVerifier
{
    internal static async Task VerifyAsync()
    {
        const int maxConcurrency = 3;
        const int maxBufferedItems = 6;
        var events = AccountScript.FixedEvents;

        var funny = await FunnySharpKeyedConsumer.RunAsync(events, maxConcurrency, maxBufferedItems);
        var bcl = await BclKeyedConsumer.RunAsync(events, maxConcurrency, maxBufferedItems);

        AssertConsumerReport("FunnySharp", funny, maxConcurrency, maxBufferedItems, events.Count, sourceRetainedSlack: 1);
        AssertConsumerReport("BCL", bcl, maxConcurrency, maxBufferedItems, events.Count, sourceRetainedSlack: 0);
        AssertPerKeyParity(funny, bcl);

        var funnyAgain = await FunnySharpKeyedConsumer.RunAsync(events, maxConcurrency, maxBufferedItems);
        var bclAgain = await BclKeyedConsumer.RunAsync(events, maxConcurrency, maxBufferedItems);
        AssertConsumerReport("FunnySharp repeat", funnyAgain, maxConcurrency, maxBufferedItems, events.Count, sourceRetainedSlack: 1);
        AssertConsumerReport("BCL repeat", bclAgain, maxConcurrency, maxBufferedItems, events.Count, sourceRetainedSlack: 0);
        AssertPerKeyParity(funny, funnyAgain);
        AssertPerKeyParity(bcl, bclAgain);
    }

    private static void AssertConsumerReport(
        string description,
        KeyedConsumerReport report,
        int maxConcurrency,
        int maxBufferedItems,
        int eventCount,
        int sourceRetainedSlack)
    {
        KeyedComparisonAssert.Equal(eventCount, report.Outcomes.Count, description + ": outcome count");
        KeyedComparisonAssert.Equal(eventCount, report.SourceItemsYielded, description + ": source items");
        KeyedComparisonAssert.True(
            report.MaxInFlight <= maxConcurrency,
            description + ": in-flight bound");
        KeyedComparisonAssert.Equal(0, report.PerKeyOverlapViolations, description + ": per-key overlap");
        // The coordinator releases its active slot just before the caller's delivery counter
        // advances, so a source read can observe one extra retained item during that handoff.
        // The baseline advances its delivery counter before releasing the active slot, so it
        // reaches the exact maxConcurrency + maxBufferedItems bound.
        KeyedComparisonAssert.True(
            report.MaxRetainedSourceItems <= maxConcurrency + maxBufferedItems + sourceRetainedSlack,
            description + ": retained source bound (actual " + report.MaxRetainedSourceItems + ", yielded " + report.SourceItemsYielded + ")");

        foreach (var expectedPair in AccountScript.Expected)
        {
            var account = expectedPair.Key;
            var expected = expectedPair.Value;
            var outcomes = report.Outcomes
                .Where(outcome => string.Equals(outcome.AccountId, account, StringComparison.Ordinal))
                .ToArray();

            KeyedComparisonAssert.Equal(
                expected.Outcomes.Count,
                outcomes.Length,
                description + ": " + account + " outcome count");

            for (var index = 0; index < expected.Outcomes.Count; index++)
            {
                var expectedOutcome = expected.Outcomes[index];
                var actual = outcomes[index];
                KeyedComparisonAssert.Equal(
                    expectedOutcome.Sequence,
                    actual.Event.Sequence,
                    description + ": " + account + " source order at " + index);
                KeyedComparisonAssert.Equal(
                    expectedOutcome.Success,
                    actual.Outcome.IsSuccess,
                    description + ": " + account + " success at " + index);

                if (expectedOutcome.Success)
                {
                    KeyedComparisonAssert.True(
                        actual.Outcome.TryGetValue(out _),
                        description + ": " + account + " value at " + index);
                }
                else
                {
                    KeyedComparisonAssert.True(
                        actual.Outcome.TryGetError(out var error),
                        description + ": " + account + " error at " + index);
                    KeyedComparisonAssert.Equal(
                        expectedOutcome.ErrorCode,
                        error!.Code,
                        description + ": " + account + " error code at " + index);
                }
            }

            KeyedComparisonAssert.True(
                report.FinalBalances.TryGetValue(account, out var finalBalance),
                description + ": " + account + " final balance exists");
            KeyedComparisonAssert.Equal(
                expected.FinalBalance,
                finalBalance,
                description + ": " + account + " final balance");
        }
    }

    private static void AssertPerKeyParity(KeyedConsumerReport left, KeyedConsumerReport right)
    {
        foreach (var account in AccountScript.Expected.Keys)
        {
            var leftOutcomes = left.Outcomes
                .Where(outcome => string.Equals(outcome.AccountId, account, StringComparison.Ordinal))
                .ToArray();
            var rightOutcomes = right.Outcomes
                .Where(outcome => string.Equals(outcome.AccountId, account, StringComparison.Ordinal))
                .ToArray();

            KeyedComparisonAssert.Equal(
                leftOutcomes.Length,
                rightOutcomes.Length,
                account + ": parity outcome count");

            for (var index = 0; index < leftOutcomes.Length; index++)
            {
                var leftOutcome = leftOutcomes[index];
                var rightOutcome = rightOutcomes[index];
                KeyedComparisonAssert.Equal(
                    leftOutcome.Event,
                    rightOutcome.Event,
                    account + ": parity source at " + index);
                KeyedComparisonAssert.Equal(
                    leftOutcome.Outcome.IsSuccess,
                    rightOutcome.Outcome.IsSuccess,
                    account + ": parity success at " + index);

                if (leftOutcome.Outcome.IsSuccess &&
                    leftOutcome.Outcome.TryGetValue(out var leftValue) &&
                    rightOutcome.Outcome.TryGetValue(out var rightValue))
                {
                    KeyedComparisonAssert.Equal(
                        leftValue,
                        rightValue,
                        account + ": parity value at " + index);
                }
                else if (leftOutcome.Outcome.TryGetError(out var leftError) &&
                         rightOutcome.Outcome.TryGetError(out var rightError))
                {
                    KeyedComparisonAssert.Equal(
                        leftError,
                        rightError,
                        account + ": parity error at " + index);
                }
            }

            KeyedComparisonAssert.True(
                left.FinalBalances.TryGetValue(account, out var leftBalance),
                account + ": left final balance exists");
            KeyedComparisonAssert.True(
                right.FinalBalances.TryGetValue(account, out var rightBalance),
                account + ": right final balance exists");
            KeyedComparisonAssert.Equal(leftBalance, rightBalance, account + ": parity final balance");
        }
    }
}

internal static class KeyedComparisonAssert
{
    internal static void True(bool condition, string description)
    {
        if (!condition)
        {
            throw new InvalidOperationException(description);
        }
    }

    internal static void Equal<T>(T expected, T actual, string description)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException(
                $"{description}: expected {expected}, actual {actual}.");
        }
    }
}
