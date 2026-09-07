using Nice3point.Revit.Toolkit.SourceGenerators.Models;

namespace Nice3point.Revit.Toolkit.SourceGenerators.ExternalEvents;

internal sealed record ContainingTypeDeclaration(
    string Keyword,
    string Name,
    bool IsStatic,
    string Accessibility,
    EquatableArray<string> TypeParameters);
