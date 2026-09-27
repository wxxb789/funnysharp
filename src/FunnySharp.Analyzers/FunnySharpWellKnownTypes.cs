namespace FunnySharp.Analyzers;

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

/// <summary>
/// Resolves and matches the FunnySharp types that the package analyzers reason about, bound to one
/// compilation.
/// </summary>
/// <remarks>
/// Matching is by fully qualified metadata name anchored at the FunnySharp core assembly that is
/// actually referenced by the compilation (located through <c>FunnySharp.Option`1</c>): a consumer
/// type that happens to reuse a FunnySharp namespace and type name in a different assembly is never
/// matched. When the compilation references neither FunnySharp assembly, the analyzers stay inert.
/// </remarks>
internal sealed class FunnySharpWellKnownTypes
{
    private const string CoreAssemblyName = "FunnySharp";
    private const string AspNetCoreAssemblyName = "FunnySharp.AspNetCore";

    private static readonly ImmutableArray<string> OutcomeTypeNames = ImmutableArray.Create(
        "FunnySharp.Option`1",
        "FunnySharp.Result`2",
        "FunnySharp.UnitResult`1",
        "FunnySharp.Validation`2",
        "FunnySharp.TransitionResult`3",
        "FunnySharp.Effect`1",
        "FunnySharp.Effect`2",
        "FunnySharp.StateTransition`2",
        "FunnySharp.StateMachine`4");

    private static readonly ImmutableArray<string> NonDefaultableTypeNames = ImmutableArray.Create(
        "FunnySharp.Result`2",
        "FunnySharp.UnitResult`1",
        "FunnySharp.Validation`2",
        "FunnySharp.NonEmpty`1",
        "FunnySharp.Effect`1",
        "FunnySharp.Effect`2",
        "FunnySharp.Lens`2",
        "FunnySharp.Optional`2");

    private readonly ImmutableHashSet<INamedTypeSymbol> outcomeTypes;
    private readonly ImmutableHashSet<INamedTypeSymbol> nonDefaultableTypes;
    private readonly INamedTypeSymbol taskOfT;
    private readonly INamedTypeSymbol valueTaskOfT;
    private readonly INamedTypeSymbol task;
    private readonly INamedTypeSymbol valueTask;
    private readonly INamedTypeSymbol asyncDisposable;

    private FunnySharpWellKnownTypes(
        ImmutableHashSet<INamedTypeSymbol> outcomeTypes,
        ImmutableHashSet<INamedTypeSymbol> nonDefaultableTypes,
        INamedTypeSymbol taskOfT,
        INamedTypeSymbol valueTaskOfT,
        INamedTypeSymbol task,
        INamedTypeSymbol valueTask,
        INamedTypeSymbol asyncDisposable)
    {
        this.outcomeTypes = outcomeTypes;
        this.nonDefaultableTypes = nonDefaultableTypes;
        this.taskOfT = taskOfT;
        this.valueTaskOfT = valueTaskOfT;
        this.task = task;
        this.valueTask = valueTask;
        this.asyncDisposable = asyncDisposable;
    }

    /// <summary>
    /// Binds the FunnySharp type table to <paramref name="compilation"/>, or returns null when the
    /// compilation does not reference the FunnySharp core assembly.
    /// </summary>
    public static FunnySharpWellKnownTypes? TryCreate(Compilation compilation)
    {
        // Option`1 is the anchor: it exists in every FunnySharp reference and identifies the core
        // assembly actually referenced by this compilation.
        var anchor = compilation.GetTypeByMetadataName("FunnySharp.Option`1");
        if (anchor is null)
        {
            return null;
        }

        var taskOfT = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");
        var valueTaskOfT = compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask`1");
        var task = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
        var valueTask = compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask");
        var asyncDisposable = compilation.GetTypeByMetadataName("System.IAsyncDisposable");
        if (taskOfT is null || valueTaskOfT is null || task is null || valueTask is null || asyncDisposable is null)
        {
            return null;
        }

        return new FunnySharpWellKnownTypes(
            ResolveCoreTypes(compilation, anchor.ContainingAssembly, OutcomeTypeNames),
            ResolveCoreTypes(compilation, anchor.ContainingAssembly, NonDefaultableTypeNames),
            taskOfT,
            valueTaskOfT,
            task,
            valueTask,
            asyncDisposable);
    }

    /// <summary>Gets a value indicating whether the assembly is one of the two shipped FunnySharp assemblies.</summary>
    public bool IsFunnySharpAssembly(IAssemblySymbol? assembly)
    {
        return assembly is not null &&
            (string.Equals(assembly.Name, CoreAssemblyName, System.StringComparison.Ordinal) ||
                string.Equals(assembly.Name, AspNetCoreAssemblyName, System.StringComparison.Ordinal));
    }

    /// <summary>
    /// Gets a value indicating whether the type is a FunnySharp semantic outcome: one of the four
    /// carriers, an <c>Effect&lt;T&gt;</c>/<c>Effect&lt;TEnvironment, T&gt;</c> deferred-work value, a
    /// composed state-transition delegate, or the transition result.
    /// </summary>
    public bool IsOutcomeType(ITypeSymbol? type)
    {
        return type is INamedTypeSymbol namedType &&
            this.outcomeTypes.Contains(namedType.OriginalDefinition);
    }

    /// <summary>Gets a value indicating whether the type has no valid default value.</summary>
    public bool IsNonDefaultable(ITypeSymbol? type)
    {
        return type is INamedTypeSymbol namedType &&
            this.nonDefaultableTypes.Contains(namedType.OriginalDefinition);
    }

    /// <summary>Gets a value indicating whether the type is the generic or non-generic ValueTask.</summary>
    public bool IsValueTask(ITypeSymbol? type)
    {
        if (type is not INamedTypeSymbol namedType)
        {
            return false;
        }

        var original = namedType.OriginalDefinition;
        return SymbolEqualityComparer.Default.Equals(original, this.valueTaskOfT) ||
            SymbolEqualityComparer.Default.Equals(original, this.valueTask);
    }

    /// <summary>Gets a value indicating whether the type is Task or ValueTask with any payload.</summary>
    public bool IsAwaitable(ITypeSymbol? type)
    {
        if (type is not INamedTypeSymbol namedType)
        {
            return false;
        }

        var original = namedType.OriginalDefinition;
        return SymbolEqualityComparer.Default.Equals(original, this.taskOfT) ||
            SymbolEqualityComparer.Default.Equals(original, this.valueTaskOfT) ||
            SymbolEqualityComparer.Default.Equals(original, this.task) ||
            SymbolEqualityComparer.Default.Equals(original, this.valueTask);
    }

    /// <summary>
    /// Gets a value indicating whether the type is a Task or ValueTask whose payload is a FunnySharp
    /// semantic outcome.
    /// </summary>
    public bool IsAwaitableOfOutcome(ITypeSymbol? type)
    {
        return type is INamedTypeSymbol { TypeArguments.Length: 1 } namedType &&
            this.IsAwaitable(type) &&
            this.IsOutcomeType(namedType.TypeArguments[0]);
    }

    /// <summary>Gets a value indicating whether the type implements IAsyncDisposable.</summary>
    public bool ImplementsIAsyncDisposable(ITypeSymbol type)
    {
        if (SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, this.asyncDisposable))
        {
            return true;
        }

        foreach (var implemented in type.AllInterfaces)
        {
            if (SymbolEqualityComparer.Default.Equals(implemented, this.asyncDisposable))
            {
                return true;
            }
        }

        return false;
    }

    private static ImmutableHashSet<INamedTypeSymbol> ResolveCoreTypes(
        Compilation compilation,
        IAssemblySymbol coreAssembly,
        ImmutableArray<string> metadataNames)
    {
        var comparer = SymbolEqualityComparer.Default;
        var builder = ImmutableHashSet.CreateBuilder<INamedTypeSymbol>(comparer);
        foreach (var metadataName in metadataNames)
        {
            var symbol = compilation.GetTypeByMetadataName(metadataName);
            if (symbol is not null && comparer.Equals(symbol.ContainingAssembly, coreAssembly))
            {
                builder.Add(symbol);
            }
        }

        return builder.ToImmutable();
    }
}
