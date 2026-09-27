namespace FunnySharp.Analyzers;

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

/// <summary>
/// Reports <c>EffectResourceExtensions.Using</c> calls whose resource type also implements
/// <c>IAsyncDisposable</c>.
/// </summary>
/// <remarks>
/// <c>Using</c> scopes the resource lifetime with the synchronous <c>Dispose</c>, so any
/// asynchronous cleanup implemented through <c>IAsyncDisposable</c> never runs. When the acquired
/// resource type implements both interfaces, <c>UsingAsync</c> runs <c>DisposeAsync</c> instead
/// with an otherwise identical signature, and the code fix applies that rename.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SyncDisposeAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Descriptors.SyncDisposeOfAsyncDisposable);

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
                OperationKind.Invocation);
        });
    }

    private static void Analyze(OperationAnalysisContext context, FunnySharpWellKnownTypes types)
    {
        var invocation = (IInvocationOperation)context.Operation;
        var method = invocation.TargetMethod;
        if (method.Name != "Using" ||
            method.ContainingType?.Name != "EffectResourceExtensions" ||
            !types.IsFunnySharpAssembly(method.ContainingType.ContainingAssembly))
        {
            return;
        }

        // The plain overload is Using<TResource, TResult>; the environment overload is
        // Using<TEnvironment, TResource, TResult>.
        var resourceType = method.TypeArguments.Length switch
        {
            2 => method.TypeArguments[0],
            3 => method.TypeArguments[1],
            _ => null,
        };

        if (resourceType is null || !types.ImplementsIAsyncDisposable(resourceType))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            Descriptors.SyncDisposeOfAsyncDisposable,
            invocation.Syntax.GetLocation(),
            resourceType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
    }
}
