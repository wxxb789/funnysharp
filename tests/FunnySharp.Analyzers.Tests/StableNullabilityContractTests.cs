namespace FunnySharp.Analyzers.Tests;

using Microsoft.CodeAnalysis;

public sealed class StableNullabilityContractTests
{
    [Fact]
    public void OptionTrueFlowAndOptionalHttpMapperCompileWithoutNullableDiagnostics()
    {
        var compilation = AnalyzerHarness.CreateCompilation("""
            using FunnySharp;
            using FunnySharp.AspNetCore;
            using Microsoft.AspNetCore.Http;
            using Microsoft.AspNetCore.Mvc;
            public static class ContractProbe
            {
                public static int Read(Option<string?> option)
                {
                    if (option.TryGetValue(out var value))
                        return value.Length;
                    return 0;
                }
                public static IResult Map(Option<string> option) =>
                    option.ToHttpResult(() => new ProblemDetails { Status = 404 }, null);
            }
            """, includeAspNetCore: true);

        Assert.Empty(compilation.GetDiagnostics(TestContext.Current.CancellationToken).Where(diagnostic =>
            diagnostic.Severity is DiagnosticSeverity.Warning or DiagnosticSeverity.Error));
    }

    [Theory]
    [InlineData("Result<string?, string>.Success(null).TryGetValue(out var value)", "value.Length")]
    [InlineData("Result<int, string?>.Failure(null).TryGetError(out var value)", "value.Length")]
    [InlineData("Validation<string?, string>.Valid(null).TryGetValue(out var value)", "value.Length")]
    [InlineData("Validation<int, string?>.Invalid(null).TryGetErrors(out var value)", "value[0].Length")]
    public void NullablePayloadFlowDoesNotPromiseNonNull(string condition, string access)
    {
        var compilation = AnalyzerHarness.CreateCompilation($$"""
            using FunnySharp;
            public static class ContractProbe
            {
                public static int Read()
                {
                    if ({{condition}})
                        return {{access}};
                    return 0;
                }
            }
            """);

        var diagnostic = Assert.Single(compilation.GetDiagnostics(TestContext.Current.CancellationToken).Where(diagnostic =>
            diagnostic.Severity is DiagnosticSeverity.Warning or DiagnosticSeverity.Error));
        Assert.Equal("CS8602", diagnostic.Id);
    }

    [Fact]
    public void NullableEffectPayloadIsStillNullableAfterAwait()
    {
        var compilation = AnalyzerHarness.CreateCompilation("""
            using FunnySharp;
            public static class ContractProbe
            {
                public static async Task<int> Read()
                {
                    var value = await Effect.FromValue<string?>(null).RunAsync();
                    return value.Length;
                }
            }
            """);

        var diagnostic = Assert.Single(compilation.GetDiagnostics(TestContext.Current.CancellationToken).Where(diagnostic =>
            diagnostic.Severity is DiagnosticSeverity.Warning or DiagnosticSeverity.Error));
        Assert.Equal("CS8602", diagnostic.Id);
    }

    [Theory]
    [InlineData("Option.Some<string>(null)")]
    [InlineData("Option<string>.Some(null)")]
    public void SomeRejectsNullAtTheCompilerBoundary(string expression)
    {
        var compilation = AnalyzerHarness.CreateCompilation($$"""
            using FunnySharp;
            public static class ContractProbe
            {
                public static Option<string> Read() => {{expression}};
            }
            """);

        var diagnostic = Assert.Single(compilation.GetDiagnostics(TestContext.Current.CancellationToken).Where(diagnostic =>
            diagnostic.Severity is DiagnosticSeverity.Warning or DiagnosticSeverity.Error));
        Assert.Equal("CS8625", diagnostic.Id);
    }

    [Fact]
    public void RequiredHttpMapperRejectsNullAtTheCompilerBoundary()
    {
        var compilation = AnalyzerHarness.CreateCompilation("""
            using FunnySharp;
            using FunnySharp.AspNetCore;
            using Microsoft.AspNetCore.Http;
            public static class ContractProbe
            {
                public static IResult Map(Option<string> option) => option.ToHttpResult(null);
            }
            """, includeAspNetCore: true);

        var diagnostic = Assert.Single(compilation.GetDiagnostics(TestContext.Current.CancellationToken).Where(diagnostic =>
            diagnostic.Severity is DiagnosticSeverity.Warning or DiagnosticSeverity.Error));
        Assert.Equal("CS8625", diagnostic.Id);
    }
}
