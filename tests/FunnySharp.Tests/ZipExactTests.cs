using System.Collections;

namespace FunnySharp.Tests;

public sealed class ZipExactTests
{
    [Fact]
    public void ZipExactRejectsNullArgumentsEagerly()
    {
        IEnumerable<int>? first = null;
        IEnumerable<string>? second = null;
        var firstSource = new[] { 1, 2 };
        var secondSource = new[] { "a", "b" };

        Assert.Throws<ArgumentNullException>(() => first!.ZipExactOrNone(secondSource));
        Assert.Throws<ArgumentNullException>(() => firstSource.ZipExactOrNone(second!));
        Assert.Throws<ArgumentNullException>(() =>
            first!.ZipExact(secondSource, static (firstCount, secondCount) => "length mismatch"));
        Assert.Throws<ArgumentNullException>(() =>
            firstSource.ZipExact(second!, static (firstCount, secondCount) => "length mismatch"));
        Assert.Throws<ArgumentNullException>(() =>
            firstSource.ZipExact(secondSource, (Func<int, int, string>)null!));
    }

    [Fact]
    public void ZipExactPairsEqualLengthSourcesInOrder()
    {
        var numbers = new[] { 1, 2, 3 };
        var letters = new[] { "a", "b", "c" };

        var optionPairs = GetOptionPairs(numbers.ZipExactOrNone(letters));
        var resultPairs = GetResultPairs(
            numbers.ZipExact(letters, static (firstCount, secondCount) => $"{firstCount} vs {secondCount}"));
        var emptyOptionPairs = GetOptionPairs(
            Enumerable.Empty<int>().ZipExactOrNone(Enumerable.Empty<string>()));
        var emptyResultPairs = GetResultPairs(Enumerable.Empty<int>().ZipExact(
            Enumerable.Empty<string>(), static (firstCount, secondCount) => $"{firstCount} vs {secondCount}"));

        Assert.Equal([(1, "a"), (2, "b"), (3, "c")], optionPairs);
        Assert.Equal([(1, "a"), (2, "b"), (3, "c")], resultPairs);
        Assert.Empty(emptyOptionPairs);
        Assert.Empty(emptyResultPairs);
        Assert.Equal(1, optionPairs[0].First);
        Assert.Equal("a", optionPairs[0].Second);
        Assert.Equal(3, resultPairs[^1].First);
        Assert.Equal("c", resultPairs[^1].Second);
    }

    [Fact]
    public void ZipExactOrNoneReturnsNoneWhenLengthsDiffer()
    {
        var leftLonger = new[] { 1, 2, 3, 4, 5 }.ZipExactOrNone(new[] { "a", "b", "c" });
        var rightLonger = new[] { 1, 2, 3 }.ZipExactOrNone(new[] { "a", "b", "c", "d", "e" });
        var emptyLeft = Enumerable.Empty<int>().ZipExactOrNone(new[] { "a", "b", "c", "d" });
        var emptyRight = new[] { 1, 2, 3, 4 }.ZipExactOrNone(Enumerable.Empty<string>());

        Assert.True(leftLonger.IsNone);
        Assert.True(rightLonger.IsNone);
        Assert.True(emptyLeft.IsNone);
        Assert.True(emptyRight.IsNone);
    }

    [Fact]
    public void ZipExactFailsWithTheFullCountsWhenLengthsDiffer()
    {
        var calls = new List<(int FirstCount, int SecondCount)>();
        Func<int, int, string> lengthError = (firstCount, secondCount) =>
        {
            calls.Add((firstCount, secondCount));
            return $"{firstCount} vs {secondCount}";
        };
        var leftLonger = new[] { 1, 2, 3, 4, 5 }.ZipExact(new[] { "a", "b", "c" }, lengthError);
        var rightLonger = new[] { 1, 2, 3 }.ZipExact(new[] { "a", "b", "c", "d", "e" }, lengthError);
        var emptyLeft = Enumerable.Empty<int>().ZipExact(new[] { "a", "b", "c", "d" }, lengthError);
        var emptyRight = new[] { 1, 2, 3, 4 }.ZipExact(Enumerable.Empty<string>(), lengthError);

        Assert.True(leftLonger.TryGetError(out var leftLongerError));
        Assert.Equal("5 vs 3", leftLongerError);
        Assert.True(rightLonger.TryGetError(out var rightLongerError));
        Assert.Equal("3 vs 5", rightLongerError);
        Assert.True(emptyLeft.TryGetError(out var emptyLeftError));
        Assert.Equal("0 vs 4", emptyLeftError);
        Assert.True(emptyRight.TryGetError(out var emptyRightError));
        Assert.Equal("4 vs 0", emptyRightError);
        Assert.Equal([(5, 3), (3, 5), (0, 4), (4, 0)], calls);
    }

    [Fact]
    public void ZipExactSuccessNeverInvokesTheLengthErrorDelegate()
    {
        var calls = 0;

        var result = new[] { 1, 2, 3 }.ZipExact(new[] { "a", "b", "c" }, (firstCount, secondCount) =>
        {
            calls++;
            return $"{firstCount} vs {secondCount}";
        });
        var empty = Enumerable.Empty<int>().ZipExact(Enumerable.Empty<string>(), (firstCount, secondCount) =>
        {
            calls++;
            return $"{firstCount} vs {secondCount}";
        });

        Assert.Equal([(1, "a"), (2, "b"), (3, "c")], GetResultPairs(result));
        Assert.Empty(GetResultPairs(empty));
        Assert.Equal(0, calls);
    }

    [Fact]
    public void ZipExactEnumeratesEachSourceOnceAndDisposesEnumeratorsOnTheSuccessPath()
    {
        var optionFirst = new ProbeEnumerable<int>([1, 2]);
        var optionSecond = new ProbeEnumerable<string>(["a", "b"]);
        var resultFirst = new ProbeEnumerable<int>([1, 2]);
        var resultSecond = new ProbeEnumerable<string>(["a", "b"]);

        _ = optionFirst.ZipExactOrNone(optionSecond);
        _ = resultFirst.ZipExact(
            resultSecond, static (firstCount, secondCount) => $"{firstCount} vs {secondCount}");

        AssertProbeCompleted(optionFirst, 2);
        AssertProbeCompleted(optionSecond, 2);
        AssertProbeCompleted(resultFirst, 2);
        AssertProbeCompleted(resultSecond, 2);
    }

    [Fact]
    public void ZipExactEnumeratesEachSourceOnceDrainsTheLongerSideAndDisposesOnTheUnequalPath()
    {
        var optionFirst = new ProbeEnumerable<int>([1, 2, 3]);
        var optionSecond = new ProbeEnumerable<string>(["a"]);
        var resultFirst = new ProbeEnumerable<int>([1, 2, 3]);
        var resultSecond = new ProbeEnumerable<string>(["a"]);
        var result = resultFirst.ZipExact(
            resultSecond, static (firstCount, secondCount) => $"{firstCount} vs {secondCount}");

        Assert.True(optionFirst.ZipExactOrNone(optionSecond).IsNone);
        Assert.True(result.TryGetError(out var error));
        Assert.Equal("3 vs 1", error);

        AssertProbeCompleted(optionFirst, 3);
        AssertProbeCompleted(optionSecond, 1);
        AssertProbeCompleted(resultFirst, 3);
        AssertProbeCompleted(resultSecond, 1);
    }

    [Fact]
    public void ZipExactPreservesSourceAndLengthErrorExceptionIdentity()
    {
        var firstException = new InvalidOperationException("first source failed");
        var secondException = new InvalidOperationException("second source failed");
        var lengthErrorException = new InvalidOperationException("length error failed");
        var firstSource = new ThrowingEnumerable<int>(firstException);
        var secondSource = new ThrowingEnumerable<string>(secondException);

        Assert.Same(
            firstException,
            Assert.Throws<InvalidOperationException>(() => firstSource.ZipExactOrNone(new[] { "a" })));
        Assert.Same(
            secondException,
            Assert.Throws<InvalidOperationException>(() => new[] { 1 }.ZipExactOrNone(secondSource)));
        Assert.Same(
            lengthErrorException,
            Assert.Throws<InvalidOperationException>(() => new[] { 1, 2 }.ZipExact(
                new[] { "a" },
                (Func<int, int, string>)((_, _) => throw lengthErrorException))));
    }

    [Fact]
    public void LargeEqualLengthSourcesAreZippedInOrder()
    {
        const int count = 100_000;

        var optionPairs = GetOptionPairs(Enumerable.Range(0, count).ZipExactOrNone(
            Enumerable.Range(0, count).Select(static value => value * 10)));
        var resultPairs = GetResultPairs(Enumerable.Range(0, count).ZipExact(
            Enumerable.Range(0, count).Select(static value => value * 10),
            static (firstCount, secondCount) => $"{firstCount} vs {secondCount}"));

        Assert.Equal(count, optionPairs.Count);
        Assert.Equal(count, resultPairs.Count);
        Assert.Equal(0, optionPairs[0].First);
        Assert.Equal(0, optionPairs[0].Second);
        Assert.Equal(50_000, optionPairs[50_000].First);
        Assert.Equal(500_000, optionPairs[50_000].Second);
        Assert.Equal(count - 1, optionPairs[^1].First);
        Assert.Equal((count - 1) * 10, optionPairs[^1].Second);
        Assert.Equal(count - 1, resultPairs[^1].First);
        Assert.Equal((count - 1) * 10, resultPairs[^1].Second);
    }

    [Fact]
    public void LargeUnequalLengthSourcesReportTheFullCounts()
    {
        const int count = 100_000;
        var calls = new List<(int FirstCount, int SecondCount)>();

        var option = Enumerable.Range(0, count).ZipExactOrNone(Enumerable.Range(0, count - 1));
        var result = Enumerable.Range(0, count).ZipExact(
            Enumerable.Range(0, count - 1),
            (firstCount, secondCount) =>
            {
                calls.Add((firstCount, secondCount));
                return $"{firstCount} vs {secondCount}";
            });

        Assert.True(option.IsNone);
        Assert.True(result.TryGetError(out var error));
        Assert.Equal($"{count} vs {count - 1}", error);
        Assert.Equal([(count, count - 1)], calls);
    }

    private static IReadOnlyList<(TFirst First, TSecond Second)> GetOptionPairs<TFirst, TSecond>(
        Option<IReadOnlyList<(TFirst First, TSecond Second)>> option)
    {
        Assert.True(option.TryGetValue(out var pairs));
        return pairs!;
    }

    private static IReadOnlyList<(TFirst First, TSecond Second)> GetResultPairs<TFirst, TSecond, TError>(
        Result<IReadOnlyList<(TFirst First, TSecond Second)>, TError> result)
    {
        Assert.True(result.TryGetValue(out var pairs));
        return pairs!;
    }

    private static void AssertProbeCompleted<T>(ProbeEnumerable<T> probe, int itemsYielded)
    {
        Assert.Equal(1, probe.EnumeratorCount);
        Assert.Equal(itemsYielded, probe.ItemsYielded);
        Assert.Equal(1, probe.DisposeCount);
    }

    private sealed class ThrowingEnumerable<T>(Exception exception) : IEnumerable<T>
    {
        public IEnumerator<T> GetEnumerator() => throw exception;

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
