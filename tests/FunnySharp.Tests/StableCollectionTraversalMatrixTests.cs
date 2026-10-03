namespace FunnySharp.Tests;

public sealed class StableCollectionTraversalMatrixTests
{
    [Theory]
    [InlineData(27, 0)]
    [InlineData(27, 1)]
    [InlineData(27, 2)]
    [InlineData(27, 3)]
    [InlineData(27, 4)]
    [InlineData(27, 5)]
    [InlineData(23, 0)]
    [InlineData(23, 1)]
    [InlineData(23, 2)]
    [InlineData(23, 3)]
    [InlineData(23, 4)]
    [InlineData(23, 5)]
    [InlineData(26, 0)]
    [InlineData(26, 1)]
    [InlineData(26, 2)]
    [InlineData(26, 3)]
    [InlineData(26, 4)]
    [InlineData(26, 5)]
    [InlineData(25, 0)]
    [InlineData(25, 1)]
    [InlineData(25, 2)]
    [InlineData(25, 3)]
    [InlineData(25, 4)]
    [InlineData(25, 5)]
    [InlineData(33, 0)]
    [InlineData(33, 1)]
    [InlineData(33, 2)]
    [InlineData(33, 3)]
    [InlineData(33, 4)]
    [InlineData(33, 5)]
    [InlineData(29, 0)]
    [InlineData(29, 1)]
    [InlineData(29, 2)]
    [InlineData(29, 3)]
    [InlineData(29, 4)]
    [InlineData(29, 5)]
    [InlineData(32, 0)]
    [InlineData(32, 1)]
    [InlineData(32, 2)]
    [InlineData(32, 3)]
    [InlineData(32, 4)]
    [InlineData(32, 5)]
    [InlineData(31, 0)]
    [InlineData(31, 1)]
    [InlineData(31, 2)]
    [InlineData(31, 3)]
    [InlineData(31, 4)]
    [InlineData(31, 5)]
    [InlineData(34, 0)]
    [InlineData(34, 1)]
    [InlineData(34, 2)]
    [InlineData(34, 3)]
    [InlineData(34, 4)]
    [InlineData(34, 5)]
    [InlineData(36, 0)]
    [InlineData(36, 1)]
    [InlineData(36, 2)]
    [InlineData(36, 3)]
    [InlineData(36, 4)]
    [InlineData(36, 5)]
    [InlineData(39, 0)]
    [InlineData(39, 1)]
    [InlineData(39, 2)]
    [InlineData(39, 3)]
    [InlineData(39, 4)]
    [InlineData(39, 5)]
    [InlineData(38, 0)]
    [InlineData(38, 1)]
    [InlineData(38, 2)]
    [InlineData(38, 3)]
    [InlineData(38, 4)]
    [InlineData(38, 5)]
    [InlineData(45, 0)]
    [InlineData(45, 1)]
    [InlineData(45, 2)]
    [InlineData(45, 3)]
    [InlineData(45, 4)]
    [InlineData(45, 5)]
    [InlineData(41, 0)]
    [InlineData(41, 1)]
    [InlineData(41, 2)]
    [InlineData(41, 3)]
    [InlineData(41, 4)]
    [InlineData(41, 5)]
    [InlineData(44, 0)]
    [InlineData(44, 1)]
    [InlineData(44, 2)]
    [InlineData(44, 3)]
    [InlineData(44, 4)]
    [InlineData(44, 5)]
    [InlineData(43, 0)]
    [InlineData(43, 1)]
    [InlineData(43, 2)]
    [InlineData(43, 3)]
    [InlineData(43, 4)]
    [InlineData(43, 5)]
    [InlineData(23, 6)]
    [InlineData(23, 7)]
    [InlineData(23, 8)]
    [InlineData(26, 6)]
    [InlineData(26, 7)]
    [InlineData(26, 8)]
    [InlineData(25, 6)]
    [InlineData(25, 7)]
    [InlineData(25, 8)]
    [InlineData(29, 6)]
    [InlineData(29, 7)]
    [InlineData(29, 8)]
    [InlineData(32, 6)]
    [InlineData(32, 7)]
    [InlineData(32, 8)]
    [InlineData(31, 6)]
    [InlineData(31, 7)]
    [InlineData(31, 8)]
    [InlineData(36, 6)]
    [InlineData(36, 7)]
    [InlineData(36, 8)]
    [InlineData(39, 6)]
    [InlineData(39, 7)]
    [InlineData(39, 8)]
    [InlineData(38, 6)]
    [InlineData(38, 7)]
    [InlineData(38, 8)]
    [InlineData(41, 6)]
    [InlineData(41, 7)]
    [InlineData(41, 8)]
    [InlineData(44, 6)]
    [InlineData(44, 7)]
    [InlineData(44, 8)]
    [InlineData(43, 6)]
    [InlineData(43, 7)]
    [InlineData(43, 8)]
    public async Task EachAsyncTraversalOverloadPreservesSourceSelectorAndDisposalIdentityAndStatus(int identity, int scenario)
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var primary = scenario is 1 or 5 or 7 ? (Exception)new OperationCanceledException("primary", cancellation.Token) : new InvalidOperationException("primary");
        var disposal = scenario == 3 ? (Exception)new OperationCanceledException("dispose", cancellation.Token) : new InvalidOperationException("dispose");
        var run = Start(identity, cancellation.Token,
            moveFault: scenario is 0 or 1 or 4 or 5 ? primary : null,
            disposeFault: scenario is 2 or 3 or 4 or 8 ? disposal : null,
            selectorFault: scenario is 6 or 7 or 8 ? primary : null,
            canceledProducer: scenario == 5);
        var expected = scenario is 2 or 3 or 4 or 8 ? disposal : primary;
        Assert.Same(expected, await Record.ExceptionAsync(async () => await run.Operation));
        Assert.Equal(expected is OperationCanceledException, run.Operation.IsCanceled);
        Assert.Equal(expected is not OperationCanceledException, run.Operation.IsFaulted);
        Assert.Equal(cancellation.Token, run.Source.Token);
        Assert.Equal(1, run.Source.Acquisitions);
        Assert.Equal(1, run.Source.Disposals);
        Assert.Equal(scenario is 2 or 3 ? 2 : 1, run.Source.Moves);
        Assert.Equal(scenario is 0 or 1 or 4 or 5 ? 0 : 1, run.Source.Reads);
        Assert.Equal(IsSequence(identity) || scenario is 0 or 1 or 4 or 5 ? 0 : 1, run.Calls());
    }

    [Theory]
    [InlineData(27)]
    [InlineData(23)]
    [InlineData(26)]
    [InlineData(25)]
    [InlineData(33)]
    [InlineData(29)]
    [InlineData(32)]
    [InlineData(31)]
    [InlineData(34)]
    [InlineData(36)]
    [InlineData(39)]
    [InlineData(38)]
    [InlineData(45)]
    [InlineData(41)]
    [InlineData(44)]
    [InlineData(43)]
    public async Task EachAsyncTraversalSubscribesPendingMoveAndDisposalAndConsumesEachOnlyOnce(int identity)
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var move = new CollectionMatrixProbe.Gate<bool>();
        var dispose = new CollectionMatrixProbe.Gate<bool>();
        var run = Start(identity, cancellation.Token, moveGate: move, disposeGate: dispose);
        await CollectionMatrixProbe.Signal(move.Subscribed.Task);
        Assert.False(run.Operation.IsCompleted);
        move.Complete(true);
        await CollectionMatrixProbe.Signal(dispose.Subscribed.Task);
        Assert.False(run.Operation.IsCompleted);
        dispose.Complete(true);
        await CollectionMatrixProbe.Signal(run.Operation);
        Assert.True(run.Operation.IsCompletedSuccessfully);
        Assert.Equal(1, move.Consumptions);
        Assert.Equal(1, dispose.Consumptions);
        Assert.Equal(cancellation.Token, run.Source.Token);
        Assert.Equal(1, run.Source.Acquisitions);
        Assert.Equal(2, run.Source.Moves);
        Assert.Equal(1, run.Source.Reads);
        Assert.Equal(1, run.Source.Disposals);
        Assert.Equal(IsSequence(identity) ? 0 : 1, run.Calls());
    }

    [Theory]
    [InlineData(26)]
    [InlineData(25)]
    [InlineData(32)]
    [InlineData(31)]
    [InlineData(39)]
    [InlineData(38)]
    [InlineData(44)]
    [InlineData(43)]
    public async Task EachValueSelectorIsPendingSingleConsumedAndTokenAwareOverloadsReceiveTheExactToken(int identity)
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var run = Start(identity, cancellation.Token, pendingSelector: true);
        await CollectionMatrixProbe.Signal(run.Subscription!);
        Assert.False(run.Operation.IsCompleted);
        Assert.Equal(1, run.Source.Moves);
        Assert.Equal(1, run.Calls());
        Assert.Equal(0, run.Consumptions());
        run.Complete();
        await CollectionMatrixProbe.Signal(run.Operation);
        Assert.Equal(1, run.Consumptions());
        Assert.Equal(1, run.Calls());
        Assert.Equal(2, run.Source.Moves);
        Assert.Equal(1, run.Source.Disposals);
    }

    [Theory]
    [InlineData(27, 0)]
    [InlineData(27, 1)]
    [InlineData(27, 2)]
    [InlineData(27, 4)]
    [InlineData(23, 0)]
    [InlineData(23, 1)]
    [InlineData(23, 2)]
    [InlineData(23, 4)]
    [InlineData(26, 0)]
    [InlineData(26, 1)]
    [InlineData(26, 2)]
    [InlineData(26, 4)]
    [InlineData(25, 0)]
    [InlineData(25, 1)]
    [InlineData(25, 2)]
    [InlineData(25, 4)]
    [InlineData(33, 0)]
    [InlineData(33, 1)]
    [InlineData(33, 2)]
    [InlineData(33, 4)]
    [InlineData(29, 0)]
    [InlineData(29, 1)]
    [InlineData(29, 2)]
    [InlineData(29, 4)]
    [InlineData(32, 0)]
    [InlineData(32, 1)]
    [InlineData(32, 2)]
    [InlineData(32, 4)]
    [InlineData(31, 0)]
    [InlineData(31, 1)]
    [InlineData(31, 2)]
    [InlineData(31, 4)]
    [InlineData(34, 0)]
    [InlineData(34, 1)]
    [InlineData(34, 2)]
    [InlineData(34, 4)]
    [InlineData(36, 0)]
    [InlineData(36, 1)]
    [InlineData(36, 2)]
    [InlineData(36, 4)]
    [InlineData(39, 0)]
    [InlineData(39, 1)]
    [InlineData(39, 2)]
    [InlineData(39, 4)]
    [InlineData(38, 0)]
    [InlineData(38, 1)]
    [InlineData(38, 2)]
    [InlineData(38, 4)]
    [InlineData(45, 0)]
    [InlineData(45, 1)]
    [InlineData(45, 2)]
    [InlineData(45, 4)]
    [InlineData(41, 0)]
    [InlineData(41, 1)]
    [InlineData(41, 2)]
    [InlineData(41, 4)]
    [InlineData(44, 0)]
    [InlineData(44, 1)]
    [InlineData(44, 2)]
    [InlineData(44, 4)]
    [InlineData(43, 0)]
    [InlineData(43, 1)]
    [InlineData(43, 2)]
    [InlineData(43, 4)]
    public async Task EachTraversalRetainsNullablePayloadsRejectsDefaultStateAndPreservesEmptySuccess(int identity, int mode)
    {
        var run = Start(identity, TestContext.Current.CancellationToken, mode: mode);
        if (mode == 2 && identity is not (23 or 25 or 26 or 27))
        {
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await run.Operation);
            Assert.True(run.Operation.IsFaulted);
        }
        else
        {
            await run.Operation;
            switch (identity)
            {
                case 23:
                case 25:
                case 26:
                case 27:
                    var option = await (Task<Option<IReadOnlyList<string?>>>)run.Operation;
                    if (mode is 1 or 2) Assert.True(option.IsNone);
                    else { Assert.True(option.TryGetValue(out var values)); Assert.Equal(mode == 4 ? Array.Empty<string?>() : new[] { "present" }, values); }
                    break;
                case 29:
                case 31:
                case 32:
                case 33:
                    var result = await (Task<Result<IReadOnlyList<string?>, string?>>)run.Operation;
                    if (mode == 1) { Assert.True(result.TryGetError(out var error)); Assert.Null(error); }
                    else { Assert.True(result.TryGetValue(out var values)); Assert.Equal(mode == 4 ? Array.Empty<string?>() : new string?[] { null }, values); }
                    break;
                case 34:
                case 36:
                case 38:
                case 39:
                    var unit = await (Task<UnitResult<string?>>)run.Operation;
                    if (mode == 1) { Assert.True(unit.TryGetError(out var error)); Assert.Null(error); }
                    else Assert.True(unit.IsSuccess);
                    break;
                default:
                    var validation = await (Task<Validation<IReadOnlyList<string?>, string?>>)run.Operation;
                    if (mode == 1) { Assert.True(validation.TryGetErrors(out var errors)); Assert.Equal(new string?[] { null, "second-error" }, errors); }
                    else { Assert.True(validation.TryGetValue(out var values)); Assert.Equal(mode == 4 ? Array.Empty<string?>() : new string?[] { null }, values); }
                    break;
            }
        }
        Assert.Equal(1, run.Source.Acquisitions);
        Assert.Equal(1, run.Source.Disposals);
        var shortCircuit = mode is 1 or 2 && identity is not (41 or 43 or 44 or 45) || mode == 2;
        Assert.Equal(mode == 4 || shortCircuit ? 1 : 2, run.Source.Moves);
        Assert.Equal(mode == 4 ? 0 : 1, run.Source.Reads);
    }

    [Theory]
    [InlineData(23, 0)]
    [InlineData(23, 1)]
    [InlineData(23, 2)]
    [InlineData(26, 0)]
    [InlineData(26, 1)]
    [InlineData(26, 2)]
    [InlineData(25, 0)]
    [InlineData(25, 1)]
    [InlineData(25, 2)]
    [InlineData(29, 0)]
    [InlineData(29, 1)]
    [InlineData(29, 2)]
    [InlineData(32, 0)]
    [InlineData(32, 1)]
    [InlineData(32, 2)]
    [InlineData(31, 0)]
    [InlineData(31, 1)]
    [InlineData(31, 2)]
    [InlineData(36, 0)]
    [InlineData(36, 1)]
    [InlineData(36, 2)]
    [InlineData(39, 0)]
    [InlineData(39, 1)]
    [InlineData(39, 2)]
    [InlineData(38, 0)]
    [InlineData(38, 1)]
    [InlineData(38, 2)]
    [InlineData(41, 0)]
    [InlineData(41, 1)]
    [InlineData(41, 2)]
    [InlineData(44, 0)]
    [InlineData(44, 1)]
    [InlineData(44, 2)]
    [InlineData(43, 0)]
    [InlineData(43, 1)]
    [InlineData(43, 2)]
    public void EverySelectorWrapperRejectsSourceBeforeSelectorEagerly(int identity, int guard)
    {
        var exception = Assert.Throws<ArgumentNullException>(() => Guard(identity, guard));
        Assert.Equal(guard is 0 or 2 ? "source" : "selector", exception.ParamName);
    }

    [Theory]
    [InlineData(23)]
    [InlineData(25)]
    [InlineData(26)]
    public async Task OptionSelectorsAttemptingSomeNullFailWithTheSelectedFactoryParameter(int identity)
    {
        var run = Start(identity, TestContext.Current.CancellationToken, mode: 3);
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () => await run.Operation);
        Assert.Equal("value", exception.ParamName);
        Assert.Equal(1, run.Source.Reads);
        Assert.Equal(1, run.Source.Disposals);
        Assert.True(run.Operation.IsFaulted);
    }

    private static bool IsSequence(int identity) => identity is 27 or 33 or 34 or 45;
    private sealed record Run(Task Operation, CollectionMatrixProbe.Counts Source, Func<int> Calls, Task? Subscription, Action Complete, Func<int> Consumptions);

    private static Run Start(int identity, CancellationToken token, Exception? moveFault = null, Exception? disposeFault = null,
        Exception? selectorFault = null, bool canceledProducer = false, CollectionMatrixProbe.Gate<bool>? moveGate = null,
        CollectionMatrixProbe.Gate<bool>? disposeGate = null, bool pendingSelector = false, int mode = 0)
    {
        switch (identity)
        {
            case 27:
            case 23:
            case 26:
            case 25:
                {
                    Option<string?> value = mode switch { 1 => Option<string?>.None, 2 => default, _ => Option<string?>.Some("present") };
                    var gate = new CollectionMatrixProbe.Gate<Option<string?>>();
                    var calls = 0;
                    ValueTask<Option<string?>> Select(int _, CancellationToken received)
                    {
                        calls++;
                        Assert.Equal(token, received);
                        if (selectorFault is not null)
                        {
                            var faulted = Task.FromException<Option<string?>>(selectorFault);
                            Assert.True(faulted.IsFaulted);
                            return new ValueTask<Option<string?>>(faulted);
                        }
                        if (mode == 3) return ValueTask.FromResult(Option<string?>.Some(null!));
                        return pendingSelector ? gate.Value : ValueTask.FromResult(value);
                    }
                    if (identity == 27)
                    {
                        var source = new CollectionMatrixProbe.AsyncSource<Option<string?>>(mode == 4 ? [] : [value])
                        { MoveFault = moveFault, DisposeFault = disposeFault, MoveGate = moveGate, DisposeGate = disposeGate, CanceledProducer = canceledProducer };
                        return new(source.SequenceAsync(token).AsTask(), source, () => calls, null, () => { }, () => 0);
                    }
                    var input = new CollectionMatrixProbe.AsyncSource<int>(mode == 4 ? [] : [1])
                    { MoveFault = moveFault, DisposeFault = disposeFault, MoveGate = moveGate, DisposeGate = disposeGate, CanceledProducer = canceledProducer };
                    Task operation = identity switch
                    {
                        23 => input.TraverseAsync((Func<int, Option<string?>>)(_ => { calls++; if (selectorFault is not null) throw selectorFault; if (mode == 3) return Option<string?>.Some(null!); return value; }), token).AsTask(),
                        26 => input.TraverseValueAsync((Func<int, ValueTask<Option<string?>>>)(item => Select(item, token)), token).AsTask(),
                        _ => input.TraverseValueAsync((Func<int, CancellationToken, ValueTask<Option<string?>>>)Select, token).AsTask(),
                    };
                    return new(operation, input, () => calls, gate.Subscribed.Task, () => gate.Complete(value), () => gate.Consumptions);
                }
            case 33:
            case 29:
            case 32:
            case 31:
                {
#pragma warning disable FS1001 // Deliberate uninitialized carrier fixture.
                    Result<string?, string?> value = mode switch { 1 => Result<string?, string?>.Failure(null), 2 => default, _ => Result<string?, string?>.Success(null) };
#pragma warning restore FS1001
                    var gate = new CollectionMatrixProbe.Gate<Result<string?, string?>>();
                    var calls = 0;
                    ValueTask<Result<string?, string?>> Select(int _, CancellationToken received)
                    {
                        calls++;
                        Assert.Equal(token, received);
                        if (selectorFault is not null)
                        {
                            var faulted = Task.FromException<Result<string?, string?>>(selectorFault);
                            Assert.True(faulted.IsFaulted);
                            return new ValueTask<Result<string?, string?>>(faulted);
                        }

                        return pendingSelector ? gate.Value : ValueTask.FromResult(value);
                    }
                    if (identity == 33)
                    {
                        var source = new CollectionMatrixProbe.AsyncSource<Result<string?, string?>>(mode == 4 ? [] : [value])
                        { MoveFault = moveFault, DisposeFault = disposeFault, MoveGate = moveGate, DisposeGate = disposeGate, CanceledProducer = canceledProducer };
                        return new(source.SequenceAsync(token).AsTask(), source, () => calls, null, () => { }, () => 0);
                    }
                    var input = new CollectionMatrixProbe.AsyncSource<int>(mode == 4 ? [] : [1])
                    { MoveFault = moveFault, DisposeFault = disposeFault, MoveGate = moveGate, DisposeGate = disposeGate, CanceledProducer = canceledProducer };
                    Task operation = identity switch
                    {
                        29 => input.TraverseAsync((Func<int, Result<string?, string?>>)(_ => { calls++; if (selectorFault is not null) throw selectorFault; return value; }), token).AsTask(),
                        32 => input.TraverseValueAsync((Func<int, ValueTask<Result<string?, string?>>>)(item => Select(item, token)), token).AsTask(),
                        _ => input.TraverseValueAsync((Func<int, CancellationToken, ValueTask<Result<string?, string?>>>)Select, token).AsTask(),
                    };
                    return new(operation, input, () => calls, gate.Subscribed.Task, () => gate.Complete(value), () => gate.Consumptions);
                }
            case 34:
            case 36:
            case 39:
            case 38:
                {
#pragma warning disable FS1001 // Deliberate uninitialized carrier fixture.
                    UnitResult<string?> value = mode switch { 1 => UnitResult<string?>.Failure(null), 2 => default, _ => UnitResult<string?>.Success() };
#pragma warning restore FS1001
                    var gate = new CollectionMatrixProbe.Gate<UnitResult<string?>>();
                    var calls = 0;
                    ValueTask<UnitResult<string?>> Select(int _, CancellationToken received)
                    {
                        calls++;
                        Assert.Equal(token, received);
                        if (selectorFault is not null)
                        {
                            var faulted = Task.FromException<UnitResult<string?>>(selectorFault);
                            Assert.True(faulted.IsFaulted);
                            return new ValueTask<UnitResult<string?>>(faulted);
                        }

                        return pendingSelector ? gate.Value : ValueTask.FromResult(value);
                    }
                    if (identity == 34)
                    {
                        var source = new CollectionMatrixProbe.AsyncSource<UnitResult<string?>>(mode == 4 ? [] : [value])
                        { MoveFault = moveFault, DisposeFault = disposeFault, MoveGate = moveGate, DisposeGate = disposeGate, CanceledProducer = canceledProducer };
                        return new(source.SequenceAsync(token).AsTask(), source, () => calls, null, () => { }, () => 0);
                    }
                    var input = new CollectionMatrixProbe.AsyncSource<int>(mode == 4 ? [] : [1])
                    { MoveFault = moveFault, DisposeFault = disposeFault, MoveGate = moveGate, DisposeGate = disposeGate, CanceledProducer = canceledProducer };
                    Task operation = identity switch
                    {
                        36 => input.TraverseAsync((Func<int, UnitResult<string?>>)(_ => { calls++; if (selectorFault is not null) throw selectorFault; return value; }), token).AsTask(),
                        39 => input.TraverseValueAsync((Func<int, ValueTask<UnitResult<string?>>>)(item => Select(item, token)), token).AsTask(),
                        _ => input.TraverseValueAsync((Func<int, CancellationToken, ValueTask<UnitResult<string?>>>)Select, token).AsTask(),
                    };
                    return new(operation, input, () => calls, gate.Subscribed.Task, () => gate.Complete(value), () => gate.Consumptions);
                }
            case 45:
            case 41:
            case 44:
            case 43:
                {
#pragma warning disable FS1001 // Deliberate uninitialized carrier fixture.
                    Validation<string?, string?> value = mode switch { 1 => Validation<string?, string?>.InvalidMany([null, "second-error"]), 2 => default, _ => Validation<string?, string?>.Valid(null) };
#pragma warning restore FS1001
                    var gate = new CollectionMatrixProbe.Gate<Validation<string?, string?>>();
                    var calls = 0;
                    ValueTask<Validation<string?, string?>> Select(int _, CancellationToken received)
                    {
                        calls++;
                        Assert.Equal(token, received);
                        if (selectorFault is not null)
                        {
                            var faulted = Task.FromException<Validation<string?, string?>>(selectorFault);
                            Assert.True(faulted.IsFaulted);
                            return new ValueTask<Validation<string?, string?>>(faulted);
                        }

                        return pendingSelector ? gate.Value : ValueTask.FromResult(value);
                    }
                    if (identity == 45)
                    {
                        var source = new CollectionMatrixProbe.AsyncSource<Validation<string?, string?>>(mode == 4 ? [] : [value])
                        { MoveFault = moveFault, DisposeFault = disposeFault, MoveGate = moveGate, DisposeGate = disposeGate, CanceledProducer = canceledProducer };
                        return new(source.SequenceAsync(token).AsTask(), source, () => calls, null, () => { }, () => 0);
                    }
                    var input = new CollectionMatrixProbe.AsyncSource<int>(mode == 4 ? [] : [1])
                    { MoveFault = moveFault, DisposeFault = disposeFault, MoveGate = moveGate, DisposeGate = disposeGate, CanceledProducer = canceledProducer };
                    Task operation = identity switch
                    {
                        41 => input.TraverseAsync((Func<int, Validation<string?, string?>>)(_ => { calls++; if (selectorFault is not null) throw selectorFault; return value; }), token).AsTask(),
                        44 => input.TraverseValueAsync((Func<int, ValueTask<Validation<string?, string?>>>)(item => Select(item, token)), token).AsTask(),
                        _ => input.TraverseValueAsync((Func<int, CancellationToken, ValueTask<Validation<string?, string?>>>)Select, token).AsTask(),
                    };
                    return new(operation, input, () => calls, gate.Subscribed.Task, () => gate.Complete(value), () => gate.Consumptions);
                }
            default: throw new ArgumentOutOfRangeException(nameof(identity));
        }
    }

    private static void Guard(int identity, int guard)
    {
        IAsyncEnumerable<int> source = guard is 0 or 2 ? null! : new CollectionMatrixProbe.AsyncSource<int>([1]);
        switch (identity)
        {
            case 23: _ = source.TraverseAsync((Func<int, Option<string?>>)(guard is 1 or 2 ? null! : _ => Option<string?>.Some("present")), TestContext.Current.CancellationToken); break;
            case 26: _ = source.TraverseValueAsync((Func<int, ValueTask<Option<string?>>>)(guard is 1 or 2 ? null! : _ => ValueTask.FromResult(Option<string?>.Some("present"))), TestContext.Current.CancellationToken); break;
            case 25: _ = source.TraverseValueAsync((Func<int, CancellationToken, ValueTask<Option<string?>>>)(guard is 1 or 2 ? null! : (_, _) => ValueTask.FromResult(Option<string?>.Some("present"))), TestContext.Current.CancellationToken); break;
            case 29: _ = source.TraverseAsync((Func<int, Result<string?, string?>>)(guard is 1 or 2 ? null! : _ => Result<string?, string?>.Success(null)), TestContext.Current.CancellationToken); break;
            case 32: _ = source.TraverseValueAsync((Func<int, ValueTask<Result<string?, string?>>>)(guard is 1 or 2 ? null! : _ => ValueTask.FromResult(Result<string?, string?>.Success(null))), TestContext.Current.CancellationToken); break;
            case 31: _ = source.TraverseValueAsync((Func<int, CancellationToken, ValueTask<Result<string?, string?>>>)(guard is 1 or 2 ? null! : (_, _) => ValueTask.FromResult(Result<string?, string?>.Success(null))), TestContext.Current.CancellationToken); break;
            case 36: _ = source.TraverseAsync((Func<int, UnitResult<string?>>)(guard is 1 or 2 ? null! : _ => UnitResult<string?>.Success()), TestContext.Current.CancellationToken); break;
            case 39: _ = source.TraverseValueAsync((Func<int, ValueTask<UnitResult<string?>>>)(guard is 1 or 2 ? null! : _ => ValueTask.FromResult(UnitResult<string?>.Success())), TestContext.Current.CancellationToken); break;
            case 38: _ = source.TraverseValueAsync((Func<int, CancellationToken, ValueTask<UnitResult<string?>>>)(guard is 1 or 2 ? null! : (_, _) => ValueTask.FromResult(UnitResult<string?>.Success())), TestContext.Current.CancellationToken); break;
            case 41: _ = source.TraverseAsync((Func<int, Validation<string?, string?>>)(guard is 1 or 2 ? null! : _ => Validation<string?, string?>.Valid(null)), TestContext.Current.CancellationToken); break;
            case 44: _ = source.TraverseValueAsync((Func<int, ValueTask<Validation<string?, string?>>>)(guard is 1 or 2 ? null! : _ => ValueTask.FromResult(Validation<string?, string?>.Valid(null))), TestContext.Current.CancellationToken); break;
            case 43: _ = source.TraverseValueAsync((Func<int, CancellationToken, ValueTask<Validation<string?, string?>>>)(guard is 1 or 2 ? null! : (_, _) => ValueTask.FromResult(Validation<string?, string?>.Valid(null))), TestContext.Current.CancellationToken); break;
            default: throw new ArgumentOutOfRangeException(nameof(identity));
        }
    }
}

