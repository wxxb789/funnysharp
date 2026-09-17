namespace FunnySharp.Tests;

public sealed class ValidationAsyncTests
{
    [Fact]
    public async Task TaskMapAsyncTransformsValidOnceAndShortCircuitsInvalidWithTheSameErrors()
    {
        var calls = 0;
        var mapped = await Validation<int, string>.Valid(2).MapAsync(value =>
        {
            calls++;
            return Task.FromResult(value * 3);
        });

        Assert.Equal(Validation<int, string>.Valid(6), mapped);
        Assert.Equal(1, calls);

        var invalid = Validation<int, string>.InvalidMany(["first", "second"]);
        var shortCircuited = invalid.MapAsync(value =>
        {
            calls++;
            return Task.FromResult(value * 3);
        });

        Assert.True(shortCircuited.IsCompletedSuccessfully);
        var shortCircuitedValue = await shortCircuited;

        Assert.True(shortCircuitedValue.TryGetErrors(out var errors));
        Assert.Equal(["first", "second"], errors);
        Assert.Equal(invalid, shortCircuitedValue);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task ValueTaskMapAsyncTransformsValidOnceAndShortCircuitsInvalidWithTheSameErrors()
    {
        var calls = 0;
        var mapped = await Validation<int, string>.Valid(4).MapValueAsync(value =>
        {
            calls++;
            return ValueTask.FromResult(value + 1);
        });

        Assert.Equal(Validation<int, string>.Valid(5), mapped);
        Assert.Equal(1, calls);

        var invalid = Validation<int, string>.InvalidMany(["first", "second"]);
        var shortCircuited = invalid.MapValueAsync(value =>
        {
            calls++;
            return ValueTask.FromResult(value + 1);
        });

        Assert.True(shortCircuited.IsCompletedSuccessfully);
        var shortCircuitedValue = await shortCircuited;

        Assert.True(shortCircuitedValue.TryGetErrors(out var errors));
        Assert.Equal(["first", "second"], errors);
        Assert.Equal(invalid, shortCircuitedValue);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task CancellationAwareMapAsyncForwardsTheExactTokenAndSkipsInvalidValidations()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var observedTokens = new List<CancellationToken>();
        var calls = 0;

        var taskMapped = await Validation<int, string>.Valid(2).MapAsync(
            (value, token) =>
            {
                observedTokens.Add(token);
                calls++;
                return Task.FromResult(value + 1);
            },
            cancellationSource.Token);
        var valueMapped = await Validation<int, string>.Valid(2).MapValueAsync(
            (value, token) =>
            {
                observedTokens.Add(token);
                calls++;
                return ValueTask.FromResult(value + 2);
            },
            cancellationSource.Token);

        var invalid = Validation<int, string>.InvalidMany(["first", "second"]);
        var taskShortCircuited = invalid.MapAsync(
            (value, token) =>
            {
                calls++;
                return Task.FromResult(value);
            },
            cancellationSource.Token);
        var valueShortCircuited = invalid.MapValueAsync(
            (value, token) =>
            {
                calls++;
                return ValueTask.FromResult(value);
            },
            cancellationSource.Token);

        Assert.Equal(Validation<int, string>.Valid(3), taskMapped);
        Assert.Equal(Validation<int, string>.Valid(4), valueMapped);
        Assert.Equal(2, observedTokens.Count);
        Assert.All(observedTokens, token => Assert.Equal(cancellationSource.Token, token));
        Assert.True(taskShortCircuited.IsCompletedSuccessfully);
        Assert.True(valueShortCircuited.IsCompletedSuccessfully);
        Assert.Equal(Validation<int, string>.InvalidMany(["first", "second"]), await taskShortCircuited);
        Assert.Equal(Validation<int, string>.InvalidMany(["first", "second"]), await valueShortCircuited);
        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task FaultedSelectorsPreserveTheExactExceptionInstance()
    {
        var taskFailure = new InvalidOperationException("task selector");
        var taskWithTokenFailure = new InvalidOperationException("task selector with token");
        var valueFailure = new InvalidOperationException("value selector");
        var valueWithTokenFailure = new InvalidOperationException("value selector with token");

        var taskMapped = Validation<int, string>.Valid(1)
            .MapAsync<int, int, string>(_ => Task.FromException<int>(taskFailure));
        var taskWithTokenMapped = Validation<int, string>.Valid(1)
            .MapAsync<int, int, string>(
                (_, _) => Task.FromException<int>(taskWithTokenFailure),
                CancellationToken.None);
        var valueMapped = Validation<int, string>.Valid(1)
            .MapValueAsync<int, int, string>(_ => ValueTask.FromException<int>(valueFailure));
        var valueWithTokenMapped = Validation<int, string>.Valid(1)
            .MapValueAsync<int, int, string>(
                (_, _) => ValueTask.FromException<int>(valueWithTokenFailure),
                CancellationToken.None);

        Assert.Same(taskFailure, await Assert.ThrowsAsync<InvalidOperationException>(() => taskMapped));
        Assert.Same(
            taskWithTokenFailure,
            await Assert.ThrowsAsync<InvalidOperationException>(() => taskWithTokenMapped));
        Assert.Same(
            valueFailure,
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await valueMapped));
        Assert.Same(
            valueWithTokenFailure,
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await valueWithTokenMapped));
    }

    [Fact]
    public async Task SynchronousSelectorExceptionsFaultTheReturnedAwaitable()
    {
        var taskFailure = new InvalidOperationException("task selector");
        var taskWithTokenFailure = new InvalidOperationException("task selector with token");
        var valueFailure = new InvalidOperationException("value selector");
        var valueWithTokenFailure = new InvalidOperationException("value selector with token");
        Task<Validation<int, string>> task = null!;
        Task<Validation<int, string>> taskWithToken = null!;
        ValueTask<Validation<int, string>> valueTask = default;
        ValueTask<Validation<int, string>> valueTaskWithToken = default;

        Assert.Null(Record.Exception(() =>
        {
            task = Validation<int, string>.Valid(1)
                .MapAsync<int, int, string>(_ => throw taskFailure);
        }));
        Assert.Null(Record.Exception(() =>
        {
            taskWithToken = Validation<int, string>.Valid(1)
                .MapAsync<int, int, string>(
                    (_, _) => throw taskWithTokenFailure,
                    CancellationToken.None);
        }));
        Assert.Null(Record.Exception(() =>
        {
            valueTask = Validation<int, string>.Valid(1)
                .MapValueAsync<int, int, string>(_ => throw valueFailure);
        }));
        Assert.Null(Record.Exception(() =>
        {
            valueTaskWithToken = Validation<int, string>.Valid(1)
                .MapValueAsync<int, int, string>(
                    (_, _) => throw valueWithTokenFailure,
                    CancellationToken.None);
        }));

        Assert.Same(taskFailure, await Assert.ThrowsAsync<InvalidOperationException>(() => task));
        Assert.Same(
            taskWithTokenFailure,
            await Assert.ThrowsAsync<InvalidOperationException>(() => taskWithToken));
        Assert.Same(
            valueFailure,
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await valueTask));
        Assert.Same(
            valueWithTokenFailure,
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await valueTaskWithToken));
    }

    [Fact]
    public async Task SynchronousSelectorCancellationsProduceCanceledOperationsWithTheToken()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var cancellations = Enumerable.Range(0, 4)
            .Select(_ => new OperationCanceledException(cancellationSource.Token))
            .ToArray();
        var operations = new Task<Validation<int, string>>[]
        {
            Validation<int, string>.Valid(1)
                .MapAsync<int, int, string>(_ => ThrowCancellation<Task<int>>(cancellations[0])),
            Validation<int, string>.Valid(1)
                .MapValueAsync<int, int, string>(_ => ThrowCancellation<ValueTask<int>>(cancellations[1]))
                .AsTask(),
            Validation<int, string>.Valid(1)
                .MapAsync<int, int, string>(
                    (_, _) => ThrowCancellation<Task<int>>(cancellations[2]),
                    cancellationSource.Token),
            Validation<int, string>.Valid(1)
                .MapValueAsync<int, int, string>(
                    (_, _) => ThrowCancellation<ValueTask<int>>(cancellations[3]),
                    cancellationSource.Token)
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
    public async Task CanceledSelectorsPreserveCancellationStatusAndToken()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        var taskMapped = Validation<int, string>.Valid(1)
            .MapAsync<int, int, string>(_ => Task.FromCanceled<int>(cancellationSource.Token));
        var valueMapped = Validation<int, string>.Valid(1)
            .MapValueAsync<int, int, string>(_ => ValueTask.FromCanceled<int>(cancellationSource.Token));
        var taskWithTokenMapped = Validation<int, string>.Valid(1)
            .MapAsync<int, int, string>(
                (_, token) => Task.FromCanceled<int>(token),
                cancellationSource.Token);
        var valueWithTokenMapped = Validation<int, string>.Valid(1)
            .MapValueAsync<int, int, string>(
                (_, token) => ValueTask.FromCanceled<int>(token),
                cancellationSource.Token);

        var taskCancellation = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => taskMapped);
        var valueCancellation = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await valueMapped);
        var taskWithTokenCancellation = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => taskWithTokenMapped);
        var valueWithTokenCancellation = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await valueWithTokenMapped);

        Assert.True(taskMapped.IsCanceled);
        Assert.True(valueMapped.IsCanceled);
        Assert.True(taskWithTokenMapped.IsCanceled);
        Assert.True(valueWithTokenMapped.IsCanceled);
        Assert.False(taskMapped.IsFaulted);
        Assert.False(valueMapped.IsFaulted);
        Assert.False(taskWithTokenMapped.IsFaulted);
        Assert.False(valueWithTokenMapped.IsFaulted);
        Assert.Equal(cancellationSource.Token, taskCancellation.CancellationToken);
        Assert.Equal(cancellationSource.Token, valueCancellation.CancellationToken);
        Assert.Equal(cancellationSource.Token, taskWithTokenCancellation.CancellationToken);
        Assert.Equal(cancellationSource.Token, valueWithTokenCancellation.CancellationToken);
    }

    [Fact]
    public void NullSelectorsAndUninitializedReceiversAreRejectedAtCallTime()
    {
        var invalid = Validation<int, string>.Invalid("bad");
        Validation<int, string> uninitialized = default;

        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = ValidationExtensions.MapAsync<int, int, string>(invalid, null!);
        });
        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = ValidationExtensions.MapAsync<int, int, string>(invalid, null!, CancellationToken.None);
        });
        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = ValidationExtensions.MapValueAsync<int, int, string>(invalid, null!);
        });
        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = ValidationExtensions.MapValueAsync<int, int, string>(invalid, null!, CancellationToken.None);
        });

        var nullSelector = Assert.Throws<ArgumentNullException>(() =>
        {
            _ = ValidationExtensions.MapAsync<int, int, string>(uninitialized, null!);
        });

        Assert.Equal("selector", nullSelector.ParamName);
        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = ValidationExtensions.MapAsync<int, int, string>(uninitialized, null!, CancellationToken.None);
        });
        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = ValidationExtensions.MapValueAsync<int, int, string>(uninitialized, null!);
        });
        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = ValidationExtensions.MapValueAsync<int, int, string>(uninitialized, null!, CancellationToken.None);
        });

        var taskException = Assert.Throws<InvalidOperationException>(() =>
        {
            _ = ValidationExtensions.MapAsync<int, int, string>(
                uninitialized,
                _ => Task.FromResult(1));
        });
        Assert.Equal("The validation has not been initialized.", taskException.Message);
        Assert.Throws<InvalidOperationException>(() =>
        {
            _ = ValidationExtensions.MapAsync<int, int, string>(
                uninitialized,
                (_, _) => Task.FromResult(1),
                CancellationToken.None);
        });
        Assert.Throws<InvalidOperationException>(() =>
        {
            _ = ValidationExtensions.MapValueAsync<int, int, string>(
                uninitialized,
                _ => ValueTask.FromResult(1));
        });
        Assert.Throws<InvalidOperationException>(() =>
        {
            _ = ValidationExtensions.MapValueAsync<int, int, string>(
                uninitialized,
                (_, _) => ValueTask.FromResult(1),
                CancellationToken.None);
        });
    }

    [Fact]
    public async Task PendingSelectorsAreObservedOnceAndDoNotBlockTheCaller()
    {
        using var cancellationSource = new CancellationTokenSource();
        var selectorCalls = 0;
        var taskSource = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var taskWithTokenSource = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var valueSource = new CountingValueTaskSource<int>();
        var valueWithTokenSource = new CountingValueTaskSource<int>();

        var taskMapped = Validation<int, string>.Valid(1).MapAsync(_ =>
        {
            selectorCalls++;
            return taskSource.Task;
        });
        var taskWithTokenMapped = Validation<int, string>.Valid(1).MapAsync(
            (_, _) =>
            {
                selectorCalls++;
                return taskWithTokenSource.Task;
            },
            cancellationSource.Token);
        var valueMapped = Validation<int, string>.Valid(1).MapValueAsync(_ =>
        {
            selectorCalls++;
            return valueSource.CreateValueTask();
        });
        var valueWithTokenMapped = Validation<int, string>.Valid(1).MapValueAsync(
            (_, _) =>
            {
                selectorCalls++;
                return valueWithTokenSource.CreateValueTask();
            },
            cancellationSource.Token);

        Assert.False(taskMapped.IsCompleted);
        Assert.False(taskWithTokenMapped.IsCompleted);
        Assert.False(valueMapped.IsCompleted);
        Assert.False(valueWithTokenMapped.IsCompleted);
        Assert.Equal(0, valueSource.GetResultCount);
        Assert.Equal(0, valueWithTokenSource.GetResultCount);

        taskSource.SetResult(2);
        taskWithTokenSource.SetResult(3);
        valueSource.SetResult(4);
        valueWithTokenSource.SetResult(5);

        Assert.Equal(Validation<int, string>.Valid(2), await taskMapped);
        Assert.Equal(Validation<int, string>.Valid(3), await taskWithTokenMapped);
        Assert.Equal(Validation<int, string>.Valid(4), await valueMapped);
        Assert.Equal(Validation<int, string>.Valid(5), await valueWithTokenMapped);
        Assert.Equal(4, selectorCalls);
        Assert.Equal(1, valueSource.GetResultCount);
        Assert.Equal(1, valueWithTokenSource.GetResultCount);
    }

    private static TResult ThrowCancellation<TResult>(OperationCanceledException cancellation) =>
        throw cancellation;
}
