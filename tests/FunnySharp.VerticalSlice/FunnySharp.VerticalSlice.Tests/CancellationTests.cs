using FunnySharp.VerticalSlice.Application;
using FunnySharp.VerticalSlice.Domain;
using FunnySharp.VerticalSlice.Http;
using FunnySharp.VerticalSlice.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace FunnySharp.VerticalSlice.Tests;

/// <summary>
/// One request token reaches every dependency, and a client disconnect cancels that exact token
/// instead of being converted into a response.
/// </summary>
public sealed class CancellationTests
{
    [Fact]
    public async Task OneRequestTokenReachesEveryDependencyAndTheClientCancelsIt()
    {
        GateOrderStore? store = null;
        GateInventoryService? inventory = null;
        await using var host = await SliceHost.StartAsync(
            services =>
            {
                services.AddSingleton<IOrderStore>(store!);
                services.AddSingleton<IInventoryService>(inventory!);
            },
            options =>
            {
                store = new GateOrderStore(options);
                inventory = new GateInventoryService(options);
            });
        var placed = await host.PlaceOrderAsync();

        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        inventory!.OnRelease = async (_, _, cancellationToken) =>
        {
            await gate.Task.WaitAsync(cancellationToken);
            return UnitResult<OrderError>.Success();
        };

        using var cancellation = new CancellationTokenSource();
        var request = host.Client.PostJsonWithTokenAsync(
            $"/orders/{placed.OrderId}/cancellation",
            new CancelOrderRequest("customer changed their mind"),
            cancellation.Token);

        await inventory.ReleaseStarted.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        Assert.Equal(store!.LastServedToken, inventory.LastReleaseToken);
        Assert.False(inventory.LastReleaseToken.IsCancellationRequested);

        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => request);
        Assert.True(inventory.LastReleaseToken.IsCancellationRequested);
        gate.TrySetResult();
    }

    [Fact]
    public async Task AStoreThatHonorsCancellationStopsInsteadOfProducingAProblem()
    {
        GateOrderStore? store = null;
        await using var host = await SliceHost.StartAsync(
            services => services.AddSingleton<IOrderStore>(store!),
            options => store = new GateOrderStore(options));
        var placed = await host.PlaceOrderAsync();
        var observed = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);

        store!.OnFind = async (_, cancellationToken) =>
        {
            observed.TrySetResult(cancellationToken);
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return Option.None<OrderRecord>();
        };

        using var cancellation = new CancellationTokenSource();
        var request = host.Client.GetWithTokenAsync($"/orders/{placed.OrderId}", cancellation.Token);
        var requestToken = await observed.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        Assert.False(requestToken.IsCancellationRequested);

        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => request);
        Assert.True(requestToken.IsCancellationRequested);
    }
}
