using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Nice3point.Revit.Toolkit.Analyzers.CSharp;
using Nice3point.Revit.Toolkit.Analyzers.ExternalEvents;

namespace Nice3point.Revit.Toolkit.Analyzers;

/// <summary>
///     A diagnostic analyzer that reports a warning when a method marked with <c>[ExternalEvent]</c> is declared as <see langword="async" /> <see langword="void" />.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AsyncVoidMethodAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
    [
        ExternalEventDiagnostics.AsyncVoidMethod
    ];

    /// <inheritdoc />
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
                if (context.Symbol is not IMethodSymbol { IsAsync: true, ReturnsVoid: true } methodSymbol)
                {
                    return;
                }

                if (!methodSymbol.HasAttribute(attributeSymbol))
                {
                    return;
                }

                context.ReportDiagnostic(Diagnostic.Create(
                    ExternalEventDiagnostics.AsyncVoidMethod,
                    methodSymbol.Locations[0],
                    methodSymbol.Name));
            }, SymbolKind.Method);
        });
    }
}
