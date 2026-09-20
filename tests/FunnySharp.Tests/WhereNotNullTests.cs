namespace FunnySharp.Tests;

#pragma warning disable xUnit1051

public sealed class WhereNotNullTests
{
    [Fact]
    public void WhereNotNullRejectsNullSourcesEagerly()
    {
        IEnumerable<string?>? classSource = null;
        IEnumerable<int?>? structSource = null;
        IAsyncEnumerable<string?>? asyncClassSource = null;
        IAsyncEnumerable<int?>? asyncStructSource = null;

        Assert.Throws<ArgumentNullException>(() => classSource!.WhereNotNull());
        Assert.Throws<ArgumentNullException>(() => structSource!.WhereNotNull());
        Assert.Throws<ArgumentNullException>(() => asyncClassSource!.WhereNotNull());
        Assert.Throws<ArgumentNullException>(() => asyncStructSource!.WhereNotNull());
    }

    [Fact]
    public void WhereNotNullKeepsNonNullReferenceItemsInSourceOrder()
    {
        string?[] withNulls = ["a", null, "b", null, "c"];
        string?[] allNulls = [null, null, null];
        string[] withoutNulls = ["x", "y"];

        Assert.Equal(["a", "b", "c"], withNulls.WhereNotNull().ToArray());
        Assert.Empty(allNulls.WhereNotNull());
        Assert.Equal(["x", "y"], withoutNulls.WhereNotNull().ToArray());
    }

    [Fact]
    public void WhereNotNullUnwrapsNonNullNullableValueItemsInSourceOrder()
    {
        int?[] withNulls = [1, null, 2, null, 3];
        int?[] allNulls = [null, null];
        int?[] withoutNulls = [4, 5];

        Assert.Equal([1, 2, 3], withNulls.WhereNotNull().ToArray());
        Assert.Empty(allNulls.WhereNotNull());
        Assert.Equal([4, 5], withoutNulls.WhereNotNull().ToArray());
    }

    [Fact]
    public void WhereNotNullIsDeferredAndEnumeratesTheSourceOncePerConsumption()
    {
        var classSource = new ProbeEnumerable<string?>(["a", null, "b"]);
        var structSource = new ProbeEnumerable<int?>([1, null, 2]);

        var classFiltered = classSource.WhereNotNull();
        var structFiltered = structSource.WhereNotNull();

        Assert.Equal(0, classSource.EnumeratorCount);
        Assert.Equal(0, structSource.EnumeratorCount);

        Assert.Equal(["a", "b"], classFiltered.ToArray());
        Assert.Equal([1, 2], structFiltered.ToArray());

        Assert.Equal(1, classSource.EnumeratorCount);
        Assert.Equal(3, classSource.ItemsYielded);
        Assert.Equal(1, classSource.DisposeCount);
        Assert.Equal(1, structSource.EnumeratorCount);
        Assert.Equal(3, structSource.ItemsYielded);
        Assert.Equal(1, structSource.DisposeCount);

        Assert.Equal(["a", "b"], classFiltered.ToArray());
        Assert.Equal([1, 2], structFiltered.ToArray());

        Assert.Equal(2, classSource.EnumeratorCount);
        Assert.Equal(6, classSource.ItemsYielded);
        Assert.Equal(2, classSource.DisposeCount);
        Assert.Equal(2, structSource.EnumeratorCount);
        Assert.Equal(6, structSource.ItemsYielded);
        Assert.Equal(2, structSource.DisposeCount);
    }

    [Fact]
    public async Task AsyncWhereNotNullKeepsNonNullReferenceItemsInSourceOrderWithoutCaching()
    {
        var classSource = new ProbeAsyncEnumerable<string?>(["a", null, "b", null, "c"]);
        var classFiltered = classSource.WhereNotNull();

        Assert.Equal(0, classSource.EnumeratorCount);

        var firstPass = await ConsumeAsync(classFiltered);
        var secondPass = await ConsumeAsync(classFiltered);

        Assert.Equal(["a", "b", "c"], firstPass);
        Assert.Equal(["a", "b", "c"], secondPass);
        Assert.Equal(2, classSource.EnumeratorCount);
        Assert.Equal(10, classSource.ItemsYielded);
        Assert.Equal(2, classSource.DisposeCount);
    }

    [Fact]
    public async Task AsyncWhereNotNullUnwrapsNonNullNullableValueItemsInSourceOrder()
    {
        var structSource = new ProbeAsyncEnumerable<int?>([1, null, 2, null, 3]);
        var structFiltered = structSource.WhereNotNull();

        Assert.Equal(0, structSource.EnumeratorCount);

        var values = new List<int>();
        await foreach (var value in structFiltered)
        {
            values.Add(value);
        }

        Assert.Equal([1, 2, 3], values);
        Assert.Equal(1, structSource.EnumeratorCount);
        Assert.Equal(5, structSource.ItemsYielded);
        Assert.Equal(1, structSource.DisposeCount);
    }

    private static async Task<List<T>> ConsumeAsync<T>(IAsyncEnumerable<T> source)
    {
        var values = new List<T>();
        await foreach (var value in source)
        {
            values.Add(value);
        }

        return values;
    }

    private sealed class ProbeAsyncEnumerable<T>(IReadOnlyList<T> values) : IAsyncEnumerable<T>
    {
        public int EnumeratorCount { get; private set; }

        public int ItemsYielded { get; private set; }

        public int DisposeCount { get; private set; }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            EnumeratorCount++;
            return new Enumerator(this, values);
        }

        private sealed class Enumerator(ProbeAsyncEnumerable<T> owner, IReadOnlyList<T> values) : IAsyncEnumerator<T>
        {
            private int index = -1;

            public T Current => values[index];

            public ValueTask<bool> MoveNextAsync()
            {
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
                return ValueTask.CompletedTask;
            }
        }
    }
}

#pragma warning restore xUnit1051
