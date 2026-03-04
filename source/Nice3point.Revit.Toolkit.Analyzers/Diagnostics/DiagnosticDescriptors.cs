using Microsoft.CodeAnalysis;

namespace Nice3point.Revit.Toolkit.Analyzers.Diagnostics;

/// <summary>
///     Contains all <see cref="DiagnosticDescriptor"/> instances for errors and warnings
///     reported by the analyzers and source generators in this project.
/// </summary>
internal static class DiagnosticDescriptors
{
    public static readonly DiagnosticDescriptor TaskReturnNotSupported = new(
        id: "RVTTK0001",
        title: "Method returns Task",
        messageFormat: "Method '{0}' marked with [ExternalEvent] must not return Task or Task<T>; use a void or value-returning signature instead",
        category: "ExternalEventGenerator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ContainingTypeNotPartial = new(
        id: "RVTTK0002",
        title: "Containing type is not partial",
        messageFormat: "The type '{0}' containing method '{1}' marked with [ExternalEvent] must be declared as partial",
        category: "ExternalEventGenerator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MethodIsGeneric = new(
        id: "RVTTK0003",
        title: "Method is generic",
        messageFormat: "Method '{0}' marked with [ExternalEvent] must not be generic",
        category: "ExternalEventGenerator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DuplicateMethodOverload = new(
        id: "RVTTK0004",
        title: "Duplicate method overloads",
        messageFormat: "Method '{0}' marked with [ExternalEvent] must not have overloads also marked with [ExternalEvent]",
        category: "ExternalEventGenerator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MethodIsAsyncVoid = new(
        id: "RVTTK0005",
        title: "Method is async void",
        messageFormat: "Method '{0}' marked with [ExternalEvent] should not be async void; this can cause unobserved exceptions",
        category: "ExternalEventGenerator",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
}
