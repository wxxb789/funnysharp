namespace FunnySharp.Tests;

#pragma warning disable xUnit1051

public sealed class UnitResultAsyncTests
{
    [Fact]
    public async Task TaskMapAndBindTransformSuccessAndShortCircuitFailure()
    {
        var error = new InvalidOperationException("failed");
        var mapCalls = 0;
        var bindCalls = 0;

        var mapped = UnitResult<string>.Success().MapAsync(() =>
        {
            mapCalls++;
            return Task.FromResult(42);
        });
        var bound = UnitResult<string>.Success().BindAsync(() =>
        {
            bindCalls++;
            return Task.FromResult(UnitResult<string>.Failure("bound"));
        });
        var failure = UnitResult<Exception>.Failure(error);
        var failedMap = failure.MapAsync(() =>
        {
            mapCalls++;
            return Task.FromResult(0);
        });
        var failedBind = failure.BindAsync(() =>
        {
            bindCalls++;
            return Task.FromResult(UnitResult<Exception>.Success());
        });

        Assert.Equal(Result<int, string>.Success(42), await mapped);
        Assert.Equal(UnitResult<string>.Failure("bound"), await bound);
        Assert.True(failedMap.IsCompletedSuccessfully);
        Assert.True(failedBind.IsCompletedSuccessfully);
        Assert.True((await failedMap).TryGetError(out var mapError));
        Assert.Same(error, mapError);
        Assert.True((await failedBind).TryGetError(out var bindError));
        Assert.Same(error, bindError);
        Assert.Equal(1, mapCalls);
        Assert.Equal(1, bindCalls);
    }

    [Fact]
    public async Task ValueTaskMapAndBindTransformSuccessAndShortCircuitFailure()
    {
        var error = new InvalidOperationException("failed");
        var mapCalls = 0;
        var bindCalls = 0;
        var mapSource = new CountingValueTaskSource<int>(42);
        var bindSource = new CountingValueTaskSource<UnitResult<string>>(UnitResult<string>.Failure("bound"));

        var mapped = await UnitResult<string>.Success().MapValueAsync(() =>
        {
            mapCalls++;
            return mapSource.CreateValueTask();
        });
        var bound = await UnitResult<string>.Success().BindValueAsync(() =>
        {
            bindCalls++;
            return bindSource.CreateValueTask();
        });
        var failure = UnitResult<Exception>.Failure(error);
        var failedMap = failure.MapValueAsync(() =>
        {
            mapCalls++;
            return mapSource.CreateValueTask();
        });
        var failedBind = failure.BindValueAsync(() =>
        {
            bindCalls++;
            return ValueTask.FromResult(UnitResult<Exception>.Success());
        });

        Assert.Equal(Result<int, string>.Success(42), mapped);
        Assert.Equal(UnitResult<string>.Failure("bound"), bound);
        Assert.True(failedMap.IsCompletedSuccessfully);
        Assert.True(failedBind.IsCompletedSuccessfully);
        Assert.True((await failedMap).TryGetError(out var mapError));
        Assert.Same(error, mapError);
        Assert.True((await failedBind).TryGetError(out var bindError));
        Assert.Same(error, bindError);
        Assert.Equal(1, mapSource.GetResultCount);
        Assert.Equal(1, bindSource.GetResultCount);
        Assert.Equal(1, mapCalls);
        Assert.Equal(1, bindCalls);
    }

    [Fact]
    public async Task PendingValueTaskCallbacksDoNotBlockTheCaller()
    {
        var mapSource = new CountingValueTaskSource<int>();
        var bindSource = new CountingValueTaskSource<UnitResult<string>>();

        var mapped = UnitResult<string>.Success().MapValueAsync(() => mapSource.CreateValueTask());
        var bound = UnitResult<string>.Success().BindValueAsync(() => bindSource.CreateValueTask());

        Assert.False(mapped.IsCompleted);
        Assert.False(bound.IsCompleted);

        mapSource.SetResult(7);
        bindSource.SetResult(UnitResult<string>.Success());

        Assert.Equal(Result<int, string>.Success(7), await mapped);
        Assert.Equal(UnitResult<string>.Success(), await bound);
        Assert.Equal(1, mapSource.GetResultCount);
        Assert.Equal(1, bindSource.GetResultCount);
    }

    [Fact]
    public async Task CancellationAwareCallbacksReceiveTheExactTokenWithoutEagerCancellation()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var observedTokens = new List<CancellationToken>();

        var taskMapped = await UnitResult<string>.Success().MapAsync(
            token =>
            {
                observedTokens.Add(token);
                return Task.FromResult(1);
            },
            cancellationSource.Token);
        var taskBound = await UnitResult<string>.Success().BindAsync(
            token =>
            {
                observedTokens.Add(token);
                return Task.FromResult(UnitResult<string>.Success());
            },
            cancellationSource.Token);
        var valueMapped = await UnitResult<string>.Success().MapValueAsync(
            token =>
            {
                observedTokens.Add(token);
                return ValueTask.FromResult(2);
            },
            cancellationSource.Token);
        var valueBound = await UnitResult<string>.Success().BindValueAsync(
            token =>
            {
                observedTokens.Add(token);
                return ValueTask.FromResult(UnitResult<string>.Success());
            },
            cancellationSource.Token);

        Assert.Equal(Result<int, string>.Success(1), taskMapped);
        Assert.Equal(UnitResult<string>.Success(), taskBound);
        Assert.Equal(Result<int, string>.Success(2), valueMapped);
        Assert.Equal(UnitResult<string>.Success(), valueBound);
        Assert.Equal(4, observedTokens.Count);
        Assert.All(observedTokens, token => Assert.Equal(cancellationSource.Token, token));
    }

    [Fact]
    public async Task FailureShortCircuitsWithoutInvokingCallbacksOrInspectingTheToken()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var error = new InvalidOperationException("failed");
        var failure = UnitResult<Exception>.Failure(error);
        var calls = 0;

        var taskMapped = await failure.MapAsync(
            token =>
            {
                calls++;
                return Task.FromResult(0);
            },
            cancellationSource.Token);
        var taskBound = await failure.BindAsync(
            token =>
            {
                calls++;
                return Task.FromResult(UnitResult<Exception>.Success());
            },
            cancellationSource.Token);
        var valueMapped = await failure.MapValueAsync(
            token =>
            {
                calls++;
                return ValueTask.FromResult(0);
            },
            cancellationSource.Token);
        var valueBound = await failure.BindValueAsync(
            token =>
            {
                calls++;
                return ValueTask.FromResult(UnitResult<Exception>.Success());
            },
            cancellationSource.Token);

        Assert.True(taskMapped.TryGetError(out var taskMapError));
        Assert.Same(error, taskMapError);
        Assert.True(taskBound.TryGetError(out var taskBindError));
        Assert.Same(error, taskBindError);
        Assert.True(valueMapped.TryGetError(out var valueMapError));
        Assert.Same(error, valueMapError);
        Assert.True(valueBound.TryGetError(out var valueBindError));
        Assert.Same(error, valueBindError);
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task AsyncCallbacksPreserveFaultIdentity()
    {
        var taskMapFailure = new InvalidOperationException("task map");
        var taskBindFailure = new InvalidOperationException("task bind");
        var valueMapFailure = new InvalidOperationException("value map");
        var valueBindFailure = new InvalidOperationException("value bind");

        var taskMap = UnitResult<string>.Success().MapAsync(() => Task.FromException<int>(taskMapFailure));
        var taskBind = UnitResult<string>.Success().BindAsync(
            () => Task.FromException<UnitResult<string>>(taskBindFailure));
        var valueMap = UnitResult<string>.Success().MapValueAsync(
            () => ValueTask.FromException<int>(valueMapFailure));
        var valueBind = UnitResult<string>.Success().BindValueAsync(
            () => ValueTask.FromException<UnitResult<string>>(valueBindFailure));

        Assert.Same(taskMapFailure, await Assert.ThrowsAsync<InvalidOperationException>(() => taskMap));
        Assert.Same(taskBindFailure, await Assert.ThrowsAsync<InvalidOperationException>(() => taskBind));
        Assert.Same(
            valueMapFailure,
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await valueMap));
        Assert.Same(
            valueBindFailure,
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await valueBind));
    }

    [Fact]
    public async Task AsyncCallbacksPreserveCancellationStatusAndToken()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        var taskMap = UnitResult<string>.Success().MapAsync(
            () => Task.FromCanceled<int>(cancellationSource.Token));
        var taskBind = UnitResult<string>.Success().BindAsync(
            () => Task.FromCanceled<UnitResult<string>>(cancellationSource.Token));
        var valueMap = UnitResult<string>.Success().MapValueAsync(
            () => ValueTask.FromCanceled<int>(cancellationSource.Token));
        var valueBind = UnitResult<string>.Success().BindValueAsync(
            () => ValueTask.FromCanceled<UnitResult<string>>(cancellationSource.Token));
        var taskMapWithToken = UnitResult<string>.Success().MapAsync(
            token => Task.FromCanceled<int>(token),
            cancellationSource.Token);
        var taskBindWithToken = UnitResult<string>.Success().BindAsync(
            token => Task.FromCanceled<UnitResult<string>>(token),
            cancellationSource.Token);
        var valueMapWithToken = UnitResult<string>.Success().MapValueAsync(
            token => ValueTask.FromCanceled<int>(token),
            cancellationSource.Token);
        var valueBindWithToken = UnitResult<string>.Success().BindValueAsync(
            token => ValueTask.FromCanceled<UnitResult<string>>(token),
            cancellationSource.Token);

        var taskMapCancellation = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => taskMap);
        var taskBindCancellation = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => taskBind);
        var valueMapCancellation = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await valueMap);
        var valueBindCancellation = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await valueBind);
        var taskMapWithTokenCancellation = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => taskMapWithToken);
        var taskBindWithTokenCancellation = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => taskBindWithToken);
        var valueMapWithTokenCancellation = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await valueMapWithToken);
        var valueBindWithTokenCancellation = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await valueBindWithToken);

        Assert.True(taskMap.IsCanceled);
        Assert.True(taskBind.IsCanceled);
        Assert.True(valueMap.IsCanceled);
        Assert.True(valueBind.IsCanceled);
        Assert.True(taskMapWithToken.IsCanceled);
        Assert.True(taskBindWithToken.IsCanceled);
        Assert.True(valueMapWithToken.IsCanceled);
        Assert.True(valueBindWithToken.IsCanceled);
        Assert.Equal(cancellationSource.Token, taskMapCancellation.CancellationToken);
        Assert.Equal(cancellationSource.Token, taskBindCancellation.CancellationToken);
        Assert.Equal(cancellationSource.Token, valueMapCancellation.CancellationToken);
        Assert.Equal(cancellationSource.Token, valueBindCancellation.CancellationToken);
        Assert.Equal(cancellationSource.Token, taskMapWithTokenCancellation.CancellationToken);
        Assert.Equal(cancellationSource.Token, taskBindWithTokenCancellation.CancellationToken);
        Assert.Equal(cancellationSource.Token, valueMapWithTokenCancellation.CancellationToken);
        Assert.Equal(cancellationSource.Token, valueBindWithTokenCancellation.CancellationToken);
    }

    [Fact]
    public async Task SynchronousCallbackExceptionsFaultTheReturnedAwaitable()
    {
        var taskMapFailure = new InvalidOperationException("task map");
        var taskBindFailure = new InvalidOperationException("task bind");
        var valueMapFailure = new InvalidOperationException("value map");
        var valueBindFailure = new InvalidOperationException("value bind");
        Task<Result<int, string>> taskMap = null!;
        Task<UnitResult<string>> taskBind = null!;
        ValueTask<Result<int, string>> valueMap = default;
        ValueTask<UnitResult<string>> valueBind = default;

        var taskMapCallException = Record.Exception(() =>
        {
            taskMap = UnitResult<string>.Success().MapAsync<string, int>(() => throw taskMapFailure);
        });
        var taskBindCallException = Record.Exception(() =>
        {
            taskBind = UnitResult<string>.Success().BindAsync<string>(() => throw taskBindFailure);
        });
        var valueMapCallException = Record.Exception(() =>
        {
            valueMap = UnitResult<string>.Success().MapValueAsync<string, int>(() => throw valueMapFailure);
        });
        var valueBindCallException = Record.Exception(() =>
        {
            valueBind = UnitResult<string>.Success().BindValueAsync<string>(() => throw valueBindFailure);
        });

        Assert.Null(taskMapCallException);
        Assert.Null(taskBindCallException);
        Assert.Null(valueMapCallException);
        Assert.Null(valueBindCallException);
        Assert.Same(taskMapFailure, await Assert.ThrowsAsync<InvalidOperationException>(() => taskMap));
        Assert.Same(taskBindFailure, await Assert.ThrowsAsync<InvalidOperationException>(() => taskBind));
        Assert.Same(
            valueMapFailure,
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await valueMap));
        Assert.Same(
            valueBindFailure,
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await valueBind));
        Assert.True(taskMap.IsFaulted);
        Assert.True(taskBind.IsFaulted);
        Assert.True(valueMap.IsFaulted);
        Assert.True(valueBind.IsFaulted);
    }

    [Fact]
    public async Task SynchronousCallbackCancellationsCompleteAsCanceled()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var cancellations = new[]
        {
            new OperationCanceledException(cancellationSource.Token),
            new OperationCanceledException(cancellationSource.Token),
            new OperationCanceledException(cancellationSource.Token),
            new OperationCanceledException(cancellationSource.Token),
        };
        var operations = new Task[]
        {
            UnitResult<string>.Success().MapAsync<string, int>(
                () => ThrowCancellation<Task<int>>(cancellations[0])),
            UnitResult<string>.Success().BindAsync<string>(
                () => ThrowCancellation<Task<UnitResult<string>>>(cancellations[1])),
            UnitResult<string>.Success().MapValueAsync<string, int>(
                () => ThrowCancellation<ValueTask<int>>(cancellations[2])).AsTask(),
            UnitResult<string>.Success().BindValueAsync<string>(
                () => ThrowCancellation<ValueTask<UnitResult<string>>>(cancellations[3])).AsTask(),
        };

        for (var index = 0; index < operations.Length; index++)
        {
            var actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operations[index]);

            Assert.True(operations[index].IsCanceled);
            Assert.Same(cancellations[index], actual);
            Assert.Equal(cancellationSource.Token, actual.CancellationToken);
        }
    }

    [Fact]
    public void NullDelegatesAreRejectedAtCallTimeBeforeTheCarrierIsInspected()
    {
        var failure = UnitResult<string>.Failure("bad");
        UnitResult<string> uninitialized = default;

        Assert.Throws<ArgumentNullException>(() => { _ = failure.MapAsync<string, int>(null!); });
        Assert.Throws<ArgumentNullException>(() => { _ = failure.MapAsync<string, int>(null!, CancellationToken.None); });
        Assert.Throws<ArgumentNullException>(() => { _ = failure.MapValueAsync<string, int>(null!); });
        Assert.Throws<ArgumentNullException>(() => { _ = failure.MapValueAsync<string, int>(null!, CancellationToken.None); });
        Assert.Throws<ArgumentNullException>(() => { _ = failure.BindAsync<string>(null!); });
        Assert.Throws<ArgumentNullException>(() => { _ = failure.BindAsync<string>(null!, CancellationToken.None); });
        Assert.Throws<ArgumentNullException>(() => { _ = failure.BindValueAsync<string>(null!); });
        Assert.Throws<ArgumentNullException>(() => { _ = failure.BindValueAsync<string>(null!, CancellationToken.None); });

        Assert.Throws<ArgumentNullException>(() => { _ = uninitialized.MapAsync<string, int>(null!); });
        Assert.Throws<ArgumentNullException>(() => { _ = uninitialized.BindAsync<string>(null!); });
        Assert.Throws<ArgumentNullException>(() => { _ = uninitialized.MapValueAsync<string, int>(null!); });
        Assert.Throws<ArgumentNullException>(() => { _ = uninitialized.BindValueAsync<string>(null!); });
    }

    [Fact]
    public void DefaultCarrierThrowsSynchronouslyForValidDelegates()
    {
        UnitResult<string> uninitialized = default;
        var operations = new Action[]
        {
            () => { _ = uninitialized.MapAsync<string, int>(() => Task.FromResult(1)); },
            () => { _ = uninitialized.MapAsync<string, int>(token => Task.FromResult(1), CancellationToken.None); },
            () => { _ = uninitialized.MapValueAsync<string, int>(() => ValueTask.FromResult(1)); },
            () => { _ = uninitialized.MapValueAsync<string, int>(token => ValueTask.FromResult(1), CancellationToken.None); },
            () => { _ = uninitialized.BindAsync<string>(() => Task.FromResult(UnitResult<string>.Success())); },
            () => { _ = uninitialized.BindAsync<string>(token => Task.FromResult(UnitResult<string>.Success()), CancellationToken.None); },
            () => { _ = uninitialized.BindValueAsync<string>(() => ValueTask.FromResult(UnitResult<string>.Success())); },
            () => { _ = uninitialized.BindValueAsync<string>(token => ValueTask.FromResult(UnitResult<string>.Success()), CancellationToken.None); },
        };

        foreach (var operation in operations)
        {
            var exception = Assert.Throws<InvalidOperationException>(operation);

            Assert.Equal("The unit result has not been initialized.", exception.Message);
        }
    }

    private static TResult ThrowCancellation<TResult>(OperationCanceledException cancellation) =>
        throw cancellation;
}

#pragma warning restore xUnit1051
