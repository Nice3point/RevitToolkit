using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Nice3point.Revit.Toolkit.Analyzers.Diagnostics;
using Nice3point.Revit.Toolkit.SourceGenerators.Extensions;
using Nice3point.Revit.Toolkit.SourceGenerators.Models;

namespace Nice3point.Revit.Toolkit.SourceGenerators;

partial class ExternalEventGenerator
{
    /// <summary>
    ///     Extracts and validates method metadata from syntax contexts
    ///     for methods annotated with [ExternalEvent].
    /// </summary>
    internal static class Extractor
    {
        /// <summary>
        ///     Symbol display format that omits the global namespace prefix and nullable annotations.
        /// </summary>
        private static readonly SymbolDisplayFormat NullableFlowFormat = new(
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.None);

        /// <summary>
        ///     Analyzes a method symbol from the given syntax context, validates it against
        ///     generator constraints, and returns the extracted metadata or diagnostics.
        /// </summary>
        public static ExternalEventMethodResult GetMethodResult(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (context.TargetSymbol is not IMethodSymbol methodSymbol)
            {
                return new ExternalEventMethodResult();
            }

            var diagnostics = ImmutableArray.CreateBuilder<DiagnosticInfo>();

            if (!ValidateMethod(methodSymbol, diagnostics))
            {
                return new ExternalEventMethodResult(diagnostics.ToImmutable());
            }

            var info = BuildExternalEventInfo(methodSymbol);

            return new ExternalEventMethodResult(info, diagnostics.ToImmutable());
        }

        /// <summary>
        ///     Validates the method symbol against all generator constraints.
        ///     Returns <c>false</c> if a fatal validation error was found.
        /// </summary>
        private static bool ValidateMethod(IMethodSymbol methodSymbol, ImmutableArray<DiagnosticInfo>.Builder diagnostics)
        {
            if (!IsAllContainingTypesPartial(methodSymbol))
            {
                var nonPartialType = FindFirstNonPartialContainingType(methodSymbol);
                diagnostics.Add(
                    DiagnosticDescriptors.ExternalEventContainingTypeNotPartial,
                    nonPartialType ?? (ISymbol)methodSymbol,
                    nonPartialType?.Name ?? methodSymbol.ContainingType.Name,
                    methodSymbol.Name);

                return false;
            }

            if (methodSymbol.IsGenericMethod)
            {
                diagnostics.Add(
                    DiagnosticDescriptors.ExternalEventGenericMethod,
                    methodSymbol,
                    methodSymbol.Name);

                return false;
            }

            if (IsTaskType(methodSymbol.ReturnType))
            {
                diagnostics.Add(
                    DiagnosticDescriptors.ExternalEventTaskReturnNotSupported,
                    methodSymbol,
                    methodSymbol.Name);

                return false;
            }

            if (HasDuplicateOverloads(methodSymbol))
            {
                diagnostics.Add(
                    DiagnosticDescriptors.ExternalEventDuplicateMethodOverload,
                    methodSymbol,
                    methodSymbol.Name);

                return false;
            }

            return true;
        }

        /// <summary>
        ///     Builds the <see cref="ExternalEventInfo"/> from a validated method symbol,
        ///     extracting attribute data, parameters, and type hierarchy.
        /// </summary>
        private static ExternalEventInfo BuildExternalEventInfo(IMethodSymbol methodSymbol)
        {
            var allowDirectInvocation = ExtractAllowDirectInvocation(methodSymbol);
            var (hasUiApplicationParameter, extraParameters) = ClassifyParameters(methodSymbol);
            var (returnsVoid, returnType) = ExtractReturnType(methodSymbol);
            var typeHierarchy = GetTypeHierarchy(methodSymbol);
            var containingNamespace = GetNamespace(methodSymbol.ContainingType);
            var delegateType = BuildDelegateType(methodSymbol, hasUiApplicationParameter, returnsVoid, returnType, extraParameters);

            return new ExternalEventInfo(
                HintName: $"{GetHintName(methodSymbol)}.{methodSymbol.Name}",
                Namespace: containingNamespace,
                MethodName: methodSymbol.Name,
                IsStatic: methodSymbol.IsStatic,
                ReturnsVoid: returnsVoid,
                FullyQualifiedReturnType: returnType,
                FullyQualifiedDelegateType: delegateType,
                HasUiApplicationParameter: hasUiApplicationParameter,
                AllowDirectInvocation: allowDirectInvocation,
                TypeHierarchy: typeHierarchy,
                ExtraParameters: extraParameters);
        }

        /// <summary>
        ///     Checks whether all containing types of the method are declared as partial.
        /// </summary>
        private static bool IsAllContainingTypesPartial(IMethodSymbol method)
        {
            var currentType = method.ContainingType;

            while (currentType is not null)
            {
                var isPartial = false;

                foreach (var syntaxReference in currentType.DeclaringSyntaxReferences)
                {
                    if (syntaxReference.GetSyntax() is TypeDeclarationSyntax typeDeclarationSyntax &&
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

        /// <summary>
        ///     Finds the first non-partial containing type in the hierarchy for diagnostic reporting.
        /// </summary>
        private static INamedTypeSymbol? FindFirstNonPartialContainingType(IMethodSymbol method)
        {
            var currentType = method.ContainingType;

            while (currentType is not null)
            {
                var isPartial = false;

                foreach (var syntaxReference in currentType.DeclaringSyntaxReferences)
                {
                    if (syntaxReference.GetSyntax() is TypeDeclarationSyntax typeDeclarationSyntax &&
                        typeDeclarationSyntax.Modifiers.Any(SyntaxKind.PartialKeyword))
                    {
                        isPartial = true;
                        break;
                    }
                }

                if (!isPartial)
                {
                    return currentType;
                }

                currentType = currentType.ContainingType;
            }

            return null;
        }

        /// <summary>
        ///     Builds the type hierarchy from outermost to innermost containing type.
        /// </summary>
        private static EquatableArray<TypeDeclarationInfo> GetTypeHierarchy(IMethodSymbol method)
        {
            var typeDeclarations = ImmutableArray.CreateBuilder<TypeDeclarationInfo>();
            var currentType = method.ContainingType;

            while (currentType is not null)
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

                typeDeclarations.Insert(0, new TypeDeclarationInfo(keyword, currentType.Name, currentType.IsStatic, accessibility));
                currentType = currentType.ContainingType;
            }

            return typeDeclarations.ToImmutable();
        }

        /// <summary>
        ///     Checks whether any sibling overload of the method is also annotated with [ExternalEvent].
        /// </summary>
        private static bool HasDuplicateOverloads(IMethodSymbol methodSymbol)
        {
            foreach (var member in methodSymbol.ContainingType.GetMembers(methodSymbol.Name))
            {
                if (member is IMethodSymbol siblingMethod && !SymbolEqualityComparer.Default.Equals(siblingMethod, methodSymbol))
                {
                    foreach (var attribute in siblingMethod.GetAttributes())
                    {
                        if (attribute.AttributeClass?.ToDisplayString() == WellKnownFullyQualifiedClassNames.ExternalEventAttribute.WithoutGlobalPrefix)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        ///     Extracts the <c>AllowDirectInvocation</c> named argument from the [ExternalEvent] attribute.
        /// </summary>
        private static bool ExtractAllowDirectInvocation(IMethodSymbol methodSymbol)
        {
            foreach (var attribute in methodSymbol.GetAttributes())
            {
                if (attribute.AttributeClass?.ToDisplayString() == WellKnownFullyQualifiedClassNames.ExternalEventAttribute.WithoutGlobalPrefix)
                {
                    return attribute.GetNamedArgument("AllowDirectInvocation", false);
                }
            }

            return false;
        }

        /// <summary>
        ///     Classifies method parameters into the optional UIApplication parameter
        ///     and extra user-defined parameters.
        /// </summary>
        private static (bool HasUiApplicationParameter, EquatableArray<ParameterData> ExtraParameters) ClassifyParameters(IMethodSymbol methodSymbol)
        {
            var parameters = methodSymbol.Parameters;
            var hasUiApplicationParameter = false;
            var extraParameters = ImmutableArray.CreateBuilder<ParameterData>();

            for (var parameterIndex = 0; parameterIndex < parameters.Length; parameterIndex++)
            {
                var parameter = parameters[parameterIndex];
                var parameterTypeFullyQualified = parameter.Type.GetFullyQualifiedNameWithNullabilityAnnotations();

                if (parameterIndex == 0 && parameter.Type.ToDisplayString(NullableFlowFormat) == WellKnownFullyQualifiedClassNames.UiApplication.WithoutGlobalPrefix)
                {
                    hasUiApplicationParameter = true;
                    continue;
                }

                extraParameters.Add(new ParameterData(parameter.Name, parameterTypeFullyQualified));
            }

            return (hasUiApplicationParameter, extraParameters.ToImmutable());
        }

        /// <summary>
        ///     Extracts the return type information from the method symbol.
        /// </summary>
        private static (bool IsVoidReturn, string? ReturnTypeFullyQualified) ExtractReturnType(IMethodSymbol methodSymbol)
        {
            if (methodSymbol.ReturnsVoid)
            {
                return (true, null);
            }

            return (false, methodSymbol.ReturnType.GetFullyQualifiedNameWithNullabilityAnnotations());
        }
        
        /// <summary>
        ///     Builds the fully qualified delegate type for wrapping the method reference.
        ///     Used to generate explicit delegate casts like <c>new global::System.Action(Method)</c>
        ///     to avoid overload resolution conflicts with method groups.
        /// </summary>
        private static string BuildDelegateType(
            IMethodSymbol methodSymbol,
            bool hasUiApplicationParameter,
            bool returnsVoid,
            string? fullyQualifiedReturnType,
            EquatableArray<ParameterData> extraParameters)
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

        /// <summary>
        ///     Determines whether the specified type is <see cref="System.Threading.Tasks.Task"/>
        ///     or <see cref="System.Threading.Tasks.Task{TResult}"/>.
        /// </summary>
        private static bool IsTaskType(ITypeSymbol type)
        {
            var fullyQualifiedName = type.ToDisplayString(NullableFlowFormat);
            return fullyQualifiedName == WellKnownFullyQualifiedClassNames.Task.WithoutGlobalPrefix
                   || type.OriginalDefinition.ToDisplayString(NullableFlowFormat) == WellKnownFullyQualifiedClassNames.TaskGeneric.WithoutGlobalPrefix;
        }

        /// <summary>
        ///     Returns the fully qualified namespace of the type, or an empty string for the global namespace.
        /// </summary>
        private static string GetNamespace(INamedTypeSymbol type)
        {
            var containingNamespace = type.ContainingNamespace;
            return containingNamespace.IsGlobalNamespace ? string.Empty : containingNamespace.ToDisplayString();
        }

        /// <summary>
        ///     Builds the hint name for the generated source file based on the method's
        ///     containing namespace and type hierarchy.
        /// </summary>
        private static string GetHintName(IMethodSymbol method)
        {
            var nameParts = new List<string>();
            var currentType = method.ContainingType;
            while (currentType is not null)
            {
                nameParts.Insert(0, currentType.Name);
                currentType = currentType.ContainingType;
            }

            var containingNamespace = GetNamespace(method.ContainingType);
            if (!string.IsNullOrEmpty(containingNamespace))
            {
                nameParts.Insert(0, containingNamespace);
            }

            return string.Join(".", nameParts);
        }
    }
}