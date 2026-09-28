namespace FunnySharp.Analyzers;

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

/// <summary>
/// Reports FunnySharp <c>TryGet*</c> calls whose Boolean presence result is ignored.
/// </summary>
/// <remarks>
/// <c>TryGetValue</c>, <c>TryGetError</c>, <c>TryGetErrors</c>, and <c>TryGetChange</c> follow the
/// Try pattern: the Boolean result reports presence, success, failure, or a produced change, and the
/// out value is <c>default</c> otherwise. A bare statement call loses that information and hands
/// the caller a default value. Declaring the out argument as a discard (<c>out _</c>) keeps the
/// statement legal, as does an explicit discard of the whole call (<c>_ = ...;</c>) or a
/// suppression.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class IgnoredTryGetResultAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.IgnoredTryGetResult);

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
        var invocation = statement.Operation switch
        {
            IInvocationOperation direct => direct,
            IConditionalAccessOperation { WhenNotNull: IInvocationOperation conditional } => conditional,
            _ => null,
        };

        if (invocation is null)
        {
            return;
        }

        var method = invocation.TargetMethod;
        if (method is null ||
            method.ReturnType.SpecialType != SpecialType.System_Boolean ||
            !types.IsFunnySharpAssembly(method.ContainingType?.ContainingAssembly) ||
            !IsTryGetMemberName(method.Name))
        {
            return;
        }

        foreach (var argument in invocation.Arguments)
        {
            if (argument.Parameter?.RefKind != RefKind.Out || argument.Value is IDiscardOperation)
            {
                continue;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                Descriptors.IgnoredTryGetResult,
                invocation.Syntax.GetLocation(),
                method.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
            return;
        }
    }

    private static bool IsTryGetMemberName(string name)
    {
        return name == "TryGetValue" || name == "TryGetError" || name == "TryGetErrors" || name == "TryGetChange";
    }
}
