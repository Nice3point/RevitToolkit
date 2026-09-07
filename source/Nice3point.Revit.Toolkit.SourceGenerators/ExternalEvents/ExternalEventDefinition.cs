using Nice3point.Revit.Toolkit.SourceGenerators.Models;

namespace Nice3point.Revit.Toolkit.SourceGenerators.ExternalEvents;

/// <summary>
///     Contains all metadata extracted from a method marked with [ExternalEvent],
///     used by the emitter to generate source code.
/// </summary>
internal sealed record ExternalEventDefinition(
    string HintName,
    string Namespace,
    string MethodName,
    bool IsStatic,
    bool ReturnsVoid,
    string? FullyQualifiedReturnType,
    string FullyQualifiedDelegateType,
    bool HasUiApplicationParameter,
    bool AllowDirectInvocation,
    EquatableArray<ContainingTypeDeclaration> TypeHierarchy,
    EquatableArray<ExternalEventParameter> ExtraParameters,
    ExternalEventExtensionDefinition Extension,
    EquatableArray<string> ReservedMemberNames);
