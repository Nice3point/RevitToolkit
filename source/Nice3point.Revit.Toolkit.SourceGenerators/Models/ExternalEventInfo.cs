namespace Nice3point.Revit.Toolkit.SourceGenerators.Models;

/// <summary>
///     Contains all metadata extracted from a method marked with [ExternalEvent],
///     used by the emitter to generate source code.
/// </summary>
internal sealed record ExternalEventInfo(
    string HintName,
    string Namespace,
    EquatableArray<TypeDeclarationInfo> TypeHierarchy,
    string MethodName,
    bool IsStatic,
    bool IsVoidReturn,
    string? ReturnTypeFullyQualified,
    bool HasUiApplicationParam,
    EquatableArray<ParameterData> ExtraParameters,
    bool AllowDirectInvocation);

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
