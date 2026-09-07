using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Nice3point.Revit.Toolkit.Analyzers.ExternalEvents;
using Nice3point.Revit.Toolkit.SourceGenerators.CSharp;
using Nice3point.Revit.Toolkit.SourceGenerators.Diagnostics;
using Nice3point.Revit.Toolkit.SourceGenerators.Models;

namespace Nice3point.Revit.Toolkit.SourceGenerators.ExternalEvents.Analysis;

internal static class ExternalEventExtractor
{
    private static readonly SymbolDisplayFormat NullableFlowFormat = new(
        SymbolDisplayGlobalNamespaceStyle.Omitted,
        SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.None);

    public static ExternalEventAnalysis AnalyzeMethod(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (context.TargetSymbol is not IMethodSymbol methodSymbol)
        {
            return new ExternalEventAnalysis();
        }

        methodSymbol = methodSymbol.PartialDefinitionPart ?? methodSymbol;

        var diagnostics = ImmutableArray.CreateBuilder<DiagnosticInfo>();

        if (!ValidateMethod(methodSymbol, context.Attributes[0].AttributeClass!, diagnostics, cancellationToken))
        {
            return new ExternalEventAnalysis(diagnostics.ToImmutable());
        }

        var eventDefinition = CreateEventDefinition(methodSymbol, context.Attributes[0], cancellationToken);

        return new ExternalEventAnalysis(eventDefinition, diagnostics.ToImmutable());
    }

    private static bool ValidateMethod(IMethodSymbol methodSymbol, INamedTypeSymbol attributeType, ImmutableArray<DiagnosticInfo>.Builder diagnostics, CancellationToken cancellationToken)
    {
        if (!IsAllContainingTypesPartial(methodSymbol, cancellationToken))
        {
            return false;
        }

        if (methodSymbol.IsGenericMethod)
        {
            diagnostics.Add(DiagnosticInfo.Create(ExternalEventDiagnostics.GenericMethod, methodSymbol, methodSymbol.Name));
            return false;
        }

        if (IsTaskType(methodSymbol.ReturnType))
        {
            diagnostics.Add(DiagnosticInfo.Create(ExternalEventDiagnostics.TaskReturnNotSupported, methodSymbol, methodSymbol.Name));
            return false;
        }

        if (HasDuplicateOverloads(methodSymbol, attributeType, cancellationToken))
        {
            diagnostics.Add(DiagnosticInfo.Create(ExternalEventDiagnostics.DuplicateMethodOverload, methodSymbol, methodSymbol.Name));
            return false;
        }

        var unsupportedReason = ExternalEventSignature.GetUnsupportedReason(methodSymbol, cancellationToken);
        if (unsupportedReason is not null)
        {
            diagnostics.Add(DiagnosticInfo.Create(ExternalEventDiagnostics.UnsupportedSignature, methodSymbol, methodSymbol.Name, unsupportedReason));
            return false;
        }

        var conflictingName = FindGeneratedMemberConflict(methodSymbol, attributeType, cancellationToken);
        if (conflictingName is not null)
        {
            diagnostics.Add(DiagnosticInfo.Create(ExternalEventDiagnostics.GeneratedMemberConflict, methodSymbol, methodSymbol.Name, conflictingName));
            return false;
        }

        return true;
    }

    private static string? FindGeneratedMemberConflict(IMethodSymbol method, INamedTypeSymbol attributeType, CancellationToken cancellationToken)
    {
        var generatedNames = GetGeneratedMemberNames(method);
        foreach (var generatedName in generatedNames)
        {
            if (method.ContainingType.Name == generatedName ||
                method.ContainingType.TypeParameters.Any(parameter => parameter.Name == generatedName) ||
                !method.ContainingType.GetMembers(generatedName).IsEmpty)
            {
                return generatedName;
            }
        }

        foreach (var sibling in method.ContainingType.GetMembers().OfType<IMethodSymbol>())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (SymbolEqualityComparer.Default.Equals(sibling, method) ||
                !sibling.GetAttributes().Any(attribute => SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attributeType)))
            {
                continue;
            }

            foreach (var siblingName in GetGeneratedMemberNames(sibling))
            {
                if (generatedNames.Contains(siblingName))
                {
                    return siblingName;
                }
            }
        }

        return ExternalEventExtensionContainer.Resolve(method, cancellationToken).ConflictingName;
    }

    private static string[] GetGeneratedMemberNames(IMethodSymbol method)
    {
        var hasUiApplicationParameter = method.Parameters.Length > 0 &&
                                        method.Parameters[0].Type.ToDisplayString(NullableFlowFormat) == ExternalEventTypeNames.UiApplication.WithoutGlobalPrefix;
        var generatedMemberNames = new List<string> { $"{method.Name}AsyncEvent" };
        if (method.ReturnsVoid)
        {
            generatedMemberNames.Add($"{method.Name}Event");
        }

        if (method.Parameters.Length - (hasUiApplicationParameter ? 1 : 0) > 1)
        {
            generatedMemberNames.Add($"{method.Name}Args");
        }

        return [.. generatedMemberNames];
    }

    private static ExternalEventDefinition CreateEventDefinition(IMethodSymbol methodSymbol, AttributeData attribute, CancellationToken cancellationToken)
    {
        var allowDirectInvocation = attribute.GetNamedArgument("AllowDirectInvocation", false);

        var (hasUiApplicationParameter, extraParameters) = ClassifyParameters(methodSymbol);
        var (returnsVoid, returnType) = ExtractReturnType(methodSymbol);
        var delegateType = BuildDelegateType(methodSymbol, hasUiApplicationParameter, returnsVoid, returnType, extraParameters);

        var typeHierarchy = GetTypeHierarchy(methodSymbol, cancellationToken);
        var containingNamespace = GetNamespace(methodSymbol.ContainingType);

        return new ExternalEventDefinition(
            GetHintName(methodSymbol),
            containingNamespace,
            methodSymbol.Name,
            methodSymbol.IsStatic,
            returnsVoid,
            returnType,
            delegateType,
            hasUiApplicationParameter,
            allowDirectInvocation,
            typeHierarchy,
            extraParameters,
            new ExternalEventExtensionSignature().Create(methodSymbol, hasUiApplicationParameter, cancellationToken),
            methodSymbol.ContainingType.GetMembers().Select(static member => member.Name).ToImmutableArray());
    }

    private static bool IsAllContainingTypesPartial(IMethodSymbol method, CancellationToken cancellationToken)
    {
        var currentType = method.ContainingType;

        while (currentType is not null)
        {
            var isPartial = false;

            foreach (var syntaxReference in currentType.DeclaringSyntaxReferences)
            {
                if (syntaxReference.GetSyntax(cancellationToken) is TypeDeclarationSyntax typeDeclarationSyntax &&
                    typeDeclarationSyntax.Modifiers.Any(SyntaxKind.PartialKeyword))
                {
                    isPartial = true;
                    break;
                }
            }

            if (!isPartial)
            {
                return false;
            }

            currentType = currentType.ContainingType;
        }

        return true;
    }

    private static EquatableArray<ContainingTypeDeclaration> GetTypeHierarchy(IMethodSymbol method, CancellationToken cancellationToken)
    {
        var typeDeclarations = ImmutableArray.CreateBuilder<ContainingTypeDeclaration>();
        var currentType = method.ContainingType;

        while (currentType is not null)
        {
            cancellationToken.ThrowIfCancellationRequested();
            typeDeclarations.Insert(0, CreateTypeDeclaration(currentType));
            currentType = currentType.ContainingType;
        }

        return typeDeclarations.ToImmutable();
    }

    private static ContainingTypeDeclaration CreateTypeDeclaration(INamedTypeSymbol currentType)
    {
        var keyword = currentType.TypeKind switch
        {
            TypeKind.Struct => currentType.IsRecord ? "record struct" : "struct",
            TypeKind.Class => currentType.IsRecord ? "record class" : "class",
            _ => "class"
        };

        var accessibility = currentType.DeclaredAccessibility switch
        {
            Accessibility.Public => "public",
            Accessibility.Internal => "internal",
            Accessibility.Protected => "protected",
            Accessibility.ProtectedOrInternal => "protected internal",
            Accessibility.ProtectedAndInternal => "private protected",
            Accessibility.Private => "private",
            _ => "internal"
        };

        var typeParameters = currentType.TypeParameters.Select(static parameter => CSharpIdentifier.Escape(parameter.Name)).ToImmutableArray();
        return new ContainingTypeDeclaration(keyword, CSharpIdentifier.Escape(currentType.Name), currentType.IsStatic, accessibility, typeParameters);
    }

    private static bool HasDuplicateOverloads(IMethodSymbol methodSymbol, INamedTypeSymbol attributeType,
        CancellationToken cancellationToken)
    {
        foreach (var member in methodSymbol.ContainingType.GetMembers(methodSymbol.Name))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (member is IMethodSymbol siblingMethod && !SymbolEqualityComparer.Default.Equals(siblingMethod, methodSymbol))
            {
                foreach (var attribute in siblingMethod.GetAttributes())
                {
                    if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attributeType))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private static (bool HasUiApplicationParameter, EquatableArray<ExternalEventParameter> ExtraParameters) ClassifyParameters(IMethodSymbol methodSymbol)
    {
        var parameters = methodSymbol.Parameters;
        var hasUiApplicationParameter = false;
        var extraParameters = ImmutableArray.CreateBuilder<ExternalEventParameter>();
        var recordPropertyNames = GetReservedRecordPropertyNames(methodSymbol);

        for (var parameterIndex = 0; parameterIndex < parameters.Length; parameterIndex++)
        {
            var parameter = parameters[parameterIndex];
            if (parameterIndex == 0 && parameter.Type.ToDisplayString(NullableFlowFormat) == ExternalEventTypeNames.UiApplication.WithoutGlobalPrefix)
            {
                hasUiApplicationParameter = true;
                continue;
            }

            var propertyName = char.ToUpperInvariant(parameter.Name[0]) + parameter.Name[1..];
            propertyName = CSharpIdentifier.Reserve(propertyName, recordPropertyNames);
            var parameterTypeFullyQualified = parameter.Type.GetFullyQualifiedNameWithNullabilityAnnotations();
            extraParameters.Add(new ExternalEventParameter(parameter.Name, parameterTypeFullyQualified, propertyName));
        }

        return (hasUiApplicationParameter, extraParameters.ToImmutable());
    }

    private static HashSet<string> GetReservedRecordPropertyNames(IMethodSymbol methodSymbol)
    {
        var reservedNames = new HashSet<string>(StringComparer.Ordinal)
        {
            $"{methodSymbol.Name}Args", "Clone", "Equals", "GetHashCode", "ToString", "EqualityContract", "PrintMembers", "Deconstruct"
        };

        for (var containingType = methodSymbol.ContainingType; containingType is not null; containingType = containingType.ContainingType)
        {
            reservedNames.UnionWith(containingType.TypeParameters.Select(static parameter => parameter.Name));
        }

        return reservedNames;
    }

    private static (bool IsVoidReturn, string? ReturnTypeFullyQualified) ExtractReturnType(IMethodSymbol methodSymbol)
    {
        if (methodSymbol.ReturnsVoid)
        {
            return (true, null);
        }

        return (false, methodSymbol.ReturnType.GetFullyQualifiedNameWithNullabilityAnnotations());
    }

    private static string BuildDelegateType(
        IMethodSymbol methodSymbol,
        bool hasUiApplicationParameter,
        bool returnsVoid,
        string? fullyQualifiedReturnType,
        EquatableArray<ExternalEventParameter> extraParameters)
    {
        var parameterTypes = new List<string>();
        if (hasUiApplicationParameter)
        {
            parameterTypes.Add(methodSymbol.Parameters[0].Type.GetFullyQualifiedNameWithNullabilityAnnotations());
        }

        foreach (var parameter in extraParameters)
        {
            parameterTypes.Add(parameter.FullyQualifiedType);
        }

        if (returnsVoid)
        {
            return parameterTypes.Count == 0
                ? "global::System.Action"
                : $"global::System.Action<{string.Join(", ", parameterTypes)}>";
        }

        parameterTypes.Add(fullyQualifiedReturnType!);
        return $"global::System.Func<{string.Join(", ", parameterTypes)}>";
    }

    private static bool IsTaskType(ITypeSymbol type)
    {
        var fullyQualifiedName = type.ToDisplayString(NullableFlowFormat);
        return fullyQualifiedName == ExternalEventTypeNames.Task.WithoutGlobalPrefix
               || type.OriginalDefinition.ToDisplayString(NullableFlowFormat) == ExternalEventTypeNames.TaskGeneric.WithoutGlobalPrefix;
    }

    private static string GetNamespace(INamedTypeSymbol type)
    {
        var containingNamespace = type.ContainingNamespace;
        return containingNamespace.IsGlobalNamespace ? string.Empty : containingNamespace.ToDisplayString();
    }

    private static string GetHintName(IMethodSymbol method)
    {
        var nameParts = new List<string>();
        var currentType = method.ContainingType;

        while (currentType is not null)
        {
            nameParts.Insert(0, currentType.MetadataName);
            currentType = currentType.ContainingType;
        }

        var containingNamespace = GetNamespace(method.ContainingType);
        if (!string.IsNullOrEmpty(containingNamespace))
        {
            nameParts.Insert(0, containingNamespace);
        }

        nameParts.Add(method.Name);
        var qualifiedName = string.Join(".", nameParts);

        using var algorithm = SHA256.Create();
        var hash = algorithm.ComputeHash(Encoding.UTF8.GetBytes(qualifiedName));

        return $"{qualifiedName}.{BitConverter.ToString(hash, 0, 8).Replace("-", string.Empty)}";
    }
}
