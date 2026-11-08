using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FunnySharp.FusedExecution.Generator;

internal static class DecoderFusion
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
            || fieldType.ContainingNamespace.ToDisplayString() != "FunnySharp"
            || fieldType.MetadataName != "Decoder`1"
            || fieldType.TypeArguments.Length != 1)
        {
            diagnostics.Add(FusionDiagnostics.InvalidFieldAt(
                methodSyntax,
                "The named field must be a Decoder<T>."));
            return new FusionEmission(null, diagnostics.ToImmutable());
        }

        var resultType = fieldType.TypeArguments[0];
        if (!IsValidationOfDecodeError(method.ReturnType, resultType))
        {
            diagnostics.Add(FusionDiagnostics.InvalidMethodAt(
                methodSyntax,
                "The fused decoding method must return Validation<T, DecodeError> for the field's T."));
            return new FusionEmission(null, diagnostics.ToImmutable());
        }

        var shape = TryParseObject(
            Unwrap(initializer),
            semanticModel,
            diagnostics,
            allowNested: false,
            cancellationToken);
        if (shape is null)
        {
            return new FusionEmission(null, diagnostics.ToImmutable());
        }

        if (!FusionTransform.TypesEqual(shape.ValueType, resultType))
        {
            diagnostics.Add(FusionDiagnostics.InvalidFieldAt(
                methodSyntax,
                "The decoder object's result type does not match the field's Decoder<T>."));
            return new FusionEmission(null, diagnostics.ToImmutable());
        }

        if (diagnostics.Count > 0)
        {
            return new FusionEmission(null, diagnostics.ToImmutable());
        }

        var source = Emit(method, methodSyntax, field, shape, initializer);
        return new FusionEmission(source, diagnostics.ToImmutable());
    }

    private static ObjectShape? TryParseObject(
        ExpressionSyntax expression,
        SemanticModel semanticModel,
        ImmutableArray<Diagnostic>.Builder diagnostics,
        bool allowNested,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (expression is not InvocationExpressionSyntax invocation)
        {
            diagnostics.Add(FusionDiagnostics.UnsupportedDecoderAt(
                expression,
                "Only a direct Decoder.Object(...) field initializer is supported."));
            return null;
        }

        var objectMethod = semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol as IMethodSymbol;
        if (objectMethod is null
            || objectMethod.ContainingNamespace.ToDisplayString() != "FunnySharp"
            || objectMethod.ContainingType.MetadataName != "Decoder"
            || objectMethod.Name != "Object")
        {
            diagnostics.Add(FusionDiagnostics.UnsupportedDecoderAt(
                invocation,
                "Only a direct Decoder.Object(...) initializer is supported."));
            return null;
        }

        var arguments = invocation.ArgumentList.Arguments;
        if (arguments.Count is < 3 or > 5)
        {
            diagnostics.Add(FusionDiagnostics.UnsupportedDecoderAt(
                invocation,
                "Decoder.Object must have two to four field decoders and one combine lambda."));
            return null;
        }

        if (arguments.Any(argument => argument.NameColon is not null))
        {
            diagnostics.Add(FusionDiagnostics.UnsupportedDecoderAt(
                invocation,
                "Named Decoder.Object arguments are not supported by the static subset."));
            return null;
        }

        var combineExpression = Unwrap(arguments[arguments.Count - 1].Expression);
        if (combineExpression is not LambdaExpressionSyntax combine)
        {
            diagnostics.Add(FusionDiagnostics.UnsupportedDecoderAt(
                combineExpression,
                "The Decoder.Object combine argument must be a lambda."));
            return null;
        }

        if (!combine.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.StaticKeyword))
            || combine.Body is not ExpressionSyntax combineBody)
        {
            diagnostics.Add(FusionDiagnostics.UnsupportedDecoderAt(
                combine,
                "The combine lambda must be static and have an expression body."));
            return null;
        }

        if (combineBody is not ObjectCreationExpressionSyntax creation
            || creation.Type is null)
        {
            diagnostics.Add(FusionDiagnostics.UnsupportedDecoderAt(
                combineBody,
                "The combine body must be an explicit object-creation expression such as new T(...)."));
            return null;
        }

        var createdType = semanticModel.GetTypeInfo(creation, cancellationToken).Type;
        var objectResultType = objectMethod.ReturnType is INamedTypeSymbol namedResult
            ? namedResult.TypeArguments[namedResult.TypeArguments.Length - 1]
            : null;
        if (createdType is null
            || objectResultType is null
            || !semanticModel.Compilation.ClassifyConversion(createdType, objectResultType).IsImplicit)
        {
            diagnostics.Add(FusionDiagnostics.UnsupportedDecoderAt(
                creation,
                "The combine object creation must produce a value assignable to the object result type."));
            return null;
        }

        var combineParameters = combine switch
        {
            SimpleLambdaExpressionSyntax simple => new[] { simple.Parameter },
            ParenthesizedLambdaExpressionSyntax parenthesized => parenthesized.ParameterList.Parameters.ToArray(),
            _ => Array.Empty<ParameterSyntax>(),
        };

        var fieldCount = arguments.Count - 1;
        if (combineParameters.Length != fieldCount)
        {
            diagnostics.Add(FusionDiagnostics.UnsupportedDecoderAt(
                combine,
                "The combine lambda parameter count must equal the field decoder count."));
            return null;
        }

        var parameterSymbols = new IParameterSymbol[fieldCount];
        for (var index = 0; index < fieldCount; index++)
        {
            var parameter = semanticModel.GetDeclaredSymbol(combineParameters[index], cancellationToken) as IParameterSymbol;
            if (parameter is null
                || parameter.RefKind != RefKind.None
                || parameter.ScopedKind != ScopedKind.None)
            {
                diagnostics.Add(FusionDiagnostics.UnsupportedDecoderAt(
                    combineParameters[index],
                    "Combine parameters must be ordinary, non-scoped by-value parameters."));
                return null;
            }

            if (parameterSymbols.Take(index).Any(
                existing => string.Equals(existing.Name, parameter.Name, StringComparison.Ordinal)))
            {
                diagnostics.Add(FusionDiagnostics.UnsupportedDecoderAt(
                    combineParameters[index],
                    "Combine parameter names must be distinct."));
                return null;
            }

            parameterSymbols[index] = parameter;
        }

        var fields = new List<FieldShape>(fieldCount);
        for (var index = 0; index < fieldCount; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var fieldShape = TryParseField(
                arguments[index].Expression,
                parameterSymbols[index],
                semanticModel,
                diagnostics,
                allowNested,
                cancellationToken);
            if (fieldShape is null)
            {
                return null;
            }

            fields.Add(fieldShape);
        }

        return new ObjectShape(objectResultType, fields, combineBody);
    }

    private static FieldShape? TryParseField(
        ExpressionSyntax expression,
        IParameterSymbol combineParameter,
        SemanticModel semanticModel,
        ImmutableArray<Diagnostic>.Builder diagnostics,
        bool allowNested,
        CancellationToken cancellationToken)
    {
        expression = Unwrap(expression);
        if (expression is not InvocationExpressionSyntax invocation)
        {
            diagnostics.Add(FusionDiagnostics.UnsupportedDecoderAt(
                expression,
                "Each object field must be a Decoder.Field, Decoder.OptionalField, or Decoder.NullableField call."));
            return null;
        }

        var method = semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol as IMethodSymbol;
        if (method is null
            || method.ContainingNamespace.ToDisplayString() != "FunnySharp"
            || method.ContainingType.MetadataName != "Decoder"
            || method.Name is not ("Field" or "OptionalField" or "NullableField")
            || invocation.ArgumentList.Arguments.Count != 2)
        {
            diagnostics.Add(FusionDiagnostics.UnsupportedDecoderAt(
                invocation,
                "Each object field must be a two-argument Decoder.Field, Decoder.OptionalField, or Decoder.NullableField call."));
            return null;
        }

        if (invocation.ArgumentList.Arguments.Any(argument => argument.NameColon is not null))
        {
            diagnostics.Add(FusionDiagnostics.UnsupportedDecoderAt(
                invocation,
                "Named field-decoder arguments are not supported by the static subset."));
            return null;
        }

        var nameValue = semanticModel.GetConstantValue(invocation.ArgumentList.Arguments[0].Expression, cancellationToken);
        if (!nameValue.HasValue || nameValue.Value is not string { Length: > 0 } name)
        {
            diagnostics.Add(FusionDiagnostics.UnsupportedDecoderAt(
                invocation.ArgumentList.Arguments[0],
                "A field decoder name must be a non-empty constant string."));
            return null;
        }

        var presence = method.Name switch
        {
            "Field" => FieldPresence.Required,
            "OptionalField" => FieldPresence.Optional,
            _ => FieldPresence.Nullable,
        };

        var valueShape = TryParseValue(
            invocation.ArgumentList.Arguments[1].Expression,
            semanticModel,
            diagnostics,
            allowNested,
            cancellationToken);
        if (valueShape is null)
        {
            return null;
        }

        ITypeSymbol? declaredType = valueShape.ValueType;
        if (presence != FieldPresence.Required)
        {
            declaredType = ConstructOption(semanticModel.Compilation, valueShape.ValueType);
        }

        if (declaredType is null
            || !FusionTransform.TypesEqual(declaredType, combineParameter.Type))
        {
            diagnostics.Add(FusionDiagnostics.UnsupportedDecoderAt(
                invocation,
                "The combine parameter type must exactly match the field decoder's result type."));
            return null;
        }

        return new FieldShape(name, presence, valueShape, combineParameter, declaredType);
    }

    private static ValueShape? TryParseValue(
        ExpressionSyntax expression,
        SemanticModel semanticModel,
        ImmutableArray<Diagnostic>.Builder diagnostics,
        bool allowNested,
        CancellationToken cancellationToken)
    {
        expression = Unwrap(expression);
        if (expression is MemberAccessExpressionSyntax or IdentifierNameSyntax)
        {
            var symbol = semanticModel.GetSymbolInfo(expression, cancellationToken).Symbol;
            if (symbol is IPropertySymbol { IsStatic: true } property
                && property.ContainingNamespace.ToDisplayString() == "FunnySharp"
                && property.ContainingType.MetadataName == "Decoder"
                && LeafKind(property.Name) is { } leaf
                && property.Type is INamedTypeSymbol decoderType
                && decoderType.MetadataName == "Decoder`1"
                && decoderType.TypeArguments.Length == 1)
            {
                return new LeafShape(decoderType.TypeArguments[0], leaf);
            }

            diagnostics.Add(FusionDiagnostics.UnsupportedDecoderAt(
                expression,
                "Only built-in scalar Decoder leaves are supported."));
            return null;
        }

        if (expression is not InvocationExpressionSyntax invocation)
        {
            diagnostics.Add(FusionDiagnostics.UnsupportedDecoderAt(
                expression,
                "Only built-in scalar Decoder leaves, List, NonEmptyList, and Object are supported."));
            return null;
        }

        var method = semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol as IMethodSymbol;
        if (method is null || method.ContainingNamespace.ToDisplayString() != "FunnySharp")
        {
            diagnostics.Add(FusionDiagnostics.UnsupportedDecoderAt(
                invocation,
                "The decoder expression is not a recognized FunnySharp decoder factory."));
            return null;
        }

        if (method.ContainingType.MetadataName == "Decoder" && method.Name == "Object")
        {
            if (!allowNested)
            {
                diagnostics.Add(FusionDiagnostics.UnsupportedDecoderAt(
                    invocation,
                    "Nested Decoder.Object fields are outside this prototype: a fallback cannot preserve nested combine invocation counts."));
                return null;
            }

            return TryParseObject(invocation, semanticModel, diagnostics, allowNested: false, cancellationToken);
        }

        if (method.ContainingType.MetadataName == "Decoder"
            && method.Name is "List" or "NonEmptyList"
            && invocation.ArgumentList.Arguments.Count == 1
            && method.TypeArguments.Length == 1)
        {
            var element = TryParseValue(
                invocation.ArgumentList.Arguments[0].Expression,
                semanticModel,
                diagnostics,
                allowNested,
                cancellationToken);
            if (element is null)
            {
                return null;
            }

            if (element is not LeafShape leafElement)
            {
                diagnostics.Add(FusionDiagnostics.UnsupportedDecoderAt(
                    invocation,
                    "Only lists of scalar leaves are supported; lists of objects would need buffered nested construction."));
                return null;
            }

            if (method.TypeArguments[0] is not ITypeSymbol listElementType
                || !FusionTransform.TypesEqual(listElementType, leafElement.ValueType))
            {
                diagnostics.Add(FusionDiagnostics.UnsupportedDecoderAt(
                    invocation,
                    "The list element decoder type does not match the list's declared element type."));
                return null;
            }

            var listType = method.ReturnType is INamedTypeSymbol namedList
                ? namedList.TypeArguments[0]
                : null;
            if (listType is null)
            {
                diagnostics.Add(FusionDiagnostics.UnsupportedDecoderAt(
                    invocation,
                    "The list result type could not be determined."));
                return null;
            }

            return new ListShape(listType, leafElement, method.Name == "NonEmptyList");
        }

        diagnostics.Add(FusionDiagnostics.UnsupportedDecoderAt(
            invocation,
            $"Decoder factory '{method.Name}' is outside the supported Object/Field/List/NonEmptyList/scalar subset."));
        return null;
    }

    private static string? LeafKind(string name) =>
        name switch
        {
            "String" or "NonEmptyString" or "Boolean" or "Int32" or "Int64" or "Decimal"
                or "Double" or "Guid" or "DateTime" or "DateTimeOffset" or "Element" => name,
            _ => null,
        };

    private static INamedTypeSymbol? ConstructOption(
        Compilation compilation,
        ITypeSymbol valueType)
    {
        var option = compilation.GetTypeByMetadataName("FunnySharp.Option`1");
        return option?.Construct(valueType);
    }

    private static bool IsValidationOfDecodeError(ITypeSymbol type, ITypeSymbol resultType) =>
        type is INamedTypeSymbol named
        && named.ContainingNamespace.ToDisplayString() == "FunnySharp"
        && named.MetadataName == "Validation`2"
        && named.TypeArguments.Length == 2
        && FusionTransform.TypesEqual(named.TypeArguments[0], resultType)
        && named.TypeArguments[1].ToDisplayString() == "FunnySharp.DecodeError"
        && named.TypeArguments[1].NullableAnnotation != NullableAnnotation.Annotated;

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
        IFieldSymbol field,
        ObjectShape shape,
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

        var inputName = FusionTransform.EscapeIdentifier(method.Parameters[0].Name);
        var fieldName = FusionTransform.EscapeIdentifier(field.Name);
        var fallback = $"return {fieldName}.Decode({inputName});";
        var usedNames = FusionTransform.CollectIdentifierNames(shape.CombineBody);
        usedNames.Add(method.Parameters[0].Name);
        var propertyHelperName = UniqueName("__FusedTryGetFirstProperty", usedNames);

        writer.Line($"if ({inputName}.ValueKind != global::System.Text.Json.JsonValueKind.Object)");
        writer.OpenBlock();
        writer.Line(fallback);
        writer.CloseBlock();
        writer.Line();

        var storageNames = new List<string>(shape.Fields.Count);
        for (var index = 0; index < shape.Fields.Count; index++)
        {
            var storageName = EmitField(
                writer,
                shape.Fields[index],
                inputName,
                fallback,
                index,
                propertyHelperName,
                usedNames);
            storageNames.Add(storageName);
            writer.Line();
        }

        var resultTypeName = shape.ValueType.ToDisplayString(FusionTransform.QualifiedFormat);
        var combineName = UniqueFunctionName("__FusedCombine", method.ContainingType, usedNames);
        var combineParameters = string.Join(
            ", ",
            shape.Fields.Select(
                static item =>
                    $"{item.CombineParameter.Type.ToDisplayString(FusionTransform.QualifiedFormat)} {FusionTransform.EscapeIdentifier(item.CombineParameter.Name)}"));

        writer.Line(
            $"return global::FunnySharp.Validation<{resultTypeName}, global::FunnySharp.DecodeError>.Valid({combineName}({string.Join(", ", storageNames)}));");
        writer.Line();

        writer.Line($"static {resultTypeName} {combineName}({combineParameters}) => {shape.CombineBody};");
        writer.Line();

        writer.Line($"static bool {propertyHelperName}(global::System.Text.Json.JsonElement element, string name, out global::System.Text.Json.JsonElement value)");
        writer.OpenBlock();
        writer.Line("foreach (var property in element.EnumerateObject())");
        writer.OpenBlock();
        writer.Line("if (property.NameEquals(name))");
        writer.OpenBlock();
        writer.Line("value = property.Value;");
        writer.Line("return true;");
        writer.CloseBlock();
        writer.CloseBlock();
        writer.Line();
        writer.Line("value = default;");
        writer.Line("return false;");
        writer.CloseBlock();

        writer.CloseBlock();
        writer.CloseType();
        writer.CloseNamespace();
        return writer.ToString();
    }

    private static string EmitField(
        CodeWriter writer,
        FieldShape field,
        string inputName,
        string fallback,
        int index,
        string propertyHelperName,
        HashSet<string> usedNames)
    {
        var storageName = UniqueName("__fusedField" + index, usedNames);
        var storageType = field.DeclaredType.ToDisplayString(FusionTransform.QualifiedFormat);
        var elementName = UniqueName("__fusedElement" + index, usedNames);
        var lookup = $"{propertyHelperName}({inputName}, {FusionTransform.StringLiteral(field.Name)}, out var {elementName})";

        if (field.Presence == FieldPresence.Required)
        {
            writer.Line($"if (!{lookup})");
            writer.OpenBlock();
            writer.Line(fallback);
            writer.CloseBlock();

            if (field.Value is LeafShape leaf)
            {
                EmitLeafParse(writer, leaf, elementName, storageName, fallback);
                return storageName;
            }

            var list = (ListShape)field.Value;
            writer.Line($"{storageType} {storageName};");
            EmitListParse(writer, list, elementName, storageName, fallback, index, usedNames);
            return storageName;
        }

        var valueTypeName = field.Value.ValueType.ToDisplayString(FusionTransform.QualifiedFormat);
        writer.Line($"{storageType} {storageName} = global::FunnySharp.Option<{valueTypeName}>.None;");
        writer.Line($"if ({lookup})");
        writer.OpenBlock();
        if (field.Presence == FieldPresence.Nullable)
        {
            writer.Line($"if ({elementName}.ValueKind != global::System.Text.Json.JsonValueKind.Null)");
            writer.OpenBlock();
        }

        if (field.Value is LeafShape optionalLeaf)
        {
            var valueName = UniqueName("__fusedValue" + index, usedNames);
            EmitLeafParse(writer, optionalLeaf, elementName, valueName, fallback);
            writer.Line($"{storageName} = global::FunnySharp.Option<{valueTypeName}>.Some({valueName});");
        }
        else
        {
            var list = (ListShape)field.Value;
            var listName = UniqueName("__fusedList" + index, usedNames);
            writer.Line($"{valueTypeName} {listName};");
            EmitListParse(writer, list, elementName, listName, fallback, index, usedNames);
            writer.Line($"{storageName} = global::FunnySharp.Option<{valueTypeName}>.Some({listName});");
        }

        if (field.Presence == FieldPresence.Nullable)
        {
            writer.CloseBlock();
        }

        writer.CloseBlock();
        return storageName;
    }

    private static void EmitListParse(
        CodeWriter writer,
        ListShape list,
        string elementName,
        string targetName,
        string fallback,
        int index,
        HashSet<string> usedNames)
    {
        var elementType = list.Element.ValueType.ToDisplayString(FusionTransform.QualifiedFormat);
        var itemName = UniqueName("__fusedArrayItem" + index, usedNames);
        var bufferName = UniqueName("__fusedBuffer" + index, usedNames);
        var valueName = UniqueName("__fusedArrayValue" + index, usedNames);

        writer.Line($"if ({elementName}.ValueKind != global::System.Text.Json.JsonValueKind.Array)");
        writer.OpenBlock();
        writer.Line(fallback);
        writer.CloseBlock();
        if (list.NonEmpty)
        {
            writer.Line($"if ({elementName}.GetArrayLength() == 0)");
            writer.OpenBlock();
            writer.Line(fallback);
            writer.CloseBlock();
            EmitListBuffer(writer, list, elementName, targetName, fallback, bufferName, itemName, valueName);
        }
        else
        {
            writer.Line($"if ({elementName}.GetArrayLength() == 0)");
            writer.OpenBlock();
            writer.Line($"{targetName} = global::System.Array.Empty<{elementType}>();");
            writer.CloseBlock();
            writer.Line("else");
            writer.OpenBlock();
            EmitListBuffer(writer, list, elementName, targetName, fallback, bufferName, itemName, valueName);
            writer.CloseBlock();
        }
    }

    private static void EmitListBuffer(
        CodeWriter writer,
        ListShape list,
        string elementName,
        string targetName,
        string fallback,
        string bufferName,
        string itemName,
        string valueName)
    {
        var elementType = list.Element.ValueType.ToDisplayString(FusionTransform.QualifiedFormat);
        writer.Line($"var {bufferName} = new global::System.Collections.Generic.List<{elementType}>({elementName}.GetArrayLength());");
        writer.Line($"foreach (var {itemName} in {elementName}.EnumerateArray())");
        writer.OpenBlock();
        EmitLeafParse(writer, list.Element, itemName, valueName, fallback);
        writer.Line($"{bufferName}.Add({valueName});");
        writer.CloseBlock();
        writer.Line($"{targetName} = {bufferName}.AsReadOnly();");
    }

    private static void EmitLeafParse(
        CodeWriter writer,
        LeafShape leaf,
        string elementName,
        string valueName,
        string fallback)
    {
        switch (leaf.Kind)
        {
            case "String":
                writer.Line($"if ({elementName}.ValueKind != global::System.Text.Json.JsonValueKind.String)");
                writer.OpenBlock();
                writer.Line(fallback);
                writer.CloseBlock();
                writer.Line($"var {valueName} = {elementName}.GetString()!;");
                break;

            case "NonEmptyString":
                writer.Line($"if ({elementName}.ValueKind != global::System.Text.Json.JsonValueKind.String)");
                writer.OpenBlock();
                writer.Line(fallback);
                writer.CloseBlock();
                writer.Line($"var {valueName} = {elementName}.GetString()!;");
                writer.Line($"if ({valueName}.Length == 0)");
                writer.OpenBlock();
                writer.Line(fallback);
                writer.CloseBlock();
                break;

            case "Boolean":
                writer.Line($"if ({elementName}.ValueKind is not (global::System.Text.Json.JsonValueKind.True or global::System.Text.Json.JsonValueKind.False))");
                writer.OpenBlock();
                writer.Line(fallback);
                writer.CloseBlock();
                writer.Line($"var {valueName} = {elementName}.GetBoolean();");
                break;

            case "Int32":
                EmitNumberParse(writer, elementName, valueName, "TryGetInt32", fallback, finite: null);
                break;

            case "Int64":
                EmitNumberParse(writer, elementName, valueName, "TryGetInt64", fallback, finite: null);
                break;

            case "Decimal":
                EmitNumberParse(writer, elementName, valueName, "TryGetDecimal", fallback, finite: null);
                break;

            case "Double":
                EmitNumberParse(writer, elementName, valueName, "TryGetDouble", fallback, finite: "global::System.Double.IsFinite");
                break;

            case "Guid":
                writer.Line($"if ({elementName}.ValueKind != global::System.Text.Json.JsonValueKind.String || !{elementName}.TryGetGuid(out var {valueName}))");
                writer.OpenBlock();
                writer.Line(fallback);
                writer.CloseBlock();
                break;

            case "DateTime":
                writer.Line($"if ({elementName}.ValueKind != global::System.Text.Json.JsonValueKind.String || !{elementName}.TryGetDateTime(out var {valueName}))");
                writer.OpenBlock();
                writer.Line(fallback);
                writer.CloseBlock();
                break;

            case "DateTimeOffset":
                writer.Line($"if ({elementName}.ValueKind != global::System.Text.Json.JsonValueKind.String || !{elementName}.TryGetDateTimeOffset(out var {valueName}))");
                writer.OpenBlock();
                writer.Line(fallback);
                writer.CloseBlock();
                break;

            case "Element":
                writer.Line($"if ({elementName}.ValueKind == global::System.Text.Json.JsonValueKind.Undefined)");
                writer.OpenBlock();
                writer.Line(fallback);
                writer.CloseBlock();
                writer.Line($"var {valueName} = {elementName};");
                break;

            default:
                throw new InvalidOperationException($"Unknown leaf kind '{leaf.Kind}'.");
        }
    }

    private static void EmitNumberParse(
        CodeWriter writer,
        string elementName,
        string valueName,
        string tryMethod,
        string fallback,
        string? finite)
    {
        if (finite is null)
        {
            writer.Line($"if ({elementName}.ValueKind != global::System.Text.Json.JsonValueKind.Number || !{elementName}.{tryMethod}(out var {valueName}))");
        }
        else
        {
            writer.Line($"if ({elementName}.ValueKind != global::System.Text.Json.JsonValueKind.Number || !{elementName}.{tryMethod}(out var {valueName}) || !{finite}({valueName}))");
        }

        writer.OpenBlock();
        writer.Line(fallback);
        writer.CloseBlock();
    }

    private static string UniqueName(string baseName, HashSet<string> usedNames)
    {
        var name = baseName;
        while (usedNames.Contains(name))
        {
            name += "_";
        }

        usedNames.Add(name);
        return name;
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

    private abstract class ValueShape
    {
        protected ValueShape(ITypeSymbol valueType) => ValueType = valueType;

        internal ITypeSymbol ValueType { get; }
    }

    private sealed class LeafShape : ValueShape
    {
        internal LeafShape(ITypeSymbol valueType, string kind)
            : base(valueType) => Kind = kind;

        internal string Kind { get; }
    }

    private sealed class ListShape : ValueShape
    {
        internal ListShape(ITypeSymbol valueType, LeafShape element, bool nonEmpty)
            : base(valueType)
        {
            Element = element;
            NonEmpty = nonEmpty;
        }

        internal LeafShape Element { get; }

        internal bool NonEmpty { get; }
    }

    private sealed class ObjectShape : ValueShape
    {
        internal ObjectShape(
            ITypeSymbol valueType,
            List<FieldShape> fields,
            ExpressionSyntax combineBody)
            : base(valueType)
        {
            Fields = fields;
            CombineBody = combineBody;
        }

        internal List<FieldShape> Fields { get; }

        internal ExpressionSyntax CombineBody { get; }
    }

    private sealed class FieldShape
    {
        internal FieldShape(
            string name,
            FieldPresence presence,
            ValueShape value,
            IParameterSymbol combineParameter,
            ITypeSymbol declaredType)
        {
            Name = name;
            Presence = presence;
            Value = value;
            CombineParameter = combineParameter;
            DeclaredType = declaredType;
        }

        internal string Name { get; }

        internal FieldPresence Presence { get; }

        internal ValueShape Value { get; }

        internal IParameterSymbol CombineParameter { get; }

        internal ITypeSymbol DeclaredType { get; }
    }

    private enum FieldPresence
    {
        Required,
        Optional,
        Nullable,
    }
}
