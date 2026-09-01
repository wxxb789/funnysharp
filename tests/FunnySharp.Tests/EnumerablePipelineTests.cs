using System.Collections;

namespace FunnySharp.Tests;

public sealed class EnumerablePipelineTests
{
    [Fact]
    public void ChooseRejectsNullArgumentsEagerly()
    {
        IEnumerable<int>? source = null;

        Assert.Throws<ArgumentNullException>(() => source!.Choose(static value => Option.Some(value)));
        Assert.Throws<ArgumentNullException>(() =>
            Enumerable.Empty<int>().Choose((Func<int, Option<int>>)null!));
    }

    [Fact]
    public void ChooseIsDeferredAndPreservesOrderWithOneSelectorCallPerReachedItem()
    {
        var source = new ProbeEnumerable<int>([1, 2, 3, 4]);
        var calls = new List<int>();

        var chosen = source.Choose(value =>
        {
            calls.Add(value);
            return value % 2 == 0 ? Option.Some(value * 10) : Option.None<int>();
        });

        Assert.Equal(0, source.EnumeratorCount);
        Assert.Empty(calls);

        Assert.Equal([20, 40], chosen.ToArray());
        Assert.Equal([1, 2, 3, 4], calls);
        Assert.Equal(1, source.EnumeratorCount);
        Assert.Equal(4, source.ItemsYielded);
        Assert.Equal(1, source.DisposeCount);
    }

    [Fact]
    public void ChooseDisposesSourceWhenTheConsumerStopsEarly()
    {
        var source = new ProbeEnumerable<int>([1, 2, 3]);
        var calls = new List<int>();

        var result = source.Choose(value =>
        {
            calls.Add(value);
            return Option.Some(value);
        }).Take(1).ToArray();

        Assert.Equal([1], result);
        Assert.Equal([1], calls);
        Assert.Equal(1, source.ItemsYielded);
        Assert.Equal(1, source.DisposeCount);
    }

    [Fact]
    public void ChooseReenumeratesSourceAndSelectorWithoutCaching()
    {
        var source = new ProbeEnumerable<int>([1, 2]);
        var selectorCalls = 0;
        var chosen = source.Choose(_ => Option.Some(++selectorCalls));

        Assert.Equal([1, 2], chosen.ToArray());
        Assert.Equal([3, 4], chosen.ToArray());
        Assert.Equal(2, source.EnumeratorCount);
        Assert.Equal(4, source.ItemsYielded);
        Assert.Equal(2, source.DisposeCount);
    }

    [Fact]
    public void ChoosePreservesSourceAndSelectorExceptionIdentity()
    {
        var sourceException = new InvalidOperationException("source failed");
        var selectorException = new InvalidOperationException("selector failed");
        var selectorSource = new ProbeEnumerable<int>([1]);

        Assert.Same(
            sourceException,
            Assert.Throws<InvalidOperationException>(() =>
                new ThrowingEnumerable<int>(sourceException)
                    .Choose(static value => Option.Some(value))
                    .ToArray()));
        Assert.Same(
            selectorException,
            Assert.Throws<InvalidOperationException>(() =>
                selectorSource.Choose<int, int>(_ => throw selectorException).ToArray()));
        Assert.Equal(1, selectorSource.DisposeCount);
    }

    private sealed class ThrowingEnumerable<T>(Exception exception) : IEnumerable<T>
    {
        public IEnumerator<T> GetEnumerator() => throw exception;

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class ProbeEnumerable<T>(IReadOnlyList<T> values) : IEnumerable<T>
    {
        public int EnumeratorCount { get; private set; }

        public int ItemsYielded { get; private set; }

        public int DisposeCount { get; private set; }

        public IEnumerator<T> GetEnumerator()
        {
            EnumeratorCount++;
            return Enumerate().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private IEnumerable<T> Enumerate()
        {
            try
            {
                foreach (var value in values)
                {
                    ItemsYielded++;
                    yield return value;
                }
            }
            finally
            {
                DisposeCount++;
            }
        }
    }
}
