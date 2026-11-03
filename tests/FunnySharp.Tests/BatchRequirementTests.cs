#pragma warning disable xUnit1051

using FunnySharp;

namespace FunnySharp.Tests;

public sealed class BatchRequirementTests
{
    [Fact]
    public async Task MapAndBindComposeSuccessfulValuesWithoutLosingTheRequestAssociation()
    {
        var customers = new RecordingBatchSource<string, string, string>(
            "customers",
            "tenant-a",
            StringComparer.Ordinal,
            static (keys, _, _) => new(Result<IReadOnlyList<BatchSourceEntry<string, string, string>>, string>.Success(
            [
                BatchSourceEntry<string, string, string>.Found("c1", "Customer One"),
            ])));
        var regions = new RecordingBatchSource<string, int, string>(
            "regions",
            "tenant-a",
            StringComparer.Ordinal,
            static (keys, _, _) => new(Result<IReadOnlyList<BatchSourceEntry<string, int, string>>, string>.Success(
            [
                BatchSourceEntry<string, int, string>.Found("r1", 42),
            ])));

        var requirement = BatchRequirement
            .Read(customers.Source, "c1")
            .Bind(customer => BatchRequirement
                .Read(regions.Source, "r1")
                .Map(region => $"{customer}:{region}"));

        await using var scope = BatchScope.Create();
        var outcome = await scope.ExecuteAsync(requirement);

        Assert.True(outcome.TryGetValue(out var value));
        Assert.Equal("Customer One:42", value);
        Assert.Equal(1, customers.InvocationCount);
        Assert.Equal(1, regions.InvocationCount);
        Assert.Equal(["c1"], customers.Batches[0]);
        Assert.Equal(["r1"], regions.Batches[0]);
    }

    [Fact]
    public async Task BindSkipsTheDependentReadWhenTheFirstReadIsMissing()
    {
        var customers = new RecordingBatchSource<string, string, string>(
            "customers",
            "tenant-a",
            StringComparer.Ordinal,
            static (keys, _, _) => new(Result<IReadOnlyList<BatchSourceEntry<string, string, string>>, string>.Success([])));
        var regions = new RecordingBatchSource<string, int, string>(
            "regions",
            "tenant-a",
            StringComparer.Ordinal,
            static (keys, _, _) => new(Result<IReadOnlyList<BatchSourceEntry<string, int, string>>, string>.Success([])));

        var requirement = BatchRequirement
            .Read(customers.Source, "missing")
            .Bind(_ => BatchRequirement.Read(regions.Source, "r1"));

        await using var scope = BatchScope.Create();
        var outcome = await scope.ExecuteAsync(requirement);

        Assert.True(outcome.TryGetError(out var error));
        Assert.Equal(BatchReadErrorKind.Missing, error.Kind);
        Assert.Equal(1, customers.InvocationCount);
        Assert.Equal(0, regions.InvocationCount);
    }

    [Fact]
    public async Task AllPreservesInputOrderAndKeepsIndependentFailuresAsValues()
    {
        var values = new RecordingBatchSource<string, string, string>(
            "values",
            "tenant-a",
            StringComparer.Ordinal,
            static (keys, _, _) => new(Result<IReadOnlyList<BatchSourceEntry<string, string, string>>, string>.Success(
            [
                BatchSourceEntry<string, string, string>.Found("first", "one"),
                BatchSourceEntry<string, string, string>.Failed("third", "third failed"),
            ])));

        var requirement = BatchRequirement.All(
        [
            BatchRequirement.Read(values.Source, "first"),
            BatchRequirement.Read(values.Source, "second"),
            BatchRequirement.Read(values.Source, "third"),
        ]);

        await using var scope = BatchScope.Create();
        var outer = await scope.ExecuteAsync(requirement);

        Assert.True(outer.TryGetValue(out var results));
        Assert.Equal(3, results.Count);
        Assert.True(results[0].TryGetValue(out var first));
        Assert.Equal("one", first);
        Assert.True(results[1].TryGetError(out var missing));
        Assert.Equal(BatchReadErrorKind.Missing, missing.Kind);
        Assert.True(results[2].TryGetError(out var failed));
        Assert.Equal(BatchReadErrorKind.KeyFailure, failed.Kind);
        Assert.True(failed.TryGetSourceError(out var sourceError));
        Assert.Equal("third failed", sourceError);
        Assert.Equal(1, values.InvocationCount);
        Assert.Equal(["first", "second", "third"], values.Batches[0]);
    }

    [Fact]
    public async Task ZipStartsBothIndependentReadsAndReportsTheFirstFailureInOperandOrder()
    {
        var first = new RecordingBatchSource<string, string, string>(
            "first",
            "tenant-a",
            StringComparer.Ordinal,
            static (keys, _, _) => new(Result<IReadOnlyList<BatchSourceEntry<string, string, string>>, string>.Success([])));
        var second = new RecordingBatchSource<string, string, string>(
            "second",
            "tenant-a",
            StringComparer.Ordinal,
            static (keys, _, _) => new(Result<IReadOnlyList<BatchSourceEntry<string, string, string>>, string>.Failure("second failed")));

        var requirement = BatchRequirement.Zip(
            BatchRequirement.Read(first.Source, "missing"),
            BatchRequirement.Read(second.Source, "value"));

        await using var scope = BatchScope.Create();
        var outcome = await scope.ExecuteAsync(requirement);

        Assert.True(outcome.TryGetError(out var error));
        Assert.Equal(BatchReadErrorKind.Missing, error.Kind);
        Assert.Equal(1, first.InvocationCount);
        Assert.Equal(1, second.InvocationCount);
    }

    [Fact]
    public async Task MapExceptionPropagatesAndSkipsTheDependentRead()
    {
        var expected = new InvalidOperationException("map failed");
        var first = new RecordingBatchSource<string, int, string>(
            "first",
            "tenant-a",
            StringComparer.Ordinal,
            static (keys, _, _) => new(Result<IReadOnlyList<BatchSourceEntry<string, int, string>>, string>.Success(
            [
                BatchSourceEntry<string, int, string>.Found("a", 1),
            ])));
        var dependent = new RecordingBatchSource<string, int, string>(
            "dependent",
            "tenant-a",
            StringComparer.Ordinal,
            static (keys, _, _) => new(Result<IReadOnlyList<BatchSourceEntry<string, int, string>>, string>.Success([])));

        var requirement = BatchRequirement
            .Read(first.Source, "a")
            .Map<int>(_ => throw expected)
            .Bind(_ => BatchRequirement.Read(dependent.Source, "b"));

        await using var scope = BatchScope.Create();
        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await scope.ExecuteAsync(requirement));

        Assert.Same(expected, thrown);
        Assert.Equal(1, first.InvocationCount);
        Assert.Equal(0, dependent.InvocationCount);
    }

    [Fact]
    public async Task ZipDoesNotHideASecondSourceExceptionBehindAFirstDomainFailure()
    {
        var expected = new InvalidOperationException("second source exploded");
        var first = new RecordingBatchSource<string, string, string>(
            "first",
            "tenant-a",
            StringComparer.Ordinal,
            static (keys, _, _) => new(Result<IReadOnlyList<BatchSourceEntry<string, string, string>>, string>.Success([])));
        var second = new RecordingBatchSource<string, string, string>(
            "second",
            "tenant-a",
            StringComparer.Ordinal,
            (keys, _, _) => throw expected);

        var requirement = BatchRequirement.Zip(
            BatchRequirement.Read(first.Source, "missing"),
            BatchRequirement.Read(second.Source, "value"));

        await using var scope = BatchScope.Create();
        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await scope.ExecuteAsync(requirement));

        Assert.Same(expected, thrown);
    }

}
