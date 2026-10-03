namespace FunnySharp.Tests;

public sealed class StableCollectionPipelineMatrixTests
{
    [Theory]
    [InlineData(13, 0)]
    [InlineData(13, 1)]
    [InlineData(13, 4)]
    [InlineData(13, 5)]
    [InlineData(13, 6)]
    [InlineData(14, 0)]
    [InlineData(14, 1)]
    [InlineData(14, 4)]
    [InlineData(14, 5)]
    [InlineData(14, 6)]
    [InlineData(15, 0)]
    [InlineData(15, 1)]
    [InlineData(15, 4)]
    [InlineData(15, 5)]
    [InlineData(15, 6)]
    [InlineData(16, 0)]
    [InlineData(16, 1)]
    [InlineData(16, 4)]
    [InlineData(16, 5)]
    [InlineData(16, 6)]
    [InlineData(17, 0)]
    [InlineData(17, 1)]
    [InlineData(17, 4)]
    [InlineData(17, 5)]
    [InlineData(17, 6)]
    [InlineData(18, 0)]
    [InlineData(18, 1)]
    [InlineData(18, 4)]
    [InlineData(18, 5)]
    [InlineData(18, 6)]
    [InlineData(19, 0)]
    [InlineData(19, 1)]
    [InlineData(19, 4)]
    [InlineData(19, 5)]
    [InlineData(19, 6)]
    [InlineData(20, 0)]
    [InlineData(20, 1)]
    [InlineData(20, 4)]
    [InlineData(20, 5)]
    [InlineData(20, 6)]
    [InlineData(15, 2)]
    [InlineData(15, 3)]
    [InlineData(15, 7)]
    [InlineData(16, 2)]
    [InlineData(16, 3)]
    [InlineData(16, 7)]
    [InlineData(17, 2)]
    [InlineData(17, 3)]
    [InlineData(17, 7)]
    [InlineData(18, 2)]
    [InlineData(18, 3)]
    [InlineData(18, 7)]
    [InlineData(19, 2)]
    [InlineData(19, 3)]
    [InlineData(19, 7)]
    [InlineData(20, 2)]
    [InlineData(20, 3)]
    [InlineData(20, 7)]
    public async Task AsyncPullFaultsPreserveIdentityAndDisposalCanOverrideSourceOrCallback(int identity, int scenario)
    {
        var primary = scenario is 1 or 3 ? (Exception)new OperationCanceledException("primary") : new InvalidOperationException("primary");
        var disposal = scenario == 5 ? (Exception)new OperationCanceledException("dispose") : new InvalidOperationException("dispose");
        var source = new CollectionMatrixProbe.AsyncSource<string?>(["item"])
        {
            MoveFault = scenario is 0 or 1 or 6 ? primary : null,
            DisposeFault = scenario is 4 or 5 or 6 or 7 ? disposal : null,
        };
        var calls = 0;
        var pipeline = Start(identity, source, (_, token) =>
        {
            Assert.Equal(TestContext.Current.CancellationToken, token);
            calls++;
            if (scenario == 3 && identity is 16 or 17 or 19 or 20)
            {
                var faulted = Task.FromException<string?>(primary);
                Assert.True(faulted.IsFaulted);
                return new ValueTask<string?>(faulted);
            }
            if (scenario is 2 or 3 or 7) throw primary;
            return ValueTask.FromResult<string?>("chosen");
        });
        if (scenario == 3 && identity is 19 or 20)
        {
            var faulted = Task.FromException<Option<string?>>(primary);
            Assert.True(faulted.IsFaulted);
            ValueTask<Option<string?>> ChooseFault(string? _, CancellationToken token)
            {
                if (identity == 19) Assert.Equal(TestContext.Current.CancellationToken, token);
                calls++;
                return new ValueTask<Option<string?>>(faulted);
            }
            pipeline = identity == 19 ? source.ChooseValueAsync(ChooseFault) : source.ChooseValueAsync(item => ChooseFault(item, CancellationToken.None));
        }
        var enumerator = pipeline.GetAsyncEnumerator(TestContext.Current.CancellationToken);
        try
        {
            var pull = enumerator.MoveNextAsync().AsTask();
            if (scenario is 4 or 5)
            {
                Assert.True(await pull);
                Assert.True(pull.IsCompletedSuccessfully);
                pull = enumerator.MoveNextAsync().AsTask();
            }
            var expected = scenario is 4 or 5 or 6 or 7 ? disposal : primary;
            Assert.Same(expected, await Record.ExceptionAsync(async () => { _ = await pull; }));
            Assert.Equal(expected is OperationCanceledException, pull.IsCanceled);
            Assert.Equal(expected is not OperationCanceledException, pull.IsFaulted);
            Assert.Equal(1, source.Acquisitions);
            Assert.Equal(1, source.Disposals);
            Assert.Equal(TestContext.Current.CancellationToken, source.Token);
            Assert.Equal(scenario is 4 or 5 ? 2 : 1, source.Moves);
            Assert.Equal(scenario is 0 or 1 or 6 ? 0 : 1, source.Reads);
            Assert.Equal(identity is 13 or 14 || scenario is 0 or 1 or 6 ? 0 : 1, calls);
        }
        finally { await enumerator.DisposeAsync(); }
        Assert.Equal(1, source.Disposals);
    }

    [Theory]
    [InlineData(13)]
    [InlineData(14)]
    [InlineData(15)]
    [InlineData(16)]
    [InlineData(17)]
    [InlineData(18)]
    [InlineData(19)]
    [InlineData(20)]
    public async Task PendingSourceAndDisposalAreSingleConsumedAndConsumerBreakStopsFurtherPulls(int identity)
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var move = new CollectionMatrixProbe.Gate<bool>();
        var dispose = new CollectionMatrixProbe.Gate<bool>();
        var source = new CollectionMatrixProbe.AsyncSource<string?>(["first", "untouched"]) { MoveGate = move, DisposeGate = dispose };
        var calls = 0;
        var pipeline = Start(identity, source, (item, token) =>
        {
            if (identity is 16 or 19) Assert.Equal(cancellation.Token, token);
            calls++;
            return ValueTask.FromResult(item);
        });
        Assert.Equal(0, source.Acquisitions);
        var enumerator = pipeline.GetAsyncEnumerator(cancellation.Token);
        Assert.Equal(0, source.Acquisitions);
        var pull = enumerator.MoveNextAsync().AsTask();
        await CollectionMatrixProbe.Signal(move.Subscribed.Task);
        Assert.False(pull.IsCompleted);
        move.Complete(true);
        Assert.True(await pull.WaitAsync(TimeSpan.FromSeconds(15), TestContext.Current.CancellationToken));
        Assert.Equal("first", enumerator.Current);
        var cleanup = enumerator.DisposeAsync().AsTask();
        await CollectionMatrixProbe.Signal(dispose.Subscribed.Task);
        Assert.False(cleanup.IsCompleted);
        dispose.Complete(true);
        await CollectionMatrixProbe.Signal(cleanup);
        Assert.Equal(1, move.Consumptions);
        Assert.Equal(1, dispose.Consumptions);
        Assert.Equal(1, source.Acquisitions);
        Assert.Equal(1, source.Moves);
        Assert.Equal(1, source.Reads);
        Assert.Equal(1, source.Disposals);
        Assert.Equal(cancellation.Token, source.Token);
        Assert.Equal(identity is 13 or 14 ? 0 : 1, calls);
    }

    [Theory]
    [InlineData(16)]
    [InlineData(17)]
    [InlineData(19)]
    [InlineData(20)]
    public async Task PendingValueCallbacksReceiveTheExactTokenAndAreConsumedOnlyOnce(int identity)
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var gate = new CollectionMatrixProbe.Gate<string?>();
        var chooseGate = new CollectionMatrixProbe.Gate<Option<string?>>();
        var source = new CollectionMatrixProbe.AsyncSource<string?>(["item"]);
        var calls = 0;
        IAsyncEnumerable<string?> pipeline;
        if (identity is 19 or 20)
        {
            ValueTask<Option<string?>> ChoosePending(string? _, CancellationToken token)
            {
                if (identity == 19) Assert.Equal(cancellation.Token, token);
                calls++;
                return chooseGate.Value;
            }
            pipeline = identity == 19
                ? source.ChooseValueAsync(ChoosePending)
                : source.ChooseValueAsync(item => ChoosePending(item, CancellationToken.None));
        }
        else pipeline = Start(identity, source, (_, token) =>
        {
            if (identity == 16) Assert.Equal(cancellation.Token, token);
            calls++;
            return gate.Value;
        });
        var enumerator = pipeline.GetAsyncEnumerator(cancellation.Token);
        var pull = enumerator.MoveNextAsync().AsTask();
        await CollectionMatrixProbe.Signal(identity is 19 or 20 ? chooseGate.Subscribed.Task : gate.Subscribed.Task);
        Assert.False(pull.IsCompleted);
        Assert.Equal(1, calls);
        Assert.Equal(1, source.Moves);
        if (identity is 19 or 20) chooseGate.Complete(Option<string?>.Some("chosen"));
        else gate.Complete("chosen");
        Assert.True(await pull.WaitAsync(TimeSpan.FromSeconds(15), TestContext.Current.CancellationToken));
        Assert.Equal("chosen", enumerator.Current);
        Assert.Equal(1, identity is 19 or 20 ? chooseGate.Consumptions : gate.Consumptions);
        await enumerator.DisposeAsync();
        Assert.Equal(1, source.Disposals);
    }

    [Theory]
    [InlineData(15)]
    [InlineData(16)]
    [InlineData(17)]
    public async Task NullSeedAndAccumulatedNullRemainPayloadsRatherThanAbsence(int identity)
    {
        var calls = 0;
        var source = new CollectionMatrixProbe.AsyncSource<string?>([null, "second"]);
        var pipeline = Start(identity, source, (item, _) =>
        {
            Assert.Equal(calls == 0 ? null : "second", item);
            calls++;
            return ValueTask.FromResult<string?>(null);
        });
        var values = new List<string?>();
        await foreach (var item in pipeline.WithCancellation(TestContext.Current.CancellationToken)) values.Add(item);
        Assert.Equal(new string?[] { null, null }, values);
        Assert.Equal(2, calls);
        Assert.Equal(3, source.Moves);
        Assert.Equal(1, source.Disposals);
    }

    [Theory]
    [InlineData(18)]
    [InlineData(19)]
    [InlineData(20)]
    public async Task ChoosingSomeNullThrowsRatherThanFilteringAndDisposesTheSource(int identity)
    {
        var source = new CollectionMatrixProbe.AsyncSource<string?>(["item"]);
        await using var enumerator = Start(identity, source, (_, _) => ValueTask.FromResult<string?>(null)).GetAsyncEnumerator(TestContext.Current.CancellationToken);
        var pull = enumerator.MoveNextAsync().AsTask();
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () => { _ = await pull; });
        Assert.Equal("value", exception.ParamName);
        Assert.True(pull.IsFaulted);
        Assert.Equal(1, source.Disposals);
        Assert.Equal(1, source.Reads);
    }

    [Fact]
    public async Task WhereNotNullRetainsDefaultValuesAndItsOutputsHaveNonNullableCompilerTypes()
    {
        var numbers = new CollectionMatrixProbe.AsyncSource<int?>([null, 0, 2]);
        IAsyncEnumerable<int> values = numbers.WhereNotNull();
        var output = new List<int>();
        await foreach (int value in values.WithCancellation(TestContext.Current.CancellationToken)) output.Add(value);
        Assert.Equal(new[] { 0, 2 }, output);
        var references = new CollectionMatrixProbe.AsyncSource<string?>([null, "abc"]);
        IAsyncEnumerable<string> strings = references.WhereNotNull();
        await foreach (string value in strings.WithCancellation(TestContext.Current.CancellationToken)) Assert.Equal(3, value.Length);
        Assert.Equal(1, numbers.Disposals);
        Assert.Equal(1, references.Disposals);
    }

    [Theory]
    [InlineData(108, 0)]
    [InlineData(108, 1)]
    [InlineData(108, 2)]
    [InlineData(109, 0)]
    [InlineData(109, 1)]
    [InlineData(109, 2)]
    [InlineData(110, 0)]
    [InlineData(110, 1)]
    [InlineData(110, 2)]
    [InlineData(111, 0)]
    [InlineData(111, 1)]
    [InlineData(111, 2)]
    public void SyncPipelineFaultsAndConsumerBreakDisposeExactlyOnce(int identity, int scenario)
    {
        var primary = new OperationCanceledException("source");
        var disposal = new InvalidOperationException("dispose");
        var source = new CollectionMatrixProbe.SyncSource<string?>(["first", "untouched"])
        {
            MoveFault = scenario is 0 or 2 ? primary : null,
            DisposeFault = scenario == 2 ? disposal : null,
        };
        using var enumerator = StartSync(identity, source).GetEnumerator();
        if (scenario == 1)
        {
            Assert.True(enumerator.MoveNext());
            Assert.Equal("first", enumerator.Current);
            enumerator.Dispose();
        }
        else Assert.Same(scenario == 2 ? disposal : primary, Record.Exception(() => enumerator.MoveNext()));
        Assert.Equal(1, source.Acquisitions);
        Assert.Equal(1, source.Moves);
        Assert.Equal(1, source.Disposals);
    }

    [Fact]
    public void SyncScanRetainsNullAndSyncChooseSomeNullFailsWithDisposal()
    {
        var scanSource = new CollectionMatrixProbe.SyncSource<string?>([null, "second"]);
        var inputs = new List<(string? State, string? Item)>();
        var values = scanSource.Scan<string?, string?>(null, (state, item) => { inputs.Add((state, item)); return null; }).ToArray();
        Assert.Equal(new string?[] { null, null }, values);
        Assert.Equal(new (string?, string?)[] { (null, null), (null, "second") }, inputs);
        Assert.Equal(1, scanSource.Disposals);
        var chooseSource = new CollectionMatrixProbe.SyncSource<string?>([null]);
        var exception = Assert.Throws<ArgumentNullException>(() => chooseSource.Choose(item => Option<string?>.Some(item!)).ToArray());
        Assert.Equal("value", exception.ParamName);
        Assert.Equal(1, chooseSource.Disposals);
        IEnumerable<int> numbers = new int?[] { null, 0 }.WhereNotNull();
        Assert.Equal(new[] { 0 }, numbers);
        IEnumerable<string> references = new string?[] { null, "a" }.WhereNotNull();
        Assert.Equal(1, references.Single().Length);
    }

    private static IAsyncEnumerable<string?> Start(int identity, IAsyncEnumerable<string?> source, Func<string?, CancellationToken, ValueTask<string?>> callback) => identity switch
    {
        13 => source.SelectNullableLengths().WhereNotNull().SelectStrings(),
        14 => source.WhereNotNull(),
        15 => source.Scan<string?, string?>(null, (_, item) => Completed(callback(item, TestContext.Current.CancellationToken))),
        16 => source.ScanValueAsync<string?, string?>(null, (state, item, token) => { Assert.Null(state); return callback(item, token); }),
        17 => source.ScanValueAsync<string?, string?>(null, (_, item) => callback(item, TestContext.Current.CancellationToken)),
        18 => source.Choose(item => Option<string?>.Some(Completed(callback(item, TestContext.Current.CancellationToken))!)),
        19 => source.ChooseValueAsync((item, token) => Choose(callback(item, token))),
        20 => source.ChooseValueAsync(item => Choose(callback(item, TestContext.Current.CancellationToken))),
        _ => throw new ArgumentOutOfRangeException(nameof(identity)),
    };

    private static string? Completed(ValueTask<string?> value)
    {
        if (value.IsCompletedSuccessfully) return value.GetAwaiter().GetResult();
        throw new InvalidOperationException("The synchronous callback fixture must be completed.");
    }
    private static async ValueTask<Option<string?>> Choose(ValueTask<string?> value) => Option<string?>.Some((await value)!);
    private static IEnumerable<string?> StartSync(int identity, IEnumerable<string?> source) => identity switch
    {
        108 => source.Select(item => item is null ? (int?)null : item.Length).WhereNotNull().Select(_ => "first"),
        109 => source.WhereNotNull(),
        110 => source.Scan<string?, string?>(null, (_, item) => item),
        111 => source.Choose(item => Option<string?>.Some(item!)),
        _ => throw new ArgumentOutOfRangeException(nameof(identity)),
    };
}

internal static class CollectionMatrixPipelineAdapters
{
    internal static async IAsyncEnumerable<int?> SelectNullableLengths(this IAsyncEnumerable<string?> source, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken token = default)
    {
        await foreach (var item in source.WithCancellation(token)) yield return item?.Length;
    }
    internal static async IAsyncEnumerable<string?> SelectStrings(this IAsyncEnumerable<int> source, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken token = default)
    {
        await foreach (var _ in source.WithCancellation(token)) yield return "first";
    }
}

