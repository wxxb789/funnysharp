namespace FunnySharp.Analyzers.Tests;

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

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
        AssertDiscardTargets(fixedText);

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
        AssertDiscardTargets(fixedText);

        await AnalyzerTestAssert.QuietAsync(fixedText);
    }

    [Theory]
    [InlineData("void Use() { int _ = 0; Find(); GC.KeepAlive(_); }")]
    [InlineData("void Use() { Option<int> _ = Option.Some(2); Find(); GC.KeepAlive(_); }")]
    [InlineData("void Use(int _) { Find(); }")]
    [InlineData("void Use(Option<int> _) { Find(); }")]
    [InlineData("async Task UseAsync(Option<int> _) { await Task.FromResult(Find()); }")]
    [InlineData("void Use() { Option<int> _ = Option.Some(2); Action call = () => { Find(); GC.KeepAlive(_); }; call(); }")]
    [InlineData("void Use() { Action<int> call = _ => { Find(); }; call(0); }")]
    [InlineData("int _ = 0; void Use() { Find(); }")]
    [InlineData("Option<int> _ = Option.Some(2); void Use() { Find(); }")]
    [InlineData("Option<int> _ { get; set; } = Option.Some(2); void Use() { Find(); }")]
    [InlineData("void Use() { Find(); int _ = 0; GC.KeepAlive(_); }")]
    [InlineData("void Use() { Option<int> @_ = Option.Some(2); Find(); GC.KeepAlive(@_); }")]
    public async Task DiscardedOutcomeFixIsNotOfferedWhenUnderscoreBindsASymbol(string members)
    {
        var source = $$"""
            using FunnySharp;
            class C
            {
                Option<int> Find() => Option.Some(1);
                {{members}}
            }
            """;
        using var workspace = new AdhocWorkspace();
        var (_, diagnostics, actions) = await AnalyzerHarness.GetCodeFixesAsync(
            workspace,
            source,
            "FS1002",
            new DiscardedOutcomeCodeFixProvider());

        Assert.Single(diagnostics);
        Assert.Empty(actions);
    }

    [Theory]
    [InlineData("void Use() { Find(); }")]
    [InlineData("async Task UseAsync() { await Task.FromResult(Find()); }")]
    [InlineData("void Use(C? subject) { subject?.Find(); }")]
    [InlineData("void Other(int _) { } void Use() { Find(); }")]
    [InlineData("void Use() { { int _ = 0; GC.KeepAlive(_); } Find(); }")]
    [InlineData("void Use() { Find(); { int _ = 0; GC.KeepAlive(_); } }")]
    public async Task DiscardedOutcomeFixIsOfferedOnlyForATrueDiscard(string members)
    {
        var source = $$"""
            using FunnySharp;
            class C
            {
                Option<int> Find() => Option.Some(1);
                {{members}}
            }
            """;
        using var workspace = new AdhocWorkspace();
        var (_, diagnostics, actions) = await AnalyzerHarness.GetCodeFixesAsync(
            workspace,
            source,
            "FS1002",
            new DiscardedOutcomeCodeFixProvider());
        Assert.Single(diagnostics);
        Assert.Single(actions);

        var fixedText = await AnalyzerHarness.ApplyFirstFixAsync(
            source,
            "FS1002",
            new DiscardedOutcomeCodeFixProvider());
        AssertDiscardTargets(fixedText);
        await AnalyzerTestAssert.QuietAsync(fixedText);
    }

    [Fact]
    public async Task DiscardedOutcomeFixPreservesStatementAndSemicolonComments()
    {
        const string source = """
            using FunnySharp;
            class C
            {
                Task<Result<int, string>> SaveAsync() => Task.FromResult(Result<int, string>.Success(1));

                async Task UseAsync()
                {
                    // leading statement
                    await /* before call */ SaveAsync() // after expression
                        /* before semicolon */; // trailing statement
                }
            }
            """;
        var original = Assert.Single(AnalyzerHarness.CreateCompilation(source).SyntaxTrees
            .SelectMany(tree => tree.GetRoot().DescendantNodes().OfType<ExpressionStatementSyntax>()));
        var fixedText = await AnalyzerHarness.ApplyFirstFixAsync(
            source,
            "FS1002",
            new DiscardedOutcomeCodeFixProvider());
        var changed = Assert.Single(AnalyzerHarness.CreateCompilation(fixedText).SyntaxTrees
            .SelectMany(tree => tree.GetRoot().DescendantNodes().OfType<ExpressionStatementSyntax>()));
        var assignment = Assert.IsType<AssignmentExpressionSyntax>(changed.Expression);

        Assert.IsType<AwaitExpressionSyntax>(assignment.Right);
        // Statement-leading trivia moves before the discard. Check executable syntax here;
        // complete statement and semicolon comment equality is asserted separately below.
        Assert.True(SyntaxFactory.AreEquivalent(original.Expression, assignment.Right));
        Assert.Equal(original.SemicolonToken.Text, changed.SemicolonToken.Text);
        Assert.Equal(CommentTrivia(original.DescendantTrivia()), CommentTrivia(changed.DescendantTrivia()));
        Assert.Equal(
            CommentTrivia(original.SemicolonToken.LeadingTrivia),
            CommentTrivia(changed.SemicolonToken.LeadingTrivia));
        AssertDiscardTargets(fixedText);
        await AnalyzerTestAssert.QuietAsync(fixedText);
    }

    [Fact]
    public async Task DiscardedOutcomeBatchFixChangesOnlySafeStatements()
    {
        const string source = """
            using FunnySharp;
            class C
            {
                Option<int> Find() => Option.Some(1);

                void Safe()
                {
                    Find();
                }

                void Shadowed(Option<int> _)
                {
                    Find();
                    GC.KeepAlive(_);
                }

                async Task SafeAsync()
                {
                    await Task.FromResult(Find());
                }
            }
            """;
        using var workspace = new AdhocWorkspace();
        var provider = new DiscardedOutcomeCodeFixProvider();
        var (document, diagnostics, actions) = await AnalyzerHarness.GetCodeFixesAsync(
            workspace,
            source,
            "FS1002",
            provider);
        Assert.Collection(
            diagnostics,
            diagnostic => Assert.Equal("FS1002", diagnostic.Id),
            diagnostic => Assert.Equal("FS1002", diagnostic.Id),
            diagnostic => Assert.Equal("FS1002", diagnostic.Id));
        var action = Assert.Single(actions);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cancellation.CancelAfter(TimeSpan.FromSeconds(30));
        var context = new FixAllContext(
            document,
            provider,
            FixAllScope.Document,
            action.EquivalenceKey,
            provider.FixableDiagnosticIds,
            new DocumentDiagnostics(document.Id, diagnostics),
            cancellation.Token);
        var fixAll = await provider.GetFixAllProvider().GetFixAsync(context).WaitAsync(cancellation.Token);
        Assert.NotNull(fixAll);
        var operations = await fixAll.GetOperationsAsync(cancellation.Token).WaitAsync(cancellation.Token);
        var apply = Assert.Single(operations.OfType<ApplyChangesOperation>());
        var fixedDocument = apply.ChangedSolution.GetDocument(document.Id);
        Assert.NotNull(fixedDocument);
        var fixedCompilation = Assert.IsType<CSharpCompilation>(
            await fixedDocument.Project.GetCompilationAsync(TestContext.Current.CancellationToken));
        var remaining = Assert.Single(await AnalyzerHarness.GetDiagnosticsAsync(fixedCompilation));
        Assert.Equal("FS1002", remaining.Id);
        var statement = remaining.Location.SourceTree!.GetRoot(TestContext.Current.CancellationToken)
            .FindNode(remaining.Location.SourceSpan)
            .FirstAncestorOrSelf<ExpressionStatementSyntax>();
        Assert.NotNull(statement);
        Assert.IsType<InvocationExpressionSyntax>(statement.Expression);
        Assert.Equal("Shadowed", statement.FirstAncestorOrSelf<MethodDeclarationSyntax>()!.Identifier.ValueText);
        AssertDiscardTargets((await fixedDocument.GetTextAsync(TestContext.Current.CancellationToken)).ToString(), expectedCount: 2);
    }

    [Fact]
    public async Task OrdinaryAssignmentToAnUnderscoreVariableRemainsQuiet()
    {
        await AnalyzerTestAssert.QuietAsync(
            """
            using FunnySharp;
            class C
            {
                Option<int> Find() => Option.Some(1);

                void Use(Option<int> _)
                {
                    _ = Find();
                    GC.KeepAlive(_);
                }
            }
            """);
    }

    private static void AssertDiscardTargets(string source, int expectedCount = 1)
    {
        var compilation = AnalyzerHarness.CreateCompilation(source);
        AnalyzerHarness.AssertNoCompileErrors(compilation);
        var assignments = compilation.SyntaxTrees
            .SelectMany(tree => tree.GetRoot().DescendantNodes().OfType<AssignmentExpressionSyntax>())
            .ToArray();
        Assert.Equal(expectedCount, assignments.Length);
        foreach (var assignment in assignments)
        {
            var operation = Assert.IsAssignableFrom<ISimpleAssignmentOperation>(
                compilation.GetSemanticModel(assignment.SyntaxTree).GetOperation(assignment));
            Assert.IsAssignableFrom<IDiscardOperation>(operation.Target);
        }
    }

    private static string[] CommentTrivia(IEnumerable<SyntaxTrivia> trivia)
    {
        return trivia
            .Where(item => item.IsKind(SyntaxKind.SingleLineCommentTrivia) ||
                item.IsKind(SyntaxKind.MultiLineCommentTrivia))
            .Select(item => item.ToFullString())
            .ToArray();
    }

    private sealed class DocumentDiagnostics(
        DocumentId documentId,
        ImmutableArray<Diagnostic> diagnostics) : FixAllContext.DiagnosticProvider
    {
        public override Task<IEnumerable<Diagnostic>> GetDocumentDiagnosticsAsync(
            Document document,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IEnumerable<Diagnostic>>(document.Id == documentId ? diagnostics : []);
        }

        public override Task<IEnumerable<Diagnostic>> GetProjectDiagnosticsAsync(
            Project project,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IEnumerable<Diagnostic>>([]);
        }

        public override Task<IEnumerable<Diagnostic>> GetAllDiagnosticsAsync(
            Project project,
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IEnumerable<Diagnostic>>(project.Id == documentId.ProjectId ? diagnostics : []);
        }
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

    [Fact]
    public async Task SyncDisposeFixPreservesTriviaOnTheUsingIdentifier()
    {
        var fixedText = await AnalyzerHarness.ApplyFirstFixAsync(
            """
            using FunnySharp;
            class C
            {
                Effect<int> Use()
                {
                    return Effect.FromSync(() => new Both()).
                        /* acquire */ Using /* release */ (resource =>
                            Effect.FromValue(1));
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
        Assert.Contains("/* acquire */ UsingAsync /* release */ (resource =>", fixedText, StringComparison.Ordinal);

        await AnalyzerTestAssert.QuietAsync(fixedText);
    }
}
