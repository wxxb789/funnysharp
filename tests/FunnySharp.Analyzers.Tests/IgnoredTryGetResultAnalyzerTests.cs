namespace FunnySharp.Analyzers.Tests;

public sealed class IgnoredTryGetResultAnalyzerTests
{
    [Fact]
    public async Task IgnoredOptionTryGetValueIsReported()
    {
        await AnalyzerTestAssert.DiagnosticAsync(
            """
            using FunnySharp;
            class C
            {
                void Use(Option<int> option)
                {
                    option.TryGetValue(out var value);
                }
            }
            """,
            "FS1003",
            "TryGetValue");
    }

    [Fact]
    public async Task IgnoredResultTryGetValueIsReported()
    {
        await AnalyzerTestAssert.DiagnosticAsync(
            """
            using FunnySharp;
            class C
            {
                void Use(Result<int, string> result)
                {
                    result.TryGetValue(out var value);
                }
            }
            """,
            "FS1003");
    }

    [Fact]
    public async Task IgnoredTryGetErrorIsReported()
    {
        await AnalyzerTestAssert.DiagnosticAsync(
            """
            using FunnySharp;
            class C
            {
                void Use(UnitResult<string> result)
                {
                    result.TryGetError(out var error);
                }
            }
            """,
            "FS1003");
    }

    [Fact]
    public async Task IgnoredTryGetErrorsIsReported()
    {
        await AnalyzerTestAssert.DiagnosticAsync(
            """
            using FunnySharp;
            class C
            {
                void Use(Validation<int, string> validation)
                {
                    validation.TryGetErrors(out var errors);
                }
            }
            """,
            "FS1003");
    }

    [Fact]
    public async Task IgnoredTryGetChangeIsReported()
    {
        await AnalyzerTestAssert.DiagnosticAsync(
            """
            using FunnySharp;
            class C
            {
                void Use(TransitionResult<int, int, string> transition)
                {
                    transition.TryGetChange(out var change);
                }
            }
            """,
            "FS1003");
    }

    [Fact]
    public async Task PreDeclaredOutVariableIsReported()
    {
        await AnalyzerTestAssert.DiagnosticAsync(
            """
            using FunnySharp;
            class C
            {
                void Use(Option<int> option)
                {
                    int value;
                    option.TryGetValue(out value);
                }
            }
            """,
            "FS1003");
    }

    [Fact]
    public async Task GuardedTryGetIsQuiet()
    {
        await AnalyzerTestAssert.QuietAsync(
            """
            using FunnySharp;
            class C
            {
                int Use(Option<int> option)
                {
                    if (option.TryGetValue(out var value))
                    {
                        return value;
                    }

                    while (option.TryGetValue(out var next))
                    {
                        return next;
                    }

                    return option.TryGetValue(out var last) ? last : 0;
                }
            }
            """);
    }

    [Fact]
    public async Task AssignedAndExplicitlyDiscardedTryGetIsQuiet()
    {
        await AnalyzerTestAssert.QuietAsync(
            """
            using FunnySharp;
            class C
            {
                void Use(Option<int> option)
                {
                    var present = option.TryGetValue(out var value);
                    _ = option.TryGetValue(out var second);
                    option.TryGetValue(out _);
                    option.TryGetValue(out var _);
                }
            }
            """);
    }

    [Fact]
    public async Task NonFunnySharpTryGetValueIsQuiet()
    {
        await AnalyzerTestAssert.QuietAsync(
            """
            using System.Collections.Generic;
            class C
            {
                void Use(Dictionary<int, int> values, Own own)
                {
                    values.TryGetValue(1, out var value);
                    own.TryGetValue(out var other);
                }
            }

            class Own
            {
                public bool TryGetValue(out int value)
                {
                    value = 0;
                    return true;
                }
            }
            """);
    }
}
