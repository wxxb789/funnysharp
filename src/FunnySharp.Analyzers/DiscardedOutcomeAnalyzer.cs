namespace FunnySharp.Analyzers;

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

/// <summary>
/// Reports expression statements that silently discard a FunnySharp outcome.
/// </summary>
/// <remarks>
/// A statement whose value is one of the FunnySharp semantic outcome types — an
/// <c>Option</c>, <c>Result</c>, <c>UnitResult</c>, <c>Validation</c>, <c>TransitionResult</c>,
/// <c>Effect</c>, composed <c>StateTransition</c> or <c>StateMachine</c> delegate — drops absence,
/// failure, accumulated-validation, transition, or deferred-work information without a trace. The
/// same holds for a <c>Task</c>/<c>ValueTask</c> of one of those outcomes, awaited or not, from any
/// method. A <c>Task</c>/<c>ValueTask</c> with any other payload is reported only when the invoked
/// member belongs to FunnySharp, because for FunnySharp members an unawaited call means the work
/// never runs at all; awaiting such a member and discarding its non-outcome payload stays legal.
/// The observation members <c>Tap</c>, <c>TapAsync</c>, and <c>TapValueAsync</c> are exempt: their
/// documented purpose is to observe a value and return it, so a statement call is ordinary use.
/// Explicit discards (<c>_ = ...;</c>), assignments, returns, and awaited-and-consumed results are
/// never reported.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DiscardedOutcomeAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.DiscardedOutcome);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(startContext =>
        {
            var types = FunnySharpWellKnownTypes.TryCreate(startContext.Compilation);
            if (types is null)
            {
                return;
            }

            startContext.RegisterOperationAction(
                operationContext => Analyze(operationContext, types),
                OperationKind.ExpressionStatement);
        });
    }

    private static void Analyze(OperationAnalysisContext context, FunnySharpWellKnownTypes types)
    {
        var statement = (IExpressionStatementOperation)context.Operation;
        switch (statement.Operation)
        {
            case IAwaitOperation { Operation: IInvocationOperation awaitedInvocation }:
                // Awaiting consumed the awaitable; only a discarded outcome payload is lost.
                if (types.IsAwaitableOfOutcome(awaitedInvocation.Type))
                {
                    Report(context, awaitedInvocation, "task of an outcome value");
                }

                break;
            case IInvocationOperation invocation:
                CheckInvocation(context, types, invocation);
                break;
            case IConditionalAccessOperation { WhenNotNull: IInvocationOperation conditionalInvocation }:
                CheckInvocation(context, types, conditionalInvocation);
                break;
        }
    }

    private static void CheckInvocation(OperationAnalysisContext context, FunnySharpWellKnownTypes types, IInvocationOperation invocation)
    {
        if (invocation.Type is null)
        {
            return;
        }

        if (IsExemptObservationMember(invocation.TargetMethod, types))
        {
            return;
        }

        if (types.IsOutcomeType(invocation.Type) || types.IsAwaitableOfOutcome(invocation.Type))
        {
            Report(
                context,
                invocation,
                types.IsOutcomeType(invocation.Type) ? "outcome value" : "task of an outcome value");
            return;
        }

        if (types.IsAwaitable(invocation.Type) &&
            types.IsFunnySharpAssembly(invocation.TargetMethod?.ContainingAssembly))
        {
            Report(context, invocation, "unawaited work");
        }
    }

    private static bool IsExemptObservationMember(IMethodSymbol? method, FunnySharpWellKnownTypes types)
    {
        return method is not null &&
            types.IsFunnySharpAssembly(method.ContainingAssembly) &&
            (method.Name == "Tap" || method.Name == "TapAsync" || method.Name == "TapValueAsync");
    }

    private static void Report(OperationAnalysisContext context, IInvocationOperation invocation, string outcomeKind)
    {
        context.ReportDiagnostic(Diagnostic.Create(
            Descriptors.DiscardedOutcome,
            invocation.Syntax.GetLocation(),
            outcomeKind));
    }
}
