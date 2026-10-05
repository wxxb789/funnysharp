namespace FunnySharp.Tests;

public sealed class StableParallelDefaultContractTests
{
    [Fact]
    public async Task DefaultResultStopsAdmissionCancelsAndDrainsBeforePublishingTheFault()
    {
        var source = new Source(holdDisposal: false);
        var first = new TaskCompletionSource<Result<int, string>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var sibling = new TaskCompletionSource<Result<int, string>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var canceled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        var operation = source.TraverseParallelValueAsync(2, (value, token) =>
        {
            Assert.Equal(source.Token, token);
            calls++;
            if (calls == 2) started.TrySetResult();
            return new ValueTask<Result<int, string>>(value == 0 ? first.Task : sibling.Task);
        }, TestContext.Current.CancellationToken).AsTask();
        try
        {
            await started.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            using var registration = source.Token.Register(() => canceled.TrySetResult());
#pragma warning disable FS1001 // Deliberately release an uninitialized Result into its typed parallel core.
            first.SetResult(default);
#pragma warning restore FS1001
            await canceled.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            Assert.False(operation.IsCompleted);
            Assert.Equal(2, source.ItemsYielded);
            Assert.Equal(2, calls);
            Assert.Equal(0, source.Disposals);
            sibling.SetResult(Result<int, string>.Success(1));
            _ = await Assert.ThrowsAsync<InvalidOperationException>(() => operation.WaitAsync(
                TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
            Assert.True(operation.IsFaulted);
            Assert.False(operation.IsCompletedSuccessfully);
            Assert.Equal(2, source.ItemsYielded);
            Assert.Equal(1, source.Disposals);
        }
        finally
        {
            first.TrySetResult(Result<int, string>.Success(0));
            sibling.TrySetResult(Result<int, string>.Success(1));
            source.ReleaseDisposal();
        }
    }

    [Fact]
    public async Task DefaultValidationIsInspectedOnlyAfterFullAdmissionDrainAndDisposal()
    {
        var source = new Source(holdDisposal: true);
        var first = new TaskCompletionSource<Validation<int, string>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var sibling = new TaskCompletionSource<Validation<int, string>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thirdStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        var operation = source.TraverseParallelValueAsync(2, (value, token) =>
        {
            Assert.Equal(source.Token, token);
            calls++;
            if (calls == 2) started.TrySetResult();
            if (value == 2)
            {
                thirdStarted.TrySetResult();
                return ValueTask.FromResult(Validation<int, string>.Valid(2));
            }
            return new ValueTask<Validation<int, string>>(value == 0 ? first.Task : sibling.Task);
        }, TestContext.Current.CancellationToken).AsTask();
        try
        {
            await started.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
#pragma warning disable FS1001 // Deliberate default Validation is not inspected by the nonterminal coordinator.
            first.SetResult(default);
#pragma warning restore FS1001
            await thirdStarted.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            Assert.False(source.Token.IsCancellationRequested);
            Assert.False(operation.IsCompleted);
            Assert.Equal(3, source.ItemsYielded);
            Assert.Equal(3, calls);
            sibling.SetResult(Validation<int, string>.Valid(1));
            await source.DisposalStarted.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            Assert.False(operation.IsCompleted);
            Assert.False(source.Token.IsCancellationRequested);
            Assert.Equal(1, source.Disposals);
            source.ReleaseDisposal();
            _ = await Assert.ThrowsAsync<InvalidOperationException>(() => operation.WaitAsync(
                TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
            Assert.True(operation.IsFaulted);
            Assert.False(operation.IsCompletedSuccessfully);
            Assert.Equal(3, source.ItemsYielded);
            Assert.Equal(1, source.Disposals);
        }
        finally
        {
            first.TrySetResult(Validation<int, string>.Valid(0));
            sibling.TrySetResult(Validation<int, string>.Valid(1));
            source.ReleaseDisposal();
        }
    }

    private sealed class Source(bool holdDisposal) : IAsyncEnumerable<int>, IAsyncEnumerator<int>
    {
        private readonly TaskCompletionSource disposed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int index = -1;
        public CancellationToken Token { get; private set; }
        public int ItemsYielded { get; private set; }
        public int Disposals { get; private set; }
        public TaskCompletionSource DisposalStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int Current => index;
        public IAsyncEnumerator<int> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            Token = cancellationToken;
            return this;
        }
        public ValueTask<bool> MoveNextAsync()
        {
            index++;
            if (index >= 3) return ValueTask.FromResult(false);
            ItemsYielded++;
            return ValueTask.FromResult(true);
        }
        public ValueTask DisposeAsync()
        {
            Disposals++;
            DisposalStarted.TrySetResult();
            return holdDisposal ? new ValueTask(disposed.Task) : ValueTask.CompletedTask;
        }
        public void ReleaseDisposal() => disposed.TrySetResult();
    }
}
