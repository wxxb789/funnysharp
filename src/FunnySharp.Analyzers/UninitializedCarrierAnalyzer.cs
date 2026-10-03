namespace FunnySharp.Analyzers;

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

/// <summary>
/// Reports creation of a FunnySharp carrier that has no valid default value through a
/// <c>default</c> expression, a <c>default</c> literal, or a parameterless <c>new()</c>,
/// including creation with an empty object initializer.
/// </summary>
/// <remarks>
/// <c>Result&lt;TValue, TError&gt;</c>, <c>UnitResult&lt;TError&gt;</c>,
/// <c>Validation&lt;TValue, TError&gt;</c>, <c>NonEmpty&lt;T&gt;</c>, <c>Effect&lt;T&gt;</c>,
/// <c>Effect&lt;TEnvironment, T&gt;</c>, <c>Lens&lt;TSource, TFocus&gt;</c>, and
/// <c>Optional&lt;TSource, TFocus&gt;</c> are uninitialized in their default state and every member
/// that reads them throws <c>InvalidOperationException</c>; only <c>ToString</c> tolerates them.
/// <c>Option&lt;T&gt;</c> (default is <c>None</c>) and <c>TransitionResult</c> (default is
/// <c>Undefined</c>) are deliberately not reported. Array creation is not reported: array elements
/// are write targets, not value producers, and the uninitialized read is caught at run time.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UninitializedCarrierAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.UninitializedCarrier);

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
                operationContext => AnalyzeDefaultValue(operationContext, types),
                OperationKind.DefaultValue);
            startContext.RegisterOperationAction(
                operationContext => AnalyzeObjectCreation(operationContext, types),
                OperationKind.ObjectCreation);
        });
    }

    private static void AnalyzeDefaultValue(OperationAnalysisContext context, FunnySharpWellKnownTypes types)
    {
        var operation = (IDefaultValueOperation)context.Operation;
        if (types.IsNonDefaultable(operation.Type))
        {
            Report(context, operation.Type!);
        }
    }

    private static void AnalyzeObjectCreation(OperationAnalysisContext context, FunnySharpWellKnownTypes types)
    {
        var operation = (IObjectCreationOperation)context.Operation;
        if (operation.Arguments.Length == 0 &&
            (operation.Initializer is null || operation.Initializer.Initializers.IsEmpty) &&
            types.IsNonDefaultable(operation.Type))
        {
            Report(context, operation.Type!);
        }
    }

    private static void Report(OperationAnalysisContext context, ITypeSymbol type)
    {
        context.ReportDiagnostic(Diagnostic.Create(
            Descriptors.UninitializedCarrier,
            context.Operation.Syntax.GetLocation(),
            type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
    }
}
