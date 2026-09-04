namespace FunnySharp.Tests;

public sealed class ResultAsyncTests
{
    [Fact]
    public async Task TaskMapAndBindTransformSuccessAndShortCircuitFailure()
    {
        var mapCalls = 0;
        var bindCalls = 0;

        var mapped = await Result<int, string>.Success(2).MapAsync(value =>
        {
            mapCalls++;
            return Task.FromResult(value * 3);
        });
        var bound = await Result<int, string>.Success(2).BindAsync(value =>
        {
            bindCalls++;
            return Task.FromResult(Result<int, string>.Success(value * 4));
        });
        var failedMap = await Result<int, string>.Failure("bad").MapAsync(value =>
        {
            mapCalls++;
            return Task.FromResult(value * 3);
        });
        var failedBind = await Result<int, string>.Failure("bad").BindAsync(value =>
        {
            bindCalls++;
            return Task.FromResult(Result<int, string>.Success(value * 4));
        });

        Assert.Equal(Result<int, string>.Success(6), mapped);
        Assert.Equal(Result<int, string>.Success(8), bound);
        Assert.Equal(Result<int, string>.Failure("bad"), failedMap);
        Assert.Equal(Result<int, string>.Failure("bad"), failedBind);
        Assert.Equal(1, mapCalls);
        Assert.Equal(1, bindCalls);
    }

    [Fact]
    public async Task ValueTaskMapAndBindTransformSuccessAndShortCircuitFailure()
    {
        var mapCalls = 0;
        var bindCalls = 0;

        var mapped = await Result<int, string>.Success(2).MapValueAsync(value =>
        {
            mapCalls++;
            return ValueTask.FromResult(value * 3);
        });
        var bound = await Result<int, string>.Success(2).BindValueAsync(value =>
        {
            bindCalls++;
            return ValueTask.FromResult(Result<int, string>.Success(value * 4));
        });
        var failedMap = await Result<int, string>.Failure("bad").MapValueAsync(value =>
        {
            mapCalls++;
            return ValueTask.FromResult(value * 3);
        });
        var failedBind = await Result<int, string>.Failure("bad").BindValueAsync(value =>
        {
            bindCalls++;
            return ValueTask.FromResult(Result<int, string>.Success(value * 4));
        });

        Assert.Equal(Result<int, string>.Success(6), mapped);
        Assert.Equal(Result<int, string>.Success(8), bound);
        Assert.Equal(Result<int, string>.Failure("bad"), failedMap);
        Assert.Equal(Result<int, string>.Failure("bad"), failedBind);
        Assert.Equal(1, mapCalls);
        Assert.Equal(1, bindCalls);
    }

    [Fact]
    public async Task CancellationAwareCallbacksReceiveTheExactTokenWithoutEagerCancellation()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var observedTokens = new List<CancellationToken>();

        var taskMapped = await Result<int, string>.Success(2).MapAsync(
            (value, token) =>
            {
                observedTokens.Add(token);
                return Task.FromResult(value + 1);
            },
            cancellationSource.Token);
        var taskBound = await Result<int, string>.Success(2).BindAsync(
            (value, token) =>
            {
                observedTokens.Add(token);
                return Task.FromResult(Result<int, string>.Success(value + 2));
            },
            cancellationSource.Token);
        var valueMapped = await Result<int, string>.Success(2).MapValueAsync(
            (value, token) =>
            {
                observedTokens.Add(token);
                return ValueTask.FromResult(value + 3);
            },
            cancellationSource.Token);
        var valueBound = await Result<int, string>.Success(2).BindValueAsync(
            (value, token) =>
            {
                observedTokens.Add(token);
                return ValueTask.FromResult(Result<int, string>.Success(value + 4));
            },
            cancellationSource.Token);

        Assert.Equal(Result<int, string>.Success(3), taskMapped);
        Assert.Equal(Result<int, string>.Success(4), taskBound);
        Assert.Equal(Result<int, string>.Success(5), valueMapped);
        Assert.Equal(Result<int, string>.Success(6), valueBound);
        Assert.Equal(4, observedTokens.Count);
        Assert.All(observedTokens, token => Assert.Equal(cancellationSource.Token, token));
    }

    [Fact]
    public async Task FailureShortCircuitsWithoutInspectingCancellation()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var calls = 0;

        var taskMapped = await Result<int, string>.Failure("bad").MapAsync(
            (value, token) =>
            {
                calls++;
                return Task.FromResult(value);
            },
            cancellationSource.Token);
        var taskBound = await Result<int, string>.Failure("bad").BindAsync(
            (value, token) =>
            {
                calls++;
                return Task.FromResult(Result<int, string>.Success(value));
            },
            cancellationSource.Token);
        var valueMapped = await Result<int, string>.Failure("bad").MapValueAsync(
            (value, token) =>
            {
                calls++;
                return ValueTask.FromResult(value);
            },
            cancellationSource.Token);
        var valueBound = await Result<int, string>.Failure("bad").BindValueAsync(
            (value, token) =>
            {
                calls++;
                return ValueTask.FromResult(Result<int, string>.Success(value));
            },
            cancellationSource.Token);

        Assert.Equal(Result<int, string>.Failure("bad"), taskMapped);
        Assert.Equal(Result<int, string>.Failure("bad"), taskBound);
        Assert.Equal(Result<int, string>.Failure("bad"), valueMapped);
        Assert.Equal(Result<int, string>.Failure("bad"), valueBound);
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task AsyncCallbacksPreserveFaultIdentity()
    {
        var taskMapFailure = new InvalidOperationException("task map");
        var taskBindFailure = new InvalidOperationException("task bind");
        var valueMapFailure = new InvalidOperationException("value map");
        var valueBindFailure = new InvalidOperationException("value bind");

        var taskMap = Result<int, string>.Success(1).MapAsync<int, string, int>(
            _ => Task.FromException<int>(taskMapFailure));
        var taskBind = Result<int, string>.Success(1).BindAsync<int, string, int>(
            _ => Task.FromException<Result<int, string>>(taskBindFailure));
        var valueMap = Result<int, string>.Success(1).MapValueAsync<int, string, int>(
            _ => ValueTask.FromException<int>(valueMapFailure));
        var valueBind = Result<int, string>.Success(1).BindValueAsync<int, string, int>(
            _ => ValueTask.FromException<Result<int, string>>(valueBindFailure));

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
    public async Task AsyncCallbacksKeepFaultedOperationCanceledExceptionsFaulted()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var taskMapFailure = new OperationCanceledException(cancellationSource.Token);
        var taskBindFailure = new OperationCanceledException(cancellationSource.Token);
        var valueMapFailure = new OperationCanceledException(cancellationSource.Token);
        var valueBindFailure = new OperationCanceledException(cancellationSource.Token);
        var taskMapWithTokenFailure = new OperationCanceledException(cancellationSource.Token);
        var taskBindWithTokenFailure = new OperationCanceledException(cancellationSource.Token);
        var valueMapWithTokenFailure = new OperationCanceledException(cancellationSource.Token);
        var valueBindWithTokenFailure = new OperationCanceledException(cancellationSource.Token);

        var taskMap = Result<int, string>.Success(1).MapAsync<int, string, int>(
            _ => Task.FromException<int>(taskMapFailure));
        var taskBind = Result<int, string>.Success(1).BindAsync<int, string, int>(
            _ => Task.FromException<Result<int, string>>(taskBindFailure));
        var valueMap = Result<int, string>.Success(1).MapValueAsync<int, string, int>(
            _ => ValueTask.FromException<int>(valueMapFailure)).AsTask();
        var valueBind = Result<int, string>.Success(1).BindValueAsync<int, string, int>(
            _ => ValueTask.FromException<Result<int, string>>(valueBindFailure)).AsTask();
        var taskMapWithToken = Result<int, string>.Success(1).MapAsync(
            (_, _) => Task.FromException<int>(taskMapWithTokenFailure),
            cancellationSource.Token);
        var taskBindWithToken = Result<int, string>.Success(1).BindAsync(
            (_, _) => Task.FromException<Result<int, string>>(taskBindWithTokenFailure),
            cancellationSource.Token);
        var valueMapWithToken = Result<int, string>.Success(1).MapValueAsync(
            (_, _) => ValueTask.FromException<int>(valueMapWithTokenFailure),
            cancellationSource.Token).AsTask();
        var valueBindWithToken = Result<int, string>.Success(1).BindValueAsync(
            (_, _) => ValueTask.FromException<Result<int, string>>(valueBindWithTokenFailure),
            cancellationSource.Token).AsTask();

        Assert.Same(taskMapFailure, await Assert.ThrowsAnyAsync<OperationCanceledException>(() => taskMap));
        Assert.Same(taskBindFailure, await Assert.ThrowsAnyAsync<OperationCanceledException>(() => taskBind));
        Assert.Same(valueMapFailure, await Assert.ThrowsAnyAsync<OperationCanceledException>(() => valueMap));
        Assert.Same(valueBindFailure, await Assert.ThrowsAnyAsync<OperationCanceledException>(() => valueBind));
        Assert.Same(
            taskMapWithTokenFailure,
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => taskMapWithToken));
        Assert.Same(
            taskBindWithTokenFailure,
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => taskBindWithToken));
        Assert.Same(
            valueMapWithTokenFailure,
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => valueMapWithToken));
        Assert.Same(
            valueBindWithTokenFailure,
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => valueBindWithToken));
        Assert.True(taskMap.IsFaulted);
        Assert.True(taskBind.IsFaulted);
        Assert.True(valueMap.IsFaulted);
        Assert.True(valueBind.IsFaulted);
        Assert.True(taskMapWithToken.IsFaulted);
        Assert.True(taskBindWithToken.IsFaulted);
        Assert.True(valueMapWithToken.IsFaulted);
        Assert.True(valueBindWithToken.IsFaulted);
        Assert.False(taskMap.IsCanceled);
        Assert.False(taskBind.IsCanceled);
        Assert.False(valueMap.IsCanceled);
        Assert.False(valueBind.IsCanceled);
        Assert.False(taskMapWithToken.IsCanceled);
        Assert.False(taskBindWithToken.IsCanceled);
        Assert.False(valueMapWithToken.IsCanceled);
        Assert.False(valueBindWithToken.IsCanceled);
    }

    [Fact]
    public async Task PendingFaultedOperationCanceledExceptionsStayFaulted()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var taskFailure = new OperationCanceledException(cancellationSource.Token);
        var valueTaskFailure = new OperationCanceledException(cancellationSource.Token);
        var taskSource = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var valueTaskSource = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

        var task = Result<int, string>.Success(1).MapAsync(_ => taskSource.Task);
        var valueTask = Result<int, string>.Success(1)
            .MapValueAsync(_ => new ValueTask<int>(valueTaskSource.Task))
            .AsTask();

        taskSource.SetException(taskFailure);
        valueTaskSource.SetException(valueTaskFailure);

        Assert.Same(taskFailure, await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task));
        Assert.Same(
            valueTaskFailure,
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => valueTask));
        Assert.True(task.IsFaulted);
        Assert.True(valueTask.IsFaulted);
        Assert.False(task.IsCanceled);
        Assert.False(valueTask.IsCanceled);
    }

    [Fact]
    public async Task AsyncCallbacksPreserveCancellationStatusAndToken()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        var taskMap = Result<int, string>.Success(1).MapAsync<int, string, int>(
            _ => Task.FromCanceled<int>(cancellationSource.Token));
        var taskBind = Result<int, string>.Success(1).BindAsync<int, string, int>(
            _ => Task.FromCanceled<Result<int, string>>(cancellationSource.Token));
        var valueMap = Result<int, string>.Success(1).MapValueAsync<int, string, int>(
            _ => ValueTask.FromCanceled<int>(cancellationSource.Token));
        var valueBind = Result<int, string>.Success(1).BindValueAsync<int, string, int>(
            _ => ValueTask.FromCanceled<Result<int, string>>(cancellationSource.Token));

        var taskMapCancellation = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => taskMap);
        var taskBindCancellation = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => taskBind);
        var valueMapCancellation = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await valueMap);
        var valueBindCancellation = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await valueBind);

        Assert.True(taskMap.IsCanceled);
        Assert.True(taskBind.IsCanceled);
        Assert.True(valueMap.IsCanceled);
        Assert.True(valueBind.IsCanceled);
        Assert.Equal(cancellationSource.Token, taskMapCancellation.CancellationToken);
        Assert.Equal(cancellationSource.Token, taskBindCancellation.CancellationToken);
        Assert.Equal(cancellationSource.Token, valueMapCancellation.CancellationToken);
        Assert.Equal(cancellationSource.Token, valueBindCancellation.CancellationToken);
    }

    [Fact]
    public async Task CancellationAwareAsyncCallbacksPreserveCancellationStatusAndToken()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        var taskMap = Result<int, string>.Success(1).MapAsync(
            (_, token) => Task.FromCanceled<int>(token),
            cancellationSource.Token);
        var taskBind = Result<int, string>.Success(1).BindAsync(
            (_, token) => Task.FromCanceled<Result<int, string>>(token),
            cancellationSource.Token);
        var valueMap = Result<int, string>.Success(1).MapValueAsync(
            (_, token) => ValueTask.FromCanceled<int>(token),
            cancellationSource.Token);
        var valueBind = Result<int, string>.Success(1).BindValueAsync(
            (_, token) => ValueTask.FromCanceled<Result<int, string>>(token),
            cancellationSource.Token);

        var taskMapCancellation = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => taskMap);
        var taskBindCancellation = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => taskBind);
        var valueMapCancellation = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await valueMap);
        var valueBindCancellation = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await valueBind);

        Assert.True(taskMap.IsCanceled);
        Assert.True(taskBind.IsCanceled);
        Assert.True(valueMap.IsCanceled);
        Assert.True(valueBind.IsCanceled);
        Assert.Equal(cancellationSource.Token, taskMapCancellation.CancellationToken);
        Assert.Equal(cancellationSource.Token, taskBindCancellation.CancellationToken);
        Assert.Equal(cancellationSource.Token, valueMapCancellation.CancellationToken);
        Assert.Equal(cancellationSource.Token, valueBindCancellation.CancellationToken);
    }

    [Fact]
    public async Task SynchronousCallbackExceptionsAreReturnedThroughTheAwaitable()
    {
        var taskFailure = new InvalidOperationException("task callback");
        var valueFailure = new InvalidOperationException("value callback");
        Task<Result<int, string>> task = null!;
        ValueTask<Result<int, string>> valueTask = default;

        var taskCallException = Record.Exception(() =>
        {
            task = Result<int, string>.Success(1).MapAsync<int, string, int>(
                _ => throw taskFailure);
        });
        var valueCallException = Record.Exception(() =>
        {
            valueTask = Result<int, string>.Success(1).MapValueAsync<int, string, int>(
                _ => throw valueFailure);
        });

        Assert.Null(taskCallException);
        Assert.Null(valueCallException);
        Assert.Same(taskFailure, await Assert.ThrowsAsync<InvalidOperationException>(() => task));
        Assert.Same(
            valueFailure,
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await valueTask));
    }

    [Fact]
    public async Task SynchronousCallbacksPreserveCancellationDiagnostics()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var cancellations = Enumerable.Range(0, 4)
            .Select(_ => new OperationCanceledException(cancellationSource.Token))
            .ToArray();
        var operations = new Task<Result<int, string>>[]
        {
            Result<int, string>.Success(1)
                .MapAsync<int, string, int>(_ => ThrowCancellation<Task<int>>(cancellations[0])),
            Result<int, string>.Success(1)
                .BindAsync<int, string, int>(_ => ThrowCancellation<Task<Result<int, string>>>(cancellations[1])),
            Result<int, string>.Success(1)
                .MapValueAsync<int, string, int>(_ => ThrowCancellation<ValueTask<int>>(cancellations[2]))
                .AsTask(),
            Result<int, string>.Success(1)
                .BindValueAsync<int, string, int>(
                    _ => ThrowCancellation<ValueTask<Result<int, string>>>(cancellations[3]))
                .AsTask(),
        };

        for (var index = 0; index < operations.Length; index++)
        {
            var actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operations[index]);

            Assert.True(operations[index].IsCanceled);
            Assert.Same(cancellations[index], actual);
            Assert.Equal(cancellationSource.Token, actual.CancellationToken);
            Assert.Contains(nameof(ThrowCancellation), actual.StackTrace);
        }
    }

    [Fact]
    public async Task PendingCallbacksDoNotBlockTheCaller()
    {
        var taskMapSource = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var taskBindSource = new TaskCompletionSource<Result<int, string>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var valueMapSource = new CountingValueTaskSource<int>();
        var valueBindSource = new CountingValueTaskSource<Result<int, string>>();

        var taskMap = Result<int, string>.Success(1).MapAsync(_ => taskMapSource.Task);
        var taskBind = Result<int, string>.Success(1).BindAsync(_ => taskBindSource.Task);
        var valueMap = Result<int, string>.Success(1).MapValueAsync(_ => valueMapSource.CreateValueTask());
        var valueBind = Result<int, string>.Success(1).BindValueAsync(_ => valueBindSource.CreateValueTask());

        Assert.False(taskMap.IsCompleted);
        Assert.False(taskBind.IsCompleted);
        Assert.False(valueMap.IsCompleted);
        Assert.False(valueBind.IsCompleted);

        taskMapSource.SetResult(2);
        taskBindSource.SetResult(Result<int, string>.Success(3));
        valueMapSource.SetResult(4);
        valueBindSource.SetResult(Result<int, string>.Success(5));

        Assert.Equal(Result<int, string>.Success(2), await taskMap);
        Assert.Equal(Result<int, string>.Success(3), await taskBind);
        Assert.Equal(Result<int, string>.Success(4), await valueMap);
        Assert.Equal(Result<int, string>.Success(5), await valueBind);
        Assert.Equal(1, valueMapSource.GetResultCount);
        Assert.Equal(1, valueBindSource.GetResultCount);
    }

    [Fact]
    public void AsyncCombinatorsValidateCallbacksSynchronously()
    {
        var failure = Result<int, string>.Failure("bad");

        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = ResultExtensions.MapAsync<int, string, int>(failure, null!);
        });
        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = ResultExtensions.BindAsync<int, string, int>(failure, null!);
        });
        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = ResultExtensions.MapValueAsync<int, string, int>(failure, null!);
        });
        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = ResultExtensions.BindValueAsync<int, string, int>(failure, null!);
        });
        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = ResultExtensions.MapAsync<int, string, int>(failure, null!, CancellationToken.None);
        });
        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = ResultExtensions.BindAsync<int, string, int>(failure, null!, CancellationToken.None);
        });
        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = ResultExtensions.MapValueAsync<int, string, int>(failure, null!, CancellationToken.None);
        });
        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = ResultExtensions.BindValueAsync<int, string, int>(failure, null!, CancellationToken.None);
        });
    }

    private static TResult ThrowCancellation<TResult>(OperationCanceledException cancellation) =>
        throw cancellation;

}
