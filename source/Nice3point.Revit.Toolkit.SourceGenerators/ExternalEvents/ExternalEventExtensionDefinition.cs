using Nice3point.Revit.Toolkit.SourceGenerators.Models;

namespace Nice3point.Revit.Toolkit.SourceGenerators.ExternalEvents;

internal sealed record ExternalEventExtensionDefinition(
    string ClassAccessibility,
    EquatableArray<string> TypeParameters,
    string Constraints,
    string QualifiedRecordType,
    string? FullyQualifiedReturnType,
    EquatableArray<string> ParameterTypes);
