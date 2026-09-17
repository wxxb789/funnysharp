using System.Collections;

namespace FunnySharp.Tests;

#pragma warning disable xUnit1051

public sealed class UnitResultTraversalTests
{
    [Fact]
    public void SyncSequenceAndTraverseReturnSuccessForEmptyAndAllSuccessSources()
    {
        var empty = Array.Empty<UnitResult<string>>().Sequence();
        var allSuccess = new[] { UnitResult<string>.Success(), UnitResult<string>.Success() }.Sequence();
        var traversed = new[] { 1, 2 }.Traverse(static _ => UnitResult<string>.Success());

        Assert.Equal(UnitResult<string>.Success(), empty);
        Assert.Equal(UnitResult<string>.Success(), allSuccess);
        Assert.Equal(UnitResult<string>.Success(), traversed);
    }

    [Fact]
    public void SyncTraversalStopsAtTheFirstFailureAndInvokesSelectorsOnceInSourceOrder()
    {
        var firstError = new InvalidOperationException("first");
        var secondError = new InvalidOperationException("second");
        var sequenceSource = new CountingEnumerable<UnitResult<Exception>>(
        [
            UnitResult<Exception>.Success(),
            UnitResult<Exception>.Failure(firstError),
            UnitResult<Exception>.Failure(secondError),
            UnitResult<Exception>.Success(),
        ]);

        var sequence = sequenceSource.Sequence();

        Assert.True(sequence.IsFailure);
        Assert.True(sequence.TryGetError(out var sequenceError));
        Assert.Same(firstError, sequenceError);
        Assert.Equal(2, sequenceSource.ItemsYielded);
        Assert.Equal(1, sequenceSource.EnumeratorCount);
        Assert.Equal(1, sequenceSource.DisposeCount);

        var calls = new List<int>();
        var traversalSource = new CountingEnumerable<int>([1, 2, 3, 4]);
        var traversed = traversalSource.Traverse(value =>
        {
            calls.Add(value);
            return value == 2
                ? UnitResult<Exception>.Failure(firstError)
                : UnitResult<Exception>.Success();
        });

        Assert.True(traversed.TryGetError(out var traversalError));
        Assert.Same(firstError, traversalError);
        Assert.Equal([1, 2], calls);
        Assert.Equal(2, traversalSource.ItemsYielded);
        Assert.Equal(1, traversalSource.EnumeratorCount);
        Assert.Equal(1, traversalSource.DisposeCount);
    }

    [Fact]
    public void SyncSequenceEnumeratesSourceOnceAndDisposesItsEnumerator()
    {
        var source = new CountingEnumerable<UnitResult<string>>(
        [
            UnitResult<string>.Success(),
            UnitResult<string>.Success(),
        ]);

        var result = source.Sequence();

        Assert.Equal(UnitResult<string>.Success(), result);
        Assert.Equal(1, source.EnumeratorCount);
        Assert.Equal(2, source.ItemsYielded);
        Assert.Equal(1, source.DisposeCount);
    }

    [Fact]
    public void SyncTraversalRejectsNullArgumentsEagerlyAndThrowsForDefaultElements()
    {
        IEnumerable<UnitResult<string>>? source = null;
        IEnumerable<int>? values = null;

        Assert.Throws<ArgumentNullException>(() => source!.Sequence());
        Assert.Throws<ArgumentNullException>(() => values!.Traverse(static _ => UnitResult<string>.Success()));
        Assert.Throws<ArgumentNullException>(() =>
            values!.Traverse((Func<int, UnitResult<string>>)null!));

        var uninitializedElement = new[] { UnitResult<string>.Success(), default };
        var sequenceException = Assert.Throws<InvalidOperationException>(() => uninitializedElement.Sequence());
        var traversalException = Assert.Throws<InvalidOperationException>(() =>
            new[] { 1 }.Traverse<int, string>((Func<int, UnitResult<string>>)(_ => default)));

        Assert.Equal("The unit result has not been initialized.", sequenceException.Message);
        Assert.Equal("The unit result has not been initialized.", traversalException.Message);

        var failureBeforeDefault = new[] { UnitResult<string>.Failure("bad"), default };

        Assert.Equal(UnitResult<string>.Failure("bad"), failureBeforeDefault.Sequence());
    }

    [Fact]
    public void SyncTraversalPropagatesSourceAndSelectorExceptionsByIdentity()
    {
        var sourceException = new InvalidOperationException("source");
        var selectorException = new InvalidOperationException("selector");
        var throwingSource = new ThrowingEnumerable<UnitResult<string>>(sourceException);
        var selectorSource = new CountingEnumerable<int>([1, 2]);

        Assert.Same(
            sourceException,
            Assert.Throws<InvalidOperationException>(() => throwingSource.Sequence()));
        Assert.Same(
            selectorException,
            Assert.Throws<InvalidOperationException>(() =>
                selectorSource.Traverse<int, string>((Func<int, UnitResult<string>>)(_ => throw selectorException))));
        Assert.Equal(1, selectorSource.EnumeratorCount);
        Assert.Equal(1, selectorSource.ItemsYielded);
        Assert.Equal(1, selectorSource.DisposeCount);
    }

    [Fact]
    public void AsyncTraversalRejectsNullArgumentsEagerly()
    {
        IAsyncEnumerable<UnitResult<string>>? source = null;
        IAsyncEnumerable<int>? values = null;

        Assert.Throws<ArgumentNullException>(() => source!.SequenceAsync());
        Assert.Throws<ArgumentNullException>(() => source!.SequenceAsync(CancellationToken.None));
        Assert.Throws<ArgumentNullException>(() => values!.TraverseAsync(static _ => UnitResult<string>.Success()));
        Assert.Throws<ArgumentNullException>(() =>
            values!.TraverseAsync((Func<int, UnitResult<string>>)null!));
        Assert.Throws<ArgumentNullException>(() =>
            values!.TraverseValueAsync((Func<int, ValueTask<UnitResult<string>>>)null!));
        Assert.Throws<ArgumentNullException>(() =>
            values!.TraverseValueAsync(
                (Func<int, CancellationToken, ValueTask<UnitResult<string>>>)null!,
                CancellationToken.None));
    }

    [Fact]
    public async Task AsyncSequenceAndTraverseReturnSuccessForEmptyAndAllSuccessSources()
    {
        var empty = await AsyncValues<UnitResult<string>>().SequenceAsync();
        var allSuccess = await AsyncValues(
            UnitResult<string>.Success(),
            UnitResult<string>.Success()).SequenceAsync();
        var traversed = await AsyncValues(1, 2).TraverseAsync(static _ => UnitResult<string>.Success());
        var valueTraversed = await AsyncValues(1, 2).TraverseValueAsync(
            static _ => ValueTask.FromResult(UnitResult<string>.Success()));

        Assert.Equal(UnitResult<string>.Success(), empty);
        Assert.Equal(UnitResult<string>.Success(), allSuccess);
        Assert.Equal(UnitResult<string>.Success(), traversed);
        Assert.Equal(UnitResult<string>.Success(), valueTraversed);
    }

    [Fact]
    public async Task AsyncTraversalStopsAtTheFirstFailureAndInvokesSelectorsOncePerReachedItem()
    {
        var firstError = new InvalidOperationException("first");
        var secondError = new InvalidOperationException("second");
        var sequenceSource = new ProbeAsyncEnumerable<UnitResult<Exception>>(
        [
            UnitResult<Exception>.Success(),
            UnitResult<Exception>.Failure(firstError),
            UnitResult<Exception>.Failure(secondError),
        ]);

        var sequence = await sequenceSource.SequenceAsync();

        Assert.True(sequence.TryGetError(out var sequenceError));
        Assert.Same(firstError, sequenceError);
        Assert.Equal(2, sequenceSource.ItemsYielded);
        Assert.Equal(1, sequenceSource.EnumeratorCount);
        Assert.Equal(1, sequenceSource.DisposeCount);

        var calls = new List<int>();
        var traversalSource = new ProbeAsyncEnumerable<int>([1, 2, 3]);
        var traversed = await traversalSource.TraverseAsync(value =>
        {
            calls.Add(value);
            return value == 2
                ? UnitResult<Exception>.Failure(firstError)
                : UnitResult<Exception>.Success();
        });

        Assert.True(traversed.TryGetError(out var traversalError));
        Assert.Same(firstError, traversalError);
        Assert.Equal([1, 2], calls);
        Assert.Equal(2, traversalSource.ItemsYielded);
        Assert.Equal(1, traversalSource.EnumeratorCount);
        Assert.Equal(1, traversalSource.DisposeCount);
    }

    [Fact]
    public async Task AsyncValueTaskTraversalStopsAtTheFirstFailureAndConsumesEachSelectorValueTaskOnce()
    {
        var error = new InvalidOperationException("first");
        var calls = new List<int>();
        var source = new ProbeAsyncEnumerable<int>([1, 2, 3]);
        var selectorSource = new CountingValueTaskSource<UnitResult<Exception>>(UnitResult<Exception>.Success());

        var result = await source.TraverseValueAsync(value =>
        {
            calls.Add(value);
            return value == 2
                ? ValueTask.FromResult(UnitResult<Exception>.Failure(error))
                : selectorSource.CreateValueTask();
        });

        Assert.True(result.TryGetError(out var actual));
        Assert.Same(error, actual);
        Assert.Equal([1, 2], calls);
        Assert.Equal(1, selectorSource.GetResultCount);
        Assert.Equal(2, source.ItemsYielded);
        Assert.Equal(1, source.DisposeCount);
    }

    [Fact]
    public async Task AsyncTraversalForwardsTheExactTokenToTheEnumeratorAndTokenAwareSelectors()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var selectorTokens = new List<CancellationToken>();
        var valueSource = new ProbeAsyncEnumerable<int>([1, 2]);

        var valueTraversed = await valueSource.TraverseValueAsync(
            (value, token) =>
            {
                selectorTokens.Add(token);
                return ValueTask.FromResult(UnitResult<string>.Success());
            },
            cancellationSource.Token);

        Assert.Equal(UnitResult<string>.Success(), valueTraversed);
        Assert.Equal(cancellationSource.Token, valueSource.ReceivedToken);
        Assert.Equal(2, selectorTokens.Count);
        Assert.All(selectorTokens, token => Assert.Equal(cancellationSource.Token, token));

        var sequenceSource = new ProbeAsyncEnumerable<UnitResult<string>>([UnitResult<string>.Success()]);

        var sequence = await sequenceSource.SequenceAsync(cancellationSource.Token);

        Assert.Equal(UnitResult<string>.Success(), sequence);
        Assert.Equal(cancellationSource.Token, sequenceSource.ReceivedToken);

        var traversalSource = new ProbeAsyncEnumerable<int>([1]);

        var traversed = await traversalSource.TraverseAsync(
            static _ => UnitResult<string>.Success(),
            cancellationSource.Token);

        Assert.Equal(UnitResult<string>.Success(), traversed);
        Assert.Equal(cancellationSource.Token, traversalSource.ReceivedToken);
    }

    [Fact]
    public async Task AsyncTraversalDisposesTheEnumeratorOnSuccessFailureAndCancellation()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var successSource = new ProbeAsyncEnumerable<UnitResult<string>>([UnitResult<string>.Success()]);
        var failureSource = new ProbeAsyncEnumerable<UnitResult<string>>([UnitResult<string>.Failure("bad")]);
        var cancellationSourceEnumerable = new ProbeAsyncEnumerable<UnitResult<string>>(
            [],
            cancelOnMoveNextToken: cancellationSource.Token);

        var success = await successSource.SequenceAsync();
        var failure = await failureSource.SequenceAsync();
        var cancellationOperation = cancellationSourceEnumerable.SequenceAsync(cancellationSource.Token).AsTask();

        Assert.Equal(UnitResult<string>.Success(), success);
        Assert.Equal(UnitResult<string>.Failure("bad"), failure);
        Assert.Equal(1, successSource.DisposeCount);
        Assert.Equal(1, failureSource.DisposeCount);
        var cancellation = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cancellationOperation);
        Assert.True(cancellationOperation.IsCanceled);
        Assert.Equal(cancellationSource.Token, cancellation.CancellationToken);
        Assert.Equal(cancellationSource.Token, cancellationSourceEnumerable.ReceivedToken);
        Assert.Equal(1, cancellationSourceEnumerable.DisposeCount);
    }

    [Fact]
    public async Task AsyncTraversalPropagatesFaultsByIdentity()
    {
        var sourceException = new InvalidOperationException("source");
        var selectorException = new InvalidOperationException("selector");
        var valueSelectorException = new InvalidOperationException("value selector");
        var source = new ProbeAsyncEnumerable<UnitResult<string>>([], moveNextException: sourceException);
        var selectorSource = new ProbeAsyncEnumerable<int>([1]);
        var valueSelectorSource = new ProbeAsyncEnumerable<int>([1]);

        var sourceOperation = source.SequenceAsync().AsTask();
        var selectorOperation = selectorSource.TraverseAsync<int, string>(
            (Func<int, UnitResult<string>>)(_ => throw selectorException)).AsTask();
        var valueSelectorOperation = valueSelectorSource.TraverseValueAsync<int, string>(
            (Func<int, ValueTask<UnitResult<string>>>)(_ => throw valueSelectorException)).AsTask();

        Assert.Same(sourceException, await Assert.ThrowsAsync<InvalidOperationException>(() => sourceOperation));
        Assert.Same(selectorException, await Assert.ThrowsAsync<InvalidOperationException>(() => selectorOperation));
        Assert.Same(
            valueSelectorException,
            await Assert.ThrowsAsync<InvalidOperationException>(() => valueSelectorOperation));
        Assert.True(sourceOperation.IsFaulted);
        Assert.True(selectorOperation.IsFaulted);
        Assert.True(valueSelectorOperation.IsFaulted);
        Assert.Equal(1, source.DisposeCount);
        Assert.Equal(1, selectorSource.DisposeCount);
        Assert.Equal(1, valueSelectorSource.DisposeCount);
    }

    [Fact]
    public async Task AsyncTraversalPreservesSelectorCancellationStatusAndToken()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var source = new ProbeAsyncEnumerable<int>([1]);

        var operation = source.TraverseValueAsync(
            _ => ValueTask.FromCanceled<UnitResult<string>>(cancellationSource.Token)).AsTask();

        var actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation);

        Assert.True(operation.IsCanceled);
        Assert.Equal(cancellationSource.Token, actual.CancellationToken);
        Assert.Equal(1, source.DisposeCount);
    }

    [Fact]
    public async Task AsyncTraversalPropagatesEnumeratorDisposalFaultsByIdentity()
    {
        var disposeException = new InvalidOperationException("dispose");
        var source = new ProbeAsyncEnumerable<UnitResult<string>>(
            [UnitResult<string>.Success()],
            disposeException: disposeException);

        var operation = source.SequenceAsync().AsTask();

        Assert.Same(disposeException, await Assert.ThrowsAsync<InvalidOperationException>(() => operation));
        Assert.Equal(1, source.DisposeCount);
    }

    [Fact]
    public async Task AsyncTraversalFaultsForDefaultElements()
    {
        var sequenceOperation = AsyncValues(UnitResult<string>.Success(), default).SequenceAsync().AsTask();
        var traversalOperation = AsyncValues(1).TraverseAsync<int, string>(
            (Func<int, UnitResult<string>>)(_ => default)).AsTask();

        var sequenceException = await Assert.ThrowsAsync<InvalidOperationException>(() => sequenceOperation);
        var traversalException = await Assert.ThrowsAsync<InvalidOperationException>(() => traversalOperation);

        Assert.Equal("The unit result has not been initialized.", sequenceException.Message);
        Assert.Equal("The unit result has not been initialized.", traversalException.Message);
    }

    [Fact]
    public async Task UnitResultTraversalOverloadsResolveWithExplicitDelegateTypes()
    {
        var expected = new InvalidOperationException("explicit");
        var syncResult = new[] { 1 }.Traverse<int, string>(
            (Func<int, UnitResult<string>>)(_ => UnitResult<string>.Success()));
        var asyncResult = await AsyncValues(1).TraverseValueAsync<int, string>(
            (Func<int, ValueTask<UnitResult<string>>>)(_ =>
                ValueTask.FromResult(UnitResult<string>.Success())));

        Assert.Equal(UnitResult<string>.Success(), syncResult);
        Assert.Equal(UnitResult<string>.Success(), asyncResult);

        var syncOperation = new ProbeAsyncEnumerable<int>([1]).TraverseAsync<int, string>(
            (Func<int, UnitResult<string>>)(_ => throw expected)).AsTask();
        var valueOperation = new ProbeAsyncEnumerable<int>([1]).TraverseValueAsync<int, string>(
            (Func<int, ValueTask<UnitResult<string>>>)(_ => throw expected)).AsTask();

        Assert.Same(expected, await Assert.ThrowsAsync<InvalidOperationException>(() => syncOperation));
        Assert.Same(expected, await Assert.ThrowsAsync<InvalidOperationException>(() => valueOperation));
    }

    private static async IAsyncEnumerable<T> AsyncValues<T>(params T[] values)
    {
        foreach (var value in values)
        {
            yield return value;
            await Task.Yield();
        }
    }

    private sealed class CountingEnumerable<T>(IReadOnlyList<T> values) : IEnumerable<T>
    {
        public int EnumeratorCount { get; private set; }

        public int ItemsYielded { get; private set; }

        public int DisposeCount { get; private set; }

        public IEnumerator<T> GetEnumerator()
        {
            EnumeratorCount++;
            return new Enumerator(this, values);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private sealed class Enumerator(
            CountingEnumerable<T> owner,
            IReadOnlyList<T> values) : IEnumerator<T>
        {
            private int index = -1;

            public T Current => values[index];

            object IEnumerator.Current => Current!;

            public bool MoveNext()
            {
                var nextIndex = index + 1;
                if (nextIndex == values.Count)
                {
                    return false;
                }

                index = nextIndex;
                owner.ItemsYielded++;
                return true;
            }

            public void Reset() => throw new NotSupportedException();

            public void Dispose() => owner.DisposeCount++;
        }
    }

    private sealed class ThrowingEnumerable<T>(Exception exception) : IEnumerable<T>
    {
        public IEnumerator<T> GetEnumerator() => throw exception;

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class ProbeAsyncEnumerable<T>(
        IReadOnlyList<T> values,
        CancellationToken? cancelOnMoveNextToken = null,
        Exception? moveNextException = null,
        Exception? disposeException = null) : IAsyncEnumerable<T>
    {
        public int EnumeratorCount { get; private set; }

        public int ItemsYielded { get; private set; }

        public int DisposeCount { get; private set; }

        public CancellationToken ReceivedToken { get; private set; }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            EnumeratorCount++;
            ReceivedToken = cancellationToken;
            return new Enumerator(this, values, cancelOnMoveNextToken, moveNextException, disposeException);
        }

        private sealed class Enumerator(
            ProbeAsyncEnumerable<T> owner,
            IReadOnlyList<T> values,
            CancellationToken? cancelOnMoveNextToken,
            Exception? moveNextException,
            Exception? disposeException) : IAsyncEnumerator<T>
        {
            private int index = -1;

            public T Current => values[index];

            public ValueTask<bool> MoveNextAsync()
            {
                if (cancelOnMoveNextToken is { } token)
                {
                    return ValueTask.FromCanceled<bool>(token);
                }

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
                owner.ItemsYielded++;
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
}

#pragma warning restore xUnit1051
