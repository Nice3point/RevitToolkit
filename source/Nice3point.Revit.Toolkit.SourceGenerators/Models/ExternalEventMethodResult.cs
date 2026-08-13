using System.Collections.Immutable;

namespace Nice3point.Revit.Toolkit.SourceGenerators.Models;

/// <summary>
///     Represents the result of analyzing a method marked with [ExternalEvent],
///     containing either the extracted metadata or diagnostics to report.
///     Uses <see cref="EquatableArray{T}" /> of <see cref="DiagnosticInfo" /> for proper incremental caching.
/// </summary>
internal readonly record struct ExternalEventMethodResult(ExternalEventInfo? Info, EquatableArray<DiagnosticInfo> Diagnostics)
{
    public ExternalEventMethodResult(ImmutableArray<DiagnosticInfo> diagnostics) : this(null, diagnostics)
    {
    }

    public ExternalEventMethodResult(ExternalEventInfo info) : this(info, [])
    {
    }
}
