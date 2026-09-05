using System.Collections.Generic;

namespace FunnySharp.Tests;

#pragma warning disable xUnit1051

public sealed class AsyncCollectionTraversalTests
{
    [Fact]
    public void AsyncTraversalRejectsNullArgumentsEagerly()
    {
        IAsyncEnumerable<Option<int>>? options = null;
        IAsyncEnumerable<Result<int, string>>? results = null;
        IAsyncEnumerable<Validation<int, string>>? validations = null;
        IAsyncEnumerable<int>? source = null;

        Assert.Throws<ArgumentNullException>(() => options!.SequenceAsync());
        Assert.Throws<ArgumentNullException>(() => results!.SequenceAsync());
        Assert.Throws<ArgumentNullException>(() => validations!.SequenceAsync());
        Assert.Throws<ArgumentNullException>(() => source!.TraverseAsync(static value => Option.Some(value)));
        Assert.Throws<ArgumentNullException>(() => source!.TraverseAsync(
            static value => Result<int, string>.Success(value)));
        Assert.Throws<ArgumentNullException>(() => source!.TraverseAsync(
            static value => Validation<int, string>.Valid(value)));
        Assert.Throws<ArgumentNullException>(() => AsyncValues(1).TraverseValueAsync(
            (Func<int, ValueTask<Option<int>>>)null!));
        Assert.Throws<ArgumentNullException>(() => AsyncValues(1).TraverseValueAsync(
            (Func<int, CancellationToken, ValueTask<Result<int, string>>>)null!, CancellationToken.None));
    }

    [Fact]
    public async Task SequenceAsyncReturnsOrderedAndEmptySuccesses()
    {
        var options = await AsyncValues(Option.Some(1), Option.Some(2)).SequenceAsync();
        var results = await AsyncValues(
            Result<int, string>.Success(1),
            Result<int, string>.Success(2)).SequenceAsync();
        var validations = await AsyncValues(
            Validation<int, string>.Valid(1),
            Validation<int, string>.Valid(2)).SequenceAsync();

        Assert.Equal([1, 2], GetOptionValues(options));
        Assert.Equal([1, 2], GetResultValues(results));
        Assert.Equal([1, 2], GetValidationValues(validations));
        Assert.Empty(GetOptionValues(await AsyncValues<Option<int>>().SequenceAsync()));
        Assert.Empty(GetResultValues(await AsyncValues<Result<int, string>>().SequenceAsync()));
        Assert.Empty(GetValidationValues(await AsyncValues<Validation<int, string>>().SequenceAsync()));
    }

    [Fact]
    public async Task SequenceAsyncUsesOneEnumeratorAndDisposesIt()
    {
        var optionSource = new ProbeAsyncEnumerable<Option<int>>([Option.Some(1)]);
        var resultSource = new ProbeAsyncEnumerable<Result<int, string>>([Result<int, string>.Success(1)]);
        var validationSource = new ProbeAsyncEnumerable<Validation<int, string>>(
            [Validation<int, string>.Valid(1)]);

        _ = await optionSource.SequenceAsync();
        _ = await resultSource.SequenceAsync();
        _ = await validationSource.SequenceAsync();

        AssertProbeCompleted(optionSource);
        AssertProbeCompleted(resultSource);
        AssertProbeCompleted(validationSource);
    }

    [Fact]
    public async Task SequenceAsyncShortCircuitsOptionAndResultButFullyScansValidation()
    {
        var optionSource = new ProbeAsyncEnumerable<Option<int>>(
            [Option.Some(1), Option.None<int>(), Option.Some(3)]);
        var resultSource = new ProbeAsyncEnumerable<Result<int, string>>(
            [Result<int, string>.Success(1), Result<int, string>.Failure("result"), Result<int, string>.Success(3)]);
        var validationSource = new ProbeAsyncEnumerable<Validation<int, string>>(
        [
            Validation<int, string>.Valid(1),
            Validation<int, string>.InvalidMany(["first", "second"]),
            Validation<int, string>.Valid(3),
            Validation<int, string>.Invalid("third"),
        ]);

        var option = await optionSource.SequenceAsync();
        var result = await resultSource.SequenceAsync();
        var validation = await validationSource.SequenceAsync();

        Assert.True(option.IsNone);
        Assert.True(result.TryGetError(out var resultError));
        Assert.Equal("result", resultError);
        Assert.True(validation.TryGetErrors(out var errors));
        Assert.Equal(["first", "second", "third"], errors);
        Assert.Equal(2, optionSource.ItemsYielded);
        Assert.Equal(2, resultSource.ItemsYielded);
        Assert.Equal(4, validationSource.ItemsYielded);
        Assert.Equal(1, optionSource.DisposeCount);
        Assert.Equal(1, resultSource.DisposeCount);
        Assert.Equal(1, validationSource.DisposeCount);
    }

    [Fact]
    public async Task TraverseAsyncInvokesSyncSelectorsInReachedOrder()
    {
        var optionCalls = new List<int>();
        var resultCalls = new List<int>();
        var validationCalls = new List<int>();

        var option = await AsyncValues(1, 2, 3).TraverseAsync(value =>
        {
            optionCalls.Add(value);
            return value == 2 ? Option.None<int>() : Option.Some(value);
        });
        var result = await AsyncValues(1, 2, 3).TraverseAsync(value =>
        {
            resultCalls.Add(value);
            return value == 2
                ? Result<int, string>.Failure("bad")
                : Result<int, string>.Success(value);
        });
        var validation = await AsyncValues(1, 2, 3).TraverseAsync(value =>
        {
            validationCalls.Add(value);
            return value == 2
                ? Validation<int, string>.InvalidMany(["bad", "worse"])
                : Validation<int, string>.Valid(value);
        });

        Assert.True(option.IsNone);
        Assert.True(result.TryGetError(out var resultError));
        Assert.Equal("bad", resultError);
        Assert.True(validation.TryGetErrors(out var errors));
        Assert.Equal(["bad", "worse"], errors);
        Assert.Equal([1, 2], optionCalls);
        Assert.Equal([1, 2], resultCalls);
        Assert.Equal([1, 2, 3], validationCalls);
    }

    [Fact]
    public async Task TokenAwareValueTaskTraversalForwardsTheExactTokenWithoutEagerCancellation()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var optionSource = new ProbeAsyncEnumerable<int>([1, 2, 3]);
        var resultSource = new ProbeAsyncEnumerable<int>([1, 2, 3]);
        var validationSource = new ProbeAsyncEnumerable<int>([1, 2, 3]);
        var optionSelectorValues = new List<int>();
        var resultSelectorValues = new List<int>();
        var validationSelectorValues = new List<int>();
        var optionSelectorTokens = new List<CancellationToken>();
        var resultSelectorTokens = new List<CancellationToken>();
        var validationSelectorTokens = new List<CancellationToken>();

        var option = await optionSource.TraverseValueAsync((value, token) =>
        {
            optionSelectorValues.Add(value);
            optionSelectorTokens.Add(token);
            return ValueTask.FromResult(value == 2 ? Option.None<int>() : Option.Some(value));
        }, cancellationSource.Token);
        var result = await resultSource.TraverseValueAsync((value, token) =>
        {
            resultSelectorValues.Add(value);
            resultSelectorTokens.Add(token);
            return ValueTask.FromResult(value == 2
                ? Result<int, string>.Failure("bad")
                : Result<int, string>.Success(value));
        }, cancellationSource.Token);
        var validation = await validationSource.TraverseValueAsync((value, token) =>
        {
            validationSelectorValues.Add(value);
            validationSelectorTokens.Add(token);
            return ValueTask.FromResult(value == 2
                ? Validation<int, string>.InvalidMany(["first", "second"])
                : value == 3
                    ? Validation<int, string>.Invalid("third")
                    : Validation<int, string>.Valid(value));
        }, cancellationSource.Token);

        Assert.True(option.IsNone);
        Assert.True(result.TryGetError(out var resultError));
        Assert.Equal("bad", resultError);
        Assert.True(validation.TryGetErrors(out var validationErrors));
        Assert.Equal(["first", "second", "third"], validationErrors);
        Assert.Equal([1, 2], optionSelectorValues);
        Assert.Equal([1, 2], resultSelectorValues);
        Assert.Equal([1, 2, 3], validationSelectorValues);
        AssertTokenForwarded(optionSource, optionSelectorTokens, cancellationSource.Token);
        AssertTokenForwarded(resultSource, resultSelectorTokens, cancellationSource.Token);
        AssertTokenForwarded(validationSource, validationSelectorTokens, cancellationSource.Token);
        Assert.Equal(1, optionSource.DisposeCount);
        Assert.Equal(1, resultSource.DisposeCount);
        Assert.Equal(1, validationSource.DisposeCount);
    }

    [Fact]
    public async Task CancellationFromSourcePreservesTheOriginalTokenAndCanceledStatus()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var optionSource = new ProbeAsyncEnumerable<Option<int>>(
            [],
            cancelOnMoveNextToken: cancellationSource.Token);
        var resultSource = new ProbeAsyncEnumerable<Result<int, string>>(
            [],
            cancelOnMoveNextToken: cancellationSource.Token);
        var validationSource = new ProbeAsyncEnumerable<Validation<int, string>>(
            [],
            cancelOnMoveNextToken: cancellationSource.Token);

        var optionOperation = optionSource.SequenceAsync(cancellationSource.Token).AsTask();
        var resultOperation = resultSource.SequenceAsync(cancellationSource.Token).AsTask();
        var validationOperation = validationSource.SequenceAsync(cancellationSource.Token).AsTask();

        await AssertCanceledWithToken(optionOperation, cancellationSource.Token);
        await AssertCanceledWithToken(resultOperation, cancellationSource.Token);
        await AssertCanceledWithToken(validationOperation, cancellationSource.Token);
        AssertCanceledSource(optionSource, cancellationSource.Token);
        AssertCanceledSource(resultSource, cancellationSource.Token);
        AssertCanceledSource(validationSource, cancellationSource.Token);
    }

    [Fact]
    public async Task TraversalPropagatesNormalFaultsByIdentityAndDisposes()
    {
        var sourceException = new InvalidOperationException("source");
        var resultSourceException = new InvalidOperationException("result source");
        var validationSourceException = new InvalidOperationException("validation source");
        var selectorException = new InvalidOperationException("selector");
        var resultSelectorException = new InvalidOperationException("result selector");
        var validationSelectorException = new InvalidOperationException("validation selector");
        var optionSource = new ProbeAsyncEnumerable<Option<int>>([], moveNextException: sourceException);
        var resultSource = new ProbeAsyncEnumerable<Result<int, string>>(
            [],
            moveNextException: resultSourceException);
        var validationSource = new ProbeAsyncEnumerable<Validation<int, string>>(
            [],
            moveNextException: validationSourceException);
        var optionSelectorSource = new ProbeAsyncEnumerable<int>([1]);
        var resultSelectorSource = new ProbeAsyncEnumerable<int>([1]);
        var validationSelectorSource = new ProbeAsyncEnumerable<int>([1]);

        await AssertFaultIsSame(optionSource.SequenceAsync().AsTask(), sourceException);
        await AssertFaultIsSame(resultSource.SequenceAsync().AsTask(), resultSourceException);
        await AssertFaultIsSame(validationSource.SequenceAsync().AsTask(), validationSourceException);
        await AssertFaultIsSame(
            optionSelectorSource.TraverseAsync<int, int>(_ => throw selectorException).AsTask(),
            selectorException);
        await AssertFaultIsSame(
            resultSelectorSource.TraverseAsync<int, int, string>(
                (Func<int, Result<int, string>>)(_ => throw resultSelectorException)).AsTask(),
            resultSelectorException);
        await AssertFaultIsSame(
            validationSelectorSource.TraverseAsync<int, int, string>(
                (Func<int, Validation<int, string>>)(_ => throw validationSelectorException)).AsTask(),
            validationSelectorException);

        Assert.Equal(1, optionSource.DisposeCount);
        Assert.Equal(1, resultSource.DisposeCount);
        Assert.Equal(1, validationSource.DisposeCount);
        Assert.Equal(1, optionSelectorSource.DisposeCount);
        Assert.Equal(1, resultSelectorSource.DisposeCount);
        Assert.Equal(1, validationSelectorSource.DisposeCount);
    }

    [Fact]
    public async Task FaultedOperationCanceledExceptionCompletesTheReturnedOperationAsCanceled()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var cancellation = new OperationCanceledException(cancellationSource.Token);
        var source = new ProbeAsyncEnumerable<Option<int>>([], moveNextException: cancellation);

        var operation = source.SequenceAsync();
        var actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await operation);

        Assert.True(operation.IsCanceled);
        Assert.Equal(cancellationSource.Token, actual.CancellationToken);
        Assert.Equal(1, source.DisposeCount);

        var optionSelectorSource = new ProbeAsyncEnumerable<int>([1]);
        var resultSelectorSource = new ProbeAsyncEnumerable<int>([1]);
        var validationSelectorSource = new ProbeAsyncEnumerable<int>([1]);
        var optionSelectorOperation = optionSelectorSource.TraverseValueAsync<int, int>(
            _ => ValueTask.FromCanceled<Option<int>>(cancellationSource.Token)).AsTask();
        var resultSelectorOperation = resultSelectorSource.TraverseValueAsync<int, int, string>(
            _ => ValueTask.FromCanceled<Result<int, string>>(cancellationSource.Token)).AsTask();
        var validationSelectorOperation = validationSelectorSource.TraverseValueAsync<int, int, string>(
            _ => ValueTask.FromCanceled<Validation<int, string>>(cancellationSource.Token)).AsTask();

        await AssertCanceledWithToken(optionSelectorOperation, cancellationSource.Token);
        await AssertCanceledWithToken(resultSelectorOperation, cancellationSource.Token);
        await AssertCanceledWithToken(validationSelectorOperation, cancellationSource.Token);
        Assert.Equal(1, optionSelectorSource.DisposeCount);
        Assert.Equal(1, resultSelectorSource.DisposeCount);
        Assert.Equal(1, validationSelectorSource.DisposeCount);
    }

    [Fact]
    public async Task TraverseValueAsyncConsumesEachSelectorValueTaskOnce()
    {
        var optionSource = new CountingValueTaskSource<Option<int>>(Option.Some(7));
        var resultSource = new CountingValueTaskSource<Result<int, string>>(
            Result<int, string>.Success(8));
        var validationSource = new CountingValueTaskSource<Validation<int, string>>(
            Validation<int, string>.Valid(9));

        var option = await AsyncValues(1).TraverseValueAsync(_ => optionSource.CreateValueTask());
        var result = await AsyncValues(1).TraverseValueAsync(_ => resultSource.CreateValueTask());
        var validation = await AsyncValues(1).TraverseValueAsync(
            _ => validationSource.CreateValueTask());

        Assert.Equal([7], GetOptionValues(option));
        Assert.Equal([8], GetResultValues(result));
        Assert.Equal([9], GetValidationValues(validation));
        Assert.Equal(1, optionSource.GetResultCount);
        Assert.Equal(1, resultSource.GetResultCount);
        Assert.Equal(1, validationSource.GetResultCount);
    }

    [Fact]
    public async Task FaultedTraverseValueAsyncSelectorsPropagateOnceAndDisposeTheirSources()
    {
        var optionFailure = new InvalidOperationException("option selector");
        var resultFailure = new InvalidOperationException("result selector");
        var validationFailure = new InvalidOperationException("validation selector");
        var optionValueTask = new CountingValueTaskSource<Option<int>>();
        var resultValueTask = new CountingValueTaskSource<Result<int, string>>();
        var validationValueTask = new CountingValueTaskSource<Validation<int, string>>();
        optionValueTask.SetException(optionFailure);
        resultValueTask.SetException(resultFailure);
        validationValueTask.SetException(validationFailure);
        var optionSource = new ProbeAsyncEnumerable<int>([1]);
        var resultSource = new ProbeAsyncEnumerable<int>([1]);
        var validationSource = new ProbeAsyncEnumerable<int>([1]);

        Assert.Same(
            optionFailure,
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await optionSource.TraverseValueAsync(_ => optionValueTask.CreateValueTask())));
        Assert.Same(
            resultFailure,
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await resultSource.TraverseValueAsync<int, int, string>(_ => resultValueTask.CreateValueTask())));
        Assert.Same(
            validationFailure,
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await validationSource.TraverseValueAsync<int, int, string>(
                    _ => validationValueTask.CreateValueTask())));

        Assert.Equal(1, optionValueTask.GetResultCount);
        Assert.Equal(1, resultValueTask.GetResultCount);
        Assert.Equal(1, validationValueTask.GetResultCount);
        Assert.Equal(1, optionSource.DisposeCount);
        Assert.Equal(1, resultSource.DisposeCount);
        Assert.Equal(1, validationSource.DisposeCount);
    }

    [Fact]
    public async Task SequenceAsyncConsumesEachMoveNextValueTaskOnce()
    {
        var optionSource = new CountingMoveNextAsyncEnumerable<Option<int>>([Option.Some(1)]);
        var resultSource = new CountingMoveNextAsyncEnumerable<Result<int, string>>(
            [Result<int, string>.Success(2)]);
        var validationSource = new CountingMoveNextAsyncEnumerable<Validation<int, string>>(
            [Validation<int, string>.Valid(3)]);

        var option = await optionSource.SequenceAsync();
        var result = await resultSource.SequenceAsync();
        var validation = await validationSource.SequenceAsync();

        Assert.Equal([1], GetOptionValues(option));
        Assert.Equal([2], GetResultValues(result));
        Assert.Equal([3], GetValidationValues(validation));
        AssertMoveNextValueTasksConsumedOnce(optionSource);
        AssertMoveNextValueTasksConsumedOnce(resultSource);
        AssertMoveNextValueTasksConsumedOnce(validationSource);
    }

    [Fact]
    public async Task SequenceAsyncPropagatesDisposeFaultsByIdentity()
    {
        var optionException = new InvalidOperationException("option dispose");
        var resultException = new InvalidOperationException("result dispose");
        var validationException = new InvalidOperationException("validation dispose");
        var optionSource = new ProbeAsyncEnumerable<Option<int>>(
            [Option.Some(1)],
            disposeException: optionException);
        var resultSource = new ProbeAsyncEnumerable<Result<int, string>>(
            [Result<int, string>.Success(1)],
            disposeException: resultException);
        var validationSource = new ProbeAsyncEnumerable<Validation<int, string>>(
            [Validation<int, string>.Valid(1)],
            disposeException: validationException);

        await AssertFaultIsSame(optionSource.SequenceAsync().AsTask(), optionException);
        await AssertFaultIsSame(resultSource.SequenceAsync().AsTask(), resultException);
        await AssertFaultIsSame(validationSource.SequenceAsync().AsTask(), validationException);
        Assert.Equal(1, optionSource.DisposeCount);
        Assert.Equal(1, resultSource.DisposeCount);
        Assert.Equal(1, validationSource.DisposeCount);
    }

    [Fact]
    public async Task LargeAsyncSequencesAreStackSafe()
    {
        const int count = 10_000;

        var option = await AsyncRange(count).TraverseAsync(static value => Option.Some(value));
        var result = await AsyncRange(count).TraverseValueAsync(
            static value => ValueTask.FromResult(Result<int, string>.Success(value)));
        var validation = await AsyncRange(count).TraverseValueAsync(
            static value => ValueTask.FromResult(Validation<int, string>.Valid(value)));

        AssertLargeSequence(GetOptionValues(option), count);
        AssertLargeSequence(GetResultValues(result), count);
        AssertLargeSequence(GetValidationValues(validation), count);
    }

    private static IReadOnlyList<T> GetOptionValues<T>(Option<IReadOnlyList<T>> option)
    {
        Assert.True(option.TryGetValue(out var values));
        return values!;
    }

    private static IReadOnlyList<T> GetResultValues<T>(Result<IReadOnlyList<T>, string> result)
    {
        Assert.True(result.TryGetValue(out var values));
        return values!;
    }

    private static IReadOnlyList<T> GetValidationValues<T>(Validation<IReadOnlyList<T>, string> validation)
    {
        Assert.True(validation.TryGetValue(out var values));
        return values!;
    }

    private static void AssertProbeCompleted<T>(ProbeAsyncEnumerable<T> probe)
    {
        Assert.Equal(1, probe.EnumeratorCount);
        Assert.Equal(2, probe.MoveNextCount);
        Assert.Equal(1, probe.ItemsYielded);
        Assert.Equal(1, probe.DisposeCount);
    }

    private static async Task AssertCanceledWithToken(Task operation, CancellationToken cancellationToken)
    {
        var actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation);

        Assert.True(operation.IsCanceled);
        Assert.Equal(cancellationToken, actual.CancellationToken);
    }

    private static void AssertCanceledSource<T>(
        ProbeAsyncEnumerable<T> source,
        CancellationToken cancellationToken)
    {
        Assert.Equal(cancellationToken, source.ReceivedToken);
        Assert.Equal(1, source.DisposeCount);
    }

    private static void AssertTokenForwarded<T>(
        ProbeAsyncEnumerable<T> source,
        IReadOnlyList<CancellationToken> selectorTokens,
        CancellationToken cancellationToken)
    {
        Assert.Equal(cancellationToken, source.ReceivedToken);
        Assert.All(selectorTokens, token => Assert.Equal(cancellationToken, token));
    }

    private static async Task AssertFaultIsSame(Task operation, InvalidOperationException expected)
    {
        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() => operation);

        Assert.True(operation.IsFaulted);
        Assert.Same(expected, actual);
    }

    private static void AssertLargeSequence(IReadOnlyList<int> values, int count)
    {
        Assert.Equal(count, values.Count);
        Assert.Equal(0, values[0]);
        Assert.Equal(count - 1, values[^1]);
    }

    private static void AssertMoveNextValueTasksConsumedOnce<T>(CountingMoveNextAsyncEnumerable<T> source)
    {
        Assert.Equal(2, source.MoveNextValueTasks.Count);
        Assert.All(source.MoveNextValueTasks, valueTask => Assert.Equal(1, valueTask.GetResultCount));
        Assert.Equal(1, source.DisposeCount);
    }

    private static async IAsyncEnumerable<T> AsyncValues<T>(params T[] values)
    {
        foreach (var value in values)
        {
            yield return value;
            await Task.Yield();
        }
    }

    private static async IAsyncEnumerable<int> AsyncRange(int count)
    {
        await Task.Yield();
        for (var value = 0; value < count; value++)
        {
            yield return value;
        }
    }

    private sealed class ProbeAsyncEnumerable<T>(
        IReadOnlyList<T> values,
        CancellationToken? cancelOnMoveNextToken = null,
        Exception? moveNextException = null,
        Exception? disposeException = null) : IAsyncEnumerable<T>
    {
        public int EnumeratorCount { get; private set; }

        public int MoveNextCount { get; private set; }

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
                owner.MoveNextCount++;
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
                var hasNext = nextIndex < values.Count;
                var valueTaskSource = new CountingValueTaskSource<bool>(hasNext);
                owner.MoveNextValueTasks.Add(valueTaskSource);
                if (hasNext)
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
