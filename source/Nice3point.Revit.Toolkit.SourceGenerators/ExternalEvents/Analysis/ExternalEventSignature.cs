using Microsoft.CodeAnalysis;

namespace Nice3point.Revit.Toolkit.SourceGenerators.ExternalEvents.Analysis;

internal static class ExternalEventSignature
{
    public static string? GetUnsupportedReason(IMethodSymbol method, CancellationToken cancellationToken)
    {
        if (method.MethodKind == MethodKind.ExplicitInterfaceImplementation)
        {
            return "explicit interface implementations are not supported";
        }

        if (method is { IsPartialDefinition: true, PartialImplementationPart: null })
        {
            return "partial methods require an implementation";
        }

        for (var containingType = method.ContainingType; containingType is not null; containingType = containingType.ContainingType)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (containingType.TypeKind is not (TypeKind.Class or TypeKind.Struct) || containingType.IsFileLocal || containingType.IsRefLikeType)
            {
                return "containing types must be non-file-local classes or non-ref structs";
            }

            if (SymbolEqualityComparer.Default.Equals(containingType, method.ContainingType) && containingType.IsReadOnly && !method.IsStatic)
            {
                return "instance events require mutable containing structs";
            }
        }

        if (method.ReturnsByRef || method.ReturnsByRefReadonly || method.Parameters.Any(static parameter => parameter.RefKind != RefKind.None))
        {
            return "parameters and return values must be passed by value";
        }

        if (method.Parameters.Length > 16)
        {
            return "the method must have at most 16 parameters";
        }

        foreach (var type in method.Parameters.Select(static parameter => parameter.Type).Append(method.ReturnType))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!CanUseAsTypeArgument(type))
            {
                return "parameter and return types must support use as generic type arguments";
            }

            if (!IsVisibleFromGeneratedMember(type, method.ContainingType))
            {
                return "parameter and return types must be at least as accessible as the containing type";
            }
        }

        return null;
    }

    private static bool CanUseAsTypeArgument(ITypeSymbol type)
    {
        return type switch
        {
            IPointerTypeSymbol or IFunctionPointerTypeSymbol => false,
            IArrayTypeSymbol array => CanUseAsTypeArgument(array.ElementType),
            ITypeParameterSymbol parameter => !parameter.AllowsRefLikeType,
            INamedTypeSymbol named => !named.IsRefLikeType && named.TypeArguments.All(CanUseAsTypeArgument),
            _ => true
        };
    }

    private static bool IsVisibleFromGeneratedMember(ITypeSymbol type, INamedTypeSymbol owner)
    {
        if (type is IArrayTypeSymbol array)
        {
            return IsVisibleFromGeneratedMember(array.ElementType, owner);
        }

        if (type is not INamedTypeSymbol named)
        {
            return true;
        }

        for (var currentType = named; currentType is not null; currentType = currentType.ContainingType)
        {
            if (!currentType.TypeArguments.All(argument => IsVisibleFromGeneratedMember(argument, owner)))
            {
                return false;
            }

            if (!IsAccessibilityCovered(currentType, owner))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsAccessibilityCovered(INamedTypeSymbol type, INamedTypeSymbol owner)
    {
        if (type.DeclaredAccessibility == Accessibility.Public)
        {
            return true;
        }

        for (var currentOwner = owner; currentOwner is not null; currentOwner = currentOwner.ContainingType)
        {
            if (SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, currentOwner.OriginalDefinition))
            {
                return true;
            }

            if (type.DeclaredAccessibility is Accessibility.Internal or Accessibility.ProtectedOrInternal &&
                currentOwner.DeclaredAccessibility is Accessibility.Internal or Accessibility.Private or Accessibility.ProtectedAndInternal)
            {
                return true;
            }

            if (currentOwner.DeclaredAccessibility == Accessibility.Private)
            {
                for (var scope = currentOwner.ContainingType; scope is not null; scope = scope.ContainingType)
                {
                    if (SymbolEqualityComparer.Default.Equals(scope, type.ContainingType))
                    {
                        return true;
                    }
                }
            }

            if (currentOwner.DeclaredAccessibility == type.DeclaredAccessibility && SymbolEqualityComparer.Default.Equals(currentOwner.ContainingType, type.ContainingType))
            {
                return true;
            }
        }

        return false;
    }
}
