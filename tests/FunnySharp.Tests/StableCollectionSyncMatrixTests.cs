using System.Collections;

namespace FunnySharp.Tests;

public sealed class StableCollectionSyncMatrixTests
{
    [Theory]
    [InlineData(47, 0)]
    [InlineData(47, 1)]
    [InlineData(47, 2)]
    [InlineData(48, 0)]
    [InlineData(48, 1)]
    [InlineData(48, 2)]
    [InlineData(49, 0)]
    [InlineData(49, 1)]
    [InlineData(49, 2)]
    [InlineData(50, 0)]
    [InlineData(50, 1)]
    [InlineData(50, 2)]
    [InlineData(51, 0)]
    [InlineData(51, 1)]
    [InlineData(51, 2)]
    [InlineData(52, 0)]
    [InlineData(52, 1)]
    [InlineData(52, 2)]
    [InlineData(53, 0)]
    [InlineData(53, 1)]
    [InlineData(53, 2)]
    [InlineData(54, 0)]
    [InlineData(54, 1)]
    [InlineData(54, 2)]
    [InlineData(55, 0)]
    [InlineData(55, 1)]
    [InlineData(55, 2)]
    [InlineData(56, 0)]
    [InlineData(56, 1)]
    [InlineData(56, 2)]
    [InlineData(57, 0)]
    [InlineData(57, 1)]
    [InlineData(57, 2)]
    [InlineData(58, 0)]
    [InlineData(58, 1)]
    [InlineData(58, 2)]
    public void SyncCardinalityPreservesSourceOceAndDisposalOverridesEarlierFaults(int identity, int scenario)
    {
        var primary = new OperationCanceledException("source");
        var disposal = new InvalidOperationException("dispose");
        var source = new CollectionMatrixProbe.SyncSource<int>([2, 1])
        { MoveFault = scenario is 0 or 2 ? primary : null, DisposeFault = scenario is 1 or 2 ? disposal : null };
        Assert.Same(scenario == 0 ? primary : disposal, Record.Exception(() => Cardinality(identity, source)));
        Assert.Equal(1, source.Acquisitions);
        Assert.Equal(1, source.Disposals);
        if (scenario is 0 or 2) { Assert.Equal(1, source.Moves); Assert.Equal(0, source.Reads); }
    }

    [Theory]
    [InlineData(50, 0)]
    [InlineData(50, 1)]
    [InlineData(50, 2)]
    [InlineData(52, 0)]
    [InlineData(52, 1)]
    [InlineData(52, 2)]
    [InlineData(58, 0)]
    [InlineData(58, 1)]
    [InlineData(58, 2)]
    public void SyncPredicatesKeepExceptionIdentityAndDisposalPrecedence(int identity, int scenario)
    {
        var failure = scenario == 1 ? (Exception)new OperationCanceledException("predicate") : new InvalidOperationException("predicate");
        var disposal = new InvalidOperationException("dispose");
        var source = new CollectionMatrixProbe.SyncSource<int>([1, 2]) { DisposeFault = scenario == 2 ? disposal : null };
        var calls = 0;
        bool Predicate(int _) { calls++; throw failure; }
        Assert.Same(scenario == 2 ? disposal : failure, Record.Exception(() => Cardinality(identity, source, Predicate)));
        Assert.Equal(1, calls);
        Assert.Equal(1, source.Moves);
        Assert.Equal(1, source.Reads);
        Assert.Equal(1, source.Disposals);
    }

    [Theory]
    [InlineData(49)]
    [InlineData(50)]
    [InlineData(51)]
    [InlineData(52)]
    [InlineData(57)]
    [InlineData(58)]
    [InlineData(48)]
    public void SyncSelectedNullableValueTypeNullIsRejectedButMultipleMatchingItemsAreAbsence(int identity)
    {
        var source = new CollectionMatrixProbe.SyncSource<int?>([null]);
        var exception = Assert.Throws<ArgumentNullException>(() => NullableCardinality(identity, source));
        Assert.Equal("value", exception.ParamName);
        Assert.Equal(1, source.Disposals);
        if (identity is 57 or 58) Assert.True(NullableCardinality(identity, new CollectionMatrixProbe.SyncSource<int?>([null, null])).IsNone);
    }

    [Theory]
    [InlineData(53)]
    [InlineData(54)]
    [InlineData(55)]
    [InlineData(56)]
    public void SyncExtremesSkipNullableValueNullsAndDisposeOnComparerFailure(int identity)
    {
        var source = new CollectionMatrixProbe.SyncSource<int?>([null, 3, null, 1]);
        var calls = 0;
        var comparer = Comparer<int?>.Create((left, right) => { Assert.True(left.HasValue); Assert.True(right.HasValue); calls++; return left.Value.CompareTo(right.Value); });
        var value = identity switch { 53 => source.MaxOrNone(), 54 => source.MaxOrNone(comparer), 55 => source.MinOrNone(), _ => source.MinOrNone(comparer) };
        Assert.True(value.TryGetValue(out var selected));
        Assert.Equal(identity is 53 or 54 ? 3 : 1, selected);
        Assert.Equal(identity is 54 or 56 ? 1 : 0, calls);
        Assert.Equal(5, source.Moves);
        Assert.Equal(1, source.Disposals);
        var fault = new OperationCanceledException("compare");
        var failed = new CollectionMatrixProbe.SyncSource<object>([new object(), new object()]);
        if (identity is 53 or 55) Assert.Throws<ArgumentException>(() => { _ = identity == 53 ? failed.MaxOrNone() : failed.MinOrNone(); });
        else Assert.Same(fault, Record.Exception(() => { _ = identity == 54 ? failed.MaxOrNone(Comparer<object>.Create((_, _) => throw fault)) : failed.MinOrNone(Comparer<object>.Create((_, _) => throw fault)); }));
        Assert.Equal(1, failed.Disposals);
    }

    [Fact]
    public void NonEmptyNullableItemsAreStoredRestIsAnAliasAndMaterializationsAreFresh()
    {
        var option = new string?[] { null, "middle", null }.ToNonEmptyOrNone();
        Assert.True(option.TryGetValue(out var nonEmpty));
        string? first = nonEmpty.First;
        Assert.Null(first);
        Assert.Equal(3, nonEmpty.Count);
        var snapshot = nonEmpty.ToReadOnlyList();
        var secondSnapshot = nonEmpty.ToReadOnlyList();
        Assert.NotSame(snapshot, secondSnapshot);
        var rest = Assert.IsType<List<string?>>(nonEmpty.Rest);
        rest[0] = "changed";
        rest.Add(null);
        Assert.Equal(4, nonEmpty.Count);
        Assert.Equal(new string?[] { null, "middle", null }, snapshot);
        Assert.Equal(new string?[] { null, "changed", null, null }, nonEmpty.ToReadOnlyList());
        Assert.Throws<NotSupportedException>(() => ((IList<string?>)snapshot)[0] = "mutation");
        var calls = new List<(string? Left, string? Right)>();
        Assert.Null(nonEmpty.Aggregate((left, right) => { calls.Add((left, right)); return null; }));
        Assert.Equal(new (string?, string?)[] { (null, "changed"), (null, null), (null, null) }, calls);
        var failure = new InvalidOperationException("aggregate");
        Assert.Same(failure, Record.Exception(() => nonEmpty.Aggregate((_, _) => throw failure)));
        Assert.False(string.IsNullOrEmpty(nonEmpty.ToString()));
#pragma warning disable FS1001
        var uninitialized = default(NonEmpty<string?>);
        Assert.Equal("func", Assert.Throws<ArgumentNullException>(() => uninitialized.Aggregate(null!)).ParamName);
        Assert.Throws<InvalidOperationException>(() => uninitialized.Aggregate((left, _) => left));
        Assert.Throws<InvalidOperationException>(() => uninitialized.ToReadOnlyList());
        Assert.Throws<InvalidOperationException>(() => uninitialized.Count);
        Assert.Throws<InvalidOperationException>(() => uninitialized.First);
        Assert.Throws<InvalidOperationException>(() => uninitialized.Rest);
#pragma warning restore FS1001
    }

    [Fact]
    public void NonEmptyCountFaultOccursAfterFirstReadAndDisposalOverridesIt()
    {
        var countFault = new OperationCanceledException("count");
        var disposeFault = new InvalidOperationException("dispose");
        var first = new CountSource<int>([1, 2], countFault);
        Assert.Same(countFault, Record.Exception(() => first.ToNonEmptyOrNone()));
        Assert.Equal(1, first.Probe.Acquisitions);
        Assert.Equal(1, first.Probe.Moves);
        Assert.Equal(1, first.Probe.Reads);
        Assert.Equal(1, first.Probe.Disposals);
        var second = new CountSource<int>([1], countFault, disposeFault);
        Assert.Same(disposeFault, Record.Exception(() => second.ToNonEmptyOrNone()));
        Assert.Equal(1, second.Probe.Disposals);
    }

    [Theory]
    [InlineData(351, 0)]
    [InlineData(351, 1)]
    [InlineData(351, 2)]
    [InlineData(351, 3)]
    [InlineData(351, 4)]
    [InlineData(351, 7)]
    [InlineData(349, 0)]
    [InlineData(349, 1)]
    [InlineData(349, 2)]
    [InlineData(349, 3)]
    [InlineData(349, 4)]
    [InlineData(349, 5)]
    [InlineData(349, 6)]
    [InlineData(349, 7)]
    [InlineData(348, 0)]
    [InlineData(348, 1)]
    [InlineData(348, 2)]
    [InlineData(348, 3)]
    [InlineData(348, 4)]
    [InlineData(348, 5)]
    [InlineData(348, 6)]
    [InlineData(348, 7)]
    [InlineData(359, 0)]
    [InlineData(359, 1)]
    [InlineData(359, 2)]
    [InlineData(359, 3)]
    [InlineData(359, 4)]
    [InlineData(359, 7)]
    [InlineData(358, 0)]
    [InlineData(358, 1)]
    [InlineData(358, 2)]
    [InlineData(358, 3)]
    [InlineData(358, 4)]
    [InlineData(358, 5)]
    [InlineData(358, 6)]
    [InlineData(358, 7)]
    [InlineData(357, 0)]
    [InlineData(357, 1)]
    [InlineData(357, 2)]
    [InlineData(357, 3)]
    [InlineData(357, 4)]
    [InlineData(357, 5)]
    [InlineData(357, 6)]
    [InlineData(357, 7)]
    [InlineData(360, 0)]
    [InlineData(360, 1)]
    [InlineData(360, 2)]
    [InlineData(360, 3)]
    [InlineData(360, 4)]
    [InlineData(362, 0)]
    [InlineData(362, 1)]
    [InlineData(362, 2)]
    [InlineData(362, 3)]
    [InlineData(362, 4)]
    [InlineData(362, 5)]
    [InlineData(362, 6)]
    [InlineData(361, 0)]
    [InlineData(361, 1)]
    [InlineData(361, 2)]
    [InlineData(361, 3)]
    [InlineData(361, 4)]
    [InlineData(361, 5)]
    [InlineData(361, 6)]
    [InlineData(373, 0)]
    [InlineData(373, 1)]
    [InlineData(373, 2)]
    [InlineData(373, 3)]
    [InlineData(373, 4)]
    [InlineData(373, 7)]
    [InlineData(372, 0)]
    [InlineData(372, 1)]
    [InlineData(372, 2)]
    [InlineData(372, 3)]
    [InlineData(372, 4)]
    [InlineData(372, 5)]
    [InlineData(372, 6)]
    [InlineData(372, 7)]
    [InlineData(371, 0)]
    [InlineData(371, 1)]
    [InlineData(371, 2)]
    [InlineData(371, 3)]
    [InlineData(371, 4)]
    [InlineData(371, 5)]
    [InlineData(371, 6)]
    [InlineData(371, 7)]
    public void EachSyncSequenceAndTraversalRetainsNullOrRejectsDefaultAndPreservesFaultPrecedence(int identity, int mode)
    {
        var failure = new OperationCanceledException("primary");
        var disposal = new InvalidOperationException("dispose");
        var counts = new CollectionMatrixProbe.Counts();
        var actual = Record.Exception(() => SyncTraversal(identity, mode, failure, disposal, out counts));
        if (mode is 3 or 5 or 7) Assert.Same(failure, actual);
        else if (mode is 4 or 6) Assert.Same(disposal, actual);
        else if (mode == 2 && identity is not (348 or 349 or 351)) Assert.IsType<InvalidOperationException>(actual);
        else Assert.Null(actual);
        Assert.Equal(1, counts.Acquisitions);
        Assert.Equal(1, counts.Disposals);
        if (mode == 3) { Assert.Equal(1, counts.Moves); Assert.Equal(0, counts.Reads); }
        else Assert.Equal(1, counts.Reads);
    }

    [Theory]
    [InlineData(347, 0)]
    [InlineData(347, 1)]
    [InlineData(347, 2)]
    [InlineData(347, 3)]
    [InlineData(355, 0)]
    [InlineData(355, 1)]
    [InlineData(355, 2)]
    [InlineData(355, 3)]
    [InlineData(369, 0)]
    [InlineData(369, 1)]
    [InlineData(369, 2)]
    [InlineData(369, 3)]
    public void ExplicitKeyComparersDetectCollapseAndKeepCountComparerAndDisposalFaultIdentity(int identity, int scenario)
    {
        var failure = new OperationCanceledException("comparer-or-count");
        var disposal = new InvalidOperationException("dispose");
        var source = new PairSource(scenario == 2 ? failure : null, scenario == 3 ? disposal : null);
        IEqualityComparer<string> comparer = scenario is 1 or 3 ? new ThrowingComparer(failure) : StringComparer.OrdinalIgnoreCase;
        var actual = Record.Exception(() =>
        {
            if (identity == 347) _ = source.Traverse((_, value) => Option<string?>.Some(value!), comparer);
            else if (identity == 355) _ = source.Traverse((_, value) => Result<string?, string?>.Success(value), comparer);
            else _ = source.Traverse((_, value) => Validation<string?, string?>.Valid(value), comparer);
        });
        if (scenario == 0) { Assert.IsType<ArgumentException>(actual); Assert.Equal(2, source.Probe.Reads); }
        else { Assert.Same(scenario == 3 ? disposal : failure, actual); Assert.Equal(1, source.Probe.Reads); }
        Assert.Equal(1, source.Probe.Acquisitions);
        Assert.Equal(1, source.Probe.Disposals);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void KeyedUnitTraversalHasNoCountReadAndStopsOnNullErrorOrDefaultOrCallbackFault(int mode)
    {
        var primary = new OperationCanceledException("selector");
        var disposal = new InvalidOperationException("dispose");
        var source = new PairSource(new InvalidOperationException("Count must not be read"), mode == 4 ? disposal : null);
        var reached = new List<string>();
        var actual = Record.Exception(() =>
        {
            var result = source.Traverse((key, value) =>
            {
                reached.Add(key);
                if (mode is 3 or 4) throw primary;
#pragma warning disable FS1001 // Deliberate uninitialized carrier fixture.
                if (mode == 2) return default(UnitResult<string?>);
#pragma warning restore FS1001
                return mode == 1 ? UnitResult<string?>.Failure(null) : UnitResult<string?>.Success();
            });
            if (mode == 1) { Assert.True(result.TryGetError(out var error)); Assert.Null(error); }
            else Assert.True(result.IsSuccess);
        });
        if (mode == 2) Assert.IsType<InvalidOperationException>(actual);
        else if (mode is 3 or 4) Assert.Same(mode == 4 ? disposal : primary, actual);
        else Assert.Null(actual);
        Assert.Equal(mode == 0 ? new[] { "A", "a" } : new[] { "A" }, reached);
        Assert.Equal(1, source.Probe.Disposals);
    }

    [Theory]
    [InlineData(113, 0)]
    [InlineData(113, 1)]
    [InlineData(113, 2)]
    [InlineData(113, 3)]
    [InlineData(114, 0)]
    [InlineData(114, 1)]
    [InlineData(114, 2)]
    [InlineData(114, 3)]
    public void ZipExactRetainsNullableTupleItemsAndDisposesInReverseAcquisitionOrder(int identity, int scenario)
    {
        var firstFault = new InvalidOperationException("first-dispose");
        var secondFault = new OperationCanceledException("second");
        var first = new CollectionMatrixProbe.SyncSource<string?>([null]) { DisposeFault = scenario == 2 ? firstFault : null };
        var second = new CollectionMatrixProbe.SyncSource<string?>([null]) { AcquisitionFault = scenario == 1 ? secondFault : null, DisposeFault = scenario is 2 or 3 ? secondFault : null };
        var actual = Record.Exception(() =>
        {
            if (identity == 113) { var result = first.ZipExactOrNone(second); Assert.True(result.TryGetValue(out var pairs)); Assert.Equal(new (string?, string?)[] { (null, null) }, pairs); }
            else { var result = first.ZipExact(second, (_, _) => (string?)null); Assert.True(result.TryGetValue(out var pairs)); Assert.Equal(new (string?, string?)[] { (null, null) }, pairs); }
        });
        Assert.Same(scenario == 0 ? null : scenario is 1 or 3 ? secondFault : firstFault, actual);
        Assert.Equal(1, first.Acquisitions);
        Assert.Equal(1, first.Disposals);
        Assert.Equal(scenario == 1 ? 0 : 1, second.Disposals);
        if (scenario == 0) { Assert.Equal(2, first.Moves); Assert.Equal(2, second.Moves); }
    }

    [Fact]
    public void ZipMismatchErrorMayBeNullAndMapperReceivesFullyDrainedCounts()
    {
        var first = new CollectionMatrixProbe.SyncSource<string?>([null, "tail", null]);
        var second = new CollectionMatrixProbe.SyncSource<string?>([null]);
        var calls = 0;
        var result = first.ZipExact(second, (left, right) => { calls++; Assert.Equal(3, left); Assert.Equal(1, right); return (string?)null; });
        Assert.True(result.TryGetError(out var error));
        Assert.Null(error);
        Assert.Equal(1, calls);
        Assert.Equal(4, first.Moves);
        Assert.Equal(2, second.Moves);
        Assert.Equal(1, first.Reads);
        Assert.Equal(1, second.Reads);
        Assert.Equal(1, first.Disposals);
        Assert.Equal(1, second.Disposals);
    }

    [Theory]
    [InlineData(269, 0)]
    [InlineData(269, 1)]
    [InlineData(269, 2)]
    [InlineData(269, 3)]
    [InlineData(270, 0)]
    [InlineData(270, 1)]
    [InlineData(270, 2)]
    [InlineData(270, 3)]
    public async Task AsyncPartitionsKeepNullableDataAndSourceOceStatusAndPendingConsumption(int identity, int mode)
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var move = new CollectionMatrixProbe.Gate<bool>();
        var dispose = new CollectionMatrixProbe.Gate<bool>();
        var primary = new OperationCanceledException("source", cancellation.Token);
        var disposal = new InvalidOperationException("dispose");
        CollectionMatrixProbe.Counts counts;
        Task operation;
        if (identity == 269)
        {
            var source = new CollectionMatrixProbe.AsyncSource<string?>([null])
            { MoveFault = mode is 1 or 3 ? primary : null, DisposeFault = mode == 3 ? disposal : null, MoveGate = mode == 0 ? move : null, DisposeGate = mode == 0 ? dispose : null };
            counts = source;
            operation = source.PartitionAsync(item => { Assert.Null(item); if (mode == 2) throw primary; return true; }, cancellation.Token).AsTask();
        }
        else
        {
#pragma warning disable FS1001 // Deliberate uninitialized carrier fixture.
            var source = new CollectionMatrixProbe.AsyncSource<Result<string?, string?>>(mode == 2 ? [default] : [Result<string?, string?>.Success(null), Result<string?, string?>.Failure(null)])
#pragma warning restore FS1001
            { MoveFault = mode is 1 or 3 ? primary : null, DisposeFault = mode == 3 ? disposal : null, MoveGate = mode == 0 ? move : null, DisposeGate = mode == 0 ? dispose : null };
            counts = source;
            operation = source.PartitionAsync(cancellation.Token).AsTask();
        }
        if (mode == 0)
        {
            await CollectionMatrixProbe.Signal(move.Subscribed.Task);
            Assert.False(operation.IsCompleted);
            move.Complete(true);
            await CollectionMatrixProbe.Signal(dispose.Subscribed.Task);
            Assert.False(operation.IsCompleted);
            dispose.Complete(true);
            await CollectionMatrixProbe.Signal(operation);
            Assert.Equal(1, move.Consumptions);
            Assert.Equal(1, dispose.Consumptions);
            if (identity == 269) { var partition = await (Task<Partition<string?>>)operation; Assert.Equal(new string?[] { null }, partition.True); Assert.Empty(partition.False); }
            else { var partition = await (Task<ResultPartition<string?, string?>>)operation; Assert.Equal(new string?[] { null }, partition.Passed); Assert.Equal(new string?[] { null }, partition.Failed); }
        }
        else if (identity == 270 && mode == 2) await Assert.ThrowsAsync<InvalidOperationException>(async () => await operation);
        else { Assert.Same(mode == 3 ? disposal : primary, await Record.ExceptionAsync(async () => await operation)); Assert.Equal(mode != 3, operation.IsCanceled); }
        Assert.Equal(1, counts.Acquisitions);
        Assert.Equal(1, counts.Disposals);
        Assert.Equal(cancellation.Token, counts.Token);
    }

    [Theory]
    [InlineData(265, 0)]
    [InlineData(265, 1)]
    [InlineData(265, 2)]
    [InlineData(265, 3)]
    [InlineData(265, 4)]
    [InlineData(265, 5)]
    [InlineData(265, 6)]
    [InlineData(266, 0)]
    [InlineData(266, 1)]
    [InlineData(266, 2)]
    [InlineData(266, 3)]
    [InlineData(266, 4)]
    [InlineData(266, 5)]
    [InlineData(266, 6)]
    [InlineData(267, 0)]
    [InlineData(267, 1)]
    [InlineData(267, 2)]
    [InlineData(267, 3)]
    [InlineData(267, 4)]
    [InlineData(267, 5)]
    [InlineData(267, 6)]
    [InlineData(268, 0)]
    [InlineData(268, 1)]
    [InlineData(268, 2)]
    [InlineData(268, 3)]
    [InlineData(268, 4)]
    [InlineData(268, 5)]
    [InlineData(268, 6)]
    public void SyncPartitionsRetainNullableAndDefaultOptionDataAndKeepCountSourceAndDisposalFaults(int identity, int mode)
    {
        var primary = mode == 4 ? (Exception)new OperationCanceledException("source") : new InvalidOperationException("primary");
        var disposal = new InvalidOperationException("dispose");
        CollectionMatrixProbe.Counts counts = new();
        Exception? actual = Record.Exception(() =>
        {
            switch (identity)
            {
                case 265:
                    {
                        var probe = new CollectionMatrixProbe.SyncSource<Option<string?>>([default, Option<string?>.Some("present")])
                        { MoveFault = mode is 3 or 4 or 6 ? primary : null, DisposeFault = mode is 5 or 6 ? disposal : null };
                        counts = probe;
                        IEnumerable<Option<string?>> source = mode == 2 ? new CountSource<Option<string?>>(probe, primary) : probe;
                        var result = source.Partition();
                        Assert.Equal(new[] { "present" }, result.Somes);
                        Assert.Equal(1, result.Nones);
                        Assert.Throws<NotSupportedException>(() => ((IList<string?>)result.Somes).Add("mutation"));
                        break;
                    }
                case 266:
                    {
                        var probe = new CollectionMatrixProbe.SyncSource<string?>([null, "item"])
                        { MoveFault = mode is 3 or 4 or 6 ? primary : null, DisposeFault = mode is 5 or 6 ? disposal : null };
                        counts = probe;
                        IEnumerable<string?> source = mode == 2 ? new CountSource<string?>(probe, primary) : probe;
                        var result = source.Partition(item => item is null);
                        Assert.Equal(new string?[] { null }, result.True);
                        Assert.Equal(new[] { "item" }, result.False);
                        break;
                    }
                case 267:
                    {
#pragma warning disable FS1001 // Deliberate uninitialized carrier fixture.
                        var probe = new CollectionMatrixProbe.SyncSource<Result<string?, string?>>(mode == 1 ? [default] : [Result<string?, string?>.Success(null), Result<string?, string?>.Failure(null)])
#pragma warning restore FS1001
                        { MoveFault = mode is 3 or 4 or 6 ? primary : null, DisposeFault = mode is 5 or 6 ? disposal : null };
                        counts = probe;
                        IEnumerable<Result<string?, string?>> source = mode == 2 ? new CountSource<Result<string?, string?>>(probe, primary) : probe;
                        var result = source.Partition();
                        Assert.Equal(new string?[] { null }, result.Passed);
                        Assert.Equal(new string?[] { null }, result.Failed);
                        break;
                    }
                default:
                    {
#pragma warning disable FS1001 // Deliberate uninitialized carrier fixture.
                        var probe = new CollectionMatrixProbe.SyncSource<UnitResult<string?>>(mode == 1 ? [default] : [UnitResult<string?>.Success(), UnitResult<string?>.Failure(null)])
#pragma warning restore FS1001
                        { MoveFault = mode is 3 or 4 or 6 ? primary : null, DisposeFault = mode is 5 or 6 ? disposal : null };
                        counts = probe;
                        IEnumerable<UnitResult<string?>> source = mode == 2 ? new CountSource<UnitResult<string?>>(probe, primary) : probe;
                        var result = source.Partition();
                        Assert.Equal(1, result.Succeeded);
                        Assert.Equal(new string?[] { null }, result.Failed);
                        break;
                    }
            }
        });
        if (mode == 1 && identity is 267 or 268) Assert.IsType<InvalidOperationException>(actual);
        else if (mode is 2 or 3 or 4) Assert.Same(primary, actual);
        else if (mode is 5 or 6) Assert.Same(disposal, actual);
        else Assert.Null(actual);
        Assert.Equal(mode == 2 ? 0 : 1, counts.Acquisitions);
        Assert.Equal(mode == 2 ? 0 : 1, counts.Disposals);
        if (mode is 3 or 4 or 6) { Assert.Equal(1, counts.Moves); Assert.Equal(0, counts.Reads); }
    }

    [Theory]
    [InlineData(66)]
    [InlineData(67)]
    [InlineData(68)]
    [InlineData(69)]
    [InlineData(70)]
    [InlineData(71)]
    public void ContainerNullSelectionRejectsNullableValueTypePayloadWithoutMutation(int identity)
    {
        var queue = new Queue<int?>([null]);
        var stack = new Stack<int?>([null]);
        var priority = new PriorityQueue<int?, int>();
        priority.Enqueue(null, 0);
        var exception = Assert.Throws<ArgumentNullException>(() =>
        {
            _ = identity switch { 66 => queue.DequeueOrNone(), 67 => queue.PeekOrNone(), 68 => stack.PeekOrNone(), 69 => stack.PopOrNone(), 70 => priority.DequeueOrNone(), _ => priority.PeekOrNone() };
        });
        Assert.Equal("value", exception.ParamName);
        Assert.Single(queue);
        Assert.Single(stack);
        Assert.Single(priority.UnorderedItems);
        Assert.Null(queue.Peek());
        Assert.Null(stack.Peek());
        Assert.Null(priority.Peek());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void IListIndexPolicyReceivesNullAndAnyNegativeResultMeansNoneWithoutGenericSearch(int mode)
    {
        var failure = new OperationCanceledException("index");
        var list = new PolicyList(mode, failure);
        IList<string?> receiver = list;
        var actual = Record.Exception(() =>
        {
            var result = receiver.IndexOfOrNone(null);
            if (mode == 0) Assert.True(result.IsNone);
            else { Assert.True(result.TryGetValue(out var index)); Assert.Equal(5, index); }
        });
        Assert.Same(mode == 2 ? failure : null, actual);
        Assert.Equal(1, list.Calls);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void DictionaryRemoveKeepsComparerFaultIdentityOnLookupAndSecondRemoveLookup(int faultCall)
    {
        var failure = new OperationCanceledException("dictionary-comparer");
        var comparer = new CountingKeyComparer(failure);
        var key = new object();
        var dictionary = new Dictionary<object, string?>(comparer) { [key] = "value" };
        comparer.Calls = 0;
        comparer.FaultCall = faultCall;
        Assert.Same(failure, Record.Exception(() => dictionary.RemoveOrNone(key)));
        Assert.Equal(faultCall, comparer.Calls);
        Assert.Single(dictionary);
        comparer.FaultCall = 0;
        Assert.Equal("value", dictionary[key]);
        var strings = new Dictionary<string, string?> { [""] = "empty-key", ["null-value"] = null };
        Assert.True(strings.RemoveOrNone("").TryGetValue(out var value));
        Assert.Equal("empty-key", value);
        Assert.False(strings.ContainsKey(""));
        Assert.Throws<ArgumentNullException>(() => strings.RemoveOrNone(null!));
        Assert.Equal("value", Assert.Throws<ArgumentNullException>(() => strings.RemoveOrNone("null-value")).ParamName);
        Assert.True(strings.ContainsKey("null-value"));
    }

    private sealed class PolicyList(int mode, Exception failure) : System.Collections.ObjectModel.Collection<string?>, IList<string?>
    {
        internal int Calls { get; private set; }
        int IList<string?>.IndexOf(string? item) { Calls++; Assert.Null(item); return mode switch { 0 => -9, 1 => 5, _ => throw failure }; }
    }

    private sealed class CountingKeyComparer(Exception failure) : IEqualityComparer<object>
    {
        internal int Calls { get; set; }
        internal int FaultCall { get; set; }
        public new bool Equals(object? left, object? right) => ReferenceEquals(left, right);
        public int GetHashCode(object value) { Calls++; if (Calls == FaultCall) throw failure; return 1; }
    }

    private static object Cardinality(int identity, IEnumerable<int> source, Func<int, bool>? predicate = null) => identity switch
    {
        47 => source.ToNonEmptyOrNone(),
        48 => source.ElementAtOrNone(0),
        49 => source.FirstOrNone(),
        50 => source.FirstOrNone(predicate ?? (_ => true)),
        51 => source.LastOrNone(),
        52 => source.LastOrNone(predicate ?? (_ => true)),
        53 => source.MaxOrNone(),
        54 => source.MaxOrNone(Comparer<int>.Default),
        55 => source.MinOrNone(),
        56 => source.MinOrNone(Comparer<int>.Default),
        57 => source.SingleOrNone(),
        58 => source.SingleOrNone(predicate ?? (_ => true)),
        _ => throw new ArgumentOutOfRangeException(nameof(identity)),
    };
    private static Option<int?> NullableCardinality(int identity, IEnumerable<int?> source) => identity switch
    {
        48 => source.ElementAtOrNone(0),
        49 => source.FirstOrNone(),
        50 => source.FirstOrNone(_ => true),
        51 => source.LastOrNone(),
        52 => source.LastOrNone(_ => true),
        57 => source.SingleOrNone(),
        58 => source.SingleOrNone(_ => true),
        _ => throw new ArgumentOutOfRangeException(nameof(identity)),
    };

    private static void SyncTraversal(int identity, int mode, Exception primary, Exception disposal, out CollectionMatrixProbe.Counts counts)
    {
        switch (identity)
        {
            case 351:
            case 349:
            case 348:
                {
                    Option<string?> value = mode switch { 1 => Option<string?>.None, 2 => default, _ => Option<string?>.Some("present") };
                    var source = new CollectionMatrixProbe.SyncSource<Option<string?>>([value]) { MoveFault = mode == 3 ? primary : null, DisposeFault = mode is 4 or 6 ? disposal : null };
                    var input = new CollectionMatrixProbe.SyncSource<string?>([null]) { MoveFault = mode == 3 ? primary : null, DisposeFault = mode is 4 or 6 ? disposal : null };
                    counts = identity == 351 ? source : input;
                    IEnumerable<Option<string?>> carrierSource = mode == 7 ? new CountSource<Option<string?>>(source, primary) : source;
                    IEnumerable<string?> itemSource = mode == 7 ? new CountSource<string?>(input, primary) : input;
                    Option<string?> Select(string? item)
                    {
                        Assert.Null(item);
                        if (mode is 5 or 6) throw primary;
                        return value;
                    }
                    var result = identity switch
                    {
                        351 => carrierSource.Sequence(),
                        349 => itemSource.Traverse((Func<string?, Option<string?>>)Select),
                        _ => itemSource.Traverse((int index, string? item) => { Assert.Equal(0, index); return Select(item); }),
                    };
                    if (mode is 0 or 1 or 2)
                    {
                        if (mode is 1 or 2) Assert.True(result.IsNone); else { Assert.True(result.TryGetValue(out var values)); Assert.Equal(new[] { "present" }, values); }
                    }
                    return;
                }
            case 359:
            case 358:
            case 357:
                {
#pragma warning disable FS1001 // Deliberate uninitialized carrier fixture.
                    Result<string?, string?> value = mode switch { 1 => Result<string?, string?>.Failure(null), 2 => default, _ => Result<string?, string?>.Success(null) };
#pragma warning restore FS1001
                    var source = new CollectionMatrixProbe.SyncSource<Result<string?, string?>>([value]) { MoveFault = mode == 3 ? primary : null, DisposeFault = mode is 4 or 6 ? disposal : null };
                    var input = new CollectionMatrixProbe.SyncSource<string?>([null]) { MoveFault = mode == 3 ? primary : null, DisposeFault = mode is 4 or 6 ? disposal : null };
                    counts = identity == 359 ? source : input;
                    IEnumerable<Result<string?, string?>> carrierSource = mode == 7 ? new CountSource<Result<string?, string?>>(source, primary) : source;
                    IEnumerable<string?> itemSource = mode == 7 ? new CountSource<string?>(input, primary) : input;
                    Result<string?, string?> Select(string? item)
                    {
                        Assert.Null(item);
                        if (mode is 5 or 6) throw primary;
                        return value;
                    }
                    var result = identity switch
                    {
                        359 => carrierSource.Sequence(),
                        358 => itemSource.Traverse((Func<string?, Result<string?, string?>>)Select),
                        _ => itemSource.Traverse((int index, string? item) => { Assert.Equal(0, index); return Select(item); }),
                    };
                    if (mode is 0 or 1 or 2)
                    {
                        if (mode == 1) { Assert.True(result.TryGetError(out var error)); Assert.Null(error); } else { Assert.True(result.TryGetValue(out var values)); Assert.Equal(new string?[] { null }, values); }
                    }
                    return;
                }
            case 360:
            case 362:
            case 361:
                {
#pragma warning disable FS1001 // Deliberate uninitialized carrier fixture.
                    UnitResult<string?> value = mode switch { 1 => UnitResult<string?>.Failure(null), 2 => default, _ => UnitResult<string?>.Success() };
#pragma warning restore FS1001
                    var source = new CollectionMatrixProbe.SyncSource<UnitResult<string?>>([value]) { MoveFault = mode == 3 ? primary : null, DisposeFault = mode is 4 or 6 ? disposal : null };
                    var input = new CollectionMatrixProbe.SyncSource<string?>([null]) { MoveFault = mode == 3 ? primary : null, DisposeFault = mode is 4 or 6 ? disposal : null };
                    counts = identity == 360 ? source : input;
                    IEnumerable<UnitResult<string?>> carrierSource = mode == 7 ? new CountSource<UnitResult<string?>>(source, primary) : source;
                    IEnumerable<string?> itemSource = mode == 7 ? new CountSource<string?>(input, primary) : input;
                    UnitResult<string?> Select(string? item)
                    {
                        Assert.Null(item);
                        if (mode is 5 or 6) throw primary;
                        return value;
                    }
                    var result = identity switch
                    {
                        360 => carrierSource.Sequence(),
                        362 => itemSource.Traverse((Func<string?, UnitResult<string?>>)Select),
                        _ => itemSource.Traverse((int index, string? item) => { Assert.Equal(0, index); return Select(item); }),
                    };
                    if (mode is 0 or 1 or 2)
                    {
                        if (mode == 1) { Assert.True(result.TryGetError(out var error)); Assert.Null(error); } else Assert.True(result.IsSuccess);
                    }
                    return;
                }
            case 373:
            case 372:
            case 371:
                {
#pragma warning disable FS1001 // Deliberate uninitialized carrier fixture.
                    Validation<string?, string?> value = mode switch { 1 => Validation<string?, string?>.InvalidMany([null, "second-error"]), 2 => default, _ => Validation<string?, string?>.Valid(null) };
#pragma warning restore FS1001
                    var source = new CollectionMatrixProbe.SyncSource<Validation<string?, string?>>([value]) { MoveFault = mode == 3 ? primary : null, DisposeFault = mode is 4 or 6 ? disposal : null };
                    var input = new CollectionMatrixProbe.SyncSource<string?>([null]) { MoveFault = mode == 3 ? primary : null, DisposeFault = mode is 4 or 6 ? disposal : null };
                    counts = identity == 373 ? source : input;
                    IEnumerable<Validation<string?, string?>> carrierSource = mode == 7 ? new CountSource<Validation<string?, string?>>(source, primary) : source;
                    IEnumerable<string?> itemSource = mode == 7 ? new CountSource<string?>(input, primary) : input;
                    Validation<string?, string?> Select(string? item)
                    {
                        Assert.Null(item);
                        if (mode is 5 or 6) throw primary;
                        return value;
                    }
                    var result = identity switch
                    {
                        373 => carrierSource.Sequence(),
                        372 => itemSource.Traverse((Func<string?, Validation<string?, string?>>)Select),
                        _ => itemSource.Traverse((int index, string? item) => { Assert.Equal(0, index); return Select(item); }),
                    };
                    if (mode is 0 or 1 or 2)
                    {
                        if (mode == 1) { Assert.True(result.TryGetErrors(out var errors)); Assert.Equal(new string?[] { null, "second-error" }, errors); } else { Assert.True(result.TryGetValue(out var values)); Assert.Equal(new string?[] { null }, values); }
                    }
                    return;
                }
            default: throw new ArgumentOutOfRangeException(nameof(identity));
        }
    }

    private sealed class CountSource<T> : ICollection<T>
    {
        internal CollectionMatrixProbe.SyncSource<T> Probe { get; }
        private readonly Exception fault;
        internal CountSource(T[] items, Exception fault, Exception? disposeFault = null) : this(new CollectionMatrixProbe.SyncSource<T>(items) { DisposeFault = disposeFault }, fault) { }
        internal CountSource(CollectionMatrixProbe.SyncSource<T> probe, Exception fault) { Probe = probe; this.fault = fault; }
        public int Count => throw fault;
        public bool IsReadOnly => true;
        public IEnumerator<T> GetEnumerator() => Probe.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public void Add(T item) => throw new NotSupportedException();
        public void Clear() => throw new NotSupportedException();
        public bool Contains(T item) => throw new NotSupportedException();
        public void CopyTo(T[] array, int arrayIndex) => throw new NotSupportedException();
        public bool Remove(T item) => throw new NotSupportedException();
    }

    private sealed class PairSource(Exception? countFault, Exception? disposeFault) : IReadOnlyDictionary<string, string?>
    {
        internal CollectionMatrixProbe.SyncSource<KeyValuePair<string, string?>> Probe { get; } = new([new("A", "first"), new("a", "second")]) { DisposeFault = disposeFault };
        public int Count => countFault is null ? 2 : throw countFault;
        public IEnumerable<string> Keys => new[] { "A", "a" };
        public IEnumerable<string?> Values => new string?[] { "first", "second" };
        public string? this[string key] => key == "A" ? "first" : "second";
        public bool ContainsKey(string key) => key is "A" or "a";
        public bool TryGetValue(string key, out string? value) { value = this[key]; return ContainsKey(key); }
        public IEnumerator<KeyValuePair<string, string?>> GetEnumerator() => Probe.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class ThrowingComparer(Exception failure) : IEqualityComparer<string>
    {
        public bool Equals(string? left, string? right) => throw failure;
        public int GetHashCode(string value) => throw failure;
    }
}

