using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FunnySharp.FusedExecution.Generator;

internal static class SummaryFusion
{
    internal static FusionEmission TryBuild(
        IMethodSymbol method,
        MethodDeclarationSyntax methodSyntax,
        IFieldSymbol field,
        ExpressionSyntax initializer,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();

        if (field.Type is not INamedTypeSymbol fieldType
            || !IsSummaryFieldType(fieldType))
        {
            diagnostics.Add(FusionDiagnostics.InvalidFieldAt(
                methodSyntax,
                "The named field must be a Summary<TSource, TState, TResult>."));
            return new FusionEmission(null, diagnostics.ToImmutable());
        }

        var sourceType = fieldType.TypeArguments[0];
        var resultType = fieldType.TypeArguments[2];

        if (method.Parameters[0].Type is not INamedTypeSymbol parameterType
            || parameterType.OriginalDefinition.SpecialType != SpecialType.System_Collections_Generic_IEnumerable_T
            || !FusionTransform.TypesEqual(parameterType.TypeArguments[0], sourceType))
        {
            diagnostics.Add(FusionDiagnostics.InvalidMethodAt(
                methodSyntax,
                "The fused summary parameter must be IEnumerable<TSource> for the field's TSource."));
            return new FusionEmission(null, diagnostics.ToImmutable());
        }

        if (!FusionTransform.TypesEqual(method.ReturnType, resultType))
        {
            diagnostics.Add(FusionDiagnostics.InvalidMethodAt(
                methodSyntax,
                "The fused summary return type must equal the field's TResult."));
            return new FusionEmission(null, diagnostics.ToImmutable());
        }

        var zip = Unwrap(initializer) as InvocationExpressionSyntax;
        var zipSymbol = zip is null ? null : semanticModel.GetSymbolInfo(zip, cancellationToken).Symbol as IMethodSymbol;
        if (zip is null
            || zipSymbol is null
            || !IsSummaryStaticType(zipSymbol.ContainingType)
            || zipSymbol.Name != "Zip")
        {
            diagnostics.Add(FusionDiagnostics.UnsupportedSummaryAt(
                initializer,
                "Only a direct Summary.Zip(...) initializer is supported."));
            return new FusionEmission(null, diagnostics.ToImmutable());
        }

        var arguments = zip.ArgumentList.Arguments;
        if (arguments.Count is < 2 or > 4)
        {
            diagnostics.Add(FusionDiagnostics.UnsupportedSummaryAt(
                zip,
                "Summary.Zip must have two to four component arguments."));
            return new FusionEmission(null, diagnostics.ToImmutable());
        }

        if (arguments.Any(argument => argument.NameColon is not null))
        {
            diagnostics.Add(FusionDiagnostics.UnsupportedSummaryAt(
                zip,
                "Named Summary.Zip arguments are not supported by the static subset."));
            return new FusionEmission(null, diagnostics.ToImmutable());
        }

        var components = new List<SummaryComponent>();
        for (var index = 0; index < arguments.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var component = TryParseComponent(
                arguments[index].Expression,
                sourceType,
                semanticModel,
                diagnostics,
                cancellationToken);
            if (!component.HasValue)
            {
                return new FusionEmission(null, diagnostics.ToImmutable());
            }

            components.Add(component.Value);
        }

        if (diagnostics.Count > 0)
        {
            return new FusionEmission(null, diagnostics.ToImmutable());
        }

        var source = Emit(method, methodSyntax, components, initializer);
        return new FusionEmission(source, diagnostics.ToImmutable());
    }

    private static SummaryComponent? TryParseComponent(
        ExpressionSyntax expression,
        ITypeSymbol sourceType,
        SemanticModel semanticModel,
        ImmutableArray<Diagnostic>.Builder diagnostics,
        CancellationToken cancellationToken)
    {
        expression = Unwrap(expression);
        if (expression is not InvocationExpressionSyntax invocation)
        {
            diagnostics.Add(FusionDiagnostics.UnsupportedSummaryAt(
                expression,
                "Each Summary.Zip component must be a built-in Count, Sum, Min, or Max factory call."));
            return null;
        }

        var symbol = semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol as IMethodSymbol;
        var factoryType = symbol?.ContainingType;
        if (symbol is null
            || factoryType is null
            || !IsSummarySourceFactoryType(factoryType)
            || factoryType.TypeArguments.Length != 1
            || !FusionTransform.TypesEqual(factoryType.TypeArguments[0], sourceType))
        {
            diagnostics.Add(FusionDiagnostics.UnsupportedSummaryAt(
                invocation,
                "Each component must be a built-in factory on Summary<TSource> for the same TSource."));
            return null;
        }

        var arguments = invocation.ArgumentList.Arguments;
        switch (symbol.Name)
        {
            case "Count" when arguments.Count == 0:
                return new SummaryComponent(SummaryComponentKind.Count, null, null);

            case "Sum" when arguments.Count == 1:
                {
                    if (!TryParseSelector(arguments[0].Expression, semanticModel, invocation, diagnostics, cancellationToken, out var selector))
                    {
                        return null;
                    }

                    var numberType = symbol.TypeArguments.Length == 1 ? symbol.TypeArguments[0] : null;
                    if (numberType is null || !IsSupportedSumType(numberType))
                    {
                        diagnostics.Add(FusionDiagnostics.UnsupportedSummaryAt(
                            invocation,
                            "Sum selectors are supported only for int, long, decimal, double, and float."));
                        return null;
                    }

                    return new SummaryComponent(SummaryComponentKind.Sum, selector, numberType);
                }

            case "Min" when arguments.Count == 1:
            case "Max" when arguments.Count == 1:
                {
                    if (!TryParseSelector(arguments[0].Expression, semanticModel, invocation, diagnostics, cancellationToken, out var selector))
                    {
                        return null;
                    }

                    var keyType = symbol.TypeArguments.Length == 1 ? symbol.TypeArguments[0] : null;
                    if (keyType is null)
                    {
                        diagnostics.Add(FusionDiagnostics.UnsupportedSummaryAt(
                            invocation,
                            "The extrema key type could not be determined."));
                        return null;
                    }

                    return new SummaryComponent(
                        symbol.Name == "Min" ? SummaryComponentKind.Min : SummaryComponentKind.Max,
                        selector,
                        keyType);
                }

            case "Min" when arguments.Count == 2:
            case "Max" when arguments.Count == 2:
                diagnostics.Add(FusionDiagnostics.UnsupportedSummaryAt(
                    invocation,
                    "Custom extrema comparers are not supported; the generated path uses Comparer<TKey>.Default."));
                return null;

            default:
                diagnostics.Add(FusionDiagnostics.UnsupportedSummaryAt(
                    invocation,
                    $"Summary factory '{symbol.Name}' is outside the supported Count/Sum/Min/Max subset."));
                return null;
        }
    }

    private static bool TryParseSelector(
        ExpressionSyntax expression,
        SemanticModel semanticModel,
        SyntaxNode location,
        ImmutableArray<Diagnostic>.Builder diagnostics,
        CancellationToken cancellationToken,
        out SelectorExpression selector)
    {
        selector = default;
        expression = Unwrap(expression);
        if (expression is not LambdaExpressionSyntax lambda)
        {
            diagnostics.Add(FusionDiagnostics.UnsupportedSummaryAt(
                location,
                "The selector must be a static expression lambda; method groups and captured delegates are not statically inlined."));
            return false;
        }

        if (!lambda.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.StaticKeyword)))
        {
            diagnostics.Add(FusionDiagnostics.UnsupportedSummaryAt(
                lambda,
                "The selector must be declared static so the generator does not capture mutable state."));
            return false;
        }

        if (lambda.Body is not ExpressionSyntax body)
        {
            diagnostics.Add(FusionDiagnostics.UnsupportedSummaryAt(
                lambda,
                "The selector must have an expression body; statement bodies are not supported."));
            return false;
        }

        var parameters = lambda switch
        {
            SimpleLambdaExpressionSyntax simple => new[] { simple.Parameter },
            ParenthesizedLambdaExpressionSyntax parenthesized => parenthesized.ParameterList.Parameters.ToArray(),
            _ => Array.Empty<ParameterSyntax>(),
        };

        if (parameters.Length != 1)
        {
            diagnostics.Add(FusionDiagnostics.UnsupportedSummaryAt(
                lambda,
                "The selector must have exactly one parameter."));
            return false;
        }

        var parameterSymbol = semanticModel.GetDeclaredSymbol(parameters[0], cancellationToken) as IParameterSymbol;
        if (parameterSymbol is null
            || parameterSymbol.RefKind != RefKind.None
            || parameterSymbol.Name == "_"
            || parameterSymbol.ScopedKind != ScopedKind.None)
        {
            diagnostics.Add(FusionDiagnostics.UnsupportedSummaryAt(
                parameters[0],
                "The selector parameter must be an ordinary, named, non-scoped by-value parameter."));
            return false;
        }

        selector = new SelectorExpression(parameterSymbol, body);
        return true;
    }

    private static bool IsSupportedSumType(ITypeSymbol type) =>
        type.SpecialType is SpecialType.System_Int32
            or SpecialType.System_Int64
            or SpecialType.System_Decimal
            or SpecialType.System_Double
            or SpecialType.System_Single;

    private static bool IsSummaryFieldType(INamedTypeSymbol type) =>
        type.ContainingNamespace.ToDisplayString() == "FunnySharp"
        && type.MetadataName == "Summary`3";

    private static bool IsSummaryStaticType(INamedTypeSymbol? type) =>
        type is not null
        && type.ContainingNamespace.ToDisplayString() == "FunnySharp"
        && type.MetadataName == "Summary";

    private static bool IsSummarySourceFactoryType(INamedTypeSymbol? type) =>
        type is not null
        && type.ContainingNamespace.ToDisplayString() == "FunnySharp"
        && type.MetadataName == "Summary`1";

    private static ExpressionSyntax Unwrap(ExpressionSyntax expression)
    {
        while (expression is ParenthesizedExpressionSyntax parenthesized)
        {
            expression = parenthesized.Expression;
        }

        return expression;
    }

    private static string Emit(
        IMethodSymbol method,
        MethodDeclarationSyntax methodSyntax,
        List<SummaryComponent> components,
        SyntaxNode sourceNode)
    {
        var writer = new CodeWriter();
        writer.Line("// <auto-generated/>");
        writer.Line("#nullable enable");
        FusionTransform.EmitUsings(writer, sourceNode);
        writer.OpenNamespace(method.ContainingType);
        writer.OpenContainingType(method.ContainingType);
        writer.Line(FusionTransform.BuildSignature(method, methodSyntax));
        writer.OpenBlock();

        var sourceName = FusionTransform.EscapeIdentifier(method.Parameters[0].Name);
        writer.Line($"global::System.ArgumentNullException.ThrowIfNull({sourceName}, \"source\");");

        var selectorParameterNames = new HashSet<string>(
            components
                .Where(static component => component.Selector is not null)
                .Select(static component => component.Selector!.Value.Parameter.Name),
            StringComparer.Ordinal);

        var reservedNames = FusionTransform.CollectIdentifierNames(sourceNode);
        var itemName = "__fusedItem";
        while (selectorParameterNames.Contains(itemName) || reservedNames.Contains(itemName))
        {
            itemName += "_";
        }

        var localNames = new List<string>(components.Count);
        for (var index = 0; index < components.Count; index++)
        {
            var localName = "__fused" + index;
            while (reservedNames.Contains(localName))
            {
                localName += "_";
            }

            localNames.Add(localName);
        }

        var usedFunctionNames = new HashSet<string>(StringComparer.Ordinal)
        {
            method.Parameters[0].Name,
            itemName,
        };
        usedFunctionNames.UnionWith(localNames);

        var selectorFunctionNames = new string?[components.Count];
        for (var index = 0; index < components.Count; index++)
        {
            var component = components[index];
            if (component.Selector is not { } selector)
            {
                continue;
            }

            var functionName = UniqueFunctionName("__FusedSelector" + index, method.ContainingType, usedFunctionNames);
            selectorFunctionNames[index] = functionName;
            var returnType = component.ValueType!.ToDisplayString(FusionTransform.QualifiedFormat);
            var parameterType = selector.Parameter.Type.ToDisplayString(FusionTransform.QualifiedFormat);
            writer.Line(
                $"static {returnType} {functionName}({parameterType} {FusionTransform.EscapeIdentifier(selector.Parameter.Name)}) => {selector.Body};");
        }

        if (components.Any(static component => component.Selector is not null))
        {
            writer.Line();
        }

        for (var index = 0; index < components.Count; index++)
        {
            var component = components[index];
            var localName = localNames[index];
            switch (component.Kind)
            {
                case SummaryComponentKind.Count:
                    writer.Line($"var {localName} = 0;");
                    break;
                case SummaryComponentKind.Sum:
                    writer.Line(
                        $"var {localName} = default({component.ValueType!.ToDisplayString(FusionTransform.QualifiedFormat)});");
                    break;
                default:
                    writer.Line(
                        $"var {localName} = global::FunnySharp.Option<{component.ValueType!.ToDisplayString(FusionTransform.QualifiedFormat)}>.None;");
                    break;
            }
        }

        writer.Line($"foreach (var {itemName} in {sourceName})");
        writer.OpenBlock();

        for (var index = 0; index < components.Count; index++)
        {
            var component = components[index];
            var localName = localNames[index];
            var functionName = selectorFunctionNames[index]!;
            switch (component.Kind)
            {
                case SummaryComponentKind.Count:
                    writer.Line($"{localName} = checked({localName} + 1);");
                    break;

                case SummaryComponentKind.Sum:
                    writer.Line($"{localName} = checked({localName} + {functionName}({itemName}));");
                    break;

                default:
                    {
                        var keyType = component.ValueType!;
                        var keyTypeName = keyType.ToDisplayString(FusionTransform.QualifiedFormat);
                        var keyName = "__fusedKey" + index;
                        var currentName = "__fusedCurrent" + index;
                        writer.Line($"var {keyName} = {functionName}({itemName});");
                        var nullCheck = NullCheck(keyType, keyName);
                        if (nullCheck is not null)
                        {
                            writer.Line($"if ({nullCheck})");
                            writer.OpenBlock();
                        }

                        writer.Line($"if (!{localName}.TryGetValue(out var {currentName}) || global::System.Collections.Generic.Comparer<{keyTypeName}>.Default.Compare({keyName}, {currentName}) {(component.Kind == SummaryComponentKind.Max ? ">" : "<")} 0)");
                        writer.OpenBlock();
                        writer.Line($"{localName} = global::FunnySharp.Option<{keyTypeName}>.Some({keyName});");
                        writer.CloseBlock();

                        if (nullCheck is not null)
                        {
                            writer.CloseBlock();
                        }

                        break;
                    }
            }
        }

        writer.CloseBlock();

        writer.Line($"return ({string.Join(", ", localNames)});");
        writer.CloseBlock();
        writer.CloseType();
        writer.CloseNamespace();
        return writer.ToString();
    }

    private static string UniqueFunctionName(
        string baseName,
        INamedTypeSymbol containingType,
        HashSet<string> usedNames)
    {
        var name = baseName;
        while (usedNames.Contains(name) || containingType.GetMembers(name).Length != 0)
        {
            name += "_";
        }

        usedNames.Add(name);
        return name;
    }

    private static string? NullCheck(ITypeSymbol type, string name)
    {
        if (type.IsReferenceType || type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            return $"{name} is not null";
        }

        return null;
    }

    private readonly record struct SelectorExpression(IParameterSymbol Parameter, ExpressionSyntax Body);

    private enum SummaryComponentKind
    {
        Count,
        Sum,
        Min,
        Max,
    }

    private readonly record struct SummaryComponent(
        SummaryComponentKind Kind,
        SelectorExpression? Selector,
        ITypeSymbol? ValueType);
}
