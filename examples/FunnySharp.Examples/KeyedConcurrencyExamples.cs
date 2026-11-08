using FunnySharp;
using System.Collections.Concurrent;


internal static class KeyedConcurrencyExamples
{
    internal static async Task VerifyAsync()
    {
        var events = new[]
        {
            new AccountEvent("ACC-1", 0, 100m),
            new AccountEvent("ACC-2", 0, -10m),
            new AccountEvent("ACC-3", 0, 40m),
            new AccountEvent("ACC-1", 1, -30m),
            new AccountEvent("ACC-2", 1, 25m),
            new AccountEvent("ACC-3", 1, 5m),
        };
        var balances = new ConcurrentDictionary<string, decimal>(StringComparer.Ordinal);
        var observations = new List<(string AccountId, int Sequence, Result<decimal, string> Outcome)>();

        await foreach (var (accountId, accountEvent, outcome) in Source(events)
            .SelectParallelByKeyCompletionOrderValueAsync(
                static accountEvent => accountEvent.AccountId,
                maxConcurrency: 3,
                maxBufferedItems: 6,
                (accountEvent, cancellationToken) =>
                    ApplyAccountEventAsync(accountEvent, balances, cancellationToken)))
        {
            observations.Add((accountId, accountEvent.Sequence, outcome));
        }

        ExampleAssertions.Equal(events.Length, observations.Count);
        ExampleAssertions.Equal(70m, balances["ACC-1"]);
        ExampleAssertions.Equal(25m, balances["ACC-2"]);
        ExampleAssertions.Equal(45m, balances["ACC-3"]);

        foreach (var group in observations.GroupBy(static observation => observation.AccountId))
        {
            ExampleAssertions.SequenceEqual(
                group.Select(static observation => observation.Sequence).ToArray(),
                Enumerable.Range(0, group.Count()).ToArray());
        }

        var failed = observations.Single(static observation =>
            observation.AccountId == "ACC-2" && observation.Sequence == 0);
        ExampleAssertions.True(failed.Outcome.IsFailure, "The first ACC-2 event must fail.");
    }

    private static async IAsyncEnumerable<AccountEvent> Source(IReadOnlyList<AccountEvent> events)
    {
        foreach (var accountEvent in events)
        {
            yield return accountEvent;
        }

        await Task.CompletedTask;
    }

    private static async ValueTask<Result<decimal, string>> ApplyAccountEventAsync(
        AccountEvent accountEvent,
        ConcurrentDictionary<string, decimal> balances,
        CancellationToken cancellationToken)
    {
        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();

        var current = balances.GetOrAdd(accountEvent.AccountId, 0m);
        var next = current + accountEvent.Amount;
        if (next < 0)
        {
            return Result<decimal, string>.Failure("insufficient funds");
        }

        balances[accountEvent.AccountId] = next;
        return Result<decimal, string>.Success(next);
    }

    private sealed record AccountEvent(string AccountId, int Sequence, decimal Amount);
}
