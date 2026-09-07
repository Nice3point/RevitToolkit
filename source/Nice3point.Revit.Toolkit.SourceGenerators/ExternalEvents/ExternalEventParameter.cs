namespace Nice3point.Revit.Toolkit.SourceGenerators.ExternalEvents;

internal sealed record ExternalEventParameter(
    string Name,
    string FullyQualifiedType,
    string RecordPropertyName);
