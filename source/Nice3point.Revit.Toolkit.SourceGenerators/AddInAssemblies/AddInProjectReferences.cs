using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;
using Nice3point.Revit.Toolkit.SourceGenerators.Models;

namespace Nice3point.Revit.Toolkit.SourceGenerators.AddInAssemblies;

/// <summary>
///     Reads the project references listed by the package targets.
/// </summary>
internal static class AddInProjectReferences
{
    private const string PropertyName = "build_property._RevitToolkitAddInProjectReferences";

    /// <summary>
    ///     Reads the assembly names of the project references from the global analyzer config options.
    /// </summary>
    /// <param name="globalOptions">The global analyzer config options of the compilation.</param>
    /// <returns>The distinct assembly names in ordinal order, or an empty array for a project without project references.</returns>
    public static EquatableArray<string> Read(AnalyzerConfigOptions globalOptions)
    {
        if (!globalOptions.TryGetValue(PropertyName, out var value))
        {
            return ImmutableArray<string>.Empty;
        }

        return value.Split(',')
            .Select(static assemblyName => assemblyName.Trim())
            .Where(static assemblyName => assemblyName.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static assemblyName => assemblyName, StringComparer.Ordinal)
            .ToImmutableArray();
    }
}
