namespace FunnySharp.Analyzers.Tests;

public sealed class UninitializedCarrierAnalyzerTests
{
    [Fact]
    public async Task DefaultResultExpressionIsReported()
    {
        await AnalyzerTestAssert.DiagnosticAsync(
            """
            using FunnySharp;
            class C
            {
                Result<int, string> Make() => default(Result<int, string>);
            }
            """,
            "FS1001");
    }

    [Fact]
    public async Task DefaultLiteralAssignmentIsReported()
    {
        await AnalyzerTestAssert.DiagnosticAsync(
            """
            using FunnySharp;
            class C
            {
                Result<int, string> Make()
                {
                    Result<int, string> result = default;
                    return result;
                }
            }
            """,
            "FS1001");
    }

    [Fact]
    public async Task TargetTypedNewIsReported()
    {
        await AnalyzerTestAssert.DiagnosticAsync(
            """
            using FunnySharp;
            class C
            {
                Result<int, string> Make()
                {
                    Result<int, string> result = new();
                    return result;
                }
            }
            """,
            "FS1001");
    }

    [Fact]
    public async Task ObjectCreationIsReported()
    {
        await AnalyzerTestAssert.DiagnosticAsync(
            """
            using FunnySharp;
            class C
            {
                Result<int, string> Make() => new Result<int, string>();
            }
            """,
            "FS1001");
    }

    [Fact]
    public async Task DefaultWithSuppressionOperatorIsReported()
    {
        await AnalyzerTestAssert.DiagnosticAsync(
            """
            using FunnySharp;
            class C
            {
                Result<int, string> Make()
                {
                    Result<int, string> result = default!;
                    return result;
                }
            }
            """,
            "FS1001");
    }

    [Fact]
    public async Task OptionalParameterDefaultIsReported()
    {
        await AnalyzerTestAssert.DiagnosticAsync(
            """
            using FunnySharp;
            class C
            {
                int Make(Result<int, string> result = default) => result.IsSuccess ? 1 : 0;
            }
            """,
            "FS1001");
    }

    [Fact]
    public async Task EqualityAgainstDefaultIsReported()
    {
        await AnalyzerTestAssert.DiagnosticAsync(
            """
            using FunnySharp;
            class C
            {
                bool Make(Result<int, string> result) => result.Equals(default);
            }
            """,
            "FS1001");
    }

    [Theory]
    [InlineData("Result<int, string>")]
    [InlineData("UnitResult<string>")]
    [InlineData("Validation<int, string>")]
    [InlineData("NonEmpty<int>")]
    [InlineData("Effect<int>")]
    [InlineData("Effect<Env, int>")]
    [InlineData("Lens<int, string>")]
    [InlineData("Optional<int, string>")]
    public async Task EveryCarrierWithoutAValidDefaultIsReported(string typeText)
    {
        await AnalyzerTestAssert.DiagnosticAsync(
            """
            using FunnySharp;
            class Env;
            class C
            {
                TYPE Make() => default;
            }
            """.Replace("TYPE", typeText),
            "FS1001");
    }

    [Theory]
    [InlineData("Result<int, string>")]
    [InlineData("UnitResult<string>")]
    [InlineData("Validation<int, string>")]
    [InlineData("NonEmpty<int>")]
    [InlineData("Effect<int>")]
    [InlineData("Effect<Env, int>")]
    [InlineData("Lens<int, string>")]
    [InlineData("Optional<int, string>")]
    public async Task EmptyInitializersDoNotInitializeNondefaultableCarriers(string typeText)
    {
        foreach (var creation in new[] { $"new {typeText} {{ }}", $"new {typeText}() {{ }}", "new() { }" })
        {
            await AnalyzerTestAssert.DiagnosticAsync(
                """
                using FunnySharp;
                class Env;
                class C
                {
                    TYPE Make() => CREATION;
                }
                """.Replace("TYPE", typeText).Replace("CREATION", creation),
                "FS1001");
        }
    }

    [Theory]
    [InlineData("Option<int>")]
    [InlineData("TransitionResult<int, int, string>")]
    [InlineData("UserValue")]
    public async Task EmptyInitializersForDefaultValidTypesStayQuiet(string typeText)
    {
        await AnalyzerTestAssert.QuietAsync(
            """
            using FunnySharp;
            struct UserValue;
            class C
            {
                TYPE Make() => new() { };
            }
            """.Replace("TYPE", typeText));
    }

    [Fact]
    public async Task DefaultOptionIsAValidNoneAndStaysQuiet()
    {
        await AnalyzerTestAssert.QuietAsync(
            """
            using FunnySharp;
            class C
            {
                int Make()
                {
                    Option<int> absent = default;
                    return absent.IsNone ? 0 : 1;
                }
            }
            """);
    }

    [Fact]
    public async Task DefaultTransitionResultIsAValidUndefinedAndStaysQuiet()
    {
        await AnalyzerTestAssert.QuietAsync(
            """
            using FunnySharp;
            class C
            {
                int Make()
                {
                    TransitionResult<int, int, string> transition = default;
                    return transition.IsUndefined ? 0 : 1;
                }
            }
            """);
    }

    [Fact]
    public async Task CarrierArrayCreationStaysQuiet()
    {
        await AnalyzerTestAssert.QuietAsync(
            """
            using FunnySharp;
            class C
            {
                Result<int, string>[] Make() => new Result<int, string>[8];
            }
            """);
    }

    [Fact]
    public async Task OrdinaryFactoryConstructionStaysQuiet()
    {
        await AnalyzerTestAssert.QuietAsync(
            """
            using FunnySharp;
            class C
            {
                int Make()
                {
                    var some = Option.Some(1);
                    var none = Option.None<int>();
                    var success = Result<int, string>.Success(1);
                    var failure = UnitResult<string>.Failure("error");
                    var valid = Validation<int, string>.Valid(1);
                    var lens = Lens.Create((C c) => 1, (C c, int value) => c);
                    return some.TryGetValue(out var value) ? value : none.IsSome ? -1 : 0;
                }
            }
            """);
    }

    [Fact]
    public async Task PragmaSuppressionSilencesTheDiagnostic()
    {
        await AnalyzerTestAssert.QuietAsync(
            """
            using FunnySharp;
            class C
            {
                Result<int, string> Make()
                {
            #pragma warning disable FS1001
                    Result<int, string> result = default;
            #pragma warning restore FS1001
                    return Result<int, string>.Success(1);
                }
            }
            """);
    }
}
