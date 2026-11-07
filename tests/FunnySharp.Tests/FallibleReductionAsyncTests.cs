using System.Runtime.CompilerServices;
using FunnySharp.TestSupport.FallibleReduction;

namespace FunnySharp.Tests;

public sealed class FallibleReductionAsyncTests
{
    [Fact]
    public async Task EmptyAsyncSourceReturnsTheExactSeedWithoutInvokingTheStep()
    {
        var seed = new object();
        var probe = new AsyncReductionProbe<int>([]);
        using var cancellationSource = new CancellationTokenSource();
        var calls = 0;

        var result = await probe.AggregateResultAsync(
            seed,
            (object accumulator, int item) =>
            {
                calls++;
                return Result<object, string>.Success(accumulator);
            },
            cancellationSource.Token);

        Assert.True(result.TryGetValue(out var value));
        Assert.Same(seed, value);
        Assert.Equal(0, calls);
        Assert.Equal(1, probe.GetAsyncEnumeratorCalls);
        Assert.Equal(1, probe.MoveNextAsyncCalls);
        Assert.Equal(0, probe.CurrentReads);
        Assert.Equal(1, probe.DisposeAsyncCalls);
        Assert.Equal(cancellationSource.Token, probe.Token);
    }

    [Fact]
    public async Task AsyncNullSeedAndNullSuccessfulAccumulatorArePreserved()
    {
        var empty = new AsyncReductionProbe<int>([]);
        var single = new AsyncReductionProbe<int>([1]);
        using var cancellationSource = new CancellationTokenSource();

        var fromEmpty = await empty.AggregateResultAsync(
            (string?)null,
            (accumulator, item) => Result<string?, string>.Success(accumulator + item),
            cancellationSource.Token);
        var fromStep = await single.AggregateResultAsync(
            (string?)"start",
            (_, _) => Result<string?, string>.Success(null),
            cancellationSource.Token);

        Assert.True(fromEmpty.TryGetValue(out var emptyValue));
        Assert.Null(emptyValue);
        Assert.True(fromStep.TryGetValue(out var stepValue));
        Assert.Null(stepValue);
        Assert.Equal(0, empty.CurrentReads);
        Assert.Equal(1, single.CurrentReads);
    }

    [Fact]
    public async Task AsyncNullFailurePayloadsArePreserved()
    {
        var probe = new AsyncReductionProbe<int>([1]);
        using var cancellationSource = new CancellationTokenSource();

        var result = await probe.AggregateResultAsync(
            0,
            (_, _) => Result<int, string?>.Failure(null),
            cancellationSource.Token);

        Assert.True(result.TryGetError(out var error));
        Assert.Null(error);
        Assert.Equal(1, probe.MoveNextAsyncCalls);
        Assert.Equal(1, probe.CurrentReads);
        Assert.Equal(1, probe.DisposeAsyncCalls);
    }

    [Fact]
    public async Task AsyncSuccessfulFoldVisitsEveryItemOnceInSourceOrder()
    {
        var probe = new AsyncReductionProbe<int>([1, 2, 3]);
        using var cancellationSource = new CancellationTokenSource();
        var steps = 0;
        var visited = new List<int>();

        var result = await probe.AggregateResultAsync(
            0,
            (total, item) =>
            {
                steps++;
                visited.Add(item);
                return Result<int, string>.Success(total + item);
            },
            cancellationSource.Token);

        Assert.Equal(Result<int, string>.Success(6), result);
        Assert.Equal([1, 2, 3], visited);
        Assert.Equal(3, steps);
        Assert.Equal(1, probe.GetAsyncEnumeratorCalls);
        Assert.Equal(4, probe.MoveNextAsyncCalls);
        Assert.Equal(3, probe.CurrentReads);
        Assert.Equal(1, probe.DisposeAsyncCalls);
    }

    [Fact]
    public async Task AsyncFirstFailureStopsBeforeTheNextMoveNextAndDisposesOnce()
    {
        var probe = new AsyncReductionProbe<int>([1, 0, 2]);
        using var cancellationSource = new CancellationTokenSource();
        var error = new object();
        var steps = 0;

        var result = await probe.AggregateResultAsync(
            0,
            (total, item) =>
            {
                steps++;
                return item == 0
                    ? Result<int, object>.Failure(error)
                    : Result<int, object>.Success(total + item);
            },
            cancellationSource.Token);

        Assert.True(result.TryGetError(out var actualError));
        Assert.Same(error, actualError);
        Assert.Equal(2, steps);
        Assert.Equal(2, probe.MoveNextAsyncCalls);
        Assert.Equal(2, probe.CurrentReads);
        Assert.Equal(1, probe.DisposeAsyncCalls);
    }

    [Fact]
    public async Task SynchronousStepExceptionIsDeliveredThroughTheReturnedOperation()
    {
        var probe = new AsyncReductionProbe<int>([1]);
        using var cancellationSource = new CancellationTokenSource();
        var fault = new InvalidOperationException("synchronous step fault");

        var throwStep = (Func<int, int, Result<int, string>>)((_, _) => throw fault);
        var task = probe.AggregateResultAsync(
            0,
            throwStep,
            cancellationSource.Token).AsTask();

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() => task);

        Assert.Same(fault, thrown);
        Assert.Equal(1, probe.MoveNextAsyncCalls);
        Assert.Equal(1, probe.DisposeAsyncCalls);
    }

    [Fact]
    public async Task AsynchronousFoldStartsEnumeratingWhenCalled()
    {
        var probe = new AsyncReductionProbe<int>([1]);
        using var cancellationSource = new CancellationTokenSource();

        var operation = probe.AggregateResultAsync(
            0,
            (total, item) => Result<int, string>.Success(total + item),
            cancellationSource.Token);

        Assert.Equal(1, probe.GetAsyncEnumeratorCalls);
        Assert.True(operation.IsCompletedSuccessfully);

        var result = await operation;

        Assert.Equal(Result<int, string>.Success(1), result);
        Assert.Equal(1, probe.DisposeAsyncCalls);
    }

    [Fact]
    public async Task AsyncMutableAccumulatorKeepsMutationsAfterFailureWithoutRollback()
    {
        var probe = new AsyncReductionProbe<ReductionLine>(
            [ReductionFixtures.Book, ReductionFixtures.InvalidQuantity, ReductionFixtures.Pen]);
        using var cancellationSource = new CancellationTokenSource();
        var total = new MutableReductionTotal();
        var error = new ReductionError("quantity", "SKU-BAD|0");

        var result = await probe.AggregateResultAsync(
            total,
            (accumulator, line) =>
            {
                accumulator.Add(line);
                return line.Quantity > 0
                    ? Result<MutableReductionTotal, ReductionError>.Success(accumulator)
                    : Result<MutableReductionTotal, ReductionError>.Failure(error);
            },
            cancellationSource.Token);

        Assert.True(result.TryGetError(out var actual));
        Assert.Equal(error, actual);
        Assert.Equal(2, total.Count);
        Assert.Equal(["SKU-BOOK", "SKU-BAD"], total.Lines.Select(line => line.Sku));
        Assert.Equal(2, probe.MoveNextAsyncCalls);
    }

    [Fact]
    public async Task AsyncSourceExceptionPropagatesUnchangedAndDisposesTheEnumerator()
    {
        var fault = new InvalidOperationException("async source fault");
        var probe = new AsyncReductionProbe<int>([1, 2, 3], faultAtIndex: 1, fault);
        using var cancellationSource = new CancellationTokenSource();

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await probe.AggregateResultAsync(
                0,
                (total, item) => Result<int, string>.Success(total + item),
                cancellationSource.Token));

        Assert.Same(fault, thrown);
        Assert.Equal(2, probe.MoveNextAsyncCalls);
        Assert.Equal(1, probe.CurrentReads);
        Assert.Equal(1, probe.DisposeAsyncCalls);
    }

    [Fact]
    public async Task AsyncStepExceptionPropagatesUnchangedAndStopsConsumption()
    {
        var fault = new FormatException("async step fault");
        var probe = new AsyncReductionProbe<int>([1, 2, 3]);
        using var cancellationSource = new CancellationTokenSource();
        var steps = 0;

        var thrown = await Assert.ThrowsAsync<FormatException>(
            async () => await probe.AggregateResultValueAsync(
                0,
                (total, item) =>
                {
                    steps++;
                    return item == 2
                        ? ValueTask.FromException<Result<int, string>>(fault)
                        : ValueTask.FromResult(Result<int, string>.Success(total + item));
                },
                cancellationSource.Token));

        Assert.Same(fault, thrown);
        Assert.Equal(2, steps);
        Assert.Equal(2, probe.MoveNextAsyncCalls);
        Assert.Equal(2, probe.CurrentReads);
        Assert.Equal(1, probe.DisposeAsyncCalls);
    }

    [Fact]
    public async Task AsyncUninitializedStepResultFaultsTheReturnedOperationAndStops()
    {
        var probe = new AsyncReductionProbe<int>([1, 2]);
        using var cancellationSource = new CancellationTokenSource();
#pragma warning disable FS1001 // Intentional uninitialized step-result witness.
        Result<int, string> uninitialized = default;
#pragma warning restore FS1001

        var task = probe.AggregateResultValueAsync(
            0,
            (_, _) => ValueTask.FromResult(uninitialized),
            cancellationSource.Token).AsTask();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => task);

        Assert.Equal("The result has not been initialized.", exception.Message);
        Assert.Equal(1, probe.MoveNextAsyncCalls);
        Assert.Equal(1, probe.DisposeAsyncCalls);
    }

    [Fact]
    public async Task AsyncTokenIsForwardedExactlyToTheEnumeratorAndTokenAwareStep()
    {
        var probe = new AsyncReductionProbe<int>([1, 2]);
        using var cancellationSource = new CancellationTokenSource();
        var observedStepToken = default(CancellationToken);

        var result = await probe.AggregateResultValueAsync(
            0,
            (total, item, stepToken) =>
            {
                observedStepToken = stepToken;
                return ValueTask.FromResult(Result<int, string>.Success(total + item));
            },
            cancellationSource.Token);

        Assert.Equal(Result<int, string>.Success(3), result);
        Assert.Equal(cancellationSource.Token, probe.Token);
        Assert.Equal(cancellationSource.Token, observedStepToken);
    }

    [Fact]
    public async Task AlreadyCanceledTokenIsNotInspectedEagerly()
    {
        var probe = new AsyncReductionProbe<int>([1]);
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var observedStepToken = default(CancellationToken);

        var synchronousStep = await probe.AggregateResultAsync(
            0,
            (total, item) => Result<int, string>.Success(total + item),
            cancellationSource.Token);
        var tokenAwareStep = await probe.AggregateResultValueAsync(
            0,
            (total, item, stepToken) =>
            {
                observedStepToken = stepToken;
                return ValueTask.FromResult(Result<int, string>.Success(total + item));
            },
            cancellationSource.Token);

        Assert.Equal(Result<int, string>.Success(1), synchronousStep);
        Assert.Equal(Result<int, string>.Success(1), tokenAwareStep);
        Assert.Equal(cancellationSource.Token, observedStepToken);
    }

    [Fact]
    public async Task CanceledSourceProducesACanceledOperationWithTheOriginalToken()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        var task = CancelableSource(cancellationSource.Token)
            .AggregateResultAsync(
                0,
                (total, item) => Result<int, string>.Success(total + item),
                cancellationSource.Token)
            .AsTask();

        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);

        Assert.True(task.IsCanceled);
        Assert.Equal(cancellationSource.Token, exception.CancellationToken);
    }

    [Fact]
    public async Task CanceledStepProducesACanceledOperationWithTheOriginalToken()
    {
        var probe = new AsyncReductionProbe<int>([1]);
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        var task = probe.AggregateResultValueAsync(
            0,
            (_, _) => ValueTask.FromCanceled<Result<int, string>>(cancellationSource.Token),
            cancellationSource.Token).AsTask();

        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);

        Assert.True(task.IsCanceled);
        Assert.Equal(cancellationSource.Token, exception.CancellationToken);
        Assert.Equal(1, probe.DisposeAsyncCalls);
    }

    [Fact]
    public async Task FaultedOperationCanceledStepCompletesTheOperationAsCanceled()
    {
        var probe = new AsyncReductionProbe<int>([1]);
        using var cancellationSource = new CancellationTokenSource();
        var canceled = new OperationCanceledException(cancellationSource.Token);

        var task = probe.AggregateResultValueAsync(
            0,
            (_, _) => ValueTask.FromException<Result<int, string>>(canceled),
            cancellationSource.Token).AsTask();

        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);

        Assert.True(task.IsCanceled);
        Assert.Equal(cancellationSource.Token, exception.CancellationToken);
        Assert.Equal(1, probe.DisposeAsyncCalls);
    }

    [Fact]
    public async Task PendingStepKeepsTheReturnedOperationPendingAndDisposesOnce()
    {
        var probe = new AsyncReductionProbe<int>([1]);
        using var cancellationSource = new CancellationTokenSource();
        var completion = new TaskCompletionSource<Result<int, string>>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        var operation = probe.AggregateResultValueAsync(
            0,
            (_, _) => new ValueTask<Result<int, string>>(completion.Task),
            cancellationSource.Token).AsTask();

        Assert.False(operation.IsCompleted);
        completion.SetResult(Result<int, string>.Success(5));

        Assert.Equal(Result<int, string>.Success(5), await operation);
        Assert.Equal(1, probe.DisposeAsyncCalls);
    }

    [Fact]
    public async Task StepValueTaskIsObservedExactlyOnce()
    {
        var probe = new AsyncReductionProbe<int>([1]);
        using var cancellationSource = new CancellationTokenSource();
        var source = new CountingValueTaskSource<Result<int, string>>(
            Result<int, string>.Success(7));

        var result = await probe.AggregateResultValueAsync(
            0,
            (_, _) => source.CreateValueTask(),
            cancellationSource.Token);

        Assert.Equal(Result<int, string>.Success(7), result);
        Assert.Equal(1, source.GetResultCount);
    }

    [Fact]
    public void AsyncNullSourceOrStepIsRejectedBeforeEnumeration()
    {
        IAsyncEnumerable<int> nullSource = null!;
        var probe = new AsyncReductionProbe<int>([1]);
        using var cancellationSource = new CancellationTokenSource();

        Assert.Throws<ArgumentNullException>(
            () => nullSource.AggregateResultAsync(
                0,
                (total, item) => Result<int, string>.Success(total + item),
                cancellationSource.Token));
        Assert.Throws<ArgumentNullException>(
            () => probe.AggregateResultAsync(
                0,
                (Func<int, int, Result<int, string>>)null!,
                cancellationSource.Token));
        Assert.Throws<ArgumentNullException>(
            () => probe.AggregateResultValueAsync(
                0,
                (Func<int, int, ValueTask<Result<int, string>>>)null!,
                cancellationSource.Token));
        Assert.Throws<ArgumentNullException>(
            () => probe.AggregateResultValueAsync(
                0,
                (Func<int, int, CancellationToken, ValueTask<Result<int, string>>>)null!,
                cancellationSource.Token));

        Assert.Equal(0, probe.GetAsyncEnumeratorCalls);
    }

    [Fact]
    public async Task AsyncDisposalExceptionReplacesThePendingFailure()
    {
        var disposeFault = new InvalidOperationException("async dispose fault");
        var probe = new AsyncReductionProbe<int>([1], disposeException: disposeFault);
        using var cancellationSource = new CancellationTokenSource();

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await probe.AggregateResultAsync(
                0,
                (_, _) => Result<int, string>.Failure("rejected"),
                cancellationSource.Token));

        Assert.Same(disposeFault, thrown);
        Assert.Equal(1, probe.DisposeAsyncCalls);
    }

    private static async IAsyncEnumerable<int> CancelableSource(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();
        yield return 1;
    }
}
