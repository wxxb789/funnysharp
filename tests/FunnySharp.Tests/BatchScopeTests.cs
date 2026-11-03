#pragma warning disable xUnit1051

using FunnySharp;

namespace FunnySharp.Tests;

public sealed class BatchScopeTests
{
    [Fact]
    public async Task IndependentReadsAreGroupedAndDeduplicatedWithinOneExecution()
    {
        var customers = new RecordingBatchSource<string, string, string>(
            "customers",
            "tenant-a",
            StringComparer.Ordinal,
            static (keys, _, _) => new(Result<IReadOnlyList<BatchSourceEntry<string, string, string>>, string>.Success(
                keys.Select(key => BatchSourceEntry<string, string, string>.Found(key, $"value:{key}")).ToArray())));

        var requirement = BatchRequirement.All(
        [
            BatchRequirement.Read(customers.Source, "c1"),
            BatchRequirement.Read(customers.Source, "c2"),
            BatchRequirement.Read(customers.Source, "c1"),
            BatchRequirement.Read(customers.Source, "c3"),
            BatchRequirement.Read(customers.Source, "c2"),
        ]);

        await using var scope = BatchScope.Create();
        var outer = await scope.ExecuteAsync(requirement);

        Assert.True(outer.TryGetValue(out var results));
        Assert.Equal(5, results.Count);
        Assert.True(results[0].TryGetValue(out var first));
        Assert.Equal("value:c1", first);
        Assert.True(results[2].TryGetValue(out var repeated));
        Assert.Equal("value:c1", repeated);
        Assert.Equal(1, customers.InvocationCount);
        Assert.Equal(["c1", "c2", "c3"], customers.Batches[0]);
    }

    [Fact]
    public async Task MaxBatchSizeSplitsKeysInFirstRequestOrder()
    {
        var values = new RecordingBatchSource<string, int, string>(
            "values",
            "tenant-a",
            StringComparer.Ordinal,
            static (keys, _, _) => new(Result<IReadOnlyList<BatchSourceEntry<string, int, string>>, string>.Success(
                keys.Select((key, index) => BatchSourceEntry<string, int, string>.Found(key, index)).ToArray())));

        var requirement = BatchRequirement.All(
        [
            BatchRequirement.Read(values.Source, "a"),
            BatchRequirement.Read(values.Source, "b"),
            BatchRequirement.Read(values.Source, "c"),
            BatchRequirement.Read(values.Source, "d"),
            BatchRequirement.Read(values.Source, "e"),
        ]);

        await using var scope = BatchScope.Create(new BatchScopeOptions { MaxBatchSize = 2 });
        var outcome = await scope.ExecuteAsync(requirement);

        Assert.True(outcome.TryGetValue(out _));
        Assert.Equal(3, values.InvocationCount);
        Assert.Equal(["a", "b"], values.Batches[0]);
        Assert.Equal(["c", "d"], values.Batches[1]);
        Assert.Equal(["e"], values.Batches[2]);
    }

    [Fact]
    public async Task IndependentSourcesReceiveTheirOwnBatches()
    {
        var left = new RecordingBatchSource<string, int, string>(
            "left",
            "tenant-a",
            StringComparer.Ordinal,
            static (keys, _, _) => new(Result<IReadOnlyList<BatchSourceEntry<string, int, string>>, string>.Success(
            [
                BatchSourceEntry<string, int, string>.Found("l1", 1),
            ])));
        var right = new RecordingBatchSource<string, int, string>(
            "right",
            "tenant-a",
            StringComparer.Ordinal,
            static (keys, _, _) => new(Result<IReadOnlyList<BatchSourceEntry<string, int, string>>, string>.Success(
            [
                BatchSourceEntry<string, int, string>.Found("r1", 2),
            ])));

        var requirement = BatchRequirement.Zip(
            BatchRequirement.Read(left.Source, "l1"),
            BatchRequirement.Read(right.Source, "r1"));

        await using var scope = BatchScope.Create();
        var outcome = await scope.ExecuteAsync(requirement);

        Assert.True(outcome.TryGetValue(out var pair));
        Assert.Equal((1, 2), pair);
        Assert.Equal(1, left.InvocationCount);
        Assert.Equal(1, right.InvocationCount);
    }

    [Fact]
    public async Task MaxConcurrentBatchesLimitsStartedSourceCalls()
    {
        var firstStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirst = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseSecond = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        var first = new RecordingBatchSource<string, int, string>(
            "first",
            "tenant-a",
            StringComparer.Ordinal,
            async (keys, _, _) =>
            {
                firstStarted.TrySetResult(true);
                await releaseFirst.Task;
                return Result<IReadOnlyList<BatchSourceEntry<string, int, string>>, string>.Success(
                [
                    BatchSourceEntry<string, int, string>.Found("a", 1),
                ]);
            });
        var second = new RecordingBatchSource<string, int, string>(
            "second",
            "tenant-a",
            StringComparer.Ordinal,
            async (keys, _, _) =>
            {
                secondStarted.TrySetResult(true);
                await releaseSecond.Task;
                return Result<IReadOnlyList<BatchSourceEntry<string, int, string>>, string>.Success(
                [
                    BatchSourceEntry<string, int, string>.Found("b", 2),
                ]);
            });

        var requirement = BatchRequirement.Zip(
            BatchRequirement.Read(first.Source, "a"),
            BatchRequirement.Read(second.Source, "b"));

        await using var scope = BatchScope.Create(new BatchScopeOptions { MaxConcurrentBatches = 1 });
        var execution = scope.ExecuteAsync(requirement).AsTask();

        await firstStarted.Task;
        Assert.Equal(1, first.InvocationCount);
        Assert.Equal(0, second.InvocationCount);

        releaseFirst.SetResult(true);
        await secondStarted.Task;
        releaseSecond.SetResult(true);

        var outcome = await execution;
        Assert.True(outcome.TryGetValue(out var pair));
        Assert.Equal((1, 2), pair);
    }

    [Fact]
    public async Task SourceResponseOrderDoesNotChangeRequestAssociation()
    {
        var values = new RecordingBatchSource<string, int, string>(
            "values",
            "tenant-a",
            StringComparer.Ordinal,
            static (keys, _, _) => new(Result<IReadOnlyList<BatchSourceEntry<string, int, string>>, string>.Success(
            [
                BatchSourceEntry<string, int, string>.Found("b", 2),
                BatchSourceEntry<string, int, string>.Found("a", 1),
            ])));

        var requirement = BatchRequirement.All(
        [
            BatchRequirement.Read(values.Source, "a"),
            BatchRequirement.Read(values.Source, "b"),
        ]);

        await using var scope = BatchScope.Create();
        var outer = await scope.ExecuteAsync(requirement);

        Assert.True(outer.TryGetValue(out var results));
        Assert.True(results[0].TryGetValue(out var a));
        Assert.Equal(1, a);
        Assert.True(results[1].TryGetValue(out var b));
        Assert.Equal(2, b);
    }

    [Fact]
    public async Task SourceAndGlobalInvalidationClearEveryMatchingEntry()
    {
        var values = new RecordingBatchSource<string, int, string>(
            "values",
            "tenant-a",
            StringComparer.Ordinal,
            static (keys, _, _) => new(Result<IReadOnlyList<BatchSourceEntry<string, int, string>>, string>.Success(
                keys.Select(key => BatchSourceEntry<string, int, string>.Found(key, key.Length)).ToArray())));

        var requirement = BatchRequirement.All(
        [
            BatchRequirement.Read(values.Source, "a"),
            BatchRequirement.Read(values.Source, "b"),
        ]);

        await using var scope = BatchScope.Create();
        _ = await scope.ExecuteAsync(requirement);
        Assert.Equal(2, scope.Invalidate(values.Source));

        _ = await scope.ExecuteAsync(requirement);
        Assert.Equal(2, scope.ClearCache());

        _ = await scope.ExecuteAsync(BatchRequirement.Read(values.Source, "a"));
        Assert.Equal(3, values.InvocationCount);
    }

    [Fact]
    public async Task MultipleInFlightBatchesRespectTheConcurrencyBoundAndAllComplete()
    {
        var active = 0;
        var maximumActive = 0;
        var sources = new RecordingBatchSource<string, int, string>[4];
        var requirements = new BatchRequirement<int, string>[4];

        for (var index = 0; index < sources.Length; index++)
        {
            var sourceIndex = index;
            sources[index] = new RecordingBatchSource<string, int, string>(
                $"source-{sourceIndex}",
                "tenant-a",
                StringComparer.Ordinal,
                async (keys, _, cancellationToken) =>
                {
                    var nowActive = Interlocked.Increment(ref active);
                    var observed = Volatile.Read(ref maximumActive);
                    while (nowActive > observed)
                    {
                        var previous = Interlocked.CompareExchange(
                            ref maximumActive,
                            nowActive,
                            observed);
                        if (previous == observed)
                        {
                            break;
                        }

                        observed = previous;
                    }

                    await Task.Delay(10, cancellationToken);
                    Interlocked.Decrement(ref active);
                    return Result<IReadOnlyList<BatchSourceEntry<string, int, string>>, string>.Success(
                    [
                        BatchSourceEntry<string, int, string>.Found(keys[0], sourceIndex),
                    ]);
                });
            requirements[index] = BatchRequirement.Read(sources[index].Source, $"k{sourceIndex}");
        }

        var requirement = BatchRequirement.All(requirements);

        await using var scope = BatchScope.Create(new BatchScopeOptions { MaxConcurrentBatches = 2 });
        var outer = await scope.ExecuteAsync(requirement);

        Assert.True(outer.TryGetValue(out var results));
        Assert.Equal(4, results.Count);
        Assert.All(results, result => Assert.True(result.TryGetValue(out _)));
        Assert.InRange(Volatile.Read(ref maximumActive), 1, 2);
        Assert.Equal(0, Volatile.Read(ref active));
    }

    [Fact]
    public async Task CacheReusesResolvedKeysAcrossSequentialExecutionsUntilInvalidated()
    {
        var customers = new RecordingBatchSource<string, string, string>(
            "customers",
            "tenant-a",
            StringComparer.Ordinal,
            static (keys, _, _) => new(Result<IReadOnlyList<BatchSourceEntry<string, string, string>>, string>.Success(
            [
                BatchSourceEntry<string, string, string>.Found("c1", "Customer One"),
            ])));

        await using var scope = BatchScope.Create();

        var first = await scope.ExecuteAsync(BatchRequirement.Read(customers.Source, "c1"));
        var second = await scope.ExecuteAsync(BatchRequirement.Read(customers.Source, "c1"));

        Assert.True(first.TryGetValue(out var firstValue));
        Assert.True(second.TryGetValue(out var secondValue));
        Assert.Equal("Customer One", firstValue);
        Assert.Equal("Customer One", secondValue);
        Assert.Equal(1, customers.InvocationCount);

        Assert.Equal(1, scope.Invalidate(customers.Source, "c1"));

        var third = await scope.ExecuteAsync(BatchRequirement.Read(customers.Source, "c1"));
        Assert.True(third.TryGetValue(out var thirdValue));
        Assert.Equal("Customer One", thirdValue);
        Assert.Equal(2, customers.InvocationCount);
    }

    [Fact]
    public async Task DisabledCachingRereadsTheSourceAcrossExecutions()
    {
        var customers = new RecordingBatchSource<string, string, string>(
            "customers",
            "tenant-a",
            StringComparer.Ordinal,
            static (keys, _, _) => new(Result<IReadOnlyList<BatchSourceEntry<string, string, string>>, string>.Success(
            [
                BatchSourceEntry<string, string, string>.Found("c1", "Customer One"),
            ])));

        await using var scope = BatchScope.Create(new BatchScopeOptions { CacheResults = false });

        _ = await scope.ExecuteAsync(BatchRequirement.Read(customers.Source, "c1"));
        _ = await scope.ExecuteAsync(BatchRequirement.Read(customers.Source, "c1"));

        Assert.Equal(2, customers.InvocationCount);
    }

    [Fact]
    public async Task CacheCapacityEvictsInsertionOrderAndBoundsRetainedResults()
    {
        var values = new RecordingBatchSource<string, int, string>(
            "values",
            "tenant-a",
            StringComparer.Ordinal,
            static (keys, _, _) => new(Result<IReadOnlyList<BatchSourceEntry<string, int, string>>, string>.Success(
                keys.Select(key => BatchSourceEntry<string, int, string>.Found(key, key.Length)).ToArray())));

        await using var scope = BatchScope.Create(new BatchScopeOptions { MaxCachedEntries = 2 });

        _ = await scope.ExecuteAsync(BatchRequirement.Read(values.Source, "a"));
        _ = await scope.ExecuteAsync(BatchRequirement.Read(values.Source, "b"));
        _ = await scope.ExecuteAsync(BatchRequirement.Read(values.Source, "a"));
        _ = await scope.ExecuteAsync(BatchRequirement.Read(values.Source, "c"));
        _ = await scope.ExecuteAsync(BatchRequirement.Read(values.Source, "a"));

        Assert.Equal(4, values.InvocationCount);
    }

    [Fact]
    public async Task CacheCapacityIsBoundedPerSource()
    {
        var first = new RecordingBatchSource<string, int, string>(
            "first",
            "tenant-a",
            StringComparer.Ordinal,
            static (keys, _, _) => new(Result<IReadOnlyList<BatchSourceEntry<string, int, string>>, string>.Success(
                keys.Select(key => BatchSourceEntry<string, int, string>.Found(key, key.Length)).ToArray())));
        var second = new RecordingBatchSource<string, int, string>(
            "second",
            "tenant-a",
            StringComparer.Ordinal,
            static (keys, _, _) => new(Result<IReadOnlyList<BatchSourceEntry<string, int, string>>, string>.Success(
                keys.Select(key => BatchSourceEntry<string, int, string>.Found(key, key.Length)).ToArray())));

        await using var scope = BatchScope.Create(new BatchScopeOptions { MaxCachedEntries = 2 });

        _ = await scope.ExecuteAsync(BatchRequirement.Read(first.Source, "a"));
        _ = await scope.ExecuteAsync(BatchRequirement.Read(first.Source, "b"));
        _ = await scope.ExecuteAsync(BatchRequirement.Read(second.Source, "a"));
        _ = await scope.ExecuteAsync(BatchRequirement.Read(second.Source, "b"));

        // Four retained entries exceed a single shared bound of two: each source keeps its own.
        _ = await scope.ExecuteAsync(BatchRequirement.Read(first.Source, "a"));
        _ = await scope.ExecuteAsync(BatchRequirement.Read(second.Source, "a"));
        Assert.Equal(2, first.InvocationCount);
        Assert.Equal(2, second.InvocationCount);

        // The per-source bound still evicts the oldest entry for the source that overflows.
        _ = await scope.ExecuteAsync(BatchRequirement.Read(first.Source, "c"));
        _ = await scope.ExecuteAsync(BatchRequirement.Read(first.Source, "a"));
        Assert.Equal(4, first.InvocationCount);
        Assert.Equal(2, second.InvocationCount);
    }

    [Fact]
    public async Task SeparateScopesDoNotShareRequestsOrCache()
    {
        var customers = new RecordingBatchSource<string, string, string>(
            "customers",
            "tenant-a",
            StringComparer.Ordinal,
            static (keys, _, _) => new(Result<IReadOnlyList<BatchSourceEntry<string, string, string>>, string>.Success(
            [
                BatchSourceEntry<string, string, string>.Found("c1", "Customer One"),
            ])));

        await using (var firstScope = BatchScope.Create())
        {
            _ = await firstScope.ExecuteAsync(BatchRequirement.Read(customers.Source, "c1"));
        }

        await using (var secondScope = BatchScope.Create())
        {
            _ = await secondScope.ExecuteAsync(BatchRequirement.Read(customers.Source, "c1"));
        }

        Assert.Equal(2, customers.InvocationCount);
    }

    [Fact]
    public async Task SourceInstancesWithTheSameNameDoNotShareCacheEntries()
    {
        var first = new RecordingBatchSource<string, string, string>(
            "customers",
            "tenant-a",
            StringComparer.Ordinal,
            static (keys, _, _) => new(Result<IReadOnlyList<BatchSourceEntry<string, string, string>>, string>.Success(
            [
                BatchSourceEntry<string, string, string>.Found("c1", "first"),
            ])));
        var second = new RecordingBatchSource<string, string, string>(
            "customers",
            "tenant-a",
            StringComparer.Ordinal,
            static (keys, _, _) => new(Result<IReadOnlyList<BatchSourceEntry<string, string, string>>, string>.Success(
            [
                BatchSourceEntry<string, string, string>.Found("c1", "second"),
            ])));

        await using var scope = BatchScope.Create();
        var firstOutcome = await scope.ExecuteAsync(BatchRequirement.Read(first.Source, "c1"));
        var secondOutcome = await scope.ExecuteAsync(BatchRequirement.Read(second.Source, "c1"));

        Assert.True(firstOutcome.TryGetValue(out var firstValue));
        Assert.True(secondOutcome.TryGetValue(out var secondValue));
        Assert.Equal("first", firstValue);
        Assert.Equal("second", secondValue);
        Assert.Equal(1, first.InvocationCount);
        Assert.Equal(1, second.InvocationCount);
    }

    [Fact]
    public async Task KeyComparerControlsGroupingAndResponseMatching()
    {
        var customers = new RecordingBatchSource<string, string, string>(
            "customers",
            "tenant-a",
            StringComparer.OrdinalIgnoreCase,
            static (keys, _, _) => new(Result<IReadOnlyList<BatchSourceEntry<string, string, string>>, string>.Success(
            [
                BatchSourceEntry<string, string, string>.Found("c1", "Customer One"),
                BatchSourceEntry<string, string, string>.Found("C2", "Customer Two"),
            ])));

        var requirement = BatchRequirement.All(
        [
            BatchRequirement.Read(customers.Source, "C1"),
            BatchRequirement.Read(customers.Source, "c1"),
            BatchRequirement.Read(customers.Source, "c2"),
        ]);

        await using var scope = BatchScope.Create();
        var outcome = await scope.ExecuteAsync(requirement);

        Assert.True(outcome.TryGetValue(out var results));
        Assert.Equal(["C1", "c2"], customers.Batches[0]);
        Assert.True(results[0].TryGetValue(out var first));
        Assert.Equal("Customer One", first);
        Assert.True(results[1].TryGetValue(out var repeated));
        Assert.Equal("Customer One", repeated);
        Assert.True(results[2].TryGetValue(out var second));
        Assert.Equal("Customer Two", second);
    }

    [Fact]
    public async Task AdmissionLimitRejectsExcessPendingKeysBeforeDispatching()
    {
        var values = new RecordingBatchSource<string, int, string>(
            "values",
            "tenant-a",
            StringComparer.Ordinal,
            static (keys, _, _) => new(Result<IReadOnlyList<BatchSourceEntry<string, int, string>>, string>.Success(
                keys.Select(key => BatchSourceEntry<string, int, string>.Found(key, 1)).ToArray())));

        var requirement = BatchRequirement.All(
        [
            BatchRequirement.Read(values.Source, "a"),
            BatchRequirement.Read(values.Source, "b"),
        ]);

        await using var scope = BatchScope.Create(new BatchScopeOptions { MaxPendingRequests = 1 });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await scope.ExecuteAsync(requirement));
        Assert.Contains("maximum of 1 pending requests", exception.Message);
        Assert.Equal(0, values.InvocationCount);
    }

    [Fact]
    public async Task InvalidationIsRejectedWhileAnExecutionIsActive()
    {
        var started = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var customers = new RecordingBatchSource<string, string, string>(
            "customers",
            "tenant-a",
            StringComparer.Ordinal,
            async (keys, _, _) =>
            {
                started.TrySetResult(true);
                await release.Task;
                return Result<IReadOnlyList<BatchSourceEntry<string, string, string>>, string>.Success(
                [
                    BatchSourceEntry<string, string, string>.Found("c1", "Customer One"),
                ]);
            });

        await using var scope = BatchScope.Create();
        var execution = scope.ExecuteAsync(BatchRequirement.Read(customers.Source, "c1")).AsTask();
        await started.Task;

        Assert.Throws<InvalidOperationException>(() => scope.Invalidate(customers.Source, "c1"));
        Assert.Throws<InvalidOperationException>(() => scope.ClearCache());

        release.SetResult(true);
        _ = await execution;
    }

    [Fact]
    public async Task ConcurrentExecutionsOnOneScopeAreRejected()
    {
        var started = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var customers = new RecordingBatchSource<string, string, string>(
            "customers",
            "tenant-a",
            StringComparer.Ordinal,
            async (keys, _, _) =>
            {
                started.TrySetResult(true);
                await release.Task;
                return Result<IReadOnlyList<BatchSourceEntry<string, string, string>>, string>.Success(
                [
                    BatchSourceEntry<string, string, string>.Found("c1", "Customer One"),
                ]);
            });

        await using var scope = BatchScope.Create();
        var first = scope.ExecuteAsync(BatchRequirement.Read(customers.Source, "c1")).AsTask();
        await started.Task;

        Assert.Throws<InvalidOperationException>(
            () => scope.ExecuteAsync(BatchRequirement.Read(customers.Source, "c1")));

        release.SetResult(true);
        _ = await first;
    }

    [Theory]
    [InlineData(0, 1, 1, 1)]
    [InlineData(1, 0, 1, 1)]
    [InlineData(1, 1, 0, 1)]
    [InlineData(1, 1, 1, 0)]
    public void InvalidOptionsAreRejectedWhenTheScopeIsCreated(
        int maxBatchSize,
        int maxConcurrentBatches,
        int maxPendingRequests,
        int maxCachedEntries)
    {
        var options = new BatchScopeOptions
        {
            MaxBatchSize = maxBatchSize,
            MaxConcurrentBatches = maxConcurrentBatches,
            MaxPendingRequests = maxPendingRequests,
            MaxCachedEntries = maxCachedEntries,
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => BatchScope.Create(options));
    }

    [Fact]
    public async Task OptionsExposeTheConfiguredBounds()
    {
        var options = new BatchScopeOptions
        {
            MaxBatchSize = 7,
            MaxConcurrentBatches = 2,
            MaxPendingRequests = 33,
            CacheResults = false,
            MaxCachedEntries = 99,
        };

        await using var scope = BatchScope.Create(options);

        Assert.Same(options, scope.Options);
        Assert.Equal(7, scope.Options.MaxBatchSize);
        Assert.Equal(2, scope.Options.MaxConcurrentBatches);
        Assert.Equal(33, scope.Options.MaxPendingRequests);
        Assert.False(scope.Options.CacheResults);
        Assert.Equal(99, scope.Options.MaxCachedEntries);
    }

    [Fact]
    public async Task AScopeCannotBeUsedAfterDisposal()
    {
        var scope = BatchScope.Create();
        await scope.DisposeAsync();

        Assert.Throws<ObjectDisposedException>(
            () => scope.ExecuteAsync(BatchRequirement.FromValue<int, string>(1)));
        Assert.Throws<ObjectDisposedException>(() => scope.ClearCache());
    }
}
