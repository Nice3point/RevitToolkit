using Microsoft.CodeAnalysis;

namespace Nice3point.Revit.Toolkit.Analyzers.Diagnostics;

/// <summary>
///     Contains all <see cref="DiagnosticDescriptor" /> instances for errors and warnings
///     reported by the analyzers and source generators in this project.
/// </summary>
internal static class DiagnosticDescriptors
{
    public static readonly DiagnosticDescriptor ExternalEventTaskReturnNotSupported = new(
        "RVTTK0001",
        "Method returns Task",
        "Method '{0}' marked with [ExternalEvent] must not return Task or Task<T>; use a void or value-returning signature instead",
        "ExternalEventGenerator",
        DiagnosticSeverity.Error,
        true);

    public static readonly DiagnosticDescriptor ExternalEventAsyncVoidMethod = new(
        "RVTTK0002",
        "Method is async void",
        "Method '{0}' marked with [ExternalEvent] should not be async void; it is called synchronously in the Revit API context",
        "ExternalEventGenerator",
        DiagnosticSeverity.Warning,
        true);

    public static readonly DiagnosticDescriptor ExternalEventGenericMethod = new(
        "RVTTK0003",
        "Method is generic",
        "Method '{0}' marked with [ExternalEvent] must not be generic",
        "ExternalEventGenerator",
        DiagnosticSeverity.Error,
        true);

    public static readonly DiagnosticDescriptor ExternalEventDuplicateMethodOverload = new(
        "RVTTK0004",
        "Duplicate method overloads",
        "Method '{0}' marked with [ExternalEvent] must not have overloads also marked with [ExternalEvent]",
        "ExternalEventGenerator",
        DiagnosticSeverity.Error,
        true);

    public static readonly DiagnosticDescriptor ExternalEventContainingTypeNotPartial = new(
        "RVTTK0005",
        "Containing type is not partial",
        "The type '{0}' containing method '{1}' marked with [ExternalEvent] must be declared as partial",
        "ExternalEventGenerator",
        DiagnosticSeverity.Error,
        true);
}
