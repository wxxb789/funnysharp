using FunnySharp.TestSupport;

namespace FunnySharp.Tests;

public sealed class LookupIndexTests
{
    [Fact]
    public void ToLookupIndexPreservesEveryOccurrenceInSourceOrderAndReportsDistinctAndEntryCounts()
    {
        LookupIndex<string, Row> index = Build([
            new("a", "first"),
            new("b", "only-b"),
            new("a", "second"),
            new("a", "third"),
            new("c", "only-c"),
        ]);

        Assert.Equal(3, index.DistinctCount);
        Assert.Equal(5, index.EntryCount);
        Assert.Equal(["first", "second", "third"], index.GetMany("a").Select(static row => row.Value));
        Assert.Equal(["only-b"], index.GetMany("b").Select(static row => row.Value));
        Assert.Empty(index.GetMany("missing"));
    }

    [Fact]
    public void KeyedReadsDistinguishZeroOneAndMultipleMatches()
    {
        LookupIndex<string, Row> index = Build([
            new("a", "first"),
            new("a", "second"),
            new("b", "only"),
        ]);

        var missingZeroOrOne = index.GetZeroOrOne("missing");
        Assert.True(missingZeroOrOne.TryGetValue(out var none));
        Assert.True(none.IsNone);

        var singleZeroOrOne = index.GetZeroOrOne("b");
        Assert.True(singleZeroOrOne.TryGetValue(out var some));
        Assert.Equal("only", some.Match(static row => row.Value, static () => "none"));

        var ambiguousZeroOrOne = index.GetZeroOrOne("a");
        Assert.True(ambiguousZeroOrOne.TryGetError(out var zeroOrOneError));
        Assert.Equal(LookupErrorKind.DuplicateKey, zeroOrOneError.Kind);
        Assert.Equal("a", zeroOrOneError.Key);
        Assert.Null(zeroOrOneError.SourceIndex);
        Assert.Equal(1, zeroOrOneError.LookupSourceIndex);
        Assert.Equal(0, zeroOrOneError.FirstLookupSourceIndex);

        var singleExactlyOne = index.GetExactlyOne("b");
        Assert.True(singleExactlyOne.TryGetValue(out var exactlyOne));
        Assert.Equal("only", exactlyOne.Value);

        var missingExactlyOne = index.GetExactlyOne("missing");
        Assert.True(missingExactlyOne.TryGetError(out var missingError));
        Assert.Equal(LookupErrorKind.MissingKey, missingError.Kind);
        Assert.Equal("missing", missingError.Key);
        Assert.Null(missingError.SourceIndex);

        var ambiguousExactlyOne = index.GetExactlyOne("a");
        Assert.True(ambiguousExactlyOne.TryGetError(out var duplicateError));
        Assert.Equal(LookupErrorKind.DuplicateKey, duplicateError.Kind);
        Assert.Equal(0, duplicateError.FirstLookupSourceIndex);
        Assert.Equal(1, duplicateError.LookupSourceIndex);
    }

    [Fact]
    public void ToLookupIndexReportsNullKeysAtTheirLookupPositionsAndDoesNotRetainThem()
    {
        var source = new (string? Key, string Value)[]
        {
            ("a", "first"),
            (null, "null-key"),
            ("b", "third"),
        };

        var result = source.ToLookupIndex(static item => item.Key!);

        Assert.True(result.TryGetErrors(out var errors));
        var error = Assert.Single(errors);
        Assert.Equal(LookupErrorKind.NullKey, error.Kind);
        Assert.Null(error.Key);
        Assert.Equal(1, error.LookupSourceIndex);
        Assert.Null(error.SourceIndex);
    }

    [Fact]
    public void QueryMembersRejectNullKeysAndNullDelegatesEagerly()
    {
        LookupIndex<string, Row> index = Build([new("a", "first")]);

        Assert.Throws<ArgumentNullException>(() => index.GetMany(null!));
        Assert.Throws<ArgumentNullException>(() => index.GetZeroOrOne(null!));
        Assert.Throws<ArgumentNullException>(() => index.GetExactlyOne(null!));

        IEnumerable<Row>? nullSource = null;
        Assert.Throws<ArgumentNullException>(() => nullSource!.ToLookupIndex(static row => row.Key));
        Assert.Throws<ArgumentNullException>(
            () => Array.Empty<Row>().ToLookupIndex<string, Row>(null!));
        Assert.Throws<ArgumentNullException>(() => nullSource!.ToUniqueLookupIndex(static row => row.Key));
        Assert.Throws<ArgumentNullException>(
            () => Array.Empty<Row>().ToUniqueLookupIndex<string, Row>(null!));
    }

    [Fact]
    public void ToUniqueLookupIndexReportsEveryOccurrenceAfterTheFirstInLookupOrder()
    {
        var result = new[]
        {
            new Row("z", "z0"),
            new Row("a", "a1"),
            new Row("a", "a2"),
            new Row("z", "z3"),
            new Row("a", "a4"),
        }.ToUniqueLookupIndex(static row => row.Key);

        Assert.True(result.TryGetErrors(out var errors));
        Assert.Equal(3, errors.Count);

        Assert.Equal(2, errors[0].LookupSourceIndex);
        Assert.Equal(1, errors[0].FirstLookupSourceIndex);
        Assert.Equal("a", errors[0].Key);

        Assert.Equal(3, errors[1].LookupSourceIndex);
        Assert.Equal(0, errors[1].FirstLookupSourceIndex);
        Assert.Equal("z", errors[1].Key);

        Assert.Equal(4, errors[2].LookupSourceIndex);
        Assert.Equal(1, errors[2].FirstLookupSourceIndex);
        Assert.Equal("a", errors[2].Key);
    }

    [Fact]
    public void ToUniqueLookupIndexCombinesNullAndDuplicateFailuresInLookupOrder()
    {
        var source = new (string? Key, string Value)[]
        {
            ("a", "a0"),
            (null, "null1"),
            ("a", "a2"),
            ("b", "b3"),
            (null, "null4"),
        };

        var result = source.ToUniqueLookupIndex(static item => item.Key!);

        Assert.True(result.TryGetErrors(out var errors));
        Assert.Equal(3, errors.Count);
        Assert.Equal(LookupErrorKind.NullKey, errors[0].Kind);
        Assert.Equal(1, errors[0].LookupSourceIndex);
        Assert.Equal(LookupErrorKind.DuplicateKey, errors[1].Kind);
        Assert.Equal(2, errors[1].LookupSourceIndex);
        Assert.Equal(0, errors[1].FirstLookupSourceIndex);
        Assert.Equal(LookupErrorKind.NullKey, errors[2].Kind);
        Assert.Equal(4, errors[2].LookupSourceIndex);
    }

    [Fact]
    public void RequireUniqueKeysValidatesTheCompleteIndexAndReturnsASharedWrapper()
    {
        LookupIndex<string, Row> many = Build([
            new("a", "a0"),
            new("b", "b1"),
            new("b", "b2"),
        ]);

        var invalid = many.RequireUniqueKeys();
        Assert.True(invalid.TryGetErrors(out var errors));
        var error = Assert.Single(errors);
        Assert.Equal(2, error.LookupSourceIndex);
        Assert.Equal(1, error.FirstLookupSourceIndex);

        LookupIndex<string, Row> unique = Build([
            new("a", "a0"),
            new("b", "b1"),
        ]);
        var valid = unique.RequireUniqueKeys();
        Assert.True(valid.TryGetValue(out var uniqueIndex));
        Assert.Equal(2, uniqueIndex.Count);
        Assert.Equal("a0", uniqueIndex.GetOption("a").Match(static row => row.Value, static () => "none"));
        Assert.True(uniqueIndex.GetOption("missing").IsNone);
        Assert.Same(unique.Comparer, uniqueIndex.Comparer);
    }

    [Fact]
    public void NonDefaultComparerGroupsMatchesAndIsExposed()
    {
        var comparer = StringComparer.OrdinalIgnoreCase;
        LookupIndex<string, Row> caseInsensitive = Build(
            [
                new("C-1", "first"),
                new("c-1", "second"),
            ],
            comparer);

        Assert.Same(comparer, caseInsensitive.Comparer);
        Assert.Equal(1, caseInsensitive.DistinctCount);
        Assert.Equal(["first", "second"], caseInsensitive.GetMany("c-1").Select(static row => row.Value));
        Assert.True(caseInsensitive.GetExactlyOne("c-1").TryGetError(out var duplicate));
        Assert.Equal(LookupErrorKind.DuplicateKey, duplicate.Kind);

        LookupIndex<string, Row> ordinal = Build(
            [
                new("C-1", "first"),
                new("c-1", "second"),
            ],
            StringComparer.Ordinal);

        Assert.Equal(2, ordinal.DistinctCount);
        Assert.True(ordinal.GetExactlyOne("C-1").TryGetValue(out var upper));
        Assert.Equal("first", upper.Value);
    }

    [Fact]
    public void ToUniqueLookupIndexUsesTheExplicitComparerForCompleteInputUniqueness()
    {
        var rows = new[]
        {
            new Row("C-1", "first"),
            new Row("c-1", "second"),
        };

        var caseInsensitive = rows.ToUniqueLookupIndex(
            static row => row.Key,
            StringComparer.OrdinalIgnoreCase);
        Assert.True(caseInsensitive.TryGetErrors(out var errors));
        var error = Assert.Single(errors);
        Assert.Equal("C-1", error.Key);
        Assert.Equal(1, error.LookupSourceIndex);
        Assert.Equal(0, error.FirstLookupSourceIndex);

        var ordinal = rows.ToUniqueLookupIndex(static row => row.Key, StringComparer.Ordinal);
        Assert.True(ordinal.TryGetValue(out var index));
        Assert.Equal(2, index.Count);
        Assert.Equal("first", index.GetOption("C-1").Match(static row => row.Value, static () => "none"));
        Assert.Equal("second", index.GetOption("c-1").Match(static row => row.Value, static () => "none"));
    }

    [Fact]
    public void UniqueIndexUsesTheExplicitComparerNotAnOpaqueDictionaryPolicy()
    {
        IReadOnlyDictionary<string, string> dictionary =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["a"] = "value",
            };

        LookupIndex<string, KeyValuePair<string, string>> index = dictionary
            .ToLookupIndex(static pair => pair.Key, StringComparer.Ordinal)
            .Require();

        Assert.Same(StringComparer.Ordinal, index.Comparer);
        Assert.Equal(1, index.DistinctCount);
        Assert.True(index.GetExactlyOne("A").TryGetError(out var missing));
        Assert.Equal(LookupErrorKind.MissingKey, missing.Kind);
        Assert.True(index.GetExactlyOne("a").TryGetValue(out var pair));
        Assert.Equal("value", pair.Value);
    }

    [Fact]
    public void EmptySourceBuildsAnEmptyIndexAndEmptyStringIsAnOrdinaryKey()
    {
        LookupIndex<string, Row> empty = Build([]);
        Assert.Equal(0, empty.DistinctCount);
        Assert.Equal(0, empty.EntryCount);
        Assert.Empty(empty.GetMany(""));

        LookupIndex<string, Row> withEmptyKey = Build([new("", "empty-key")]);
        Assert.Equal(1, withEmptyKey.DistinctCount);
        Assert.True(withEmptyKey.GetExactlyOne("").TryGetValue(out var row));
        Assert.Equal("empty-key", row.Value);
    }

    [Fact]
    public void ToLookupIndexEnumeratesTheSourceOnceAndQueriesDoNotEnumerateAgain()
    {
        var probe = new ProbeEnumerable<Row>(
        [
            new("a", "first"),
            new("b", "second"),
            new("a", "third"),
        ]);

        LookupIndex<string, Row> index = probe.ToLookupIndex(static row => row.Key)
            .Require();

        Assert.Equal(1, probe.EnumeratorCount);
        Assert.Equal(3, probe.ItemsYielded);
        Assert.Equal(1, probe.DisposeCount);

        _ = index.GetMany("a");
        _ = index.GetZeroOrOne("b");
        _ = index.GetExactlyOne("missing");
        _ = index.RequireUniqueKeys();
        _ = index.GetMany("a");

        Assert.Equal(1, probe.EnumeratorCount);
        Assert.Equal(3, probe.ItemsYielded);
        Assert.Equal(1, probe.DisposeCount);
    }

    [Fact]
    public void IndexRetainsCallerValuesByReferenceAndDoesNotObserveLaterKeyChanges()
    {
        var row = new MutableRow("a", "first");
        LookupIndex<string, MutableRow> index = Build([row], static item => item.Key);

        row.Name = "changed";
        Assert.True(index.GetExactlyOne("a").TryGetValue(out var current));
        Assert.Equal("changed", current.Name);

        row.Key = "b";
        Assert.True(index.GetExactlyOne("a").TryGetValue(out var oldKey));
        Assert.Same(row, oldKey);
        Assert.True(index.GetExactlyOne("b").TryGetError(out var missing));
        Assert.Equal(LookupErrorKind.MissingKey, missing.Kind);
    }

    [Fact]
    public void GetZeroOrOneRejectsAPresentNullValueWhileGetManyAndGetExactlyOneRetainIt()
    {
        LookupIndex<string, string?> index = new string?[] { null }
            .ToLookupIndex(static _ => "a")
            .Require();

        var values = index.GetMany("a");
        Assert.Single(values);
        Assert.Null(values[0]);
        Assert.True(index.GetExactlyOne("a").TryGetValue(out var value));
        Assert.Null(value);
        Assert.Throws<ArgumentNullException>(() => index.GetZeroOrOne("a"));
    }

    [Fact]
    public void LookupErrorFactoriesPinEqualityPositionsAndRendering()
    {
        var first = LookupError<string>.DuplicateKey("C-2", 1, 3);
        var same = LookupError<string>.DuplicateKey("C-2", 1, 3);
        var different = LookupError<string>.DuplicateKey("C-2", 1, 4);

        Assert.Equal(first, same);
        Assert.NotEqual(first, different);
        Assert.Equal(LookupErrorKind.DuplicateKey, first.Kind);
        Assert.Equal("C-2", first.Key);
        Assert.Null(first.SourceIndex);
        Assert.Equal(3, first.LookupSourceIndex);
        Assert.Equal(1, first.FirstLookupSourceIndex);
        Assert.Equal("DuplicateKey 'C-2' at lookup[3] (first at lookup[1])", first.ToString());

        var missing = LookupError<string>.MissingKey("C-9", 4);
        Assert.Equal("MissingKey 'C-9' at source[4]", missing.ToString());

        var nullSource = LookupError<string>.NullSourceKey(2);
        Assert.Equal("NullKey at source[2]", nullSource.ToString());
        Assert.Null(nullSource.Key);

        Assert.Throws<ArgumentNullException>(() => LookupError<string>.MissingKey(null!, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => LookupError<string>.MissingKey("a", -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => LookupError<string>.NullSourceKey(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => LookupError<string>.NullLookupKey(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => LookupError<string>.DuplicateKey("a", -1, 0));
    }

    [Fact]
    public void UniqueGetOptionRejectsNullQueryKeyAndPresentNullValue()
    {
        UniqueLookupIndex<string, string?> index = new string?[] { null }
            .ToLookupIndex(static _ => "a")
            .Require()
            .RequireUniqueKeys()
            .Require();

        Assert.Throws<ArgumentNullException>(() => index.GetOption("a"));
        Assert.Throws<ArgumentNullException>(() => index.GetOption(null!));
    }

    [Fact]
    public void UniqueWrapperSharesStorageAndObservesValueMutation()
    {
        var row = new MutableRow("a", "first");
        UniqueLookupIndex<string, MutableRow> index = Build([row], static item => item.Key)
            .RequireUniqueKeys()
            .Require();

        row.Name = "changed";

        Assert.True(index.GetOption("a").TryGetValue(out var current));
        Assert.Same(row, current);
        Assert.Equal("changed", current!.Name);
    }

    [Fact]
    public void KeyedReadsOnThreeOccurrencesReportTheSecondLookupPosition()
    {
        LookupIndex<string, Row> index = Build([
            new("a", "first"),
            new("a", "second"),
            new("a", "third"),
        ]);

        Assert.True(index.GetExactlyOne("a").TryGetError(out var error));
        Assert.Equal(1, error.LookupSourceIndex);
        Assert.Equal(0, error.FirstLookupSourceIndex);
    }

    [Fact]
    public void GetManyReturnsTheRetainedReferencesWithoutCloning()
    {
        var first = new MutableRow("a", "first");
        var second = new MutableRow("a", "second");
        LookupIndex<string, MutableRow> index = Build([first, second], static row => row.Key);

        var values = index.GetMany("a");

        Assert.Same(first, values[0]);
        Assert.Same(second, values[1]);
    }

    [Theory]
    [InlineData("null-lookup", "NullKey at lookup[3]")]
    [InlineData("null-source", "NullKey at source[2]")]
    [InlineData("null-default", "NullKey")]
    [InlineData(
        "duplicate-source",
        "DuplicateKey 'C-2' at source[0] / lookup[3] (first at lookup[1])")]
    [InlineData("duplicate-lookup", "DuplicateKey 'C-2' at lookup[3] (first at lookup[1])")]
    [InlineData("missing-source", "MissingKey 'C-9' at source[4]")]
    [InlineData("missing-direct", "MissingKey 'C-9'")]
    public void LookupErrorToStringRendersEveryReachableBranch(string branch, string expected)
    {
        LookupError<string> error = branch switch
        {
            "null-lookup" => LookupError<string>.NullLookupKey(3),
            "null-source" => LookupError<string>.NullSourceKey(2),
            "null-default" => default,
            "duplicate-source" => LookupError<string>.DuplicateKey("C-2", 1, 3, 0),
            "duplicate-lookup" => LookupError<string>.DuplicateKey("C-2", 1, 3),
            "missing-source" => LookupError<string>.MissingKey("C-9", 4),
            "missing-direct" => LookupError<string>.MissingKey("C-9"),
            _ => throw new ArgumentOutOfRangeException(nameof(branch)),
        };

        Assert.Equal(expected, error.ToString());
    }

    [Fact]
    public void RequireUniqueKeysCalledTwiceReturnsEquivalentResults()
    {
        LookupIndex<string, Row> many = Build([
            new("a", "a0"),
            new("a", "a1"),
        ]);
        Assert.True(many.RequireUniqueKeys().TryGetErrors(out var firstErrors));
        Assert.True(many.RequireUniqueKeys().TryGetErrors(out var secondErrors));
        Assert.Equal(firstErrors, secondErrors);

        LookupIndex<string, Row> unique = Build([
            new("a", "a0"),
            new("b", "b1"),
        ]);
        Assert.True(unique.RequireUniqueKeys().TryGetValue(out var first));
        Assert.True(unique.RequireUniqueKeys().TryGetValue(out var second));
        Assert.Equal(first!.Count, second!.Count);
    }

    [Fact]
    public void ComparerExceptionsPropagateFromConstructionAndKeyedReads()
    {
        var comparer = new ThrowingKeyComparer("boom");
        var construction = Assert.Throws<InvalidOperationException>(
            () => new[] { new Row("boom", "x") }
                .ToLookupIndex(static row => row.Key, comparer));
        Assert.Same(comparer.Exception, construction);

        LookupIndex<string, Row> index = Build([new("a", "a0")], comparer);
        var read = Assert.Throws<InvalidOperationException>(() => index.GetExactlyOne("boom"));
        Assert.Same(comparer.Exception, read);
    }

    [Fact]
    public void ValueTypeKeyZeroIsAnOrdinaryKeyAndMissingReportsTheKey()
    {
        LookupIndex<int, (int Key, string Value)> index = new[]
        {
            (Key: 0, Value: "zero"),
            (Key: 1, Value: "one"),
        }.ToLookupIndex(static item => item.Key).Require();

        Assert.Equal(2, index.DistinctCount);
        Assert.True(index.GetExactlyOne(0).TryGetValue(out var zero) && zero.Value == "zero");
        Assert.True(index.GetExactlyOne(2).TryGetError(out var missing) && missing.Key == 2);
    }

    private static LookupIndex<string, Row> Build(
        IReadOnlyList<Row> rows,
        IEqualityComparer<string>? comparer = null) =>
        rows.ToLookupIndex(static row => row.Key, comparer)
            .Require();

    private static LookupIndex<string, MutableRow> Build(
        IReadOnlyList<MutableRow> rows,
        Func<MutableRow, string> keySelector) =>
        rows.ToLookupIndex(keySelector)
            .Require();

    [Fact]
    public void NullLookupKeysNeverReachTheComparerAndValidEntriesRemainIndexable()
    {
        var comparer = new NullRejectingStringComparer();
        var source = new (string? Key, string Value)[]
        {
            (null, "null-key"),
            ("a", "value"),
        };

        var invalid = source.ToLookupIndex(static item => item.Key!, comparer);
        Assert.True(invalid.TryGetErrors(out var errors));
        var error = Assert.Single(errors);
        Assert.Equal(LookupErrorKind.NullKey, error.Kind);
        Assert.Equal(0, error.LookupSourceIndex);
        Assert.Equal(0, comparer.NullCalls);

        var valid = source[1..].ToLookupIndex(static item => item.Key!, comparer).Require();
        Assert.Equal(1, valid.EntryCount);
        Assert.True(valid.GetExactlyOne("a").TryGetValue(out var row));
        Assert.Equal("value", row.Value);
        Assert.Equal(0, comparer.NullCalls);
    }

    private sealed record Row(string Key, string Value);

    private sealed class NullRejectingStringComparer : IEqualityComparer<string>
    {
        internal int NullCalls { get; private set; }

        public bool Equals(string? x, string? y)
        {
            if (x is null || y is null)
            {
                NullCalls++;
                throw new InvalidOperationException("The comparer received a null key.");
            }

            return StringComparer.Ordinal.Equals(x, y);
        }

        public int GetHashCode(string obj)
        {
            if (obj is null)
            {
                NullCalls++;
                throw new InvalidOperationException("The comparer received a null key.");
            }

            return StringComparer.Ordinal.GetHashCode(obj);
        }
    }

    private sealed class MutableRow(string key, string name)
    {
        internal string Key { get; set; } = key;

        internal string Name { get; set; } = name;
    }

    private sealed class ThrowingKeyComparer(string throwOn) : IEqualityComparer<string>
    {
        internal InvalidOperationException Exception { get; } = new("comparer failed");

        public bool Equals(string? x, string? y)
        {
            if (x == throwOn || y == throwOn)
            {
                throw Exception;
            }

            return StringComparer.Ordinal.Equals(x, y);
        }

        public int GetHashCode(string obj) =>
            obj == throwOn ? throw Exception : StringComparer.Ordinal.GetHashCode(obj);
    }
}
