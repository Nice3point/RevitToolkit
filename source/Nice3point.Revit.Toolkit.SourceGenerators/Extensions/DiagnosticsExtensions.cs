using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Nice3point.Revit.Toolkit.SourceGenerators.Models;

namespace Nice3point.Revit.Toolkit.SourceGenerators.Extensions;

/// <summary>
///     Extension methods for working with diagnostics from incremental generator pipelines.
/// </summary>
internal static class DiagnosticsExtensions
{
    /// <summary>
    ///     Adds a new diagnostic to the target builder.
    /// </summary>
    /// <param name="diagnostics">The collection of produced <see cref="DiagnosticInfo"/> instances.</param>
    /// <param name="descriptor">The input <see cref="DiagnosticDescriptor"/> for the diagnostic to create.</param>
    /// <param name="symbol">The source <see cref="ISymbol"/> to attach the diagnostic to.</param>
    /// <param name="args">The optional arguments for the formatted message to include.</param>
    public static void Add(
        this ImmutableArray<DiagnosticInfo>.Builder diagnostics,
        DiagnosticDescriptor descriptor,
        ISymbol symbol,
        params string[] args)
    {
        diagnostics.Add(DiagnosticInfo.Create(descriptor, symbol, args));
    }

    /// <summary>
    ///     Registers an output node into an <see cref="IncrementalGeneratorInitializationContext"/> to output diagnostics.
    /// </summary>
    /// <param name="context">The input <see cref="IncrementalGeneratorInitializationContext"/> instance.</param>
    /// <param name="diagnostics">The input <see cref="IncrementalValuesProvider{TValues}"/> sequence of diagnostics.</param>
    public static void ReportDiagnostics(
        this IncrementalGeneratorInitializationContext context,
        IncrementalValuesProvider<EquatableArray<DiagnosticInfo>> diagnostics)
    {
        context.RegisterSourceOutput(diagnostics, static (sourceProductionContext, diagnostics) =>
        {
            foreach (var diagnostic in diagnostics)
            {
                sourceProductionContext.ReportDiagnostic(diagnostic.ToDiagnostic());
            }
        });
    }
}
