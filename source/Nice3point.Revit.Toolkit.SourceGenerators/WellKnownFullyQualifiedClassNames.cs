using Nice3point.Revit.Toolkit.SourceGenerators.Models;

namespace Nice3point.Revit.Toolkit.SourceGenerators;

/// <summary>
///     Centralized catalog of well-known fully qualified type names
///     used throughout the source generators for consistent type references.
/// </summary>
internal static class WellKnownFullyQualifiedClassNames
{
    // Toolkit types
    public static readonly FullyQualifiedTypeName ExternalEvent = "Nice3point.Revit.Toolkit.External.ExternalEvent";
    public static readonly FullyQualifiedTypeName AsyncExternalEvent = "Nice3point.Revit.Toolkit.External.AsyncExternalEvent";
    public static readonly FullyQualifiedTypeName ExternalEventHandler = "Nice3point.Revit.Toolkit.External.ExternalEventHandler";
    public static readonly FullyQualifiedTypeName ExternalEventInterface = "Nice3point.Revit.Toolkit.External.IExternalEvent";
    public static readonly FullyQualifiedTypeName AsyncExternalEventInterface = "Nice3point.Revit.Toolkit.External.IAsyncExternalEvent";
    public static readonly FullyQualifiedTypeName ExternalEventOptions = "Nice3point.Revit.Toolkit.External.ExternalEventOptions";
    public static readonly FullyQualifiedTypeName RevitContext = "Nice3point.Revit.Toolkit.External.RevitContext";
    public static readonly FullyQualifiedTypeName ExternalEventAttribute = "Nice3point.Revit.Toolkit.External.ExternalEventAttribute";

    // Revit types
    public static readonly FullyQualifiedTypeName UiApplication = "Autodesk.Revit.UI.UIApplication";

    // System types
    public static readonly FullyQualifiedTypeName Task = "System.Threading.Tasks.Task";
    public static readonly FullyQualifiedTypeName TaskGeneric = "System.Threading.Tasks.Task`1";
    public static readonly FullyQualifiedTypeName TaskCompletionSource = "System.Threading.Tasks.TaskCompletionSource";
    public static readonly FullyQualifiedTypeName TaskCreationOptions = "System.Threading.Tasks.TaskCreationOptions";
    public static readonly FullyQualifiedTypeName Action = "System.Action";
    public static readonly FullyQualifiedTypeName Func = "System.Func";
    public static readonly FullyQualifiedTypeName Exception = "System.Exception";
}
