using FunnySharp.TestSupport.Checks;

namespace FunnySharp.Tests;

public sealed class CheckAllAsyncTests
{
    [Fact]
    public async Task CheckAllAsyncRunsRulesSequentiallyAndAccumulatesAfterFailure()
    {
        var order = CheckFixtures.ValidOrder;
        var log = new List<string>();
        var first = new TaskCompletionSource<UnitResult<CheckError>>();
        var second = new TaskCompletionSource<UnitResult<CheckError>>();

        var task = Result<CheckOrder, CheckError>.Success(order).CheckAllAsync(
        [
            _ =>
            {
                log.Add("first");
                return first.Task;
            },
            _ =>
            {
                log.Add("second");
                return second.Task;
            },
            _ =>
            {
                log.Add("third");
                return Task.FromResult(
                    UnitResult<CheckError>.Failure(new CheckError("third", "failure")));
            },
        ]);

        Assert.False(task.IsCompleted);
        Assert.Equal(["first"], log);

        first.SetResult(UnitResult<CheckError>.Failure(new CheckError("first", "failure")));

        Assert.False(task.IsCompleted);
        Assert.Equal(["first", "second"], log);

        second.SetResult(UnitResult<CheckError>.Success());

        var validation = await task;
        Assert.True(validation.TryGetErrors(out var errors));
        Assert.Equal(
            [new CheckError("first", "failure"), new CheckError("third", "failure")],
            errors);
        Assert.Equal(["first", "second", "third"], log);
    }

    [Fact]
    public async Task CheckAllAsyncEmptyRulesAndAlreadyFailedInputShortCircuit()
    {
        var order = CheckFixtures.ValidOrder;
        var empty = await Result<CheckOrder, CheckError>.Success(order).CheckAllAsync(
            Array.Empty<Func<CheckOrder, Task<UnitResult<CheckError>>>>());

        Assert.True(empty.TryGetValue(out var value));
        Assert.Same(order, value);

        var existing = new CheckError("existing", "already failed");
        var checks = new ProbeEnumerable<Func<CheckOrder, Task<UnitResult<CheckError>>>>(
            [_ => throw new InvalidOperationException("must not run")]);
        var failed = Result<CheckOrder, CheckError>.Failure(existing);
        var failedValidation = await failed.CheckAllAsync(checks);

        Assert.True(failedValidation.TryGetErrors(out var errors));
        Assert.Same(existing, Assert.Single(errors));
        Assert.Equal(0, checks.EnumeratorCount);
        Assert.Equal(0, checks.ItemsYielded);

        var invalid = Validation<CheckOrder, CheckError>.Invalid(existing);
        var invalidResult = await invalid.CheckAllAsync(checks);

        Assert.True(invalidResult.TryGetErrors(out var invalidErrors));
        Assert.Same(existing, Assert.Single(invalidErrors));
        Assert.Equal(0, checks.EnumeratorCount);

        Assert.Throws<ArgumentNullException>(
            () => { _ = failed.CheckAllAsync(null!); });
    }

    [Fact]
    public async Task CheckAllAsyncForwardsTheExactTokenToEveryReachedRule()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var tokens = new List<CancellationToken>();
        var order = CheckFixtures.ValidOrder;

        var validation = await Result<CheckOrder, CheckError>
            .Success(order)
            .CheckAllAsync(
                [
                    (_, token) =>
                    {
                        tokens.Add(token);
                        return Task.FromResult(UnitResult<CheckError>.Success());
                    },
                    (_, token) =>
                    {
                        tokens.Add(token);
                        return Task.FromResult(
                            UnitResult<CheckError>.Failure(new CheckError("typed", "failure")));
                    },
                    (_, token) =>
                    {
                        tokens.Add(token);
                        return Task.FromResult(UnitResult<CheckError>.Success());
                    },
                ],
                cancellationSource.Token);

        Assert.True(validation.TryGetErrors(out var errors));
        Assert.Equal(["typed"], errors.Select(error => error.Code));
        Assert.Equal(3, tokens.Count);
        Assert.All(tokens, token => Assert.Equal(cancellationSource.Token, token));

        var failedInputTokens = new List<CancellationToken>();
        var failed = Result<CheckOrder, CheckError>.Failure(
            new CheckError("existing", "already failed"));
        var failedValidation = await failed.CheckAllAsync(
            [
                (_, token) =>
                {
                    failedInputTokens.Add(token);
                    return Task.FromResult(UnitResult<CheckError>.Success());
                },
            ],
            cancellationSource.Token);

        Assert.True(failedValidation.TryGetErrors(out var failedErrors));
        Assert.Equal(["existing"], failedErrors.Select(error => error.Code));
        Assert.Empty(failedInputTokens);
    }

    [Fact]
    public async Task CheckAllAsyncStopsAtTheFirstFaultAndDoesNotInvokeLaterRules()
    {
        var expected = new InvalidOperationException("check failed");
        var log = new List<string>();
        var task = Result<int, string>.Success(1).CheckAllAsync(
        [
            _ =>
            {
                log.Add("first");
                return Task.FromResult(UnitResult<string>.Failure("typed"));
            },
            _ =>
            {
                log.Add("second");
                return Task.FromException<UnitResult<string>>(expected);
            },
            _ =>
            {
                log.Add("third");
                return Task.FromResult(UnitResult<string>.Success());
            },
        ]);

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() => task);

        Assert.Same(expected, actual);
        Assert.True(task.IsFaulted);
        Assert.Equal(["first", "second"], log);
    }

    [Fact]
    public async Task CheckAllAsyncPreservesFaultedCancellationAndCancellationStates()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var faultedCancellation = new OperationCanceledException(cancellationSource.Token);
        var calls = 0;
        var faulted = Result<int, string>.Success(1).CheckAllAsync(
        [
            _ => Task.FromException<UnitResult<string>>(faultedCancellation),
            _ =>
            {
                calls++;
                return Task.FromResult(UnitResult<string>.Success());
            },
        ]);
        var canceled = Result<int, string>.Success(1).CheckAllAsync(
        [
            _ => Task.FromCanceled<UnitResult<string>>(cancellationSource.Token),
            _ =>
            {
                calls++;
                return Task.FromResult(UnitResult<string>.Success());
            },
        ]);

        var faultedActual = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => faulted);
        var canceledActual = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => canceled);

        Assert.True(faulted.IsFaulted);
        Assert.False(faulted.IsCanceled);
        Assert.True(canceled.IsCanceled);
        Assert.False(canceled.IsFaulted);
        Assert.Same(faultedCancellation, faultedActual);
        Assert.Equal(cancellationSource.Token, canceledActual.CancellationToken);
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task CheckAllAsyncSynchronousCallbackFailuresBecomeFaultsOrCancellation()
    {
        var expected = new InvalidOperationException("synchronous callback");
        using var cancellationSource = new CancellationTokenSource();
        var cancellation = new OperationCanceledException(cancellationSource.Token);
        var calls = 0;

        var faulted = Result<int, string>.Success(1).CheckAllAsync(
        [
            _ => throw expected,
            _ =>
            {
                calls++;
                return Task.FromResult(UnitResult<string>.Success());
            },
        ]);
        var canceled = Result<int, string>.Success(1).CheckAllAsync(
        [
            _ => throw cancellation,
            _ =>
            {
                calls++;
                return Task.FromResult(UnitResult<string>.Success());
            },
        ]);

        Assert.Same(
            expected,
            await Assert.ThrowsAsync<InvalidOperationException>(() => faulted));
        Assert.True(faulted.IsFaulted);
        Assert.False(faulted.IsCanceled);
        Assert.True(canceled.IsCanceled);
        Assert.False(canceled.IsFaulted);
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task CheckAllAsyncNullTasksAndUninitializedResultsFaultBeforeLaterRules()
    {
        var calls = 0;
        var nullTask = Result<int, string>.Success(1).CheckAllAsync(
        [
            _ => (Task<UnitResult<string>>)null!,
            _ =>
            {
                calls++;
                return Task.FromResult(UnitResult<string>.Success());
            },
        ]);
#pragma warning disable FS1001 // Intentional runtime misuse witness.
        var uninitialized = Result<int, string>.Success(1).CheckAllAsync(
        [
            _ => Task.FromResult(default(UnitResult<string>)),
            _ =>
            {
                calls++;
                return Task.FromResult(UnitResult<string>.Success());
            },
        ]);
#pragma warning restore FS1001

        await Assert.ThrowsAsync<NullReferenceException>(() => nullTask);
        await Assert.ThrowsAsync<InvalidOperationException>(() => uninitialized);
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task CheckAllAsyncSnapshotsTheRuleSequenceAndRejectsNullElements()
    {
        var calls = new List<string>();
        var rules = new List<Func<int, Task<UnitResult<string>>>>();
        rules.Add(_ =>
        {
            calls.Add("first");
            rules.Add(_ => Task.FromResult(UnitResult<string>.Failure("late")));
            return Task.FromResult(UnitResult<string>.Success());
        });
        rules.Add(_ =>
        {
            calls.Add("second");
            return Task.FromResult(UnitResult<string>.Success());
        });

        var validation = await Result<int, string>.Success(1).CheckAllAsync(rules);

        Assert.True(validation.IsValid);
        Assert.Equal(["first", "second"], calls);

        calls.Clear();
        var nullRule = new Func<int, Task<UnitResult<string>>>[]
        {
            _ =>
            {
                calls.Add("must-not-run");
                return Task.FromResult(UnitResult<string>.Success());
            },
            null!,
        };

        Assert.Throws<ArgumentException>(
            () => { _ = Result<int, string>.Success(1).CheckAllAsync(nullRule); });
        Assert.Empty(calls);
    }

    [Fact]
    public async Task CheckAllValueAsyncRunsSequentiallyAndConsumesEveryValueTaskOnce()
    {
        var order = CheckFixtures.ValidOrder;
        var log = new List<string>();
        var first = new CountingValueTaskSource<UnitResult<CheckError>>();

        var task = Result<CheckOrder, CheckError>.Success(order).CheckAllValueAsync(
        [
            _ =>
            {
                log.Add("first");
                return first.CreateValueTask();
            },
            _ =>
            {
                log.Add("second");
                return ValueTask.FromResult(
                    UnitResult<CheckError>.Failure(new CheckError("second", "failure")));
            },
            _ =>
            {
                log.Add("third");
                return ValueTask.FromResult(UnitResult<CheckError>.Success());
            },
        ]);

        Assert.False(task.IsCompleted);
        Assert.Equal(["first"], log);
        Assert.Equal(0, first.GetResultCount);

        first.SetResult(
            UnitResult<CheckError>.Failure(new CheckError("first", "failure")));

        var validation = await task;

        Assert.True(validation.TryGetErrors(out var errors));
        Assert.Equal(
            [new CheckError("first", "failure"), new CheckError("second", "failure")],
            errors);
        Assert.Equal(["first", "second", "third"], log);
        Assert.Equal(1, first.GetResultCount);
    }

    [Fact]
    public async Task CheckAllValueAsyncPreservesFaultsAndCancellationWithoutLaterRules()
    {
        var expected = new InvalidOperationException("fault");
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var calls = 0;

        var faulted = Result<int, string>.Success(1).CheckAllValueAsync(
        [
            _ => ValueTask.FromException<UnitResult<string>>(expected),
            _ =>
            {
                calls++;
                return ValueTask.FromResult(UnitResult<string>.Success());
            },
        ]).AsTask();
        var canceled = Result<int, string>.Success(1).CheckAllValueAsync(
        [
            _ => ValueTask.FromCanceled<UnitResult<string>>(cancellationSource.Token),
            _ =>
            {
                calls++;
                return ValueTask.FromResult(UnitResult<string>.Success());
            },
        ]).AsTask();

        Assert.Same(expected, await Assert.ThrowsAsync<InvalidOperationException>(() => faulted));
        Assert.True(faulted.IsFaulted);
        Assert.True(canceled.IsCanceled);
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task CheckAllValueAsyncForwardsTheExactTokenAndPreservesEmptyAndInvalidStates()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var tokens = new List<CancellationToken>();
        var order = CheckFixtures.ValidOrder;

        var validation = await Result<CheckOrder, CheckError>
            .Success(order)
            .CheckAllValueAsync(
                [
                    (_, token) =>
                    {
                        tokens.Add(token);
                        return ValueTask.FromResult(UnitResult<CheckError>.Success());
                    },
                    (_, token) =>
                    {
                        tokens.Add(token);
                        return ValueTask.FromResult(UnitResult<CheckError>.Success());
                    },
                ],
                cancellationSource.Token);

        Assert.True(validation.TryGetValue(out var value));
        Assert.Same(order, value);
        Assert.Equal(2, tokens.Count);
        Assert.All(tokens, token => Assert.Equal(cancellationSource.Token, token));

        var empty = await Result<CheckOrder, CheckError>.Success(order).CheckAllValueAsync(
            Array.Empty<Func<CheckOrder, ValueTask<UnitResult<CheckError>>>>());
        Assert.True(empty.TryGetValue(out var emptyValue));
        Assert.Same(order, emptyValue);

        var existing = new CheckError("existing", "already failed");
        var invalid = Validation<CheckOrder, CheckError>.Invalid(existing);
        var invalidResult = await invalid.CheckAllValueAsync(
            Array.Empty<Func<CheckOrder, ValueTask<UnitResult<CheckError>>>>());
        Assert.True(invalidResult.TryGetErrors(out var errors));
        Assert.Same(existing, Assert.Single(errors));
    }

    [Fact]
    public async Task CheckAllOnValidationAsyncKeepsExistingErrorsAndAppliesRulesToValidValues()
    {
        var calls = 0;
        var invalid = Validation<int, string>.Invalid("existing");
        var invalidResult = await invalid.CheckAllAsync(
        [
            _ =>
            {
                calls++;
                return Task.FromResult(UnitResult<string>.Success());
            },
        ]);

        Assert.True(invalidResult.TryGetErrors(out var errors));
        Assert.Equal(["existing"], errors);
        Assert.Equal(0, calls);

        var valid = Validation<int, string>.Valid(1);
        var validResult = await valid.CheckAllAsync(
        [
            value =>
            {
                Assert.Equal(1, value);
                return Task.FromResult(UnitResult<string>.Failure("first"));
            },
            _ => Task.FromResult(UnitResult<string>.Failure("second")),
        ]);

        Assert.True(validResult.TryGetErrors(out var validErrors));
        Assert.Equal(["first", "second"], validErrors);
    }
    [Fact]
    public async Task CheckAllAsyncHandlesFaultAndCancellationAfterAPendingSuccess()
    {
        var expected = new InvalidOperationException("second fault");
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var thirdCalls = 0;

        var first = new TaskCompletionSource<UnitResult<string>>();
        var second = new TaskCompletionSource<UnitResult<string>>();
        var faulted = Result<int, string>.Success(1).CheckAllAsync(
        [
            _ => first.Task,
            _ => second.Task,
            _ =>
            {
                thirdCalls++;
                return Task.FromResult(UnitResult<string>.Success());
            },
        ]);

        first.SetResult(UnitResult<string>.Success());
        Assert.False(faulted.IsCompleted);
        second.SetException(expected);

        Assert.Same(expected, await Assert.ThrowsAsync<InvalidOperationException>(() => faulted));
        Assert.Equal(0, thirdCalls);

        var firstForCancellation = new TaskCompletionSource<UnitResult<string>>();
        var secondForCancellation = new TaskCompletionSource<UnitResult<string>>();
        var canceled = Result<int, string>.Success(1).CheckAllAsync(
        [
            _ => firstForCancellation.Task,
            _ => secondForCancellation.Task,
            _ =>
            {
                thirdCalls++;
                return Task.FromResult(UnitResult<string>.Success());
            },
        ]);

        firstForCancellation.SetResult(UnitResult<string>.Success());
        secondForCancellation.SetCanceled(cancellationSource.Token);

        var cancellation = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => canceled);

        Assert.True(canceled.IsCanceled);
        Assert.Equal(cancellationSource.Token, cancellation.CancellationToken);
        Assert.Equal(0, thirdCalls);
    }

    [Fact]
    public async Task CheckAllAsyncContinuationPreservesTheOriginalCancellationException()
    {
        using var cancellationSource = new CancellationTokenSource();
        var cancellation = new OperationCanceledException("rule canceled", cancellationSource.Token);
        var pending = new TaskCompletionSource<UnitResult<string>>();
        var thirdCalls = 0;

        var task = Result<int, string>.Success(1).CheckAllAsync(
        [
            _ => pending.Task,
            _ => throw cancellation,
            _ =>
            {
                thirdCalls++;
                return Task.FromResult(UnitResult<string>.Success());
            },
        ]);
        var value = Result<int, string>.Success(1).CheckAllValueAsync(
        [
            _ => new ValueTask<UnitResult<string>>(pending.Task),
            _ => throw cancellation,
            _ =>
            {
                thirdCalls++;
                return ValueTask.FromResult(UnitResult<string>.Success());
            },
        ]).AsTask();

        pending.SetResult(UnitResult<string>.Success());

        Assert.Same(
            cancellation,
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task));
        Assert.Same(
            cancellation,
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => value));
        Assert.True(task.IsCanceled);
        Assert.True(value.IsCanceled);
        Assert.Equal(0, thirdCalls);
    }

    [Fact]
    public async Task CheckAllValueAsyncPendingFaultAndCancellationPreserveTheirStates()
    {
        var expected = new InvalidOperationException("pending fault");
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var faultSource = new TaskCompletionSource<UnitResult<string>>();
        var cancelSource = new TaskCompletionSource<UnitResult<string>>();
        var calls = 0;

        var faulted = Result<int, string>.Success(1).CheckAllValueAsync(
        [
            _ => new ValueTask<UnitResult<string>>(faultSource.Task),
            _ =>
            {
                calls++;
                return ValueTask.FromResult(UnitResult<string>.Success());
            },
        ]).AsTask();
        var canceled = Result<int, string>.Success(1).CheckAllValueAsync(
        [
            _ => new ValueTask<UnitResult<string>>(cancelSource.Task),
            _ =>
            {
                calls++;
                return ValueTask.FromResult(UnitResult<string>.Success());
            },
        ]).AsTask();

        faultSource.SetException(expected);
        cancelSource.SetCanceled(cancellationSource.Token);

        Assert.Same(expected, await Assert.ThrowsAsync<InvalidOperationException>(() => faulted));
        var cancellation = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => canceled);

        Assert.True(faulted.IsFaulted);
        Assert.True(canceled.IsCanceled);
        Assert.Equal(cancellationSource.Token, cancellation.CancellationToken);
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task CheckAllValueAsyncOnValidationAccumulatesAndSkipsInvalidInput()
    {
        var order = CheckFixtures.ValidOrder;
        var valid = Validation<CheckOrder, CheckError>.Valid(order);
        var accumulated = await valid.CheckAllValueAsync(
        [
            _ => ValueTask.FromResult(
                UnitResult<CheckError>.Failure(new CheckError("first", "failure"))),
            _ => ValueTask.FromResult(UnitResult<CheckError>.Success()),
            _ => ValueTask.FromResult(
                UnitResult<CheckError>.Failure(new CheckError("third", "failure"))),
        ]);

        Assert.True(accumulated.TryGetErrors(out var errors));
        Assert.Equal(["first", "third"], errors.Select(error => error.Code));

        var existing = new CheckError("existing", "already failed");
        var invalid = Validation<CheckOrder, CheckError>.Invalid(existing);
        var probe = new ProbeEnumerable<Func<CheckOrder, ValueTask<UnitResult<CheckError>>>>(
            [_ => throw new InvalidOperationException("must not run")]);
        var invalidResult = await invalid.CheckAllValueAsync(probe);

        Assert.True(invalidResult.TryGetErrors(out var invalidErrors));
        Assert.Same(existing, Assert.Single(invalidErrors));
        Assert.Equal(0, probe.EnumeratorCount);
    }

    [Fact]
    public void CheckAllRuleEnumerationFaultsBeforeInvokingCallbacks()
    {
        var calls = 0;

        Assert.Throws<InvalidOperationException>(() =>
        {
            _ = Result<int, string>.Success(1).CheckAll(ThrowingRules(() => calls++));
        });
        Assert.Throws<InvalidOperationException>(() =>
        {
            _ = Result<int, string>.Success(1).CheckAllAsync(ThrowingTaskRules(() => calls++));
        });
        Assert.Equal(0, calls);
    }

    private static IEnumerable<Func<int, UnitResult<string>>> ThrowingRules(
        Action onInvoked)
    {
        yield return _ =>
        {
            onInvoked();
            return UnitResult<string>.Success();
        };
        throw new InvalidOperationException("source failure");
    }

    private static IEnumerable<Func<int, Task<UnitResult<string>>>> ThrowingTaskRules(
        Action onInvoked)
    {
        yield return _ =>
        {
            onInvoked();
            return Task.FromResult(UnitResult<string>.Success());
        };
        throw new InvalidOperationException("source failure");
    }

    [Fact]
    public async Task CheckAllAsyncFailedInputDoesNotValidateNullRuleElements()
    {
        var checks = new ProbeEnumerable<Func<int, Task<UnitResult<string>>>>([null!]);
        var result = Result<int, string>.Failure("existing");

        var validation = await result.CheckAllAsync(checks);

        Assert.True(validation.TryGetErrors(out var errors));
        Assert.Equal(["existing"], errors);
        Assert.Equal(0, checks.EnumeratorCount);
    }

    [Fact]
    public void CheckAllValueAsyncRejectsNullRulesAndArgumentsSynchronously()
    {
        Assert.Throws<ArgumentNullException>(
            () => { _ = Result<int, string>.Success(1).CheckAllValueAsync(null!); });
        Assert.Throws<ArgumentNullException>(
            () => { _ = Validation<int, string>.Valid(1).CheckAllValueAsync(null!); });

        var nullResultRule = new Func<int, ValueTask<UnitResult<string>>>[] { null! };
        Assert.Throws<ArgumentException>(
            () => { _ = Result<int, string>.Success(1).CheckAllValueAsync(nullResultRule); });
        Assert.Throws<ArgumentException>(
            () => { _ = Validation<int, string>.Valid(1).CheckAllValueAsync(nullResultRule); });
    }

    [Fact]
    public void CheckAllValueAsyncRuleEnumerationFaultsBeforeInvokingCallbacks()
    {
        var calls = 0;

        Assert.Throws<InvalidOperationException>(() =>
        {
            _ = Result<int, string>.Success(1)
                .CheckAllValueAsync(ThrowingValueRules(() => calls++));
        });
        Assert.Throws<InvalidOperationException>(() =>
        {
            _ = Validation<int, string>.Valid(1)
                .CheckAllValueAsync(ThrowingValueRules(() => calls++));
        });
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task CheckAllValueAsyncFailedInputDoesNotValidateNullRuleElements()
    {
        var checks = new ProbeEnumerable<Func<int, ValueTask<UnitResult<string>>>>([null!]);
        var result = Result<int, string>.Failure("existing");

        var validation = await result.CheckAllValueAsync(checks);

        Assert.True(validation.TryGetErrors(out var errors));
        Assert.Equal(["existing"], errors);
        Assert.Equal(0, checks.EnumeratorCount);
    }

    [Fact]
    public void CheckAllValueAsyncRejectsUninitializedReceivers()
    {
#pragma warning disable FS1001 // Intentional uninitialized receiver witness.
        Result<int, string> uninitializedResult = default;
        Validation<int, string> uninitializedValidation = default;
#pragma warning restore FS1001
        var rules = Array.Empty<Func<int, ValueTask<UnitResult<string>>>>();

        Assert.Throws<InvalidOperationException>(
            () => { _ = uninitializedResult.CheckAllValueAsync(rules); });
        Assert.Throws<InvalidOperationException>(
            () => { _ = uninitializedValidation.CheckAllValueAsync(rules); });
    }

    private static IEnumerable<Func<int, ValueTask<UnitResult<string>>>> ThrowingValueRules(
        Action onInvoked)
    {
        yield return _ =>
        {
            onInvoked();
            return ValueTask.FromResult(UnitResult<string>.Success());
        };
        throw new InvalidOperationException("source failure");
    }

    [Fact]
    public async Task CheckAllValueAsyncConsumesEachValueTaskOnceAcrossTheRuleSet()
    {
        var first = new CountingValueTaskSource<UnitResult<string>>();
        var second = new CountingValueTaskSource<UnitResult<string>>();

        var task = Result<int, string>.Success(1).CheckAllValueAsync(
        [
            _ => first.CreateValueTask(),
            _ => second.CreateValueTask(),
        ]);

        Assert.Equal(0, first.GetResultCount);
        Assert.Equal(0, second.GetResultCount);

        first.SetResult(UnitResult<string>.Success());
        second.SetResult(UnitResult<string>.Failure("second"));

        var validation = await task;

        Assert.True(validation.TryGetErrors(out var errors));
        Assert.Equal(["second"], errors);
        Assert.Equal(1, first.GetResultCount);
        Assert.Equal(1, second.GetResultCount);
    }

    [Fact]
    public async Task CheckAllAsyncEnumeratesTheRuleSourceOnceAndInvokesEachSnapshotRuleOnce()
    {
        var calls = 0;
        var checks = new ProbeEnumerable<Func<int, Task<UnitResult<string>>>>(
        [
            _ =>
            {
                calls++;
                return Task.FromResult(UnitResult<string>.Success());
            },
            _ =>
            {
                calls++;
                return Task.FromResult(UnitResult<string>.Failure("second"));
            },
        ]);

        var validation = await Result<int, string>.Success(1).CheckAllAsync(checks);

        Assert.True(validation.TryGetErrors(out var errors));
        Assert.Equal(["second"], errors);
        Assert.Equal(1, checks.EnumeratorCount);
        Assert.Equal(2, checks.ItemsYielded);
        Assert.Equal(2, calls);
    }

}
