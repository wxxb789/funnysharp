using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Runtime.CompilerServices;
using System.Text.Json;
using FunnySharp.VerticalSlice.Application;
using FunnySharp.VerticalSlice.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace FunnySharp.VerticalSlice.Tests;

/// <summary>
/// The export is a stream: rows reach the client as they are produced, the running totals are part
/// of the stream, and a client disconnect cancels the underlying enumeration.
/// </summary>
public sealed class StreamingExportTests
{
    [Fact]
    public async Task TheExportStreamsEveryRowWithItsRunningTotal()
    {
        await using var host = await SliceHost.StartAsync();
        var first = await host.PlaceOrderAsync("customer-42", ("SKU-BOOK", 1));
        var second = await host.PlaceOrderAsync("customer-43", ("SKU-PEN", 2));

        using var response = await host.Client.GetStreamAsync("/orders/export");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/x-ndjson", response.Content.Headers.ContentType?.MediaType);
        await using var stream = await response.ReadStreamAsync();
        using var reader = new StreamReader(stream);
        var rows = new List<ExportRow>();
        while (await reader.ReadLineBoundedAsync() is { } line)
        {
            rows.Add(JsonSerializer.Deserialize<ExportRow>(line, JsonSerializerOptions.Web)!);
        }

        Assert.Equal(2, rows.Count);
        Assert.Equal(new[] { 1, 2 }, rows.Select(static row => row.Sequence));
        Assert.Equal(first.Total, rows[0].Total);
        Assert.Equal(first.Total + second.Total, rows[1].RevenueToDate);
    }

    [Fact]
    public async Task ARowReachesTheClientBeforeTheNextOneExists()
    {
        GateOrderStore? store = null;
        await using var host = await SliceHost.StartAsync(
            services => services.AddSingleton<IOrderStore>(store!),
            options => store = new GateOrderStore(options));
        var first = await host.PlaceOrderAsync();
        _ = await host.PlaceOrderAsync();
        var secondRowGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        store!.OnStream = cancellationToken => GatedRowsAsync(store, secondRowGate, cancellationToken);

        using var response = await host.Client.GetStreamAsync("/orders/export");
        await using var stream = await response.ReadStreamAsync();
        using var reader = new StreamReader(stream);

        var line = await reader.ReadLineBoundedAsync();

        Assert.NotNull(line);
        var row = JsonSerializer.Deserialize<ExportRow>(line, JsonSerializerOptions.Web);
        Assert.NotNull(row);
        Assert.Equal(first.OrderId, row.OrderId);
        secondRowGate.TrySetResult();
        Assert.NotNull(await reader.ReadLineBoundedAsync());
    }

    [Fact]
    public async Task AClientDisconnectStopsTheStoreEnumeration()
    {
        GateOrderStore? store = null;
        await using var host = await SliceHost.StartKestrelAsync(
            services => services.AddSingleton<IOrderStore>(store!),
            options => store = new GateOrderStore(options));
        _ = await host.PlaceOrderAsync();
        _ = await host.PlaceOrderAsync();
        var never = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var enumerationCanceled = new TaskCompletionSource<CancellationToken>(TaskCreationOptions.RunContinuationsAsynchronously);
        store!.OnStream = cancellationToken => CancellableRowsAsync(store, never, enumerationCanceled, cancellationToken);

        using var socket = new TcpClient();
        await socket.ConnectAsync(
            host.BaseAddress!.Host,
            host.BaseAddress.Port,
            TestContext.Current.CancellationToken);
        await using var response = socket.GetStream();
        await response.WriteAsync(
            "GET /orders/export HTTP/1.1\r\nHost: 127.0.0.1\r\nConnection: keep-alive\r\n\r\n"u8.ToArray(),
            TestContext.Current.CancellationToken);

        var received = new StringBuilder();
        var buffer = new byte[1024];
        while (!received.ToString().Contains("\n{", StringComparison.Ordinal))
        {
            var count = await response.ReadAsync(buffer.AsMemory(), TestContext.Current.CancellationToken);
            Assert.True(count > 0, "The export stream ended before its first row arrived.");
            received.Append(Encoding.UTF8.GetString(buffer, 0, count));
        }

        // A hard socket reset is the disconnect Kestrel reports as RequestAborted.
        socket.Client.LingerState = new LingerOption(true, 0);
        socket.Dispose();

        var token = await enumerationCanceled.Task.WaitAsync(
            TimeSpan.FromSeconds(10),
            TestContext.Current.CancellationToken);
        Assert.True(token.IsCancellationRequested);
        never.TrySetResult();
    }

    private static async IAsyncEnumerable<OrderRecord> GatedRowsAsync(
        GateOrderStore store,
        TaskCompletionSource secondRowGate,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var index = 0;
        await foreach (var record in store.PeekStreamAsync(cancellationToken))
        {
            index++;
            if (index > 1)
            {
                await secondRowGate.Task.WaitAsync(cancellationToken);
            }

            yield return record;
        }
    }

    private static async IAsyncEnumerable<OrderRecord> CancellableRowsAsync(
        GateOrderStore store,
        TaskCompletionSource never,
        TaskCompletionSource<CancellationToken> enumerationCanceled,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var index = 0;
        await foreach (var record in store.PeekStreamAsync(cancellationToken))
        {
            index++;
            if (index > 1)
            {
                try
                {
                    await never.Task.WaitAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    enumerationCanceled.TrySetResult(cancellationToken);
                    throw;
                }
            }

            yield return record;
        }
    }

}
