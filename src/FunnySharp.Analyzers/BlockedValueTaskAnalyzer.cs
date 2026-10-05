namespace FunnySharp.Analyzers;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;

/// <summary>
/// Reports blocking access to a ValueTask that carries a FunnySharp outcome or was produced by a
/// FunnySharp member.
/// </summary>
/// <remarks>
/// FunnySharp's <c>*ValueAsync</c> members return <c>ValueTask&lt;TResult&gt;</c> under the
/// documented single-consumption rule, and the underlying value may be backed by pooled resources.
/// Reading <c>Result</c> or calling <c>GetAwaiter().GetResult()</c> on such a ValueTask may block or
/// consume it incorrectly. A proven successfully completed, single-consumption local or by-value
/// parameter path is exempt; mutable, escaped, repeating, and unsupported paths remain diagnosed.
/// Reported are <c>ValueTask&lt;T&gt;</c> values whose
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

            var proofs = new ConditionalWeakTable<IOperation, Lazy<ImmutableHashSet<TextSpan>>>();
            startContext.RegisterOperationAction(
                operationContext => Analyze(operationContext, types, proofs),
                OperationKind.PropertyReference,
                OperationKind.Invocation);
        });
    }

    private static void Analyze(
        OperationAnalysisContext context,
        FunnySharpWellKnownTypes types,
        ConditionalWeakTable<IOperation, Lazy<ImmutableHashSet<TextSpan>>> proofs)
    {
        var value = GetBlockingReceiver(context.Operation, types, out var blockedMember);
        if (!IsBlockedValueTask(value, types))
        {
            return;
        }

        if (GetSymbol(value) is not null)
        {
            var root = context.Operation;
            while (root.Parent is { } parent)
            {
                root = parent;
            }

            var cancellationToken = context.CancellationToken;
            var proof = proofs.GetValue(root, body =>
                new Lazy<ImmutableHashSet<TextSpan>>(
                    () => FindCompletedAccesses(body, types, cancellationToken)));
            if (proof.Value.Contains(context.Operation.Syntax.Span))
            {
                return;
            }
        }

        Report(context, blockedMember!);
    }

    private static IOperation? GetBlockingReceiver(
        IOperation operation,
        FunnySharpWellKnownTypes types,
        out string? blockedMember)
    {
        if (operation is IPropertyReferenceOperation reference &&
            reference.Property.Name == "Result" &&
            types.IsValueTask(reference.Property.ContainingType))
        {
            blockedMember = "Result";
            return reference.Instance;
        }

        if (operation is IInvocationOperation invocation &&
            invocation.TargetMethod.Name == "GetResult" &&
            invocation.Instance is IInvocationOperation getAwaiterCall &&
            getAwaiterCall.TargetMethod.Name == "GetAwaiter")
        {
            blockedMember = "GetAwaiter().GetResult()";
            return getAwaiterCall.Instance;
        }

        blockedMember = null;
        return null;
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

    private static ImmutableHashSet<TextSpan> FindCompletedAccesses(
        IOperation root,
        FunnySharpWellKnownTypes types,
        CancellationToken cancellationToken)
    {
        var operations = root.DescendantsAndSelf().ToImmutableArray();
        if (operations.Any(operation => operation is ILoopOperation or ITryOperation or IInvalidOperation))
        {
            return ImmutableHashSet<TextSpan>.Empty;
        }

        var symbols = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
        foreach (var operation in operations)
        {
            var receiver = GetBlockingReceiver(operation, types, out _);
            if (IsBlockedValueTask(receiver, types) && GetSymbol(receiver) is { } symbol)
            {
                symbols.Add(symbol);
            }
        }

        symbols.RemoveWhere(symbol => !IsStable(symbol, operations, types));
        if (symbols.Count == 0)
        {
            return ImmutableHashSet<TextSpan>.Empty;
        }

        // Unsupported roots keep the original diagnostic. No analysis failure grants an exemption.
        ControlFlowGraph? graph = root switch
        {
            IMethodBodyOperation body => ControlFlowGraph.Create(body, cancellationToken),
            IConstructorBodyOperation body => ControlFlowGraph.Create(body, cancellationToken),
            IBlockOperation body => ControlFlowGraph.Create(body, cancellationToken),
            _ => null,
        };
        if (graph is null)
        {
            return ImmutableHashSet<TextSpan>.Empty;
        }

        foreach (var block in graph.Blocks)
        {
            if (block.Predecessors.Any(branch => branch.Source.Ordinal >= block.Ordinal))
            {
                return ImmutableHashSet<TextSpan>.Empty;
            }

            for (ControlFlowRegion? region = block.EnclosingRegion; region is not null; region = region.EnclosingRegion)
            {
                if (region.Kind is not (ControlFlowRegionKind.Root or ControlFlowRegionKind.LocalLifetime))
                {
                    return ImmutableHashSet<TextSpan>.Empty;
                }
            }
        }

        var blockOperations = graph.Blocks.Select(block =>
            block.Operations.SelectMany(operation => operation.DescendantsAndSelf())
                .Concat(block.BranchValue is { } branchValue
                    ? branchValue.DescendantsAndSelf()
                    : Enumerable.Empty<IOperation>())
                .ToImmutableArray()).ToImmutableArray();

        // CFG operations are different instances. Consumption sites retain their kind and span,
        // including when conditional results or configured awaitables move into flow captures.
        var loweredSites = blockOperations.SelectMany(block => block)
            .Select(operation => (operation.Kind, operation.Syntax.Span))
            .ToImmutableHashSet();
        var captures = new Dictionary<CaptureId, IOperation?>();
        foreach (var capture in blockOperations.SelectMany(block => block).OfType<IFlowCaptureOperation>())
        {
            captures[capture.Id] = captures.ContainsKey(capture.Id) ? null : capture.Value;
        }

        var result = ImmutableHashSet.CreateBuilder<TextSpan>();
        foreach (var symbol in symbols)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var consumptions = operations
                .Where(operation => SymbolEqualityComparer.Default.Equals(
                    GetSymbol(GetConsumedValue(operation, types)), symbol))
                .Select(operation => (operation.Kind, operation.Syntax.Span))
                .ToImmutableHashSet();

            // An unrecognized lowering must not hide a prior or later consumption.
            if (consumptions.IsSubsetOf(loweredSites))
            {
                result.UnionWith(ProveSingleConsumption(
                    symbol, consumptions, graph, blockOperations, captures, types));
            }
        }

        return result.ToImmutable();
    }

    private static ImmutableHashSet<TextSpan> ProveSingleConsumption(
        ISymbol symbol,
        ImmutableHashSet<(OperationKind Kind, TextSpan Span)> consumptions,
        ControlFlowGraph graph,
        ImmutableArray<ImmutableArray<IOperation>> blockOperations,
        Dictionary<CaptureId, IOperation?> captures,
        FunnySharpWellKnownTypes types)
    {
        var reached = new bool[graph.Blocks.Length];
        var completedAt = new bool[graph.Blocks.Length];
        var consumedAt = new bool[graph.Blocks.Length];
        var proven = ImmutableHashSet.CreateBuilder<TextSpan>();
        var uncompleted = new HashSet<TextSpan>();
        reached[0] = true;

        // Back edges and exception regions were excluded. A single forward pass therefore
        // intersects completion facts and unions possible prior consumption at every join.
        foreach (var block in graph.Blocks)
        {
            if (!block.IsReachable || !reached[block.Ordinal])
            {
                continue;
            }

            var completed = completedAt[block.Ordinal];
            var consumed = consumedAt[block.Ordinal];
            foreach (var operation in blockOperations[block.Ordinal])
            {
                if (!consumptions.Contains((operation.Kind, operation.Syntax.Span)))
                {
                    continue;
                }

                if (consumed)
                {
                    // This also withdraws exemptions for accesses earlier on the path.
                    return ImmutableHashSet<TextSpan>.Empty;
                }

                if (completed)
                {
                    proven.Add(operation.Syntax.Span);
                }
                else
                {
                    uncompleted.Add(operation.Syntax.Span);
                }

                consumed = true;
            }

            var guard = GetCompletionTest(block.BranchValue, captures, types, out var completedWhenTrue);
            foreach (var branch in new[] { block.FallThroughSuccessor, block.ConditionalSuccessor })
            {
                if (branch?.Destination is not { } destination)
                {
                    continue;
                }

                var nextCompleted = completed;
                if (block.ConditionKind != ControlFlowConditionKind.None &&
                    SymbolEqualityComparer.Default.Equals(guard, symbol))
                {
                    var takesTrue = branch.IsConditionalSuccessor ==
                        (block.ConditionKind == ControlFlowConditionKind.WhenTrue);
                    nextCompleted = takesTrue == completedWhenTrue;
                }

                var ordinal = destination.Ordinal;
                if (reached[ordinal])
                {
                    completedAt[ordinal] &= nextCompleted;
                    consumedAt[ordinal] |= consumed;
                }
                else
                {
                    reached[ordinal] = true;
                    completedAt[ordinal] = nextCompleted;
                    consumedAt[ordinal] = consumed;
                }
            }
        }

        proven.ExceptWith(uncompleted);
        return proven.ToImmutable();
    }

    private static bool IsStable(
        ISymbol symbol,
        ImmutableArray<IOperation> operations,
        FunnySharpWellKnownTypes types)
    {
        foreach (var operation in operations)
        {
            // A copied local is not a new owned value. Replacements are rejected below rather
            // than carrying a completion fact from one value generation to another.
            if (operation is IVariableDeclaratorOperation declarator &&
                SymbolEqualityComparer.Default.Equals(declarator.Symbol, symbol) &&
                declarator.Initializer?.Value is not (
                    IInvocationOperation { TargetMethod.RefKind: RefKind.None } or
                    IObjectCreationOperation or IDefaultValueOperation))
            {
                return false;
            }

            if (!SymbolEqualityComparer.Default.Equals(GetSymbol(operation), symbol))
            {
                continue;
            }

            for (var parent = operation.Parent; parent is not null; parent = parent.Parent)
            {
                if (parent is IAnonymousFunctionOperation or ILocalFunctionOperation)
                {
                    return false;
                }
            }

            var use = operation.Parent;
            if (use is IPropertyReferenceOperation property &&
                property.Property.Name == "IsCompletedSuccessfully" &&
                types.IsValueTask(property.Property.ContainingType) &&
                ReferenceEquals(property.Instance, operation))
            {
                continue;
            }

            if (use is IInvocationOperation invocation &&
                types.IsValueTask(invocation.TargetMethod.ContainingType) &&
                invocation.TargetMethod.Name is "GetAwaiter" or "ConfigureAwait")
            {
                use = use.Parent;
            }

            // Everything else, including assignments, aliases, ref arguments, returns of the
            // ValueTask itself and unknown calls, prevents a local single-consumption proof.
            if (use is null || !ReferenceEquals(GetConsumedValue(use, types), operation))
            {
                return false;
            }
        }

        return true;
    }

    private static IOperation? GetConsumedValue(IOperation operation, FunnySharpWellKnownTypes types)
    {
        if (operation is IAwaitOperation awaited)
        {
            return awaited.Operation is IInvocationOperation configured &&
                configured.TargetMethod.Name == "ConfigureAwait" &&
                types.IsValueTask(configured.TargetMethod.ContainingType)
                    ? configured.Instance
                    : awaited.Operation;
        }

        if (operation is IInvocationOperation invocation &&
            invocation.TargetMethod.Name == "AsTask" &&
            types.IsValueTask(invocation.TargetMethod.ContainingType))
        {
            return invocation.Instance;
        }

        return GetBlockingReceiver(operation, types, out _);
    }

    private static ISymbol? GetSymbol(IOperation? operation)
    {
        return operation switch
        {
            ILocalReferenceOperation { Local.RefKind: RefKind.None } local => local.Local,
            IParameterReferenceOperation { Parameter.RefKind: RefKind.None } parameter => parameter.Parameter,
            _ => null,
        };
    }

    private static ISymbol? GetCompletionTest(
        IOperation? operation,
        Dictionary<CaptureId, IOperation?> captures,
        FunnySharpWellKnownTypes types,
        out bool completedWhenTrue)
    {
        operation = ResolveCapture(operation, captures);
        completedWhenTrue = true;
        if (operation is IUnaryOperation { OperatorKind: UnaryOperatorKind.Not, OperatorMethod: null } negated)
        {
            var symbol = GetCompletionTest(negated.Operand, captures, types, out completedWhenTrue);
            completedWhenTrue = !completedWhenTrue;
            return symbol;
        }

        return operation is IPropertyReferenceOperation property &&
            property.Property.Name == "IsCompletedSuccessfully" &&
            types.IsValueTask(property.Property.ContainingType)
                ? GetSymbol(ResolveCapture(property.Instance, captures))
                : null;
    }

    private static IOperation? ResolveCapture(
        IOperation? operation,
        Dictionary<CaptureId, IOperation?> captures)
    {
        while (operation is IFlowCaptureReferenceOperation capture)
        {
            if (!captures.TryGetValue(capture.Id, out operation))
            {
                return null;
            }
        }

        return operation;
    }

    private static void Report(OperationAnalysisContext context, string blockedMember)
    {
        context.ReportDiagnostic(Diagnostic.Create(
            Descriptors.BlockedValueTask,
            context.Operation.Syntax.GetLocation(),
            blockedMember));
    }
}
