using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FunnySharp.FusedExecution.Generator;

internal static class FusionTransform
{
    internal static readonly SymbolDisplayFormat QualifiedFormat =
        SymbolDisplayFormat.FullyQualifiedFormat.AddMiscellaneousOptions(
            SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    internal static bool TypesEqual(ITypeSymbol left, ITypeSymbol right) =>
        left.IsTupleType && right.IsTupleType
        && left is INamedTypeSymbol leftTuple && right is INamedTypeSymbol rightTuple
            ? SymbolEqualityComparer.IncludeNullability.Equals(
                leftTuple.TupleUnderlyingType,
                rightTuple.TupleUnderlyingType)
            : SymbolEqualityComparer.IncludeNullability.Equals(left, right);

    internal static FusionRequest? Transform(
        GeneratorAttributeSyntaxContext context,
        FusionKind kind,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (context.TargetSymbol is not IMethodSymbol method)
        {
            return new FusionRequest(
                FallbackHintName(context.TargetSymbol, context.TargetNode),
                null,
                ImmutableArray.Create(FusionDiagnostics.InvalidMethodAt(
                    context.TargetNode,
                    "Fused-execution attributes must be placed on an ordinary method declaration.")));
        }

        if (context.TargetNode is not MethodDeclarationSyntax methodSyntax)
        {
            return new FusionRequest(
                FallbackHintName(method, context.TargetNode),
                null,
                ImmutableArray.Create(FusionDiagnostics.InvalidMethodAt(
                    context.TargetNode,
                    "Fused-execution attributes must be placed on an ordinary method declaration; local functions, accessors, constructors, and operators are not supported.")));
        }

        if (context.Attributes.IsDefaultOrEmpty)
        {
            return null;
        }

        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
        var methodProblem = ValidateMethod(method, methodSyntax, kind);
        if (methodProblem is not null)
        {
            diagnostics.Add(FusionDiagnostics.InvalidMethodAt(methodSyntax, methodProblem));
            return new FusionRequest(HintName(method), null, diagnostics.ToImmutable());
        }

        var fieldName = ReadAttributeFieldName(context.Attributes[0]);
        if (fieldName is null)
        {
            diagnostics.Add(FusionDiagnostics.InvalidMethodAt(
                methodSyntax,
                "The fused attribute requires one non-empty field-name argument, normally nameof(Field)."));
            return new FusionRequest(
                HintName(method),
                BuildThrowingMethod(method, methodSyntax, "FS3903"),
                diagnostics.ToImmutable());
        }

        var containingType = method.ContainingType;
        var field = containingType.GetMembers(fieldName).OfType<IFieldSymbol>().FirstOrDefault();
        if (field is null)
        {
            diagnostics.Add(FusionDiagnostics.InvalidFieldAt(
                methodSyntax,
                $"No field named '{fieldName}' exists on '{containingType.Name}'."));
            return new FusionRequest(
                HintName(method),
                BuildThrowingMethod(method, methodSyntax, "FS3904"),
                diagnostics.ToImmutable());
        }

        if (!field.IsStatic || !field.IsReadOnly)
        {
            diagnostics.Add(FusionDiagnostics.InvalidFieldAt(
                methodSyntax,
                $"Field '{fieldName}' must be static readonly so the generated shape and the runtime path cannot diverge."));
            return new FusionRequest(
                HintName(method),
                BuildThrowingMethod(method, methodSyntax, "FS3904"),
                diagnostics.ToImmutable());
        }

        if (GetInitializer(field) is not { } initializer)
        {
            diagnostics.Add(FusionDiagnostics.InvalidFieldAt(
                methodSyntax,
                $"Field '{fieldName}' must have an inline initializer; helper-built descriptions are not statically analyzable."));
            return new FusionRequest(
                HintName(method),
                BuildThrowingMethod(method, methodSyntax, "FS3904"),
                diagnostics.ToImmutable());
        }

        var semanticModel = context.SemanticModel.Compilation.GetSemanticModel(initializer.SyntaxTree);
        var emission = kind == FusionKind.Summary
            ? SummaryFusion.TryBuild(method, methodSyntax, field, initializer, semanticModel, cancellationToken)
            : DecoderFusion.TryBuild(method, methodSyntax, field, initializer, semanticModel, cancellationToken);

        diagnostics.AddRange(emission.Diagnostics);
        var source = emission.Source;
        if (source is null)
        {
            source = BuildThrowingMethod(
                method,
                methodSyntax,
                kind == FusionKind.Summary ? "FS3901" : "FS3902");
        }

        return new FusionRequest(HintName(method), source, diagnostics.ToImmutable());
    }

    private static string? ValidateMethod(
        IMethodSymbol method,
        MethodDeclarationSyntax syntax,
        FusionKind kind)
    {
        if (!method.IsStatic)
        {
            return "A fused-execution method must be static.";
        }

        if (method.IsExtensionMethod || method.Parameters.Length > 0 && method.Parameters[0].IsThis)
        {
            return "Extension methods and 'this' parameters are not supported.";
        }

        if (!method.IsPartialDefinition)
        {
            return "A fused-execution method must be declared with the partial modifier and no body.";
        }

        if (method.MethodKind != MethodKind.Ordinary || method.TypeParameters.Length != 0)
        {
            return "Generic or non-ordinary methods are not supported.";
        }

        if (method.Parameters.Length != 1)
        {
            return "A fused-execution method must have exactly one parameter.";
        }

        if (method.ReturnsVoid || method.ReturnsByRef || method.ReturnsByRefReadonly)
        {
            return "A fused-execution method must return the calculation result by value.";
        }

        var parameter = method.Parameters[0];
        if (parameter.RefKind != RefKind.None
            || parameter.IsParams
            || parameter.IsOptional
            || parameter.IsThis
            || parameter.ScopedKind != ScopedKind.None)
        {
            return "The fused-execution parameter must be an ordinary by-value, non-scoped parameter.";
        }

        var containingType = method.ContainingType;
        if (containingType is null
            || containingType.TypeKind != TypeKind.Class
            || containingType.Arity != 0
            || containingType.ContainingType is not null)
        {
            return "The containing type must be a non-generic, non-nested class.";
        }

        if (IsFileLocal(containingType))
        {
            return "The containing type must not be file-local; the generated part cannot see a file-local type.";
        }

        if (!IsPartial(containingType))
        {
            return "The containing type must be declared partial, because the generator adds another part.";
        }

        if (!HasModifier(syntax, SyntaxKind.StaticKeyword))
        {
            return "A fused-execution method must be declared static.";
        }

        foreach (var modifier in syntax.Modifiers)
        {
            if (!modifier.IsKind(SyntaxKind.PublicKeyword)
                && !modifier.IsKind(SyntaxKind.InternalKeyword)
                && !modifier.IsKind(SyntaxKind.PrivateKeyword)
                && !modifier.IsKind(SyntaxKind.ProtectedKeyword)
                && !modifier.IsKind(SyntaxKind.StaticKeyword)
                && !modifier.IsKind(SyntaxKind.PartialKeyword))
            {
                return $"Modifier '{modifier.Text}' is outside the supported fused-execution subset.";
            }
        }

        if (kind == FusionKind.Summary)
        {
            if (!IsEnumerableOfT(parameter.Type))
            {
                return "A fused summary method parameter must be IEnumerable<TSource>.";
            }
        }
        else
        {
            if (!IsJsonElement(parameter.Type))
            {
                return "A fused decoding method parameter must be System.Text.Json.JsonElement.";
            }
        }

        return null;
    }

    private static bool IsPartial(INamedTypeSymbol type) =>
        type.DeclaringSyntaxReferences
            .Select(reference => reference.GetSyntax())
            .OfType<TypeDeclarationSyntax>()
            .Any(declaration => HasModifier(declaration, SyntaxKind.PartialKeyword));

    private static bool IsFileLocal(INamedTypeSymbol type) =>
        type.DeclaringSyntaxReferences
            .Select(reference => reference.GetSyntax())
            .OfType<TypeDeclarationSyntax>()
            .Any(declaration => HasModifier(declaration, SyntaxKind.FileKeyword));

    private static bool HasModifier(SyntaxNode node, SyntaxKind kind) =>
        node switch
        {
            MemberDeclarationSyntax member => member.Modifiers.Any(modifier => modifier.IsKind(kind)),
            _ => false,
        };

    private static string? ReadAttributeFieldName(AttributeData attribute)
    {
        if (attribute.ConstructorArguments.Length != 1)
        {
            return null;
        }

        var value = attribute.ConstructorArguments[0].Value as string;
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static ExpressionSyntax? GetInitializer(IFieldSymbol field)
    {
        foreach (var reference in field.DeclaringSyntaxReferences)
        {
            if (reference.GetSyntax() is VariableDeclaratorSyntax declarator
                && declarator.Initializer is not null)
            {
                return declarator.Initializer.Value;
            }
        }

        return null;
    }

    private static bool IsEnumerableOfT(ITypeSymbol type) =>
        type is INamedTypeSymbol named
        && named.OriginalDefinition.ToDisplayString() == "System.Collections.Generic.IEnumerable<T>";

    private static bool IsJsonElement(ITypeSymbol type) =>
        type.ToDisplayString() == "System.Text.Json.JsonElement";

    private static string HintName(IMethodSymbol method)
    {
        var name = method.ContainingType.ToDisplayString()
            + "."
            + method.Name
            + "."
            + method.Parameters[0].Type.ToDisplayString();
        return Sanitize(name) + ".Fused.g.cs";
    }

    private static string FallbackHintName(ISymbol? symbol, SyntaxNode node)
    {
        var name = (symbol?.ToDisplayString() ?? symbol?.Kind.ToString() ?? "Unknown")
            + "."
            + node.Kind()
            + "."
            + node.SpanStart;
        return Sanitize(name) + ".Fused.g.cs";
    }

    private static string Sanitize(string name)
    {
        var builder = new StringBuilder(name.Length + 20);
        foreach (var character in name)
        {
            builder.Append(char.IsLetterOrDigit(character) || character == '_' || character == '.'
                ? character
                : '_');
        }

        return builder.ToString();
    }

    private static string BuildThrowingMethod(
        IMethodSymbol method,
        MethodDeclarationSyntax syntax,
        string diagnosticId)
    {
        var writer = new CodeWriter();
        writer.Line("// <auto-generated/>");
        writer.Line("#nullable enable");
        EmitUsings(writer, syntax);
        writer.OpenNamespace(method.ContainingType);
        writer.OpenContainingType(method.ContainingType);
        writer.Line(BuildSignature(method, syntax));
        writer.OpenBlock();
        writer.Line(
            $"throw new global::System.NotSupportedException(\"FunnySharp fused execution rejected this declaration; see diagnostic {diagnosticId}.\");");
        writer.CloseBlock();
        writer.CloseType();
        writer.CloseNamespace();
        return writer.ToString();
    }

    internal static string BuildSignature(IMethodSymbol method, MethodDeclarationSyntax syntax)
    {
        var modifiers = string.Join(
            " ",
            syntax.Modifiers
                .Where(modifier => modifier.IsKind(SyntaxKind.PublicKeyword)
                    || modifier.IsKind(SyntaxKind.InternalKeyword)
                    || modifier.IsKind(SyntaxKind.PrivateKeyword)
                    || modifier.IsKind(SyntaxKind.ProtectedKeyword))
                .Select(modifier => modifier.Text));

        var prefix = string.IsNullOrEmpty(modifiers) ? string.Empty : modifiers + " ";
        var parameter = method.Parameters[0];
        var parameterType = parameter.Type.ToDisplayString(QualifiedFormat);
        var returnType = method.ReturnType.ToDisplayString(QualifiedFormat);
        var methodName = EscapeIdentifier(method.Name);
        var parameterName = EscapeIdentifier(parameter.Name);
        return $"{prefix}static partial {returnType} {methodName}({parameterType} {parameterName})";
    }

    internal static string EscapeIdentifier(string name) =>
        SyntaxFacts.GetKeywordKind(name) != SyntaxKind.None ? "@" + name : name;

    internal static string StringLiteral(string value) =>
        SymbolDisplay.FormatLiteral(value, quote: true);

    internal static HashSet<string> CollectIdentifierNames(SyntaxNode node)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var name in node.DescendantNodesAndSelf().OfType<SimpleNameSyntax>())
        {
            names.Add(name.Identifier.ValueText);
        }

        return names;
    }

    internal static void EmitUsings(CodeWriter writer, SyntaxNode sourceNode)
    {
        var directives = new List<UsingDirectiveSyntax>();
        for (var current = sourceNode.Parent; current is not null; current = current.Parent)
        {
            switch (current)
            {
                case BaseNamespaceDeclarationSyntax namespaceDeclaration:
                    directives.InsertRange(0, namespaceDeclaration.Usings);
                    break;
                case CompilationUnitSyntax unit:
                    directives.InsertRange(0, unit.Usings);
                    break;
            }
        }

        if (directives.Count == 0)
        {
            return;
        }

        foreach (var usingDirective in directives)
        {
            writer.Line(usingDirective.ToString());
        }

        writer.Line();
    }
}

internal sealed class FusionEmission
{
    internal FusionEmission(string? source, ImmutableArray<Diagnostic> diagnostics)
    {
        Source = source;
        Diagnostics = diagnostics;
    }

    internal string? Source { get; }

    internal ImmutableArray<Diagnostic> Diagnostics { get; }
}

internal sealed class CodeWriter
{
    private readonly StringBuilder builder = new();
    private int indent;
    private bool namespaceOpened;

    internal void Line() => builder.Append('\n');

    internal void Line(string text)
    {
        if (text.Length > 0)
        {
            builder.Append(' ', indent * 4);
            builder.Append(text);
        }

        builder.Append('\n');
    }

    internal void OpenBlock()
    {
        Line("{");
        indent++;
    }

    internal void CloseBlock()
    {
        indent--;
        Line("}");
    }

    internal void OpenNamespace(INamedTypeSymbol type)
    {
        var namespaceSymbol = type.ContainingNamespace;
        if (namespaceSymbol is null || namespaceSymbol.IsGlobalNamespace)
        {
            return;
        }

        var segments = new List<string>();
        for (var current = namespaceSymbol; current is not null && !current.IsGlobalNamespace; current = current.ContainingNamespace)
        {
            segments.Insert(0, FusionTransform.EscapeIdentifier(current.Name));
        }

        Line($"namespace {string.Join(".", segments)}");
        OpenBlock();
        namespaceOpened = true;
    }

    internal void CloseNamespace()
    {
        if (namespaceOpened)
        {
            CloseBlock();
            namespaceOpened = false;
        }
    }

    internal void OpenContainingType(INamedTypeSymbol type)
    {
        var keyword = type.IsRecord
            ? (type.TypeKind == TypeKind.Struct ? "partial record struct" : "partial record class")
            : (type.TypeKind == TypeKind.Struct ? "partial struct" : "partial class");
        Line($"{keyword} {FusionTransform.EscapeIdentifier(type.Name)}");
        OpenBlock();
    }

    internal void CloseType() => CloseBlock();

    public override string ToString() => builder.ToString();
}
