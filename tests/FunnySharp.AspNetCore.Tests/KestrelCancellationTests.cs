using System.Net;
using System.Net.Sockets;
using FunnySharp;
using FunnySharp.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FunnySharp.AspNetCore.Tests;

public sealed class KestrelCancellationTests
{
    [Fact]
    public async Task ClientDisconnectCancelsRequestAbortedAndForwardsItsExactTokenToEffect()
    {
        var requestStarted = new TaskCompletionSource<CancellationToken>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var requestCanceled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var effectStarted = new TaskCompletionSource<CancellationToken>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var effectCanceled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationTokenRegistration requestRegistration = default;

        var effect = Effect.FromValueTask<Option<Payload>>(async cancellationToken =>
        {
            effectStarted.TrySetResult(cancellationToken);
            using var registration = cancellationToken.Register(effectCanceled.SetResult);
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return Option.Some(new Payload("unreachable"));
        });

        await using var host = await KestrelApplication.StartAsync(app =>
            app.MapPost("/effect/disconnect", (HttpContext context) =>
            {
                var requestToken = context.RequestAborted;
                requestRegistration = requestToken.Register(requestCanceled.SetResult);
                requestStarted.TrySetResult(requestToken);
                return effect.ToHttpResultAsync(context, NotFound);
            }));

        using var client = new TcpClient(AddressFamily.InterNetwork) { NoDelay = true };
        await client.ConnectAsync(
            host.Endpoint.Address,
            host.Endpoint.Port,
            TestContext.Current.CancellationToken);

        await using var stream = client.GetStream();
        await stream.WriteAsync(
            "POST /effect/disconnect HTTP/1.1\r\nHost: 127.0.0.1\r\nContent-Length: 1\r\nConnection: keep-alive\r\n\r\n"u8.ToArray(),
            TestContext.Current.CancellationToken);

        var requestToken = await requestStarted.Task.WaitAsync(
            TimeSpan.FromSeconds(10),
            TestContext.Current.CancellationToken);
        var effectToken = await effectStarted.Task.WaitAsync(
            TimeSpan.FromSeconds(10),
            TestContext.Current.CancellationToken);

        Assert.Equal(requestToken, effectToken);
        Assert.False(requestToken.IsCancellationRequested);

        client.Client.LingerState = new LingerOption(true, 0);
        client.Dispose();

        await Task.WhenAll(
            requestCanceled.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken),
            effectCanceled.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));

        Assert.True(requestToken.IsCancellationRequested);
        Assert.True(effectToken.IsCancellationRequested);
        requestRegistration.Dispose();
    }

    private static ProblemDetails NotFound() => new()
    {
        Status = StatusCodes.Status404NotFound,
        Title = "Resource not found",
    };

    private sealed class KestrelApplication(WebApplication application, IPEndPoint endpoint) : IAsyncDisposable
    {
        public IPEndPoint Endpoint { get; } = endpoint;

        public static async Task<KestrelApplication> StartAsync(Action<WebApplication> configure)
        {
            var builder = WebApplication.CreateSlimBuilder();
            builder.WebHost.UseKestrel(options => options.Listen(IPAddress.Loopback, 0));

            var application = builder.Build();
            configure(application);
            await application.StartAsync(TestContext.Current.CancellationToken);

            var address = Assert.Single(application.Urls);
            var uri = new Uri(address);
            return new KestrelApplication(
                application,
                new IPEndPoint(IPAddress.Parse(uri.Host), uri.Port));
        }

        public async ValueTask DisposeAsync()
        {
            await application.StopAsync(TestContext.Current.CancellationToken);
            await application.DisposeAsync();
        }
    }

    private sealed record Payload(string Name);
}
