using System.Collections;
using System.Threading.Tasks.Sources;

namespace FunnySharp.Tests;

public sealed class StableCollectionCardinalityMatrixTests
{
    [Theory]
    [InlineData(3, 0)]
    [InlineData(3, 1)]
    [InlineData(3, 2)]
    [InlineData(3, 3)]
    [InlineData(3, 4)]
    [InlineData(3, 5)]
    [InlineData(4, 0)]
    [InlineData(4, 1)]
    [InlineData(4, 2)]
    [InlineData(4, 3)]
    [InlineData(4, 4)]
    [InlineData(4, 5)]
    [InlineData(5, 0)]
    [InlineData(5, 1)]
    [InlineData(5, 2)]
    [InlineData(5, 3)]
    [InlineData(5, 4)]
    [InlineData(5, 5)]
    [InlineData(6, 0)]
    [InlineData(6, 1)]
    [InlineData(6, 2)]
    [InlineData(6, 3)]
    [InlineData(6, 4)]
    [InlineData(6, 5)]
    [InlineData(7, 0)]
    [InlineData(7, 1)]
    [InlineData(7, 2)]
    [InlineData(7, 3)]
    [InlineData(7, 4)]
    [InlineData(7, 5)]
    [InlineData(8, 0)]
    [InlineData(8, 1)]
    [InlineData(8, 2)]
    [InlineData(8, 3)]
    [InlineData(8, 4)]
    [InlineData(8, 5)]
    [InlineData(9, 0)]
    [InlineData(9, 1)]
    [InlineData(9, 2)]
    [InlineData(9, 3)]
    [InlineData(9, 4)]
    [InlineData(9, 5)]
    [InlineData(10, 0)]
    [InlineData(10, 1)]
    [InlineData(10, 2)]
    [InlineData(10, 3)]
    [InlineData(10, 4)]
    [InlineData(10, 5)]
    [InlineData(11, 0)]
    [InlineData(11, 1)]
    [InlineData(11, 2)]
    [InlineData(11, 3)]
    [InlineData(11, 4)]
    [InlineData(11, 5)]
    public async Task AsyncSourceAndDisposalFaultsPreserveTheWinningIdentityAndNaturalTaskStatus(int identity, int scenario)
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Exception primary = scenario is 1 or 5
            ? new OperationCanceledException("source", cancellation.Token)
            : new InvalidOperationException("source");
        Exception disposal = scenario == 3
            ? new OperationCanceledException("dispose", cancellation.Token)
            : new InvalidOperationException("dispose");
        var source = new CollectionMatrixProbe.AsyncSource<int>([2, 1])
        {
            MoveFault = scenario is 0 or 1 or 4 or 5 ? primary : null,
            DisposeFault = scenario is 2 or 3 or 4 ? disposal : null,
            CanceledProducer = scenario == 5,
        };
        var operation = Start(identity, source, cancellation.Token);
        var expected = scenario is 2 or 3 or 4 ? disposal : primary;
        var actual = await Record.ExceptionAsync(async () => await operation);
        Assert.Same(expected, actual);
        Assert.Equal(expected is OperationCanceledException, operation.IsCanceled);
        Assert.Equal(expected is not OperationCanceledException, operation.IsFaulted);
        Assert.Equal(cancellation.Token, source.Token);
        Assert.Equal(1, source.Acquisitions);
        Assert.Equal(1, source.Disposals);
        if (source.MoveFault is not null)
        {
            Assert.Equal(1, source.Moves);
            Assert.Equal(0, source.Reads);
            Assert.NotNull(source.FaultTask);
            Assert.Equal(scenario == 5, source.FaultTask.IsCanceled);
            Assert.Equal(scenario != 5, source.FaultTask.IsFaulted);
        }
        else
        {
            Assert.Equal(identity is 4 or 5 ? 1 : identity == 11 ? 2 : 3, source.Moves);
            Assert.Equal(identity is 4 or 5 or 11 ? 1 : 2, source.Reads);
        }
    }

    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(11)]
    public async Task PendingMoveAndDisposalAreSubscribedBeforeCompletionAndConsumedExactlyOnce(int identity)
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var move = new CollectionMatrixProbe.Gate<bool>();
        var dispose = new CollectionMatrixProbe.Gate<bool>();
        var source = new CollectionMatrixProbe.AsyncSource<int>([2, 1]) { MoveGate = move, DisposeGate = dispose };
        var operation = Start(identity, source, cancellation.Token);
        await CollectionMatrixProbe.Signal(move.Subscribed.Task);
        Assert.False(operation.IsCompleted);
        Assert.Equal(0, source.Reads);
        Assert.Equal(0, move.Consumptions);
        move.Complete(true);
        await CollectionMatrixProbe.Signal(dispose.Subscribed.Task);
        Assert.False(operation.IsCompleted);
        dispose.Complete(true);
        await operation.WaitAsync(TimeSpan.FromSeconds(15), TestContext.Current.CancellationToken);
        Assert.True(operation.IsCompletedSuccessfully);
        Assert.Equal(1, move.Consumptions);
        Assert.Equal(1, dispose.Consumptions);
        Assert.Equal(1, source.Acquisitions);
        Assert.Equal(1, source.Disposals);
        Assert.Equal(cancellation.Token, source.Token);
        Assert.Equal(identity is 4 or 5 ? 1 : identity == 11 ? 2 : 3, source.Moves);
        Assert.Equal(identity is 4 or 5 or 11 ? 1 : 2, source.Reads);
    }

    [Fact]
    public async Task NegativeIndexDoesNotAcquireOrCancelAndSingleDoesNotReadTheSecondCurrent()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var negative = new CollectionMatrixProbe.AsyncSource<int>([1]) { AcquisitionFault = new InvalidOperationException() };
        var operation = negative.ElementAtOrNoneAsync(-1, cancellation.Token);
        Assert.True(operation.IsCompletedSuccessfully);
        Assert.True((await operation).IsNone);
        Assert.Equal(0, negative.Acquisitions);
        var multiple = new CollectionMatrixProbe.AsyncSource<string?>([null, "second"]) { CurrentFaultIndex = 1 };
        Assert.True((await multiple.SingleOrNoneAsync(cancellation.Token)).IsNone);
        Assert.Equal(2, multiple.Moves);
        Assert.Equal(1, multiple.Reads);
        Assert.Equal(1, multiple.Disposals);
    }

    [Theory]
    [InlineData(7)]
    [InlineData(9)]
    public void ExplicitComparerGuardChecksSourceBeforeComparerWithoutAcquiring(int identity)
    {
        var source = new CollectionMatrixProbe.AsyncSource<int>([1]);
        var missingSource = Assert.Throws<ArgumentNullException>(() => { _ = StartExtreme(identity, null!, null!, CancellationToken.None); });
        Assert.Equal("source", missingSource.ParamName);
        var missingComparer = Assert.Throws<ArgumentNullException>(() => { _ = StartExtreme(identity, source, null!, CancellationToken.None); });
        Assert.Equal("comparer", missingComparer.ParamName);
        Assert.Equal(0, source.Acquisitions);
    }

    [Theory]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    public async Task NullableValueItemsAreSkippedBeforeComparingAndAllNullMeansNone(int identity)
    {
        var comparisons = 0;
        var comparer = Comparer<int?>.Create((left, right) =>
        {
            Assert.True(left.HasValue);
            Assert.True(right.HasValue);
            comparisons++;
            return left.Value.CompareTo(right.Value);
        });
        var source = new CollectionMatrixProbe.AsyncSource<int?>([null, 3, null, 1]);
        var result = await Extreme(identity, source, comparer);
        Assert.True(result.TryGetValue(out var value));
        Assert.Equal(identity is 7 or 8 ? 3 : 1, value);
        Assert.Equal(identity is 7 or 9 ? 1 : 0, comparisons);
        Assert.Equal(5, source.Moves);
        Assert.Equal(4, source.Reads);
        Assert.Equal(1, source.Disposals);
        Assert.True((await Extreme(identity, new CollectionMatrixProbe.AsyncSource<int?>([null, null]), comparer)).IsNone);
    }

    [Theory]
    [InlineData(7, 0)]
    [InlineData(7, 1)]
    [InlineData(7, 2)]
    [InlineData(9, 0)]
    [InlineData(9, 1)]
    [InlineData(9, 2)]
    public async Task ExplicitComparerFaultsKeepTheirIdentityUnlessDisposalReplacesThem(int identity, int scenario)
    {
        var failure = scenario == 1 ? (Exception)new OperationCanceledException("compare") : new InvalidOperationException("compare");
        var disposal = new InvalidOperationException("dispose");
        var source = new CollectionMatrixProbe.AsyncSource<int>([2, 1]) { DisposeFault = scenario == 2 ? disposal : null };
        var calls = 0;
        var comparer = Comparer<int>.Create((_, _) => { calls++; throw failure; });
        var operation = StartExtreme(identity, source, comparer, TestContext.Current.CancellationToken);
        Assert.Same(scenario == 2 ? disposal : failure, await Record.ExceptionAsync(async () => await operation));
        Assert.Equal(scenario == 1, operation.IsCanceled);
        Assert.Equal(1, calls);
        Assert.Equal(2, source.Moves);
        Assert.Equal(2, source.Reads);
        Assert.Equal(1, source.Disposals);
    }

    [Theory]
    [InlineData(8)]
    [InlineData(10)]
    public async Task DefaultComparerRejectsUnorderableValuesAfterOneItemAndDisposes(int identity)
    {
        var source = new CollectionMatrixProbe.AsyncSource<object>([new object(), new object()]);
        var operation = identity == 8 ? source.MaxOrNoneAsync(TestContext.Current.CancellationToken).AsTask() : source.MinOrNoneAsync(TestContext.Current.CancellationToken).AsTask();
        await Assert.ThrowsAsync<ArgumentException>(async () => { _ = await operation; });
        Assert.True(operation.IsFaulted);
        Assert.Equal(2, source.Reads);
        Assert.Equal(1, source.Disposals);
    }

    private static Task Start(int identity, IAsyncEnumerable<int> source, CancellationToken token) => identity switch
    {
        3 => source.ToNonEmptyOrNoneAsync(token).AsTask(),
        4 => source.ElementAtOrNoneAsync(0, token).AsTask(),
        5 => source.FirstOrNoneAsync(token).AsTask(),
        6 => source.LastOrNoneAsync(token).AsTask(),
        7 => source.MaxOrNoneAsync(Comparer<int>.Default, token).AsTask(),
        8 => source.MaxOrNoneAsync(token).AsTask(),
        9 => source.MinOrNoneAsync(Comparer<int>.Default, token).AsTask(),
        10 => source.MinOrNoneAsync(token).AsTask(),
        11 => source.SingleOrNoneAsync(token).AsTask(),
        _ => throw new ArgumentOutOfRangeException(nameof(identity)),
    };

    private static Task StartExtreme(int identity, IAsyncEnumerable<int> source, IComparer<int> comparer, CancellationToken token) =>
        identity == 7 ? source.MaxOrNoneAsync(comparer, token).AsTask() : source.MinOrNoneAsync(comparer, token).AsTask();

    private static ValueTask<Option<int?>> Extreme(int identity, IAsyncEnumerable<int?> source, IComparer<int?> comparer) => identity switch
    {
        7 => source.MaxOrNoneAsync(comparer, TestContext.Current.CancellationToken),
        8 => source.MaxOrNoneAsync(TestContext.Current.CancellationToken),
        9 => source.MinOrNoneAsync(comparer, TestContext.Current.CancellationToken),
        10 => source.MinOrNoneAsync(TestContext.Current.CancellationToken),
        _ => throw new ArgumentOutOfRangeException(nameof(identity)),
    };
}

internal static class CollectionMatrixProbe
{
    internal static Task Signal(Task task) => task.WaitAsync(TimeSpan.FromSeconds(15), TestContext.Current.CancellationToken);

    internal sealed class Gate<T> : IValueTaskSource<T>
    {
        private ManualResetValueTaskSourceCore<T> core = new() { RunContinuationsAsynchronously = true };
        internal TaskCompletionSource Subscribed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal int Consumptions { get; private set; }
        internal ValueTask<T> Value => new(this, core.Version);
        internal void Complete(T value) => core.SetResult(value);
        public T GetResult(short token)
        {
            Assert.Equal(0, Consumptions);
            Consumptions++;
            return core.GetResult(token);
        }
        public ValueTaskSourceStatus GetStatus(short token) => core.GetStatus(token);
        public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
        {
            core.OnCompleted(continuation, state, token, flags);
            Subscribed.TrySetResult();
        }
    }

    internal class Counts
    {
        internal int Acquisitions { get; set; }
        internal int Moves { get; set; }
        internal int Reads { get; set; }
        internal int Disposals { get; set; }
        internal CancellationToken Token { get; set; }
    }

    internal sealed class AsyncSource<T>(T[] items) : Counts, IAsyncEnumerable<T>
    {
        private readonly T[] values = items;
        internal Exception? AcquisitionFault { get; init; }
        internal Exception? MoveFault { get; init; }
        internal Exception? DisposeFault { get; init; }
        internal bool CanceledProducer { get; init; }
        internal Gate<bool>? MoveGate { get; init; }
        internal Gate<bool>? DisposeGate { get; init; }
        internal int CurrentFaultIndex { get; init; } = -1;
        internal Task<bool>? FaultTask { get; private set; }
        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            Acquisitions++;
            Token = cancellationToken;
            if (AcquisitionFault is not null) throw AcquisitionFault;
            return new Enumerator(this);
        }
        private sealed class Enumerator(AsyncSource<T> owner) : IAsyncEnumerator<T>
        {
            private int index = -1;
            public T Current
            {
                get
                {
                    owner.Reads++;
                    if (index == owner.CurrentFaultIndex) throw new InvalidOperationException("Current must not be read");
                    return owner.values[index];
                }
            }
            public ValueTask<bool> MoveNextAsync()
            {
                owner.Moves++;
                index++;
                if (owner.MoveFault is not null)
                {
                    owner.FaultTask = owner.CanceledProducer ? CancelWithKnownIdentity(owner.MoveFault) : Task.FromException<bool>(owner.MoveFault);
                    Assert.Equal(owner.CanceledProducer, owner.FaultTask.IsCanceled);
                    Assert.Equal(!owner.CanceledProducer, owner.FaultTask.IsFaulted);
                    return new(owner.FaultTask);
                }
                return index == 0 && owner.MoveGate is not null ? owner.MoveGate.Value : ValueTask.FromResult(index < owner.values.Length);
            }
            public ValueTask DisposeAsync()
            {
                owner.Disposals++;
                if (owner.DisposeFault is not null) return ValueTask.FromException(owner.DisposeFault);
                return owner.DisposeGate is null ? ValueTask.CompletedTask : AwaitDisposal(owner.DisposeGate);
            }
        }
        private static async Task<bool> CancelWithKnownIdentity(Exception exception)
        {
            await Task.CompletedTask;
            throw exception;
        }
        private static async ValueTask AwaitDisposal(Gate<bool> gate) { _ = await gate.Value; }
    }

    internal sealed class SyncSource<T>(T[] items) : Counts, IEnumerable<T>
    {
        private readonly T[] values = items;
        internal Exception? AcquisitionFault { get; init; }
        internal Exception? MoveFault { get; init; }
        internal Exception? DisposeFault { get; init; }
        internal int CurrentFaultIndex { get; init; } = -1;
        public IEnumerator<T> GetEnumerator()
        {
            Acquisitions++;
            if (AcquisitionFault is not null) throw AcquisitionFault;
            return new Enumerator(this);
        }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        private sealed class Enumerator(SyncSource<T> owner) : IEnumerator<T>
        {
            private int index = -1;
            public T Current
            {
                get
                {
                    owner.Reads++;
                    if (index == owner.CurrentFaultIndex) throw new InvalidOperationException("Current must not be read");
                    return owner.values[index];
                }
            }
            object? IEnumerator.Current => Current;
            public bool MoveNext()
            {
                owner.Moves++;
                if (owner.MoveFault is not null) throw owner.MoveFault;
                return ++index < owner.values.Length;
            }
            public void Dispose()
            {
                owner.Disposals++;
                if (owner.DisposeFault is not null) throw owner.DisposeFault;
            }
            public void Reset() => throw new NotSupportedException();
        }
    }
}

