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
/// Provides the FS1002 code fix: it converts a silently discarded outcome statement into an
/// explicit discard assignment.
/// </summary>
/// <remarks>
/// The fix is purely mechanical: it does not decide what the discarded outcome should mean, it only
/// makes the discard visible at the call site, so the statement keeps its exact behavior.
/// </remarks>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DiscardedOutcomeCodeFixProvider))]
public sealed class DiscardedOutcomeCodeFixProvider : CodeFixProvider
{
    private const string EquivalenceKey = "MakeDiscardedOutcomeExplicit";

    /// <inheritdoc />
    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        ImmutableArray.Create(DiagnosticIds.DiscardedOutcome);

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
            var statement = root?
                .FindToken(context.Span.Start)
                .Parent?
                .FirstAncestorOrSelf<ExpressionStatementSyntax>();
            if (statement is null)
            {
                continue;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    "Make the discarded outcome explicit with '_ ='",
                    cancellationToken => ApplyAsync(context.Document, statement, cancellationToken),
                    EquivalenceKey),
                diagnostic);
        }
    }

    private static async Task<Document> ApplyAsync(
        Document document,
        ExpressionStatementSyntax statement,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return document;
        }

        var discardAssignment = SyntaxFactory.ExpressionStatement(
            SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                SyntaxFactory.IdentifierName("_").WithTrailingTrivia(SyntaxFactory.Space),
                statement.Expression.WithoutLeadingTrivia()))
            .WithTriviaFrom(statement);

        return document.WithSyntaxRoot(root.ReplaceNode(statement, discardAssignment));
    }
}
