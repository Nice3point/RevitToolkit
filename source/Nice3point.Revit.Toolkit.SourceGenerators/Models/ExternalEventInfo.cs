namespace Nice3point.Revit.Toolkit.SourceGenerators.Models;

/// <summary>
///     Contains all metadata extracted from a method marked with [ExternalEvent],
///     used by the emitter to generate source code.
/// </summary>
internal sealed record ExternalEventInfo(
    string HintName,
    string Namespace,
    string MethodName,
    bool IsStatic,
    bool ReturnsVoid,
    string? FullyQualifiedReturnType,
    string FullyQualifiedDelegateType,
    bool HasUiApplicationParameter,
    bool AllowDirectInvocation,
    EquatableArray<TypeDeclarationInfo> TypeHierarchy,
    EquatableArray<ParameterData> ExtraParameters);

/// <summary>
///     Describes a type in the containing type hierarchy (name, keyword, and whether it is static).
/// </summary>
internal sealed record TypeDeclarationInfo(
    string Keyword,
    string Name,
    bool IsStatic);

/// <summary>
///     Describes an extra parameter of the annotated method beyond the UIApplication parameter.
/// </summary>
internal sealed record ParameterData(
    string Name,
    string FullyQualifiedType);