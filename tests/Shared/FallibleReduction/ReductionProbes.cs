using System.Collections;

namespace FunnySharp.TestSupport.FallibleReduction;

internal sealed class ReductionProbe<T> : IEnumerable<T>
{
    private readonly IReadOnlyList<T> items;
    private readonly int? faultAtIndex;
    private readonly Exception? fault;
    private readonly Exception? disposeException;

    internal ReductionProbe(
        IReadOnlyList<T> items,
        int? faultAtIndex = null,
        Exception? fault = null,
        Exception? disposeException = null)
    {
        this.items = items;
        this.faultAtIndex = faultAtIndex;
        this.fault = fault;
        this.disposeException = disposeException;
    }

    internal int GetEnumeratorCalls { get; private set; }

    internal int MoveNextCalls { get; private set; }

    internal int CurrentReads { get; private set; }

    internal int DisposeCalls { get; private set; }

    public IEnumerator<T> GetEnumerator()
    {
        GetEnumeratorCalls++;
        return new Enumerator(this);
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private sealed class Enumerator(ReductionProbe<T> probe) : IEnumerator<T>
    {
        private int index = -1;

        public T Current
        {
            get
            {
                probe.CurrentReads++;
                return probe.items[index];
            }
        }

        object IEnumerator.Current => Current!;

        public bool MoveNext()
        {
            probe.MoveNextCalls++;

            if (index + 1 >= probe.items.Count)
            {
                index = probe.items.Count;
                return false;
            }

            if (probe.faultAtIndex == index + 1)
            {
                throw probe.fault
                    ?? new InvalidOperationException("The probe fault was not configured.");
            }

            index++;
            return true;
        }

        public void Reset() => throw new NotSupportedException();

        public void Dispose()
        {
            probe.DisposeCalls++;

            if (probe.disposeException is not null)
            {
                throw probe.disposeException;
            }
        }
    }
}

internal sealed class AsyncReductionProbe<T> : IAsyncEnumerable<T>
{
    private readonly IReadOnlyList<T> items;
    private readonly int? faultAtIndex;
    private readonly Exception? fault;
    private readonly Exception? disposeException;

    internal AsyncReductionProbe(
        IReadOnlyList<T> items,
        int? faultAtIndex = null,
        Exception? fault = null,
        Exception? disposeException = null)
    {
        this.items = items;
        this.faultAtIndex = faultAtIndex;
        this.fault = fault;
        this.disposeException = disposeException;
    }

    internal int GetAsyncEnumeratorCalls { get; private set; }

    internal int MoveNextAsyncCalls { get; private set; }

    internal int CurrentReads { get; private set; }

    internal int DisposeAsyncCalls { get; private set; }

    internal CancellationToken Token { get; private set; }

    public IAsyncEnumerator<T> GetAsyncEnumerator(
        CancellationToken cancellationToken = default)
    {
        GetAsyncEnumeratorCalls++;
        Token = cancellationToken;
        return new Enumerator(this);
    }

    private sealed class Enumerator(AsyncReductionProbe<T> probe) : IAsyncEnumerator<T>
    {
        private int index = -1;

        public T Current
        {
            get
            {
                probe.CurrentReads++;
                return probe.items[index];
            }
        }

        public ValueTask<bool> MoveNextAsync()
        {
            probe.MoveNextAsyncCalls++;

            if (index + 1 >= probe.items.Count)
            {
                index = probe.items.Count;
                return ValueTask.FromResult(false);
            }

            if (probe.faultAtIndex == index + 1)
            {
                return ValueTask.FromException<bool>(
                    probe.fault
                    ?? new InvalidOperationException("The probe fault was not configured."));
            }

            index++;
            return ValueTask.FromResult(true);
        }

        public ValueTask DisposeAsync()
        {
            probe.DisposeAsyncCalls++;

            return probe.disposeException is null
                ? ValueTask.CompletedTask
                : ValueTask.FromException(probe.disposeException);
        }
    }
}
