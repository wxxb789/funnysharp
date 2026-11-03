#pragma warning disable xUnit1051

using FunnySharp;

namespace FunnySharp.Tests;

public sealed class BatchCancellationTests
{
    [Fact]
    public async Task AlreadyCanceledTokenPreventsSourceInvocationAndPreservesTheCallerToken()
    {
        var values = new RecordingBatchSource<string, int, string>(
            "values",
            "tenant-a",
            StringComparer.Ordinal,
            static (keys, _, _) => new(Result<IReadOnlyList<BatchSourceEntry<string, int, string>>, string>.Success(
            [
                BatchSourceEntry<string, int, string>.Found("a", 1),
            ])));

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await using var scope = BatchScope.Create();
        var thrown = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await scope.ExecuteAsync(
                BatchRequirement.Read(values.Source, "a"),
                cancellation.Token));

        Assert.Equal(cancellation.Token, thrown.CancellationToken);
        Assert.Equal(0, values.InvocationCount);
    }

    [Fact]
    public async Task CancellationDuringASourceCallWaitsForTheSourceAndThenReportsCallerCancellation()
    {
        var started = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var completed = false;
        var values = new RecordingBatchSource<string, int, string>(
            "values",
            "tenant-a",
            StringComparer.Ordinal,
            async (keys, _, _) =>
            {
                started.TrySetResult(true);
                await release.Task;
                completed = true;
                return Result<IReadOnlyList<BatchSourceEntry<string, int, string>>, string>.Success(
                [
                    BatchSourceEntry<string, int, string>.Found("a", 1),
                ]);
            });

        using var cancellation = new CancellationTokenSource();
        await using var scope = BatchScope.Create();
        var execution = scope.ExecuteAsync(
            BatchRequirement.Read(values.Source, "a"),
            cancellation.Token).AsTask();

        await started.Task;
        cancellation.Cancel();

        var raced = await Task.WhenAny(execution, Task.Delay(50));
        Assert.NotSame(execution, raced);
        Assert.False(completed);

        release.SetResult(true);
        var thrown = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await execution);
        Assert.Equal(cancellation.Token, thrown.CancellationToken);
        Assert.True(completed);
    }

    [Fact]
    public async Task QueuedBatchesAreNotDispatchedAfterCancellation()
    {
        var started = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var values = new RecordingBatchSource<string, int, string>(
            "values",
            "tenant-a",
            StringComparer.Ordinal,
            async (keys, invocation, _) =>
            {
                if (invocation == 1)
                {
                    started.TrySetResult(true);
                    await release.Task;
                }

                return Result<IReadOnlyList<BatchSourceEntry<string, int, string>>, string>.Success(
                    keys.Select(key => BatchSourceEntry<string, int, string>.Found(key, invocation)).ToArray());
            });

        var requirement = BatchRequirement.All(
        [
            BatchRequirement.Read(values.Source, "a"),
            BatchRequirement.Read(values.Source, "b"),
        ]);

        using var cancellation = new CancellationTokenSource();
        await using var scope = BatchScope.Create(new BatchScopeOptions { MaxBatchSize = 1 });
        var execution = scope.ExecuteAsync(requirement, cancellation.Token).AsTask();

        await started.Task;
        cancellation.Cancel();
        release.SetResult(true);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await execution);
        Assert.Equal(1, values.InvocationCount);
    }

    [Fact]
    public async Task SourceThatObservesTheForwardedTokenPropagatesCancellation()
    {
        var started = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var observedToken = default(CancellationToken);
        var values = new RecordingBatchSource<string, int, string>(
            "values",
            "tenant-a",
            StringComparer.Ordinal,
            async (keys, _, cancellationToken) =>
            {
                observedToken = cancellationToken;
                started.TrySetResult(true);
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                return Result<IReadOnlyList<BatchSourceEntry<string, int, string>>, string>.Success([]);
            });

        using var cancellation = new CancellationTokenSource();
        await using var scope = BatchScope.Create();
        var execution = scope.ExecuteAsync(
            BatchRequirement.Read(values.Source, "a"),
            cancellation.Token).AsTask();

        await started.Task;
        cancellation.Cancel();

        var thrown = await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await execution);
        Assert.Equal(cancellation.Token, thrown.CancellationToken);
        Assert.True(observedToken.CanBeCanceled);
    }

    [Fact]
    public async Task DisposalCancelsAnActiveExecutionWaitsForStartedWorkAndClearsTheScope()
    {
        var started = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var completed = false;
        var values = new RecordingBatchSource<string, int, string>(
            "values",
            "tenant-a",
            StringComparer.Ordinal,
            async (keys, _, _) =>
            {
                started.TrySetResult(true);
                await release.Task;
                completed = true;
                return Result<IReadOnlyList<BatchSourceEntry<string, int, string>>, string>.Success(
                [
                    BatchSourceEntry<string, int, string>.Found("a", 1),
                ]);
            });

        var scope = BatchScope.Create();
        var execution = scope.ExecuteAsync(BatchRequirement.Read(values.Source, "a")).AsTask();
        await started.Task;

        var disposal = scope.DisposeAsync().AsTask();
        var raced = await Task.WhenAny(disposal, Task.Delay(50));
        Assert.NotSame(disposal, raced);
        Assert.False(completed);

        release.SetResult(true);
        await disposal;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await execution);
        Assert.True(completed);
        Assert.Throws<ObjectDisposedException>(() => scope.ClearCache());
    }

    [Fact]
    public async Task DisposalIsIdempotent()
    {
        var scope = BatchScope.Create();

        await scope.DisposeAsync();
        await scope.DisposeAsync();
    }
}
