using FunnySharp.TestSupport.FallibleReduction;

namespace FunnySharp.Tests;

public sealed class FallibleReductionTests
{
    [Fact]
    public void EmptySourceReturnsTheExactSeedWithoutInvokingTheStep()
    {
        var seed = new object();
        var probe = new ReductionProbe<int>([]);
        var calls = 0;

        var result = probe.AggregateResult(
            seed,
            (object accumulator, int item) =>
            {
                calls++;
                return Result<object, string>.Success(accumulator);
            });

        Assert.True(result.TryGetValue(out var value));
        Assert.Same(seed, value);
        Assert.Equal(0, calls);
        Assert.Equal(1, probe.GetEnumeratorCalls);
        Assert.Equal(1, probe.MoveNextCalls);
        Assert.Equal(0, probe.CurrentReads);
        Assert.Equal(1, probe.DisposeCalls);
    }

    [Fact]
    public void SuccessfulFoldVisitsEveryItemOnceInSourceOrderAndReturnsTheFinalAccumulator()
    {
        var probe = new ReductionProbe<ReductionLine>(
            [ReductionFixtures.Book, ReductionFixtures.Pen, ReductionFixtures.Notebook]);
        var visited = new List<string>();
        var steps = 0;

        var result = probe.AggregateResult(
            new ReductionTotal(),
            (total, line) =>
            {
                steps++;
                visited.Add(line.Sku);
                Assert.Equal(total, new ReductionTotal(steps - 1, total.Amount));
                return ReductionRules.Add(total, line);
            });

        Assert.True(result.TryGetValue(out var total));
        Assert.Equal(3, total.Count);
        Assert.Equal(
            ReductionFixtures.Book.Amount + ReductionFixtures.Pen.Amount + ReductionFixtures.Notebook.Amount,
            total.Amount);
        Assert.Equal(
            ["SKU-BOOK", "SKU-PEN", "SKU-NOTE"],
            visited);
        Assert.Equal(3, steps);
        Assert.Equal(1, probe.GetEnumeratorCalls);
        Assert.Equal(4, probe.MoveNextCalls);
        Assert.Equal(3, probe.CurrentReads);
        Assert.Equal(1, probe.DisposeCalls);
    }

    [Fact]
    public void FirstFailureIsReturnedAndNoLaterSourceOrStepRequestIsMade()
    {
        var probe = new ReductionProbe<ReductionLine>(
            [ReductionFixtures.Book, ReductionFixtures.InvalidQuantity, ReductionFixtures.Notebook]);
        var visited = new List<string>();

        var result = probe.AggregateResult(
            new ReductionTotal(),
            (total, line) =>
            {
                visited.Add(line.Sku);
                return ReductionRules.Add(total, line);
            });

        Assert.True(result.TryGetError(out var error));
        Assert.Equal(new ReductionError("quantity", "SKU-BAD|0"), error);
        Assert.Equal(["SKU-BOOK", "SKU-BAD"], visited);
        Assert.Equal(2, probe.MoveNextCalls);
        Assert.Equal(2, probe.CurrentReads);
        Assert.Equal(1, probe.DisposeCalls);
    }

    [Fact]
    public void StepFailurePreservesTheExactErrorPayload()
    {
        var error = new object();
        var probe = new ReductionProbe<int>([1, 2]);

        var result = probe.AggregateResult(
            new object(),
            (accumulator, _) => Result<object, object>.Failure(error));

        Assert.True(result.TryGetError(out var actual));
        Assert.Same(error, actual);
    }

    [Fact]
    public void SourceExceptionPropagatesUnchangedAndDisposesTheEnumerator()
    {
        var fault = new InvalidOperationException("source fault");
        var probe = new ReductionProbe<int>([1, 2, 3], faultAtIndex: 1, fault);
        var steps = 0;

        var thrown = Assert.Throws<InvalidOperationException>(
            () => probe.AggregateResult(
                0,
                (total, item) =>
                {
                    steps++;
                    return Result<int, string>.Success(total + item);
                }));

        Assert.Same(fault, thrown);
        Assert.Equal(1, steps);
        Assert.Equal(2, probe.MoveNextCalls);
        Assert.Equal(1, probe.CurrentReads);
        Assert.Equal(1, probe.DisposeCalls);
    }

    [Fact]
    public void StepExceptionPropagatesUnchangedAndStopsBeforeTheNextSourceRequest()
    {
        var fault = new FormatException("step fault");
        var probe = new ReductionProbe<int>([1, 2, 3]);
        var steps = 0;

        var thrown = Assert.Throws<FormatException>(
            () => probe.AggregateResult(
                0,
                (total, item) =>
                {
                    steps++;
                    if (item == 2)
                    {
                        throw fault;
                    }

                    return Result<int, string>.Success(total + item);
                }));

        Assert.Same(fault, thrown);
        Assert.Equal(2, steps);
        Assert.Equal(2, probe.MoveNextCalls);
        Assert.Equal(2, probe.CurrentReads);
        Assert.Equal(1, probe.DisposeCalls);
    }

    [Fact]
    public void UninitializedStepResultThrowsAndDisposesTheEnumerator()
    {
        var probe = new ReductionProbe<int>([1, 2]);
        var steps = 0;
#pragma warning disable FS1001 // Intentional uninitialized step-result witness.
        Result<int, string> uninitialized = default;
#pragma warning restore FS1001

        var exception = Assert.Throws<InvalidOperationException>(
            () => probe.AggregateResult(
                0,
                (_, _) =>
                {
                    steps++;
                    return uninitialized;
                }));

        Assert.Equal("The result has not been initialized.", exception.Message);
        Assert.Equal(1, steps);
        Assert.Equal(1, probe.MoveNextCalls);
        Assert.Equal(1, probe.DisposeCalls);
    }

    [Fact]
    public void NullSeedsAndNullSuccessfulAccumulatorsArePreserved()
    {
        var empty = new ReductionProbe<int>([]);
        var single = new ReductionProbe<int>([1]);

        var fromEmpty = empty.AggregateResult(
            (string?)null,
            (accumulator, item) => Result<string?, string>.Success(accumulator + item));
        var fromStep = single.AggregateResult(
            (string?)"start",
            (_, _) => Result<string?, string>.Success(null));

        Assert.True(fromEmpty.TryGetValue(out var emptyValue));
        Assert.Null(emptyValue);
        Assert.True(fromStep.TryGetValue(out var stepValue));
        Assert.Null(stepValue);
    }

    [Fact]
    public void NullFailurePayloadsArePreserved()
    {
        var probe = new ReductionProbe<int>([1]);

        var result = probe.AggregateResult(
            0,
            (_, _) => Result<int, string?>.Failure(null));

        Assert.True(result.TryGetError(out var error));
        Assert.Null(error);
        Assert.Equal(1, probe.MoveNextCalls);
        Assert.Equal(1, probe.CurrentReads);
        Assert.Equal(1, probe.DisposeCalls);
    }

    [Fact]
    public void MutableAccumulatorKeepsMutationsAfterFailureWithoutRollback()
    {
        var total = new MutableReductionTotal();
        var probe = new ReductionProbe<ReductionLine>(
            [ReductionFixtures.Book, ReductionFixtures.InvalidQuantity, ReductionFixtures.Pen]);
        var error = new ReductionError("quantity", "SKU-BAD|0");

        var result = probe.AggregateResult(
            total,
            (accumulator, line) =>
            {
                accumulator.Add(line);
                return line.Quantity > 0
                    ? Result<MutableReductionTotal, ReductionError>.Success(accumulator)
                    : Result<MutableReductionTotal, ReductionError>.Failure(error);
            });

        Assert.True(result.TryGetError(out var actual));
        Assert.Equal(error, actual);
        Assert.Equal(2, total.Count);
        Assert.Equal(ReductionFixtures.Book.Amount, total.Amount);
        Assert.Equal(["SKU-BOOK", "SKU-BAD"], total.Lines.Select(line => line.Sku));
        Assert.Equal(2, probe.MoveNextCalls);
    }

    [Fact]
    public void MutableAccumulatorIdentityIsReturnedOnSuccess()
    {
        var total = new MutableReductionTotal();
        var probe = new ReductionProbe<ReductionLine>([ReductionFixtures.Book]);

        var result = probe.AggregateResult(total, ReductionRules.AddMutable);

        Assert.True(result.TryGetValue(out var returned));
        Assert.Same(total, returned);
        Assert.Equal(1, total.Count);
    }

    [Fact]
    public void RepeatedCallsEnumerateAgainWithoutCaching()
    {
        var probe = new ReductionProbe<int>([1, 2]);
        var steps = 0;

        var first = probe.AggregateResult(0, (total, item) =>
        {
            steps++;
            return Result<int, string>.Success(total + item);
        });
        var second = probe.AggregateResult(0, (total, item) =>
        {
            steps++;
            return Result<int, string>.Success(total + item);
        });

        Assert.Equal(Result<int, string>.Success(3), first);
        Assert.Equal(Result<int, string>.Success(3), second);
        Assert.Equal(4, steps);
        Assert.Equal(2, probe.GetEnumeratorCalls);
        Assert.Equal(6, probe.MoveNextCalls);
        Assert.Equal(4, probe.CurrentReads);
        Assert.Equal(2, probe.DisposeCalls);
    }

    [Fact]
    public void NullSourceOrStepIsRejectedBeforeEnumeration()
    {
        IEnumerable<int> nullSource = null!;
        var probe = new ReductionProbe<int>([1]);

        Assert.Throws<ArgumentNullException>(
            () => nullSource.AggregateResult(0, (total, item) => Result<int, string>.Success(total)));
        Assert.Throws<ArgumentNullException>(
            () => probe.AggregateResult(
                0,
                (Func<int, int, Result<int, string>>)null!));
        Assert.Equal(0, probe.GetEnumeratorCalls);
    }

    [Fact]
    public void DisposalExceptionReplacesThePendingFailureOrSuccess()
    {
        var disposeFault = new InvalidOperationException("dispose fault");

        var failedProbe = new ReductionProbe<int>(
            [1, 2],
            disposeException: disposeFault);
        var successfulProbe = new ReductionProbe<int>(
            [1, 2],
            disposeException: disposeFault);

        var failed = Assert.Throws<InvalidOperationException>(
            () => failedProbe.AggregateResult(
                0,
                (_, item) => Result<int, string>.Failure("rejected")));
        var succeeded = Assert.Throws<InvalidOperationException>(
            () => successfulProbe.AggregateResult(
                0,
                (total, item) => Result<int, string>.Success(total + item)));

        Assert.Same(disposeFault, failed);
        Assert.Same(disposeFault, succeeded);
        Assert.Equal(1, failedProbe.DisposeCalls);
        Assert.Equal(1, successfulProbe.DisposeCalls);
    }

    [Fact]
    public void SinglePassSourceIsEnumeratedOnce()
    {
        var enumerations = 0;
        IEnumerable<int> Source()
        {
            enumerations++;
            yield return 1;
            yield return 2;
        }

        var result = Source().AggregateResult(
            0,
            (total, item) => Result<int, string>.Success(total + item));

        Assert.Equal(Result<int, string>.Success(3), result);
        Assert.Equal(1, enumerations);
    }
}
