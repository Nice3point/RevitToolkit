using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Nice3point.Revit.Toolkit.SourceGenerators.Diagnostics;
using Nice3point.Revit.Toolkit.SourceGenerators.Models;

namespace Nice3point.Revit.Toolkit.SourceGenerators;

partial class ExternalEventGenerator
{
    /// <summary>
    ///     Extracts and validates method metadata from syntax contexts
    ///     for methods annotated with [ExternalEvent].
    /// </summary>
    internal static class Execute
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
                return default;
            }

            var diagnostics = new List<Diagnostic>();

            var validationResult = ValidateMethod(methodSymbol, diagnostics);
            if (validationResult != null)
            {
                return validationResult.Value;
            }

            CollectWarnings(methodSymbol, diagnostics);

            var info = BuildExternalEventInfo(methodSymbol);

            return new ExternalEventMethodResult(info, diagnostics.Count > 0 ? diagnostics.ToArray() : null);
        }

        /// <summary>
        ///     Validates the method symbol against all generator constraints.
        ///     Returns an error result if validation fails, or <c>null</c> if the method is valid.
        /// </summary>
        private static ExternalEventMethodResult? ValidateMethod(IMethodSymbol methodSymbol, List<Diagnostic> diagnostics)
        {
            if (!IsContainingTypePartial(methodSymbol, out _))
            {
                diagnostics.Add(Diagnostic.Create(
                    DiagnosticDescriptors.ContainingTypeNotPartial,
                    methodSymbol.Locations[0],
                    methodSymbol.ContainingType.Name,
                    methodSymbol.Name));

                return new ExternalEventMethodResult(null, diagnostics.ToArray());
            }

            if (methodSymbol.IsGenericMethod)
            {
                diagnostics.Add(Diagnostic.Create(
                    DiagnosticDescriptors.MethodIsGeneric,
                    methodSymbol.Locations[0],
                    methodSymbol.Name));

                return new ExternalEventMethodResult(null, diagnostics.ToArray());
            }

            if (IsTaskType(methodSymbol.ReturnType))
            {
                diagnostics.Add(Diagnostic.Create(
                    DiagnosticDescriptors.TaskReturnNotSupported,
                    methodSymbol.Locations[0],
                    methodSymbol.Name));

                return new ExternalEventMethodResult(null, diagnostics.ToArray());
            }

            if (HasDuplicateOverloads(methodSymbol))
            {
                diagnostics.Add(Diagnostic.Create(
                    DiagnosticDescriptors.DuplicateMethodOverload,
                    methodSymbol.Locations[0],
                    methodSymbol.Name));

                return new ExternalEventMethodResult(null, diagnostics.ToArray());
            }

            return null;
        }

        /// <summary>
        ///     Collects non-fatal warnings for the method (e.g., async void usage).
        /// </summary>
        private static void CollectWarnings(IMethodSymbol methodSymbol, List<Diagnostic> diagnostics)
        {
            if (methodSymbol is { IsAsync: true, ReturnsVoid: true })
            {
                diagnostics.Add(Diagnostic.Create(
                    DiagnosticDescriptors.MethodIsAsyncVoid,
                    methodSymbol.Locations[0],
                    methodSymbol.Name));
            }
        }

        /// <summary>
        ///     Builds the <see cref="ExternalEventInfo"/> from a validated method symbol,
        ///     extracting attribute data, parameters, and type hierarchy.
        /// </summary>
        private static ExternalEventInfo BuildExternalEventInfo(IMethodSymbol methodSymbol)
        {
            var allowDirectInvocation = ExtractAllowDirectInvocation(methodSymbol);
            var (hasUiApplicationParameter, extraParameters) = ClassifyParameters(methodSymbol);
            var (isVoidReturn, returnType) = ExtractReturnType(methodSymbol);

            IsContainingTypePartial(methodSymbol, out var typeHierarchy);
            var containingNamespace = GetNamespace(methodSymbol.ContainingType);

            return new ExternalEventInfo(
                HintName: $"{GetHintName(methodSymbol)}.{methodSymbol.Name}",
                Namespace: containingNamespace,
                TypeHierarchy: typeHierarchy,
                MethodName: methodSymbol.Name,
                IsStatic: methodSymbol.IsStatic,
                IsVoidReturn: isVoidReturn,
                ReturnTypeFullyQualified: returnType,
                HasUiApplicationParam: hasUiApplicationParameter,
                ExtraParameters: extraParameters,
                AllowDirectInvocation: allowDirectInvocation);
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
                if (attribute.AttributeClass?.ToDisplayString() != WellKnownFullyQualifiedClassNames.ExternalEventAttribute.WithoutGlobalPrefix)
                {
                    continue;
                }

                foreach (var namedArgument in attribute.NamedArguments)
                {
                    if (namedArgument is { Key: "AllowDirectInvocation", Value.Value: bool value })
                    {
                        return value;
                    }
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
                var parameterTypeFullyQualified = parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                if (parameterIndex == 0 && parameter.Type.ToDisplayString(NullableFlowFormat) == WellKnownFullyQualifiedClassNames.UiApplication.WithoutGlobalPrefix)
                {
                    hasUiApplicationParameter = true;
                    continue;
                }

                extraParameters.Add(new ParameterData(parameter.Name, parameterTypeFullyQualified));
            }

            return (hasUiApplicationParameter, extraParameters.ToImmutable().ToEquatableArray());
        }

        /// <summary>
        ///     Extracts the return type information from the method symbol.
        /// </summary>
        private static (bool IsVoidReturn, string? ReturnTypeFullyQualified) ExtractReturnType(IMethodSymbol methodSymbol)
        {
            var isVoidReturn = methodSymbol.ReturnsVoid;
            string? returnType = null;
            if (!isVoidReturn)
            {
                returnType = methodSymbol.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            }

            return (isVoidReturn, returnType);
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
        ///     Checks whether all containing types of the method are declared as partial
        ///     and builds the type hierarchy from outermost to innermost.
        /// </summary>
        private static bool IsContainingTypePartial(IMethodSymbol method, out EquatableArray<TypeDeclarationInfo> hierarchy)
        {
            var typeDeclarations = ImmutableArray.CreateBuilder<TypeDeclarationInfo>();
            var currentType = method.ContainingType;
            var allPartial = true;

            while (currentType is not null)
            {
                var isPartial = false;
                var isStatic = currentType.IsStatic;
                var keyword = currentType.TypeKind switch
                {
                    TypeKind.Struct => currentType.IsRecord ? "record struct" : "struct",
                    TypeKind.Class => currentType.IsRecord ? "record class" : "class",
                    _ => "class"
                };

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
                    allPartial = false;
                }

                typeDeclarations.Insert(0, new TypeDeclarationInfo(keyword, currentType.Name, isStatic));
                currentType = currentType.ContainingType;
            }

            hierarchy = typeDeclarations.ToImmutable().ToEquatableArray();
            return allPartial;
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
