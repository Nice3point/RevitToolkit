using Microsoft.CodeAnalysis;

namespace Nice3point.Revit.Toolkit.SourceGenerators.Models;

/// <summary>
///     Represents the result of analyzing a method marked with [ExternalEvent],
///     containing either the extracted metadata or diagnostics to report.
///     Implements <see cref="IEquatable{T}"/> based on <see cref="Info"/> for incremental caching.
/// </summary>
internal readonly struct ExternalEventMethodResult(ExternalEventInfo? info, Diagnostic[]? diagnostics) : IEquatable<ExternalEventMethodResult>
{
    /// <summary>
    ///     The extracted method metadata, or <c>null</c> if validation failed.
    /// </summary>
    public ExternalEventInfo? Info { get; } = info;

    /// <summary>
    ///     Diagnostics to report, or <c>null</c> if there are none.
    /// </summary>
    public Diagnostic[]? Diagnostics { get; } = diagnostics;

    public bool Equals(ExternalEventMethodResult other) => Equals(Info, other.Info);

    public override bool Equals(object? obj) => obj is ExternalEventMethodResult other && Equals(other);
    
    public override int GetHashCode() => Info?.GetHashCode() ?? 0;
}
