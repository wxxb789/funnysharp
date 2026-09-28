namespace FunnySharp.Analyzers;

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

/// <summary>
/// Provides the FS1005 code fix: it renames <c>Using</c> to <c>UsingAsync</c> so a resource that
/// implements <c>IAsyncDisposable</c> is disposed asynchronously.
/// </summary>
/// <remarks>
/// The fix is a pure rename: <c>UsingAsync</c> has the same overload shape as <c>Using</c> for a
/// resource type that implements both interfaces, so type inference and argument shapes are
/// unchanged, and only the disposal path switches from <c>Dispose</c> to <c>DisposeAsync</c>.
/// </remarks>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SyncDisposeCodeFixProvider))]
public sealed class SyncDisposeCodeFixProvider : CodeFixProvider
{
    private const string EquivalenceKey = "UseUsingAsync";

    /// <inheritdoc />
    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(DiagnosticIds.SyncDisposeOfAsyncDisposable);

    /// <inheritdoc />
    public override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    /// <inheritdoc />
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        foreach (var diagnostic in context.Diagnostics)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            // The FS1005 location is the whole Using invocation, whose receiver chain can contain
            // nested invocations (the acquisition effect), so match the ancestor invocation whose
            // member name is the one the diagnostic is about.
            var invocation = root?
                .FindToken(context.Span.Start)
                .Parent?
                .FirstAncestorOrSelf<InvocationExpressionSyntax>(candidate =>
                    candidate.Expression is MemberAccessExpressionSyntax access &&
                    access.Name.Identifier.ValueText == "Using");
            if (invocation?.Expression is not MemberAccessExpressionSyntax memberAccess)
            {
                continue;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    "Use UsingAsync so DisposeAsync runs",
                    cancellationToken => ApplyAsync(context.Document, invocation, memberAccess, cancellationToken),
                    EquivalenceKey),
                diagnostic);
        }
    }

    private static async Task<Document> ApplyAsync(
        Document document,
        InvocationExpressionSyntax invocation,
        MemberAccessExpressionSyntax memberAccess,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return document;
        }

        // SyntaxFactory.Identifier(string) yields a bare token and WithIdentifier replaces the
        // identifier wholesale, so the replacement must carry the original identifier's trivia
        // or comments beside Using are deleted by the rename.
        var identifier = memberAccess.Name.Identifier;
        var renamed = memberAccess.WithName(
            memberAccess.Name.WithIdentifier(SyntaxFactory.Identifier(
                identifier.LeadingTrivia,
                "UsingAsync",
                identifier.TrailingTrivia)));
        return document.WithSyntaxRoot(root.ReplaceNode(invocation, invocation.WithExpression(renamed)));
    }
}
