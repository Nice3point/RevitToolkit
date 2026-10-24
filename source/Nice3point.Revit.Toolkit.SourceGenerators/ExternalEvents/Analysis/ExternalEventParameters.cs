using Microsoft.CodeAnalysis;
using Nice3point.Revit.Toolkit.SourceGenerators.CSharp;

namespace Nice3point.Revit.Toolkit.SourceGenerators.ExternalEvents.Analysis;

/// <summary>
///     Classifies the parameters of a method marked with [ExternalEvent].
/// </summary>
/// <remarks>
///     The event supplies a leading <c>UIApplication</c> parameter and a trailing <c>CancellationToken</c> parameter.
///     The parameters between them are the arguments the caller passes when raising the event.
/// </remarks>
internal static class ExternalEventParameters
{
    private static readonly SymbolDisplayFormat TypeNameFormat = new(
        SymbolDisplayGlobalNamespaceStyle.Omitted,
        SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);

    /// <summary>
    ///     Determines whether the first parameter of the method receives the current <c>UIApplication</c>.
    /// </summary>
    public static bool HasUiApplicationParameter(IMethodSymbol method)
    {
        return method.Parameters.Length > 0 && IsType(method.Parameters[0].Type, ExternalEventTypeNames.UiApplication);
    }

    /// <summary>
    ///     Determines whether the last parameter of the method receives the cancellation token of the raise.
    /// </summary>
    public static bool HasCancellationTokenParameter(IMethodSymbol method)
    {
        var firstArgumentIndex = HasUiApplicationParameter(method) ? 1 : 0;
        return method.Parameters.Length > firstArgumentIndex && IsType(method.Parameters[^1].Type, ExternalEventTypeNames.CancellationToken);
    }

    /// <summary>
    ///     Gets the parameters the caller passes when raising the event, in declaration order.
    /// </summary>
    public static IEnumerable<IParameterSymbol> GetArgumentParameters(IMethodSymbol method)
    {
        var firstArgumentIndex = HasUiApplicationParameter(method) ? 1 : 0;
        var argumentCount = method.Parameters.Length - firstArgumentIndex - (HasCancellationTokenParameter(method) ? 1 : 0);
        return method.Parameters.Skip(firstArgumentIndex).Take(argumentCount);
    }

    private static bool IsType(ITypeSymbol type, FullyQualifiedTypeName typeName)
    {
        return type.ToDisplayString(TypeNameFormat) == typeName.WithoutGlobalPrefix;
    }
}
