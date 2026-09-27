namespace FunnySharp.Analyzers.Tests;

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;


/// <summary>
/// Builds in-memory compilations that reference the real FunnySharp assemblies and runs the
/// shipped analyzers against them.
/// </summary>
internal static class AnalyzerHarness
{
    private const string GlobalUsings = """
        global using System;
        global using System.Collections.Generic;
        global using System.IO;
        global using System.Linq;
        global using System.Net.Http;
        global using System.Threading;
        global using System.Threading.Tasks;
        """;

    internal static readonly DiagnosticAnalyzer[] AllAnalyzers =
    [
        new UninitializedCarrierAnalyzer(),
        new DiscardedOutcomeAnalyzer(),
        new IgnoredTryGetResultAnalyzer(),
        new BlockedValueTaskAnalyzer(),
        new SyncDisposeAnalyzer(),
    ];

    internal static CSharpCompilation CreateCompilation(
        string source,
        OutputKind outputKind = OutputKind.DynamicallyLinkedLibrary,
        bool includeAspNetCore = false)
    {
        return CreateCompilation([source], outputKind, includeAspNetCore);
    }

    internal static CSharpCompilation CreateCompilation(
        IReadOnlyList<string> sources,
        OutputKind outputKind = OutputKind.DynamicallyLinkedLibrary,
        bool includeAspNetCore = false)
    {
        var parseOptions = CSharpParseOptions.Default
            .WithLanguageVersion(LanguageVersion.Latest)
            .WithDocumentationMode(DocumentationMode.None);
        var trees = sources.Select(source => CSharpSyntaxTree.ParseText(source, parseOptions))
            .Prepend(CSharpSyntaxTree.ParseText(GlobalUsings, parseOptions));
        var options = CreateOptions(outputKind);
        return CSharpCompilation.Create(
            "AnalyzerHarnessCompilation",
            trees,
            BuildReferences(includeAspNetCore),
            options);
    }

    private static CSharpCompilationOptions CreateOptions(OutputKind outputKind)
    {
        return new CSharpCompilationOptions(outputKind, nullableContextOptions: NullableContextOptions.Enable);
    }

    /// <summary>
    /// Runs all shipped analyzers over a compilation that must itself compile without errors,
    /// and returns the unsuppressed FunnySharp diagnostics.
    /// </summary>
    internal static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(
        CSharpCompilation compilation,
        IReadOnlyList<DiagnosticAnalyzer>? analyzers = null)
    {
        AssertNoCompileErrors(compilation);
        var withAnalyzers = compilation.WithAnalyzers(
            (analyzers ?? AllAnalyzers).ToImmutableArray());
        var diagnostics = await withAnalyzers.GetAllDiagnosticsAsync();
        return diagnostics
            .Where(diagnostic =>
                diagnostic.Id.StartsWith("FS1", StringComparison.Ordinal) &&
                !diagnostic.IsSuppressed)
            .ToImmutableArray();
    }

    /// <summary>
    /// Applies the first code fix of <paramref name="fixProvider"/> for
    /// <paramref name="diagnosticId"/> in <paramref name="source"/> and returns the fixed text.
    /// </summary>
    internal static async Task<string> ApplyFirstFixAsync(
        string source,
        string diagnosticId,
        CodeFixProvider fixProvider,
        bool includeAspNetCore = false)
    {
        using var workspace = new AdhocWorkspace();
        var projectId = ProjectId.CreateNewId(debugName: "AnalyzerHarnessProject");
        var documentId = DocumentId.CreateNewId(projectId, debugName: "AnalyzerHarnessDocument.cs");
        var parseOptions = CSharpParseOptions.Default
            .WithLanguageVersion(LanguageVersion.Latest)
            .WithDocumentationMode(DocumentationMode.None);
        var compilationOptions = CreateOptions(OutputKind.DynamicallyLinkedLibrary);
        var solution = workspace.CurrentSolution
            .AddProject(ProjectInfo.Create(
                projectId,
                VersionStamp.Create(),
                name: "AnalyzerHarnessProject",
                assemblyName: "AnalyzerHarnessProject",
                language: LanguageNames.CSharp,
                compilationOptions: compilationOptions,
                parseOptions: parseOptions,
                metadataReferences: BuildReferences(includeAspNetCore)))
            .AddDocument(documentId, "AnalyzerHarnessDocument.cs", SourceText.From(source))
            .AddDocument(DocumentId.CreateNewId(projectId, debugName: "AnalyzerHarnessGlobalUsings.cs"), "AnalyzerHarnessGlobalUsings.cs", SourceText.From(GlobalUsings));
        var document = solution.GetDocument(documentId);
        Assert.True(document is not null, "The harness document must exist.");
        var projectCompilation = await document.Project.GetCompilationAsync();
        var compilation = Assert.IsType<CSharpCompilation>(projectCompilation);
        AssertNoCompileErrors(compilation);
        var diagnostics = await GetDiagnosticsAsync(compilation);
        var target = diagnostics.Single(diagnostic => diagnostic.Id == diagnosticId);

        var fixes = new List<CodeAction>();
        var context = new CodeFixContext(
            document,
            target.Location.SourceSpan,
            ImmutableArray.Create(target),
            (action, _) => fixes.Add(action),
            CancellationToken.None);
        await fixProvider.RegisterCodeFixesAsync(context);
        Assert.NotEmpty(fixes);

        var operations = await fixes[0].GetOperationsAsync(CancellationToken.None);
        var apply = Assert.Single(operations.OfType<ApplyChangesOperation>());
        var fixedDocument = apply.ChangedSolution.GetDocument(documentId);
        Assert.True(fixedDocument is not null, "The fixed harness document must exist.");
        return (await fixedDocument.GetTextAsync(CancellationToken.None)).ToString();
    }

    internal static void AssertNoCompileErrors(Compilation compilation)
    {
        var errors = compilation.GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToImmutableArray();
        Assert.True(
            errors.IsEmpty,
            "The harness source must compile without errors:\n" +
            string.Join('\n', errors));
    }

    internal static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "FunnySharp.slnx")))
        {
            directory = directory.Parent;
        }

        Assert.True(directory is not null, "Could not locate the repository root from " + AppContext.BaseDirectory);
        return directory.FullName;
    }

    private static ImmutableArray<MetadataReference> BuildReferences(bool includeAspNetCore)
    {
        var references = new List<MetadataReference>();
        var names = new HashSet<string>(StringComparer.Ordinal);
        AddDirectoryReferences(
            Path.GetDirectoryName(typeof(object).Assembly.Location)!,
            references,
            names,
            prefix: null);

        references.Add(MetadataReference.CreateFromFile(typeof(FunnySharp.Option).Assembly.Location));
        names.Add(typeof(FunnySharp.Option).Assembly.GetName().Name!);

        if (includeAspNetCore)
        {
            references.Add(MetadataReference.CreateFromFile(
                typeof(FunnySharp.AspNetCore.HttpResultExtensions).Assembly.Location));
            AddDirectoryReferences(
                Path.GetDirectoryName(typeof(Microsoft.AspNetCore.Builder.WebApplication).Assembly.Location)!,
                references,
                names,
                prefix: "Microsoft.");
            references.Add(MetadataReference.CreateFromFile(
                typeof(Microsoft.AspNetCore.Builder.WebApplication).Assembly.Location));
        }

        return references.ToImmutableArray();
    }

    private static void AddDirectoryReferences(
        string directory,
        List<MetadataReference> references,
        HashSet<string> knownNames,
        string? prefix)
    {
        foreach (var file in Directory.EnumerateFiles(directory, "*.dll"))
        {
            var assemblyName = Path.GetFileNameWithoutExtension(file);
            if ((prefix is not null && !assemblyName.StartsWith(prefix, StringComparison.Ordinal)) ||
                !knownNames.Add(assemblyName))
            {
                continue;
            }

            references.Add(MetadataReference.CreateFromFile(file));
        }
    }
}
