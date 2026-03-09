using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
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
                    var nonPartialSyntax = FindNonPartialIdentifier(context, currentType);
                    if (nonPartialSyntax.HasValue)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            descriptor: DiagnosticDescriptors.ExternalEventContainingTypeNotPartial,
                            location: nonPartialSyntax.Value.GetLocation(),
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

    private static SyntaxToken? FindNonPartialIdentifier(SymbolAnalysisContext context, INamedTypeSymbol type)
    {
        foreach (var syntaxReference in type.DeclaringSyntaxReferences)
        {
            if (syntaxReference.GetSyntax(context.CancellationToken) is TypeDeclarationSyntax typeDeclaration)
            {
                if (typeDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword))
                {
                    return null;
                }
            }
        }

        if (type.DeclaringSyntaxReferences[0].GetSyntax(context.CancellationToken) is TypeDeclarationSyntax firstDeclaration)
        {
            return firstDeclaration.Identifier;
        }

        return null;
    }
}