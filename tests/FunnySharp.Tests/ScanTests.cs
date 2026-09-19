using System.Collections;

namespace FunnySharp.Tests;

public sealed class ScanTests
{
    [Fact]
    public void ScanRejectsNullArgumentsEagerly()
    {
        IEnumerable<int>? source = null;

        Assert.Throws<ArgumentNullException>(() => source!.Scan(0, static (accumulator, item) => accumulator + item));
        Assert.Throws<ArgumentNullException>(() =>
            Enumerable.Empty<int>().Scan(0, (Func<int, int, int>)null!));
    }

    [Fact]
    public void ScanYieldsTheAccumulatorAfterEachElementInSourceOrderWithoutTheSeed()
    {
        var numbers = new[] { 1, 2, 3, 4 };

        var differences = numbers.Scan(5, static (accumulator, item) => accumulator - item).ToArray();
        var letters = new[] { 'a', 'b', 'c' }
            .Scan("", static (accumulator, item) => accumulator + item)
            .ToArray();

        Assert.Equal([4, 2, -1, -5], differences);
        Assert.Equal(["a", "ab", "abc"], letters);
        Assert.Equal(
            numbers.Aggregate(5, static (accumulator, item) => accumulator - item),
            differences[^1]);
    }

    [Fact]
    public void ScanYieldsNothingForAnEmptySource()
    {
        var calls = 0;

        var scanned = Enumerable.Empty<int>().Scan(0, (accumulator, item) =>
        {
            calls++;
            return accumulator + item;
        }).ToArray();

        Assert.Empty(scanned);
        Assert.Equal(0, calls);
    }

    [Fact]
    public void ScanIsDeferredAndAccumulatesOncePerItemInSourceOrder()
    {
        var source = new ProbeEnumerable<int>([1, 2, 3, 4]);
        var calls = new List<(int Accumulator, int Item)>();

        var scanned = source.Scan(0, (accumulator, item) =>
        {
            calls.Add((accumulator, item));
            return accumulator + item;
        });

        Assert.Equal(0, source.EnumeratorCount);
        Assert.Empty(calls);

        Assert.Equal([1, 3, 6, 10], scanned.ToArray());
        Assert.Equal([(0, 1), (1, 2), (3, 3), (6, 4)], calls);
        Assert.Equal(1, source.EnumeratorCount);
        Assert.Equal(4, source.ItemsYielded);
        Assert.Equal(1, source.DisposeCount);
    }

    [Fact]
    public void ScanDisposesSourceWhenTheConsumerStopsEarly()
    {
        var source = new ProbeEnumerable<int>([1, 2, 3]);

        var result = source.Scan(0, static (accumulator, item) => accumulator + item).Take(1).ToArray();

        Assert.Equal([1], result);
        Assert.Equal(1, source.ItemsYielded);
        Assert.Equal(1, source.DisposeCount);
    }

    [Fact]
    public void ScanReenumeratesSourceAndAccumulatorWithoutCaching()
    {
        var source = new ProbeEnumerable<int>([1, 2]);
        var accumulatorCalls = 0;
        var scanned = source.Scan(0, (accumulator, _) => ++accumulatorCalls);

        Assert.Equal([1, 2], scanned.ToArray());
        Assert.Equal([3, 4], scanned.ToArray());
        Assert.Equal(2, source.EnumeratorCount);
        Assert.Equal(4, source.ItemsYielded);
        Assert.Equal(2, source.DisposeCount);
    }

    [Fact]
    public void ScanPreservesSourceAndAccumulatorExceptionIdentityAndStopsIterating()
    {
        var sourceException = new InvalidOperationException("source failed");
        var accumulatorException = new InvalidOperationException("accumulator failed");
        var accumulatorSource = new ProbeEnumerable<int>([1, 2, 3]);

        Assert.Same(
            sourceException,
            Assert.Throws<InvalidOperationException>(() =>
                new ThrowingEnumerable<int>(sourceException)
                    .Scan(0, static (accumulator, item) => accumulator + item)
                    .ToArray()));
        Assert.Same(
            accumulatorException,
            Assert.Throws<InvalidOperationException>(() =>
                accumulatorSource.Scan(0, (accumulator, item) =>
                    item == 2 ? throw accumulatorException : accumulator + item).ToArray()));

        Assert.Equal(2, accumulatorSource.ItemsYielded);
        Assert.Equal(1, accumulatorSource.DisposeCount);
    }

    private sealed class ThrowingEnumerable<T>(Exception exception) : IEnumerable<T>
    {
        public IEnumerator<T> GetEnumerator() => throw exception;

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
