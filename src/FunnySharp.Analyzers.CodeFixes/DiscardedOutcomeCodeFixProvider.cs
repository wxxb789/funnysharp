namespace FunnySharp.Analyzers;

using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

/// <summary>
/// Provides the FS1002 code fix: it converts a silently discarded outcome statement into an
/// explicit discard assignment.
/// </summary>
/// <remarks>
/// The fix is registered only when the generated assignment binds as a discard rather than an
/// existing underscore variable. It preserves the statement's behavior and trivia without deciding
/// what the discarded outcome should mean.
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
            if (root is null)
            {
                continue;
            }

            var statement = root
                .FindToken(diagnostic.Location.SourceSpan.Start)
                .Parent?
                .FirstAncestorOrSelf<ExpressionStatementSyntax>();
            if (statement is null)
            {
                continue;
            }

            var annotation = new SyntaxAnnotation();
            var assignment = SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                SyntaxFactory.ParseExpression("_").WithTrailingTrivia(SyntaxFactory.Space),
                statement.Expression.WithoutLeadingTrivia())
                .WithOperatorToken(SyntaxFactory.Token(SyntaxKind.EqualsToken)
                    .WithTrailingTrivia(SyntaxFactory.Space))
                .WithLeadingTrivia(statement.Expression.GetLeadingTrivia())
                .WithAdditionalAnnotations(annotation);
            var candidateDocument = context.Document.WithSyntaxRoot(
                root.ReplaceNode(statement, statement.WithExpression(assignment)));
            var candidateRoot = await candidateDocument.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var generatedAssignment = candidateRoot!.GetAnnotatedNodes(annotation).Single();
            var semanticModel = await candidateDocument.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
            if (semanticModel?.GetOperation(generatedAssignment, context.CancellationToken) is not
                ISimpleAssignmentOperation { Target: IDiscardOperation })
            {
                continue;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    "Make the discarded outcome explicit with '_ ='",
                    cancellationToken => Task.FromResult(candidateDocument),
                    EquivalenceKey),
                diagnostic);
        }
    }
}
