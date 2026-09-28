namespace FunnySharp.Analyzers;

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

/// <summary>
/// Reports blocking access to a ValueTask that carries a FunnySharp outcome or was produced by a
/// FunnySharp member.
/// </summary>
/// <remarks>
/// FunnySharp's <c>*ValueAsync</c> members return <c>ValueTask&lt;TResult&gt;</c> under the
/// documented single-consumption rule, and the underlying value may be backed by pooled resources.
/// Reading <c>Result</c> or calling <c>GetAwaiter().GetResult()</c> on such a ValueTask blocks and
/// may consume it without a proper await. Reported are <c>ValueTask&lt;T&gt;</c> values whose
/// payload is a FunnySharp outcome — regardless of where the value came from — and any ValueTask
/// produced directly by a FunnySharp member. Blocking a Task, and the general sync-over-async
/// hazard, stay outside this diagnostic's low-false-positive scope.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class BlockedValueTaskAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.BlockedValueTask);

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
                operationContext => AnalyzePropertyReference(operationContext, types),
                OperationKind.PropertyReference);
            startContext.RegisterOperationAction(
                operationContext => AnalyzeInvocation(operationContext, types),
                OperationKind.Invocation);
        });
    }

    private static void AnalyzePropertyReference(OperationAnalysisContext context, FunnySharpWellKnownTypes types)
    {
        var reference = (IPropertyReferenceOperation)context.Operation;
        if (reference.Property.Name == "Result" &&
            types.IsValueTask(reference.Property.ContainingType) &&
            IsBlockedValueTask(reference.Instance, types))
        {
            Report(context, "Result");
        }
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context, FunnySharpWellKnownTypes types)
    {
        var invocation = (IInvocationOperation)context.Operation;
        if (invocation.TargetMethod.Name == "GetResult" &&
            invocation.Instance is IInvocationOperation getAwaiterCall &&
            getAwaiterCall.TargetMethod.Name == "GetAwaiter" &&
            IsBlockedValueTask(getAwaiterCall.Instance, types))
        {
            Report(context, "GetAwaiter().GetResult()");
        }
    }

    private static bool IsBlockedValueTask(IOperation? value, FunnySharpWellKnownTypes types)
    {
        if (!types.IsValueTask(value?.Type))
        {
            return false;
        }

        return types.IsAwaitableOfOutcome(value!.Type) ||
            (value is IInvocationOperation producer &&
                types.IsFunnySharpAssembly(producer.TargetMethod?.ContainingAssembly));
    }

    private static void Report(OperationAnalysisContext context, string blockedMember)
    {
        context.ReportDiagnostic(Diagnostic.Create(
            Descriptors.BlockedValueTask,
            context.Operation.Syntax.GetLocation(),
            blockedMember));
    }
}
