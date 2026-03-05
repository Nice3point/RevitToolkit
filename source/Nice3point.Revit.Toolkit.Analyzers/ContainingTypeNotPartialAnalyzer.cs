using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Nice3point.Revit.Toolkit.Analyzers.Diagnostics;

namespace Nice3point.Revit.Toolkit.Analyzers;

/// <summary>
///     A diagnostic analyzer that reports an error when a method marked with <c>[ExternalEvent]</c> is declared inside a type that is not <see langword="partial"/>.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ExternalEventContainingTypeNotPartialAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [DiagnosticDescriptors.ExternalEventContainingTypeNotPartial];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(static context =>
        {
            var attributeSymbol = context.Compilation.GetTypeByMetadataName("Nice3point.Revit.Toolkit.External.ExternalEventAttribute");
            if (attributeSymbol is null)
            {
                return;
            }

            context.RegisterSymbolAction(context =>
            {
                if (context.Symbol is not IMethodSymbol methodSymbol)
                {
                    return;
                }

                if (!HasTargetAttribute(methodSymbol, attributeSymbol))
                {
                    return;
                }

                var currentType = methodSymbol.ContainingType;
                while (currentType is not null)
                {
                    if (!IsTypeDeclarationPartial(context, currentType))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            descriptor: DiagnosticDescriptors.ExternalEventContainingTypeNotPartial,
                            location: methodSymbol.Locations[0],
                            messageArgs: [currentType.Name, methodSymbol.Name]));

                        return;
                    }

                    currentType = currentType.ContainingType;
                }
            }, SymbolKind.Method);
        });
    }

    private static bool HasTargetAttribute(IMethodSymbol methodSymbol, INamedTypeSymbol attributeSymbol)
    {
        foreach (var attribute in methodSymbol.GetAttributes())
        {
            if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attributeSymbol))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsTypeDeclarationPartial(SymbolAnalysisContext context, INamedTypeSymbol currentType)
    {
        foreach (var syntaxReference in currentType.DeclaringSyntaxReferences)
        {
            if (syntaxReference.GetSyntax(context.CancellationToken) is TypeDeclarationSyntax typeDeclaration)
            {
                if (typeDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword))
                {
                    return true;
                }
            }
        }

        return false;
    }
}