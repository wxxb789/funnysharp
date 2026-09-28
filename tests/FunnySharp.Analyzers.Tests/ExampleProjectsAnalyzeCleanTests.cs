namespace FunnySharp.Analyzers.Tests;

using System.Text;
using FunnySharp.TestSupport;
using Microsoft.CodeAnalysis;

/// <summary>
/// Compiles the repository's example projects against the real FunnySharp assemblies and asserts
/// the shipped analyzers report no unsuppressed diagnostic: correct ordinary code stays quiet.
/// </summary>
public sealed class ExampleProjectsAnalyzeCleanTests
{
    [Fact]
    public async Task ConsoleExamplesAreClean()
    {
        var sources = ReadProjectSources(
            Path.Combine("examples", "FunnySharp.Examples"));
        await AssertCleanAsync(sources, OutputKind.ConsoleApplication, includeAspNetCore: false);
    }

    [Fact]
    public async Task DocumentationSamplesAreClean()
    {
        var sources = ReadProjectSources(
            Path.Combine("examples", "FunnySharp.DocumentationSamples"));
        await AssertCleanAsync(sources, OutputKind.DynamicallyLinkedLibrary, includeAspNetCore: true);
    }

    [Fact]
    public async Task AspNetCoreExamplesAreClean()
    {
        var sources = ReadProjectSources(
            Path.Combine("examples", "FunnySharp.AspNetCore.Examples"));
        await AssertCleanAsync(sources, OutputKind.ConsoleApplication, includeAspNetCore: true);
    }

    private static async Task AssertCleanAsync(
        IReadOnlyList<string> sources,
        OutputKind outputKind,
        bool includeAspNetCore)
    {
        Assert.NotEmpty(sources);
        var compilation = AnalyzerHarness.CreateCompilation(sources, outputKind, includeAspNetCore);
        var diagnostics = await AnalyzerHarness.GetDiagnosticsAsync(compilation);
        Assert.True(
            diagnostics.IsEmpty,
            BuildFailureMessage(diagnostics));
    }

    private static string BuildFailureMessage(System.Collections.Immutable.ImmutableArray<Diagnostic> diagnostics)
    {
        var builder = new StringBuilder("The examples must analyze clean.");
        foreach (var diagnostic in diagnostics)
        {
            var span = diagnostic.Location.GetLineSpan();
            builder.Append('\n')
                .Append(Path.GetFileName(span.Path))
                .Append('(').Append(span.StartLinePosition.Line + 1).Append("): ")
                .Append(diagnostic.Id)
                .Append(": ")
                .Append(diagnostic.GetMessage());
        }

        return builder.ToString();
    }

    private static IReadOnlyList<string> ReadProjectSources(string relativePath)
    {
        var root = TestRepositoryRoot.Find()
            ?? throw new InvalidOperationException("Could not locate the repository root from " + AppContext.BaseDirectory);
        var directory = Path.Combine(root, relativePath);
        var sources = new List<string>();
        foreach (var file in Directory.EnumerateFiles(directory, "*.cs"))
        {
            sources.Add(File.ReadAllText(file));
        }

        return sources;
    }
}
