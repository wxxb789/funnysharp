namespace FunnySharp.Analyzers.Tests;

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

/// <summary>
/// Shared assertions for analyzer tests: exactly one expected FunnySharp diagnostic, or none.
/// </summary>
internal static class AnalyzerTestAssert
{
    internal static async Task DiagnosticAsync(string source, string expectedId, string? messageFragment = null)
    {
        var diagnostics = await AnalyzeAsync(source);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal(expectedId, diagnostic.Id);
        if (messageFragment is not null)
        {
            Assert.Contains(messageFragment, diagnostic.GetMessage());
        }
    }

    internal static async Task QuietAsync(string source)
    {
        var diagnostics = await AnalyzeAsync(source);
        if (diagnostics.IsEmpty)
        {
            return;
        }

        Assert.Fail(
            "Expected no diagnostics but got:\n" +
            string.Join('\n', diagnostics.Select(diagnostic =>
                diagnostic.Id + ": " + diagnostic.GetMessage() + " at line " +
                (diagnostic.Location.GetLineSpan().StartLinePosition.Line + 1))));
    }

    internal static async Task<ImmutableArray<Diagnostic>> AnalyzeAsync(string source)
    {
        var compilation = AnalyzerHarness.CreateCompilation(source);
        return await AnalyzerHarness.GetDiagnosticsAsync(compilation);
    }
}
