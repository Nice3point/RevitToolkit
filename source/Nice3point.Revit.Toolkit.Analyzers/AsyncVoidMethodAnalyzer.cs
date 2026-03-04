using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Nice3point.Revit.Toolkit.Analyzers.Diagnostics;

namespace Nice3point.Revit.Toolkit.Analyzers;

/// <summary>
///     A diagnostic analyzer that reports a warning when a method marked with <c>[ExternalEvent]</c> is declared as <see langword="async"/> <see langword="void"/>.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AsyncVoidMethodAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [DiagnosticDescriptors.MethodIsAsyncVoid];

    /// <inheritdoc/>
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
                if (context.Symbol is not IMethodSymbol { IsAsync: true, ReturnsVoid: true } methodSymbol)
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

                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.MethodIsAsyncVoid,
                    methodSymbol.Locations[0],
                    methodSymbol.Name));
            }, SymbolKind.Method);
        });
    }
}
