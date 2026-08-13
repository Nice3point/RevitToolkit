using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Nice3point.Revit.Toolkit.SourceGenerators.Models;

namespace Nice3point.Revit.Toolkit.SourceGenerators.Extensions;

/// <summary>
///     Extension methods for working with diagnostics from incremental generator pipelines.
/// </summary>
internal static class DiagnosticsExtensions
{
    /// <param name="diagnostics">The collection of produced <see cref="Nice3point.Revit.Toolkit.SourceGenerators.Models.DiagnosticInfo" /> instances.</param>
    extension(ImmutableArray<DiagnosticInfo>.Builder diagnostics)
    {
        /// <summary>
        ///     Adds a new diagnostic to the target builder from a symbol location.
        /// </summary>
        /// <param name="descriptor">The input <see cref="DiagnosticDescriptor" /> for the diagnostic to create.</param>
        /// <param name="symbol">The source <see cref="ISymbol" /> to attach the diagnostic to.</param>
        /// <param name="args">The optional arguments for the formatted message to include.</param>
        public void Add(DiagnosticDescriptor descriptor, ISymbol symbol, params string[] args)
        {
            diagnostics.Add(DiagnosticInfo.Create(descriptor, symbol, args));
        }

        /// <summary>
        ///     Adds a new diagnostic to the target builder from a syntax node location.
        /// </summary>
        /// <param name="descriptor">The input <see cref="DiagnosticDescriptor" /> for the diagnostic to create.</param>
        /// <param name="node">The input <see cref="SyntaxNode" /> for the diagnostic to create.</param>
        /// <param name="args">The optional arguments for the formatted message to include.</param>
        public void Add(DiagnosticDescriptor descriptor,
            SyntaxNode node,
            params string[] args)
        {
            diagnostics.Add(DiagnosticInfo.Create(descriptor, node, args));
        }
    }

    /// <param name="context">The input <see cref="IncrementalGeneratorInitializationContext" /> instance.</param>
    extension(IncrementalGeneratorInitializationContext context)
    {
        /// <summary>
        ///     Registers an output node into an <see cref="IncrementalGeneratorInitializationContext" /> to output diagnostics.
        /// </summary>
        /// <param name="diagnostics">The input <see cref="IncrementalValuesProvider{TValues}" /> sequence of diagnostics.</param>
        public void ReportDiagnostics(IncrementalValuesProvider<EquatableArray<DiagnosticInfo>> diagnostics)
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
}
