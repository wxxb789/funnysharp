namespace FunnySharp.Analyzers.Tests;

using System.Reflection;
using FunnySharp;
using Microsoft.CodeAnalysis;

/// <summary>
/// Asserts the packaging invariants of the shipped analyzer suite.
/// </summary>
public sealed class AnalyzerPackagingTests
{
    [Fact]
    public void CoreAssemblyReferencesNoCompilerTooling()
    {
        var referenced = typeof(Option).Assembly.GetReferencedAssemblies();
        Assert.DoesNotContain(referenced, name =>
            name.Name!.StartsWith("Microsoft.CodeAnalysis", StringComparison.Ordinal) ||
            name.Name.StartsWith("FunnySharp.Analyzers", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("FS1001", DiagnosticSeverity.Error)]
    [InlineData("FS1002", DiagnosticSeverity.Warning)]
    [InlineData("FS1003", DiagnosticSeverity.Warning)]
    [InlineData("FS1004", DiagnosticSeverity.Warning)]
    [InlineData("FS1005", DiagnosticSeverity.Warning)]
    public void ShippedDiagnosticsUseTheRecordedSeverities(string diagnosticId, DiagnosticSeverity severity)
    {
        var descriptor = AnalyzerHarness.AllAnalyzers
            .SelectMany(analyzer => analyzer.SupportedDiagnostics)
            .Single(descriptor => descriptor.Id == diagnosticId);
        Assert.Equal(severity, descriptor.DefaultSeverity);
        Assert.True(descriptor.IsEnabledByDefault);
        Assert.Equal("FunnySharp", descriptor.Category, StringComparer.Ordinal);
    }

    [Fact]
    public void EveryShippedDiagnosticHasADistinctIdentifier()
    {
        var identifiers = AnalyzerHarness.AllAnalyzers
            .SelectMany(analyzer => analyzer.SupportedDiagnostics)
            .Select(descriptor => descriptor.Id)
            .ToArray();
        Assert.Equal(identifiers.Length, identifiers.Distinct(StringComparer.Ordinal).Count());
    }
}
