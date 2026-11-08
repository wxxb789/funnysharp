#pragma warning disable xUnit1051

using System.Runtime.CompilerServices;
using FunnySharp.TestSupport.FallibleReduction;

namespace FunnySharp.Tests;

public sealed class NestedDataAsyncTests
{
    [Fact]
    public async Task ExpandAsyncMatchesTheSynchronousRowsAndOrigins()
    {
        var records = new AsyncReductionProbe<AsyncExpansionRecord>(
        [
            new AsyncExpansionRecord("A", ["a1", "a2"]),
            new AsyncExpansionRecord("B", []),
            new AsyncExpansionRecord("C", ["c1"]),
        ]);
        var origins = new List<RowOrigin>();

        var result = await records.ExpandAsync(
            static record => record.Children,
            (record, child, origin) =>
            {
                origins.Add(origin);
                return $"{record.Id}:{child}";
            });

        Assert.True(result.TryGetValue(out var rows));
        Assert.Equal(["A:a1", "A:a2", "C:c1"], rows!);
        Assert.Equal(new[] { 0, 0, 2 }, origins.Select(static origin => origin.RecordIndex));
        Assert.Equal(new[] { 0, 1, 0 }, origins.Select(static origin => origin.ElementIndex));
        Assert.Equal(1, records.GetAsyncEnumeratorCalls);
        Assert.Equal(4, records.MoveNextAsyncCalls);
        Assert.Equal(3, records.CurrentReads);
        Assert.Equal(1, records.DisposeAsyncCalls);
    }

    [Fact]
    public async Task ExpandAsyncEmptySourceProducesAnEmptySuccess()
    {
        var records = new AsyncReductionProbe<AsyncExpansionRecord>([]);

        var result = await records.ExpandAsync(
            static record => record.Children,
            static (record, child, _) => $"{record.Id}:{child}");

        Assert.True(result.TryGetValue(out var rows));
        Assert.Empty(rows!);
        Assert.Equal(1, records.MoveNextAsyncCalls);
        Assert.Equal(1, records.DisposeAsyncCalls);
    }

    [Fact]
    public async Task ExpandAsyncReportsNullChildListsAndDisposesTheSource()
    {
        var records = new AsyncReductionProbe<AsyncExpansionRecord>(
        [
            new AsyncExpansionRecord("A", ["a1"]),
            new AsyncExpansionRecord("B", null!),
        ]);

        var result = await records.ExpandAsync(
            static record => record.Children,
            static (record, child, _) => $"{record.Id}:{child}");

        Assert.True(result.TryGetErrors(out var errors));
        Assert.Equal([NestedRowError.NullChildSequence(1)], errors!);
        Assert.Equal(1, records.DisposeAsyncCalls);
    }

    [Fact]
    public async Task ExpandAlignedAsyncMatchesTheSynchronousMismatch()
    {
        var records = new AsyncReductionProbe<AsyncAlignmentRecord>(
        [
            new AsyncAlignmentRecord("A", ["a1", "a2"], [10, 20]),
            new AsyncAlignmentRecord("B", ["b1"], [30, 40]),
        ]);

        var result = await records.ExpandAlignedAsync(
            static record => record.First,
            static record => record.Second,
            static (record, first, second, _) => $"{record.Id}:{first}:{second}");

        Assert.True(result.TryGetErrors(out var errors));
        Assert.Equal([NestedRowError.ChildCountMismatch(1, 1, 2)], errors!);
        Assert.Equal(1, records.DisposeAsyncCalls);
    }

    [Fact]
    public async Task ExpandAlignedAsyncProducesTheSameAlignedRowsAsTheSynchronousForm()
    {
        var records = new AsyncReductionProbe<AsyncAlignmentRecord>(
        [
            new AsyncAlignmentRecord("A", ["a1", "a2"], [10, 20]),
            new AsyncAlignmentRecord("B", ["b1"], [30]),
        ]);
        var origins = new List<RowOrigin>();

        var result = await records.ExpandAlignedAsync(
            static record => record.First,
            static record => record.Second,
            (record, first, second, origin) =>
            {
                origins.Add(origin);
                return $"{record.Id}:{first}:{second}";
            });

        Assert.True(result.TryGetValue(out var rows));
        Assert.Equal(["A:a1:10", "A:a2:20", "B:b1:30"], rows!);
        Assert.Equal(new[] { 0, 0, 1 }, origins.Select(static origin => origin.RecordIndex));
        Assert.Equal(new[] { 0, 1, 0 }, origins.Select(static origin => origin.ElementIndex));
        Assert.Equal(1, records.DisposeAsyncCalls);
    }

    [Fact]
    public async Task ExpandCartesianAsyncProducesTheExplicitProduct()
    {
        var records = new AsyncReductionProbe<AsyncAlignmentRecord>(
        [
            new AsyncAlignmentRecord("A", ["S", "M"], [10, 20]),
        ]);
        var origins = new List<CartesianRowOrigin>();

        var result = await records.ExpandCartesianAsync(
            static record => record.First,
            static record => record.Second,
            (record, first, second, origin) =>
            {
                origins.Add(origin);
                return $"{record.Id}:{first}:{second}";
            });

        Assert.True(result.TryGetValue(out var rows));
        Assert.Equal(
            ["A:S:10", "A:S:20", "A:M:10", "A:M:20"],
            rows!);
        Assert.Equal(new[] { 0, 0, 0, 0 }, origins.Select(static origin => origin.RecordIndex));
        Assert.Equal(new[] { 0, 0, 1, 1 }, origins.Select(static origin => origin.FirstElementIndex));
        Assert.Equal(new[] { 0, 1, 0, 1 }, origins.Select(static origin => origin.SecondElementIndex));
        Assert.Equal(1, records.DisposeAsyncCalls);
    }

    [Fact]
    public async Task AsyncFormsForwardTheExactCancellationTokenToTheEnumerator()
    {
        using var cancellationSource = new CancellationTokenSource();
        var records = new AsyncReductionProbe<AsyncExpansionRecord>(
        [
            new AsyncExpansionRecord("A", ["a1"]),
        ]);

        var result = await records.ExpandAsync(
            static record => record.Children,
            static (record, child, _) => $"{record.Id}:{child}",
            cancellationSource.Token);

        Assert.True(result.TryGetValue(out _));
        Assert.Equal(cancellationSource.Token, records.Token);
    }

    [Fact]
    public async Task AsyncFormsPropagateCancellationWithoutConvertingIt()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await CancellableRecords(cancellationSource.Token).ExpandAsync(
                static record => record.Children,
                static (record, child, _) => $"{record.Id}:{child}",
                cancellationSource.Token));
    }

    [Fact]
    public async Task AsyncFormsPropagateSourceFaultsUnchanged()
    {
        var expected = new InvalidOperationException("source");
        var records = new AsyncReductionProbe<AsyncExpansionRecord>(
            [new AsyncExpansionRecord("A", ["a1"])],
            faultAtIndex: 0,
            fault: expected);

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await records.ExpandAsync(
                static record => record.Children,
                static (record, child, _) => $"{record.Id}:{child}"));

        Assert.Same(expected, thrown);
        Assert.Equal(1, records.DisposeAsyncCalls);
    }

    [Fact]
    public async Task AsyncFormsPropagateSelectorAndCombineExceptionsUnchanged()
    {
        var selectorException = new InvalidOperationException("selector");
        var combineException = new InvalidOperationException("combine");
        var records = new AsyncReductionProbe<AsyncExpansionRecord>(
        [
            new AsyncExpansionRecord("A", ["a1"]),
        ]);

        var selectorThrown = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await records.ExpandAsync<AsyncExpansionRecord, string, string>(
                (AsyncExpansionRecord _) => throw selectorException,
                static (record, child, _) => $"{record.Id}:{child}"));
        var combineThrown = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await records.ExpandAsync<AsyncExpansionRecord, string, string>(
                static record => record.Children,
                (AsyncExpansionRecord _, string _, RowOrigin _) => throw combineException));

        Assert.Same(selectorException, selectorThrown);
        Assert.Same(combineException, combineThrown);
    }

    [Fact]
    public async Task AlignedAndCartesianAsyncFormsPropagateCancellationWithoutConvertingIt()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await CancellableAlignmentRecords(cancellationSource.Token)
                .ExpandAlignedAsync(
                    static record => record.First,
                    static record => record.Second,
                    static (record, first, second, _) => $"{record.Id}:{first}:{second}",
                    cancellationSource.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await CancellableAlignmentRecords(cancellationSource.Token)
                .ExpandCartesianAsync(
                    static record => record.First,
                    static record => record.Second,
                    static (record, first, second, _) => $"{record.Id}:{first}:{second}",
                    cancellationSource.Token));
    }

    [Fact]
    public async Task AlignedAndCartesianAsyncFormsPropagateSourceFaultsUnchanged()
    {
        var expected = new InvalidOperationException("source");
        var alignedRecords = new AsyncReductionProbe<AsyncAlignmentRecord>(
            [new AsyncAlignmentRecord("A", ["a1"], [1])],
            faultAtIndex: 0,
            fault: expected);
        var cartesianRecords = new AsyncReductionProbe<AsyncAlignmentRecord>(
            [new AsyncAlignmentRecord("A", ["a1"], [1])],
            faultAtIndex: 0,
            fault: expected);

        var alignedThrown = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await alignedRecords.ExpandAlignedAsync(
                static record => record.First,
                static record => record.Second,
                static (record, first, second, _) => $"{record.Id}:{first}:{second}"));
        var cartesianThrown = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await cartesianRecords.ExpandCartesianAsync(
                static record => record.First,
                static record => record.Second,
                static (record, first, second, _) => $"{record.Id}:{first}:{second}"));

        Assert.Same(expected, alignedThrown);
        Assert.Same(expected, cartesianThrown);
        Assert.Equal(1, alignedRecords.DisposeAsyncCalls);
        Assert.Equal(1, cartesianRecords.DisposeAsyncCalls);
    }

    [Fact]
    public async Task AlignedAndCartesianAsyncFormsPropagateSelectorAndCombineExceptionsUnchanged()
    {
        var selectorException = new InvalidOperationException("selector");
        var combineException = new InvalidOperationException("combine");
        var records = new AsyncReductionProbe<AsyncAlignmentRecord>(
        [
            new AsyncAlignmentRecord("A", ["a1"], [1]),
        ]);

        var alignedSelectorThrown = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await records.ExpandAlignedAsync<AsyncAlignmentRecord, string, int, string>(
                (AsyncAlignmentRecord _) => throw selectorException,
                static record => record.Second,
                static (record, first, second, _) => $"{record.Id}:{first}:{second}"));
        var alignedCombineThrown = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await records.ExpandAlignedAsync<AsyncAlignmentRecord, string, int, string>(
                static record => record.First,
                static record => record.Second,
                (AsyncAlignmentRecord _, string _, int _, RowOrigin _) => throw combineException));
        var cartesianSelectorThrown = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await records.ExpandCartesianAsync<AsyncAlignmentRecord, string, int, string>(
                (AsyncAlignmentRecord _) => throw selectorException,
                static record => record.Second,
                static (record, first, second, _) => $"{record.Id}:{first}:{second}"));
        var cartesianCombineThrown = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await records.ExpandCartesianAsync<AsyncAlignmentRecord, string, int, string>(
                static record => record.First,
                static record => record.Second,
                (AsyncAlignmentRecord _, string _, int _, CartesianRowOrigin _) => throw combineException));

        Assert.Same(selectorException, alignedSelectorThrown);
        Assert.Same(combineException, alignedCombineThrown);
        Assert.Same(selectorException, cartesianSelectorThrown);
        Assert.Same(combineException, cartesianCombineThrown);
    }

    [Fact]
    public async Task ExpandCartesianAsyncDisposesTheSourceOnStructuralFailure()
    {
        var records = new AsyncReductionProbe<AsyncAlignmentRecord>(
        [
            new AsyncAlignmentRecord("A", ["a1"], null!),
        ]);

        var result = await records.ExpandCartesianAsync(
            static record => record.First,
            static record => record.Second,
            static (record, first, second, _) => $"{record.Id}:{first}:{second}");

        Assert.True(result.TryGetErrors(out var errors));
        Assert.Equal([NestedRowError.NullSecondChildSequence(0)], errors!);
        Assert.Equal(1, records.DisposeAsyncCalls);
    }

    [Fact]
    public void AsyncFormsRejectNullArgumentsEagerly()
    {
        Assert.Throws<ArgumentNullException>(
            static () => NestedDataExtensions.ExpandAsync(
                (IAsyncEnumerable<AsyncExpansionRecord>)null!,
                static record => record.Children,
                static (record, child, _) => $"{record.Id}:{child}"));
        Assert.Throws<ArgumentNullException>(
            static () => AsyncRecords().ExpandAsync(
                (Func<AsyncExpansionRecord, IReadOnlyList<string>>)null!,
                static (record, child, _) => $"{record.Id}:{child}"));
        Assert.Throws<ArgumentNullException>(
            static () => AsyncRecords().ExpandAsync(
                static record => record.Children,
                (Func<AsyncExpansionRecord, string, RowOrigin, string>)null!));
        Assert.Throws<ArgumentNullException>(
            static () => NestedDataExtensions.ExpandAlignedAsync(
                (IAsyncEnumerable<AsyncAlignmentRecord>)null!,
                static record => record.First,
                static record => record.Second,
                static (record, first, second, _) => $"{record.Id}:{first}:{second}"));
        Assert.Throws<ArgumentNullException>(
            static () => AsyncAlignmentRecords().ExpandAlignedAsync(
                (Func<AsyncAlignmentRecord, IReadOnlyList<string>>)null!,
                static record => record.Second,
                static (record, first, second, _) => $"{record.Id}:{first}:{second}"));
        Assert.Throws<ArgumentNullException>(
            static () => AsyncAlignmentRecords().ExpandAlignedAsync(
                static record => record.First,
                (Func<AsyncAlignmentRecord, IReadOnlyList<int>>)null!,
                static (record, first, second, _) => $"{record.Id}:{first}:{second}"));
        Assert.Throws<ArgumentNullException>(
            static () => AsyncAlignmentRecords().ExpandAlignedAsync(
                static record => record.First,
                static record => record.Second,
                (Func<AsyncAlignmentRecord, string, int, RowOrigin, string>)null!));
        Assert.Throws<ArgumentNullException>(
            static () => NestedDataExtensions.ExpandCartesianAsync(
                (IAsyncEnumerable<AsyncAlignmentRecord>)null!,
                static record => record.First,
                static record => record.Second,
                static (record, first, second, _) => $"{record.Id}:{first}:{second}"));
        Assert.Throws<ArgumentNullException>(
            static () => AsyncAlignmentRecords().ExpandCartesianAsync(
                (Func<AsyncAlignmentRecord, IReadOnlyList<string>>)null!,
                static record => record.Second,
                static (record, first, second, _) => $"{record.Id}:{first}:{second}"));
        Assert.Throws<ArgumentNullException>(
            static () => AsyncAlignmentRecords().ExpandCartesianAsync(
                static record => record.First,
                (Func<AsyncAlignmentRecord, IReadOnlyList<int>>)null!,
                static (record, first, second, _) => $"{record.Id}:{first}:{second}"));
        Assert.Throws<ArgumentNullException>(
            static () => AsyncAlignmentRecords().ExpandCartesianAsync(
                static record => record.First,
                static record => record.Second,
                (Func<AsyncAlignmentRecord, string, int, CartesianRowOrigin, string>)null!));
    }

    private static async IAsyncEnumerable<AsyncExpansionRecord> AsyncRecords(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        yield return new AsyncExpansionRecord("A", ["a1"]);
        cancellationToken.ThrowIfCancellationRequested();
    }

    private static async IAsyncEnumerable<AsyncAlignmentRecord> AsyncAlignmentRecords(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        yield return new AsyncAlignmentRecord("A", ["a1"], [1]);
        cancellationToken.ThrowIfCancellationRequested();
    }

    private static async IAsyncEnumerable<AsyncExpansionRecord> CancellableRecords(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.Yield();
        yield return new AsyncExpansionRecord("A", ["a1"]);
    }

    private static async IAsyncEnumerable<AsyncAlignmentRecord> CancellableAlignmentRecords(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.Yield();
        yield return new AsyncAlignmentRecord("A", ["a1"], [1]);
    }

    internal sealed record AsyncExpansionRecord(string Id, IReadOnlyList<string> Children);

    internal sealed record AsyncAlignmentRecord(
        string Id,
        IReadOnlyList<string> First,
        IReadOnlyList<int> Second);
}
