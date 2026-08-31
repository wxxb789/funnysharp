using System.Threading.Tasks.Sources;

namespace FunnySharp.Tests;

public sealed class OptionAsyncTests
{
    [Fact]
    public async Task AsyncMapTransformsSomeAndShortCircuitsNone()
    {
        var taskCalls = 0;
        var valueTaskCalls = 0;

        var taskSome = await Option.Some(2).MapAsync(value => Task.FromResult(value * 3));
        var taskNone = await Option.None<int>().MapAsync(value =>
        {
            taskCalls++;
            return Task.FromResult(value * 3);
        });
        var valueTaskSome = await Option.Some(2).MapValueAsync(
            value => ValueTask.FromResult(value * 3));
        var valueTaskNone = await Option.None<int>().MapValueAsync(value =>
        {
            valueTaskCalls++;
            return ValueTask.FromResult(value * 3);
        });

        Assert.Equal(Option.Some(6), taskSome);
        Assert.True(taskNone.IsNone);
        Assert.Equal(Option.Some(6), valueTaskSome);
        Assert.True(valueTaskNone.IsNone);
        Assert.Equal(0, taskCalls);
        Assert.Equal(0, valueTaskCalls);
    }

    [Fact]
    public async Task AsyncMapPreservesDeclaredNullableTypesAndNormalizesNull()
    {
        Option<string?> taskText = await Option.Some(1).MapAsync(
            _ => Task.FromResult<string?>(null));
        Option<int?> taskNumber = await Option.Some(1).MapAsync(
            _ => Task.FromResult<int?>(0));
        Option<string?> valueTaskText = await Option.Some(1).MapValueAsync(
            _ => ValueTask.FromResult<string?>(null));
        Option<int?> valueTaskNumber = await Option.Some(1).MapValueAsync(
            _ => ValueTask.FromResult<int?>(0));

        Assert.True(taskText.IsNone);
        Assert.Equal(Option.Some<int?>((int?)0), taskNumber);
        Assert.True(valueTaskText.IsNone);
        Assert.Equal(Option.Some<int?>((int?)0), valueTaskNumber);
    }

    [Fact]
    public async Task AsyncBindReturnsCallbackOptionsAndShortCircuitsNone()
    {
        var taskCalls = 0;
        var valueTaskCalls = 0;

        var taskSome = await Option.Some(2).BindAsync(
            value => Task.FromResult(Option.Some(value * 3)));
        var taskNone = await Option.None<int>().BindAsync(value =>
        {
            taskCalls++;
            return Task.FromResult(Option.Some(value * 3));
        });
        var valueTaskSome = await Option.Some(2).BindValueAsync(
            value => ValueTask.FromResult(Option.Some(value * 3)));
        var valueTaskNone = await Option.None<int>().BindValueAsync(value =>
        {
            valueTaskCalls++;
            return ValueTask.FromResult(Option.Some(value * 3));
        });

        Assert.Equal(Option.Some(6), taskSome);
        Assert.True(taskNone.IsNone);
        Assert.Equal(Option.Some(6), valueTaskSome);
        Assert.True(valueTaskNone.IsNone);
        Assert.Equal(0, taskCalls);
        Assert.Equal(0, valueTaskCalls);
    }

    [Fact]
    public void AsyncCombinatorsRejectNullCallbacksEagerly()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = OptionExtensions.MapAsync<int, int>(Option.None<int>(), null!);
        });
        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = OptionExtensions.MapAsync<int, int>(Option.None<int>(), null!, CancellationToken.None);
        });
        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = OptionExtensions.BindAsync<int, int>(Option.None<int>(), null!);
        });
        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = OptionExtensions.BindAsync<int, int>(Option.None<int>(), null!, CancellationToken.None);
        });
        Assert.Throws<ArgumentNullException>(
            () => OptionExtensions.MapValueAsync<int, int>(Option.None<int>(), null!));
        Assert.Throws<ArgumentNullException>(
            () => OptionExtensions.MapValueAsync<int, int>(Option.None<int>(), null!, CancellationToken.None));
        Assert.Throws<ArgumentNullException>(
            () => OptionExtensions.BindValueAsync<int, int>(Option.None<int>(), null!));
        Assert.Throws<ArgumentNullException>(
            () => OptionExtensions.BindValueAsync<int, int>(Option.None<int>(), null!, CancellationToken.None));
        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = OptionExtensions.MapAsync<int, int>(Option.Some(1), null!);
        });
        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = OptionExtensions.MapAsync<int, int>(Option.Some(1), null!, CancellationToken.None);
        });
        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = OptionExtensions.BindAsync<int, int>(Option.Some(1), null!);
        });
        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = OptionExtensions.BindAsync<int, int>(Option.Some(1), null!, CancellationToken.None);
        });
        Assert.Throws<ArgumentNullException>(
            () => OptionExtensions.MapValueAsync<int, int>(Option.Some(1), null!));
        Assert.Throws<ArgumentNullException>(
            () => OptionExtensions.MapValueAsync<int, int>(Option.Some(1), null!, CancellationToken.None));
        Assert.Throws<ArgumentNullException>(
            () => OptionExtensions.BindValueAsync<int, int>(Option.Some(1), null!));
        Assert.Throws<ArgumentNullException>(
            () => OptionExtensions.BindValueAsync<int, int>(Option.Some(1), null!, CancellationToken.None));
    }

    [Fact]
    public async Task SynchronousCallbackThrowsAreCapturedInReturnedAwaitables()
    {
        var taskExpected = new InvalidOperationException("task failed");
        var valueTaskExpected = new InvalidOperationException("value task failed");
        var bindTaskExpected = new InvalidOperationException("bind task failed");
        var bindValueTaskExpected = new InvalidOperationException("bind value task failed");
        Task<Option<int>>? taskResult = null;
        ValueTask<Option<int>> valueTaskResult = default;
        Task<Option<int>>? bindTaskResult = null;
        ValueTask<Option<int>> bindValueTaskResult = default;

        var taskInvocationException = Record.Exception(() =>
        {
            taskResult = Option.Some(1).MapAsync<int, int>(_ => throw taskExpected);
        });
        var valueTaskInvocationException = Record.Exception(
            () => valueTaskResult = Option.Some(1).MapValueAsync<int, int>(_ => throw valueTaskExpected));
        var bindTaskInvocationException = Record.Exception(() =>
        {
            bindTaskResult = Option.Some(1).BindAsync<int, int>(_ => throw bindTaskExpected);
        });
        var bindValueTaskInvocationException = Record.Exception(
            () => bindValueTaskResult = Option.Some(1).BindValueAsync<int, int>(_ => throw bindValueTaskExpected));

        Assert.Null(taskInvocationException);
        Assert.Null(valueTaskInvocationException);
        Assert.Null(bindTaskInvocationException);
        Assert.Null(bindValueTaskInvocationException);
        Assert.Same(taskExpected, await Assert.ThrowsAsync<InvalidOperationException>(() => taskResult!));
        Assert.Same(
            valueTaskExpected,
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await valueTaskResult));
        Assert.Same(bindTaskExpected, await Assert.ThrowsAsync<InvalidOperationException>(() => bindTaskResult!));
        Assert.Same(
            bindValueTaskExpected,
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await bindValueTaskResult));
    }

    [Fact]
    public async Task SynchronousCancellationThrowsProduceCanceledAwaitables()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        Task<Option<int>>? taskResult = null;
        ValueTask<Option<int>> valueTaskResult = default;
        Task<Option<int>>? bindTaskResult = null;
        ValueTask<Option<int>> bindValueTaskResult = default;

        var taskInvocationException = Record.Exception(() =>
        {
            taskResult = Option.Some(1).MapAsync<int, int>(
                _ => throw new OperationCanceledException(cancellationSource.Token));
        });
        var valueTaskInvocationException = Record.Exception(
            () => valueTaskResult = Option.Some(1).MapValueAsync<int, int>(
                _ => throw new OperationCanceledException(cancellationSource.Token)));
        var bindTaskInvocationException = Record.Exception(() =>
        {
            bindTaskResult = Option.Some(1).BindAsync<int, int>(
                _ => throw new OperationCanceledException(cancellationSource.Token));
        });
        var bindValueTaskInvocationException = Record.Exception(
            () => bindValueTaskResult = Option.Some(1).BindValueAsync<int, int>(
                _ => throw new OperationCanceledException(cancellationSource.Token)));

        Assert.Null(taskInvocationException);
        Assert.Null(valueTaskInvocationException);
        Assert.Null(bindTaskInvocationException);
        Assert.Null(bindValueTaskInvocationException);
        var taskCancellation = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => taskResult!);
        var valueTaskCancellation = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await valueTaskResult);
        var bindTaskCancellation = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => bindTaskResult!);
        var bindValueTaskCancellation = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await bindValueTaskResult);
        Assert.True(taskResult!.IsCanceled);
        Assert.True(valueTaskResult.IsCanceled);
        Assert.True(bindTaskResult!.IsCanceled);
        Assert.True(bindValueTaskResult.IsCanceled);
        Assert.Equal(cancellationSource.Token, taskCancellation.CancellationToken);
        Assert.Equal(cancellationSource.Token, valueTaskCancellation.CancellationToken);
        Assert.Equal(cancellationSource.Token, bindTaskCancellation.CancellationToken);
        Assert.Equal(cancellationSource.Token, bindValueTaskCancellation.CancellationToken);
    }

    [Fact]
    public async Task AsyncCallbacksPreserveFaultAndNullTaskSemantics()
    {
        var taskExpected = new InvalidOperationException("task failed");
        var valueTaskExpected = new InvalidOperationException("value task failed");

        var taskActual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => Option.Some(1).MapAsync(_ => Task.FromException<int>(taskExpected)));
        var valueTaskActual = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await Option.Some(1).MapValueAsync(
                _ => ValueTask.FromException<int>(valueTaskExpected)));
        await Assert.ThrowsAsync<NullReferenceException>(
            () => Option.Some(1).MapAsync<int, int>(_ => null!));

        Assert.Same(taskExpected, taskActual);
        Assert.Same(valueTaskExpected, valueTaskActual);
    }

    [Fact]
    public async Task CancellationAwareCallbacksReceiveTheExactTokenWithoutEagerCancellation()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        CancellationToken taskToken = default;
        CancellationToken valueTaskToken = default;
        var noneCalls = 0;

        var taskResult = await Option.Some(1).MapAsync(
            (value, token) =>
            {
                taskToken = token;
                return Task.FromResult(value + 1);
            },
            cancellationSource.Token);
        var valueTaskResult = await Option.Some(1).MapValueAsync(
            (value, token) =>
            {
                valueTaskToken = token;
                return ValueTask.FromResult(value + 1);
            },
            cancellationSource.Token);
        var noneResult = await Option.None<int>().MapAsync(
            (value, token) =>
            {
                noneCalls++;
                return Task.FromCanceled<int>(token);
            },
            cancellationSource.Token);

        Assert.Equal(Option.Some(2), taskResult);
        Assert.Equal(Option.Some(2), valueTaskResult);
        Assert.True(noneResult.IsNone);
        Assert.Equal(cancellationSource.Token, taskToken);
        Assert.Equal(cancellationSource.Token, valueTaskToken);
        Assert.Equal(0, noneCalls);
    }

    [Fact]
    public async Task CancellationAwareCallbacksPreserveCanceledStatusAndToken()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        var task = Option.Some(1).BindAsync(
            (_, token) => Task.FromCanceled<Option<int>>(token),
            cancellationSource.Token);
        var valueTask = Option.Some(1).BindValueAsync(
            (_, token) => ValueTask.FromCanceled<Option<int>>(token),
            cancellationSource.Token);
        var taskCancellation = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        var valueTaskCancellation = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await valueTask);

        Assert.True(task.IsCanceled);
        Assert.True(valueTask.IsCanceled);
        Assert.Equal(cancellationSource.Token, taskCancellation.CancellationToken);
        Assert.Equal(cancellationSource.Token, valueTaskCancellation.CancellationToken);
    }

    [Fact]
    public async Task TaskNullableConversionsUnwrapValuesAndPreserveFailures()
    {
        Task<string?> missingText = Task.FromResult<string?>(null);
        Task<string?> presentText = Task.FromResult<string?>("value");
        Task<int?> missingNumber = Task.FromResult<int?>(null);
        Task<int?> zero = Task.FromResult<int?>(0);
        var expected = new InvalidOperationException("source failed");
        Task<string?> missingTask = null!;

        Option<string> missingTextOption = await missingText.ToOptionAsync();
        Option<string> presentTextOption = await presentText.ToOptionAsync();
        Option<int> missingNumberOption = await missingNumber.ToOptionAsync();
        Option<int> zeroOption = await zero.ToOptionAsync();
        var fault = await Assert.ThrowsAsync<InvalidOperationException>(
            () => Task.FromException<string?>(expected).ToOptionAsync());

        Assert.True(missingTextOption.IsNone);
        Assert.Equal(Option.Some("value"), presentTextOption);
        Assert.True(missingNumberOption.IsNone);
        Assert.Equal(Option.Some(0), zeroOption);
        Assert.Same(expected, fault);
        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = missingTask.ToOptionAsync();
        });
    }

    [Fact]
    public async Task NullableAsyncConversionsPreserveNestedAbsence()
    {
        Option<int>? nestedValue = Option.None<int>();

        var fromTask = await Task.FromResult(nestedValue).ToOptionAsync();
        var fromValueTask = await ValueTask.FromResult(nestedValue).ToOptionAsync();

        Assert.Equal(Option.Some(Option.None<int>()), fromTask);
        Assert.Equal(Option.Some(Option.None<int>()), fromValueTask);
    }

    [Fact]
    public async Task ValueTaskNullableConversionsHandleDefaultAndPreserveFailures()
    {
        ValueTask<string?> defaultText = default;
        ValueTask<int?> zero = ValueTask.FromResult<int?>(0);
        var expected = new InvalidOperationException("source failed");

        Option<string> defaultTextOption = await defaultText.ToOptionAsync();
        Option<int> zeroOption = await zero.ToOptionAsync();
        var fault = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await ValueTask.FromException<string?>(expected).ToOptionAsync());

        Assert.True(defaultTextOption.IsNone);
        Assert.Equal(Option.Some(0), zeroOption);
        Assert.Same(expected, fault);
    }

    [Fact]
    public async Task AsyncNullableConversionsPreserveCancellationStatusAndToken()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        var task = Task.FromCanceled<string?>(cancellationSource.Token).ToOptionAsync();
        var valueTask = ValueTask.FromCanceled<string?>(cancellationSource.Token).ToOptionAsync();
        var taskCancellation = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
        var valueTaskCancellation = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await valueTask);

        Assert.True(task.IsCanceled);
        Assert.True(valueTask.IsCanceled);
        Assert.Equal(cancellationSource.Token, taskCancellation.CancellationToken);
        Assert.Equal(cancellationSource.Token, valueTaskCancellation.CancellationToken);
    }

    [Fact]
    public async Task PendingTaskConversionDoesNotBlockTheCaller()
    {
        var completion = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);

        var result = completion.Task.ToOptionAsync();

        Assert.False(result.IsCompleted);
        completion.SetResult("value");
        Assert.Equal(Option.Some("value"), await result);
    }

    [Fact]
    public async Task PendingAsyncCallbacksDoNotBlockTheCaller()
    {
        var taskMapSource = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var taskBindSource = new TaskCompletionSource<Option<int>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var valueTaskMapSource = new CountingValueTaskSource<int>();
        var valueTaskBindSource = new CountingValueTaskSource<Option<int>>();

        var taskMap = Option.Some(1).MapAsync(_ => taskMapSource.Task);
        var taskBind = Option.Some(1).BindAsync(_ => taskBindSource.Task);
        var valueTaskMap = Option.Some(1).MapValueAsync(_ => valueTaskMapSource.CreateValueTask());
        var valueTaskBind = Option.Some(1).BindValueAsync(_ => valueTaskBindSource.CreateValueTask());

        Assert.False(taskMap.IsCompleted);
        Assert.False(taskBind.IsCompleted);
        Assert.False(valueTaskMap.IsCompleted);
        Assert.False(valueTaskBind.IsCompleted);

        taskMapSource.SetResult(2);
        taskBindSource.SetResult(Option.Some(2));
        valueTaskMapSource.SetResult(2);
        valueTaskBindSource.SetResult(Option.Some(2));

        Assert.Equal(Option.Some(2), await taskMap);
        Assert.Equal(Option.Some(2), await taskBind);
        Assert.Equal(Option.Some(2), await valueTaskMap);
        Assert.Equal(Option.Some(2), await valueTaskBind);
        Assert.Equal(1, valueTaskMapSource.GetResultCount);
        Assert.Equal(1, valueTaskBindSource.GetResultCount);
    }

    [Fact]
    public async Task PendingValueTaskConversionDoesNotBlockTheCaller()
    {
        var source = new CountingValueTaskSource<string?>();

        var result = source.CreateValueTask().ToOptionAsync();

        Assert.False(result.IsCompleted);
        source.SetResult("value");
        Assert.Equal(Option.Some("value"), await result);
        Assert.Equal(1, source.GetResultCount);
    }

    [Fact]
    public async Task ValueTaskInputsAndCallbacksAreConsumedOnce()
    {
        var mapSource = new CountingValueTaskSource<int>(6);
        var bindSource = new CountingValueTaskSource<Option<int>>(Option.Some(6));
        var conversionSource = new CountingValueTaskSource<string?>("value");

        var mapped = await Option.Some(2).MapValueAsync(_ => mapSource.CreateValueTask());
        var bound = await Option.Some(2).BindValueAsync(_ => bindSource.CreateValueTask());
        var converted = await conversionSource.CreateValueTask().ToOptionAsync();

        Assert.Equal(Option.Some(6), mapped);
        Assert.Equal(Option.Some(6), bound);
        Assert.Equal(Option.Some("value"), converted);
        Assert.Equal(1, mapSource.GetResultCount);
        Assert.Equal(1, bindSource.GetResultCount);
        Assert.Equal(1, conversionSource.GetResultCount);
    }

    private sealed class CountingValueTaskSource<T> : IValueTaskSource<T>
    {
        private ManualResetValueTaskSourceCore<T> source;

        public CountingValueTaskSource()
        {
            source.RunContinuationsAsynchronously = true;
        }

        public CountingValueTaskSource(T result) : this()
        {
            source.SetResult(result);
        }

        public int GetResultCount { get; private set; }

        public ValueTask<T> CreateValueTask() => new(this, source.Version);

        public void SetResult(T result) => source.SetResult(result);

        public T GetResult(short token)
        {
            GetResultCount++;
            return source.GetResult(token);
        }

        public ValueTaskSourceStatus GetStatus(short token) => source.GetStatus(token);

        public void OnCompleted(
            Action<object?> continuation,
            object? state,
            short token,
            ValueTaskSourceOnCompletedFlags flags) =>
            source.OnCompleted(continuation, state, token, flags);
    }
}
