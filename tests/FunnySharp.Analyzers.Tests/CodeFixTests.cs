namespace FunnySharp.Analyzers.Tests;

public sealed class CodeFixTests
{
    [Fact]
    public async Task DiscardedOutcomeFixMakesTheDiscardExplicit()
    {
        var fixedText = await AnalyzerHarness.ApplyFirstFixAsync(
            """
            using FunnySharp;
            class C
            {
                Option<int> Find() => Option.Some(1);

                void Use()
                {
                    Find();
                }
            }
            """,
            "FS1002",
            new DiscardedOutcomeCodeFixProvider());
        Assert.Contains("_ = Find();", fixedText, StringComparison.Ordinal);

        await AnalyzerTestAssert.QuietAsync(fixedText);
    }

    [Fact]
    public async Task DiscardedOutcomeFixPreservesAwaitedStatements()
    {
        var fixedText = await AnalyzerHarness.ApplyFirstFixAsync(
            """
            using FunnySharp;
            class C
            {
                async Task UseAsync()
                {
                    await SaveAsync();
                }

                Task<Result<int, string>> SaveAsync() => Task.FromResult(Result<int, string>.Success(1));
            }
            """,
            "FS1002",
            new DiscardedOutcomeCodeFixProvider());
        Assert.Contains("_ = await SaveAsync();", fixedText, StringComparison.Ordinal);

        await AnalyzerTestAssert.QuietAsync(fixedText);
    }

    [Fact]
    public async Task SyncDisposeFixRenamesUsingToUsingAsync()
    {
        var fixedText = await AnalyzerHarness.ApplyFirstFixAsync(
            """
            using FunnySharp;
            class C
            {
                Effect<int> Use()
                {
                    return Effect.FromSync(() => new Both()).Using(resource => Effect.FromValue(1));
                }
            }

            class Both : IDisposable, IAsyncDisposable
            {
                public void Dispose()
                {
                }

                public ValueTask DisposeAsync()
                {
                    return ValueTask.CompletedTask;
                }
            }
            """,
            "FS1005",
            new SyncDisposeCodeFixProvider());
        Assert.Contains(".UsingAsync(resource => Effect.FromValue(1))", fixedText, StringComparison.Ordinal);

        await AnalyzerTestAssert.QuietAsync(fixedText);
    }
}
