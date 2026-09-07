using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Nice3point.Revit.Toolkit.SourceGenerators.ExternalEvents.Analysis;

internal static class ExternalEventExtensionContainer
{
    public static (string ClassAccessibility, string? ConflictingName) Resolve(IMethodSymbol method, CancellationToken cancellationToken)
    {
        var hasUiApplicationParameter = method.Parameters.Length > 0 &&
                                        method.Parameters[0].Type.WithNullableAnnotation(NullableAnnotation.NotAnnotated).ToDisplayString() == ExternalEventTypeNames.UiApplication.WithoutGlobalPrefix;

        if (method.Parameters.Length - (hasUiApplicationParameter ? 1 : 0) <= 1)
        {
            return ("public", null);
        }

        var outermostType = method.ContainingType;
        var publiclyAccessible = true;
        for (var containingType = method.ContainingType; containingType is not null; containingType = containingType.ContainingType)
        {
            if (containingType.DeclaredAccessibility is Accessibility.Private or Accessibility.Protected or Accessibility.ProtectedAndInternal)
            {
                return ("public", null);
            }

            publiclyAccessible &= containingType.DeclaredAccessibility == Accessibility.Public;
            outermostType = containingType;
        }

        var className = $"{outermostType.Name}Extensions";
        foreach (var member in outermostType.ContainingNamespace.GetMembers(className))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (member is INamedTypeSymbol { Arity: > 0 } or INamedTypeSymbol { IsFileLocal: true } || !member.Locations.Any(static location => location.IsInSource))
            {
                continue;
            }

            if (member is not INamedTypeSymbol { TypeKind: TypeKind.Class, IsStatic: true, IsFileLocal: false } existingClass)
            {
                return ("public", className);
            }

            if (!IsCompatibleDeclaration(existingClass, publiclyAccessible, cancellationToken))
            {
                return ("public", className);
            }

            return (existingClass.DeclaredAccessibility == Accessibility.Public ? "public" : "internal", null);
        }

        return ("public", null);
    }

    private static bool IsCompatibleDeclaration(INamedTypeSymbol existingClass, bool requiresPublicAccessibility, CancellationToken cancellationToken)
    {
        foreach (var syntaxReference in existingClass.DeclaringSyntaxReferences)
        {
            if (syntaxReference.GetSyntax(cancellationToken) is not ClassDeclarationSyntax declaration || !declaration.Modifiers.Any(SyntaxKind.PartialKeyword))
            {
                return false;
            }
        }

        return !requiresPublicAccessibility || existingClass.DeclaredAccessibility == Accessibility.Public;
    }
}
