using System.Collections.Immutable;
using Nice3point.Revit.Toolkit.SourceGenerators.Diagnostics;
using Nice3point.Revit.Toolkit.SourceGenerators.Models;

namespace Nice3point.Revit.Toolkit.SourceGenerators.ExternalEvents;

/// <summary>
///     Represents the result of analyzing a method marked with [ExternalEvent],
///     containing either the extracted metadata or diagnostics to report.
///     Uses <see cref="EquatableArray{T}" /> of <see cref="DiagnosticInfo" /> for proper incremental caching.
/// </summary>
internal readonly record struct ExternalEventAnalysis(ExternalEventDefinition? Definition, EquatableArray<DiagnosticInfo> Diagnostics)
{
    public ExternalEventAnalysis(ImmutableArray<DiagnosticInfo> diagnostics) : this(null, diagnostics)
    {
    }

    public ExternalEventAnalysis(ExternalEventDefinition eventDefinition) : this(eventDefinition, [])
    {
    }
}
