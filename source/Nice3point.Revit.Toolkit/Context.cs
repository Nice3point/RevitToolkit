using System.Reflection;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using Nice3point.Revit.Toolkit.Utils;

namespace Nice3point.Revit.Toolkit;

/// <summary>
///     Provides members for setting and retrieving data about Revit application context.
/// </summary>
[PublicAPI]
public static class Context
{
    private static bool _suppressDialogs;
    private static bool _suppressFailures;

    private static bool _suppressFailureErrors;
    private static int? _suppressDialogCode;
    private static Action<DialogBoxShowingEventArgs>? _suppressDialogHandler;

    private static MethodInfo _apiCallDepthManagerMethod;
    private static MethodInfo _isRevitInApiModeMethod;

    static Context()
    {
        const BindingFlags staticFlags = BindingFlags.NonPublic | BindingFlags.Static;
        var apiAssembly = AppDomain.CurrentDomain.GetAssemblies().First(assembly => assembly.GetName().Name == "APIUIAPI");
        var dbAssembly = AppDomain.CurrentDomain.GetAssemblies().First(assembly => assembly.GetName().Name == "RevitDBAPI");
        
        var apiAssemblyMethods = apiAssembly.ManifestModule.GetMethods(staticFlags);
        var dbAssemblyMethods = dbAssembly.ManifestModule.GetMethods(staticFlags);

        var getApplicationMethod = dbAssemblyMethods.FirstOrDefault(info => info.Name == "RevitApplication.getApplication_");
        ThrowIfNotSupported(getApplicationMethod);

        var proxyType = dbAssembly.DefinedTypes.FirstOrDefault(info => info.FullName == "Autodesk.Revit.Proxy.ApplicationServices.ApplicationProxy");
        ThrowIfNotSupported(proxyType);

        const BindingFlags internalFlags = BindingFlags.NonPublic | BindingFlags.DeclaredOnly | BindingFlags.Instance;
        var proxyConstructor = proxyType!.GetConstructor(internalFlags, null, [getApplicationMethod!.ReturnType], null);
        ThrowIfNotSupported(proxyConstructor);

        var proxy = proxyConstructor!.Invoke([getApplicationMethod.Invoke(null, null)]);
        ThrowIfNotSupported(proxy);

        var applicationType = typeof(Application);
        var applicationConstructor = applicationType.GetConstructor(internalFlags, null, [proxyType], null);
        ThrowIfNotSupported(applicationConstructor);

        var application = (Application)applicationConstructor!.Invoke([proxy]);
        ThrowIfNotSupported(proxy);

        var apiCallDepthManagerMethod = apiAssemblyMethods.FirstOrDefault(info => info.Name == "APICallDepthManager.singletonfactory");
        ThrowIfNotSupported(apiCallDepthManagerMethod);
        
        var isRevitInApiModeMethod = apiAssemblyMethods.FirstOrDefault(info => info.Name == "APICallDepthManager.isRevitInAPIMode");
        ThrowIfNotSupported(isRevitInApiModeMethod);

        _apiCallDepthManagerMethod = apiCallDepthManagerMethod!;
        _isRevitInApiModeMethod = isRevitInApiModeMethod!;
        UiApplication = new UIApplication(application);
    }

    /// <summary>
    ///     Represents an active session of the Autodesk Revit user interface, providing access to
    ///     UI customization methods, events, the main window, and the active document.
    /// </summary>
    public static UIApplication UiApplication { get; }

    /// <summary>
    ///     Represents the database level Autodesk Revit Application, providing access to documents, options and other application wide data and settings.
    /// </summary>
    public static Application Application => UiApplication.Application;

    /// <summary>Represents a currently active Autodesk Revit project at the UI level.</summary>
    /// <remarks>
    ///     External API commands can access this property in read-only mode only.
    /// </remarks>
    /// <exception cref="T:Autodesk.Revit.Exceptions.InvalidOperationException">Thrown when attempting to modify the property.</exception>
    /// <returns>
    ///     Currently active project.<br/>
    ///     Returns <see langword="null" /> if there are no active projects.
    /// </returns>
    public static UIDocument? UiDocument => UiApplication.ActiveUIDocument;

    /// <summary>Represents a currently active Autodesk Revit project at the database level.</summary>
    /// <remarks>
    ///     Revit can have multiple projects open and multiple views to those projects.
    ///     The active or top most view will be the active project and hence the active document which is available from the Application object.<br/><br/>
    ///     Returns <see langword="null" /> if there are no active projects.
    /// </remarks>
    public static Document? Document => UiApplication.ActiveUIDocument?.Document;

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
    public static bool IsRevitInApiMode
    {
        get
        {
            var apiCallDepthManager = _apiCallDepthManagerMethod.Invoke(null, null);
            return (bool) _isRevitInApiModeMethod.Invoke(null, [apiCallDepthManager])!;
        }
    }

    /// <summary>
    ///     Suppresses the display of the Revit error and warning messages during transaction.
    /// </summary>
    /// <param name="resolveErrors">
    ///     Set <see langword="true"/> if errors should be automatically resolved, otherwise <see langword="false"/> to cancel the transaction.
    /// </param>
    /// <remarks>
    ///     By default, Revit uses manual error resolution control with user interaction.
    ///     This method provides automatic resolution of all failures without notifying the user or interrupting the program.
    /// </remarks>
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
    /// <remarks>
    ///     The range of valid result values depends on the type of dialog as follows:
    ///     <list type="number">
    ///         <item>
    ///             DialogBox: Any non-zero value will cause a dialog to be dismissed.
    ///         </item>
    ///         <item>
    ///             MessageBox: Standard Message Box IDs, such as IDOK and IDCANCEL, are accepted.
    ///             For all possible IDs, refer to the Windows API documentation.
    ///             The ID used must be relevant to the buttons in a message box.
    ///         </item>
    ///         <item>
    ///             TaskDialog: Standard Message Box IDs and Revit Custom IDs are accepted,
    ///             depending on the buttons used in a dialog. Standard buttons, such as OK
    ///             and Cancel, have standard IDs described in Windows API documentation.
    ///             Buttons with custom text have custom IDs with incremental values
    ///             starting at 1001 for the left-most or top-most button in a task dialog.
    ///         </item>
    ///     </list>
    /// </remarks>
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
    /// <remarks>
    ///     The range of valid result values depends on the type of dialog as follows:
    ///     <list type="number">
    ///         <item>
    ///             DialogBox: Any non-zero value will cause a dialog to be dismissed.
    ///         </item>
    ///         <item>
    ///             MessageBox: Standard Message Box IDs, such as IDOK and IDCANCEL, are accepted.
    ///             For all possible IDs, refer to the Windows API documentation.
    ///             The ID used must be relevant to the buttons in a message box.
    ///         </item>
    ///         <item>
    ///             TaskDialog: Standard Message Box IDs and Revit Custom IDs are accepted,
    ///             depending on the buttons used in a dialog. Standard buttons, such as OK
    ///             and Cancel, have standard IDs described in Windows API documentation.
    ///             Buttons with custom text have custom IDs with incremental values
    ///             starting at 1001 for the left-most or top-most button in a task dialog.
    ///         </item>
    ///     </list>
    /// </remarks>
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

    [ContractAnnotation("null => halt")]
    private static void ThrowIfNotSupported(object? argument)
    {
        if (argument is null)
        {
            throw new NotSupportedException("The operation is not supported by current Revit API version. Failed to retrieve the application context.");
        }
    }
}