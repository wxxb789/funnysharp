namespace FunnySharp.Analyzers.Tests;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

public sealed class BlockedValueTaskAnalyzerTests
{
    [Fact]
    public async Task ResultOnDirectValueTaskIsReported()
    {
        await AnalyzerTestAssert.DiagnosticAsync(
            """
            using FunnySharp;
            class C
            {
                void Use()
                {
                    var value = Option.Some(1).MapValueAsync(number => ValueTask.FromResult(number)).Result;
                    _ = value;
                }
            }
            """,
            "FS1004",
            "Result");
    }

    [Fact]
    public async Task ResultOnStoredValueTaskIsReported()
    {
        await AnalyzerTestAssert.DiagnosticAsync(
            """
            using FunnySharp;
            class C
            {
                void Use()
                {
                    var task = Option.Some(1).MapValueAsync(number => ValueTask.FromResult(number));
                    var value = task.Result;
                    _ = value;
                }
            }
            """,
            "FS1004");
    }

    [Fact]
    public async Task GetAwaiterGetResultOnDirectValueTaskIsReported()
    {
        await AnalyzerTestAssert.DiagnosticAsync(
            """
            using FunnySharp;
            class C
            {
                void Use()
                {
                    var value = Option.Some(1).MapValueAsync(number => ValueTask.FromResult(number)).GetAwaiter().GetResult();
                    _ = value;
                }
            }
            """,
            "FS1004",
            "GetAwaiter().GetResult()");
    }

    [Fact]
    public async Task GetAwaiterGetResultOnUserValueTaskOfOutcomeIsReported()
    {
        await AnalyzerTestAssert.DiagnosticAsync(
            """
            using FunnySharp;
            class C
            {
                ValueTask<Option<int>> LoadAsync() =>
                    ValueTask.FromResult(Option.Some(1));

                void Use()
                {
                    var value = LoadAsync().GetAwaiter().GetResult();
                    _ = value;
                }
            }
            """,
            "FS1004");
    }

    [Fact]
    public async Task TaskResultIsQuiet()
    {
        await AnalyzerTestAssert.QuietAsync(
            """
            using FunnySharp;
            class C
            {
                void Use()
                {
                    var value = Option.Some(1).MapAsync(number => Task.FromResult(number)).Result;
                    _ = value;
                }
            }
            """);
    }

    [Theory]
    [InlineData("if (pending.IsCompletedSuccessfully) return pending.Result; return await pending;")]
    [InlineData("if (pending.IsCompletedSuccessfully) { return pending.Result; } else { return await pending; }")]
    [InlineData("if (!pending.IsCompletedSuccessfully) return await pending; return pending.Result;")]
    [InlineData("return pending.IsCompletedSuccessfully ? pending.Result : await pending;")]
    [InlineData("if (pending.IsCompletedSuccessfully) return pending.GetAwaiter().GetResult(); return await pending;")]
    [InlineData("if (!pending.IsCompletedSuccessfully) return await pending; if (choose) return pending.Result; return pending.GetAwaiter().GetResult();")]
    [InlineData("if (pending.IsCompletedSuccessfully && choose) return pending.Result; return await pending;")]
    [InlineData("return pending.IsCompletedSuccessfully ? pending.Result : await pending.ConfigureAwait(choose ? true : false);")]
    [InlineData("return choose ? (pending.IsCompletedSuccessfully ? pending.Result : await pending) : (pending.IsCompletedSuccessfully ? pending.GetAwaiter().GetResult() : await pending);")]
    public async Task CompletedParameterWithSingleConsumptionIsQuiet(string body)
    {
        await AnalyzerTestAssert.QuietAsync(
            $$"""
            using FunnySharp;
            class C
            {
                async ValueTask<Option<int>> UseAsync(ValueTask<Option<int>> pending, bool choose)
                {
                    {{body}}
                }
            }
            """);
    }

    [Theory]
    [InlineData("pending.Result")]
    [InlineData("pending.GetAwaiter().GetResult()")]
    public async Task CompletedLocalWithSingleConsumptionIsQuiet(string access)
    {
        await AnalyzerTestAssert.QuietAsync(
            $$"""
            using FunnySharp;
            class C
            {
                async ValueTask<Option<int>> UseAsync()
                {
                    var pending = Option.Some(1).MapValueAsync(number => ValueTask.FromResult(number));
                    if (pending.IsCompletedSuccessfully)
                    {
                        return {{access}};
                    }
                    else
                    {
                        return await pending;
                    }
                }
            }
            """);
    }

    [Theory]
    [InlineData("if (other.IsCompletedSuccessfully) { _ = pending.Result; }", "pending.Result")]
    [InlineData("if (other.IsCompletedSuccessfully) { _ = pending.GetAwaiter().GetResult(); }", "pending.GetAwaiter().GetResult()")]
    [InlineData("if (LoadAsync().IsCompletedSuccessfully) { _ = LoadAsync().Result; }", "LoadAsync().Result")]
    [InlineData("if (!pending.IsCompletedSuccessfully) { _ = pending.Result; }", "pending.Result")]
    [InlineData("_ = pending.IsCompletedSuccessfully ? await pending : pending.Result;", "pending.Result")]
    [InlineData("if (pending.IsCompleted) { _ = pending.Result; }", "pending.Result")]
    [InlineData("if (pending.IsCompletedSuccessfully) { pending = LoadAsync(); _ = pending.Result; }", "pending.Result")]
    [InlineData("var local = LoadAsync(); if (local.IsCompletedSuccessfully) { local = LoadAsync(); _ = local.Result; }", "local.Result")]
    [InlineData("if (pending.IsCompletedSuccessfully) { _ = pending.Result; } pending = LoadAsync();", "pending.Result")]
    [InlineData("if (choose) { if (!pending.IsCompletedSuccessfully) return await pending; } _ = pending.Result;", "pending.Result")]
    [InlineData("if (pending.IsCompletedSuccessfully) { _ = pending.Result; _ = pending.Result; }", "pending.Result", "pending.Result")]
    [InlineData("if (pending.IsCompletedSuccessfully) { _ = pending.GetAwaiter().GetResult(); _ = pending.GetAwaiter().GetResult(); }", "pending.GetAwaiter().GetResult()", "pending.GetAwaiter().GetResult()")]
    [InlineData("if (pending.IsCompletedSuccessfully) { _ = pending.Result; _ = await pending; }", "pending.Result")]
    [InlineData("_ = await pending; if (pending.IsCompletedSuccessfully) { _ = pending.Result; }", "pending.Result")]
    [InlineData("if (choose) { _ = await pending; } if (pending.IsCompletedSuccessfully) { _ = pending.Result; }", "pending.Result")]
    [InlineData("if (pending.IsCompletedSuccessfully) { _ = pending.Result; _ = await pending.ConfigureAwait(choose ? true : false); }", "pending.Result")]
    [InlineData("_ = await pending.ConfigureAwait(choose ? true : false); if (pending.IsCompletedSuccessfully) { _ = pending.Result; }", "pending.Result")]
    [InlineData("_ = pending.AsTask(); if (pending.IsCompletedSuccessfully) { _ = pending.Result; }", "pending.Result")]
    [InlineData("if (pending.IsCompletedSuccessfully) { _ = pending.Result; _ = pending.AsTask(); }", "pending.Result")]
    [InlineData("_ = pending.GetAwaiter().GetResult(); if (pending.IsCompletedSuccessfully) { _ = pending.Result; }", "pending.GetAwaiter().GetResult()", "pending.Result")]
    [InlineData("if (pending.IsCompletedSuccessfully) { _ = pending.Result; _ = pending.GetAwaiter().GetResult(); }", "pending.Result", "pending.GetAwaiter().GetResult()")]
    [InlineData("var copy = pending; if (pending.IsCompletedSuccessfully) { _ = pending.Result; }", "pending.Result")]
    [InlineData("var copy = pending; if (copy.IsCompletedSuccessfully) { _ = copy.Result; }", "copy.Result")]
    [InlineData("if (pending.IsCompletedSuccessfully) { _ = pending.Result; } Store(pending);", "pending.Result")]
    [InlineData("Store(pending); if (pending.IsCompletedSuccessfully) { _ = pending.Result; }", "pending.Result")]
    [InlineData("Escape(ref pending); if (pending.IsCompletedSuccessfully) { _ = pending.Result; }", "pending.Result")]
    [InlineData("Observe(in pending); if (pending.IsCompletedSuccessfully) { _ = pending.Result; }", "pending.Result")]
    [InlineData("object boxed = pending; if (pending.IsCompletedSuccessfully) { _ = pending.Result; }", "pending.Result")]
    [InlineData("Func<ValueTask<Option<int>>> capture = () => pending; if (pending.IsCompletedSuccessfully) { _ = pending.Result; }", "pending.Result")]
    [InlineData("ValueTask<Option<int>> Capture() => pending; _ = Capture(); if (pending.IsCompletedSuccessfully) { _ = pending.Result; }", "pending.Result")]
    [InlineData("Func<Option<int>> read = () => pending.IsCompletedSuccessfully ? pending.Result : Option.Some(0);", "pending.Result")]
    [InlineData("while (choose && pending.IsCompletedSuccessfully) { _ = pending.Result; }", "pending.Result")]
    [InlineData("try { if (pending.IsCompletedSuccessfully) { _ = pending.Result; } } finally { _ = await pending; }", "pending.Result")]
    [InlineData("if (field.IsCompletedSuccessfully) { _ = field.Result; }", "field.Result")]
    public async Task UnprovenFastPathsKeepExactDiagnostics(string body, params string[] accesses)
    {
        var source = $$"""
            using FunnySharp;
            class C
            {
                ValueTask<Option<int>> field;

                static ValueTask<Option<int>> LoadAsync() => ValueTask.FromResult(Option.Some(1));
                static void Store(ValueTask<Option<int>> value) { }
                static void Escape(ref ValueTask<Option<int>> value) { }
                static void Observe(in ValueTask<Option<int>> value) { }

                async ValueTask<Option<int>> UseAsync(
                    ValueTask<Option<int>> pending,
                    ValueTask<Option<int>> other,
                    bool choose)
                {
                    {{body}}
                    return Option.Some(0);
                }
            }
            """;

        var diagnostics = (await AnalyzerTestAssert.AnalyzeAsync(source))
            .OrderBy(diagnostic => diagnostic.Location.SourceSpan.Start)
            .ToArray();

        Assert.Equal(accesses.Length, diagnostics.Length);
        var searchFrom = 0;
        for (var index = 0; index < accesses.Length; index++)
        {
            var access = accesses[index];
            var start = source.IndexOf(access, searchFrom, StringComparison.Ordinal);
            Assert.True(start >= 0);
            Assert.Equal("FS1004", diagnostics[index].Id);
            Assert.Equal(DiagnosticSeverity.Warning, diagnostics[index].Severity);
            Assert.Equal(new TextSpan(start, access.Length), diagnostics[index].Location.SourceSpan);
            searchFrom = start + access.Length;
        }
    }

    [Theory]
    [InlineData("ref")]
    [InlineData("in")]
    [InlineData("ref readonly")]
    public async Task ByReferenceParameterFastPathIsReported(string modifier)
    {
        await AnalyzerTestAssert.DiagnosticAsync(
            $$"""
            using FunnySharp;
            class C
            {
                Option<int> Use({{modifier}} ValueTask<Option<int>> pending)
                {
                    if (pending.IsCompletedSuccessfully)
                        return pending.Result;
                    return Option.Some(0);
                }
            }
            """,
            "FS1004");
    }

    [Fact]
    public async Task ByReferenceLocalFastPathIsReported()
    {
        await AnalyzerTestAssert.DiagnosticAsync(
            """
            using FunnySharp;
            class C
            {
                Option<int> Use(ValueTask<Option<int>> input)
                {
                    ref var pending = ref input;
                    if (pending.IsCompletedSuccessfully)
                        return pending.Result;
                    return Option.Some(0);
                }
            }
            """,
            "FS1004");
    }

    [Fact]
    public async Task UnsupportedInitializerRootKeepsDiagnostic()
    {
        await AnalyzerTestAssert.DiagnosticAsync(
            """
            using FunnySharp;
            class C
            {
                static readonly Func<ValueTask<Option<int>>, Option<int>> Read =
                    pending => pending.IsCompletedSuccessfully ? pending.Result : Option.Some(0);
            }
            """,
            "FS1004");
    }

    [Fact]
    public async Task UserNonOutcomeValueTaskIsQuiet()
    {
        await AnalyzerTestAssert.QuietAsync(
            """
            using FunnySharp;
            class C
            {
                ValueTask<int> LoadAsync() => ValueTask.FromResult(1);

                void Use()
                {
                    var value = LoadAsync().Result;
                    _ = value;
                    var direct = LoadAsync().GetAwaiter().GetResult();
                    _ = direct;
                }
            }
            """);
    }

    [Fact]
    public async Task AsTaskConversionIsQuiet()
    {
        await AnalyzerTestAssert.QuietAsync(
            """
            using FunnySharp;
            class C
            {
                async Task UseAsync()
                {
                    var task = Option.Some(1).MapValueAsync(number => ValueTask.FromResult(number)).AsTask();
                    var value = await task;
                    _ = value;
                }
            }
            """);
    }
}
