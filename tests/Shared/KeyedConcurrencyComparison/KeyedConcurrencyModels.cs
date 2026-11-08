using System.Collections.Concurrent;
using System.Collections.Generic;

namespace FunnySharp.TestSupport.KeyedConcurrencyComparison;

internal sealed record AccountEvent(string AccountId, int Sequence, decimal Amount);

internal sealed record AccountError(string Code, string Message);

internal readonly record struct KeyedOutcome(
    string AccountId,
    AccountEvent Event,
    Result<decimal, AccountError> Outcome);

internal readonly record struct KeyTransition(string AccountId, int Sequence, bool Started);

internal sealed record KeyedConsumerReport(
    IReadOnlyList<KeyedOutcome> Outcomes,
    IReadOnlyDictionary<string, decimal> FinalBalances,
    int MaxInFlight,
    int PerKeyOverlapViolations,
    int MaxRetainedSourceItems,
    int SourceItemsYielded,
    IReadOnlyList<KeyTransition> Transitions);

internal sealed class AccountProcessor
{
    internal const string InsufficientFunds = "insufficient-funds";

    private readonly ConcurrentDictionary<string, decimal> balances = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, int> activeByKey = new(StringComparer.Ordinal);
    private readonly object transitionsGate = new();
    private readonly List<KeyTransition> transitions = [];
    private int inFlight;
    private int maxInFlight;
    private int perKeyOverlapViolations;

    internal int MaxInFlight => Volatile.Read(ref maxInFlight);

    internal int PerKeyOverlapViolations => Volatile.Read(ref perKeyOverlapViolations);

    internal IReadOnlyList<KeyTransition> SnapshotTransitions()
    {
        lock (transitionsGate)
        {
            return [.. transitions];
        }
    }

    internal IReadOnlyDictionary<string, decimal> Snapshot()
    {
        var snapshot = new SortedDictionary<string, decimal>(StringComparer.Ordinal);
        foreach (var pair in balances)
        {
            snapshot[pair.Key] = pair.Value;
        }

        return snapshot;
    }

    internal async ValueTask<Result<decimal, AccountError>> ProcessAsync(
        AccountEvent accountEvent,
        CancellationToken cancellationToken)
    {
        RecordTransition(accountEvent, started: true);

        var activeForAccount = activeByKey.AddOrUpdate(
            accountEvent.AccountId,
            1,
            static (_, current) => current + 1);
        if (activeForAccount > 1)
        {
            Interlocked.Increment(ref perKeyOverlapViolations);
        }

        var currentInFlight = Interlocked.Increment(ref inFlight);
        var observedMaximum = Volatile.Read(ref maxInFlight);
        while (currentInFlight > observedMaximum)
        {
            var previous = Interlocked.CompareExchange(
                ref maxInFlight,
                currentInFlight,
                observedMaximum);
            if (previous == observedMaximum)
            {
                break;
            }

            observedMaximum = previous;
        }

        try
        {
            // One real asynchronous continuation models an I/O boundary without wall-clock timing.
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            var current = balances.GetOrAdd(accountEvent.AccountId, 0m);
            var next = current + accountEvent.Amount;
            if (next < 0)
            {
                return Result<decimal, AccountError>.Failure(
                    new AccountError(
                        InsufficientFunds,
                        $"Account '{accountEvent.AccountId}' cannot apply {accountEvent.Amount}."));
            }

            balances[accountEvent.AccountId] = next;
            return Result<decimal, AccountError>.Success(next);
        }
        finally
        {
            RecordTransition(accountEvent, started: false);
            Interlocked.Decrement(ref inFlight);
            activeByKey.AddOrUpdate(
                accountEvent.AccountId,
                0,
                static (_, current) => current - 1);
        }
    }

    private void RecordTransition(AccountEvent accountEvent, bool started)
    {
        lock (transitionsGate)
        {
            transitions.Add(new KeyTransition(accountEvent.AccountId, accountEvent.Sequence, started));
        }
    }

    internal static Result<decimal, AccountError> ApplySequentially(
        IDictionary<string, decimal> balances,
        AccountEvent accountEvent)
    {
        balances.TryGetValue(accountEvent.AccountId, out var current);
        var next = current + accountEvent.Amount;
        if (next < 0)
        {
            return Result<decimal, AccountError>.Failure(
                new AccountError(
                    InsufficientFunds,
                    $"Account '{accountEvent.AccountId}' cannot apply {accountEvent.Amount}."));
        }

        balances[accountEvent.AccountId] = next;
        return Result<decimal, AccountError>.Success(next);
    }
}

internal sealed class CountingAsyncSource<T>(
    IReadOnlyList<T> items,
    Func<int> delivered) : IAsyncEnumerable<T>
{
    private int itemsYielded;
    private int maxRetained;
    private int disposeCount;

    internal int ItemsYielded => Volatile.Read(ref itemsYielded);

    internal int MaxRetained => Volatile.Read(ref maxRetained);

    internal int DisposeCount => Volatile.Read(ref disposeCount);

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
        new Enumerator(this, items, delivered);

    private sealed class Enumerator(
        CountingAsyncSource<T> owner,
        IReadOnlyList<T> items,
        Func<int> delivered) : IAsyncEnumerator<T>
    {
        private int index = -1;

        public T Current => items[index];

        public ValueTask<bool> MoveNextAsync()
        {
            var next = index + 1;
            if (next >= items.Count)
            {
                return ValueTask.FromResult(false);
            }

            index = next;
            var yielded = Interlocked.Increment(ref owner.itemsYielded);
            var retained = yielded - delivered();
            var observedMaximum = Volatile.Read(ref owner.maxRetained);
            while (retained > observedMaximum)
            {
                var previous = Interlocked.CompareExchange(
                    ref owner.maxRetained,
                    retained,
                    observedMaximum);
                if (previous == observedMaximum)
                {
                    break;
                }

                observedMaximum = previous;
            }

            return ValueTask.FromResult(true);
        }

        public ValueTask DisposeAsync()
        {
            Interlocked.Increment(ref owner.disposeCount);
            return ValueTask.CompletedTask;
        }
    }
}

internal readonly record struct ExpectedKeyOutcome(int Sequence, bool Success, string? ErrorCode);

internal sealed record ExpectedKeyReport(
    IReadOnlyList<ExpectedKeyOutcome> Outcomes,
    decimal FinalBalance);

internal static class AccountScript
{
    internal static IReadOnlyList<AccountEvent> FixedEvents { get; } =
    [
        new("ACC-1", 0, 100m),
        new("ACC-2", 0, 50m),
        new("ACC-3", 0, -10m),
        new("ACC-4", 0, 10m),
        new("ACC-5", 0, 7m),
        new("ACC-1", 1, -30m),
        new("ACC-2", 1, -20m),
        new("ACC-3", 1, 25m),
        new("ACC-4", 1, 20m),
        new("ACC-5", 1, -7m),
        new("ACC-1", 2, 5m),
        new("ACC-2", 2, -40m),
        new("ACC-3", 2, 5m),
        new("ACC-4", 2, -5m),
        new("ACC-5", 2, 1m),
        new("ACC-6", 0, 1000m),
        new("ACC-6", 1, -500m),
        new("ACC-6", 2, -500m),
    ];

    internal static IReadOnlyDictionary<string, ExpectedKeyReport> Expected { get; } =
        new SortedDictionary<string, ExpectedKeyReport>(StringComparer.Ordinal)
        {
            ["ACC-1"] = new(
                [new(0, true, null), new(1, true, null), new(2, true, null)],
                75m),
            ["ACC-2"] = new(
                [
                    new(0, true, null),
                    new(1, true, null),
                    new(2, false, AccountProcessor.InsufficientFunds),
                ],
                30m),
            ["ACC-3"] = new(
                [
                    new(0, false, AccountProcessor.InsufficientFunds),
                    new(1, true, null),
                    new(2, true, null),
                ],
                30m),
            ["ACC-4"] = new(
                [new(0, true, null), new(1, true, null), new(2, true, null)],
                25m),
            ["ACC-5"] = new(
                [new(0, true, null), new(1, true, null), new(2, true, null)],
                1m),
            ["ACC-6"] = new(
                [new(0, true, null), new(1, true, null), new(2, true, null)],
                0m),
        };

    internal static IReadOnlyList<AccountEvent> CreateSkewed(int hotEvents, int coldAccounts, int coldEvents)
    {
        var events = new List<AccountEvent>(hotEvents + (coldAccounts * coldEvents));
        for (var index = 0; index < hotEvents; index++)
        {
            events.Add(new("HOT-0", index, index % 3 == 0 ? 5m : -1m));
        }

        for (var account = 0; account < coldAccounts; account++)
        {
            for (var index = 0; index < coldEvents; index++)
            {
                events.Add(new($"COLD-{account}", index, 1m + account));
            }
        }

        return events;
    }

    internal static IReadOnlyList<AccountEvent> CreateInterleavedSkew(
        int hotEvents,
        int coldAccounts,
        int coldEvents)
    {
        var events = new List<AccountEvent>(hotEvents + (coldAccounts * coldEvents));
        var coldSequences = new int[coldAccounts];
        var coldEventsAdded = 0;
        for (var index = 0; index < Math.Max(hotEvents, coldAccounts * coldEvents); index++)
        {
            if (index < hotEvents)
            {
                events.Add(new("HOT-0", index, index % 3 == 0 ? 5m : -1m));
            }

            if (coldEventsAdded < coldAccounts * coldEvents)
            {
                var account = index % coldAccounts;
                events.Add(new($"COLD-{account}", coldSequences[account], 1m + account));
                coldSequences[account]++;
                coldEventsAdded++;
            }
        }

        return events;
    }
}
