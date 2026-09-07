using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Nice3point.Revit.Toolkit.Analyzers.CSharp;
using Nice3point.Revit.Toolkit.Analyzers.ExternalEvents;

namespace Nice3point.Revit.Toolkit.Analyzers;

/// <summary>
///     A diagnostic analyzer that reports an error when a method marked with <c>[ExternalEvent]</c> is declared inside a type that is not <see langword="partial" />.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ExternalEventContainingTypeNotPartialAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
    [
        ExternalEventDiagnostics.ContainingTypeNotPartial
    ];

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
                if (context.Symbol is not INamedTypeSymbol typeSymbol)
                {
                    return;
                }

                var nonPartialIdentifier = FindNonPartialIdentifier(typeSymbol, context.CancellationToken);
                if (nonPartialIdentifier is null)
                {
                    return;
                }

                var methodName = FindFirstAttributedMethodName(typeSymbol, attributeSymbol, context.CancellationToken);
                if (methodName is not null)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        ExternalEventDiagnostics.ContainingTypeNotPartial,
                        nonPartialIdentifier.Value.GetLocation(),
                        typeSymbol.Name,
                        methodName));
                }
            }, SymbolKind.NamedType);
        });
    }

    private static string? FindFirstAttributedMethodName(INamedTypeSymbol typeSymbol, INamedTypeSymbol attributeSymbol, CancellationToken cancellationToken)
    {
        var members = typeSymbol.GetMembers();
        foreach (var member in members)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (member is IMethodSymbol methodSymbol)
            {
                if (methodSymbol.HasAttribute(attributeSymbol))
                {
                    return methodSymbol.Name;
                }
            }
        }

        foreach (var member in members)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (member is INamedTypeSymbol nestedType)
            {
                var attributedMethodName = FindFirstAttributedMethodName(nestedType, attributeSymbol, cancellationToken);
                if (attributedMethodName is not null)
                {
                    return attributedMethodName;
                }
            }
        }

        return null;
    }

    private static SyntaxToken? FindNonPartialIdentifier(INamedTypeSymbol type, CancellationToken cancellationToken)
    {
        SyntaxToken? identifier = null;
        foreach (var syntaxReference in type.DeclaringSyntaxReferences)
        {
            if (syntaxReference.GetSyntax(cancellationToken) is TypeDeclarationSyntax typeDeclaration)
            {
                if (typeDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword))
                {
                    return null;
                }

                identifier ??= typeDeclaration.Identifier;
            }
        }

        return identifier;
    }
}
