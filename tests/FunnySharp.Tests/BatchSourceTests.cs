#pragma warning disable xUnit1051

using FunnySharp;

namespace FunnySharp.Tests;

public sealed class BatchSourceTests
{
    [Theory]
    [InlineData("", "partition")]
    [InlineData(" ", "partition")]
    [InlineData("source", "")]
    [InlineData("source", " ")]
    public void SourceRejectsBlankNameOrPartition(string name, string partition)
    {
        Assert.Throws<ArgumentException>(
            () => new BatchSource<string, string, string>(
                name,
                partition,
                StringComparer.Ordinal,
                static (keys, _) => new(Result<IReadOnlyList<BatchSourceEntry<string, string, string>>, string>.Success([]))));
    }

    [Fact]
    public void SourceRejectsNullComparerAndDelegate()
    {
        Assert.Throws<ArgumentNullException>(
            () => new BatchSource<string, string, string>(
                "source",
                "partition",
                null!,
                static (keys, _) => new(Result<IReadOnlyList<BatchSourceEntry<string, string, string>>, string>.Success([]))));

        Assert.Throws<ArgumentNullException>(
            () => new BatchSource<string, string, string>(
                "source",
                "partition",
                StringComparer.Ordinal,
                null!));
    }

    [Fact]
    public void SourceExposesItsExplicitIdentityAndEqualityPolicy()
    {
        var comparer = StringComparer.OrdinalIgnoreCase;
        var source = new BatchSource<string, string, string>(
            "customers",
            "tenant-a/user-7",
            comparer,
            static (keys, _) => new(Result<IReadOnlyList<BatchSourceEntry<string, string, string>>, string>.Success([])));

        Assert.Equal("customers", source.Name);
        Assert.Equal("tenant-a/user-7", source.Partition);
        Assert.Same(comparer, source.KeyComparer);
        Assert.Equal("customers[tenant-a/user-7]", source.ToString());
    }

    [Fact]
    public void ReadRejectsNullSourceAndKey()
    {
        var source = new BatchSource<string, string, string>(
            "customers",
            "tenant-a",
            StringComparer.Ordinal,
            static (keys, _) => new(Result<IReadOnlyList<BatchSourceEntry<string, string, string>>, string>.Success([])));

        Assert.Throws<ArgumentNullException>(
            () => BatchRequirement.Read<string, string, string>(null!, "key"));
        Assert.Throws<ArgumentNullException>(
            () => BatchRequirement.Read(source, null!));
    }

    [Fact]
    public void AllRejectsNullSequencesAndNullRequirements()
    {
        Assert.Throws<ArgumentNullException>(
            () => BatchRequirement.All<string, string>(null!));
        Assert.Throws<ArgumentNullException>(
            () => BatchRequirement.All(new BatchRequirement<string, string>[] { null! }));
    }

    [Fact]
    public void PlanCompositionRejectsNullCallbacks()
    {
        var requirement = BatchRequirement.FromValue<int, string>(1);

        Assert.Throws<ArgumentNullException>(() => requirement.Map<int>(null!));
        Assert.Throws<ArgumentNullException>(() => requirement.Bind<int>(null!));
        Assert.Throws<ArgumentNullException>(
            () => BatchRequirement.Zip<int, int, string>(null!, requirement));
        Assert.Throws<ArgumentNullException>(
            () => BatchRequirement.Zip<int, int, string>(requirement, null!));
    }

    [Fact]
    public void ReadErrorFactoriesExposeTheirDistinctKinds()
    {
        var missing = BatchReadError<string>.Missing();
        var keyFailure = BatchReadError<string>.KeyFailure("key failed");
        var batchFailure = BatchReadError<string>.BatchFailure("batch failed");
        var unexpected = BatchReadError<string>.UnexpectedResponse("a duplicate key");

        Assert.Equal(BatchReadErrorKind.Missing, missing.Kind);
        Assert.False(missing.TryGetSourceError(out _));
        Assert.Equal(BatchReadErrorKind.KeyFailure, keyFailure.Kind);
        Assert.True(keyFailure.TryGetSourceError(out var keyError));
        Assert.Equal("key failed", keyError);
        Assert.Equal(BatchReadErrorKind.BatchFailure, batchFailure.Kind);
        Assert.True(batchFailure.TryGetSourceError(out var batchError));
        Assert.Equal("batch failed", batchError);
        Assert.Equal(BatchReadErrorKind.UnexpectedResponse, unexpected.Kind);
        Assert.Equal("a duplicate key", unexpected.Detail);
        Assert.False(unexpected.TryGetSourceError(out _));
        Assert.NotEqual(keyFailure, batchFailure);
        Assert.Throws<ArgumentException>(() => BatchReadError<string>.UnexpectedResponse(""));
    }

    [Fact]
    public async Task MapErrorAdaptsPerKeyAndWholeBatchFailuresWithoutTouchingMissingOutcomes()
    {
        var source = new BatchSource<string, int, string>(
            "values",
            "tenant-a",
            StringComparer.Ordinal,
            static (keys, _) => new(Result<IReadOnlyList<BatchSourceEntry<string, int, string>>, string>.Success(
                new BatchSourceEntry<string, int, string>[]
                {
                    BatchSourceEntry<string, int, string>.Found("a", 1),
                    BatchSourceEntry<string, int, string>.Failed("b", "bad key"),
                }.Where(entry => keys.Contains(entry.Key, StringComparer.Ordinal)).ToArray())));
        var adapted = source.MapError(static error => error.Length);
        var mappings = 0;
        var counted = source.MapError(error =>
        {
            mappings++;
            return error.Length;
        });

        var requirement = BatchRequirement.All(
        [
            BatchRequirement.Read(adapted, "a"),
            BatchRequirement.Read(adapted, "b"),
            BatchRequirement.Read(counted, "missing"),
        ]);

        await using var scope = BatchScope.Create();
        var outer = await scope.ExecuteAsync(requirement);

        Assert.True(outer.TryGetValue(out var results));
        Assert.True(results[0].TryGetValue(out var value));
        Assert.Equal(1, value);
        Assert.True(results[1].TryGetError(out var keyError));
        Assert.Equal(BatchReadErrorKind.KeyFailure, keyError.Kind);
        Assert.True(keyError.TryGetSourceError(out var mappedError));
        Assert.Equal(7, mappedError);
        Assert.True(results[2].TryGetError(out var missing));
        Assert.Equal(BatchReadErrorKind.Missing, missing.Kind);
        Assert.Equal(0, mappings);
    }

    [Fact]
    public async Task MapErrorAdaptsAWholeBatchFailure()
    {
        var source = new BatchSource<string, int, string>(
            "values",
            "tenant-a",
            StringComparer.Ordinal,
            static (keys, _) => new(Result<IReadOnlyList<BatchSourceEntry<string, int, string>>, string>.Failure("broken")));

        await using var scope = BatchScope.Create();
        var outcome = await scope.ExecuteAsync(BatchRequirement.Read(source.MapError(static error => error.Length), "a"));

        Assert.True(outcome.TryGetError(out var error));
        Assert.Equal(BatchReadErrorKind.BatchFailure, error.Kind);
        Assert.True(error.TryGetSourceError(out var mappedError));
        Assert.Equal(6, mappedError);
    }

    [Fact]
    public async Task MapErrorPreservesANullResponseAsUnexpected()
    {
        var source = new BatchSource<string, int, string>(
            "values",
            "tenant-a",
            StringComparer.Ordinal,
            static (keys, _) => new(Result<IReadOnlyList<BatchSourceEntry<string, int, string>>, string>.Success(
                null!)));
        var mappings = 0;

        await using var scope = BatchScope.Create();
        var outcome = await scope.ExecuteAsync(
            BatchRequirement.Read(
                source.MapError(error =>
                {
                    mappings++;
                    return error.Length;
                }),
                "a"));

        Assert.True(outcome.TryGetError(out var error));
        Assert.Equal(BatchReadErrorKind.UnexpectedResponse, error.Kind);
        Assert.Equal(0, mappings);
    }

    [Fact]
    public void DefaultSourceEntryIsUnsetAndNotAFoundOrFailedEntry()
    {
        var entry = default(BatchSourceEntry<string, int, string>);

        Assert.Equal(BatchSourceEntryKind.Unset, entry.Kind);
        Assert.False(entry.TryGetValue(out _));
        Assert.False(entry.TryGetError(out _));
        Assert.Equal("Unset", entry.ToString());
    }
}
