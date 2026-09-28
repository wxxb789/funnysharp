using System.IO;

using FunnySharp;

namespace FunnySharp.DocumentationSamples;

// The Goal 21 analyzer samples. The reported-form snippets deliberately contain the misuse each
// diagnostic exists for; the surrounding pragma keeps this project building while the snippet
// region shows exactly what gets reported. Every sample keeps the same shape as its fix-form
// counterpart, so the two read as one before/after pair.
internal static class AnalyzerSamples
{
    private static void UninitializedCarrier()
    {
#pragma warning disable FS1001 // Deliberate misuse: FS1001 reports this declaration.
        // <snippet DocumentationSamples.Analyzers.UninitializedCarrier>
        Result<int, string> result = default;
        // </snippet>
#pragma warning restore FS1001
        _ = result.ToString();
    }

    private static void UninitializedCarrierFix()
    {
        // <snippet DocumentationSamples.Analyzers.UninitializedCarrierFix>
        Result<int, string> result = Result<int, string>.Success(42);
        // </snippet>
        _ = result.ToString();
    }

    private static void DiscardedOutcome(Checkout checkout)
    {
#pragma warning disable FS1002 // Deliberate misuse: FS1002 reports this statement.
        // <snippet DocumentationSamples.Analyzers.DiscardedOutcome>
        checkout.Save();
        // </snippet>
#pragma warning restore FS1002
    }

    private static void DiscardedOutcomeFix(Checkout checkout)
    {
        // <snippet DocumentationSamples.Analyzers.DiscardedOutcomeFix>
        Result<Order, CheckoutError> saved = checkout.Save();
        // </snippet>
        _ = saved.ToString();
    }

    private static async Task DiscardedAsyncWork(Checkout checkout)
    {
#pragma warning disable FS1002, CS4014 // Deliberate misuse: FS1002 reports this statement.
        // <snippet DocumentationSamples.Analyzers.DiscardedAsyncWork>
        checkout.SaveAsync();
        // </snippet>
#pragma warning restore FS1002, CS4014
        await Task.CompletedTask;
    }

    private static void DiscardedOutcomeSuppression(Checkout checkout)
    {
        // <snippet DocumentationSamples.Analyzers.DiscardedOutcomeSuppression>
#pragma warning disable FS1002 // The discard is deliberate; see docs/analyzers.md.
        checkout.Save();
#pragma warning restore FS1002
        // </snippet>
    }

    private static void IgnoredTryGetPresence(Option<int> option)
    {
#pragma warning disable FS1003 // Deliberate misuse: FS1003 reports this statement.
        // <snippet DocumentationSamples.Analyzers.IgnoredTryGetPresence>
        option.TryGetValue(out var value);
        // </snippet>
#pragma warning restore FS1003
        _ = value;
    }

    private static int IgnoredTryGetPresenceFix(Option<int> option)
    {
        // <snippet DocumentationSamples.Analyzers.IgnoredTryGetPresenceFix>
        return option.TryGetValue(out var value) ? value : 0;
        // </snippet>
    }

    private static void BlockedValueTask(Option<int> option)
    {
#pragma warning disable FS1004 // Deliberate misuse: FS1004 reports this access.
        // <snippet DocumentationSamples.Analyzers.BlockedValueTask>
        var value = option.MapValueAsync(number => ValueTask.FromResult(number)).Result;
        // </snippet>
#pragma warning restore FS1004
        _ = value;
    }

    private static async Task BlockedValueTaskFix(Option<int> option)
    {
        // <snippet DocumentationSamples.Analyzers.BlockedValueTaskFix>
        var value = await option.MapValueAsync(number => ValueTask.FromResult(number));
        // </snippet>
        _ = value;
    }

    private static Effect<long> SyncDisposeOfAsyncDisposableResource()
    {
#pragma warning disable FS1005 // Deliberate misuse: FS1005 reports this call.
        // <snippet DocumentationSamples.Analyzers.SyncDisposeOfAsyncDisposableResource>
        return Effect.FromSync(() => new MemoryStream())
            .Using(stream => Effect.FromValue(stream.Length));
        // </snippet>
#pragma warning restore FS1005
    }

    private static Effect<long> SyncDisposeOfAsyncDisposableResourceFix()
    {
        // <snippet DocumentationSamples.Analyzers.SyncDisposeOfAsyncDisposableResourceFix>
        return Effect.FromSync(() => new MemoryStream())
            .UsingAsync(stream => Effect.FromValue(stream.Length));
        // </snippet>
    }
}

internal sealed class Checkout
{
    public Result<Order, CheckoutError> Save() => Result<Order, CheckoutError>.Success(new Order());

    public Task<Result<Order, CheckoutError>> SaveAsync() =>
        Task.FromResult(Result<Order, CheckoutError>.Success(new Order()));
}

internal sealed class Order;

internal sealed record CheckoutError(string Code);
