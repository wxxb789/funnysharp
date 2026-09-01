using System.Collections.Generic;

namespace FunnySharp.Tests;

#pragma warning disable xUnit1051

public sealed class AsyncEnumerablePipelineTests
{
    [Fact]
    public void ChooseRejectsNullArgumentsEagerly()
    {
        IAsyncEnumerable<int>? source = null;

        Assert.Throws<ArgumentNullException>(() => source!.Choose(static value => Option.Some(value)));
        Assert.Throws<ArgumentNullException>(() => source!.ChooseValueAsync(
            static value => ValueTask.FromResult(Option.Some(value))));
        Assert.Throws<ArgumentNullException>(() => source!.ChooseValueAsync(
            static (value, token) => ValueTask.FromResult(Option.Some(value))));
        Assert.Throws<ArgumentNullException>(() => AsyncValues(1).Choose((Func<int, Option<int>>)null!));
        Assert.Throws<ArgumentNullException>(() => AsyncValues(1).ChooseValueAsync(
            (Func<int, ValueTask<Option<int>>>)null!));
        Assert.Throws<ArgumentNullException>(() => AsyncValues(1).ChooseValueAsync(
            (Func<int, CancellationToken, ValueTask<Option<int>>>)null!));
    }

    [Fact]
    public async Task ChooseValueAsyncDefersSourceAndSelectorUntilFirstPull()
    {
        var source = new ProbeAsyncEnumerable<int>([1]);
        var selectorCalls = 0;
        var pipeline = source.ChooseValueAsync(value =>
        {
            selectorCalls++;
            return ValueTask.FromResult(Option.Some(value));
        });

        Assert.Equal(0, source.EnumeratorCount);
        Assert.Equal(0, selectorCalls);

        await using var enumerator = pipeline.GetAsyncEnumerator();
        Assert.Equal(0, source.EnumeratorCount);
        Assert.Equal(0, selectorCalls);

        Assert.True(await enumerator.MoveNextAsync());
        Assert.Equal(1, enumerator.Current);
        Assert.Equal(1, source.EnumeratorCount);
        Assert.Equal(1, source.MoveNextCount);
        Assert.Equal(1, selectorCalls);
    }

    [Fact]
    public async Task ChooseAndChooseValueAsyncPreserveSomeOrderAndSkipNone()
    {
        var chosen = await AsyncValues(1, 2, 3)
            .Choose(value => value == 2 ? Option.None<int>() : Option.Some(value * 10))
            .ToListAsync();
        var chosenAsync = await AsyncValues(1, 2, 3)
            .ChooseValueAsync(value =>
                ValueTask.FromResult(value == 2 ? Option.None<int>() : Option.Some(value * 100)))
            .ToListAsync();

        Assert.Equal([10, 30], chosen);
        Assert.Equal([100, 300], chosenAsync);
    }

    [Fact]
    public async Task ChooseStreamsStrictlyWithoutPrefetching()
    {
        var source = new ProbeAsyncEnumerable<int>([1, 2, 3]);
        var selectorCalls = 0;
        var enumerator = source.Choose(value =>
        {
            selectorCalls++;
            return Option.Some(value);
        }).GetAsyncEnumerator();

        try
        {
            Assert.Equal(0, source.EnumeratorCount);
            Assert.Equal(0, source.MoveNextCount);
            Assert.Equal(0, selectorCalls);

            Assert.True(await enumerator.MoveNextAsync());
            Assert.Equal(1, enumerator.Current);
            Assert.Equal(1, source.EnumeratorCount);
            Assert.Equal(1, source.MoveNextCount);
            Assert.Equal(1, selectorCalls);
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

        await foreach (var value in source.Choose(static value => Option.Some(value)))
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
    public async Task TokenAwareChooseValueAsyncForwardsTheEnumerationTokenWithoutEagerCancellation()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var source = new ProbeAsyncEnumerable<int>([1, 2, 3]);
        var selectorValues = new List<int>();
        var selectorTokens = new List<CancellationToken>();

        var values = await source.ChooseValueAsync((value, token) =>
            {
                selectorValues.Add(value);
                selectorTokens.Add(token);
                return ValueTask.FromResult(value == 2 ? Option.None<int>() : Option.Some(value));
            })
            .ToListAsync(cancellationSource.Token);

        Assert.Equal([1, 3], values);
        Assert.Equal([1, 2, 3], selectorValues);
        Assert.Equal(cancellationSource.Token, source.ReceivedToken);
        Assert.All(selectorTokens, token => Assert.Equal(cancellationSource.Token, token));
        Assert.Equal(1, source.DisposeCount);
    }

    [Fact]
    public async Task ChoosePropagatesSourceAndSelectorFaultsByIdentity()
    {
        var sourceException = new InvalidOperationException("source");
        var syncSelectorException = new InvalidOperationException("sync selector");
        var asyncSelectorException = new InvalidOperationException("async selector");
        var faultingSource = new ProbeAsyncEnumerable<int>([], moveNextException: sourceException);

        var sourceActual = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await faultingSource.Choose(static value => Option.Some(value)).ToListAsync());
        var syncSelectorActual = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await AsyncValues(1)
                .Choose((Func<int, Option<int>>)(_ => throw syncSelectorException))
                .ToListAsync());
        var asyncSelectorActual = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await AsyncValues(1)
                .ChooseValueAsync<int, int>(
                    _ => ValueTask.FromException<Option<int>>(asyncSelectorException))
                .ToListAsync());

        Assert.Same(sourceException, sourceActual);
        Assert.Same(syncSelectorException, syncSelectorActual);
        Assert.Same(asyncSelectorException, asyncSelectorActual);
        Assert.Equal(1, faultingSource.DisposeCount);
    }

    [Fact]
    public async Task ChoosePropagatesDisposeFaultsByIdentity()
    {
        var disposeException = new InvalidOperationException("dispose");
        var source = new ProbeAsyncEnumerable<int>([1], disposeException: disposeException);

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await source.Choose(static value => Option.Some(value)).ToListAsync());

        Assert.Same(disposeException, actual);
        Assert.Equal(1, source.DisposeCount);
    }

    [Fact]
    public async Task ChooseValueAsyncConsumesSelectorAndMoveNextValueTasksOnce()
    {
        var source = new CountingMoveNextAsyncEnumerable<int>([1]);
        var selector = new CountingValueTaskSource<Option<int>>(Option.Some(7));

        var values = await source
            .ChooseValueAsync(_ => selector.CreateValueTask())
            .ToListAsync();

        Assert.Equal([7], values);
        Assert.Equal(1, selector.GetResultCount);
        Assert.Equal(2, source.MoveNextValueTasks.Count);
        Assert.All(source.MoveNextValueTasks, valueTask => Assert.Equal(1, valueTask.GetResultCount));
        Assert.Equal(1, source.DisposeCount);
    }

    [Fact]
    public async Task ChooseValueAsyncPreservesSelectorCancellationAndDisposesTheSource()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var source = new ProbeAsyncEnumerable<int>([1]);
        var operation = source
            .ChooseValueAsync(_ => ValueTask.FromCanceled<Option<int>>(cancellationSource.Token))
            .ToListAsync()
            .AsTask();

        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation);

        Assert.Equal(cancellationSource.Token, exception.CancellationToken);
        Assert.True(operation.IsCanceled);
        Assert.Equal(1, source.DisposeCount);
    }

    [Fact]
    public async Task ChooseDoesNotCacheAcrossEnumerations()
    {
        var source = new ProbeAsyncEnumerable<int>([1, 2]);
        var selectorValues = new List<int>();
        var pipeline = source.Choose(value =>
        {
            selectorValues.Add(value);
            return Option.Some(value);
        });

        var first = await pipeline.ToListAsync();
        var second = await pipeline.ToListAsync();

        Assert.Equal([1, 2], first);
        Assert.Equal([1, 2], second);
        Assert.Equal([1, 2, 1, 2], selectorValues);
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
