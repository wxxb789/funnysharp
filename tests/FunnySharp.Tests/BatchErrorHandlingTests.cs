#pragma warning disable xUnit1051

using FunnySharp;
using FunnySharp.TestSupport.Batching;

namespace FunnySharp.Tests;

public sealed class BatchErrorHandlingTests
{
    [Fact]
    public async Task WholeBatchFailureMarksEveryKeyInThatBatchAndLeavesAnotherBatchUntouched()
    {
        var values = new RecordingBatchSource<string, int, string>(
            "values",
            "tenant-a",
            StringComparer.Ordinal,
            static (keys, invocation, _) => invocation == 1
                ? new(Result<IReadOnlyList<BatchSourceEntry<string, int, string>>, string>.Failure("batch unavailable"))
                : new(Result<IReadOnlyList<BatchSourceEntry<string, int, string>>, string>.Success(
                [
                    BatchSourceEntry<string, int, string>.Found("c", 3),
                ])));

        var requirement = BatchRequirement.All(
        [
            BatchRequirement.Read(values.Source, "a"),
            BatchRequirement.Read(values.Source, "b"),
            BatchRequirement.Read(values.Source, "c"),
        ]);

        await using var scope = BatchScope.Create(new BatchScopeOptions { MaxBatchSize = 2 });
        var outer = await scope.ExecuteAsync(requirement);

        Assert.True(outer.TryGetValue(out var results));
        Assert.Equal(2, values.InvocationCount);
        foreach (var index in new[] { 0, 1 })
        {
            Assert.True(results[index].TryGetError(out var error));
            Assert.Equal(BatchReadErrorKind.BatchFailure, error.Kind);
            Assert.True(error.TryGetSourceError(out var sourceError));
            Assert.Equal("batch unavailable", sourceError);
        }

        Assert.True(results[2].TryGetValue(out var c));
        Assert.Equal(3, c);
    }

    [Theory]
    [InlineData(BatchUnexpectedShape.Duplicate, "more than once")]
    [InlineData(BatchUnexpectedShape.Unknown, "not requested")]
    [InlineData(BatchUnexpectedShape.Unset, "uninitialized")]
    [InlineData(BatchUnexpectedShape.NullList, "null entry collection")]
    [InlineData(BatchUnexpectedShape.TooMany, "more entries than requested")]
    public async Task UnexpectedSourceResponsesBecomeOneTypedOutcomeForEveryKey(
        BatchUnexpectedShape shape,
        string expectedDetail)
    {
        var values = new RecordingBatchSource<string, int, string>(
            "values",
            "tenant-a",
            StringComparer.Ordinal,
            (keys, _, _) => shape switch
            {
                BatchUnexpectedShape.Duplicate => new(Result<IReadOnlyList<BatchSourceEntry<string, int, string>>, string>.Success(
                [
                    BatchSourceEntry<string, int, string>.Found("a", 1),
                    BatchSourceEntry<string, int, string>.Found("a", 2),
                ])),
                BatchUnexpectedShape.Unknown => new(Result<IReadOnlyList<BatchSourceEntry<string, int, string>>, string>.Success(
                [
                    BatchSourceEntry<string, int, string>.Found("other", 2),
                ])),
                BatchUnexpectedShape.Unset => new(Result<IReadOnlyList<BatchSourceEntry<string, int, string>>, string>.Success(
                [
                    default(BatchSourceEntry<string, int, string>),
                ])),
                BatchUnexpectedShape.TooMany => new(Result<IReadOnlyList<BatchSourceEntry<string, int, string>>, string>.Success(
                [
                    BatchSourceEntry<string, int, string>.Found("a", 1),
                    BatchSourceEntry<string, int, string>.Found("b", 2),
                    BatchSourceEntry<string, int, string>.Found("a", 3),
                ])),
                _ => new(Result<IReadOnlyList<BatchSourceEntry<string, int, string>>, string>.Success(
                    null!)),
            });

        var requirement = BatchRequirement.All(
        [
            BatchRequirement.Read(values.Source, "a"),
            BatchRequirement.Read(values.Source, "b"),
        ]);

        await using var scope = BatchScope.Create();
        var outer = await scope.ExecuteAsync(requirement);

        Assert.True(outer.TryGetValue(out var results));
        Assert.Equal(2, results.Count);
        foreach (var result in results)
        {
            Assert.True(result.TryGetError(out var error));
            Assert.Equal(BatchReadErrorKind.UnexpectedResponse, error.Kind);
            Assert.Contains(expectedDetail, error.Detail, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task SourceExceptionPropagatesAndIndependentStartedWorkIsStillObserved()
    {
        var expected = new InvalidOperationException("source exploded");
        var failing = new RecordingBatchSource<string, int, string>(
            "failing",
            "tenant-a",
            StringComparer.Ordinal,
            (keys, _, _) => throw expected);
        var healthy = new RecordingBatchSource<string, int, string>(
            "healthy",
            "tenant-a",
            StringComparer.Ordinal,
            static (keys, _, _) => new(Result<IReadOnlyList<BatchSourceEntry<string, int, string>>, string>.Success(
            [
                BatchSourceEntry<string, int, string>.Found("b", 2),
            ])));

        var requirement = BatchRequirement.All(
        [
            BatchRequirement.Read(failing.Source, "a"),
            BatchRequirement.Read(healthy.Source, "b"),
        ]);

        await using var scope = BatchScope.Create();
        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await scope.ExecuteAsync(requirement));

        Assert.Same(expected, thrown);
        Assert.Equal(1, failing.InvocationCount);
        Assert.Equal(1, healthy.InvocationCount);
    }

    [Fact]
    public async Task MissingRecordsAreCachedAsNegativeResultsWithinTheScope()
    {
        var values = new RecordingBatchSource<string, int, string>(
            "values",
            "tenant-a",
            StringComparer.Ordinal,
            static (keys, _, _) => new(Result<IReadOnlyList<BatchSourceEntry<string, int, string>>, string>.Success([])));

        await using var scope = BatchScope.Create();
        var first = await scope.ExecuteAsync(BatchRequirement.Read(values.Source, "missing"));
        var second = await scope.ExecuteAsync(BatchRequirement.Read(values.Source, "missing"));

        Assert.True(first.TryGetError(out var firstError));
        Assert.True(second.TryGetError(out var secondError));
        Assert.Equal(BatchReadErrorKind.Missing, firstError.Kind);
        Assert.Equal(firstError, secondError);
        Assert.Equal(1, values.InvocationCount);
    }

    [Fact]
    public async Task PerKeyBatchAndUnexpectedFailureOutcomesAreCachedWithinTheScope()
    {
        var perKey = new RecordingBatchSource<string, int, string>(
            "per-key",
            "tenant-a",
            StringComparer.Ordinal,
            static (keys, _, _) => new(Result<IReadOnlyList<BatchSourceEntry<string, int, string>>, string>.Success(
            [
                BatchSourceEntry<string, int, string>.Failed("a", "key unavailable"),
            ])));
        var wholeBatch = new RecordingBatchSource<string, int, string>(
            "whole-batch",
            "tenant-a",
            StringComparer.Ordinal,
            static (keys, _, _) => new(Result<IReadOnlyList<BatchSourceEntry<string, int, string>>, string>.Failure("batch unavailable")));
        var unexpected = new RecordingBatchSource<string, int, string>(
            "unexpected",
            "tenant-a",
            StringComparer.Ordinal,
            static (keys, _, _) => new(Result<IReadOnlyList<BatchSourceEntry<string, int, string>>, string>.Success(
            [
                BatchSourceEntry<string, int, string>.Found("a", 1),
                BatchSourceEntry<string, int, string>.Found("a", 2),
            ])));

        await using var scope = BatchScope.Create();

        var firstKey = await scope.ExecuteAsync(BatchRequirement.Read(perKey.Source, "a"));
        var secondKey = await scope.ExecuteAsync(BatchRequirement.Read(perKey.Source, "a"));
        Assert.True(firstKey.TryGetError(out var firstKeyError));
        Assert.True(secondKey.TryGetError(out var secondKeyError));
        Assert.Equal(BatchReadErrorKind.KeyFailure, firstKeyError.Kind);
        Assert.Equal(firstKeyError, secondKeyError);
        Assert.Equal(1, perKey.InvocationCount);

        var firstBatch = await scope.ExecuteAsync(BatchRequirement.Read(wholeBatch.Source, "a"));
        var secondBatch = await scope.ExecuteAsync(BatchRequirement.Read(wholeBatch.Source, "a"));
        Assert.True(firstBatch.TryGetError(out var firstBatchError));
        Assert.True(secondBatch.TryGetError(out var secondBatchError));
        Assert.Equal(BatchReadErrorKind.BatchFailure, firstBatchError.Kind);
        Assert.Equal(firstBatchError, secondBatchError);
        Assert.Equal(1, wholeBatch.InvocationCount);

        var firstUnexpected = await scope.ExecuteAsync(BatchRequirement.Read(unexpected.Source, "a"));
        var secondUnexpected = await scope.ExecuteAsync(BatchRequirement.Read(unexpected.Source, "a"));
        Assert.True(firstUnexpected.TryGetError(out var firstUnexpectedError));
        Assert.True(secondUnexpected.TryGetError(out var secondUnexpectedError));
        Assert.Equal(BatchReadErrorKind.UnexpectedResponse, firstUnexpectedError.Kind);
        Assert.Equal(firstUnexpectedError, secondUnexpectedError);
        Assert.Equal(1, unexpected.InvocationCount);
    }

    [Fact]
    public void EntryFactoriesRejectInvalidFoundAndFailedValues()
    {
        Assert.Throws<ArgumentNullException>(
            () => BatchSourceEntry<string, string, string>.Found(null!, "value"));
        Assert.Throws<ArgumentNullException>(
            () => BatchSourceEntry<string, string, string>.Found("key", null!));
        Assert.Throws<ArgumentNullException>(
            () => BatchSourceEntry<string, string, string>.Failed(null!, "error"));
    }
}
