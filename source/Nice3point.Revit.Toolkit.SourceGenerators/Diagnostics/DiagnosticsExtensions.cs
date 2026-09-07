using Microsoft.CodeAnalysis;
using Nice3point.Revit.Toolkit.SourceGenerators.Models;

namespace Nice3point.Revit.Toolkit.SourceGenerators.Diagnostics;

/// <summary>
///     Extension methods for working with diagnostics from incremental generator pipelines.
/// </summary>
internal static class DiagnosticsExtensions
{
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
