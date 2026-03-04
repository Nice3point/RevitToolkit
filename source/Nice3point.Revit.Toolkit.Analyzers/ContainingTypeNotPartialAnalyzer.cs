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
public sealed class ContainingTypeNotPartialAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [DiagnosticDescriptors.ContainingTypeNotPartial];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(static context =>
        {
            if (context.Compilation.GetTypeByMetadataName("Nice3point.Revit.Toolkit.External.ExternalEventAttribute") is not { } attributeSymbol)
            {
                return;
            }

            context.RegisterSymbolAction(context =>
            {
                if (context.Symbol is not IMethodSymbol methodSymbol)
                {
                    return;
                }

                var hasAttribute = false;
                foreach (var attribute in methodSymbol.GetAttributes())
                {
                    if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attributeSymbol))
                    {
                        hasAttribute = true;
                        break;
                    }
                }

                if (!hasAttribute)
                {
                    return;
                }

                var currentType = methodSymbol.ContainingType;
                while (currentType is not null)
                {
                    var isPartial = false;
                    foreach (var syntaxReference in currentType.DeclaringSyntaxReferences)
                    {
                        if (syntaxReference.GetSyntax(context.CancellationToken) is TypeDeclarationSyntax typeDeclaration)
                        {
                            if (typeDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword))
                            {
                                isPartial = true;
                                break;
                            }
                        }
                    }

                    if (!isPartial)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            DiagnosticDescriptors.ContainingTypeNotPartial,
                            currentType.Locations[0],
                            currentType.Name,
                            methodSymbol.Name));
                        
                        return;
                    }

                    currentType = currentType.ContainingType;
                }
            }, SymbolKind.Method);
        });
    }
}
