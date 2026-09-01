using System.Collections;

namespace FunnySharp.Tests;

public sealed class CollectionTraversalTests
{
    [Fact]
    public void SequenceAndTraverseRejectNullArgumentsEagerly()
    {
        IEnumerable<Option<int>>? options = null;
        IEnumerable<Result<int, string>>? results = null;
        IEnumerable<Validation<int, string>>? validations = null;
        IEnumerable<int>? source = null;

        Assert.Throws<ArgumentNullException>(() => options!.Sequence());
        Assert.Throws<ArgumentNullException>(() => results!.Sequence());
        Assert.Throws<ArgumentNullException>(() => validations!.Sequence());
        Assert.Throws<ArgumentNullException>(() => source!.Traverse(static value => Option.Some(value)));
        Assert.Throws<ArgumentNullException>(() =>
            source!.Traverse(static value => Result<int, string>.Success(value)));
        Assert.Throws<ArgumentNullException>(() =>
            source!.Traverse(static value => Validation<int, string>.Valid(value)));
        Assert.Throws<ArgumentNullException>(() =>
            Enumerable.Empty<int>().Traverse((Func<int, Option<int>>)null!));
        Assert.Throws<ArgumentNullException>(() =>
            Enumerable.Empty<int>().Traverse((Func<int, Result<int, string>>)null!));
        Assert.Throws<ArgumentNullException>(() =>
            Enumerable.Empty<int>().Traverse((Func<int, Validation<int, string>>)null!));
    }

    [Fact]
    public void SequenceReturnsOrderedAndEmptySuccesses()
    {
        var options = new[] { Option.Some(1), Option.Some(2) }.Sequence();
        var results = new[]
        {
            Result<int, string>.Success(1),
            Result<int, string>.Success(2),
        }.Sequence();
        var validations = new[]
        {
            Validation<int, string>.Valid(1),
            Validation<int, string>.Valid(2),
        }.Sequence();

        Assert.Equal([1, 2], GetOptionValues(options));
        Assert.Equal([1, 2], GetResultValues(results));
        Assert.Equal([1, 2], GetValidationValues(validations));
        Assert.Empty(GetOptionValues(Enumerable.Empty<Option<int>>().Sequence()));
        Assert.Empty(GetResultValues(Enumerable.Empty<Result<int, string>>().Sequence()));
        Assert.Empty(GetValidationValues(Enumerable.Empty<Validation<int, string>>().Sequence()));
    }

    [Fact]
    public void SequenceEnumeratesNonCollectionSourcesOnceAndDisposesEnumerators()
    {
        var optionSource = new ProbeEnumerable<Option<int>>([Option.Some(1)]);
        var resultSource = new ProbeEnumerable<Result<int, string>>([Result<int, string>.Success(1)]);
        var validationSource = new ProbeEnumerable<Validation<int, string>>(
            [Validation<int, string>.Valid(1)]);

        _ = optionSource.Sequence();
        _ = resultSource.Sequence();
        _ = validationSource.Sequence();

        Assert.False((object)optionSource is ICollection<Option<int>>);
        AssertProbeCompleted(optionSource);
        AssertProbeCompleted(resultSource);
        AssertProbeCompleted(validationSource);
    }

    [Fact]
    public void SequenceShortCircuitsOptionAndResultButAccumulatesAllValidationErrorsInOrder()
    {
        var optionSource = new ProbeEnumerable<Option<int>>(
            [Option.Some(1), Option.None<int>(), Option.Some(3)]);
        var resultSource = new ProbeEnumerable<Result<int, string>>(
            [Result<int, string>.Success(1), Result<int, string>.Failure("result"), Result<int, string>.Success(3)]);
        var validationSource = new ProbeEnumerable<Validation<int, string>>(
        [
            Validation<int, string>.Valid(1),
            Validation<int, string>.InvalidMany(["first", "second"]),
            Validation<int, string>.Valid(3),
            Validation<int, string>.Invalid("third"),
        ]);

        var option = optionSource.Sequence();
        var result = resultSource.Sequence();
        var validation = validationSource.Sequence();

        Assert.True(option.IsNone);
        Assert.True(result.TryGetError(out var resultError));
        Assert.Equal("result", resultError);
        Assert.True(validation.TryGetErrors(out var errors));
        Assert.Equal(["first", "second", "third"], errors);
        Assert.Equal(2, optionSource.ItemsYielded);
        Assert.Equal(2, resultSource.ItemsYielded);
        Assert.Equal(4, validationSource.ItemsYielded);
        Assert.Equal(1, optionSource.DisposeCount);
        Assert.Equal(1, resultSource.DisposeCount);
        Assert.Equal(1, validationSource.DisposeCount);
    }

    [Fact]
    public void SequenceDefersCollectionCountUntilASuccessValueNeedsBuffering()
    {
        var optionSource = new ThrowingCountCollection<Option<int>>(
            [Option.None<int>(), Option.Some(1)]);
        var resultSource = new ThrowingCountCollection<Result<int, string>>(
            [Result<int, string>.Failure("result"), Result<int, string>.Success(1)]);
        var validationSource = new ThrowingCountCollection<Validation<int, string>>(
        [
            Validation<int, string>.Invalid("first"),
            Validation<int, string>.InvalidMany(["second", "third"]),
        ]);

        var option = optionSource.Sequence();
        var result = resultSource.Sequence();
        var validation = validationSource.Sequence();

        Assert.True(option.IsNone);
        Assert.True(result.TryGetError(out var resultError));
        Assert.Equal("result", resultError);
        Assert.True(validation.TryGetErrors(out var validationErrors));
        Assert.Equal(["first", "second", "third"], validationErrors);
        Assert.Equal(1, optionSource.ItemsYielded);
        Assert.Equal(1, resultSource.ItemsYielded);
        Assert.Equal(2, validationSource.ItemsYielded);
    }

    [Fact]
    public void TraverseMatchesIdentitySequenceByCaseAndContents()
    {
        var optionInput = new[] { Option.Some(1), Option.Some(2) };
        var resultInput = new[]
        {
            Result<int, string>.Success(1),
            Result<int, string>.Success(2),
        };
        var validationInput = new[]
        {
            Validation<int, string>.Valid(1),
            Validation<int, string>.Valid(2),
        };

        AssertEquivalent(optionInput.Sequence(), optionInput.Traverse(static value => value));
        AssertEquivalent(resultInput.Sequence(), resultInput.Traverse(static value => value));
        AssertEquivalent(validationInput.Sequence(), validationInput.Traverse(static value => value));

        var absentOptions = new[] { Option.Some(1), Option.None<int>() };
        var failedResults = new[]
        {
            Result<int, string>.Success(1),
            Result<int, string>.Failure("bad"),
        };
        var invalidValidations = new[]
        {
            Validation<int, string>.InvalidMany(["bad", "worse"]),
            Validation<int, string>.Valid(1),
        };

        AssertEquivalent(absentOptions.Sequence(), absentOptions.Traverse(static value => value));
        AssertEquivalent(failedResults.Sequence(), failedResults.Traverse(static value => value));
        AssertEquivalent(invalidValidations.Sequence(), invalidValidations.Traverse(static value => value));
    }

    [Fact]
    public void TraverseInvokesSelectorsInReachedSourceOrder()
    {
        var optionCalls = new List<int>();
        var resultCalls = new List<int>();
        var validationCalls = new List<int>();

        var option = new[] { 1, 2, 3 }.Traverse(value =>
        {
            optionCalls.Add(value);
            return value == 2 ? Option.None<int>() : Option.Some(value);
        });
        var result = new[] { 1, 2, 3 }.Traverse(value =>
        {
            resultCalls.Add(value);
            return value == 2
                ? Result<int, string>.Failure("bad")
                : Result<int, string>.Success(value);
        });
        var validation = new[] { 1, 2, 3 }.Traverse(value =>
        {
            validationCalls.Add(value);
            return value == 2
                ? Validation<int, string>.Invalid("bad")
                : Validation<int, string>.Valid(value);
        });

        Assert.True(option.IsNone);
        Assert.True(result.TryGetError(out var resultError));
        Assert.Equal("bad", resultError);
        Assert.True(validation.TryGetErrors(out var errors));
        Assert.Equal(["bad"], errors);
        Assert.Equal([1, 2], optionCalls);
        Assert.Equal([1, 2], resultCalls);
        Assert.Equal([1, 2, 3], validationCalls);
    }

    [Fact]
    public void TraversalPropagatesExceptionsByIdentityAndDisposesWhenAnEnumeratorExists()
    {
        var optionSourceException = new InvalidOperationException("option source");
        var resultSourceException = new InvalidOperationException("result source");
        var validationSourceException = new InvalidOperationException("validation source");
        var optionEnumeratorException = new InvalidOperationException("option enumerator");
        var resultEnumeratorException = new InvalidOperationException("result enumerator");
        var validationEnumeratorException = new InvalidOperationException("validation enumerator");
        var optionSelectorException = new InvalidOperationException("option selector");
        var resultSelectorException = new InvalidOperationException("result selector");
        var validationSelectorException = new InvalidOperationException("validation selector");
        var optionEnumeratorSource = new ProbeEnumerable<Option<int>>(
            [Option.Some(1)],
            throwOnMoveNextCall: 2,
            moveNextException: optionEnumeratorException);
        var resultEnumeratorSource = new ProbeEnumerable<Result<int, string>>(
            [Result<int, string>.Success(1)],
            throwOnMoveNextCall: 2,
            moveNextException: resultEnumeratorException);
        var validationEnumeratorSource = new ProbeEnumerable<Validation<int, string>>(
            [Validation<int, string>.Valid(1)],
            throwOnMoveNextCall: 2,
            moveNextException: validationEnumeratorException);
        var optionSelectorSource = new ProbeEnumerable<int>([1]);
        var resultSelectorSource = new ProbeEnumerable<int>([1]);
        var validationSelectorSource = new ProbeEnumerable<int>([1]);

        Assert.Same(
            optionSourceException,
            Assert.Throws<InvalidOperationException>(() =>
                new ThrowingEnumerable<Option<int>>(optionSourceException).Sequence()));
        Assert.Same(
            resultSourceException,
            Assert.Throws<InvalidOperationException>(() =>
                new ThrowingEnumerable<Result<int, string>>(resultSourceException).Sequence()));
        Assert.Same(
            validationSourceException,
            Assert.Throws<InvalidOperationException>(() =>
                new ThrowingEnumerable<Validation<int, string>>(validationSourceException).Sequence()));
        Assert.Same(
            optionEnumeratorException,
            Assert.Throws<InvalidOperationException>(() => optionEnumeratorSource.Sequence()));
        Assert.Same(
            resultEnumeratorException,
            Assert.Throws<InvalidOperationException>(() => resultEnumeratorSource.Sequence()));
        Assert.Same(
            validationEnumeratorException,
            Assert.Throws<InvalidOperationException>(() => validationEnumeratorSource.Sequence()));
        Assert.Same(
            optionSelectorException,
            Assert.Throws<InvalidOperationException>(() =>
                optionSelectorSource.Traverse<int, int>(_ => throw optionSelectorException)));
        Assert.Same(
            resultSelectorException,
            Assert.Throws<InvalidOperationException>(() =>
                resultSelectorSource.Traverse<int, int, string>(
                    (Func<int, Result<int, string>>)(_ => throw resultSelectorException))));
        Assert.Same(
            validationSelectorException,
            Assert.Throws<InvalidOperationException>(() =>
                validationSelectorSource.Traverse<int, int, string>(
                    (Func<int, Validation<int, string>>)(_ => throw validationSelectorException))));

        Assert.Equal(1, optionEnumeratorSource.DisposeCount);
        Assert.Equal(1, resultEnumeratorSource.DisposeCount);
        Assert.Equal(1, validationEnumeratorSource.DisposeCount);
        Assert.Equal(1, optionSelectorSource.DisposeCount);
        Assert.Equal(1, resultSelectorSource.DisposeCount);
        Assert.Equal(1, validationSelectorSource.DisposeCount);
    }

    [Fact]
    public void LargeSequencesAreStackSafe()
    {
        const int count = 100_000;

        var options = Enumerable.Range(0, count).Select(static value => Option.Some(value)).Sequence();
        var results = Enumerable.Range(0, count)
            .Select(static value => Result<int, string>.Success(value))
            .Sequence();
        var validations = Enumerable.Range(0, count)
            .Select(static value => Validation<int, string>.Valid(value))
            .Sequence();

        AssertLargeSequence(GetOptionValues(options), count);
        AssertLargeSequence(GetResultValues(results), count);
        AssertLargeSequence(GetValidationValues(validations), count);
    }

    private static IReadOnlyList<T> GetOptionValues<T>(Option<IReadOnlyList<T>> option)
    {
        Assert.True(option.TryGetValue(out var values));
        return values!;
    }

    private static IReadOnlyList<T> GetResultValues<T>(Result<IReadOnlyList<T>, string> result)
    {
        Assert.True(result.TryGetValue(out var values));
        return values!;
    }

    private static IReadOnlyList<T> GetValidationValues<T>(Validation<IReadOnlyList<T>, string> validation)
    {
        Assert.True(validation.TryGetValue(out var values));
        return values!;
    }

    private static void AssertEquivalent<T>(
        Option<IReadOnlyList<T>> expected,
        Option<IReadOnlyList<T>> actual)
    {
        Assert.Equal(expected.IsSome, actual.IsSome);
        if (expected.IsSome)
        {
            Assert.Equal(GetOptionValues(expected), GetOptionValues(actual));
        }
    }

    private static void AssertEquivalent<T>(
        Result<IReadOnlyList<T>, string> expected,
        Result<IReadOnlyList<T>, string> actual)
    {
        Assert.Equal(expected.IsSuccess, actual.IsSuccess);
        if (expected.IsSuccess)
        {
            Assert.Equal(GetResultValues(expected), GetResultValues(actual));
        }
        else
        {
            Assert.True(expected.TryGetError(out var expectedError));
            Assert.True(actual.TryGetError(out var actualError));
            Assert.Equal(expectedError, actualError);
        }
    }

    private static void AssertEquivalent<T>(
        Validation<IReadOnlyList<T>, string> expected,
        Validation<IReadOnlyList<T>, string> actual)
    {
        Assert.Equal(expected.IsValid, actual.IsValid);
        if (expected.IsValid)
        {
            Assert.Equal(GetValidationValues(expected), GetValidationValues(actual));
        }
        else
        {
            Assert.True(expected.TryGetErrors(out var expectedErrors));
            Assert.True(actual.TryGetErrors(out var actualErrors));
            Assert.Equal(expectedErrors, actualErrors);
        }
    }

    private static void AssertProbeCompleted<T>(ProbeEnumerable<T> probe)
    {
        Assert.Equal(1, probe.EnumeratorCount);
        Assert.Equal(2, probe.MoveNextCount);
        Assert.Equal(1, probe.ItemsYielded);
        Assert.Equal(1, probe.DisposeCount);
    }

    private static void AssertLargeSequence(IReadOnlyList<int> values, int count)
    {
        Assert.Equal(count, values.Count);
        Assert.Equal(0, values[0]);
        Assert.Equal(count - 1, values[^1]);
    }

    private sealed class ThrowingEnumerable<T>(Exception exception) : IEnumerable<T>
    {
        public IEnumerator<T> GetEnumerator() => throw exception;

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class ProbeEnumerable<T>(
        IReadOnlyList<T> values,
        int? throwOnMoveNextCall = null,
        Exception? moveNextException = null) : IEnumerable<T>
    {
        public int EnumeratorCount { get; private set; }

        public int MoveNextCount { get; private set; }

        public int ItemsYielded { get; private set; }

        public int DisposeCount { get; private set; }

        public IEnumerator<T> GetEnumerator()
        {
            EnumeratorCount++;
            return new Enumerator(this, values, throwOnMoveNextCall, moveNextException);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private sealed class Enumerator(
            ProbeEnumerable<T> owner,
            IReadOnlyList<T> values,
            int? throwOnMoveNextCall,
            Exception? moveNextException) : IEnumerator<T>
        {
            private int index = -1;

            public T Current => values[index];

            object IEnumerator.Current => Current!;

            public bool MoveNext()
            {
                owner.MoveNextCount++;
                if (throwOnMoveNextCall == owner.MoveNextCount)
                {
                    throw moveNextException!;
                }

                var nextIndex = index + 1;
                if (nextIndex == values.Count)
                {
                    return false;
                }

                index = nextIndex;
                owner.ItemsYielded++;
                return true;
            }

            public void Reset() => throw new NotSupportedException();

            public void Dispose() => owner.DisposeCount++;
        }
    }

    private sealed class ThrowingCountCollection<T>(IReadOnlyList<T> values) : ICollection<T>, IReadOnlyCollection<T>
    {
        public int ItemsYielded { get; private set; }

        public int Count => throw new InvalidOperationException("Count should not be read.");

        bool ICollection<T>.IsReadOnly => true;

        public IEnumerator<T> GetEnumerator()
        {
            foreach (var value in values)
            {
                ItemsYielded++;
                yield return value;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        void ICollection<T>.Add(T item) => throw new NotSupportedException();

        void ICollection<T>.Clear() => throw new NotSupportedException();

        bool ICollection<T>.Contains(T item) => values.Contains(item);

        void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            for (var index = 0; index < values.Count; index++)
            {
                array[arrayIndex + index] = values[index];
            }
        }

        bool ICollection<T>.Remove(T item) => throw new NotSupportedException();
    }
}
