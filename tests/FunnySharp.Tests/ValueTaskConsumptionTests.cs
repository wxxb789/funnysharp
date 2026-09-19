using System.Threading.Tasks.Sources;

namespace FunnySharp.Tests;

public sealed class ValueTaskConsumptionTests
{
    [Fact]
    public async Task UnitResultTryValueAsyncConsumesACompletedVoidSource()
    {
        var source = new CountingVoidValueTaskSource();

        var operation = UnitResult.TryValueAsync(() => source.CreateValueTask());

        Assert.Equal(1, source.GetResultCount);
        Assert.True((await operation).IsSuccess);
        Assert.Equal(1, source.GetResultCount);
    }

    [Fact]
    public async Task UnitResultTryValueAsyncConsumesAPendingVoidSourceOnce()
    {
        var source = new CountingVoidValueTaskSource(pending: true);

        var operation = UnitResult.TryValueAsync(() => source.CreateValueTask());

        Assert.False(operation.IsCompleted);
        source.SetResult();
        Assert.True((await operation).IsSuccess);
        Assert.Equal(1, source.GetResultCount);
    }

    [Fact]
    public async Task ResultMapValueAsyncConsumesACompletedSourceAndKeepsThePayload()
    {
        var source = new CountingValueTaskSource<int>(42);

        var operation = Result<string, string>.Success("ok").MapValueAsync(_ => source.CreateValueTask());

        Assert.Equal(1, source.GetResultCount);
        var result = await operation;
        Assert.True(result.TryGetValue(out var value));
        Assert.Equal(42, value);
    }

    [Fact]
    public async Task UnitResultToResultValueAsyncAndBindValueAsyncConsumeCompletedSources()
    {
        var mapSource = new CountingValueTaskSource<int>(7);
        var bindSource = new CountingValueTaskSource<UnitResult<string>>(UnitResult<string>.Success());

        var mapped = await UnitResult<string>.Success().ToResultValueAsync(() => mapSource.CreateValueTask());
        var bound = await UnitResult<string>.Success().BindValueAsync(() => bindSource.CreateValueTask());

        Assert.Equal(1, mapSource.GetResultCount);
        Assert.Equal(1, bindSource.GetResultCount);
        Assert.True(mapped.TryGetValue(out var value));
        Assert.Equal(7, value);
        Assert.True(bound.IsSuccess);
    }

    [Fact]
    public async Task ValidationMapValueAsyncConsumesACompletedSource()
    {
        var source = new CountingValueTaskSource<int>(5);

        var operation = Validation<int, string>.Valid(1).MapValueAsync(_ => source.CreateValueTask());

        Assert.Equal(1, source.GetResultCount);
        var validation = await operation;
        Assert.True(validation.TryGetValue(out var value));
        Assert.Equal(5, value);
    }

    private sealed class CountingVoidValueTaskSource : IValueTaskSource
    {
        private ManualResetValueTaskSourceCore<bool> source;

        public CountingVoidValueTaskSource(bool pending = false)
        {
            source.RunContinuationsAsynchronously = true;
            if (!pending)
            {
                source.SetResult(true);
            }
        }

        public int GetResultCount { get; private set; }

        public ValueTask CreateValueTask() => new(this, source.Version);

        public void SetResult() => source.SetResult(true);

        public void GetResult(short token)
        {
            GetResultCount++;
            source.GetResult(token);
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
