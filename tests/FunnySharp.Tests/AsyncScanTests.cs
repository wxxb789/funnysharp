namespace FunnySharp.Tests;

#pragma warning disable xUnit1051

public sealed class AsyncScanTests
{
    [Fact]
    public void ScanRejectsNullArgumentsEagerly()
    {
        IAsyncEnumerable<int>? source = null;

        Assert.Throws<ArgumentNullException>(() => source!.Scan(0, static (accumulator, item) => accumulator + item));
        Assert.Throws<ArgumentNullException>(() => source!.ScanValueAsync(
            0, static (accumulator, item) => ValueTask.FromResult(accumulator + item)));
        Assert.Throws<ArgumentNullException>(() => source!.ScanValueAsync(
            0, static (accumulator, item, token) => ValueTask.FromResult(accumulator + item)));
        Assert.Throws<ArgumentNullException>(() => AsyncValues(1).Scan(0, (Func<int, int, int>)null!));
        Assert.Throws<ArgumentNullException>(() => AsyncValues(1).ScanValueAsync(
            0, (Func<int, int, ValueTask<int>>)null!));
        Assert.Throws<ArgumentNullException>(() => AsyncValues(1).ScanValueAsync(
            0, (Func<int, int, CancellationToken, ValueTask<int>>)null!));
    }

    [Fact]
    public async Task ScanAndScanValueAsyncYieldTheAccumulatorAfterEachElementWithoutTheSeed()
    {
        var scanned = await AsyncValues(1, 2, 3, 4)
            .Scan(5, static (accumulator, item) => accumulator - item)
            .ToListAsync();
        var scannedAsync = await AsyncValues(1, 2, 3, 4)
            .ScanValueAsync(5, (accumulator, item) => ValueTask.FromResult(accumulator - item))
            .ToListAsync();
        var empty = await AsyncValues<int>()
            .Scan(0, static (accumulator, item) => accumulator + item)
            .ToListAsync();

        Assert.Equal([4, 2, -1, -5], scanned);
        Assert.Equal([4, 2, -1, -5], scannedAsync);
        Assert.Empty(empty);
    }

    [Fact]
    public async Task ScanValueAsyncDefersSourceAndAccumulatorUntilFirstPull()
    {
        var source = new ProbeAsyncEnumerable<int>([1]);
        var accumulatorCalls = 0;
        var pipeline = source.ScanValueAsync(0, (accumulator, item) =>
        {
            accumulatorCalls++;
            return ValueTask.FromResult(accumulator + item);
        });

        Assert.Equal(0, source.EnumeratorCount);
        Assert.Equal(0, accumulatorCalls);

        await using var enumerator = pipeline.GetAsyncEnumerator();
        Assert.Equal(0, source.EnumeratorCount);
        Assert.Equal(0, accumulatorCalls);

        Assert.True(await enumerator.MoveNextAsync());
        Assert.Equal(1, enumerator.Current);
        Assert.Equal(1, source.EnumeratorCount);
        Assert.Equal(1, source.MoveNextCount);
        Assert.Equal(1, accumulatorCalls);
    }

    [Fact]
    public async Task ScanStreamsStrictlyWithoutPrefetching()
    {
        var source = new ProbeAsyncEnumerable<int>([1, 2, 3]);
        var accumulatorCalls = 0;
        var enumerator = source
            .Scan(0, (accumulator, item) =>
            {
                accumulatorCalls++;
                return accumulator + item;
            })
            .GetAsyncEnumerator();

        try
        {
            Assert.Equal(0, source.EnumeratorCount);
            Assert.Equal(0, source.MoveNextCount);
            Assert.Equal(0, accumulatorCalls);

            Assert.True(await enumerator.MoveNextAsync());
            Assert.Equal(1, enumerator.Current);
            Assert.Equal(1, source.EnumeratorCount);
            Assert.Equal(1, source.MoveNextCount);
            Assert.Equal(1, accumulatorCalls);
        }
        finally
        {
            await enumerator.DisposeAsync();
        }

        Assert.Equal(1, source.DisposeCount);
    }

    [Fact]
    public async Task ConsumerBreakDisposesTheSourceEnumerator()
    {
        var source = new ProbeAsyncEnumerable<int>([1, 2, 3]);
        var values = new List<int>();

        await foreach (var value in source.Scan(0, static (accumulator, item) => accumulator + item))
        {
            values.Add(value);
            break;
        }

        Assert.Equal([1], values);
        Assert.Equal(1, source.EnumeratorCount);
        Assert.Equal(1, source.MoveNextCount);
        Assert.Equal(1, source.DisposeCount);
    }

    [Fact]
    public async Task TokenAwareScanValueAsyncForwardsTheEnumerationTokenWithoutEagerCancellation()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var source = new ProbeAsyncEnumerable<int>([1, 2, 3]);
        var accumulatorCalls = new List<(int Accumulator, int Item)>();
        var accumulatorTokens = new List<CancellationToken>();

        var values = await source.ScanValueAsync(0, (accumulator, item, token) =>
            {
                accumulatorCalls.Add((accumulator, item));
                accumulatorTokens.Add(token);
                return ValueTask.FromResult(accumulator + item);
            })
            .ToListAsync(cancellationSource.Token);

        Assert.Equal([1, 3, 6], values);
        Assert.Equal([(0, 1), (1, 2), (3, 3)], accumulatorCalls);
        Assert.Equal(cancellationSource.Token, source.ReceivedToken);
        Assert.All(accumulatorTokens, token => Assert.Equal(cancellationSource.Token, token));
        Assert.Equal(1, source.DisposeCount);
    }

    [Fact]
    public async Task ScanPropagatesSourceAndAccumulatorFaultsByIdentity()
    {
        var sourceException = new InvalidOperationException("source");
        var syncAccumulatorException = new InvalidOperationException("sync accumulator");
        var asyncAccumulatorException = new InvalidOperationException("async accumulator");
        var faultingSource = new ProbeAsyncEnumerable<int>([], moveNextException: sourceException);

        var sourceActual = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await faultingSource.Scan(0, static (accumulator, item) => accumulator + item).ToListAsync());
        var syncAccumulatorActual = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await AsyncValues(1)
                .Scan(0, (accumulator, item) => throw syncAccumulatorException)
                .ToListAsync());
        var asyncAccumulatorActual = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await AsyncValues(1)
                .ScanValueAsync(0, (accumulator, item) =>
                    ValueTask.FromException<int>(asyncAccumulatorException))
                .ToListAsync());

        Assert.Same(sourceException, sourceActual);
        Assert.Same(syncAccumulatorException, syncAccumulatorActual);
        Assert.Same(asyncAccumulatorException, asyncAccumulatorActual);
        Assert.Equal(1, faultingSource.DisposeCount);
    }

    [Fact]
    public async Task ScanValueAsyncConsumesTheAccumulatorValueTasksOnce()
    {
        var source = new CountingMoveNextAsyncEnumerable<int>([1]);
        var accumulatorSources = new List<CountingValueTaskSource<int>>();

        var values = await source
            .ScanValueAsync(0, (accumulator, item) =>
            {
                var accumulatorSource = new CountingValueTaskSource<int>(accumulator + item);
                accumulatorSources.Add(accumulatorSource);
                return accumulatorSource.CreateValueTask();
            })
            .ToListAsync();

        Assert.Equal([1], values);
        Assert.Single(accumulatorSources);
        Assert.All(accumulatorSources, accumulatorSource => Assert.Equal(1, accumulatorSource.GetResultCount));
        Assert.Equal(2, source.MoveNextValueTasks.Count);
        Assert.All(source.MoveNextValueTasks, valueTask => Assert.Equal(1, valueTask.GetResultCount));
        Assert.Equal(1, source.DisposeCount);
    }

    [Fact]
    public async Task ScanValueAsyncPreservesAccumulatorCancellationAndDisposesTheSource()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var source = new ProbeAsyncEnumerable<int>([1]);
        var operation = source
            .ScanValueAsync(0, (_, _) => ValueTask.FromCanceled<int>(cancellationSource.Token))
            .ToListAsync()
            .AsTask();

        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation);

        Assert.Equal(cancellationSource.Token, exception.CancellationToken);
        Assert.True(operation.IsCanceled);
        Assert.Equal(1, source.DisposeCount);
    }

    [Fact]
    public async Task ScanDoesNotCacheAcrossEnumerations()
    {
        var source = new ProbeAsyncEnumerable<int>([1, 2]);
        var accumulatorCalls = new List<(int Accumulator, int Item)>();
        var pipeline = source.Scan(0, (accumulator, item) =>
        {
            accumulatorCalls.Add((accumulator, item));
            return accumulator + item;
        });

        var first = await pipeline.ToListAsync();
        var second = await pipeline.ToListAsync();

        Assert.Equal([1, 3], first);
        Assert.Equal([1, 3], second);
        Assert.Equal([(0, 1), (1, 2), (0, 1), (1, 2)], accumulatorCalls);
        Assert.Equal(2, source.EnumeratorCount);
        Assert.Equal(6, source.MoveNextCount);
        Assert.Equal(2, source.DisposeCount);
    }

    private static async IAsyncEnumerable<T> AsyncValues<T>(params T[] values)
    {
        foreach (var value in values)
        {
            yield return value;
            await Task.Yield();
        }
    }

    private sealed class ProbeAsyncEnumerable<T>(
        IReadOnlyList<T> values,
        Exception? moveNextException = null,
        Exception? disposeException = null) : IAsyncEnumerable<T>
    {
        public int EnumeratorCount { get; private set; }

        public int MoveNextCount { get; private set; }

        public int DisposeCount { get; private set; }

        public CancellationToken ReceivedToken { get; private set; }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            EnumeratorCount++;
            ReceivedToken = cancellationToken;
            return new Enumerator(this, values, moveNextException, disposeException);
        }

        private sealed class Enumerator(
            ProbeAsyncEnumerable<T> owner,
            IReadOnlyList<T> values,
            Exception? moveNextException,
            Exception? disposeException) : IAsyncEnumerator<T>
        {
            private int index = -1;

            public T Current => values[index];

            public ValueTask<bool> MoveNextAsync()
            {
                owner.MoveNextCount++;
                if (moveNextException is not null)
                {
                    return ValueTask.FromException<bool>(moveNextException);
                }

                var nextIndex = index + 1;
                if (nextIndex == values.Count)
                {
                    return ValueTask.FromResult(false);
                }

                index = nextIndex;
                return ValueTask.FromResult(true);
            }

            public ValueTask DisposeAsync()
            {
                owner.DisposeCount++;
                return disposeException is null
                    ? ValueTask.CompletedTask
                    : ValueTask.FromException(disposeException);
            }
        }
    }

    private sealed class CountingMoveNextAsyncEnumerable<T>(IReadOnlyList<T> values) : IAsyncEnumerable<T>
    {
        public List<CountingValueTaskSource<bool>> MoveNextValueTasks { get; } = [];

        public int DisposeCount { get; private set; }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
            new Enumerator(this, values);

        private sealed class Enumerator(
            CountingMoveNextAsyncEnumerable<T> owner,
            IReadOnlyList<T> values) : IAsyncEnumerator<T>
        {
            private int index = -1;

            public T Current => values[index];

            public ValueTask<bool> MoveNextAsync()
            {
                var nextIndex = index + 1;
                var valueTaskSource = new CountingValueTaskSource<bool>(nextIndex < values.Count);
                owner.MoveNextValueTasks.Add(valueTaskSource);
                if (nextIndex < values.Count)
                {
                    index = nextIndex;
                }

                return valueTaskSource.CreateValueTask();
            }

            public ValueTask DisposeAsync()
            {
                owner.DisposeCount++;
                return ValueTask.CompletedTask;
            }
        }
    }
}

#pragma warning restore xUnit1051
