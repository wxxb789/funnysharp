// The cancellation scenarios pass explicit pre-canceled and mid-run tokens; xUnit1051's linked
// test-token guidance does not apply to those assertions.
#pragma warning disable xUnit1051

using FunnySharp;
using FunnySharp.OperationPlans;

using static FunnySharp.OperationPlans.Tests.PlanTestFactory;

namespace FunnySharp.OperationPlans.Tests;

public sealed class PlanExecutionTests
{
    [Fact]
    public async Task SuccessCommitsAndReturnsTypedState()
    {
        var transaction = new FakeTransaction();
        var plan = CreatePlan(Step("a", Succeed), Step("b", Succeed));

        var result = await plan.RunAsync(null, 0, transaction);

        Assert.True(result.TryGetValue(out var success));
        Assert.Equal(2, success.State);
        Assert.Equal(new[] { "a", "b" }, success.Executed);
        Assert.Empty(success.Skipped);
        Assert.Equal(1, transaction.CommitCount);
        Assert.Equal(0, transaction.RollbackCount);
    }

    [Fact]
    public async Task DomainFailureNamesOperationAndRollsBack()
    {
        var transaction = new FakeTransaction();
        var plan = CreatePlan(Step("a", Succeed), Step("b", Fail("domain")));

        var result = await plan.RunAsync(null, 0, transaction);

        Assert.True(result.TryGetError(out var failure));
        Assert.Equal(RunFailureKind.OperationFailed, failure.Kind);
        Assert.Equal("b", failure.Operation);
        Assert.Equal("domain", failure.Error);
        Assert.Equal(new[] { "a" }, failure.Completed);
        Assert.Equal(1, failure.State);
        Assert.Equal(0, transaction.CommitCount);
        Assert.Equal(1, transaction.RollbackCount);
    }

    [Fact]
    public async Task CallbackExceptionIsFaultedAndRollsBack()
    {
        var transaction = new FakeTransaction();
        var plan = CreatePlan(
            Step("a", Succeed),
            Step(
                "b",
                (_, _, _) => throw new InvalidOperationException("fault")));

        var result = await plan.RunAsync(null, 0, transaction);

        Assert.True(result.TryGetError(out var failure));
        Assert.Equal(RunFailureKind.OperationFaulted, failure.Kind);
        Assert.Equal("b", failure.Operation);
        Assert.IsType<InvalidOperationException>(failure.Exception);
        Assert.Equal(new[] { "a" }, failure.Completed);
        Assert.Equal(1, failure.State);
        Assert.Equal(1, transaction.RollbackCount);
    }

    [Fact]
    public async Task NullTaskIsFaulted()
    {
        var transaction = new FakeTransaction();
        var plan = CreatePlan(
            Step("a", Succeed),
            Step("b", (_, _, _) => null!));

        var result = await plan.RunAsync(null, 0, transaction);

        Assert.True(result.TryGetError(out var failure));
        Assert.Equal(RunFailureKind.OperationFaulted, failure.Kind);
        Assert.Equal("b", failure.Operation);
        Assert.IsType<InvalidOperationException>(failure.Exception);
        Assert.Equal(1, transaction.RollbackCount);
    }

    [Fact]
    public async Task ConditionExceptionIsFaultedBeforeTheStepRuns()
    {
        var transaction = new FakeTransaction();
        var executions = 0;
        var plan = CreatePlan(
            Step("a", Succeed),
            Step(
                "b",
                (_, state, _) =>
                {
                    executions++;
                    return Task.FromResult(Result<int, string>.Success(state));
                },
                _ => throw new InvalidOperationException("condition")));

        var result = await plan.RunAsync(null, 0, transaction);

        Assert.True(result.TryGetError(out var failure));
        Assert.Equal(RunFailureKind.OperationFaulted, failure.Kind);
        Assert.Equal("b", failure.Operation);
        Assert.Equal(0, executions);
        Assert.Equal(1, transaction.RollbackCount);
    }

    [Fact]
    public async Task PreCanceledTokenDoesNotStartAnOperation()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        var transaction = new FakeTransaction();
        var executions = 0;
        var plan = CreatePlan(
            Step(
                "a",
                (_, state, _) =>
                {
                    executions++;
                    return Task.FromResult(Result<int, string>.Success(state));
                }));

        var result = await plan.RunAsync(null, 0, transaction, cancellation.Token);

        Assert.True(result.TryGetError(out var failure));
        Assert.Equal(RunFailureKind.Canceled, failure.Kind);
        Assert.Equal("a", failure.Operation);
        Assert.Empty(failure.Completed);
        Assert.Equal(0, executions);
        Assert.Null(failure.Exception);
        Assert.Equal(1, transaction.RollbackCount);
    }

    [Fact]
    public async Task CancellationBetweenStepsNamesTheNextOperation()
    {
        using var cancellation = new CancellationTokenSource();
        var transaction = new FakeTransaction();
        var plan = CreatePlan(
            Step(
                "a",
                (_, state, _) =>
                {
                    cancellation.Cancel();
                    return Task.FromResult(Result<int, string>.Success(state + 1));
                }),
            Step("b", Succeed));

        var result = await plan.RunAsync(null, 0, transaction, cancellation.Token);

        Assert.True(result.TryGetError(out var failure));
        Assert.Equal(RunFailureKind.Canceled, failure.Kind);
        Assert.Equal("b", failure.Operation);
        Assert.Equal(new[] { "a" }, failure.Completed);
        Assert.Equal(1, failure.State);
        Assert.Null(failure.Exception);
        Assert.Equal(1, transaction.RollbackCount);
    }

    [Fact]
    public async Task CancellationInsideAStepNamesTheInFlightOperation()
    {
        var transaction = new FakeTransaction();
        var plan = CreatePlan(
            Step("a", Succeed),
            Step(
                "b",
                (_, _, cancellationToken) => throw new OperationCanceledException(cancellationToken)));

        var result = await plan.RunAsync(null, 0, transaction);

        Assert.True(result.TryGetError(out var failure));
        Assert.Equal(RunFailureKind.Canceled, failure.Kind);
        Assert.Equal("b", failure.Operation);
        Assert.Equal(new[] { "a" }, failure.Completed);
        Assert.IsAssignableFrom<OperationCanceledException>(failure.Exception);
        Assert.Equal(1, transaction.RollbackCount);
    }

    [Fact]
    public async Task CancellationAfterTheLastStepPreventsCommit()
    {
        using var cancellation = new CancellationTokenSource();
        var transaction = new FakeTransaction();
        var plan = CreatePlan(
            Step(
                "a",
                (_, state, _) =>
                {
                    cancellation.Cancel();
                    return Task.FromResult(Result<int, string>.Success(state + 1));
                }));

        var result = await plan.RunAsync(null, 0, transaction, cancellation.Token);

        Assert.True(result.TryGetError(out var failure));
        Assert.Equal(RunFailureKind.Canceled, failure.Kind);
        Assert.Null(failure.Operation);
        Assert.Equal(new[] { "a" }, failure.Completed);
        Assert.Equal(1, failure.State);
        Assert.Equal(0, transaction.CommitCount);
        Assert.Equal(1, transaction.RollbackCount);
    }

    [Fact]
    public async Task ConditionalStepCanBeSkippedOrExecuted()
    {
        var skippedTransaction = new FakeTransaction();
        var skippedPlan = CreatePlan(
            Step("a", Succeed),
            Step("b", Succeed, static state => state > 5));

        var skipped = await skippedPlan.RunAsync(null, 0, skippedTransaction);

        Assert.True(skipped.TryGetValue(out var skippedSuccess));
        Assert.Equal(new[] { "a" }, skippedSuccess.Executed);
        Assert.Equal(new[] { "b" }, skippedSuccess.Skipped);
        Assert.Equal(1, skippedSuccess.State);

        var executedTransaction = new FakeTransaction();
        var executedPlan = CreatePlan(
            Step("a", Succeed),
            Step("b", Succeed, static state => state > 0));

        var executed = await executedPlan.RunAsync(null, 0, executedTransaction);

        Assert.True(executed.TryGetValue(out var executedSuccess));
        Assert.Equal(new[] { "a", "b" }, executedSuccess.Executed);
        Assert.Empty(executedSuccess.Skipped);
        Assert.Equal(2, executedSuccess.State);
    }

    [Fact]
    public async Task CommitFailureIsDistinctAndRolledBack()
    {
        var transaction = new FakeTransaction
        {
            CommitException = new InvalidOperationException("commit"),
        };
        var plan = CreatePlan(Step("a", Succeed), Step("b", Succeed));

        var result = await plan.RunAsync(null, 0, transaction);

        Assert.True(result.TryGetError(out var failure));
        Assert.Equal(RunFailureKind.CommitFailed, failure.Kind);
        Assert.Null(failure.Operation);
        Assert.Equal(new[] { "a", "b" }, failure.Completed);
        Assert.Equal(2, failure.State);
        Assert.IsType<InvalidOperationException>(failure.Exception);
        Assert.Equal(1, transaction.CommitCount);
        Assert.Equal(1, transaction.RollbackCount);
    }

    [Fact]
    public async Task RollbackFailurePreservesTheOriginalFailure()
    {
        var transaction = new FakeTransaction
        {
            RollbackException = new InvalidOperationException("rollback"),
        };
        var plan = CreatePlan(Step("a", Succeed), Step("b", Fail("domain")));

        var result = await plan.RunAsync(null, 0, transaction);

        Assert.True(result.TryGetError(out var failure));
        Assert.Equal(RunFailureKind.RollbackFailed, failure.Kind);
        Assert.Equal(RunFailureKind.OperationFailed, failure.OriginalKind);
        Assert.Equal("b", failure.Operation);
        Assert.Equal("domain", failure.Error);
        Assert.Null(failure.Exception);
        Assert.IsType<InvalidOperationException>(failure.RollbackException);
        Assert.Equal(new[] { "a" }, failure.Completed);
        Assert.Equal(1, failure.State);
        Assert.Equal(1, transaction.RollbackCount);
    }

    [Fact]
    public async Task RollbackFailureRetainsTheCommitFailureKind()
    {
        var commitException = new InvalidOperationException("commit");
        var rollbackException = new InvalidOperationException("rollback");
        var transaction = new FakeTransaction
        {
            CommitException = commitException,
            RollbackException = rollbackException,
        };
        var plan = CreatePlan(Step("a", Succeed));

        var result = await plan.RunAsync(null, 0, transaction);

        Assert.True(result.TryGetError(out var failure));
        Assert.Equal(RunFailureKind.RollbackFailed, failure.Kind);
        Assert.Equal(RunFailureKind.CommitFailed, failure.OriginalKind);
        Assert.Same(commitException, failure.Exception);
        Assert.Same(rollbackException, failure.RollbackException);
        Assert.Equal(new[] { "a" }, failure.Completed);
        Assert.Equal(1, failure.State);
    }

    [Fact]
    public async Task NullTransactionIsRejected()
    {
        var plan = CreatePlan(Step("a", Succeed));

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => plan.RunAsync(null, 0, null!));
    }

    private sealed class FakeTransaction : IPlanTransaction
    {
        internal int CommitCount { get; private set; }

        internal int RollbackCount { get; private set; }

        internal Exception? CommitException { get; init; }

        internal Exception? RollbackException { get; init; }

        public ValueTask CommitAsync()
        {
            CommitCount++;

            if (CommitException is not null)
            {
                throw CommitException;
            }

            return ValueTask.CompletedTask;
        }

        public ValueTask RollbackAsync()
        {
            RollbackCount++;

            if (RollbackException is not null)
            {
                throw RollbackException;
            }

            return ValueTask.CompletedTask;
        }
    }
}

#pragma warning restore xUnit1051
