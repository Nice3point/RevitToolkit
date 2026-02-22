using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using JetBrains.Annotations;
using Nice3point.Revit.Toolkit.Utils;

namespace Nice3point.Revit.Toolkit;

/// <summary>
///     Provides members for setting and retrieving data about Revit application context.
/// </summary>
[PublicAPI]
[Obsolete("Use RevitContext or RevitApiContext instead")]
public static class Context
{
    //Global state
    private static bool _suppressDialogs;
    private static bool _suppressFailures;

    private static bool _suppressFailureErrors;
    private static int? _suppressDialogCode;
    private static Action<DialogBoxShowingEventArgs>? _suppressDialogHandler;

    private static readonly Func<bool> GetIsRevitInApiMode;

    static Context()
    {
        const BindingFlags staticFlags = BindingFlags.NonPublic | BindingFlags.Static;
        var apiAssembly = AppDomain.CurrentDomain.GetAssemblies().First(assembly => assembly.GetName().Name == "APIUIAPI");
        var dbAssembly = AppDomain.CurrentDomain.GetAssemblies().First(assembly => assembly.GetName().Name == "RevitDBAPI");

        var apiAssemblyMethods = apiAssembly.ManifestModule.GetMethods(staticFlags);
        var dbAssemblyMethods = dbAssembly.ManifestModule.GetMethods(staticFlags);

        var getApplicationMethod = dbAssemblyMethods.FirstOrDefault(info => info.Name == "RevitApplication.getApplication_");
        ThrowWhen(getApplicationMethod is null);

        var proxyType = dbAssembly.DefinedTypes.FirstOrDefault(info => info.FullName == "Autodesk.Revit.Proxy.ApplicationServices.ApplicationProxy");
        ThrowWhen(proxyType is null);

        const BindingFlags internalFlags = BindingFlags.NonPublic | BindingFlags.DeclaredOnly | BindingFlags.Instance;
        var proxyConstructor = proxyType.GetConstructor(internalFlags, null, [getApplicationMethod.ReturnType], null);
        ThrowWhen(proxyConstructor is null);

        var proxy = proxyConstructor.Invoke([getApplicationMethod.Invoke(null, null)]);
        ThrowWhen(proxy is null);

        var apiCallDepthManagerMethod = apiAssemblyMethods.FirstOrDefault(info => info.Name == "APICallDepthManager.singletonfactory");
        ThrowWhen(apiCallDepthManagerMethod is null);

        var isRevitInApiModeMethod = apiAssemblyMethods.FirstOrDefault(info => info.Name == "APICallDepthManager.isRevitInAPIMode");
        ThrowWhen(isRevitInApiModeMethod is null);

        GetIsRevitInApiMode = () =>
        {
            var apiCallDepthManager = apiCallDepthManagerMethod.Invoke(null, null);
            return (bool)isRevitInApiModeMethod.Invoke(null, [apiCallDepthManager])!;
        };

#if NET8_0_OR_GREATER
        Application = UnsafeAccessors.CreateApplication(proxy);
#else
        var applicationType = typeof(Application);
        var applicationConstructor = applicationType.GetConstructor(internalFlags, null, [proxyType], null);
        ThrowWhen(applicationConstructor is null);

        var application = (Application)applicationConstructor.Invoke([proxy]);
        ThrowWhen(application is null);

        Application = application;
#endif
        UiApplication = new UIApplication(Application);
    }

    /// <summary>
    ///     Represents an active session of the Autodesk Revit user interface, providing access to
    ///     UI customization methods, events, the main window, and the active document.
    /// </summary>
    [Obsolete("Use RevitContext.UiApplication instead")]
    [CodeTemplate(
        searchTemplate: "Context.UiApplication",
        Message = "Context.UiApplication is obsolete, use RevitContext.UiApplication instead",
        ReplaceTemplate = "RevitContext.UiApplication",
        ReplaceMessage = "Replace with RevitContext.UiApplication")]
    public static UIApplication UiApplication { get; }

    /// <summary>
    ///     Represents the Autodesk Revit user interface, providing access to UI customization methods and events.
    /// </summary>
    [Obsolete("Use RevitContext.UiControlledApplication instead")]
    [CodeTemplate(
        searchTemplate: "Context.UiControlledApplication",
        Message = "Context.UiControlledApplication is obsolete, use RevitContext.UiControlledApplication instead",
        ReplaceTemplate = "RevitContext.UiControlledApplication",
        ReplaceMessage = "Replace with RevitContext.UiControlledApplication")]
    public static UIControlledApplication UiControlledApplication =>
#if NET8_0_OR_GREATER
        UnsafeAccessors.CreateUiControlledApplication(UiApplication);
#else
        (UIControlledApplication)Activator.CreateInstance(
            typeof(UIControlledApplication),
            BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            [UiApplication],
            null)!;
#endif

    /// <summary>
    ///     Represents the database level Autodesk Revit Application, providing access to documents, options and other application wide data and settings.
    /// </summary>
    [Obsolete("Use RevitApiContext.Application instead")]
    [CodeTemplate(
        searchTemplate: "Context.Application",
        Message = "Context.Application is obsolete, use RevitApiContext.Application instead",
        ReplaceTemplate = "RevitApiContext.Application",
        ReplaceMessage = "Replace with RevitApiContext.Application")]
    public static Application Application { get; }

    /// <summary>Represents a currently active Autodesk Revit project at the UI level.</summary>
    /// <remarks>
    ///     External API commands can access this property in read-only mode only.
    /// </remarks>
    /// <exception cref="T:Autodesk.Revit.Exceptions.InvalidOperationException">Thrown when attempting to modify the property.</exception>
    /// <returns>
    ///     Currently active project.<br/>
    ///     Returns <see langword="null" /> if there are no active projects.
    /// </returns>
    [Obsolete("Use RevitContext.ActiveUiDocument instead")]
    [CodeTemplate(
        searchTemplate: "Context.ActiveUiDocument",
        Message = "Context.ActiveUiDocument is obsolete, use RevitContext.ActiveUiDocument instead",
        ReplaceTemplate = "RevitContext.ActiveUiDocument",
        ReplaceMessage = "Replace with RevitContext.ActiveUiDocument")]
    public static UIDocument? ActiveUiDocument => UiApplication.ActiveUIDocument;

    /// <summary>Represents a currently active Autodesk Revit project at the database level.</summary>
    /// <remarks>
    ///     Revit can have multiple projects open and multiple views to those projects.
    ///     The active or top most view will be the active project and hence the active document which is available from the Application object.<br/><br/>
    ///     Returns <see langword="null" /> if there are no active projects.
    /// </remarks>
    [Obsolete("Use RevitContext.ActiveDocument instead")]
    [CodeTemplate(
        searchTemplate: "Context.ActiveDocument",
        Message = "Context.ActiveDocument is obsolete, use RevitContext.ActiveDocument instead",
        ReplaceTemplate = "RevitContext.ActiveDocument",
        ReplaceMessage = "Replace with RevitContext.ActiveDocument")]
    public static Document? ActiveDocument => UiApplication.ActiveUIDocument?.Document;

    /// <summary>Represents the currently active view of the currently active document.</summary>
    /// <remarks>
    ///     <para>
    ///         This property is applicable to the currently active document only.<br/>
    ///         Returns <see langword="null" /> if there are no active projects.
    ///     </para>
    ///     <para>
    ///         The active view can only be changed when:
    ///         <ul>
    ///             <li>There is no open transaction.</li><li><see cref="P:Autodesk.Revit.DB.Document.IsModifiable" /> is false.</li>
    ///             <li><see cref="P:Autodesk.Revit.DB.Document.IsReadOnly" /> is false.</li>
    ///             <li>ViewActivating, ViewActivated, and any pre-action of events (such as DocumentSaving or DocumentClosing events) are not being handled.</li>
    ///         </ul>
    ///     </para>
    /// </remarks>
    /// <exception cref="T:Autodesk.Revit.Exceptions.ArgumentNullException">
    ///     When setting the property: If the 'view' argument is NULL.
    /// </exception>
    /// <exception cref="T:Autodesk.Revit.Exceptions.ArgumentException">
    ///     When setting the property:
    ///     <ul>
    ///         <li>If the given view is not a valid view of the document; -or-</li><li>If the given view is a template view; -or-</li><li>If the given view is an internal view.</li>
    ///     </ul>
    /// </exception>
    /// <exception cref="T:Autodesk.Revit.Exceptions.InvalidOperationException">
    ///     <para>
    ///         When setting the property:
    ///         <ul>
    ///             <li>If the document is not currently active; -or-</li><li>If the document is currently modifiable (i.e. with an active transaction); -or-</li>
    ///             <li>If the document is currently in read-only state; -or-</li><li>When invoked during either ViewActivating or ViewActivated event; -or-</li>
    ///             <li>When invoked during any pre-action kind of event, such as DocumentSaving, DocumentClosing, etc.</li>
    ///             <li>When there are no active documents in the current Autodesk Revit session</li>
    ///         </ul>
    ///     </para>
    /// </exception>
    [Obsolete("Use RevitContext.ActiveView instead")]
    [CodeTemplate(
        searchTemplate: "Context.ActiveView",
        Message = "Context.ActiveView is obsolete, use RevitContext.ActiveView instead",
        ReplaceTemplate = "RevitContext.ActiveView",
        ReplaceMessage = "Replace with RevitContext.ActiveView")]
    public static View? ActiveView
    {
        get => UiApplication.ActiveUIDocument?.ActiveView;
        set
        {
            if (UiApplication.ActiveUIDocument is null) throw new InvalidOperationException("There are no active documents in the current Autodesk Revit session");
            UiApplication.ActiveUIDocument.ActiveView = value;
        }
    }

    /// <summary>Represents the currently active graphical view of the currently active document.</summary>
    /// <remarks>
    ///     This property is applicable to the currently active document only.
    ///     Returns <see langword="null" /> if there are no active projects.
    /// </remarks>
    [Obsolete("Use RevitContext.ActiveGraphicalView instead")]
    [CodeTemplate(
        searchTemplate: "Context.ActiveGraphicalView",
        Message = "Context.ActiveGraphicalView is obsolete, use RevitContext.ActiveGraphicalView instead",
        ReplaceTemplate = "RevitContext.ActiveGraphicalView",
        ReplaceMessage = "Replace with RevitContext.ActiveGraphicalView")]
    public static View? ActiveGraphicalView => UiApplication.ActiveUIDocument?.ActiveGraphicalView;

    /// <summary>
    ///     Determines whether Revit is in API mode or not.
    /// </summary>
    /// <remarks>
    ///     If Revit is within an API context, direct API calls should be used.
    ///     Otherwise, when Revit is outside the API context, API calls should be handled 
    ///     through the <see cref="Autodesk.Revit.UI.IExternalEventHandler"/> interface.
    ///     IExternalEventHandler enables safely executing commands and operations from external threads 
    ///     or the user interface, ensuring they are synchronized with Revit's main thread.
    /// </remarks>
    [Obsolete("Use RevitContext.IsRevitInApiMode instead")]
    [CodeTemplate(
        searchTemplate: "Context.IsRevitInApiMode",
        Message = "Context.IsRevitInApiMode is obsolete, use RevitContext.IsRevitInApiMode instead",
        ReplaceTemplate = "RevitContext.IsRevitInApiMode",
        ReplaceMessage = "Replace with RevitContext.IsRevitInApiMode")]
    public static bool IsRevitInApiMode => GetIsRevitInApiMode();

    /// <summary>
    ///     Suppresses the display of the Revit error and warning messages during transaction.
    /// </summary>
    /// <param name="resolveErrors">
    ///     Set <see langword="true"/> if errors should be automatically resolved, otherwise <see langword="false"/> to cancel the transaction.
    /// </param>
    [Obsolete("Use RevitApiContext.BeginFailureSuppressionScope instead")]
    [CodeTemplate(
        searchTemplate: "Context.SuppressFailures($args$)",
        Message = "Context.SuppressFailures is obsolete, use RevitApiContext.BeginFailureSuppressionScope with 'using' statement instead",
        ReplaceTemplate = "RevitApiContext.BeginFailureSuppressionScope($args$)",
        ReplaceMessage = "Replace with RevitApiContext.BeginFailureSuppressionScope")]
    public static void SuppressFailures(bool resolveErrors = true)
    {
        if (_suppressFailures)
        {
            _suppressFailureErrors = resolveErrors;
            return;
        }

        _suppressFailures = true;
        _suppressFailureErrors = resolveErrors;
        Application.FailuresProcessing += ResolveFailures;
    }

    /// <summary>
    ///     Suppresses the display of the Revit dialogs.
    /// </summary>
    /// <param name="resultCode">The result code you wish the Revit dialog to return.</param>
    [Obsolete("Use RevitContext.BeginDialogSuppressionScope instead")]
    [CodeTemplate(
        searchTemplate: "Context.SuppressDialogs($args$)",
        Message = "Context.SuppressDialogs is obsolete, use RevitContext.BeginDialogSuppressionScope with 'using' statement instead",
        ReplaceTemplate = "RevitContext.BeginDialogSuppressionScope($args$)",
        ReplaceMessage = "Replace with RevitContext.BeginDialogSuppressionScope")]
    public static void SuppressDialogs(int resultCode = 1)
    {
        if (_suppressDialogs)
        {
            _suppressDialogCode = resultCode;
            return;
        }

        _suppressDialogs = true;
        _suppressDialogCode = resultCode;
        UiApplication.DialogBoxShowing += ResolveDialogBox;
    }

    /// <summary>
    ///     Suppresses the display of the Revit dialogs.
    /// </summary>
    /// <param name="handler">Suppress handler.</param>
    [Obsolete("Use RevitContext.BeginDialogSuppressionScope instead")]
    [CodeTemplate(
        searchTemplate: "Context.SuppressDialogs($args$)",
        Message = "Context.SuppressDialogs is obsolete, use RevitContext.BeginDialogSuppressionScope with 'using' statement instead",
        ReplaceTemplate = "RevitContext.BeginDialogSuppressionScope($args$)",
        ReplaceMessage = "Replace with RevitContext.BeginDialogSuppressionScope")]
    public static void SuppressDialogs(Action<DialogBoxShowingEventArgs> handler)
    {
        if (_suppressDialogs)
        {
            _suppressDialogHandler = handler;
            return;
        }

        _suppressDialogs = true;
        _suppressDialogHandler = handler;
        UiApplication.DialogBoxShowing += ResolveDialogBox;
    }

    /// <summary>
    ///     Restores display of the Revit dialogs.
    /// </summary>
    [Obsolete("Use RevitContext.BeginDialogSuppressionScope instead for automatic resource management")]
    public static void RestoreDialogs()
    {
        _suppressDialogs = false;
        _suppressDialogCode = null;
        _suppressDialogHandler = null;
        UiApplication.DialogBoxShowing -= ResolveDialogBox;
    }

    /// <summary>
    ///     Restores failure handling.
    /// </summary>
    [Obsolete("Use RevitApiContext.BeginFailureSuppressionScope instead for automatic resource management")]
    public static void RestoreFailures()
    {
        _suppressFailures = false;
        Application.FailuresProcessing -= ResolveFailures;
    }

    private static void ResolveDialogBox(object? sender, DialogBoxShowingEventArgs args)
    {
        if (_suppressDialogCode.HasValue)
        {
            args.OverrideResult(_suppressDialogCode.Value);
            return;
        }

        _suppressDialogHandler?.Invoke(args);
    }

    private static void ResolveFailures(object? sender, FailuresProcessingEventArgs args)
    {
        var failuresAccessor = args.GetFailuresAccessor();
        var result = _suppressFailureErrors ? FailureUtils.ResolveFailures(failuresAccessor) : FailureUtils.DismissFailures(failuresAccessor);

        args.SetProcessingResult(result);
    }

    /// <summary>
    ///     Dynamically throw when the <paramref name="condition"/> is <c>true</c>.
    /// </summary>
    private static void ThrowWhen([DoesNotReturnIf(true)] bool condition)
    {
        if (condition)
        {
            throw new NotSupportedException("The operation is not supported by current Revit API version. Failed to retrieve the application context.");
        }
    }
}