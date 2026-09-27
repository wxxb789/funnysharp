namespace FunnySharp.Analyzers.Tests;

using FunnySharp;

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
